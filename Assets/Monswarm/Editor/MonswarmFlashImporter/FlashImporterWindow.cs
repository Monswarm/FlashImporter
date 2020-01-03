using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AssetUsageDetectorNamespace.Extras;
using UnityEditor;
using UnityEngine;
using static Monswarm.Editor.MonswarmFlashImporter.JSONAtlas;
using Debug = UnityEngine.Debug;

namespace Monswarm.Editor.MonswarmFlashImporter
{
    public class FlashImporterWindow : EditorWindow
    {
        private Texture2D spriteSheet;
        private TextAsset _textAsset;
        private string _textAssetPath;
        private string _layersInfoAssetPath;
        private string _spriteAssetPath;
        private TextAsset _layersInfoAsset;
        private bool _removeDuplicatedImages = true;
        private bool _moveFlashFiles = true;
        private string _flashFolderName = "AtlasFlashFiles";
        private string _flashFolderNamePath;
        private bool _debugLog = false;
        private bool _renameFiles = false;
        private string _fileNameBase;

        private string _symbolName;
        private Dictionary<string, string> _layersDictionary = null;

        private float _logoX = 10;
        private float _logoY = 10;
        private float _logoHeight;
        
        [MenuItem("Window/Monswarm/Flash Importer")]
        static void Init()
        {
            FlashImporterWindow window = (FlashImporterWindow) EditorWindow.GetWindow(typeof(FlashImporterWindow));
            window.titleContent = new GUIContent("Flash Sprite Sheet Importer", "Flash Sprite Sheet Importer");
            window.Show();
        }


        void OnGUI()
        {
            const int logoMargin = 20;

            GuiDrawLogo();
            GUILayout.Space(_logoHeight + logoMargin);
            GuiTextureSection();
            GuiSpritesheetInfoSection();
            GUIExportOptionsSection();
            GuiDebugResultSection();
        }

        /// <summary>
        /// Draw an image centered on the top of the editor window
        /// </summary>
        private void GuiDrawLogo()
        {
            Texture2D logoImage = null;
            GetLogoFromFolder(ref logoImage);

            if (logoImage != null)
            {
                // Calculates the x coordinate in order to center the logo images
                // Only calculates when the window editor is larger than the image
                if (position.width > logoImage.width)
                {
                    _logoX = position.width / 2;
                    _logoX -= (float)logoImage.width / 2;
                }

                //GUI.DrawTexture(new Rect(_logoX, _logoY, _logoWidth, _logoHeight), _logoImage);
                //EditorGUI.DrawPreviewTexture(new Rect(_logoX, _logoY, _logoWidth, _logoHeight), _logoImage);
                GUI.DrawTexture(new Rect(_logoX, _logoY, logoImage.width, logoImage.height), logoImage);

                _logoHeight = logoImage.height;
            }
        }

        /// <summary>
        /// Draws the texture section.
        /// This method allows the user to assign an sprite atlas.
        /// </summary>
        private void GuiTextureSection()
        {
            GUILayout.Label("Texture", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Texture2D newSpriteSheet =
                (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
            if (newSpriteSheet != null)
            {
                spriteSheet = newSpriteSheet;
            }
            if (EditorGUI.EndChangeCheck())
                SpriteSheetChanged();
        }

        /// <summary>
        /// Called when a user changes the spritemap property.
        /// This function tries to get the animation and spritemap JSON files and associated it with the correct property. Is a QoL function.
        /// </summary>
        private void SpriteSheetChanged()
        {
            // Clean JSON properties
            _textAsset = null;
            _textAssetPath = null;
            _layersInfoAsset = null;
            _layersInfoAssetPath = null;

            //Find Animation and Spritemap files
            string spritePath = AssetDatabase.GetAssetPath(spriteSheet);
            spritePath = spritePath.Remove(spritePath.LastIndexOf("/", StringComparison.Ordinal));

            FindJSONFile(spritePath, "spritemap.json", ref _textAsset, ref _textAssetPath);
            FindJSONFile(spritePath, "Animation.json", ref _layersInfoAsset, ref _layersInfoAssetPath);
        }


        /// <summary>
        /// Find the JSON file and assign it to the property in the editor window
        /// </summary>
        /// <param name="filePath">Root folder to search</param>
        /// <param name="fileName">File to find</param>
        /// <param name="propertyToAssignate">Property in the editor window that we want to change</param>
        /// <param name="propertyPathToAssignate">Associated disk path from the property changed. This parameter will be used in other functions.</param>
        private void FindJSONFile(string filePath, string fileName, ref TextAsset propertyToAssignate, ref string propertyPathToAssignate)
        {
            string fullFilePath = filePath + "/" + fileName;

            propertyToAssignate = (TextAsset)AssetDatabase.LoadAssetAtPath(filePath + "/" + fileName, typeof(TextAsset));
            if (propertyToAssignate != null)
                propertyPathToAssignate = AssetDatabase.GetAssetPath(propertyToAssignate);
            else
            {
                DebugLog(fileName + " file not found");
                fullFilePath = null;
            }
        }

        /// <summary>
        /// This method allows the user to assign the data information generated by Flash Animate.
        /// </summary>
        private void GuiSpritesheetInfoSection()
        {
            GUILayout.Label("Sprite sheet Information", EditorStyles.boldLabel);
            _textAsset = (TextAsset)EditorGUILayout.ObjectField("Sprite Sheet JSON", _textAsset, typeof(TextAsset), false);
            _textAssetPath = AssetDatabase.GetAssetPath(_textAsset);

            _layersInfoAsset =
                (TextAsset)EditorGUILayout.ObjectField("Animation JSON", _layersInfoAsset, typeof(TextAsset), false);
            _layersInfoAssetPath = AssetDatabase.GetAssetPath(_layersInfoAsset);
        }

        /// <summary>
        /// Export options section.
        /// </summary>
        private void GUIExportOptionsSection()
        {
            GUILayout.Label("Export options", EditorStyles.boldLabel);

            _removeDuplicatedImages = EditorGUILayout.Toggle("Remove duplicated images", _removeDuplicatedImages);

            _renameFiles = EditorGUILayout.Toggle("Rename files?", _renameFiles);
            if (_renameFiles)
            {
                _fileNameBase = EditorGUILayout.TextField("File name base: ", _fileNameBase);
                _moveFlashFiles = EditorGUILayout.Toggle("Move Flash files?", _moveFlashFiles);
                if (_moveFlashFiles)
                    _flashFolderName = EditorGUILayout.TextField("Flash folder name: ", _flashFolderName);
            }

            _debugLog = EditorGUILayout.Toggle("Display debug information", _debugLog);
        }

        /// <summary>
        /// Displays information to the user on the bottom of the editor window.
        /// </summary>
        private void GuiDebugResultSection()
        {
            GUILayout.Space(10);
            if (_textAsset != null && spriteSheet != null)
            {
                if (GUILayout.Button("Import Sprites"))
                {
                    bool result = ParseLayersInformation();
                    if (!result)
                    {
                        Debug.LogError("Failed to read animation/spritesheet information.");
                        return;
                    }

                    result = ParseAtlasInformation();
                    if (!result)
                        Debug.LogError("Failed to import sprite sheet");

                    if (_renameFiles)
                        RenameAllFiles();
                }
            }
            else
            {
                GUILayout.Label("Import information", EditorStyles.boldLabel);
                GUILayout.Label("Please select a sprite sheet and text asset to import sprite sheet", EditorStyles.helpBox);
            }
        }

        /// <summary>
        /// Call the rename file procedure for all the files acceded by the tool
        /// </summary>
        private void RenameAllFiles()
        {
            CreateFlashFolder();

            RenameFile(_textAssetPath, "Sheet", _moveFlashFiles);
            RenameFile(_layersInfoAssetPath, "Anim", _moveFlashFiles);
            RenameFile(_spriteAssetPath, "Sprite", false);

            // We have to force Unity to refresh the Asset Database in order to synchronize the Project window
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Obtain the path where the sprite is located and create the flash folder
        /// </summary>
        private void CreateFlashFolder()
        {
            if (_moveFlashFiles)
            {
                // Obtain the root folder of the sprite. In this folder we will create a folder for the flash files
                string spritePath = AssetDatabase.GetAssetPath(spriteSheet);
                spritePath = spritePath.Remove(spritePath.LastIndexOf("/", StringComparison.Ordinal));
                _flashFolderNamePath = spritePath.ToString().Replace('\\', '/') + "/" + _flashFolderName;
                System.IO.Directory.CreateDirectory(_flashFolderNamePath);
            }
        }

        /// <summary>
        /// Rename a file and attach it the base name (added by the user in the tool window) + predefined suffix
        /// Meta files are renamed too to avoid the sprite split to be reseted
        /// </summary>
        /// <param name="filePath">Original file path</param>
        /// <param name="suffix">Suffix to be applied, usually: "Sheet" for spritmap.json. "Anim" for animation.json. "Sprite" for texture.</param>
        private void RenameFile(string filePath, string suffix, bool moveToFlashFolder)
        {
            string finalPath = filePath;
            string extension = Path.GetExtension(filePath);
            string fileName = _fileNameBase + suffix + extension;
            int baseIndex = filePath.LastIndexOf("/", StringComparison.Ordinal);
            baseIndex++;

            finalPath = finalPath.Remove(baseIndex);
            finalPath += fileName;
            if (moveToFlashFolder)
                FileUtil.MoveFileOrDirectory(filePath, _flashFolderNamePath + "/" + fileName);
            else
                FileUtil.MoveFileOrDirectory(filePath, finalPath);

            // Move meta files too
            // This is needed to avoid loosing the sprite split
            filePath += ".meta";
            finalPath += ".meta";
            if (moveToFlashFolder)
                FileUtil.MoveFileOrDirectory(filePath, _flashFolderNamePath + "/" + fileName + ".meta");
            else
                FileUtil.MoveFileOrDirectory(filePath, finalPath);
        }

        /// <summary>
        /// This method gets the information about the folder where the script is in order to avoid errors when the folder is moved.
        /// </summary>
        /// <param name="logoImage">Image logo to be loaded</param>
        private void GetLogoFromFolder(ref Texture2D logoImage)
        {
            string scriptFilePath = "";
            string scriptFolder = "";

            // Get the actual folder of the script
            MonoScript monoScript = MonoScript.FromScriptableObject(this);
            scriptFilePath = AssetDatabase.GetAssetPath(monoScript);
            FileInfo fi = new FileInfo(scriptFilePath);
            if (fi.Directory != null)
            {
                scriptFolder = fi.Directory.ToString();
                scriptFolder.Replace('\\', '/');
            }

            logoImage = new Texture2D(1, 1);
            logoImage.LoadImage(
                System.IO.File.ReadAllBytes(scriptFolder + "/Resources/FlashImporterLogo.png"));

            if (logoImage == null)
                Debug.LogError("Logo image not found. Did you move Monswarm folder?");
        }

        /// <summary>
        /// Parse Animation.json and get a dictionary of values.
        /// This dictionary will be used when slicing the spritesheet in order to rename
        /// each part.
        /// </summary>
        /// <returns>True = everything goes ok</returns>
        private bool ParseLayersInformation()
        {
            JSONAnimation.LayersInformation layersinfojson;
            _layersDictionary = new Dictionary<string, string>();

            using (StreamReader r = new StreamReader(_layersInfoAssetPath))
            {
                string json = r.ReadToEnd();
                layersinfojson = JsonUtility.FromJson<JSONAnimation.LayersInformation>(json);
            }

            if (layersinfojson.ANIMATION == null)
            {
                Debug.LogError("Spritesheet information is empty.");
                return false;
            }
            
            _symbolName = layersinfojson.ANIMATION.SYMBOL_name;
            DebugLog("Symbol name: " + _symbolName);

            foreach (JSONAnimation.LayersInfoLayers layersInfo in layersinfojson.ANIMATION.TIMELINE.LAYERS)
            {
                string layerName = layersInfo.Layer_name;
                JSONAnimation.LayersInfoFrames[] frames = layersInfo.Frames;

                if (frames.Length <= 0)
                {
                    Debug.LogWarning("Folder detected, no frames imported: " + layerName);
                }
                else
                {
                    DebugLog("Layer detected: " + layerName);

                    int frameCount = 0;
                    foreach (JSONAnimation.LayersInfoFrames frame in frames)
                    {
                        string elementName = layerName + "_" + frameCount;
                        foreach (JSONAnimation.LayersInfoElements element in frame.elements)
                        {
                            if (_layersDictionary.ContainsKey(element.ATLAS_SPRITE_instance.name))
                            {
                                Debug.LogWarning("An element with the same ID found: " + element.ATLAS_SPRITE_instance.name + ". Layer: " + layerName + ". Position information: " + element.ATLAS_SPRITE_instance.Position.y + ";" + element.ATLAS_SPRITE_instance.Position.x);
                            }
                            else
                            {
                                _layersDictionary.Add(element.ATLAS_SPRITE_instance.name, elementName);
                                frameCount++;
                            }
                        }

                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Parse sprite atlas information and slice the texture
        /// </summary>
        /// <returns></returns>
        public bool ParseAtlasInformation()
        {
            AtlasInformation atlasJson;
            Texture2D texture = spriteSheet;
            List<string> sprites = new List<string>();


            //Get texture image information
            _spriteAssetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImport = AssetImporter.GetAtPath(_spriteAssetPath) as TextureImporter;
            if (textureImport == null)
                return false;
            textureImport.textureType = TextureImporterType.Sprite;
            textureImport.allowAlphaSplitting = false;
            textureImport.isReadable = true;
            
            List<SpriteMetaData> newSpritesMetadata = new List<SpriteMetaData>();
            using (StreamReader r = new StreamReader(_textAssetPath))
            {
                string json = r.ReadToEnd();
                atlasJson = JsonUtility.FromJson<AtlasInformation>(json);
            }

            foreach (Sprites sprite in atlasJson.ATLAS.SPRITES)
            {
                // String that identifies each unique sprite
                string dataJsonRow = sprite.SPRITE.x + ";" + sprite.SPRITE.y + ";" + sprite.SPRITE.w + ";" +
                                     sprite.SPRITE.h;

                // Only create a new sprite if flag is active and is not duplicated (same X,Y)
                if (!_removeDuplicatedImages || (_removeDuplicatedImages && !sprites.Contains(dataJsonRow)))
                {
                    //Find real name
                    string newElementName = "";
                    _layersDictionary.TryGetValue(sprite.SPRITE.name, out newElementName);

                    if (newElementName.Equals(""))
                    {
                        Debug.LogError("Sprite name not found: " + sprite.SPRITE.name);
                        newElementName = sprite.SPRITE.name;
                    }

                    SliceAndStoreMetadata(sprites, dataJsonRow, sprite, newElementName, atlasJson, newSpritesMetadata);
                }
            }

            if (newSpritesMetadata.Count <= 0)
            {
                Debug.LogError("No sprites detected.");
                return false;
            }

            textureImport.spritesheet = newSpritesMetadata.ToArray();
            textureImport.spriteImportMode = SpriteImportMode.Multiple;
            textureImport.filterMode = FilterMode.Point;
            textureImport.spritePixelsPerUnit = 1;
            AssetDatabase.ImportAsset(_spriteAssetPath, ImportAssetOptions.ForceUpdate);
            texture.Apply(true);

            return true;
        }

        /// <summary>
        /// Debug a message taking in consideration the Debug flag
        /// </summary>
        /// <param name="message"></param>
        private void DebugLog(string message)
        {
            if (_debugLog)
                Debug.Log(message);
        }

        /// <summary>
        /// Slice an identified sprite and store its metadata
        /// in order to be detect unique sprites.
        /// </summary>
        /// <param name="sprites">List of all sprites</param>
        /// <param name="dataJsonRow">Metadata composed by the sprite properties</param>
        /// <param name="sprite">Sliced sprite detected</param>
        /// <param name="newElementName">Element detected</param>
        /// <param name="atlasJson">Serialized atlas</param>
        /// <param name="newSpritesMetadata">List of all sliced sprites</param>
        private static void SliceAndStoreMetadata(List<string> sprites, string dataJsonRow, Sprites sprite, string newElementName,
            AtlasInformation atlasJson, List<SpriteMetaData> newSpritesMetadata)
        {
            int sliceWidth;
            int sliceHeight;
            int pivotX;
            int pivotY;
            
            // Store sprite identifier in order to not load it again in the next iteration
            sprites.Add(dataJsonRow);
            sliceWidth = sprite.SPRITE.w;
            sliceHeight = sprite.SPRITE.h;
            pivotX = Mathf.Abs(sliceWidth / sliceWidth);
            pivotY = Mathf.Abs(sliceHeight / sliceHeight);


            //Slice sprite and store it in its metadata
            SpriteMetaData newSpriteMetadata = new SpriteMetaData();
            newSpriteMetadata.pivot = new Vector2((1 - pivotX), pivotY);
            newSpriteMetadata.name = newElementName;
            newSpriteMetadata.rect = new Rect(sprite.SPRITE.x, atlasJson.meta.size.h - sprite.SPRITE.y - sliceHeight,
                sliceWidth, sliceHeight);
            newSpriteMetadata.alignment = (int) SpriteAlignment.Custom;
            newSpritesMetadata.Add(newSpriteMetadata);
        }
    }
}

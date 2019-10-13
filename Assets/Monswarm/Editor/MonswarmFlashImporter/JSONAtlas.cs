using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Monswarm.Editor.MonswarmFlashImporter
{
    public class JSONAtlas
    {
        [System.Serializable]
        public class AtlasInformation
        {
            public Atlas ATLAS;
            public Metadata meta;
        }

        [System.Serializable]
        public class Atlas
        {
            public Sprites[] SPRITES;
        }

        [System.Serializable]
        public class Sprites
        {
            public SpriteInformation SPRITE;
        }

        [System.Serializable]
        public class SpriteInformation
        {
            public string name;
            public int x;
            public int y;
            public int w;
            public int h;
            public bool rotated;
        }

        [System.Serializable]
        public class Metadata
        {
            public float framerate;
            public Size size;
        }

        [System.Serializable]
        public class Size
        {
            public float w;
            public float h;
        }
    }
}

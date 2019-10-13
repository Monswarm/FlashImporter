using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Monswarm.Editor.MonswarmFlashImporter
{
    public class JSONAnimation
    {
        [System.Serializable]
        public class LayersInformation
        {
            public LayersInfoAnimation ANIMATION;
            public LayersInfoMetadata metadata;
        }

        [System.Serializable]
        public class LayersInfoMetadata
        {
            public float framerate;
        }

        [System.Serializable]
        public class LayersInfoAnimation
        {
            public string SYMBOL_name;
            public LayersInfoTimeline TIMELINE;
        }

        [System.Serializable]
        public class LayersInfoTimeline
        {
            public LayersInfoLayers[] LAYERS;
        }

        [System.Serializable]
        public class LayersInfoLayers
        {
            public string Layer_name;
            public LayersInfoFrames[] Frames;
        }

        [System.Serializable]
        public class LayersInfoFrames
        {
            public int index;
            public int duration;
            public LayersInfoElements[] elements;
        }

        [System.Serializable]
        public class LayersInfoElements
        {
            public LayersInfoAtlasSpriteInstance ATLAS_SPRITE_instance;
        }

        [System.Serializable]
        public class LayersInfoAtlasSpriteInstance
        {
            public string name;
            public LayersInfoPosition Position;
        }

        [System.Serializable]
        public class LayersInfoPosition
        {
            public int x;
            public int y;
        }
    }
}
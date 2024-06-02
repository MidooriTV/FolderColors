using System;
using UnityEditor;
using UnityEngine;

namespace FolderColors
{
    [CreateAssetMenu(fileName = "FolderIconManager", menuName = "ScriptableObjects/FolderColors")]
    public class FolderColorsSettings : ScriptableObject
    {
        [Serializable]
        public class FolderIcon
        {
            public DefaultAsset folder;
            public Texture2D folderIcon;
            public Texture2D overlayIcon;
        }
        
        public bool showOverlay = true;
        public bool showCustomFolder = true;
        public FolderIcon[] icons = Array.Empty<FolderIcon>();
    }
}
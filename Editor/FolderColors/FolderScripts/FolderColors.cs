using UnityEditor;
using UnityEngine;

namespace FolderColors
{
    [InitializeOnLoad]
    internal static class FolderColorReplacer
    {
        private static Object[] _allFolderColors;
        private static FolderColorsSettings _folderColors;

        public static bool ShowFolder;
        public static bool ShowOverlay;

        private const string PrefFolder = "FolderColors_ShowCustomFolders";
        private const string PrefOverlay = "FolderColors_ShowCustomIcons";
        private const float MaxTreeHeight = 16f;
        
        public static readonly Color SelectedColor = new Color(0.235f, 0.360f, 0.580f);
        public static readonly Color BackgroundColour = EditorGUIUtility.isProSkin
            ? new Color32(51, 51, 51, 255)
            : new Color32(190, 190, 190, 255);

        static FolderColorReplacer()
        {
            CheckPreferences();

            EditorApplication.projectWindowItemOnGUI -= ReplaceFolders;
            EditorApplication.projectWindowItemOnGUI += ReplaceFolders;
        }

        private static void ReplaceFolders(string guid, Rect selectionRect)
        {
            if (_folderColors == null)
            {
                _allFolderColors = GetAllInstances<FolderColorsSettings>();

                if (_allFolderColors.Length > 0)
                {
                    _folderColors = _allFolderColors[0] as FolderColorsSettings;
                }
                else
                {
                    CreateFolderColorsSettings();
                }
            }

            if (_folderColors == null || (!_folderColors.showCustomFolder && !_folderColors.showOverlay))
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var folderAsset = AssetDatabase.LoadAssetAtPath(path, typeof(DefaultAsset));

            if (folderAsset == null)
            {
                return;
            }

            foreach (var icon in _folderColors.icons)
            {
                if (icon.folder != folderAsset)
                {
                    continue;
                }

                DrawTextures(selectionRect, icon, guid);
            }
        }

        private static void DrawTextures(Rect rect, FolderColorsSettings.FolderIcon icon, string guid)
        {
            var isTreeView = rect.width > rect.height;
            var isSideView = FolderColorsGUI.IsSideView(rect);

            if (isTreeView)
            {
                rect.width = rect.height = MaxTreeHeight;

                if (!isSideView)
                {
                    rect.x += 3f;
                }
            }
            else
            {
                rect.height -= 14f;
            }

            if (ShowFolder && icon.folderIcon)
            {
                FolderColorsGUI.DrawFolderTexture(rect, icon.folderIcon, guid);
            }

            if (ShowOverlay && icon.overlayIcon)
            {
                FolderColorsGUI.DrawOverlayTexture(rect, icon.overlayIcon);
            }
        }

        private static void CheckPreferences()
        {
            ShowFolder = EditorPrefs.GetBool(PrefFolder, true);
            ShowOverlay = EditorPrefs.GetBool(PrefOverlay, true);
        }

        private static void CreateFolderColorsSettings()
        {
            var settings = ScriptableObject.CreateInstance<FolderColorsSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Editor/FolderColors/FolderColors.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _folderColors = AssetDatabase.LoadAssetAtPath("Assets/Editor/FolderColors/FolderColors.asset", typeof(FolderColorsSettings)) as FolderColorsSettings;
        }

        private static Object[] GetAllInstances<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var instances = new Object[guids.Length];

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                instances[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return instances;
        }
    }
}

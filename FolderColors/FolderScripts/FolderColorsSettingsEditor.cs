using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FolderColors
{
    [CustomEditor(typeof(FolderColorsSettings))]
    internal class FolderColorsSettingsEditor : Editor
    {
        private FolderColorsSettings _settings;
        private SerializedProperty _serializedIcons;
        private ReorderableList _iconList;
        private Texture2D _selectedTexture;
        private Texture2D _previewTexture;
        private RenderTexture _previewRender;
        private readonly GUIContent _previewContent = new GUIContent();
        private Color _replacementColour = Color.gray;
        private string _textureName = "Folder_";
        private string _savePath;
        private int _heightIndex;

        private const float MaxLabelWidth = 90f;
        private const float MaxFieldWidth = 150f;
        private const float PropertyHeight = 19f;
        private const float PropertyPadding = 4f;

        private GUIStyle _elementStyle;
        private GUIStyle _propertyStyle;
        private GUIStyle _previewStyle;

        private void OnEnable()
        {
            if (target == null) return;

            _settings = target as FolderColorsSettings;
            _serializedIcons = serializedObject.FindProperty("icons");
            _savePath = Application.dataPath;

            _iconList = new ReorderableList(serializedObject, _serializedIcons)
            {
                drawHeaderCallback = OnHeaderDraw,
                drawElementCallback = OnElementDraw,
                drawElementBackgroundCallback = DrawElementBackground,
                elementHeightCallback = GetPropertyHeight,
                showDefaultBackground = false
            };

            if (_selectedTexture != null) UpdatePreview();
        }

        private void OnDisable()
        {
            ClearPreviewData();
        }

        public override void OnInspectorGUI()
        {
            if (_previewStyle == null)
            {
                _previewStyle = new GUIStyle(EditorStyles.label)
                {
                    fixedHeight = 64,
                    alignment = TextAnchor.MiddleCenter
                };

                _elementStyle = new GUIStyle(GUI.skin.box);
            }

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            {
                _settings.showCustomFolder = EditorGUILayout.ToggleLeft("Show Folder Textures", _settings.showCustomFolder);
                _settings.showOverlay = EditorGUILayout.ToggleLeft("Show Overlay Textures", _settings.showOverlay);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ApplySettings();
            }

            EditorGUILayout.Space(16f);

            EditorGUI.BeginChangeCheck();
            _iconList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            DrawTexturePreview();

            EditorGUI.BeginDisabledGroup(!_previewTexture);
            {
                EditorGUI.BeginChangeCheck();
                {
                    _replacementColour = EditorGUILayout.ColorField(new GUIContent("Replacement Color"), _replacementColour);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    SetPreviewColour();
                }

                DrawTextureSaving();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ApplySettings()
        {
            FolderColorReplacer.ShowFolder = _settings.showCustomFolder;
            FolderColorReplacer.ShowOverlay = _settings.showOverlay;
        }

        private static void OnHeaderDraw(Rect rect)
        {
            rect.y += 5f;
            rect.x -= 6f;
            rect.width += 12f;

            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0.15f, 0.15f, 0.15f, 1f), new Color(0.15f, 0.15f, 0.15f, 1f));
            Handles.EndGUI();

            EditorGUI.LabelField(rect, "Folders", EditorStyles.boldLabel);
        }

        private void OnElementDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            var property = _serializedIcons.GetArrayElementAtIndex(index);
            var fullWidth = rect.width;

            var originalLabelWidth = EditorGUIUtility.labelWidth;
            const float rectWidth = MaxLabelWidth + MaxFieldWidth;
            EditorGUIUtility.labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, MaxLabelWidth);
            rect.width = Mathf.Min(rect.width, rectWidth);

            DrawPropertyNoDepth(rect, property);

            rect.x += rect.width;
            rect.width = fullWidth - rect.width;

            SerializedProperty folderTexture = property.FindPropertyRelative("folderIcon");
            SerializedProperty overlayTexture = property.FindPropertyRelative("overlayIcon");

            FolderColorsGUI.DrawFolderPreview(rect, folderTexture.objectReferenceValue as Texture, overlayTexture.objectReferenceValue as Texture);

            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private static void DrawPropertyNoDepth(Rect rect, SerializedProperty property)
        {
            rect.width++;
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.15f, 0.15f, 0.15f, 1f));
            Handles.EndGUI();

            rect.x++;
            rect.width -= 3;
            rect.y += PropertyPadding;
            rect.height = PropertyHeight;

            var copy = property.Copy();
            var enterChildren = true;

            while (copy.Next(enterChildren))
            {
                if (SerializedProperty.EqualContents(copy, property.GetEndProperty()))
                {
                    break;
                }

                EditorGUI.PropertyField(rect, copy, false);
                rect.y += PropertyHeight + PropertyPadding;
                enterChildren = false;
            }
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.LabelField(rect, "", _elementStyle);
            Color fill = isFocused ? FolderColorReplacer.SelectedColor : Color.clear;

            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(rect, fill, new Color(0.15f, 0.15f, 0.15f, 1f));
            Handles.EndGUI();
        }

        private float GetPropertyHeight(SerializedProperty property)
        {
            if (_heightIndex == 0)
            {
                _heightIndex = property.CountInProperty();
            }
            return (PropertyHeight + PropertyPadding) * 3 + PropertyPadding;
        }

        private float GetPropertyHeight(int index)
        {
            return GetPropertyHeight(_serializedIcons.GetArrayElementAtIndex(index));
        }

        private void DrawTexturePreview()
        {
            EditorGUILayout.LabelField("Texture Color Replacement", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Original Texture");
                EditorGUILayout.LabelField("Modified Texture");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    _selectedTexture = EditorGUILayout.ObjectField(_selectedTexture, typeof(Texture2D), false, GUILayout.Width(64f)) as Texture2D;
                    EditorGUILayout.LabelField(_previewContent, _previewStyle, GUILayout.Height(64));
                }
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (!_selectedTexture)
                {
                    ClearPreviewData();
                    _previewTexture = null;
                }
                else
                {
                    UpdatePreview();
                }
            }

            EditorGUILayout.Space();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void DrawTextureSaving()
        {
            EditorGUILayout.LabelField("Save Created Texture", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
                _textureName = EditorGUILayout.TextField("Texture Name", _textureName);
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.TextField(".png", GUILayout.Width(40f));
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                _savePath = EditorGUILayout.TextField("Save Path", _savePath);
                if (GUILayout.Button("Select", GUILayout.MaxWidth(80f)))
                {
                    _savePath = EditorUtility.OpenFolderPanel("Texture Save Path", "Assets", "");
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.EndHorizontal();

            if (!GUILayout.Button("Save Texture")) return;
            var fullPath = $"{_savePath}/{_textureName}.png";
            SaveTextureAsPNG(_previewTexture, fullPath);
        }

        private void SetPreviewColour()
        {
            for (var x = 0; x < _previewTexture.width; x++)
            {
                for (var y = 0; y < _previewTexture.height; y++)
                {
                    var oldCol = _previewTexture.GetPixel(x, y);
                    var newCol = _replacementColour;
                    newCol.a = oldCol.a;
                    _previewTexture.SetPixel(x, y, newCol);
                }
            }
            _previewTexture.Apply();
        }

        private static void SaveTextureAsPNG(Texture2D texture, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.Contains("Assets"))
            {
                Debug.LogWarning("Cannot save texture to invalid path.");
                return;
            }

            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            var localPathIndex = path.IndexOf("Assets", StringComparison.Ordinal);
            path = path[localPathIndex..];

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer!.textureType = TextureImporterType.GUI;
            importer.isReadable = true;
            AssetDatabase.ImportAsset(path);
        }

        private void ClearPreviewData()
        {
            if (_previewRender)
            {
                _previewRender.Release();
            }
        }

        private void UpdatePreview()
        {
            ClearPreviewData();

            var width = Mathf.Min(256, _selectedTexture.width);
            var height = Mathf.Min(256, _selectedTexture.height);

            _previewRender = new RenderTexture(width, height, 16);
            _previewTexture = new Texture2D(_previewRender.width, _previewRender.height)
            {
                 alphaIsTransparency = true
            };
            _previewContent.image = _previewTexture;

            Graphics.Blit (_selectedTexture, _previewRender);
            
            _previewTexture.ReadPixels (new Rect (0, 0, _previewRender.width, _previewRender.height), 0, 0);
            SetPreviewColour ();
        }
    }
}
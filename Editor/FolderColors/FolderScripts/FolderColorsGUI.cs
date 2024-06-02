using UnityEditor;
using UnityEngine;

namespace FolderColors
{
    public static class FolderColorsGUI
    {
        public static void DrawFolderPreview(Rect rect, Texture folder, Texture overlay)
        {
            if (folder != null)
            {
                GUI.DrawTexture(rect, folder, ScaleMode.ScaleToFit);
            }

            if (overlay == null) return;
            var overlayRect = new Rect(rect)
            {
                size = rect.size * 0.5f,
                position = rect.position + rect.size * 0.5f
            };
            GUI.DrawTexture(overlayRect, overlay, ScaleMode.ScaleToFit);
        }

        public static void DrawFolderTexture(Rect rect, Texture folder, string guid)
        {
            if (folder == null) return;
            EditorGUI.DrawRect(rect, FolderColorReplacer.BackgroundColour);
            GUI.DrawTexture(rect, folder, ScaleMode.ScaleAndCrop);
        }

        public static void DrawOverlayTexture(Rect rect, Texture overlay)
        {
            if (overlay == null) return;
            var overlayRect = new Rect(rect)
            {
                size = rect.size * 0.5f,
                position = rect.position + rect.size * 0.5f
            };
            GUI.DrawTexture(overlayRect, overlay);
        }

        public static bool IsSideView(Rect rect)
        {
            return rect.x != 14;
        }
    }
}
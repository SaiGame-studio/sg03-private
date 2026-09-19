using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    /// <summary>
    /// Base editor providing collapsible sections with status icons for Ghost VFX inspectors.
    /// </summary>
    public abstract class GhostVfxCtrlEditorBase : UnityEditor.Editor
    {
        protected bool showBloodWisps = false;
        protected bool showBloodMist = false;
        protected bool showEmitters = false;

        private static GUIContent validIcon;
        private static GUIContent invalidIcon;

        protected static GUIContent GetValidIcon(string tooltip)
        {
            if (validIcon == null || validIcon.image == null)
            {
                GUIContent content = EditorGUIUtility.IconContent("d_Valid");
                if (content == null || content.image == null)
                    content = EditorGUIUtility.IconContent("TestPassed");
                validIcon = new GUIContent(content?.image, tooltip);
            }
            validIcon.tooltip = tooltip;
            return validIcon;
        }

        protected static GUIContent GetInvalidIcon(string tooltip)
        {
            if (invalidIcon == null || invalidIcon.image == null)
            {
                GUIContent content = EditorGUIUtility.IconContent("d_Invalid");
                if (content == null || content.image == null)
                    content = EditorGUIUtility.IconContent("TestFailed");
                invalidIcon = new GUIContent(content?.image, tooltip);
            }
            invalidIcon.tooltip = tooltip;
            return invalidIcon;
        }

        protected bool DrawCollapsibleHeader(string title, bool isExpanded, SerializedProperty enableProp, string tooltipPrefix)
        {
            EditorGUILayout.Space(6f);
            Rect rect = EditorGUILayout.GetControlRect(false, 22f);

            Rect foldoutRect = new Rect(rect.x, rect.y + 1f, rect.width - 24f, 20f);
            bool newExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, title, true, EditorStyles.foldoutHeader);

            if (!newExpanded)
            {
                bool isEnabled = enableProp != null && enableProp.boolValue;
                GUIContent iconContent = isEnabled
                    ? GetValidIcon($"{tooltipPrefix}: Enabled (Click to toggle)")
                    : GetInvalidIcon($"{tooltipPrefix}: Disabled (Click to toggle)");

                Rect iconRect = new Rect(rect.xMax - 22f, rect.y + 3f, 18f, 18f);
                if (GUI.Button(iconRect, iconContent, GUIStyle.none))
                {
                    if (enableProp != null)
                    {
                        enableProp.boolValue = !enableProp.boolValue;
                    }
                }
            }

            return newExpanded;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace SG03.UI.Editor
{
    [CustomEditor(typeof(BattleState))]
    public class BattleStateEditor : UnityEditor.Editor
    {
        // Array property names that should always be drawn collapsed.
        private static readonly string[] CollapsedArrays = new string[]
        {
            "alphaTheVoid",
            "omegaTheVoid",
            "alphaTheSource",
            "omegaTheSource",
            "alphaHand",
            "alphaBackLine",
            "alphaFrontLine",
            "omegaHand",
            "omegaFrontLine",
            "omegaBackLine",
        };

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            SerializedProperty prop = this.serializedObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (this.IsCollapsedArray(prop.name))
                {
                    this.DrawCollapsedArray(prop);
                    enterChildren = false;
                    continue;
                }

                using (new EditorGUI.DisabledScope(prop.name == "m_Script"))
                    EditorGUILayout.PropertyField(prop, true);
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private bool IsCollapsedArray(string propName)
        {
            foreach (string name in CollapsedArrays)
            {
                if (name == propName) return true;
            }
            return false;
        }

        private void DrawCollapsedArray(SerializedProperty prop)
        {
            int count = prop.isArray ? prop.arraySize : 0;
            string label = $"{prop.displayName}   [{count}]";
            EditorGUILayout.Foldout(false, label, EditorStyles.foldout);
        }
    }
}

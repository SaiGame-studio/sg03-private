using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomEditor(typeof(BattleState))]
    public class BattleStateEditor : UnityEditor.Editor
    {
        private static readonly string[] AlphaProperties =
        {
            "alphaHp",
            "alphaTheSourceCount",
            "alphaTheVoidCount",
            "alphaDefending",
            "alphaTheVoid",
            "alphaTheSource",
            "alphaHand",
            "alphaBackLine",
            "alphaFrontLine",
        };

        private static readonly string[] OmegaProperties =
        {
            "omegaHp",
            "omegaTheSourceCount",
            "omegaTheVoidCount",
            "omegaDefending",
            "omegaTheVoid",
            "omegaHand",
            "omegaFrontLine",
            "omegaBackLine",
            "omegaPlanning",
        };

        private static readonly string[] JsonProperties =
        {
            "battleStatusJson",
            "metadataJson",
        };

        private static readonly string[] ExcludedProperties =
        {
            "debugLog",
            "clientActions",
            "battleStatusJson",
            "metadataJson",
            "alphaHp",
            "alphaTheSourceCount",
            "alphaTheVoidCount",
            "alphaDefending",
            "alphaTheVoid",
            "alphaTheSource",
            "alphaHand",
            "alphaBackLine",
            "alphaFrontLine",
            "omegaHp",
            "omegaTheSourceCount",
            "omegaTheVoidCount",
            "omegaDefending",
            "omegaTheVoid",
            "omegaHand",
            "omegaFrontLine",
            "omegaBackLine",
            "omegaPlanning",
        };

        private bool debugLogFoldout = true;

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            DrawPropertiesExcluding(this.serializedObject, ExcludedProperties);
            this.DrawProperties(AlphaProperties);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            this.DrawProperties(OmegaProperties);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            this.DrawProperties(JsonProperties);
            this.serializedObject.ApplyModifiedProperties();

            this.DrawDebugLog();

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Clear Data"))
            {
                BattleState battleState = (BattleState)target;
                Undo.RecordObject(battleState, "Clear BattleState Data");
                battleState.ClearData();
                EditorUtility.SetDirty(battleState);
            }
        }

        private void DrawProperties(string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = this.serializedObject.FindProperty(propertyName);
                if (property != null) EditorGUILayout.PropertyField(property, true);
            }
        }

        private void DrawDebugLog()
        {
            SerializedProperty prop = this.serializedObject.FindProperty("debugLog");
            if (prop == null) return;

            this.debugLogFoldout = EditorGUILayout.Foldout(this.debugLogFoldout, $"Debug Log ({prop.arraySize})", true);
            if (!this.debugLogFoldout) return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < prop.arraySize; i++)
            {
                EditorGUILayout.SelectableLabel(
                    prop.GetArrayElementAtIndex(i).stringValue,
                    EditorStyles.miniLabel,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUI.indentLevel--;
        }
    }
}

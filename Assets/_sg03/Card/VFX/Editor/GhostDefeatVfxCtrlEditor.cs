using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomEditor(typeof(GhostDefeatVfxCtrl))]
    public class GhostDefeatVfxCtrlEditor : GhostVfxCtrlEditorBase
    {
        private SerializedProperty scriptProperty;
        private SerializedProperty duration;
        private SerializedProperty cardSurfaceBoxScale;

        private SerializedProperty skullsPerVariant;
        private SerializedProperty skullLifetime;
        private SerializedProperty skullAscentSpeed;
        private SerializedProperty skullEmergenceWindow;

        private SerializedProperty enableBloodWisps;
        private SerializedProperty useBloodRed;
        private SerializedProperty bloodWispsCount;
        private SerializedProperty wispLifetime;
        private SerializedProperty wispAscentSpeed;
        private SerializedProperty wispSize;
        private SerializedProperty wispEmergenceWindow;

        private SerializedProperty enableBloodMist;
        private SerializedProperty bloodMistCount;
        private SerializedProperty mistLifetime;
        private SerializedProperty mistStartSize;
        private SerializedProperty mistRiseSpeed;
        private SerializedProperty mistSpreadSpeed;
        private SerializedProperty mistMaxAlpha;
        private SerializedProperty mistEmergenceWindow;

        private SerializedProperty mainParticleSystem;
        private SerializedProperty childParticleSystems;
        private SerializedProperty ghostVariantEmitters;
        private SerializedProperty soulBurst;
        private SerializedProperty soulWisps;
        private SerializedProperty bloodMist;
        private SerializedProperty despawn;

        private void OnEnable()
        {
            this.scriptProperty = this.serializedObject.FindProperty("m_Script");
            this.duration = this.serializedObject.FindProperty("duration");
            this.cardSurfaceBoxScale = this.serializedObject.FindProperty("cardSurfaceBoxScale");

            this.skullsPerVariant = this.serializedObject.FindProperty("skullsPerVariant");
            this.skullLifetime = this.serializedObject.FindProperty("skullLifetime");
            this.skullAscentSpeed = this.serializedObject.FindProperty("skullAscentSpeed");
            this.skullEmergenceWindow = this.serializedObject.FindProperty("skullEmergenceWindow");

            this.enableBloodWisps = this.serializedObject.FindProperty("enableBloodWisps");
            this.useBloodRed = this.serializedObject.FindProperty("useBloodRed");
            this.bloodWispsCount = this.serializedObject.FindProperty("bloodWispsCount");
            this.wispLifetime = this.serializedObject.FindProperty("wispLifetime");
            this.wispAscentSpeed = this.serializedObject.FindProperty("wispAscentSpeed");
            this.wispSize = this.serializedObject.FindProperty("wispSize");
            this.wispEmergenceWindow = this.serializedObject.FindProperty("wispEmergenceWindow");

            this.enableBloodMist = this.serializedObject.FindProperty("enableBloodMist");
            this.bloodMistCount = this.serializedObject.FindProperty("bloodMistCount");
            this.mistLifetime = this.serializedObject.FindProperty("mistLifetime");
            this.mistStartSize = this.serializedObject.FindProperty("mistStartSize");
            this.mistRiseSpeed = this.serializedObject.FindProperty("mistRiseSpeed");
            this.mistSpreadSpeed = this.serializedObject.FindProperty("mistSpreadSpeed");
            this.mistMaxAlpha = this.serializedObject.FindProperty("mistMaxAlpha");
            this.mistEmergenceWindow = this.serializedObject.FindProperty("mistEmergenceWindow");

            this.mainParticleSystem = this.serializedObject.FindProperty("mainParticleSystem");
            this.childParticleSystems = this.serializedObject.FindProperty("childParticleSystems");
            this.ghostVariantEmitters = this.serializedObject.FindProperty("ghostVariantEmitters");
            this.soulBurst = this.serializedObject.FindProperty("soulBurst");
            this.soulWisps = this.serializedObject.FindProperty("soulWisps");
            this.bloodMist = this.serializedObject.FindProperty("bloodMist");
            this.despawn = this.serializedObject.FindProperty("despawn");
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(this.scriptProperty);
            }

            this.DrawVfxSettings();
            this.DrawSkullSettings();
            this.DrawBloodWispSettings();
            this.DrawBloodMistSettings();
            this.DrawEmitters();

            this.serializedObject.ApplyModifiedProperties();
        }

        private void DrawVfxSettings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("VFX Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.duration);
            EditorGUILayout.PropertyField(this.cardSurfaceBoxScale);
        }

        private void DrawSkullSettings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Skull Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.skullsPerVariant);
            EditorGUILayout.PropertyField(this.skullLifetime);
            EditorGUILayout.PropertyField(this.skullAscentSpeed);
            EditorGUILayout.PropertyField(this.skullEmergenceWindow);
        }

        private void DrawBloodWispSettings()
        {
            this.showBloodWisps = this.DrawCollapsibleHeader(
                "Blood Wisp Settings",
                this.showBloodWisps,
                this.enableBloodWisps,
                "Blood Wisps"
            );

            if (this.showBloodWisps)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(this.enableBloodWisps);
                EditorGUILayout.PropertyField(this.useBloodRed);
                EditorGUILayout.PropertyField(this.bloodWispsCount);
                EditorGUILayout.PropertyField(this.wispLifetime);
                EditorGUILayout.PropertyField(this.wispAscentSpeed);
                EditorGUILayout.PropertyField(this.wispSize);
                EditorGUILayout.PropertyField(this.wispEmergenceWindow);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawBloodMistSettings()
        {
            this.showBloodMist = this.DrawCollapsibleHeader(
                "Blood Mist Settings",
                this.showBloodMist,
                this.enableBloodMist,
                "Blood Mist"
            );

            if (this.showBloodMist)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(this.enableBloodMist);
                EditorGUILayout.PropertyField(this.bloodMistCount);
                EditorGUILayout.PropertyField(this.mistLifetime);
                EditorGUILayout.PropertyField(this.mistStartSize);
                EditorGUILayout.PropertyField(this.mistRiseSpeed);
                EditorGUILayout.PropertyField(this.mistSpreadSpeed);
                EditorGUILayout.PropertyField(this.mistMaxAlpha);
                EditorGUILayout.PropertyField(this.mistEmergenceWindow);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawEmitters()
        {
            EditorGUILayout.Space(6f);
            this.showEmitters = EditorGUILayout.Foldout(this.showEmitters, "Emitters & Dependencies", true, EditorStyles.foldoutHeader);
            if (this.showEmitters)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(this.mainParticleSystem);
                EditorGUILayout.PropertyField(this.childParticleSystems);
                EditorGUILayout.PropertyField(this.ghostVariantEmitters);
                EditorGUILayout.PropertyField(this.soulBurst);
                EditorGUILayout.PropertyField(this.soulWisps);
                EditorGUILayout.PropertyField(this.bloodMist);
                EditorGUILayout.PropertyField(this.despawn);
                EditorGUI.indentLevel--;
            }
        }
    }
}

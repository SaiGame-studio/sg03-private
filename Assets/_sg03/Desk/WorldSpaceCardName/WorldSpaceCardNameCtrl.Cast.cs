using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace SG03
{
    public sealed partial class WorldSpaceCardNameCtrl
    {
        [Header("Energy Band")]
        [SerializeField] private LineRenderer energyRibbon;
        [SerializeField] private Material energyMaterial;
        [SerializeField] private bool showEnergyRibbon = true;
        [SerializeField, Range(0.5f, 6f)] private float ribbonLength = 2.8f;
        [SerializeField, Range(0.05f, 1f)] private float ribbonWidth = 0.32f;
        [SerializeField] private float ribbonYOffset = -0.16f;
        [SerializeField, Range(0.5f, 6f)] private float energyFlowSpeed = 2f;
        [SerializeField, Range(1f, 8f)] private float energyPulseSpeed = 3f;
        [SerializeField, Range(0f, 0.5f)] private float energyPulseAmount = 0.15f;
        [SerializeField] private int ribbonSortingOrder = 98;

        [Header("Ember Particles")]
        [SerializeField] private ParticleSystem castParticles;
        [SerializeField, Range(0f, 7f)] private float idleFloatPixels = 2.5f;

        private const int RibbonSegments = 24;
        private static readonly int EffectTimeId = Shader.PropertyToID("_EffectTime");
        private static readonly int PathLengthId = Shader.PropertyToID("_PathLength");
        private static readonly int HeadModeId = Shader.PropertyToID("_HeadMode");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int HeadLengthId = Shader.PropertyToID("_HeadLength");
        private static readonly int RibbonScaleId = Shader.PropertyToID("_RibbonScale");
        private static readonly Color InscribedColor = new Color(0.96f, 0.82f, 0.58f);
        private static readonly Color EnergyCoreColor = new Color(1f, 0.98f, 0.86f);

        private readonly List<string> nameGlyphs = new List<string>();
        private readonly StringBuilder castText = new StringBuilder(1024);
        private readonly Vector3[] ribbonPoints = new Vector3[RibbonSegments];
        private MaterialPropertyBlock energyProperties;
        private float castStartedAt;
        private float nextCastUpdate;
        private float nextEmberAt;
        private bool castRunning;

        private void LoadCastParticles()
        {
            if (this.castParticles != null) return;
            Transform child = this.transform.Find("CastEmbers");
            if (child != null) this.castParticles = child.GetComponent<ParticleSystem>();
        }

        private void LoadEnergyRibbon()
        {
            if (this.energyRibbon == null)
            {
                Transform child = this.transform.Find("EnergyRibbon");
                if (child != null)
                {
                    this.energyRibbon = child.GetComponent<LineRenderer>();
                }
            }

            if (this.energyRibbon == null)
            {
                var ribbonObj = new GameObject("EnergyRibbon");
                ribbonObj.transform.SetParent(this.transform, false);
                this.energyRibbon = ribbonObj.AddComponent<LineRenderer>();
            }

            this.ConfigureEnergyRibbonRenderer();
        }

        private void LoadEnergyMaterial()
        {
            if (this.energyMaterial != null) return;
#if UNITY_EDITOR
            this.energyMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/Desk/ArrowIndicator/ArrowIndicatorMat.mat");
#endif
            if (this.energyRibbon != null && this.energyMaterial != null)
            {
                this.energyRibbon.sharedMaterial = this.energyMaterial;
            }
        }

        private void ConfigureEnergyRibbonRenderer()
        {
            if (this.energyRibbon == null) return;
            if (this.energyMaterial != null) this.energyRibbon.sharedMaterial = this.energyMaterial;
            this.energyRibbon.useWorldSpace = false;
            this.energyRibbon.alignment = LineAlignment.View;
            this.energyRibbon.textureMode = LineTextureMode.Stretch;
            this.energyRibbon.shadowCastingMode = ShadowCastingMode.Off;
            this.energyRibbon.receiveShadows = false;
            this.energyRibbon.lightProbeUsage = LightProbeUsage.Off;
            this.energyRibbon.reflectionProbeUsage = ReflectionProbeUsage.Off;
            this.energyRibbon.startColor = Color.white;
            this.energyRibbon.endColor = Color.white;
            this.energyRibbon.numCapVertices = 2;
            this.energyRibbon.numCornerVertices = 2;
            this.energyRibbon.sortingOrder = this.ribbonSortingOrder;
        }

        private void CacheNameGlyphs()
        {
            this.nameGlyphs.Clear();
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(this.cardDisplayName ?? string.Empty);
            while (elements.MoveNext()) this.nameGlyphs.Add(elements.GetTextElement());
        }

        private void RestartNameCast()
        {
            this.castStartedAt = Time.unscaledTime;
            this.nextCastUpdate = 0f;
            this.nextEmberAt = 0f;
            this.castRunning = true;
            this.ClearCastParticles();
        }

        private void StopNameCast()
        {
            this.castRunning = false;
            this.ClearCastParticles();
            if (this.energyRibbon != null) this.energyRibbon.positionCount = 0;
        }

        private void ClearCastParticles()
        {
            if (this.castParticles == null) return;
            this.castParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void UpdateNameCast()
        {
            if (!this.castRunning || this.cardNameLabel == null) return;
            if (Time.unscaledTime < this.nextCastUpdate) return;
            this.nextCastUpdate = Time.unscaledTime + 1f / 30f;
            float age = Time.unscaledTime - this.castStartedAt;
            this.AnimateNameText(age);
            this.AnimateEnergyRibbon(age);
            this.EmitCastEmbers(age);
        }

        private void RefreshNameCastText()
        {
            this.cardNameLabel.enableRichText = true;
            float age = Application.isPlaying ? Time.unscaledTime - this.castStartedAt : 10f;
            this.AnimateNameText(age);
            this.AnimateEnergyRibbon(age);
        }

        private void AnimateNameText(float age)
        {
            this.castText.Clear();
            int glyphCount = this.nameGlyphs.Count;
            if (glyphCount == 0)
            {
                this.cardNameLabel.text = string.Empty;
                return;
            }

            for (int index = 0; index < glyphCount; index++)
            {
                this.AppendEnergyGlyph(index, glyphCount, age);
            }
            this.cardNameLabel.text = this.castText.ToString();

            float lift = Mathf.Sin(age * 1.8f) * this.idleFloatPixels;
            this.cardNameLabel.style.translate = new Translate(0f, -lift);

            float pulse = 0.5f + 0.5f * Mathf.Sin(age * this.energyPulseSpeed);
            this.cardNameLabel.style.textShadow = new TextShadow
            {
                offset = Vector2.zero,
                blurRadius = 6f + pulse * 4f,
                color = new Color(1f, 0.38f, 0.08f, 0.72f * (0.7f + 0.3f * pulse))
            };
        }

        private void AppendEnergyGlyph(int index, int total, float age)
        {
            float progress = total > 1 ? index / (float)(total - 1) : 0.5f;
            // Energy crest wave flowing continuously across letters from left to right
            float flow = progress * 2f - age * this.energyFlowSpeed * 1.2f;
            float crestPhase = Mathf.Repeat(flow, 1f);
            float crest = Mathf.SmoothStep(0.42f, 0.72f, crestPhase) * (1f - Mathf.SmoothStep(0.72f, 0.95f, crestPhase));
            float heartbeat = 1f - this.energyPulseAmount * (0.5f + 0.5f * Mathf.Sin(age * this.energyPulseSpeed));

            Color color = Color.Lerp(InscribedColor, EnergyCoreColor, Mathf.Clamp01(crest * 1.4f) * heartbeat);
            this.castText.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(color)).Append("><noparse>");
            this.castText.Append(this.nameGlyphs[index]).Append("</noparse></color>");
        }

        private void AnimateEnergyRibbon(float age)
        {
            if (this.energyRibbon == null) return;
            if (!this.showEnergyRibbon)
            {
                if (this.energyRibbon.positionCount > 0) this.energyRibbon.positionCount = 0;
                return;
            }

            if (this.energyProperties == null) this.energyProperties = new MaterialPropertyBlock();

            float halfLength = this.ribbonLength * 0.5f;
            for (int i = 0; i < RibbonSegments; i++)
            {
                float t = i / (float)(RibbonSegments - 1);
                float x = Mathf.Lerp(-halfLength, halfLength, t);
                float wave = Mathf.Sin(t * Mathf.PI * 2f - age * this.energyFlowSpeed * 2f) * 0.02f;
                float envelope = Mathf.Sin(t * Mathf.PI);
                float y = this.ribbonYOffset + wave * envelope;
                this.ribbonPoints[i] = new Vector3(x, y, -0.01f);
            }

            this.energyRibbon.positionCount = RibbonSegments;
            this.energyRibbon.SetPositions(this.ribbonPoints);
            this.energyRibbon.widthMultiplier = this.ribbonWidth;

            this.energyProperties.SetFloat(EffectTimeId, age);
            this.energyProperties.SetFloat(PathLengthId, this.ribbonLength);
            this.energyProperties.SetFloat(HeadModeId, 0f);
            this.energyProperties.SetFloat(OpacityId, 1f);
            this.energyProperties.SetFloat(HeadLengthId, 0f);
            this.energyProperties.SetFloat(RibbonScaleId, 1f);
            this.energyRibbon.SetPropertyBlock(this.energyProperties);
        }

        private void EmitCastEmbers(float age)
        {
            if (this.castParticles == null || Time.unscaledTime < this.nextEmberAt) return;
            Rect bounds = this.cardNameLabel.worldBound;
            if (float.IsNaN(bounds.width) || bounds.width < 1f || this.uiDocument.panelSettings == null) return;

            this.nextEmberAt = Time.unscaledTime + 0.12f;
            float progress = Mathf.Repeat(age * this.energyFlowSpeed * 0.35f, 1f);
            Vector2 point = new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, progress), bounds.center.y);
            this.EmitEmberAt(point);
        }

        private void EmitEmberAt(Vector2 panelPoint)
        {
            Rect rootBounds = this.uiDocument.rootVisualElement.worldBound;
            Vector2 centered = panelPoint - rootBounds.center;
            Vector3 local = new Vector3(centered.x / 100f, -centered.y / 100f, -0.015f);
            var emit = new ParticleSystem.EmitParams
            {
                position = local + new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(-0.05f, 0.05f), 0f),
                velocity = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(0.08f, 0.25f), 0f),
                startLifetime = Random.Range(0.3f, 0.6f),
                startSize = Random.Range(0.018f, 0.038f),
                startColor = new Color(1f, Random.Range(0.45f, 0.8f), 0.18f, 0.75f)
            };
            if (!this.castParticles.isPlaying) this.castParticles.Play(true);
            this.castParticles.Emit(emit, 1);
        }
    }
}

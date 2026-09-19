using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    public sealed partial class WorldSpaceCardNameCtrl
    {
        [Header("Energy Text Animation")]
        [SerializeField, Range(0.5f, 6f)] private float energyFlowSpeed = 2f;
        [SerializeField, Range(1f, 8f)] private float energyPulseSpeed = 3f;
        [SerializeField, Range(0f, 0.5f)] private float energyPulseAmount = 0.15f;

        private static readonly Color InscribedColor = new Color(0.96f, 0.82f, 0.58f);
        private static readonly Color EnergyCoreColor = new Color(1f, 0.98f, 0.86f);

        private readonly List<string> nameGlyphs = new List<string>();
        private readonly StringBuilder castText = new StringBuilder(1024);
        private float castStartedAt;
        private float nextCastUpdate;
        private bool castRunning;

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
            this.castRunning = true;
        }

        private void StopNameCast()
        {
            this.castRunning = false;
        }

        private void UpdateNameCast()
        {
            if (!this.castRunning || this.cardNameLabel == null) return;
            if (Time.unscaledTime < this.nextCastUpdate) return;
            this.nextCastUpdate = Time.unscaledTime + 1f / 30f;
            float age = Time.unscaledTime - this.castStartedAt;
            this.AnimateNameText(age);
        }

        private void RefreshNameCastText()
        {
            this.cardNameLabel.enableRichText = true;
            float age = Application.isPlaying ? Time.unscaledTime - this.castStartedAt : 10f;
            this.AnimateNameText(age);
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
    }
}

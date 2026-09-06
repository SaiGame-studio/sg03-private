using System.Collections;
using SaiGame.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace SG03
{
    /// <summary>
    /// 3D card component. Attach to any GameObject that represents a physical card.
    ///
    /// Expected child hierarchy (created automatically via "Setup Card Structure"):
    ///   CardObject  ← this component lives here
    ///   ├── FrontFace
    ///   │   ├── Character   (background quad — character artwork)
    ///   │   └── Frame       (foreground quad — transparent frame PNG)
    ///   └── BackFace        (rotated 180° on Y)
    ///       └── Back        (quad — card back image)
    ///
    /// Flip mechanics rotate this root transform on the Y axis.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card 3D")]
    public class Card3D : SaiBehaviour
    {
        [Header("Face Renderers")]
        [Tooltip("Renderer for the transparent frame PNG overlaid on the front face.")]
        [SerializeField] private Renderer frontFrameRenderer;

        [Tooltip("Renderer for the character artwork (background layer of the front face).")]
        [SerializeField] private Renderer characterRenderer;

        [Tooltip("Renderer for the card back image.")]
        [SerializeField] private Renderer backRenderer;

        [Header("Card Data")]
        [Tooltip("ScriptableObject that provides the frame, character, and back textures.")]
        [SerializeField] private CardData cardData;

        [Tooltip("Shared default textures used as fallback when CardData leaves frame or back null.")]
        [SerializeField] private CardDefaults cardDefaults;

        [Header("Flip Animation")]
        [Tooltip("Duration of the flip animation in seconds.")]
        [SerializeField] private float flipDuration = 0.4f;

        // Sourced from the card definition via Card3DCtrl.SetFallbackName().
        private string fallbackName;

        // Sourced from the card definition via Card3DCtrl.SetFallbackStats().
        private CardBaseStats fallbackStats;

        // Shown in DescriptionText. Sourced from CardDefinitionMetadata.description
        // via Card3DCtrl.SetFallbackDescription().
        private string fallbackDescription;

        // Sourced from CardDefinitionMetadata.type via Card3DCtrl.SetDefinition().
        private string cardType;

        [Header("Card Text")]
        [Tooltip("TextMeshPro showing the card name.")]
        [SerializeField] private TextMeshPro cardNameText;

        [Tooltip("TextMeshPro showing the star rating (* symbols, LiberationSans SDF does not support U+2605).")]
        [SerializeField] private TextMeshPro starsText;

        [Tooltip("TextMeshPro showing the ATK value.")]
        [SerializeField] private TextMeshPro atkText;

        [Tooltip("TextMeshPro showing the DEF value.")]
        [SerializeField] private TextMeshPro defText;

        [Tooltip("TextMeshPro showing the card description.")]
        [SerializeField] private TextMeshPro descriptionText;

        /// <summary>Gets the world-space centre between this card's ATK and DEF text.</summary>
        public bool TryGetStatsCenterWorldPosition(out Vector3 position)
        {
            if (this.atkText != null && this.defText != null)
            {
                position = (this.atkText.transform.position + this.defText.transform.position) * 0.5f;
                return true;
            }

            position = this.transform.position;
            return false;
        }

        /// <summary>Gets the world-space position of the top edge of this card.</summary>
        public bool TryGetTopEdgeWorldPosition(out Vector3 position)
        {
            if (this.cardHeightPixels <= 0 || this.pixelsPerUnit <= 0f)
            {
                position = this.transform.position;
                return false;
            }

            position = this.transform.TransformPoint(Vector3.up * (this.CardHeight * 0.5f));
            return true;
        }

        [Header("Card Size")]
        [Tooltip("Card width in pixels (converted to world units via Pixels Per Unit).")]
        [SerializeField] private int cardWidthPixels = 750;
        [Tooltip("Card height in pixels (converted to world units via Pixels Per Unit).")]
        [SerializeField] private int cardHeightPixels = 1050;
        [Tooltip("How many pixels equal 1 Unity world unit. Match your texture import setting.")]
        [SerializeField] private float pixelsPerUnit = 100f;

        private float CardWidth => cardWidthPixels / pixelsPerUnit;
        private float CardHeight => cardHeightPixels / pixelsPerUnit;

        private bool isFacingFront = true;
        private float currentYAngle = 0f;
        private Coroutine flipCoroutine;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            this.ApplyFrontFaceCulling();
            this.ApplySortingGroup();
            this.ApplyStatsVisibility();
            // this.HideCharacterRenderer();
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadFrontFrameRenderer();
            this.LoadCharacterRenderer();
            this.LoadBackRenderer();
            this.LoadCardDefaults();
            this.LoadCardNameText();
            this.LoadStarsText();
            this.LoadAtkText();
            this.LoadDefText();
            this.LoadDescriptionText();
        }

        protected override void ResetValue()
        {
            base.ResetValue();
            this.ApplyDefaultFonts();
            this.ApplyDefaultColors();
            this.ApplyFrontFaceCulling();
        }

        private void LoadFrontFrameRenderer()
        {
            if (this.frontFrameRenderer != null) return;
            this.frontFrameRenderer = this.FindChildComponent<Renderer>("FrontFace/Frame");
        }

        private void LoadCharacterRenderer()
        {
            if (this.characterRenderer != null) return;
            this.characterRenderer = this.FindChildComponent<Renderer>("FrontFace/Character");
        }

        private void LoadBackRenderer()
        {
            if (this.backRenderer != null) return;
            this.backRenderer = this.FindChildComponent<Renderer>("BackFace/Back");
        }

        private void LoadCardDefaults()
        {
            if (this.cardDefaults != null) return;
            this.cardDefaults = Resources.Load<CardDefaults>("CardDefaults");
        }

        private void LoadCardNameText() => this.LoadText(ref this.cardNameText, "CardNameText");
        private void LoadStarsText() => this.LoadText(ref this.starsText, "StarsText");
        private void LoadAtkText() => this.LoadText(ref this.atkText, "AtkText");
        private void LoadDefText() => this.LoadText(ref this.defText, "DefText");
        private void LoadDescriptionText() => this.LoadText(ref this.descriptionText, "DescriptionText");

        private T FindChildComponent<T>(string path) where T : Component
        {
            Transform child = this.transform.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void LoadText(ref TextMeshPro field, string objectName)
        {
            if (field != null) return;

            foreach (TextMeshPro text in this.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (text.gameObject.name == objectName)
                {
                    field = text;
                    return;
                }
            }
        }

        // Hides the character quad until a texture is loaded via ApplyTextures.
        // Prevents the white default texture from showing when no CardData is assigned.
        private void HideCharacterRenderer()
        {
            if (this.characterRenderer == null) return;
            this.characterRenderer.enabled = false;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Scales all face quads (Character, Frame, Back) to match the pixel size
        /// converted to world units via Pixels Per Unit.
        /// </summary>
        public void ApplySize()
        {
            Vector3 size = new Vector3(CardWidth, CardHeight, 1f);
            if (frontFrameRenderer != null) frontFrameRenderer.transform.localScale = size;
            if (characterRenderer != null) characterRenderer.transform.localScale = size;
            if (backRenderer != null) backRenderer.transform.localScale = size;
        }

        /// <summary>
        /// Applies only the CardDefaults textures (frame + back) directly to the renderers.
        /// Used in Editor setup when no CardData is assigned yet.
        /// </summary>
        public void ApplyDefaults()
        {
            if (cardDefaults == null) return;
            SetRendererTexture(frontFrameRenderer, cardDefaults.CardFrontChar1);
            SetRendererTexture(backRenderer, cardDefaults.BackTexture);
            ApplyDefaultFonts();
            ApplyDefaultColors();
        }

        /// <summary>
        /// Pushes the textures from the assigned CardData to their respective renderer
        /// material instances. Optional <paramref name="defaults"/> fills in any null
        /// frame or back texture in CardData.
        /// </summary>
        public void ApplyTextures()
        {
            if (cardData == null)
            {
                Debug.LogWarning($"[Card3D] No CardData assigned on '{name}'.", this);
                return;
            }

            Texture2D frame = this.cardData.FrameTexture != null ? this.cardData.FrameTexture : this.GetDefaultFrontFrame();
            Texture2D back = this.cardData.BackTexture != null ? this.cardData.BackTexture : this.cardDefaults?.BackTexture;

            SetRendererTexture(this.frontFrameRenderer, frame);
            SetRendererTexture(this.characterRenderer, this.cardData.CharacterTexture);
            SetRendererTexture(this.backRenderer, back);
            this.SetCharacterRendererVisible(this.cardData.CharacterTexture != null);

            this.ApplyCardText();
        }

        /// <summary>
        /// Sets the display name sourced from the card definition.
        /// </summary>
        public void SetFallbackName(string name) => this.fallbackName = name;

        /// <summary>
        /// Sets fallback ATK / DEF / Stars shown when CardData has zero values.
        /// Pass the stats parsed from <c>ItemDefinitionData.base_stats</c>.
        /// </summary>
        public void SetFallbackStats(CardBaseStats stats) => this.fallbackStats = stats;

        /// <summary>
        /// Sets the description shown in DescriptionText. Pass
        /// <c>CardDefinitionMetadata.description</c> parsed from the battle response.
        /// </summary>
        public void SetFallbackDescription(string description) => this.fallbackDescription = description;

        /// <summary>
        /// Sets the card type from its definition. ATK and DEF are only shown for
        /// character cards.
        /// </summary>
        public void SetCardType(string type)
        {
            this.cardType = type;
            this.ApplyStatsVisibility();

            // Card data can load before its definition. Re-apply the fallback front
            // once the definition type becomes available.
            if (this.cardData != null && this.cardData.FrameTexture == null)
                this.ApplyTextures();
        }

        /// <summary>
        /// Assigns a new <see cref="CardData"/> and immediately applies its textures.
        /// Called by <see cref="CardDataManager"/> after an Addressables load completes.
        /// </summary>
        public void SetCardData(CardData data)
        {
            this.cardData = data;
            this.ApplyTextures();
        }

        /// <summary>Shows the front face immediately (no animation).</summary>
        public void ShowFront()
        {
            StopFlip();
            currentYAngle = 0f;
            transform.localEulerAngles = new Vector3(0f, currentYAngle, 0f);
            isFacingFront = true;
        }

        /// <summary>Shows the card back face immediately (no animation).</summary>
        public void ShowBack()
        {
            StopFlip();
            currentYAngle = 180f;
            transform.localEulerAngles = new Vector3(0f, currentYAngle, 0f);
            isFacingFront = false;
        }

        /// <summary>Flips the card with a smooth SmoothStep animation.</summary>
        public void Flip()
        {
            StopFlip();
            float targetY = isFacingFront ? 180f : 0f;
            isFacingFront = !isFacingFront;
            flipCoroutine = StartCoroutine(FlipRoutine(currentYAngle, targetY));
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private Texture2D GetDefaultFrontFrame()
        {
            if (this.cardDefaults == null) return null;
            if (this.cardType == CardType.ability.ToString())
                return this.cardDefaults.CardFrontAbility1 ?? this.cardDefaults.CardFrontChar1;

            return this.cardDefaults.CardFrontChar1;
        }

        private void StopFlip()
        {
            if (flipCoroutine == null) return;
            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }

        private IEnumerator FlipRoutine(float fromY, float toY)
        {
            float elapsed = 0f;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flipDuration);
                currentYAngle = Mathf.LerpAngle(fromY, toY, Mathf.SmoothStep(0f, 1f, t));
                transform.localEulerAngles = new Vector3(0f, currentYAngle, 0f);
                yield return null;
            }

            currentYAngle = toY;
            transform.localEulerAngles = new Vector3(0f, toY, 0f);
            flipCoroutine = null;
        }

        /// <summary>
        /// Pushes card name, stars, ATK, DEF, and description from the card-definition
        /// fallback data to the matching <see cref="TextMeshPro"/> components.
        /// </summary>
        public void ApplyCardText()
        {
            if (this.cardData == null) return;

            this.ApplyDefaultFonts();
            this.ApplyDefaultColors();

            string displayName = this.fallbackName;
            // The card-face AtkText/DefText always show base_stats. Runtime buffs,
            // including Abyssal Mist, belong only to the battle stat UI and damage preview.
            int displayAtk = this.fallbackStats?.atk ?? 0;
            int displayDef = this.fallbackStats?.def ?? 0;
            int displayStars = this.fallbackStats?.star ?? 0;

            // Card artwork is local CardData, but the description is game data and
            // must come from the definition returned for the current battle.
            string displayDescription = this.fallbackDescription;

            this.SetTMPText(this.cardNameText, displayName);
            this.SetTMPText(this.starsText, new string('*', displayStars));
            this.SetTMPText(this.atkText, $"{displayAtk}");
            this.SetTMPText(this.defText, $"{displayDef}");
            this.SetTMPText(this.descriptionText, displayDescription);
            this.ApplyStatsVisibility();
        }

        private void SetTMPText(TextMeshPro tmp, string text)
        {
            if (tmp == null) return;
            tmp.text = text;
        }

        private void ApplyDefaultFonts()
        {
            if (this.cardDefaults == null) return;

            this.SetTMPFont(this.cardNameText, this.cardDefaults.CardNameFont);
            this.SetTMPFont(this.atkText, this.cardDefaults.AtkFont);
            this.SetTMPFont(this.defText, this.cardDefaults.DefFont);
            this.SetTMPFont(this.descriptionText, this.cardDefaults.DescriptionFont);
            SetTMPTypography(this.cardNameText, this.cardDefaults.CardNameFontSize, this.cardDefaults.CardNameBold);
            SetTMPTypography(this.starsText, this.cardDefaults.StarsFontSize, this.cardDefaults.StarsBold);
            SetTMPTypography(this.atkText, this.cardDefaults.AtkFontSize, this.cardDefaults.AtkBold);
            SetTMPTypography(this.defText, this.cardDefaults.DefFontSize, this.cardDefaults.DefBold);
            SetTMPTypography(this.descriptionText, this.cardDefaults.DescriptionFontSize, this.cardDefaults.DescriptionBold);
        }

        /// <summary>Applies the text colors configured in <see cref="CardDefaults"/>.</summary>
        public void ApplyDefaultColors()
        {
            if (this.cardDefaults == null) return;

            SetTMPColor(this.cardNameText, this.cardDefaults.CardNameColor);
            SetTMPColor(this.starsText, this.cardDefaults.StarsColor);
            SetTMPColor(this.atkText, this.cardDefaults.AtkColor);
            SetTMPColor(this.defText, this.cardDefaults.DefColor);
            SetTMPColor(this.descriptionText, this.cardDefaults.DescriptionColor);
        }

        private void SetTMPFont(TextMeshPro tmp, TMP_FontAsset font)
        {
            if (tmp == null || font == null) return;
            tmp.font = font;
        }

        private static void SetTMPColor(TextMeshPro tmp, Color color)
        {
            if (tmp == null) return;
            tmp.color = color;
        }

        private static void SetTMPTypography(TextMeshPro tmp, float fontSize, bool bold)
        {
            if (tmp == null) return;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }

        private void ApplyStatsVisibility()
        {
            bool showStats = string.Equals(this.cardType, "character", System.StringComparison.OrdinalIgnoreCase);
            if (this.atkText != null) this.atkText.gameObject.SetActive(showStats);
            if (this.defText != null) this.defText.gameObject.SetActive(showStats);
        }

        private void SetCharacterRendererVisible(bool visible)
        {
            if (this.characterRenderer == null) return;
            this.characterRenderer.enabled = visible;
        }

        private static void SetRendererTexture(Renderer rend, Texture2D texture)
        {
            if (rend == null || texture == null) return;
            rend.material.mainTexture = texture;
        }

        /// <summary>
        /// Makes all card text components visible from the front side only.
        /// Prevents text from showing through the back of the card.
        /// </summary>
        private void ApplyFrontFaceCulling()
        {
            SetTMPCullMode(this.cardNameText);
            SetTMPCullMode(this.starsText);
            SetTMPCullMode(this.atkText);
            SetTMPCullMode(this.defText);
            SetTMPCullMode(this.descriptionText);
        }

        /// <summary>
        /// Adds a SortingGroup to the card root so every renderer on this card
        /// is treated as one atomic unit during transparency sorting.
        /// Prevents text from any other card from interleaving with
        /// the Character/Frame/Text layers of this card.
        /// </summary>
        private void ApplySortingGroup()
        {
            this.EnsureSortingGroup();
            this.ApplySortingOrder();
        }

        private void EnsureSortingGroup()
        {
            if (this.gameObject.GetComponent<SortingGroup>() != null) return;
            this.gameObject.AddComponent<SortingGroup>();
        }

        /// <summary>
        /// Sets per-renderer sortingOrder within this card's SortingGroup:
        /// Character (0) → Text (1) → Frame (2).
        /// </summary>
        private void ApplySortingOrder()
        {
            if (this.characterRenderer != null) this.characterRenderer.sortingOrder = 0;
            this.SetTextSortingOrder(1);
            if (this.frontFrameRenderer != null) this.frontFrameRenderer.sortingOrder = 2;
        }

        private void SetTextSortingOrder(int order)
        {
            SetTMPSortingOrder(this.cardNameText, order);
            SetTMPSortingOrder(this.starsText, order);
            SetTMPSortingOrder(this.atkText, order);
            SetTMPSortingOrder(this.defText, order);
            SetTMPSortingOrder(this.descriptionText, order);
        }

        private static void SetTMPSortingOrder(TextMeshPro tmp, int order)
        {
            if (tmp == null) return;
            tmp.sortingOrder = order;
        }

        /// <summary>Sets Back face culling on a single TMP component's material instance.</summary>
        private static void SetTMPCullMode(TMPro.TextMeshPro tmp)
        {
            if (tmp == null) return;
            tmp.fontMaterial.SetFloat("_CullMode", 2f);
        }
    }
}

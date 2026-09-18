using System.Collections;
using SG03.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    /// <summary>Drives a UI Toolkit health bar displayed above a world-space target.</summary>
    [AddComponentMenu("SG03/BattleState/World Space HP Bar")]
    public sealed class WorldSpaceHpBarCtrl : PoolObj
    {
        [Header("Required runtime references")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private BattleStateCtrl battleStateCtrl;

        [Header("Icon Settings")]
        [Tooltip("Width and height of the defense shield icon.")]
        [SerializeField] private Vector2 iconSize = new Vector2(52f, 56f);

        [Header("Preview health")]
        [SerializeField, Min(0f)] private float currentHealth = 0f;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool faceMainCamera = true;

        [Header("Health change preview")]
        [Tooltip("Signed preview amount added temporarily to current damage. Zero hides the preview.")]
        [SerializeField] private float healthPreviewDelta;
        [SerializeField, Range(0f, 1f)] private float previewOpacity = 1f;
        [Tooltip("Number of full gradient traversals per second. Zero pauses the color animation.")]
        [SerializeField, Min(0f)] private float previewColorAnimationSpeed = 1.5f;

        [Header("Bar appearance")]
        [SerializeField, Min(1f)] private float barWidth = 350;
        [SerializeField, Min(1f)] private float barHeight = 20f;
        [SerializeField, Min(0f)] private float borderThickness = 4f;
        [Tooltip("Green is on the left and red is on the right. The bar reads this gradient from left to right as HP increases.")]
        [SerializeField] private Gradient healthColorGradient = CreateDefaultHealthColorGradient();
        [Tooltip("Offset of the HP values from the top-left corner of HealthBarRoot.")]
        [SerializeField] private Vector2 healthLabelOffset = new(0, -45f);
        [SerializeField, Min(1f)] private float healthLabelFontSize = 60f;

        [Header("Display mode")]
        [Tooltip("When enabled, only the HP bar is displayed. Disable it to also show the current and maximum HP.")]
        [SerializeField] private bool miniMode = true;
        private bool finalDefOnlyMode;

        [Header("Parenting")]
        [SerializeField] private Card3DCtrl cardCtrl;
        [Tooltip("World-space height above the card. This is unaffected by card flips.")]
        [SerializeField, Min(0f)] private float aboveCardYOffset = 0.2f;

        private VisualElement fill;
        private VisualElement healthPreview;
        private VisualElement root;
        private VisualElement track;
        private VisualElement healthLabelRow;
        private Label currentHealthLabel;
        private Label healthLabelSeparator;
        private Label maxHealthLabel;
        private Vector3 baseWorldRotation;
        private bool hasBaseWorldRotation;
        private ClientActions subscribedClientActions;
        private Coroutine deferredUiRefreshRoutine;
        private float previewColorAnimationTime;

        /// <summary>Runtime UI Toolkit element that renders the health fill.</summary>
        public VisualElement FillElement => this.fill;

        /// <summary>Runtime UI Toolkit element that renders a pending health change.</summary>
        public VisualElement HealthPreviewElement => this.healthPreview;

        /// <summary>Runtime UI Toolkit root element of this health bar.</summary>
        public VisualElement RootElement => this.root;

        /// <summary>Runtime UI Toolkit element that renders the health track.</summary>
        public VisualElement TrackElement => this.track;

        private void LateUpdate()
        {
            this.UpdateWorldSpacePresentation();
        }

        private void OnEnable()
        {
            this.HandleEnabled();
        }

        private void OnDisable()
        {
            this.HandleDisabled();
        }

        protected override void Start()
        {
            this.InitializeHpBar();
        }

        private void InitializeHpBar()
        {
            this.RefreshUi();
        }

        private void RefreshUiWhenEnabled()
        {
            this.RefreshUi();
        }

        private void HandleEnabled()
        {
            this.RefreshUiWhenEnabled();
            this.RefreshUiWhenDocumentIsReady();
            this.BindClientActionEvents();
            this.RefreshHealthFromBattleState();
        }

        private void HandleDisabled()
        {
            this.UnbindClientActionEvents();
            this.deferredUiRefreshRoutine = null;
        }

        private void RefreshUiWhenDocumentIsReady()
        {
            if (this.deferredUiRefreshRoutine != null) return;
            this.deferredUiRefreshRoutine = this.StartCoroutine(this.RefreshUiWhenDocumentIsReadyRoutine());
        }

        private IEnumerator RefreshUiWhenDocumentIsReadyRoutine()
        {
            yield return null;
            this.deferredUiRefreshRoutine = null;
            if (!this.isActiveAndEnabled) yield break;
            this.RefreshUi();
        }

        /// <summary>Returns the pool key used by <see cref="Spawner{T}"/>.</summary>
        public override string GetName()
        {
            return nameof(WorldSpaceHpBarCtrl);
        }

        private void UpdateWorldSpacePresentation()
        {
            this.UpdateWorldPositionFromCard();
            this.FaceMainCamera();
            this.UpdateHealthPreviewAnimation();
        }

        /// <summary>Immediately aligns this bar with its tracked card after a face-state change.</summary>
        public void RefreshWorldSpacePresentation()
        {
            this.UpdateWorldSpacePresentation();
        }

        private void FaceMainCamera()
        {
            if (!this.faceMainCamera || Camera.main == null) return;

            Vector3 directionFromCamera = this.transform.position - Camera.main.transform.position;
            if (directionFromCamera.sqrMagnitude < Mathf.Epsilon) return;

            // IMPORTANT: Update only pitch (X). Y/Z define the card-side orientation and must
            // remain unchanged; do not replace this with LookAt or transform.forward assignment.
            float horizontalDistance = new Vector2(directionFromCamera.x, directionFromCamera.z).magnitude;
            Vector3 worldRotation = this.hasBaseWorldRotation
                ? this.baseWorldRotation
                : this.transform.eulerAngles;
            worldRotation.x = -Mathf.Atan2(directionFromCamera.y, horizontalDistance) * Mathf.Rad2Deg;
            this.transform.rotation = Quaternion.Euler(worldRotation);
        }

        /// <summary>Gets the active DEF value for the card this bar is attached to.</summary>
        private int GetFinalDef()
        {
            return this.TryGetBattleCardSlot(out BattleCardSlot slot) ? slot.final_def : 0;
        }

        private int GetTotalDamageReceived()
        {
            return this.TryGetBattleCardSlot(out BattleCardSlot slot) ? slot.total_damage_received : 0;
        }

        private void RefreshMaxHealthFromBattleState()
        {
            if (!this.TryGetBattleCardSlot(out _)) return;

            this.SetMaxHealth(this.GetFinalDef());
        }

        private bool TryGetBattleCardSlot(out BattleCardSlot result)
        {
            result = null;
            // Pool activation occurs before Card3DCtrl assigns the tracked card through SetCard.
            // Use Unity's null check explicitly; ?. does not handle destroyed/unassigned Unity objects.
            if (this.cardCtrl == null) return false;

            string inventoryItemId = this.cardCtrl?.InventoryItemId;
            BattleState state = this.battleStateCtrl?.BattleState;
            if (state == null || string.IsNullOrEmpty(inventoryItemId)) return false;

            result = this.FindSlot(state.AlphaHand, inventoryItemId)
                ?? this.FindSlot(state.AlphaFrontLine, inventoryItemId)
                ?? this.FindSlot(state.AlphaBackLine, inventoryItemId)
                ?? this.FindSlot(state.AlphaTheVoid, inventoryItemId)
                ?? this.FindSlot(state.AlphaTheSource, inventoryItemId)
                ?? this.FindSlot(state.OmegaHand, inventoryItemId)
                ?? this.FindSlot(state.OmegaFrontLine, inventoryItemId)
                ?? this.FindSlot(state.OmegaBackLine, inventoryItemId)
                ?? this.FindSlot(state.OmegaTheVoid, inventoryItemId);
            return result != null;
        }

        private BattleCardSlot FindSlot(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot != null && slot.inventory_item_id == inventoryItemId) return slot;
            }

            return null;
        }

        public void SetHealth(float current, float maximum)
        {
            this.maxHealth = Mathf.Max(1f, maximum);
            this.currentHealth = Mathf.Clamp(current, 0f, this.maxHealth);
            this.RefreshUi();
        }

        /// <summary>Previews a signed damage change without modifying the resolved battle-state value.</summary>
        public void SetHealthPreview(float signedDelta)
        {
            this.healthPreviewDelta = signedDelta;
            this.previewColorAnimationTime = 0f;
            this.RefreshUi();
        }

        /// <summary>Hides the pending health-change preview.</summary>
        public void ClearHealthPreview()
        {
            this.SetHealthPreview(0f);
        }

        /// <summary>Previews an incoming damage delta starting from an explicit base damage amount.</summary>
        public void SetDamagePreview(float baseDamage, float damageDelta)
        {
            int finalDef = this.GetFinalDef();
            if (finalDef <= 0 && this.cardCtrl?.Definition != null)
                finalDef = this.cardCtrl.Definition.GetBaseStatInt("def");

            this.maxHealth = Mathf.Max(1f, finalDef);
            this.currentHealth = Mathf.Clamp(baseDamage, 0f, this.maxHealth);
            this.healthPreviewDelta = Mathf.Max(0f, damageDelta);
            this.previewColorAnimationTime = 0f;
            this.RefreshUi();
        }

        private void SetMaxHealth(float maximum)
        {
            this.maxHealth = Mathf.Max(1f, maximum);
            this.currentHealth = Mathf.Clamp(this.currentHealth, 0f, this.maxHealth);
            this.RefreshUi();
        }

        private void ResetCurrentHealthForNewCard()
        {
            this.currentHealth = 0f;
            this.healthPreviewDelta = 0f;
            this.previewColorAnimationTime = 0f;
        }

        /// <summary>Sets the health bar's world-space position.</summary>
        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }

        /// <summary>Assigns the card this bar follows without inheriting its transform.</summary>
        public void SetCard(Card3DCtrl newCard)
        {
            if (newCard == null) return;

            bool isNewCard = this.cardCtrl != newCard;
            this.cardCtrl = newCard;
            if (isNewCard) this.ResetCurrentHealthForNewCard();
            this.transform.SetParent(null, true);
            this.UpdateWorldPositionFromCard();
            this.baseWorldRotation = this.transform.eulerAngles;
            this.hasBaseWorldRotation = true;
            this.BindClientActionEvents();
            this.RefreshMaxHealthFromBattleState();
        }

        /// <summary>Clears the card currently shown in this pooled bar's Inspector.</summary>
        public void ClearCard()
        {
            this.cardCtrl = null;
        }

        /// <summary>Sets whether this bar displays only its fill or also its HP values.</summary>
        public void SetMiniMode(bool enabled)
        {
            this.SetDisplayMode(enabled, false);
        }

        /// <summary>Sets the compact/full presentation and whether the full view contains only final DEF.</summary>
        public void SetDisplayMode(bool miniModeEnabled, bool finalDefOnlyEnabled)
        {
            this.miniMode = miniModeEnabled;
            this.finalDefOnlyMode = !miniModeEnabled && finalDefOnlyEnabled;
            this.RefreshTrackVisibility();
            this.RefreshHealthLabel();
        }

        /// <summary>Switches between the mini bar-only view and the full view with HP values.</summary>
        public void ToggleDisplayMode()
        {
            this.SetMiniMode(!this.miniMode);
        }

        /// <summary>Reapplies the world-space follow offset for the assigned card.</summary>
        public void UpdateParent()
        {
            if (this.cardCtrl == null)
            {
                Debug.LogWarning($"{this.name}: CardCtrl is not assigned.", this);
                return;
            }

            this.transform.SetParent(null, true);
            this.UpdateWorldPositionFromCard();
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.BindUi();
            this.LoadBattleStateCtrl();
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = FindAnyObjectByType<BattleStateCtrl>();
        }

        private void BindClientActionEvents()
        {
            this.LoadBattleStateCtrl();
            ClientActions clientActions = this.battleStateCtrl?.ClientActions;
            if (clientActions == this.subscribedClientActions) return;

            this.UnbindClientActionEvents();
            this.subscribedClientActions = clientActions;
            if (this.subscribedClientActions != null)
            {
                this.subscribedClientActions.OnCardTakeDamageExecuted += this.OnCardTakeDamageExecuted;
            }
        }

        private void UnbindClientActionEvents()
        {
            if (this.subscribedClientActions == null) return;
            this.subscribedClientActions.OnCardTakeDamageExecuted -= this.OnCardTakeDamageExecuted;
            this.subscribedClientActions = null;
        }

        private void OnCardTakeDamageExecuted(string targetCardId, int totalDamage)
        {
            if (this.cardCtrl == null) return;

            string cardId = this.cardCtrl.InventoryItemId;
            if (string.IsNullOrEmpty(cardId) || cardId != targetCardId) return;
            this.RefreshHealthFromDamageAction(totalDamage);
        }

        private void RefreshHealthFromDamageAction(int totalDamage = -1)
        {
            // The client action selects when to refresh; totalDamage provides action authority.
            int currentDamage = totalDamage >= 0 ? totalDamage : this.GetTotalDamageReceived();
            int finalDef = this.GetFinalDef();
            if (finalDef <= 0 && this.cardCtrl?.Definition != null)
                finalDef = this.cardCtrl.Definition.GetBaseStatInt("def");

            this.SetHealth(currentDamage, finalDef);
            // The damage animation starts in the same action. Re-anchor now rather than waiting
            // for the next LateUpdate, so the bar remains above the card for this frame as well.
            this.RefreshWorldSpacePresentation();
        }

        /// <summary>Refreshes both values from battle state after an end-turn action resets card state.</summary>
        public void RefreshHealthFromTurnEnd()
        {
            if (!this.TryGetBattleCardSlot(out _)) return;
            this.SetHealth(this.GetTotalDamageReceived(), this.GetFinalDef());
        }

        /// <summary>Refreshes the card's resolved health values without clearing its pending preview.</summary>
        public void RefreshHealthFromBattleState()
        {
            if (!this.TryGetBattleCardSlot(out _)) return;
            this.SetHealth(this.GetTotalDamageReceived(), this.GetFinalDef());
        }

        private void UpdateWorldPositionFromCard()
        {
            if (this.cardCtrl == null) return;

            Card3D card = this.cardCtrl.GetComponent<Card3D>();
            if (card != null && card.TryGetStatsCenterWorldPosition(out Vector3 statsCenter))
            {
                this.transform.position = statsCenter + Vector3.up * this.aboveCardYOffset;
                return;
            }

            this.transform.position = this.cardCtrl.transform.position + Vector3.up * this.aboveCardYOffset;
        }

        private void BindUi(bool forceRebind = false)
        {
            if (forceRebind) this.ClearUiElementReferences();

            if (this.uiDocument == null)
            {
                this.uiDocument = this.GetComponent<UIDocument>();
            }

            if (this.uiDocument == null)
            {
                Debug.LogWarning($"{this.name}: UIDocument is missing.", this.gameObject);
                return;
            }

            VisualElement uiRoot = this.uiDocument.rootVisualElement;
            if (uiRoot == null)
            {
                // Expected while SpawnInactive configures this object before it is enabled.
                return;
            }

            this.LoadUiElement(ref this.root, uiRoot, "HealthBarRoot");
            this.LoadUiElement(ref this.track, uiRoot, "HealthTrack");
            this.LoadUiElement(ref this.fill, uiRoot, "HealthFill");
            this.LoadUiElement(ref this.healthPreview, uiRoot, "HealthPreview");
            this.LoadUiElement(ref this.healthLabelRow, uiRoot, "HealthLabelRow");
            this.LoadHealthLabels(uiRoot);
        }

        private void ClearUiElementReferences()
        {
            this.fill = null;
            this.healthPreview = null;
            this.root = null;
            this.track = null;
            this.healthLabelRow = null;
            this.currentHealthLabel = null;
            this.healthLabelSeparator = null;
            this.maxHealthLabel = null;
        }

        private void LoadUiElement(ref VisualElement element, VisualElement uiRoot, string elementName)
        {
            if (element != null) return;
            element = uiRoot.Q<VisualElement>(elementName);
            if (element == null)
            {
                Debug.LogWarning($"{this.name}: UI element '{elementName}' is missing.", this.gameObject);
            }
        }

        private void LoadHealthLabels(VisualElement uiRoot)
        {
            if (this.currentHealthLabel != null
                && this.healthLabelSeparator != null
                && this.maxHealthLabel != null) return;

            this.currentHealthLabel = uiRoot.Q<Label>("CurrentHealthLabel");
            this.healthLabelSeparator = uiRoot.Q<Label>("HealthLabelSeparator");
            this.maxHealthLabel = uiRoot.Q<Label>("MaxHealthLabel");
            if (this.currentHealthLabel == null
                || this.healthLabelSeparator == null
                || this.maxHealthLabel == null)
            {
                Debug.LogWarning($"{this.name}: one or more health-label UI elements are missing.", this.gameObject);
            }

            Image shieldImage = uiRoot.Q<Image>("DefShieldIcon");
            if (shieldImage != null)
            {
                if (this.iconSize.x > 0f && this.iconSize.y > 0f)
                {
                    shieldImage.style.width = this.iconSize.x;
                    shieldImage.style.height = this.iconSize.y;
                }
            }
        }

        public void RefreshUi()
        {
            this.BindUi(true);
            if (this.fill == null) return;
            float currentRatio = Mathf.Clamp01(this.currentHealth / this.maxHealth);
            this.ApplyBarAppearance(currentRatio);
            this.RefreshHealthPreview();

            this.RefreshTrackVisibility();
            this.RefreshHealthLabel();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            this.RefreshUi();
        }
#endif

        private void RefreshHealthPreview()
        {
            if (this.fill == null || this.healthPreview == null) this.BindUi();
            if (this.fill == null || this.healthPreview == null) return;

            float currentRatio = Mathf.Clamp01(this.currentHealth / this.maxHealth);
            float previewHealth = Mathf.Clamp(this.currentHealth + this.healthPreviewDelta, 0f, this.maxHealth);
            float previewRatio = Mathf.Clamp01(previewHealth / this.maxHealth);
            bool hasPreview = !Mathf.Approximately(previewRatio, currentRatio);

            this.healthPreview.style.display = hasPreview ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasPreview)
            {
                this.fill.style.width = Length.Percent(currentRatio * 100f);
                return;
            }

            bool isDamagePreview = previewRatio > currentRatio;
            float segmentStart = Mathf.Min(currentRatio, previewRatio);
            float segmentWidth = Mathf.Abs(previewRatio - currentRatio);
            this.fill.style.width = Length.Percent((isDamagePreview ? previewRatio : currentRatio) * 100f);
            this.fill.style.backgroundColor = this.healthColorGradient.Evaluate(
                isDamagePreview ? previewRatio : currentRatio);
            this.healthPreview.style.left = Length.Percent(segmentStart * 100f);
            this.healthPreview.style.width = Length.Percent(segmentWidth * 100f);
            this.healthPreview.style.backgroundColor = this.GetHealthPreviewColor(!isDamagePreview);
            this.healthPreview.style.opacity = this.previewOpacity;
        }

        private Color GetHealthPreviewColor(bool isHealing)
        {
            float animationPosition = Mathf.PingPong(
                this.previewColorAnimationTime * this.previewColorAnimationSpeed,
                1f);
            float gradientPosition = isHealing ? animationPosition : 1f - animationPosition;
            return this.healthColorGradient.Evaluate(gradientPosition);
        }

        private void UpdateHealthPreviewAnimation()
        {
            if (this.healthPreview == null || this.healthPreview.resolvedStyle.display == DisplayStyle.None) return;

            this.previewColorAnimationTime += Time.deltaTime;
            bool isHealingPreview = this.healthPreviewDelta < 0f;
            Color previewColor = this.GetHealthPreviewColor(isHealingPreview);
            this.healthPreview.style.backgroundColor = previewColor;
            this.healthPreview.style.opacity = this.previewOpacity;
            if (this.currentHealthLabel != null)
                this.currentHealthLabel.style.color = previewColor;
        }

        private void RefreshTrackVisibility()
        {
            if (this.track == null) this.BindUi();
            if (this.track == null) return;

            // A bar revealed specifically by hover shows only final DEF as a centered value.
            this.track.style.display = this.ShouldShowFinalDefOnly() ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private bool ShouldShowFinalDefOnly()
        {
            return !this.miniMode
                && this.finalDefOnlyMode
                && Mathf.Approximately(this.healthPreviewDelta, 0f);
        }

        private void RefreshHealthLabel()
        {
            if (this.healthLabelRow == null
                || this.currentHealthLabel == null
                || this.healthLabelSeparator == null
                || this.maxHealthLabel == null) this.BindUi();
            if (this.healthLabelRow == null
                || this.currentHealthLabel == null
                || this.healthLabelSeparator == null
                || this.maxHealthLabel == null) return;

            // Keep the label in the UI layout when hidden so the HP bar never changes position.
            bool hideLabel = this.miniMode && Mathf.Approximately(this.healthPreviewDelta, 0f);
            this.healthLabelRow.style.visibility = hideLabel ? Visibility.Hidden : Visibility.Visible;
            if (this.ShouldShowFinalDefOnly())
            {
                this.currentHealthLabel.text = $"{Mathf.CeilToInt(this.maxHealth)}";
                this.currentHealthLabel.style.color = StyleKeyword.Null;
                this.healthLabelSeparator.style.display = DisplayStyle.None;
                this.maxHealthLabel.style.display = DisplayStyle.None;
                return;
            }

            this.healthLabelSeparator.style.display = DisplayStyle.Flex;
            this.maxHealthLabel.style.display = DisplayStyle.Flex;
            float displayedCurrentHealth = Mathf.Clamp(
                this.currentHealth + this.healthPreviewDelta,
                0f,
                this.maxHealth);
            this.currentHealthLabel.text = $"{Mathf.CeilToInt(displayedCurrentHealth)}";
            this.maxHealthLabel.text = $"{Mathf.CeilToInt(this.maxHealth)}";
            bool hasPreview = !Mathf.Approximately(displayedCurrentHealth, this.currentHealth);
            this.currentHealthLabel.style.color = hasPreview
                ? this.GetHealthPreviewColor(this.healthPreviewDelta < 0f)
                : StyleKeyword.Null;
        }

        private void ApplyBarAppearance(float healthRatio)
        {
            this.fill.style.backgroundColor = this.healthColorGradient.Evaluate(healthRatio);

            if (this.root != null)
            {
                this.root.style.width = this.barWidth;
                this.root.style.height = this.barHeight;
            }

            if (this.track != null)
            {
                this.track.style.width = this.barWidth;
                this.track.style.height = this.barHeight;
                this.track.style.borderTopWidth = this.borderThickness;
                this.track.style.borderRightWidth = this.borderThickness;
                this.track.style.borderBottomWidth = this.borderThickness;
                this.track.style.borderLeftWidth = this.borderThickness;
            }

            if (this.healthLabelRow != null)
            {
                this.healthLabelRow.style.width = this.barWidth;
                this.healthLabelRow.style.height = this.barHeight;
                this.healthLabelRow.style.left = this.healthLabelOffset.x;
                this.healthLabelRow.style.top = this.healthLabelOffset.y;
            }

            if (this.currentHealthLabel != null)
            {
                this.currentHealthLabel.style.fontSize = this.healthLabelFontSize;
            }

            if (this.healthLabelSeparator != null)
            {
                this.healthLabelSeparator.style.fontSize = this.healthLabelFontSize;
            }

            if (this.maxHealthLabel != null)
            {
                this.maxHealthLabel.style.fontSize = this.healthLabelFontSize;
            }
        }

        private static Gradient CreateDefaultHealthColorGradient()
        {
            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.18f, 0.8f, 0.28f), 0f),
                    new GradientColorKey(new Color(0.95f, 0.72f, 0.12f), 0.5f),
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.12f), 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                },
            };
        }

    }
}

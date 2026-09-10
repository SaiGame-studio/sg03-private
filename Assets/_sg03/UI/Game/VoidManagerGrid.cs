using System.Collections.Generic;
using SaiGame.Services;
using SG03;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace SG03.UI
{
    public class VoidManagerGrid : SaiBehaviour
    {
        private enum VoidCardSortMode
        {
            NewestFirst,
            OldestFirst,
            StarsAscending,
            StarsDescending,
            AttackAscending,
            AttackDescending,
            DefenseAscending,
            DefenseDescending
        }

        private static readonly List<string> SortChoices = new List<string>
        {
            "Void time: Newest first",
            "Void time: Oldest first",
            "Stars: Low to high",
            "Stars: High to low",
            "ATK: Low to high",
            "ATK: High to low",
            "DEF: Low to high",
            "DEF: High to low"
        };

        [Header("Grid Size")]
        [SerializeField, Min(1)] private int columns = 6;
        [SerializeField, Min(1)] private int rows = 2;

        [Header("Card Size (9:16 Default)")]
        [SerializeField, Min(1)] private int cardWidth = 130;
        [SerializeField, Min(1)] private int cardHeight = 160;

        [Header("Card Text")]
        [SerializeField, Min(1)] private int fontSize = 9;

        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private BattleStateCtrl battleStateCtrl;
        [SerializeField] private CardDataManager cardDataManager;
        [SerializeField] private ModalDimLayer dimLayer;

        private VisualElement content;
        private VisualElement panel;
        private Label alphaVoidCountLabel;
        private Label omegaVoidCountLabel;
        private Label titleLabel;
        private Label pageLabel;
        private DropdownField sortField;
        private Button closeButton;
        private Button previousButton;
        private Button nextButton;
        private Card3DCtrl hoveredCard;
        private BattleState subscribedBattleState;
        private readonly Dictionary<string, Texture2D> cardArtCache = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, List<Image>> pendingCardArtImages = new Dictionary<string, List<Image>>();
        private readonly Dictionary<string, AsyncOperationHandle<CardData>> cardArtHandles = new Dictionary<string, AsyncOperationHandle<CardData>>();
        private int currentPage;
        private VoidCardSortMode sortMode = VoidCardSortMode.NewestFirst;
        private Owner voidOwner = Owner.alpha;
        private bool isVisible;
        private bool isDisposed;

        public int Columns => Mathf.Max(1, this.columns);
        public int Rows => Mathf.Max(1, this.rows);
        public int CardWidth => Mathf.Max(1, this.cardWidth);
        public int CardHeight => Mathf.Max(1, this.cardHeight);
        public int FontSize => Mathf.Max(1, this.fontSize);

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadUIDocument();
            this.LoadBattleStateCtrl();
            this.LoadCardDataManager();
            this.LoadDimLayer();
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            if (this.uiDocument == null) return;
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = FindFirstObjectByType<BattleStateCtrl>();
            if (this.battleStateCtrl == null) return;
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        private void LoadCardDataManager()
        {
            if (this.cardDataManager != null) return;
            this.cardDataManager = CardDataManager.Instance;
            if (this.cardDataManager == null) this.cardDataManager = FindFirstObjectByType<CardDataManager>();
            if (this.cardDataManager == null) return;
            Debug.LogWarning(this.transform.name + ": LoadCardDataManager", this.gameObject);
        }

        private void LoadDimLayer()
        {
            if (this.dimLayer != null) return;
            this.dimLayer = this.GetComponentInChildren<ModalDimLayer>(true);
            if (this.dimLayer == null) return;
            Debug.LogWarning(this.transform.name + ": LoadDimLayer", this.gameObject);
        }

        protected override void Start()
        {
            base.Start();
            this.InitializeGrid();
        }

        private void InitializeGrid()
        {
            this.isDisposed = false;
            if (this.uiDocument == null)
            {
                Debug.LogError(this.transform.name + ": VoidManagerGrid requires a UIDocument reference.", this.gameObject);
                return;
            }

            this.BindElements(this.uiDocument.rootVisualElement);
            if (!this.InitializeDimLayer(this.uiDocument.rootVisualElement)) return;
            this.ConfigureSortField();
            this.RegisterCallbacks();
            this.SubscribeToCardHoverEvents();
            this.SubscribeToBattleState();
            this.SubscribeToCardDefinitions();
            this.Hide();
        }

        public void SetGridSize(int columnCount, int rowCount)
        {
            this.columns = Mathf.Max(1, columnCount);
            this.rows = Mathf.Max(1, rowCount);
            this.currentPage = 0;
            if (this.isVisible) this.Refresh();
        }

        public void SetCardSize(int width, int height)
        {
            this.cardWidth = Mathf.Max(1, width);
            this.cardHeight = Mathf.Max(1, height);
            if (this.isVisible) this.Refresh();
        }

        public void RefreshUI()
        {
            this.Refresh();
        }

        private void Update()
        {
            this.TickGrid();
        }

        private void TickGrid()
        {
            this.SubscribeToBattleState();
            if (this.isVisible)
            {
                this.CloseWhenEscapeIsPressed();
                return;
            }

            this.OpenWhenVoidCardIsClicked();
        }

        protected virtual void OnDestroy()
        {
            this.DisposeGrid();
        }

        private void DisposeGrid()
        {
            this.isDisposed = true;
            this.UnregisterCallbacks();
            this.UnsubscribeFromCardHoverEvents();
            this.UnsubscribeFromBattleState();
            this.UnsubscribeFromCardDefinitions();
            this.ReleaseCardArtHandles();
            this.hoveredCard = null;
        }

        private void SubscribeToCardDefinitions()
        {
            BattleCardDefinitions.OnDefinitionsLoaded += this.OnCardDefinitionsLoaded;
        }

        private void UnsubscribeFromCardDefinitions()
        {
            BattleCardDefinitions.OnDefinitionsLoaded -= this.OnCardDefinitionsLoaded;
        }

        private void ReleaseCardArtHandles()
        {
            foreach (AsyncOperationHandle<CardData> handle in this.cardArtHandles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            this.cardArtHandles.Clear();
            this.pendingCardArtImages.Clear();
            this.cardArtCache.Clear();
        }

        private void BindElements(VisualElement root)
        {
            this.panel = root?.Q("VoidGridPanel");
            this.content = root?.Q("VoidGridContent");
            this.alphaVoidCountLabel = root?.Q<Label>("AlphaTheVoidCountLabel");
            this.omegaVoidCountLabel = root?.Q<Label>("OmegaTheVoidCountLabel");
            this.titleLabel = root?.Q<Label>("VoidGridTitle");
            this.pageLabel = root?.Q<Label>("VoidGridPageLabel");
            this.sortField = root?.Q<DropdownField>("VoidGridSortField");
            this.closeButton = root?.Q<Button>("VoidGridCloseButton");
            this.previousButton = root?.Q<Button>("VoidGridPreviousButton");
            this.nextButton = root?.Q<Button>("VoidGridNextButton");
        }

        private bool InitializeDimLayer(VisualElement root)
        {
            if (this.dimLayer == null)
            {
                Debug.LogError(this.transform.name + ": VoidManagerGrid requires a ModalDimLayer child.", this.gameObject);
                return false;
            }

            if (!this.dimLayer.Initialize(root, this.panel)) return false;
            return this.dimLayer.Overlay != null;
        }

        private void ConfigureSortField()
        {
            if (this.sortField == null) return;
            this.sortField.choices = new List<string>(SortChoices);
            this.sortField.SetValueWithoutNotify(SortChoices[(int)this.sortMode]);
        }

        private void RegisterCallbacks()
        {
            this.alphaVoidCountLabel?.RegisterCallback<ClickEvent>(this.OnAlphaVoidCountClicked);
            this.omegaVoidCountLabel?.RegisterCallback<ClickEvent>(this.OnOmegaVoidCountClicked);
            this.closeButton?.RegisterCallback<ClickEvent>(this.OnCloseClicked);
            this.previousButton?.RegisterCallback<ClickEvent>(this.OnPreviousClicked);
            this.nextButton?.RegisterCallback<ClickEvent>(this.OnNextClicked);
            this.sortField?.RegisterValueChangedCallback(this.OnSortChanged);
            this.panel?.RegisterCallback<ClickEvent>(this.OnPanelClicked);
        }

        private void UnregisterCallbacks()
        {
            this.alphaVoidCountLabel?.UnregisterCallback<ClickEvent>(this.OnAlphaVoidCountClicked);
            this.omegaVoidCountLabel?.UnregisterCallback<ClickEvent>(this.OnOmegaVoidCountClicked);
            this.closeButton?.UnregisterCallback<ClickEvent>(this.OnCloseClicked);
            this.previousButton?.UnregisterCallback<ClickEvent>(this.OnPreviousClicked);
            this.nextButton?.UnregisterCallback<ClickEvent>(this.OnNextClicked);
            this.sortField?.UnregisterValueChangedCallback(this.OnSortChanged);
            this.panel?.UnregisterCallback<ClickEvent>(this.OnPanelClicked);
        }

        private void SubscribeToCardHoverEvents()
        {
            Card3DCtrl.HoverEntered += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited += this.OnCardHoverExited;
        }

        private void UnsubscribeFromCardHoverEvents()
        {
            Card3DCtrl.HoverEntered -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited -= this.OnCardHoverExited;
        }

        private void SubscribeToBattleState()
        {
            BattleState battleState = this.battleStateCtrl?.BattleState;
            if (battleState == this.subscribedBattleState) return;
            this.UnsubscribeFromBattleState();
            if (battleState == null) return;
            this.subscribedBattleState = battleState;
            this.subscribedBattleState.OnBattleStatusChanged += this.OnBattleStatusChanged;
        }

        private void UnsubscribeFromBattleState()
        {
            if (this.subscribedBattleState == null) return;
            this.subscribedBattleState.OnBattleStatusChanged -= this.OnBattleStatusChanged;
            this.subscribedBattleState = null;
        }

        private void OnAlphaVoidCountClicked(ClickEvent _)
        {
            this.Show(Owner.alpha);
        }

        private void OnOmegaVoidCountClicked(ClickEvent _)
        {
            this.Show(Owner.omega);
        }

        private void OnCloseClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            this.Hide();
        }

        private void OnPreviousClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            if (this.currentPage <= 0) return;
            this.currentPage--;
            this.Refresh();
        }

        private void OnNextClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            int pageCount = this.GetPageCount(this.GetVoidCards().Count);
            if (this.currentPage >= pageCount - 1) return;
            this.currentPage++;
            this.Refresh();
        }

        private void OnSortChanged(ChangeEvent<string> evt)
        {
            int sortIndex = SortChoices.IndexOf(evt.newValue);
            if (sortIndex < 0) return;
            this.sortMode = (VoidCardSortMode)sortIndex;
            this.currentPage = 0;
            this.Refresh();
        }

        private void OnPanelClicked(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnCardHoverEntered(Card3DCtrl card)
        {
            this.hoveredCard = card;
        }

        private void OnCardHoverExited(Card3DCtrl card)
        {
            if (this.hoveredCard != card) return;
            this.hoveredCard = null;
        }

        private void OnBattleStatusChanged()
        {
            if (!this.isVisible) return;
            this.Refresh();
        }

        private void OnCardDefinitionsLoaded()
        {
            if (!this.isVisible) return;
            this.Refresh();
        }

        private void OpenWhenVoidCardIsClicked()
        {
            if (Mouse.current?.leftButton.wasPressedThisFrame != true) return;
            if (this.hoveredCard == null) return;
            if (this.hoveredCard.Location != Location.in_void) return;
            this.Show(this.hoveredCard.CardOwner);
        }

        private void CloseWhenEscapeIsPressed()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            this.Hide();
        }

        private void Show(Owner owner)
        {
            if (this.dimLayer == null) return;
            this.voidOwner = owner;
            this.isVisible = true;
            this.currentPage = 0;
            this.dimLayer.Show();
            this.Refresh();
        }

        private void Hide()
        {
            this.isVisible = false;
            this.dimLayer?.Hide();
        }

        private void Refresh()
        {
            if (this.content == null) return;
            List<BattleCardSlot> cards = this.GetVoidCards();
            int pageCount = this.GetPageCount(cards.Count);
            this.currentPage = Mathf.Clamp(this.currentPage, 0, pageCount - 1);
            this.UpdateHeader(cards.Count);
            this.UpdatePagination(pageCount);
            this.BuildPage(cards);
        }

        private List<BattleCardSlot> GetVoidCards()
        {
            List<BattleCardSlot> cards = new List<BattleCardSlot>();
            BattleCardSlot[] slots = this.voidOwner == Owner.omega
                ? this.battleStateCtrl?.BattleState?.OmegaTheVoid
                : this.battleStateCtrl?.BattleState?.AlphaTheVoid;
            if (slots == null) return cards;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot != null) cards.Add(slot);
            }
            this.SortCards(cards);
            return cards;
        }

        private void SortCards(List<BattleCardSlot> cards)
        {
            if (this.sortMode == VoidCardSortMode.OldestFirst) return;
            if (this.sortMode == VoidCardSortMode.NewestFirst)
            {
                cards.Reverse();
                return;
            }

            Dictionary<BattleCardSlot, int> voidOrder = new Dictionary<BattleCardSlot, int>();
            for (int index = 0; index < cards.Count; index++) voidOrder[cards[index]] = index;
            bool ascending = this.sortMode == VoidCardSortMode.StarsAscending
                             || this.sortMode == VoidCardSortMode.AttackAscending
                             || this.sortMode == VoidCardSortMode.DefenseAscending;
            cards.Sort((left, right) => this.CompareCardStats(left, right, voidOrder, ascending));
        }

        private int CompareCardStats(
            BattleCardSlot left,
            BattleCardSlot right,
            IReadOnlyDictionary<BattleCardSlot, int> voidOrder,
            bool ascending)
        {
            int result = this.GetSortValue(left).CompareTo(this.GetSortValue(right));
            if (!ascending) result = -result;
            if (result != 0) return result;
            return voidOrder[right].CompareTo(voidOrder[left]);
        }

        private int GetSortValue(BattleCardSlot slot)
        {
            switch (this.sortMode)
            {
                case VoidCardSortMode.StarsAscending:
                case VoidCardSortMode.StarsDescending:
                    return this.GetCardStarCount(slot);
                case VoidCardSortMode.AttackAscending:
                case VoidCardSortMode.AttackDescending:
                    return this.GetCardAttack(slot);
                case VoidCardSortMode.DefenseAscending:
                case VoidCardSortMode.DefenseDescending:
                    return this.GetCardDefense(slot);
                default:
                    return 0;
            }
        }

        private int GetPageCount(int cardCount)
        {
            int pageSize = this.Columns * this.Rows;
            return Mathf.Max(1, Mathf.CeilToInt(cardCount / (float)pageSize));
        }

        private void UpdateHeader(int cardCount)
        {
            if (this.titleLabel == null) return;
            this.titleLabel.text = $"{this.GetVoidOwnerLabel()} - The Void ({cardCount})";
        }

        private void UpdatePagination(int pageCount)
        {
            if (this.pageLabel != null) this.pageLabel.text = $"Page {this.currentPage + 1} / {pageCount}";
            this.previousButton?.SetEnabled(this.currentPage > 0);
            this.nextButton?.SetEnabled(this.currentPage < pageCount - 1);
        }

        private void BuildPage(List<BattleCardSlot> cards)
        {
            this.content.Clear();
            if (cards.Count == 0)
            {
                Label emptyLabel = new Label($"{this.GetVoidOwnerLabel()}'s Void is empty.");
                emptyLabel.AddToClassList("void-grid-empty-label");
                this.content.Add(emptyLabel);
                return;
            }

            int pageStart = this.currentPage * this.Columns * this.Rows;
            for (int rowIndex = 0; rowIndex < this.Rows; rowIndex++)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("void-grid-row");
                this.content.Add(row);
                this.BuildRow(row, cards, pageStart + rowIndex * this.Columns);
            }
        }

        private void BuildRow(VisualElement row, List<BattleCardSlot> cards, int rowStart)
        {
            for (int columnIndex = 0; columnIndex < this.Columns; columnIndex++)
            {
                int cardIndex = rowStart + columnIndex;
                if (cardIndex < cards.Count)
                {
                    row.Add(this.BuildCard(cards[cardIndex]));
                    continue;
                }

                VisualElement placeholder = new VisualElement();
                placeholder.AddToClassList("void-grid-card-placeholder");
                this.ApplyCardSize(placeholder);
                row.Add(placeholder);
            }
        }

        private VisualElement BuildCard(BattleCardSlot slot)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("void-grid-card");
            this.ApplyCardSize(card);

            VisualElement artArea = new VisualElement();
            artArea.AddToClassList("void-grid-card-art");
            Image artImage = new Image { scaleMode = ScaleMode.ScaleAndCrop };
            artImage.AddToClassList("void-grid-card-art-image");
            artArea.Add(artImage);
            this.AddCardTextOverlays(artArea, slot);
            card.Add(artArea);
            this.LoadCardArt(artImage, slot.item_definition_code_name);
            return card;
        }

        private void AddCardTextOverlays(VisualElement artArea, BattleCardSlot slot)
        {
            CardDefinitionData definition = this.GetCardDefinition(slot.item_definition_code_name);

            VisualElement topOverlay = new VisualElement();
            topOverlay.AddToClassList("void-grid-card-overlay");
            topOverlay.AddToClassList("void-grid-card-overlay--top");

            Label nameLabel = new Label(this.GetCardDisplayName(slot, definition));
            nameLabel.AddToClassList("void-grid-card-overlay-name");
            this.ApplyFontSize(nameLabel);
            topOverlay.Add(nameLabel);

            int starCount = this.GetCardStarCount(slot, definition);
            Label starLabel = new Label(starCount.ToString());
            starLabel.AddToClassList("void-grid-card-overlay-stars");
            this.ApplyFontSize(starLabel);
            topOverlay.Add(starLabel);

            VisualElement bottomOverlay = new VisualElement();
            bottomOverlay.AddToClassList("void-grid-card-overlay");
            bottomOverlay.AddToClassList("void-grid-card-overlay--bottom");

            int attack = this.GetCardAttack(slot, definition);
            Label attackLabel = new Label($"ATK {attack}");
            attackLabel.AddToClassList("void-grid-card-overlay-stat");
            attackLabel.AddToClassList("void-grid-card-overlay-stat--attack");
            this.ApplyFontSize(attackLabel);
            bottomOverlay.Add(attackLabel);

            int defense = this.GetCardDefense(slot, definition);
            Label defenseLabel = new Label($"DEF {defense}");
            defenseLabel.AddToClassList("void-grid-card-overlay-stat");
            defenseLabel.AddToClassList("void-grid-card-overlay-stat--defense");
            this.ApplyFontSize(defenseLabel);
            bottomOverlay.Add(defenseLabel);

            artArea.Add(topOverlay);
            artArea.Add(bottomOverlay);
        }

        private int GetCardStarCount(BattleCardSlot slot, CardDefinitionData definition = null)
        {
            definition ??= this.GetCardDefinition(slot.item_definition_code_name);
            return Mathf.Max(0, definition?.GetBaseStatInt("star") ?? 0);
        }

        private int GetCardAttack(BattleCardSlot slot, CardDefinitionData definition = null)
        {
            definition ??= this.GetCardDefinition(slot.item_definition_code_name);
            return definition?.GetBaseStatInt("atk") ?? 0;
        }

        private int GetCardDefense(BattleCardSlot slot, CardDefinitionData definition = null)
        {
            definition ??= this.GetCardDefinition(slot.item_definition_code_name);
            return definition?.GetBaseStatInt("def") ?? slot.final_def;
        }

        private CardDefinitionData GetCardDefinition(string itemCode)
        {
            IReadOnlyList<CardDefinitionData> definitions = this.battleStateCtrl?.BattleCardDefinitions?.Definitions;
            if (definitions == null || string.IsNullOrWhiteSpace(itemCode)) return null;

            foreach (CardDefinitionData definition in definitions)
            {
                if (definition?.item_code == itemCode) return definition;
            }

            return null;
        }

        private string GetCardDisplayName(BattleCardSlot slot, CardDefinitionData definition)
        {
            if (!string.IsNullOrWhiteSpace(definition?.name)) return definition.name;
            if (!string.IsNullOrWhiteSpace(slot.item_definition_name)) return slot.item_definition_name;
            return slot.item_definition_code_name ?? string.Empty;
        }

        private void ApplyFontSize(Label label)
        {
            label.style.fontSize = this.FontSize;
        }

        private void LoadCardArt(Image target, string itemCode)
        {
            if (target == null) return;
            if (string.IsNullOrWhiteSpace(itemCode)) return;

            if (this.cardArtCache.TryGetValue(itemCode, out Texture2D cachedArt))
            {
                target.image = cachedArt;
                return;
            }

            if (this.pendingCardArtImages.TryGetValue(itemCode, out List<Image> pendingImages))
            {
                pendingImages.Add(target);
                return;
            }

            if (this.cardDataManager == null) this.LoadComponents();
            if (this.cardDataManager == null) return;
            if (!CardLoader.TryResolveAddressByAssetName(
                    this.cardDataManager.CardAddresses,
                    itemCode,
                    out string cardAddress))
                return;

            this.pendingCardArtImages[itemCode] = new List<Image> { target };
            AsyncOperationHandle<CardData> handle = Addressables.LoadAssetAsync<CardData>(cardAddress);
            this.cardArtHandles[itemCode] = handle;
            handle.Completed += operation => this.CompleteCardArtLoad(itemCode, operation);
        }

        private void CompleteCardArtLoad(string itemCode, AsyncOperationHandle<CardData> operation)
        {
            if (this.isDisposed) return;

            this.pendingCardArtImages.TryGetValue(itemCode, out List<Image> images);
            this.pendingCardArtImages.Remove(itemCode);
            if (operation.Status != AsyncOperationStatus.Succeeded
                || operation.Result?.CharacterTexture == null)
            {
                this.ReleaseFailedCardArtHandle(itemCode, operation);
                return;
            }

            Texture2D artwork = operation.Result.CharacterTexture;
            this.cardArtCache[itemCode] = artwork;
            if (images == null) return;
            foreach (Image image in images)
            {
                if (image != null) image.image = artwork;
            }
        }

        private void ReleaseFailedCardArtHandle(string itemCode, AsyncOperationHandle<CardData> operation)
        {
            this.cardArtHandles.Remove(itemCode);
            if (operation.IsValid()) Addressables.Release(operation);
        }

        private void ApplyCardSize(VisualElement card)
        {
            card.style.width = this.CardWidth;
            card.style.height = this.CardHeight;
        }

        private string GetVoidOwnerLabel()
            => this.voidOwner == Owner.omega ? "Omega" : "Alpha";
    }
}

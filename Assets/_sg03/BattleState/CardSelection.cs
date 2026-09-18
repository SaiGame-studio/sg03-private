using System.Collections;
using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public partial class CardSelection : SaiBehaviour
    {
        // ─── Static events ────────────────────────────────────────────────────────

        /// <summary>Fired when the player confirms a targeting selection (source → target).</summary>
        public static event System.Action<Card3DCtrl, Card3DCtrl> TargetSelected;

        /// <summary>Fired when the player enters full-detail view for a card.</summary>
        public static event System.Action OnFullDetailEntered;

        /// <summary>Fired when the player exits full-detail view.</summary>
        public static event System.Action OnFullDetailExited;

        [SerializeField] private bool debugLog;

        [Header("Input Control")]
        [SerializeField] private bool debugMouseEvents = false;

        [SerializeField] private BattleStateCtrl battleStateCtrl;

        [SerializeField] private Card3DCtrl selected;
        [SerializeField] private Card3DCtrl hovered;

        [Header("Holders")]
        [SerializeField] private CardHolderCtrl holderSelected;
        [SerializeField] private CardHolderCtrl holderHover;
        [SerializeField] private CardHolderHoverDetector holderHoverDetector;


        [Header("Full Detail")]
        [SerializeField] private DeskPositionCtrl deskPositions;
        [SerializeField] private bool fullDetail;

        public bool IsFullDetail => this.fullDetail;

        [Header("Marks")]
        [SerializeField] private GameObject markSelected;
        [SerializeField] private float markFollowSpeed = 10f;
        [SerializeField] private Transform markIdlePosition;

        [Header("Targeting")]
        [SerializeField] private ArrowIndicatorCtrl arrowIndicator;
        private Card3DCtrl targetingSource;
        private Card3DCtrl healthPreviewTarget;

        [Header("Front Line Holders")]
        [SerializeField] private CardHolderCtrl[] alphaFrontLineHolders;

        [Header("Deploy Limit")]
        [SerializeField] private int maxCharDeploy   = 1;
        [SerializeField] private int countCharDeploy = 0;

        private readonly Dictionary<string, PlayerDeployRecord> pendingPlayerDeploys = new Dictionary<string, PlayerDeployRecord>();

        private bool IsTargeting => this.targetingSource != null;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleStateCtrl();
            this.LoadDeskPositions();
            this.LoadMarkSelected();
            this.LoadMarkIdlePosition();
            this.LoadArrowIndicator();
            this.LoadAlphaFrontLineHolders();
            this.LoadHolderHoverDetector();
        }

        protected virtual void LoadHolderHoverDetector()
        {
            if (this.holderHoverDetector != null) return;
            this.holderHoverDetector = Object.FindFirstObjectByType<CardHolderHoverDetector>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadHolderHoverDetector", this.gameObject);
        }

        protected virtual void LoadAlphaFrontLineHolders()
        {
            if (this.alphaFrontLineHolders != null && this.alphaFrontLineHolders.Length > 0) return;
            CardHolderCtrl[] all = Object.FindObjectsByType<CardHolderCtrl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<CardHolderCtrl> result = new List<CardHolderCtrl>();
            foreach (CardHolderCtrl h in all)
            {
                if (h.HolderOwner != Owner.alpha) continue;
                if (h.HolderLink != Link.front) continue;
                result.Add(h);
            }
            this.alphaFrontLineHolders = result.ToArray();
            Debug.LogWarning(this.transform.name + ": LoadAlphaFrontLineHolders", this.gameObject);
        }

        protected virtual void LoadArrowIndicator()
        {
            if (this.arrowIndicator != null) return;
            GameObject go = GameObject.Find("ArrowIndicatorCtrl");
            if (go == null) return;
            this.arrowIndicator = go.GetComponent<ArrowIndicatorCtrl>();
            Debug.LogWarning(this.transform.name + ": FindArrowIndicator", this.gameObject);
        }

        protected virtual void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = this.GetComponentInParent<BattleStateCtrl>(true);
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        protected virtual void LoadMarkIdlePosition()
        {
            if (this.markIdlePosition != null) return;
            if (this.deskPositions == null) return;
            this.markIdlePosition = this.deskPositions.AlphaTheSource;
            Debug.LogWarning(this.transform.name + ": LoadMarkIdlePosition", this.gameObject);
        }

        protected virtual void LoadMarkSelected()
        {
            if (this.markSelected != null) return;
            this.markSelected = this.transform.Find("MarkSelected")?.gameObject;
            Debug.LogWarning(this.transform.name + ": LoadMarkSelected", this.gameObject);
        }

        protected virtual void LoadDeskPositions()
        {
            if (this.deskPositions != null) return;
            this.deskPositions = Object.FindFirstObjectByType<DeskPositionCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadDeskPositions", this.gameObject);
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable() => this.Subscribe();
        private void OnDisable() => this.Unsubscribe();
        private void Update()
        {
            this.CheckClick();
            this.UpdateMarkSelected();
            this.UpdateArrow();
        }

        // ─── Mark selected ────────────────────────────────────────────────────────

        private void UpdateMarkSelected()
        {
            if (this.markSelected == null) return;
            if (this.selected == null || !this.IsLocationFlippable(this.selected.Location))
            {
                this.HideMarkSelected();
                return;
            }
            if (CardMovement.IsAnyCardMoving)
            {
                this.HideMarkSelected();
                return;
            }
            if (this.fullDetail)
            {
                this.HideMarkSelected();
                return;
            }
            this.SnapToSelectedCard();
            this.ShowMarkSelected();
        }

        private void ShowMarkSelected()
        {
            if (this.markSelected.activeSelf) return;
            this.markSelected.SetActive(true);
        }

        private void HideMarkSelected()
        {
            if (!this.markSelected.activeSelf) return;
            this.markSelected.SetActive(false);
        }

        private void FollowIdlePosition()
        {
            if (this.markIdlePosition == null) return;
            this.markSelected.transform.position = Vector3.Lerp(
                this.markSelected.transform.position,
                this.markIdlePosition.position,
                this.markFollowSpeed * Time.deltaTime);
        }

        private void SnapToSelectedCard()
        {
            this.markSelected.transform.position = this.selected.transform.position;
        }

        // ─── Click detection ──────────────────────────────────────────────────────

        private void CheckClick()
        {
            if (ModalDimLayer.IsInputBlocked) return;
            if (this.IsBattleCompleted())
            {
                if (this.IsMouseClickedThisFrame()) { if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Cannot click: Battle is completed."); }
                this.ClearInteractionState();
                return;
            }
            if (this.fullDetail)
            {
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                {
                    this.ExitFullDetail();
                    return;
                }
                this.HandleFullDetailClick();
                return;
            }
            if (CardMovement.IsAnyCardMoving)
            {
                this.HandleCardSelectionOnly();
                return;
            }
            this.HandleLeftClick();
            this.HandleMiddleClick();
            this.HandleRightClick();
        }

        private void HandleCardSelectionOnly()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            this.HandleCardClick();
        }

        private void HandleFullDetailClick()
        {
            // Middle click cancels the preview. Right drag and mouse-wheel input
            // are consumed by CardFullDetailManipulator.
            if (!this.IsMouseClickedThisFrame() && !this.IsMouseMiddleClickedThisFrame()) return;
            this.ExitFullDetail();
        }

        private void HandleLeftClick()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Left click detected in HandleLeftClick!");

            Card3DCtrl previousSelected = this.selected;
            bool wasSelectedInHand = previousSelected != null && previousSelected.Location == Location.in_hand;

            this.HandleCardClick();
            this.HandleHolderClick();

            if (wasSelectedInHand)
            {
                // If the selected card is still selected and still in hand, but the click was not on the card itself, deselect it.
                if (this.selected == previousSelected && this.selected.Location == Location.in_hand)
                {
                    if (this.hovered != previousSelected)
                    {
                        this.ClearInteractionState();
                    }
                }
            }
        }

        private void HandleMiddleClick()
        {
            if (!this.IsMouseMiddleClickedThisFrame()) return;
            if (this.AreClientActionsPending()) return;
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
            if (!this.IsClickOnSelected()) this.SelectHovered();
            this.EnterFullDetail();
        }

        private void HandleRightClick()
        {
            if (!this.IsMouseRightClickedThisFrame()) return;
            if (this.TryConfirmTargeting()) return;
            this.HandleSelectedToggleFace();
        }

        private bool IsMouseClickedThisFrame()
        {
            if (Mouse.current == null) 
            {
                // Commented out to avoid spam, but if we need it we can log here
                return false;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Mouse leftButton.wasPressedThisFrame is TRUE!");
                return true;
            }
            return false;
        }

        private bool IsMouseRightClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.rightButton.wasPressedThisFrame;
        }

        private bool IsMouseMiddleClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.middleButton.wasPressedThisFrame;
        }

        private void HandleCardClick()
        {
            if (this.AreClientActionsPending()) { if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Cannot click: Actions pending"); return; }
            if (this.hovered == null) { if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Cannot click: Hovered card is null"); return; }
            if (this.IsLocationNonSelectable(this.hovered.Location)) { if (this.debugMouseEvents) Debug.LogWarning($"[CardSelection] Cannot click: Location {this.hovered.Location} non-selectable"); return; }
            
            if (this.IsClickOnSelected()) 
            {
                if (this.IsTargeting && this.targetingSource == this.selected)
                {
                    this.CancelTargeting();
                    return;
                }

                if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Re-selecting already selected card"); 
            }
            else
            {
                if (this.debugMouseEvents) Debug.LogWarning($"[CardSelection] Selecting hovered card: {this.hovered.name}");
            }
            
            this.SelectHovered();
        }

        private void HandleHolderClick()
        {
            CardHolderCtrl clickedHolder = this.holderHoverDetector != null
                ? this.holderHoverDetector.GetHolderUnderPointer()
                : this.holderHover;
            if (clickedHolder == null) return;
            clickedHolder.NotifySelected();
        }

        private bool IsClickOnSelected()
        {
            return this.hovered == this.selected && this.selected != null;
        }

        private void SelectHovered()
        {
            this.fullDetail = false;
            this.selected = this.hovered;
            this.selected.NotifySelected();
            this.EvaluateTargetingStart();
        }

        private void EnterFullDetail()
        {
            if (this.deskPositions == null) return;
            this.fullDetail = true;
            this.selected.SetFullDetailMode(true);
            this.arrowIndicator?.Hide();
            OnFullDetailEntered?.Invoke();
            this.selected.MoveToFullDetail(this.deskPositions.FullDetailPoint);
        }

        private void ExitFullDetail()
        {
            this.fullDetail = false;
            this.selected?.SetFullDetailMode(false);
            if (this.IsTargeting)
                this.arrowIndicator?.Show(this.targetingSource.transform.position, this.targetingSource.transform.position);
            OnFullDetailExited?.Invoke();
            this.selected?.ReturnFromFullDetail();
        }

        /// <summary>
        /// Closes the full-detail view, returning its card so callers can wait for
        /// the return animation before starting another board animation.
        /// </summary>
        public Card3DCtrl ReturnFullDetailCard()
        {
            if (!this.fullDetail) return null;

            Card3DCtrl fullDetailCard = this.selected;
            this.ExitFullDetail();
            return fullDetailCard;
        }

        private bool IsLocationNonSelectable(Location location)
        {
            return location == Location.in_source || location == Location.in_void;
        }

        private void HandleSelectedToggleFace()
        {
            if (this.selected == null) return;
            if (this.hovered != this.selected) return;
            if (!this.IsLocationFlippable(this.selected.Location)) return;
            if (this.selected.Expose && this.selected.FaceState == FaceState.FaceUp) return;

            if (this.selected.FaceState == FaceState.FaceUp)
            {
                this.selected.FaceDown();
                return;
            }

            if (!this.IsAlphaTurn()) return;
            if (this.selected.CardOwner != Owner.alpha) return;

            BattleScripts scripts = this.battleStateCtrl?.BattleScripts;
            if (scripts == null || scripts.IsRunning) return;

            Card3DCtrl card = this.selected;
            card.FaceUp();
            scripts.RunAlphaCardDeploy(
                response => this.OnFaceUpDeploySuccess(response, card),
                error => this.OnFaceUpDeployError(error, card));
        }

        private void OnFaceUpDeploySuccess(string response, Card3DCtrl card)
        {
            if (!this.IsDeployResponseSuccessful(response, out string error))
            {
                this.OnFaceUpDeployError(error, card);
                return;
            }

            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void OnFaceUpDeployError(string error, Card3DCtrl card)
        {
            Debug.LogError($"[CardSelection] Face-up deploy was rejected; returning card to face-down. {error}");
            if (card == null || card.FaceState != FaceState.FaceUp) return;
            card.FaceDown();
        }

        private bool IsLocationFlippable(Location location)
        {
            return location == Location.in_front || location == Location.in_back;
        }



        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited += this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered += this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited += this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected += this.OnHolderSelected;
            BattleState.OnNextMoveChanged += this.OnNextMoveChanged;
            Card3DCtrl.LocationChanged += this.OnLocationChanged;
            Card3DCtrl.TriggerStateChanged += this.OnTriggerStateChanged;
        }

        private void Unsubscribe()
        {
            this.ClearHealthPreviewTarget();
            Card3DCtrl.HoverEntered -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited -= this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered -= this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited -= this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected -= this.OnHolderSelected;
            BattleState.OnNextMoveChanged -= this.OnNextMoveChanged;
            Card3DCtrl.LocationChanged -= this.OnLocationChanged;
            Card3DCtrl.TriggerStateChanged -= this.OnTriggerStateChanged;
        }

        private void OnNextMoveChanged(NextMoveType nextMove)
        {
            this.ClearInteractionState();
        }

        private void OnLocationChanged(Card3DCtrl card, Location newLocation)
        {
            if (this.selected == card && newLocation == Location.in_void)
            {
                this.ClearInteractionState();
            }
        }

        private void OnTriggerStateChanged(Card3DCtrl card, bool isTrigger)
        {
            if (this.selected == card && isTrigger)
            {
                this.ClearInteractionState();
            }
        }

        // ─── Card hover handlers ──────────────────────────────────────────────────

        private void OnCardHoverEntered(Card3DCtrl card)
        {
            this.hovered = card;
            this.RefreshHealthPreviewTarget(card);
        }

        private void OnCardHoverExited(Card3DCtrl card)
        {
            this.ClearHealthPreviewTarget(card);
            this.ClearHoveredIfMatch(card);
        }

        private void RefreshHealthPreviewTarget(Card3DCtrl card)
        {
            this.ClearHealthPreviewTarget();
            if (!this.CanPreviewDamageOn(card)) return;

            int attack = this.targetingSource.GetDamagePreviewAttack();
            if (attack <= 0) return;

            this.healthPreviewTarget = card;
            this.healthPreviewTarget.SetHealthPreview(attack);
        }

        private bool CanPreviewDamageOn(Card3DCtrl card)
        {
            if (!this.IsTargeting || card == null || card == this.targetingSource) return false;
            if (this.targetingSource.CardOwner != Owner.alpha) return false;
            if (!card.IsCharacter()) return false;

            if (card.CardOwner == Owner.omega) return true;

            return card.CardOwner == this.targetingSource.CardOwner
                && this.targetingSource.CardType == CardType.ability
                && this.targetingSource.Definition?.GetBaseStatInt("atk") > 0;
        }

        private void ClearHealthPreviewTarget(Card3DCtrl card = null)
        {
            if (this.healthPreviewTarget == null) return;
            if (card != null && this.healthPreviewTarget != card) return;

            this.healthPreviewTarget.ClearHealthPreview();
            this.healthPreviewTarget = null;
        }

        private void ClearHoveredIfMatch(Card3DCtrl card)
        {
            if (this.hovered != card) return;
            this.hovered = null;
        }

        // ─── Holder hover/select handlers ────────────────────────────────────────

        private void OnHolderHoverEntered(CardHolderCtrl holder) => this.holderHover = holder;

        private void OnHolderHoverExited(CardHolderCtrl holder) => this.ClearHolderHoverIfMatch(holder);

        private void ClearHolderHoverIfMatch(CardHolderCtrl holder)
        {
            if (this.holderHover != holder) return;
            this.holderHover = null;
        }

        private void OnHolderSelected(CardHolderCtrl holder)
        {
            this.holderSelected = holder;
            if (this.IsBattleCompleted()) return;
            if (this.AreClientActionsPending()) return;
            if (this.selected == null) { if (this.debugLog) Debug.LogWarning("[CardSelection] OnHolderSelected — no card selected"); return; }
            if (this.debugLog) Debug.Log($"[CardSelection] OnHolderSelected — card='{this.selected.name}' owner={this.selected.CardOwner} isCharacter={this.selected.IsCharacter()} location={this.selected.Location} → holder='{holder.name}' link={holder.HolderLink} owner={holder.HolderOwner} heldCard={holder.HeldCard?.name ?? "null"}");
            if (!this.IsCardDeployPhase()) return;
            if (this.selected.Location != Location.in_hand) return;
            if (holder.HeldCard != null) { if (this.debugLog) Debug.LogWarning($"[CardSelection] OnHolderSelected — holder '{holder.name}' already has card '{holder.HeldCard.name}' — skipped"); return; }
            if (!this.IsPlacementValid(this.selected, holder)) return;
            BattleScripts scripts = this.battleStateCtrl?.BattleScripts;
            if (scripts == null || scripts.IsRunning) return;
            Card3DCtrl card = this.selected;
            int handSlotIndex = this.FindAlphaHandSlotIndex(card.InventoryItemId);
            if (handSlotIndex < 0) return;
            if (!this.UpdateLocalStateForDeployRequest(holder, card))
                Debug.LogWarning($"[CardSelection] Local deploy staging was skipped for " +
                    $"{holder.HolderLink}[{holder.Index}]; sending card_deploy from the selected card and holder.");
            this.PlaceFromHandIntoHolder(card, holder);
            this.RegisterPlayerDeploy(card, holder);
            this.TryIncrementCharDeploy(card);
            this.RunDeployScript(card, holder, handSlotIndex);
        }

        private void RunDeployScript(Card3DCtrl card, CardHolderCtrl holder, int handSlotIndex)
        {
            this.battleStateCtrl?.BattleScripts?.RunAlphaCardDeploy(
                card.InventoryItemId,
                holder.HolderLink,
                holder.Index,
                response => this.OnDeployScriptSuccess(response, card, holder, handSlotIndex),
                error => this.OnDeployScriptError(error, card, holder, handSlotIndex));
        }

        private void OnDeployScriptSuccess(string response, Card3DCtrl card, CardHolderCtrl holder, int handSlotIndex)
        {
            if (!this.IsDeployResponseSuccessful(response, out string error))
            {
                this.OnDeployScriptError(error, card, holder, handSlotIndex);
                return;
            }

            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private bool IsDeployResponseSuccessful(string response, out string error)
        {
            error = "Card deploy returned an invalid response.";
            if (string.IsNullOrWhiteSpace(response)) return false;

            BattleStatusScriptResponse parsed = JsonUtility.FromJson<BattleStatusScriptResponse>(response);
            if (parsed?.output == null) return false;
            if (string.IsNullOrEmpty(parsed.output.error)) return true;

            error = parsed.output.error;
            return false;
        }

        private void OnDeployScriptError(string error, Card3DCtrl card, CardHolderCtrl holder, int handSlotIndex)
        {
            Debug.LogError($"[CardSelection] Card deploy was rejected; returning card to hand. {error}");
            this.pendingPlayerDeploys.Remove(card.InventoryItemId);
            this.TryDecrementCharDeploy(card);
            this.battleStateCtrl?.BattleState?.RestoreCardFromLineToHand(
                card.InventoryItemId, holder.HolderLink, holder.Index, handSlotIndex);
            this.ReturnCardToHand(card, holder, handSlotIndex);
        }

        private void ReturnCardToHand(Card3DCtrl card, CardHolderCtrl lineHolder, int handSlotIndex)
        {
            if (card == null || this.deskPositions == null) return;

            Transform handTarget = this.deskPositions.GetAlphaHand(handSlotIndex);
            if (handTarget == null) return;

            if (lineHolder != null && lineHolder.HeldCard == card) lineHolder.SetCard(null);
            this.ApplyBattleMotionSettings(card);
            card.ReturnToHand(handTarget);
        }

        private int FindAlphaHandSlotIndex(string inventoryItemId)
        {
            BattleCardSlot[] hand = this.battleStateCtrl?.BattleState?.AlphaHand;
            if (hand == null) return -1;
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i]?.inventory_item_id == inventoryItemId) return i;
            }

            return -1;
        }

        private bool UpdateLocalStateForDeployRequest(CardHolderCtrl holder, Card3DCtrl card)
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            return this.battleStateCtrl.BattleState.MoveCardFromHandToLine(card.InventoryItemId, holder.HolderLink, holder.Index);
        }

        private void PlaceFromHandIntoHolder(Card3DCtrl card, CardHolderCtrl targetHolder)
        {
            if (card == null || targetHolder == null) return;
            Card3DCtrl cardToRotate = card;
            this.ApplyBattleMotionSettings(cardToRotate);
            cardToRotate.MoveToUnknow(targetHolder, () => this.StartCoroutine(this.RotateAfterArrival(cardToRotate, targetHolder)));
            targetHolder.SetCard(cardToRotate);
        }

        private IEnumerator RotateAfterArrival(Card3DCtrl card, CardHolderCtrl expectedHolder)
        {
            yield return new UnityEngine.WaitUntil(() => !card.IsFlipping);
            if (card.CardHolder != expectedHolder) yield break;
            card.RotateZ180();
        }



        private bool AreClientActionsPending()
        {
            return this.battleStateCtrl?.ClientActions?.HasPendingActions == true;
        }

        private bool IsBattleCompleted()
        {
            string battleStatus = this.battleStateCtrl?.BattleState?.BattleStatus;
            return string.Equals(battleStatus, "completed", System.StringComparison.OrdinalIgnoreCase);
        }

        private void ClearInteractionState()
        {
            this.ClearHealthPreviewTarget();
            this.fullDetail = false;
            Card3DCtrl prevSource = this.targetingSource;
            this.selected = null;
            this.targetingSource = null;
            this.holderSelected = null;
            this.arrowIndicator?.Hide();
            prevSource?.RefreshAtkUiVisibility();
            Card3DCtrl.NotifyDeselected();
        }



        private void ApplyBattleMotionSettings(Card3DCtrl card)
        {
            if (card == null) return;
            if (this.battleStateCtrl == null) return;
            card.SetMoveDuration(this.battleStateCtrl.CardMoveDuration);
            card.SetRotateDuration(this.battleStateCtrl.CardRotateDuration);
        }

        private Card3DCtrl FindFrontLineCharacter(Card3DCtrl excludeCard)
        {
            if (this.alphaFrontLineHolders == null) return null;
            foreach (CardHolderCtrl h in this.alphaFrontLineHolders)
            {
                if (h.HeldCard == null) continue;
                if (h.HeldCard == excludeCard) continue;
                if (string.IsNullOrEmpty(h.HeldCard.InventoryItemId)) continue;
                if (h.HeldCard.IsCharacter()) return h.HeldCard;
            }
            return null;
        }



    }
}

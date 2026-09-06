using System.Collections;
using System.Collections.Generic;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public partial class CardSelection
    {
        // ─── Targeting ────────────────────────────────────────────────────────────

        private bool TryBeginTargeting()
        {
            if (this.selected == null) return false;
            this.BeginTargeting();
            return true;
        }

        private bool TryCancelTargeting()
        {
            if (!this.IsTargeting) return false;
            this.CancelTargeting();
            return true;
        }

        private bool TryConfirmTargeting()
        {
            if (!this.IsTargeting) return false;
            if (this.hovered != null)
            {
                if (this.hovered == this.targetingSource) return false;
                this.ConfirmTargeting();
                return true;
            }
            if (this.holderHover == null) return false;
            if (this.holderHover.HolderOwner != Owner.omega && this.holderHover.HolderOwner != Owner.alpha) return false;
            this.ConfirmHolderTargeting();
            return true;
        }

        private void BeginTargeting()
        {
            this.ClearHealthPreviewTarget();
            this.targetingSource = this.selected;
            this.targeted = null;
            this.targetingSource?.SpawnAtkUi();
            if (this.IsBeginningAlphaAttack())
            {
                this.battleStateCtrl?.CardSpawning?.ClearCharacterHealthPreviews(Owner.omega);
            }
        }

        private bool IsBeginningAlphaAttack()
        {
            if (this.targetingSource == null || !this.targetingSource.IsCharacter()) return false;
            if (this.targetingSource.CardOwner != Owner.alpha) return false;

            NextMoveType nextMove = this.battleStateCtrl?.BattleState?.NextMove ?? NextMoveType.unknown;
            return nextMove == NextMoveType.alpha_turn || nextMove == NextMoveType.alpha_draw;
        }

        private void CancelTargeting()
        {
            this.ClearHealthPreviewTarget();
            Card3DCtrl prevSource = this.targetingSource;
            this.targetingSource = null;
            this.targeted = null;
            this.arrowIndicator?.Hide();
            prevSource?.RefreshAtkUiVisibility();
        }

        private void ConfirmTargeting()
        {
            Card3DCtrl source = this.targetingSource;
            Card3DCtrl target = this.hovered;
            this.CancelTargeting();
            this.LogTargetConfirmed(source, target);
            TargetSelected?.Invoke(source, target);
            this.DispatchAttackingScripts(source, target);
        }

        private void ConfirmHolderTargeting()
        {
            Card3DCtrl source = this.targetingSource;
            CardHolderCtrl holder = this.holderHover;
            if (source == null || holder == null) return;
            string defenderId = this.ResolveDefenderId(holder);
            this.CancelTargeting();
            Debug.Log($"<color=#00FFAA>[Targeting] <b>{source.name}</b> → <b>{holder.name}</b> ({defenderId})</color>");
            this.battleStateCtrl?.BattleScripts?.RunAlphaAttacking(
                source.InventoryItemId,
                defenderId,
                source.CodeName,
                "",
                this.OnAlphaAttackingSuccess,
                this.OnAlphaAttackingError);
        }

        private void DispatchAttackingScripts(Card3DCtrl source, Card3DCtrl target)
        {
            if (this.IsAlphaDrawPhase())
                this.RunAlphaCardDeployThenAttack(source, target);
            else
                this.battleStateCtrl?.BattleScripts?.RunAlphaAttacking(source.InventoryItemId, this.ResolveDefenderId(target), source.CodeName, target.CodeName, this.OnAlphaAttackingSuccess, this.OnAlphaAttackingError);
        }

        private bool IsAlphaDrawPhase()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            return this.battleStateCtrl.BattleState.NextMove == NextMoveType.alpha_draw;
        }

        private void RunAlphaCardDeployThenAttack(Card3DCtrl source, Card3DCtrl target)
        {
            this.battleStateCtrl?.BattleScripts?.RunAlphaCardDeploy(
                response => this.OnAlphaCardDeploySuccess(response, source, target), this.OnAlphaCardDeployError);
        }

        private void OnAlphaCardDeploySuccess(string response, Card3DCtrl source, Card3DCtrl target)
        {
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
            this.StartCoroutine(this.WaitActionsAndAttack(source, target));
        }

        private IEnumerator WaitActionsAndAttack(Card3DCtrl source, Card3DCtrl target)
        {
            yield return null;
            yield return this.WaitUntilActionsComplete();
            this.RunAlphaAttackingAfterDeploy(source, target);
        }

        private IEnumerator WaitUntilActionsComplete()
        {
            ClientActions clientActions = this.battleStateCtrl?.ClientActions;
            if (clientActions == null) yield break;
            yield return new WaitUntil(() => !clientActions.HasPendingActions);
        }

        private void RunAlphaAttackingAfterDeploy(Card3DCtrl source, Card3DCtrl target)
        {
            this.battleStateCtrl?.BattleScripts?.RunAlphaAttacking(
                source.InventoryItemId, this.ResolveDefenderId(target), source.CodeName, target.CodeName, this.OnAlphaAttackingSuccess, this.OnAlphaAttackingError);
        }

        private string ResolveDefenderId(Card3DCtrl target)
        {
            if (target.CardOwner == Owner.omega && target.Location == Location.in_void)   return "omega";
            if (target.CardOwner == Owner.omega && target.Location == Location.in_source) return "omega";
            if (target.CardOwner == Owner.alpha && target.Location == Location.in_void)   return "alpha";
            if (target.CardOwner == Owner.alpha && target.Location == Location.in_source) return "alpha";
            return target.InventoryItemId;
        }

        private string ResolveDefenderId(CardHolderCtrl holder)
        {
            if (holder == null) return "omega";
            if (holder.HolderOwner == Owner.alpha) return "alpha";
            return "omega";
        }

        private bool HasAnyOmegaFrontlineCard()
        {
            BattleCardSlot[] slots = this.battleStateCtrl?.BattleState?.OmegaFrontLine;
            if (slots == null) return false;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (!string.IsNullOrEmpty(slot.inventory_item_id)) return true;
            }
            return false;
        }

        private void OnAlphaAttackingSuccess(string response)
        {
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
            this.ClearInteractionState();
        }

        private void OnAlphaAttackingError(string error)
        {
            this.ClearInteractionState();
        }

        private void OnAlphaCardDeployError(string error)
        {
            this.ClearInteractionState();
        }

        private void LogTargetConfirmed(Card3DCtrl source, Card3DCtrl target)
        {
            Debug.Log($"<color=#00FFAA>[Targeting] <b>{source.name}</b> → <b>{target.name}</b></color>");
        }

        private void UpdateArrow()
        {
            if (this.IsBattleCompleted())
            {
                this.ClearInteractionState();
                this.arrowIndicator?.Hide();
                return;
            }
            
            if (!this.IsTargeting) return;
            if (this.arrowIndicator == null) return;
            if (this.fullDetail)
            {
                this.arrowIndicator.Hide();
                return;
            }
            if (!this.HasArrowTarget())
            {
                this.arrowIndicator.Hide();
                return;
            }
            Vector3 from = this.targetingSource.transform.position;
            Vector3 to = this.GetArrowTarget();
            this.arrowIndicator.Show(from, to);
            this.targetingSource?.SpawnAtkUi();
        }

        private bool HasArrowTarget()
        {
            if (this.hovered != null && this.hovered != this.targetingSource) return true;
            if (this.holderHover != null) return true;
            return false;
        }

        private void EvaluateTargetingStart()
        {
            this.TryCancelTargeting();

            if (this.selected == null) return;

            if (!this.IsAlphaTurn() && !this.IsAlphaDefendingBackLineSelected() && !this.IsAlphaDrawCharacterSelected() && !this.IsAlphaDrawBackLineSelected())
            {
                return;
            }
            if (this.selected.CardOwner == Owner.omega)
            {
                return;
            }
            if (this.IsSelectedCardTriggered())
            {
                return;
            }
            if (this.selected.Location == Location.in_hand)
            {
                return;
            }

            this.BeginTargeting();
        }

        private bool IsAlphaDefendingBackLineSelected()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            if (!this.battleStateCtrl.BattleState.AlphaDefending) return false;
            return this.selected.CardOwner == Owner.alpha && this.selected.Location == Location.in_back;
        }

        private bool IsAlphaDrawCharacterSelected()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            if (this.battleStateCtrl.BattleState.NextMove != NextMoveType.alpha_draw) return false;
            return this.selected.CardOwner == Owner.alpha && this.selected.IsCharacter();
        }

        private bool IsAlphaDrawBackLineSelected()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            if (this.battleStateCtrl.BattleState.NextMove != NextMoveType.alpha_draw) return false;
            return this.selected.CardOwner == Owner.alpha && this.selected.Location == Location.in_back;
        }

        private bool IsSelectedCardTriggered()
        {
            BattleCardSlot slot = this.FindSlotForCard(this.selected);
            if (slot == null) return false;
            return slot.trigger;
        }

        private BattleCardSlot FindSlotForCard(Card3DCtrl card)
        {
            BattleState state = this.battleStateCtrl?.BattleState;
            if (state == null) return null;
            return this.FindSlotInState(state, card.InventoryItemId);
        }

        private BattleCardSlot FindSlotInState(BattleState state, string inventoryItemId)
        {
            return this.FindInArray(state.AlphaHand,      inventoryItemId)
                ?? this.FindInArray(state.AlphaFrontLine, inventoryItemId)
                ?? this.FindInArray(state.AlphaBackLine,  inventoryItemId)
                ?? this.FindInArray(state.AlphaTheVoid,   inventoryItemId)
                ?? this.FindInArray(state.AlphaTheSource, inventoryItemId);
        }

        private BattleCardSlot FindInArray(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id == inventoryItemId) return slot;
            }
            return null;
        }

        private bool IsAlphaTurn()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            return this.battleStateCtrl.BattleState.NextMove == NextMoveType.alpha_turn;
        }

        private Vector3 GetArrowTarget()
        {
            if (this.hovered != null && this.hovered != this.targetingSource)
                return this.hovered.transform.position;
            if (this.holderHover != null)
                return this.holderHover.transform.position;
            return this.GetMouseWorldPosition();
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return this.targetingSource.transform.position;
            if (Mouse.current == null) return this.targetingSource.transform.position;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane plane = new Plane(Vector3.up, this.targetingSource.transform.position);
            if (!plane.Raycast(ray, out float distance)) return this.targetingSource.transform.position;
            return ray.GetPoint(distance);
        }

        // ─── Placement validation ─────────────────────────────────────────────────

        private bool IsCardDeployPhase()
        {
            if (this.battleStateCtrl?.BattleState == null) { if (this.debugLog) Debug.LogWarning("[CardSelection] IsCardDeployPhase — battleState is NULL"); return false; }
            NextMoveType nextMove = this.battleStateCtrl.BattleState.NextMove;
            bool valid = nextMove == NextMoveType.card_deploy || nextMove == NextMoveType.alpha_draw || nextMove == NextMoveType.alpha_turn;
            if (!valid && this.debugLog) Debug.LogWarning($"[CardSelection] IsCardDeployPhase — NextMove={nextMove} — not a deploy phase, skipped");
            return valid;
        }

        private bool IsPlacementValid(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card.CardOwner == Owner.omega) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — card '{card.name}' is omega-owned — skipped"); return false; }
            if (card.CardOwner != holder.HolderOwner) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — card owner={card.CardOwner} != holder owner={holder.HolderOwner} — skipped"); return false; }
            if (card.IsCharacter() && card.Location == Location.in_hand && this.countCharDeploy >= this.maxCharDeploy) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — char deploy limit reached ({this.countCharDeploy}/{this.maxCharDeploy}) — skipped"); return false; }
            if (card.IsCharacter() && holder.HolderLink != Link.front) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — character card must go to front, but holder link={holder.HolderLink} — skipped"); return false; }
            if (!card.IsCharacter() && holder.HolderLink != Link.back) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — non-character card must go to back, but holder link={holder.HolderLink} — skipped"); return false; }
            return true;
        }

        private void TryIncrementCharDeploy(Card3DCtrl card)
        {
            if (!card.IsCharacter()) return;
            this.countCharDeploy++;
            if (this.debugLog) Debug.Log($"[CardSelection] TryIncrementCharDeploy — countCharDeploy={this.countCharDeploy}/{this.maxCharDeploy}");
        }

        private void TryDecrementCharDeploy(Card3DCtrl card)
        {
            if (card == null || !card.IsCharacter()) return;
            this.countCharDeploy = Mathf.Max(0, this.countCharDeploy - 1);
            if (this.debugLog) Debug.Log($"[CardSelection] TryDecrementCharDeploy — countCharDeploy={this.countCharDeploy}/{this.maxCharDeploy}");
        }

        public void ResetCharDeployCount()
        {
            this.countCharDeploy = 0;
            if (this.debugLog) Debug.Log($"[CardSelection] ResetCharDeployCount — reset to 0 (max={this.maxCharDeploy})");
        }

        public bool TryConsumePlayerDeploy(string inventoryItemId, Link link, int slotIndex)
        {
            if (string.IsNullOrEmpty(inventoryItemId)) return false;
            if (!this.pendingPlayerDeploys.TryGetValue(inventoryItemId, out PlayerDeployRecord record)) return false;
            if (record.Link != link || record.SlotIndex != slotIndex) return false;
            this.pendingPlayerDeploys.Remove(inventoryItemId);
            if (this.debugLog) Debug.Log($"[CardSelection] Consumed local player deploy — id={inventoryItemId}, link={link}, slot={slotIndex}");
            return true;
        }

        private void RegisterPlayerDeploy(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card == null || holder == null) return;
            if (string.IsNullOrEmpty(card.InventoryItemId)) return;
            this.pendingPlayerDeploys[card.InventoryItemId] = new PlayerDeployRecord(holder.HolderLink, holder.Index);
            if (this.debugLog) Debug.Log($"[CardSelection] Registered local player deploy — id={card.InventoryItemId}, link={holder.HolderLink}, slot={holder.Index}");
        }

        private readonly struct PlayerDeployRecord
        {
            public PlayerDeployRecord(Link link, int slotIndex)
            {
                this.Link = link;
                this.SlotIndex = slotIndex;
            }

            public Link Link { get; }
            public int SlotIndex { get; }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/Desk/Card Spawning")]
    public partial class CardSpawning : SaiBehaviour
    {
        [SerializeField] private CardPool               cardPool;
        [SerializeField] private DeskPositionCtrl         deskPosition;
        [SerializeField] private BattleState              battleState;
        [SerializeField] private BattleCardDefinitions    battleCardDefinitions;
        [SerializeField] private string prefabName = "Card3D";
        [SerializeField] private int spawnPerFrame = 1;
        [SerializeField] private float spawnInterval = 0.05f;

        [Header("Source Stack")]
        [SerializeField] private float sourceStackOffsetY = 0.05f;

        [Header("Void Stack")]
        [SerializeField] private float voidDropHeight = 5f;
        [SerializeField, Range(0.1f, 1f)] private float sourceToVoidMoveDurationMultiplier = 0.4f;

        [Header("Line Transition")]
        [SerializeField] private float aboveLineHeight = 5f;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private Coroutine alphaSourceRoutine;
        private Coroutine omegaSourceRoutine;
        private readonly Dictionary<string, Card3DCtrl>    sourceCardRegistry  = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<string, Card3DCtrl>    handCardRegistry    = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<Transform, Card3DCtrl> slotOccupancy       = new Dictionary<Transform, Card3DCtrl>();
        private readonly Dictionary<Transform, Card3DCtrl> pendingSlotAttackers = new Dictionary<Transform, Card3DCtrl>();
        private readonly LinkedList<Card3DCtrl>             omegaSourceCardQueue = new LinkedList<Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  omegaHandCardQueue   = new Queue<Card3DCtrl>();
        private readonly LinkedList<Card3DCtrl>             alphaSourceCardQueue = new LinkedList<Card3DCtrl>();
        private readonly List<Card3DCtrl>                   alphaVoidCardList    = new List<Card3DCtrl>();
        private readonly List<Card3DCtrl>                   omegaVoidCardList    = new List<Card3DCtrl>();
        private int omegaSourceSpawnedCount = 0;
        private int alphaSourceSpawnedCount = 0;
        private int alphaVoidSpawnedCount = 0;
        private int omegaVoidSpawnedCount = 0;

        // ─── Public API ───────────────────────────────────────────────────────────

        public float ActionMoveDuration   { get; set; } = 1f;
        public float ActionRotateDuration { get; set; } = 0.4f;

        /// <summary>Spawns HP bars for all eligible character cards after resume playback finishes.</summary>
        public void SpawnHpBarsAfterResume()
        {
            Card3DCtrl[] cards = FindObjectsByType<Card3DCtrl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                card.RefreshHpBarAfterResume();
            }
        }

        /// <summary>Refreshes every active character HP bar from the resolved end-turn battle state.</summary>
        public void RefreshHpBarsAfterTurnEnd()
        {
            Card3DCtrl[] cards = FindObjectsByType<Card3DCtrl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                if (!card.IsCharacter()) continue;
                card.RefreshHpBarAfterTurnEnd();
            }
        }

        /// <summary>Clears stale damage previews and refreshes HP bars when Omega ends its turn.</summary>
        public void RefreshHpBarsAfterOmegaTurnEnd()
        {
            this.pendingSlotAttackers.Clear();
            Card3DCtrl[] cards = FindObjectsByType<Card3DCtrl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                if (!card.IsCharacter()) continue;
                card.SetAttacker(null);
                card.RefreshHpBarAfterTurnEnd();
            }
        }

        /// <summary>Clears pending damage previews from active character cards owned by one side.</summary>
        public void ClearCharacterHealthPreviews(Owner owner)
        {
            Card3DCtrl[] cards = FindObjectsByType<Card3DCtrl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                if (!card.IsCharacter() || card.CardOwner != owner) continue;
                card.ClearHealthPreview();
            }
        }

        public void RefreshHpBarsForTurnChange()
        {
            Card3DCtrl[] cards = FindObjectsByType<Card3DCtrl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                card.RefreshHpBarAfterTurnChange();
            }
        }

        public Coroutine SpawnAlphaSourceCards(int count)
        {
            if (this.alphaSourceRoutine != null) this.StopCoroutine(this.alphaSourceRoutine);
            this.alphaSourceRoutine = this.StartCoroutine(this.SpawnAlphaSourceCardsRoutine(count));
            return this.alphaSourceRoutine;
        }

        public Coroutine SpawnOmegaSourceCards(int count)
        {
            if (this.omegaSourceRoutine != null) this.StopCoroutine(this.omegaSourceRoutine);
            this.omegaSourceRoutine = this.StartCoroutine(this.SpawnOmegaSourceCardsRoutine(count));
            return this.omegaSourceRoutine;
        }

        public Card3DCtrl SetAlphaSourceCardData(string inventoryItemId)
        {
            Card3DCtrl card;
            if (this.alphaSourceCardQueue.Count > 0)
            {
                card = this.alphaSourceCardQueue.Last.Value;
                this.alphaSourceCardQueue.RemoveLast();
            }
            else
            {
                Card3DCtrl prefab = this.ResolvePrefab();
                card = this.SpawnCardAt(prefab, this.deskPosition.AlphaTheSource);
            }
            if (card == null) return null;
            card.SetOwner(Owner.alpha);
            card.SetInventoryItemId(inventoryItemId);
            BattleCardSlot slot = this.FindAlphaSlotById(inventoryItemId);
            if (slot == null) return card;
            string code = slot.item_definition_code_name;
            card.SetCodeName(code);
            CardDefinitionData alphaDef = this.battleCardDefinitions?.GetDefinitionByCode(code);
            this.ApplyCardFallbacks(card, slot.item_definition_name, alphaDef);
            card.LoadCardByCodeName(code);
            card.SetDefinition(alphaDef);
            return card;
        }

        public void CommitAlphaSourceToHand(Card3DCtrl card, string inventoryItemId, int slotIndex)
        {
            if (card == null) { if (this.debugLog) Debug.LogWarning("[CommitAlphaSourceToHand] card is NULL — skipped"); return; }
            Transform target = this.ResolveHandTarget(slotIndex, this.deskPosition.AlphaHand);
            if (target == null) { if (this.debugLog) Debug.LogWarning($"[CommitAlphaSourceToHand] no hand target for slot {slotIndex} — skipped"); return; }
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            if (this.debugLog) Debug.Log($"[CommitAlphaSourceToHand] MoveAndRotate {card.name} → '{target.name}', moveDuration={this.ActionMoveDuration}");
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            this.handCardRegistry[inventoryItemId] = card;
        }

        public BattleCardSlot FindAlphaSlotById(string inventoryItemId)
        {
            return this.FindSlotById(this.battleState?.AlphaHand, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaFrontLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaBackLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaTheVoid, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaTheSource, inventoryItemId);
        }

        public BattleCardSlot FindOmegaSlotById(string inventoryItemId)
        {
            return this.FindSlotById(this.battleState?.OmegaHand, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.OmegaFrontLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.OmegaBackLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.OmegaTheVoid, inventoryItemId);
        }

        public Card3DCtrl MoveOmegaSourceToHand(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) return null;

            // Try to find the exact slot index from BattleState using the item ID
            BattleCardSlot slot = this.FindOmegaSlotById(inventoryItemId);
            if (slot != null && slot.slot_index >= 0)
            {
                slotIndex = slot.slot_index;
            }

            Transform target = this.ResolveHandTarget(slotIndex, this.deskPosition.OmegaHand);
            if (target == null) return null;
            
            // If the exact slot is somehow still occupied, fallback to prevent missing
            if (this.IsSlotOccupied(target))
            {
                target = this.FindFirstEmptyHandSlot(this.deskPosition.OmegaHand);
                if (target == null) return null;
            }
            Card3DCtrl card = this.DequeueOmegaSourceCard(prefab);
            if (card == null) return null;
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            this.omegaHandCardQueue.Enqueue(card);
            return card;
        }

        public Card3DCtrl PeekOmegaHandCard()
            => this.omegaHandCardQueue.Count > 0 ? this.omegaHandCardQueue.Peek() : null;

        public Card3DCtrl MoveAlphaHandToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveAlphaHandToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaFrontLine);

        public Card3DCtrl MoveAlphaHandToBackLine(string inventoryItemId, int slotIndex)
            => this.MoveAlphaHandToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaBackLine);

        private Card3DCtrl MoveAlphaHandToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (!this.handCardRegistry.TryGetValue(inventoryItemId, out Card3DCtrl card)) return null;
            if (card.Location == Location.in_front || card.Location == Location.in_back) return null;
            if (slotIndex < 0 || slotIndex >= holders.Length) return null;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return null;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return null;
            this.handCardRegistry.Remove(inventoryItemId);
            this.RemoveFromSlotOccupancy(card);
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            Vector3 abovePos = holder.transform.position + Vector3.up * this.aboveLineHeight;
            card.MoveTo(abovePos, holder.HolderLocation);
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
            this.ApplyPendingAttacker(holder, card);
            return card;
        }

        public void SettleAlphaHandInFrontLine(Card3DCtrl card, string inventoryItemId, int slotIndex)
            => this.SettleAlphaHandInLine(card, inventoryItemId, slotIndex, this.deskPosition.AlphaFrontLine);

        public void SettleAlphaHandInBackLine(Card3DCtrl card, string inventoryItemId, int slotIndex)
            => this.SettleAlphaHandInLine(card, inventoryItemId, slotIndex, this.deskPosition.AlphaBackLine);

        private void SettleAlphaHandInLine(Card3DCtrl card, string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (slotIndex < 0 || slotIndex >= holders.Length) return;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return;
            BattleCardSlot slot = this.FindAlphaSlotById(inventoryItemId);
            if (slot != null)
            {
                card.SetExpose(slot.expose);
                card.SetIsTrigger(slot.trigger);
            }
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveToUnknow(holder, slot != null ? () => this.ApplyAlphaFaceState(card, slot) : null);
        }

        public Card3DCtrl MoveOmegaHandToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveOmegaHandToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaFrontLine);

        public Card3DCtrl MoveOmegaHandToBackLine(string inventoryItemId, int slotIndex)
            => this.MoveOmegaHandToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaBackLine);

        private Card3DCtrl MoveOmegaHandToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (this.omegaHandCardQueue.Count == 0) return null;
            if (slotIndex < 0 || slotIndex >= holders.Length) return null;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return null;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return null;
            Card3DCtrl card = this.omegaHandCardQueue.Dequeue();
            this.RemoveFromSlotOccupancy(card);
            card.SetOwner(Owner.omega);
            card.SetInventoryItemId(inventoryItemId);
            BattleCardSlot slot = this.FindOmegaSlotById(inventoryItemId);
            if (slot != null)
            {
                string code = slot.item_definition_code_name;
                card.SetCodeName(code);
                CardDefinitionData omegaDef = this.battleCardDefinitions?.GetDefinitionByCode(code);
                this.ApplyCardFallbacks(card, slot.item_definition_name, omegaDef);
                card.LoadCardByCodeName(code);
                card.SetDefinition(omegaDef);
                card.SetExpose(slot.expose);
                card.SetIsTrigger(slot.trigger);
            }
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            Vector3 abovePos = holder.transform.position + Vector3.up * this.aboveLineHeight;
            card.MoveTo(abovePos, holder.HolderLocation);
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
            this.ApplyPendingAttacker(holder, card);
            return card;
        }

        public void SettleOmegaHandInFrontLine(Card3DCtrl card, string inventoryItemId, int slotIndex)
            => this.SettleOmegaHandInLine(card, inventoryItemId, slotIndex, this.deskPosition.OmegaFrontLine);

        public void SettleOmegaHandInBackLine(Card3DCtrl card, string inventoryItemId, int slotIndex)
            => this.SettleOmegaHandInLine(card, inventoryItemId, slotIndex, this.deskPosition.OmegaBackLine);

        private void SettleOmegaHandInLine(Card3DCtrl card, string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (slotIndex < 0 || slotIndex >= holders.Length) return;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return;
            BattleCardSlot slot = this.FindOmegaSlotById(inventoryItemId);
            if (slot != null)
            {
                card.SetExpose(slot.expose);
                card.SetIsTrigger(slot.trigger);
            }
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveToUnknow(holder, slot != null ? () => this.ApplyFaceState(card, slot) : null);
        }

        private BattleCardSlot FindSlotById(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id == inventoryItemId) return slot;
            }
            return null;
        }

        /// <summary>
        /// Rebuilds all battle-line holder bindings from the latest authoritative
        /// BattleState snapshot. Client actions remain responsible for animation;
        /// this method repairs any state/view drift after their queue is drained.
        /// </summary>
        public bool ReconcileLineBindingsFromBattleState()
        {
            if (this.battleState == null || this.deskPosition == null) return false;

            Dictionary<string, Card3DCtrl> cardsById = this.FindActiveCardsByInventoryId();
            this.ClearIncorrectLineBindings(
                this.deskPosition.AlphaFrontLine, this.battleState.AlphaFrontLine);
            this.ClearIncorrectLineBindings(
                this.deskPosition.AlphaBackLine, this.battleState.AlphaBackLine);
            this.ClearIncorrectLineBindings(
                this.deskPosition.OmegaFrontLine, this.battleState.OmegaFrontLine);
            this.ClearIncorrectLineBindings(
                this.deskPosition.OmegaBackLine, this.battleState.OmegaBackLine);

            bool reconciled = true;
            reconciled &= this.BindSnapshotLine(
                this.deskPosition.AlphaFrontLine, this.battleState.AlphaFrontLine, Owner.alpha, cardsById);
            reconciled &= this.BindSnapshotLine(
                this.deskPosition.AlphaBackLine, this.battleState.AlphaBackLine, Owner.alpha, cardsById);
            reconciled &= this.BindSnapshotLine(
                this.deskPosition.OmegaFrontLine, this.battleState.OmegaFrontLine, Owner.omega, cardsById);
            reconciled &= this.BindSnapshotLine(
                this.deskPosition.OmegaBackLine, this.battleState.OmegaBackLine, Owner.omega, cardsById);
            this.SyncAttackersFromBattleState();
            return reconciled;
        }

        /// <summary>
        /// Restores planned Omega attacker links from the authoritative battle
        /// snapshot. This is required after Resume, where the final board may be
        /// reconstructed without relying on a planning animation action.
        /// </summary>
        public void SyncAttackersFromBattleState()
        {
            this.pendingSlotAttackers.Clear();
            Card3DCtrl[] cards = Object.FindObjectsByType<Card3DCtrl>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                card.SetAttacker(null);
            }

            BattlePlanningEntry[] plans = this.battleState?.OmegaPlanning;
            if (plans == null) return;
            foreach (BattlePlanningEntry plan in plans)
            {
                if (plan == null || plan.action != "card_attack_card") continue;
                if (string.IsNullOrEmpty(plan.attacker_inv_id) || string.IsNullOrEmpty(plan.defender_inv_id)) continue;

                Card3DCtrl attacker = this.FindCardById(plan.attacker_inv_id);
                Card3DCtrl defender = this.FindCardById(plan.defender_inv_id);
                if (attacker == null || defender == null) continue;
                defender.SetAttacker(attacker);
            }
        }

        private Dictionary<string, Card3DCtrl> FindActiveCardsByInventoryId()
        {
            Dictionary<string, Card3DCtrl> cardsById = new Dictionary<string, Card3DCtrl>();
            Card3DCtrl[] cards = Object.FindObjectsByType<Card3DCtrl>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Card3DCtrl card in cards)
            {
                if (card == null || string.IsNullOrEmpty(card.InventoryItemId)) continue;
                if (!cardsById.ContainsKey(card.InventoryItemId))
                    cardsById.Add(card.InventoryItemId, card);
            }
            return cardsById;
        }

        private void ClearIncorrectLineBindings(CardHolderCtrl[] holders, BattleCardSlot[] slots)
        {
            if (holders == null) return;
            for (int i = 0; i < holders.Length; i++)
            {
                CardHolderCtrl holder = holders[i];
                if (holder == null) continue;
                string expectedId = i < (slots?.Length ?? 0) ? slots[i]?.inventory_item_id : null;
                Card3DCtrl heldCard = holder.HeldCard;
                string actualId = heldCard?.InventoryItemId;
                if (string.Equals(expectedId ?? string.Empty, actualId ?? string.Empty,
                    System.StringComparison.Ordinal)) continue;

                this.slotOccupancy.Remove(holder.transform);
                this.PreserveAttackerForReplacement(heldCard, holder);
                holder.SetCard(null);
                if (heldCard != null && heldCard.CardHolder == holder)
                    heldCard.AssignCardHolder(null);
            }
        }

        private bool BindSnapshotLine(CardHolderCtrl[] holders, BattleCardSlot[] slots, Owner owner,
            Dictionary<string, Card3DCtrl> cardsById)
        {
            if (holders == null) return slots == null || slots.Length == 0;
            bool reconciled = true;
            for (int i = 0; i < holders.Length; i++)
            {
                CardHolderCtrl holder = holders[i];
                BattleCardSlot slot = i < (slots?.Length ?? 0) ? slots[i] : null;
                if (holder == null)
                {
                    if (!string.IsNullOrEmpty(slot?.inventory_item_id)) reconciled = false;
                    continue;
                }
                if (string.IsNullOrEmpty(slot?.inventory_item_id)) continue;

                if (!cardsById.TryGetValue(slot.inventory_item_id, out Card3DCtrl card))
                {
                    Card3DCtrl prefab = this.ResolvePrefab();
                    card = this.SpawnCardAt(prefab, holder.transform);
                    if (card == null)
                    {
                        Debug.LogError($"[CardSpawning] Cannot reconcile {owner} {holder.HolderLink}[{i}]: " +
                            $"card '{slot.inventory_item_id}' is missing and could not be spawned.", this.gameObject);
                        reconciled = false;
                        continue;
                    }
                    cardsById[slot.inventory_item_id] = card;
                }

                bool holderChanged = card.CardHolder != holder;
                this.RemoveCardFromNonLineRegistries(card, slot.inventory_item_id);
                if (holderChanged) this.RemoveFromSlotOccupancy(card);
                this.ApplySnapshotCardData(card, slot, owner);
                holder.SetCard(card);
                card.AssignCardHolder(holder);
                this.slotOccupancy[holder.transform] = card;
                this.ApplyPendingAttacker(holder, card);
                if (holderChanged || card.Location != holder.HolderLocation)
                {
                    card.SetMoveDuration(this.ActionMoveDuration);
                    card.SetRotateDuration(this.ActionRotateDuration);
                    card.MoveTo(holder.transform, holder.HolderLocation);
                }
            }
            return reconciled;
        }

        private void ApplySnapshotCardData(Card3DCtrl card, BattleCardSlot slot, Owner owner)
        {
            card.SetOwner(owner);
            card.SetInventoryItemId(slot.inventory_item_id);
            string code = slot.item_definition_code_name;
            if (!string.IsNullOrEmpty(code) && (card.CodeName != code || card.Definition == null))
            {
                card.SetCodeName(code);
                CardDefinitionData definition = this.battleCardDefinitions?.GetDefinitionByCode(code);
                this.ApplyCardFallbacks(card, slot.item_definition_name, definition);
                card.LoadCardByCodeName(code);
                card.SetDefinition(definition);
            }
            card.SetExpose(slot.expose);
            card.SetIsTrigger(slot.trigger);
            if ((slot.face_up || slot.expose) && card.FaceState != FaceState.FaceUp) card.FaceUp();
        }

        private void RemoveCardFromNonLineRegistries(Card3DCtrl card, string inventoryItemId)
        {
            this.handCardRegistry.Remove(inventoryItemId);
            this.sourceCardRegistry.Remove(inventoryItemId);
            this.alphaVoidCardList.Remove(card);
            this.omegaVoidCardList.Remove(card);
            this.alphaSourceCardQueue.Remove(card);
            this.omegaSourceCardQueue.Remove(card);
            this.RemoveFromOmegaHandQueue(card);
        }

        private void RemoveFromOmegaHandQueue(Card3DCtrl card)
        {
            if (card == null || this.omegaHandCardQueue.Count == 0) return;
            int count = this.omegaHandCardQueue.Count;
            for (int i = 0; i < count; i++)
            {
                Card3DCtrl queuedCard = this.omegaHandCardQueue.Dequeue();
                if (queuedCard != card) this.omegaHandCardQueue.Enqueue(queuedCard);
            }
        }

        public void ClearSourceRegistry()
        {
            this.sourceCardRegistry.Clear();
            this.handCardRegistry.Clear();
            this.omegaHandCardQueue.Clear();
            this.slotOccupancy.Clear();
            this.omegaSourceCardQueue.Clear();
            this.alphaSourceCardQueue.Clear();
            this.omegaSourceSpawnedCount = 0;
            this.alphaSourceSpawnedCount = 0;
            this.alphaVoidSpawnedCount = 0;
            this.omegaVoidSpawnedCount = 0;
        }

        public Card3DCtrl FindCardById(string inventoryItemId)
        {
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            if (this.handCardRegistry.TryGetValue(inventoryItemId, out Card3DCtrl handCard)) return handCard;
            if (this.sourceCardRegistry.TryGetValue(inventoryItemId, out Card3DCtrl sourceCard)) return sourceCard;
            foreach (Card3DCtrl card in this.slotOccupancy.Values)
            {
                if (card != null && card.InventoryItemId == inventoryItemId) return card;
            }
            foreach (Card3DCtrl card in this.alphaVoidCardList)
            {
                if (card != null && card.InventoryItemId == inventoryItemId) return card;
            }
            foreach (Card3DCtrl card in this.omegaVoidCardList)
            {
                if (card != null && card.InventoryItemId == inventoryItemId) return card;
            }
            return null;
        }

        /// <summary>
        /// Applies code name, fallback name, texture, and definition to an omega card
        /// found by inventoryItemId. Called before flipping the card face-up on expose.
        /// </summary>
        public void LoadOmegaCardData(string inventoryItemId)
        {
            Card3DCtrl card = this.FindCardById(inventoryItemId);
            if (card == null)
            {
                card = this.PrepareOmegaSourceCardForVoid(inventoryItemId);
            }
            if (card == null) return;
            this.ApplyOmegaCardData(card, inventoryItemId);
        }

        private Card3DCtrl PrepareOmegaSourceCardForVoid(string inventoryItemId)
        {
            Card3DCtrl card = this.FindCardById(inventoryItemId);
            if (card != null) return card;

            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) return null;
            card = this.DequeueOmegaSourceCard(prefab);
            if (card == null) return null;
            card.SetOwner(Owner.omega);
            card.SetInventoryItemId(inventoryItemId);
            this.sourceCardRegistry[inventoryItemId] = card;
            return card;
        }

        private void ApplyOmegaCardData(Card3DCtrl card, string inventoryItemId)
        {
            if (card == null) return;
            BattleCardSlot slot = this.FindOmegaSlotById(inventoryItemId);
            if (slot == null) return;
            card.SetExpose(slot.expose);
            string code = string.IsNullOrEmpty(slot.item_definition_code_name) ? card.CodeName : slot.item_definition_code_name;
            if (string.IsNullOrEmpty(code)) return;
            card.SetCodeName(code);
            CardDefinitionData omegaLoadDef = this.battleCardDefinitions?.GetDefinitionByCode(code);
            this.ApplyCardFallbacks(card, slot.item_definition_name, omegaLoadDef);
            card.SetDefinition(omegaLoadDef);
            card.LoadCardByCodeName(code);
            card.SetIsTrigger(slot.trigger);
        }

        public Card3DCtrl MoveAlphaCardToVoid(string inventoryItemId)
            => this.MoveCardToVoid(inventoryItemId, this.deskPosition.AlphaTheVoid);

        public Card3DCtrl MoveOmegaCardToVoid(string inventoryItemId)
        {
            this.PrepareOmegaSourceCardForVoid(inventoryItemId);
            return this.MoveCardToVoid(inventoryItemId, this.deskPosition.OmegaTheVoid);
        }

        public void SettleAlphaCardInVoid(Card3DCtrl card)
        {
            this.SettleCardInVoidAt(card, this.deskPosition.AlphaTheVoid, this.alphaVoidSpawnedCount);
            this.alphaVoidSpawnedCount++;
        }

        public void SettleOmegaCardInVoid(Card3DCtrl card)
        {
            this.SettleCardInVoidAt(card, this.deskPosition.OmegaTheVoid, this.omegaVoidSpawnedCount);
            this.omegaVoidSpawnedCount++;
        }

        private void SettleCardInVoidAt(Card3DCtrl card, Transform voidPoint, int stackCount)
        {
            Vector3 finalPos = voidPoint.position + Vector3.up * (stackCount * this.sourceStackOffsetY);
            card.SetMoveDuration(this.ActionMoveDuration * 0.4f);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveAndRotate(finalPos, voidPoint.rotation, Location.in_void);
        }

        public void RotateAlphaCardAtVoidTransit(Card3DCtrl card)
            => card.RotateTo(this.deskPosition.AlphaTheVoid.rotation);

        public void RotateOmegaCardAtVoidTransit(Card3DCtrl card)
            => card.RotateTo(this.deskPosition.OmegaTheVoid.rotation);

        private Card3DCtrl MoveCardToVoid(string inventoryItemId, Transform voidPoint)
        {
            Card3DCtrl card = this.FindCardById(inventoryItemId);
            if (card == null)
            {
                if (voidPoint == this.deskPosition.AlphaTheVoid)
                {
                    card = this.SetAlphaSourceCardData(inventoryItemId);
                }
            }
            if (card == null) return null;
            this.handCardRegistry.Remove(inventoryItemId);
            this.sourceCardRegistry.Remove(inventoryItemId);
            this.PreserveAttackerForReplacement(card, card.CardHolder);
            this.RemoveFromSlotOccupancy(card);
            float moveDuration = card.Location == Location.in_source
                ? this.ActionMoveDuration * this.sourceToVoidMoveDurationMultiplier
                : this.ActionMoveDuration;
            card.SetMoveDuration(moveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            Vector3 abovePos = voidPoint.position + Vector3.up * this.voidDropHeight;
            card.MoveAndRotate(abovePos, voidPoint.rotation, Location.in_void);

            if (voidPoint == this.deskPosition.AlphaTheVoid)
            {
                if (!this.alphaVoidCardList.Contains(card)) this.alphaVoidCardList.Add(card);
            }
            else if (voidPoint == this.deskPosition.OmegaTheVoid)
            {
                if (!this.omegaVoidCardList.Contains(card)) this.omegaVoidCardList.Add(card);
            }

            return card;
        }

        private void PreserveAttackerForReplacement(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card == null || holder == null || card.Attacker == null) return;
            this.pendingSlotAttackers[holder.transform] = card.Attacker;
            card.SetAttacker(null);
        }

        private void ApplyPendingAttacker(CardHolderCtrl holder, Card3DCtrl replacement)
        {
            if (holder == null || replacement == null) return;
            if (!this.pendingSlotAttackers.Remove(holder.transform, out Card3DCtrl attacker)) return;
            replacement.SetAttacker(attacker);
        }

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCardPool();
            this.LoadDeskPosition();
            this.LoadBattleState();
            this.LoadBattleCardDefinitions();
        }

        protected virtual void LoadCardPool()
        {
            if (this.cardPool != null) return;
            this.cardPool = GameObject.FindAnyObjectByType<CardPool>();
            Debug.LogWarning(this.transform.name + ": LoadCardPool", this.gameObject);
        }

        protected virtual void LoadDeskPosition()
        {
            if (this.deskPosition != null) return;
            this.deskPosition = GameObject.FindAnyObjectByType<DeskPositionCtrl>();
            Debug.LogWarning(this.transform.name + ": LoadDeskPosition", this.gameObject);
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleState = ctrl.BattleState;
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        protected virtual void LoadBattleCardDefinitions()
        {
            if (this.battleCardDefinitions != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleCardDefinitions = ctrl.BattleCardDefinitions;
            Debug.LogWarning(this.transform.name + ": LoadBattleCardDefinitions", this.gameObject);
        }

        private IEnumerator WaitForDefinitions()
        {
            if (this.battleCardDefinitions == null) yield break;
            if (this.battleCardDefinitions.IsLoaded) yield break;
            if (this.debugLog) Debug.Log("<color=#FFD700>[CardSpawning] Waiting for BattleCardDefinitions to load...</color>");
            bool loaded = false;
            BattleCardDefinitions.OnDefinitionsLoaded += SetLoaded;
            yield return new UnityEngine.WaitUntil(() => loaded);
            BattleCardDefinitions.OnDefinitionsLoaded -= SetLoaded;
            void SetLoaded() => loaded = true;
        }

        private bool IsSlotOccupied(Transform target)
        {
            if (!this.slotOccupancy.TryGetValue(target, out Card3DCtrl existing)) return false;
            if (!existing.gameObject.activeInHierarchy)
            {
                this.slotOccupancy.Remove(target);
                existing.CardHolder?.SetCard(null);
                existing.AssignCardHolder(null);
                return false;
            }
            return true;
        }

        private Transform ResolveHandTarget(int slotIndex, Transform[] handTargets)
        {
            if (slotIndex >= 0 && slotIndex < handTargets.Length)
                return handTargets[slotIndex];
            return this.FindFirstEmptyHandSlot(handTargets);
        }

        private Transform FindFirstEmptyHandSlot(Transform[] handTargets)
        {
            foreach (Transform slot in handTargets)
            {
                if (!this.IsSlotOccupied(slot)) return slot;
            }
            return null;
        }

        private IEnumerator SpawnAlphaSourceCardsRoutine(int count)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            int spawnedThisFrame = 0;
            while (this.alphaSourceSpawnedCount < count)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card == null) break;
                card.SetOwner(Owner.alpha);
                card.SetMoveDuration(this.ActionMoveDuration);
                card.SetRotateDuration(this.ActionRotateDuration);
                Vector3 alphaSourcePos = this.deskPosition.AlphaTheSource.position + Vector3.up * (this.alphaSourceSpawnedCount * this.sourceStackOffsetY);
                card.MoveAndRotate(alphaSourcePos, this.deskPosition.AlphaTheSource.rotation, Location.in_source);
                this.alphaSourceCardQueue.AddLast(card);
                this.alphaSourceSpawnedCount++;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
            this.alphaSourceRoutine = null;
        }

        private IEnumerator SpawnOmegaSourceCardsRoutine(int count)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            int spawnedThisFrame = 0;
            while (this.omegaSourceSpawnedCount < count)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card == null) break;
                card.SetOwner(Owner.omega);
                card.SetMoveDuration(this.ActionMoveDuration);
                card.SetRotateDuration(this.ActionRotateDuration);
                Vector3 omegaSourcePos = this.deskPosition.OmegaTheSource.position + Vector3.up * (this.omegaSourceSpawnedCount * this.sourceStackOffsetY);
                card.MoveAndRotate(omegaSourcePos, this.deskPosition.OmegaTheSource.rotation, Location.in_source);
                this.omegaSourceCardQueue.AddLast(card);
                this.omegaSourceSpawnedCount++;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
            this.omegaSourceRoutine = null;
        }

        private Card3DCtrl DequeueOmegaSourceCard(Card3DCtrl prefab)
        {
            if (this.omegaSourceCardQueue.Count > 0)
            {
                Card3DCtrl top = this.omegaSourceCardQueue.Last.Value;
                this.omegaSourceCardQueue.RemoveLast();
                return top;
            }
            return this.SpawnCardAt(prefab, this.deskPosition.OmegaTheSource);
        }

        public void DespawnTopAlphaSourceCard()
        {
            if (this.alphaSourceCardQueue.Count == 0) return;
            Card3DCtrl top = this.alphaSourceCardQueue.Last.Value;
            this.alphaSourceCardQueue.RemoveLast();
            top.Despawn?.DoDespawn();
        }

        public void DespawnTopOmegaSourceCard()
        {
            if (this.omegaSourceCardQueue.Count == 0) return;
            Card3DCtrl top = this.omegaSourceCardQueue.Last.Value;
            this.omegaSourceCardQueue.RemoveLast();
            top.Despawn?.DoDespawn();
        }

        public void ShakeAlphaSourceAndVoidCards()
        {
            foreach (Card3DCtrl card in this.alphaSourceCardQueue)
            {
                if (card != null) card.Damaged();
            }
            foreach (Card3DCtrl card in this.alphaVoidCardList)
            {
                if (card != null) card.Damaged();
            }
        }

        public void ShakeOmegaSourceAndVoidCards()
        {
            foreach (Card3DCtrl card in this.omegaSourceCardQueue)
            {
                if (card != null) card.Damaged();
            }
            foreach (Card3DCtrl card in this.omegaVoidCardList)
            {
                if (card != null) card.Damaged();
            }
        }

        private void RemoveFromSlotOccupancy(Card3DCtrl card)
        {
            Transform keyToRemove = null;
            foreach (KeyValuePair<Transform, Card3DCtrl> kvp in this.slotOccupancy)
            {
                if (kvp.Value != card) continue;
                keyToRemove = kvp.Key;
                break;
            }
            if (keyToRemove != null) this.slotOccupancy.Remove(keyToRemove);
            card.CardHolder?.SetCard(null);
            card.AssignCardHolder(null);
        }

        private void ApplyFaceState(Card3DCtrl card, BattleCardSlot slot)
        {
            if (!slot.face_up && !slot.expose) return;
            this.StartCoroutine(this.WaitForFlipThenFaceUp(card));
        }

        private IEnumerator WaitForFlipThenFaceUp(Card3DCtrl card)
        {
            yield return new UnityEngine.WaitUntil(() => !card.IsFlipping);
            card.FaceUp();
        }

        private void ApplyAlphaFaceState(Card3DCtrl card, BattleCardSlot slot)
        {
            this.StartCoroutine(this.RotateY180ThenFaceState(card, slot));
        }

        private IEnumerator RotateY180ThenFaceState(Card3DCtrl card, BattleCardSlot slot)
        {
            yield return new UnityEngine.WaitUntil(() => !card.IsFlipping);
            card.RotateZ180(null);
            yield return new UnityEngine.WaitUntil(() => !card.IsFlipping);
            if (!slot.face_up && !slot.expose) yield break;
            card.FaceUp();
        }

        private Card3DCtrl ResolvePrefab()
        {
            if (this.cardPool == null) return null;
            return this.cardPool.PoolPrefabs.GetByName(this.prefabName);
        }

        private YieldInstruction WaitAfterSpawn()
        {
            if (this.spawnInterval > 0f) return new WaitForSeconds(this.spawnInterval);
            return null;
        }

        private Card3DCtrl SpawnCardAt(Card3DCtrl prefab, Transform spawnPoint)
        {
            if (spawnPoint == null) return null;
            Card3DCtrl card = this.cardPool.Spawn(prefab, spawnPoint.position);
            card.transform.rotation = spawnPoint.rotation;
            card.gameObject.SetActive(true);
            return card;
        }

        // Applies fallback name, stats (ATK/DEF/Stars), and description from a card definition.
        private void ApplyCardFallbacks(Card3DCtrl card, string fallbackName, CardDefinitionData def)
        {
            string resolvedName = string.IsNullOrEmpty(fallbackName) ? def?.name : fallbackName;
            card.SetFallbackName(resolvedName);
            card.SetFallbackStats(this.ToCardBaseStats(def));
            card.SetFallbackDescription(CardDescriptionTemplateResolver.Resolve(def?.description, def));
        }

        private CardBaseStats ToCardBaseStats(CardDefinitionData definition)
        {
            if (definition == null) return null;
            return new CardBaseStats
            {
                atk = definition.GetBaseStatInt("atk"),
                def = definition.GetBaseStatInt("def"),
                star = definition.GetBaseStatInt("star")
            };
        }

    }
}

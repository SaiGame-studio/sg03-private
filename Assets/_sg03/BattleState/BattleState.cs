using System;
using SaiGame.Services;
using SG03;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>
    /// Singleton cache that stores the latest battle state.
    /// Any script that receives a battle_status response should call UpdateFromBattleStatus
    /// to keep this cache up to date. All data is exposed as readonly properties.
    /// </summary>
    public class BattleState : SaiBehaviour
    {

        [Header("References")]
        [SerializeField] private CardSpawning cardSpawning;

        [Header("Flags")]
        private bool gameStartFired;

        [SerializeField] private int turn;
        [SerializeField] private int action;
        [SerializeField] private int alphaHp;
        [SerializeField] private int omegaHp;
        [SerializeField] private int alphaTheSourceCount;
        [SerializeField] private int omegaTheSourceCount;
        [SerializeField] private int alphaTheVoidCount;
        [SerializeField] private int omegaTheVoidCount;
        [SerializeField] private string sessionId;
        [SerializeField] private string battleDifficulty;
        [SerializeField] private string battleStatus;
        [SerializeField] private bool alphaDefending;
        [SerializeField] private bool omegaDefending;
        [SerializeField] private bool isDevelopment;
        [SerializeField] private NextMoveType nextMove;
        [SerializeField] private BattleCardSlot[] alphaTheVoid;
        [SerializeField] private BattleCardSlot[] omegaTheVoid;
        [SerializeField] private BattleCardSlot[] alphaTheSource;
        [SerializeField] private BattleCardSlot[] omegaTheSource;
        [SerializeField] private BattleCardSlot[] alphaHand;
        [SerializeField] private BattleCardSlot[] alphaBackLine;
        [SerializeField] private BattleCardSlot[] alphaFrontLine;

        [SerializeField] private BattleCardSlot[] omegaHand;
        [SerializeField] private BattleCardSlot[] omegaFrontLine;
        [SerializeField] private BattleCardSlot[] omegaBackLine;
        [SerializeField] private BattlePlanningEntry[] omegaPlanning;
        [SerializeField] private string[] clientActions;
        [SerializeField] private string[] debugLog;

        [SerializeField][TextArea(5, 20)] private string battleStatusJson;
        [SerializeField][TextArea(5, 20)] private string metadataJson;

        public string BattleStatusJson => this.battleStatusJson;
        public string MetadataJson => this.metadataJson;
        public int Turn => this.turn;
        public int Action => this.action;
        public int AlphaHp => this.alphaHp;
        public int OmegaHp => this.omegaHp;
        public int AlphaTheSourceCount => this.alphaTheSourceCount;
        public int OmegaTheSourceCount => this.omegaTheSourceCount;
        public int AlphaTheVoidCount => this.alphaTheVoidCount;
        public int OmegaTheVoidCount => this.omegaTheVoidCount;
        public BattleCardSlot[] AlphaTheVoid => this.alphaTheVoid;
        public BattleCardSlot[] OmegaTheVoid => this.omegaTheVoid;
        public BattleCardSlot[] AlphaTheSource => this.alphaTheSource;
        public BattleCardSlot[] OmegaTheSource => this.omegaTheSource;
        public BattleCardSlot[] AlphaHand => this.alphaHand;
        public BattleCardSlot[] AlphaBackLine => this.alphaBackLine;
        public BattleCardSlot[] AlphaFrontLine => this.alphaFrontLine;
        public BattleCardSlot[] OmegaHand => this.omegaHand;
        public BattleCardSlot[] OmegaFrontLine => this.omegaFrontLine;
        public BattleCardSlot[] OmegaBackLine => this.omegaBackLine;
        public BattlePlanningEntry[] OmegaPlanning => this.omegaPlanning;
        public string SessionId => this.sessionId;
        public string BattleDifficulty => this.battleDifficulty;
        public string BattleStatus     => this.battleStatus;
        public NextMoveType NextMove => this.nextMove;
        public bool AlphaDefending => this.alphaDefending;
        public bool OmegaDefending => this.omegaDefending;
        public bool IsDevelopment => this.isDevelopment;
        public string[] ClientActions => this.clientActions;
        public string[] DebugLog => this.debugLog;

        public event Action OnBattleStatusChanged;
        public event Action<string[]> OnClientActionsChanged;
        public static event Action OnGameStart;
        public static event Action<NextMoveType> OnNextMoveChanged;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCardSpawning();
        }

        protected virtual void LoadCardSpawning()
        {
            if (this.cardSpawning != null) return;
            this.cardSpawning = GameObject.FindAnyObjectByType<CardSpawning>();
            Debug.LogWarning(this.transform.name + ": LoadCardSpawning", this.gameObject);
        }

        private void OnEnable() => this.SubscribeEvents();
        private void OnDisable() => this.UnsubscribeEvents();

        private void SubscribeEvents()
        {
            Card3DCtrl.FaceStateChanged += this.OnCardFaceStateChanged;
            Card3DCtrl.TriggerStateChanged += this.OnCardTriggerStateChanged;
        }

        private void UnsubscribeEvents()
        {
            Card3DCtrl.FaceStateChanged -= this.OnCardFaceStateChanged;
            Card3DCtrl.TriggerStateChanged -= this.OnCardTriggerStateChanged;
        }

        private void OnCardFaceStateChanged(Card3DCtrl card, bool faceUp)
        {
            this.UpdateSlotFaceUp(this.alphaFrontLine, card.InventoryItemId, faceUp);
            this.UpdateSlotFaceUp(this.alphaBackLine, card.InventoryItemId, faceUp);
        }

        private void UpdateSlotFaceUp(BattleCardSlot[] slots, string inventoryItemId, bool faceUp)
        {
            if (slots == null) return;
            if (string.IsNullOrEmpty(inventoryItemId)) return;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id != inventoryItemId) continue;
                slot.face_up = faceUp;
                return;
            }
        }

        private void OnCardTriggerStateChanged(Card3DCtrl card, bool isTrigger)
        {
            this.UpdateSlotTrigger(this.alphaFrontLine, card.InventoryItemId, isTrigger);
            this.UpdateSlotTrigger(this.alphaBackLine, card.InventoryItemId, isTrigger);
            this.UpdateSlotTrigger(this.omegaFrontLine, card.InventoryItemId, isTrigger);
            this.UpdateSlotTrigger(this.omegaBackLine, card.InventoryItemId, isTrigger);
        }

        private void UpdateSlotTrigger(BattleCardSlot[] slots, string inventoryItemId, bool isTrigger)
        {
            if (slots == null) return;
            if (string.IsNullOrEmpty(inventoryItemId)) return;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id != inventoryItemId) continue;
                slot.trigger = isTrigger;
                return;
            }
        }

        public void ClearData()
        {
            this.battleStatusJson = string.Empty;
            this.metadataJson = string.Empty;
            this.turn = 0;
            this.action = 0;
            this.alphaHp = 0;
            this.omegaHp = 0;
            this.alphaTheSourceCount = 0;
            this.omegaTheSourceCount = 0;
            this.alphaTheVoidCount = 0;
            this.omegaTheVoidCount = 0;
            this.alphaTheVoid = null;
            this.omegaTheVoid = null;
            this.alphaTheSource = null;
            this.omegaTheSource = null;
            this.alphaHand = null;
            this.alphaBackLine = null;
            this.alphaFrontLine = null;
            this.omegaHand = null;
            this.omegaFrontLine = null;
            this.omegaBackLine = null;
            this.omegaPlanning = null;
            this.sessionId = string.Empty;
            this.battleDifficulty = string.Empty;
            this.battleStatus = string.Empty;
            this.alphaDefending = false;
            this.omegaDefending = false;
            this.isDevelopment = false;
            this.debugLog = null;
            this.SetNextMove(string.Empty);
            this.gameStartFired = false;
            this.cardSpawning?.ClearSourceRegistry();
            this.OnBattleStatusChanged?.Invoke();
        }

        /// <summary>
        /// Called by any script that receives a raw battle_status JSON response.
        /// Stores the raw JSON and parses all fields into the cache.
        /// </summary>
        public void ShowMetadataJson(CardDefinitionMetadata metadata)
        {
            if (metadata == null)
            {
                this.metadataJson = string.Empty;
                return;
            }
            this.metadataJson = BeautifyJson(JsonUtility.ToJson(metadata));
        }

        public void UpdateFromBattleStatus(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return;
            this.battleStatusJson = BeautifyJson(rawJson);
            this.ParseAndApply(rawJson);
        }

        private void ParseAndApply(string rawJson)
        {
            BattleStatusScriptResponse response = JsonUtility.FromJson<BattleStatusScriptResponse>(rawJson);
            if (response?.output == null) { Debug.LogError("[BattleState] UpdateFromBattleStatus — failed to parse response"); return; }
            if (!string.IsNullOrEmpty(response.output.error)) { Debug.LogError($"[BattleState] Script error: {response.output.error}"); return; }
            if (response.output.alpha_hand == null) { Debug.LogError("[BattleState] UpdateFromBattleStatus — alpha_hand is missing from response"); return; }
            this.ApplyOutput(response.output);
        }

        private static string BeautifyJson(string json)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int indent = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 4));
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent--;
                        sb.Append(new string(' ', indent * 4));
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 4));
                        break;
                    case ':':
                        sb.Append(c);
                        sb.Append(' ');
                        break;
                    default:
                        if (!char.IsWhiteSpace(c))
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private void ApplyOutput(BattleStatusOutput output)
        {
            this.turn = output.turn;
            this.action = output.action;
            this.alphaHp = output.alpha_hp;
            this.omegaHp = output.omega_hp;
            this.alphaTheSourceCount = output.alpha_the_source_count;
            this.omegaTheSourceCount = output.omega_the_source_count;
            this.alphaTheVoidCount = output.alpha_the_void_count;
            this.omegaTheVoidCount = output.omega_the_void_count;
            this.alphaTheVoid = output.alpha_the_void;
            this.omegaTheVoid = output.omega_the_void;
            this.alphaTheSource = output.alpha_the_source;
            this.omegaTheSource = output.omega_the_source;
            this.alphaHand = output.alpha_hand;
            this.alphaBackLine = output.alpha_back_line;
            this.alphaFrontLine = output.alpha_front_line;
            if (output.omega_hand != null) this.omegaHand = output.omega_hand;
            if (output.omega_front_line != null) this.omegaFrontLine = output.omega_front_line;
            if (output.omega_back_line != null) this.omegaBackLine = output.omega_back_line;
            this.omegaPlanning = output.omega_planning;
            this.clientActions = output.client_actions;
            this.debugLog = output.debug_log;
            this.battleDifficulty = output.metadata?.battle_difficulty ?? output.battle_difficulty;
            this.battleStatus = output.status;
            this.alphaDefending = output.alpha_defending;
            this.omegaDefending = output.omega_defending;
            this.isDevelopment = output.is_development;
            this.SetNextMove(output.metadata?.next_move ?? output.next_move);
            this.metadataJson = BeautifyJson(JsonUtility.ToJson(output.metadata));
            this.TryFireGameStart();
            this.SyncTriggerStates();
            this.OnBattleStatusChanged?.Invoke();
            this.NotifyClientActions();
            this.ReconcileVisualStateWhenNoClientActions();
        }

        private void ReconcileVisualStateWhenNoClientActions()
        {
            if (this.clientActions != null && this.clientActions.Length > 0) return;
            this.cardSpawning?.ReconcileLineBindingsFromBattleState();
        }

        private void SyncTriggerStates()
        {
            if (this.cardSpawning == null) return;
            this.SyncTriggerStateForSlots(this.alphaFrontLine);
            this.SyncTriggerStateForSlots(this.alphaBackLine);
            this.SyncTriggerStateForSlots(this.omegaFrontLine);
            this.SyncTriggerStateForSlots(this.omegaBackLine);
        }

        private void SyncTriggerStateForSlots(BattleCardSlot[] slots)
        {
            if (slots == null) return;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null || string.IsNullOrEmpty(slot.inventory_item_id)) continue;
                Card3DCtrl card = this.cardSpawning.FindCardById(slot.inventory_item_id);
                if (card != null)
                {
                    card.SetIsTrigger(slot.trigger);
                }
            }
        }

        private void NotifyClientActions()
        {
            if (this.clientActions == null || this.clientActions.Length == 0) return;
            this.OnClientActionsChanged?.Invoke(this.clientActions);
        }

        private void TryFireGameStart()
        {
            if (this.gameStartFired) return;
            this.gameStartFired = true;
            Debug.Log($"<color=#00FF88><b>[BattleState] OnGameStart fired — turn={this.turn}, action={this.action}</b></color>");
            OnGameStart?.Invoke();
        }

        private void SetNextMove(string value)
        {
            NextMoveType parsed = ParseNextMove(value);
            if (this.nextMove == parsed) return;
            this.nextMove = parsed;
            OnNextMoveChanged?.Invoke(this.nextMove);
        }

        private static NextMoveType ParseNextMove(string value)
        {
            if (string.IsNullOrEmpty(value)) return NextMoveType.unknown;
            switch (value)
            {
                case "card_deploy": return NextMoveType.card_deploy;
                case "init_cards": return NextMoveType.init_cards;
                case "alpha_turn": return NextMoveType.alpha_turn;
                case "omega_turn": return NextMoveType.omega_turn;
                case "alpha_draw": return NextMoveType.alpha_draw;
                default: return NextMoveType.unknown;
            }
        }

        // ─── Local placement update ───────────────────────────────────────────────

        /// <summary>
        /// Updates alpha hand, front line, and back line when the player drags a card
        /// from hand to a front or back line holder on the board.
        /// </summary>
        public bool MoveCardFromHandToLine(string inventoryItemId, Link link, int slotIndex)
        {
            if (string.IsNullOrEmpty(inventoryItemId) || slotIndex < 0) return false;
            BattleCardSlot targetSlot = this.GetLineSlot(link, slotIndex);
            if (!this.IsSlotEmpty(targetSlot))
            {
                Debug.LogWarning($"[BattleState] Cannot stage deploy of '{inventoryItemId}': {link}[{slotIndex}] contains '{targetSlot.inventory_item_id}'.", this.gameObject);
                return false;
            }

            BattleCardSlot slot = this.RemoveFromHand(inventoryItemId);
            if (slot == null) return false;
            if (link == Link.front) this.InsertIntoFrontLine(slot, slotIndex);
            else this.InsertIntoBackLine(slot, slotIndex);
            this.OnBattleStatusChanged?.Invoke();
            return true;
        }

        /// <summary>Restores a locally staged deploy when the backend rejects it.</summary>
        public bool RestoreCardFromLineToHand(string inventoryItemId, Link link, int lineSlotIndex, int handSlotIndex)
        {
            if (string.IsNullOrEmpty(inventoryItemId) || lineSlotIndex < 0 || handSlotIndex < 0) return false;
            this.alphaHand = this.EnsureSlotCapacity(this.alphaHand, handSlotIndex);
            if (!this.IsSlotEmpty(this.alphaHand[handSlotIndex])) return false;

            BattleCardSlot slot = this.GetLineSlot(link, lineSlotIndex);
            if (slot == null || slot.inventory_item_id != inventoryItemId)
            {
                Debug.LogWarning($"[BattleState] Cannot roll back deploy of '{inventoryItemId}': {link}[{lineSlotIndex}] no longer contains that card.", this.gameObject);
                return false;
            }

            this.RemoveFromLine(link, lineSlotIndex);

            slot.slot_index = handSlotIndex;
            this.alphaHand[handSlotIndex] = slot;
            this.OnBattleStatusChanged?.Invoke();
            return true;
        }

        private bool IsSlotEmpty(BattleCardSlot slot)
        {
            return slot == null || string.IsNullOrEmpty(slot.inventory_item_id);
        }

        private BattleCardSlot RemoveFromHand(string inventoryItemId)
        {
            if (this.alphaHand == null) return null;
            for (int i = 0; i < this.alphaHand.Length; i++)
            {
                if (this.alphaHand[i]?.inventory_item_id != inventoryItemId) continue;
                BattleCardSlot slot = this.alphaHand[i];
                this.alphaHand[i] = null;
                return slot;
            }
            return null;
        }

        private BattleCardSlot RemoveFromLine(Link link, int slotIndex)
        {
            BattleCardSlot[] line = link == Link.front ? this.alphaFrontLine : this.alphaBackLine;
            if (line == null || slotIndex >= line.Length) return null;
            BattleCardSlot slot = line[slotIndex];
            line[slotIndex] = null;
            return slot;
        }

        private BattleCardSlot GetLineSlot(Link link, int slotIndex)
        {
            BattleCardSlot[] line = link == Link.front ? this.alphaFrontLine : this.alphaBackLine;
            if (line == null || slotIndex >= line.Length) return null;
            return line[slotIndex];
        }

        private void SetLineSlot(Link link, int slotIndex, BattleCardSlot slot)
        {
            if (link == Link.front)
                this.alphaFrontLine = this.EnsureSlotCapacity(this.alphaFrontLine, slotIndex);
            else
                this.alphaBackLine = this.EnsureSlotCapacity(this.alphaBackLine, slotIndex);
            BattleCardSlot[] line = link == Link.front ? this.alphaFrontLine : this.alphaBackLine;
            line[slotIndex] = slot;
        }

        private void InsertIntoFrontLine(BattleCardSlot slot, int slotIndex)
        {
            this.alphaFrontLine = this.EnsureSlotCapacity(this.alphaFrontLine, slotIndex);
            slot.slot_index = slotIndex;
            this.alphaFrontLine[slotIndex] = slot;
        }

        private void InsertIntoBackLine(BattleCardSlot slot, int slotIndex)
        {
            this.alphaBackLine = this.EnsureSlotCapacity(this.alphaBackLine, slotIndex);
            slot.slot_index = slotIndex;
            this.alphaBackLine[slotIndex] = slot;
        }

        private BattleCardSlot[] EnsureSlotCapacity(BattleCardSlot[] array, int requiredIndex)
        {
            if (array != null && array.Length > requiredIndex) return array;
            int needed = requiredIndex + 1;
            BattleCardSlot[] result = new BattleCardSlot[needed];
            if (array != null) System.Array.Copy(array, result, Mathf.Min(array.Length, needed));
            return result;
        }

        /// <summary>Moves a card already on the board from one line slot to an empty slot.</summary>
        public void MoveCardOnLine(string codeName, Link fromLink, int fromIndex, Link toLink, int toIndex)
        {
            if (fromIndex < 0 || toIndex < 0) return;
            if (!this.IsSlotEmpty(this.GetLineSlot(toLink, toIndex))) return;
            BattleCardSlot slot = this.RemoveFromLine(fromLink, fromIndex);
            if (slot == null) return;
            if (toLink == Link.front) this.InsertIntoFrontLine(slot, toIndex);
            else this.InsertIntoBackLine(slot, toIndex);
            this.OnBattleStatusChanged?.Invoke();
        }

        /// <summary>Swaps two card slots on the board (front or back line).</summary>
        public void SwapCardsOnLine(Link linkA, int indexA, Link linkB, int indexB)
        {
            BattleCardSlot slotA = this.GetLineSlot(linkA, indexA);
            BattleCardSlot slotB = this.GetLineSlot(linkB, indexB);
            this.SetLineSlot(linkA, indexA, slotB);
            this.SetLineSlot(linkB, indexB, slotA);
            this.OnBattleStatusChanged?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Central facade for all Lua script calls in SG03.
    /// Defines script name constants and typed entry-point methods.
    /// Delegates every call to BattleScript (SaiGame) which sends the
    /// request to the backend Lua runner endpoint.
    /// </summary>
    public class BattleScripts : SaiBehaviour
    {
        [Header("Script Names")]
        [SerializeField] private string scriptNameBattleStart        = "battle_start";
        [SerializeField] private string scriptNameBattleEnd          = "battle_end";
        [SerializeField] private string scriptNameBattleStatus       = "battle_status";
        [SerializeField] private string scriptNameBattleSessionExists = "battle_session_exists";
        [SerializeField] private string scriptNameInitCards          = "init_cards";
        [SerializeField] private string scriptNameGetCardDefinitions = "get_card_definitions";
        [SerializeField] private string scriptNameCardDeploy         = "card_deploy";
        [SerializeField] private string scriptNameAlphaCardActive    = "alpha_card_active";
        [SerializeField] private string scriptNameAlphaCardDeploy    = "alpha_card_deploy";
        [SerializeField] private string scriptNameAlphaTurnEnd       = "alpha_turn_end";
        [SerializeField] private string scriptNameAlphaDefendingEnd  = "alpha_defending_end";
        [SerializeField] private string scriptNameAlphaCheatSelectDraws = "alpha_cheat_select_draws";
        [SerializeField] private string scriptNameCheatSeppuku = "cheat_seppuku";

        private BattleScript battleScript => SaiServer.Instance != null ? SaiServer.Instance.BattleScript : null;

        [SerializeField] private BattleState battleState;

        [Header("UI")]
        [SerializeField] private GamePanelUI gamePanelUI;

        private bool isRunning;

        /// <summary>True while a script request is in-flight (waiting for server response).</summary>
        public bool IsRunning => this.isRunning;

        [Header("Debug")]
        [SerializeField] private bool logPayload = true;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleState();
            this.LoadGamePanelUI();
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            this.battleState = UnityEngine.Object.FindFirstObjectByType<BattleState>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        private void LoadGamePanelUI()
        {
            if (this.gamePanelUI != null) return;
            this.gamePanelUI = UnityEngine.Object.FindFirstObjectByType<GamePanelUI>(FindObjectsInactive.Include);
        }

        public void RunBattleStart(string requestBody, Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleStart))) return;
            this.LogPayload("RunBattleStart", "#00FFCC", requestBody);
            this.RunWithLock(this.scriptNameBattleStart, requestBody, onSuccess, onError);
        }

        public void RunBattleEnd(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleEnd))) return;
            this.LogPayload("RunBattleEnd", "#FF6B6B", null);
            this.RunWithLock(this.scriptNameBattleEnd, null, onSuccess, onError);
        }

        public void RunBattleStatus(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleStatus))) return;
            this.LogPayload("RunBattleStatus", "#FFD700", null);
            this.RunWithLock(this.scriptNameBattleStatus, null, onSuccess, onError);
        }

        public void RunBattleSessionExists(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleSessionExists)))
            {
                onError?.Invoke("Battle script service is unavailable.");
                return;
            }
            this.LogPayload("RunBattleSessionExists", "#88DDFF", null);
            this.RunWithLock(this.scriptNameBattleSessionExists, null, onSuccess, onError);
        }

        public void RunInitCards(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunInitCards))) return;
            this.LogPayload("RunInitCards", "#88DDFF", null);
            this.RunWithLock(this.scriptNameInitCards, null, onSuccess, onError);
        }

        public void RunGetCardDefinitions(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunGetCardDefinitions))) return;
            this.LogPayload("RunGetCardDefinitions", "#AAFFAA", null);
            this.RunWithLock(this.scriptNameGetCardDefinitions, null, onSuccess, onError);
        }

        public void RunCardDeploy(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunCardDeploy))) return;
            string requestBody = this.BuildCardDeployRequestBody();
            this.LogPayload("RunCardDeploy", "#FF88FF", requestBody);
            this.RunWithLock(this.scriptNameCardDeploy, requestBody, onSuccess, onError);
        }

        public void RunAlphaAttacking(string attackerInventoryItemId, string defenderInventoryItemId, string attackerItemDefinitionCodeName, string defenderItemDefinitionCodeName, Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaAttacking))) return;
            string requestBody = this.BuildAlphaAttackingRequestBody(attackerInventoryItemId, defenderInventoryItemId, attackerItemDefinitionCodeName, defenderItemDefinitionCodeName);
            this.LogPayload("RunAlphaAttacking", "#FF4444", requestBody);
            this.RunWithLock(this.scriptNameAlphaCardActive, requestBody, onSuccess, onError);
        }

        public void RunAlphaCardDeploy(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaCardDeploy)))
            {
                onError?.Invoke("Battle script service is unavailable.");
                return;
            }
            string requestBody = this.BuildAlphaCardDeployRequestBody();
            this.LogPayload("RunAlphaCardDeploy", "#AADDFF", requestBody);
            this.RunWithLock(this.scriptNameAlphaCardDeploy, requestBody, onSuccess, onError);
        }

        public void RunAlphaCardDeploy(string inventoryItemId, Link targetLink, int targetIndex,
            Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaCardDeploy)))
            {
                onError?.Invoke("Battle script service is unavailable.");
                return;
            }
            string requestBody = this.BuildAlphaCardDeployRequestBody(
                inventoryItemId, targetLink, targetIndex);
            this.LogPayload("RunAlphaCardDeploy", "#AADDFF", requestBody);
            this.RunWithLock(this.scriptNameAlphaCardDeploy, requestBody, onSuccess, onError);
        }

        public void RunAlphaTurnEnd(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaTurnEnd))) return;
            this.LogPayload("RunAlphaTurnEnd", "#FFAA33", null);
            this.RunWithLock(this.scriptNameAlphaTurnEnd, null, onSuccess, onError);
        }

        public void RunAlphaDefendingEnd(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaDefendingEnd))) return;
            this.LogPayload("RunAlphaDefendingEnd", "#88CCFF", null);
            this.RunWithLock(this.scriptNameAlphaDefendingEnd, null, onSuccess, onError);
        }

        public void RunAlphaCheatSelectDraws(IReadOnlyList<string> inventoryItemIds,
            Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaCheatSelectDraws))) return;
            string requestBody = this.BuildAlphaCheatSelectDrawsRequestBody(inventoryItemIds);
            this.LogPayload("RunAlphaCheatSelectDraws", "#B57BFF", requestBody);
            this.RunWithLock(this.scriptNameAlphaCheatSelectDraws, requestBody, onSuccess, onError);
        }

        public void RunCheatSeppuku(string sourceInventoryItemId, string targetInventoryItemId,
            Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunCheatSeppuku))) return;
            string requestBody = this.BuildCheatSeppukuRequestBody(sourceInventoryItemId, targetInventoryItemId);
            this.LogPayload("RunCheatSeppuku", "#B57BFF", requestBody);
            this.RunWithLock(this.scriptNameCheatSeppuku, requestBody, onSuccess, onError);
        }

        /// <summary>Guards against concurrent requests; acquires the lock and dispatches the script call.</summary>
        private void RunWithLock(string scriptName, string requestBody, Action<string> onSuccess, Action<string> onError)
        {
            if (this.isRunning)
            {
                Debug.LogWarning($"[BattleScripts] '{scriptName}' blocked — a script is already in-flight.", this.gameObject);
                return;
            }

            this.isRunning = true;
            string payloadPart = string.IsNullOrEmpty(requestBody) ? string.Empty : "\n" + requestBody;
            Debug.Log($"[BattleScripts] RunScript: <color=#FFFF00>{scriptName}</color>{payloadPart}");
            this.battleScript.RunScript(scriptName, requestBody, this.ReleaseLockThenSuccess(scriptName, onSuccess), this.ReleaseLockThenError(scriptName, onError));
        }

        private Action<string> ReleaseLockThenSuccess(string scriptName, Action<string> onSuccess)
        {
            return result =>
            {
                this.isRunning = false;
                if (this.logPayload)
                    this.LogResponse(scriptName, result);
                this.ShowScriptOutputError(scriptName, result);
                onSuccess?.Invoke(result);
            };
        }

        private void ShowScriptOutputError(string scriptName, string result)
        {
            if (string.IsNullOrWhiteSpace(result)) return;

            try
            {
                BattleStatusScriptResponse response = JsonUtility.FromJson<BattleStatusScriptResponse>(result);
                string error = response?.output?.error;
                if (string.IsNullOrWhiteSpace(error)) return;

                Debug.LogWarning($"[BattleScripts] '{scriptName}' output error: {error}", this.gameObject);
                this.ShowToastError(error);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"[BattleScripts] Could not inspect '{scriptName}' response for an output error: {exception.Message}", this.gameObject);
            }
        }

        private void ShowToastError(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return;
            if (this.gamePanelUI == null)
            {
                Debug.LogWarning("[BattleScripts] GamePanelUI is not assigned for error toast.", this.gameObject);
                return;
            }
            this.gamePanelUI.ShowErrorToast(error);
        }

        private void LogResponse(string scriptName, string result)
        {
            int timestamp = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (result.Length <= 500)
            {
                Debug.Log($"[BattleScripts] <color=#FFFF00>{scriptName}</color> [{timestamp}] response:\n{result}", this.gameObject);
                return;
            }
            string path = Path.Combine(Application.persistentDataPath, $"{timestamp}_BattleScripts_{scriptName}.json");
            File.WriteAllText(path, result);
            Debug.Log($"[BattleScripts] <color=#FFFF00>{scriptName}</color> [{timestamp}] response too large ({result.Length} chars) \u2192 saved to:\n{path}", this.gameObject);
        }

        private Action<string> ReleaseLockThenError(string scriptName, Action<string> onError)
        {
            return error =>
            {
                this.isRunning = false;
                Debug.LogWarning($"[BattleScripts] '{scriptName}' error: {error}", this.gameObject);
                this.ShowToastError(error);
                onError?.Invoke(error);
            };
        }

        private bool IsBattleScriptMissing(string callerName)
        {
            if (this.battleScript != null) return false;
            string serverState = SaiServer.Instance == null ? "SaiServer=null" : "SaiServer=ok, BattleScript=null";
            Debug.LogWarning($"[BattleScripts] {callerName}: battleScript is null ({serverState})", this.gameObject);
            return true;
        }

        private void LogPayload(string methodName, string color, string payload)
        {
            if (!this.logPayload) return;
            string payloadPart = string.IsNullOrEmpty(payload) ? string.Empty : "\n" + payload;
            Debug.Log($"<color={color}><b>[BattleScripts] \u25ba {methodName}</b></color>{payloadPart}", this.gameObject);
        }

        private string BuildAlphaAttackingRequestBody(string attackerInventoryItemId, string defenderInventoryItemId, string attackerItemDefinitionCodeName, string defenderItemDefinitionCodeName)
        {
            return $"{{\"payload\":{{\"attacker_inventory_item_id\":\"{attackerInventoryItemId}\",\"defender_inventory_item_id\":\"{defenderInventoryItemId}\",\"attacker_item_definition_code_name\":\"{attackerItemDefinitionCodeName}\",\"defender_item_definition_code_name\":\"{defenderItemDefinitionCodeName}\"}}}}";
        }

        private string BuildAlphaCheatSelectDrawsRequestBody(IReadOnlyList<string> inventoryItemIds)
        {
            if (inventoryItemIds == null) return "{\"payload\":{\"inventory_item_ids\":[]}}";
            string[] ids = new string[inventoryItemIds.Count];
            for (int i = 0; i < inventoryItemIds.Count; i++) ids[i] = inventoryItemIds[i] ?? string.Empty;
            return $"{{\"payload\":{{\"inventory_item_ids\":{this.ToJsonStringArray(ids)}}}}}";
        }

        private string BuildCheatSeppukuRequestBody(string sourceInventoryItemId, string targetInventoryItemId)
        {
            return $"{{\"payload\":{{\"source_inventory_item_id\":\"{sourceInventoryItemId ?? string.Empty}\",\"target_inventory_item_id\":\"{targetInventoryItemId ?? string.Empty}\"}}}}";
        }

        private string BuildAlphaCardDeployRequestBody()
        {
            string[] hand                  = this.CollectInventoryIds(this.battleState?.AlphaHand);
            CardDeployLineSlot[] frontLine = this.CollectLineSlots(this.battleState?.AlphaFrontLine);
            CardDeployLineSlot[] backLine  = this.CollectLineSlots(this.battleState?.AlphaBackLine);
            string handJson  = this.ToJsonStringArray(hand);
            string frontJson = this.ToJsonLineSlotArray(frontLine);
            string backJson  = this.ToJsonLineSlotArray(backLine);
            return $"{{\"payload\":{{\"hand\":{handJson},\"front_line\":{frontJson},\"back_line\":{backJson}}}}}";
        }

        private string BuildAlphaCardDeployRequestBody(string inventoryItemId, Link targetLink, int targetIndex)
        {
            string[] hand = this.CollectInventoryIdsExcept(
                this.battleState?.AlphaHand, inventoryItemId);
            CardDeployLineSlot[] frontLine = this.CollectLineSlots(this.battleState?.AlphaFrontLine);
            CardDeployLineSlot[] backLine = this.CollectLineSlots(this.battleState?.AlphaBackLine);
            CardDeployLineSlot[] targetLine = targetLink == Link.front ? frontLine : backLine;
            if (targetIndex >= 0 && targetIndex < targetLine.Length)
            {
                targetLine[targetIndex] = new CardDeployLineSlot
                {
                    inventory_item_id = inventoryItemId ?? string.Empty,
                    face_up = false,
                    slot_index = targetIndex
                };
            }

            string handJson = this.ToJsonStringArray(hand);
            string frontJson = this.ToJsonLineSlotArray(frontLine);
            string backJson = this.ToJsonLineSlotArray(backLine);
            return $"{{\"payload\":{{\"hand\":{handJson},\"front_line\":{frontJson},\"back_line\":{backJson}}}}}";
        }

        private string[] CollectInventoryIdsExcept(BattleCardSlot[] slots, string excludedInventoryItemId)
        {
            if (slots == null) return Array.Empty<string>();
            string[] result = new string[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                string inventoryItemId = slots[i]?.inventory_item_id;
                result[i] = inventoryItemId == excludedInventoryItemId
                    ? string.Empty
                    : inventoryItemId ?? string.Empty;
            }
            return result;
        }

        private string BuildCardDeployRequestBody()
        {
            string sessionId  = this.battleState != null ? this.battleState.SessionId : string.Empty;
            string[] hand      = this.CollectInventoryIds(this.battleState?.AlphaHand);
            CardDeployLineSlot[] frontLine = this.CollectLineSlots(this.battleState?.AlphaFrontLine);
            CardDeployLineSlot[] backLine  = this.CollectLineSlots(this.battleState?.AlphaBackLine);
            string handJson  = this.ToJsonStringArray(hand);
            string frontJson = this.ToJsonLineSlotArray(frontLine);
            string backJson  = this.ToJsonLineSlotArray(backLine);
            return $"{{\"payload\":{{\"session_id\":\"{sessionId}\",\"hand\":{handJson},\"front_line\":{frontJson},\"back_line\":{backJson}}}}}";
        }

        private CardDeployLineSlot[] CollectLineSlots(BattleCardSlot[] slots)
        {
            if (slots == null) return new CardDeployLineSlot[0];
            CardDeployLineSlot[] result = new CardDeployLineSlot[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                BattleCardSlot slot = slots[i];
                result[i] = new CardDeployLineSlot
                {
                    inventory_item_id = slot?.inventory_item_id ?? string.Empty,
                    face_up           = slot?.face_up ?? false,
                    slot_index        = i
                };
            }
            return result;
        }

        private string ToJsonLineSlotArray(CardDeployLineSlot[] slots)
        {
            if (slots == null || slots.Length == 0) return "[]";
            System.Text.StringBuilder sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < slots.Length; i++)
            {
                if (i > 0) sb.Append(",");
                string faceUpStr = slots[i].face_up ? "true" : "false";
                sb.Append($"{{\"inventory_item_id\":\"{slots[i].inventory_item_id}\",\"face_up\":{faceUpStr},\"slot_index\":{slots[i].slot_index}}}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string[] CollectInventoryIds(BattleCardSlot[] slots)
        {
            if (slots == null) return new string[0];
            string[] ids = new string[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                ids[i] = slots[i]?.inventory_item_id ?? string.Empty;
            return ids;
        }

        private string ToJsonStringArray(string[] items)
        {
            if (items == null || items.Length == 0) return "[]";
            string joined = string.Join(",", System.Array.ConvertAll(items, id => $"\"{id}\""));
            return $"[{joined}]";
        }
    }
}

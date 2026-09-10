using System;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SG03.UI.Components;

namespace SG03.UI
{
    // Handles the Start, Resume, Cancel Last Game, and End Game controls.
    // Receives the selected preset via HandlePresetTabSelected / HandlePresetSlotsLoaded
    // so it can validate and initiate StartBattle.
    public class GameBattleActionsUI
    {
        private const string BattleModeNormal = "normal";
        private const string DefaultEnemyCodeName = "the_bent_spoon_1";
        private const string NewGameButtonText = "Start with 5 soul";
        private const string ResumeButtonText = "Resume";

        private readonly Func<BattleScripts> getBattleScripts;
        private readonly Func<BattleStateCtrl> getBattleStateCtrl;

        private PresetData selectedPreset;

        private Button btnEndBattle;
        private Button btnCancelLastGame;
        private Button btnStartBattle;
        private DropdownField enemyCodeNameInput;

        private bool hasActiveBattleSession;

        public event Action OnBattleStartedOrResumed;
        public event Action OnBattleStarted;
        public event Action OnBattleCancelled;

        public GameBattleActionsUI(
            Func<BattleScripts> getBattleScripts,
            Func<BattleStateCtrl> getBattleStateCtrl)
        {
            this.getBattleScripts = getBattleScripts;
            this.getBattleStateCtrl = getBattleStateCtrl;
        }

        public void Bind(VisualElement root)
        {
            this.BindButtons(root);
            this.RegisterCallbacks();
        }

        private void BindButtons(VisualElement root)
        {
            this.btnEndBattle = root.Q<Button>("BtnEndBattle");
            this.btnCancelLastGame = root.Q<Button>("BtnCancelLastGame");
            this.btnStartBattle = root.Q<Button>("BtnStartBattle");
            this.enemyCodeNameInput = root.Q<DropdownField>("EnemyCodeNameInput");
            this.enemyCodeNameInput?.SetValueWithoutNotify(DefaultEnemyCodeName);
        }

        private void RegisterCallbacks()
        {
            this.btnEndBattle?.RegisterCallback<ClickEvent>(_ => this.OnEndBattleClicked());
            this.btnCancelLastGame?.RegisterCallback<ClickEvent>(_ => this.OnCancelLastGameClicked());
            this.btnStartBattle?.RegisterCallback<ClickEvent>(_ => this.OnStartBattleClicked());
            this.enemyCodeNameInput?.RegisterValueChangedCallback(_ => this.ResetStartBattleButtonText());
        }

        // Called by GamePanelUI when a desk tab is tapped (before full slot load).
        public void HandlePresetTabSelected(PresetData preset)
        {
            this.selectedPreset = preset;
            this.ResetStartBattleButtonText();
        }

        // Called by GamePanelUI when full slot data has been fetched.
        public void HandlePresetSlotsLoaded(PresetData preset)
        {
            this.selectedPreset = preset;
        }

        public void SetBattleSessionAvailabilityLoading()
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(false);
            this.btnStartBattle.text = "Checking...";
        }

        public void SetBattleSessionAvailability(bool hasActiveBattleSession)
        {
            this.hasActiveBattleSession = hasActiveBattleSession;
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(true);
            this.btnStartBattle.text = this.hasActiveBattleSession ? ResumeButtonText : NewGameButtonText;
        }



        protected virtual void OnEndBattleClicked()
        {
            this.TriggerEndBattle();
        }

        protected virtual void OnStartBattleClicked()
        {
            if (this.hasActiveBattleSession)
            {
                this.TriggerResumeBattle();
                return;
            }
            this.TriggerStartBattle();
        }

        protected virtual void OnCancelLastGameClicked()
        {
            this.TriggerCancelLastGame();
        }

        private void TriggerCancelLastGame()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (scripts == null)
            {
                this.SetCancelLastGameButtonText("No Script");
                return;
            }
            if (scripts.IsRunning)
            {
                ToastMessage.ShowError("Another battle action is still running.", this.btnCancelLastGame);
                return;
            }

            this.SetCancelLastGameLoading(true);
            scripts.RunBattleEnd(this.OnBattleCancelSucceeded, this.OnBattleCancelFailed);
        }

        private void SetCancelLastGameLoading(bool isLoading)
        {
            if (this.btnCancelLastGame != null)
            {
                this.btnCancelLastGame.SetEnabled(!isLoading);
                this.btnCancelLastGame.text = isLoading ? "Cancelling..." : "Cancel Last Game";
            }
            this.btnStartBattle?.SetEnabled(!isLoading);
        }

        private void SetCancelLastGameButtonText(string text)
        {
            if (this.btnCancelLastGame == null) return;
            this.btnCancelLastGame.text = text;
        }

        private void OnBattleCancelSucceeded(string response)
        {
            this.SetCancelLastGameLoading(false);
            if (TryGetScriptError(response, out string error))
            {
                ToastMessage.ShowError(error, this.btnCancelLastGame);
                return;
            }

            this.hasActiveBattleSession = false;
            this.getBattleStateCtrl()?.ClientActions?.CancelResume();
            this.getBattleStateCtrl()?.BattleState?.ClearData();
            this.OnBattleCancelled?.Invoke();
        }

        private void OnBattleCancelFailed(string error)
        {
            this.SetCancelLastGameLoading(false);
            ToastMessage.ShowError(error, this.btnCancelLastGame);
            Debug.LogWarning("GameBattleActionsUI: Cancel last game failed: " + error);
        }

        private void TriggerEndBattle()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (!this.CanEndBattle(scripts)) return;
            this.SetEndBattleLoading(true);
            scripts.RunBattleEnd(this.OnBattleEndSucceeded, this.OnBattleEndFailed);
        }

        private bool CanEndBattle(BattleScripts scripts)
        {
            if (scripts != null) return true;
            this.SetEndBattleButtonText("No Script");
            return false;
        }

        private void SetEndBattleLoading(bool isLoading)
        {
            if (this.btnEndBattle == null) return;
            this.btnEndBattle.SetEnabled(!isLoading);
            this.btnEndBattle.text = isLoading ? "Ending..." : "End Game";
        }

        private void SetEndBattleButtonText(string text)
        {
            if (this.btnEndBattle == null) return;
            this.btnEndBattle.text = text;
        }

        private void OnBattleEndSucceeded(string response)
        {
            this.SetEndBattleLoading(false);
            this.SetEndBattleButtonText("Battle Ended");
            this.getBattleStateCtrl()?.BattleState?.ClearData();
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;
            if (string.IsNullOrWhiteSpace(activeScene.name)) return;
            SceneManager.LoadScene(activeScene.name);
        }

        private void OnBattleEndFailed(string error)
        {
            this.SetEndBattleLoading(false);
            this.SetEndBattleButtonText("End Failed");
            Debug.LogWarning("GameBattleActionsUI: Battle end failed: " + error);
        }

        private void TriggerStartBattle()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (!this.CanStartBattle(scripts)) return;
            this.getBattleStateCtrl()?.ClientActions?.CancelResume();
            string enemyCodeName = this.GetEnemyCodeName();
            string presetInstanceId = this.selectedPreset.id;
            string requestBody = this.BuildBattleStartRequestBody(enemyCodeName, presetInstanceId);
            this.SetStartBattleLoading(true);
            scripts.RunBattleStart(requestBody, this.OnBattleStartSucceeded, this.OnBattleStartFailed);
        }

        private void TriggerResumeBattle()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (scripts == null)
            {
                this.SetStartBattleButtonText("No Script");
                return;
            }

            this.getBattleStateCtrl()?.ClientActions?.BeginResume();
            this.SetResumeBattleLoading(true);
            scripts.RunBattleStatus(this.OnResumeBattleSucceeded, this.OnResumeBattleFailed);
        }

        private bool CanStartBattle(BattleScripts scripts)
        {
            if (this.selectedPreset == null)
            {
                this.SetStartBattleButtonText("Select Desk");
                return false;
            }
            if (string.IsNullOrWhiteSpace(this.selectedPreset.id))
            {
                this.SetStartBattleButtonText("Select Desk");
                return false;
            }
            if (string.IsNullOrWhiteSpace(this.GetEnemyCodeName()))
            {
                this.SetStartBattleButtonText("Enter Enemy");
                return false;
            }
            if (scripts != null) return true;
            this.SetStartBattleButtonText("No Script");
            return false;
        }

        private string GetEnemyCodeName()
        {
            if (this.enemyCodeNameInput == null) return string.Empty;
            if (this.enemyCodeNameInput.value == null) return string.Empty;
            return this.enemyCodeNameInput.value.Trim();
        }

        private string BuildBattleStartRequestBody(string enemyCodeName, string presetInstanceId)
        {
            BattleStartScriptRequest request = new BattleStartScriptRequest();
            request.payload = new BattleStartPayload();
            request.payload.battle_mode = BattleModeNormal;
            request.payload.enemy_entity_key = enemyCodeName;
            request.payload.preset_instance_id = presetInstanceId;
            return JsonUtility.ToJson(request);
        }

        private void SetStartBattleLoading(bool isLoading)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(!isLoading);
            this.btnStartBattle.text = isLoading ? "Starting..." : "Start Battle";
        }

        private void SetResumeBattleLoading(bool isLoading)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(!isLoading);
            this.btnStartBattle.text = isLoading ? "Resuming..." : ResumeButtonText;
        }

        private void SetStartBattleButtonText(string text)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.text = text;
        }

        private void ResetStartBattleButtonText()
        {
            this.SetStartBattleButtonText(this.hasActiveBattleSession ? ResumeButtonText : NewGameButtonText);
        }

        private void OnResumeBattleSucceeded(string response)
        {
            this.SetResumeBattleLoading(false);
            this.GetAllCardDefinitions();
            this.ApplyBattleStatusResponse(response);
            this.OnBattleStartedOrResumed?.Invoke();
        }

        private void OnResumeBattleFailed(string error)
        {
            this.getBattleStateCtrl()?.ClientActions?.CancelResume();
            this.SetResumeBattleLoading(false);
            Debug.LogWarning("GameBattleActionsUI: Resume battle failed: " + error);
        }

        private void OnBattleStartSucceeded(string response)
        {
            this.SetStartBattleLoading(false);

            if (TryGetScriptError(response, out string error))
            {
                this.hasActiveBattleSession = false;
                this.ResetStartBattleButtonText();
                ToastMessage.ShowError(error, this.btnStartBattle);
                return;
            }

            this.hasActiveBattleSession = true;
            this.SetStartBattleButtonText("Battle Started");
            this.GetAllCardDefinitions();
            this.ApplyBattleStatusResponse(response);
            this.OnBattleStarted?.Invoke();
            this.OnBattleStartedOrResumed?.Invoke();
        }

        private void OnBattleStartFailed(string error)
        {
            this.SetStartBattleLoading(false);
            this.SetStartBattleButtonText("Battle Failed");
            Debug.LogWarning("GameBattleActionsUI: Battle start failed: " + error);
        }

        private static bool TryGetScriptError(string response, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(response)) return false;

            BattleStatusScriptResponse scriptResponse = JsonUtility.FromJson<BattleStatusScriptResponse>(response);
            error = scriptResponse?.output?.error;
            return !string.IsNullOrWhiteSpace(error);
        }

        private void ApplyBattleStatusResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return;
            this.getBattleStateCtrl()?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void GetAllCardDefinitions()
        {
            BattleStateCtrl ctrl = this.getBattleStateCtrl();
            if (ctrl?.BattleCardDefinitions == null) return;
            ctrl.BattleCardDefinitions.GetAll();
        }
    }
}

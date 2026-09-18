using System;
using SaiGame.Services;
using SG03.UI.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SG03.UI
{
    public class GamePanelUI : SaiBehaviour
    {
        public string PanelId => "Game";

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelAsset;

        [Header("References")]
        [SerializeField] private SaiServer saiServer;
        [SerializeField] private ItemPreset itemPreset;
        [SerializeField] private BattleScripts battleScripts;
        [SerializeField] private BattleStateCtrl battleStateCtrl;
        [SerializeField] private CurrencyWallet currencyWallet;
        [SerializeField] private UIDocument uiDocument;

        [Header("Scene Navigation")]
        [SerializeField] private string lobbySceneName = "1-lobby";

        private Button btnBackToLobby;
        private Button btnCancelLastGame;
        private Button btnEndBattle;
        private Button btnStartBattle;
        private Label playerNameLabel;
        private VisualElement battleDeskInfo;
        private VisualElement gameRoot;
        private VisualElement gameViewport;
        private VisualElement root;
        private bool authEventsSubscribed;
        private bool battleStateEventsSubscribed;

        private GameDeskTabsUI deskTabsUI;
        private GameBattleStatusUI battleStatusUI;
        private GameBattleActionsUI battleActionsUI;
        private SoulEnergyUI soulEnergyUI;
        private GameMapUI mapUI;

        public void ShowErrorToast(string error)
        {
            VisualElement toastSource = this.root ?? this.uiDocument?.rootVisualElement;
            if (toastSource == null)
            {
                Debug.LogWarning("[GamePanelUI] Cannot show error toast because the main UI root is unavailable.", this.gameObject);
                return;
            }
            ToastMessage.ShowError(error, toastSource);
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadSaiServer();
            this.LoadCurrencyWallet();
            this.LoadItemPreset();
            this.LoadBattleScript();
            this.LoadBattleStateCtrl();
            this.LoadUIDocument();
            this.LoadPanelSettings();
            this.LoadPanelAsset();
        }

        private void LoadSaiServer()
        {
            SaiServer instance = SaiServer.Instance;
            if (instance == null) return;
            if (this.saiServer == instance) return;
            this.UnsubscribeFromAuthEvents();
            this.saiServer = instance;
            this.itemPreset = null;
            this.battleScripts = null;
            Debug.LogWarning(this.transform.name + ": LoadSaiServer", this.gameObject);
        }

        private void LoadItemPreset()
        {
            if (this.saiServer == null) return;
            if (this.HasValidItemPresetReference()) return;
            this.itemPreset = this.saiServer.ItemPreset;
            if (this.itemPreset == null) this.itemPreset = this.saiServer.GetComponentInChildren<ItemPreset>(true);
            if (this.itemPreset == null) return;
            Debug.LogWarning(this.transform.name + ": LoadItemPreset", this.gameObject);
        }

        private void LoadCurrencyWallet()
        {
            if (this.currencyWallet != null) return;
            this.currencyWallet = this.GetComponent<CurrencyWallet>();
            if (this.currencyWallet == null)
                this.currencyWallet = this.gameObject.AddComponent<CurrencyWallet>();
        }

        private void LoadBattleScript()
        {
            if (this.battleScripts != null) return;
            this.battleScripts = GameObject.FindAnyObjectByType<BattleScripts>();
            if (this.battleScripts == null) return;
            Debug.LogWarning(this.transform.name + ": LoadBattleScript", this.gameObject);
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = GameObject.FindAnyObjectByType<BattleStateCtrl>();
            if (this.battleStateCtrl == null) return;
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        private bool HasValidItemPresetReference()
        {
            if (this.itemPreset == null) return false;
            if (this.saiServer == null) return false;
            return this.itemPreset.transform.IsChildOf(this.saiServer.transform);
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        private void LoadPanelSettings()
        {
            if (this.uiDocument == null) return;
            if (this.uiDocument.panelSettings != null) return;
#if UNITY_EDITOR
            PanelSettings panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_sg03/UI/LobbyPanelSettings.asset");
            if (panelSettings == null) return;
            this.uiDocument.panelSettings = panelSettings;
            Debug.LogWarning(this.transform.name + ": LoadPanelSettings", this.gameObject);
#endif
        }

        private void LoadPanelAsset()
        {
#if UNITY_EDITOR
            this.LoadPanelAssetReference();
            this.AssignPanelAssetToDocument();
#endif
        }

#if UNITY_EDITOR
        private void LoadPanelAssetReference()
        {
            if (this.panelAsset != null) return;
            this.panelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Game/GamePanel.uxml");
            Debug.LogWarning(this.transform.name + ": LoadPanelAsset", this.gameObject);
        }

        private void AssignPanelAssetToDocument()
        {
            if (this.uiDocument == null) return;
            if (this.uiDocument.visualTreeAsset != null) return;
            if (this.panelAsset == null) return;
            this.uiDocument.visualTreeAsset = this.panelAsset;
            Debug.LogWarning(this.transform.name + ": LoadPanelAssetToUIDocument", this.gameObject);
        }
#endif

        protected override void Start()
        {
            base.Start();
            this.EnsureServiceReferences();
            this.InitializeStandalonePanel();
            this.currencyWallet?.Refresh();
        }

        private void EnsureServiceReferences()
        {
            this.EnsureSaiServerReference();
            this.EnsureItemPresetReference();
            this.EnsureBattleScriptReference();
            this.EnsureBattleStateCtrlReference();
            this.SubscribeToAuthEvents();
        }

        private void EnsureSaiServerReference()
        {
            SaiServer instance = SaiServer.Instance;
            if (instance == null) return;
            if (this.saiServer == instance) return;
            this.UnsubscribeFromAuthEvents();
            this.saiServer = instance;
            this.itemPreset = null;
            this.battleScripts = null;
        }

        private void EnsureItemPresetReference()
        {
            if (this.saiServer == null) return;
            ItemPreset serverItemPreset = this.saiServer.ItemPreset;
            if (serverItemPreset == null) return;
            if (this.itemPreset == serverItemPreset) return;
            this.itemPreset = serverItemPreset;
        }

        private void EnsureBattleScriptReference()
        {
            if (this.battleScripts != null) return;
            this.battleScripts = GameObject.FindAnyObjectByType<BattleScripts>();
        }

        private void EnsureBattleStateCtrlReference()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = GameObject.FindAnyObjectByType<BattleStateCtrl>();
        }

        private void InitializeStandalonePanel()
        {
            if (this.root != null) return;
            if (this.uiDocument == null) return;
            this.BindPanelRoot(this.uiDocument.rootVisualElement);
        }

        private void BindPanelRoot(VisualElement panelRoot)
        {
            if (panelRoot == null) return;
            this.root = panelRoot;
            this.BindFromRoot(this.root);
        }

        private void BindFromRoot(VisualElement panelRoot)
        {
            this.BindPlayerName(panelRoot);
            this.BindNavigation(panelRoot);
            this.BindViewport(panelRoot);
            this.BindDeskTabs(panelRoot);
            this.BindBattleStatus(panelRoot);
            this.BindBattleActions(panelRoot);
            this.BindSoulEnergy(panelRoot);
            this.BindMap(panelRoot);
            this.WirePresetEventsToBattleActions();
            this.SubscribeToAuthEvents();
            this.SubscribeToBattleStateEvents();
            this.RefreshBattleSessionAvailability();
        }

        private void BindPlayerName(VisualElement panelRoot)
        {
            this.playerNameLabel = panelRoot.Q<Label>("PlayerNameLabel");
            this.RefreshPlayerName();
        }

        private void BindNavigation(VisualElement panelRoot)
        {
            this.btnBackToLobby = panelRoot.Q<Button>("BtnBackToLobby");
            this.btnBackToLobby?.RegisterCallback<ClickEvent>(_ => this.OnBackToLobbyClicked());
        }

        private void BindViewport(VisualElement panelRoot)
        {
            this.gameRoot = panelRoot.Q("GameRoot");
            this.gameViewport = panelRoot.Q("GameViewport");
            if (this.gameRoot == null) return;
            if (this.gameViewport == null) return;
            _ = new LobbyAspectRatioKeeper(this.gameRoot, this.gameViewport);
        }

        private void BindDeskTabs(VisualElement panelRoot)
        {
            this.battleDeskInfo = panelRoot.Q("BattleDeskInfo");
            this.deskTabsUI = new GameDeskTabsUI(this.GetCurrentItemPreset);
            this.deskTabsUI.Bind(panelRoot);
        }

        private void BindBattleStatus(VisualElement panelRoot)
        {
            this.battleStatusUI = new GameBattleStatusUI(this.GetCurrentBattleStateCtrl);
            this.battleStatusUI.Bind(panelRoot);
        }

        private void BindBattleActions(VisualElement panelRoot)
        {
            this.btnStartBattle = panelRoot.Q<Button>("BtnStartBattle");
            this.btnCancelLastGame = panelRoot.Q<Button>("BtnCancelLastGame");
            this.btnEndBattle = panelRoot.Q<Button>("BtnEndBattle");
            this.battleActionsUI = new GameBattleActionsUI(
                this.GetCurrentBattleScripts,
                this.GetCurrentBattleStateCtrl);
            this.battleActionsUI.Bind(panelRoot);
            this.battleActionsUI.OnBattleStarted += this.RefreshCurrencyWallet;
            this.battleActionsUI.OnBattleStartedOrResumed += this.HideBattleSetupControls;
            this.battleActionsUI.OnBattleCancelled += this.ShowNewGameSetup;
        }

        private void RefreshCurrencyWallet()
        {
            this.currencyWallet?.Refresh();
        }

        private void BindSoulEnergy(VisualElement panelRoot)
        {
            this.soulEnergyUI?.Dispose();
            this.soulEnergyUI = new SoulEnergyUI(this, panelRoot, this.currencyWallet, panelRoot.Q("PlayerNavigation"));
            this.soulEnergyUI.Initialize();
        }

        private void BindMap(VisualElement panelRoot)
        {
            this.mapUI = new GameMapUI();
            this.mapUI.Bind(panelRoot);
        }

        private void WirePresetEventsToBattleActions()
        {
            if (this.deskTabsUI == null) return;
            if (this.battleActionsUI == null) return;
            this.deskTabsUI.OnPresetTabSelected += this.battleActionsUI.HandlePresetTabSelected;
            this.deskTabsUI.OnPresetSlotsLoaded += this.battleActionsUI.HandlePresetSlotsLoaded;
        }

        private ItemPreset GetCurrentItemPreset()
        {
            this.EnsureServiceReferences();
            return this.itemPreset;
        }

        private BattleScripts GetCurrentBattleScripts()
        {
            this.EnsureServiceReferences();
            return this.battleScripts;
        }

        private BattleStateCtrl GetCurrentBattleStateCtrl()
        {
            this.EnsureServiceReferences();
            return this.battleStateCtrl;
        }

        private void SubscribeToAuthEvents()
        {
            if (this.authEventsSubscribed) return;
            if (this.saiServer?.SaiAuth == null) return;
            this.saiServer.SaiAuth.OnLoginSuccess += this.OnLoginSuccess;
            this.authEventsSubscribed = true;
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (!this.authEventsSubscribed) return;
            if (this.saiServer?.SaiAuth == null) return;
            this.saiServer.SaiAuth.OnLoginSuccess -= this.OnLoginSuccess;
            this.authEventsSubscribed = false;
        }

        private void OnLoginSuccess(LoginResponse response)
        {
            this.RefreshPlayerName();
            this.soulEnergyUI?.Load();
            this.currencyWallet?.Refresh();
            this.RefreshBattleSessionAvailability();
        }

        private void RefreshBattleSessionAvailability()
        {
            if (this.battleActionsUI == null) return;
            this.EnsureServiceReferences();
            if (this.saiServer == null || !this.saiServer.IsAuthenticated) return;

            BattleScripts scripts = this.GetCurrentBattleScripts();
            if (scripts == null)
            {
                this.ShowNewGameSetup();
                return;
            }

            this.battleActionsUI.SetBattleSessionAvailabilityLoading();
            scripts.RunBattleSessionExists(this.OnBattleSessionExistsSucceeded, this.OnBattleSessionExistsFailed);
        }

        private void OnBattleSessionExistsSucceeded(string response)
        {
            BattleSessionExistsScriptResponse scriptResponse = JsonUtility.FromJson<BattleSessionExistsScriptResponse>(response);
            BattleSessionExistsOutput output = scriptResponse?.output;
            if (output == null)
            {
                Debug.LogWarning("[GamePanelUI] Could not parse battle_session_exists response.");
                this.ShowNewGameSetup();
                return;
            }

            if (!string.IsNullOrWhiteSpace(output.error))
            {
                if (this.IsBattleSessionNotFoundError(output.error))
                {
                    this.ShowNewGameSetup();
                    return;
                }

                Debug.LogWarning("[GamePanelUI] battle_session_exists error: " + output.error);
                this.ShowNewGameSetup();
                return;
            }

            if (output.exists)
            {
                this.ShowResumeControl();
                this.battleActionsUI?.SetBattleSessionAvailability(true);
                return;
            }

            // A missing battle session is a valid successful response: { exists: false }.
            this.ShowNewGameSetup();
        }

        private void OnBattleSessionExistsFailed(string error)
        {
            Debug.LogWarning("[GamePanelUI] battle_session_exists failed: " + error);
            this.ShowNewGameSetup();
        }

        private bool IsBattleSessionNotFoundError(string error)
        {
            return string.Equals(error, "no active battle session found", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error, "no active battle session", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error, "current battle session not found", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error, "battle session not found", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowNewGameSetup()
        {
            this.ShowBattleSetupControls();
            this.battleActionsUI?.SetBattleSessionAvailability(false);
            this.deskTabsUI?.LoadPresets();
        }

        private void ShowBattleSetupControls()
        {
            if (this.battleDeskInfo != null) this.battleDeskInfo.style.display = DisplayStyle.Flex;
            if (this.btnStartBattle != null) this.btnStartBattle.style.display = DisplayStyle.Flex;
            if (this.btnCancelLastGame != null) this.btnCancelLastGame.style.display = DisplayStyle.None;
            if (this.btnEndBattle != null) this.btnEndBattle.style.display = DisplayStyle.None;
        }

        private void ShowResumeControl()
        {
            if (this.battleDeskInfo != null) this.battleDeskInfo.style.display = DisplayStyle.None;
            if (this.btnStartBattle != null) this.btnStartBattle.style.display = DisplayStyle.Flex;
            if (this.btnCancelLastGame != null) this.btnCancelLastGame.style.display = DisplayStyle.Flex;
            if (this.btnEndBattle != null) this.btnEndBattle.style.display = DisplayStyle.None;
        }

        private void HideBattleSetupControls()
        {
            if (this.battleDeskInfo != null) this.battleDeskInfo.style.display = DisplayStyle.None;
            if (this.btnStartBattle != null) this.btnStartBattle.style.display = DisplayStyle.None;
            if (this.btnCancelLastGame != null) this.btnCancelLastGame.style.display = DisplayStyle.None;
            if (this.btnEndBattle != null) this.btnEndBattle.style.display = DisplayStyle.Flex;
            this.mapUI?.Hide();
        }

        private void RefreshPlayerName()
        {
            this.EnsureServiceReferences();
            if (this.playerNameLabel == null) return;
            UserData user = this.saiServer != null ? this.saiServer.CurrentUser : null;
            string displayName = user?.display_name;
            if (string.IsNullOrEmpty(displayName)) displayName = user?.username;
            this.playerNameLabel.text = string.IsNullOrEmpty(displayName) ? "Guest" : displayName;
        }

        protected virtual void OnBackToLobbyClicked()
        {
            if (string.IsNullOrWhiteSpace(this.lobbySceneName)) return;
            SceneManager.LoadScene(this.lobbySceneName);
        }

        protected virtual void OnDestroy()
        {
            this.DisposePanel();
        }

        private void DisposePanel()
        {
            this.battleStatusUI?.Dispose();
            this.deskTabsUI?.Dispose();
            this.soulEnergyUI?.Dispose();
            this.UnsubscribeFromAuthEvents();
            this.UnsubscribeFromBattleStateEvents();
        }

        private void SubscribeToBattleStateEvents()
        {
            if (this.battleStateEventsSubscribed) return;
            BattleStateCtrl ctrl = this.GetCurrentBattleStateCtrl();
            if (ctrl?.ClientActions == null) return;
            ctrl.ClientActions.OnBattleCompleted += this.HandleBattleCompletedAction;
            this.battleStateEventsSubscribed = true;
        }

        private void UnsubscribeFromBattleStateEvents()
        {
            if (!this.battleStateEventsSubscribed) return;
            BattleStateCtrl ctrl = this.GetCurrentBattleStateCtrl();
            if (ctrl?.ClientActions == null) return;
            ctrl.ClientActions.OnBattleCompleted -= this.HandleBattleCompletedAction;
            this.battleStateEventsSubscribed = false;
        }

        private void HandleBattleCompletedAction(string winner)
        {
            if (this.root == null || this.root.Q("BattleResultPopupOverlay") != null) return;

            BattleState state = this.GetCurrentBattleStateCtrl()?.BattleState;
            if (state == null) return;

            if (this.battleScripts == null)
            {
                bool isWin = string.Equals(winner, "alpha", System.StringComparison.OrdinalIgnoreCase);
                var popup = new GameBattleResultPopupUI(state, isWin, state.Turn, state.AlphaHp, state.OmegaHp, null, this.lobbySceneName);
                popup.Show(this.root);
                return;
            }

            this.battleScripts.RunBattleEnd(
                response =>
                {
                    if (string.IsNullOrWhiteSpace(response)) return;

                    BattleEndScriptResponse endResponse = JsonUtility.FromJson<BattleEndScriptResponse>(response);
                    BattleEndOutput output = endResponse?.output;

                    if (output != null && !string.IsNullOrEmpty(output.error))
                    {
                        Debug.LogError($"[GamePanelUI] Battle end script error: {output.error}");
                        return;
                    }

                    bool isWin = string.Equals(output?.winner ?? winner, "alpha", System.StringComparison.OrdinalIgnoreCase);
                    int turn = output != null ? output.turn : state.Turn;
                    int playerHp = output != null ? output.alpha_hp : state.AlphaHp;
                    int enemyHp = output != null ? output.omega_hp : state.OmegaHp;
                    BattleEndDropItem[] drops = output?.drops;

                    // Clear the battle state cache
                    state.ClearData();

                    // Show the popup
                    var popup = new GameBattleResultPopupUI(state, isWin, turn, playerHp, enemyHp, drops, this.lobbySceneName);
                    popup.Show(this.root);
                },
                error =>
                {
                    Debug.LogError($"[GamePanelUI] RunBattleEnd failed: {error}");
                    bool isWin = string.Equals(winner, "alpha", System.StringComparison.OrdinalIgnoreCase);
                    var popup = new GameBattleResultPopupUI(state, isWin, state.Turn, state.AlphaHp, state.OmegaHp, null, this.lobbySceneName);
                    popup.Show(this.root);
                }
            );
        }
    }
}

using System;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Subscribes to BattleState events and refreshes all battle-status labels
    // (HP, Source count, Void count, NextMove, Turn) in the Game panel.
    public class GameBattleStatusUI
    {
        private const int DefaultBattleDurationSeconds = 1500;

        private readonly Func<BattleStateCtrl> getBattleStateCtrl;

        private Label alphaHpLabel;
        private Label omegaHpLabel;
        private Label alphaSourceCountLabel;
        private Label omeraSourceCountLabel;
        private Label alphaTheVoidCountLabel;
        private Label omegaTheVoidCountLabel;
        private Label nextMoveLabel;
        private Label turnLabel;
        private Label countdownLabel;
        private IVisualElementScheduledItem countdownSchedule;

        private bool eventsSubscribed;

        public GameBattleStatusUI(Func<BattleStateCtrl> getBattleStateCtrl)
        {
            this.getBattleStateCtrl = getBattleStateCtrl;
        }

        public void Bind(VisualElement root)
        {
            this.BindLabels(root);
            this.SubscribeToStateEvents();
        }

        private void BindLabels(VisualElement root)
        {
            this.alphaHpLabel = root.Q<Label>("AlphaHpLabel");
            this.omegaHpLabel = root.Q<Label>("OmegaHpLabel");
            this.alphaSourceCountLabel = root.Q<Label>("AlphaSourceCountLabel");
            this.omeraSourceCountLabel = root.Q<Label>("OmeraSourceCountLabel");
            this.alphaTheVoidCountLabel = root.Q<Label>("AlphaTheVoidCountLabel");
            this.omegaTheVoidCountLabel = root.Q<Label>("OmegaTheVoidCountLabel");
            this.nextMoveLabel = root.Q<Label>("NextMoveLabel");
            this.turnLabel = root.Q<Label>("TurnLabel");
            this.countdownLabel = root.Q<Label>("BattleCountdownLabel");
            this.StartCountdownTimer();
        }

        private void SubscribeToStateEvents()
        {
            if (this.eventsSubscribed) return;
            BattleStateCtrl ctrl = this.getBattleStateCtrl();
            if (ctrl?.BattleState == null) return;
            ctrl.BattleState.OnBattleStatusChanged += this.RefreshBattleStatusUI;
            this.eventsSubscribed = true;
        }

        private void RefreshBattleStatusUI()
        {
            BattleState state = this.getBattleStateCtrl()?.BattleState;
            if (state == null) return;
            this.SetBattleHp(state.AlphaHp, state.OmegaHp);
            this.SetBattleSourceCounts(state.AlphaTheSourceCount, state.OmegaTheSourceCount);
            this.SetBattleVoidCounts(state.AlphaTheVoidCount, state.OmegaTheVoidCount);
            this.SetNextMoveLabel(state.NextMove);
            this.SetTurnLabel(state.Turn);
            this.UpdateCountdown();
        }

        private void SetBattleHp(int alphaHp, int omegaHp)
        {
            if (this.alphaHpLabel != null) this.alphaHpLabel.text = $"HP: {alphaHp}";
            if (this.omegaHpLabel != null) this.omegaHpLabel.text = $"HP: {omegaHp}";
        }

        private void SetBattleSourceCounts(int alphaCount, int omegaCount)
        {
            if (this.alphaSourceCountLabel != null) this.alphaSourceCountLabel.text = $"Source: {alphaCount}";
            if (this.omeraSourceCountLabel != null) this.omeraSourceCountLabel.text = $"Source: {omegaCount}";
        }

        private void SetBattleVoidCounts(int alphaCount, int omegaCount)
        {
            if (this.alphaTheVoidCountLabel != null) this.alphaTheVoidCountLabel.text = $"Void: {alphaCount}";
            if (this.omegaTheVoidCountLabel != null) this.omegaTheVoidCountLabel.text = $"Void: {omegaCount}";
        }

        private void SetNextMoveLabel(NextMoveType move)
        {
            if (this.nextMoveLabel == null) return;
            this.nextMoveLabel.text = $"Move: {move}";
        }

        private void SetTurnLabel(int turn)
        {
            if (this.turnLabel == null) return;
            this.turnLabel.text = $"Turn: {turn}";
        }

        private void StartCountdownTimer()
        {
            if (this.countdownLabel == null) return;
            this.countdownSchedule?.Pause();
            this.countdownSchedule = this.countdownLabel.schedule.Execute(this.UpdateCountdown).Every(1000);
            this.UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (this.countdownLabel == null) return;
            BattleState state = this.getBattleStateCtrl()?.BattleState;
            long startedAt = state != null ? state.StartedAt : 0;
            int duration = state != null && state.DurationSeconds > 0 ? state.DurationSeconds : DefaultBattleDurationSeconds;

            if (startedAt <= 0)
            {
                int initialMin = duration / 60;
                int initialSec = duration % 60;
                this.countdownLabel.text = $"{initialMin:D2}:{initialSec:D2}";
                return;
            }

            long currentUnix = this.GetCurrentUnixTimestamp();
            long elapsed = currentUnix - startedAt;
            long remaining = Math.Max(0, duration - elapsed);
            long minutes = remaining / 60;
            long seconds = remaining % 60;
            this.countdownLabel.text = $"{minutes:D2}:{seconds:D2}";
        }

        private long GetCurrentUnixTimestamp()
        {
            SaiGame.Services.SaiServer server = SaiGame.Services.SaiServer.Instance;
            if (server != null && server.HasServerTime)
            {
                DateTime serverTimeUtc = server.CurrentServerTime.ToUniversalTime();
                return (long)(serverTimeUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            }
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void Dispose()
        {
            this.countdownSchedule?.Pause();
            if (!this.eventsSubscribed) return;
            BattleStateCtrl ctrl = this.getBattleStateCtrl();
            if (ctrl?.BattleState == null) return;
            ctrl.BattleState.OnBattleStatusChanged -= this.RefreshBattleStatusUI;
            this.eventsSubscribed = false;
        }
    }
}

using NanoFrame.Core;
using NanoFrame.Event;
using Project.Gameplay.Config;
using Project.Gameplay.Grid;
using Project.Gameplay.Input;
using Project.Gameplay.Player;
using UnityEngine;

namespace Project.Gameplay.Match
{
    public class PrototypeMatchManager : Singleton<PrototypeMatchManager>, IManager
    {
        [SerializeField] private PrototypeBalanceConfig _balanceAsset;

        private PrototypeBalanceConfig _runtimeBalance;
        private float _remainingTime;
        private bool _roundActive;
        private bool _isPaused;
        private bool _hasEnded;

        public PrototypeBalanceConfig Settings => _runtimeBalance;
        public float RemainingTime => _remainingTime;
        public bool IsPaused => _isPaused;
        public bool IsRoundActive => _roundActive && !_hasEnded && !_isPaused;

        public void OnInit()
        {
            if (_balanceAsset != null)
            {
                _runtimeBalance = Instantiate(_balanceAsset);
            }
            else
            {
                _runtimeBalance = ScriptableObject.CreateInstance<PrototypeBalanceConfig>();
            }

            _remainingTime = _runtimeBalance.RoundDuration;
            _roundActive = false;
            _isPaused = false;
            _hasEnded = false;

            EventManager.Instance.Subscribe<OnPlayerDieEvent>(HandlePlayerDied);
        }

        public void OnUpdate()
        {
            if (PrototypeInputManager.Instance.ConsumePausePressed())
            {
                TogglePause();
            }

            if (!_roundActive || _hasEnded || _isPaused)
            {
                return;
            }

            _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
            EventManager.Instance.Fire(new OnMatchTimerChangedEvent
            {
                RemainingTime = _remainingTime,
                TotalTime = _runtimeBalance.RoundDuration
            });

            if (_remainingTime <= 0f)
            {
                ResolveByTerritory();
            }
        }

        public void OnDestroyManager()
        {
            EventManager.Instance.Unsubscribe<OnPlayerDieEvent>(HandlePlayerDied);
        }

        public void RestartMatch()
        {
            _hasEnded = false;
            _isPaused = false;
            _roundActive = false;
            Time.timeScale = 1f;

            _remainingTime = _runtimeBalance.RoundDuration;

            PrototypeGridManager.Instance.ResetGrid();
            PrototypePlayerManager.Instance.ResetPlayers();

            _roundActive = true;

            EventManager.Instance.Fire(new OnMatchTimerChangedEvent
            {
                RemainingTime = _remainingTime,
                TotalTime = _runtimeBalance.RoundDuration
            });
        }

        public void TogglePause()
        {
            if (!_roundActive || _hasEnded)
            {
                return;
            }

            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
        }

        private void HandlePlayerDied(OnPlayerDieEvent eventData)
        {
            if (_hasEnded)
            {
                return;
            }

            int winnerPlayerId = eventData.PlayerID == 1 ? 2 : 1;
            EndRound(winnerPlayerId, false);
        }

        private void ResolveByTerritory()
        {
            int player1Count = PrototypeGridManager.Instance.Player1TerritoryCount;
            int player2Count = PrototypeGridManager.Instance.Player2TerritoryCount;

            if (player1Count == player2Count)
            {
                EndRound(0, true);
                return;
            }

            int winnerPlayerId = player1Count > player2Count ? 1 : 2;
            EndRound(winnerPlayerId, false);
        }

        private void EndRound(int winnerPlayerId, bool isDraw)
        {
            _hasEnded = true;
            _roundActive = false;
            _isPaused = false;
            Time.timeScale = 1f;

            EventManager.Instance.Fire(new OnMatchFinishedEvent
            {
                WinnerPlayerID = winnerPlayerId,
                IsDraw = isDraw,
                Player1TerritoryCount = PrototypeGridManager.Instance.Player1TerritoryCount,
                Player2TerritoryCount = PrototypeGridManager.Instance.Player2TerritoryCount
            });
        }
    }
}
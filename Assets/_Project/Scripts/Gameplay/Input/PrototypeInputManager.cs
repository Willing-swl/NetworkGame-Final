using System;
using NanoFrame.Core;
using Project.Gameplay.Match;
using UnityEngine;

namespace Project.Gameplay.Input
{
    public class PrototypeInputManager : Singleton<PrototypeInputManager>, IManager
    {
        private const int DefaultBufferSize = 10;

        [SerializeField] private PlayerKeyboardBindings _player1Bindings = PlayerKeyboardBindings.CreateP1();
        [SerializeField] private PlayerKeyboardBindings _player2Bindings = PlayerKeyboardBindings.CreateP2();

        private IPlayerInputSource _player1Source;
        private IPlayerInputSource _player2Source;

        private PlayerInputFrame[] _player1Buffer = new PlayerInputFrame[DefaultBufferSize];
        private PlayerInputFrame[] _player2Buffer = new PlayerInputFrame[DefaultBufferSize];
        private int _player1WriteIndex;
        private int _player2WriteIndex;
        private int _player1SampleCount;
        private int _player2SampleCount;
        private bool _pausePressedThisFrame;

        public void OnInit()
        {
            _player1Source = new KeyboardPlayerInputSource(_player1Bindings ?? PlayerKeyboardBindings.CreateP1());
            _player2Source = new KeyboardPlayerInputSource(_player2Bindings ?? PlayerKeyboardBindings.CreateP2());

            int bufferSize = DefaultBufferSize;
            PrototypeMatchManager matchManager = PrototypeMatchManager.Instance;
            if (matchManager != null && matchManager.Settings != null)
            {
                bufferSize = Mathf.Max(1, matchManager.Settings.InputBufferSize);
            }

            _player1Buffer = new PlayerInputFrame[bufferSize];
            _player2Buffer = new PlayerInputFrame[bufferSize];
            _player1WriteIndex = 0;
            _player2WriteIndex = 0;
            _player1SampleCount = 0;
            _player2SampleCount = 0;
            _pausePressedThisFrame = false;
        }

        public void OnUpdate()
        {
            RefreshBufferSizeIfNeeded();
            _pausePressedThisFrame = false;

            PushFrame(1, _player1Source?.ReadFrame() ?? default);
            PushFrame(2, _player2Source?.ReadFrame() ?? default);
        }

        public void OnDestroyManager()
        {
            _player1Source = null;
            _player2Source = null;
            Array.Clear(_player1Buffer, 0, _player1Buffer.Length);
            Array.Clear(_player2Buffer, 0, _player2Buffer.Length);
            _player1WriteIndex = 0;
            _player2WriteIndex = 0;
            _player1SampleCount = 0;
            _player2SampleCount = 0;
            _pausePressedThisFrame = false;
        }

        public PlayerInputFrame GetCurrentFrame(int playerId)
        {
            return GetBufferedFrame(playerId, 0);
        }

        public PlayerInputFrame GetBufferedFrame(int playerId, int framesAgo)
        {
            if (playerId == 1)
            {
                return ReadBuffer(_player1Buffer, _player1WriteIndex, _player1SampleCount, framesAgo);
            }

            if (playerId == 2)
            {
                return ReadBuffer(_player2Buffer, _player2WriteIndex, _player2SampleCount, framesAgo);
            }

            return default;
        }

        public bool ConsumePausePressed()
        {
            bool pausePressed = _pausePressedThisFrame;
            _pausePressedThisFrame = false;
            return pausePressed;
        }

        private void RefreshBufferSizeIfNeeded()
        {
            PrototypeMatchManager matchManager = PrototypeMatchManager.Instance;
            if (matchManager == null || matchManager.Settings == null)
            {
                return;
            }

            int desiredSize = Mathf.Max(1, matchManager.Settings.InputBufferSize);
            if (_player1Buffer != null && _player1Buffer.Length == desiredSize)
            {
                return;
            }

            _player1Buffer = new PlayerInputFrame[desiredSize];
            _player2Buffer = new PlayerInputFrame[desiredSize];
            _player1WriteIndex = 0;
            _player2WriteIndex = 0;
            _player1SampleCount = 0;
            _player2SampleCount = 0;
        }

        private void PushFrame(int playerId, PlayerInputFrame frame)
        {
            if (frame.PausePressed)
            {
                _pausePressedThisFrame = true;
            }

            if (playerId == 1)
            {
                _player1Buffer[_player1WriteIndex] = frame;
                _player1WriteIndex = (_player1WriteIndex + 1) % _player1Buffer.Length;
                _player1SampleCount = Mathf.Min(_player1SampleCount + 1, _player1Buffer.Length);
                return;
            }

            if (playerId == 2)
            {
                _player2Buffer[_player2WriteIndex] = frame;
                _player2WriteIndex = (_player2WriteIndex + 1) % _player2Buffer.Length;
                _player2SampleCount = Mathf.Min(_player2SampleCount + 1, _player2Buffer.Length);
            }
        }

        private static PlayerInputFrame ReadBuffer(PlayerInputFrame[] buffer, int writeIndex, int sampleCount, int framesAgo)
        {
            if (buffer == null || buffer.Length == 0 || sampleCount == 0)
            {
                return default;
            }

            int clampedFramesAgo = Mathf.Clamp(framesAgo, 0, sampleCount - 1);
            int index = writeIndex - 1 - clampedFramesAgo;
            while (index < 0)
            {
                index += buffer.Length;
            }

            return buffer[index % buffer.Length];
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace UMP
{
    public enum PlayerState
    {
        Empty,
        Opening,
        Buffering,
        ImageReady,
        Prepared,
        Playing,
        Paused,
        Stopped,
        EndReached,
        EncounteredError,
        TimeChanged,
        PositionChanged,
        SnapshotTaken
    }

    public class PlayerManagerEvents
    {
        internal class PlayerEvent
        {
            private PlayerState _state;
            private object _arg;

            public PlayerEvent(PlayerState state, object arg)
            {
                _state = state;
                _arg = arg;
            }

            public PlayerState State
            {
                get
                {
                    return _state;
                }
            }

            public object Arg
            {
                get
                {
                    return _arg;
                }
                set
                {
                    _arg = value;
                }
            }

            public float GetFloatArg
            {
                get
                {
                    return (_arg != null && _arg is float) ? (float)_arg : 0f;
                }
            }

            public long GetLongArg
            {
                get
                {
                    return (_arg != null && _arg is long) ? (long)_arg : 0;
                }
            }

            public string GetStringArg
            {
                get
                {
                    return (_arg != null && _arg is string) ? (string)_arg : string.Empty;
                }
            }
        }

        private MonoBehaviour _monoObject;
        private IPlayer _player;
        private Queue<PlayerEvent> _playerEvents;
        private IEnumerator _eventListenerEnum;
        private PlayerState _replaceState;
        private PlayerEvent _replaceEvent;

        internal PlayerManagerEvents(MonoBehaviour monoObject, IPlayer player)
        {
            _monoObject = monoObject;
            _player = player;
            _playerEvents = new Queue<PlayerEvent>();
        }

        private PlayerEvent Event
        {
            get
            {
                return new PlayerEvent(_player.State, _player.StateValue);
            }
        }

        private IEnumerator EventManager()
        {
            while (true)
            {
                var currentEvent = Event;
                if (currentEvent != null && currentEvent.State != PlayerState.Empty)
                    _playerEvents.Enqueue(currentEvent);

                if (_playerEvents.Count <= 0)
                {
                    yield return null;
                    continue;
                }

                CallEvent();
            }
        }

        private void CallEvent()
        {
            var eventValue = _playerEvents.Dequeue();

            if (_replaceState == eventValue.State)
            {
                _replaceState = PlayerState.Empty;
                eventValue = _replaceEvent;
            }

            switch (eventValue.State)
            {
                case PlayerState.Opening:
                    if (_playerOpeningListener != null)
                        _playerOpeningListener();
                    break;

                case PlayerState.Buffering:
                    if (_playerBufferingListener != null)
                        _playerBufferingListener(eventValue.GetFloatArg);

                    break;

                case PlayerState.ImageReady:
                    if (_playerImageReadyListener != null)
                        _playerImageReadyListener((Texture2D)eventValue.Arg);

                    break;

                case PlayerState.Prepared:
                    if (_playerPreparedListener != null)
                    {
                        var videoSize = (Vector2)eventValue.Arg;
                        _playerPreparedListener((int)videoSize.x, (int)videoSize.y);
                    }

                    break;

                case PlayerState.Playing:
                    if (_playerPlayingListener != null)
                        _playerPlayingListener();

                    break;

                case PlayerState.Paused:
                    if (_playerPausedListener != null)
                        _playerPausedListener();

                    break;

                case PlayerState.Stopped:
                    if (_playerStoppedListener != null)
                        _playerStoppedListener();

                    break;

                case PlayerState.EndReached:
                    if (_playerEndReachedListener != null)
                        _playerEndReachedListener();

                    break;

                case PlayerState.EncounteredError:
                    if (_playerEncounteredErrorListener != null)
                        _playerEncounteredErrorListener();

                    break;

                case PlayerState.TimeChanged:
                    if (_playerTimeChangedListener != null && _player.IsReady)
                        _playerTimeChangedListener(eventValue.GetLongArg);

                    break;

                case PlayerState.PositionChanged:
                    if (_playerPositionChangedListener != null && _player.IsReady)
                        _playerPositionChangedListener(eventValue.GetFloatArg);

                    break;

                case PlayerState.SnapshotTaken:
                    if (_playerSnapshotTakenListener != null)
                        _playerSnapshotTakenListener(eventValue.GetStringArg);

                    break;
            }
        }

        private bool IsNativeEvents(object events)
        {
            return events is MediaPlayerStandalone ||
                events is MediaPlayerWebGL;
        }

        internal void SetEvent(PlayerState state)
        {
            _playerEvents.Enqueue(new PlayerEvent(state, null));
        }

        internal void SetEvent(PlayerState state, object arg)
        {
            _playerEvents.Enqueue(new PlayerEvent(state, arg));
        }

        internal void ReplaceEvent(PlayerState replaceState, PlayerState newState, object arg)
        {
            _replaceState = replaceState;
            _replaceEvent = new PlayerEvent(newState, arg);
        }

        public void StartListener()
        {
            _playerEvents.Clear();
            if (_eventListenerEnum != null)
                _monoObject.StopCoroutine(_eventListenerEnum);

            _eventListenerEnum = EventManager();
            _monoObject.StartCoroutine(_eventListenerEnum);
        }

        public void StopListener()
        {
            if (_eventListenerEnum != null)
                _monoObject.StopCoroutine(_eventListenerEnum);

            if (!_monoObject.isActiveAndEnabled)
            {
                _playerEvents.Clear();
                return;
            }

            do
            {
                if (_playerEvents.Count > 0)
                    CallEvent();

                var currentEvent = Event;
                if (currentEvent != null && currentEvent.State != PlayerState.Empty)
                    _playerEvents.Enqueue(currentEvent);
            } while (_playerEvents.Count > 0);
        }

        public void RemoveAllEvents()
        {
            if (_playerOpeningListener != null)
            {
                foreach (Action eh in _playerOpeningListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerOpeningListener -= eh;
                }
            }

            if (_playerBufferingListener != null)
            {
                foreach (Action<float> eh in _playerBufferingListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerBufferingListener -= eh;
                }
            }

            if (_playerImageReadyListener != null)
            {
                foreach (Action<Texture2D> eh in _playerImageReadyListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerImageReadyListener -= eh;
                }
            }

            if (_playerPreparedListener != null)
            {
                foreach (Action<int, int> eh in _playerPreparedListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerPreparedListener -= eh;
                }
            }

            if (_playerPlayingListener != null)
            {
                foreach (Action eh in _playerPlayingListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerPlayingListener -= eh;
                }
            }

            if (_playerPausedListener != null)
            {
                foreach (Action eh in _playerPausedListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerPausedListener -= eh;
                }
            }

            if (_playerStoppedListener != null)
            {
                foreach (Action eh in _playerStoppedListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerStoppedListener -= eh;
                }
            }

            if (_playerEndReachedListener != null)
            {
                foreach (Action eh in _playerEndReachedListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerEndReachedListener -= eh;
                }
            }

            if (_playerEncounteredErrorListener != null)
            {
                foreach (Action eh in _playerEncounteredErrorListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerEncounteredErrorListener -= eh;
                }
            }

            if (_playerTimeChangedListener != null)
            {
                foreach (Action<long> eh in _playerTimeChangedListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerTimeChangedListener -= eh;
                }
            }

            if (_playerPositionChangedListener != null)
            {
                foreach (Action<float> eh in _playerPositionChangedListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerPositionChangedListener -= eh;
                }
            }

            if (_playerSnapshotTakenListener != null)
            {
                foreach (Action<string> eh in _playerSnapshotTakenListener.GetInvocationList())
                {
                    if (!IsNativeEvents(eh.Target))
                        _playerSnapshotTakenListener -= eh;
                }
            }
        }

        public void CopyPlayerEvents(PlayerManagerEvents events)
        {
            RemoveAllEvents();

            foreach (Action eh in events._playerOpeningListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerOpeningListener += eh;
            }

            foreach (Action<float> eh in events._playerBufferingListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerBufferingListener += eh;
            }

            foreach (Action<Texture2D> eh in events._playerImageReadyListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerImageReadyListener += eh;
            }

            foreach (Action<int, int> eh in events._playerPreparedListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerPreparedListener += eh;
            }

            foreach (Action eh in events._playerPlayingListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerPlayingListener += eh;
            }

            foreach (Action eh in events._playerPausedListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerPausedListener += eh;
            }

            foreach (Action eh in events._playerStoppedListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerStoppedListener += eh;
            }

            foreach (Action eh in events._playerEndReachedListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerEndReachedListener += eh;
            }

            foreach (Action eh in events._playerEncounteredErrorListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerEncounteredErrorListener += eh;
            }

            foreach (Action<long> eh in events._playerTimeChangedListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerTimeChangedListener += eh;
            }

            foreach (Action<float> eh in events._playerPositionChangedListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerPositionChangedListener += eh;
            }

            foreach (Action<string> eh in events._playerSnapshotTakenListener.GetInvocationList())
            {
                if (!IsNativeEvents(eh.Target))
                    PlayerSnapshotTakenListener += eh;
            }
        }

        #region Actions
        private event Action _playerOpeningListener;

        public event Action PlayerOpeningListener
        {
            add
            {
                _playerOpeningListener = (Action)Delegate.Combine(_playerOpeningListener, value);
            }
            remove
            {
                if (_playerOpeningListener != null)
                    _playerOpeningListener = (Action)Delegate.Remove(_playerOpeningListener, value);
            }
        }

        private event Action<float> _playerBufferingListener;

        public event Action<float> PlayerBufferingListener
        {
            add
            {
                _playerBufferingListener = (Action<float>)Delegate.Combine(_playerBufferingListener, value);
            }
            remove
            {
                if (_playerBufferingListener != null)
                    _playerBufferingListener = (Action<float>)Delegate.Remove(_playerBufferingListener, value);
            }
        }

        private event Action<Texture2D> _playerImageReadyListener;

        public event Action<Texture2D> PlayerImageReadyListener
        {
            add
            {
                _playerImageReadyListener = (Action<Texture2D>)Delegate.Combine(_playerImageReadyListener, value);
            }
            remove
            {
                if (_playerImageReadyListener != null)
                    _playerImageReadyListener = (Action<Texture2D>)Delegate.Remove(_playerImageReadyListener, value);
            }
        }

        private event Action<int, int> _playerPreparedListener;

        public event Action<int, int> PlayerPreparedListener
        {
            add
            {
                _playerPreparedListener = (Action<int, int>)Delegate.Combine(_playerPreparedListener, value);
            }
            remove
            {
                if (_playerPreparedListener != null)
                    _playerPreparedListener = (Action<int, int>)Delegate.Remove(_playerPreparedListener, value);
            }
        }

        private event Action _playerPlayingListener;

        public event Action PlayerPlayingListener
        {
            add
            {
                _playerPlayingListener = (Action)Delegate.Combine(_playerPlayingListener, value);
            }
            remove
            {
                if (_playerPlayingListener != null)
                    _playerPlayingListener = (Action)Delegate.Remove(_playerPlayingListener, value);
            }
        }

        private event Action _playerPausedListener;

        public event Action PlayerPausedListener
        {
            add
            {
                _playerPausedListener = (Action)Delegate.Combine(_playerPausedListener, value);
            }
            remove
            {
                if (_playerPausedListener != null)
                    _playerPausedListener = (Action)Delegate.Remove(_playerPausedListener, value);
            }
        }

        private event Action _playerStoppedListener;

        public event Action PlayerStoppedListener
        {
            add
            {
                _playerStoppedListener = (Action)Delegate.Combine(_playerStoppedListener, value);
            }
            remove
            {
                if (_playerStoppedListener != null)
                    _playerStoppedListener = (Action)Delegate.Remove(_playerStoppedListener, value);
            }
        }

        private event Action _playerEndReachedListener;

        public event Action PlayerEndReachedListener
        {
            add
            {
                _playerEndReachedListener = (Action)Delegate.Combine(_playerEndReachedListener, value);
            }
            remove
            {
                if (_playerEndReachedListener != null)
                    _playerEndReachedListener = (Action)Delegate.Remove(_playerEndReachedListener, value);
            }
        }

        private event Action _playerEncounteredErrorListener;

        public event Action PlayerEncounteredErrorListener
        {
            add
            {
                _playerEncounteredErrorListener = (Action)Delegate.Combine(_playerEncounteredErrorListener, value);
            }
            remove
            {
                if (_playerEncounteredErrorListener != null)
                    _playerEncounteredErrorListener = (Action)Delegate.Remove(_playerEncounteredErrorListener, value);
            }
        }

        private event Action<long> _playerTimeChangedListener;

        public event Action<long> PlayerTimeChangedListener
        {
            add
            {
                _playerTimeChangedListener = (Action<long>)Delegate.Combine(_playerTimeChangedListener, value);
            }
            remove
            {
                if (_playerTimeChangedListener != null)
                    _playerTimeChangedListener = (Action<long>)Delegate.Remove(_playerTimeChangedListener, value);
            }
        }

        private event Action<float> _playerPositionChangedListener;

        public event Action<float> PlayerPositionChangedListener
        {
            add
            {
                _playerPositionChangedListener = (Action<float>)Delegate.Combine(_playerPositionChangedListener, value);
            }
            remove
            {
                if (_playerPositionChangedListener != null)
                    _playerPositionChangedListener = (Action<float>)Delegate.Remove(_playerPositionChangedListener, value);
            }
        }

        private event Action<string> _playerSnapshotTakenListener;

        public event Action<string> PlayerSnapshotTakenListener
        {
            add
            {
                _playerSnapshotTakenListener = (Action<string>)Delegate.Combine(_playerSnapshotTakenListener, value);
            }
            remove
            {
                if (_playerSnapshotTakenListener != null)
                    _playerSnapshotTakenListener = (Action<string>)Delegate.Remove(_playerSnapshotTakenListener, value);
            }
        }
        #endregion
    }
}
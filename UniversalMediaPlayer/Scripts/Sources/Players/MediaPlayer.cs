using System;
using UnityEngine;

namespace UMP
{
    public class MediaPlayer : IPlayer, IPlayerAudio, IPlayerSpu
    {
        private object _playerObject;
        private IPlayer _player;
        private IPlayerAudio _playerAudio;
        private IPlayerSpu _playerSpu;

        #region Constructors
        /// <summary>
        /// Create new instance of media player object
        /// </summary>
        /// <param name="monoObject">MonoBehaviour instanse</param>
        /// <param name="videoOutputObjects">Objects that will be rendering video output</param>
        public MediaPlayer(MonoBehaviour monoObject, GameObject[] videoOutputObjects) : this(monoObject, videoOutputObjects, null)
        {
        }

        /// <summary>
        /// Create instance of media player object with additional arguments
        /// </summary>
        /// <param name="monoObject">MonoBehaviour instanse</param>
        /// <param name="videoOutputObjects">Objects that will be rendering video output</param>
        /// <param name="options">Additional player options</param>
        public MediaPlayer(MonoBehaviour monoObject, GameObject[] videoOutputObjects, PlayerOptions options)
        {
            var supportedPlatform = UMPSettings.RuntimePlatform;

            switch (supportedPlatform)
            {
                case UMPSettings.Platforms.Win:
                case UMPSettings.Platforms.Mac:
                case UMPSettings.Platforms.Linux:
                    PlayerOptionsStandalone standaloneOptions = null;
                    if (options is PlayerOptionsStandalone)
                        standaloneOptions = options as PlayerOptionsStandalone;
                    else
                        standaloneOptions = new PlayerOptionsStandalone(null);

                    _playerObject = new MediaPlayerStandalone(monoObject, videoOutputObjects, standaloneOptions);
                    break;
                /*
                case UMPSettings.Platforms.iOS:
                    PlayerOptionsIPhone iphoneOptions = null;
                    if (options is PlayerOptionsIPhone)
                        iphoneOptions = options as PlayerOptionsIPhone;
                    else
                        iphoneOptions = new PlayerOptionsIPhone(null);

                    _playerObject = new MediaPlayerIPhone(monoObject, videoOutputObjects, iphoneOptions);
                    break;

                case UMPSettings.Platforms.Android:
                    PlayerOptionsAndroid androidOptions = null;
                    if (options is PlayerOptionsAndroid)
                        androidOptions = options as PlayerOptionsAndroid;
                    else
                        androidOptions = new PlayerOptionsAndroid(null);

                    _playerObject = new MediaPlayerAndroid(monoObject, videoOutputObjects, androidOptions);
                    break;
                    */
                case UMPSettings.Platforms.WebGL:
                    _playerObject = new MediaPlayerWebGL(monoObject, videoOutputObjects, options);
                    break;
            }

            if (_playerObject is IPlayer)
                _player = (_playerObject as IPlayer);

            if (_playerObject is IPlayerAudio)
                _playerAudio = (_playerObject as IPlayerAudio);

            if (_playerObject is IPlayerSpu)
                _playerSpu = (_playerObject as IPlayerSpu);

        }

        /// <summary>
        /// Create new instance of MediaPlayer object from another MediaPlayer instance
        /// </summary>
        /// <param name="monoObject">MonoBehaviour instanse</param>
        /// <param name="basedPlayer">Based on MediaPlayer instance</param>
        public MediaPlayer(MonoBehaviour monoObject, MediaPlayer basedPlayer) : this(monoObject, basedPlayer.VideoOutputObjects, basedPlayer.Options)
        {
            if (basedPlayer.DataSource != null && string.IsNullOrEmpty(basedPlayer.DataSource.ToString()))
                _player.DataSource = basedPlayer.DataSource;
            
            _player.EventManager.CopyPlayerEvents(basedPlayer.EventManager);
            _player.Mute = basedPlayer.Mute;
            _player.Volume = basedPlayer.Volume;
            _player.PlaybackRate = basedPlayer.PlaybackRate;
        }
        #endregion

        /// <summary>
        /// Get media player object for current running platform 
        /// (supported: Standalone, WebGL, Android and iOS platforms)
        /// for get more additional possibilities that exists only for this platform.
        /// </summary>
        public object PlatformPlayer
        {
            get
            {
                return _playerObject;
            }
        }

        /// <summary>
        /// Get/Set simple array that consist with Unity 'GameObject' that have 'Mesh Renderer' (with some material)
        /// or 'Raw Image' component
        /// </summary>
        public GameObject[] VideoOutputObjects
        {
            get
            {
                return _player.VideoOutputObjects;
            }
            set
            {
                _player.VideoOutputObjects = value;
            }
        }

        /// <summary>
        /// Get event manager for current media player to add possibility to attach/detach special playback listeners
        /// </summary>
        public PlayerManagerEvents EventManager
        {
            get
            {
                return _player.EventManager;
            }
        }

        /// <summary>
        /// Get player additional options for current running platform
        /// </summary>
        public PlayerOptions Options
        {
            get
            {
                return _player.Options;
            }
        }

        /// <summary>
        /// Get current video playback state
        /// </summary>
        public PlayerState State
        {
            get
            {
                return _player.State;
            }
        }

        /// <summary>
        /// Get current video playback state value (can be float, long or string type)
        /// </summary>
        public object StateValue
        {
            get
            {
                return _player.StateValue;
            }
        }

        /// <summary>
        /// Add new main group of listeners to current media player instance
        /// </summary>
        /// <param name="listener">Group of listeners</param>
        public void AddMediaListener(IMediaListener listener)
        {
            _player.AddMediaListener(listener);
        }

        /// <summary>
        /// Remove group of listeners from current media player instance
        /// </summary>
        /// <param name="listener">Group of listeners</param>
        public void RemoveMediaListener(IMediaListener listener)
        {
            _player.RemoveMediaListener(listener);
        }

        /// <summary>
        /// Prepare current video playback
        /// </summary>
        public void Prepare()
        {
            _player.Prepare();
        }

        /// <summary>
        /// Play or resume (True if playback started (and was already started), or False on error.
        /// </summary>
        /// <returns></returns>
        public bool Play()
        {
            return _player.Play();
        }

        /// <summary>
        /// Toggle pause current video playback (no effect if there is no media)
        /// </summary>
        public void Pause()
        {
            _player.Pause();
        }

        /// <summary>
        /// Stop current video playback (no effect if there is no media)
        /// </summary>
        /// <param name="resetTexture">Clear the last frame</param>
        public void Stop(bool resetTexture)
        {
            _player.Stop(resetTexture);
        }

        /// <summary>
        /// Stop current video playback (no effect if there is no media)
        /// </summary>
        public void Stop()
        {
            Stop(true);
        }

        /// <summary>
        /// Release a current media player instance
        /// </summary>
        public void Release()
        {
            _player.Release();
        }

        /// <summary>
        /// Get/Set local path or url link to your video/audio file/stream
        /// Example of using:
        /// Local storage space - 'file:///C:\MyFolder\Videos\video1.mp4' or 
        /// 'C:\MyFolder\Videos\video1.mp4' or 
        /// 'file:///DCIM/100ANDRO/MyVideo.mp4' (example for Android platform);
        /// Remote space (streams) - 'rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov';
        /// 'StreamingAssets' folder - 'file:///myVideoFile.mp4';
        /// </summary>
        public string DataSource
        {
            get
            {
                return _player.DataSource;
            }
            set
            {
                _player.DataSource = value;
            }
        }

        /// <summary>
        /// Is media is currently playing
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return _player.IsPlaying;
            }
        }

        /// <summary>
        /// Is media is ready to play (first frame available)
        /// </summary>
        public bool IsReady
        {
            get
            {
                return _player.IsReady;
            }
        }

        /// <summary>
        /// Is the player able to play
        /// </summary>
        public bool AbleToPlay
        {
            get
            {
                return _player.AbleToPlay;
            }
        }

        /// <summary>
        /// Get the current video length (in milliseconds)
        /// </summary>
        public long Length
        {
            get
            {
                return _player.Length;
            }
        }

        /// <summary>
        /// Get the current video formatted length (hh:mm:ss[:ms]).
        /// </summary>
        /// <param name="detail">True: formatted length will be with [:ms]</param>
        public string GetFormattedLength(bool detail)
        {
            return _player.GetFormattedLength(detail);
        }

        /// <summary>
        /// Get frames per second (fps) for current video playback.
        /// * Warning: it's not a predefined value from video file/stream - calculated in video playback process
        /// </summary>
        public float FrameRate
        {
            get { return _player.FrameRate; }
        }

        /// <summary>
        /// Get video frames counter
        /// </summary>
        public int FramesCounter
        {
            get { return _player.FramesCounter; }
        }

        /// <summary>
        /// Get pixels of current video frame
        /// Example of using:
        /// texture.LoadRawTextureData(_player.FramePixels);
        /// texture.Apply();
        /// </summary>
        public byte[] FramePixels
        {
            get
            {
                return _player.FramePixels;
            }
        }

        /// <summary>
        /// Get/Set the current video time (in milliseconds). 
        /// This has no effect if no media is being played. 
        /// Not all formats and protocols support this
        /// </summary>
        public long Time
        {
            get
            {
                return _player.Time;
            }
            set
            {
                _player.Time = value;
            }
        }

        /// <summary>
        /// Get/Set video position.
        /// This has no effect if playback is not enabled.
        /// This might not work depending on the underlying input format and protocol
        /// </summary>
        public float Position
        {
            get
            {
                return _player.Position;
            }
            set
            {
                _player.Position = value;
            }
        }

        /// <summary>
        /// Get/Set the requested video play rate
        /// </summary>
        public float PlaybackRate
        {
            get
            {
                return _player.PlaybackRate;
            }
            set
            {
                _player.PlaybackRate = value;
            }
        }

        /// <summary>
        /// Get/Set current software audio volume 
        /// (by default you can change this value from '0' to '100')
        /// </summary>
        public int Volume
        {
            get
            {
                return _player.Volume;
            }
            set
            {
                _player.Volume = value;
            }
        }

        /// <summary>
        /// Get/Set mute status for current video playback
        /// </summary>
        public bool Mute
        {
            get
            {
                return _player.Mute;
            }
            set
            {
                _player.Mute = value;
            }
        }

        /// <summary>
        /// Get current video width in pixels
        /// </summary>
        public int VideoWidth
        {
            get
            {
                return _player.VideoWidth;
            }
        }

        /// <summary>
        /// Get current video height in pixels
        /// </summary>
        public int VideoHeight
        {
            get
            {
                return _player.VideoHeight;
            }
        }

        /// <summary>
        /// Get the pixel dimensions of current video
        /// </summary>
        public Vector2 VideoSize
        {
            get
            {
                return new Vector2(VideoWidth, VideoHeight);
            }
        }

        /// <summary>
        /// Get the available audio tracks
        /// </summary>
        public MediaTrackInfo[] AudioTracks
        {
            get
            {
                if (_playerAudio != null)
                    return _playerAudio.AudioTracks;

                return null;
            }
        }

        /// <summary>
        /// Get/Set the current audio track
        /// </summary>
        public MediaTrackInfo AudioTrack
        {
            get
            {
                if (_playerAudio != null)
                    return _playerAudio.AudioTrack;

                return null;
            }
            set
            {
                if (_playerAudio != null)
                    _playerAudio.AudioTrack = value;
            }
        }

        /// <summary>
        /// Get the available subtitle tracks
        /// </summary>
        public MediaTrackInfo[] SpuTracks
        {
            get
            {
                if (_playerSpu != null)
                    return _playerSpu.SpuTracks;

                return null;
            }
        }

        /// <summary>
        /// Get/Set the current subtitle track (supported only on Standalone platform)
        /// </summary>
        public MediaTrackInfo SpuTrack
        {
            get
            {
                if (_playerSpu != null)
                    return _playerSpu.SpuTrack;

                return null;
            }
            set
            {
                if (_playerSpu != null)
                    _playerSpu.SpuTrack = value;
            }
        }

        /// <summary>
        /// Set new video subtitle file
        /// </summary>
        /// <param name="path">Path to the new video subtitle file</param>
        /// <returns></returns>
        public bool SetSubtitleFile(Uri path)
        {
            if (_playerSpu != null)
                return _playerSpu.SetSubtitleFile(path);

            return false;
        }
    }
}

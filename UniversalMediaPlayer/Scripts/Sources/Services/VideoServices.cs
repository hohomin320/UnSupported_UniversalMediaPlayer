using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UMP.Services.Youtube;
using UnityEngine;

namespace UMP.Services
{
    public class VideoServices
    {
        private MonoBehaviour _monoObject;
        private List<YoutubeService> _services;
        private IEnumerator _getVideosEnum;

        public VideoServices(MonoBehaviour monoObject)
        {
            _monoObject = monoObject;
            _services = new List<YoutubeService>();
            _services.Add(new YoutubeService(monoObject));
        }

        public bool ValidUrl(string url)
        {
            var isValid = false;

            foreach (var service in _services)
            {
                if (service.ValidUrl(url))
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        public IEnumerator GetVideos(string url, Action<List<Video>> resultCallback, Action<string> errorCallback)
        {
            foreach (var service in _services)
            {
                if (service.ValidUrl(url))
                {
                    _getVideosEnum = service.GetAllVideos(url, resultCallback, errorCallback);
                    yield return _monoObject.StartCoroutine(_getVideosEnum);
                }
            }
        }

        public static Video FindVideo(List<Video> videos, int maxResolution, int maxAudioBitrate = -1)
        {
            Video result = null;

            if (videos != null && videos.Count > 0)
            {
                result = videos[0];
                var ytVideos = new List<YoutubeVideo>();

                try
                {
                    ytVideos = videos.Cast<YoutubeVideo>().ToList();
                }
                catch (Exception e) { e.ToString(); }

                if (ytVideos.Count > 0)
                {
                    ytVideos = ytVideos.FindAll((video) => {
                            if (maxAudioBitrate < 0)
                                return video.Resolution <= maxResolution;
                            else
                                return video.Resolution <= maxResolution && video.AudioBitrate >= 0 && video.AudioBitrate <= maxAudioBitrate;
                    });

                    var orderedVideos = from video in ytVideos orderby video.Resolution, video.AudioBitrate select video;
                    result = orderedVideos.LastOrDefault();
                }
            }

            return result;
        }
    }
}

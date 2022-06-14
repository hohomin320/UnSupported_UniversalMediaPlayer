using Newtonsoft.Json.Linq;
using Services.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UMP.Services.Helpers;
using UnityEngine;
using UnityEngine.Networking;
using VideoLibrary.Exceptions;

namespace UMP.Services.Youtube
{
    public class YoutubeService : ServiceBase<YoutubeVideo>
    {
        private const string Playback = "videoplayback";
        private static string _signatureKey;
        private string[] _signatures = { "youtu.be/", "www.youtube", "youtube.com/embed/" };
        MonoBehaviour _mono;
        public static YoutubeService Default { get; }// = new YoutubeService();
        public const string YoutubeUrl = "https://youtube.com/";

        public YoutubeService(MonoBehaviour mono)
        {
            _mono = mono;
        }

        public override bool ValidUrl(string url)
        {
            foreach (var signature in _signatures)
            {
                if (url.Contains(signature))
                    return true;
            }

            return false;
        }

        public IEnumerator GetAllVideos(string url, Action<List<Video>> resultCallback, Action<string> errorCallback = null)
        {
            if (!TryNormalize(url, out url))
                throw new ArgumentException("URL is not a valid Youtube URL!");

            var requestText = string.Empty;
#if UNITY_2017_2_OR_NEWER
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", string.Empty);
            yield return request.SendWebRequest();
#else
            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", string.Empty);
            var request = new WWW(url, null, headers);
            yield return request;
#endif

            if (!string.IsNullOrEmpty(request.error))
            {
                errorCallback(string.Format("[YouTubeService.GetAllVideos] url request is failed: {0}", request.error));
                yield break;
            }

#if UNITY_2017_2_OR_NEWER
            requestText = request.downloadHandler.text;
#else
            requestText = request.text;
#endif
            var ytVideos = new List<YoutubeVideo>();
            yield return _mono.StartCoroutine(ParseVideos(requestText, (videos) =>
            {
                var orderedVideos = from video in videos orderby video.Resolution, video.AudioBitrate select video;
                ytVideos = orderedVideos.ToList();
            }, errorCallback));

            if (resultCallback != null)
                resultCallback(ytVideos.Cast<Video>().ToList());
        }

        private bool TryNormalize(string videoUri, out string normalized)
        {
            normalized = null;

            var builder = new StringBuilder(videoUri);

            videoUri = builder.Replace("youtu.be/", "youtube.com/watch?v=")
                .Replace("youtube.com/embed/", "youtube.com/watch?v=")
                .Replace("/v/", "/watch?v=")
                .Replace("/watch#", "/watch?")
                .Replace("youtube.com/shorts/", "youtube.com/watch?v=")
                .ToString();

            var query = new Query(videoUri);
            var value = string.Empty;

            if (!query.TryGetValue("v", out value))
                return false;

            normalized = $"{YoutubeUrl}watch?v={value}";
            return true;
        }


        private  IEnumerator ParseVideos(string source, Action<List<YoutubeVideo>> resultCallback, Action<string> errorCallback = null)
        {
            var videos = new List<YoutubeVideo>();
            IEnumerable<UnscrambledQuery> queries;
            string jsPlayer = ParseJsPlayer(source);
            if (jsPlayer == null)
            {
                yield break;
            }

            var playerResponseJson = JToken.Parse(Json.Extract(ParsePlayerJson(source)));
            if (string.Equals(playerResponseJson.SelectToken("playabilityStatus.status")?.Value<string>(), "error", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnavailableStreamException($"Video has unavailable stream.");
            }
            var errorReason = playerResponseJson.SelectToken("playabilityStatus.reason")?.Value<string>();
            if (string.IsNullOrWhiteSpace(errorReason))
            {
                var isLiveStream = playerResponseJson.SelectToken("videoDetails.isLive")?.Value<bool>() == true;
                var videoInfo = new VideoInfo(
                    playerResponseJson.SelectToken("videoDetails.title")?.Value<string>(),
                    playerResponseJson.SelectToken("videoDetails.lengthSeconds")?.Value<int>(),
                    playerResponseJson.SelectToken("videoDetails.author")?.Value<string>());

                if (isLiveStream)
                {
                    throw new UnavailableStreamException($"This is live stream so unavailable stream.");
                }
                // url_encoded_fmt_stream_map
                string map = Json.GetKey("url_encoded_fmt_stream_map", source);
                if (!string.IsNullOrWhiteSpace(map))
                {
                    queries = map.Split(',').Select(Unscramble);
                    foreach (var query in queries)
                        videos.Add(new YoutubeVideo(videoInfo, query, jsPlayer));
                }
                else // player_response
                {
                    List<JToken> streamObjects = new List<JToken>();
                    // Extract Muxed streams
                    var streamFormat = playerResponseJson.SelectToken("streamingData.formats");
                    if (streamFormat != null)
                    {
                        streamObjects.AddRange(streamFormat.ToArray());
                    }
                    // Extract AdaptiveFormat streams
                    var streamAdaptiveFormats = playerResponseJson.SelectToken("streamingData.adaptiveFormats");
                    if (streamAdaptiveFormats != null)
                    {
                        streamObjects.AddRange(streamAdaptiveFormats.ToArray());
                    }

                    foreach (var item in streamObjects)
                    {
                        var urlValue = item.SelectToken("url")?.Value<string>();
                        if (!string.IsNullOrEmpty(urlValue))
                        {
                            var query = new UnscrambledQuery(urlValue, false);
                            videos.Add(new YoutubeVideo(videoInfo, query, jsPlayer));
                            continue;
                        }
                        var cipherValue = ((item.SelectToken("cipher") ?? item.SelectToken("signatureCipher")) ?? string.Empty).Value<string>();
                        if (!string.IsNullOrEmpty(cipherValue))
                        {
                            videos.Add(new YoutubeVideo(videoInfo, Unscramble(cipherValue), jsPlayer));
                        }
                    }
                }
                // adaptive_fmts
                string adaptiveMap = Json.GetKey("adaptive_fmts", source);
                if (!string.IsNullOrWhiteSpace(adaptiveMap))
                {
                    queries = adaptiveMap.Split(',').Select(Unscramble);
                    foreach (var query in queries)
                        videos.Add(new YoutubeVideo(videoInfo, query, jsPlayer));
                }
                else
                {
                    // dashmpd
                    string dashmpdMap = Json.GetKey("dashmpd", source);
                    if (!string.IsNullOrWhiteSpace(adaptiveMap))
                    {
                        using (HttpClient hc = new HttpClient())
                        {
                            IEnumerable<string> uris = null;
                            try
                            {

                                dashmpdMap = WebUtility.UrlDecode(dashmpdMap).Replace(@"\/", "/");

                                var manifest = hc.GetStringAsync(dashmpdMap)
                                    .GetAwaiter().GetResult()
                                    .Replace(@"\/", "/");

                                uris = Html.GetUrisFromManifest(manifest);
                            }
                            catch (Exception e)
                            {
                                throw new UnavailableStreamException(e.Message);
                            }

                            if (uris != null)
                            {
                                foreach (var v in uris)
                                {
                                    videos.Add(new YoutubeVideo(videoInfo, UnscrambleManifestUri(v), jsPlayer));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (errorCallback != null)
                    errorCallback(new UnavailableStreamException($"Error caused by Youtube.({errorReason}))").ToString());
                //throw new UnavailableStreamException($"Error caused by Youtube.({errorReason}))");
            }


            if (resultCallback != null)
                resultCallback(videos);
        }

        //todo ParsePlayerJson
        private string ParsePlayerJson(string source)
        {
            string playerResponseMap = null, ytInitialPlayerPattern = @"\s*var\s*ytInitialPlayerResponse\s*=\s*(\{\""responseContext\"".*\});", ytWindowInitialPlayerResponse = @"\[\""ytInitialPlayerResponse\""\]\s*=\s*(\{.*\});", ytPlayerPattern = @"ytplayer\.config\s*=\s*(\{\"".*\""\}\});";
            Match match;
            if ((match = Regex.Match(source, ytPlayerPattern)).Success && Json.TryGetKey("player_response", match.Groups[1].Value, out string json))
            {
                playerResponseMap = Regex.Unescape(json);
            }
            if (string.IsNullOrWhiteSpace(playerResponseMap) && (match = Regex.Match(source, ytInitialPlayerPattern)).Success)
            {
                playerResponseMap = match.Groups[1].Value;
            }
            if (string.IsNullOrWhiteSpace(playerResponseMap) && (match = Regex.Match(source, ytWindowInitialPlayerResponse)).Success)
            {
                playerResponseMap = match.Groups[1].Value;
            }
            if (string.IsNullOrWhiteSpace(playerResponseMap))
            {
                throw new UnavailableStreamException("Player json has no found.");
            }
            return playerResponseMap.Replace(@"\u0026", "&").Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\\&", "\\\\&");
        }

        internal override async Task<IEnumerable<YoutubeVideo>> GetAllVideosAsync(string videoUri, Func<string, Task<string>> sourceFactory, Action<List<Video>> resultCallback = null, Action<string> errorCallback = null)
        {
            if (!TryNormalize(videoUri, out videoUri))
                throw new ArgumentException("URL is not a valid YouTube URL!");

            string source = await
                sourceFactory(videoUri)
                .ConfigureAwait(false);

            var ytVideos = new List<YoutubeVideo>();
             ParseVideos(source, (videos) => {
                var orderedVideos = from video in videos orderby video.Resolution, video.AudioBitrate select video;
                ytVideos = orderedVideos.ToList();
            }, errorCallback);

            if (resultCallback != null)
                resultCallback(ytVideos.Cast<Video>().ToList());

            return ytVideos;
        }

        internal override async Task<IEnumerable<YoutubeVideo>> GetAllVideosAsync(string videoUri, Func<string, Task<string>> sourceFactory)
        {
            if (!TryNormalize(videoUri, out videoUri))
                throw new ArgumentException("URL is not a valid YouTube URL!");

            string source = await
                sourceFactory(videoUri)
                .ConfigureAwait(false);

            var ytVideos = new List<YoutubeVideo>();
             ParseVideos(source, (videos) => {
                var orderedVideos = from video in videos orderby video.Resolution, video.AudioBitrate select video;
                ytVideos = orderedVideos.ToList();
            }, null);

            return ytVideos;
        }








        private string ParseJsPlayer(string source)
        {
            if (Json.TryGetKey("jsUrl", source, out var jsPlayer) || Json.TryGetKey("PLAYER_JS_URL", source, out jsPlayer))
            {
                jsPlayer = jsPlayer.Replace(@"\/", "/");
            }
            else
            {
                // Alternative solution
                Match match = Regex.Match(source, "<script\\s*src=\"([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)\".*name=\"player_ias/base\".*>\\s*</script>");
                if (match.Success)
                {
                    jsPlayer = match.Groups[1].Value.Replace(@"\/", "/");
                }
                else
                {
                    return null;
                }
            }

            if (jsPlayer.StartsWith("/yts") || jsPlayer.StartsWith("/s"))
            {
                return $"https://www.youtube.com{jsPlayer}";
            }

            // Fall back on old implementation (not sure it's needed)
            if (!jsPlayer.StartsWith("http"))
            {
                jsPlayer = $"https:{jsPlayer}";
            }

            return jsPlayer;
        }

        // TODO: Consider making this static...
        private UnscrambledQuery Unscramble(string queryString)
        {
            queryString = queryString.Replace(@"\u0026", "&");
            var query = new Query(queryString);
            string uri = query["url"];

            query.TryGetValue("sp", out _signatureKey);

            bool encrypted = false;
            string signature;

            if (query.TryGetValue("s", out signature))
            {
                encrypted = true;
                uri += GetSignatureAndHost(GetSignatureKey(), signature, query);
            }
            else if (query.TryGetValue("sig", out signature))
                uri += GetSignatureAndHost(GetSignatureKey(), signature, query);

            uri = WebUtility.UrlDecode(
                WebUtility.UrlDecode(uri));

            var uriQuery = new Query(uri);

            if (!uriQuery.ContainsKey("ratebypass"))
                uri += "&ratebypass=yes";

            return new UnscrambledQuery(uri, encrypted);
        }
        private string GetSignatureAndHost(string key, string signature, Query query)
        {
            string result = $"&{key}={signature}";

            string host;

            if (query.TryGetValue("fallback_host", out host))
                result += "&fallback_host=" + host;

            return result;
        }

        private UnscrambledQuery UnscrambleManifestUri(string manifestUri)
        {
            var start = manifestUri.IndexOf(Playback) + Playback.Length;
            var baseUri = manifestUri.Substring(0, start);
            var parametersString = manifestUri.Substring(start, manifestUri.Length - start);
            var parameters = parametersString.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder(baseUri);
            builder.Append("?");

            for (var i = 0; i < parameters.Length; i += 2)
            {
                builder.Append(parameters[i]);
                builder.Append('=');
                builder.Append(parameters[i + 1].Replace("%2F", "/"));

                if (i < parameters.Length - 2)
                    builder.Append('&');
            }

            return new UnscrambledQuery(builder.ToString(), false);
        }
        
        public static string GetSignatureKey()
        {
            return string.IsNullOrWhiteSpace(_signatureKey) ? "signature" : _signatureKey;
        }

    }
}

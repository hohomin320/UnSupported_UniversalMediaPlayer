using Services.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VideoLibrary;

namespace UMP.Services.Youtube
{
    public partial class YoutubeVideo
    {
        private static readonly Regex DefaultDecryptionFunctionRegex = new Regex(@"\bc\s*&&\s*d\.set\([^,]+\s*,\s*\([^)]*\)\s*\(\s*([a-zA-Z0-9$]+)\(");

        public IEnumerator Decrypt(string uri = null, Action<string> errorCallback = null)
        {
            if (_encrypted)
            {
                var query = new Query(this.uri);
                var signature = string.Empty;

                if (!query.TryGetValue("signature", out signature))
                    yield break;

                var requestText = string.Empty;
#if UNITY_2017_2_OR_NEWER
                var request = UnityWebRequest.Get(jsPlayerUrl);
                yield return request.SendWebRequest();
#else
                var request = new WWW(_jsPlayer);
                yield return request;
#endif

                try
                {
                    if (!string.IsNullOrEmpty(request.error))
                        throw new Exception(string.Format("[YouTubeVideo.Decrypt] jsPlayer request is failed: {0}", request.error));

#if UNITY_2017_2_OR_NEWER
                    requestText = request.downloadHandler.text;
#else
                    requestText = request.text;
#endif
                    var decryptFunction = UMPSettings.Instance.YoutubeDecryptFunction;
                    query[YoutubeService.GetSignatureKey()] = DecryptSignature(jsPlayer, signature);
                    this.uri = query.ToString();
                    _encrypted = false;
                }
                catch (Exception error)
                {
                    if (errorCallback != null)
                        errorCallback(error.ToString());
                }
            }
        }

        private async Task<string> DecryptAsync(string uri, Func<DelegatingClient> makeClient)
        {
            var query = new Query(uri);

            string signature;
            if (!query.TryGetValue(YoutubeService.GetSignatureKey(), out signature))
                return uri;

            if (string.IsNullOrWhiteSpace(signature))
                throw new Exception("Signature not found.");

            if (jsPlayer == null)
            {
                jsPlayer = await makeClient()
                    .GetStringAsync(jsPlayerUrl)
                    .ConfigureAwait(false);
            }

            query[YoutubeService.GetSignatureKey()] = DecryptSignature(jsPlayer, signature);
            return query.ToString();
        }

        private string DecryptSignature(string js, string signature)
        {
            var functionNameRegex = new Regex(@"\w+(?:.|\[)(\""?\w+(?:\"")?)\]?\(");
            var functionLines = GetDecryptionFunctionLines(js);
            var decryptor = new Decryptor();
            var decipherDefinitionName = Regex.Match(string.Join(";", functionLines), "([\\$_\\w]+).\\w+\\(\\w+,\\d+\\);").Groups[1].Value;
            if (string.IsNullOrEmpty(decipherDefinitionName))
            {
                throw new Exception("Could not find signature decipher definition name. Please report this issue to us.");
            }

            var decipherDefinitionBody = Regex.Match(js, $@"var\s+{Regex.Escape(decipherDefinitionName)}=\{{(\w+:function\(\w+(,\w+)?\)\{{(.*?)\}}),?\}};", RegexOptions.Singleline).Groups[0].Value;
            if (string.IsNullOrEmpty(decipherDefinitionBody))
            {
                throw new Exception("Could not find signature decipher definition body. Please report this issue to us.");
            }
            foreach (var functionLine in functionLines)
            {
                if (decryptor.IsComplete)
                {
                    break;
                }

                var match = functionNameRegex.Match(functionLine);
                if (match.Success)
                {
                    decryptor.AddFunction(decipherDefinitionBody, match.Groups[1].Value);
                }
            }

            foreach (var functionLine in functionLines)
            {
                var match = functionNameRegex.Match(functionLine);
                if (match.Success)
                {
                    signature = decryptor.ExecuteFunction(signature, functionLine, match.Groups[1].Value);
                }
            }

            return signature;
        }

        private string[] GetDecryptionFunctionLines(string js)
        {
            var decryptionFunction = GetDecryptionFunction(js);
            var match = Regex.Match(js, string.Format(@"(?!h\.){0}=function\(\w+\)\{{(.*?)\}}", Regex.Escape(decryptionFunction)), RegexOptions.Singleline);

            if (!match.Success)
                throw new Exception("[YouTubeVideo.Decrypt] GetDecryptionFunctionLines failed");

            return match.Groups[1].Value.Split(';');
        }

        private string GetDecryptionFunction(string js)
        {
            var decryptionFunctionRegex = DefaultDecryptionFunctionRegex;

            var match = decryptionFunctionRegex.Match(js);

            if (!match.Success)
                throw new Exception("[YouTubeVideo.Decrypt] GetDecryptionFunction failed");

            return match.Groups[1].Value;
        }
        private class Decryptor
        {
            private static readonly Regex ParametersRegex = new Regex(@"\(\w+,(\d+)\)");

            private readonly Dictionary<string, FunctionType> _functionTypes = new Dictionary<string, FunctionType>();
            private readonly StringBuilder _stringBuilder = new StringBuilder();

            public bool IsComplete =>
                _functionTypes.Count == Enum.GetValues(typeof(FunctionType)).Length;

            public void AddFunction(string js, string function)
            {
                var escapedFunction = Regex.Escape(function);
                FunctionType? type = null;
                /* Pass  "do":function(a){} or xa:function(a,b){} */
                if (Regex.IsMatch(js, $@"(\"")?{escapedFunction}(\"")?:\bfunction\b\([a],b\).(\breturn\b)?.?\w+\."))
                {
                    type = FunctionType.Slice;
                }
                else if (Regex.IsMatch(js, $@"(\"")?{escapedFunction}(\"")?:\bfunction\b\(\w+\,\w\).\bvar\b.\bc=a\b"))
                {
                    type = FunctionType.Swap;
                }
                if (Regex.IsMatch(js, $@"(\"")?{escapedFunction}(\"")?:\bfunction\b\(\w+\){{\w+\.reverse"))
                {
                    type = FunctionType.Reverse;
                }
                if (type.HasValue)
                {
                    _functionTypes[function] = type.Value;
                }
            }

            public string ExecuteFunction(string signature, string line, string function)
            {
                if (!_functionTypes.TryGetValue(function, out var type))
                {
                    return signature;
                }

                switch (type)
                {
                    case FunctionType.Reverse:
                        return Reverse(signature);
                    case FunctionType.Slice:
                    case FunctionType.Swap:
                        var index =
                            int.Parse(
                                ParametersRegex.Match(line).Groups[1].Value,
                                NumberStyles.AllowThousands,
                                NumberFormatInfo.InvariantInfo);
                        return
                            type == FunctionType.Slice
                            ? Slice(signature, index)
                            : Swap(signature, index);
                    default:
                        throw new ArgumentOutOfRangeException($"[YouTubeVideo.Decryptor] {type}");
                }
            }

            private string Reverse(string signature)
            {
                //_stringBuilder.Clear();
                _stringBuilder.Remove(0, _stringBuilder.Length);

                for (var index = signature.Length - 1; index >= 0; index--)
                {
                    _stringBuilder.Append(signature[index]);
                }

                return _stringBuilder.ToString();
            }

            private string Slice(string signature, int index) =>
                signature.Substring(index);
            private string Swap(string signature, int index)
            {
                //_stringBuilder.Clear();
                _stringBuilder.Remove(0, _stringBuilder.Length);

                _stringBuilder.Append(signature);
                _stringBuilder[0] = signature[index];
                _stringBuilder[index] = signature[0];
                return _stringBuilder.ToString();
            }

            private enum FunctionType
            {
                Reverse,
                Slice,
                Swap
            }
        }
    }
}

using Services.Helpers;
using System;
using System.Collections;
using System.Threading.Tasks;
using UMP.Services.Helpers;
using VideoLibrary;

namespace UMP.Services.Youtube
{
    public partial class YoutubeVideo : Video
    {
        private readonly string jsPlayerUrl;
        private string jsPlayer;
        private string uri;
        private readonly Query _uriQuery;
        private bool _encrypted;
        private bool _needNDescramble;

        internal YoutubeVideo(string title, UnscrambledQuery query, string jsPlayerUrl)
        {
            Title = title;
            uri = query.Uri;
            this.jsPlayerUrl = jsPlayerUrl;
            _encrypted = query.IsEncrypted;
            FormatCode = int.Parse(new Query(uri)["itag"]);
        }

        internal YoutubeVideo(VideoInfo info, UnscrambledQuery query, string jsPlayerUrl)
        {
            this.Info = info;
            this.Title = info?.Title;
            this.uri = query.Uri;
            this._uriQuery = new Query(uri);
            this.jsPlayerUrl = jsPlayerUrl;
            this._encrypted = query.IsEncrypted;
            this._needNDescramble = _uriQuery.ContainsKey("n");
            this.FormatCode = int.Parse(_uriQuery["itag"]);
        }

        public override string Title { get; }

        public override VideoInfo Info { get; }

        public override WebSites WebSite => WebSites.YouTube;

        public override string Uri => GetUriAsync().GetAwaiter().GetResult();

        public string GetUri(Func<DelegatingClient> makeClient) => GetUriAsync(makeClient).GetAwaiter().GetResult();

        public override Task<string> GetUriAsync() => GetUriAsync(() => new DelegatingClient());

        public async Task<string> GetUriAsync(Func<DelegatingClient> makeClient)
        {
            if (_encrypted)
            {
                uri = await DecryptAsync(uri, makeClient).ConfigureAwait(false);
                _encrypted = false;
            }

            if (_needNDescramble)
            {
                //uri = await NDescrambleAsync(uri, makeClient).ConfigureAwait(false);
                _needNDescramble = false;
            }

            return uri;
        }

        public int FormatCode { get; }

        public bool IsEncrypted
        {
            get { return _encrypted; }
        }

        public override string ToString()
        {
            return string.Format("{0}, Resolution: {1}, AudioBitrate: {2}, Is3D: {3}", base.ToString(), Resolution, AudioBitrate, Is3D);
        }
    }
}

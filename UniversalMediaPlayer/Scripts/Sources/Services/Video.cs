using System.IO;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace UMP.Services
{
    public abstract class Video
    {
        /// <summary>
        /// Gets the video title.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Gets the download URL.
        /// </summary>
        public abstract string Uri { get; }
        public abstract VideoInfo Info { get; }
        public abstract WebSites WebSite { get; }

        /// <summary>
        /// Gets the video type (container).
        /// </summary>
        public virtual VideoFormat VideoFormat
        {
            get { return VideoFormat.Unknown; }
        }

        /// <summary>
        /// Gets the audio type (encoding).
        /// </summary>
        public virtual AudioFormat AudioFormat
        {
            get { return AudioFormat.Unknown; }
        }

        public virtual Task<string> GetUriAsync() =>
            Task.FromResult(Uri);

        public byte[] GetBytes() =>
            GetBytesAsync().GetAwaiter().GetResult();

        public async Task<byte[]> GetBytesAsync()
        {
            using (var client = new VideoClient())
            {
                return await client
                    .GetBytesAsync(this)
                    .ConfigureAwait(false);
            }
        }

        public Stream Stream() => 
        	StreamAsync().GetAwaiter().GetResult();

        public async Task<Stream> StreamAsync()
        {
            using (var client = new VideoClient())
            {
                return await client
                    .StreamAsync(this)
                    .ConfigureAwait(false);
            }
        }
        
        public Stream Head() => HeadAsync().GetAwaiter().GetResult();
        public async Task<Stream> HeadAsync()
        {
            using (var client = new VideoClient())
            {
                return await client
                    .StreamAsync(this)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the audio extension.
        /// </summary>
        /// <value>The audio extension, or empty if the audio extension is unknown.</value>
        public string AudioExtension
        {
            get
            {
                switch (AudioFormat)
                {
                    case AudioFormat.Mp3:
                        return ".mp3";

                    case AudioFormat.Aac:
                        return ".aac";

                    case AudioFormat.Vorbis:
                        return ".ogg";

                    case AudioFormat.Opus:
                        return ".opus";

                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the video extension.
        /// </summary>
        /// <value>The video extension, or empty if the video extension is unknown.</value>
        public virtual string VideoExtension
        {
            get
            {
                switch (VideoFormat)
                {
                    case VideoFormat.Mp4:
                        return ".mp4";

                    case VideoFormat.WebM:
                        return ".webm";

                    case VideoFormat.Mobile:
                        return ".3gp";

                    case VideoFormat.Flv:
                        return ".flv";

                    default:
                        return string.Empty;

                }
            }
        }

        public string FullName
        {
            get
            {
                var builder =
                    new StringBuilder(Title)
                    .Append(VideoExtension ?? AudioExtension);
                
                foreach (char bad in Path.GetInvalidFileNameChars())
                    builder.Replace(bad, '_');

                return builder.ToString();
            }
        }

        public override string ToString()
        {
            return string.Format("Title: {0}, Url: {1}, VideoFormat: {2}, AudioFormat: {3}", Title, Uri, VideoFormat, AudioFormat);
        }
    }


    public class VideoInfo
    {
        public VideoInfo(string title, int? second, string author)
        {
            this.Title = title;
            this.LengthSeconds = second;
            this.Author = author;
        }
        public string Title { get; }
        public int? LengthSeconds { get; }
        public string Author { get; }
    }
}

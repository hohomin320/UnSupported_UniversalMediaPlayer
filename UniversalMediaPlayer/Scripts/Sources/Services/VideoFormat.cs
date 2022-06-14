namespace UMP.Services
{
    /// <summary>
    /// The video format. Also known as video container.
    /// </summary>
    public enum VideoFormat
    {
        /// <summary>
        /// MPEG-4 Part 14 (.mp4).
        /// </summary>
        Mp4,

        /// <summary>
        /// Web Media (.webm).
        /// </summary>
        WebM,

        /// <summary>
        /// Video for mobile devices (3GP).
        /// </summary>
        Mobile,

        /// <summary>
        /// Flash Video (.flv).
        /// </summary>
        Flv,

        /// <summary>
        /// The video type is unknown.
        /// </summary>
        Unknown
    }
}

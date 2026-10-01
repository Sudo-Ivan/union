using System;

namespace NzbDrone.Core.Parser.Model
{
    [Flags]
    public enum IndexerFlags
    {
        /// <summary>
        /// Torrent download amount does not count
        /// </summary>
        Freeleech = 1,
        G_Freeleech = 1,

        /// <summary>
        /// Torrent download amount only counts 50%
        /// </summary>
        Halfleech = 2,
        G_Halfleech = 2,

        /// <summary>
        /// Torrent upload amount is doubled
        /// </summary>
        DoubleUpload = 4,
        G_DoubleUpload = 4,

        /// <summary>
        /// Torrent is a very high quality encode, as applied manually by the PTP staff
        /// </summary>
        PTP_Golden = 8,

        /// <summary>
        /// Torrent from PTP that has been checked (by staff or torrent checkers) for release description requirements
        /// </summary>
        PTP_Approved = 16,

        /// <summary>
        /// Uploader is an internal release group
        /// </summary>
        Internal = 32,
        G_Internal = 32,

        // AHD, internal
        [Obsolete]
        AHD_Internal = 64,

        /// <summary>
        /// The release comes from a scene group
        /// </summary>
        Scene = 128,
        G_Scene = 128,

        /// <summary>
        /// Torrent download amount only counts 75%
        /// </summary>
        Freeleech75 = 256,
        G_Freeleech75 = 256,

        /// <summary>
        /// Torrent download amount only counts 25%
        /// </summary>
        Freeleech25 = 512,
        G_Freeleech25 = 512,

        // AHD, internal
        [Obsolete]
        AHD_UserRelease = 1024,

        /// <summary>
        /// The release is nuked
        /// </summary>
        Nuked = 2048,

        /// <summary>
        /// The release contains subtitles
        /// </summary>
        Subtitles = 4096
    }
}

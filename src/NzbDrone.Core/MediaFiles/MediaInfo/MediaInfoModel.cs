using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public class MediaInfoModel : IEmbeddedDocument
    {
        public string RawStreamData { get; set; }

        public int SchemaRevision { get; set; }

        public string ContainerFormat { get; set; }
        public string VideoFormat { get; set; }

        public string VideoCodecID { get; set; }

        public string VideoProfile { get; set; }

        public long VideoBitrate { get; set; }

        public int VideoBitDepth { get; set; }

        public string VideoColourPrimaries { get; set; }

        public string VideoTransferCharacteristics { get; set; }

        public HdrFormat VideoHdrFormat { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public TimeSpan RunTime { get; set; }

        public decimal VideoFps { get; set; }

        public MediaInfoAudioStreamModel PrimaryAudioStream => AudioStreams?.FirstOrDefault();

        public List<MediaInfoAudioStreamModel> AudioStreams { get; set; }

        public List<MediaInfoSubtitleStreamModel> SubtitleStreams { get; set; }

        public int VideoMultiViewCount { get; set; }

        // Flat accessors for movie-domain callers (derived from stream lists)
        [JsonIgnore]
        public string AudioFormat => PrimaryAudioStream?.Format;

        [JsonIgnore]
        public string AudioCodecID => PrimaryAudioStream?.CodecId;

        [JsonIgnore]
        public string AudioProfile => PrimaryAudioStream?.Profile;

        [JsonIgnore]
        public long AudioBitrate => PrimaryAudioStream?.Bitrate ?? 0;

        [JsonIgnore]
        public int AudioStreamCount => AudioStreams?.Count ?? 0;

        [JsonIgnore]
        public int AudioChannels => PrimaryAudioStream?.Channels ?? 0;

        [JsonIgnore]
        public string AudioChannelPositions => PrimaryAudioStream?.ChannelPositions;

        [JsonIgnore]
        public List<string> AudioLanguages => AudioStreams?.Select(s => s.Language).Where(l => l.IsNotNullOrWhiteSpace()).Distinct().ToList() ?? new List<string>();

        [JsonIgnore]
        public List<string> Subtitles => SubtitleStreams?.Select(s => s.Language).Where(l => l.IsNotNullOrWhiteSpace()).ToList() ?? new List<string>();

        public string ScanType { get; set; }

        [JsonIgnore]
        public string Title { get; set; }
    }
}

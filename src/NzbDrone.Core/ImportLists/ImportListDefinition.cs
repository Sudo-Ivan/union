using System;
using Equ;
using NzbDrone.Core.Movies;
using NzbDrone.Core.ThingiProvider;
using MovieMonitorTypes = NzbDrone.Core.Movies.MonitorTypes;
using NewItemMonitorTypes = NzbDrone.Core.Tv.NewItemMonitorTypes;
using SeriesMonitorTypes = NzbDrone.Core.Tv.MonitorTypes;
using SeriesTypes = NzbDrone.Core.Tv.SeriesTypes;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListDefinition : ProviderDefinition, IEquatable<ImportListDefinition>
    {
        private static readonly MemberwiseEqualityComparer<ImportListDefinition> Comparer = MemberwiseEqualityComparer<ImportListDefinition>.ByProperties;

        // Series-domain settings (Sonarr)
        public bool EnableAutomaticAdd { get; set; }
        public bool SearchForMissingEpisodes { get; set; }
        public SeriesMonitorTypes ShouldMonitor { get; set; }
        public NewItemMonitorTypes MonitorNewItems { get; set; }
        public SeriesTypes SeriesType { get; set; }
        public bool SeasonFolder { get; set; }
        public bool TagExisting { get; set; }

        // Movie-domain settings (Radarr)
        public bool Enabled { get; set; }
        public bool EnableAuto { get; set; }
        public MovieMonitorTypes Monitor { get; set; }
        public MovieStatusType MinimumAvailability { get; set; }
        public bool SearchOnAdd { get; set; }

        // Shared settings
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; }

        [MemberwiseEqualityIgnore]
        public override bool Enable => EnableAutomaticAdd || Enabled;

        [MemberwiseEqualityIgnore]
        public ImportListStatus Status { get; set; }

        [MemberwiseEqualityIgnore]
        public ImportListType ListType { get; set; }

        [MemberwiseEqualityIgnore]
        public TimeSpan MinRefreshInterval { get; set; }

        public bool Equals(ImportListDefinition other)
        {
            return Comparer.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ImportListDefinition);
        }

        public override int GetHashCode()
        {
            return Comparer.GetHashCode(this);
        }
    }
}

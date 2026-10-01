using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine
{
    public class DownloadDecisionComparer : IComparer<DownloadDecision>
    {
        private readonly IConfigService _configService;
        private readonly IDelayProfileService _delayProfileService;
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public delegate int CompareDelegate(DownloadDecision x, DownloadDecision y);
        public delegate int CompareDelegate<TSubject, TValue>(DownloadDecision x, DownloadDecision y);

        public DownloadDecisionComparer(IConfigService configService, IDelayProfileService delayProfileService)
            : this(configService, delayProfileService, null)
        {
        }

        public DownloadDecisionComparer(IConfigService configService, IDelayProfileService delayProfileService, IQualityDefinitionService qualityDefinitionService)
        {
            _configService = configService;
            _delayProfileService = delayProfileService;
            _qualityDefinitionService = qualityDefinitionService;
        }

        public int Compare(DownloadDecision x, DownloadDecision y)
        {
            var comparers = new List<CompareDelegate>
            {
                CompareQuality,
                CompareCustomFormatScore,
                CompareProtocol,
                CompareEpisodeCount,
                CompareEpisodeNumber,
                CompareIndexerPriority,
                CompareIndexerFlags,
                ComparePeersIfTorrent,
                CompareAgeIfUsenet,
                CompareSize
            };

            return comparers.Select(comparer => comparer(x, y)).FirstOrDefault(result => result != 0);
        }

        private int CompareBy<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            var leftValue = funcValue(left);
            var rightValue = funcValue(right);

            return leftValue.CompareTo(rightValue);
        }

        private int CompareByReverse<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            return CompareBy(left, right, funcValue) * -1;
        }

        private int CompareAll(params int[] comparers)
        {
            return comparers.Select(comparer => comparer).FirstOrDefault(result => result != 0);
        }

        private static ReleaseInfo GetRelease(DownloadDecision decision)
        {
            return decision.RemoteEpisode?.Release ?? decision.RemoteMovie?.Release;
        }

        private static QualityModel GetQuality(DownloadDecision decision)
        {
            return decision.RemoteEpisode?.ParsedEpisodeInfo?.Quality ?? decision.RemoteMovie?.ParsedMovieInfo?.Quality;
        }

        private static QualityIndex GetQualityProfileIndex(DownloadDecision decision)
        {
            if (decision.RemoteEpisode != null)
            {
                return decision.RemoteEpisode.Series.QualityProfile.Value.GetIndex(decision.RemoteEpisode.ParsedEpisodeInfo.Quality.Quality);
            }

            return decision.RemoteMovie.Movie.QualityProfile.GetIndex(decision.RemoteMovie.ParsedMovieInfo.Quality.Quality);
        }

        private static HashSet<int> GetTags(DownloadDecision decision)
        {
            return decision.RemoteEpisode?.Series?.Tags ?? decision.RemoteMovie?.Movie?.Tags;
        }

        private static int GetCustomFormatScore(DownloadDecision decision)
        {
            return decision.RemoteEpisode?.CustomFormatScore ?? decision.RemoteMovie?.CustomFormatScore ?? 0;
        }

        private int CompareIndexerPriority(DownloadDecision x, DownloadDecision y)
        {
            return CompareByReverse(GetRelease(x), GetRelease(y), release => release.IndexerPriority);
        }

        private int CompareQuality(DownloadDecision x, DownloadDecision y)
        {
            if (_configService.DownloadPropersAndRepacks == ProperDownloadTypes.DoNotPrefer)
            {
                return CompareBy(x, y, GetQualityProfileIndex);
            }

            return CompareAll(
                CompareBy(x, y, GetQualityProfileIndex),
                CompareBy(x, y, decision => GetQuality(decision).Revision));
        }

        private int CompareCustomFormatScore(DownloadDecision x, DownloadDecision y)
        {
            return CompareBy(x, y, GetCustomFormatScore);
        }

        private int CompareProtocol(DownloadDecision x, DownloadDecision y)
        {
            var result = CompareBy(x, y, decision =>
            {
                var delayProfile = _delayProfileService.BestForTags(GetTags(decision));
                var downloadProtocol = GetRelease(decision).DownloadProtocol;
                return downloadProtocol == delayProfile.PreferredProtocol;
            });

            return result;
        }

        private int CompareEpisodeCount(DownloadDecision x, DownloadDecision y)
        {
            if (x.RemoteEpisode == null || y.RemoteEpisode == null)
            {
                return 0;
            }

            var seasonPackCompare = CompareBy(x.RemoteEpisode,
                y.RemoteEpisode,
                remoteEpisode => remoteEpisode.ParsedEpisodeInfo.FullSeason);

            if (seasonPackCompare != 0)
            {
                return seasonPackCompare;
            }

            if (x.RemoteEpisode.Series.SeriesType == SeriesTypes.Anime &
                y.RemoteEpisode.Series.SeriesType == SeriesTypes.Anime)
            {
                return CompareBy(x.RemoteEpisode, y.RemoteEpisode, remoteEpisode => remoteEpisode.Episodes.Count);
            }

            return CompareByReverse(x.RemoteEpisode, y.RemoteEpisode, remoteEpisode => remoteEpisode.Episodes.Count);
        }

        private int CompareEpisodeNumber(DownloadDecision x, DownloadDecision y)
        {
            if (x.RemoteEpisode == null || y.RemoteEpisode == null)
            {
                return 0;
            }

            return CompareByReverse(x.RemoteEpisode, y.RemoteEpisode, remoteEpisode => remoteEpisode.Episodes.Select(e => e.EpisodeNumber).MinOrDefault());
        }

        private int CompareIndexerFlags(DownloadDecision x, DownloadDecision y)
        {
            if (!_configService.PreferIndexerFlags)
            {
                return 0;
            }

            return CompareBy(GetRelease(x), GetRelease(y), release => ScoreFlags(release.IndexerFlags));
        }

        private int ComparePeersIfTorrent(DownloadDecision x, DownloadDecision y)
        {
            // Different protocols should get caught when checking the preferred protocol,
            // since we're dealing with the same item in our comparisons
            if (GetRelease(x).DownloadProtocol != DownloadProtocol.Torrent ||
                GetRelease(y).DownloadProtocol != DownloadProtocol.Torrent)
            {
                return 0;
            }

            return CompareAll(
                CompareBy(x, y, decision =>
                {
                    var seeders = TorrentInfo.GetSeeders(GetRelease(decision));

                    return seeders.HasValue && seeders.Value > 0 ? Math.Round(Math.Log10(seeders.Value)) : 0;
                }),
                CompareBy(x, y, decision =>
                {
                    var peers = TorrentInfo.GetPeers(GetRelease(decision));

                    return peers.HasValue && peers.Value > 0 ? Math.Round(Math.Log10(peers.Value)) : 0;
                }));
        }

        private int CompareAgeIfUsenet(DownloadDecision x, DownloadDecision y)
        {
            if (GetRelease(x).DownloadProtocol != DownloadProtocol.Usenet ||
                GetRelease(y).DownloadProtocol != DownloadProtocol.Usenet)
            {
                return 0;
            }

            return CompareBy(x, y, decision =>
            {
                var ageHours = GetRelease(decision).AgeHours;
                var age = GetRelease(decision).Age;

                if (ageHours < 1)
                {
                    return 1000;
                }
                else if (age <= 7)
                {
                    return 300;
                }
                else if (age <= 30)
                {
                    return 200;
                }
                else
                {
                    return 100;
                }
            });
        }

        private int CompareSize(DownloadDecision x, DownloadDecision y)
        {
            var sizeCompare = CompareBy(x, y, decision =>
            {
                var preferredSize = GetPreferredSize(decision);
                var runtime = GetRuntime(decision);

                // If no value for preferred it means unlimited so fallback to sort largest is best
                if (preferredSize.HasValue && runtime > 0)
                {
                    var preferredReleaseSize = runtime * preferredSize.Value.Megabytes();

                    // Calculate closest to the preferred size
                    return Math.Abs((GetRelease(decision).Size - preferredReleaseSize).Round(200.Megabytes())) * (-1);
                }
                else
                {
                    return GetRelease(decision).Size.Round(200.Megabytes());
                }
            });

            return sizeCompare;
        }

        private double? GetPreferredSize(DownloadDecision decision)
        {
            if (decision.RemoteEpisode != null)
            {
                var qualityProfile = decision.RemoteEpisode.Series.QualityProfile.Value;
                var qualityIndex = qualityProfile.GetIndex(decision.RemoteEpisode.ParsedEpisodeInfo.Quality.Quality, true);
                var qualityOrGroup = qualityProfile.Items[qualityIndex.Index];
                var item = qualityOrGroup.Quality == null ? qualityOrGroup.Items[qualityIndex.GroupIndex] : qualityOrGroup;

                return item.PreferredSize;
            }

            if (_qualityDefinitionService != null)
            {
                return _qualityDefinitionService.Get(decision.RemoteMovie.ParsedMovieInfo.Quality.Quality).PreferredSize;
            }

            return null;
        }

        private static int GetRuntime(DownloadDecision decision)
        {
            if (decision.RemoteMovie != null)
            {
                return decision.RemoteMovie.Movie.MovieMetadata.Value.Runtime;
            }

            var remoteEpisode = decision.RemoteEpisode;

            if (remoteEpisode.Episodes.Count == 0)
            {
                return remoteEpisode.Series.Runtime;
            }

            return remoteEpisode.Episodes.Sum(episode => episode.Runtime > 0 ? episode.Runtime : remoteEpisode.Series.Runtime);
        }

        private int ScoreFlags(IndexerFlags flags)
        {
            var flagValues = Enum.GetValues(typeof(IndexerFlags));

            var score = 0;

            foreach (IndexerFlags value in flagValues)
            {
                if ((flags & value) == value)
                {
                    switch (value)
                    {
                        case IndexerFlags.Freeleech:
                        case IndexerFlags.DoubleUpload:
                        case IndexerFlags.Internal:
                        case IndexerFlags.PTP_Approved:
                        case IndexerFlags.PTP_Golden:
                            score += 2;
                            break;
                        case IndexerFlags.Halfleech:
                            score += 1;
                            break;
                    }
                }
            }

            return score;
        }
    }
}

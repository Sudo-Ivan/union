using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class PrioritizeDownloadDecisionFixture : CoreTest<DownloadDecisionPriorizationService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            _series = Builder<Series>.CreateNew()
                .With(e => e.Runtime = 60)
                .With(e => e.QualityProfile = new QualityProfile
                {
                    Items = Qualities.QualityFixture.GetDefaultQualities()
                })
                .Build();
        }

        private void GivenPreferredSize(QualityProfile qualityProfile, double? size)
        {
            foreach (var qualityOrGroup in qualityProfile.Items)
            {
                if (qualityOrGroup.Quality != null)
                {
                    qualityOrGroup.PreferredSize = size;
                }
                else
                {
                    qualityOrGroup.Items.ForEach(i => i.PreferredSize = size);
                }
            }
        }

        private Episode GivenEpisode(int id)
        {
            return Builder<Episode>.CreateNew()
                            .With(e => e.Id = id)
                            .With(e => e.EpisodeNumber = id)
                            .With(e => e.Runtime = 0)
                            .Build();
        }

        private RemoteEpisode GivenRemoteEpisode(List<Episode> episodes, QualityModel quality, Language language, int age = 0, long size = 0, DownloadProtocol downloadProtocol = DownloadProtocol.Usenet, int indexerPriority = 25)
        {
            var remoteEpisode = new RemoteEpisode();
            remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo();
            remoteEpisode.ParsedEpisodeInfo.Quality = quality;
            remoteEpisode.ParsedEpisodeInfo.Languages = new List<Language> { language };

            remoteEpisode.Episodes = new List<Episode>();
            remoteEpisode.Episodes.AddRange(episodes);

            remoteEpisode.Release = new ReleaseInfo();
            remoteEpisode.Release.PublishDate = DateTime.Now.AddDays(-age);
            remoteEpisode.Release.Size = size;
            remoteEpisode.Release.DownloadProtocol = downloadProtocol;
            remoteEpisode.Release.IndexerPriority = indexerPriority;

            remoteEpisode.Series = _series;

            return remoteEpisode;
        }

        private void GivenPreferredDownloadProtocol(DownloadProtocol downloadProtocol)
        {
            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(new DelayProfile
                  {
                      PreferredProtocol = downloadProtocol
                  });
        }

        [Test]
        public void should_put_reals_before_non_reals()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p, new Revision(version: 1, real: 0)), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p, new Revision(version: 1, real: 1)), Language.English);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Real.Should().Be(1);
        }

        [Test]
        public void should_put_propers_before_non_propers()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p, new Revision(version: 1)), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p, new Revision(version: 2)), Language.English);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_put_higher_quality_before_lower()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.SDTV), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Quality.Should().Be(Quality.HDTV720p);
        }

        [Test]
        public void should_order_by_lowest_number_of_episodes()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(2) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Episodes.First().EpisodeNumber.Should().Be(1);
        }

        [Test]
        public void should_order_by_lowest_number_of_episodes_with_multiple_episodes()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(2), GivenEpisode(3) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1), GivenEpisode(2) }, new QualityModel(Quality.HDTV720p), Language.English);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Episodes.First().EpisodeNumber.Should().Be(1);
        }

        [Test]
        public void should_order_by_age_then_largest_rounded_to_200mb()
        {
            var remoteEpisodeSd = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.SDTV), Language.English, size: 100.Megabytes(), age: 1);
            var remoteEpisodeHdSmallOld = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 1200.Megabytes(), age: 1000);
            var remoteEpisodeSmallYoung = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 1250.Megabytes(), age: 10);
            var remoteEpisodeHdLargeYoung = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 3000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisodeSd));
            decisions.Add(new DownloadDecision(remoteEpisodeHdSmallOld));
            decisions.Add(new DownloadDecision(remoteEpisodeSmallYoung));
            decisions.Add(new DownloadDecision(remoteEpisodeHdLargeYoung));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisodeHdLargeYoung);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_both_under()
        {
            // 200 MB/Min * 60 Min Runtime = 12000 MB
            GivenPreferredSize(_series.QualityProfile.Value, 200);

            var remoteEpisodeSmall = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 1200.Megabytes(), age: 1);
            var remoteEpisodeLarge = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 10000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisodeSmall));
            decisions.Add(new DownloadDecision(remoteEpisodeLarge));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisodeLarge);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_preferred_is_in_between()
        {
            // 46 MB/Min * 60 Min Runtime = 6900 MB
            GivenPreferredSize(_series.QualityProfile.Value, 46);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 500.Megabytes(), age: 1);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 2000.Megabytes(), age: 1);
            var remoteEpisode3 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 3000.Megabytes(), age: 1);
            var remoteEpisode4 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 5000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));
            decisions.Add(new DownloadDecision(remoteEpisode3));
            decisions.Add(new DownloadDecision(remoteEpisode4));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisode3);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_of_the_whole_release()
        {
            // 46 MB/Min * (10 episodes * 60 Min Runtime) = 27600 MB
            GivenPreferredSize(_series.QualityProfile.Value, 46);

            var episodes = Builder<Episode>.CreateListOfSize(10)
                                           .All()
                                           .With(e => e.Runtime = 60)
                                           .Build()
                                           .ToList();

            var remoteEpisodeSmall = GivenRemoteEpisode(episodes, new QualityModel(Quality.HDTV720p), Language.English, size: 12000.Megabytes(), age: 1);
            var remoteEpisodeLarge = GivenRemoteEpisode(episodes, new QualityModel(Quality.HDTV720p), Language.English, size: 26000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisodeSmall));
            decisions.Add(new DownloadDecision(remoteEpisodeLarge));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisodeLarge);
        }

        [Test]
        public void should_order_by_youngest()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, age: 10);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, age: 5);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisode2);
        }

        [Test]
        public void should_not_throw_if_no_episodes_are_found()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 500.Megabytes());
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, size: 500.Megabytes());

            remoteEpisode1.Episodes = new List<Episode>();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            Subject.PrioritizeDecisions(decisions);
        }

        [Test]
        public void should_put_usenet_above_torrent_when_usenet_is_preferred()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, downloadProtocol: DownloadProtocol.Torrent);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Release.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
        }

        [Test]
        public void should_put_torrent_above_usenet_when_torrent_is_preferred()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Torrent);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, downloadProtocol: DownloadProtocol.Torrent);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English, downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Release.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
        }

        [Test]
        public void should_prefer_season_pack_above_single_episode()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1), GivenEpisode(2) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            remoteEpisode1.ParsedEpisodeInfo.FullSeason = true;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.FullSeason.Should().BeTrue();
        }

        [Test]
        public void should_prefer_multiepisode_over_single_episode_for_anime()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1), GivenEpisode(2) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            remoteEpisode1.Series.SeriesType = SeriesTypes.Anime;
            remoteEpisode2.Series.SeriesType = SeriesTypes.Anime;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Episodes.Count.Should().Be(remoteEpisode1.Episodes.Count);
        }

        [Test]
        public void should_prefer_single_episode_over_multi_episode_for_non_anime()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1), GivenEpisode(2) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Episodes.Count.Should().Be(remoteEpisode2.Episodes.Count);
        }

        [Test]
        public void should_prefer_releases_with_more_seeders()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 100;

            remoteEpisode1.Release = torrentInfo1;
            remoteEpisode2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteEpisode.Release).Seeders.Should().Be(torrentInfo2.Seeders);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_given_equal_number_of_seeds()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Peers = 100;

            remoteEpisode1.Release = torrentInfo1;
            remoteEpisode2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteEpisode.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_no_seeds()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 0;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 0;
            torrentInfo2.Peers = 100;

            remoteEpisode1.Release = torrentInfo1;
            remoteEpisode2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteEpisode.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_first_release_if_peers_and_size_are_too_similar()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 1000;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteEpisode1.Release = torrentInfo1;
            remoteEpisode2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteEpisode.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_prefer_first_release_if_age_and_size_are_too_similar()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.English);

            remoteEpisode1.Release.PublishDate = DateTime.UtcNow.AddDays(-100);
            remoteEpisode1.Release.Size = 200.Megabytes();

            remoteEpisode2.Release.PublishDate = DateTime.UtcNow.AddDays(-150);
            remoteEpisode2.Release.Size = 250.Megabytes();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Release.Should().Be(remoteEpisode1.Release);
        }

        [Test]
        public void should_prefer_quality_over_the_number_of_peers()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.Bluray1080p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.SDTV), Language.English);

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 100;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteEpisode1.Release = torrentInfo1;
            remoteEpisode2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteEpisode.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_put_higher_quality_before_lower_always()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.SDTV), Language.French);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p), Language.German);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Quality.Should().Be(Quality.HDTV720p);
        }

        [Test]
        public void should_prefer_higher_score_over_lower_score()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p), Language.English);

            remoteEpisode1.CustomFormatScore = 10;
            remoteEpisode2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_prefer_and_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(2)), Language.English);

            remoteEpisode1.CustomFormatScore = 10;
            remoteEpisode2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_do_not_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(2)), Language.English);

            remoteEpisode1.CustomFormatScore = 10;
            remoteEpisode2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_score_over_proper_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(2)), Language.English);

            remoteEpisode1.CustomFormatScore = 10;
            remoteEpisode2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Quality.Should().Be(Quality.WEBDL1080p);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteEpisode.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_score_over_real_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1, 0)), Language.English);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1, 1)), Language.English);

            remoteEpisode1.CustomFormatScore = 10;
            remoteEpisode2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Quality.Should().Be(Quality.WEBDL1080p);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteEpisode.ParsedEpisodeInfo.Quality.Revision.Real.Should().Be(0);
            qualifiedReports.First().RemoteEpisode.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void sort_download_decisions_based_on_indexer_priority()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English, indexerPriority: 25);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English, indexerPriority: 50);
            var remoteEpisode3 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English, indexerPriority: 1);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteEpisode1), new DownloadDecision(remoteEpisode2), new DownloadDecision(remoteEpisode3) });

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisode3);
            qualifiedReports.Skip(1).First().RemoteEpisode.Should().Be(remoteEpisode1);
            qualifiedReports.Last().RemoteEpisode.Should().Be(remoteEpisode2);
        }

        [Test]
        public void ensure_download_decisions_indexer_priority_is_not_perfered_over_quality()
        {
            var remoteEpisode1 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.HDTV720p, new Revision(1)), Language.English, indexerPriority: 25);
            var remoteEpisode2 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English, indexerPriority: 50);
            var remoteEpisode3 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.SDTV, new Revision(1)), Language.English, indexerPriority: 1);
            var remoteEpisode4 = GivenRemoteEpisode(new List<Episode> { GivenEpisode(1) }, new QualityModel(Quality.WEBDL1080p, new Revision(1)), Language.English, indexerPriority: 25);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteEpisode1), new DownloadDecision(remoteEpisode2), new DownloadDecision(remoteEpisode3), new DownloadDecision(remoteEpisode4) });

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteEpisode.Should().Be(remoteEpisode4);
            qualifiedReports.Skip(1).First().RemoteEpisode.Should().Be(remoteEpisode2);
            qualifiedReports.Skip(2).First().RemoteEpisode.Should().Be(remoteEpisode1);
            qualifiedReports.Last().RemoteEpisode.Should().Be(remoteEpisode3);
        }

        // Movie-domain members merged from Radarr

        private CustomFormat _customFormat1;

        private CustomFormat _customFormat2;

        [SetUp]
        public void SetupMovie()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            _customFormat1 = new CustomFormat("My Format 1", new LanguageSpecification { Value = (int)Language.English }) { Id = 1 };
            _customFormat2 = new CustomFormat("My Format 2", new LanguageSpecification { Value = (int)Language.French }) { Id = 2 };

            CustomFormatsTestHelpers.GivenCustomFormats(_customFormat1, _customFormat2);

            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(s => s.Get(It.IsAny<Quality>()))
                  .Returns(new QualityDefinition { PreferredSize = null });
        }

        private void GivenPreferredSizeMovie(double? size)
        {
            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(s => s.Get(It.IsAny<Quality>()))
                  .Returns(new QualityDefinition { PreferredSize = size });
        }

        private RemoteMovie GivenRemoteMovie(QualityModel quality, int age = 0, long size = 0, DownloadProtocol downloadProtocol = DownloadProtocol.Usenet, int runtime = 150, int indexerPriority = 25)
        {
            var remoteMovie = new RemoteMovie();
            remoteMovie.ParsedMovieInfo = new ParsedMovieInfo();
            remoteMovie.ParsedMovieInfo.MovieTitles = new List<string> { "A Movie" };
            remoteMovie.ParsedMovieInfo.Year = 1998;
            remoteMovie.ParsedMovieInfo.Quality = quality;

            remoteMovie.Movie = Builder<Movie>.CreateNew().With(m => m.QualityProfile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_customFormat1.Name, _customFormat2.Name),
                MinFormatScore = 0
            })
                .With(m => m.Title = "A Movie")
                .With(m => m.MovieMetadata.Value.Runtime = runtime).Build();

            remoteMovie.Release = new ReleaseInfo();
            remoteMovie.Release.PublishDate = DateTime.Now.AddDays(-age);
            remoteMovie.Release.Size = size;
            remoteMovie.Release.DownloadProtocol = downloadProtocol;
            remoteMovie.Release.Title = "A Movie 1998";
            remoteMovie.Release.IndexerPriority = indexerPriority;

            remoteMovie.CustomFormats = new List<CustomFormat>();
            remoteMovie.CustomFormatScore = 0;

            return remoteMovie;
        }

        [Test]
        public void should_put_reals_before_non_reals_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p, new Revision(version: 1, real: 0)));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p, new Revision(version: 1, real: 1)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Real.Should().Be(1);
        }

        [Test]
        public void should_put_propers_before_non_propers_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p, new Revision(version: 1)));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p, new Revision(version: 2)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_put_higher_quality_before_lower_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.SDTV));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Quality.Should().Be(Quality.HDTV720p);
        }

        [Test]
        public void should_order_by_age_then_largest_rounded_to_200mb_movie()
        {
            var remoteMovieSd = GivenRemoteMovie(new QualityModel(Quality.SDTV), size: 100.Megabytes(), age: 1);
            var remoteMovieHdSmallOld = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 1200.Megabytes(), age: 1000);
            var remoteMovieSmallYoung = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 1250.Megabytes(), age: 10);
            var remoteMovieHdLargeYoung = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 3000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovieSd));
            decisions.Add(new DownloadDecision(remoteMovieHdSmallOld));
            decisions.Add(new DownloadDecision(remoteMovieSmallYoung));
            decisions.Add(new DownloadDecision(remoteMovieHdLargeYoung));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovieHdLargeYoung);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_both_over()
        {
            // 2 MB/Min * 150 Min Runtime = 300 MB
            GivenPreferredSizeMovie(2);

            var remoteMovieSmall = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 400.Megabytes(), age: 1);
            var remoteMovieLarge = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 15000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovieSmall));
            decisions.Add(new DownloadDecision(remoteMovieLarge));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovieSmall);
        }

        [Test]
        public void should_order_by_largest_to_if_zero_runtime()
        {
            // 2 MB/Min * 150 Min Runtime = 300 MB
            GivenPreferredSizeMovie(2);

            var remoteMovieSmall = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 400.Megabytes(), age: 1, runtime: 0);
            var remoteMovieLarge = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 15000.Megabytes(), age: 1, runtime: 0);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovieSmall));
            decisions.Add(new DownloadDecision(remoteMovieLarge));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovieLarge);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_both_under_movie()
        {
            // 390 MB/Min * 150 Min Runtime = 58,500 MB
            GivenPreferredSizeMovie(390);

            var remoteMovieSmall = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 100.Megabytes(), age: 1);
            var remoteMovieLarge = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 15000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovieSmall));
            decisions.Add(new DownloadDecision(remoteMovieLarge));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovieLarge);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_preferred_is_in_between_movie()
        {
            // 46 MB/Min * 150 Min Runtime = 6900 MB
            GivenPreferredSizeMovie(46);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 100.Megabytes(), age: 1);
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 500.Megabytes(), age: 1);
            var remoteMovie3 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 7000.Megabytes(), age: 1);
            var remoteMovie4 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), size: 15000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));
            decisions.Add(new DownloadDecision(remoteMovie3));
            decisions.Add(new DownloadDecision(remoteMovie4));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovie3);
        }

        [Test]
        public void should_order_by_youngest_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), age: 10);
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), age: 5);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovie2);
        }

        [Test]
        public void should_put_usenet_above_torrent_when_usenet_is_preferred_movie()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), downloadProtocol: DownloadProtocol.Torrent);
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Release.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
        }

        [Test]
        public void should_put_torrent_above_usenet_when_torrent_is_preferred_movie()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Torrent);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), downloadProtocol: DownloadProtocol.Torrent);
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Release.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
        }

        [Test]
        public void should_prefer_releases_with_more_seeders_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 100;

            remoteMovie1.Release = torrentInfo1;
            remoteMovie1.Release.Title = "A Movie 1998";
            remoteMovie2.Release = torrentInfo2;
            remoteMovie2.Release.Title = "A Movie 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteMovie.Release).Seeders.Should().Be(torrentInfo2.Seeders);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_given_equal_number_of_seeds_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Peers = 100;

            remoteMovie1.Release = torrentInfo1;
            remoteMovie1.Release.Title = "A Movie 1998";
            remoteMovie2.Release = torrentInfo2;
            remoteMovie2.Release.Title = "A Movie 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteMovie.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_no_seeds_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 0;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 0;
            torrentInfo2.Peers = 100;

            remoteMovie1.Release = torrentInfo1;
            remoteMovie1.Release.Title = "A Movie 1998";
            remoteMovie2.Release = torrentInfo2;
            remoteMovie2.Release.Title = "A Movie 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteMovie.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_first_release_if_peers_and_size_are_too_similar_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 1000;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteMovie1.Release = torrentInfo1;
            remoteMovie1.Release.Title = "A Movie 1998";
            remoteMovie2.Release = torrentInfo2;
            remoteMovie2.Release.Title = "A Movie 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteMovie.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_prefer_first_release_if_age_and_size_are_too_similar_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p));

            remoteMovie1.Release.PublishDate = DateTime.UtcNow.AddDays(-100);
            remoteMovie1.Release.Size = 200.Megabytes();

            remoteMovie2.Release.PublishDate = DateTime.UtcNow.AddDays(-150);
            remoteMovie2.Release.Size = 250.Megabytes();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Release.Should().Be(remoteMovie1.Release);
        }

        [Test]
        public void should_prefer_better_custom_format()
        {
            var quality1 = new QualityModel(Quality.Bluray720p);
            var remoteMovie1 = GivenRemoteMovie(quality1);

            var quality2 = new QualityModel(Quality.Bluray720p);
            var remoteMovie2 = GivenRemoteMovie(quality2);
            remoteMovie2.CustomFormats.Add(_customFormat1);
            remoteMovie2.CustomFormatScore = remoteMovie2.Movie.QualityProfile.CalculateCustomFormatScore(remoteMovie2.CustomFormats);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Release.Should().Be(remoteMovie2.Release);
        }

        [Test]
        public void should_prefer_better_custom_format2()
        {
            var quality1 = new QualityModel(Quality.Bluray720p);
            var remoteMovie1 = GivenRemoteMovie(quality1);
            remoteMovie1.CustomFormats.Add(_customFormat1);
            remoteMovie1.CustomFormatScore = remoteMovie1.Movie.QualityProfile.CalculateCustomFormatScore(remoteMovie1.CustomFormats);

            var quality2 = new QualityModel(Quality.Bluray720p);
            var remoteMovie2 = GivenRemoteMovie(quality2);
            remoteMovie2.CustomFormats.Add(_customFormat2);
            remoteMovie2.CustomFormatScore = remoteMovie2.Movie.QualityProfile.CalculateCustomFormatScore(remoteMovie2.CustomFormats);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Release.Should().Be(remoteMovie2.Release);
        }

        [Test]
        public void should_prefer_2_custom_formats()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.Bluray720p));
            remoteMovie1.CustomFormats.Add(_customFormat1);
            remoteMovie1.CustomFormatScore = remoteMovie1.Movie.QualityProfile.CalculateCustomFormatScore(remoteMovie1.CustomFormats);

            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.Bluray720p));
            remoteMovie2.CustomFormats.AddRange(new List<CustomFormat> { _customFormat1, _customFormat2 });
            remoteMovie2.CustomFormatScore = remoteMovie2.Movie.QualityProfile.CalculateCustomFormatScore(remoteMovie2.CustomFormats);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Release.Should().Be(remoteMovie2.Release);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_prefer_and_upgrade_movie()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(1)));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(2)));

            remoteMovie1.CustomFormatScore = 10;
            remoteMovie2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_do_not_upgrade_movie()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(1)));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(2)));

            remoteMovie1.CustomFormatScore = 10;
            remoteMovie2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_score_over_proper_when_download_propers_is_do_not_prefer_movie()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(1)));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(2)));

            remoteMovie1.CustomFormatScore = 10;
            remoteMovie2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Quality.Should().Be(Quality.WEBDL1080p);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteMovie.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_score_over_real_when_download_propers_is_do_not_prefer_movie()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(1, 0)));
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p, new Revision(1, 1)));

            remoteMovie1.CustomFormatScore = 10;
            remoteMovie2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteMovie1));
            decisions.Add(new DownloadDecision(remoteMovie2));

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Quality.Should().Be(Quality.WEBDL1080p);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteMovie.ParsedMovieInfo.Quality.Revision.Real.Should().Be(0);
            qualifiedReports.First().RemoteMovie.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void sort_download_decisions_based_on_indexer_priority_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p), indexerPriority: 25);
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p), indexerPriority: 50);
            var remoteMovie3 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p), indexerPriority: 1);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteMovie1), new DownloadDecision(remoteMovie2), new DownloadDecision(remoteMovie3) });

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovie3);
            qualifiedReports.Skip(1).First().RemoteMovie.Should().Be(remoteMovie1);
            qualifiedReports.Last().RemoteMovie.Should().Be(remoteMovie2);
        }

        [Test]
        public void ensure_download_decisions_indexer_priority_is_not_perfered_over_quality_movie()
        {
            var remoteMovie1 = GivenRemoteMovie(new QualityModel(Quality.HDTV720p), indexerPriority: 25);
            var remoteMovie2 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p), indexerPriority: 50);
            var remoteMovie3 = GivenRemoteMovie(new QualityModel(Quality.SDTV), indexerPriority: 1);
            var remoteMovie4 = GivenRemoteMovie(new QualityModel(Quality.WEBDL1080p), indexerPriority: 25);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteMovie1), new DownloadDecision(remoteMovie2), new DownloadDecision(remoteMovie3), new DownloadDecision(remoteMovie4) });

            var qualifiedReports = Subject.PrioritizeDecisionsForMovies(decisions);
            qualifiedReports.First().RemoteMovie.Should().Be(remoteMovie4);
            qualifiedReports.Skip(1).First().RemoteMovie.Should().Be(remoteMovie2);
            qualifiedReports.Skip(2).First().RemoteMovie.Should().Be(remoteMovie1);
            qualifiedReports.Last().RemoteMovie.Should().Be(remoteMovie3);
        }

}
}

using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class PendingSpecificationFixture : CoreTest<PendingSpecification>
    {
        private Series _series;
        private Episode _episode;
        private RemoteEpisode _remoteEpisode;

        private Series _otherSeries;
        private Episode _otherEpisode;

        private ReleaseInfo _releaseInfo;
        private ReleaseDecisionInformation _information = new(false, null);

        [SetUp]
        public void Setup()
        {
            CustomFormatsTestHelpers.GivenCustomFormats();

            _series = Builder<Series>.CreateNew()
                                     .With(e => e.QualityProfile = new QualityProfile
                                                                {
                                                                    UpgradeAllowed = true,
                                                                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                                                                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(),
                                                                    MinFormatScore = 0
                                                                })
                                     .Build();

            _episode = Builder<Episode>.CreateNew()
                                       .With(e => e.SeriesId = _series.Id)
                                       .Build();

            _otherSeries = Builder<Series>.CreateNew()
                                          .With(s => s.Id = 2)
                                          .Build();

            _otherEpisode = Builder<Episode>.CreateNew()
                                            .With(e => e.SeriesId = _otherSeries.Id)
                                            .With(e => e.Id = 2)
                                            .With(e => e.SeasonNumber = 2)
                                            .With(e => e.EpisodeNumber = 2)
                                            .Build();

            _releaseInfo = Builder<ReleaseInfo>.CreateNew()
                                               .Build();

            _remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                   .With(r => r.Series = _series)
                                                   .With(r => r.Episodes = new List<Episode> { _episode })
                                                   .With(r => r.ParsedEpisodeInfo = new ParsedEpisodeInfo { Quality = new QualityModel(Quality.DVD), Languages = new List<Language> { Language.Spanish } })
                                                   .With(r => r.CustomFormats = new List<CustomFormat>())
                                                   .Build();

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<RemoteEpisode>(), It.IsAny<long>()))
                  .Returns(new List<CustomFormat>());
        }

        private void GivenEmptyPendingQueue()
        {
            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<Queue.Queue>());
        }

        private void GivenPendingQueue(IEnumerable<RemoteEpisode> remoteEpisodes)
        {
            var queue = remoteEpisodes.Select(remoteEpisode => new Queue.Queue
            {
                RemoteEpisode = remoteEpisode
            });

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(queue.ToList());
        }

        [Test]
        public void should_return_true_when_pending_queue_is_empty()
        {
            GivenEmptyPendingQueue();

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_true_when_not_pushed_release()
        {
            _remoteEpisode.ReleaseSource = ReleaseSourceType.Rss;

            GivenEmptyPendingQueue();

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Never);
        }

        [Test]
        public void should_return_true_when_series_and_episode_is_not_pending()
        {
            GivenEmptyPendingQueue();

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<Queue.Queue>
                {
                    new()
                    {
                        RemoteEpisode = new RemoteEpisode
                        {
                            Series = _otherSeries,
                            Episodes = new List<Episode> { _otherEpisode }
                        }
                    }
                });

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_true_when_episode_is_not_pending()
        {
            GivenEmptyPendingQueue();

            _otherEpisode.SeriesId = _series.Id;

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<Queue.Queue>
                {
                    new()
                    {
                        RemoteEpisode = new RemoteEpisode
                        {
                            Series = _series,
                            Episodes = new List<Episode> { _otherEpisode }
                        }
                    }
                });

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_false_when_episode_is_pending()
        {
            GivenEmptyPendingQueue();

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<Queue.Queue>
                {
                    new()
                    {
                        RemoteEpisode = new RemoteEpisode
                        {
                            Series = _series,
                            Episodes = new List<Episode> { _episode }
                        }
                    }
                });

            Subject.IsSatisfiedBy(_remoteEpisode, _information).Accepted.Should().BeFalse();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        // Movie-domain members merged from Radarr

        private Movie _movie;

        private RemoteMovie _remoteMovie;

        private Movie _otherMovie;

        [SetUp]
        public void SetupMovie()
        {
            CustomFormatsTestHelpers.GivenCustomFormats();

            _movie = Builder<Movie>.CreateNew()
                .With(e => e.QualityProfile = new QualityProfile
                {
                    UpgradeAllowed = true,
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(),
                    MinFormatScore = 0
                })
                .Build();

            _otherMovie = Builder<Movie>.CreateNew()
                .With(s => s.Id = 2)
                .Build();

            _remoteMovie = Builder<RemoteMovie>.CreateNew()
                .With(r => r.Movie = _movie)
                .With(r => r.ParsedMovieInfo = new ParsedMovieInfo { Quality = new QualityModel(Quality.DVD), Languages = new List<Language> { Language.Spanish } })
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .Build();

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<RemoteMovie>(), It.IsAny<long>()))
                  .Returns(new List<CustomFormat>());
        }

        private void GivenPendingQueueMovie(IEnumerable<RemoteMovie> remoteMovies)
        {
            var queue = remoteMovies.Select(remoteMovie => new Queue.Queue
            {
                RemoteMovie = remoteMovie
            });

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(queue.ToList());
        }

        [Test]
        public void should_return_true_when_pending_queue_is_empty_movie()
        {
            GivenEmptyPendingQueue();

            Subject.IsSatisfiedBy(_remoteMovie, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_true_when_not_pushed_release_movie()
        {
            _remoteMovie.ReleaseSource = ReleaseSourceType.Rss;

            GivenEmptyPendingQueue();

            Subject.IsSatisfiedBy(_remoteMovie, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Never);
        }

        [Test]
        public void should_return_true_when_movie_is_not_pending()
        {
            GivenEmptyPendingQueue();

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<Queue.Queue>
                {
                    new()
                    {
                        RemoteMovie = new RemoteMovie
                        {
                            Movie = _otherMovie,
                        }
                    }
                });

            Subject.IsSatisfiedBy(_remoteMovie, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_false_when_movie_is_pending()
        {
            GivenEmptyPendingQueue();

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<Queue.Queue>
                {
                    new()
                    {
                        RemoteMovie = new RemoteMovie
                        {
                            Movie = _movie,
                        }
                    }
                });

            Subject.IsSatisfiedBy(_remoteMovie, _information).Accepted.Should().BeFalse();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

}
}

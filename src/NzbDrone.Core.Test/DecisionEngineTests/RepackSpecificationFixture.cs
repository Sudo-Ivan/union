using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class RepackSpecificationFixture : CoreTest<RepackSpecification>
    {
        private ParsedEpisodeInfo _parsedEpisodeInfo;
        private List<Episode> _episodes;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            _parsedEpisodeInfo = Builder<ParsedEpisodeInfo>.CreateNew()
                                                           .With(p => p.Quality = new QualityModel(Quality.SDTV,
                                                               new Revision(2, 0, false)))
                                                           .With(p => p.ReleaseGroup = "Sonarr")
                                                           .Build();

            _episodes = Builder<Episode>.CreateListOfSize(1)
                                        .All()
                                        .With(e => e.EpisodeFileId = 0)
                                        .BuildList();
        }

        [Test]
        public void should_return_true_if_it_is_not_a_repack()
        {
            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_there_are_is_no_episode_file()
        {
            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_is_a_repack_for_a_different_quality()
        {
            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.DVD))
                                                                .With(e => e.ReleaseGroup = "Sonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_is_a_repack_for_existing_file()
        {
            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "Sonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_is_a_repack_for_a_different_file()
        {
            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "NotSonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_release_group_for_existing_file_is_unknown()
        {
            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_release_group_for_release_is_unknown()
        {
            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _parsedEpisodeInfo.ReleaseGroup = null;

            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "Sonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_repacks_are_not_preferred()
        {
            Mocker.GetMock<IConfigService>()
            .Setup(s => s.DownloadPropersAndRepacks)
            .Returns(ProperDownloadTypes.DoNotPrefer);

            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "Sonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_repack_but_auto_download_repacks_is_true()
        {
            Mocker.GetMock<IConfigService>()
            .Setup(s => s.DownloadPropersAndRepacks)
            .Returns(ProperDownloadTypes.PreferAndUpgrade);

            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "Sonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_repack_but_auto_download_repacks_is_false()
        {
            Mocker.GetMock<IConfigService>()
            .Setup(s => s.DownloadPropersAndRepacks)
            .Returns(ProperDownloadTypes.DoNotUpgrade);

            _parsedEpisodeInfo.Quality.Revision.IsRepack = true;
            _episodes.First().EpisodeFileId = 1;
            _episodes.First().EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                                .With(e => e.ReleaseGroup = "Sonarr")
                                                                .Build();

            var remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                      .With(e => e.ParsedEpisodeInfo = _parsedEpisodeInfo)
                                                      .With(e => e.Episodes = _episodes)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        // Movie-domain members merged from Radarr

        private ParsedMovieInfo _parsedMovieInfo;

        private Movie _movie;

        [SetUp]
        public void SetupMovie()
        {
            Mocker.Resolve<UpgradableSpecification>();

            _parsedMovieInfo = Builder<ParsedMovieInfo>.CreateNew()
                                                           .With(p => p.Quality = new QualityModel(Quality.SDTV,
                                                               new Revision(2, 0, false)))
                                                           .With(p => p.ReleaseGroup = "Radarr")
                                                           .Build();

            _movie = Builder<Movie>.CreateNew()
                                        .With(e => e.MovieFileId = 0)
                                        .Build();
        }

        [Test]
        public void should_return_true_if_it_is_not_a_repack_movie()
        {
            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_there_are_is_no_movie_file()
        {
            _parsedMovieInfo.Quality.Revision.IsRepack = true;

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_is_a_repack_for_a_different_quality_movie()
        {
            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.DVD))
                                                                .With(e => e.ReleaseGroup = "Radarr")
                                                                .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_is_a_repack_for_existing_file_movie()
        {
            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "Radarr")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_is_a_repack_for_a_different_file_movie()
        {
            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "NotRadarr")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_release_group_for_existing_file_is_unknown_movie()
        {
            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_release_group_for_release_is_unknown_movie()
        {
            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _parsedMovieInfo.ReleaseGroup = null;

            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "Radarr")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_repack_but_auto_download_repack_is_false()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "Radarr")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_repack_but_auto_download_repack_is_true()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "Radarr")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_repacks_are_not_preferred_movie()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            _parsedMovieInfo.Quality.Revision.IsRepack = true;
            _movie.MovieFileId = 1;
            _movie.MovieFile = Builder<MovieFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.SDTV))
                                                 .With(e => e.ReleaseGroup = "Radarr")
                                                 .Build();

            var remoteMovie = Builder<RemoteMovie>.CreateNew()
                                                      .With(e => e.ParsedMovieInfo = _parsedMovieInfo)
                                                      .With(e => e.Movie = _movie)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteMovie, new()).Accepted.Should().BeTrue();
        }
}
}

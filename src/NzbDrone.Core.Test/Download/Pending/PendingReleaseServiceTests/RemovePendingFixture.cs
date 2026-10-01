using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Crypto;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Download.Pending.PendingReleaseServiceTests
{
    [TestFixture]
    public class RemovePendingFixture : CoreTest<PendingReleaseService>
    {
        private List<PendingRelease> _pending;
        private Episode _episode;

        [SetUp]
        public void Setup()
        {
            _pending = new List<PendingRelease>();

            _episode = Builder<Episode>.CreateNew()
                                       .Build();

            Mocker.GetMock<IPendingReleaseRepository>()
                 .Setup(s => s.AllBySeriesId(It.IsAny<int>()))
                 .Returns(_pending);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_pending);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<int>()))
                  .Returns(new Series());

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Series> { new Series() });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedEpisodeInfo>(), It.IsAny<Series>()))
                  .Returns(new RemoteEpisode { Episodes = new List<Episode> { _episode } });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetEpisodes(It.IsAny<ParsedEpisodeInfo>(), It.IsAny<Series>(), It.IsAny<bool>(), null))
                  .Returns(new List<Episode> { _episode });
        }

        private void AddPending(int id, int seasonNumber, int[] episodes)
        {
            _pending.Add(new PendingRelease
             {
                 Id = id,
                 Title = "Series.Title.S01E05.abc-Sonarr",
                 ParsedEpisodeInfo = new ParsedEpisodeInfo { SeasonNumber = seasonNumber, EpisodeNumbers = episodes },
                 Release = Builder<ReleaseInfo>.CreateNew().Build()
             });
        }

        private void InitializeReleases()
        {
            Subject.Handle(new ApplicationStartedEvent());
        }

        [Test]
        public void should_remove_same_release()
        {
            AddPending(id: 1, seasonNumber: 2, episodes: new[] { 3 });

            var queueId = HashConverter.GetHashInt31($"pending-{1}");

            InitializeReleases();
            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1);
        }

        [Test]
        public void should_remove_multiple_releases_release()
        {
            AddPending(id: 1, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 2, seasonNumber: 2, episodes: new[] { 2 });
            AddPending(id: 3, seasonNumber: 2, episodes: new[] { 3 });
            AddPending(id: 4, seasonNumber: 2, episodes: new[] { 3 });

            var queueId = HashConverter.GetHashInt31($"pending-{3}");

            InitializeReleases();
            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(3, 4);
        }

        [Test]
        public void should_not_remove_different_season()
        {
            AddPending(id: 1, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 2, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 3, seasonNumber: 3, episodes: new[] { 1 });
            AddPending(id: 4, seasonNumber: 3, episodes: new[] { 1 });

            var queueId = HashConverter.GetHashInt31($"pending-{1}");

            InitializeReleases();
            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1, 2);
        }

        [Test]
        public void should_not_remove_different_episodes()
        {
            AddPending(id: 1, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 2, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 3, seasonNumber: 2, episodes: new[] { 2 });
            AddPending(id: 4, seasonNumber: 2, episodes: new[] { 3 });

            var queueId = HashConverter.GetHashInt31($"pending-{1}");

            InitializeReleases();
            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1, 2);
        }

        [Test]
        public void should_not_remove_multiepisodes()
        {
            AddPending(id: 1, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 2, seasonNumber: 2, episodes: new[] { 1, 2 });

            var queueId = HashConverter.GetHashInt31($"pending-{1}");

            InitializeReleases();
            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1);
        }

        [Test]
        public void should_not_remove_singleepisodes()
        {
            AddPending(id: 1, seasonNumber: 2, episodes: new[] { 1 });
            AddPending(id: 2, seasonNumber: 2, episodes: new[] { 1, 2 });

            var queueId = HashConverter.GetHashInt31($"pending-{2}");

            InitializeReleases();
            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(2);
        }

        private void AssertRemoved(params int[] ids)
        {
            Mocker.GetMock<IPendingReleaseRepository>().Verify(c => c.DeleteMany(It.Is<IEnumerable<int>>(s => s.SequenceEqual(ids))));
        }

        // Movie-domain members merged from Radarr

        private Movie _movie;

        [SetUp]
        public void SetupMovie()
        {
            _pending = new List<PendingRelease>();

            _movie = Builder<Movie>.CreateNew()
                                       .Build();

            Mocker.GetMock<IPendingReleaseRepository>()
                 .Setup(s => s.AllByMovieId(It.IsAny<int>()))
                 .Returns(_pending);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_pending);

            Mocker.GetMock<IMovieService>()
                  .Setup(s => s.GetMovie(It.IsAny<int>()))
                  .Returns(_movie);

            Mocker.GetMock<IMovieService>()
                  .Setup(s => s.GetMovies(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Movie> { _movie });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetMovie(It.IsAny<string>()))
                  .Returns(_movie);
        }

        private void AddPendingMovie(int id, string title, int year)
        {
            _pending.Add(new PendingRelease
            {
                Id = id,
                Title = "Movie.Title.2020.720p-Radarr",
                ParsedMovieInfo = new ParsedMovieInfo { MovieTitles = new List<string> { title }, Year = year },
                Release = Builder<ReleaseInfo>.CreateNew().Build(),
                MovieId = _movie.Id
            });
        }

        [Test]
        public void should_remove_same_release_movie()
        {
            AddPendingMovie(id: 1, title: "Movie", year: 2001);

            var queueId = HashConverter.GetHashInt31(string.Format("pending-{0}-movie{1}", 1, _movie.Id));

            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1);
        }

}
}

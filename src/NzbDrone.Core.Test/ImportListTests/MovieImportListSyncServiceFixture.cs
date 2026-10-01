using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ImportListTests
{
    public class MovieImportListSyncServiceFixture : CoreTest<ImportListSyncService>
    {
        private ImportListFetchResult _importListFetch;
        private List<ImportListMovie> _list1Movies;
        private List<ImportListMovie> _list2Movies;

        private List<Movie> _existingMovies;
        private List<IImportList> _importLists;
        private ImportListSyncCommand _commandAll;
        private ImportListSyncCommand _commandSingle;

        [SetUp]
        public void Setup()
        {
            _importLists = new List<IImportList>();

            _list1Movies = Builder<ImportListMovie>.CreateListOfSize(5)
                .Build().ToList();

            _existingMovies = Builder<Movie>.CreateListOfSize(3)
                .TheFirst(1)
                .With(s => s.TmdbId = 6)
                .With(s => s.ImdbId = "6")
                .TheNext(1)
                .With(s => s.TmdbId = 7)
                .With(s => s.ImdbId = "7")
                .TheNext(1)
                .With(s => s.TmdbId = 8)
                .With(s => s.ImdbId = "8")
                .Build().ToList();

            _list2Movies = Builder<ImportListMovie>.CreateListOfSize(3)
                .TheFirst(1)
                .With(s => s.TmdbId = 6)
                .With(s => s.ImdbId = "6")
                .TheNext(1)
                .With(s => s.TmdbId = 7)
                .With(s => s.ImdbId = "7")
                .TheNext(1)
                .With(s => s.TmdbId = 8)
                .With(s => s.ImdbId = "8")
                .Build().ToList();

            _importListFetch = new ImportListFetchResult
            {
                Movies = _list1Movies,
                AnyFailure = false,
                SyncedLists = 1
            };

            _commandAll = new ImportListSyncCommand
            {
            };

            _commandSingle = new ImportListSyncCommand
            {
                DefinitionId = 1
            };

            Mocker.GetMock<IImportListFactory>()
                  .Setup(v => v.AutomaticAddEnabled(It.IsAny<bool>()))
                  .Returns(() => _importLists.Where(x => (x.Definition as ImportListDefinition).EnableAutomaticAdd).ToList());

            Mocker.GetMock<IImportListFactory>()
                  .Setup(v => v.Enabled(It.IsAny<bool>()))
                  .Returns(() => _importLists.Where(x => (x.Definition as ImportListDefinition).Enabled).ToList());

            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(v => v.All())
                  .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.MovieExists(It.IsAny<Movie>()))
                  .Returns(false);

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.AllMovieTmdbIds())
                  .Returns(new List<int>());

            Mocker.GetMock<IFetchAndParseImportList>()
                  .Setup(v => v.Fetch())
                  .Returns(_importListFetch);
        }

        private void GivenListFailure()
        {
            _importListFetch.AnyFailure = true;
        }

        private void GivenNoListSync()
        {
            _importListFetch.SyncedLists = 0;
        }

        private void GivenCleanLevel(ListSyncLevelType cleanLevel, int? tagId = null)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(v => v.ListSyncLevel)
                  .Returns(cleanLevel);

            if (tagId.HasValue)
            {
                Mocker.GetMock<IConfigService>()
                    .SetupGet(v => v.ListSyncTag)
                    .Returns(tagId.Value);
            }
        }

        private void GivenList(int id, bool enabledAuto, int lastSyncHoursOffset = 0, bool pendingRemovals = true, DateTime? disabledTill = null)
        {
            var importListDefinition = new ImportListDefinition { Id = id, EnableAuto = enabledAuto, EnableAutomaticAdd = enabledAuto, Enabled = true };

            Mocker.GetMock<IImportListFactory>()
                  .Setup(v => v.Get(id))
                  .Returns(importListDefinition);

            var mockImportList = new Mock<IImportList>();
            mockImportList.SetupGet(s => s.Definition).Returns(importListDefinition);
            mockImportList.SetupGet(s => s.MinRefreshInterval).Returns(TimeSpan.FromHours(12));

            var status = new ImportListStatus()
            {
                LastInfoSync = DateTime.UtcNow.AddHours(lastSyncHoursOffset),
                HasRemovedItemSinceLastClean = pendingRemovals,
                DisabledTill = disabledTill
            };

            if (disabledTill.HasValue)
            {
                _importListFetch.AnyFailure = true;
            }

            Mocker.GetMock<IImportListStatusService>()
                .Setup(v => v.GetListStatus(id))
                .Returns(status);

            _importLists.Add(mockImportList.Object);
        }

        [Test]
        public void should_not_clean_library_if_config_value_disable()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.Disabled);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.GetAllMovies(), Times.Never());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(new List<Movie>(), true), Times.Never());
        }

        [Test]
        public void should_not_clean_library_or_process_movies_if_no_synced_lists()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenList(1, true, pendingRemovals: false);
            GivenCleanLevel(ListSyncLevelType.LogOnly);
            GivenNoListSync();

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.GetAllMovies(), Times.Never());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(new List<Movie>(), true), Times.Never());

            Mocker.GetMock<IImportListExclusionService>()
                  .Verify(v => v.All(), Times.Never);
        }

        [Test]
        public void should_log_only_on_clean_library_if_config_value_logonly()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.LogOnly);

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.GetAllMovies())
                  .Returns(_existingMovies);

            Mocker.GetMock<IImportListMovieService>()
                .Setup(v => v.GetAllListMovies())
                .Returns(_list1Movies);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.GetAllMovies(), Times.Once());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.DeleteMovie(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(new List<Movie>(), true), Times.Once());
        }

        [Test]
        public void should_unmonitor_on_clean_library_if_config_value_keepAndUnmonitor()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.KeepAndUnmonitor);

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.GetAllMovies())
                  .Returns(_existingMovies);

            Mocker.GetMock<IImportListMovieService>()
                .Setup(v => v.GetAllListMovies())
                .Returns(_list1Movies);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.GetAllMovies(), Times.Once());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.DeleteMovie(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(It.Is<List<Movie>>(s => s.Count == 3 && s.All(m => !m.Monitored)), true), Times.Once());
        }

        [Test]
        public void should_tag_movie_on_clean_library_if_config_value_keepAndTag()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.KeepAndTag, 1);

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.GetAllMovies())
                  .Returns(_existingMovies);

            Mocker.GetMock<IImportListMovieService>()
                .Setup(v => v.GetAllListMovies())
                .Returns(_list1Movies);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.GetAllMovies(), Times.Once());

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(It.Is<List<Movie>>(s => s.Count == 3 && s.All(m => m.Tags.Contains(1))), true), Times.Once());
        }

        [Test]
        public void should_not_clean_on_clean_library_if_tmdb_match()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies[0].TmdbId = 6;

            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.KeepAndUnmonitor);

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.GetAllMovies())
                  .Returns(_existingMovies);

            Mocker.GetMock<IImportListMovieService>()
                .Setup(v => v.GetAllListMovies())
                .Returns(_list1Movies);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(It.Is<List<Movie>>(s => s.Count == 2 && s.All(m => !m.Monitored)), true), Times.Once());
        }

        [Test]
        public void should_fallback_to_imdbid_on_clean_library_if_tmdb_not_found()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies[0].TmdbId = 0;
            _importListFetch.Movies[0].ImdbId = "6";

            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.KeepAndUnmonitor);

            Mocker.GetMock<IMovieService>()
                  .Setup(v => v.GetAllMovies())
                  .Returns(_existingMovies);

            Mocker.GetMock<IImportListMovieService>()
                .Setup(v => v.GetAllListMovies())
                .Returns(_list1Movies);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(It.Is<List<Movie>>(s => s.Count == 2 && s.All(m => !m.Monitored)), true), Times.Once());
        }

        [Test]
        public void should_not_clean_if_list_failures()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenListFailure();

            GivenList(1, true, disabledTill: DateTime.UtcNow.AddHours(1));
            GivenCleanLevel(ListSyncLevelType.LogOnly);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.UpdateMovie(new List<Movie>(), true), Times.Never());
        }

        [Test]
        public void should_add_new_movies_from_single_list_to_library()
        {
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel(ListSyncLevelType.Disabled);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddMovieService>()
                  .Verify(v => v.AddMovies(It.Is<List<Movie>>(s => s.Count == 5), true), Times.Once());
        }

        [Test]
        public void should_add_new_movies_from_multiple_list_to_library()
        {
            _list2Movies.ForEach(m => m.ListId = 2);
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies.AddRange(_list2Movies);

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel(ListSyncLevelType.Disabled);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddMovieService>()
                  .Verify(v => v.AddMovies(It.Is<List<Movie>>(s => s.Count == 8), true), Times.Once());
        }

        [Test]
        public void should_add_new_movies_from_enabled_lists_to_library()
        {
            _list2Movies.ForEach(m => m.ListId = 2);
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies.AddRange(_list2Movies);

            GivenList(1, true);
            GivenList(2, false);

            GivenCleanLevel(ListSyncLevelType.Disabled);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddMovieService>()
                  .Verify(v => v.AddMovies(It.Is<List<Movie>>(s => s.Count == 5), true), Times.Once());
        }

        [Test]
        public void should_not_add_duplicate_movies_from_separate_lists()
        {
            _list2Movies.ForEach(m => m.ListId = 2);
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies.AddRange(_list2Movies);
            _importListFetch.Movies[0].TmdbId = 4;

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel(ListSyncLevelType.Disabled);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddMovieService>()
                  .Verify(v => v.AddMovies(It.Is<List<Movie>>(s => s.Count == 7), true), Times.Once());
        }

        [Test]
        public void should_not_add_movie_from_on_exclusion_list()
        {
            _list2Movies.ForEach(m => m.ListId = 2);
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies.AddRange(_list2Movies);

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel(ListSyncLevelType.Disabled);

            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(v => v.All())
                  .Returns(new List<ImportListExclusion> { new ImportListExclusion { TmdbId = _existingMovies[0].TmdbId } });

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddMovieService>()
                  .Verify(v => v.AddMovies(It.Is<List<Movie>>(s => s.Count == 7 && s.All(m => m.TmdbId != _existingMovies[0].TmdbId)), true), Times.Once());
        }

        [Test]
        public void should_not_add_movie_that_exists_in_library()
        {
            _list2Movies.ForEach(m => m.ListId = 2);
            _importListFetch.Movies.ForEach(m => m.ListId = 1);
            _importListFetch.Movies.AddRange(_list2Movies);

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel(ListSyncLevelType.Disabled);

            Mocker.GetMock<IMovieService>()
                 .Setup(v => v.AllMovieTmdbIds())
                 .Returns(new List<int> { _existingMovies[0].TmdbId });

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddMovieService>()
                  .Verify(v => v.AddMovies(It.Is<List<Movie>>(s => s.Count == 7 && s.All(m => m.TmdbId != _existingMovies[0].TmdbId)), true), Times.Once());
        }
    }
}

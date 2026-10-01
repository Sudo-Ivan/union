using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Download.DownloadHistoryTests
{
    [TestFixture]
    public class DownloadHistoryRepositoryFixture : DbTest<DownloadHistoryRepository, DownloadHistory>
    {
        private Series _series1;
        private Series _series2;

        [SetUp]
        public void Setup()
        {
            _series1 = Builder<Series>.CreateNew()
                                      .With(s => s.Id = 7)
                                      .Build();

            _series2 = Builder<Series>.CreateNew()
                                      .With(s => s.Id = 8)
                                      .Build();
        }

        [Test]
        public void should_delete_history_items_by_seriesId()
        {
            var items = Builder<DownloadHistory>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.Id = 0)
                .With(c => c.SeriesId = _series2.Id)
                .TheRest()
                .With(c => c.Id = 0)
                .With(c => c.SeriesId = _series1.Id)
                .BuildListOfNew();

            Db.InsertMany(items);

            Subject.DeleteBySeriesIds(new List<int> { _series1.Id });

            var removedItems = Subject.All().Where(h => h.SeriesId == _series1.Id);
            var nonRemovedItems = Subject.All().Where(h => h.SeriesId == _series2.Id);

            removedItems.Should().HaveCount(0);
            nonRemovedItems.Should().HaveCount(1);
        }

        // Movie-domain members merged from Radarr

        private Movie _movie1;

        private Movie _movie2;

        [SetUp]
        public void SetupMovie()
        {
            _movie1 = Builder<Movie>.CreateNew()
                                     .With(s => s.Id = 7)
                                     .Build();

            _movie2 = Builder<Movie>.CreateNew()
                                     .With(s => s.Id = 8)
                                     .Build();
        }

        [Test]
        public void should_delete_history_items_by_movieId()
        {
            var items = Builder<DownloadHistory>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.Id = 0)
                .With(c => c.MovieId = _movie2.Id)
                .TheRest()
                .With(c => c.Id = 0)
                .With(c => c.MovieId = _movie1.Id)
                .BuildListOfNew();

            Db.InsertMany(items);

            Subject.DeleteByMovieIds(new List<int> { _movie1.Id });

            var removedItems = Subject.All().Where(h => h.MovieId == _movie1.Id);
            var nonRemovedItems = Subject.All().Where(h => h.MovieId == _movie2.Id);

            removedItems.Should().HaveCount(0);
            nonRemovedItems.Should().HaveCount(1);
        }
}
}

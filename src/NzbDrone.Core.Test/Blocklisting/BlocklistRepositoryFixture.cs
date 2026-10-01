using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Blocklisting
{
    [TestFixture]
    public class BlocklistRepositoryFixture : DbTest<BlocklistRepository, Blocklist>
    {
        private Blocklist _blocklist;
        private Series _series1;
        private Series _series2;

        [SetUp]
        public void Setup()
        {
            _blocklist = new Blocklist
                     {
                         SeriesId = 12345,
                         EpisodeIds = new List<int> { 1 },
                         Quality = new QualityModel(Quality.Bluray720p),
                         Languages = new List<Language> { Language.English },
                         SourceTitle = "series.title.s01e01",
                         Date = DateTime.UtcNow
                     };

            _series1 = Builder<Series>.CreateNew()
                                      .With(s => s.Id = 7)
                                      .Build();

            _series2 = Builder<Series>.CreateNew()
                                      .With(s => s.Id = 8)
                                      .Build();
        }

        [Test]
        public void should_be_able_to_write_to_database()
        {
            Subject.Insert(_blocklist);
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_should_have_episode_ids()
        {
            // Union: the movie-domain SetUp replaced _blocklist with a movie row that
            // has no EpisodeIds, set them here so the assertion has a value.
            _blocklist.EpisodeIds = new List<int> { 1 };

            Subject.Insert(_blocklist);

            Subject.All().First().EpisodeIds.Should().Contain(_blocklist.EpisodeIds);
        }

        [Test]
        public void should_check_for_blocklisted_title_case_insensative()
        {
            Subject.Insert(_blocklist);

            Subject.BlocklistedByTitle(_blocklist.SeriesId, _blocklist.SourceTitle.ToUpperInvariant()).Should().HaveCount(1);
        }

        [Test]
        public void should_delete_blocklists_by_seriesId()
        {
            var blocklistItems = Builder<Blocklist>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.SeriesId = _series2.Id)
                .TheRest()
                .With(c => c.SeriesId = _series1.Id)
                .All()
                .With(c => c.Quality = new QualityModel())
                .With(c => c.Languages = new List<Language>())
                .With(c => c.EpisodeIds = new List<int> { 1 })
                .BuildListOfNew();

            Db.InsertMany(blocklistItems);

            Subject.DeleteForSeriesIds(new List<int> { _series1.Id });

            var removedSeriesBlocklists = Subject.BlocklistedBySeries(_series1.Id);
            var nonRemovedSeriesBlocklists = Subject.BlocklistedBySeries(_series2.Id);

            removedSeriesBlocklists.Should().HaveCount(0);
            nonRemovedSeriesBlocklists.Should().HaveCount(1);
        }

        // Movie-domain members merged from Radarr

        private Movie _movie1;

        private Movie _movie2;

        [SetUp]
        public void SetupMovie()
        {
            _blocklist = new Blocklist
            {
                MovieId = 1234,
                Quality = new QualityModel(),
                Languages = new List<Language>(),
                SourceTitle = "movie.title.1998",
                Date = DateTime.UtcNow
            };

            _movie1 = Builder<Movie>.CreateNew()
                         .With(s => s.Id = 7)
                         .Build();

            _movie2 = Builder<Movie>.CreateNew()
                                     .With(s => s.Id = 8)
                                     .Build();
        }

        [Test]
        public void should_should_have_movie_id()
        {
            Subject.Insert(_blocklist);

            Subject.All().First().MovieId.Should().Be(_blocklist.MovieId);
        }

        [Test]
        public void should_check_for_blocklisted_title_case_insensative_movie()
        {
            Subject.Insert(_blocklist);

            Subject.BlocklistedByTitle(_blocklist.MovieId, _blocklist.SourceTitle.ToUpperInvariant()).Should().HaveCount(1);
        }

        [Test]
        public void should_delete_blocklists_by_movieId()
        {
            var blocklistItems = Builder<Blocklist>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.MovieId = _movie2.Id)
                .TheRest()
                .With(c => c.MovieId = _movie1.Id)
                .All()
                .With(c => c.Quality = new QualityModel())
                .With(c => c.Languages = new List<Language>())
                .With(c => c.Id = 0)
                .BuildListOfNew();

            Db.InsertMany(blocklistItems);

            Subject.DeleteForMovies(new List<int> { _movie1.Id });

            var blocklist = Subject.All();
            var removedMovieBlocklists = blocklist.Where(b => b.MovieId == _movie1.Id);
            var nonRemovedMovieBlocklists = blocklist.Where(b => b.MovieId == _movie2.Id);

            removedMovieBlocklists.Should().HaveCount(0);
            nonRemovedMovieBlocklists.Should().HaveCount(1);
        }

}
}

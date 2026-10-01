using FluentAssertions;
using NUnit.Framework;
using Radarr.Api.V3.Movies;
using Sonarr.Api.V3.Blocklist;
using Sonarr.Api.V3.Series;
using MovieBlocklistResource = Sonarr.Api.V3.Blocklist.BlocklistResource;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class BlocklistFixture : IntegrationTest
    {
        private SeriesResource _series;
        private MovieResource _movie;

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_add_to_blocklist()
        {
            _series = EnsureSeries(266189, "The Blacklist");

            Blocklist.Post(new BlocklistResource
            {
                SeriesId = _series.Id,
                SourceTitle = "Blacklist.S01E01.Brought.To.You.By-BoomBoxHD"
            });
        }

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_add_movie_to_blocklist()
        {
            _movie = EnsureMovie(11, "The Blocklist");

            MovieBlocklist.Post(new MovieBlocklistResource
            {
                MovieId = _movie.Id,
                SourceTitle = "Blocklist.S01E01.Brought.To.You.By-BoomBoxHD"
            });
        }

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_get_all_blocklisted()
        {
            var result = Blocklist.GetPaged(0, 1000, "date", "desc");

            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(1);
            result.Records.Should().NotBeNullOrEmpty();
        }

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_remove_from_blocklist()
        {
            Blocklist.Delete(1);

            var result = Blocklist.GetPaged(0, 1000, "date", "desc");

            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(0);
        }
    }
}

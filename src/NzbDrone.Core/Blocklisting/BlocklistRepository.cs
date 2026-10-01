using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistRepository : IBasicRepository<Blocklist>
    {
        List<Blocklist> BlocklistedByTitle(int id, string sourceTitle);
        List<Blocklist> BlocklistedByTorrentInfoHash(int id, string torrentInfoHash);
        List<Blocklist> BlocklistedBySeries(int seriesId);
        List<Blocklist> BlocklistedByMovie(int movieId);
        void DeleteForSeriesIds(List<int> seriesIds);
        void DeleteForMovies(List<int> movieIds);
    }

    public class BlocklistRepository : BasicRepository<Blocklist>, IBlocklistRepository
    {
        public BlocklistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Blocklist> BlocklistedByTitle(int id, string sourceTitle)
        {
            return Query(e => (e.SeriesId == id || e.MovieId == id) && e.SourceTitle.Contains(sourceTitle));
        }

        public List<Blocklist> BlocklistedByTorrentInfoHash(int id, string torrentInfoHash)
        {
            return Query(e => (e.SeriesId == id || e.MovieId == id) && e.TorrentInfoHash.Contains(torrentInfoHash));
        }

        public List<Blocklist> BlocklistedBySeries(int seriesId)
        {
            return Query(b => b.SeriesId == seriesId);
        }

        public List<Blocklist> BlocklistedByMovie(int movieId)
        {
            var builder = Builder().Join<Blocklist, Movie>((h, a) => h.MovieId == a.Id)
                                   .Where<Blocklist>(h => h.MovieId == movieId);

            return _database.QueryJoined<Blocklist, Movie>(builder, (blocklist, movie) =>
            {
                blocklist.Movie = movie;
                return blocklist;
            }).OrderByDescending(h => h.Date).ToList();
        }

        public void DeleteForSeriesIds(List<int> seriesIds)
        {
            Delete(x => seriesIds.Contains(x.SeriesId));
        }

        public void DeleteForMovies(List<int> movieIds)
        {
            Delete(x => movieIds.Contains(x.MovieId));
        }

        public override PagingSpec<Blocklist> GetPaged(PagingSpec<Blocklist> pagingSpec)
        {
            var sortingByQuality = string.Equals(pagingSpec.SortKey, "quality", StringComparison.OrdinalIgnoreCase);
            var customSortExpression = sortingByQuality ? "COALESCE(\"r\".\"Score\", \"rm\".\"Score\", -1)" : null;

            pagingSpec.Records = GetPagedRecords(PagedBuilder(sortingByQuality), pagingSpec, PagedQuery, customSortExpression);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(Blocklist))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(PagedBuilder(sortingByQuality).Select(typeof(Blocklist)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        protected override SqlBuilder PagedBuilder() => PagedBuilder(false);

        private SqlBuilder PagedBuilder(bool joinQualityRanks)
        {
            var builder = Builder()
                .LeftJoin<Blocklist, Series>((b, m) => b.SeriesId == m.Id)
                .LeftJoin<Blocklist, Movie>((b, m) => b.MovieId == m.Id)
                .LeftJoin<Movie, MovieMetadata>((m, mm) => m.MovieMetadataId == mm.Id);

            if (joinQualityRanks)
            {
                var qualityIdExpr = _database.DatabaseType == DatabaseType.PostgreSQL
                    ? "(\"Blocklist\".\"Quality\"::jsonb ->> 'quality')::int"
                    : "json_extract(\"Blocklist\".\"Quality\", '$.quality')";

                builder.LeftJoin(
                    $"\"QualityProfileQualityRanks\" AS \"r\" " +
                    $"ON \"r\".\"ProfileId\" = \"Series\".\"QualityProfileId\" " +
                    $"AND \"r\".\"QualityId\" = {qualityIdExpr}");

                builder.LeftJoin(
                    $"\"QualityProfileQualityRanks\" AS \"rm\" " +
                    $"ON \"rm\".\"ProfileId\" = \"Movie\".\"QualityProfileId\" " +
                    $"AND \"rm\".\"QualityId\" = {qualityIdExpr}");
            }

            return builder;
        }

        protected override IEnumerable<Blocklist> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<Blocklist, Series, Movie>(builder, (blocklist, series, movie) =>
            {
                if (series != null && series.Id > 0)
                {
                    blocklist.Series = series;
                }

                if (movie != null && movie.Id > 0)
                {
                    blocklist.Movie = movie;
                }

                return blocklist;
            });
    }
}

using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupDuplicateMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupDuplicateMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteDuplicateSeriesMetadata();
            DeleteDuplicateEpisodeMetadata();
            DeleteDuplicateEpisodeImages();
            DeleteDuplicateMovieMetadata();
            DeleteDuplicateMovieFileMetadata();
        }

        private void DeleteDuplicateSeriesMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                                     WHERE ""Id"" IN (
                                         SELECT MIN(""Id"") FROM ""MetadataFiles""
                                         WHERE ""Type"" = 1
                                         AND ""SeriesId"" > 0
                                         GROUP BY ""SeriesId"", ""Consumer""
                                         HAVING COUNT(""SeriesId"") > 1
                                     )");
        }

        private void DeleteDuplicateEpisodeMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                                     WHERE ""Id"" IN (
                                         SELECT MIN(""Id"") FROM ""MetadataFiles""
                                         WHERE ""Type"" = 2
                                         AND ""EpisodeFileId"" > 0
                                         GROUP BY ""EpisodeFileId"", ""Consumer""
                                         HAVING COUNT(""EpisodeFileId"") > 1
                                     )");
        }

        private void DeleteDuplicateEpisodeImages()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                                     WHERE ""Id"" IN (
                                         SELECT MIN(""Id"") FROM ""MetadataFiles""
                                         WHERE ""Type"" = 5
                                         AND ""EpisodeFileId"" > 0
                                         GROUP BY ""EpisodeFileId"", ""Consumer""
                                         HAVING COUNT(""EpisodeFileId"") > 1
                                     )");
        }

        private void DeleteDuplicateMovieMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" = 1
                                 AND ""MovieId"" > 0
                                 GROUP BY ""MovieId"", ""Consumer""
                                 HAVING COUNT(""MovieId"") > 1
                             )");
        }

        private void DeleteDuplicateMovieFileMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" = 1
                                 AND ""MovieFileId"" > 0
                                 GROUP BY ""MovieFileId"", ""Consumer""
                                 HAVING COUNT(""MovieFileId"") > 1
                             )");
        }
    }
}

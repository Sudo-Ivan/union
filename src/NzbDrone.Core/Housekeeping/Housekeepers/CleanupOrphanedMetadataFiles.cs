using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteOrphanedBySeries();
            DeleteOrphanedByEpisodeFile();
            DeleteOrphanedByMovie();
            DeleteOrphanedByMovieFile();
            DeleteWhereFileIsZero();
        }

        private void DeleteOrphanedBySeries()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                                     LEFT OUTER JOIN ""Series""
                                     ON ""MetadataFiles"".""SeriesId"" = ""Series"".""Id""
                                     WHERE ""MetadataFiles"".""SeriesId"" > 0
                                     AND ""Series"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByEpisodeFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                                     LEFT OUTER JOIN ""EpisodeFiles""
                                     ON ""MetadataFiles"".""EpisodeFileId"" = ""EpisodeFiles"".""Id""
                                     WHERE ""MetadataFiles"".""EpisodeFileId"" > 0
                                     AND ""EpisodeFiles"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByMovie()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""Movies""
                             ON ""MetadataFiles"".""MovieId"" = ""Movies"".""Id""
                             WHERE ""MetadataFiles"".""MovieId"" > 0
                             AND ""Movies"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByMovieFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""MovieFiles""
                             ON ""MetadataFiles"".""MovieFileId"" = ""MovieFiles"".""Id""
                             WHERE ""MetadataFiles"".""MovieFileId"" > 0
                             AND ""MovieFiles"".""Id"" IS NULL)");
        }

        private void DeleteWhereFileIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""Id"" FROM ""MetadataFiles""
                                     WHERE ""Type"" IN (1, 2, 5)
                                     AND COALESCE(""EpisodeFileId"", 0) = 0
                                     AND COALESCE(""MovieFileId"", 0) = 0)");
        }
    }
}

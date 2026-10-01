using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedExtraFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedExtraFiles(IMainDatabase database)
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
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""ExtraFiles"".""Id"" FROM ""ExtraFiles""
                                     LEFT OUTER JOIN ""Series""
                                     ON ""ExtraFiles"".""SeriesId"" = ""Series"".""Id""
                                     WHERE ""ExtraFiles"".""SeriesId"" > 0
                                     AND ""Series"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByEpisodeFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""ExtraFiles"".""Id"" FROM ""ExtraFiles""
                                     LEFT OUTER JOIN ""EpisodeFiles""
                                     ON ""ExtraFiles"".""EpisodeFileId"" = ""EpisodeFiles"".""Id""
                                     WHERE ""ExtraFiles"".""EpisodeFileId"" > 0
                                     AND ""EpisodeFiles"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByMovie()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                             WHERE ""Id"" IN (
                             SELECT ""ExtraFiles"".""Id"" FROM ""ExtraFiles""
                             LEFT OUTER JOIN ""Movies""
                             ON ""ExtraFiles"".""MovieId"" = ""Movies"".""Id""
                             WHERE ""ExtraFiles"".""MovieId"" > 0
                             AND ""Movies"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByMovieFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                             WHERE ""Id"" IN (
                             SELECT ""ExtraFiles"".""Id"" FROM ""ExtraFiles""
                             LEFT OUTER JOIN ""MovieFiles""
                             ON ""ExtraFiles"".""MovieFileId"" = ""MovieFiles"".""Id""
                             WHERE ""ExtraFiles"".""MovieFileId"" > 0
                             AND ""MovieFiles"".""Id"" IS NULL)");
        }

        private void DeleteWhereFileIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""Id"" FROM ""ExtraFiles""
                                     WHERE COALESCE(""EpisodeFileId"", 0) = 0
                                     AND COALESCE(""MovieFileId"", 0) = 0)");
        }
    }
}

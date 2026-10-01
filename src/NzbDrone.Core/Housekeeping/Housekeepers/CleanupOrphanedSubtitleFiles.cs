using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedSubtitleFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedSubtitleFiles(IMainDatabase database)
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
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""SubtitleFiles"".""Id"" FROM ""SubtitleFiles""
                                     LEFT OUTER JOIN ""Series""
                                     ON ""SubtitleFiles"".""SeriesId"" = ""Series"".""Id""
                                     WHERE ""SubtitleFiles"".""SeriesId"" > 0
                                     AND ""Series"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByEpisodeFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""SubtitleFiles"".""Id"" FROM ""SubtitleFiles""
                                     LEFT OUTER JOIN ""EpisodeFiles""
                                     ON ""SubtitleFiles"".""EpisodeFileId"" = ""EpisodeFiles"".""Id""
                                     WHERE ""SubtitleFiles"".""EpisodeFileId"" > 0
                                     AND ""EpisodeFiles"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByMovie()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                             WHERE ""Id"" IN (
                             SELECT ""SubtitleFiles"".""Id"" FROM ""SubtitleFiles""
                             LEFT OUTER JOIN ""Movies""
                             ON ""SubtitleFiles"".""MovieId"" = ""Movies"".""Id""
                             WHERE ""SubtitleFiles"".""MovieId"" > 0
                             AND ""Movies"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByMovieFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                             WHERE ""Id"" IN (
                             SELECT ""SubtitleFiles"".""Id"" FROM ""SubtitleFiles""
                             LEFT OUTER JOIN ""MovieFiles""
                             ON ""SubtitleFiles"".""MovieFileId"" = ""MovieFiles"".""Id""
                             WHERE ""SubtitleFiles"".""MovieFileId"" > 0
                             AND ""MovieFiles"".""Id"" IS NULL)");
        }

        private void DeleteWhereFileIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                                     WHERE ""Id"" IN (
                                     SELECT ""Id"" FROM ""SubtitleFiles""
                                     WHERE COALESCE(""EpisodeFileId"", 0) = 0
                                     AND COALESCE(""MovieFileId"", 0) = 0)");
        }
    }
}

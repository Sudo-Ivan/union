using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1161)]
    public class speed_improvements : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Auto indices SQLite is creating
            if (!Schema.Table("MovieFiles").Index("IX_MovieFiles_MovieId").Exists())
            {
            Create.Index("IX_MovieFiles_MovieId").OnTable("MovieFiles").OnColumn("MovieId");
            }
            if (!Schema.Table("AlternativeTitles").Index("IX_AlternativeTitles_MovieId").Exists())
            {
            Create.Index("IX_AlternativeTitles_MovieId").OnTable("AlternativeTitles").OnColumn("MovieId");
            }

            // Speed up release processing (these are present in Sonarr)
            if (!Schema.Table("Movies").Index("IX_Movies_CleanTitle").Exists())
            {
            Create.Index("IX_Movies_CleanTitle").OnTable("Movies").OnColumn("CleanTitle");
            }
            if (!Schema.Table("Movies").Index("IX_Movies_ImdbId").Exists())
            {
            Create.Index("IX_Movies_ImdbId").OnTable("Movies").OnColumn("ImdbId");
            }
            if (!Schema.Table("Movies").Index("IX_Movies_TmdbId").Exists())
            {
            Create.Index("IX_Movies_TmdbId").OnTable("Movies").OnColumn("TmdbId");
            }
        }
    }
}

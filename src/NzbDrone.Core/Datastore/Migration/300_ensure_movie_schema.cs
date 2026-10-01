using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Runs only when upgrading an existing sonarr.db: migration 001 is already
    // applied so the movie-domain tables would never be created, and the
    // radarr-lineage migrations at 1104+ assume they exist. Movies is created in
    // the same squash shape union's own 001 produces so the 1104+ transforms
    // converge to the union schema. Every other movie-domain table is created
    // by the radarr lineage itself and shaped forward from there. On a fresh
    // install 001 already created Movies and this statement is a no-op.
    [Migration(300)]
    public class ensure_movie_schema : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            IfDatabase(ProcessorIdConstants.SQLite).Execute.Sql(@"
CREATE TABLE IF NOT EXISTS ""Movies"" (""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, ""ImdbId"" TEXT UNIQUE, ""Title"" TEXT NOT NULL, ""TitleSlug"" TEXT NOT NULL UNIQUE, ""SortTitle"" TEXT, ""CleanTitle"" TEXT NOT NULL, ""Status"" INTEGER NOT NULL, ""Overview"" TEXT, ""Images"" TEXT NOT NULL, ""Path"" TEXT NOT NULL, ""Monitored"" INTEGER NOT NULL, ""ProfileId"" INTEGER NOT NULL, ""LastInfoSync"" DATETIME, ""LastDiskSync"" DATETIME, ""Runtime"" INTEGER NOT NULL, ""InCinemas"" DATETIME, ""Year"" INTEGER, ""Added"" DATETIME, ""Ratings"" TEXT, ""Genres"" TEXT, ""Tags"" TEXT, ""Certification"" TEXT, ""AddOptions"" TEXT, ""MovieFileId"" INTEGER NOT NULL DEFAULT 0, ""TmdbId"" INTEGER NOT NULL UNIQUE, ""Website"" TEXT, ""AlternativeTitles"" TEXT, ""PhysicalRelease"" DATETIME, ""YouTubeTrailerId"" TEXT, ""Studio"" TEXT, ""MinimumAvailability"" INTEGER NOT NULL DEFAULT 3, ""SecondaryYear"" INTEGER, ""Collection"" TEXT, ""Recommendations"" TEXT NOT NULL DEFAULT '[]', ""OriginalLanguage"" INTEGER NOT NULL DEFAULT 1, ""OriginalTitle"" TEXT, ""DigitalRelease"" DATETIME);");
        }
    }
}

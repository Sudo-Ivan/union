using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1129)]
    public class add_parsed_movie_info_to_pending_release : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("PendingReleases").Column("ParsedMovieInfo").Exists())
            {
            Alter.Table("PendingReleases").AddColumn("ParsedMovieInfo").AsString().Nullable();
            }
        }
    }
}

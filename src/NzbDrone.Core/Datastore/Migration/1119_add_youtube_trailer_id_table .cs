using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1119)]
    public class add_youtube_trailer_id : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("YouTubeTrailerId").Exists())
            {
            Alter.Table("Movies").AddColumn("YouTubeTrailerId").AsString().Nullable();
            }
        }
    }
}

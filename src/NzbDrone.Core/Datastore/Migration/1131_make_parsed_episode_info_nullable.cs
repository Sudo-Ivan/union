using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1131)]
    public class make_parsed_episode_info_nullable : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("PendingReleases").Column("ParsedEpisodeInfo").Exists())
            {
            Alter.Table("PendingReleases").AlterColumn("ParsedEpisodeInfo").AsString().Nullable();
            }
        }
    }
}

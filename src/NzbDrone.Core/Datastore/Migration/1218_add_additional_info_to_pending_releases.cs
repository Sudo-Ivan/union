using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1218)]
    public class add_additional_info_to_pending_releases_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("PendingReleases").Column("AdditionalInfo").Exists())
            {
            Alter.Table("PendingReleases").AddColumn("AdditionalInfo").AsString().Nullable();
            }
        }
    }
}

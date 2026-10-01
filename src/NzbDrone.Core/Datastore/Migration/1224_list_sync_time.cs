using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1224)]
    public class list_sync_time_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("ImportListStatus").Column("LastSyncListInfo").Exists())
            {
            if (Schema.Table("ImportListStatus").Column("LastSyncListInfo").Exists())
            {
            Delete.Column("LastSyncListInfo").FromTable("ImportListStatus");
            }
            }

            if (!Schema.Table("ImportListStatus").Column("LastInfoSync").Exists())
            {
            if (!Schema.Table("ImportListStatus").Column("LastInfoSync").Exists())
            {
            Alter.Table("ImportListStatus").AddColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            }
            }

            if (Schema.Table("Config").Exists())
            {
            Delete.FromTable("Config").Row(new { Key = "importlistsyncinterval" });
            }
        }
    }
}

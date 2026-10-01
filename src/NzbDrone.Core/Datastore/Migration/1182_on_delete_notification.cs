using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1182)]
    public class on_delete_notification : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Notifications").Column("OnDelete").Exists())
            {
            Alter.Table("Notifications").AddColumn("OnDelete").AsBoolean().WithDefaultValue(false);
            }
        }
    }
}

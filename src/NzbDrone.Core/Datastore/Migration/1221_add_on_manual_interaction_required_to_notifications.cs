using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1221)]
    public class add_on_manual_interaction_required_to_notifications_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Notifications").Column("OnManualInteractionRequired").Exists())
            {
            if (!Schema.Table("Notifications").Column("OnManualInteractionRequired").Exists())
            {
            Alter.Table("Notifications").AddColumn("OnManualInteractionRequired").AsBoolean().WithDefaultValue(false);
            }
            }
        }
    }
}

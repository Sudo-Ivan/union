using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1157)]
    public class remove_growl_prowl : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Notifications").Exists())
            {
            Delete.FromTable("Notifications").Row(new { Implementation = "Growl" });
            }

            // Prowl Added back
            // Delete.FromTable("Notifications").Row(new { Implementation = "Prowl" });
        }
    }
}

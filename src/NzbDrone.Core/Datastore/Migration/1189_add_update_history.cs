using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1189)]
    public class add_update_history_r : NzbDroneMigrationBase
    {
        protected override void LogDbUpgrade()
        {
            if (!Schema.Table("UpdateHistory").Exists())
            {
            if (!Schema.Table("UpdateHistory").Exists())
            {
            Create.TableForModel("UpdateHistory")
                  .WithColumn("Date").AsDateTime().NotNullable().Indexed()
                  .WithColumn("Version").AsString().NotNullable()
                  .WithColumn("EventType").AsInt32().NotNullable();
            }
            }
        }
    }
}

using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1232)]
    public class add_notification_status_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("NotificationStatus").Exists())
            {
            if (!Schema.Table("NotificationStatus").Exists())
            {
            Create.TableForModel("NotificationStatus")
                  .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                  .WithColumn("InitialFailure").AsDateTimeOffset().Nullable()
                  .WithColumn("MostRecentFailure").AsDateTimeOffset().Nullable()
                  .WithColumn("EscalationLevel").AsInt32().NotNullable()
                  .WithColumn("DisabledTill").AsDateTimeOffset().Nullable();
            }
            }
        }
    }
}

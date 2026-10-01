using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1195)]
    public class update_notifiarr : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Notifications").Exists() && Schema.Table("Notifications").Column("Implementation").Exists())
            {
            Execute.Sql("UPDATE \"Notifications\" SET \"Implementation\" = Replace(\"Implementation\", 'DiscordNotifier', 'Notifiarr'),\"ConfigContract\" = Replace(\"ConfigContract\", 'DiscordNotifierSettings', 'NotifiarrSettings') WHERE \"Implementation\" = 'DiscordNotifier';");
            }
        }
    }
}

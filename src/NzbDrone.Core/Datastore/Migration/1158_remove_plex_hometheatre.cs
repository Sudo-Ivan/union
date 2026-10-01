using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1158)]
    public class remove_plex_hometheatre_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Notifications").Exists())
            {
            Delete.FromTable("Notifications").Row(new { Implementation = "PlexHomeTheater" });
            }

            if (Schema.Table("Notifications").Exists())
            {
            Delete.FromTable("Notifications").Row(new { Implementation = "PlexClient" });
            }
        }
    }
}

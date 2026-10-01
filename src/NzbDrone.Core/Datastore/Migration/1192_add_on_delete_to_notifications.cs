using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1192)]
    public class add_on_delete_to_notifications_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Notifications").Column("OnDelete").Exists() && !Schema.Table("Notifications").Column("OnMovieDelete").Exists())
            {
            Rename.Column("OnDelete").OnTable("Notifications").To("OnMovieDelete");
            }

            if (!Schema.Table("Notifications").Column("OnMovieFileDelete").Exists())
            {
            Alter.Table("Notifications").AddColumn("OnMovieFileDelete").AsBoolean().WithDefaultValue(false);
            }

            if (!Schema.Table("Notifications").Column("OnMovieFileDeleteForUpgrade").Exists())
            {
            Alter.Table("Notifications").AddColumn("OnMovieFileDeleteForUpgrade").AsBoolean().WithDefaultValue(false);
            }
        }
    }
}

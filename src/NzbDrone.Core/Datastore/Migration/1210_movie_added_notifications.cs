using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1210)]
    public class movie_added_notifications : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Notifications").Column("OnMovieAdded").Exists())
            {
            if (!Schema.Table("Notifications").Column("OnMovieAdded").Exists())
            {
            Alter.Table("Notifications").AddColumn("OnMovieAdded").AsBoolean().WithDefaultValue(false);
            }
            }
        }
    }
}

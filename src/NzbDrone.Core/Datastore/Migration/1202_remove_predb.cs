using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1202)]
    public class remove_predb : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Set PreDb entries to Released
            if (Schema.Table("Movies").Column("MinimumAvailability").Exists())
            {
            Update.Table("Movies").Set(new { MinimumAvailability = 3 }).Where(new { MinimumAvailability = 4 });
            }

            if (Schema.Table("ImportLists").Column("MinimumAvailability").Exists())
            {
            Update.Table("ImportLists").Set(new { MinimumAvailability = 3 }).Where(new { MinimumAvailability = 4 });
            }

            // Should never be set, but just in case
            if (Schema.Table("Movies").Column("Status").Exists())
            {
            Update.Table("Movies").Set(new { Status = 3 }).Where(new { Status = 4 });
            }

            if (Schema.Table("ImportListMovies").Column("Status").Exists())
            {
            Update.Table("ImportListMovies").Set(new { Status = 3 }).Where(new { Status = 4 });
            }

            // Remove unused column
            if (Schema.Table("Movies").Column("HasPreDBEntry").Exists())
            {
            if (Schema.Table("Movies").Column("HasPreDBEntry").Exists())
            {
            Delete.Column("HasPreDBEntry").FromTable("Movies");
            }
            }
        }
    }
}

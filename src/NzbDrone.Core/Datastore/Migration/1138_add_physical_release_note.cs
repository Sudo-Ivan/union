using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1138)]
    public class add_physical_release_note : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("PhysicalReleaseNote").Exists())
            {
            if (!Schema.Table("Movies").Column("PhysicalReleaseNote").Exists())
            {
            Alter.Table("Movies").AddColumn("PhysicalReleaseNote").AsString().Nullable();
            }
            }
        }
    }
}

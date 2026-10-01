using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1110)]
    public class add_phyiscal_release : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("PhysicalRelease").Exists())
            {
            Alter.Table("Movies").AddColumn("PhysicalRelease").AsDateTime().Nullable();
            }
        }
    }
}

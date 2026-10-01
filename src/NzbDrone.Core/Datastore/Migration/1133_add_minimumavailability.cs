using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1133)]
    public class add_minimumavailability : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("ImportLists").Column("MinimumAvailability").Exists())
            {
                if (!Schema.Table("ImportLists").Column("MinimumAvailability").Exists())
                {
                Alter.Table("ImportLists").AddColumn("MinimumAvailability").AsInt32().WithDefaultValue((int)MovieStatusType.Released);
                }
            }

            if (!Schema.Table("Movies").Column("MinimumAvailability").Exists())
            {
                if (!Schema.Table("Movies").Column("MinimumAvailability").Exists())
                {
                Alter.Table("Movies").AddColumn("MinimumAvailability").AsInt32().WithDefaultValue((int)MovieStatusType.Released);
                }
            }
        }
    }
}

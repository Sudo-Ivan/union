using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1184)]
    public class add_priority_to_indexers_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Indexers").Column("Priority").Exists())
            {
            Alter.Table("Indexers").AddColumn("Priority").AsInt32().NotNullable().WithDefaultValue(25);
            }
        }
    }
}

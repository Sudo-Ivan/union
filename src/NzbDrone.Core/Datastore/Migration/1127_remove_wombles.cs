using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1127)]
    public class remove_wombles_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Indexers").Exists())
            {
            Delete.FromTable("Indexers").Row(new { Implementation = "Wombles" });
            }
        }
    }
}

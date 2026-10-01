using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1217)]
    public class remove_omg_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Indexers").Exists())
            {
            Delete.FromTable("Indexers").Row(new { Implementation = "Omgwtfnzbs" });
            }
        }
    }
}

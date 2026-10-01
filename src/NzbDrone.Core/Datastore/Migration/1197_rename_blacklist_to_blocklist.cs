using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1197)]
    public class rename_blacklist_to_blocklist_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Blacklist").Exists() && !Schema.Table("Blocklist").Exists())
            {
            Rename.Table("Blacklist").To("Blocklist");
            }
        }
    }
}

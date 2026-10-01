using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1198)]
    public class add_indexer_tags_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Indexers").Column("Tags").Exists())
            {
            if (!Schema.Table("Indexers").Column("Tags").Exists())
            {
            Alter.Table("Indexers").AddColumn("Tags").AsString().Nullable();
            }
            }
        }
    }
}

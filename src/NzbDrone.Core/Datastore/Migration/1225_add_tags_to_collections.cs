using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1225)]
    public class add_tags_to_collections : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Collections").Column("Tags").Exists())
            {
            if (!Schema.Table("Collections").Column("Tags").Exists())
            {
            Alter.Table("Collections").AddColumn("Tags").AsString().Nullable();
            }
            }
        }
    }
}

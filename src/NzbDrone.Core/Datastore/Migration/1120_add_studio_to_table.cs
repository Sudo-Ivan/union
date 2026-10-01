using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1120)]
    public class add_studio : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("Studio").Exists())
            {
            if (!Schema.Table("Movies").Column("Studio").Exists())
            {
            Alter.Table("Movies").AddColumn("Studio").AsString().Nullable();
            }
            }
        }
    }
}

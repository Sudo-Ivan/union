using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1219)]
    public class add_result_to_commands_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Commands").Column("Result").Exists())
            {
            Alter.Table("Commands").AddColumn("Result").AsInt32().WithDefaultValue(1);
            }
        }
    }
}

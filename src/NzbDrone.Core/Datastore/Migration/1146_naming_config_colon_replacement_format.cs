using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1146)]
    public class naming_config_colon_action : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("NamingConfig").Column("ColonReplacementFormat").Exists())
            {
            if (!Schema.Table("NamingConfig").Column("ColonReplacementFormat").Exists())
            {
            Alter.Table("NamingConfig").AddColumn("ColonReplacementFormat").AsInt32().NotNullable().WithDefaultValue(0);
            }
            }
        }
    }
}

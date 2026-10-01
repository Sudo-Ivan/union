using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1148)]
    public class remove_extra_naming_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Remove all but 1 NamingConfig
            try
            {
            Execute.Sql("DELETE FROM \"NamingConfig\" WHERE \"Id\" NOT IN(SELECT \"Id\" FROM \"NamingConfig\" LIMIT 1)");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }
    }
}

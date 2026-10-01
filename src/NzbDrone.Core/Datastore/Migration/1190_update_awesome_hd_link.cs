using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1190)]
    public class update_awesome_hd_link : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.Sql("UPDATE \"Indexers\" SET \"Settings\" = Replace(\"Settings\", 'https://awesome-hd.net', 'https://awesome-hd.club') WHERE \"Implementation\" = 'AwesomeHD';");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
            try
            {
            Execute.Sql("UPDATE \"Indexers\" SET \"Settings\" = Replace(\"Settings\", 'https://awesome-hd.me', 'https://awesome-hd.club') WHERE \"Implementation\" = 'AwesomeHD';");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }
    }
}

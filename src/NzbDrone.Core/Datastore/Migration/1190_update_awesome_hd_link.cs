using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1190)]
    public class update_awesome_hd_link : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Indexers").Exists() && Schema.Table("Indexers").Column("Settings").Exists())
            {
            Execute.Sql("UPDATE \"Indexers\" SET \"Settings\" = Replace(\"Settings\", 'https://awesome-hd.net', 'https://awesome-hd.club') WHERE \"Implementation\" = 'AwesomeHD';");
            }

            if (Schema.Table("Indexers").Exists() && Schema.Table("Indexers").Column("Settings").Exists())
            {
            Execute.Sql("UPDATE \"Indexers\" SET \"Settings\" = Replace(\"Settings\", 'https://awesome-hd.me', 'https://awesome-hd.club') WHERE \"Implementation\" = 'AwesomeHD';");
            }
        }
    }
}

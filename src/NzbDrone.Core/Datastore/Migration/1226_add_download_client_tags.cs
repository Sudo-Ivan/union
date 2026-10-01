using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1226)]
    public class add_download_client_tags_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("DownloadClients").Column("Tags").Exists())
            {
            Alter.Table("DownloadClients").AddColumn("Tags").AsString().Nullable();
            }
        }
    }
}

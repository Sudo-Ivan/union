using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1132)]
    public class rename_torrent_downloadstation_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.Sql("UPDATE \"DownloadClients\" SET \"Implementation\" = 'TorrentDownloadStation' WHERE \"Implementation\" = 'DownloadStation';");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }
    }
}

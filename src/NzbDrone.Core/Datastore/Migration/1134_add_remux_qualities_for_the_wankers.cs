using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1134)]
    public class add_remux_qualities_for_the_wankers : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.WithConnection(ConvertProfile);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void ConvertProfile(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var updater = new ProfileUpdater125(conn, tran);
                updater.SplitQualityAppend(19, 31); // Remux2160p    AFTER     Bluray2160p
                updater.SplitQualityAppend(7, 30);  // Remux1080p    AFTER     Bluray1080p

                updater.Commit();
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

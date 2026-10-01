using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1125)]
    public class fix_imdb_unique : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.WithConnection(DeleteUniqueIndex);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void DeleteUniqueIndex(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                using (var getSeriesCmd = conn.CreateCommand())
                {
                    getSeriesCmd.Transaction = tran;
                    getSeriesCmd.CommandText = @"DROP INDEX ""IX_Movies_ImdbId""";
    
                    getSeriesCmd.ExecuteNonQuery();
                }
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

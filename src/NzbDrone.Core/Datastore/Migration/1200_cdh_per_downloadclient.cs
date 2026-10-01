using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1200)]
    public class cdh_per_downloadclient_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("DownloadClients").Column("RemoveCompletedDownloads").Exists())
            {
            Alter.Table("DownloadClients")
                 .AddColumn("RemoveCompletedDownloads").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("RemoveFailedDownloads").AsBoolean().NotNullable().WithDefaultValue(true);
            }

            try
            {
            Execute.WithConnection(MoveRemoveSettings);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void MoveRemoveSettings(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var removeCompletedDownloads = false;
                var removeFailedDownloads = true;
    
                using (var removeCompletedDownloadsCmd = conn.CreateCommand(tran, "SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'removecompleteddownloads'"))
                {
                    if ((removeCompletedDownloadsCmd.ExecuteScalar() as string)?.ToLower() == "true")
                    {
                        removeCompletedDownloads = true;
                    }
                }
    
                using (var removeFailedDownloadsCmd = conn.CreateCommand(tran, "SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'removefaileddownloads'"))
                {
                    if ((removeFailedDownloadsCmd.ExecuteScalar() as string)?.ToLower() == "false")
                    {
                        removeFailedDownloads = false;
                    }
                }
    
                string commandText;
    
                if (conn.GetType().FullName == "Npgsql.NpgsqlConnection")
                {
                    commandText = $"UPDATE \"DownloadClients\" SET \"RemoveCompletedDownloads\" = (CASE WHEN \"Implementation\" IN ('RTorrent', 'Flood') THEN 'false' ELSE $1 END), \"RemoveFailedDownloads\" = $2";
                }
                else
                {
                    commandText = $"UPDATE \"DownloadClients\" SET \"RemoveCompletedDownloads\" = (CASE WHEN \"Implementation\" IN ('RTorrent', 'Flood') THEN 'false' ELSE ? END), \"RemoveFailedDownloads\" = ?";
                }
    
                using (var updateClientCmd = conn.CreateCommand(tran, commandText))
                {
                    updateClientCmd.AddParameter(removeCompletedDownloads);
                    updateClientCmd.AddParameter(removeFailedDownloads);
                    updateClientCmd.ExecuteNonQuery();
                }
    
                using (var removeConfigCmd = conn.CreateCommand(tran, $"DELETE FROM \"Config\" WHERE \"Key\" IN ('removecompleteddownloads', 'removefaileddownloads')"))
                {
                    removeConfigCmd.ExecuteNonQuery();
                }
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

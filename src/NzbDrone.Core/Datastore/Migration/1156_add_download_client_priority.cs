using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1156)]
    public class add_download_client_priority_r : NzbDroneMigrationBase
    {
        // Need snapshot in time without having to instantiate.
        private static HashSet<string> _usenetImplementations = new HashSet<string>
        {
            "Sabnzbd", "NzbGet", "NzbVortex", "UsenetBlackhole", "UsenetDownloadStation"
        };

        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("DownloadClients").Column("Priority").Exists())
            {
            if (!Schema.Table("DownloadClients").Column("Priority").Exists())
            {
            Alter.Table("DownloadClients").AddColumn("Priority").AsInt32().WithDefaultValue(1);
            }
            }

            try
            {
            WithConnectionGuarded(InitPriorityForBackwardCompatibility);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void InitPriorityForBackwardCompatibility(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var downloadClients = conn.Query<DownloadClients156>($"SELECT \"Id\", \"Implementation\" FROM \"DownloadClients\" WHERE \"Enable\"");

                if (!downloadClients.Any())
                {
                    return;
                }

                var nextUsenet = 1;
                var nextTorrent = 1;

                foreach (var downloadClient in downloadClients)
                {
                    var isUsenet = _usenetImplementations.Contains(downloadClient.Implementation);
                    using (var updateCmd = conn.CreateCommand())
                    {
                        updateCmd.Transaction = tran;
                        if (conn.GetType().FullName == "Npgsql.NpgsqlConnection")
                        {
                            updateCmd.CommandText = "UPDATE \"DownloadClients\" SET \"Priority\" = $1 WHERE \"Id\" = $2";
                        }
                        else
                        {
                            updateCmd.CommandText = "UPDATE \"DownloadClients\" SET \"Priority\" = ? WHERE \"Id\" = ?";
                        }

                        updateCmd.AddParameter(isUsenet ? nextUsenet++ : nextTorrent++);
                        updateCmd.AddParameter(downloadClient.Id);

                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }

    public class DownloadClients156
    {
        public int Id { get; set; }
        public string Implementation { get; set; }
    }

    public class DelugeSettings156
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UrlBase { get; set; }
        public string Password { get; set; }
        public string TvCategory { get; set; }
        public int RecentTvPriority { get; set; }
        public int OlderTvPriority { get; set; }
        public bool UseSsl { get; set; }
    }

    public class SabnzbdSettings156
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string TvCategory { get; set; }
        public int RecentTvPriority { get; set; }
        public int OlderTvPriority { get; set; }
        public bool UseSsl { get; set; }
    }
}

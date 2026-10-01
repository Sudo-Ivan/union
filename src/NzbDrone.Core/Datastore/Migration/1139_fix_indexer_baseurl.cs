using System.Data;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1139)]
    public class consolidate_indexer_baseurl_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.WithConnection(RenameUrlToBaseUrl);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void RenameUrlToBaseUrl(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"ConfigContract\" IN ('NewznabSettings', 'TorznabSettings', 'IPTorrentsSettings', 'OmgwtfnzbsSettings')";
    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetInt32(0);
                            var settings = reader.GetString(1);
    
                            if (settings.IsNotNullOrWhiteSpace())
                            {
                                var jsonObject = Json.Deserialize<JObject>(settings);
    
                                if (jsonObject.Property("url") != null)
                                {
                                    jsonObject.AddFirst(new JProperty("baseUrl", jsonObject["url"]));
                                    jsonObject.Remove("url");
                                    settings = jsonObject.ToJson();
    
                                    using (var updateCmd = conn.CreateCommand())
                                    {
                                        updateCmd.Transaction = tran;
                                        updateCmd.CommandText = "UPDATE \"Indexers\" SET \"Settings\" = ? WHERE \"Id\" = ?";
                                        updateCmd.AddParameter(settings);
                                        updateCmd.AddParameter(id);
                                        updateCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

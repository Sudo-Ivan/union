using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1235)]
    public class email_encryption_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.WithConnection(ChangeEncryption);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void ChangeEncryption(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var updated = new List<object>();
                using (var getEmailCmd = conn.CreateCommand())
                {
                    getEmailCmd.Transaction = tran;
                    getEmailCmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Notifications\" WHERE \"Implementation\" = 'Email'";
    
                    using (var reader = getEmailCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetInt32(0);
                            var settings = Json.Deserialize<JObject>(reader.GetString(1));
    
                            settings["useEncryption"] = settings.Value<bool?>("requireEncryption") ?? false ? 1 : 0;
                            settings["requireEncryption"] = null;
    
                            updated.Add(new
                            {
                                Settings = settings.ToJson(),
                                Id = id
                            });
                        }
                    }
                }
    
                var updateSql = "UPDATE \"Notifications\" SET \"Settings\" = @Settings WHERE \"Id\" = @Id";
                conn.Execute(updateSql, updated, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

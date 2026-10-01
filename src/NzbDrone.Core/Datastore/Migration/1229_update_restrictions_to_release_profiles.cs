using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1229)]
    public class update_restrictions_to_release_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Restrictions").Exists() && !Schema.Table("ReleaseProfiles").Exists())
            {
            if (Schema.Table("Restrictions").Exists() && !Schema.Table("ReleaseProfiles").Exists())
            {
            Rename.Table("Restrictions").To("ReleaseProfiles");
            }
            }

            if (!Schema.Table("ReleaseProfiles").Column("Name").Exists())
            {
            if (!Schema.Table("ReleaseProfiles").Column("Name").Exists())
            {
            Alter.Table("ReleaseProfiles").AddColumn("Name").AsString().Nullable().WithDefaultValue(null);
            }
            }

            if (!Schema.Table("ReleaseProfiles").Column("Enabled").Exists())
            {
            if (!Schema.Table("ReleaseProfiles").Column("Enabled").Exists())
            {
            Alter.Table("ReleaseProfiles").AddColumn("Enabled").AsBoolean().WithDefaultValue(true);
            }
            }

            if (!Schema.Table("ReleaseProfiles").Column("IndexerId").Exists())
            {
            if (!Schema.Table("ReleaseProfiles").Column("IndexerId").Exists())
            {
            Alter.Table("ReleaseProfiles").AddColumn("IndexerId").AsInt32().WithDefaultValue(0);
            }
            }

            if (Schema.Table("ReleaseProfiles").Column("Preferred").Exists())
            {
            if (Schema.Table("ReleaseProfiles").Column("Preferred").Exists())
            {
            Delete.Column("Preferred").FromTable("ReleaseProfiles");
            }
            }

            try
            {
            WithConnectionGuarded(ChangeRequiredIgnoredTypes);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            if (Schema.Table("ReleaseProfiles").Exists())
            {
            Delete.FromTable("ReleaseProfiles").Row(new { Required = "[]", Ignored = "[]" });
            }
        }

        // Update the Required and Ignored columns to be JSON arrays instead of comma separated strings
        private void ChangeRequiredIgnoredTypes(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var updatedReleaseProfiles = new List<object>();

                using (var getEmailCmd = conn.CreateCommand())
                {
                    getEmailCmd.Transaction = tran;
                    getEmailCmd.CommandText = "SELECT \"Id\", \"Required\", \"Ignored\" FROM \"ReleaseProfiles\"";

                    using var reader = getEmailCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var requiredObj = reader.GetValue(1);
                        var ignoredObj = reader.GetValue(2);

                        var required = requiredObj == DBNull.Value
                            ? Enumerable.Empty<string>()
                            : requiredObj.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        var ignored = ignoredObj == DBNull.Value
                            ? Enumerable.Empty<string>()
                            : ignoredObj.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        updatedReleaseProfiles.Add(new
                        {
                            Id = id,
                            Required = required.ToJson(),
                            Ignored = ignored.ToJson()
                        });
                    }
                }

                var updateReleaseProfilesSql = "UPDATE \"ReleaseProfiles\" SET \"Required\" = @Required, \"Ignored\" = @Ignored WHERE \"Id\" = @Id";
                conn.Execute(updateReleaseProfilesSql, updatedReleaseProfiles, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

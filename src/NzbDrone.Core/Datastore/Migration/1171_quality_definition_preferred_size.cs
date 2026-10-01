using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1171)]
    public class quality_definition_preferred_size_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("QualityDefinitions").Column("PreferredSize").Exists())
            {
            Alter.Table("QualityDefinitions").AddColumn("PreferredSize").AsDouble().Nullable();
            }

            try
            {
            Execute.WithConnection(UpdateQualityDefinitions);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void UpdateQualityDefinitions(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var existing = conn.Query<QualityDefinition170>("SELECT \"Id\", \"MaxSize\" FROM \"QualityDefinitions\"");
    
                var updated = new List<QualityDefinition171>();
    
                foreach (var row in existing)
                {
                    var maxSize = row.MaxSize;
                    var preferredSize = maxSize;
    
                    if (maxSize.HasValue && maxSize.Value > 5)
                    {
                        preferredSize = maxSize.Value - 5;
                    }
    
                    updated.Add(new QualityDefinition171
                    {
                        Id = row.Id,
                        PreferredSize = preferredSize
                    });
                }
    
                var updateSql = "UPDATE \"QualityDefinitions\" SET \"PreferredSize\" = @PreferredSize WHERE \"Id\" = @Id";
                conn.Execute(updateSql, updated, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }

        private class QualityDefinition170 : ModelBase
        {
            public int? MaxSize { get; set; }
        }

        private class QualityDefinition171 : ModelBase
        {
            public int? PreferredSize { get; set; }
        }
    }
}

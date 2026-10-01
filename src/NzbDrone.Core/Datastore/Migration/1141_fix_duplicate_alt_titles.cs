using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1141)]
    public class fix_duplicate_alt_titles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.WithConnection(RemoveDuplicateAlternateTitles);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            if (Schema.Table("AlternativeTitles").Column("CleanTitle").Exists())
            {
            Alter.Table("AlternativeTitles").AlterColumn("CleanTitle").AsString().Unique();
            }
        }

        private void RemoveDuplicateAlternateTitles(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = "DELETE FROM \"AlternativeTitles\" WHERE \"Id\" NOT IN (Select Min(\"Id\") From \"AlternativeTitles\" Group By \"CleanTitle\")";

                    cmd.ExecuteNonQuery();
                }
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1167)]
    public class remove_movie_pathstate : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Movies").Column("PathState").Exists())
            {
            Delete.Column("PathState").FromTable("Movies");
            }

            try
            {
            Execute.Sql("DELETE FROM \"Config\" WHERE \"Key\" IN ('pathsdefaultstatic')");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            if (!Schema.Table("MovieFiles").Column("OriginalFilePath").Exists())
            {
            Alter.Table("MovieFiles").AddColumn("OriginalFilePath").AsString().Nullable();
            }

            // This is Ignored in mapping, should not be in DB
            if (Schema.Table("MovieFiles").Column("Path").Exists())
            {
            Delete.Column("Path").FromTable("MovieFiles");
            }
        }
    }
}

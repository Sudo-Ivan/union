using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1109)]
    public class add_movie_formats_to_naming_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("NamingConfig").Column("StandardMovieFormat").Exists())
            {
            Alter.Table("NamingConfig").AddColumn("StandardMovieFormat").AsString().Nullable();
            }

            if (!Schema.Table("NamingConfig").Column("MovieFolderFormat").Exists())
            {
            Alter.Table("NamingConfig").AddColumn("MovieFolderFormat").AsString().Nullable();
            }

            try
            {
            Execute.WithConnection(ConvertConfig);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void ConvertConfig(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                using (var namingConfigCmd = conn.CreateCommand())
                {
                    namingConfigCmd.Transaction = tran;
                    namingConfigCmd.CommandText = @"SELECT * FROM ""NamingConfig"" LIMIT 1";
                    using (var namingConfigReader = namingConfigCmd.ExecuteReader())
                    {
                        while (namingConfigReader.Read())
                        {
                            // Output Settings
                            var movieTitlePattern = "";
                            var movieYearPattern = "({Release Year})";
                            var qualityFormat = "[{Quality Title}]";

                            movieTitlePattern = "{Movie Title}";

                            var standardMovieFormat = string.Format("{0} {1} {2}", movieTitlePattern, movieYearPattern, qualityFormat);

                            var movieFolderFormat = string.Format("{0} {1}", movieTitlePattern, movieYearPattern);

                            using (var updateCmd = conn.CreateCommand())
                            {
                                var text = string.Format("UPDATE \"NamingConfig\" " +
                                                         "SET \"StandardMovieFormat\" = '{0}', " +
                                                         "\"MovieFolderFormat\" = '{1}'",
                                                         standardMovieFormat,
                                                         movieFolderFormat);

                                updateCmd.Transaction = tran;
                                updateCmd.CommandText = text;
                                updateCmd.ExecuteNonQuery();
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

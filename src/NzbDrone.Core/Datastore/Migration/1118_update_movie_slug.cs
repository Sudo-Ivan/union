using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1118)]
    public class update_movie_slug : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.WithConnection(SetTitleSlug);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void SetTitleSlug(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                using (var getSeriesCmd = conn.CreateCommand())
                {
                    getSeriesCmd.Transaction = tran;
                    getSeriesCmd.CommandText = @"SELECT ""Id"", ""Title"", ""Year"", ""TmdbId"" FROM ""Movies""";
                    using (var seriesReader = getSeriesCmd.ExecuteReader())
                    {
                        while (seriesReader.Read())
                        {
                            var id = seriesReader.GetInt32(0);
                            var title = seriesReader.GetString(1);
                            var year = seriesReader.GetInt32(2);
                            var tmdbId = seriesReader.GetInt32(3);
    
                            var titleSlug = Parser.Parser.ToUrlSlug(title + "-" + tmdbId);
    
                            using (var updateCmd = conn.CreateCommand())
                            {
                                updateCmd.Transaction = tran;
                                updateCmd.CommandText = "UPDATE \"Movies\" SET \"TitleSlug\" = ? WHERE \"Id\" = ?";
                                updateCmd.AddParameter(titleSlug);
                                updateCmd.AddParameter(id);
    
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

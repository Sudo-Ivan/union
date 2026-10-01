using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1196)]
    public class legacy_mediainfo_hdr : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.Sql("UPDATE \"NamingConfig\" SET \"StandardMovieFormat\" = Replace(\"StandardMovieFormat\", '{MediaInfo HDR}', '{MediaInfo VideoDynamicRange}');");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }
    }
}

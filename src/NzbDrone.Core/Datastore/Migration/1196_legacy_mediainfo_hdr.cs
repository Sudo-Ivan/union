using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1196)]
    public class legacy_mediainfo_hdr : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("NamingConfig").Exists() && Schema.Table("NamingConfig").Column("StandardMovieFormat").Exists())
            {
            Execute.Sql("UPDATE \"NamingConfig\" SET \"StandardMovieFormat\" = Replace(\"StandardMovieFormat\", '{MediaInfo HDR}', '{MediaInfo VideoDynamicRange}');");
            }
        }
    }
}

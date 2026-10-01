using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1231)]
    public class update_images_remote_url_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("MovieMetadata").Exists() && Schema.Table("MovieMetadata").Column("Images").Exists())
            {
            Execute.Sql("UPDATE \"MovieMetadata\" SET \"Images\" = REPLACE(\"Images\", '\"url\"', '\"remoteUrl\"')");
            }

            if (Schema.Table("Credits").Exists() && Schema.Table("Credits").Column("Images").Exists())
            {
            Execute.Sql("UPDATE \"Credits\" SET \"Images\" = REPLACE(\"Images\", '\"url\"', '\"remoteUrl\"')");
            }

            if (Schema.Table("Collections").Exists() && Schema.Table("Collections").Column("Images").Exists())
            {
            Execute.Sql("UPDATE \"Collections\" SET \"Images\" = REPLACE(\"Images\", '\"url\"', '\"remoteUrl\"')");
            }
        }
    }
}

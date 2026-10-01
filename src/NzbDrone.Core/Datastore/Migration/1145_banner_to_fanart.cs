using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1145)]
    public class banner_to_fanart : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Movies").Exists() && Schema.Table("Movies").Column("Images").Exists())
            {
            Execute.Sql("UPDATE \"Movies\" SET \"Images\" = replace(\"Images\", \'\"coverType\": \"banner\"\', \'\"coverType\": \"fanart\"\')");
            }

            // Remove Link for images to specific MovieFiles, Images are now related to the Movie object only
            if (Schema.Table("MetadataFiles").Exists())
            {
                if (Schema.Table("MetadataFiles").Exists() && Schema.Table("MetadataFiles").Column("MovieFileId").Exists())
                {
                Execute.Sql("UPDATE \"MetadataFiles\" SET \"MovieFileId\" = null WHERE \"Type\" = 2");
                }
            }
        }
    }
}

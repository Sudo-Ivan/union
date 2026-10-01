using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1145)]
    public class banner_to_fanart : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.Sql("UPDATE \"Movies\" SET \"Images\" = replace(\"Images\", \'\"coverType\": \"banner\"\', \'\"coverType\": \"fanart\"\')");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            // Remove Link for images to specific MovieFiles, Images are now related to the Movie object only
            if (Schema.Table("MetadataFiles").Exists())
            {
                try
                {
                Execute.Sql("UPDATE \"MetadataFiles\" SET \"MovieFileId\" = null WHERE \"Type\" = 2");
                }
                catch (System.Exception e)
                {
                    _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
                }
            }
        }
    }
}

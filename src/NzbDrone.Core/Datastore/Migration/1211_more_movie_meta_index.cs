using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1211)]
    public class more_movie_meta_index : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("AlternativeTitles").Index("IX_AlternativeTitles_MovieMetadataId").Exists())
            {
            Create.Index("IX_AlternativeTitles_MovieMetadataId").OnTable("AlternativeTitles").OnColumn("MovieMetadataId");
            }
            if (!Schema.Table("Credits").Index("IX_Credits_MovieMetadataId").Exists())
            {
            Create.Index("IX_Credits_MovieMetadataId").OnTable("Credits").OnColumn("MovieMetadataId");
            }
        }
    }
}

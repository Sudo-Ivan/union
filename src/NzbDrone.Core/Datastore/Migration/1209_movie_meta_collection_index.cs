using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1209)]
    public class movie_meta_collection_index : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("MovieMetadata").Index("IX_MovieMetadata_CollectionTmdbId").Exists())
            {
            Create.Index("IX_MovieMetadata_CollectionTmdbId").OnTable("MovieMetadata").OnColumn("CollectionTmdbId");
            }
            if (!Schema.Table("MovieTranslations").Index("IX_MovieTranslations_MovieMetadataId").Exists())
            {
            Create.Index("IX_MovieTranslations_MovieMetadataId").OnTable("MovieTranslations").OnColumn("MovieMetadataId");
            }
        }
    }
}

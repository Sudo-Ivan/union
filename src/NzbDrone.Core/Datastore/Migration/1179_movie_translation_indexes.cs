using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1179)]
    public class movie_translation_indexes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("MovieTranslations").Index("IX_MovieTranslations_Language").Exists())
            {
            if (Schema.Table("MovieTranslations").Column("Language").Exists())
            {
            Create.Index("IX_MovieTranslations_Language").OnTable("MovieTranslations").OnColumn("Language");
            }
            }

            if (!Schema.Table("MovieTranslations").Index("IX_MovieTranslations_MovieId").Exists())
            {
            if (Schema.Table("MovieTranslations").Column("MovieId").Exists())
            {
            Create.Index("IX_MovieTranslations_MovieId").OnTable("MovieTranslations").OnColumn("MovieId");
            }
            }
        }
    }
}

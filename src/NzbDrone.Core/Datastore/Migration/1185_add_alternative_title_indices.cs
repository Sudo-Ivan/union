using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1185)]
    public class add_alternative_title_indices : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("AlternativeTitles").Index("IX_AlternativeTitles_CleanTitle").Exists())
            {
            if (Schema.Table("AlternativeTitles").Column("CleanTitle").Exists())
            {
            Create.Index().OnTable("AlternativeTitles").OnColumn("CleanTitle");
            }
            }

            if (!Schema.Table("MovieTranslations").Index("IX_MovieTranslations_CleanTitle").Exists())
            {
            if (Schema.Table("MovieTranslations").Column("CleanTitle").Exists())
            {
            Create.Index().OnTable("MovieTranslations").OnColumn("CleanTitle");
            }
            }
        }
    }
}

using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1214)]
    public class add_language_tags_to_subtitle_files_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("SubtitleFiles").Column("LanguageTags").Exists())
            {
            Alter.Table("SubtitleFiles").AddColumn("LanguageTags").AsString().Nullable();
            }
        }
    }
}

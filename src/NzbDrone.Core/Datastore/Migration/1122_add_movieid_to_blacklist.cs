using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1122)]
    public class add_movieid_to_blacklist : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Blocklist").Column("MovieId").Exists())
            {
            Alter.Table("Blocklist").AddColumn("MovieId").AsInt32().Nullable().WithDefaultValue(0);
            }
            if (Schema.Table("Blocklist").Column("SeriesId").Exists() && !Schema.Table("Blocklist").Index("IX_Blocklist_QualityId").Exists())
            {
            Alter.Table("Blocklist").AlterColumn("SeriesId").AsInt32().Nullable();
            }
            if (Schema.Table("Blocklist").Column("EpisodeIds").Exists() && !Schema.Table("Blocklist").Index("IX_Blocklist_QualityId").Exists())
            {
            Alter.Table("Blocklist").AlterColumn("EpisodeIds").AsString().Nullable();
            }
        }
    }
}

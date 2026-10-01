using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Union: the merged QualityProfile entity keeps the movie-domain Language column.
    [Migration(1243)]
    public class union_fixups : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("QualityProfiles").Column("Language").Exists())
            {
                Alter.Table("QualityProfiles").AddColumn("Language").AsInt32().WithDefaultValue(1);
            }

            // Dead columns from the squashed base.
            if (Schema.Table("Episodes").Column("TvDbEpisodeId").Exists())
            {
                Delete.Column("TvDbEpisodeId").FromTable("Episodes");
            }

            // EpisodeFiles.Path is not mapped by the union entity (RelativePath is the
            // series-side path column), drop it so unmapped inserts do not violate the
            // NOT NULL constraint. The unique index must go first.
            if (Schema.Table("EpisodeFiles").Column("Path").Exists())
            {
                Execute.Sql("DROP INDEX IF EXISTS \"IX_EpisodeFiles_Path\"");
                Delete.Column("Path").FromTable("EpisodeFiles");
            }

            // Series.BacklogSetting is a leftover from the squashed base and is not
            // mapped by the union Series entity.
            if (Schema.Table("Series").Column("BacklogSetting").Exists())
            {
                Delete.Column("BacklogSetting").FromTable("Series");
            }

            // Indexers.Enable is a leftover from the squashed base. The union entity
            // computes Enable from EnableRss/EnableAutomaticSearch/EnableInteractiveSearch
            // and does not persist it.
            if (Schema.Table("Indexers").Column("Enable").Exists())
            {
                Delete.Column("Enable").FromTable("Indexers");
            }

            // The union ImportListDefinition entity carries both the series-domain
            // columns and the movie-domain columns. Sonarr's lineage already created
            // the table so radarr's create step was skipped, leaving the movie-domain
            // columns missing. ShouldMonitor may also have been dropped by 1208 on
            // databases that already migrated.
            if (!Schema.Table("ImportLists").Column("Enabled").Exists())
            {
                Alter.Table("ImportLists").AddColumn("Enabled").AsBoolean().WithDefaultValue(true);
            }

            if (!Schema.Table("ImportLists").Column("EnableAuto").Exists())
            {
                Alter.Table("ImportLists").AddColumn("EnableAuto").AsBoolean().WithDefaultValue(false);
            }

            if (!Schema.Table("ImportLists").Column("ShouldMonitor").Exists())
            {
                Alter.Table("ImportLists").AddColumn("ShouldMonitor").AsInt32().WithDefaultValue(0);
            }
        }
    }
}

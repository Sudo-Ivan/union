using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1142)]
    public class movie_extras : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Union: keep the shared ExtraFiles/SubtitleFiles/MetadataFiles tables and add the movie columns.

            if (!Schema.Table("ExtraFiles").Column("MovieId").Exists())
            {
                Alter.Table("ExtraFiles").AddColumn("MovieId").AsInt32().Nullable();
            }

            if (!Schema.Table("ExtraFiles").Column("MovieFileId").Exists())
            {
                Alter.Table("ExtraFiles").AddColumn("MovieFileId").AsInt32().Nullable();
            }

            if (!Schema.Table("SubtitleFiles").Column("MovieId").Exists())
            {
                Alter.Table("SubtitleFiles").AddColumn("MovieId").AsInt32().Nullable();
            }

            if (!Schema.Table("SubtitleFiles").Column("MovieFileId").Exists())
            {
                Alter.Table("SubtitleFiles").AddColumn("MovieFileId").AsInt32().Nullable();
            }

            if (!Schema.Table("MetadataFiles").Column("MovieId").Exists())
            {
                Alter.Table("MetadataFiles").AddColumn("MovieId").AsInt32().Nullable();
            }

            if (!Schema.Table("MetadataFiles").Column("MovieFileId").Exists())
            {
                Alter.Table("MetadataFiles").AddColumn("MovieFileId").AsInt32().Nullable();
            }

            // Union: movie rows have no series-side ids, so relax the constraints
            Alter.Table("ExtraFiles").AlterColumn("SeriesId").AsInt32().Nullable();
            Alter.Table("ExtraFiles").AlterColumn("EpisodeFileId").AsInt32().Nullable();
            Alter.Table("SubtitleFiles").AlterColumn("SeriesId").AsInt32().Nullable();
            Alter.Table("SubtitleFiles").AlterColumn("EpisodeFileId").AsInt32().Nullable();
            Alter.Table("MetadataFiles").AlterColumn("SeriesId").AsInt32().Nullable();
            Alter.Table("MetadataFiles").AlterColumn("EpisodeFileId").AsInt32().Nullable();
        }
    }
}

using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1104)]
    public class add_moviefiles_table : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("MovieFiles").Exists())
            {
            if (!Schema.Table("MovieFiles").Exists())
            {
            if (!Schema.Table("MovieFiles").Exists())
            {
            Create.TableForModel("MovieFiles")
                  .WithColumn("MovieId").AsInt32()
                  .WithColumn("Path").AsString().Unique()
                  .WithColumn("Quality").AsString()
                  .WithColumn("Size").AsInt64()
                  .WithColumn("DateAdded").AsDateTime()
                  .WithColumn("SceneName").AsString().Nullable()
                  .WithColumn("MediaInfo").AsString().Nullable()
                  .WithColumn("ReleaseGroup").AsString().Nullable()
                  .WithColumn("RelativePath").AsString().Nullable();
            }
            }
            }

            if (!Schema.Table("Movies").Column("MovieFileId").Exists())
            {
            if (!Schema.Table("Movies").Column("MovieFileId").Exists())
            {
            if (!Schema.Table("Movies").Column("MovieFileId").Exists())
            {
            Alter.Table("Movies").AddColumn("MovieFileId").AsInt32().WithDefaultValue(0);
            }
            }
            }
        }
    }
}

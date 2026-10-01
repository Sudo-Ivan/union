using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1242)]
    public class add_movie_keywords : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("MovieMetadata").Column("Keywords").Exists())
            {
            if (!Schema.Table("MovieMetadata").Column("Keywords").Exists())
            {
            Alter.Table("MovieMetadata").AddColumn("Keywords").AsString().Nullable();
            }
            }
        }
    }
}

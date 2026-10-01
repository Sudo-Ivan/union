using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1105)]
    public class fix_history_movieId : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("History").Column("MovieId").Exists())
            {
            if (!Schema.Table("History").Column("MovieId").Exists())
            {
            Alter.Table("History")
                  .AddColumn("MovieId").AsInt32().WithDefaultValue(0);
            }
            }
        }
    }
}

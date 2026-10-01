using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1234)]
    public class movie_last_searched_time : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("LastSearchTime").Exists())
            {
            Alter.Table("Movies").AddColumn("LastSearchTime").AsDateTimeOffset().Nullable();
            }
        }
    }
}

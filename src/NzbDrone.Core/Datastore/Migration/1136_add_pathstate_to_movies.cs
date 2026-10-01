using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1136)]
    public class add_pathstate_to_movies : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("PathState").Exists())
            {
            Alter.Table("Movies").AddColumn("PathState").AsInt32().WithDefaultValue(2);
            }
        }
    }
}

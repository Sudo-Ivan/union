using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1176)]
    public class movie_recommendations : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("Recommendations").Exists())
            {
            Alter.Table("Movies").AddColumn("Recommendations").AsString().WithDefaultValue("[]");
            }
        }
    }
}

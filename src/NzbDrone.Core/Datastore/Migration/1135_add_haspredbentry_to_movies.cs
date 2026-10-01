using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1135)]
    public class add_haspredbentry_to_movies : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("HasPreDBEntry").Exists())
            {
            if (!Schema.Table("Movies").Column("HasPreDBEntry").Exists())
            {
            Alter.Table("Movies").AddColumn("HasPreDBEntry").AsBoolean().WithDefaultValue(false);
            }
            }
        }
    }
}

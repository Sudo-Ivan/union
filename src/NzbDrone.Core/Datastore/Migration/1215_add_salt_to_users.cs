using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1215)]
    public class add_salt_to_users_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Users").Column("Salt").Exists())
            {
            Alter.Table("Users")
                .AddColumn("Salt").AsString().Nullable()
                .AddColumn("Iterations").AsInt32().Nullable();
            }
        }
    }
}

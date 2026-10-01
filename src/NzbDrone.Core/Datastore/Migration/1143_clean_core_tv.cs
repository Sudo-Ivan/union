using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1143)]
    public class clean_core_tv : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Union: series schema is retained alongside movies, so the original tv cleanup is intentionally a no-op.
        }
    }
}

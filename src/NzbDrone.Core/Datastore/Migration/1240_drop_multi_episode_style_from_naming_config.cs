using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1240)]
    public class drop_multi_episode_style_from_naming_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Union: MultiEpisodeStyle is still used by the series domain.
        }
    }
}

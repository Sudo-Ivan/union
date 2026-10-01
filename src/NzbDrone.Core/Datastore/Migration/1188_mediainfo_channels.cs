using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1188)]
    public class mediainfo_channels_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            try
            {
            Execute.Sql("UPDATE \"MovieFiles\" SET \"MediaInfo\" = Replace(\"MediaInfo\", '\"audioChannels\"', '\"audioChannelsContainer\"');");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
            try
            {
            Execute.Sql("UPDATE \"MovieFiles\" SET \"MediaInfo\" = Replace(\"MediaInfo\", '\"audioChannelPositionsText\"', '\"audioChannelPositionsTextContainer\"');");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            // Change List Interval from Min to Hour
            IfDatabase("sqlite").Execute.Sql("UPDATE \"Config\" SET \"Value\" = max((\"Value\" / 60) + 1, 6) WHERE \"Key\" = 'importlistsyncinterval'");
            IfDatabase("postgres").Execute.Sql("UPDATE \"Config\" SET \"Value\" = greatest((\"Value\"::int / 60) + 1, 6) WHERE \"Key\" = 'importlistsyncinterval'");
        }
    }
}

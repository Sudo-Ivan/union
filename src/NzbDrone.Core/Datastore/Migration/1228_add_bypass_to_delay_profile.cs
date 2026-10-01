using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1228)]
    public class add_custom_format_score_bypass_to_delay_profile_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("DelayProfiles").Column("BypassIfAboveCustomFormatScore").Exists())
            {
            Alter.Table("DelayProfiles").AddColumn("BypassIfAboveCustomFormatScore").AsBoolean().WithDefaultValue(false);
            }

            if (!Schema.Table("DelayProfiles").Column("MinimumCustomFormatScore").Exists())
            {
            Alter.Table("DelayProfiles").AddColumn("MinimumCustomFormatScore").AsInt32().Nullable();
            }
        }
    }
}

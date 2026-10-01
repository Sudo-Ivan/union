using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1239)]
    public class add_minimum_upgrade_format_score_to_quality_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("QualityProfiles").Column("MinUpgradeFormatScore").Exists())
            {
            Alter.Table("QualityProfiles").AddColumn("MinUpgradeFormatScore").AsInt32().WithDefaultValue(1);
            }
        }
    }
}

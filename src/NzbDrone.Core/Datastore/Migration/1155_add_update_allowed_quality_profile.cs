using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1155)]
    public class add_update_allowed_quality_profile : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("QualityProfiles").Column("UpgradeAllowed").Exists())
            {
            if (!Schema.Table("QualityProfiles").Column("UpgradeAllowed").Exists())
            {
            Alter.Table("QualityProfiles").AddColumn("UpgradeAllowed").AsBoolean().Nullable();
            }
            }

            // Set upgrade allowed for existing profiles (default will be false for new profiles)
            if (Schema.Table("QualityProfiles").Column("UpgradeAllowed").Exists())
            {
            Update.Table("QualityProfiles").Set(new { UpgradeAllowed = true }).AllRows();
            }
        }
    }
}

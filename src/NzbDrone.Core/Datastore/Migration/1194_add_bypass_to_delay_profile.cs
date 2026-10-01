using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1194)]
    public class add_bypass_to_delay_profile_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("DelayProfiles").Column("BypassIfHighestQuality").Exists())
            {
            Alter.Table("DelayProfiles").AddColumn("BypassIfHighestQuality").AsBoolean().WithDefaultValue(false);
            }

            // Set to true for existing Delay Profiles to keep behavior the same.
            Update.Table("DelayProfiles").Set(new { BypassIfHighestQuality = true }).AllRows();
        }
    }
}

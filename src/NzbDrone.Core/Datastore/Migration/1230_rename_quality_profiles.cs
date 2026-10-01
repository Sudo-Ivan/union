using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1230)]
    public class rename_quality_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("Profiles").Exists() && !Schema.Table("QualityProfiles").Exists())
            {
            if (Schema.Table("Profiles").Exists() && !Schema.Table("QualityProfiles").Exists())
            {
            Rename.Table("Profiles").To("QualityProfiles");
            }
            }

            if (Schema.Table("Movies").Column("ProfileId").Exists() && !Schema.Table("Movies").Column("QualityProfileId").Exists())
            {
            if (Schema.Table("Movies").Column("ProfileId").Exists() && !Schema.Table("Movies").Column("QualityProfileId").Exists())
            {
            Rename.Column("ProfileId").OnTable("Movies").To("QualityProfileId");
            }
            }

            if (Schema.Table("ImportLists").Column("ProfileId").Exists() && !Schema.Table("ImportLists").Column("QualityProfileId").Exists())
            {
            if (Schema.Table("ImportLists").Column("ProfileId").Exists() && !Schema.Table("ImportLists").Column("QualityProfileId").Exists())
            {
            Rename.Column("ProfileId").OnTable("ImportLists").To("QualityProfileId");
            }
            }
        }
    }
}

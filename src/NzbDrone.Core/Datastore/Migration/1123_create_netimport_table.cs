using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1123)]
    public class create_netimport_table : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("ImportLists").Exists())
            {
                Create.TableForModel("ImportLists")
                    .WithColumn("Enabled").AsBoolean()
                    .WithColumn("Name").AsString().Unique()
                    .WithColumn("Implementation").AsString()
                    .WithColumn("ConfigContract").AsString().Nullable()
                    .WithColumn("Settings").AsString().Nullable()
                    .WithColumn("EnableAuto").AsBoolean()
                    .WithColumn("RootFolderPath").AsString()
                    .WithColumn("ShouldMonitor").AsBoolean()
                    .WithColumn("ProfileId").AsInt32();
            }
            else
            {
                // Union: ImportLists already exists from the sonarr lineage, add the
                // movie-domain columns the merged entity maps.
                if (!Schema.Table("ImportLists").Column("Enabled").Exists())
                {
                    Alter.Table("ImportLists").AddColumn("Enabled").AsBoolean().WithDefaultValue(true);
                }

                if (!Schema.Table("ImportLists").Column("EnableAuto").Exists())
                {
                    Alter.Table("ImportLists").AddColumn("EnableAuto").AsBoolean().WithDefaultValue(false);
                }
            }
        }
    }
}

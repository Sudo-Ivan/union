using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1151)]
    public class add_tags_to_net_import : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("ImportLists").Column("Tags").Exists())
            {
            Alter.Table("ImportLists")
                 .AddColumn("Tags").AsString().Nullable();
            }

            try
            {
            Execute.Sql("UPDATE \"ImportLists\" SET \"Tags\" = '[]'");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }
    }
}

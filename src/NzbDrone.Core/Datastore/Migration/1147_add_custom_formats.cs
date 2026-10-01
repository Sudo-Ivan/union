using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1147)]
    public class add_custom_formats_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // WithConnectionGuarded(RenameUrlToBaseUrl);
            if (!Schema.Table("CustomFormats").Exists())
            {
            if (!Schema.Table("CustomFormats").Exists())
            {
            Create.TableForModel("CustomFormats")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("FormatTags").AsString();
            }
            }

            if (!Schema.Table("QualityProfiles").Column("FormatItems").Exists())
            {
            if (!Schema.Table("QualityProfiles").Column("FormatItems").Exists())
            {
            Alter.Table("QualityProfiles").AddColumn("FormatItems").AsString().WithDefaultValue("[{format:0, allowed:true}]").AddColumn("FormatCutoff").AsInt32().WithDefaultValue(0);
            }
            }

            try
            {
            WithConnectionGuarded(AddCustomFormatsToProfile);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void AddCustomFormatsToProfile(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}

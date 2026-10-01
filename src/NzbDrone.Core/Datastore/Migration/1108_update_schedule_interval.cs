using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1108)]
    public class update_schedule_intervale : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("ScheduledTasks").Column("Interval").Exists())
            {
            Alter.Table("ScheduledTasks").AlterColumn("Interval").AsDouble();
            }
            try
            {
            Execute.Sql("UPDATE \"ScheduledTasks\" SET \"Interval\" = 0.25 WHERE \"TypeName\" = 'NzbDrone.Core.Download.CheckForFinishedDownloadCommand'");
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }
    }
}

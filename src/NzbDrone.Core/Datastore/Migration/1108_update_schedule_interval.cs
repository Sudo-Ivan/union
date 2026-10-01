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
            if (Schema.Table("ScheduledTasks").Column("Interval").Exists())
            {
            Alter.Table("ScheduledTasks").AlterColumn("Interval").AsDouble();
            }
            }

            if (Schema.Table("ScheduledTasks").Exists() && Schema.Table("ScheduledTasks").Column("Interval").Exists())
            {
            Execute.Sql("UPDATE \"ScheduledTasks\" SET \"Interval\" = 0.25 WHERE \"TypeName\" = 'NzbDrone.Core.Download.CheckForFinishedDownloadCommand'");
            }
        }
    }
}

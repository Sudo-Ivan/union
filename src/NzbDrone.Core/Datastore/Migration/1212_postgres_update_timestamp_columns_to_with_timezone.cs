using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1212)]
    public class postgres_update_timestamp_columns_to_with_timezone_r : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Commands").AllRows();

            if (Schema.Table("Blocklist").Column("Date").Exists() && !Schema.Table("Blocklist").Index("IX_Blocklist_QualityId").Exists())
            {
            Alter.Table("Blocklist").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("Blocklist").Column("PublishedDate").Exists() && !Schema.Table("Blocklist").Index("IX_Blocklist_QualityId").Exists())
            {
            Alter.Table("Blocklist").AlterColumn("PublishedDate").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("Collections").Column("Added").Exists())
            {
            Alter.Table("Collections").AlterColumn("Added").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("Collections").Column("LastInfoSync").Exists())
            {
            Alter.Table("Collections").AlterColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("Commands").Column("QueuedAt").Exists())
            {
            Alter.Table("Commands").AlterColumn("QueuedAt").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("Commands").Column("StartedAt").Exists())
            {
            Alter.Table("Commands").AlterColumn("StartedAt").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("Commands").Column("EndedAt").Exists())
            {
            Alter.Table("Commands").AlterColumn("EndedAt").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("DownloadClientStatus").Column("InitialFailure").Exists())
            {
            Alter.Table("DownloadClientStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("DownloadClientStatus").Column("MostRecentFailure").Exists())
            {
            Alter.Table("DownloadClientStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("DownloadClientStatus").Column("DisabledTill").Exists())
            {
            Alter.Table("DownloadClientStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("DownloadHistory").Column("Date").Exists())
            {
            Alter.Table("DownloadHistory").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("ExtraFiles").Column("Added").Exists())
            {
            Alter.Table("ExtraFiles").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("ExtraFiles").Column("LastUpdated").Exists())
            {
            Alter.Table("ExtraFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("History").Column("Date").Exists() && !Schema.Table("History").Index("IX_History_QualityId").Exists())
            {
            Alter.Table("History").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("ImportListStatus").Column("InitialFailure").Exists())
            {
            Alter.Table("ImportListStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("ImportListStatus").Column("MostRecentFailure").Exists())
            {
            Alter.Table("ImportListStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("ImportListStatus").Column("DisabledTill").Exists())
            {
            Alter.Table("ImportListStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("IndexerStatus").Column("InitialFailure").Exists())
            {
            Alter.Table("IndexerStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("IndexerStatus").Column("MostRecentFailure").Exists())
            {
            Alter.Table("IndexerStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("IndexerStatus").Column("DisabledTill").Exists())
            {
            Alter.Table("IndexerStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("IndexerStatus").Column("CookiesExpirationDate").Exists())
            {
            Alter.Table("IndexerStatus").AlterColumn("CookiesExpirationDate").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("MetadataFiles").Column("LastUpdated").Exists())
            {
            Alter.Table("MetadataFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("MetadataFiles").Column("Added").Exists())
            {
            Alter.Table("MetadataFiles").AlterColumn("Added").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("MovieFiles").Column("DateAdded").Exists())
            {
            Alter.Table("MovieFiles").AlterColumn("DateAdded").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("MovieMetadata").Column("DigitalRelease").Exists())
            {
            Alter.Table("MovieMetadata").AlterColumn("DigitalRelease").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("MovieMetadata").Column("InCinemas").Exists())
            {
            Alter.Table("MovieMetadata").AlterColumn("InCinemas").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("MovieMetadata").Column("LastInfoSync").Exists())
            {
            Alter.Table("MovieMetadata").AlterColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("MovieMetadata").Column("PhysicalRelease").Exists())
            {
            Alter.Table("MovieMetadata").AlterColumn("PhysicalRelease").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("Movies").Column("Added").Exists())
            {
            Alter.Table("Movies").AlterColumn("Added").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("PendingReleases").Column("Added").Exists())
            {
            Alter.Table("PendingReleases").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("ScheduledTasks").Column("LastExecution").Exists())
            {
            Alter.Table("ScheduledTasks").AlterColumn("LastExecution").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("ScheduledTasks").Column("LastStartTime").Exists())
            {
            Alter.Table("ScheduledTasks").AlterColumn("LastStartTime").AsDateTimeOffset().Nullable();
            }

            if (Schema.Table("SubtitleFiles").Column("Added").Exists())
            {
            Alter.Table("SubtitleFiles").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("SubtitleFiles").Column("LastUpdated").Exists())
            {
            Alter.Table("SubtitleFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("VersionInfo").Column("AppliedOn").Exists())
            {
            Alter.Table("VersionInfo").AlterColumn("AppliedOn").AsDateTimeOffset().Nullable();
            }
        }

        protected override void LogDbUpgrade()
        {
            if (Schema.Table("Logs").Column("Time").Exists())
            {
            Alter.Table("Logs").AlterColumn("Time").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("UpdateHistory").Column("Date").Exists())
            {
            Alter.Table("UpdateHistory").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            }

            if (Schema.Table("VersionInfo").Column("AppliedOn").Exists())
            {
            Alter.Table("VersionInfo").AlterColumn("AppliedOn").AsDateTimeOffset().Nullable();
            }
        }
    }
}

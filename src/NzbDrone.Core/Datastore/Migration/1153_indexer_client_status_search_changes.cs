using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1153)]
    public class indexer_client_status_search_changes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Cleanup cases of Sonarr Interference with Radarr db
            if (Schema.Table("PendingReleases").Column("Reason").Exists())
            {
                if (Schema.Table("PendingReleases").Column("Reason").Exists())
                {
            if (Schema.Table("PendingReleases").Column("Reason").Exists())
            {
                Delete.Column("Reason").FromTable("PendingReleases");
            }
                }
            }

            if (!Schema.Table("PendingReleases").Column("Reason").Exists())
            {
            if (!Schema.Table("PendingReleases").Column("Reason").Exists())
            {
            Alter.Table("PendingReleases").AddColumn("Reason").AsInt32().WithDefaultValue(0);
            }
            }

            if (Schema.Table("IndexerStatus").Column("IndexerId").Exists() && !Schema.Table("IndexerStatus").Column("ProviderId").Exists())
            {
            if (Schema.Table("IndexerStatus").Column("IndexerId").Exists() && !Schema.Table("IndexerStatus").Column("ProviderId").Exists())
            {
            Rename.Column("IndexerId").OnTable("IndexerStatus").To("ProviderId");
            }
            }

            if (Schema.Table("Indexers").Column("EnableSearch").Exists() && !Schema.Table("Indexers").Column("EnableAutomaticSearch").Exists())
            {
            if (Schema.Table("Indexers").Column("EnableSearch").Exists() && !Schema.Table("Indexers").Column("EnableAutomaticSearch").Exists())
            {
            Rename.Column("EnableSearch").OnTable("Indexers").To("EnableAutomaticSearch");
            }
            }

            if (!Schema.Table("Indexers").Column("EnableInteractiveSearch").Exists())
            {
            if (!Schema.Table("Indexers").Column("EnableInteractiveSearch").Exists())
            {
            Alter.Table("Indexers").AddColumn("EnableInteractiveSearch").AsBoolean().Nullable();
            }
            }

            if (Schema.Table("Indexers").Exists() && Schema.Table("Indexers").Column("EnableInteractiveSearch").Exists())
            {
            Execute.Sql("UPDATE \"Indexers\" SET \"EnableInteractiveSearch\" = \"EnableAutomaticSearch\"");
            }

            if (Schema.Table("Indexers").Column("EnableInteractiveSearch").Exists())
            {
            if (Schema.Table("Indexers").Column("EnableInteractiveSearch").Exists())
            {
            Alter.Table("Indexers").AlterColumn("EnableInteractiveSearch").AsBoolean().NotNullable();
            }
            }

            if (!Schema.Table("DownloadClientStatus").Exists())
            {
            if (!Schema.Table("DownloadClientStatus").Exists())
            {
            Create.TableForModel("DownloadClientStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable();
            }
            }
        }
    }
}

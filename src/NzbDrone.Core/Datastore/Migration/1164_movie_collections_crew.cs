using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1164)]
    public class movie_collections_crew : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("Collection").Exists())
            {
            if (!Schema.Table("Movies").Column("Collection").Exists())
            {
            Alter.Table("Movies").AddColumn("Collection").AsString().Nullable();
            }
            }

            if (Schema.Table("Movies").Column("Actors").Exists())
            {
            if (Schema.Table("Movies").Column("Actors").Exists())
            {
            Delete.Column("Actors").FromTable("Movies");
            }
            }

            if (!Schema.Table("Credits").Exists())
            {
            if (!Schema.Table("Credits").Exists())
            {
            Create.TableForModel("Credits").WithColumn("MovieId").AsInt32()
                                  .WithColumn("CreditTmdbId").AsString().Unique()
                                  .WithColumn("PersonTmdbId").AsInt32()
                                  .WithColumn("Name").AsString()
                                  .WithColumn("Images").AsString()
                                  .WithColumn("Character").AsString().Nullable()
                                  .WithColumn("Order").AsInt32()
                                  .WithColumn("Job").AsString().Nullable()
                                  .WithColumn("Department").AsString().Nullable()
                                  .WithColumn("Type").AsInt32();
            }
            }

            if (!Schema.Table("Credits").Index("IX_Credits_MovieId").Exists())
            {
            if (Schema.Table("Credits").Column("MovieId").Exists())
            {
            Create.Index().OnTable("Credits").OnColumn("MovieId");
            }
            }

            if (Schema.Table("Notifications").Exists())
            {
            Delete.FromTable("Notifications").Row(new { Implementation = "NotifyMyAndroid" });
            }

            if (Schema.Table("Notifications").Exists())
            {
            Delete.FromTable("Notifications").Row(new { Implementation = "Pushalot" });
            }
        }
    }
}

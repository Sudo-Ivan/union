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
            Alter.Table("Movies").AddColumn("Collection").AsString().Nullable();
            }
            if (Schema.Table("Movies").Column("Actors").Exists())
            {
            Delete.Column("Actors").FromTable("Movies");
            }

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

            if (!Schema.Table("Credits").Index("IX_Credits_MovieId").Exists())
            {
            Create.Index().OnTable("Credits").OnColumn("MovieId");
            }

            Delete.FromTable("Notifications").Row(new { Implementation = "NotifyMyAndroid" });
            Delete.FromTable("Notifications").Row(new { Implementation = "Pushalot" });
        }
    }
}

using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1106)]
    public class add_tmdb_stuff : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Movies").Column("TmdbId").Exists())
            {
            if (!Schema.Table("Movies").Column("TmdbId").Exists())
            {
            Alter.Table("Movies")
                  .AddColumn("TmdbId").AsInt32().WithDefaultValue(0);
            }
            }

            if (!Schema.Table("Movies").Column("Website").Exists())
            {
            if (!Schema.Table("Movies").Column("Website").Exists())
            {
            Alter.Table("Movies")
                .AddColumn("Website").AsString().Nullable();
            }
            }

            if (Schema.Table("Movies").Column("ImdbId").Exists())
            {
            if (Schema.Table("Movies").Column("ImdbId").Exists())
            {
            Alter.Table("Movies")
                .AlterColumn("ImdbId").AsString().Nullable();
            }
            }

            if (!Schema.Table("Movies").Column("AlternativeTitles").Exists())
            {
            if (!Schema.Table("Movies").Column("AlternativeTitles").Exists())
            {
            Alter.Table("Movies")
                .AddColumn("AlternativeTitles").AsString().Nullable();
            }
            }
        }
    }
}

using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1216)]
    public class clean_alt_titles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (Schema.Table("AlternativeTitles").Column("Language").Exists())
            {
            Delete.Column("Language").FromTable("AlternativeTitles");
            }
            if (Schema.Table("AlternativeTitles").Column("Votes").Exists())
            {
            Delete.Column("Votes").FromTable("AlternativeTitles");
            }
            if (Schema.Table("AlternativeTitles").Column("VoteCount").Exists())
            {
            Delete.Column("VoteCount").FromTable("AlternativeTitles");
            }
            if (Schema.Table("AlternativeTitles").Column("SourceId").Exists())
            {
            Delete.Column("SourceId").FromTable("AlternativeTitles");
            }
        }
    }
}

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1177)]
    public class language_improvements : NzbDroneMigrationBase
    {
        private readonly JsonSerializerOptions _serializerSettings;

        public language_improvements()
        {
            _serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNameCaseInsensitive = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        protected override void MainDbUpgrade()
        {
            // Use original language to set default language fallback for releases
            // Set all to English (1) on migration to ensure default behavior persists until refresh
            if (!Schema.Table("Movies").Column("OriginalLanguage").Exists())
            {
            if (!Schema.Table("Movies").Column("OriginalLanguage").Exists())
            {
            Alter.Table("Movies").AddColumn("OriginalLanguage").AsInt32().WithDefaultValue((int)Language.English);
            }
            }

            if (!Schema.Table("Movies").Column("OriginalTitle").Exists())
            {
            if (!Schema.Table("Movies").Column("OriginalTitle").Exists())
            {
            Alter.Table("Movies").AddColumn("OriginalTitle").AsString().Nullable();
            }
            }

            if (!Schema.Table("Movies").Column("DigitalRelease").Exists())
            {
            if (!Schema.Table("Movies").Column("DigitalRelease").Exists())
            {
            Alter.Table("Movies").AddColumn("DigitalRelease").AsDateTime().Nullable();
            }
            }

            // Column not used
            if (Schema.Table("Movies").Column("PhysicalReleaseNote").Exists())
            {
            if (Schema.Table("Movies").Column("PhysicalReleaseNote").Exists())
            {
            Delete.Column("PhysicalReleaseNote").FromTable("Movies");
            }
            }

            if (Schema.Table("Movies").Column("SecondaryYearSourceId").Exists())
            {
            if (Schema.Table("Movies").Column("SecondaryYearSourceId").Exists())
            {
            Delete.Column("SecondaryYearSourceId").FromTable("Movies");
            }
            }

            if (!Schema.Table("NamingConfig").Column("RenameMovies").Exists())
            {
            if (!Schema.Table("NamingConfig").Column("RenameMovies").Exists())
            {
            Alter.Table("NamingConfig").AddColumn("RenameMovies").AsBoolean().WithDefaultValue(false);
            }
            }

            try
            {
            // Union: keep RenameEpisodes; add RenameMovies separately for movies
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            // Manual SQL, Fluent Migrator doesn't support multi-column unique constraint on table creation, SQLite doesn't support adding it after creation
            if (!Schema.Table("MovieTranslations").Exists())
            {
                IfDatabase("sqlite").Execute.Sql("CREATE TABLE \"MovieTranslations\"(" +
                    "\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    "\"MovieId\" INTEGER NOT NULL, " +
                    "\"Title\" TEXT, " +
                    "\"CleanTitle\" TEXT, " +
                    "\"Overview\" TEXT, " +
                    "\"Language\" INTEGER NOT NULL, " +
                    "Unique(\"MovieId\", \"Language\"));");

                IfDatabase("postgres").Execute.Sql("CREATE TABLE \"MovieTranslations\"(" +
                    "\"Id\" SERIAL PRIMARY KEY , " +
                    "\"MovieId\" INTEGER NOT NULL, " +
                    "\"Title\" TEXT, " +
                    "\"CleanTitle\" TEXT, " +
                    "\"Overview\" TEXT, " +
                    "\"Language\" INTEGER NOT NULL, " +
                    "Unique(\"MovieId\", \"Language\"));");
            }

            // Prevent failure if two movies have same alt titles
            Execute.Sql("DROP INDEX IF EXISTS \"IX_AlternativeTitles_CleanTitle\"");

            try
            {
            WithConnectionGuarded(FixLanguagesMoveFile);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            try
            {
            WithConnectionGuarded(FixLanguagesHistory);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            // Force refresh all movies in library
            if (Schema.Table("ScheduledTasks").Column("LastExecution").Exists())
            {
            Update.Table("ScheduledTasks")
                .Set(new { LastExecution = "2014-01-01 00:00:00" })
                .Where(new { TypeName = "NzbDrone.Core.Movies.Commands.RefreshMovieCommand" });
            }

            if (Schema.Table("Movies").Column("LastInfoSync").Exists())
            {
            Update.Table("Movies")
                .Set(new { LastInfoSync = "2014-01-01 00:00:00" })
                .AllRows();
            }
        }

        private void FixLanguagesMoveFile(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var rows = conn.Query<LanguageEntity177>($"SELECT \"Id\", \"Languages\" FROM \"MovieFiles\"");

                var corrected = new List<LanguageEntity177>();

                foreach (var row in rows)
                {
                    var languages = JsonSerializer.Deserialize<List<int>>(row.Languages, _serializerSettings);

                    var newLanguages = languages.Distinct().ToList();

                    corrected.Add(new LanguageEntity177
                    {
                        Id = row.Id,
                        Languages = JsonSerializer.Serialize(newLanguages, _serializerSettings)
                    });
                }

                var updateSql = "UPDATE \"MovieFiles\" SET \"Languages\" = @Languages WHERE \"Id\" = @Id";
                conn.Execute(updateSql, corrected, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }

        private void FixLanguagesHistory(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var rows = conn.Query<LanguageEntity177>($"SELECT \"Id\", \"Languages\" FROM \"History\"");

                var corrected = new List<LanguageEntity177>();

                foreach (var row in rows)
                {
                    var languages = JsonSerializer.Deserialize<List<int>>(row.Languages, _serializerSettings);

                    var newLanguages = languages.Distinct().ToList();

                    corrected.Add(new LanguageEntity177
                    {
                        Id = row.Id,
                        Languages = JsonSerializer.Serialize(newLanguages, _serializerSettings)
                    });
                }

                var updateSql = "UPDATE \"History\" SET \"Languages\" = @Languages WHERE \"Id\" = @Id";
                conn.Execute(updateSql, corrected, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }

        private class LanguageEntity177 : ModelBase
        {
            public string Languages { get; set; }
        }
    }
}

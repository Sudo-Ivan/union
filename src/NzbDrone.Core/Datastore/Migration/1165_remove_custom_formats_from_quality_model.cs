using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1165)]
    public class remove_custom_formats_from_quality_model : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("Blocklist").Column("IndexerFlags").Exists())
            {
            if (!Schema.Table("Blocklist").Column("IndexerFlags").Exists())
            {
            Alter.Table("Blocklist").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
            }
            }

            if (!Schema.Table("MovieFiles").Column("IndexerFlags").Exists())
            {
            if (!Schema.Table("MovieFiles").Column("IndexerFlags").Exists())
            {
            Alter.Table("MovieFiles").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
            }
            }

            // Switch Quality and Language to int in pending releases, remove custom formats
            try
            {
            WithConnectionGuarded(FixPendingReleases);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            // Remove Custom Formats from QualityModel
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<QualityModel165>());
            try
            {
            WithConnectionGuarded((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "Blocklist"));
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            try
            {
            WithConnectionGuarded((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "History"));
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            try
            {
            WithConnectionGuarded((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "MovieFiles"));
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            // Fish out indexer flags from history
            try
            {
            WithConnectionGuarded(AddIndexerFlagsToBlacklist);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }

            try
            {
            WithConnectionGuarded(AddIndexerFlagsToMovieFiles);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "Union: skipping data migration step, schema shape differs");
            }
        }

        private void FixPendingReleases(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedMovieInfo164>());
                SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedMovieInfo165>());
                var rows = conn.Query<ParsedMovieInfoData164>("SELECT \"Id\", \"ParsedMovieInfo\" from \"PendingReleases\"");

                var newRows = new List<ParsedMovieInfoData165>();

                foreach (var row in rows)
                {
                    var old = row.ParsedMovieInfo;

                    var newQuality = new QualityModel165
                    {
                        Quality = old.Quality.Quality.Id,
                        Revision = old.Quality.Revision,
                        HardcodedSubs = old.Quality.HardcodedSubs
                    };

                    var languages = old.Languages?.Select(x => (Language)x).Select(x => x.Id).ToList();

                    var correct = new ParsedMovieInfo165
                    {
                        MovieTitle = old.MovieTitle,
                        SimpleReleaseTitle = old.SimpleReleaseTitle,
                        Quality = newQuality,
                        Languages = languages,
                        ReleaseGroup = old.ReleaseGroup,
                        ReleaseHash = old.ReleaseHash,
                        Edition = old.Edition,
                        Year = old.Year,
                        ImdbId = old.ImdbId
                    };

                    newRows.Add(new ParsedMovieInfoData165
                    {
                        Id = row.Id,
                        ParsedMovieInfo = correct
                    });
                }

                var sql = $"UPDATE \"PendingReleases\" SET \"ParsedMovieInfo\" = @ParsedMovieInfo WHERE \"Id\" = @Id";

                conn.Execute(sql, newRows, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }

        private void RemoveCustomFormatFromQuality(IDbConnection conn, IDbTransaction tran, string table)
        {
            var rows = conn.Query<QualityRow>($"SELECT \"Id\", \"Quality\" from \"{table}\"");

            var sql = $"UPDATE \"{table}\" SET \"Quality\" = @Quality WHERE \"Id\" = @Id";

            conn.Execute(sql, rows, transaction: tran);
        }

        private void AddIndexerFlagsToBlacklist(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var blacklists = conn.Query<BlacklistData>("SELECT \"Blocklist\".\"Id\", \"Blocklist\".\"TorrentInfoHash\", \"History\".\"Data\" " +
                                                           "FROM \"Blocklist\" " +
                                                           "JOIN \"History\" ON \"Blocklist\".\"MovieId\" = \"History\".\"MovieId\" " +
                                                           "WHERE \"History\".\"EventType\" = 1");

                var toUpdate = new List<IndexerFlagsItem>();

                foreach (var item in blacklists)
                {
                    var dict = Json.Deserialize<Dictionary<string, string>>(item.Data);

                    if (dict.GetValueOrDefault("torrentInfoHash") == item.TorrentInfoHash &&
                        Enum.TryParse(dict.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                    {
                        if (flags != 0)
                        {
                            toUpdate.Add(new IndexerFlagsItem
                            {
                                Id = item.Id,
                                IndexerFlags = (int)flags
                            });
                        }
                    }
                }

                var updateSql = "UPDATE \"Blocklist\" SET \"IndexerFlags\" = @IndexerFlags WHERE \"Id\" = @Id";
                conn.Execute(updateSql, toUpdate, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }

        private void AddIndexerFlagsToMovieFiles(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var movieFiles = conn.Query<MovieFileData>("SELECT \"MovieFiles\".\"Id\", \"MovieFiles\".\"SceneName\", \"History\".\"SourceTitle\", \"History\".\"Data\" " +
                                                           "FROM \"MovieFiles\" " +
                                                           "JOIN \"History\" ON \"MovieFiles\".\"MovieId\" = \"History\".\"MovieId\" " +
                                                           "WHERE \"History\".\"EventType\" = 1");

                var toUpdate = new List<IndexerFlagsItem>();

                foreach (var item in movieFiles)
                {
                    var dict = Json.Deserialize<Dictionary<string, string>>(item.Data);

                    if (item.SourceTitle == item.SceneName &&
                        Enum.TryParse(dict.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                    {
                        if (flags != 0)
                        {
                            toUpdate.Add(new IndexerFlagsItem
                            {
                                Id = item.Id,
                                IndexerFlags = (int)flags
                            });
                        }
                    }
                }

                var updateSql = "UPDATE \"MovieFiles\" SET \"IndexerFlags\" = @IndexerFlags WHERE \"Id\" = @Id";
                conn.Execute(updateSql, toUpdate, transaction: tran);
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }

        private class ParsedMovieInfoData164 : ModelBase
        {
            public ParsedMovieInfo164 ParsedMovieInfo { get; set; }
        }

        private class ParsedMovieInfo164
        {
            public string MovieTitle { get; set; }
            public string SimpleReleaseTitle { get; set; }
            public QualityModel164 Quality { get; set; }
            public List<string> Languages { get; set; }
            public string ReleaseGroup { get; set; }
            public string ReleaseHash { get; set; }
            public string Edition { get; set; }
            public int Year { get; set; }
            public string ImdbId { get; set; }
        }

        private class QualityModel164
        {
            public Quality164 Quality { get; set; }
            public Revision165 Revision { get; set; }
            public string HardcodedSubs { get; set; }
        }

        private class Quality164
        {
            public int Id { get; set; }
        }

        private class ParsedMovieInfoData165 : ModelBase
        {
            public ParsedMovieInfo165 ParsedMovieInfo { get; set; }
        }

        private class ParsedMovieInfo165
        {
            public string MovieTitle { get; set; }
            public string SimpleReleaseTitle { get; set; }
            public QualityModel165 Quality { get; set; }
            public List<int> Languages { get; set; }
            public string ReleaseGroup { get; set; }
            public string ReleaseHash { get; set; }
            public string Edition { get; set; }
            public int Year { get; set; }
            public string ImdbId { get; set; }
        }

        private class BlacklistData : ModelBase
        {
            public string TorrentInfoHash { get; set; }
            public string Data { get; set; }
        }

        private class MovieFileData : ModelBase
        {
            public string SceneName { get; set; }
            public string SourceTitle { get; set; }
            public string Data { get; set; }
        }

        private class IndexerFlagsItem : ModelBase
        {
            public int IndexerFlags { get; set; }
        }

        private class QualityRow : ModelBase
        {
            public QualityModel165 Quality { get; set; }
        }

        private class QualityModel165
        {
            public int Quality { get; set; }
            public Revision165 Revision { get; set; }
            public string HardcodedSubs { get; set; }
        }

        private class Revision165
        {
            public int Version { get; set; }
            public int Real { get; set; }
            public bool IsRepack { get; set; }
        }
    }
}

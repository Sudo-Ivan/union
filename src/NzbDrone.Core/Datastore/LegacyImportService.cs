using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Datastore
{
    public interface ILegacyImportService
    {
        void ImportIfNeeded();
    }

    // Imports a standalone radarr.db into the union database. The radarr config
    // folder goes to <appData>/radarr (radarr.db, config.xml, MediaCover). Runs
    // once; marks success with radarr.db.imported so failures can retry.
    public class LegacyImportService : ILegacyImportService, IHandle<ApplicationStartedEvent>
    {
        private readonly IMainDatabase _mainDb;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConnectionStringFactory _connectionStringFactory;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        private Dictionary<long, long> _movieIdMap;

        public LegacyImportService(IMainDatabase mainDb,
                                   IAppFolderInfo appFolderInfo,
                                   IConfigFileProvider configFileProvider,
                                   IConnectionStringFactory connectionStringFactory,
                                   IDiskProvider diskProvider)
        {
            _mainDb = mainDb;
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
            _connectionStringFactory = connectionStringFactory;
            _diskProvider = diskProvider;
            _logger = NzbDroneLogger.GetLogger(this);
        }

        public void Handle(ApplicationStartedEvent message)
        {
            ImportIfNeeded();
        }

        public void ImportIfNeeded()
        {
            var legacyFolder = Path.Combine(_appFolderInfo.AppDataFolder, "radarr");
            var legacyDb = Path.Combine(legacyFolder, "radarr.db");

            if (!_diskProvider.FileExists(legacyDb) || _diskProvider.FileExists(legacyDb + ".imported"))
            {
                return;
            }

            if (_mainDb.DatabaseType != DatabaseType.SQLite)
            {
                _logger.Warn("radarr.db found at {0} but the main database is not SQLite, skipping legacy import", legacyDb);
                return;
            }

            _logger.Info("Importing legacy Radarr database from {0}", legacyDb);

            try
            {
                // Raw connection: the pooled MiniProfiler-wrapped connection cannot
                // set CommandType and breaks Dapper on System.Data.SQLite.
                using var conn = new SQLiteConnection(_connectionStringFactory.MainDbConnection.ConnectionString);
                conn.Open();
                Execute(conn, "ATTACH DATABASE '" + legacyDb.Replace("'", "''") + "' AS radarr", null);
                Execute(conn, "BEGIN");

                try
                {
                    ImportDatabase(conn);
                    Execute(conn, "COMMIT");
                    File.WriteAllBytes(legacyDb + ".imported", Array.Empty<byte>());
                    CopyMediaCovers(Path.Combine(legacyFolder, "MediaCover"));
                    MergeConfigXml(Path.Combine(legacyFolder, "config.xml"));
                    _logger.Info("Legacy Radarr import finished");
                }
                catch
                {
                    Execute(conn, "ROLLBACK");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Legacy Radarr import failed, will retry on next start");
            }
        }

        private void ImportDatabase(DbConnection conn)
        {
            var maps = new Dictionary<string, Dictionary<long, long>>();

            // Shared definition tables: dedupe by natural key, keep a map of
            // radarr id -> union id for rows that were reused or inserted.
            maps["QualityProfiles"] = MergeDefinition(conn, "QualityProfiles", "QualityProfiles", new[] { "Name" });
            maps["Tags"] = MergeDefinition(conn, "Tags", "Tags", new[] { "Label" });
            maps["RootFolders"] = MergeDefinition(conn, "RootFolders", "RootFolders", new[] { "Path" });
            maps["CustomFormats"] = MergeDefinition(conn, "CustomFormats", "CustomFormats", new[] { "Name" });
            maps["Indexers"] = MergeDefinition(conn, "Indexers", "Indexers", new[] { "Implementation", "Name" });
            maps["DownloadClients"] = MergeDefinition(conn, "DownloadClients", "DownloadClients", new[] { "Implementation", "Name" });
            maps["Notifications"] = MergeDefinition(conn, "Notifications", "Notifications", new[] { "Implementation", "Name" });
            maps["ImportLists"] = MergeDefinition(conn, "ImportLists", "ImportLists", new[] { "Implementation", "Name" });
            maps["RemotePathMappings"] = MergeDefinition(conn, "RemotePathMappings", "RemotePathMappings", new[] { "Host", "RemotePath" });
            maps["CustomFilters"] = MergeDefinition(conn, "CustomFilters", "CustomFilters", new[] { "Label" });
            maps["Users"] = MergeDefinition(conn, "Users", "Users", new[] { "Username" });
            maps["Metadata"] = MergeDefinition(conn, "Metadata", "Metadata", new[] { "Implementation", "Name" });
            maps["Collections"] = MergeDefinition(conn, "Collections", "Collections", new[] { "TmdbId" });
            maps["ImportExclusions"] = MergeDefinition(conn, "ImportExclusions", "ImportExclusions", new[] { "TmdbId" });

            MergeDefinition(conn, "DelayProfiles", "DelayProfiles", new[] { "Order", "Protocol" });
            CopyTable(conn, "ReleaseProfiles", "ReleaseProfiles", FkRemap(("IndexerId", maps["Indexers"])), null);
            CopyTable(conn, "Restrictions", "ReleaseProfiles", FkRemap(("IndexerId", maps["Indexers"])), null);

            // Per-definition status rows follow the remapped owner ids.
            MergeDefinition(conn, "IndexerStatus", "IndexerStatus", new[] { "ProviderId" }, maps["Indexers"]);
            MergeDefinition(conn, "DownloadClientStatus", "DownloadClientStatus", new[] { "ProviderId" }, maps["DownloadClients"]);
            MergeDefinition(conn, "ImportListStatus", "ImportListStatus", new[] { "ProviderId" }, maps["ImportLists"]);
            MergeDefinition(conn, "NotificationStatus", "NotificationStatus", new[] { "ProviderId" }, maps["Notifications"]);

            // Movie-domain tables. Every row gets a fresh id and FK columns are
            // rewritten through the maps built above.
            var movieMap = new Dictionary<long, long>();
            var metadataMap = new Dictionary<long, long>();
            var fileMap = new Dictionary<long, long>();

            CopyTable(conn,
                      "Movies",
                      "Movies",
                      FkRemap(("QualityProfileId", maps["QualityProfiles"])),
                      movieMap,
                      deferColumns: new[] { "MovieMetadataId", "MovieFileId" },
                      jsonRemaps: FkRemap(("Tags", maps["Tags"])));

            CopyTable(conn, "MovieMetadata", "MovieMetadata", null, metadataMap);
            CopyTable(conn, "MovieFiles", "MovieFiles", FkRemap(("MovieId", movieMap)), fileMap);
            CopyTable(conn, "AlternativeTitles", "AlternativeTitles", FkRemap(("MovieMetadataId", metadataMap)), null);
            CopyTable(conn, "Credits", "Credits", FkRemap(("MovieMetadataId", metadataMap)), null);
            CopyTable(conn, "MovieTranslations", "MovieTranslations", FkRemap(("MovieMetadataId", metadataMap)), null);
            CopyTable(conn, "ImportListMovies", "ImportListMovies", FkRemap(("MovieMetadataId", metadataMap), ("ListId", maps["ImportLists"])), null);

            var historyRemap = FkRemap(("MovieId", movieMap));
            var fillJson = new[] { "EpisodeIds" };
            CopyTable(conn, "History", "History", historyRemap, null, fillColumns: fillJson);
            CopyTable(conn, "Blacklist", "Blocklist", historyRemap, null, fillColumns: fillJson);
            CopyTable(conn, "Blocklist", "Blocklist", historyRemap, null, fillColumns: fillJson);
            CopyTable(conn, "DownloadHistory", "DownloadHistory", historyRemap, null);

            var extraRemap = FkRemap(("MovieId", movieMap), ("MovieFileId", fileMap));
            CopyTable(conn, "SubtitleFiles", "SubtitleFiles", extraRemap, null);
            CopyTable(conn, "ExtraFiles", "ExtraFiles", extraRemap, null);
            CopyTable(conn, "MetadataFiles", "MetadataFiles", extraRemap, null);

            // Movies.MovieMetadataId and Movies.MovieFileId point at rows that did
            // not exist yet when Movies was inserted.
            UpdateMoviePointers(conn, movieMap, metadataMap, fileMap);

            MergeNamingConfig(conn);

            _movieIdMap = movieMap;
        }

        private static Dictionary<string, Dictionary<long, long>> FkRemap(params (string Column, Dictionary<long, long> Map)[] entries)
        {
            return entries.ToDictionary(e => e.Column, e => e.Map);
        }

        // Dedupe by key columns, insert missing rows, return old -> new id map.
        private Dictionary<long, long> MergeDefinition(DbConnection conn, string srcTable, string dstTable, string[] keyColumns, Dictionary<long, long> remap = null)
        {
            var result = new Dictionary<long, long>();

            if (!TableExists(conn, "radarr", srcTable) || !TableExists(conn, "main", dstTable))
            {
                return result;
            }

            var srcCols = GetColumns(conn, "radarr", srcTable);
            var dstCols = GetColumns(conn, "main", dstTable);
            var common = srcCols.Where(dstCols.Contains).ToList();
            var keys = keyColumns.Where(k => srcCols.Contains(k) && dstCols.Contains(k)).ToArray();

            var existing = new List<Dictionary<string, object>>();
            if (keys.Length > 0)
            {
                var select = "SELECT \"Id\", " + string.Join(", ", keys.Select(k => "\"" + k + "\"")) + " FROM \"main\".\"" + dstTable + "\"";
                existing = QueryRows(conn, select);
            }

            foreach (var dict in QueryRows(conn, "SELECT * FROM \"radarr\".\"" + srcTable + "\""))
            {
                if (!dict.TryGetValue("Id", out var idVal) || idVal == null || idVal == DBNull.Value)
                {
                    continue;
                }

                var oldId = Convert.ToInt64(idVal);

                var match = existing.FirstOrDefault(e => keys.All(k => ValuesEqual(e[k], dict.TryGetValue(k, out var keyVal) ? keyVal : null)));

                if (match != null)
                {
                    result[oldId] = Convert.ToInt64(match["Id"]);
                    continue;
                }

                var missing = MissingNotNullColumns(conn, "main", dstTable, srcCols);
                var cols = common.Where(c => c != "Id").ToList();
                foreach (var m in missing)
                {
                    cols.Add(m.Name);
                    dict[m.Name] = DefaultFor(m.Dflt, m.Type);
                }

                if (remap != null)
                {
                    foreach (var c in cols)
                    {
                        if (dict.TryGetValue(c, out var v) && v != null && v != DBNull.Value && long.TryParse(v.ToString(), out var fk) && remap.TryGetValue(fk, out var mappedId))
                        {
                            dict[c] = mappedId;
                        }
                    }
                }

                var sql = "INSERT INTO \"main\".\"" + dstTable + "\" (" + string.Join(", ", cols.Select(c => "\"" + c + "\"")) + ") VALUES (" + string.Join(", ", cols.Select(_ => "?")) + ")";
                Execute(conn, sql, cols.Select(c => dict.TryGetValue(c, out var v) ? v : null).ToList());

                var newId = ScalarLong(conn, "SELECT last_insert_rowid()");
                result[oldId] = newId;

                var tracked = dict.Where(kv => kv.Key == "Id" || keys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                tracked["Id"] = newId;
                existing.Add(tracked);
            }

            _logger.Debug("Imported {0} rows from radarr.{1} into {2}", result.Count, srcTable, dstTable);

            return result;
        }

        private void CopyTable(DbConnection conn,
                               string srcTable,
                               string dstTable,
                               Dictionary<string, Dictionary<long, long>> fkRemaps,
                               Dictionary<long, long> idMap,
                               string[] deferColumns = null,
                               Dictionary<string, Dictionary<long, long>> jsonRemaps = null,
                               string[] fillColumns = null)
        {
            if (!TableExists(conn, "radarr", srcTable) || !TableExists(conn, "main", dstTable))
            {
                return;
            }

            var srcCols = GetColumns(conn, "radarr", srcTable);
            var dstCols = GetColumns(conn, "main", dstTable);
            var deferred = deferColumns ?? Array.Empty<string>();

            // Deferred FK columns stay in the insert as 0 (NOT NULL) and get fixed
            // once the referenced rows exist.
            var missing = MissingNotNullColumns(conn, "main", dstTable, srcCols);
            var common = srcCols.Where(dstCols.Contains).Where(c => c != "Id").ToList();
            var missingNames = missing.Select(m => m.Name).ToHashSet();
            common.AddRange(missingNames);

            foreach (var fill in fillColumns ?? Array.Empty<string>())
            {
                if (!common.Contains(fill) && dstCols.Contains(fill))
                {
                    common.Add(fill);
                }
            }

            var sql = "INSERT INTO \"main\".\"" + dstTable + "\" (" + string.Join(", ", common.Select(c => "\"" + c + "\"")) + ") VALUES (" + string.Join(", ", common.Select(_ => "?")) + ")";

            var count = 0;
            foreach (var dict in QueryRows(conn, "SELECT * FROM \"radarr\".\"" + srcTable + "\""))
            {
                dict.TryGetValue("Id", out var idVal);
                var oldId = idVal != null && idVal != DBNull.Value ? (long?)Convert.ToInt64(idVal) : null;

                foreach (var c in common)
                {
                    if (missingNames.Contains(c))
                    {
                        var m = missing.First(x => x.Name == c);
                        dict[c] = fillColumns != null && fillColumns.Contains(c) ? "[]" : DefaultFor(m.Dflt, m.Type);
                        continue;
                    }

                    if (deferred.Contains(c))
                    {
                        dict[c] = 0;
                        continue;
                    }

                    if (fillColumns != null && fillColumns.Contains(c) && !srcCols.Contains(c))
                    {
                        dict[c] = "[]";
                        continue;
                    }

                    if (!dict.TryGetValue(c, out var value) || value == null || value == DBNull.Value)
                    {
                        dict[c] = null;
                        continue;
                    }

                    if (fkRemaps != null && fkRemaps.TryGetValue(c, out var map) && map.Count > 0)
                    {
                        if (long.TryParse(value.ToString(), out var fk) && map.TryGetValue(fk, out var mapped))
                        {
                            dict[c] = mapped;
                        }
                    }
                    else if (jsonRemaps != null && jsonRemaps.TryGetValue(c, out var jsonMap) && jsonMap.Count > 0)
                    {
                        dict[c] = RemapJsonArray(value.ToString(), jsonMap);
                    }
                    else if (c == "Ratings" && value.ToString() == "[]")
                    {
                        // Older radarr builds stored an empty array where union
                        // expects a JSON object
                        dict[c] = "{}";
                    }
                    else if (c == "Quality")
                    {
                        dict[c] = NormalizeQualityJson(value.ToString());
                    }
                    else if (c == "Languages")
                    {
                        dict[c] = NormalizeIdArrayJson(value.ToString());
                    }
                    else if (c == "EpisodeIds" && string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        dict[c] = "[]";
                    }
                }

                Execute(conn, sql, common.Select(c => dict.TryGetValue(c, out var v) ? v : null).ToList());

                if (idMap != null && oldId.HasValue)
                {
                    idMap[oldId.Value] = ScalarLong(conn, "SELECT last_insert_rowid()");
                }

                count++;
            }

            _logger.Debug("Imported {0} rows from radarr.{1} into {2}", count, srcTable, dstTable);
        }

        private void UpdateMoviePointers(DbConnection conn, Dictionary<long, long> movieMap, Dictionary<long, long> metadataMap, Dictionary<long, long> fileMap)
        {
            var dstCols = GetColumns(conn, "main", "Movies");
            var srcCols = GetColumns(conn, "radarr", "Movies");

            if (dstCols.Contains("MovieMetadataId") && srcCols.Contains("MovieMetadataId"))
            {
                foreach (var dict in QueryRows(conn, "SELECT \"Id\", \"MovieMetadataId\" FROM \"radarr\".\"Movies\""))
                {
                    if (dict["MovieMetadataId"] == null || dict["MovieMetadataId"] == DBNull.Value || !movieMap.TryGetValue(Convert.ToInt64(dict["Id"]), out var newMovieId))
                    {
                        continue;
                    }

                    if (metadataMap.TryGetValue(Convert.ToInt64(dict["MovieMetadataId"]), out var newMetaId))
                    {
                        Execute(conn, "UPDATE \"main\".\"Movies\" SET \"MovieMetadataId\" = ? WHERE \"Id\" = ?", new List<object> { newMetaId, newMovieId });
                    }
                }
            }

            if (dstCols.Contains("MovieFileId") && srcCols.Contains("MovieFileId"))
            {
                foreach (var dict in QueryRows(conn, "SELECT \"Id\", \"MovieFileId\" FROM \"radarr\".\"Movies\""))
                {
                    if (dict["MovieFileId"] == null || dict["MovieFileId"] == DBNull.Value || !movieMap.TryGetValue(Convert.ToInt64(dict["Id"]), out var newMovieId))
                    {
                        continue;
                    }

                    if (fileMap.TryGetValue(Convert.ToInt64(dict["MovieFileId"]), out var newFileId))
                    {
                        Execute(conn, "UPDATE \"main\".\"Movies\" SET \"MovieFileId\" = ? WHERE \"Id\" = ?", new List<object> { newFileId, newMovieId });
                    }
                }
            }
        }

        private void MergeNamingConfig(DbConnection conn)
        {
            if (!TableExists(conn, "radarr", "NamingConfig"))
            {
                return;
            }

            var srcCols = GetColumns(conn, "radarr", "NamingConfig");
            var dstCols = GetColumns(conn, "main", "NamingConfig");
            var movieCols = new[] { "StandardMovieFormat", "MovieFolderFormat", "ColonReplacementFormat", "RenameMovies", "ReplaceIllegalCharacters", "ReplaceImproperCharacters", "Separator", "MultiEpisodeStyle" }
                .Where(c => srcCols.Contains(c) && dstCols.Contains(c))
                .ToList();

            var row = QueryRows(conn, "SELECT * FROM \"radarr\".\"NamingConfig\" LIMIT 1").FirstOrDefault();
            if (row == null || movieCols.Count == 0)
            {
                return;
            }

            var set = string.Join(", ", movieCols.Select(c => "\"" + c + "\" = ?"));
            Execute(conn, "UPDATE \"main\".\"NamingConfig\" SET " + set, movieCols.Select(c => row[c]).ToList());
        }

        private void MergeConfigXml(string radarrConfigPath)
        {
            if (!_diskProvider.FileExists(radarrConfigPath))
            {
                return;
            }

            var unionConfig = _configFileProvider.LoadConfigFile();
            var radarrConfig = System.Xml.Linq.XDocument.Load(radarrConfigPath);
            var additions = new Dictionary<string, object>();

            var existing = unionConfig.Root?.Elements().Select(e => e.Name.LocalName).ToHashSet() ?? new HashSet<string>();

            foreach (var el in radarrConfig.Root?.Elements() ?? Enumerable.Empty<System.Xml.Linq.XElement>())
            {
                if (!existing.Contains(el.Name.LocalName))
                {
                    additions[el.Name.LocalName] = el.Value;
                }
            }

            if (additions.Count > 0)
            {
                _configFileProvider.SaveConfigDictionary(additions);
                _logger.Info("Merged {0} keys from radarr config.xml", additions.Count);
            }
        }

        private void CopyMediaCovers(string sourceCoverFolder)
        {
            if (_movieIdMap == null || _movieIdMap.Count == 0 || !_diskProvider.FolderExists(sourceCoverFolder))
            {
                return;
            }

            var coverRoot = _appFolderInfo.GetMediaCoverPath();
            var copied = 0;

            foreach (var pair in _movieIdMap)
            {
                var src = Path.Combine(sourceCoverFolder, pair.Key.ToString());
                var dst = Path.Combine(coverRoot, pair.Value.ToString());

                if (_diskProvider.FolderExists(src) && !_diskProvider.FolderExists(dst))
                {
                    CopyDirectory(src, dst);
                    copied++;
                }
            }

            _logger.Debug("Copied {0} media cover folders for imported movies", copied);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
            }
        }

        // Columns that are NOT NULL in the union table but absent from the
        // radarr table (e.g. sonarr-side schema columns on shared tables).
        private static List<(string Name, string Dflt, string Type)> MissingNotNullColumns(DbConnection conn, string schema, string table, HashSet<string> srcCols)
        {
            var result = new List<(string, string, string)>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =  "SELECT name, [notnull], dflt_value, type FROM pragma_table_info('" + table.Replace("'", "''") + "', '" + schema + "')";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (!reader.IsDBNull(1) && reader.GetInt32(1) == 1 && !srcCols.Contains(name))
                {
                    result.Add((name, reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
                }
            }

            return result;
        }

        private static object DefaultFor(string dflt, string type)
        {
            if (dflt != null)
            {
                if (long.TryParse(dflt, out var l))
                {
                    return l;
                }

                return dflt.Trim('\u0027');
            }

            return type.Contains("INT") ? (object)0 : "";
        }

        private static void Execute(DbConnection conn, string sql, IList<object> parameters = null)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var values = parameters == null ? "none" : string.Join("|", parameters.Select(p => p == null || p == DBNull.Value ? "NULL" : p.ToString()));
                throw new Exception("Legacy import failed running: " + (sql.Length > 200 ? sql[..200] : sql) + " vals: " + values, ex);
            }
        }

        private static long ScalarLong(DbConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private static List<Dictionary<string, object>> QueryRows(DbConnection conn, string sql)
        {
            var rows = new List<Dictionary<string, object>>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var dict = new Dictionary<string, object>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.GetValue(i);
                }

                rows.Add(dict);
            }

            return rows;
        }

        private static void AddParameters(DbCommand cmd, IList<object> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (var v in parameters)
            {
                var p = cmd.CreateParameter();
                p.Value = v ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
        }

        private static bool TableExists(DbConnection conn, string schema, string table)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM \"" + schema + "\".sqlite_master WHERE type='table' AND name=@t";
            var p = cmd.CreateParameter();
            p.ParameterName = "@t";
            p.Value = table;
            cmd.Parameters.Add(p);
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private static HashSet<string> GetColumns(DbConnection conn, string schema, string table)
        {
            var result = new HashSet<string>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =  "SELECT name FROM pragma_table_info('" + table.Replace("'", "''") + "', '" + schema + "')";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        private static bool ValuesEqual(object a, object b)
        {
            if (a == null || b == null || a == DBNull.Value || b == DBNull.Value)
            {
                return a == null || a == DBNull.Value ? b == null || b == DBNull.Value : false;
            }

            return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // Radarr stores Quality as {"quality":{"id":7,...}} while union reads
        // the id inline as {"quality":7,...}. Keep every other field as is.
        private static string NormalizeQualityJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("quality", out var q) ||
                    q.ValueKind != JsonValueKind.Object)
                {
                    return json;
                }

                if (!q.TryGetProperty("id", out var id))
                {
                    return json;
                }

                var dict = new Dictionary<string, JsonElement>();
                foreach (var prop in root.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value;
                }

                dict["quality"] = JsonSerializer.SerializeToElement(id.GetInt32());
                return JsonSerializer.Serialize(dict);
            }
            catch (JsonException)
            {
                return json;
            }
        }

        // Radarr stores Languages as [{"id":1,...}] while union reads bare
        // ids as [1].
        private static string NormalizeIdArrayJson(string json)
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(json);
                if (items == null || items.Count == 0)
                {
                    return json;
                }

                if (items[0].ValueKind == JsonValueKind.Object)
                {
                    var ids = items
                        .Select(i => i.TryGetProperty("id", out var id) ? id.GetInt32() : (int?)null)
                        .Where(i => i.HasValue)
                        .Select(i => i.Value)
                        .ToList();

                    return JsonSerializer.Serialize(ids);
                }

                return json;
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private static string RemapJsonArray(string json, Dictionary<long, long> map)
        {
            try
            {
                var ids = JsonSerializer.Deserialize<List<long>>(json);
                if (ids == null)
                {
                    return json;
                }

                return JsonSerializer.Serialize(ids.Select(id => map.TryGetValue(id, out var m) ? m : id).ToList());
            }
            catch (JsonException)
            {
                return json;
            }
        }
    }
}

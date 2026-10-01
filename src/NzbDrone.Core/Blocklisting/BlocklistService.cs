using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Movies.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistService
    {
        bool Blocklisted(int id, ReleaseInfo release);
        bool BlocklistedTorrentHash(int id, string hash);
        PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec);
        List<Blocklist> GetBySeriesId(int seriesId);
        List<Blocklist> GetByMovieId(int movieId);
        void Block(RemoteEpisode remoteEpisode, string message, string source);
        void Block(RemoteMovie remoteMovie, string message, string source = null);
        void Delete(int id);
        void Delete(List<int> ids);
    }

    public class BlocklistService : IBlocklistService,
                                    IExecute<ClearBlocklistCommand>,
                                    IHandle<DownloadFailedEvent>,
                                    IHandleAsync<SeriesDeletedEvent>,
                                    IHandleAsync<MoviesDeletedEvent>
    {
        private readonly IBlocklistRepository _blocklistRepository;

        public BlocklistService(IBlocklistRepository blocklistRepository)
        {
            _blocklistRepository = blocklistRepository;
        }

        public bool Blocklisted(int id, ReleaseInfo release)
        {
            if (release.DownloadProtocol == DownloadProtocol.Torrent)
            {
                var torrentInfo = release as TorrentInfo;

                if (torrentInfo.InfoHash.IsNotNullOrWhiteSpace())
                {
                    var blocklistedByTorrentInfohash = _blocklistRepository.BlocklistedByTorrentInfoHash(id, torrentInfo.InfoHash);

                    return blocklistedByTorrentInfohash.Any(b => SameTorrent(b, torrentInfo));
                }

                return _blocklistRepository.BlocklistedByTitle(id, release.Title)
                    .Where(b => b.Protocol == DownloadProtocol.Torrent)
                    .Any(b => SameTorrent(b, torrentInfo));
            }

            return _blocklistRepository.BlocklistedByTitle(id, release.Title)
                .Where(b => b.Protocol == DownloadProtocol.Usenet)
                .Any(b => SameNzb(b, release));
        }

        public bool BlocklistedTorrentHash(int id, string hash)
        {
            return _blocklistRepository.BlocklistedByTorrentInfoHash(id, hash).Any(b =>
                b.TorrentInfoHash.Equals(hash, StringComparison.InvariantCultureIgnoreCase));
        }

        public PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec)
        {
            return _blocklistRepository.GetPaged(pagingSpec);
        }

        public List<Blocklist> GetBySeriesId(int seriesId)
        {
            return _blocklistRepository.BlocklistedBySeries(seriesId);
        }

        public List<Blocklist> GetByMovieId(int movieId)
        {
            return _blocklistRepository.BlocklistedByMovie(movieId);
        }

        public void Block(RemoteEpisode remoteEpisode, string message, string source)
        {
            var blocklist = new Blocklist
                            {
                                SeriesId = remoteEpisode.Series.Id,
                                EpisodeIds = remoteEpisode.Episodes.Select(e => e.Id).ToList(),
                                SourceTitle = remoteEpisode.Release.Title,
                                Quality = remoteEpisode.ParsedEpisodeInfo.Quality,
                                Date = DateTime.UtcNow,
                                PublishedDate = remoteEpisode.Release.PublishDate,
                                Size = remoteEpisode.Release.Size,
                                Indexer = remoteEpisode.Release.Indexer,
                                Protocol = remoteEpisode.Release.DownloadProtocol,
                                Message = message,
                                Source = source,
                                Languages = remoteEpisode.ParsedEpisodeInfo.Languages
                            };

            if (remoteEpisode.Release is TorrentInfo torrentRelease)
            {
                blocklist.TorrentInfoHash = torrentRelease.InfoHash;
            }

            _blocklistRepository.Insert(blocklist);
        }

        public void Block(RemoteMovie remoteMovie, string message, string source = null)
        {
            var blocklist = new Blocklist
                            {
                                MovieId = remoteMovie.Movie.Id,
                                SourceTitle = remoteMovie.Release.Title,
                                Quality = remoteMovie.ParsedMovieInfo.Quality,
                                Date = DateTime.UtcNow,
                                PublishedDate = remoteMovie.Release.PublishDate,
                                Size = remoteMovie.Release.Size,
                                Indexer = remoteMovie.Release.Indexer,
                                Protocol = remoteMovie.Release.DownloadProtocol,
                                Message = message,
                                Source = source,
                                Languages = remoteMovie.ParsedMovieInfo.Languages
                            };

            if (remoteMovie.Release is TorrentInfo torrentRelease)
            {
                blocklist.TorrentInfoHash = torrentRelease.InfoHash;
            }

            _blocklistRepository.Insert(blocklist);
        }

        public void Delete(int id)
        {
            _blocklistRepository.Delete(id);
        }

        public void Delete(List<int> ids)
        {
            _blocklistRepository.DeleteMany(ids);
        }

        private bool SameNzb(Blocklist item, ReleaseInfo release)
        {
            return ReleaseComparer.SameNzb(new ReleaseComparerModel(item), release);
        }

        private bool SameTorrent(Blocklist item, TorrentInfo release)
        {
            return ReleaseComparer.SameTorrent(new ReleaseComparerModel(item), release);
        }

        public void Execute(ClearBlocklistCommand message)
        {
            _blocklistRepository.Purge();
        }

        public void Handle(DownloadFailedEvent message)
        {
            var blocklist = new Blocklist
            {
                SeriesId = message.SeriesId,
                EpisodeIds = message.EpisodeIds,
                MovieId = message.MovieId,
                SourceTitle = message.SourceTitle,
                Quality = message.Quality,
                Date = DateTime.UtcNow,
                PublishedDate = DateTime.Parse(message.Data.GetValueOrDefault("publishedDate")),
                Size = long.Parse(message.Data.GetValueOrDefault("size", "0")),
                Indexer = message.Data.GetValueOrDefault("indexer"),
                Protocol = (DownloadProtocol)Convert.ToInt32(message.Data.GetValueOrDefault("protocol")),
                Message = message.Message,
                Source = message.Source,
                Languages = message.Languages,
                TorrentInfoHash = message.TrackedDownload?.Protocol == DownloadProtocol.Torrent
                    ? message.TrackedDownload.DownloadItem.DownloadId
                    : message.Data.GetValueOrDefault("torrentInfoHash", null)
            };

            if (Enum.TryParse(message.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
            {
                blocklist.IndexerFlags = flags;
            }

            if (Enum.TryParse(message.Data.GetValueOrDefault("releaseType"), true, out ReleaseType releaseType))
            {
                blocklist.ReleaseType = releaseType;
            }

            _blocklistRepository.Insert(blocklist);
        }

        public void HandleAsync(SeriesDeletedEvent message)
        {
            _blocklistRepository.DeleteForSeriesIds(message.Series.Select(m => m.Id).ToList());
        }

        public void HandleAsync(MoviesDeletedEvent message)
        {
            _blocklistRepository.DeleteForMovies(message.Movies.Select(m => m.Id).ToList());
        }
    }
}

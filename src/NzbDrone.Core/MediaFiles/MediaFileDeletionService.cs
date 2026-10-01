using System;
using System.IO;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Events;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDeleteMediaFiles
    {
        void DeleteEpisodeFile(Series series, EpisodeFile episodeFile);
        void DeleteMovieFile(Movie movie, MovieFile movieFile);
    }

    public class MediaFileDeletionService : IDeleteMediaFiles,
                                            IExecute<DeleteSeriesFilesCommand>,
                                            IHandleAsync<SeriesDeletedEvent>,
                                            IHandle<EpisodeFileDeletedEvent>,
                                            IHandleAsync<MoviesDeletedEvent>,
                                            IHandle<MovieFileDeletedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly ISeriesService _seriesService;
        private readonly IMovieService _movieService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IConfigService _configService;
        private readonly ICommandResultReporter _commandResultReporter;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MediaFileDeletionService(IDiskProvider diskProvider,
                                        IRecycleBinProvider recycleBinProvider,
                                        IMediaFileService mediaFileService,
                                        ISeriesService seriesService,
                                        IMovieService movieService,
                                        IRootFolderService rootFolderService,
                                        IConfigService configService,
                                        ICommandResultReporter commandResultReporter,
                                        IEventAggregator eventAggregator,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _seriesService = seriesService;
            _movieService = movieService;
            _rootFolderService = rootFolderService;
            _configService = configService;
            _commandResultReporter = commandResultReporter;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void DeleteEpisodeFile(Series series, EpisodeFile episodeFile)
        {
            var fullPath = Path.Combine(series.Path, episodeFile.RelativePath);
            var rootFolder = _rootFolderService.GetBestRootFolderPath(series.Path);

            if (!_diskProvider.FolderExists(rootFolder))
            {
                _logger.Warn("Series' root folder ({0}) doesn't exist.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Series' root folder ({0}) doesn't exist.", rootFolder);
            }

            if (_diskProvider.GetDirectories(rootFolder).Empty())
            {
                _logger.Warn("Series' root folder ({0}) is empty.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Series' root folder ({0}) is empty.", rootFolder);
            }

            if (_diskProvider.FolderExists(series.Path) && _diskProvider.FileExists(fullPath))
            {
                _logger.Info("Deleting episode file: {0}", fullPath);

                var subfolder = _diskProvider.GetParentFolder(series.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));

                try
                {
                    _recycleBinProvider.DeleteFile(fullPath, subfolder);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Unable to delete episode file");
                    throw new NzbDroneClientException(HttpStatusCode.InternalServerError, "Unable to delete episode file");
                }
            }

            // Delete the episode file from the database to clean it up even if the file was already deleted
            _mediaFileService.Delete(episodeFile, DeleteMediaFileReason.Manual);

            _eventAggregator.PublishEvent(new DeleteCompletedEvent());
        }

        public void DeleteMovieFile(Movie movie, MovieFile movieFile)
        {
            var fullPath = Path.Combine(movie.Path, movieFile.RelativePath);
            var rootFolder = _diskProvider.GetParentFolder(movie.Path);

            if (!_diskProvider.FolderExists(rootFolder))
            {
                _logger.Warn("Movie's root folder ({0}) doesn't exist.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Movie's root folder ({0}) doesn't exist.", rootFolder);
            }

            if (_diskProvider.GetDirectories(rootFolder).Empty())
            {
                _logger.Warn("Movie's root folder ({0}) is empty. Rescan will not update movies as a failsafe.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Movie's root folder ({0}) is empty. Rescan will not update movies as a failsafe.", rootFolder);
            }

            if (_diskProvider.FolderExists(movie.Path) && _diskProvider.FileExists(fullPath))
            {
                _logger.Info("Deleting movie file: {0}", fullPath);

                var subfolder = _diskProvider.GetParentFolder(movie.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));

                try
                {
                    _recycleBinProvider.DeleteFile(fullPath, subfolder);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Unable to delete movie file");
                    throw new NzbDroneClientException(HttpStatusCode.InternalServerError, "Unable to delete movie file");
                }
            }

            // Delete the movie file from the database to clean it up even if the file was already deleted
            _mediaFileService.Delete(movieFile, DeleteMediaFileReason.Manual);

            _eventAggregator.PublishEvent(new DeleteCompletedEvent());
        }

        public void Execute(DeleteSeriesFilesCommand message)
        {
            foreach (var seriesId in message.SeriesIds)
            {
                try
                {
                    var series = _seriesService.GetSeries(seriesId);
                    var mediaFiles = _mediaFileService.GetFilesBySeries(seriesId);

                    _logger.ProgressDebug("{0}: Deleting episode files}", series.Title);

                    if (mediaFiles.Count == 0)
                    {
                        _logger.Debug("No files found for series: {0}", series.Title);
                        continue;
                    }

                    var rootFolder = _rootFolderService.GetBestRootFolderPath(series.Path);

                    if (!_diskProvider.FolderExists(rootFolder))
                    {
                        _logger.Warn("Series' root folder ({0}) doesn't exist.", rootFolder);
                        _commandResultReporter.Report(CommandResult.Indeterminate);
                        continue;
                    }

                    if (_diskProvider.GetDirectories(rootFolder).Empty())
                    {
                        _logger.Warn("Series' root folder ({0}) is empty.", rootFolder);
                        _commandResultReporter.Report(CommandResult.Indeterminate);
                        continue;
                    }

                    if (!_diskProvider.FolderExists(series.Path))
                    {
                        _logger.Warn("Series' folder ({0}) does not exist.", series.Path);
                        _commandResultReporter.Report(CommandResult.Indeterminate);
                        continue;
                    }

                    foreach (var episodeFile in mediaFiles)
                    {
                        var fullPath = Path.Combine(series.Path, episodeFile.RelativePath);

                        if (_diskProvider.FileExists(fullPath))
                        {
                            _logger.Info("Deleting episode file: {0}", fullPath);

                            var subfolder = _diskProvider.GetParentFolder(series.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));

                            try
                            {
                                _recycleBinProvider.DeleteFile(fullPath, subfolder);
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, "Unable to delete episode file");
                                _commandResultReporter.Report(CommandResult.Indeterminate);
                                continue;
                            }

                            _mediaFileService.Delete(episodeFile, DeleteMediaFileReason.Manual);
                        }
                    }

                    _logger.ProgressDebug("{0}: Deleted episode files", series.Title);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to delete files for series with ID: {0}", seriesId);
                    _commandResultReporter.Report(CommandResult.Indeterminate);
                }
            }
        }

        public void HandleAsync(SeriesDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var allSeries = _seriesService.GetAllSeriesPaths();

                foreach (var series in message.Series)
                {
                    foreach (var s in allSeries)
                    {
                        if (s.Key == series.Id)
                        {
                            continue;
                        }

                        if (series.Path.IsParentPath(s.Value))
                        {
                            _logger.Error("Series path: '{0}' is a parent of another series, not deleting files.", series.Path);
                            return;
                        }

                        if (series.Path.PathEquals(s.Value))
                        {
                            _logger.Error("Series path: '{0}' is the same as another series, not deleting files.", series.Path);
                            return;
                        }
                    }

                    if (_diskProvider.FolderExists(series.Path))
                    {
                        _recycleBinProvider.DeleteFolder(series.Path);
                    }

                    _eventAggregator.PublishEvent(new DeleteCompletedEvent());
                }
            }
        }

        public void HandleAsync(MoviesDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var allMovies = _movieService.AllMoviePaths();

                foreach (var movie in message.Movies)
                {
                    foreach (var s in allMovies)
                    {
                        if (s.Key == movie.Id)
                        {
                            continue;
                        }

                        if (movie.Path.IsParentPath(s.Value))
                        {
                            _logger.Error("Movie path: '{0}' is a parent of another movie, not deleting files.", movie.Path);
                            return;
                        }

                        if (movie.Path.PathEquals(s.Value))
                        {
                            _logger.Error("Movie path: '{0}' is the same as another movie, not deleting files.", movie.Path);
                            return;
                        }
                    }

                    if (_diskProvider.FolderExists(movie.Path))
                    {
                        _recycleBinProvider.DeleteFolder(movie.Path);
                    }
                }

                _eventAggregator.PublishEvent(new DeleteCompletedEvent());
            }
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(EpisodeFileDeletedEvent message)
        {
            if (!_configService.DeleteEmptyFolders || message.Reason == DeleteMediaFileReason.MissingFromDisk)
            {
                return;
            }

            var series = message.EpisodeFile.Series.Value;
            var seriesPath = series.Path;
            var folder = message.EpisodeFile.Path.GetParentPath();

            while (seriesPath.IsParentPath(folder))
            {
                if (_diskProvider.FolderExists(folder))
                {
                    _diskProvider.RemoveEmptySubfolders(folder);
                }

                folder = folder.GetParentPath();
            }

            _diskProvider.RemoveEmptySubfolders(seriesPath);

            if (_diskProvider.FolderEmpty(seriesPath))
            {
                _diskProvider.DeleteFolder(seriesPath, true);
            }
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(MovieFileDeletedEvent message)
        {
            if (_configService.DeleteEmptyFolders)
            {
                var movie = message.MovieFile.Movie;
                var moviePath = movie.Path;
                var folder = message.MovieFile.Path.GetParentPath();

                while (moviePath.IsParentPath(folder))
                {
                    if (_diskProvider.FolderExists(folder))
                    {
                        _diskProvider.RemoveEmptySubfolders(folder);
                    }

                    folder = folder.GetParentPath();
                }

                _diskProvider.RemoveEmptySubfolders(moviePath);

                if (_diskProvider.FolderEmpty(moviePath))
                {
                    _diskProvider.DeleteFolder(moviePath, true);
                }
            }
        }
    }
}

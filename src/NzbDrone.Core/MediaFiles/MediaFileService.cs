using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileService
    {
        EpisodeFile Add(EpisodeFile episodeFile);
        MovieFile Add(MovieFile movieFile);
        void Update(EpisodeFile episodeFile);
        void Update(MovieFile movieFile);
        void Update(List<EpisodeFile> episodeFiles);
        void Update(List<MovieFile> movieFiles);
        void Delete(EpisodeFile episodeFile, DeleteMediaFileReason reason);
        void Delete(MovieFile movieFile, DeleteMediaFileReason reason);
        List<EpisodeFile> GetFilesBySeries(int seriesId);
        List<EpisodeFile> GetFilesBySeriesIds(List<int> seriesIds);
        List<EpisodeFile> GetFilesBySeason(int seriesId, int seasonNumber);
        List<EpisodeFile> GetFiles(IEnumerable<int> ids);
        List<EpisodeFile> GetFilesWithoutMediaInfo();
        List<string> FilterExistingFiles(List<string> files, Series series);
        EpisodeFile Get(int id);
        List<EpisodeFile> Get(IEnumerable<int> ids);
        List<EpisodeFile> GetFilesWithRelativePath(int seriesId, string relativePath);
        List<MovieFile> GetFilesByMovie(int movieId);
        List<MovieFile> GetFilesByMovies(IEnumerable<int> movieIds);
        List<MovieFile> GetMovieFilesWithoutMediaInfo();
        List<string> FilterExistingFiles(List<string> files, Movie movie);
        MovieFile GetMovie(int id);
        List<MovieFile> GetMovies(IEnumerable<int> ids);
        List<MovieFile> GetMovieFilesWithRelativePath(int movieId, string relativePath);
    }

    public class MediaFileService : IMediaFileService,
                                    IHandleAsync<SeriesDeletedEvent>,
                                    IHandleAsync<MoviesDeletedEvent>
    {
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MediaFileService(IMediaFileRepository mediaFileRepository,
                                IMovieRepository movieRepository,
                                IEventAggregator eventAggregator,
                                Logger logger)
        {
            _mediaFileRepository = mediaFileRepository;
            _movieRepository = movieRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public EpisodeFile Add(EpisodeFile episodeFile)
        {
            var addedFile = _mediaFileRepository.Insert(episodeFile);
            _eventAggregator.PublishEvent(new EpisodeFileAddedEvent(addedFile));
            return addedFile;
        }

        public MovieFile Add(MovieFile movieFile)
        {
            var addedFile = _mediaFileRepository.Insert(movieFile);
            if (addedFile.Movie == null)
            {
                addedFile.Movie = _movieRepository.Get(movieFile.MovieId);
            }

            _eventAggregator.PublishEvent(new MovieFileAddedEvent(addedFile));

            return addedFile;
        }

        public void Update(EpisodeFile episodeFile)
        {
            _mediaFileRepository.Update(episodeFile);
        }

        public void Update(MovieFile movieFile)
        {
            _mediaFileRepository.Update(movieFile);
        }

        public void Update(List<EpisodeFile> episodeFiles)
        {
            _mediaFileRepository.UpdateMany(episodeFiles);
        }

        public void Update(List<MovieFile> movieFiles)
        {
            _mediaFileRepository.UpdateMany(movieFiles);
        }

        public void Delete(EpisodeFile episodeFile, DeleteMediaFileReason reason)
        {
            // Little hack so we have the episodes and series attached for the event consumers
            episodeFile.Episodes.LazyLoad();
            episodeFile.Path = Path.Combine(episodeFile.Series.Value.Path, episodeFile.RelativePath);

            _mediaFileRepository.Delete(episodeFile);
            _eventAggregator.PublishEvent(new EpisodeFileDeletedEvent(episodeFile, reason));
        }

        public void Delete(MovieFile movieFile, DeleteMediaFileReason reason)
        {
            // Little hack so we have the movie attached for the event consumers
            if (movieFile.Movie == null)
            {
                movieFile.Movie = _movieRepository.Get(movieFile.MovieId);
            }

            movieFile.Path = Path.Combine(movieFile.Movie.Path, movieFile.RelativePath);

            _mediaFileRepository.Delete(movieFile);
            _eventAggregator.PublishEvent(new MovieFileDeletedEvent(movieFile, reason));
        }

        public List<EpisodeFile> GetFilesBySeries(int seriesId)
        {
            return _mediaFileRepository.GetFilesBySeries(seriesId);
        }

        public List<EpisodeFile> GetFilesBySeriesIds(List<int> seriesIds)
        {
            return _mediaFileRepository.GetFilesBySeriesIds(seriesIds);
        }

        public List<EpisodeFile> GetFilesBySeason(int seriesId, int seasonNumber)
        {
            return _mediaFileRepository.GetFilesBySeason(seriesId, seasonNumber);
        }

        public List<MovieFile> GetFilesByMovie(int movieId)
        {
            return _mediaFileRepository.GetFilesByMovie(movieId);
        }

        public List<MovieFile> GetFilesByMovies(IEnumerable<int> movieIds)
        {
            return _mediaFileRepository.GetFilesByMovies(movieIds);
        }

        public List<EpisodeFile> GetFiles(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public List<EpisodeFile> GetFilesWithoutMediaInfo()
        {
            return _mediaFileRepository.GetFilesWithoutMediaInfo();
        }

        public List<MovieFile> GetMovieFilesWithoutMediaInfo()
        {
            return _mediaFileRepository.GetMovieFilesWithoutMediaInfo();
        }

        public List<string> FilterExistingFiles(List<string> files, Series series)
        {
            var seriesFiles = GetFilesBySeries(series.Id);

            return FilterExistingFiles(files, seriesFiles, series);
        }

        public List<string> FilterExistingFiles(List<string> files, Movie movie)
        {
            var movieFiles = GetFilesByMovie(movie.Id).Select(f => Path.Combine(movie.Path, f.RelativePath)).ToList();

            if (!movieFiles.Any())
            {
                return files;
            }

            return files.Except(movieFiles, PathEqualityComparer.Instance).ToList();
        }

        public EpisodeFile Get(int id)
        {
            return _mediaFileRepository.Get(id);
        }

        public List<EpisodeFile> Get(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public MovieFile GetMovie(int id)
        {
            return _mediaFileRepository.GetMovie(id);
        }

        public List<MovieFile> GetMovies(IEnumerable<int> ids)
        {
            return _mediaFileRepository.GetMovies(ids);
        }

        public List<EpisodeFile> GetFilesWithRelativePath(int seriesId, string relativePath)
        {
            return _mediaFileRepository.GetFilesWithRelativePath(seriesId, relativePath);
        }

        public List<MovieFile> GetMovieFilesWithRelativePath(int movieId, string relativePath)
        {
            return _mediaFileRepository.GetMovieFilesWithRelativePath(movieId, relativePath);
        }

        public void HandleAsync(SeriesDeletedEvent message)
        {
            _mediaFileRepository.DeleteForSeries(message.Series.Select(s => s.Id).ToList());
        }

        public void HandleAsync(MoviesDeletedEvent message)
        {
            _mediaFileRepository.DeleteForMovies(message.Movies.Select(m => m.Id).ToList());
        }

        public static List<string> FilterExistingFiles(List<string> files, List<EpisodeFile> seriesFiles, Series series)
        {
            var seriesFilePaths = seriesFiles.Select(f => Path.Combine(series.Path, f.RelativePath)).ToList();

            if (!seriesFilePaths.Any())
            {
                return files;
            }

            return files.Except(seriesFilePaths, PathEqualityComparer.Instance).ToList();
        }

        public static List<string> FilterExistingFiles(List<string> files, List<MovieFile> movieFiles, Movie movie)
        {
            var movieFilePaths = movieFiles.Select(f => Path.Combine(movie.Path, f.RelativePath)).ToList();

            if (!movieFilePaths.Any())
            {
                return files;
            }

            return files.Except(movieFilePaths, PathEqualityComparer.Instance).ToList();
        }
    }
}

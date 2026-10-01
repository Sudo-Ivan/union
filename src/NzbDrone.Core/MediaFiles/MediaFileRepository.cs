using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileRepository : IBasicRepository<EpisodeFile>
    {
        List<EpisodeFile> GetFilesBySeries(int seriesId);
        List<EpisodeFile> GetFilesBySeriesIds(List<int> seriesIds);
        List<EpisodeFile> GetFilesBySeason(int seriesId, int seasonNumber);
        List<EpisodeFile> GetFilesWithoutMediaInfo();
        List<EpisodeFile> GetFilesWithRelativePath(int seriesId, string relativePath);
        void DeleteForSeries(List<int> seriesIds);
        MovieFile Insert(MovieFile movieFile);
        MovieFile Update(MovieFile movieFile);
        void InsertMany(IList<MovieFile> movieFiles);
        void UpdateMany(IList<MovieFile> movieFiles);
        void Delete(MovieFile movieFile);
        void DeleteMany(List<MovieFile> movieFiles);
        MovieFile GetMovie(int id);
        List<MovieFile> GetMovies(IEnumerable<int> ids);
        List<MovieFile> GetFilesByMovie(int movieId);
        List<MovieFile> GetFilesByMovies(IEnumerable<int> movieIds);
        List<MovieFile> GetMovieFilesWithoutMediaInfo();
        List<MovieFile> GetMovieFilesWithRelativePath(int movieId, string relativePath);
        void DeleteForMovies(List<int> movieIds);
    }

    public class MediaFileRepository : BasicRepository<EpisodeFile>, IMediaFileRepository
    {
        private readonly MovieFileRepository _movieFileRepository;

        public MediaFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
            _movieFileRepository = new MovieFileRepository(database, eventAggregator);
        }

        public void DeleteForSeries(List<int> seriesIds)
        {
            Delete(x => seriesIds.Contains(x.SeriesId));
        }

        public void DeleteForSeason(int seriesId, int seasonNumber)
        {
            Delete(c => c.SeriesId == seriesId && c.SeasonNumber == seasonNumber);
        }

        public List<EpisodeFile> GetFilesBySeries(int seriesId)
        {
            return Query(c => c.SeriesId == seriesId).ToList();
        }

        public List<EpisodeFile> GetFilesBySeriesIds(List<int> seriesIds)
        {
            return Query(c => seriesIds.Contains(c.SeriesId)).ToList();
        }

        public List<EpisodeFile> GetFilesBySeason(int seriesId, int seasonNumber)
        {
            return Query(c => c.SeriesId == seriesId && c.SeasonNumber == seasonNumber).ToList();
        }

        public List<EpisodeFile> GetFilesWithoutMediaInfo()
        {
            return Query(c => c.MediaInfo == null).ToList();
        }

        public List<EpisodeFile> GetFilesWithRelativePath(int seriesId, string relativePath)
        {
            return Query(c => c.SeriesId == seriesId && c.RelativePath == relativePath)
                        .ToList();
        }

        public MovieFile Insert(MovieFile movieFile)
        {
            return _movieFileRepository.Insert(movieFile);
        }

        public MovieFile Update(MovieFile movieFile)
        {
            return _movieFileRepository.Update(movieFile);
        }

        public void InsertMany(IList<MovieFile> movieFiles)
        {
            _movieFileRepository.InsertMany(movieFiles);
        }

        public void UpdateMany(IList<MovieFile> movieFiles)
        {
            _movieFileRepository.UpdateMany(movieFiles);
        }

        public void Delete(MovieFile movieFile)
        {
            _movieFileRepository.Delete(movieFile);
        }

        public void DeleteMany(List<MovieFile> movieFiles)
        {
            _movieFileRepository.DeleteMany(movieFiles);
        }

        public MovieFile GetMovie(int id)
        {
            return _movieFileRepository.Get(id);
        }

        public List<MovieFile> GetMovies(IEnumerable<int> ids)
        {
            return _movieFileRepository.Get(ids).ToList();
        }

        public List<MovieFile> GetFilesByMovie(int movieId)
        {
            return _movieFileRepository.QueryWhere(x => x.MovieId == movieId);
        }

        public List<MovieFile> GetFilesByMovies(IEnumerable<int> movieIds)
        {
            return _movieFileRepository.QueryWhere(x => movieIds.Contains(x.MovieId));
        }

        public List<MovieFile> GetMovieFilesWithoutMediaInfo()
        {
            return _movieFileRepository.QueryWhere(x => x.MediaInfo == null);
        }

        public List<MovieFile> GetMovieFilesWithRelativePath(int movieId, string relativePath)
        {
            return _movieFileRepository.QueryWhere(c => c.MovieId == movieId && c.RelativePath == relativePath);
        }

        public void DeleteForMovies(List<int> movieIds)
        {
            _movieFileRepository.DeleteWhere(x => movieIds.Contains(x.MovieId));
        }

        private class MovieFileRepository : BasicRepository<MovieFile>
        {
            public MovieFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
                : base(database, eventAggregator)
            {
            }

            public List<MovieFile> QueryWhere(Expression<Func<MovieFile, bool>> where)
            {
                return Query(where).ToList();
            }

            public void DeleteWhere(Expression<Func<MovieFile, bool>> where)
            {
                Delete(where);
            }
        }
    }
}

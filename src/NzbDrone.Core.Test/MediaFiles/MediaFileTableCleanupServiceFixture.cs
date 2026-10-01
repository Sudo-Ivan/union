using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class MediaFileTableCleanupServiceFixture : CoreTest<MediaFileTableCleanupService>
    {
        private const string DELETED_PATH = "ANY FILE WITH THIS PATH IS CONSIDERED DELETED!";
        private List<Episode> _episodes;
        private Series _series;

        [SetUp]
        public void SetUp()
        {
            _episodes = Builder<Episode>.CreateListOfSize(10)
                  .Build()
                  .ToList();

            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\TV\Series".AsOsAgnostic())
                                     .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(e => e.FileExists(It.Is<string>(c => !c.Contains(DELETED_PATH))))
                  .Returns(true);

            Mocker.GetMock<IEpisodeService>()
                  .Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                  .Returns(_episodes);
        }

        private void GivenEpisodeFiles(IEnumerable<EpisodeFile> episodeFiles)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesBySeries(It.IsAny<int>()))
                  .Returns(episodeFiles.ToList());
        }

        private void GivenFilesAreNotAttachedToEpisode()
        {
            _episodes.ForEach(e => e.EpisodeFileId = 0);

            Mocker.GetMock<IEpisodeService>()
                  .Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                  .Returns(_episodes);
        }

        private List<string> FilesOnDisk(IEnumerable<EpisodeFile> episodeFiles)
        {
            return episodeFiles.Select(e => Path.Combine(_series.Path, e.RelativePath)).ToList();
        }

        [Test]
        public void should_skip_files_that_exist_in_disk()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                .Build();

            GivenEpisodeFiles(episodeFiles);

            Subject.Clean(_series, FilesOnDisk(episodeFiles));

            Mocker.GetMock<IEpisodeService>().Verify(c => c.UpdateEpisode(It.IsAny<Episode>()), Times.Never());
        }

        [Test]
        public void should_delete_non_existent_files()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                .Random(2)
                .With(c => c.RelativePath = DELETED_PATH)
                .Build();

            GivenEpisodeFiles(episodeFiles);

            Subject.Clean(_series, FilesOnDisk(episodeFiles.Where(e => e.RelativePath != DELETED_PATH)));

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.Is<EpisodeFile>(e => e.RelativePath == DELETED_PATH), DeleteMediaFileReason.MissingFromDisk), Times.Exactly(2));
        }

        [Test]
        public void should_delete_files_that_dont_belong_to_any_episodes()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenEpisodeFiles(episodeFiles);
            GivenFilesAreNotAttachedToEpisode();

            Subject.Clean(_series, FilesOnDisk(episodeFiles));

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.IsAny<EpisodeFile>(), DeleteMediaFileReason.NoLinkedEpisodes), Times.Exactly(10));
        }

        [Test]
        public void should_unlink_episode_when_episodeFile_does_not_exist()
        {
            GivenEpisodeFiles(new List<EpisodeFile>());

            Subject.Clean(_series, new List<string>());

            Mocker.GetMock<IEpisodeService>().Verify(c => c.UpdateEpisode(It.Is<Episode>(e => e.EpisodeFileId == 0)), Times.Exactly(10));
        }

        [Test]
        public void should_not_update_episode_when_episodeFile_exists()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenEpisodeFiles(episodeFiles);

            Subject.Clean(_series, FilesOnDisk(episodeFiles));

            Mocker.GetMock<IEpisodeService>().Verify(c => c.UpdateEpisode(It.IsAny<Episode>()), Times.Never());
        }

        // Movie-domain members merged from Radarr

        private Movie _movie;

        [SetUp]
        public void SetUpMovie()
        {
            _movie = Builder<Movie>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\TV\Series".AsOsAgnostic())
                                     .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(e => e.FileExists(It.Is<string>(c => !c.Contains(DELETED_PATH))))
                  .Returns(true);
        }

        private void GivenMovieFiles(IEnumerable<MovieFile> movieFiles)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesByMovie(It.IsAny<int>()))
                  .Returns(movieFiles.ToList());
        }

        private void GivenFilesAreNotAttachedToEpisodeMovie()
        {
            _movie.MovieFileId = 0;
        }

        private List<string> FilesOnDiskMovie(IEnumerable<MovieFile> movieFiles)
        {
            return movieFiles.Select(e => Path.Combine(_movie.Path, e.RelativePath)).ToList();
        }

        [Test]
        public void should_skip_files_that_exist_in_disk_movie()
        {
            var movieFiles = Builder<MovieFile>.CreateListOfSize(10)
                .Build();

            GivenMovieFiles(movieFiles);

            Subject.Clean(_movie, FilesOnDiskMovie(movieFiles));

            Mocker.GetMock<IMovieService>().Verify(c => c.UpdateMovie(It.IsAny<Movie>()), Times.Never());
        }

        [Test]
        public void should_delete_non_existent_files_movie()
        {
            var movieFiles = Builder<MovieFile>.CreateListOfSize(10)
                .Random(2)
                .With(c => c.RelativePath = DELETED_PATH)
                .Build();

            GivenMovieFiles(movieFiles);

            Subject.Clean(_movie, FilesOnDiskMovie(movieFiles.Where(e => e.RelativePath != DELETED_PATH)));

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.Is<MovieFile>(e => e.RelativePath == DELETED_PATH), DeleteMediaFileReason.MissingFromDisk), Times.Exactly(2));
        }

        [Test]
        [Ignore("idc")]
        public void should_delete_files_that_dont_belong_to_any_episodes_movie()
        {
            var movieFiles = Builder<MovieFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenMovieFiles(movieFiles);
            GivenFilesAreNotAttachedToEpisodeMovie();

            Subject.Clean(_movie, FilesOnDiskMovie(movieFiles));

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.IsAny<MovieFile>(), DeleteMediaFileReason.NoLinkedEpisodes), Times.Exactly(10));
        }

        [Test]
        [Ignore("Idc")]
        public void should_unlink_episode_when_episodeFile_does_not_exist_movie()
        {
            GivenMovieFiles(new List<MovieFile>());

            Subject.Clean(_movie, new List<string>());

            Mocker.GetMock<IMovieService>().Verify(c => c.UpdateMovie(It.Is<Movie>(e => e.MovieFileId == 0)), Times.Exactly(10));
        }

        [Test]
        public void should_not_update_episode_when_episodeFile_exists_movie()
        {
            var movieFiles = Builder<MovieFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenMovieFiles(movieFiles);

            Subject.Clean(_movie, FilesOnDiskMovie(movieFiles));

            Mocker.GetMock<IMovieService>().Verify(c => c.UpdateMovie(It.IsAny<Movie>()), Times.Never());
        }
}
}

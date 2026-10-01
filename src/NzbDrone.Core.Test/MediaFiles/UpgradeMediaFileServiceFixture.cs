using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;
using EpisodeRootFolderNotFoundException = NzbDrone.Core.MediaFiles.EpisodeImport.RootFolderNotFoundException;
using MovieRootFolderNotFoundException = NzbDrone.Core.MediaFiles.MovieImport.RootFolderNotFoundException;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class UpgradeMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private EpisodeFile _episodeFile;
        private LocalEpisode _localEpisode;

        [SetUp]
        public void Setup()
        {
            _localEpisode = new LocalEpisode();
            _localEpisode.Series = new Series
                                   {
                                       Path = @"C:\Test\TV\Series".AsOsAgnostic()
                                   };

            _episodeFile = Builder<EpisodeFile>
                .CreateNew()
                .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FolderExists(Directory.GetParent(_localEpisode.Series.Path).FullName))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                  .Returns<string>(c => Path.GetDirectoryName(c));
        }

        private void GivenSingleEpisodeWithSingleEpisodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new EpisodeFile
                                                                                {
                                                                                    Id = 1,
                                                                                    RelativePath = @"Season 01\30.rock.s01e01.avi",
                                                                                })
                                                     .Build()
                                                     .ToList();
        }

        private void GivenMultipleEpisodesWithSingleEpisodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new EpisodeFile
                                                                                {
                                                                                    Id = 1,
                                                                                    RelativePath = @"Season 01\30.rock.s01e01.avi",
                                                                                })
                                                     .Build()
                                                     .ToList();
        }

        private void GivenMultipleEpisodesWithMultipleEpisodeFiles()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .TheFirst(1)
                                                     .With(e => e.EpisodeFile = new EpisodeFile
                                                                                {
                                                                                    Id = 1,
                                                                                    RelativePath = @"Season 01\30.rock.s01e01.avi",
                                                                                })
                                                     .TheNext(1)
                                                     .With(e => e.EpisodeFile = new EpisodeFile
                                                                                {
                                                                                    Id = 2,
                                                                                    RelativePath = @"Season 01\30.rock.s01e02.avi",
                                                                                })
                                                     .Build()
                                                     .ToList();
        }

        [Test]
        public void should_delete_single_episode_file_once()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_the_same_episode_file_only_once()
        {
            GivenMultipleEpisodesWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_multiple_different_episode_files()
        {
            GivenMultipleEpisodesWithMultipleEpisodeFiles();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void should_delete_episode_file_from_database()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<EpisodeFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localEpisode.Episodes.Single().EpisodeFile, DeleteMediaFileReason.Upgrade), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_old_episode_file_in_oldFiles()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode).OldFiles.Count.Should().Be(1);
        }

        [Test]
        public void should_return_old_episode_files_in_oldFiles()
        {
            GivenMultipleEpisodesWithMultipleEpisodeFiles();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode).OldFiles.Count.Should().Be(2);
        }

        [Test]
        public void should_throw_if_there_are_existing_episode_files_and_the_root_folder_is_missing()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FolderExists(Directory.GetParent(_localEpisode.Series.Path).FullName))
                  .Returns(false);

            Assert.Throws<EpisodeRootFolderNotFoundException>(() => Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode));

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localEpisode.Episodes.Single().EpisodeFile, DeleteMediaFileReason.Upgrade), Times.Never());
        }

        [Test]
        public void should_import_if_existing_file_doesnt_exist_in_db()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(null))
                                                     .Build()
                                                     .ToList();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localEpisode.Episodes.Single().EpisodeFile, It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        // Movie-domain members merged from Radarr

        private MovieFile _movieFile;

        private LocalMovie _localMovie;

        [SetUp]
        public void SetupMovie()
        {
            _localMovie = new LocalMovie();
            _localMovie.Movie = new Movie
            {
                Path = @"C:\Test\Movies\Movie".AsOsAgnostic()
            };

            _movieFile = Builder<MovieFile>
                  .CreateNew()
                  .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FolderExists(Directory.GetParent(_localMovie.Movie.Path).FullName))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                  .Returns<string>(c => Path.GetDirectoryName(c));
        }

        private void GivenSingleMovieWithSingleMovieFile()
        {
            _localMovie.Movie.MovieFileId = 1;
            _localMovie.Movie.MovieFile =
                new MovieFile
                {
                    Id = 1,
                    RelativePath = @"A.Movie.2019.avi",
                };
        }

        [Test]
        public void should_delete_single_movie_file_once()
        {
            GivenSingleMovieWithSingleMovieFile();

            Subject.UpgradeMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_movie_file_from_database()
        {
            GivenSingleMovieWithSingleMovieFile();

            Subject.UpgradeMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<MovieFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist_movie()
        {
            GivenSingleMovieWithSingleMovieFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localMovie.Movie.MovieFile, DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist_movie()
        {
            GivenSingleMovieWithSingleMovieFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_old_movie_file_in_oldFiles()
        {
            GivenSingleMovieWithSingleMovieFile();

            Subject.UpgradeMovieFile(_movieFile, _localMovie).OldFiles.Count.Should().Be(1);
        }

        [Test]
        public void should_throw_if_there_are_existing_movie_files_and_the_root_folder_is_missing()
        {
            GivenSingleMovieWithSingleMovieFile();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FolderExists(Directory.GetParent(_localMovie.Movie.Path).FullName))
                  .Returns(false);

            Assert.Throws<MovieRootFolderNotFoundException>(() => Subject.UpgradeMovieFile(_movieFile, _localMovie));

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localMovie.Movie.MovieFile, DeleteMediaFileReason.Upgrade), Times.Never());
        }
}
}

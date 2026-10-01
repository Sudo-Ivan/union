using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class OnDownloadFixture : CoreTest<Notifications.Xbmc.Xbmc>
    {
        private DownloadMessage _downloadMessage;

        [SetUp]
        public void Setup()
        {
            var series = Builder<Series>.CreateNew()
                                        .Build();

            var episodeFile = Builder<EpisodeFile>.CreateNew()
                                                   .Build();

            _downloadMessage = Builder<DownloadMessage>.CreateNew()
                                                       .With(d => d.Series = series)
                                                       .With(d => d.EpisodeFile = episodeFile)
                                                       .With(d => d.OldFiles = new List<DeletedEpisodeFile>())
                                                       .Build();

            Subject.Definition = new NotificationDefinition();
            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true
                                          };
        }

        private void GivenOldFiles()
        {
            _downloadMessage.OldFiles = Builder<DeletedEpisodeFile>
                .CreateListOfSize(1)
                .All()
                .WithFactory(() => new DeletedEpisodeFile(Builder<EpisodeFile>.CreateNew().Build(), null))
                .Build()
                .ToList();

            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true,
                                              CleanLibrary = true
                                          };
        }

        [Test]
        public void should_not_clean_if_no_episode_was_replaced()
        {
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Never());
        }

        [Test]
        public void should_clean_if_episode_was_replaced()
        {
            GivenOldFiles();
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Once());
        }

        // Movie-domain members merged from Radarr

        [SetUp]
        public void SetupMovie()
        {
            var movie = Builder<Movie>.CreateNew()
                                        .Build();

            var movieFile = Builder<MovieFile>.CreateNew()
                                                   .Build();

            _downloadMessage = Builder<DownloadMessage>.CreateNew()
                                                       .With(d => d.Movie = movie)
                                                       .With(d => d.MovieFile = movieFile)
                                                       .With(d => d.OldMovieFiles = new List<DeletedMovieFile>())
                                                       .Build();

            Subject.Definition = new NotificationDefinition();
            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true
                                          };
        }

        private void GivenOldFilesMovie()
        {
            _downloadMessage.OldMovieFiles = Builder<DeletedMovieFile>
                .CreateListOfSize(1)
                .All()
                .WithFactory(() => new DeletedMovieFile(Builder<MovieFile>.CreateNew().Build(), null))
                .Build()
                .ToList();

            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true,
                                              CleanLibrary = true
                                          };
        }

        [Test]
        public void should_not_clean_if_no_movie_was_replaced()
        {
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Never());
        }

        [Test]
        public void should_clean_if_movie_was_replaced()
        {
            GivenOldFilesMovie();
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Once());
        }
}
}

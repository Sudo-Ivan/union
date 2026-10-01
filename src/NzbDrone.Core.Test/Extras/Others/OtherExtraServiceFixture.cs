using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Others
{
    [TestFixture]
    public class OtherExtraServiceFixture : CoreTest<OtherExtraService>
    {
        private Series _series;
        private EpisodeFile _episodeFile;
        private LocalEpisode _localEpisode;

        private string _seriesFolder;
        private string _episodeFolder;

        [SetUp]
        public void Setup()
        {
            _seriesFolder = @"C:\Test\TV\Series Title".AsOsAgnostic();
            _episodeFolder = @"C:\Test\Unsorted TV\Series.Title.S01".AsOsAgnostic();

            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Path = _seriesFolder)
                                     .Build();

            var episodes = Builder<Episode>.CreateListOfSize(1)
                                           .All()
                                           .With(e => e.SeasonNumber = 1)
                                           .Build()
                                           .ToList();

            _episodeFile = Builder<EpisodeFile>.CreateNew()
                                               .With(f => f.Path = Path.Combine(_series.Path, "Season 1", "Series Title - S01E01.mkv").AsOsAgnostic())
                                               .With(f => f.RelativePath = @"Season 1\Series Title - S01E01.mkv")
                                               .Build();

            _localEpisode = Builder<LocalEpisode>.CreateNew()
                                                 .With(l => l.Series = _series)
                                                 .With(l => l.Episodes = episodes)
                                                 .With(l => l.Path = Path.Combine(_episodeFolder, "Series.Title.S01E01.mkv").AsOsAgnostic())
                                                 .With(l => l.FileEpisodeInfo = new ParsedEpisodeInfo
                                                 {
                                                     SeasonNumber = 1,
                                                     EpisodeNumbers = new[] { 1 }
                                                 })
                                                 .Build();
        }

        [Test]
        [TestCase("Series Title - S01E01.nfo", "Series Title - S01E01.nfo")]
        [TestCase("Series.Title.S01E01.nfo", "Series Title - S01E01.nfo")]
        [TestCase("Series-Title-S01E01.nfo", "Series Title - S01E01.nfo")]
        [TestCase("Series Title S01E01.nfo", "Series Title - S01E01.nfo")]
        [TestCase("Series_Title_S01E01.nfo", "Series Title - S01E01.nfo")]
        [TestCase("S01E01.thumb.jpg", "Series Title - S01E01.jpg")]
        [TestCase(@"Series.Title.S01E01\thumb.jpg", "Series Title - S01E01.jpg")]
        public void should_import_matching_file(string filePath, string expectedOutputPath)
        {
            var files = new List<string> { Path.Combine(_episodeFolder, filePath).AsOsAgnostic() };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(1);

            results[0].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", expectedOutputPath).AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_not_import_multiple_nfo_files()
        {
            var files = new List<string>
            {
                Path.Combine(_episodeFolder, "Series.Title.S01E01.nfo").AsOsAgnostic(),
                Path.Combine(_episodeFolder, "Series_Title_S01E01.nfo").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(1);
        }

        [Test]
        [TestCase(@"audio_folder_1\Series Title S01E01.mka", @"audio_folder_2\Series Title S01E01.mka", "Series Title - S01E01.1.mka", "Series Title - S01E01.2.mka")]
        public void should_import_all_files_with_same_name(string firstExtraFilePath, string secondExtraFilePath, string firstOutputPath, string secondOutputPath)
        {
            var files = new List<string>
            {
                Path.Combine(_episodeFolder, firstExtraFilePath).AsOsAgnostic(),
                Path.Combine(_episodeFolder, secondExtraFilePath).AsOsAgnostic()
            };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(2);

            results[0].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", firstOutputPath).AsOsAgnostic()).Should().Be(true);
            results[1].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", secondOutputPath).AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_increment_suffix_for_each_duplicate_file()
        {
            var files = new List<string>
            {
                Path.Combine(_episodeFolder, @"audio_folder_1\Series Title S01E01.mka").AsOsAgnostic(),
                Path.Combine(_episodeFolder, @"audio_folder_2\Series Title S01E01.mka").AsOsAgnostic(),
                Path.Combine(_episodeFolder, @"audio_folder_3\Series Title S01E01.mka").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(3);
            results[0].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.1.mka").AsOsAgnostic()).Should().Be(true);
            results[1].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.2.mka").AsOsAgnostic()).Should().Be(true);
            results[2].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.3.mka").AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_suffix_files_matched_by_filename_prefix()
        {
            var files = new List<string>
            {
                Path.Combine(_episodeFolder, "Series.Title.S01E01.behind_scenes.mka").AsOsAgnostic(),
                Path.Combine(_episodeFolder, "Series.Title.S01E01.commentary.mka").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(2);
            results[0].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.1.mka").AsOsAgnostic()).Should().Be(true);
            results[1].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.2.mka").AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_suffix_files_matched_by_both_filename_and_episode_info()
        {
            var files = new List<string>
            {
                Path.Combine(_episodeFolder, "Series.Title.S01E01.behind_scenes.mka").AsOsAgnostic(),
                Path.Combine(_episodeFolder, @"extras\S01E01.mka").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(2);
            results[0].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.1.mka").AsOsAgnostic()).Should().Be(true);
            results[1].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.2.mka").AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_not_suffix_when_other_files_do_not_match_episode()
        {
            var files = new List<string>
            {
                Path.Combine(_episodeFolder, "Series.Title.S01E01.mka").AsOsAgnostic(),
                Path.Combine(_episodeFolder, "Series.Title.S01E02.mka").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localEpisode, _episodeFile, files, true).ToList();

            results.Count.Should().Be(1);
            results[0].RelativePath.AsOsAgnostic().PathEquals(Path.Combine("Season 1", "Series Title - S01E01.mka").AsOsAgnostic()).Should().Be(true);
        }

        // Movie-domain members merged from Radarr

        private Movie _movie;

        private MovieFile _movieFile;

        private LocalMovie _localMovie;

        private string _movieFolder;

        private string _releaseFolder;

        [SetUp]
        public void SetupMovie()
        {
            _movieFolder = @"C:\Test\Movies\Movie Title".AsOsAgnostic();
            _releaseFolder = @"C:\Test\Unsorted Movies\Movie.Title.2022".AsOsAgnostic();

            _movie = Builder<Movie>.CreateNew()
                                     .With(s => s.Path = _movieFolder)
                                     .Build();

            _movieFile = Builder<MovieFile>.CreateNew()
                                               .With(f => f.Path = Path.Combine(_movie.Path, "Movie Title - 2022.mkv").AsOsAgnostic())
                                               .With(f => f.RelativePath = @"Movie Title - 2022.mkv")
                                               .Build();

            _localMovie = Builder<LocalMovie>.CreateNew()
                                                 .With(l => l.Movie = _movie)
                                                 .With(l => l.Path = Path.Combine(_releaseFolder, "Movie.Title.2022.mkv").AsOsAgnostic())
                                                 .With(l => l.FileMovieInfo = new ParsedMovieInfo
                                                 {
                                                     MovieTitles = new List<string> { "Movie Title" },
                                                     Year = 2022
                                                 })
                                                 .Build();
        }

        [Test]
        [TestCase("Movie Title - 2022.nfo", "Movie Title - 2022.nfo")]
        [TestCase("Movie.Title.2022.nfo", "Movie Title - 2022.nfo")]
        [TestCase("Movie Title 2022.nfo", "Movie Title - 2022.nfo")]
        [TestCase("Movie_Title_2022.nfo", "Movie Title - 2022.nfo")]
        [TestCase(@"Movie.Title.2022\thumb.jpg", "Movie Title - 2022.jpg")]
        public void should_import_matching_file_movie(string filePath, string expectedOutputPath)
        {
            var files = new List<string> { Path.Combine(_releaseFolder, filePath).AsOsAgnostic() };

            var results = Subject.ImportFiles(_localMovie, _movieFile, files, true).ToList();

            results.Count.Should().Be(1);

            results[0].RelativePath.AsOsAgnostic().PathEquals(expectedOutputPath.AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_not_import_multiple_nfo_files_movie()
        {
            var files = new List<string>
            {
                Path.Combine(_releaseFolder, "Movie.Title.2022.nfo").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Movie_Title_2022.nfo").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localMovie, _movieFile, files, true).ToList();

            results.Count.Should().Be(1);
        }

}
}

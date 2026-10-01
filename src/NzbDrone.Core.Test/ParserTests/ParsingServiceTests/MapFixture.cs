using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.AlternativeTitles;
using NzbDrone.Core.Movies.Translations;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class MapFixture : TestBase<ParsingService>
    {
        private Series _series;
        private List<Episode> _episodes;
        private ParsedEpisodeInfo _parsedEpisodeInfo;
        private SingleEpisodeSearchCriteria _singleEpisodeSearchCriteria;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                .With(s => s.Title = "30 Stone")
                .With(s => s.CleanTitle = "stone")
                .Build();

            _episodes = Builder<Episode>.CreateListOfSize(1)
                                        .All()
                                        .With(e => e.AirDate = DateTime.Today.ToString(Episode.AIR_DATE_FORMAT))
                                        .Build()
                                        .ToList();

            _parsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeriesTitle = _series.Title,
                SeriesTitleInfo = new SeriesTitleInfo(),
                SeasonNumber = 1,
                EpisodeNumbers = new[] { 1 },
                Languages = new List<Language> { Language.English }
            };

            _singleEpisodeSearchCriteria = new SingleEpisodeSearchCriteria
            {
                Series = _series,
                EpisodeNumber = _episodes.First().EpisodeNumber,
                SeasonNumber = _episodes.First().SeasonNumber,
                Episodes = _episodes
            };
        }

        private void GivenMatchBySeriesTitle()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByTitle(It.IsAny<string>()))
                  .Returns(_series);
        }

        private void GivenMatchByTvdbId()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByTvdbId(It.IsAny<int>()))
                  .Returns(_series);
        }

        private void GivenMatchByTvRageId()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByTvRageId(It.IsAny<int>()))
                  .Returns(_series);
        }

        private void GivenParseResultSeriesDoesntMatchSearchCriteria()
        {
            _parsedEpisodeInfo.SeriesTitle = "Another Name";
        }

        [Test]
        public void should_lookup_series_by_name()
        {
            GivenMatchBySeriesTitle();

            Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_use_tvdbid_when_series_title_lookup_fails()
        {
            GivenMatchByTvdbId();

            Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTvdbId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_use_tvrageid_when_series_title_lookup_fails()
        {
            GivenMatchByTvRageId();

            Subject.Map(_parsedEpisodeInfo, 0, _series.TvRageId, null);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTvRageId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_not_use_tvrageid_when_scene_naming_exception_exists()
        {
            GivenMatchByTvRageId();

            Mocker.GetMock<ISceneMappingService>()
                  .Setup(v => v.FindSceneMapping(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Returns(new SceneMapping { TvdbId = 10 });

            var result = Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTvRageId(It.IsAny<int>()), Times.Never());

            result.Series.Should().BeNull();
        }

        [Test]
        public void should_use_search_criteria_series_title()
        {
            GivenMatchBySeriesTitle();

            Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_FindByTitle_when_search_criteria_matching_fails()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, 10, 10, null, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_FindByTitle_using_year_when_FindByTitle_matching_fails()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            _parsedEpisodeInfo.SeriesTitleInfo = new SeriesTitleInfo
            {
                Title = "Series Title 2017",
                TitleWithoutYear = "Series Title",
                Year = 2017
            };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByTitle(_parsedEpisodeInfo.SeriesTitleInfo.TitleWithoutYear, _parsedEpisodeInfo.SeriesTitleInfo.Year))
                  .Returns(_series);

            Subject.Map(_parsedEpisodeInfo, 10, 10, null, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>(), It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_FindByTvdbId_when_search_criteria_and_FindByTitle_matching_fails()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, 10, 10, null, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTvdbId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_FindByTvRageId_when_search_criteria_and_FindByTitle_matching_fails()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, 0, 10, null, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTvRageId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_not_FindByTvRageId_when_search_criteria_and_FindByTitle_matching_fails_and_tvdb_id_is_specified()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, 10, 10, null, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.FindByTvRageId(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_FindByImdbId_when_search_criteria_and_FindByTitle_matching_fails()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, 0, 0, "tt12345", _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.FindByImdbId(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_FindByImdbId_when_search_criteria_and_FindByTitle_matching_fails_and_tvdb_id_is_specified()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, 10, 10, "tt12345", _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.FindByImdbId(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_use_tvdbid_matching_when_alias_is_found()
        {
            Mocker.GetMock<ISceneMappingService>()
                  .Setup(s => s.FindTvdbId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Returns(_series.TvdbId);

            Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_use_tvrageid_match_from_search_criteria_when_title_match_fails()
        {
            GivenParseResultSeriesDoesntMatchSearchCriteria();

            Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId, _singleEpisodeSearchCriteria);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_use_scene_season_number_from_xem_mapping_if_alias_matches_a_specific_season_number()
        {
            _parsedEpisodeInfo.SeasonNumber = 1;

            var sceneMapping = new SceneMapping
            {
                Type = "XemService",
                SceneSeasonNumber = 2
            };

            Mocker.GetMock<ISceneMappingService>()
                .Setup(s => s.FindSceneMapping(_parsedEpisodeInfo.SeriesTitle, _parsedEpisodeInfo.ReleaseTitle, _parsedEpisodeInfo.SeasonNumber.Value))
                .Returns(sceneMapping);

            var result = Subject.Map(_parsedEpisodeInfo, _series);

            result.MappedSeasonNumber.Should().Be(sceneMapping.SceneSeasonNumber);
        }

        [Test]
        public void should_not_use_scene_season_number_from_xem_mapping_if_alias_matches_a_specific_season_number_but_did_not_parse_season_1()
        {
            _parsedEpisodeInfo.SeasonNumber = 2;

            var sceneMapping = new SceneMapping
            {
                Type = "XemService",
                SceneSeasonNumber = 2
            };

            Mocker.GetMock<ISceneMappingService>()
                .Setup(s => s.FindSceneMapping(_parsedEpisodeInfo.SeriesTitle, _parsedEpisodeInfo.ReleaseTitle, _parsedEpisodeInfo.SeasonNumber.Value))
                .Returns(sceneMapping);

            var result = Subject.Map(_parsedEpisodeInfo, _series);

            result.MappedSeasonNumber.Should().Be(sceneMapping.SceneSeasonNumber);
        }

        [Test]
        public void should_use_tvdbid_matching_when_alias_without_year_is_found()
        {
            var alias = "Series Alias";

            _parsedEpisodeInfo.SeriesTitle = $"{alias} {_series.Year}";
            _parsedEpisodeInfo.SeriesTitleInfo.TitleWithoutYear = alias;
            _parsedEpisodeInfo.SeriesTitleInfo.Year = _series.Year;

            Mocker.GetMock<ISceneMappingService>()
                .Setup(s => s.FindTvdbId(alias, It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_series.TvdbId);

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.FindByTvdbId(_series.Id))
                .Returns(_series);

            var result = Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId, null);

            result.Series.Should().Be(_series);

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.FindByTvdbId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_not_use_tvdbid_matching_when_alias_without_year_is_found_with_wrong_year()
        {
            var alias = "Series Alias";

            _parsedEpisodeInfo.SeriesTitle = $"{alias} {_series.Year}";
            _parsedEpisodeInfo.SeriesTitleInfo.TitleWithoutYear = alias;
            _parsedEpisodeInfo.SeriesTitleInfo.Year = _series.Year + 1;

            Mocker.GetMock<ISceneMappingService>()
                .Setup(s => s.FindTvdbId(alias, It.IsAny<string>(), It.IsAny<int>()))
                .Returns(_series.TvdbId);

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.FindByTvdbId(_series.Id))
                .Returns(_series);

            var result = Subject.Map(_parsedEpisodeInfo, 0, 0, "", null);

            result.Series.Should().BeNull();

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.FindByTvdbId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_use_year_when_looking_up_by_all_titles_in_release_title()
        {
            var alias = "Series Alias";
            var title = "Series Title";

            _parsedEpisodeInfo.SeriesTitle = $"Series Title AKA Series Alias {_series.Year}";
            _parsedEpisodeInfo.SeriesTitleInfo.AllTitles = [
                title,
                alias
            ];
            _parsedEpisodeInfo.SeriesTitleInfo.Year = _series.Year;

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.FindByTitle(title, _series.Year))
                .Returns(_series);

            var result = Subject.Map(_parsedEpisodeInfo, 0, 0, "", null);

            result.Series.Should().Be(_series);
        }

        [Test]
        public void should_use_title_with_year_when_looking_up_by_all_titles_in_release_title()
        {
            var alias = "Series Alias";
            var title = "Series Title";

            _parsedEpisodeInfo.SeriesTitle = $"Series Title AKA Series Alias {_series.Year}";
            _parsedEpisodeInfo.SeriesTitleInfo.AllTitles = [
                title,
                alias
            ];
            _parsedEpisodeInfo.SeriesTitleInfo.Year = _series.Year;

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.FindByTitle($"{title} {_series.Year}"))
                .Returns(_series);

            var result = Subject.Map(_parsedEpisodeInfo, 0, 0, "", null);

            result.Series.Should().Be(_series);
        }

        [Test]
        public void should_not_have_a_mapped_season_number_when_the_scene_mapping_has_no_season()
        {
            GivenMatchBySeriesTitle();

            _parsedEpisodeInfo.SeasonNumber = null;
            _parsedEpisodeInfo.IsSeasonTitle = true;
            _parsedEpisodeInfo.EpisodeNumbers = [];

            Mocker.GetMock<ISceneMappingService>()
                  .Setup(v => v.FindSceneMapping(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Returns(new SceneMapping { TvdbId = _series.TvdbId });

            var result = Subject.Map(_parsedEpisodeInfo, _series.TvdbId, _series.TvRageId, _series.ImdbId);

            result.MappedSeasonNumber.Should().BeNull();
        }

        // Movie-domain members merged from Radarr

        private Movie _movie;

        private ParsedMovieInfo _parsedMovieInfo;

        private ParsedMovieInfo _wrongYearInfo;

        private ParsedMovieInfo _wrongTitleInfo;

        private ParsedMovieInfo _romanTitleInfo;

        private ParsedMovieInfo _alternativeTitleInfo;

        private ParsedMovieInfo _translationTitleInfo;

        private ParsedMovieInfo _umlautInfo;

        private ParsedMovieInfo _umlautAltInfo;

        private ParsedMovieInfo _multiLanguageInfo;

        private ParsedMovieInfo _multiLanguageWithOriginalInfo;

        private MovieSearchCriteria _movieSearchCriteria;

        [SetUp]
        public void SetupMovie()
        {
            _movie = Builder<Movie>.CreateNew()
                                   .With(m => m.Title = "Fack Ju Göthe 2")
                                   .With(m => m.MovieMetadata.Value.CleanTitle = "fackjugoethe2")
                                   .With(m => m.Year = 2015)
                                   .With(m => m.MovieMetadata.Value.AlternativeTitles = new List<AlternativeTitle> { new AlternativeTitle("Fack Ju Göthe 2: Same same") })
                                   .With(m => m.MovieMetadata.Value.Translations = new List<MovieTranslation> { new MovieTranslation { Title = "Translated Title", CleanTitle = "translatedtitle" } })
                                   .With(m => m.MovieMetadata.Value.OriginalLanguage = Language.English)
                                   .Build();

            _parsedMovieInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { _movie.Title },
                Languages = new List<Language> { Language.English },
                Year = _movie.Year,
            };

            _wrongYearInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { _movie.Title },
                Languages = new List<Language> { Language.English },
                Year = 1900,
            };

            _wrongTitleInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { "Other Title" },
                Languages = new List<Language> { Language.English },
                Year = 2015
            };

            _alternativeTitleInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { _movie.MovieMetadata.Value.AlternativeTitles.First().Title },
                Languages = new List<Language> { Language.English },
                Year = _movie.Year,
            };

            _translationTitleInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { _movie.MovieMetadata.Value.Translations.First().Title },
                Languages = new List<Language> { Language.English },
                Year = _movie.Year,
            };

            _romanTitleInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { "Fack Ju Göthe II" },
                Languages = new List<Language> { Language.English },
                Year = _movie.Year,
            };

            _umlautInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { "Fack Ju Goethe 2" },
                Languages = new List<Language> { Language.English },
                Year = _movie.Year
            };

            _umlautAltInfo = new ParsedMovieInfo
            {
                MovieTitles = new List<string> { "Fack Ju Goethe 2: Same same" },
                Languages = new List<Language> { Language.English },
                Year = _movie.Year
            };

            _multiLanguageInfo = new ParsedMovieInfo
            {
                MovieTitles = { _movie.Title },
                Languages = new List<Language> { Language.Original, Language.French }
            };

            _multiLanguageWithOriginalInfo = new ParsedMovieInfo
            {
                MovieTitles = { _movie.Title },
                Languages = new List<Language> { Language.Original, Language.French, Language.English }
            };

            _movieSearchCriteria = new MovieSearchCriteria
            {
                Movie = _movie
            };
        }

        private void GivenMatchByMovieTitle()
        {
            Mocker.GetMock<IMovieService>()
                  .Setup(s => s.FindByTitle(It.IsAny<string>()))
                  .Returns(_movie);
        }

        [Test]
        public void should_lookup_Movie_by_name()
        {
            GivenMatchByMovieTitle();

            Subject.Map(_parsedMovieInfo, "", 0, null);

            Mocker.GetMock<IMovieService>()
                .Verify(v => v.FindByTitle(It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<List<string>>(), null), Times.Once());
        }

        [Test]
        public void should_use_search_criteria_movie_title()
        {
            GivenMatchByMovieTitle();

            Subject.Map(_parsedMovieInfo, "", 0, _movieSearchCriteria);

            Mocker.GetMock<IMovieService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_match_alternative_title()
        {
            Subject.Map(_alternativeTitleInfo, "", 0, _movieSearchCriteria).Movie.Should().Be(_movieSearchCriteria.Movie);
        }

        [Test]
        public void should_match_translation_title()
        {
            Subject.Map(_translationTitleInfo, "", 0, _movieSearchCriteria).Movie.Should().Be(_movieSearchCriteria.Movie);
        }

        [Test]
        public void should_match_roman_title()
        {
            Subject.Map(_romanTitleInfo, "", 0, _movieSearchCriteria).Movie.Should().Be(_movieSearchCriteria.Movie);
        }

        [Test]
        public void should_match_umlauts()
        {
            Subject.Map(_umlautInfo, "", 0, _movieSearchCriteria).Movie.Should().Be(_movieSearchCriteria.Movie);
            Subject.Map(_umlautAltInfo, "", 0, _movieSearchCriteria).Movie.Should().Be(_movieSearchCriteria.Movie);
        }

}
}

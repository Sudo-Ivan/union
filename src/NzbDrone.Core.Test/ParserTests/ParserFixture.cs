using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ParserFixture : CoreTest
    {
        [TestCase("Series Title - 4x05 - Title", "seriestitle")]
        [TestCase("Series & Title - 4x05 - Title", "seriestitle")]
        [TestCase("Bad Format", "badformat")]
        [TestCase("Mad Series - Season 1 [Bluray720p]", "madseries")]
        [TestCase("Mad Series - Season 1 [Bluray1080p]", "madseries")]
        [TestCase("The Daily Series -", "thedailyseries")]
        [TestCase("The Series Bros. (2006)", "theseriesbros2006")]
        [TestCase("Series (2011)", "series2011")]
        [TestCase("Series Time S02 720p HDTV x264 CRON", "seriestime")]
        [TestCase("Series Title 0", "seriestitle0")]
        [TestCase("Series of the Day", "seriesday")]
        [TestCase("Series of the Day 2", "seriesday2")]
        [TestCase("[ www.Torrenting.com ] - Series.S03E14.720p.HDTV.X264-DIMENSION", "series")]
        [TestCase("www.Torrenting.com - Series.S03E14.720p.HDTV.X264-DIMENSION", "series")]
        [TestCase("Series S02E09 HDTV x264-2HD [eztv]-[rarbg.com]", "series")]
        [TestCase("Series.911.S01.DVDRip.DD2.0.x264-DEEP", "series 911")]
        [TestCase("www.Torrenting.org - Series.S03E14.720p.HDTV.X264-DIMENSION", "series")]
        public void should_parse_series_name(string postTitle, string title)
        {
            var result = Parser.Parser.ParseSeriesName(postTitle).CleanSeriesTitle();
            result.Should().Be(title.CleanSeriesTitle());
        }

        [TestCase("Series S03E14 720p HDTV X264-DIMENSION", "Series")]
        [TestCase("Series.S03E14.720p.HDTV.X264-DIMENSION", "Series")]
        [TestCase("Series-S03E14-720p-HDTV-X264-DIMENSION", "Series")]
        [TestCase("Series_S03E14_720p_HDTV_X264-DIMENSION", "Series")]
        [TestCase("Series 2022 S03E14 720p HDTV X264-DIMENSION", "Series", 2022)]
        [TestCase("Series (2022) S03E14 720p HDTV X264-DIMENSION", "Series", 2022)]
        [TestCase("Series.2022.S03E14.720p.HDTV.X264-DIMENSION", "Series", 2022)]
        [TestCase("Series-2022-S03E14-720p-HDTV-X264-DIMENSION", "Series", 2022)]
        [TestCase("Series_2022_S03E14_720p_HDTV_X264-DIMENSION", "Series", 2022)]
        [TestCase("1234 S03E14 720p HDTV X264-DIMENSION", "1234")]
        [TestCase("1234.S03E14.720p.HDTV.X264-DIMENSION", "1234")]
        [TestCase("1234-S03E14-720p-HDTV-X264-DIMENSION", "1234")]
        [TestCase("1234_S03E14_720p_HDTV_X264-DIMENSION", "1234")]
        [TestCase("1234 2022 S03E14 720p HDTV X264-DIMENSION", "1234", 2022)]
        [TestCase("1234 (2022) S03E14 720p HDTV X264-DIMENSION", "1234", 2022)]
        [TestCase("1234.2022.S03E14.720p.HDTV.X264-DIMENSION", "1234", 2022)]
        [TestCase("1234-2022-S03E14-720p-HDTV-X264-DIMENSION", "1234", 2022)]
        [TestCase("1234_2022_S03E14_720p_HDTV_X264-DIMENSION", "1234", 2022)]
        public void should_parse_series_title_info(string postTitle, string titleWithoutYear, int year = 0)
        {
            var seriesTitleInfo = Parser.Parser.ParseTitle(postTitle).SeriesTitleInfo;
            seriesTitleInfo.TitleWithoutYear.Should().Be(titleWithoutYear);
            seriesTitleInfo.Year.Should().Be(year);
        }

        [Test]
        public void should_remove_accents_from_title()
        {
            const string title = "Seri\u00E0es";

            title.CleanSeriesTitle().Should().Be("seriaes");
        }

        [TestCase("Sonar TV - Series Title : 02 Road From Code [S04].mp4")]
        public void should_clean_up_invalid_path_characters(string postTitle)
        {
            Parser.Parser.ParseTitle(postTitle);
        }

        [TestCase("[scnzbefnet][509103] 2.Developers.Series.S03E18.720p.HDTV.X264-DIMENSION", "2 Developers Series")]
        public void should_remove_request_info_from_title(string postTitle, string title)
        {
            Parser.Parser.ParseTitle(postTitle).SeriesTitle.Should().Be(title);
        }

        [TestCase("Series.S01E02.Chained.Title.mkv")]
        [TestCase("Show - S01E01 - Title.avi")]
        public void should_parse_quality_from_extension(string title)
        {
            Parser.Parser.ParseTitle(title).Quality.Quality.Should().NotBe(Quality.Unknown);
            Parser.Parser.ParseTitle(title).Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Extension);
            Parser.Parser.ParseTitle(title).Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.Extension);
        }

        [TestCase("Series.S01E02.Chained.Title.mkv", "Series.S01E02.Chained.Title")]
        public void should_parse_releasetitle(string path, string releaseTitle)
        {
            var result = Parser.Parser.ParseTitle(path);
            result.ReleaseTitle.Should().Be(releaseTitle);
        }

        [TestCase("Босх: Спадок (S2E1) / Series: Legacy (S2E1) (2023) WEB-DL 1080p Ukr/Eng | sub Eng", "Босх: Спадок", "Series: Legacy")]
        [TestCase("Босх: Спадок / Series: Legacy / S2E1-4 of 10 (2023) WEB-DL 1080p Ukr/Eng | sub Eng", "Босх: Спадок", "Series: Legacy")]
        [TestCase("Босх: Спадок AKA Series: Legacy S02 1080p NF WEB-DL Dual- Audio DD+ 5.1 Atmos H.264-APEX", "Босх: Спадок", "Series: Legacy")]
        [TestCase("Босх.Спадок.AKA.Series.Legacy.S02.1080p.NF.WEB-DL.DUAL.DDP5.1.Atmos.H.264-APEX", "Босх Спадок", "Series Legacy")]
        [TestCase("[Judas] Series E (Series J) (Season 04) [1080p][HEVC x265 10bit][Dual-Audio][Multi-Subs] (Batch)", "Series E", "Series J")]
        [TestCase("Serie / Series [S01E02] [MULTI] [1080p] [HMAX] [WEB-DL] [H.264] [DDP5.1.Atmos-K83] [Lektor i Napisy PL]", "Serie", "Series")]
        public void should_parse_multiple_series_titles(string postTitle, params string[] titles)
        {
            var seriesTitleInfo = Parser.Parser.ParseTitle(postTitle).SeriesTitleInfo;
            seriesTitleInfo.AllTitles.Should().BeEquivalentTo(titles);
        }

        [TestCase("[Reza] Series in Russian - S01E08 [WEBRip 1080p HEVC AAC] (Dual Audio) (Tokidoki Bosotto Russiago de Dereru Tonari no Alya-san)", "Unknown")]
        public void should_parse_language_after_parsing_title(string postTitle, string expectedLanguage)
        {
            var result = Parser.Parser.ParseTitle(postTitle);

            result.Languages.Count.Should().Be(1);
            result.Languages.Should().Contain((Language)expectedLanguage);
        }

        // Movie-domain members merged from Radarr

        [Test]
        public void should_remove_accents_from_title_movie()
        {
            const string title = "Carniv\u00E0le";

            title.CleanMovieTitle().Should().Be("carnivale");
        }

        [TestCase("The.Movie.from.U.N.C.L.E.2015.1080p.BluRay.x264-SPARKS", "The Movie from U.N.C.L.E.")]
        [TestCase("1776.1979.EXTENDED.720p.BluRay.X264-AMIABLE", "1776")]
        [TestCase("MY MOVIE (2016) [R][Action, Horror][720p.WEB-DL.AVC.8Bit.6ch.AC3].mkv", "MY MOVIE")]
        [TestCase("R.I.P.D.2013.720p.BluRay.x264-SPARKS", "R.I.P.D.")]
        [TestCase("V.H.S.2.2013.LIMITED.720p.BluRay.x264-GECKOS", "V.H.S. 2")]
        [TestCase("This Is A Movie (1999) [IMDB #] <Genre, Genre, Genre> {ACTORS} !DIRECTOR +MORE_SILLY_STUFF_NO_ONE_NEEDS ?", "This Is A Movie")]
        [TestCase("We Are the Movie!.2013.720p.H264.mkv", "We Are the Movie!")]
        [TestCase("(500).Days.Of.Movie.(2009).DTS.1080p.BluRay.x264.NLsubs", "(500) Days Of Movie")]
        [TestCase("To.Live.and.Movie.in.L.A.1985.1080p.BluRay", "To Live and Movie in L.A.")]
        [TestCase("A.I.Artificial.Movie.(2001)", "A.I. Artificial Movie")]
        [TestCase("A.Movie.Name.(1998)", "A Movie Name")]
        [TestCase("www.Torrenting.com - Movie.2008.720p.X264-DIMENSION", "Movie")]
        [TestCase("www.5MovieRulz.tc - Movie (2000) Malayalam HQ HDRip - x264 - AAC - 700MB.mkv", "Movie")]
        [TestCase("Movie: The Movie World 2013", "Movie: The Movie World")]
        [TestCase("Movie.The.Final.Chapter.2016", "Movie The Final Chapter")]
        [TestCase("Der.Movie.James.German.Bluray.FuckYou.Pso.Why.cant.you.follow.scene.rules.1998", "Der Movie James")]
        [TestCase("Movie.German.DL.AC3.Dubbed..BluRay.x264-PsO", "Movie")]
        [TestCase("Valana la Movie TRUEFRENCH BluRay 720p 2016 kjhlj", "Valana la Movie")]
        [TestCase("Mission Movie: Rogue Movie (2015)�[XviD - Ita Ac3 - SoftSub Ita]azione, spionaggio, thriller *Prima Visione* Team mulnic Tom Cruise", "Mission Movie: Rogue Movie")]
        [TestCase("Movie.Movie.2000.FRENCH..BluRay.-AiRLiNE", "Movie Movie")]
        [TestCase("My Movie 1999 German Bluray", "My Movie")]
        [TestCase("Leaving Movie by Movie (1897) [DVD].mp4", "Leaving Movie by Movie")]
        [TestCase("Movie.2018.1080p.AMZN.WEB-DL.DD5.1.H.264-NTG", "Movie")]
        [TestCase("Movie.Title.Imax.2018.1080p.AMZN.WEB-DL.DD5.1.H.264-NTG", "Movie Title")]
        [TestCase("World.Movie.Z.EXTENDED.2013.German.DL.1080p.BluRay.AVC-XANOR", "World Movie Z")]
        [TestCase("World.Movie.Z.2.EXTENDED.2013.German.DL.1080p.BluRay.AVC-XANOR", "World Movie Z 2")]
        [TestCase("G.I.Movie.Movie.2013.THEATRiCAL.COMPLETE.BLURAY-GLiMMER", "G.I. Movie Movie")]
        [TestCase("www.Torrenting.org - Movie.2008.720p.X264-DIMENSION", "Movie")]
        [TestCase("The.French.Movie.2013.720p.BluRay.x264 - ROUGH[PublicHD]", "The French Movie")]
        [TestCase("The.Good.German.2006.720p.BluRay.x264-RlsGrp", "The Good German", Description = "Hardcoded to exclude from German regex")]
        public void should_parse_movie_title(string postTitle, string title)
        {
            Parser.Parser.ParseMovieTitle(postTitle).PrimaryMovieTitle.Should().Be(title);
        }

        [TestCase("[MTBB] Kimi no Na wa. (2016) v2 [97681524].mkv", "Kimi no Na wa", "MTBB", 2016)]
        [TestCase("[sam] Toward the Terra (1980) [BD 1080p TrueHD].mkv", "Toward the Terra", "sam", 1980)]
        [TestCase("[Inka-Subs] Rokudenashi Blues 1993 (1993) [VHS]", "Rokudenashi Blues 1993", "Inka-Subs", 1993)]
        public void should_parse_anime_movie_title(string postTitle, string title, string releaseGroup, int year)
        {
            var movie = Parser.Parser.ParseMovieTitle(postTitle);
            using (new AssertionScope())
            {
                movie.PrimaryMovieTitle.Should().Be(title);
                movie.ReleaseGroup.Should().Be(releaseGroup);
                movie.Year.Should().Be(year);
            }
        }

        [TestCase("[Arid] Cowboy Bebop - Knockin' on Heaven's Door v2 [00F4CDA0].mkv", "Cowboy Bebop - Knockin' on Heaven's Door", "Arid")]
        [TestCase("[Baws] Evangelion 1.11 - You Are (Not) Alone v2 (1080p BD HEVC FLAC) [BF42B1C8].mkv", "Evangelion 1 11 - You Are (Not) Alone", "Baws")]
        [TestCase("[Arid] 5 Centimeters per Second (BDRip 1920x1080 Hi10 FLAC) [FD8B6FF2].mkv", "5 Centimeters per Second", "Arid")]
        [TestCase("[Baws] Evangelion 2.22 - You Can (Not) Advance (1080p BD HEVC FLAC) [56E7A5B8].mkv", "Evangelion 2 22 - You Can (Not) Advance", "Baws")]
        [TestCase("[sam] Goblin Slayer - Goblin's Crown [BD 1080p FLAC] [CD298D48].mkv", "Goblin Slayer - Goblin's Crown", "sam")]
        [TestCase("[Kulot] Violet Evergarden Gaiden Eien to Jidou Shuki Ningyou [Dual-Audio][BDRip 1920x804 HEVC FLACx2] [91FC62A8].mkv", "Violet Evergarden Gaiden Eien to Jidou Shuki Ningyou", "Kulot")]
        public void should_parse_anime_movie_title_without_year(string postTitle, string title, string releaseGroup)
        {
            var movie = Parser.Parser.ParseMovieTitle(postTitle);
            using (new AssertionScope())
            {
                movie.PrimaryMovieTitle.Should().Be(title);
                movie.ReleaseGroup.Should().Be(releaseGroup);
            }
        }

        [TestCase("Movie.Aufbruch.nach.Pandora.Extended.2009.German.DTS.720p.BluRay.x264-SoW", "Movie Aufbruch nach Pandora", "Extended", 2009)]
        [TestCase("Drop.Movie.1994.German.AC3D.DL.720p.BluRay.x264-KLASSiGERHD", "Drop Movie", "", 1994)]
        [TestCase("Kick.Movie.2.2013.German.DTS.DL.720p.BluRay.x264-Pate", "Kick Movie 2", "", 2013)]
        [TestCase("Movie.Hills.2019.German.DL.AC3.Dubbed.1080p.BluRay.x264-muhHD", "Movie Hills", "", 2019)]
        [TestCase("96.Hours.Movie.3.EXTENDED.2014.German.DL.1080p.BluRay.x264-ENCOUNTERS", "96 Hours Movie 3", "EXTENDED", 2014)]
        [TestCase("Movie.War.Q.EXTENDED.CUT.2013.German.DL.1080p.BluRay.x264-HQX", "Movie War Q", "EXTENDED CUT", 2013)]
        [TestCase("Sin.Movie.2005.RECUT.EXTENDED.German.DL.1080p.BluRay.x264-DETAiLS", "Sin Movie", "RECUT EXTENDED", 2005)]
        [TestCase("2.Movie.in.L.A.1996.GERMAN.DL.720p.WEB.H264-SOV", "2 Movie in L.A.", "", 1996)]
        [TestCase("8.2019.GERMAN.720p.BluRay.x264-UNiVERSUM", "8", "", 2019)]
        [TestCase("Life.Movie.2014.German.DL.PAL.DVDR-ETM", "Life Movie", "", 2014)]
        [TestCase("Joe.Movie.2.EXTENDED.EDITION.2015.German.DL.PAL.DVDR-ETM", "Joe Movie 2", "EXTENDED EDITION", 2015)]
        [TestCase("Movie.EXTENDED.2011.HDRip.AC3.German.XviD-POE", "Movie", "EXTENDED", 2011)]

        // Special cases (see description)
        [TestCase("Movie.Klasse.von.1999.1990.German.720p.HDTV.x264-NORETAiL", "Movie Klasse von 1999", "", 1990, Description = "year in the title")]
        [TestCase("Movie.Squad.2016.EXTENDED.German.DL.AC3.BDRip.x264-hqc", "Movie Squad", "EXTENDED", 2016, Description = "edition after year")]
        [TestCase("Movie.and.Movie.2010.Extended.Cut.German.DTS.DL.720p.BluRay.x264-HDS", "Movie and Movie", "Extended Cut", 2010, Description = "edition after year")]
        [TestCase("Der.Movie.James.German.Bluray.FuckYou.Pso.Why.cant.you.follow.scene.rules.1998", "Der Movie James", "", 1998, Description = "year at the end")]
        [TestCase("Der.Movie.Eine.Unerwartete.Reise.Extended.German.720p.BluRay.x264-EXQUiSiTE", "Der Movie Eine Unerwartete Reise", "Extended", 0, Description = "no year & edition")]
        [TestCase("Movie.Weg.des.Kriegers.EXTENDED.German.720p.BluRay.x264-EXQUiSiTE", "Movie Weg des Kriegers", "EXTENDED", 0, Description = "no year & edition")]
        [TestCase("Die.Unfassbaren.Movie.Name.EXTENDED.German.DTS.720p.BluRay.x264-RHD", "Die Unfassbaren Movie Name", "EXTENDED", 0, Description = "no year & edition")]
        [TestCase("Die Unfassbaren Movie Name EXTENDED German DTS 720p BluRay x264-RHD", "Die Unfassbaren Movie Name", "EXTENDED", 0, Description = "no year & edition & without dots")]
        [TestCase("Passengers.German.DL.AC3.Dubbed..BluRay.x264-PsO", "Passengers", "", 0, Description = "no year")]
        [TestCase("Das.A.Team.Der.Film.Extended.Cut.German.720p.BluRay.x264-ANCIENT", "Das A Team Der Film", "Extended Cut", 0, Description = "no year")]
        [TestCase("Cars.2.German.DL.720p.BluRay.x264-EmpireHD", "Cars 2", "", 0, Description = "no year")]
        [TestCase("Die.fantastische.Reise.des.Dr.Dolittle.2020.German.DL.LD.1080p.WEBRip.x264-PRD", "Die fantastische Reise des Dr. Dolittle", "", 2020, Description = "dot after dr")]
        [TestCase("Der.Film.deines.Lebens.German.2011.PAL.DVDR-ETM", "Der Film deines Lebens", "", 2011, Description = "year at wrong position")]
        [TestCase("Kick.Ass.2.2013.German.DTS.DL.720p.BluRay.x264-Pate_", "Kick Ass 2", "", 2013, Description = "underscore at the end")]
        [TestCase("The.Good.German.2006.GERMAN.720p.HDTV.x264-RLsGrp", "The Good German", "", 2006, Description = "German in the title")]
        public void should_parse_german_movie(string postTitle, string title, string edition, int year)
        {
            var movie = Parser.Parser.ParseMovieTitle(postTitle);
            using (new AssertionScope())
            {
                movie.PrimaryMovieTitle.Should().Be(title);
                movie.Edition.Should().Be(edition);
                movie.Year.Should().Be(year);
            }
        }

        [TestCase("L'hypothèse.du.movie.volé.AKA.The.Hypothesis.of.the.Movie.Title.1978.1080p.CINET.WEB-DL.AAC2.0.x264-Cinefeel.mkv",
            new string[]
            {
                "L'hypothèse du movie volé AKA The Hypothesis of the Movie Title",
                "L'hypothèse du movie volé",
                "The Hypothesis of the Movie Title"
            })]
        [TestCase("Skjegg.AKA.Rox.Beard.1965.CD1.CRiTERiON.DVDRip.XviD-KG.avi",
            new string[]
            {
                "Skjegg AKA Rox Beard",
                "Skjegg",
                "Rox Beard"
            })]
        [TestCase("Kjeller.chitai.AKA.Basement.of.Shame.1956.1080p.BluRay.x264.FLAC.1.0.mkv",
            new string[]
            {
                "Kjeller chitai AKA Basement of Shame",
                "Kjeller chitai",
                "Basement of Shame"
            })]
        [TestCase("Radarr.Under.Water.(aka.Beneath.the.Code.Freeze).1997.DVDRip.x264.CG-Grzechsin.mkv",
            new string[]
            {
                "Radarr Under Water (aka Beneath the Code Freeze)",
                "Radarr Under Water",
                "Beneath the Code Freeze"
            })]
        [TestCase("Radarr.prodavet. AKA.Radarr.Shift.2005.DVDRip.x264-HANDJOB.mkv",
            new string[]
            {
                "Radarr prodavet  AKA Radarr Shift",
                "Radarr prodavet",
                "Radarr Shift"
            })]
        [TestCase("AKA.2002.DVDRip.x264-HANDJOB.mkv",
            new string[]
            {
                "AKA"
            })]
        [TestCase("KillRoyWasHere.2000.BluRay.1080p.DTS.x264.dxva-EuReKA.mkv",
            new string[]
            {
                "KillRoyWasHere"
            })]
        [TestCase("Aka Rox (2008).avi",
            new string[]
            {
                "Aka Rox"
            })]
        [TestCase("Return Earth to Normal 'em High aka World 2 (2022) 1080p.mp4",
            new string[]
            {
                "Return Earth to Normal 'em High aka World 2",
                "Return Earth to Normal 'em High",
                "World 2"
            })]
        [TestCase("Енола Голмс / Enola Holmes (2020) UHD WEB-DL 2160p 4K HDR H.265 Ukr/Eng | Sub Ukr/Eng",
            new string[]
            {
                "Енола Голмс / Enola Holmes",
                "Енола Голмс",
                "Enola Holmes"
            })]
        [TestCase("Mon cousin a.k.a. My Cousin 2020 1080p Blu-ray DD 5.1 x264.mkv",
            new string[]
            {
                "Mon cousin AKA My Cousin",
                "Mon cousin",
                "My Cousin"
            })]
        [TestCase("Sydney A.K.A. Hard Eight 1996 1080p AMZN WEB-DL DD+ 2.0 H.264.mkv",
            new string[]
            {
                "Sydney AKA Hard Eight",
                "Sydney",
                "Hard Eight"
            })]
        public void should_parse_movie_alternative_titles(string postTitle, string[] parsedTitles)
        {
            var movieInfo = Parser.Parser.ParseMovieTitle(postTitle, true);

            movieInfo.MovieTitles.Count.Should().Be(parsedTitles.Length);

            for (var i = 0; i < movieInfo.MovieTitles.Count; i += 1)
            {
                movieInfo.MovieTitles[i].Should().Be(parsedTitles[i]);
            }
        }

        [TestCase("(1995) Movie Name", "Movie Name")]
        public void should_parse_movie_folder_name(string postTitle, string title)
        {
            Parser.Parser.ParseMovieTitle(postTitle, true).PrimaryMovieTitle.Should().Be(title);
        }

        [TestCase("1776.1979.EXTENDED.720p.BluRay.X264-AMIABLE", 1979)]
        [TestCase("Movie Name FRENCH BluRay 720p 2016 kjhlj", 2016)]
        [TestCase("Der.Movie.German.Bluray.FuckYou.Pso.Why.cant.you.follow.scene.rules.1998", 1998)]
        [TestCase("Movie Name (1897) [DVD].mp4", 1897)]
        [TestCase("World Movie Z Movie [2023]", 2023)]
        public void should_parse_movie_year(string postTitle, int year)
        {
            Parser.Parser.ParseMovieTitle(postTitle).Year.Should().Be(year);
        }

        [TestCase("Movie Name (2016) {tmdbid-43074}", 43074)]
        [TestCase("Movie Name (2016) [tmdb-43074]", 43074)]
        [TestCase("Movie Name (2016) {tmdb-43074}", 43074)]
        [TestCase("Movie Name (2016) {tmdb-2020}", 2020)]
        public void should_parse_tmdb_id(string postTitle, int tmdbId)
        {
            Parser.Parser.ParseMovieTitle(postTitle).TmdbId.Should().Be(tmdbId);
        }

        [TestCase("The.Italian.Movie.2025.720p.BluRay.X264-AMIABLE")]
        [TestCase("The.French.Movie.2013.720p.BluRay.x264 - ROUGH[PublicHD]")]
        [TestCase("The.German.Doctor.2013.LIMITED.DVDRip.x264-RedBlade", Description = "When German is not followed by a year or a SCENE word it is not matched")]
        [TestCase("The.Good.German.2006.720p.HDTV.x264-TVP", Description = "The Good German is hardcoded not to match")]
        [TestCase("German.Lancers.2019.720p.BluRay.x264-UNiVERSUM", Description = "German at the beginning is never matched")]
        [TestCase("The.German.2019.720p.BluRay.x264-UNiVERSUM", Description = "The German is hardcoded not to match")]
        [TestCase("Movie Name (2016) BluRay 1080p DTS-ES AC3 x264-3Li", Description = "DTS-ES should not match ES (Spanish)")]
        public void should_not_parse_wrong_language_in_title(string postTitle)
        {
            var parsed = Parser.Parser.ParseMovieTitle(postTitle, true);
            parsed.Languages.Count.Should().Be(1);
            parsed.Languages.First().Should().Be(Language.Unknown);
        }

        [TestCase("Movie.Title.2016.1080p.KORSUB.WEBRip.x264.AAC2.0-RADARR", "KORSUB")]
        [TestCase("Movie.Title.2016.1080p.KORSUBS.WEBRip.x264.AAC2.0-RADARR", "KORSUBS")]
        [TestCase("Movie Title 2017 HC 720p HDRiP DD5 1 x264-LEGi0N", "Generic Hardcoded Subs")]
        [TestCase("Movie.Title.2017.720p.SUBBED.HDRip.V2.XViD-26k.avi", "Generic Hardcoded Subs")]
        [TestCase("Movie.Title.2000.1080p.BlueRay.x264.DTS.RoSubbed-playHD", null)]
        [TestCase("Movie Title! 2018 [Web][MKV][h264][480p][AAC 2.0][Softsubs]", null)]
        [TestCase("Movie Title! 2019 [HorribleSubs][Web][MKV][h264][848x480][AAC 2.0][Softsubs(HorribleSubs)]", null)]
        [TestCase("Movie Title! 2024 [Web][x265][1080p][EAC3][MultiSubs]", null)]
        public void should_parse_hardcoded_subs(string postTitle, string sub)
        {
            Parser.Parser.ParseMovieTitle(postTitle).HardcodedSubs.Should().Be(sub);
        }

        [TestCase("That Italian Movie 2008 [tt1234567] 720p BluRay X264", "tt1234567")]
        [TestCase("That Italian Movie 2008 [tt12345678] 720p BluRay X264", "tt12345678")]
        public void should_parse_imdb_in_title(string postTitle, string imdb)
        {
            var parsed = Parser.Parser.ParseMovieTitle(postTitle, true);
            parsed.ImdbId.Should().Be(imdb);
        }

        [TestCase("asfd", null)]
        [TestCase("123", "tt0000123")]
        [TestCase("1234567", "tt1234567")]
        [TestCase("tt1234567", "tt1234567")]
        [TestCase("tt12345678", "tt12345678")]
        [TestCase("12345678", "tt12345678")]
        public void should_normalize_imdbid(string imdbid, string normalized)
        {
            Parser.Parser.NormalizeImdbId(imdbid).Should().BeEquivalentTo(normalized);
        }
}
}

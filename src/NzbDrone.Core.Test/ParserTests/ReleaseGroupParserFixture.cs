using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ReleaseGroupParserFixture : CoreTest
    {
        [TestCase("Series.2009.S01E14.English.HDTV.XviD-LOL", "LOL")]
        [TestCase("Series 2009 S01E14 English HDTV XviD LOL", null)]
        [TestCase("Series Now S05 EXTRAS DVDRip XviD RUNNER", null)]
        [TestCase("Series.Title.S01.EXTRAS.DVDRip.XviD-RUNNER", "RUNNER")]
        [TestCase("2020.Series.2011.12.02.PDTV.XviD-C4TV", "C4TV")]
        [TestCase("The.Series.S03E115.DVDRip.XviD-OSiTV", "OSiTV")]
        [TestCase("Series Title - S01E01 - Pilot [HTDV-480p]", null)]
        [TestCase("Series Title - S01E01 - Pilot [HTDV-720p]", null)]
        [TestCase("Series Title - S01E01 - Pilot [HTDV-1080p]", null)]
        [TestCase("The.Series.Title.S04E13.720p.WEB-DL.AAC2.0.H.264-Cyphanix", "Cyphanix")]
        [TestCase("Series.S02E01.720p.WEB-DL.DD5.1.H.264.mkv", null)]
        [TestCase("Series Title S01E01 Episode Title", null)]
        [TestCase("The Series Title - 2014-06-02 - Thomas Piketty.mkv", null)]
        [TestCase("The Series Title S12E17 May 23, 2014.mp4", null)]
        [TestCase("Reizen Waes - S01E08 - Transistri\u00EB, Zuid-Osseti\u00EB en Abchazi\u00EB SDTV.avi", null)]
        [TestCase("The Series Title 10x11 - Wild Devs Cant Be Broken [rl].avi", "rl")]
        [TestCase("[ www.Torrenting.com ] - Series.S03E14.720p.HDTV.X264-DIMENSION", "DIMENSION")]
        [TestCase("Series S02E09 HDTV x264-2HD [eztv]-[rarbg.com]", "2HD")]
        [TestCase("7s-Series-s02e01-720p.mkv", null)]
        [TestCase("The.Series.S09E13.720p.HEVC.x265-MeGusta-Pre", "MeGusta")]
        [TestCase("Series Title - S01E01 - Episode Title [RlsGroup]", "RlsGroup")]
        [TestCase("Red Series S01 E01-E02 1080p AMZN WEBRip DDP5.1 x264 monkee", null)]
        [TestCase("Series.Title.S01E05.The-Aniversary.WEBDL-1080p.mkv", null)]
        [TestCase("Series.Title.S01E05.The-Aniversary.HDTV-1080p.mkv", null)]
        [TestCase("Series US (2010) S04 (1080p BDRip x265 10bit DTS-HD MA 5 1 - WEM)[TAoE]", "TAoE")]
        [TestCase("The.Series.S03E04.2160p.Amazon.WEBRip.DTS-HD.MA.5.1.x264", null)]
        [TestCase("SomeShow.S20E13.1080p.BluRay.DTS-X.MA.5.1.x264", null)]
        [TestCase("SomeShow.S20E13.1080p.BluRay.DTS-MA.5.1.x264", null)]
        [TestCase("SomeShow.S20E13.1080p.BluRay.DTS-ES.5.1.x264", null)]
        [TestCase("SomeShow.S20E13.1080p.Blu-Ray.DTS-ES.5.1.x264", null)]
        [TestCase("SomeShow.S20E13.1080p.Blu-Ray.DTS-ES.5.1.x264-ROUGH [PublicHD]", "ROUGH")]
        [TestCase("SomeShow S01E168 1080p WEB-DL AAC 2.0 x264-Erai-raws", "Erai-raws")]
        [TestCase("The.Good.Series.S05E03.Series.of.Intelligence.1080p.10bit.AMZN.WEB-DL.DDP5.1.HEVC-Vyndros", "Vyndros")]
        [TestCase("[Tenrai-Sensei] Series [BD][1080p][HEVC 10bit x265][Dual Audio]", "Tenrai-Sensei")]
        [TestCase("[Erai-raws] Series - 0955 ~ 1005 [1080p]", "Erai-raws")]
        [TestCase("[Exiled-Destiny] Series Title", "Exiled-Destiny")]
        [TestCase("Series.Title.S01E09.1080p.DSNP.WEB-DL.DDP2.0.H.264-VARYG", "VARYG")]
        [TestCase("Series (1997) - S01E01-02 - Children of the Gods (Showtime) (1080p.BD.DD5.1.x265-TheSickle[TAoE])", "TAoE")]
        [TestCase("Series Title S01 [2160p REMUX] [HEVC DV HYBRID HDR10+ Dolby TrueHD Atmos 7 1 24-bit Audio English] [Data Lass]", null)]
        [TestCase("Series Title S01 [2160p REMUX] [HEVC DV HYBRID HDR10+ Dolby TrueHD Atmos 7 1 24-bit Audio English]-DataLass", "DataLass")]
        [TestCase("Series Title S01 REMUX Dual Audio AVC 1080p 8-Bit-ZR-", "ZR")]
        [TestCase("Show.Name.2009.S01.1080p.BluRay.DTS5.1.x264-D-Z0N3", "D-Z0N3")]
        [TestCase("Series Stampede S01 MULTi 1080p BD x265 Opus AAC -Báleygr", "Báleygr")]
        [TestCase("Series S01E01 VOSTFR 1080p WEB x265 EAC3 -Hveðrungr", "Hveðrungr")]
        [TestCase("Series.S01E06.1080p.AMZN.WEB-DL.JPN.DDP2.0.H.264-ToonsHub.mkv-[N-Z-B]-xpost", "ToonsHub")]
        public void should_parse_release_group(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Show.Name.S01E01.1080p.WEB-DL.H264.Fight-BB.mkv", "Fight-BB")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p BluRay x265 HEVC 10bit AAC 5.1 Tigole) [QxR]", "QxR")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p BluRay x265 HEVC 10bit AAC 2.0 afm72) [QxR]", "QxR")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p DSNP WEB-DL x265 HEVC 10bit EAC3 5.1 Silence) [QxR]", "QxR")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p BluRay x265 HEVC 10bit AAC 2.0 Panda) [QxR]", "QxR")]
        [TestCase("Show Name (2020) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 2.0 Ghost) [QxR]", "QxR")]
        [TestCase("Show Name (2020) Season 1 S01 (1080p WEB-DL x265 HEVC 10bit AC3 5.1 MONOLITH) [QxR]", "QxR")]
        [TestCase("The Show S08E09 The Series.1080p.AMZN.WEB-DL.x265.10bit.EAC3.6.0-Qman[UTR]", "UTR")]
        [TestCase("The Show S03E07 Fire and Series[1080p x265 10bit S87 Joy]", "Joy")]
        [TestCase("The Show (2016) - S02E01 - Soul Series #1 (1080p NF WEBRip x265 ImE)", "ImE")]
        [TestCase("The Show (2020) - S02E03 - Fighting His Series(1080p ATVP WEB-DL x265 t3nzin)", "t3nzin")]
        [TestCase("[Anime Time] A Show [BD][Dual Audio][1080p][HEVC 10bit x265][AAC][Eng Sub] [Batch] Title)", "Anime Time")]
        [TestCase("[Project Angel] Anime Series [DVD 480p] [10-bit x265 HEVC | Opus]", "Project Angel")]
        [TestCase("[Hakata Ramen] Show Title - Season 2 - Revival of The Commandments", "Hakata Ramen")]
        [TestCase("Show Name (2022) S01 (2160p DSNP WEB-DL H265 DV HDR DDP Atmos 5.1 English - HONE)", "HONE")]
        [TestCase("Show Title (2021) S01 (2160p ATVP WEB-DL Hybrid H265 DV HDR10+ DDP Atmos 5.1 English - HONE)", "HONE")]
        [TestCase("Series.Title.S01E09.1080p.DSNP.WEB-DL.DDP2.0.H.264-VARYG (Blue Lock, Multi-Subs)", "VARYG")]
        [TestCase("Series.Title (2014) S09E10 (1080p AMZN WEB-DL x265 HEVC 10bit DDP 5.1 Vyndros)", "Vyndros")]
        [TestCase("Series Title S02E03 Title 4k to 1080p DSNP WEBrip x265 DDP 5 1 Releaser[SEV]", "SEV")]
        [TestCase("Series Title Season 01 S01 1080p AMZN UHD WebRip x265 DDP 5.1 Atmos Releaser-SEV", "SEV")]
        [TestCase("Series Title - S01.E06 - Title 1080p AMZN WebRip x265 DDP 5.1 Atmos Releaser [SEV]", "SEV")]
        [TestCase("Series Title (2005) - S01E01 - A Hard Day's Night (1080p DSNP WEB-DL x265 Garshasp).mkv", "Garshasp")]
        [TestCase("Series Title (2015) - S02E04 - Smoke & Mirrors (1080p BluRay x265 Kappa).mkv", "Kappa")]
        [TestCase("Series Title (2020) - S02E03 - A Great Odyssey (1080p BluRay x265 Kappa).mkv", "Kappa")]
        [TestCase("Series Title (2019) - S01E01 - Episode 1 (1080p WEB-DL x265 Natty).mkv", "Natty")]
        [TestCase("Series Title (1999) - S03E01-E02 - Mermaid Man and Barnacle Boy IV & Doing Time (1080p AMZN WEB-DL x265 RCVR).mkv", "RCVR")]
        [TestCase("Series Title (2021) - S01E02 - Here Goes Nothing (1080p WEB-DL x265 SAMPA).mkv", "SAMPA")]
        [TestCase("Series Title (2021) - S01E01 - Aftermath (1080p DSNP WEB-DL x265 YOGI).mkv", "YOGI")]
        [TestCase("Series Title (2012) - S01E01 - Episode 1 (1080p BluRay x265 r00t).mkv", "r00t")]
        [TestCase("Series Title - S01E01 - Girls Gone Wild Exposed (720p x265 EDGE2020).mkv", "EDGE2020")]
        [TestCase("Series.Title.S01E02.1080p.BluRay.Remux.AVC.FLAC.2.0-E.N.D", "E.N.D")]
        [TestCase("Show Name (2016) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5 1 RZeroX) QxR", "QxR")]
        [TestCase("Series Title S01 1080p Blu-ray Remux AVC FLAC 2.0 - KRaLiMaRKo", "KRaLiMaRKo")]
        [TestCase("Series Title S01 1080p Blu-ray Remux AVC DTS-HD MA 2.0 - BluDragon", "BluDragon")]
        [TestCase("Example (2013) S01E01 (1080p iP WEBRip x265 SDR AAC 2.0 English - DarQ)", "DarQ")]
        [TestCase("Series.Title.S08E03.720p.WEB.DL.AAC2.0.H.264.KCRT", "KCRT")]
        [TestCase("Series Title S02E05 2160p WEB-DL DV HDR ENG DDP5.1 Atmos H265 MP4-BEN THE MEN", "BEN THE MEN")]
        [TestCase("Series Title S02E05 2160p AMZN WEB-DL DV HDR10 PLUS DDP5 1 Atmos H265 MKV-BEN THE MEN-xpost", "BEN THE MEN")]
        [TestCase("Series.S01E05.1080p.WEB-DL.DDP5.1.H264-BEN.THE.MEN", "BEN.THE.MEN")]
        [TestCase("Series (2022) S01 (1080p BluRay x265 SDR DDP 5.1 English - JBENT TAoE)", "TAoE")]
        [TestCase("Series (2005) S21E12 (1080p AMZN WEB-DL x265 SDR DDP 5.1 English - Goki TAoE)", "TAoE")]
        [TestCase("Series (2022) S03E12 (1080p AMZN Webrip x265 10 bit EAC3 5 1 - Ainz)[TAoE]", "TAoE")]
        [TestCase("Series Things (2016) S04 Part 1 (1080p Webrip NF x265 10bit EAC3 5 1 - AJJMIN) [TAoE]", "TAoE")]
        [TestCase("Series Soup (2024) S01 (1080p NF Webrip x265 10bit EAC3 5 1 Multi - ANONAZ)[TAoE]", "TAoE")]
        [TestCase("Series (2022) S01 (1080p NF Webrip x265 10bit EAC3 5 1 Atmos - ArcX)[TAoE]", "TAoE")]
        [TestCase("Series - King of Titles (2021) S01 (1080p HMAX Webrip x265 10bit AC3 5 1 - bccornfo) [TAoE]", "TAoE")]
        [TestCase("Welcome to Series (2022) S04 (1080p AMZN Webrip x265 10bit EAC3 5 1 - DNU)[TAoE]", "TAoE")]
        [TestCase("Series Who (2005) S01 (1080p BDRip x265 10bit AC3 5 1 - DrainedDay)[TAoE]", "TAoE")]
        [TestCase("Series Down (2019) (1080p AMZN Webrip x265 10bit EAC3 5 1 - DUHiT)[TAoE]", "TAoE")]
        [TestCase("Series (2016) S09 (1080p CRAV Webrip x265 10bit EAC3 5 1 - Erie) [TAoE]", "TAoE")]
        [TestCase("Common Series Effects (2025) S01 (1080p AMZN Webrip x265 10bit EAC3 2 0 - Frys) [TAoE]", "TAoE")]
        [TestCase("Murderbot (2025) S01 (2160p HDR10 DV Hybrid ATVP Webrip x265 10bit EAC3 5 1 Atmos - Goki)[TAoE]", "TAoE")]
        [TestCase("Series In Real Life (2019) S01 REPACK (1080p DSNP Webrip x265 10bit AAC 2 0 - HxD)[TAoE]", "TAoE")]
        [TestCase("Series Discovery (2017) S02 (1080p BDRip x265 10bit DTS-HD MA 5 1 - jb2049) [TAoE]", "TAoE")]
        [TestCase("Series (2021) S03 (1080p DS4K NF Webrip x265 10bit EAC3 5 1 Atmos English - JBENT)[TAoE]", "TAoE")]
        [TestCase("SuSeriespergirl (2015) S04 (1080p BDRip x265 10bit AC3 5 1 - Nostradamus)[TAoE]", "TAoE")]
        [TestCase("Series (2019) S02 (4Kto1080p ATVP Webrip x265 10bit AC3 5 1 - r0b0t) [TAoE]", "TAoE")]
        [TestCase("v (1970) S01 (2160p AIUS HDR10 DV Hybrid BDRip x265 10bit DTS-HD MA 5 1 - Species180) [TAoE]", "TAoE")]
        [TestCase("Series (2024) S02 (1080p ATVP Webrip x265 10bit EAC3 5 1 - TheSickle)[TAoE]", "TAoE")]
        [TestCase("Series (2016) S05 Part 02 (1080p NF Webrip x265 10bit EAC3 5 1 - xtrem3x) [TAoE]", "TAoE")]
        [TestCase("Series (2013) S01 (1080p BDRip x265 10bit DTS-HD MA 5 1 - WEM)[TAoE]", "TAoE")]
        [TestCase("The.Series.1989.S00E65.1080p.DSNP.Webrip.x265.10bit.EAC3.5.1.Goki.TAoE", "TAoE")]
        [TestCase("Series (2008) S05E01 (1080p BDRip DDP2.0 x265) - Vialle", "Vialle")]
        [TestCase("The Series (2023) S03E01 (1080p DS4K NF Webrip DV HDR DDP5.1 x265) - Vialle.mkv", "Vialle")]
        [TestCase("The Show (2020) - S02E03 - Fighting His Series(1080p ATVP WEB-DL x265 t3nzinFAKE)", null)]
        [TestCase("The Series (2023) S03E01 (1080p DS4K NF Webrip DV HDR DDP5.1 x265) - VialleFAKE.mkv", null)]
        [TestCase("The Series (2025) S03E01 (1080p AMZN WEB-DL x265 SDR DDP 5.1 English - Celdra QxR)", "QxR")]
        [TestCase("The Series (2025) - S03E01 - We're Already Here (1080p AMZN WEB-DL x265 Celdra)", "Celdra")]
        public void should_parse_exception_release_group(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [Test]
        public void should_not_include_extension_in_release_group()
        {
            const string path = @"C:\Test\Doctor.Series.2005.s01e01.internal.bdrip.x264-archivist.mkv";

            Parser.Parser.ParsePath(path).ReleaseGroup.Should().Be("archivist");
        }

        [TestCase("Series.Title.S02E04.720p.WEBRip.x264-SKGTV English", "SKGTV")]
        [TestCase("Series.Title.S02E04.720p.WEBRip.x264-SKGTV_English", "SKGTV")]
        [TestCase("Series.Title.S02E04.720p.WEBRip.x264-SKGTV.English", "SKGTV")]

        // [TestCase("", "")]
        public void should_not_include_language_in_release_group(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Series.Title.S02E04.720p.WEB-DL.AAC2.0.H.264-EVL-RP", "EVL")]
        [TestCase("Series.Title.S02E04.720p.WEB-DL.AAC2.0.H.264-EVL-RP-RP", "EVL")]
        [TestCase("Series.Title.S02E04.720p.WEB-DL.AAC2.0.H.264-EVL-Obfuscated", "EVL")]
        [TestCase("Series.Title.S04E04.720p.BluRay.x264-xHD-NZBgeek", "xHD")]
        [TestCase("Series.Title.S05E11.720p.HDTV.X264-DIMENSION-NZBgeek", "DIMENSION")]
        [TestCase("Series.Title.S04E04.720p.BluRay.x264-xHD-1", "xHD")]
        [TestCase("Series.Title.S05E11.720p.HDTV.X264-DIMENSION-1", "DIMENSION")]
        [TestCase("series.title.s40e11.kevin.hart_sia.720p.hdtv.x264-w4f-sample.mkv", "w4f")]
        [TestCase("The.Series.2017.S05E02.1080p.WEB-DL.DD5.1.H264-EVL-Scrambled", "EVL")]
        [TestCase("Series.S01E08.Haunted.Hayride.720p.AMZN.WEBRip.DDP5.1.x264-NTb-postbot", "NTb")]
        [TestCase("Series.S01E08.Haunted.Hayride.720p.AMZN.WEBRip.DDP5.1.x264-NTb-xpost", "NTb")]
        [TestCase("Series.Title.S08E05.The.Forgotten.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-Rakuv", "NTb")]
        [TestCase("The.Series.S30E01.Devs.Not.Dead.1080p.AMZN.WEB-DL.DDP5.1.H264-QOQ-Rakuv02", "QOQ")]
        [TestCase("Lie.To.Developers.S01E13.720p.BluRay.x264-SiNNERS-Rakuvfinhel", "SiNNERS")]
        [TestCase("Who.is.Sonarr.S01E01.INTERNAL.720p.HDTV.x264-aAF-RakuvUS-Obfuscated", "aAF")]
        [TestCase("Deadly.Development.S01E10.Sink.With.Code.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTG-WhiteRev", "NTG")]
        [TestCase("The.Sonarr.Series.S09E12.Developers.REPACK.1080p.AMZN.WEB-DL.DD.5.1.H.264-CasStudio-BUYMORE", "CasStudio")]
        [TestCase("2.Tired.Developers.S02E24.1080p.AMZN.WEBRip.DD5.1.x264-CasStudio-AsRequested", "CasStudio")]
        [TestCase("Series.S04E11.Lines.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-AlternativeToRequested", "NTb")]
        [TestCase("Series.S16E04.Third.Wheel.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-GEROV", "NTb")]
        [TestCase("Series.and.Title.S10E06.Dev.n.Play.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-Z0iDS3N", "NTb")]
        [TestCase("Absolute.Series.S02E06.The.House.of.Sonarr.DVDRip.x264-MaG-Chamele0n", "MaG")]
        [TestCase("The.Series.Title.S08E08.1080p.BluRay.x264-ROVERS-4P", "ROVERS")]
        [TestCase("Series.Title.S01E02.720p.BluRay.X264-REWARD-4Planet", "REWARD")]
        [TestCase("Series.S01E01.Rites.of.Passage.1080p.BluRay.x264-DON-AlteZachen", "DON")]
        [TestCase("Series.Title.S04E06.Episode.Name.720p.WEB-DL.DD5.1.H.264-HarrHD-RePACKPOST", "HarrHD")]
        public void should_not_include_repost_in_release_group(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("[FFF] Series Title!! - S01E11 - Someday, With Sonarr", "FFF")]
        [TestCase("[HorribleSubs] Series Title!! - S01E12 - Sonarr Going Well!!", "HorribleSubs")]
        [TestCase("[Anime-Koi] Series Title - S01E06 - Guys From Sonarr", "Anime-Koi")]
        [TestCase("[Anime-Koi] Series Title - S01E07 - A High-Grade Sonarr", "Anime-Koi")]
        [TestCase("[Anime-Koi] Series Title 2 - 01 [h264-720p][28D54E2C]", "Anime-Koi")]

        // [TestCase("Tokyo.Ghoul.02x01.013.HDTV-720p-Anime-Koi", "Anime-Koi")]
        // [TestCase("", "")]
        public void should_parse_anime_release_groups(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Terrible.Anime.Title.001.DBOX.480p.x264-iKaos [v3] [6AFFEF6B]")]
        public void should_not_parse_anime_hash_as_release_group(string title)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().BeNull();
        }

        [TestCase("Some TV (1979) - S02E10 - Hart-Shaped Murder [SDTV][AAC 2.0][x264]")]
        [TestCase("Some TV (1979) - S02E10 - Hart-Shaped Murder [HDTV-480p][AAC 2.0][x264]")]
        [TestCase("Some TV (1979) - S02E10 - Hart-Shaped Murder [480p-HDTV][AAC 2.0][x264]")]
        public void should_not_parse_episode_title_as_release_group(string title)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().BeNull();
        }

        // Movie-domain members merged from Radarr

        [TestCase("Movie.2009.S01E14.English.HDTV.XviD-LOL", "LOL")]
        [TestCase("Movie 2009 S01E14 English HDTV XviD LOL", null)]
        [TestCase("Acropolis Now S05 EXTRAS DVDRip XviD RUNNER", null)]
        [TestCase("Punky.Brewster.S01.EXTRAS.DVDRip.XviD-RUNNER", "RUNNER")]
        [TestCase("2020.NZ.2011.12.02.PDTV.XviD-C4TV", "C4TV")]
        [TestCase("Some.Movie.S03E115.DVDRip.XviD-OSiTV", "OSiTV")]
        [TestCase("Some Movie - S01E01 - Pilot [HTDV-480p]", null)]
        [TestCase("Some Movie - S01E01 - Pilot [HTDV-720p]", null)]
        [TestCase("Some Movie - S01E01 - Pilot [HTDV-1080p]", null)]
        [TestCase("Movie.Name.S04E13.720p.WEB-DL.AAC2.0.H.264-Cyphanix", "Cyphanix")]
        [TestCase("Movie.Name.S02E01.720p.WEB-DL.DD5.1.H.264.mkv", null)]
        [TestCase("Series Title S01E01 Episode Title", null)]
        [TestCase("Movie.Name- 2014-06-02 - Some Movie.mkv", null)]
        [TestCase("Movie.Name S12E17 May 23, 2014.mp4", null)]
        [TestCase("Movie.Name - S01E08 - Transistri\u00EB, Zuid-Osseti\u00EB en Abchazi\u00EB SDTV.avi", null)]
        [TestCase("Movie.Name 10x11 - Wild Movies Cant Be Broken [rl].avi", "rl")]
        [TestCase("[ www.Torrenting.com ] - Movie.Name.S03E14.720p.HDTV.X264-DIMENSION", "DIMENSION")]
        [TestCase("Movie.Name S02E09 HDTV x264-2HD [eztv]-[rarbg.com]", "2HD")]
        [TestCase("7s-Movie.Name-s02e01-720p.mkv", null)]
        [TestCase("The.Movie.Name.720p.HEVC.x265-MeGusta-Pre", "MeGusta")]
        [TestCase("Blue.Movie.Name.S08E05.The.Movie.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-Rakuv", "NTb")]
        [TestCase("Movie.Name.S01E13.720p.BluRay.x264-SiNNERS-Rakuvfinhel", "SiNNERS")]
        [TestCase("Movie.Name.S01E01.INTERNAL.720p.HDTV.x264-aAF-RakuvUS-Obfuscated", "aAF")]
        [TestCase("Movie.Name.2018.720p.WEBRip.DDP5.1.x264-NTb-postbot", "NTb")]
        [TestCase("Movie.Name.2018.720p.WEBRip.DDP5.1.x264-NTb-xpost", "NTb")]
        [TestCase("Movie.Name.S02E24.1080p.AMZN.WEBRip.DD5.1.x264-CasStudio-AsRequested", "CasStudio")]
        [TestCase("Movie.Name.S04E11.Lamster.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-AlternativeToRequested", "NTb")]
        [TestCase("Movie.Name.S16E04.Third.Wheel.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-GEROV", "NTb")]
        [TestCase("Movie.NameS10E06.Kid.n.Play.1080p.AMZN.WEB-DL.DDP5.1.H.264-NTb-Z0iDS3N", "NTb")]
        [TestCase("Movie.Name.S02E06.The.House.of.Lords.DVDRip.x264-MaG-Chamele0n", "MaG")]
        [TestCase("Some.Movie.2013.1080p.BluRay.REMUX.AVC.DTS-X.MA.5.1", null)]
        [TestCase("Some.Movie.2013.1080p.BluRay.REMUX.AVC.DTS-MA.5.1", null)]
        [TestCase("Movie.Name.2013.1080p.BluRay.REMUX.AVC.DTS-ES.MA.5.1", null)]
        [TestCase("SomeMovie.1080p.BluRay.DTS-X.264.-D-Z0N3.mkv", "D-Z0N3")]
        [TestCase("SomeMovie.1080p.BluRay.DTS.x264.-Blu-bits.mkv", "Blu-bits")]
        [TestCase("SomeMovie.1080p.BluRay.DTS.x264.-DX-TV.mkv", "DX-TV")]
        [TestCase("SomeMovie.1080p.BluRay.DTS.x264.-FTW-HS.mkv", "FTW-HS")]
        [TestCase("SomeMovie.1080p.BluRay.DTS.x264.-VH-PROD.mkv", "VH-PROD")]
        [TestCase("Some.Dead.Movie.2006.1080p.BluRay.DTS.x264.D-Z0N3", "D-Z0N3")]
        [TestCase("Movie.Title.2010.720p.BluRay.x264.-[YTS.LT]", "YTS.LT")]
        [TestCase("The.Movie.Title.2013.720p.BluRay.x264-ROUGH [PublicHD]", "ROUGH")]
        [TestCase("Some.Really.Bad.Movie.Title.[2021].1080p.WEB-HDRip.Dual.Audio.[Hindi.[Clean]. .English].x264.AAC.DD.2.0.By.Full4Movies.mkv-xpost", null)]
        [TestCase("The.Movie.Title.2013.1080p.10bit.AMZN.WEB-DL.DDP5.1.HEVC-Vyndros", "Vyndros")]
        [TestCase("Movie.Name.2022.1080p.BluRay.x264-[YTS.AG]", "YTS.AG")]
        [TestCase("Movie.Name.2022.1080p.BluRay.x264-VARYG", "VARYG")]
        [TestCase("Movie.Title.2019.1080p.AMZN.WEB-Rip.DDP.5.1.HEVC", null)]
        [TestCase("Movie Name (2017) [2160p REMUX] [HEVC DV HYBRID HDR10+ Dolby TrueHD Atmos 7 1 24-bit Audio English] [Data Lass]", null)]
        [TestCase("Movie Name (2017) [2160p REMUX] [HEVC DV HYBRID HDR10+ Dolby TrueHD Atmos 7 1 24-bit Audio English]-DataLass", "DataLass")]
        [TestCase("Movie Name (2017) (Showtime) (1080p.BD.DD5.1.x265-TheSickle[TAoE])", "TAoE")]
        public void should_parse_release_group_movie(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Movie Name (2020) [2160p x265 10bit S82 Joy]", "Joy")]
        [TestCase("Movie Name (2003) (2160p BluRay X265 HEVC 10bit HDR AAC 7.1 Tigole) [QxR]", "QxR")]
        [TestCase("Ode To Joy (2009) (2160p BluRay x265 10bit HDR Joy)", "Joy")]
        [TestCase("Movie Name (2001) 1080p NF WEB-DL DDP2.0 x264-E.N.D", "E.N.D")]
        [TestCase("Movie Name (2020) [1080p] [WEBRip] [5.1] [YTS.MX]", "YTS.MX")]
        [TestCase("Movie Name.2018.1080p.Blu-ray.Remux.AVC.DTS-HD.MA.5.1.KRaLiMaRKo", "KRaLiMaRKo")]
        [TestCase("Ode To Joy (2009) (2160p BluRay x265 10bit HDR FreetheFish)", "FreetheFish")]
        [TestCase("Ode To Joy (2009) (2160p BluRay x265 10bit HDR afm72)", "afm72")]
        [TestCase("Ode To Joy (2009) (2160p BluRay x265 10bit HDR)", null)]
        [TestCase("Movie Name (2012) (1080p BluRay x265 HEVC 10bit AC3 2.0 Anna)", "Anna")]
        [TestCase("Movie Name (2019) (1080p BluRay x265 HEVC 10bit AAC 7.1 Q22 Joy)", "Joy")]
        [TestCase("Movie Name (2019) (2160p BluRay x265 HEVC 10bit HDR AAC 7.1 Bandi)", "Bandi")]
        [TestCase("Movie Name (2009) (1080p HDTV x265 HEVC 10bit AAC 2.0 Ghost)", "Ghost")]
        [TestCase("Movie Name in the Movie (2017) (1080p BluRay x265 HEVC 10bit AAC 7.1 Tigole)", "Tigole")]
        [TestCase("Mission - Movie Name - Movie Protocol (2011) (1080p BluRay x265 HEVC 10bit AAC 7.1 Tigole)", "Tigole")]
        [TestCase("Movie Name (1990) (1080p BluRay x265 HEVC 10bit AAC 5.1 Silence)", "Silence")]
        [TestCase("Happy Movie Name (1999) (1080p BluRay x265 HEVC 10bit AAC 5.1 Korean Kappa)", "Kappa")]
        [TestCase("Movie Name (2007) Open Matte (1080p AMZN WEB-DL x265 HEVC 10bit AAC 5.1 MONOLITH)", "MONOLITH")]
        [TestCase("Movie-Name (2019) (1080p BluRay x265 HEVC 10bit DTS 7.1 Qman)", "Qman")]
        [TestCase("Movie Name - Hell to Ticket (2018) + Extras (1080p BluRay x265 HEVC 10bit AAC 5.1 RZeroX)", "RZeroX")]
        [TestCase("Movie Name (2013) (Diamond Luxe Edition) + Extras (1080p BluRay x265 HEVC 10bit EAC3 7.1 SAMPA)", "SAMPA")]
        [TestCase("Movie Name 1984 (2020) (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5.1 Silence)", "Silence")]
        [TestCase("The.Movie.of.the.Name.1991.REMASTERED.720p.10bit.BluRay.6CH.x265.HEVC-PSA", "PSA")]
        [TestCase("Movie Name 2016 (1080p BluRay x265 HEVC 10bit DDP 5.1 theincognito)", "theincognito")]
        [TestCase("Movie Name - A History of Movie (2017) (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 2.0 t3nzin)", "t3nzin")]
        [TestCase("Movie Name (2019) (1080p BluRay x265 HEVC 10bit AAC 7.1 Vyndros)", "Vyndros")]
        [TestCase("Movie Name (2015) [BDRemux 1080p AVC ES-CAT-EN DTS-HD MA 5.1 Subs][HDO]", "HDO")]
        [TestCase("Movie Name (2015) [BDRemux 1080p AVC EN-CAT-ES DTS-HD MA 5.1 Subs][HDO]", "HDO")]
        [TestCase("Movie Name (2017) [BDRemux 1080p AVC ES DTS 5.1 - EN DTS-HD MA 7.1 Subs][HDO]", "HDO")]
        [TestCase("Movie Name (2006) [BDRemux 1080p AVC ES DTS-HD MA 2.0 - EN DTS-HD MA 5.1 Sub][HDO]", "HDO")]
        [TestCase("Movie Name (2015) [BDRemux 1080p AVC ES-CAT-EN DTS-HD MA 5.1 Subs]", null)]
        [TestCase("Movie Name (2015) [BDRemux 1080p AVC EN-CAT-ES DTS-HD MA 5.1 Subs]", null)]
        [TestCase("Movie Name (2015) [BDRemux 1080p AVC EN-ES-CAT DTS-HD MA 5.1 Subs]", null)]
        [TestCase("Another Crappy Anime Movie Name 1999 [DusIctv] [Blu-ray][MKV][h264][1080p][DTS-HD MA 5.1][Dual Audio][Softsubs (DusIctv)", "DusIctv")]
        [TestCase("Another Crappy Anime Movie Name 1999 [DHD] [Blu-ray][MKV][h264][1080p][AAC 5.1][Dual Audio][Softsubs (DHD)]", "DHD")]
        [TestCase("Another Crappy Anime Movie Name 1999 [SEV] [Blu-ray][MKV][h265 10-bit][1080p][FLAC 5.1][Dual Audio][Softsubs (SEV)]", "SEV")]
        [TestCase("Another Crappy Anime Movie Name 1999 [CtrlHD] [Blu-ray][MKV][h264][720p][AC3 2.0][Dual Audio][Softsubs (CtrlHD)]", "CtrlHD")]
        [TestCase("Crappy Anime Movie Name 2017 [-ZR-] [Blu-ray][MKV][h264][1080p][TrueHD 5.1][Dual Audio][Softsubs (-ZR-)]", "-ZR-")]
        [TestCase("Crappy Anime Movie Name 2017 [XZVN] [Blu-ray][MKV][h264][1080p][TrueHD 5.1][Dual Audio][Softsubs (XZVN)]", "XZVN")]
        [TestCase("Crappy Anime Movie Name 2017 [ADC] [Blu-ray][M2TS (A)][16:9][h264][1080p][TrueHD 5.1][Dual Audio][Softsubs (ADC)]", "ADC")]
        [TestCase("Crappy Anime Movie Name 2017 [Koten_Gars] [Blu-ray][MKV][h264][1080p][TrueHD 5.1][Dual Audio][Softsubs (Koten_Gars)]", "Koten_Gars")]
        [TestCase("Crappy Anime Movie Name 2017 [RH] [Blu-ray][MKV][h264 10-bit][1080p][FLAC 5.1][Dual Audio][Softsubs (RH)]", "RH")]
        [TestCase("Yet Another Anime Movie 2012 [Kametsu] [Blu-ray][MKV][h264 10-bit][1080p][FLAC 5.1][Dual Audio][Softsubs (Kametsu)]", "Kametsu")]
        [TestCase("Another.Anime.Film.Name.2016.JPN.Blu-Ray.Remux.AVC.DTS-MA.BluDragon", "BluDragon")]
        [TestCase("A Movie in the Name (1964) (1080p BluRay x265 r00t)", "r00t")]
        [TestCase("Movie Title (2022) (2160p ATV WEB-DL Hybrid H265 DV HDR DDP Atmos 5.1 English - HONE)", "HONE")]
        [TestCase("Movie Title (2009) (2160p PMTP WEB-DL Hybrid H265 DV HDR10+ DDP Atmos 5.1 English - HONE)", "HONE")]
        [TestCase("Movie Title (2022) (1080p PCOK WEB-DL H265 DV HDR DDP Atmos 5.1 English - GiLG)", "GiLG")]
        [TestCase("Movie Title (2022) Extended (2160p PCOK WEB-DL H265 DV HDR DDP Atmos 5.1 English - GiLG)", "GiLG")]
        [TestCase("Why.Cant.You.Use.Normal.Characters.2021.2160p.UHD.HDR10+.BluRay.TrueHD.Atmos.7.1.x265-ZØNEHD", "ZØNEHD")]
        [TestCase("Movie.Should.Not.Use.Dots.2022.1080p.BluRay.x265.10bit.Tigole)", "Tigole")]
        [TestCase("Movie.Should.Not.Use.Dots.2022.1080p.BluRay.x265.10bit.Tigole", null)]
        [TestCase("Movie.Title.2005.2160p.UHD.BluRay.TrueHD 7.1.Atmos.x265 - HQMUX", "HQMUX")]
        [TestCase("Movie.Name.2022.1080p.BluRay.x264-VARYG (Blue Lock, Multi-Subs)", "VARYG")]
        [TestCase("Movie Title (2023) (1080p BluRay x265 SDR AAC 2.0 English Vyndros)", "Vyndros")]
        [TestCase("Movie Title (2010) 1080p BrRip x264 - YIFY", "YIFY")]
        [TestCase("Movie Title (2011) [BluRay] [1080p] [YTS.MX] [YIFY]", "YIFY")]
        [TestCase("Movie Title (2014) [BluRay] [1080p] [YIFY] [YTS]", "YTS")]
        [TestCase("Movie Title (2018) [BluRay] [1080p] [YIFY] [YTS.LT]", "YTS.LT")]
        [TestCase("Movie Title (2016) (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5 1 RZeroX) QxR", "QxR")]
        [TestCase("Movie Title (2016) (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5 1 Garshasp) QxR", "QxR")]
        [TestCase("Movie Title 2024 mUHD 10Bits DoVi HDR10 2160p BluRay DD 5 1 x265 - TMd", "TMd")]
        [TestCase("Movie Title 2024 mUHD 10Bits DoVi HDR10 2160p BluRay DD 5 1 x265 TMd", "TMd")]
        [TestCase("Movie Title (2024) 2160p WEB-DL ESP DD+ 5.1 ING DD+ 5.1 Atmos DV HDR H.265-Eml HDTeam", "Eml HDTeam")]
        [TestCase("Movie Title(2023) 1080p SkySHO WEB-DL ESP DD+ 5.1 H.264-EML HDTeam", "EML HDTeam")]
        [TestCase("Movie Title (2022) BDFull 1080p DTS-HD MA 5.1 AVC LMain", "LMain")]
        [TestCase("Movie Title (2024) (1080p BluRay x265 SDR DDP 5.1 English - DarQ)", "DarQ")]
        [TestCase("Movie Title (2024) (1080p BluRay x265 SDR DDP 5.1 English -BEN THE MEN", "BEN THE MEN")]
        [TestCase("Movie Title 2024 2160p WEB-DL DoVi HDR10+ H265 DDP 5.1 Atmos-126811", "126811")]
        [TestCase("Movie Title (2025) (1080p BluRay x265 SDR DDP Atmos 7.1 English - Celdra QxR)", "QxR")]
        [TestCase("Movie Title (2025) (1080p BluRay x265 Celdra)", "Celdra")]
        public void should_parse_exception_release_group_movie(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase(@"C:\Test\Doctor.Series.2005.s01e01.internal.bdrip.x264-archivist.mkv", "archivist")]
        public void should_not_include_extension_in_release_group_movie(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Some.Movie.S02E04.720p.WEBRip.x264-SKGTV English", "SKGTV")]
        [TestCase("Some.Movie.S02E04.720p.WEBRip.x264-SKGTV_English", "SKGTV")]
        [TestCase("Some.Movie.S02E04.720p.WEBRip.x264-SKGTV.English", "SKGTV")]

        public void should_not_include_language_in_release_group_movie(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-RP", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-RP-RP", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Obfuscation", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-NZBgeek", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-1", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-sample.mkv", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Scrambled", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-postbot", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-xpost", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Rakuv", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Rakuv02", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Rakuvfinhel", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Obfuscated", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-WhiteRev", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-BUYMORE", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-AsRequested", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-AlternativeToRequested", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-GEROV", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Z0iDS3N", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-Chamele0n", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-4P", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-EVO-4Planet", "EVO")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-DON-AlteZachen", "DON")]
        [TestCase("Some.Movie.2019.1080p.BDRip.X264.AC3-HarrHD-RePACKPOST", "HarrHD")]

        public void should_not_include_bad_suffix_in_release_group(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("[FFF] Invaders of the Movies!! - S01E11 - Someday, With Movies", "FFF")]
        [TestCase("[HorribleSubs] Invaders of the Movies!! - S01E12 - Movies Going Well!!", "HorribleSubs")]
        [TestCase("[Anime-Koi] Movies - S01E06 - Guys From Movies", "Anime-Koi")]
        [TestCase("[Anime-Koi] Movies - S01E07 - A High-Grade Movies", "Anime-Koi")]
        [TestCase("[Anime-Koi] Kami-sama Movies 2 - 01 [h264-720p][28D54E2C]", "Anime-Koi")]

        public void should_parse_anime_release_groups_movie(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Terrible.Anime.Title.2020.DBOX.480p.x264-iKaos [v3] [6AFFEF6B]")]
        public void should_not_parse_anime_hash_as_release_group_movie(string title)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().BeNull();
        }

}
}

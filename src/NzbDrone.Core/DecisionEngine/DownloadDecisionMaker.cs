using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IMakeDownloadDecision
    {
        List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false);
        List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase);
    }

    public class DownloadDecisionMaker : IMakeDownloadDecision
    {
        private readonly IEnumerable<IDownloadDecisionEngineSpecification> _specifications;
        private readonly IParsingService _parsingService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IRemoteEpisodeAggregationService _episodeAggregationService;
        private readonly IRemoteMovieAggregationService _movieAggregationService;
        private readonly ISceneMappingService _sceneMappingService;
        private readonly Logger _logger;

        public DownloadDecisionMaker(IEnumerable<IDownloadDecisionEngineSpecification> specifications,
                                     IParsingService parsingService,
                                     ICustomFormatCalculationService formatCalculator,
                                     IRemoteEpisodeAggregationService episodeAggregationService,
                                     IRemoteMovieAggregationService movieAggregationService,
                                     ISceneMappingService sceneMappingService,
                                     Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _formatCalculator = formatCalculator;
            _episodeAggregationService = episodeAggregationService;
            _movieAggregationService = movieAggregationService;
            _sceneMappingService = sceneMappingService;
            _logger = logger;
        }

        public List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false)
        {
            return GetDecisions(reports, pushedRelease).ToList();
        }

        public List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase)
        {
            return GetDecisions(reports, false, searchCriteriaBase).ToList();
        }

        private IEnumerable<DownloadDecision> GetDecisions(List<ReleaseInfo> reports, bool pushedRelease = false, SearchCriteriaBase searchCriteria = null)
        {
            if (reports.Any())
            {
                _logger.ProgressInfo("Processing {0} releases", reports.Count);
            }
            else
            {
                _logger.ProgressInfo("No results found");
            }

            var reportNumber = 1;

            foreach (var report in reports)
            {
                DownloadDecision decision = null;
                _logger.ProgressTrace("Processing release {0}/{1}", reportNumber, reports.Count);
                _logger.Debug("Processing release '{0}' from '{1}'", report.Title, report.Indexer);

                try
                {
                    if (searchCriteria is MovieSearchCriteria)
                    {
                        decision = ProcessMovieReport(report, pushedRelease, searchCriteria);
                    }
                    else if (searchCriteria != null)
                    {
                        decision = ProcessEpisodeReport(report, pushedRelease, searchCriteria);
                    }
                    else
                    {
                        // RSS and pushed releases can belong to either domain, so try
                        // both and prefer whichever matched a library item.
                        var episodeDecision = ProcessEpisodeReport(report, pushedRelease, null);
                        var movieDecision = ProcessMovieReport(report, pushedRelease, null);

                        var episodeMatched = episodeDecision?.RemoteEpisode?.Series != null && episodeDecision.RemoteEpisode.Episodes?.Any() == true;
                        var movieMatched = movieDecision?.RemoteMovie?.Movie != null;

                        if (movieMatched && !episodeMatched)
                        {
                            decision = movieDecision;
                        }
                        else if (episodeMatched && !movieMatched)
                        {
                            decision = episodeDecision;
                        }
                        else
                        {
                            // Both matched or neither matched, prefer the episode domain.
                            decision = episodeDecision ?? movieDecision;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't process release.");

                    if (searchCriteria is MovieSearchCriteria)
                    {
                        var remoteMovie = new RemoteMovie { Release = report, ReleaseSource = GetReleaseSource(pushedRelease, searchCriteria) };
                        decision = new DownloadDecision(remoteMovie, new DownloadRejection(DownloadRejectionReason.Error, "Unexpected error processing release"));
                    }
                    else
                    {
                        var remoteEpisode = new RemoteEpisode { Release = report, ReleaseSource = GetReleaseSource(pushedRelease, searchCriteria) };
                        decision = new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.Error, "Unexpected error processing release"));
                    }
                }

                reportNumber++;

                if (decision != null)
                {
                    if (decision.Rejections.Any())
                    {
                        _logger.Debug("Release '{0}' from '{1}' rejected for the following reasons: {2}", report.Title, report.Indexer, string.Join(", ", decision.Rejections));
                    }
                    else
                    {
                        _logger.Debug("Release '{0}' from '{1}' accepted", report.Title, report.Indexer);
                    }

                    yield return decision;
                }
            }
        }

        private DownloadDecision ProcessEpisodeReport(ReleaseInfo report, bool pushedRelease, SearchCriteriaBase searchCriteria)
        {
            DownloadDecision decision = null;

            var parsedEpisodeInfo = Parser.Parser.ParseTitle(report.Title);

            if (parsedEpisodeInfo == null || parsedEpisodeInfo.IsPossibleSpecialEpisode)
            {
                var specialEpisodeInfo = _parsingService.ParseSpecialEpisodeTitle(parsedEpisodeInfo, report.Title, report.TvdbId, report.TvRageId, report.ImdbId, searchCriteria);

                if (specialEpisodeInfo != null)
                {
                    parsedEpisodeInfo = specialEpisodeInfo;
                }
            }

            if (parsedEpisodeInfo == null)
            {
                // Attempt to parse as a release that includes the season title without a season number
                parsedEpisodeInfo = Parser.Parser.ParseSeasonTitle(report.Title);
            }

            if (parsedEpisodeInfo != null && !parsedEpisodeInfo.SeriesTitle.IsNullOrWhiteSpace())
            {
                var remoteEpisode = _parsingService.Map(parsedEpisodeInfo, report.TvdbId, report.TvRageId, report.ImdbId, searchCriteria);
                remoteEpisode.Release = report;
                remoteEpisode.ReleaseSource = GetReleaseSource(pushedRelease, searchCriteria);

                if (remoteEpisode.Series == null)
                {
                    var matchingTvdbId = _sceneMappingService.FindTvdbId(parsedEpisodeInfo.SeriesTitle, parsedEpisodeInfo.ReleaseTitle, parsedEpisodeInfo.SeasonNumber ?? -1);

                    if (matchingTvdbId.HasValue)
                    {
                        decision = new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.MatchesAnotherSeries, $"{parsedEpisodeInfo.SeriesTitle} matches an alias for series with TVDB ID: {matchingTvdbId}"));
                    }
                    else
                    {
                        decision = new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.UnknownSeries, "Unknown Series"));
                    }
                }
                else if (remoteEpisode.Episodes.Empty())
                {
                    decision = new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.UnknownEpisode, "Unable to identify correct episode(s) using release name and scene mappings"));
                }
                else
                {
                    _episodeAggregationService.Augment(remoteEpisode);

                    remoteEpisode.CustomFormats = _formatCalculator.ParseCustomFormat(remoteEpisode, remoteEpisode.Release.Size);
                    remoteEpisode.CustomFormatScore = remoteEpisode?.Series?.QualityProfile?.Value.CalculateCustomFormatScore(remoteEpisode.CustomFormats) ?? 0;

                    _logger.Trace("Custom Format Score of '{0}' [{1}] calculated for '{2}'", remoteEpisode.CustomFormatScore, remoteEpisode.CustomFormats?.ConcatToString(), report.Title);

                    remoteEpisode.DownloadAllowed = remoteEpisode.Episodes.Any();
                    decision = GetDecisionForReport(remoteEpisode, new ReleaseDecisionInformation(pushedRelease, searchCriteria));
                }
            }

            if (searchCriteria != null)
            {
                if (parsedEpisodeInfo == null)
                {
                    parsedEpisodeInfo = new ParsedEpisodeInfo
                    {
                        Languages = LanguageParser.ParseLanguages(report.Title),
                        Quality = QualityParser.ParseQuality(report.Title)
                    };
                }

                if (parsedEpisodeInfo.SeriesTitle.IsNullOrWhiteSpace())
                {
                    var remoteEpisode = new RemoteEpisode
                    {
                        Release = report,
                        ReleaseSource = GetReleaseSource(pushedRelease, searchCriteria),
                        ParsedEpisodeInfo = parsedEpisodeInfo,
                        Languages = parsedEpisodeInfo.Languages,
                    };

                    decision = new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.UnableToParse, "Unable to parse release"));
                }
            }

            return decision;
        }

        private DownloadDecision ProcessMovieReport(ReleaseInfo report, bool pushedRelease, SearchCriteriaBase searchCriteria)
        {
            DownloadDecision decision = null;

            var parsedMovieInfo = Parser.Parser.ParseMovieTitle(report.Title);

            if (parsedMovieInfo != null && !parsedMovieInfo.PrimaryMovieTitle.IsNullOrWhiteSpace())
            {
                var remoteMovie = _parsingService.Map(parsedMovieInfo, report.ImdbId, report.TmdbId, searchCriteria);
                remoteMovie.Release = report;
                remoteMovie.ReleaseSource = GetReleaseSource(pushedRelease, searchCriteria);

                if (remoteMovie.Movie == null)
                {
                    decision = new DownloadDecision(remoteMovie, new DownloadRejection(DownloadRejectionReason.UnknownMovie, pushedRelease ? "Unknown Movie. Unable to match to existing movie in Library using release title." : "Unknown Movie. Unable to match to correct movie using release title."));
                }
                else
                {
                    _movieAggregationService.Augment(remoteMovie);

                    remoteMovie.CustomFormats = _formatCalculator.ParseCustomFormat(remoteMovie, remoteMovie.Release.Size);
                    remoteMovie.CustomFormatScore = remoteMovie?.Movie?.QualityProfile?.CalculateCustomFormatScore(remoteMovie.CustomFormats) ?? 0;

                    _logger.Trace("Custom Format Score of '{0}' [{1}] calculated for '{2}'", remoteMovie.CustomFormatScore, remoteMovie.CustomFormats?.ConcatToString(), report.Title);

                    remoteMovie.DownloadAllowed = remoteMovie.Movie != null;
                    decision = GetDecisionForReport(remoteMovie, new ReleaseDecisionInformation(pushedRelease, searchCriteria));
                }
            }

            if (searchCriteria != null)
            {
                if (parsedMovieInfo == null)
                {
                    parsedMovieInfo = new ParsedMovieInfo
                    {
                        Languages = LanguageParser.ParseLanguages(report.Title),
                        Quality = QualityParser.ParseQuality(report.Title)
                    };
                }

                if (parsedMovieInfo.PrimaryMovieTitle.IsNullOrWhiteSpace())
                {
                    var remoteMovie = new RemoteMovie
                    {
                        Release = report,
                        ReleaseSource = GetReleaseSource(pushedRelease, searchCriteria),
                        ParsedMovieInfo = parsedMovieInfo,
                        Languages = parsedMovieInfo.Languages,
                    };

                    decision = new DownloadDecision(remoteMovie, new DownloadRejection(DownloadRejectionReason.UnableToParse, "Unable to parse release"));
                }
            }

            return decision;
        }

        private DownloadDecision GetDecisionForReport(RemoteEpisode remoteEpisode, ReleaseDecisionInformation information)
        {
            var reasons = Array.Empty<DownloadRejection>();

            foreach (var specifications in _specifications.GroupBy(v => v.Priority).OrderBy(v => v.Key))
            {
                reasons = specifications.Select(c => EvaluateSpec(c, remoteEpisode, information))
                                        .Where(c => c != null)
                                        .ToArray();

                if (reasons.Any())
                {
                    break;
                }
            }

            return new DownloadDecision(remoteEpisode, reasons.ToArray());
        }

        private DownloadDecision GetDecisionForReport(RemoteMovie remoteMovie, ReleaseDecisionInformation information)
        {
            var reasons = Array.Empty<DownloadRejection>();

            foreach (var specifications in _specifications.GroupBy(v => v.Priority).OrderBy(v => v.Key))
            {
                reasons = specifications.Select(c => EvaluateSpec(c, remoteMovie, information))
                                        .Where(c => c != null)
                                        .ToArray();

                if (reasons.Any())
                {
                    break;
                }
            }

            return new DownloadDecision(remoteMovie, reasons.ToArray());
        }

        private DownloadRejection EvaluateSpec(IDownloadDecisionEngineSpecification spec, RemoteEpisode remoteEpisode, ReleaseDecisionInformation information)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteEpisode, information);

                if (!result.Accepted)
                {
                    return new DownloadRejection(result.Reason, result.Message, spec.Type);
                }
            }
            catch (NotImplementedException)
            {
                _logger.Trace("Spec " + spec.GetType().Name + " does not care about episodes.");
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteEpisode.Release.ToJson());
                e.Data.Add("parsed", remoteEpisode.ParsedEpisodeInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on {0}, with spec: {1}", remoteEpisode.Release.Title, spec.GetType().Name);
                return new DownloadRejection(DownloadRejectionReason.DecisionError, $"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }

        private DownloadRejection EvaluateSpec(IDownloadDecisionEngineSpecification spec, RemoteMovie remoteMovie, ReleaseDecisionInformation information)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteMovie, information);

                if (!result.Accepted)
                {
                    return new DownloadRejection(result.Reason, result.Message, spec.Type);
                }
            }
            catch (NotImplementedException)
            {
                _logger.Trace("Spec " + spec.GetType().Name + " does not care about movies.");
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteMovie.Release.ToJson());
                e.Data.Add("parsed", remoteMovie.ParsedMovieInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on {0}, with spec: {1}", remoteMovie.Release.Title, spec.GetType().Name);
                return new DownloadRejection(DownloadRejectionReason.DecisionError, $"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }

        private ReleaseSourceType GetReleaseSource(bool pushedRelease, SearchCriteriaBase searchCriteria = null)
        {
            if (searchCriteria == null)
            {
                return pushedRelease ? ReleaseSourceType.ReleasePush : ReleaseSourceType.Rss;
            }

            if (searchCriteria.InteractiveSearch)
            {
                return ReleaseSourceType.InteractiveSearch;
            }
            else if (searchCriteria.UserInvokedSearch)
            {
                return ReleaseSourceType.UserInvokedSearch;
            }
            else
            {
                return ReleaseSourceType.Search;
            }
        }
    }
}

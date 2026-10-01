using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IPrioritizeDownloadDecision
    {
        List<DownloadDecision> PrioritizeDecisions(List<DownloadDecision> decisions);
        List<DownloadDecision> PrioritizeDecisionsForMovies(List<DownloadDecision> decisions);
    }

    public class DownloadDecisionPriorizationService : IPrioritizeDownloadDecision
    {
        private readonly IConfigService _configService;
        private readonly IDelayProfileService _delayProfileService;
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public DownloadDecisionPriorizationService(IConfigService configService, IDelayProfileService delayProfileService)
            : this(configService, delayProfileService, null)
        {
        }

        public DownloadDecisionPriorizationService(IConfigService configService, IDelayProfileService delayProfileService, IQualityDefinitionService qualityDefinitionService)
        {
            _configService = configService;
            _delayProfileService = delayProfileService;
            _qualityDefinitionService = qualityDefinitionService;
        }

        public List<DownloadDecision> PrioritizeDecisions(List<DownloadDecision> decisions)
        {
            var comparer = new DownloadDecisionComparer(_configService, _delayProfileService, _qualityDefinitionService);

            // Group episode decisions by series and movie decisions by movie so the
            // two domains never share a grouping key space.
            return decisions.Where(c => c.RemoteEpisode?.Series != null || c.RemoteMovie?.Movie != null)
                            .GroupBy(c => c.RemoteEpisode?.Series != null
                                    ? $"series-{c.RemoteEpisode.Series.Id}"
                                    : $"movie-{c.RemoteMovie.Movie.Id}",
                                (key, downloadDecisions) =>
                                {
                                    return downloadDecisions.OrderByDescending(decision => decision, comparer);
                                })
                            .SelectMany(c => c)
                            .Union(decisions.Where(c => c.RemoteEpisode?.Series == null && c.RemoteMovie?.Movie == null))
                            .ToList();
        }

        public List<DownloadDecision> PrioritizeDecisionsForMovies(List<DownloadDecision> decisions)
        {
            return decisions.Where(c => c.RemoteMovie?.Movie != null)
                            .GroupBy(c => c.RemoteMovie.Movie.Id, (movieId, downloadDecisions) =>
                            {
                                return downloadDecisions.OrderByDescending(decision => decision, new DownloadDecisionComparer(_configService, _delayProfileService, _qualityDefinitionService));
                            })
                            .SelectMany(c => c)
                            .Union(decisions.Where(c => c.RemoteMovie?.Movie == null))
                            .ToList();
        }
    }
}

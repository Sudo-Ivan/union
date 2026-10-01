using System;
using System.Collections.Generic;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerRequestGenerator
    {
        IndexerPageableRequestChain GetRecentRequests();

        IndexerPageableRequestChain GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(SeasonSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(DailySeasonSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(AnimeEpisodeSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(AnimeSeasonSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(SpecialEpisodeSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            throw new NotSupportedException();
        }

        Func<IDictionary<string, string>> GetCookies { get => null; set { } }

        Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get => null; set { } }
    }
}

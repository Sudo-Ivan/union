using RestSharp;
using Sonarr.Api.V3.Indexers;
using MovieReleaseResource = Sonarr.Api.V3.Indexers.ReleaseResource;

namespace NzbDrone.Integration.Test.Client
{
    public class ReleaseClient : ClientBase<ReleaseResource>
    {
        public ReleaseClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }
    }

    public class MovieReleaseClient : ClientBase<MovieReleaseResource>
    {
        public MovieReleaseClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey, "release")
        {
        }
    }
}

using System.Collections.Generic;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Notifications.Trakt.Resource;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Trakt
{
    public class TraktParser : IParseImportListResponse<ImportListItemInfo>, IParseImportListResponse<ImportListMovie>
    {
        private ImportListResponse _importResponse;

        public virtual IList<ImportListItemInfo> ParseResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var series = new List<ImportListItemInfo>();

            if (!PreProcess(_importResponse))
            {
                return series;
            }

            var traktResponses = STJson.Deserialize<List<TraktResponse>>(_importResponse.Content);

            // no series were returned
            if (traktResponses == null)
            {
                return series;
            }

            foreach (var traktResponse in traktResponses)
            {
                series.AddIfNotNull(new ImportListItemInfo()
                {
                    Title = traktResponse.Show.Title,
                    TvdbId = traktResponse.Show.Ids.Tvdb.GetValueOrDefault(),
                    ImdbId = traktResponse.Show.Ids.Imdb
                });
            }

            return series;
        }

        IList<ImportListMovie> IParseImportListResponse<ImportListMovie>.ParseResponse(ImportListResponse importResponse)
        {
            return ParseMovieResponse(importResponse);
        }

        public virtual IList<ImportListMovie> ParseMovieResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var movies = new List<ImportListMovie>();

            if (!PreProcess(_importResponse))
            {
                return movies;
            }

            var jsonResponse = STJson.Deserialize<List<TraktListResource>>(_importResponse.Content);

            // no movies were returned
            if (jsonResponse == null)
            {
                return movies;
            }

            foreach (var movie in jsonResponse)
            {
                movies.AddIfNotNull(new ImportListMovie()
                {
                    Title = movie.Movie.Title,
                    ImdbId = movie.Movie.Ids.Imdb,
                    TmdbId = movie.Movie.Ids.Tmdb ?? 0,
                    Year = movie.Movie.Year ?? 0
                });
            }

            return movies;
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse, "Trakt API call resulted in an unexpected StatusCode [{0}]", importListResponse.HttpResponse.StatusCode);
            }

            if (importListResponse.HttpResponse.Headers.ContentType != null && importListResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                importListResponse.HttpRequest.Headers.Accept != null && !importListResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(importListResponse, "Trakt API responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}

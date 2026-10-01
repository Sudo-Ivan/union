using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Simkl
{
    public class SimklParser : IParseImportListResponse<ImportListItemInfo>, IParseImportListResponse<ImportListMovie>
    {
        private ImportListResponse _importResponse;
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(SimklParser));

        public virtual IList<ImportListItemInfo> ParseResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var series = new List<ImportListItemInfo>();

            if (!PreProcess(_importResponse))
            {
                return series;
            }

            var jsonResponse = Json.Deserialize<SimklResponse>(_importResponse.Content);

            // no shows were returned
            if (jsonResponse == null)
            {
                return series;
            }

            if (jsonResponse.Anime != null)
            {
                foreach (var show in jsonResponse.Anime)
                {
                    if (int.TryParse(show.Show.Ids.Tvdb, out var tvdbId) && tvdbId > 0 && show.AnimeType is SimklAnimeType.Tv or SimklAnimeType.Ona or SimklAnimeType.Ova or SimklAnimeType.Special)
                    {
                        series.AddIfNotNull(new ImportListItemInfo
                        {
                            Title = show.Show.Title,
                            ImdbId = show.Show.Ids.Imdb,
                            TvdbId = tvdbId,
                            MalId = int.TryParse(show.Show.Ids.Mal, out var malId) ? malId : 0,
                        });
                    }
                    else
                    {
                        Logger.Warn("Skipping info grabbing for '{0}' because it is an unsupported content type.", show.Show.Title);
                    }
                }
            }

            if (jsonResponse.Shows != null)
            {
                foreach (var show in jsonResponse.Shows)
                {
                    series.AddIfNotNull(new ImportListItemInfo
                    {
                        Title = show.Show.Title,
                        TvdbId = int.TryParse(show.Show.Ids.Tvdb, out var tvdbId) ? tvdbId : 0,
                        ImdbId = show.Show.Ids.Imdb,
                    });
                }
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

            var jsonResponse = Json.Deserialize<SimklResponse>(_importResponse.Content);

            // no movies were returned
            if (jsonResponse == null)
            {
                return movies;
            }

            if (jsonResponse.Anime != null)
            {
                foreach (var movie in jsonResponse.Anime)
                {
                    if (int.TryParse(movie.Show.Ids.Tmdb, out var tmdbId) && tmdbId > 0 && movie.AnimeType is SimklAnimeType.Movie)
                    {
                        movies.AddIfNotNull(new ImportListMovie
                        {
                            Title = movie.Show.Title,
                            ImdbId = movie.Show.Ids.Imdb,
                            TmdbId = tmdbId,
                        });
                    }
                    else
                    {
                        Logger.Warn("Skipping info grabbing for '{0}' because it is an unsupported content type.", movie.Show.Title);
                    }
                }
            }

            if (jsonResponse.Movies != null)
            {
                foreach (var movie in jsonResponse.Movies)
                {
                    movies.AddIfNotNull(new ImportListMovie
                    {
                        Title = movie.Movie.Title,
                        TmdbId = int.TryParse(movie.Movie.Ids.Tmdb, out var tmdbId) ? tmdbId : 0,
                        ImdbId = movie.Movie.Ids.Imdb,
                    });
                }
            }

            return movies;
        }

        protected virtual bool PreProcess(ImportListResponse netImportResponse)
        {
            if (netImportResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(netImportResponse, "Simkl API call resulted in an unexpected StatusCode [{0}]", netImportResponse.HttpResponse.StatusCode);
            }

            if (netImportResponse.HttpResponse.Headers.ContentType != null && netImportResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                netImportResponse.HttpRequest.Headers.Accept != null && !netImportResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(netImportResponse, "Simkl API responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}

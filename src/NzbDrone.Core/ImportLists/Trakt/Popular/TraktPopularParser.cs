using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Notifications.Trakt.Resource;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Trakt.Popular
{
    public class TraktPopularParser : TraktParser
    {
        private readonly TraktPopularSettings _settings;

        public TraktPopularParser(TraktPopularSettings settings)
        {
            _settings = settings;
        }

        public override IList<ImportListItemInfo> ParseResponse(ImportListResponse importResponse)
        {
            var listItems = new List<ImportListItemInfo>();

            if (!PreProcess(importResponse))
            {
                return listItems;
            }

            var traktSeries = _settings.TraktListType switch
            {
                (int)TraktPopularListType.Popular => STJson.Deserialize<List<TraktSeriesResource>>(importResponse.Content),
                _ => STJson.Deserialize<List<TraktResponse>>(importResponse.Content).SelectList(c => c.Show)
            };

            // no series were returned
            if (traktSeries == null)
            {
                return listItems;
            }

            foreach (var series in traktSeries)
            {
                listItems.AddIfNotNull(new ImportListItemInfo
                {
                    Title = series.Title,
                    TvdbId = series.Ids.Tvdb.GetValueOrDefault(),
                });
            }

            return listItems;
        }

        public override IList<ImportListMovie> ParseMovieResponse(ImportListResponse importResponse)
        {
            var movies = new List<ImportListMovie>();

            if (!PreProcess(importResponse))
            {
                return movies;
            }

            var jsonResponse = new List<TraktMovieResource>();

            if (_settings.TraktListType == (int)TraktPopularListType.Popular)
            {
                jsonResponse = STJson.Deserialize<List<TraktMovieResource>>(importResponse.Content);
            }
            else
            {
                jsonResponse = STJson.Deserialize<List<TraktListResource>>(importResponse.Content).SelectList(c => c.Movie);
            }

            // no movies were returned
            if (jsonResponse == null)
            {
                return movies;
            }

            foreach (var movie in jsonResponse)
            {
                movies.AddIfNotNull(new ImportListMovie()
                {
                    Title = movie.Title,
                    ImdbId = movie.Ids.Imdb,
                    TmdbId = movie.Ids.Tmdb ?? 0,
                    Year = movie.Year ?? 0
                });
            }

            return movies;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Tv;
using Sonarr.Http;

namespace Sonarr.Api.V3.Calendar
{
    [V3FeedController("calendar")]
    public class CalendarFeedController : Controller
    {
        private readonly IEpisodeService _episodeService;
        private readonly ISeriesService _seriesService;
        private readonly IMovieService _movieService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IEpisodeService episodeService, ISeriesService seriesService, IMovieService movieService, ITagService tagService)
        {
            _episodeService = episodeService;
            _seriesService = seriesService;
            _movieService = movieService;
            _tagService = tagService;
        }

        [HttpGet("Sonarr.ics")]
        public IActionResult GetCalendarFeed(int pastDays = 7, int futureDays = 28, string tags = "", bool unmonitored = false, bool premieresOnly = false, bool asAllDay = false)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var parsedTags = new List<int>();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var episodes = _episodeService.EpisodesBetweenDates(start, end, unmonitored, true);
            var allSeries = _seriesService.GetAllSeries();
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//sonarr.tv//Sonarr//EN"
            };

            var calendarName = "Sonarr TV Schedule";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var episode in episodes.OrderBy(v => v.AirDateUtc.Value))
            {
                var series = allSeries.SingleOrDefault(s => s.Id == episode.SeriesId);

                if (series == null)
                {
                    continue;
                }

                if (premieresOnly && (episode.SeasonNumber == 0 || episode.EpisodeNumber != 1))
                {
                    continue;
                }

                if (parsedTags.Any() && parsedTags.None(series.Tags.Contains))
                {
                    continue;
                }

                var occurrence = calendar.Create<CalendarEvent>();
                occurrence.Uid = "NzbDrone_episode_" + episode.Id;
                occurrence.Status = episode.HasFile ? EventStatus.Confirmed : EventStatus.Tentative;
                occurrence.Description = episode.Overview;
                occurrence.Categories = new List<string>() { series.Network };

                if (asAllDay)
                {
                    occurrence.Start = new CalDateTime(episode.AirDateUtc.Value.ToLocalTime(), false);
                }
                else
                {
                    occurrence.Start = new CalDateTime(episode.AirDateUtc.Value, true);
                    occurrence.End = new CalDateTime(episode.AirDateUtc.Value.AddMinutes(series.Runtime), true);
                }

                switch (series.SeriesType)
                {
                    case SeriesTypes.Daily:
                        occurrence.Summary = $"{series.Title} - {episode.Title}";
                        break;
                    default:
                        occurrence.Summary = $"{series.Title} - {episode.SeasonNumber}x{episode.EpisodeNumber:00} - {episode.Title}";
                        break;
                }
            }

            var serializer = new SerializerFactory().Build(calendar.GetType(), new SerializationContext()) as IStringSerializer;
            var icalendar = serializer?.SerializeToString(calendar);

            return icalendar is null ? NoContent() : Content(icalendar, "text/calendar");
        }

        [HttpGet("Radarr.ics")]
        public IActionResult GetMovieCalendarFeed(int pastDays = 7, int futureDays = 28, string tags = "", bool unmonitored = false, IReadOnlyCollection<CalendarReleaseType> releaseTypes = null)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var parsedTags = new List<int>();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var movies = _movieService.GetMoviesBetweenDates(start, end, unmonitored);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//radarr.video//Radarr//EN"
            };

            var calendarName = "Radarr Movies Calendar";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var movie in movies.OrderBy(v => v.Added))
            {
                if (parsedTags.Any() && parsedTags.None(movie.Tags.Contains))
                {
                    continue;
                }

                if (releaseTypes is not { Count: not 0 } || releaseTypes.Contains(CalendarReleaseType.CinemaRelease))
                {
                    CreateEvent(calendar, movie.MovieMetadata, "cinematic");
                }

                if (releaseTypes is not { Count: not 0 } || releaseTypes.Contains(CalendarReleaseType.DigitalRelease))
                {
                    CreateEvent(calendar, movie.MovieMetadata, "digital");
                }

                if (releaseTypes is not { Count: not 0 } || releaseTypes.Contains(CalendarReleaseType.PhysicalRelease))
                {
                    CreateEvent(calendar, movie.MovieMetadata, "physical");
                }
            }

            var serializer = new SerializerFactory().Build(calendar.GetType(), new SerializationContext()) as IStringSerializer;
            var icalendar = serializer?.SerializeToString(calendar);

            return icalendar is null ? NoContent() : Content(icalendar, "text/calendar");
        }

        private void CreateEvent(Ical.Net.Calendar calendar, MovieMetadata movie, string releaseType)
        {
            var date = movie.InCinemas;
            var eventType = "_cinemas";
            var summaryText = "(Theatrical Release)";

            if (releaseType == "digital")
            {
                date = movie.DigitalRelease;
                eventType = "_digital";
                summaryText = "(Digital Release)";
            }
            else if (releaseType == "physical")
            {
                date = movie.PhysicalRelease;
                eventType = "_physical";
                summaryText = "(Physical Release)";
            }

            if (!date.HasValue)
            {
                return;
            }

            var occurrence = calendar.Create<CalendarEvent>();
            occurrence.Uid = "NzbDrone_movie_" + movie.Id + eventType;
            occurrence.Status = movie.Status == MovieStatusType.Announced ? EventStatus.Tentative : EventStatus.Confirmed;

            occurrence.Start = new CalDateTime(date.Value, false);
            occurrence.End = new CalDateTime(date.Value.AddDays(1), false);

            occurrence.Description = movie.Overview;
            occurrence.Categories = new List<string> { movie.Studio };

            occurrence.Summary = $"{movie.Title} {summaryText}";
        }
    }
}

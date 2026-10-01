using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Queue;

namespace Radarr.Http.Mcp
{
    [McpServerToolType]
    public class RadarrMcpTools
    {
        private readonly IMovieService _movieService;
        private readonly IQueueService _queueService;
        private readonly IHealthCheckService _healthCheckService;

        public RadarrMcpTools(IMovieService movieService,
                              IQueueService queueService,
                              IHealthCheckService healthCheckService)
        {
            _movieService = movieService;
            _queueService = queueService;
            _healthCheckService = healthCheckService;
        }

        [McpServerTool(Name = "get_movies")]
        [Description("List all movies in the library")]
        public object GetMovies()
        {
            return _movieService.GetAllMovies()
                .OrderBy(m => m.Title)
                .Select(m => new { m.Id, m.Title, m.Year, m.TmdbId, m.HasFile })
                .ToList();
        }

        [McpServerTool(Name = "get_movie")]
        [Description("Get details for a single movie by id")]
        public object GetMovie([Description("Movie id")] int id)
        {
            var movie = _movieService.GetMovie(id);

            if (movie == null)
            {
                return null;
            }

            return new { movie.Id, movie.Title, movie.Year, movie.TmdbId, movie.HasFile, movie.Path };
        }

        [McpServerTool(Name = "get_queue")]
        [Description("List the active download queue")]
        public object GetQueue()
        {
            return _queueService.GetQueue()
                .Select(q => new
                {
                    q.Id,
                    q.Title,
                    Status = q.Status.ToString(),
                    q.Size,
                    q.SizeLeft,
                    q.TimeLeft,
                    Protocol = q.Protocol.ToString()
                })
                .ToList();
        }

        [McpServerTool(Name = "get_health")]
        [Description("List current health check results")]
        public object GetHealth()
        {
            return _healthCheckService.Results()
                .Select(h => new { Type = h.Type.ToString(), Reason = h.Reason.ToString(), h.Message })
                .ToList();
        }
    }
}

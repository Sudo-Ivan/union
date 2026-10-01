using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Crypto;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.MediaFiles.EpisodeImport.Manual;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using Sonarr.Api.V3.CustomFormats;
using Sonarr.Api.V3.Episodes;
using Sonarr.Api.V3.Series;
using Sonarr.Http.REST;
using MovieManualImportItem = NzbDrone.Core.MediaFiles.MovieImport.Manual.ManualImportItem;

namespace Sonarr.Api.V3.ManualImport
{
    public class ManualImportResource : RestResource
    {
        public string Path { get; set; }
        public string RelativePath { get; set; }
        public string FolderName { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public SeriesResource Series { get; set; }
        public int? SeasonNumber { get; set; }
        public List<EpisodeResource> Episodes { get; set; }
        public int? EpisodeFileId { get; set; }
        public ManualImportMovieResource Movie { get; set; }
        public int? MovieFileId { get; set; }
        public string ReleaseGroup { get; set; }
        public QualityModel Quality { get; set; }
        public List<Language> Languages { get; set; }
        public int QualityWeight { get; set; }
        public string DownloadId { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public int IndexerFlags { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public IEnumerable<ImportRejectionResource> Rejections { get; set; }
    }

    public static class ManualImportResourceMapper
    {
        public static ManualImportResource ToResource(this ManualImportItem model)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = model.CustomFormats;
            var customFormatScore = model.Series?.QualityProfile?.Value?.CalculateCustomFormatScore(customFormats) ?? 0;

            return new ManualImportResource
            {
                Id = HashConverter.GetHashInt31(model.Path),
                Path = model.Path,
                RelativePath = model.RelativePath,
                FolderName = model.FolderName,
                Name = model.Name,
                Size = model.Size,
                Series = model.Series.ToResource(),
                SeasonNumber = model.SeasonNumber,
                Episodes = model.Episodes.ToResource(),
                EpisodeFileId = model.EpisodeFileId,
                ReleaseGroup = model.ReleaseGroup,
                Quality = model.Quality,
                Languages = model.Languages,
                CustomFormats = customFormats.ToResource(false),
                CustomFormatScore = customFormatScore,

                // QualityWeight
                DownloadId = model.DownloadId,
                IndexerFlags = model.IndexerFlags,
                ReleaseType = model.ReleaseType,
                Rejections = model.Rejections.Select(r => r.ToResource())
            };
        }

        public static List<ManualImportResource> ToResource(this IEnumerable<ManualImportItem> models)
        {
            return models.Select(ToResource).ToList();
        }

        public static ManualImportResource ToResource(this MovieManualImportItem model)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = model.CustomFormats;
            var customFormatScore = model.Movie?.QualityProfile?.CalculateCustomFormatScore(customFormats) ?? 0;

            return new ManualImportResource
            {
                Id = HashConverter.GetHashInt31(model.Path),
                Path = model.Path,
                RelativePath = model.RelativePath,
                FolderName = model.FolderName,
                Name = model.Name,
                Size = model.Size,
                Movie = model.Movie.ToMovieResource(),
                MovieFileId = model.MovieFileId,
                ReleaseGroup = model.ReleaseGroup,
                Quality = model.Quality,
                Languages = model.Languages,
                CustomFormats = customFormats.ToResource(false),
                CustomFormatScore = customFormatScore,

                // QualityWeight
                DownloadId = model.DownloadId,
                IndexerFlags = model.IndexerFlags,
                Rejections = model.Rejections.Select(r => r.ToResource())
            };
        }

        public static List<ManualImportResource> ToResource(this IEnumerable<MovieManualImportItem> models)
        {
            return models.Select(ToResource).ToList();
        }
    }

    public class ManualImportMovieResource
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public int TmdbId { get; set; }
        public string ImdbId { get; set; }
    }

    public static class ManualImportMovieResourceMapper
    {
        public static ManualImportMovieResource ToMovieResource(this NzbDrone.Core.Movies.Movie model)
        {
            if (model == null)
            {
                return null;
            }

            return new ManualImportMovieResource
            {
                Id = model.Id,
                Title = model.Title,
                Year = model.Year,
                TmdbId = model.TmdbId,
                ImdbId = model.ImdbId
            };
        }
    }

    public class ImportRejectionResource
    {
        public string Reason { get; set; }
        public RejectionType Type { get; set; }
    }

    public static class ImportRejectionResourceMapper
    {
        public static ImportRejectionResource ToResource(this ImportRejection rejection)
        {
            if (rejection == null)
            {
                return null;
            }

            return new ImportRejectionResource
            {
                Reason = rejection.Message,
                Type = rejection.Type
            };
        }

        public static ImportRejectionResource ToResource(this NzbDrone.Core.MediaFiles.MovieImport.ImportRejection rejection)
        {
            if (rejection == null)
            {
                return null;
            }

            return new ImportRejectionResource
            {
                Reason = rejection.Message,
                Type = rejection.Type
            };
        }
    }
}

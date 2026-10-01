using System.Linq;
using NLog;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Indexers;
using EpisodeImportResult = NzbDrone.Core.MediaFiles.EpisodeImport.ImportResult;
using EpisodeImportResultType = NzbDrone.Core.MediaFiles.EpisodeImport.ImportResultType;
using EpisodeImportRejectionReason = NzbDrone.Core.MediaFiles.EpisodeImport.ImportRejectionReason;
using MovieImportResult = NzbDrone.Core.MediaFiles.MovieImport.ImportResult;
using MovieImportResultType = NzbDrone.Core.MediaFiles.MovieImport.ImportResultType;
using MovieImportRejectionReason = NzbDrone.Core.MediaFiles.MovieImport.ImportRejectionReason;

namespace NzbDrone.Core.Download;

public interface IRejectedImportService
{
    bool Process(TrackedDownload trackedDownload, EpisodeImportResult importResult);
    bool Process(TrackedDownload trackedDownload, MovieImportResult importResult);
}

public class RejectedImportService : IRejectedImportService
{
    private readonly ICachedIndexerSettingsProvider _cachedIndexerSettingsProvider;
    private readonly Logger _logger;

    public RejectedImportService(ICachedIndexerSettingsProvider cachedIndexerSettingsProvider, Logger logger)
    {
        _cachedIndexerSettingsProvider = cachedIndexerSettingsProvider;
        _logger = logger;
    }

    public bool Process(TrackedDownload trackedDownload, EpisodeImportResult importResult)
    {
        if (importResult.Result != EpisodeImportResultType.Rejected || trackedDownload.RemoteEpisode?.Release == null)
        {
            return false;
        }

        var indexerSettings = _cachedIndexerSettingsProvider.GetSettings(trackedDownload.RemoteEpisode.Release.IndexerId);
        var rejectionReason = importResult.ImportDecision.Rejections.FirstOrDefault()?.Reason;

        return ProcessRejection(trackedDownload, indexerSettings, rejectionReason, importResult.Errors);
    }

    public bool Process(TrackedDownload trackedDownload, MovieImportResult importResult)
    {
        if (importResult.Result != MovieImportResultType.Rejected || trackedDownload.RemoteMovie?.Release == null)
        {
            return false;
        }

        var indexerSettings = _cachedIndexerSettingsProvider.GetSettings(trackedDownload.RemoteMovie.Release.IndexerId);
        var rejectionReason = importResult.ImportDecision.Rejections.FirstOrDefault()?.Reason;

        return ProcessRejection(trackedDownload, indexerSettings, rejectionReason, importResult.Errors);
    }

    private bool ProcessRejection(TrackedDownload trackedDownload, CachedIndexerSettings indexerSettings, object rejectionReason, System.Collections.Generic.List<string> errors)
    {
        if (indexerSettings == null)
        {
            trackedDownload.Warn(new TrackedDownloadStatusMessage(trackedDownload.DownloadItem.Title, errors));
            return true;
        }

        var isDangerous = rejectionReason?.Equals(EpisodeImportRejectionReason.DangerousFile) == true ||
                          rejectionReason?.Equals(MovieImportRejectionReason.DangerousFile) == true;
        var isExecutable = rejectionReason?.Equals(EpisodeImportRejectionReason.ExecutableFile) == true ||
                           rejectionReason?.Equals(MovieImportRejectionReason.ExecutableFile) == true;
        var isUserRejectedExtension = rejectionReason?.Equals(EpisodeImportRejectionReason.UserRejectedExtension) == true;

        if (isDangerous && indexerSettings.FailDownloads.Contains(FailDownloads.PotentiallyDangerous))
        {
            _logger.Trace("Download '{0}' contains potentially dangerous file, marking as failed", trackedDownload.DownloadItem.Title);
            trackedDownload.Fail();
        }
        else if (isExecutable && indexerSettings.FailDownloads.Contains(FailDownloads.Executables))
        {
            _logger.Trace("Download '{0}' contains executable file, marking as failed", trackedDownload.DownloadItem.Title);
            trackedDownload.Fail();
        }
        else if (isUserRejectedExtension && indexerSettings.FailDownloads.Contains(FailDownloads.UserDefinedExtensions))
        {
            _logger.Trace("Download '{0}' contains user defined rejected file extension, marking as failed", trackedDownload.DownloadItem.Title);
            trackedDownload.Fail();
        }
        else
        {
            trackedDownload.Warn(new TrackedDownloadStatusMessage(trackedDownload.DownloadItem.Title, errors));
        }

        return true;
    }
}

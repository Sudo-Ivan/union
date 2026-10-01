using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ImportLists.ImportListMovies;

namespace NzbDrone.Core.ImportLists.TMDb.Person
{
    public class TMDbPersonImport : TMDbImportListBase<TMDbPersonSettings>
    {
        public TMDbPersonImport(IRadarrCloudRequestBuilder requestBuilder,
                                 IHttpClient httpClient,
                                 IImportListStatusService importListStatusService,
                                 IConfigService configService,
                                 IParsingService parsingService,
                                 ISearchForNewMovie searchForNewMovie,
                                 ILocalizationService localizationService,
                                 Logger logger)
        : base(requestBuilder, httpClient, importListStatusService, configService, parsingService, searchForNewMovie, localizationService, logger)
        {
        }

        public override string Name => "TMDb Person";
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public override IParseImportListResponse<ImportListMovie> GetParser()
        {
            return new TMDbPersonParser(Settings);
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new TMDbPersonRequestGenerator()
            {
                RequestBuilder = _requestBuilder,
                Settings = Settings,
                Logger = _logger,
                HttpClient = _httpClient
            };
        }
    }
}

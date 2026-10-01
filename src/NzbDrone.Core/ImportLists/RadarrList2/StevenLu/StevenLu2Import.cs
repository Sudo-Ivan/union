using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ImportLists.ImportListMovies;

namespace NzbDrone.Core.ImportLists.RadarrList2.StevenLu
{
    public class StevenLu2Import : HttpImportListMovieBase<StevenLu2Settings>
    {
        private readonly IHttpRequestBuilderFactory _radarrMetadata;

        public override string Name => "StevenLu List";

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public StevenLu2Import(IRadarrCloudRequestBuilder requestBuilder,
                              IHttpClient httpClient,
                              IImportListStatusService importListStatusService,
                              IConfigService configService,
                              IParsingService parsingService,
                              ILocalizationService localizationService,
                              Logger logger)
        : base(httpClient, importListStatusService, configService, parsingService, localizationService, logger)
        {
            _radarrMetadata = requestBuilder.RadarrMetadata;
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new StevenLu2RequestGenerator()
            {
                Settings = Settings,
                Logger = _logger,
                HttpClient = _httpClient,
                RequestBuilder = _radarrMetadata
            };
        }

        public override IParseImportListResponse<ImportListMovie> GetParser()
        {
            return new RadarrList2Parser();
        }
    }
}

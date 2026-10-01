using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ImportLists.ImportListMovies;

namespace NzbDrone.Core.ImportLists.StevenLu
{
    public class StevenLuImport : HttpImportListMovieBase<StevenLuSettings>
    {
        public override string Name => "StevenLu Custom";

        public override ImportListType ListType => ImportListType.Advanced;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(24);
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public StevenLuImport(IHttpClient httpClient,
                              IImportListStatusService importListStatusService,
                              IConfigService configService,
                              IParsingService parsingService,
                              ILocalizationService localizationService,
                              Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, localizationService, logger)
        {
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new StevenLuRequestGenerator() { Settings = Settings };
        }

        public override IParseImportListResponse<ImportListMovie> GetParser()
        {
            return new StevenLuParser();
        }
    }
}

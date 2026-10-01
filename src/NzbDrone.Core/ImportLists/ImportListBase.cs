using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListFetchResult
    {
        public ImportListFetchResult()
        {
            Series = new List<ImportListItemInfo>();
            Movies = new List<ImportListMovie>();
        }

        public ImportListFetchResult(IEnumerable<ImportListItemInfo> series, bool anyFailure)
        {
            Series = series.ToList();
            Movies = new List<ImportListMovie>();
            AnyFailure = anyFailure;
        }

        public ImportListFetchResult(List<ImportListMovie> movies, bool anyFailure)
        {
            Series = new List<ImportListItemInfo>();
            Movies = movies;
            AnyFailure = anyFailure;
        }

        public List<ImportListItemInfo> Series { get; set; }
        public List<ImportListMovie> Movies { get; set; }
        public bool AnyFailure { get; set; }
        public int SyncedLists { get; set; }
    }

    public abstract class ImportListBase<TSettings> : IImportList
        where TSettings : IImportListSettings, new()
    {
        protected readonly IImportListStatusService _importListStatusService;
        protected readonly IConfigService _configService;
        protected readonly IParsingService _parsingService;
        protected readonly ILocalizationService _localizationService;
        protected readonly Logger _logger;

        public abstract string Name { get; }

        public abstract ImportListType ListType { get; }

        public abstract TimeSpan MinRefreshInterval { get; }

        // Radarr-style providers can gate themselves; Sonarr-style providers use
        // EnableAutomaticAdd on the definition.
        public virtual bool Enabled => true;
        public virtual bool EnableAuto => false;

        public abstract ImportListFetchResult Fetch();

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                var config = (IProviderConfig)new TSettings();

                if (config.Validate().IsValid)
                {
                    yield return new ImportListDefinition
                    {
                        EnableAutomaticAdd = true,
                        Enabled = Enabled,
                        EnableAuto = true,
                        Implementation = GetType().Name,
                        Settings = config
                    };
                }
            }
        }

        public virtual ProviderDefinition Definition { get; set; }

        protected ImportListBase(IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, ILocalizationService localizationService, Logger logger)
        {
            _importListStatusService = importListStatusService;
            _configService = configService;
            _parsingService = parsingService;
            _localizationService = localizationService;
            _logger = logger;
        }

        public virtual ProviderMessage RequestInactiveAction()
        {
            return null;
        }

        protected TSettings Settings => (TSettings)Definition.Settings;

        protected virtual IList<ImportListItemInfo> CleanupListItems(IEnumerable<ImportListItemInfo> releases)
        {
            var result = releases.DistinctBy(r => new { r.Title, r.TvdbId, r.ImdbId }).ToList();

            result.ForEach(c =>
            {
                c.ImportListId = Definition.Id;
                c.ImportList = Definition.Name;
            });

            return result;
        }

        protected virtual List<ImportListMovie> CleanupListItems(IEnumerable<ImportListMovie> listMovies)
        {
            var result = listMovies.ToList();

            result.ForEach(c =>
            {
                c.ListId = Definition.Id;
            });

            return result;
        }

        public ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                Test(failures);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Test aborted due to exception");
                failures.Add(new ValidationFailure(string.Empty, _localizationService.GetLocalizedString("ImportListsValidationTestFailed", new Dictionary<string, object> { { "exceptionMessage", ex.Message } })));
            }

            return new ValidationResult(failures);
        }

        protected virtual void Test(List<ValidationFailure> failures)
        {
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }
    }
}

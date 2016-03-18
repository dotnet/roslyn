using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.VsHub.ServiceModulesCommon;
using Microsoft.VsHub.Services;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    public partial class SymbolSearchController : JsonController
    {
        private static ConcurrentDictionary<string, AddReferenceDatabase> _sourceToDatabase = 
            new ConcurrentDictionary<string, AddReferenceDatabase>();

        private readonly IPackageSearchDelayService _delayService;
        private readonly IPackageSearchIOService _ioService;
        private readonly IPackageSearchLogService _logService;
        private readonly IPackageSearchRemoteControlService _remoteControlService;
        private readonly IPackageSearchPatchService _patchService;
        private readonly IPackageSearchDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, bool> _reportAndSwallowException;

        public SymbolSearchController()
            : this(new RemoteControlService(),
                   new LogService(ServiceModulesUtilities.GetService<ILogger>()),
                   new DelayService(),
                   new IOService(),
                   new PatchService(),
                   new DatabaseFactoryService(),
                   // Report all exceptions we encounter, but don't crash on them.
                   FatalError.ReportWithoutCrash,
                   new CancellationTokenSource())
        {
            ServiceModulesUtilities.GetService<IServiceLifetime>().Stopped += OnServiceStopped;
        }

        // For testing purposes.
        internal SymbolSearchController(
            IPackageSearchRemoteControlService remoteControlService,
            IPackageSearchLogService logService,
            IPackageSearchDelayService delayService,
            IPackageSearchIOService ioService,
            IPackageSearchPatchService patchService,
            IPackageSearchDatabaseFactoryService databaseFactoryService,
            Func<Exception, bool> reportAndSwallowException,
            CancellationTokenSource cancellationTokenSource)
        {
            _delayService = delayService;
            _ioService = ioService;
            _logService = logService;
            _remoteControlService = remoteControlService;
            _patchService = patchService;
            _databaseFactoryService = databaseFactoryService;
            _reportAndSwallowException = reportAndSwallowException;

            _cancellationTokenSource = cancellationTokenSource;
            _cancellationToken = _cancellationTokenSource.Token;
        }

        private void OnServiceStopped(object sender, EventArgs e)
        {
            // VSHub is stopping.  Stop all work we're currently doing.
            _cancellationTokenSource.Cancel();
            _sourceToDatabase.Clear();
        }

        private void LogInfo(string text) => _logService.LogInfo(text);

        private void LogException(Exception e, string text) => _logService.LogException(e, text);

        [HttpPost]
        [Route(WellKnownHubServiceNames.SymbolSearch + "/" + nameof(HubProtocolConstants.CancelOperationName))]
        public new void CancelOperation(HubDataModel value)
        {
            base.CancelOperation(value);
        }

        [HttpPost]
        [Route(WellKnownHubServiceNames.SymbolSearch + "/" + nameof(OnConfigurationChanged))]
        public HttpResponseMessage OnConfigurationChanged(HubDataModel model)
        {
            return base.ProcessRequest<JObject>(model, (obj, c) =>
            {
                var cacheDirectory = (string)obj.Property(HubProtocolConstants.CacheDirectoryName);
                var sourceNames = obj.Property(HubProtocolConstants.PackageSourcesName).Value
                                     .OfType<JObject>()
                                     .Select(o => o.Properties().First().Name);

                foreach (var sourceName in sourceNames)
                {
                    Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ =>
                        UpdateSourceInBackgroundAsync(cacheDirectory, sourceName), TaskScheduler.Default);
                }

                return new JObject();
            });
        }

#if false
        [HttpGet]
        [Route("SymbolSearch/FindPackagesWithType/{queryJson}")]
        public string FindPackagesWithType(string queryJson)
        {
            var query = JObject.Parse(queryJson);
            return new JObject().ToString();
        }

        [HttpGet]
        [Route("SymbolSearch/FindReferenceAssembliesWithType/{queryJson}")]
        public string FindReferenceAssembliesWithType(string queryJson)
        {
            var query = JObject.Parse(queryJson);
            return new JObject().ToString();
        }
#endif
    }
}
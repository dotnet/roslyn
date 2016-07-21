using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Setup;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [Export(typeof(IRoslynTelemetrySetup))]
    internal class RoslynTelemetrySetup : IRoslynTelemetrySetup
    {
        public void Initialize(IServiceProvider serviceProvider)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();

            // initialize host context on UI thread.
            var projectTypeLookup = workspace.Services.GetService<IProjectTypeLookupService>();

            // set initial logger
            var optionService = workspace.Services.GetService<IOptionService>();
            var loggingChecker = Logger.GetLoggingChecker(optionService);

            var telemetryService = serviceProvider.GetService(typeof(SVsTelemetryService)) as IVsTelemetryService;
            Logger.SetLogger(
                AggregateLogger.Create(
                    CodeMarkerLogger.Instance,
                    new EtwLogger(loggingChecker),
                    new VSTelemetryLogger(telemetryService),
                    new VSTelemetryActivityLogger(telemetryService),
                    Logger.GetLogger()));

            Logger.Log(FunctionId.Run_Environment, KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));
        }
    }
}

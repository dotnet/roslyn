using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal static class RoslynTelemetrySetup
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            var optionService = componentModel.GetService<IGlobalOptionService>();

            // Fetch the session synchronously on the UI thread; if this doesn't happen before we try using this on
            // the background thread then we will experience hangs like we see in this bug:
            // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=190808 or
            // https://devdiv.visualstudio.com/DevDiv/_workitems?id=296981&_a=edit
            var unused1 = TelemetryHelper.TelemetryService;
            var unused2 = TelemetryHelper.DefaultTelemetrySession;

            var telemetryService = serviceProvider.GetService(typeof(SVsTelemetryService)) as IVsTelemetryService;
            Logger.SetLogger(
                AggregateLogger.Create(
                    CodeMarkerLogger.Instance,
                    new EtwLogger(optionService),
                    new VSTelemetryLogger(telemetryService),
                    new VSTelemetryActivityLogger(telemetryService),
                    Logger.GetLogger()));

            Logger.Log(FunctionId.Run_Environment, KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));
        }
    }
}

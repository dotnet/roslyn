using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal static class RoslynTelemetrySetup
    {
        public static void Initialize(IServiceProvider serviceProvider, TelemetrySession session)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            var optionService = componentModel.GetService<IGlobalOptionService>();

            Logger.SetLogger(
                AggregateLogger.Create(
                    CodeMarkerLogger.Instance,
                    new EtwLogger(optionService),
                    new VSTelemetryLogger(session),
                    Logger.GetLogger()));

            Logger.Log(FunctionId.Run_Environment, KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));
        }
    }
}

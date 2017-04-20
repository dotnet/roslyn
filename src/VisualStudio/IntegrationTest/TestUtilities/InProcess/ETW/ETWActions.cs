using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    public class ETWActions
    {
        public static void StartETWListener(VisualStudioInstance instance)
        {
            TraceEventMonitor.StartListener(instance.HostProcess);
        }

        public static void StopETWListener(VisualStudioInstance instance)
        {
            TraceEventMonitor.StopListener();
        }

        public static void WaitForSolutionCrawler(VisualStudioInstance instance)
        {
            var @event = Microsoft.Diagnostics.Tracing.Parsers.RoslynEventSource.FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem.ToString();
            var option = FunctionIdOptions.GetOption(FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem);
            instance.Workspace.SetOption(option.Name, option.Feature, true);
            TraceEventMonitor.StartListening(@event);
            TraceEventMonitor.WaitFor(@event);
        }

        public static void ForceGC(VisualStudioInstance instance)
        {
            instance.ExecuteCommand("Tools.ForceGC");
            instance.ExecuteCommand("Tools.ForceGC");
        }

        public static void WaitForIdleCPU()
        {
            WaitForIdleCPUAction.Execute();
        }
    }
}

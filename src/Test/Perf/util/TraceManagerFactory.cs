using System.IO;

namespace Roslyn.Test.Performance.Utilities
{
    internal class TraceManagerFactory
    {
        public static ITraceManager GetTraceManager(bool isVerbose = false)
        {
            var cpcFullPath = Path.Combine(TestUtilities.GetCPCDirectoryPath(), "CPC.exe");
            var scenarioPath = TestUtilities.GetCPCDirectoryPath();
            if (File.Exists(cpcFullPath))
            {
                return new TraceManager(
                    cpcFullPath,
                    scenarioPath,
                    verbose: isVerbose,
                    logger: isVerbose ? new ConsoleAndFileLogger() : null);
            }
            else
            {
                return new NoOpTraceManager();
            }
        }
    }
}

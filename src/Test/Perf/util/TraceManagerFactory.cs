using System.IO;

namespace Roslyn.Test.Performance.Utilities
{
    public class TraceManagerFactory
    {
        public static ITraceManager GetTraceManager(int iterations = 1)
        {
            var cpcFullPath = Path.Combine(TestUtilities.GetCPCDirectoryPath(), "CPC.exe");
            var scenarioPath = TestUtilities.GetCPCDirectoryPath();
            if (File.Exists(cpcFullPath))
            {
                return new TraceManager(iterations, cpcFullPath, scenarioPath, verbose: false, logger: null);
            }
            else
            {
                return new NoOpTraceManager(iterations);
            }
        }
    }
}

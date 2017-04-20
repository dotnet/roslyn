using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class DelayTracker : IDisposable
    {
        private static int fileCount = 1;
        private string outputPath;
        private string applicationPath;
        private string testName;

        private const string argsTemplate = @"/start /log:{0}\TypingResponsiveness.log.{1}.{2}.{3}.txt /result:{0}\TypingResponsiveness.result.{1}.{2}.{3}.xml  /csv:{0}\TypingResponsiveness.log.{1}.{2}.{3}.csv";
        private Process process;

        public DelayTracker(string outputPath, string applicationPath, string testName)
        {
            this.outputPath = outputPath;
            this.applicationPath = applicationPath;
            this.testName = testName;
        }

        public static DelayTracker Start(string outputPath, string applicationPath, string testName="NoName")
        {
            var tracker = new DelayTracker(outputPath, applicationPath, testName);
            tracker.Start();
            return tracker;
        }

        private void Start()
        {
            fileCount++;
            var args = string.Format(argsTemplate, Path.Combine(outputPath, "PerfResults"), testName, "Roslyn", fileCount);
            var startinfo = new ProcessStartInfo(applicationPath, args);
            startinfo.UseShellExecute = false;
            startinfo.CreateNoWindow = true;
            this.process = Process.Start(startinfo);
        }

        public void Dispose()
        {
            // Wait a little before and after to allow everything to wrap up
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Process.Start(applicationPath, "/stop");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

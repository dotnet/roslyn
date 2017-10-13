using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.VisualStudio.IntegrationTests.Performance
{
    internal class PerformanceTestUtilities
    {
        public static void DownloadProject(string name, int version, string targetDirectory)
        {
            var zipFileName = $"{name}.{version}.zip";
            var zipPath = Path.Combine(targetDirectory, zipFileName);
            // If we've already downloaded the zip, assume that it
            // has been downloaded *and* extracted.
            if (File.Exists(zipPath))
            {
                return;
            }

            // Remove all .zip files that were downloaded before.
            foreach (var path in Directory.EnumerateFiles(targetDirectory, $"{name}.*.zip"))
            {
                File.Delete(path);
            }

            // Download zip file to temp directory
            var downloadTarget = $"https://dotnetci.blob.core.windows.net/roslyn-perf/{zipFileName}";
            var client = new WebClient();
            client.DownloadFile(downloadTarget, zipPath);

            // Extract to temp directory
            ZipFile.ExtractToDirectory(zipPath, targetDirectory);
        }
    }
}

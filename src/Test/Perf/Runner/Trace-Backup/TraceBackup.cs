using System;
using System.Linq;
using static Roslyn.Test.Performance.Runner.Tools;
using static Roslyn.Test.Performance.Runner.Benchview;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using System.IO;

namespace Roslyn.Test.Performance.Runner
{
    class TraceBackup
    {
        private const string TraceDestination = @"\\mlangfs1\public\basoundr\PerfTraces";
        public static void UploadTraces()
        {
            UploadTraces(CPCDirectoryPath, TraceDestination);
        }

        private static void UploadTraces(string sourceFolderPath, string destinationFolderPath)
        {
            if (Directory.Exists(sourceFolderPath))
            {
                var directoriesToUpload = new DirectoryInfo(sourceFolderPath).GetDirectories("DataBackup*");
                if (directoriesToUpload.Count() == 0)
                {
                    Log($"There are no trace directory starting with DataBackup in {sourceFolderPath}");
                    return;
                }

                var perfResultDestinationFolderName = string.Format("PerfResults-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);

                var destination = Path.Combine(destinationFolderPath, perfResultDestinationFolderName);
                foreach (var directoryToUpload in directoriesToUpload)
                {
                    var destinationDataBackupDirectory = Path.Combine(destination, directoryToUpload.Name);
                    if (Directory.Exists(destinationDataBackupDirectory))
                    {
                        Directory.CreateDirectory(destinationDataBackupDirectory);
                    }

                    CopyDirectory(directoryToUpload.FullName, destinationDataBackupDirectory);
                }

                foreach (var file in new DirectoryInfo(sourceFolderPath).GetFiles().Where(f => f.Name.StartsWith("ConsumptionTemp", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("Roslyn-", StringComparison.OrdinalIgnoreCase)))
                {
                    File.Copy(file.FullName, Path.Combine(destination, file.Name));
                }
            }
            else
            {
                Log($"sourceFolderPath: {sourceFolderPath} does not exist");
            }
        }
    }
}

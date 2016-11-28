// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Linq;
using static Roslyn.Test.Performance.Runner.Tools;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using System.IO;

namespace Roslyn.Test.Performance.Runner
{
    public static class TraceBackup
    {
        public static void UploadTraces(string sourceFolderPath, string destinationFolderPath)
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

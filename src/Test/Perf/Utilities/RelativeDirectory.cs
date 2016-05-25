// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;

namespace Roslyn.Test.Performance.Utilities
{
    public class RelativeDirectory
    {
        string _workingDir;

        public RelativeDirectory([CallerFilePath] string workingFile = "")
        {
            _workingDir = Directory.GetParent(workingFile).FullName;
        }

        public string MyWorkingDirectory => _workingDir;

        public string TempDirectory
        {
            get
            {
                var tempDirectory = Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\PerfTemp");
                Directory.CreateDirectory(tempDirectory);
                return tempDirectory;
            }
        }

        public string RoslynDirectory
        {
            get
            {
                var workingDir = MyWorkingDirectory;
                var binaryDebug = Path.Combine("Binaries", "Debug").ToString();
                int binaryDebugIndex = workingDir.IndexOf(binaryDebug, StringComparison.OrdinalIgnoreCase);
                if (binaryDebugIndex != -1)
                {
                    return workingDir.Substring(0, binaryDebugIndex);
                }

                var binaryRelease = Path.Combine("Binaries", "Release").ToString();
                return workingDir.Substring(0, workingDir.IndexOf(binaryRelease, StringComparison.OrdinalIgnoreCase));
            }
        }

        public string MyBinaries()
        {
            // The exceptation is that scripts calling this are included
            // in a project in the solution and have already been deployed
            // to a binaries folder

            // Debug?
            var debug = "debug";
            var debugIndex = _workingDir.IndexOf(debug, StringComparison.CurrentCultureIgnoreCase);
            if (debugIndex != -1)
            {
                return _workingDir.Substring(0, debugIndex + debug.Length);
            }

            // Release?
            var release = "release";
            var releaseIndex = _workingDir.IndexOf(release, StringComparison.CurrentCultureIgnoreCase);
            if (releaseIndex != -1)
            {
                return _workingDir.Substring(0, releaseIndex + release.Length);
            }

            throw new Exception("Couldn't find binaries. Are you running from the binaries directory?");
        }

        public string TaoPath => Path.Combine(MyBinaries(), "Tao");

        public string CPCDirectoryPath
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC");
            }
        }

        public string GetViBenchToJsonExeFilePath => Path.Combine(CPCDirectoryPath, "ViBenchToJson.exe");



        /// Downloads a zip from azure store and extracts it into
        /// the ./temp directory.
        ///
        /// If this current version has already been downloaded
        /// and extracted, do nothing.
        public void DownloadProject(string name, int version, ILogger logger)
        {
            var zipFileName = $"{name}.{version}.zip";
            var zipPath = Path.Combine(TempDirectory, zipFileName);
            // If we've already downloaded the zip, assume that it
            // has been downloaded *and* extracted.
            if (File.Exists(zipPath))
            {
                logger.Log($"Didn't download and extract {zipFileName} because one already exists.");
                return;
            }

            // Remove all .zip files that were downloaded before.
            foreach (var path in Directory.EnumerateFiles(TempDirectory, $"{name}.*.zip"))
            {
                logger.Log($"Removing old zip {path}");
                File.Delete(path);
            }

            // Download zip file to temp directory
            var downloadTarget = $"https://dotnetci.blob.core.windows.net/roslyn-perf/{zipFileName}";
            logger.Log($"Downloading {downloadTarget}");
            var client = new WebClient();
            client.DownloadFile(downloadTarget, zipPath);
            logger.Log($"Done Downloading");

            // Extract to temp directory
            logger.Log($"Extracting {zipPath} to {TempDirectory}");
            ZipFile.ExtractToDirectory(zipPath, TempDirectory);
            logger.Log($"Done Extracting");
        }
    }
}

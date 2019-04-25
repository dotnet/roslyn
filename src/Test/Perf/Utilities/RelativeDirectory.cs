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
        private string _workingDir;


        public RelativeDirectory()
        {
        }

        public RelativeDirectory(string workingDir)
        {
            _workingDir = workingDir;
        }

        public void SetWorkingDirectory(string workingDir)
        {
            _workingDir = workingDir;
        }

        private void ThrowIfNotSetup()
        {
            if (_workingDir == null)
            {
                throw new InvalidOperationException("The test has not been set up correctly.  Avoid doing any directory operations in the constructor.");
            }
        }

        /// <summary>
        /// Returns the current working directory that the test has access to.  
        /// This is typically the same directory as the script is located in.
        /// </summary>
        public string MyWorkingDirectory
        {
            get
            {
                ThrowIfNotSetup();
                return _workingDir;
            }
        }

        /// <summary>
        /// Returns a directory that the test can use for temporary file storage.
        /// </summary>
        public string TempDirectory
        {
            get
            {
                ThrowIfNotSetup();
                var tempDirectory = Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\PerfTemp");
                Directory.CreateDirectory(tempDirectory);
                return tempDirectory;
            }
        }

        /// <summary>
        /// Returns the directory that contains built roslyn binaries.  Usually this will be 
        /// Binaries/Debug or Binaries/Release.
        /// </summary>
        /// <returns></returns>
        public string MyBinaries()
        {
            ThrowIfNotSetup();
            // The exceptation is that scripts calling this are included
            // in a project in the solution and have already been deployed
            // to a binaries folder

            foreach (var configuration in new string[] { "debug", "release" })
            {
                var configurationIndex = _workingDir.IndexOf(configuration, StringComparison.CurrentCultureIgnoreCase);
                if (configurationIndex != -1)
                {
                    return _workingDir.Substring(0, configurationIndex + configuration.Length);
                }
            }

            throw new Exception("Couldn't find binaries. Are you running from the binaries directory?");
        }

        /// <returns>
        /// The path to TAO
        /// </returns>
        public string TaoPath => Path.Combine(MyBinaries(), "exes", "EditorTestApp", "Tao");

        /// Downloads a zip from azure store and extracts it into
        /// the ./temp directory.
        ///
        /// If this current version has already been downloaded
        /// and extracted, do nothing.
        public void DownloadProject(string name, int version, ILogger logger)
        {
            ThrowIfNotSetup();
            var zipFileName = $"{name}.{version}.zip";
            var zipPath = Path.Combine(TempDirectory, zipFileName);
            // If we've already downloaded the zip, assume that it
            // has been downloaded *and* extracted.
            if (File.Exists(zipPath))
            {
                logger.Log($"Didn't download and extract {zipFileName} because one already exists at {zipPath}.");
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

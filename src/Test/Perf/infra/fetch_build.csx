// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// CopyDirectory()
#load "../util/tools_util.csx"

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

// TODO: Use actual command line argument parser so we can have help text, etc...
var sourceFolder = Args[0];
var destinationFolder = Args.Count() == 2 ? Args[1] : @"C:\Roslyn\Binaries\Release";

// Get the last successful build that was tested
string lastSuccessfulBuild = null;
var lastSuccessfulDirectory = Environment.ExpandEnvironmentVariables($@"%SYSTEMDRIVE%\Last-Successful-{Path.GetFileName(sourceFolder)}");
var buildInfoPath = Path.Combine((lastSuccessfulDirectory), "BuildNumber.txt");

if (Directory.Exists(lastSuccessfulDirectory) && File.Exists(buildInfoPath))
{
    lastSuccessfulBuild = File.ReadAllText(buildInfoPath);
}

// Start monitoring the share for new successful builds
Task.Run(() => StartWatching(sourceFolder, destinationFolder, lastSuccessfulBuild, buildInfoPath));
Console.WriteLine("Press any key to continue");
Console.Read();

void StartWatching(
    string sourceFolder,
    string destinationFolder,
    string lastSuccessfulBuild,
    string buildInfoPath)
{
    string latestBuild = null;
    var lastSuccessfulBuildNumber = lastSuccessfulBuild == null ? 0 : Convert.ToInt32(Convert.ToDouble(lastSuccessfulBuild) * 100);
    while (true)
    {
        foreach (var folder in Directory.GetDirectories(sourceFolder, "????????.?").Reverse())
        {
            var buildDirectoryName = Path.GetFileName(folder);
            var buildDirectoryNumber = Convert.ToInt32(Convert.ToDouble(buildDirectoryName) * 100);

            // if we are no longer looking at the newer builds, then quit searching
            if (!(buildDirectoryNumber > lastSuccessfulBuildNumber))
            {
                break;
            }

            if (SanityTestPassesForBuild(folder))
            {
                // Copy the binaries from the drop to Release binaries
                latestBuild = folder;
                var latestBuildFolder = Path.Combine(sourceFolder, latestBuild);

                if (Directory.Exists(destinationFolder))
                {
                    Directory.Delete(destinationFolder, recursive: true);
                }

                CopyDirectory(latestBuildFolder, destinationFolder);

                // Start Automation
                // Fire and forget
                var processInfo = new ProcessStartInfo(@"TriggerAutomation.bat");
                processInfo.Arguments = $"{buildInfoPath} {buildDirectoryName} {sourceFolder}";
                var process = Process.Start(processInfo);

                // We dont want the process to be running when running the tests
                Process.GetCurrentProcess().Kill();
            }
        }

        // Wait for 10 minutes before looking for new build again
        Thread.Sleep(600000);
    }
}

bool SanityTestPassesForBuild(string buildPath)
{
    var testResultsFilePath = Directory.GetFiles(Path.Combine(buildPath, "logs"), "ActivityLog.AgentScope.*.xml").SingleOrDefault();
    if (testResultsFilePath == null)
    {
        return false;
    }

    var doc = new XmlDocument();
    doc.Load(testResultsFilePath);
    var logfileNodes = doc.SelectNodes("//BuildInformationNode[@Type='BuildError']");

    return logfileNodes.Count == 0;
}

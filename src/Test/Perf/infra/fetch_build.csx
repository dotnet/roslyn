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

Console.WriteLine("Kill the process or close the window to stop the Perf Test Automation");

while (true)
{
    try
    {
        // Get the last build that was tested
        string lastBuild = null;
        var perfRunStatusPath = Path.Combine((Environment.ExpandEnvironmentVariables("%USERPROFILE%")), "PerfRunStatus.txt");
        if (File.Exists(perfRunStatusPath))
        {
            lastBuild = File.ReadAllText(perfRunStatusPath);
        }

        StartWatching(sourceFolder, destinationFolder, lastBuild, perfRunStatusPath);

        // Wait for 10 minutes before looking for new build again
        Console.WriteLine($"Waiting for 10 minutes from {DateTime.Now} to look for new builds");
        Thread.Sleep(10 * 60 * 1000);
        Console.WriteLine($"Resume looking for new builds in {sourceFolder}");
    }
    // Dont let any exception to stop our Perf Automation
    catch (System.Exception e)
    {
        var logFilePath = Path.Combine((Environment.ExpandEnvironmentVariables("%USERPROFILE%")), "PerfRunErrorLog.txt");
        File.AppendAllLines(logFilePath, new[] { DateTime.Now.ToString()});
        File.AppendAllLines(logFilePath, new[] { e.Message});
        File.AppendAllLines(logFilePath, new[] { e.StackTrace});

        CleanupSystem();
    }
}

void CleanupSystem()
{
    var processNamesToKill = new[] {"csc", "vbc", "vbcscompiler", "devenv", "cpc" };
    System.Diagnostics.Process.GetProcesses()
        .Where(x => processNamesToKill.Any(p => x.ProcessName.ToLower().StartsWith(p)))
        .ToList()
        .ForEach(x => x.Kill());

    //Cleanup the registry entries by CPC to make it ready for the next run 
    var processInfo = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC\CPC.exe"));
    processInfo.Arguments = @"/DisableArchive /Cleanup";
    var process = Process.Start(processInfo);

    // Shouldn't take more than 1 minute to cleanup
    if(!process.WaitForExit(60000))
    {
        // If the CPC doesn't cleanup in a minute then we need to stop the automation and check the machine
        var logFilePath = Path.Combine((Environment.ExpandEnvironmentVariables("%USERPROFILE%")), "PerfRunErrorLog.txt");
        File.AppendAllLines(logFilePath, new[] { DateTime.Now.ToString()});
        File.AppendAllLines(logFilePath, new[] { "CPC did not get cleaned up within 1 minute. Hence shutting down automation" });

        Process.GetCurrentProcess().Kill();
    }
}

void StartWatching(
    string sourceFolder,
    string destinationFolder,
    string lastBuild,
    string perfRunStatusPath)
{
    var lastBuildVersion = lastBuild == null ? null : Version.Parse(lastBuild);
    foreach (var folder in Directory.GetDirectories(sourceFolder, "????????.?").Reverse())
    {
        var buildDirectoryName = Path.GetFileName(folder);
        var currentBuildVersion = Version.Parse(buildDirectoryName);

        // if we are no longer looking at the newer builds, then quit searching
        if (lastBuildVersion != null && currentBuildVersion <= lastBuildVersion)
        {
            break;
        }

        if (SanityTestPassesForBuild(folder))
        {
            // Copy the binaries from the drop to Release binaries
            var latestBuildFolder = Path.Combine(sourceFolder, folder);

            if (Directory.Exists(destinationFolder))
            {
                Directory.Delete(destinationFolder, recursive: true);
            }

            CopyDirectory(latestBuildFolder, destinationFolder);

            // The test files have been copied. Now is a good time to record the status for this build
            File.WriteAllText(perfRunStatusPath, buildDirectoryName);

            Console.WriteLine($"Starting tests for {latestBuildFolder}");
            // Start Automation
            var processInfo = new ProcessStartInfo(@"TriggerAutomation.bat");
            var process = Process.Start(processInfo);

            // Wait for 4 hours for the tests to complete 
            var runStatus = process.WaitForExit(4 * 60 * 60 * 60 * 1000);

            if (runStatus)
            {
                // Send a mail saying perf run passed
            }
            else
            {
                // Send a mail saying perf run failed
                CleanupSystem();
            }

            break;
        }
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

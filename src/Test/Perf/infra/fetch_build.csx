// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml;

// TODO: Use actual command line argument parser so we can have help text, etc...
var branch = Args.Length == 2 ? Args[0] : "master";
var destinationFolder = Args.Length == 2 ? Args[1] : @"C:\Roslyn\Binaries\Release";

var sourceFolder = $@"\\cpvsbuild\drops\Roslyn\Roslyn-{branch}-Signed-Release";

string latestBuild = null;
foreach (var folder in Directory.GetFiles(sourceFolder, "????????.?").Reverse())
{
    if (SanityTestPassesForBuild(folder))
    {
        latestBuild = folder;
        break;
    }
}

if (latestBuild == null)
{
    throw new InvalidOperationException($"Could not locate build with passing tests at location \"{sourceFolder}\".");
}

var latestBuildFolder = Path.Combine(sourceFolder, latestBuild);
Log($"Fetching build \"{latestBuildFolder}\".");

Directory.Delete(destinationFolder, recursive: true);

CopyDirectory(latestBuildFolder, destinationFolder);

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

void CopyDirectory(string source, string destination)
{
    var result = ShellOut("Robocopy", $"/s {source} {destination}");
    if (!result.Succeeded)
    {
        throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
    }
}

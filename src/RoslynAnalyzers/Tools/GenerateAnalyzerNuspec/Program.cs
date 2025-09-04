// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

if (args.Length != 22)
{
    for (var i = 0; i < args.Length; i++)
    {
        Console.WriteLine($"Arg {i}: {args[i]}");
    }
}

string nuspecFile = args[0];
string assetsDir = args[1];
string projectDir = args[2];
string configuration = args[3];
string[] tfms = args[4].Split(';');
var metadataList = args[5].Split([';'], StringSplitOptions.RemoveEmptyEntries);
var fileList = args[6].Split([';'], StringSplitOptions.RemoveEmptyEntries);
var readmeFile = args[7];
var folderList = args[8].Split([';'], StringSplitOptions.RemoveEmptyEntries);
var assemblyList = args[9].Split([';'], StringSplitOptions.RemoveEmptyEntries);
var dependencyList = args[10].Split([';'], StringSplitOptions.RemoveEmptyEntries);
var libraryList = args[11].Split([';'], StringSplitOptions.RemoveEmptyEntries);
var rulesetsDir = args[12];
var editorconfigsDir = args[13];
var artifactsBinDir = args[14];
var analyzerDocumentationFileDir = args[15];
var analyzerDocumentationFileName = args[16];
var analyzerSarifFileDir = args[17];
var analyzerSarifFileName = args[18];
var analyzerConfigurationFileDir = args[19];
var analyzerConfigurationFileName = args[20];
var globalAnalyzerConfigsDir = args[21];

var result = new StringBuilder();

result.AppendLine(@"<?xml version=""1.0""?>");
result.AppendLine(@"<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">");
result.AppendLine(@"  <metadata>");

string version = string.Empty;
string repositoryType = string.Empty;
string repositoryUrl = string.Empty;
string repositoryCommit = string.Empty;
string readmePackageLocation = string.Empty;

foreach (string entry in metadataList)
{
    int equals = entry.IndexOf('=');
    string name = entry[..equals];
    string value = entry[(equals + 1)..];
    switch (name)
    {
        case "repositoryType": repositoryType = value; continue;
        case "repositoryUrl": repositoryUrl = value; continue;
        case "repositoryCommit": repositoryCommit = value; continue;
        case "license": result.AppendLine($"    <license type=\"expression\">{value}</license>"); continue;
        case "readme": readmePackageLocation = value; break;
    }

    if (value.Length > 0)
    {
        result.AppendLine($"    <{name}>{value}</{name}>");
    }

    if (name == "version")
    {
        version = value;
    }
}

if (!string.IsNullOrEmpty(repositoryType))
{
    result.AppendLine($@"    <repository type=""{repositoryType}"" url=""{repositoryUrl}"" commit=""{repositoryCommit}""/>");
}

if (dependencyList.Length > 0)
{
    result.AppendLine(@"    <dependencies>");

    foreach (var dependency in dependencyList)
    {
        result.AppendLine($@"      <dependency id=""{dependency}"" version=""{version}"" />");
    }

    result.AppendLine(@"    </dependencies>");
}

result.AppendLine(@"  </metadata>");

result.AppendLine(@"  <files>");
result.AppendLine(@"    $CommonFileElements$");

if (fileList.Length > 0 || assemblyList.Length > 0 || libraryList.Length > 0 || folderList.Length > 0 || readmePackageLocation.Length > 0)
{
    const string csName = "CSharp";
    const string vbName = "VisualBasic";
    const string csTarget = @"analyzers\dotnet\cs";
    const string vbTarget = @"analyzers\dotnet\vb";
    const string agnosticTarget = @"analyzers\dotnet";

    var allTargets = new List<string>();
    if (assemblyList.Any(assembly => assembly.Contains(csName, StringComparison.Ordinal)))
    {
        allTargets.Add(csTarget);
    }

    if (assemblyList.Any(assembly => assembly.Contains(vbName, StringComparison.Ordinal)))
    {
        allTargets.Add(vbTarget);
    }

    if (allTargets.Count == 0)
    {
        allTargets.Add(agnosticTarget);
    }

    foreach (string assembly in assemblyList)
    {
        IEnumerable<string> targets;

        if (assembly.Contains(csName, StringComparison.Ordinal))
        {
            targets = new[] { csTarget };
        }
        else if (assembly.Contains(vbName, StringComparison.Ordinal))
        {
            targets = new[] { vbTarget };
        }
        else
        {
            targets = allTargets;
        }

        string assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(assembly);

        foreach (var tfm in tfms)
        {
            string assemblyFolder = Path.Combine(artifactsBinDir, assemblyNameWithoutExtension, configuration, tfm);
            string assemblyPathForNuspec = Path.Combine(assemblyFolder, assembly);

            foreach (string target in targets)
            {
                result.AppendLine(FileElement(assemblyPathForNuspec, target));

                if (Directory.Exists(assemblyFolder))
                {
                    string resourceAssemblyName = assemblyNameWithoutExtension + ".resources.dll";
                    foreach (var directory in Directory.EnumerateDirectories(assemblyFolder))
                    {
                        var resourceAssemblyFullPath = Path.Combine(directory, resourceAssemblyName);
                        if (File.Exists(resourceAssemblyFullPath))
                        {
                            var directoryName = Path.GetFileName(directory);
                            string resourceAssemblyPathForNuspec = Path.Combine(artifactsBinDir, assemblyNameWithoutExtension, configuration, tfm, directoryName, resourceAssemblyName);
                            string targetForNuspec = Path.Combine(target, directoryName);
                            result.AppendLine(FileElement(resourceAssemblyPathForNuspec, targetForNuspec));
                        }
                    }
                }
            }
        }
    }

    foreach (string file in fileList)
    {
        var fileWithPath = Path.IsPathRooted(file) ? file : Path.Combine(projectDir, file);
        result.AppendLine(FileElement(fileWithPath, "buildTransitive"));
    }

    if (readmePackageLocation.Length > 0)
    {
        readmeFile = Path.IsPathRooted(readmeFile) ? readmeFile : Path.GetFullPath(Path.Combine(projectDir, readmeFile));
        var directoryName = Path.GetDirectoryName(readmePackageLocation) ?? string.Empty;
        result.AppendLine(FileElement(readmeFile, directoryName));
    }

    foreach (string file in libraryList)
    {
        foreach (var tfm in tfms)
        {
            var fileWithPath = Path.Combine(artifactsBinDir, Path.GetFileNameWithoutExtension(file), configuration, tfm, file);

            // For multi-tfm case, file may not exist for all tfms.
            if (File.Exists(fileWithPath))
            {
                result.AppendLine(FileElement(fileWithPath, Path.Combine("lib", tfm)));
            }
        }
    }

    // Skip packaging certain well-known third-party assemblies that ship within Microsoft.CodeAnalysis.Features package.
    var fileNamesToExclude = new List<string>() { "Humanizer.dll", "MessagePack.dll", "MessagePack.Annotations.dll" };

    foreach (string folder in folderList)
    {
        foreach (var tfm in tfms)
        {
            string folderPath = Path.Combine(artifactsBinDir, folder, configuration, tfm);
            foreach (var file in Directory.EnumerateFiles(folderPath))
            {
                var fileExtension = Path.GetExtension(file);
                if (fileExtension is ".exe" or ".dll" or ".config" or ".xml")
                {
                    var fileName = Path.GetFileName(file);
                    if (fileNamesToExclude.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    var fileWithPath = Path.Combine(folderPath, file);
                    var targetPath = tfms.Length > 1 ? Path.Combine(folder, tfm) : folder;
                    result.AppendLine(FileElement(fileWithPath, targetPath));
                }
            }
        }
    }

    result.AppendLine(FileElement(Path.Combine(assetsDir, "Install.ps1"), "tools"));
    result.AppendLine(FileElement(Path.Combine(assetsDir, "Uninstall.ps1"), "tools"));
}

if (rulesetsDir.Length > 0 && Directory.Exists(rulesetsDir))
{
    foreach (string ruleset in Directory.EnumerateFiles(rulesetsDir))
    {
        if (Path.GetExtension(ruleset) == ".ruleset")
        {
            result.AppendLine(FileElement(Path.Combine(rulesetsDir, ruleset), "rulesets"));
        }
    }
}

if (editorconfigsDir.Length > 0 && Directory.Exists(editorconfigsDir))
{
    foreach (string directory in Directory.EnumerateDirectories(editorconfigsDir))
    {
        var directoryName = new DirectoryInfo(directory).Name;
        foreach (string editorconfig in Directory.EnumerateFiles(directory))
        {
            result.AppendLine(FileElement(Path.Combine(directory, editorconfig), $"editorconfig\\{directoryName}"));
        }
    }
}

if (globalAnalyzerConfigsDir.Length > 0 && Directory.Exists(globalAnalyzerConfigsDir))
{
    foreach (string globalconfig in Directory.EnumerateFiles(globalAnalyzerConfigsDir))
    {
        if (Path.GetExtension(globalconfig) == ".globalconfig")
        {
            result.AppendLine(FileElement(Path.Combine(globalAnalyzerConfigsDir, globalconfig), $"buildTransitive\\config"));
        }
        else
        {
            throw new InvalidDataException($"Encountered a file with unexpected extension: {globalconfig}");
        }
    }
}

if (analyzerDocumentationFileDir.Length > 0 && Directory.Exists(analyzerDocumentationFileDir) && analyzerDocumentationFileName.Length > 0)
{
    var fileWithPath = Path.Combine(analyzerDocumentationFileDir, analyzerDocumentationFileName);
    if (File.Exists(fileWithPath))
    {
        result.AppendLine(FileElement(fileWithPath, "documentation"));
    }
}

if (analyzerSarifFileDir.Length > 0 && Directory.Exists(analyzerSarifFileDir) && analyzerSarifFileName.Length > 0)
{
    var fileWithPath = Path.Combine(analyzerSarifFileDir, analyzerSarifFileName);
    if (File.Exists(fileWithPath))
    {
        result.AppendLine(FileElement(fileWithPath, "documentation"));
    }
}

if (analyzerConfigurationFileDir.Length > 0 && Directory.Exists(analyzerConfigurationFileDir) && analyzerConfigurationFileName.Length > 0)
{
    var fileWithPath = Path.Combine(analyzerConfigurationFileDir, analyzerConfigurationFileName);
    if (File.Exists(fileWithPath))
    {
        result.AppendLine(FileElement(fileWithPath, "documentation"));
    }
}

result.AppendLine(FileElement(Path.Combine(assetsDir, "ThirdPartyNotices.txt"), ""));
result.AppendLine(@"  </files>");

result.AppendLine(@"</package>");

File.WriteAllText(nuspecFile, result.ToString());

static string FileElement(string file, string target) => $@"    <file src=""{file}"" target=""{target}""/>";

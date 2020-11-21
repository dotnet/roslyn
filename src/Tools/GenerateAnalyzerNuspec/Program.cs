// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1820, CS8600, IDE0062, IDE0078

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

string nuspecFile = args[0];
string assetsDir = args[1];
string projectDir = args[2];
string configuration = args[3];
string[] tfms = args[4].Split(';');
var metadataList = args[5].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
var fileList = args[6].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
var folderList = args[7].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
var assemblyList = args[8].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
var dependencyList = args[9].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
var libraryList = args[10].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
var rulesetsDir = args[11];
var editorconfigsDir = args[12];
var legacyRulesets = args[13].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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

string version = null;
string repositoryType = null;
string repositoryUrl = null;
string repositoryCommit = null;

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
    }

    if (value != "")
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

string FileElement(string file, string target) => $@"    <file src=""{file}"" target=""{target}""/>";

if (fileList.Length > 0 || assemblyList.Length > 0 || libraryList.Length > 0 || folderList.Length > 0)
{
    const string csName = "CSharp";
    const string vbName = "VisualBasic";
    const string csTarget = @"analyzers\dotnet\cs";
    const string vbTarget = @"analyzers\dotnet\vb";
    const string agnosticTarget = @"analyzers\dotnet";

    var allTargets = new List<string>();
    if (assemblyList.Any(assembly => assembly.Contains(csName)))
    {
        allTargets.Add(csTarget);
    }

    if (assemblyList.Any(assembly => assembly.Contains(vbName)))
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

        if (assembly.Contains(csName))
        {
            targets = new[] { csTarget };
        }
        else if (assembly.Contains(vbName))
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
            string assemblyPathForNuspec = Path.Combine(assemblyNameWithoutExtension, configuration, tfm, assembly);

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
                            string resourceAssemblyPathForNuspec = Path.Combine(assemblyNameWithoutExtension, configuration, tfm, directoryName, resourceAssemblyName);
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
        result.AppendLine(FileElement(fileWithPath, "build"));
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

    foreach (string folder in folderList)
    {
        foreach (var tfm in tfms)
        {
            string folderPath = Path.Combine(artifactsBinDir, folder, configuration, tfm);
            foreach (var file in Directory.EnumerateFiles(folderPath))
            {
                var fileExtension = Path.GetExtension(file);
                if (fileExtension == ".exe" ||
                    fileExtension == ".dll" ||
                    fileExtension == ".config" ||
                    fileExtension == ".xml")
                {
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
    foreach (string editorconfig in Directory.EnumerateFiles(globalAnalyzerConfigsDir))
    {
        if (Path.GetExtension(editorconfig) == ".editorconfig")
        {
            result.AppendLine(FileElement(Path.Combine(globalAnalyzerConfigsDir, editorconfig), $"build\\config"));
        }
        else
        {
            throw new InvalidDataException($"Encountered a file with unexpected extension: {editorconfig}");
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

if (legacyRulesets.Length > 0)
{
    foreach (string legacyRuleset in legacyRulesets)
    {
        if (Path.GetExtension(legacyRuleset) == ".ruleset")
        {
            result.AppendLine(FileElement(Path.Combine(projectDir, legacyRuleset), @"rulesets\legacy"));
        }
    }
}

result.AppendLine(FileElement(Path.Combine(assetsDir, "EULA.rtf"), ""));
result.AppendLine(FileElement(Path.Combine(assetsDir, "ThirdPartyNotices.rtf"), ""));
result.AppendLine(@"  </files>");

result.AppendLine(@"</package>");

File.WriteAllText(nuspecFile, result.ToString());

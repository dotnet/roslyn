#r "System.Xml.Linq"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

string usage = @"usage: BuildNuGets.csx <binaries-dir> <build-version> <output-directory>";

if (Args.Count() != 3)
{
    Console.WriteLine(usage);
    Environment.Exit(1);
}

var SolutionRoot = Path.GetFullPath(Path.Combine(ScriptRoot(), "../"));

string ScriptRoot([CallerFilePath]string path = "") => Path.GetDirectoryName(path);


#region Config Variables

// Strip trailing '\' characters because if the path is later passed on the
// command line when surrounded by quotes (in case the path has spaces) some
// utilities will consider the '\"' as an escape sequence for the end quote
var BinDir = Path.GetFullPath(Args[0]).TrimEnd('\\');
var BuildVersion = Args[1].Trim();
var NuspecDirPath = Path.Combine(SolutionRoot, "src/NuGet");
var OutDir = Path.GetFullPath(Args[2]).TrimEnd('\\');

var LicenseUrl = @"http://go.microsoft.com/fwlink/?LinkId=529443";
var Authors = @"Microsoft";
var ProjectURL = @"http://msdn.com/roslyn";
var Tags = @"Roslyn CodeAnalysis Compiler CSharp VB VisualBasic Parser Scanner Lexer Emit CodeGeneration Metadata IL Compilation Scripting Syntax Semantics";

string SystemCollectionsImmutableVersion;
string SystemReflectionMetadataVersion;
string CodeAnalysisAnalyzersVersion;

// Read preceding variables from MSBuild file
var doc = XDocument.Load(Path.Combine(SolutionRoot, "build/Targets/VSL.Versions.targets"));
XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";
SystemCollectionsImmutableVersion = doc.Descendants(ns + nameof(SystemCollectionsImmutableVersion)).Single().Value;
SystemReflectionMetadataVersion = doc.Descendants(ns + nameof(SystemReflectionMetadataVersion)).Single().Value;
CodeAnalysisAnalyzersVersion = doc.Descendants(ns + nameof(CodeAnalysisAnalyzersVersion)).Single().Value;

#endregion

var NuGetAdditionalFilesPath = Path.Combine(SolutionRoot, "build/NuGetAdditionalFiles");
var ThirdPartyNoticesPath = Path.Combine(NuGetAdditionalFilesPath, "ThirdPartyNotices.rtf");
var NetCompilersPropsPath = Path.Combine(NuGetAdditionalFilesPath, "Microsoft.Net.Compilers.props");

var files = Directory.GetFiles(NuspecDirPath, "*.nuspec");

int exit = 0;

foreach (var file in files)
{
    var nugetArgs = $@"pack {file} " +
        $"-BasePath \"{BinDir}\" " +
        $"-OutputDirectory \"{OutDir}\" " +
        "-ExcludeEmptyDirectories " +
        $"-prop licenseUrl=\"{LicenseUrl}\" " +
        $"-prop version=\"{BuildVersion}\" " +
        $"-prop authors={Authors} " +
        $"-prop projectURL=\"{ProjectURL}\" " +
        $"-prop tags=\"{Tags}\" " +
        $"-prop systemCollectionsImmutableVersion=\"{SystemCollectionsImmutableVersion}\" " +
        $"-prop systemReflectionMetadataVersion=\"{SystemReflectionMetadataVersion}\" " +
        $"-prop codeAnalysisAnalyzersVersion=\"{CodeAnalysisAnalyzersVersion}\" " +
        $"-prop thirdPartyNoticesPath=\"{ThirdPartyNoticesPath}\" " +
        $"-prop netCompilersPropsPath=\"{NetCompilersPropsPath}\"";

    var nugetExePath = Path.GetFullPath(Path.Combine(SolutionRoot, "nuget.exe"));
    var p = new Process();
    p.StartInfo.FileName = nugetExePath;
    p.StartInfo.Arguments = nugetArgs;
    p.StartInfo.UseShellExecute = false;

    Console.WriteLine($"Running: nuget.exe {nugetArgs}");
    p.Start();
    p.WaitForExit();
    exit = p.ExitCode;

    if (exit != 0)
    {
        break;
    }
}

Environment.Exit(exit);

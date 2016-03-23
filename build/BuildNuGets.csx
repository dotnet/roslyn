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

var LicenseUrlRedist = @"http://go.microsoft.com/fwlink/?LinkId=529443";
var LicenseUrlNonRedist = @"http://go.microsoft.com/fwlink/?LinkId=529444";
var LicenseUrlTest = @"http://go.microsoft.com/fwlink/?LinkId=529445";

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

string[] RedistFiles = {
    "Microsoft.CodeAnalysis.BuildTask.Portable.nuspec",
    "Microsoft.CodeAnalysis.EditorFeatures.Text.nuspec",
    "Microsoft.CodeAnalysis.VisualBasic.Scripting.nuspec",
    "Microsoft.CodeAnalysis.Common.nuspec",
    "Microsoft.CodeAnalysis.Features.nuspec",
    "Microsoft.CodeAnalysis.VisualBasic.Workspaces.nuspec",
    "Microsoft.CodeAnalysis.Compilers.nuspec",
    "Microsoft.CodeAnalysis.nuspec",
    "Microsoft.CodeAnalysis.Workspaces.Common.nuspec",
    "Microsoft.CodeAnalysis.CSharp.Features.nuspec",
    "Microsoft.CodeAnalysis.Scripting.Common.nuspec",
    "Microsoft.CodeAnalysis.CSharp.nuspec",
    "Microsoft.CodeAnalysis.Scripting.nuspec",
    "Microsoft.CodeAnalysis.CSharp.Scripting.nuspec",
    "Microsoft.CodeAnalysis.CSharp.Workspaces.nuspec",
    "Microsoft.CodeAnalysis.VisualBasic.Features.nuspec",
    "Microsoft.VisualStudio.LanguageServices.nuspec",
    "Microsoft.CodeAnalysis.EditorFeatures.nuspec",
    "Microsoft.CodeAnalysis.VisualBasic.nuspec",
};

string[] NonRedistFiles = {
    "Microsoft.Net.Compilers.nuspec",
    "Microsoft.Net.Compilers.netcore.nuspec",
    "Microsoft.Net.CSharp.Interactive.netcore.nuspec",
};

string[] TestFiles = {
    "Microsoft.CodeAnalysis.Test.Resources.Proprietary.nuspec",
};

int PackFiles(string[] files, string licenseUrl)
{
    int exit = 0;

    foreach (var file in files.Select(f => Path.Combine(NuspecDirPath, f)))
    {
        var nugetArgs = $@"pack {file} " +
            $"-BasePath \"{BinDir}\" " +
            $"-OutputDirectory \"{OutDir}\" " +
            "-ExcludeEmptyDirectories " +
            $"-prop licenseUrl=\"{licenseUrl}\" " +
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

        if ((exit = p.ExitCode) != 0)
        {
            break;
        }
    }

    return exit;
}

int exit = PackFiles(RedistFiles, LicenseUrlRedist);
if (exit == 0) PackFiles(NonRedistFiles, LicenseUrlNonRedist);
if (exit == 0) PackFiles(TestFiles, LicenseUrlTest);

Environment.Exit(exit);

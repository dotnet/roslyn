#r "System.Xml.Linq"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
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

// Read preceding variables from MSBuild file
var doc = XDocument.Load(Path.Combine(SolutionRoot, "build/Targets/VSL.Versions.targets"));
XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";
string SystemCollectionsImmutableVersion = doc.Descendants(ns + nameof(SystemCollectionsImmutableVersion)).Single().Value;
string SystemReflectionMetadataVersion = doc.Descendants(ns + nameof(SystemReflectionMetadataVersion)).Single().Value;
string CodeAnalysisAnalyzersVersion = doc.Descendants(ns + nameof(CodeAnalysisAnalyzersVersion)).Single().Value;

string MicrosoftDiaSymReaderVersion = GetExistingPackageVersion("Microsoft.DiaSymReader");
string MicrosoftDiaSymReaderPortablePdbVersion = GetExistingPackageVersion("Microsoft.DiaSymReader.PortablePdb");

string GetExistingPackageVersion(string name)
{
    string path = Directory.Exists(OutDir) ? Directory.GetFiles(OutDir, name + ".*.nupkg").SingleOrDefault() : null;
    return (path == null) ? null : Path.GetFileNameWithoutExtension(path).Substring(name.Length + 1);
}

#endregion

var NuGetAdditionalFilesPath = Path.Combine(SolutionRoot, "build/NuGetAdditionalFiles");
var ThirdPartyNoticesPath = Path.Combine(NuGetAdditionalFilesPath, "ThirdPartyNotices.rtf");
var NetCompilersPropsPath = Path.Combine(NuGetAdditionalFilesPath, "Microsoft.Net.Compilers.props");

string[] RedistPackageNames = {
    "Microsoft.CodeAnalysis.BuildTask.Portable",
    "Microsoft.CodeAnalysis.Common",
    "Microsoft.CodeAnalysis.Compilers",
    "Microsoft.CodeAnalysis.CSharp.Features",
    "Microsoft.CodeAnalysis.CSharp",
    "Microsoft.CodeAnalysis.CSharp.Scripting",
    "Microsoft.CodeAnalysis.CSharp.Workspaces",
    "Microsoft.CodeAnalysis.EditorFeatures",
    "Microsoft.CodeAnalysis.EditorFeatures.Text",
    "Microsoft.CodeAnalysis.Features",
    "Microsoft.CodeAnalysis",
    "Microsoft.CodeAnalysis.Scripting.Common",
    "Microsoft.CodeAnalysis.Scripting",
    "Microsoft.CodeAnalysis.VisualBasic.Features",
    "Microsoft.CodeAnalysis.VisualBasic",
    "Microsoft.CodeAnalysis.VisualBasic.Scripting",
    "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
    "Microsoft.CodeAnalysis.Workspaces.Common",
    "Microsoft.VisualStudio.LanguageServices",
};

string[] NonRedistPackageNames = {
    "Microsoft.Net.Compilers",
    "Microsoft.Net.Compilers.netcore",
    "Microsoft.Net.CSharp.Interactive.netcore",
};

string[] TestPackageNames = {
    "Microsoft.CodeAnalysis.Test.Resources.Proprietary",
};

// the following packages will only be publised on myget not on nuget:
var PreReleaseOnlyPackages = new HashSet<string>
{
    "Microsoft.CodeAnalysis.EditorFeatures",
    "Microsoft.CodeAnalysis.VisualBasic.Scripting",
    "Microsoft.Net.Compilers.netcore",
    "Microsoft.Net.CSharp.Interactive.netcore",
};

// Create an empty directory to be used in NuGet pack
var emptyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
var dirInfo = Directory.CreateDirectory(emptyDir);
File.Create(Path.Combine(emptyDir, "_._")).Close();

int PackFiles(string[] packageNames, string licenseUrl)
{
    int exit = 0;

    foreach (var file in packageNames.Select(f => Path.Combine(NuspecDirPath, f + ".nuspec")))
    {
        var nugetArgs = $@"pack {file} " +
            $"-BasePath \"{BinDir}\" " +
            $"-OutputDirectory \"{OutDir}\" " +
            $"-prop licenseUrl=\"{licenseUrl}\" " +
            $"-prop version=\"{BuildVersion}\" " +
            $"-prop authors={Authors} " +
            $"-prop projectURL=\"{ProjectURL}\" " +
            $"-prop tags=\"{Tags}\" " +
            $"-prop systemCollectionsImmutableVersion=\"{SystemCollectionsImmutableVersion}\" " +
            $"-prop systemReflectionMetadataVersion=\"{SystemReflectionMetadataVersion}\" " +
            $"-prop codeAnalysisAnalyzersVersion=\"{CodeAnalysisAnalyzersVersion}\" " +
            $"-prop thirdPartyNoticesPath=\"{ThirdPartyNoticesPath}\" " +
            $"-prop netCompilersPropsPath=\"{NetCompilersPropsPath}\" " +
            $"-prop emptyDirPath=\"{emptyDir}\"";;

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

XElement MakePackageElement(string packageName, string version)
{
    return new XElement("package", new XAttribute("id", packageName), new XAttribute("version", version));
}

IEnumerable<XElement> MakeRoslynPackageElements(bool isRelease)
{
    var packageNames = RedistPackageNames.Concat(NonRedistPackageNames);

    if (isRelease)
    {
        packageNames = packageNames.Where(pn => !PreReleaseOnlyPackages.Contains(pn));
    }

    return packageNames.Select(packageName => MakePackageElement(packageName, BuildVersion));
}

void GeneratePublishingConfig(string fileName, IEnumerable<XElement> packages)
{
    var doc = new XDocument(new XElement("packages", packages.ToArray()));
    doc.Save(Path.Combine(OutDir, fileName));
}

// Currently we publish some of the Roslyn dependencies. Remove this once they are moved to a separate repo.
IEnumerable<XElement> MakePackageElementsForPublishedDependencies(bool isRelease)
{
    if (MicrosoftDiaSymReaderVersion != null && isRelease == IsReleaseVersion(MicrosoftDiaSymReaderVersion))
    {
        yield return MakePackageElement("Microsoft.DiaSymReader", MicrosoftDiaSymReaderVersion);
    }

    if (MicrosoftDiaSymReaderPortablePdbVersion != null && isRelease == IsReleaseVersion(MicrosoftDiaSymReaderPortablePdbVersion))
    {
        yield return MakePackageElement("Microsoft.DiaSymReader.PortablePdb", MicrosoftDiaSymReaderPortablePdbVersion);
    }
}

void GeneratePublishingConfig()
{
    if (IsReleaseVersion(BuildVersion))
    {
        // nuget:
        var packages = MakeRoslynPackageElements(isRelease: true).Concat(MakePackageElementsForPublishedDependencies(isRelease: true));
        GeneratePublishingConfig("nuget_org-packages.config", packages);
    }
    else
    {
        // myget:
        var packages = MakeRoslynPackageElements(isRelease: false).Concat(MakePackageElementsForPublishedDependencies(isRelease: false));
        GeneratePublishingConfig("myget_org-packages.config", packages);
    }
}

bool IsReleaseVersion(string version) => !version.Contains('-');

Directory.CreateDirectory(OutDir);

GeneratePublishingConfig();

int exit = PackFiles(RedistPackageNames, LicenseUrlRedist);
if (exit == 0) exit = PackFiles(NonRedistPackageNames, LicenseUrlNonRedist);
if (exit == 0) exit = PackFiles(TestPackageNames, LicenseUrlTest);

try
{
    dirInfo.Delete(recursive: true);
}
catch
{
    // Ignore errors
}

Environment.Exit(exit);

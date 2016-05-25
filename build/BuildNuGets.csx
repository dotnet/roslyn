#r "System.Xml.Linq"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
var BuildingReleaseNugets = IsReleaseVersion(BuildVersion);
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
string CoreFXVersionSuffix = doc.Descendants(ns + nameof(CoreFXVersionSuffix)).Single().Value;

string MicrosoftDiaSymReaderVersion = GetExistingPackageVersion("Microsoft.DiaSymReader");
string MicrosoftDiaSymReaderPortablePdbVersion = GetExistingPackageVersion("Microsoft.DiaSymReader.PortablePdb");

string GetExistingPackageVersion(string name)
{
    if (!Directory.Exists(OutDir))
    {
        return null;
    }

    foreach (var file in Directory.GetFiles(OutDir, "*.nupkg"))
    {
        string packageNameAndVersion = Path.GetFileNameWithoutExtension(file);
        string packageName = string.Join(".", packageNameAndVersion.Split('.').TakeWhile(s => !char.IsNumber(s[0])));

        if (packageName == name)
        {
            return packageNameAndVersion.Substring(packageName.Length + 1);
        }
    }

    return null;
}

#endregion

var NuGetAdditionalFilesPath = Path.Combine(SolutionRoot, "build/NuGetAdditionalFiles");
var ThirdPartyNoticesPath = Path.Combine(NuGetAdditionalFilesPath, "ThirdPartyNotices.rtf");
var NetCompilersPropsPath = Path.Combine(NuGetAdditionalFilesPath, "Microsoft.Net.Compilers.props");

string[] RedistPackageNames = {
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
    "Roslyn.VisualStudio.Test.Utilities",
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
    "Microsoft.CodeAnalysis.Test.Resources.Proprietary",
};

// Create an empty directory to be used in NuGet pack
var emptyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
var dirInfo = Directory.CreateDirectory(emptyDir);
File.Create(Path.Combine(emptyDir, "_._")).Close();

int PackFiles(string[] nuspecFiles, string licenseUrl)
{
    int exit = 0;
    foreach (var file in nuspecFiles)
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
            $"-prop coreFXVersionSuffix=\"{CoreFXVersionSuffix}\" " +
            $"-prop thirdPartyNoticesPath=\"{ThirdPartyNoticesPath}\" " +
            $"-prop netCompilersPropsPath=\"{NetCompilersPropsPath}\" " +
            $"-prop emptyDirPath=\"{emptyDir}\"";

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

string[] GetRoslynPackageNames()
{
    var packageNames = RedistPackageNames.Concat(NonRedistPackageNames).Concat(TestPackageNames);

    if (BuildingReleaseNugets)
    {
        packageNames = packageNames.Where(pn => !PreReleaseOnlyPackages.Contains(pn));
    }

    return packageNames.ToArray();
}

IEnumerable<XElement> MakeRoslynPackageElements(string[] roslynPackageNames)
{
    return roslynPackageNames.Select(packageName => MakePackageElement(packageName, BuildVersion));
}

void GeneratePublishingConfig(string fileName, IEnumerable<XElement> packages)
{
    var doc = new XDocument(new XElement("packages", packages.ToArray()));
    doc.Save(Path.Combine(OutDir, fileName));
}

// Currently we publish some of the Roslyn dependencies. Remove this once they are moved to a separate repo.
IEnumerable<XElement> MakePackageElementsForPublishedDependencies()
{
    if (MicrosoftDiaSymReaderVersion != null && BuildingReleaseNugets == IsReleaseVersion(MicrosoftDiaSymReaderVersion))
    {
        yield return MakePackageElement("Microsoft.DiaSymReader", MicrosoftDiaSymReaderVersion);
    }

    if (MicrosoftDiaSymReaderPortablePdbVersion != null && BuildingReleaseNugets == IsReleaseVersion(MicrosoftDiaSymReaderPortablePdbVersion))
    {
        yield return MakePackageElement("Microsoft.DiaSymReader.PortablePdb", MicrosoftDiaSymReaderPortablePdbVersion);
    }
}

void GeneratePublishingConfig(string[] roslynPackageNames)
{
    var packages = MakeRoslynPackageElements(roslynPackageNames).Concat(MakePackageElementsForPublishedDependencies());
    if (BuildingReleaseNugets)
    {
        // nuget:
        GeneratePublishingConfig("nuget_org-packages.config", packages);
    }
    else
    {
        // myget:
        GeneratePublishingConfig("myget_org-packages.config", packages);
    }
}

bool IsReleaseVersion(string version) => !version.Contains('-');

bool IsPreReleaseDependency(string dependencyName, string dependencyVersion, List<string> warnings, string nuspecFile = null)
{
    if (!string.IsNullOrWhiteSpace(dependencyName) && !string.IsNullOrWhiteSpace(dependencyVersion) && !IsReleaseVersion(dependencyVersion))
    {
        var message = $"Detected dependency on prerelease version {dependencyVersion} of {dependencyName}";

        string warning;
        if (nuspecFile == null)
        {
            warning = message;
        }
        else
        {
            warning = $"{nuspecFile}: {message}";
        }

        warnings.Add(warning);
        Console.WriteLine(warning);

        return true;
    }

    return false;
}

XName NuspecDependencyElementName = (XNamespace)@"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd" + "dependency";
bool HasPreReleaseDependencies(string nuspecFile, List<string> warnings)
{
    var hasPreReleaseDependencies = false;
    var nuspecDocument = XDocument.Load(nuspecFile);
    foreach (var dependency in nuspecDocument.Descendants(NuspecDependencyElementName))
    {
        if (IsPreReleaseDependency(dependency.Attribute("id").Value, dependency.Attribute("version").Value, warnings, nuspecFile))
        {
            hasPreReleaseDependencies = true;
        }
    }

    return hasPreReleaseDependencies;
}

bool HasPreReleaseDependencies(string[] nuspecFiles, out List<string> warnings)
{
    warnings = new List<string>();
    var hasPreReleaseDependencies = false;
    if (IsPreReleaseDependency("System.Collections.Immutable", SystemCollectionsImmutableVersion, warnings) ||
        IsPreReleaseDependency("System.Reflection.Metadata", SystemReflectionMetadataVersion, warnings) ||
        IsPreReleaseDependency("Microsoft.CodeAnalysis.Analyzers", CodeAnalysisAnalyzersVersion, warnings) ||
        IsPreReleaseDependency("Microsoft.DiaSymReader", MicrosoftDiaSymReaderVersion, warnings) ||
        IsPreReleaseDependency("Microsoft.DiaSymReader.PortablePdb", MicrosoftDiaSymReaderPortablePdbVersion, warnings))
    {
        hasPreReleaseDependencies = true;
    }

    foreach (var nuspecFile in nuspecFiles)
    {
        if (HasPreReleaseDependencies(nuspecFile, warnings))
        {
            hasPreReleaseDependencies = true;
        }
    }

    return hasPreReleaseDependencies;
}

Directory.CreateDirectory(OutDir);

var roslynPackageNames = GetRoslynPackageNames();
GeneratePublishingConfig(roslynPackageNames);
string[] roslynNuspecFiles = roslynPackageNames.Select(f => Path.Combine(NuspecDirPath, f + ".nuspec")).ToArray();

if (BuildingReleaseNugets)
{
    List<string> warnings;
    if (HasPreReleaseDependencies(roslynNuspecFiles, out warnings))
    {
        // If we are building release nugets and if any packages have dependencies on prerelease packages
        // then print a warning and skip building release nugets.
        Console.WriteLine("warning: Skipping generation of release nugets since prerelease dependencies were detected");
        File.WriteAllLines(Path.Combine(OutDir, "warnings.log"), warnings);
        Environment.Exit(0);
    }
}

int exit = PackFiles(roslynNuspecFiles, LicenseUrlRedist);

try
{
    dirInfo.Delete(recursive: true);
}
catch
{
    // Ignore errors
}

Environment.Exit(exit);

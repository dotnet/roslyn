#r "System.Xml.XDocument.dll"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

string usage = @"usage: BuildNuGets.csx <binaries-dir> <build-version> <output-directory> <git sha> [<filter>]";

if (Args.Count < 4 || Args.Count > 5)
{
    Console.WriteLine(usage);
    Environment.Exit(1);
}

var SolutionRoot = Path.GetFullPath(Path.Combine(ScriptRoot(), "..", ".."));
var ToolsetPath = Path.Combine(SolutionRoot, "Binaries", "toolset");

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

var CommitSha = Args[3];
var CommitIsDeveloperBuild = CommitSha == "<developer build>";
if (!CommitIsDeveloperBuild && !Regex.IsMatch(CommitSha, "[A-Fa-f0-9]+"))
{
    Console.WriteLine("Invalid Git sha value: expected <developer build> or a valid sha");
    Environment.Exit(1);
}
var CommitPathMessage = CommitIsDeveloperBuild
    ? "This an unofficial build from a developer's machine"
    : $"This package was built from the source at https://github.com/dotnet/roslyn/commit/{CommitSha}";

var NuspecNameFilter = Args.Count > 4 ? Args[4] : null;

var LicenseUrlRedist = @"http://go.microsoft.com/fwlink/?LinkId=529443";
var LicenseUrlNonRedist = @"http://go.microsoft.com/fwlink/?LinkId=529444";
var LicenseUrlTest = @"http://go.microsoft.com/fwlink/?LinkId=529445";
var LicenseUrlSource = @"https://github.com/dotnet/roslyn/blob/master/License.txt";

var Authors = @"Microsoft";
var ProjectURL = @"http://msdn.com/roslyn";
var Tags = @"Roslyn CodeAnalysis Compiler CSharp VB VisualBasic Parser Scanner Lexer Emit CodeGeneration Metadata IL Compilation Scripting Syntax Semantics";

// Read preceding variables from MSBuild file
var packagesDoc = XDocument.Load(Path.Combine(SolutionRoot, "build/Targets/Packages.props"));
var fixedPackagesDoc = XDocument.Load(Path.Combine(SolutionRoot, "build/Targets/FixedPackages.props"));
XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";

var dependencyVersions = from e in packagesDoc.Root.Descendants().Concat(fixedPackagesDoc.Root.Descendants())
                         where e.Name.LocalName.EndsWith("Version")
                         select new { VariableName = e.Name.LocalName, Value=e.Value };

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

var IsCoreBuild = File.Exists(Path.Combine(ToolsetPath, "corerun"));

#endregion

var NuGetAdditionalFilesPath = Path.Combine(SolutionRoot, "build/NuGetAdditionalFiles");
var SrcDirPath = Path.Combine(SolutionRoot, "src");

string[] RedistPackageNames = {
    "Microsoft.CodeAnalysis",
    "Microsoft.CodeAnalysis.Build.Tasks",
    "Microsoft.CodeAnalysis.Common",
    "Microsoft.CodeAnalysis.Compilers",
    "Microsoft.CodeAnalysis.CSharp.Features",
    "Microsoft.CodeAnalysis.CSharp",
    "Microsoft.CodeAnalysis.CSharp.CodeStyle",
    "Microsoft.CodeAnalysis.CSharp.Scripting",
    "Microsoft.CodeAnalysis.CSharp.Workspaces",
    "Microsoft.CodeAnalysis.EditorFeatures",
    "Microsoft.CodeAnalysis.EditorFeatures.Text",
    "Microsoft.CodeAnalysis.Features",
    "Microsoft.CodeAnalysis.Remote.ServiceHub",
    "Microsoft.CodeAnalysis.Remote.Workspaces",
    "Microsoft.CodeAnalysis.Scripting.Common",
    "Microsoft.CodeAnalysis.Scripting",
    "Microsoft.CodeAnalysis.VisualBasic.Features",
    "Microsoft.CodeAnalysis.VisualBasic",
    "Microsoft.CodeAnalysis.VisualBasic.CodeStyle",
    "Microsoft.CodeAnalysis.VisualBasic.Scripting",
    "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
    "Microsoft.CodeAnalysis.Workspaces.Common",
    "Microsoft.VisualStudio.LanguageServices",
};

string[] SourcePackageNames = {
    "Microsoft.CodeAnalysis.PooledObjects",
    "Microsoft.CodeAnalysis.Debugging",
};

string[] NonRedistPackageNames = {
    "Microsoft.CodeAnalysis.Remote.Razor.ServiceHub",
    "Microsoft.Net.Compilers",
    "Microsoft.Net.Compilers.netcore",
    "Microsoft.Net.CSharp.Interactive.netcore",
    "Microsoft.NETCore.Compilers",
    "Microsoft.VisualStudio.IntegrationTest.Utilities",
    "Microsoft.VisualStudio.LanguageServices.Razor.RemoteClient",
};

string[] TestPackageNames = {

};

// The following packages will only be published on myget not on nuget
// Packages listed here must also appear in RedistPackageNames (above)
// or they will not be published anywhere at all
var PreReleaseOnlyPackages = new HashSet<string>
{
    "Microsoft.CodeAnalysis.Build.Tasks",
    "Microsoft.CodeAnalysis.VisualBasic.Scripting",
    "Microsoft.Net.Compilers.netcore",
    "Microsoft.Net.CSharp.Interactive.netcore",
    "Microsoft.NETCore.Compilers",
    "Microsoft.CodeAnalysis.Remote.Razor.ServiceHub",
    "Microsoft.CodeAnalysis.Remote.ServiceHub",
    "Microsoft.CodeAnalysis.Remote.Workspaces",
    "Microsoft.CodeAnalysis.Test.Resources.Proprietary",
    "Microsoft.VisualStudio.IntegrationTest.Utilities",
    "Microsoft.VisualStudio.LanguageServices.Razor.RemoteClient",
    "Microsoft.CodeAnalysis.PooledObjects",
    "Microsoft.CodeAnalysis.Debugging",
};

// The assets for these packages are not produced when building on Unix
// and we don't want to attempt to package them when building packages.
var PackagesNotBuiltOnCore = new HashSet<string>
{
     "Microsoft.CodeAnalysis.CSharp.Features",
     "Microsoft.CodeAnalysis.EditorFeatures",
     "Microsoft.CodeAnalysis.EditorFeatures.Text",
     "Microsoft.CodeAnalysis.Features",
     "Microsoft.CodeAnalysis.Remote.Razor.ServiceHub",
     "Microsoft.CodeAnalysis.Remote.ServiceHub",
     "Microsoft.CodeAnalysis.Remote.Workspaces",
     "Microsoft.CodeAnalysis.VisualBasic.Features",
     "Microsoft.CodeAnalysis.Workspaces.Common",
     "Microsoft.Net.Compilers",
     "Microsoft.VisualStudio.IntegrationTest.Utilities",
     "Microsoft.VisualStudio.LanguageServices",
     "Microsoft.VisualStudio.LanguageServices.Razor.RemoteClient",
     "Roslyn.VisualStudio.Test.Utilities",
};

// Create an empty directory to be used in NuGet pack
var emptyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
var dirInfo = Directory.CreateDirectory(emptyDir);
File.Create(Path.Combine(emptyDir, "_._")).Close();

var errors = new List<string>();

void ReportError(string message)
{
    errors.Add(message);
    PrintError(message);
}

void PrintError(string message)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ForegroundColor = color;
}

string GetPackageVersion(string packageName)
{
    // HACK: since Microsoft.Net.Compilers 2.0.0 was uploaded by accident and later deleted, we must bump the minor.
    // We will do this to both the regular Microsoft.Net.Compilers package and also the netcore package to keep them
    // in sync.
    if (BuildVersion.StartsWith("2.0.") && packageName.StartsWith("Microsoft.Net.Compilers", StringComparison.OrdinalIgnoreCase))
    {
        string[] buildVersionParts = BuildVersion.Split('-');
        string[] buildVersionBaseParts = buildVersionParts[0].Split('.');
        
        buildVersionBaseParts[buildVersionBaseParts.Length - 1] =
            (int.Parse(buildVersionBaseParts[buildVersionBaseParts.Length - 1]) + 1).ToString();

        buildVersionParts[0] = string.Join(".", buildVersionBaseParts);
        return string.Join("-", buildVersionParts);
    }

    return BuildVersion;
}

int PackFiles(string[] nuspecFiles, string licenseUrl)
{
    var commonProperties = new Dictionary<string, string>()
    {
        { "licenseUrl", licenseUrl },
        { "version", BuildVersion },
        { "authors", Authors },
        { "projectURL", ProjectURL },
        { "tags", Tags },
        { "emptyDirPath", emptyDir },
        { "additionalFilesPath", NuGetAdditionalFilesPath },
        { "commitPathMessage", CommitPathMessage },
        { "srcDirPath", SrcDirPath }
    };

    foreach (var dependencyVersion in dependencyVersions)
    {
        commonProperties[dependencyVersion.VariableName] = dependencyVersion.Value;
    }

    string commonArgs;

    if (!IsCoreBuild)
    {
        // The -NoPackageAnalysis argument is to work around the following issue.  The warning output of 
        // NuGet gets promoted to an error by MSBuild /warnaserror
        // https://github.com/dotnet/roslyn/issues/18152
        commonArgs = $"-BasePath \"{BinDir}\" " +
        $"-OutputDirectory \"{OutDir}\" " +
        $"-NoPackageAnalysis " +
        string.Join(" ", commonProperties.Select(p => $"-prop {p.Key}=\"{p.Value}\""));
    }
    else
    {
        commonArgs = $"--base-path \"{BinDir}\" " +
        $"--output-directory \"{OutDir}\" " +
        $"--properties \"{string.Join(";", commonProperties.Select(p => $"{p.Key}={p.Value}"))}\"";
    }

    int exit = 0;
    foreach (var file in nuspecFiles)
    {
        if (NuspecNameFilter != null && !file.Contains(NuspecNameFilter))
        {
            continue;
        }

        var p = new Process();

        if (!IsCoreBuild)
        {
            string packageArgs = commonArgs.Replace($"-prop version=\"{BuildVersion}\"", $"-prop version=\"{GetPackageVersion(Path.GetFileNameWithoutExtension(file))}\"");

            p.StartInfo.FileName = Path.GetFullPath(Path.Combine(SolutionRoot, @"Binaries\Tools\nuget.exe"));
            p.StartInfo.Arguments = $@"pack {file} {packageArgs}";
        }
        else
        {
            p.StartInfo.FileName = Path.Combine(ToolsetPath, "corerun");
            p.StartInfo.Arguments = $@"{Path.Combine(ToolsetPath, "NuGet.CommandLine.XPlat.dll")} pack {file} {commonArgs}";
        }

        p.StartInfo.UseShellExecute = false;

        Console.WriteLine($"Packing {file}");

        p.Start();
        p.WaitForExit();

        var currentExit = p.ExitCode;
        if (currentExit != 0)
        {
            Console.WriteLine($"nuget pack {p.StartInfo.Arguments}");
            ReportError($"Pack operation failed with {currentExit}");
        }

        // We want to try and generate all nugets and log any errors encountered along the way.
        // We also want to fail the build in case of all encountered errors except the prerelease package dependency error above.
        exit = (exit == 0) ? currentExit : exit;
    }

    return exit;
}

XElement MakePackageElement(string packageName, string version)
{
    return new XElement("package", new XAttribute("id", packageName), new XAttribute("version", version));
}

IEnumerable<XElement> MakeRoslynPackageElements(string[] roslynPackageNames)
{
    return roslynPackageNames.Select(packageName => MakePackageElement(packageName, GetPackageVersion(packageName)));
}

void GeneratePublishingConfig(string fileName, IEnumerable<XElement> packages)
{
    var doc = new XDocument(new XElement("packages", packages.ToArray()));
    using (FileStream fs = File.OpenWrite(Path.Combine(OutDir, fileName)))
    {
        doc.Save(fs);
    }
}

void GeneratePublishingConfig(string[] roslynPackageNames)
{
    var packages = MakeRoslynPackageElements(roslynPackageNames);
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

string[] GetRoslynPackageNames(string[] packages)
{
    IEnumerable<string> packageNames = packages;

    if (BuildingReleaseNugets)
    {
        packageNames = packageNames.Where(pn => !PreReleaseOnlyPackages.Contains(pn));
    }

    if (IsCoreBuild)
    {
        packageNames = packageNames.Where(pn => !PackagesNotBuiltOnCore.Contains(pn));
    }

    return packageNames.ToArray();
}

int DoWork(string[] packageNames, string licenseUrl)
{
    var roslynPackageNames = GetRoslynPackageNames(packageNames);
    string[] roslynNuspecFiles = roslynPackageNames.Select(f => Path.Combine(NuspecDirPath, f + ".nuspec")).ToArray();
    return PackFiles(roslynNuspecFiles, licenseUrl);
}

bool IsReleaseVersion(string version) => !version.Contains('-');
Directory.CreateDirectory(OutDir);
var ErrorLogFile = Path.Combine(OutDir, "skipped_packages.txt");
try
{
    if (File.Exists(ErrorLogFile)) File.Delete(ErrorLogFile);
}
catch
{
    // Ignore errors
}

int exit = DoWork(RedistPackageNames, LicenseUrlRedist);
exit |= DoWork(NonRedistPackageNames, LicenseUrlNonRedist);
exit |= DoWork(TestPackageNames, LicenseUrlTest);
exit |= DoWork(SourcePackageNames, LicenseUrlSource);

var allPackageNames = RedistPackageNames.Concat(NonRedistPackageNames).Concat(TestPackageNames).Concat(SourcePackageNames).ToArray();
var roslynPackageNames = GetRoslynPackageNames(allPackageNames);
GeneratePublishingConfig(roslynPackageNames);

try
{
    dirInfo.Delete(recursive: true);
}
catch
{
    // Ignore errors
}

foreach (var error in errors)
{
    PrintError(error);
}

Environment.Exit(exit);

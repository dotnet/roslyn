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

var SolutionRoot = Path.GetFullPath(Path.Combine(ScriptRoot(), "../../"));

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
var doc = XDocument.Load(Path.Combine(SolutionRoot, "build/Targets/Dependencies.props"));
XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";

var dependencyVersions = from e in doc.Root.Descendants()
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

#endregion

var NuGetAdditionalFilesPath = Path.Combine(SolutionRoot, "build/NuGetAdditionalFiles");
var ThirdPartyNoticesPath = Path.Combine(NuGetAdditionalFilesPath, "ThirdPartyNotices.rtf");
var NetCompilersPropsPath = Path.Combine(NuGetAdditionalFilesPath, "Microsoft.Net.Compilers.props");

string[] RedistPackageNames = {
    "Microsoft.CodeAnalysis",
    "Microsoft.Codeanalysis.Build.Tasks",
    "Microsoft.CodeAnalysis.Common",
    "Microsoft.CodeAnalysis.Compilers",
    "Microsoft.CodeAnalysis.CSharp.Features",
    "Microsoft.CodeAnalysis.CSharp",
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
    "Microsoft.CodeAnalysis.VisualBasic.Scripting",
    "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
    "Microsoft.CodeAnalysis.Workspaces.Common",
    "Microsoft.VisualStudio.LanguageServices",
    "Microsoft.VisualStudio.LanguageServices.Next",
};

string[] NonRedistPackageNames = {
    "Microsoft.Net.Compilers",
    "Microsoft.Net.Compilers.netcore",
    "Microsoft.Net.CSharp.Interactive.netcore",
    "Roslyn.VisualStudio.Test.Utilities",
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
    "Microsoft.CodeAnalysis.Remote.ServiceHub",
    "Microsoft.CodeAnalysis.Remote.Workspaces",
    "Microsoft.CodeAnalysis.Test.Resources.Proprietary",
    "Microsoft.VisualStudio.LanguageServices.Next",
};

// Create an empty directory to be used in NuGet pack
var emptyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
var dirInfo = Directory.CreateDirectory(emptyDir);
File.Create(Path.Combine(emptyDir, "_._")).Close();

var errors = new List<string>();

void ReportError(string message)
{
    errors.Add(message);

    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ForegroundColor = color;
}

int PackFiles(string[] nuspecFiles, string licenseUrl)
{
    string commonArgs =
        $"-BasePath \"{BinDir}\" " +
        $"-OutputDirectory \"{OutDir}\" " +
        $"-prop licenseUrl=\"{licenseUrl}\" " +
        $"-prop version=\"{BuildVersion}\" " +
        $"-prop authors={Authors} " +
        $"-prop projectURL=\"{ProjectURL}\" " +
        $"-prop tags=\"{Tags}\" " +
        $"-prop thirdPartyNoticesPath=\"{ThirdPartyNoticesPath}\" " +
        $"-prop netCompilersPropsPath=\"{NetCompilersPropsPath}\" " +
        $"-prop emptyDirPath=\"{emptyDir}\" " +
        string.Join(" ", dependencyVersions.Select(d => $"-prop {d.VariableName}=\"{d.Value}\""));

    int exit = 0;
    foreach (var file in nuspecFiles)
    {
        var nugetArgs = $@"pack {file} {commonArgs}";

        var nugetExePath = Path.GetFullPath(Path.Combine(SolutionRoot, "nuget.exe"));
        var p = new Process();
        p.StartInfo.FileName = nugetExePath;
        p.StartInfo.Arguments = nugetArgs;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardError = true;

        Console.WriteLine($"{Environment.NewLine}Running: nuget.exe {nugetArgs}");

        p.Start();
        p.WaitForExit();

        var currentExit = p.ExitCode;
        if (currentExit != 0)
        {
            var stdErr = p.StandardError.ReadToEnd();
            string message;
            if (BuildingReleaseNugets && stdErr.Contains("A stable release of a package should not have on a prerelease dependency."))
            {
                // If we are building release nugets and if any packages have dependencies on prerelease packages  
                // then we want to ignore the error and allow the build to succeed.
                currentExit = 0;
                message = $"{file}: {stdErr}";
                Console.WriteLine(message);
            }
            else
            {
                message = $"{file}: error: {stdErr}";
                ReportError(message);
            }

            File.AppendAllText(ErrorLogFile, Environment.NewLine + message);
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

var roslynPackageNames = GetRoslynPackageNames();
GeneratePublishingConfig(roslynPackageNames);
string[] roslynNuspecFiles = roslynPackageNames.Select(f => Path.Combine(NuspecDirPath, f + ".nuspec")).ToArray();
int exit = PackFiles(roslynNuspecFiles, LicenseUrlRedist);

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
    ReportError(error);
}

Environment.Exit(exit);

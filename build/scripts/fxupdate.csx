#r "System.Net.Http.dll"

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

var knownRepos = new[] { "coreclr", "corefx", "projectk-tfs", "symreader" };

if (Args.Count < 1)
{
    Console.Error.WriteLine("Usage: fxupdate <Roslyn branch> [/c:<repo>=<commit-sha1>]");
    Console.Error.WriteLine($"Where <repo> is name of a repo: {string.Join(", ", knownRepos)}");
    return 1;
}

string roslynBranch = Args[0];

var commits = 
    Args.Skip(1).Where(a => a.StartsWith("/c:")).Select(a => a.Substring("/c:".Length)).
    ToDictionary(k => k.Split('=')[0].Trim(), k => k.Split('=')[1].Trim(), StringComparer.OrdinalIgnoreCase);

string unknownRepo = commits.Keys.FirstOrDefault(k => !knownRepos.Contains(k));
if (unknownRepo != null)
{
    Console.Error.WriteLine($"Unknown repo: {unknownRepo}");
    return 1;
}

string GetScriptPath([CallerFilePath]string path = null) => path;
string roslynRoot = Path.GetFullPath(Path.Combine(GetScriptPath(), "..", "..", ".."));

string coreFxChannel, coreClrChannel, symReaderChannel, projectkChannel;

switch (roslynBranch)
{
    case "master":
    case "future-stabilization":
        coreFxChannel = coreClrChannel = projectkChannel = symReaderChannel = "master";
        break;

    case "stabilization":
        coreFxChannel = coreClrChannel = projectkChannel = "release/1.0.0";
        symReaderChannel = "netcore1.0";
        break;

    default:
        Console.Error.WriteLine($"Error: Unexpected branch name: '{roslynBranch}'.");
        return 2;
}

// fetch latest CoreCLR and CoreFX package versions:
var client = new HttpClient();

IEnumerable<KeyValuePair<string, string>> ParsePackageVersions(string content) =>
    from line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
    let pair = line.Split(' ')
    select new KeyValuePair<string, string>(pair[0], pair[1]);

string GetVersionSuffix(string version)
{
    int dash = version.IndexOf('-');
    return (dash > 0) ? version.Substring(dash) : "";
}

string GetCommonVersionSuffix(IEnumerable<KeyValuePair<string, string>> packages)
{
    var firstPkg = packages.First();
    string firstSuffix = GetVersionSuffix(firstPkg.Value);
    var otherPkg = packages.FirstOrDefault(p => GetVersionSuffix(p.Value) != firstSuffix);
    if (otherPkg.Key != null)
    {
        Console.Error.WriteLine($"Error: Inconsistent version suffixes: {firstPkg.Key} {firstPkg.Value} vs {otherPkg.Key} {otherPkg.Value}");
        Environment.Exit(3);
    }

    return firstSuffix;
}

async Task<string> DownloadPackageList(string repo, string channel)
{
    string versionsUrl = "https://raw.githubusercontent.com/dotnet/versions";
    string commit = commits.ContainsKey(repo) ? "blob/" + commits[repo] : "master";
    string url = $"{versionsUrl}/{commit}/build-info/dotnet/{repo}/{channel}/Latest_Packages.txt";

    try
    {
        return await client.GetStringAsync(url);
    }
    catch (HttpRequestException e)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine($"Error downloading {url}");
        Console.Error.WriteLine(e.Message);
        Environment.Exit(4);
        return null;
    }
}

Write("Downloading list of CoreFX packages...");
var coreFXPackages = ParsePackageVersions(await DownloadPackageList("corefx", coreFxChannel));
var coreFXVersionSuffix = GetCommonVersionSuffix(coreFXPackages);
WriteLine($"Done. Version suffix: {coreFXVersionSuffix}");

Write("Downloading list of ProjectK packages ...");
var projectkPackages = ParsePackageVersions(await DownloadPackageList("projectk-tfs", projectkChannel));
WriteLine("Done.");

Write("Downloading list of CoreCLR packages ...");
var coreClrPackages = ParsePackageVersions(await DownloadPackageList("coreclr", coreClrChannel));
var coreClrVersionSuffix = GetCommonVersionSuffix(coreClrPackages);
WriteLine($"Done. Version suffix: {coreClrVersionSuffix}");

Write("Downloading list of SymReader packages ...");
var symReaderPackages = ParsePackageVersions(await DownloadPackageList("symreader", symReaderChannel));
WriteLine("Done.");

var packages = coreFXPackages.Concat(projectkPackages).Concat(coreClrPackages).Concat(symReaderPackages).ToArray();

WriteLine("Updating project.json files ...");

void UpdateProjectJsonFiles(string root)
{
    foreach (var filePath in Directory.EnumerateFiles(root, "project.json", SearchOption.AllDirectories))
    {
        Write($"  {filePath} ... ");

        string originalText = File.ReadAllText(filePath);
        string text = originalText;

        foreach (var package in packages)
        {
            // only update pre-release versions
            text = Regex.Replace(
                text,
                $"\"{package.Key}\": \"[0-9]+[.][0-9]+[.][0-9]+-[-a-zA-Z0-9]+\"",
                $"\"{package.Key}\": \"{package.Value}\"");
        }

        if (text != originalText)
        {
            File.WriteAllText(filePath, text);

            WriteLine("UPDATED");
        }
        else
        {
            WriteLine("OK");
        }
    }
}

UpdateProjectJsonFiles(Path.Combine(roslynRoot, "src"));
UpdateProjectJsonFiles(Path.Combine(roslynRoot, "build"));

WriteLine("Done.");

WriteLine("Updating VSL.Versions.targets ...");

void UpdateTargetsFile(string path)
{
    string originalText = File.ReadAllText(path);
    string newText = originalText;

    newText = UpdateVersionElement(newText, "CoreFXVersionSuffix", coreFXVersionSuffix);
    newText = UpdateVersionElement(newText, "CoreClrVersionSuffix", coreClrVersionSuffix);

    if (originalText != newText)
    {
        File.WriteAllText(path, newText);
    }
}

string UpdateVersionElement(string text, string elementName, string newValue)
{
    return Regex.Replace(
        text,
        $"<{elementName}>[^<]+</{elementName}>",
        $"<{elementName}>{newValue}</{elementName}>");
}

UpdateTargetsFile(Path.Combine(roslynRoot, "build", "Targets", "VSL.Versions.targets"));

WriteLine("Done.");


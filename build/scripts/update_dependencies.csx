#r "System.Net.Http.dll"
#r "System.Xml.Linq"

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

bool help = Args.Remove("/help") || Args.Remove("/?");

if (help || Args.Count > 0)
{
    Console.Error.WriteLine("Usage: update_dependencies.csx [/help]");
    Console.Error.WriteLine();
    return 1;
}

string GetScriptPath([CallerFilePath]string path = null) => path;
string roslynRoot = Path.GetFullPath(Path.Combine(GetScriptPath(), "..", "..", ".."));

string specPath = Path.Combine(roslynRoot, "dependencies.xml");
var spec = XDocument.Load(specPath);
var repos = spec.Element("dependencies").Elements("repo");
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

async Task<string> DownloadPackageList(string repo, string channel, bool lkg)
{
    string versionsUrl = "https://raw.githubusercontent.com/dotnet/versions";
    string url = $"{versionsUrl}/master/build-info/dotnet/{repo}/{channel}/{(lkg ? "LKG" : "Latest")}_Packages.txt";

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

var allPackages = new List<KeyValuePair<string, string>>();

foreach (var repo in repos)
{
    string name = repo.Attribute("name").Value;
    string channel = repo.Attribute("channel").Value;
    bool lkg = repo.Attribute("lkg")?.Value == "true";

    WriteLine($"Downloading list of '{name}' packages...");
    var packages = ParsePackageVersions(await DownloadPackageList(name, channel, lkg)).ToArray();

    WriteLine($"  Found {packages.Length} packages.");

    allPackages.AddRange(packages);
    WriteLine("Done.");
}

WriteLine("Updating project.json files ...");

void UpdateProjectJsonFiles(string root)
{
    foreach (var filePath in Directory.EnumerateFiles(root, "project.json", SearchOption.AllDirectories))
    {
        Write($"  {filePath} ... ");

        string originalText = File.ReadAllText(filePath);
        string text = originalText;

        foreach (var package in allPackages)
        {
            // only update pre-release versions
            text = Regex.Replace(
                text,
                $"\"{package.Key}\": \"[0-9]+[.][0-9]+[.][0-9]+(-[-a-zA-Z0-9]+)?\"",
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

void UpdateTargetsFile(string path)
{
    Write("Updating VSL.Versions.targets ... ");

    string originalText = File.ReadAllText(path);
    string newText = originalText;

    foreach (var package in allPackages)
    {
        var elementName = package.Key.Replace(".", "") + "Version";
        newText = UpdateVersionElement(newText, elementName, package.Value);
    }

    if (originalText != newText)
    {
        File.WriteAllText(path, newText);
        Console.WriteLine("UPDATED");
    }
    else
    {
        Console.WriteLine("OK");
    }
}

string UpdateVersionElement(string text, string elementName, string newValue)
{
    return Regex.Replace(
        text,
        $"<{elementName}>[^<]+</{elementName}>",
        $"<{elementName}>{newValue}</{elementName}>");
}

if (suffixes.Count > 0)
{
    UpdateTargetsFile(Path.Combine(roslynRoot, "build", "Targets", "Dependencies.props"));
}


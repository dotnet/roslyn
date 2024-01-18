#r "System.Xml.Linq"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

var nugetPackageDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
if (!Directory.Exists(nugetPackageDir))
{
    nugetPackageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
}

var versions = XDocument.Parse(File.ReadAllText(Path.Combine(GetScriptDirectory(), "Directory.Packages.props")));
var vssdkVersion = versions.Descendants().First(n => n.Name.LocalName == "PackageVersion" && n.Attribute("Include").Value == "Microsoft.VisualStudio.SDK").Attribute("Version").Value;

var vssdkPackageSpecPath = Path.Combine(nugetPackageDir, "microsoft.visualstudio.sdk", vssdkVersion, "microsoft.visualstudio.sdk.nuspec");

Console.WriteLine($"NuGet package directory: '{nugetPackageDir}'");
Console.WriteLine($"VSSDK version: '{vssdkVersion}'");
Console.WriteLine();

if (!File.Exists(vssdkPackageSpecPath))
{
    Console.Error.WriteLine($"File not found: {vssdkPackageSpecPath}. Please, restore packages.");
    return 1;
}

var vssdkPackageSpec = XDocument.Parse(File.ReadAllText(vssdkPackageSpecPath));

var properties = new List<string>();

foreach (var node in vssdkPackageSpec.Descendants())
{
    if (node.Name.LocalName == "dependency")
    {
        var id = node.Attribute("id")?.Value;
        var version = node.Attribute("version")?.Value;

        if (id is null || version is null)
            continue;

        if (!id.StartsWith("Microsoft.VisualStudio") &&
            !id.StartsWith("Microsoft.ServiceHub") &&
            id != "Newtonsoft.Json" &&
            id != "StreamJsonRpc")
        {
            continue;
        }

        if (!versions.Descendants().Any(n => n.Name.LocalName == "PackageVersion" && n.Attribute("Include").Value == id))
        {
            continue;
        }

        properties.Add($"<PackageVersion Include=\"{id}\" Version=\"{version}\" />");
    }
}

properties.Sort();

foreach (var property in properties)
{
    Console.WriteLine(property);
}

return 0;

static string GetScriptDirectory([CallerFilePath] string path = null)
    => Path.GetDirectoryName(path);

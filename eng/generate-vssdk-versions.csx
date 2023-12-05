#r "System.Xml.Linq"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

var nugetPackageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

var versions = XDocument.Parse(File.ReadAllText(Path.Combine(GetScriptDirectory(), "Versions.props")));
var vssdkVersion = versions.Descendants().First(n => n.Name.LocalName == "MicrosoftVisualStudioSdkVersion").Value;

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

        var versionProperty = id.Replace(".", "") + "Version";
        if (versions.Descendants().Any(n => n.Name == versionProperty) &&
            (versionProperty.StartsWith("MicrosoftVisualStudio") ||
             versionProperty.StartsWith("MicrosoftServiceHub") ||
             versionProperty.StartsWith("NewtonsoftJson") ||
             versionProperty.StartsWith("StreamJsonRpc")))
        {
            properties.Add($"<{versionProperty}>{version}</{versionProperty}>");
        }
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

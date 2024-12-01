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

var properties = new List<(string packageId, string version)>();

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
            !id.StartsWith("Microsoft.Build") &&
            id != "Newtonsoft.Json" &&
            id != "StreamJsonRpc" &&
            id != "Nerdbank.Streams")
        {
            continue;
        }

        if (!versions.Descendants().Any(n => n.Name.LocalName == "PackageVersion" && n.Attribute("Include").Value == id))
        {
            continue;
        }

        properties.Add((id, version));
    }
}

properties.Sort();

var seenMsbuild = false;
foreach (var (id, version) in properties)
{
    if (!id.StartsWith("Microsoft.Build"))
    {
        Console.WriteLine($"<PackageVersion Include=\"{id}\" Version=\"{version}\" />");
    }
    else if (!seenMsbuild)
    {
        Console.WriteLine($$"""
            <ItemGroup Condition="'$(DotNetBuildSourceOnly)' != 'true' and '$(TargetFramework)' == 'net472'">
              <PackageVersion Include="Microsoft.Build" Version="{{version}}" />
              <PackageVersion Include="Microsoft.Build.Framework" Version="{{version}}" />
              <PackageVersion Include="Microsoft.Build.Tasks.Core" Version="{{version}}" />
            </ItemGroup>
            """);

        seenMsbuild = true;
    }
}

return 0;

static string GetScriptDirectory([CallerFilePath] string path = null)
    => Path.GetDirectoryName(path);

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#:property TargetFramework=net472
#:property PublishAot=false
#:package Microsoft.Build.Locator

using Microsoft.Build.Locator;
using System.Reflection;

var instances = MSBuildLocator.QueryVisualStudioInstances()
    .Where(vs => vs.DiscoveryType != DiscoveryType.DotNetSdk)
    .OrderByDescending(vs => vs.Version)
    .ToArray();

Console.Error.WriteLine($"Found VS instances ({instances.Length}):");
foreach (var candidate in instances)
{
    Console.Error.WriteLine($"  [{candidate.Version}] {candidate.VisualStudioRootPath}");
}

var instance = instances.First();
var cscDir = Path.Combine(instance.MSBuildPath, "Roslyn");
var cscPath = Path.Combine(cscDir, "csc.exe");
Console.Error.WriteLine($"Using csc: {cscPath}");

AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, e) =>
{
    try
    {
        var name = new AssemblyName(e.Name);
        var path = Path.Combine(cscDir, name.Name + ".dll");
        if (File.Exists(path))
        {
            return Assembly.ReflectionOnlyLoadFrom(path);
        }

        return Assembly.ReflectionOnlyLoad(e.Name);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to resolve assembly '{e.Name}': {ex}");
        return null;
    }
};

var assembly = Assembly.ReflectionOnlyLoadFrom(cscPath);
var commitHash = (string)assembly.GetCustomAttributesData()
    .Single(a => a.AttributeType.FullName == "Microsoft.CodeAnalysis.CommitHashAttribute")
    .ConstructorArguments[0].Value;

Console.WriteLine(commitHash);

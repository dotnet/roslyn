// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PostSharp.Engineering.BuildTools.Build;

namespace Build.NuGetDependencies;

internal class NuspecDependenciesSource : NuGetDependenciesSourceBase
{
    private readonly IEnumerable<string> _topLevelPackages;

    public NuspecDependenciesSource(IEnumerable<string> topLevelPackages)
    {
        _topLevelPackages = topLevelPackages;
    }

    public override bool GetDependencies(BuildContext context, out IEnumerable<string> dependencies)
    {
        List<string> dependenciesList = new();
        var success = true;

        var nextPackagesToListDependencies = _topLevelPackages.ToHashSet();
        var packagesWithListedDependencies = new HashSet<string>();

        while (nextPackagesToListDependencies.Count > 0)
        {
            var currentPackagesToListDependencies = nextPackagesToListDependencies;
            nextPackagesToListDependencies = new();

            foreach (var packagePath in currentPackagesToListDependencies)
            {
                if (!packagesWithListedDependencies.Add(packagePath))
                {
                    continue;
                }

                var packageDirectory = Path.GetDirectoryName(packagePath)!;
                var nuspecFiles = Directory.GetFiles(packageDirectory, "*.nuspec");

                if (nuspecFiles.Length != 1)
                {
                    context.Console.WriteError(
                        $"There's {nuspecFiles.Length} nuspec files instead of one for '{packagePath}' package.");

                    success = false;

                    continue;
                }

                context.Console.WriteMessage($"Processing '{nuspecFiles[0]}' of '{packagePath}'.");

                var nuspec = XDocument.Load(nuspecFiles[0]).Root!;
                XNamespace ns = nuspec.Attribute("xmlns")!.Value;
                var dependenciesElement = nuspec.Element(ns + "metadata")!.Element(ns + "dependencies");

                if (dependenciesElement == null)
                {
                    continue;
                }

                var dependenciesWithoutGroups = dependenciesElement.Elements(ns + "dependency");
                var dependenciesWithGroups = dependenciesElement.Elements(ns + "group").Elements(ns + "dependency");
                var dependencyPackagePaths = dependenciesWithoutGroups.Concat(dependenciesWithGroups).Select(d =>
                    GetPackagePath(d.Attribute("id")!.Value, d.Attribute("version")!.Value)).ToArray();

                foreach (var dependencyPackagePath in dependencyPackagePaths)
                {
                    if (File.Exists(dependencyPackagePath))
                    {
                        nextPackagesToListDependencies.Add(dependencyPackagePath);
                        dependenciesList.Add(dependencyPackagePath);
                    }
                    else
                    {
                        context.Console.WriteImportantMessage(
                            $"'{dependencyPackagePath}' doesn't exist. Origin: {nuspecFiles[0]}");
                    }
                }
            }
        }

        dependencies = dependenciesList.ToImmutableList();
        return success;
    }
}

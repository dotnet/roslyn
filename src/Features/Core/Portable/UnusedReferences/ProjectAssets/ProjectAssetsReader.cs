// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets
{
    // This class will read the dependency heirarchy from the project.assets.json file. The format of this file
    // is subject to change and in the future this information will be provided by an API.  See https://github.com/dotnet/roslyn/issues/50054
    internal static class ProjectAssetsReader
    {
        // NuGet will include entries to keep empty folders from being removed. These entries can be ignored.
        private const string NuGetEmptyFileName = "_._";

        /// <summary>
        /// Enhances references with the assemblies they bring into the compilation and their dependency hierarchy.
        /// </summary>
        public static ImmutableArray<ReferenceInfo> EnhanceReferences(
            ImmutableArray<ReferenceInfo> projectReferences,
            ProjectAssetsFile projectAssets)
        {
            if (projectAssets is null ||
                projectAssets.Version != 3)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            if (projectAssets.Targets is null ||
                projectAssets.Targets.Count == 0)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            if (projectAssets.Libraries is null ||
                projectAssets.Libraries.Count == 0)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            var autoReferences = projectAssets.Project?.Frameworks?.Values
                .Where(framework => framework.Dependencies != null)
                .SelectMany(framework => framework.Dependencies!.Keys.Where(key => framework.Dependencies[key].AutoReferenced))
                .Distinct()
                .ToImmutableHashSet();
            autoReferences ??= ImmutableHashSet<string>.Empty;

            var targetLibraryKeys = projectAssets.Targets
                .ToImmutableDictionary(t => t.Key, t => t.Value.ToImmutableDictionary(l => l.Key.Split('/')[0], l => l.Key));

            var builtReferences = new Dictionary<string, ReferenceInfo?>();

            var references = projectReferences
                .Select(projectReference => EnhanceReference(projectAssets, projectReference, autoReferences, targetLibraryKeys, builtReferences))
                .WhereNotNull()
                .ToImmutableArray();

            return references;
        }

        private static ReferenceInfo? EnhanceReference(
            ProjectAssetsFile projectAssets,
            ReferenceInfo referenceInfo,
            ImmutableHashSet<string> autoReferences,
            ImmutableDictionary<string, ImmutableDictionary<string, string>> targetLibraryKeys,
            Dictionary<string, ReferenceInfo?> builtReferences)
        {
            var referenceName = referenceInfo.ReferenceType == ReferenceType.Project
                ? Path.GetFileNameWithoutExtension(referenceInfo.ItemSpecification)
                : referenceInfo.ItemSpecification;

            if (autoReferences.Contains(referenceName))
            {
                return null;
            }

            var reference = BuildReference(projectAssets, referenceName, referenceInfo.TreatAsUsed, targetLibraryKeys, builtReferences);

            // Since the reference being enhanced was provided by the Project System we should always return an
            // enhanced reference with its original ItemSpecification. The project assets file typically works with
            // full paths, however project reference typically are relative. This ensures that when changes are
            // persisted back by the Project System, it will match the specification it is expecting.
            return reference?.WithItemSpecification(referenceInfo.ItemSpecification);
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
            string referenceName,
            bool treatAsUsed,
            ImmutableDictionary<string, ImmutableDictionary<string, string>> targetLibraryKeys,
            Dictionary<string, ReferenceInfo?> builtReferences)
        {
            var dependencyNames = new HashSet<string>();
            var compilationAssemblies = ImmutableArray.CreateBuilder<string>();
            var referenceType = ReferenceType.Unknown;
            var itemSpecification = referenceName;

            var packagesPath = projectAssets.Project?.Restore?.PackagesPath ?? string.Empty;

            RoslynDebug.AssertNotNull(projectAssets.Targets);
            RoslynDebug.AssertNotNull(projectAssets.Libraries);

            foreach (var libraryKeysKvp in targetLibraryKeys)
            {
                if (!libraryKeysKvp.Value.TryGetValue(referenceName, out var key) ||
                    !projectAssets.Libraries.TryGetValue(key, out var library))
                {
                    continue;
                }

                var target = projectAssets.Targets[libraryKeysKvp.Key];
                var targetLibrary = target[key];

                referenceType = targetLibrary.Type switch
                {
                    "package" => ReferenceType.Package,
                    "project" => ReferenceType.Project,
                    _ => ReferenceType.Assembly
                };

                if (referenceType == ReferenceType.Project &&
                    library.Path is not null)
                {
                    // Project references are keyed by their filename but the
                    // item specification should be the path to the project file
                    // with Windows-style directory separators.
                    itemSpecification = library.Path.Replace('/', '\\');
                }

                if (targetLibrary.Dependencies != null)
                {
                    dependencyNames.AddRange(targetLibrary.Dependencies.Keys);
                }

                if (targetLibrary.Compile != null)
                {
                    foreach (var kvp in targetLibrary.Compile)
                    {
                        var assemblyPath = kvp.Key;
                        if (!assemblyPath.EndsWith(NuGetEmptyFileName))
                        {
                            compilationAssemblies.Add(Path.GetFullPath(Path.Combine(packagesPath, library.Path ?? "", assemblyPath)));
                        }
                    }
                }
            }

            if (referenceType == ReferenceType.Unknown)
            {
                return null;
            }
            
            if (referenceType == ReferenceType.Package && itemSpecification == ".NETStandard.Library")
            {
                // This depenedency is large and not useful in determining whether the parent reference has been used.
                return null;
            }

            if (builtReferences.TryGetValue(itemSpecification, out var builtReference))
            {
                return builtReference;
            }

            var dependencies = dependencyNames
                .Select(dependency => BuildReference(projectAssets, dependency, treatAsUsed: false, targetLibraryKeys, builtReferences))
                .WhereNotNull()
                .ToImmutableArray();

            var reference = new ReferenceInfo(referenceType, itemSpecification, treatAsUsed, compilationAssemblies.ToImmutable(), dependencies);

            builtReferences.Add(reference.ItemSpecification, reference);

            return reference;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.UnusedReferences;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets
{
    // This class will read the dependency heirarchy from the project.assets.json file. The format of this file
    // is subject to change and in the future this information will be provided by an API with the ProjectSystem.
    internal static class ProjectAssetsReader
    {
        public static ImmutableArray<ReferenceInfo> ReadReferences(
            ImmutableArray<ReferenceInfo> projectReferences,
            string projectAssetsFilePath,
            string targetFrameworkMoniker)
        {
            var projectAssetsFileContents = File.ReadAllText(projectAssetsFilePath);
            var projectAssets = JsonConvert.DeserializeObject<ProjectAssetsFile>(projectAssetsFileContents);

            if (projectAssets is null ||
                projectAssets.Version != 3)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            if (projectAssets.Targets is null ||
                !projectAssets.Targets.TryGetValue(targetFrameworkMoniker, out var target))
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            var autoReferences = projectAssets.Project?.Frameworks?.Values
                .SelectMany(framework => framework.Dependencies?.Keys.Where(key => framework.Dependencies[key].AutoReferenced))
                .Distinct()
                .ToImmutableHashSet();
            autoReferences ??= ImmutableHashSet<string>.Empty;

            var references = projectReferences
                .Select(projectReference => BuildReference(projectAssets, target, projectReference, autoReferences))
                .OfType<ReferenceInfo>()
                .ToImmutableArray();

            return references;
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
            Dictionary<string, ProjectAssetsTargetLibrary> target,
            ReferenceInfo projectReference,
            ImmutableHashSet<string> autoReferences)
        {
            var referenceName = projectReference.ReferenceType == ReferenceType.Project
                ? Path.GetFileNameWithoutExtension(projectReference.ItemSpecification)
                : projectReference.ItemSpecification;

            if (autoReferences.Contains(referenceName))
            {
                return null;
            }

            return BuildReference(projectAssets, target, referenceName, projectReference.TreatAsUsed);
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
            Dictionary<string, ProjectAssetsTargetLibrary> target,
            string dependency,
            bool treatAsUsed)
        {
            var key = target.Keys.FirstOrDefault(library => library.Split('/')[0] == dependency);
            if (key is null)
            {
                return null;
            }

            return BuildReference(projectAssets, target, dependency, treatAsUsed, key, target[key]);
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
            Dictionary<string, ProjectAssetsTargetLibrary> target,
            string referenceName,
            bool treatAsUsed,
            string key,
            ProjectAssetsTargetLibrary targetLibrary)
        {
            if (projectAssets.Libraries is null ||
                !projectAssets.Libraries.TryGetValue(key, out var library))
            {
                return null;
            }

            var type = targetLibrary.Type switch
            {
                "package" => ReferenceType.Package,
                "project" => ReferenceType.Project,
                _ => ReferenceType.Assembly
            };

            var dependencies = targetLibrary.Dependencies != null
                ? targetLibrary.Dependencies.Keys
                    .Select(dependency => BuildReference(projectAssets, target, dependency, treatAsUsed: false))
                    .OfType<ReferenceInfo>()
                    .ToImmutableArray()
                : ImmutableArray<ReferenceInfo>.Empty;

            var packagesPath = projectAssets.Project?.Restore?.PackagesPath ?? string.Empty;
            var compilationAssemblies = targetLibrary.Compile != null
                ? targetLibrary.Compile.Keys
                    .Where(assemblyPath => !assemblyPath.EndsWith("_._"))
                    .Select(assemblyPath => Path.GetFullPath(Path.Combine(packagesPath, library.Path, assemblyPath)))
                    .ToImmutableArray()
                : ImmutableArray<string>.Empty;

            return new ReferenceInfo(type, referenceName, treatAsUsed, compilationAssemblies, dependencies);
        }
    }
}

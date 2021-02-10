// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.UnusedReferences;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets
{
    // This class will read the dependency heirarchy from the project.assets.json file. The format of this file
    // is subject to change and in the future this information will be provided by an API.  See https://github.com/dotnet/roslyn/issues/50054
    internal static class ProjectAssetsReader
    {
        // NuGet will include entries to keep empty folders from being removed. These entries can be ignored.
        private const string NuGetEmptyFileName = "_._";

        public static ImmutableArray<ReferenceInfo> ReadReferences(
            ImmutableArray<ReferenceInfo> projectReferences,
            string projectAssetsFilePath,
            string targetFrameworkMoniker)
        {
            if (!File.Exists(projectAssetsFilePath))
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            var projectAssetsFileContents = File.ReadAllText(projectAssetsFilePath);
            ProjectAssetsFile projectAssets;

            try
            {
                projectAssets = JsonConvert.DeserializeObject<ProjectAssetsFile>(projectAssetsFileContents);
            }
            catch
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

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
                .WhereNotNull()
                .ToImmutableArray();

            return references;
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
            Dictionary<string, ProjectAssetsTargetLibrary> target,
            ReferenceInfo referenceInfo,
            ImmutableHashSet<string> autoReferences)
        {
            var referenceName = referenceInfo.ReferenceType == ReferenceType.Project
                ? Path.GetFileNameWithoutExtension(referenceInfo.ItemSpecification)
                : referenceInfo.ItemSpecification;

            if (autoReferences.Contains(referenceName))
            {
                return null;
            }

            return BuildReference(projectAssets, target, referenceName, referenceInfo.TreatAsUsed);
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
                    .WhereNotNull()
                    .ToImmutableArray()
                : ImmutableArray<ReferenceInfo>.Empty;

            var packagesPath = projectAssets.Project?.Restore?.PackagesPath ?? string.Empty;
            var compilationAssemblies = targetLibrary.Compile != null
                ? targetLibrary.Compile.Keys
                    .Where(assemblyPath => !assemblyPath.EndsWith(NuGetEmptyFileName))
                    .Select(assemblyPath => Path.GetFullPath(Path.Combine(packagesPath, library.Path, assemblyPath)))
                    .ToImmutableArray()
                : ImmutableArray<string>.Empty;

            return new ReferenceInfo(type, referenceName, treatAsUsed, compilationAssemblies, dependencies);
        }
    }
}

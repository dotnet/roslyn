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
            string projectAssetsFilePath)
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
                .SelectMany(framework => framework.Dependencies?.Keys.Where(key => framework.Dependencies[key].AutoReferenced))
                .Distinct()
                .ToImmutableHashSet();
            autoReferences ??= ImmutableHashSet<string>.Empty;

            var references = projectReferences
                .Select(projectReference => BuildReference(projectAssets, projectReference, autoReferences))
                .WhereNotNull()
                .ToImmutableArray();

            return references;
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
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

            return BuildReference(projectAssets, referenceName, referenceInfo.TreatAsUsed);
        }

        private static ReferenceInfo? BuildReference(
            ProjectAssetsFile projectAssets,
            string referenceName,
            bool treatAsUsed)
        {
            var dependencyNames = new HashSet<string>();
            var compilationAssemblies = ImmutableArray.CreateBuilder<string>();
            var referenceType = ReferenceType.Unknown;

            var packagesPath = projectAssets.Project?.Restore?.PackagesPath ?? string.Empty;

            RoslynDebug.AssertNotNull(projectAssets.Targets);
            RoslynDebug.AssertNotNull(projectAssets.Libraries);

            foreach (var target in projectAssets.Targets.Values)
            {
                var key = target.Keys.FirstOrDefault(library => library.Split('/')[0] == referenceName);
                if (key is null ||
                    !projectAssets.Libraries.TryGetValue(key, out var library))
                {
                    continue;
                }

                var targetLibrary = target[key];

                referenceType = targetLibrary.Type switch
                {
                    "package" => ReferenceType.Package,
                    "project" => ReferenceType.Project,
                    _ => ReferenceType.Assembly
                };

                if (targetLibrary.Dependencies != null)
                {
                    dependencyNames.AddRange(targetLibrary.Dependencies.Keys);
                }

                if (targetLibrary.Compile != null)
                {
                    compilationAssemblies.AddRange(targetLibrary.Compile.Keys
                        .Where(assemblyPath => !assemblyPath.EndsWith(NuGetEmptyFileName))
                        .Select(assemblyPath => Path.GetFullPath(Path.Combine(packagesPath, library.Path, assemblyPath))));
                }
            }

            if (referenceType == ReferenceType.Unknown)
            {
                return null;
            }

            var dependencies = dependencyNames
                .Select(dependency => BuildReference(projectAssets, dependency, treatAsUsed: false))
                .WhereNotNull()
                .ToImmutableArray();

            return new ReferenceInfo(referenceType, referenceName, treatAsUsed, compilationAssemblies.ToImmutable(), dependencies);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.UnusedReferences.ProjectAssets
{
    internal static partial class TestProjectAssetsFile
    {
        public static ProjectAssetsFile Create(int version, string targetFramework, ImmutableArray<ReferenceInfo> references)
        {
            var allReferences = new List<ReferenceInfo>();
            FlattenReferences(references, allReferences);

            var libraries = BuildLibraries(allReferences);
            var targets = BuildTargets(targetFramework, allReferences);
            var project = BuildProject(targetFramework);
            var projectAssets = new ProjectAssetsFile()
            {
                Version = version,
                Targets = targets,
                Libraries = libraries,
                Project = project
            };

            return projectAssets;
        }

        private static void FlattenReferences(ImmutableArray<ReferenceInfo> references, List<ReferenceInfo> allReferences)
        {
            foreach (var reference in references)
                FlattenReference(reference, allReferences);
        }

        private static void FlattenReference(ReferenceInfo reference, List<ReferenceInfo> allReferences)
        {
            allReferences.Add(reference);
            FlattenReferences(reference.Dependencies, allReferences);
        }

        private static Dictionary<string, ProjectAssetsLibrary> BuildLibraries(List<ReferenceInfo> references)
        {
            var libraries = new Dictionary<string, ProjectAssetsLibrary>();
            foreach (var reference in references)
            {
                var library = new ProjectAssetsLibrary() { Path = reference.ItemSpecification };
                libraries.Add(Path.GetFileNameWithoutExtension(library.Path), library);
            }

            return libraries;
        }

        private static Dictionary<string, Dictionary<string, ProjectAssetsTargetLibrary>> BuildTargets(string targetFramework, List<ReferenceInfo> references)
        {
            var libraries = new Dictionary<string, ProjectAssetsTargetLibrary>();
            foreach (var reference in references)
            {
                var dependencies = BuildDependencies(reference.Dependencies);
                var library = new ProjectAssetsTargetLibrary()
                {
                    Type = GetLibraryType(reference.ReferenceType),
                    Compile = new Dictionary<string, ProjectAssetsTargetLibraryCompile>() { { Path.ChangeExtension(reference.ItemSpecification, "dll"), new ProjectAssetsTargetLibraryCompile() } },
                    Dependencies = dependencies
                };
                libraries[Path.GetFileNameWithoutExtension(reference.ItemSpecification)] = library;
            }

            return new Dictionary<string, Dictionary<string, ProjectAssetsTargetLibrary>>() { { targetFramework, libraries } };
        }

        private static string GetLibraryType(ReferenceType referenceType)
        {
            return referenceType switch
            {
                ReferenceType.Package => "package",
                ReferenceType.Project => "project",
                _ => "assembly"
            };
        }

        private static Dictionary<string, string> BuildDependencies(ImmutableArray<ReferenceInfo> references)
        {
            return references.ToDictionary(reference => Path.GetFileNameWithoutExtension(reference.ItemSpecification), reference => string.Empty);
        }

        private static ProjectAssetsProject BuildProject(string targetFramework)
        {
            // Frameworks won't always specify a set of dependencies.
            // This ensures the project asset reader does not error in these cases.
            return new ProjectAssetsProject()
            {
                Frameworks = new Dictionary<string, ProjectAssetsProjectFramework>() { { targetFramework, new ProjectAssetsProjectFramework() } }
            };
        }
    }
}

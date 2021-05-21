// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal static class UnusedReferencesRemover

    {
        // This is the order that we look for used references. We set this processing order because we
        // want to favor transitive references when possible. For instance we process Projects before
        // Packages, since a particular Package could be brought in transitively by a Project reference.
        private static readonly ReferenceType[] _processingOrder = new[]
        {
            ReferenceType.Project,
            ReferenceType.Package,
            ReferenceType.Assembly
        };

        public static async Task<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(
            Solution solution,
            string projectFilePath,
            ImmutableArray<ReferenceInfo> references,
            CancellationToken cancellationToken)
        {
            var projects = solution.Projects
                .Where(project => projectFilePath.Equals(project.FilePath, System.StringComparison.OrdinalIgnoreCase));

            HashSet<string> usedAssemblyFilePaths = new();

            foreach (var project in projects)
            {
                // Create a lookup of used assembly paths
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation is null)
                {
                    continue;
                }

                var usedAssemblyReferences = compilation.GetUsedAssemblyReferences(cancellationToken);

                usedAssemblyFilePaths.AddRange(usedAssemblyReferences
                    .OfType<PortableExecutableReference>()
                    .Select(reference => reference.FilePath)
                    .WhereNotNull());
            }

            return GetUnusedReferences(usedAssemblyFilePaths, references);
        }

        internal static ImmutableArray<ReferenceInfo> GetUnusedReferences(
            HashSet<string> usedAssemblyFilePaths,
            ImmutableArray<ReferenceInfo> references)
        {
            var unusedReferencesBuilder = ImmutableArray.CreateBuilder<ReferenceInfo>();
            var referencesByType = references.GroupBy(reference => reference.ReferenceType)
                .ToDictionary(group => group.Key, group => group.ToImmutableArray());

            // In this method we will determine which references bring in used assemblies and which don't.
            // Once we know a reference is "used", meaning brings in a used assembly, then we have answered
            // the question of which reference is responsible for bringing in all those compilation assemblies
            // (both directly and transitively). So, we will remove them from our lookup and proceed to determine
            // the source of the remaining used assemblies. The remaining references will need to bring in a
            // different used assembly into the compilation to be considered necessary.
            //
            // We will process the list of references twice. First we will look at the compilation assemblies
            // brought in directly by the reference to see if any are used. Then, after all references direct
            // compilation assemblies have been considered, we will expand our search and look at all compilation
            // assemblies brought in transitively by each reference.

            // Pass 1: Find all directly used references and remove them.
            foreach (var referenceType in _processingOrder)
            {
                if (!referencesByType.TryGetValue(referenceType, out var referencesForReferenceType))
                {
                    continue;
                }

                var unusedReferences = RemoveDirectlyUsedReferences(
                    referencesForReferenceType,
                    usedAssemblyFilePaths);

                // Update with the references that are remaining.
                if (unusedReferences.IsEmpty)
                {
                    referencesByType.Remove(referenceType);
                }
                else
                {
                    referencesByType[referenceType] = unusedReferences;
                }
            }

            // Pass 2: Find all transitively used refrences and remove them.
            foreach (var referenceType in _processingOrder)
            {
                if (!referencesByType.TryGetValue(referenceType, out var referencesForReferenceType))
                {
                    continue;
                }

                var unusedReferences = RemoveTransitivelyUsedReferences(
                    referencesForReferenceType,
                    usedAssemblyFilePaths);

                // If a references isn't directly or transitively used, then we will consider it unused.
                unusedReferencesBuilder.AddRange(unusedReferences);
            }

            return unusedReferencesBuilder.ToImmutable();
        }

        private static ImmutableArray<ReferenceInfo> RemoveDirectlyUsedReferences(
            ImmutableArray<ReferenceInfo> references,
            HashSet<string> usedAssemblyFilePaths)
        {
            // In this method we will check if a reference directly brings in a used compilation assembly.
            //
            //    references: [ PackageReference(compilationAssembly: "/libs/Used.dll") ],
            //    usedAssemblyLookup: [ "/libs/Used.dll" ]
            //

            var unusedReferencesBuilder = ImmutableArray.CreateBuilder<ReferenceInfo>();

            foreach (var reference in references)
            {
                // We will look at the compilation assemblies brought in directly by the
                // references to see if they are used.
                if (!reference.CompilationAssemblies.Any(usedAssemblyFilePaths.Contains))
                {
                    // None of the assemblies brought into this compilation are in the
                    // used assemblies list, so we will consider the reference unused.
                    unusedReferencesBuilder.Add(reference);
                    continue;
                }

                // Remove all assemblies that are brought into this compilation by this reference.
                usedAssemblyFilePaths.ExceptWith(GetAllCompilationAssemblies(reference));

            }

            return unusedReferencesBuilder.ToImmutable();
        }

        private static ImmutableArray<ReferenceInfo> RemoveTransitivelyUsedReferences(
            ImmutableArray<ReferenceInfo> references,
            HashSet<string> usedAssemblyFilePaths)
        {
            // In this method we will check if a reference transitively brings in a used compilation assembly.
            //
            //    references: [
            //      ProjectReference(
            //        compilationAssembly: "/libs/Unused.dll",
            //        dependencies: [ PackageReference(compilationAssembly: "/libs/Used.dll") ]
            //      ) ]
            //    usedAssemblyLookup: [ "/libs/Used.dll" ]
            //

            var unusedReferencesBuilder = ImmutableArray.CreateBuilder<ReferenceInfo>();

            foreach (var reference in references)
            {
                // Get all compilation assemblies brought in by this reference so we
                // can determine if any of them are used.
                var allCompilationAssemblies = GetAllCompilationAssemblies(reference);
                if (allCompilationAssemblies.IsEmpty)
                {
                    // We will consider References that do not contribute any assemblies to the
                    // compilation, such as Analyzer packages, as used.
                    continue;
                }

                if (!allCompilationAssemblies.Any(usedAssemblyFilePaths.Contains))
                {
                    // None of the assemblies brought into this compilation are in the
                    // used assemblies list, so we will consider the reference unused.
                    unusedReferencesBuilder.Add(reference);
                    continue;
                }

                // Remove all assemblies that are brought into this compilation by this reference.
                usedAssemblyFilePaths.ExceptWith(allCompilationAssemblies);
            }

            return unusedReferencesBuilder.ToImmutable();
        }

        internal static ImmutableArray<string> GetAllCompilationAssemblies(ReferenceInfo reference)
        {
            var transitiveCompilationAssemblies = reference.Dependencies
                .SelectMany(dependency => GetAllCompilationAssemblies(dependency));
            return reference.CompilationAssemblies
                .Concat(transitiveCompilationAssemblies)
                .ToImmutableArray();
        }

        public static async Task<Solution> UpdateReferencesAsync(
            Solution solution,
            string projectFilePath,
            ImmutableArray<ReferenceUpdate> referenceUpdates,
            CancellationToken cancellationToken)
        {
            var referenceCleanupService = solution.Workspace.Services.GetRequiredService<IReferenceCleanupService>();

            await ApplyReferenceUpdatesAsync(referenceCleanupService, projectFilePath, referenceUpdates, cancellationToken).ConfigureAwait(true);

            return solution.Workspace.CurrentSolution;
        }

        internal static async Task ApplyReferenceUpdatesAsync(
            IReferenceCleanupService referenceCleanupService,
            string projectFilePath,
            ImmutableArray<ReferenceUpdate> referenceUpdates,
            CancellationToken cancellationToken)
        {
            foreach (var referenceUpdate in referenceUpdates)
            {
                // If the update action would not change the reference, then
                // continue to the next update.
                if (referenceUpdate.Action == UpdateAction.TreatAsUnused &&
                    !referenceUpdate.ReferenceInfo.TreatAsUsed)
                {
                    continue;
                }
                else if (referenceUpdate.Action == UpdateAction.TreatAsUsed &&
                    referenceUpdate.ReferenceInfo.TreatAsUsed)
                {
                    continue;
                }
                else if (referenceUpdate.Action == UpdateAction.None)
                {
                    continue;
                }

                await referenceCleanupService.TryUpdateReferenceAsync(
                    projectFilePath,
                    referenceUpdate,
                    cancellationToken).ConfigureAwait(true);
            }
        }
    }
}

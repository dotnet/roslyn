// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal static class UnusedReferencesService
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
            Project project,
            ImmutableArray<ReferenceInfo> references,
            CancellationToken cancellationToken)
        {
            // Create a lookup of used assembly paths
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
            {
                return ImmutableArray<ReferenceInfo>.Empty;
            }

            var usedAssemblyReferences = compilation.GetUsedAssemblyReferences(cancellationToken);
            HashSet<string> usedAssemblyFilePaths = new(usedAssemblyReferences
                .OfType<PortableExecutableReference>()
                .Select(reference => reference.FilePath)
                .OfType<string>());

            return GetUnusedReferences(usedAssemblyFilePaths, references);
        }

        internal static ImmutableArray<ReferenceInfo> GetUnusedReferences(
            HashSet<string> usedAssemblyFilePaths,
            ImmutableArray<ReferenceInfo> references)
        {
            var unusedReferencesBuilder = ImmutableArray.CreateBuilder<ReferenceInfo>();
            var referencesByType = references.GroupBy(reference => reference.ReferenceType)
                .ToImmutableDictionary(group => group.Key);

            // We process the references in order by their type. This means we favor transitive references
            // over direct references where possible.
            foreach (var referenceType in _processingOrder)
            {
                if (!referencesByType.TryGetValue(referenceType, out var referencesForReferenceType))
                {
                    continue;
                }

                AddUnusedReferences(
                    referencesForReferenceType.ToImmutableArray(),
                    usedAssemblyFilePaths,
                    unusedReferencesBuilder);
            }

            return unusedReferencesBuilder.ToImmutableArray();
        }

        private static void AddUnusedReferences(
            ImmutableArray<ReferenceInfo> references,
            HashSet<string> usedAssemblyFilePaths,
            ImmutableArray<ReferenceInfo>.Builder unusedReferencesBuilder)
        {

            // This method checks for used reference two different ways.

            // #1. We check if a reference directly brings in a used compilation assembly.
            //
            //    references: [ PackageReference(compilationAssembly: "/libs/Used.dll") ],
            //    usedAssemblyLookup: [ "/libs/Used.dll" ]
            //

            // #2. We check if a reference transitively brings in a used compilation assembly.
            //
            //    references: [
            //      ProjectReference(
            //        compilationAssembly: "/libs/Unused.dll",
            //        dependencies: [ PackageReference(compilationAssembly: "/libs/Used.dll") ]
            //      ) ]
            //    usedAssemblyLookup: [ "/libs/Used.dll" ]

            // Check #1. we will look at the compilation assemblies brought in directly by the
            // references to see if they are used.
            var usedDirectReferences = references.Where(reference
                => reference.CompilationAssemblies.Any(usedAssemblyFilePaths.Contains)).ToArray();

            // We then want to gather all the assemblies brought in directly or transitively by
            // these used assemblies so that we can remove them from our lookup.
            var usedAssemblyReferences = usedDirectReferences.SelectMany(reference
                => GetAllCompilationAssemblies(reference)).ToArray();
            usedAssemblyFilePaths.ExceptWith(usedAssemblyReferences);

            // Now we want to look at the remaining possibly unused references to see if any of
            // the assemblies they transitively bring in to the compilation are used.
            var remainingReferences = references.Except(usedDirectReferences).ToArray();

            foreach (var reference in remainingReferences)
            {
                // Check #2. Get all compilation assemblies brought in by this reference so we
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
                    // None of the assemblies brought into this compilation are in the remaining
                    // used assemblies list, so we will consider the reference unused.
                    unusedReferencesBuilder.Add(reference);
                    continue;
                }

                // Remove all assemblies that are brought into this compilation by this reference.
                usedAssemblyFilePaths.ExceptWith(allCompilationAssemblies);
            }

            return;

            static ImmutableArray<string> GetAllCompilationAssemblies(ReferenceInfo reference)
            {
                var transitiveCompilationAssemblies = reference.Dependencies
                    .SelectMany(dependency => GetAllCompilationAssemblies(dependency));
                return reference.CompilationAssemblies
                    .Concat(transitiveCompilationAssemblies)
                    .ToImmutableArray();
            }
        }

        public static async Task<Project> UpdateReferencesAsync(
            Project project,
            ImmutableArray<ReferenceUpdate> referenceUpdates,
            CancellationToken cancellationToken)
        {
            var referenceCleanupService = project.Solution.Workspace.Services.GetRequiredService<IReferenceCleanupService>();

            await ApplyReferenceUpdatesAsync(referenceCleanupService, project.FilePath!, referenceUpdates, cancellationToken).ConfigureAwait(true);

            return project.Solution.Workspace.CurrentSolution.GetRequiredProject(project.Id);
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

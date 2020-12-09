// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    [ExportWorkspaceService(typeof(IUnusedReferencesService), ServiceLayer.Default), Shared]
    internal class UnusedReferencesService : IUnusedReferencesService
    {
        private readonly ReferenceType[] _processingOrder = new[] { ReferenceType.Project, ReferenceType.Package, ReferenceType.Assembly };

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnusedReferencesService()
        {

        }

        public async Task<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(
            Project project,
            ImmutableArray<ReferenceInfo> references,
            string targetFrameworkMoniker,
            CancellationToken cancellationToken)
        {
            // Create a lookup of used assembly paths
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var usedAssemblyReferences = compilation.GetUsedAssemblyReferences(cancellationToken).Select(reference => reference.Display);
            var usedAssemblyLookup = usedAssemblyReferences.ToImmutableHashSet();

            var unusedReferences = ImmutableArray.CreateBuilder<ReferenceInfo>();
            var referencesByType = references.GroupBy(reference => reference.ReferenceType).ToImmutableDictionary(group => group.Key);

            foreach (var referenceType in _processingOrder)
            {
                if (!referencesByType.ContainsKey(referenceType))
                {
                    continue;
                }

                usedAssemblyReferences = DetermineUnusedReferences(referencesByType[referenceType], usedAssemblyReferences, unusedReferences);
            }

            return unusedReferences.ToImmutableArray();
        }

        private static IEnumerable<string> DetermineUnusedReferences(IEnumerable<ReferenceInfo> references, IEnumerable<string> usedAssemblyReferences, ImmutableArray<ReferenceInfo>.Builder unusedReferences)
        {
            var usedAssemblyLookup = usedAssemblyReferences.ToImmutableHashSet();

            // Determine which toplevel references have compilation assemblies in set of used assemblies.
            var toplevelUsedReferences = references.Where(reference =>
                reference.CompilationAssemblies.Any(assembly => usedAssemblyLookup.Contains(assembly)));
            var toplevelUsedAssemblyReferences = toplevelUsedReferences.SelectMany(reference => reference.GetAllCompilationAssemblies());

            // Remove all assemblies that are brought into the compilation from the toplevel used references. When
            // determining transtively used references this will reduce false positives.
            var remainingReferences = references.Except(toplevelUsedReferences);
            var remainingUsedAssemblyReferences = usedAssemblyReferences.Except(toplevelUsedAssemblyReferences);
            var remainingUsedAssemblyLookup = remainingUsedAssemblyReferences.ToImmutableHashSet();

            // Determine which transtive references have compilation assemblies in the set of remaining used assemblies.
            foreach (var reference in remainingReferences)
            {
                var allCompilationAssemblies = reference.GetAllCompilationAssemblies();
                if (allCompilationAssemblies.IsEmpty)
                {
                    // Analyzer packages won't contribute any assemblies to the compilation but will
                    // show up as a reference. We will consider them used.
                    continue;
                }

                if (!allCompilationAssemblies.Any(assembly => remainingUsedAssemblyLookup.Contains(assembly)))
                {
                    // References that do not contribute a used compilation assembly are considered unsused.
                    unusedReferences.Add(reference);
                    continue;
                }

                remainingUsedAssemblyReferences = remainingUsedAssemblyReferences.Except(allCompilationAssemblies);
                remainingUsedAssemblyLookup = remainingUsedAssemblyReferences.ToImmutableHashSet();
            }

            return remainingUsedAssemblyReferences;
        }

        public async Task<Project> UpdateReferencesAsync(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            var referenceCleanupService = project.Solution.Workspace.Services.GetRequiredService<IReferenceCleanupService>();

            // Remove unused usings

            foreach (var referenceUpdate in referenceUpdates)
            {
                if (referenceUpdate.Action == UpdateAction.None)
                {
                    referenceUpdate.Action = referenceUpdate.ReferenceInfo.TreatAsUsed
                        ? UpdateAction.TreatAsUnused
                        : UpdateAction.None;
                }
                else if (referenceUpdate.Action == UpdateAction.TreatAsUsed)
                {
                    referenceUpdate.Action = referenceUpdate.ReferenceInfo.TreatAsUsed
                        ? UpdateAction.None
                        : UpdateAction.TreatAsUsed;
                }

                if (referenceUpdate.Action == UpdateAction.None)
                {
                    continue;
                }

                await referenceCleanupService.TryUpdateReferenceAsync(
                    project.FilePath,
                    referenceUpdate,
                    cancellationToken).ConfigureAwait(true);
            }

            return project;
        }
    }
}

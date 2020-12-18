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
        private static readonly ReferenceType[] _processingOrder = new[]
        {
            ReferenceType.Project,
            ReferenceType.Package,
            ReferenceType.Assembly
        };

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnusedReferencesService()
        {

        }

        public async Task<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(
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

            var usedAssemblyLookup = compilation.GetUsedAssemblyReferences(cancellationToken)
                .Select(reference => reference.Display)
                .OfType<string>()
                .ToImmutableHashSet();

            return GetUnusedReferences(usedAssemblyLookup, references);
        }

        internal static ImmutableArray<ReferenceInfo> GetUnusedReferences(
            ImmutableHashSet<string> usedAssemblyLookup,
            ImmutableArray<ReferenceInfo> references)
        {
            var unusedReferences = ImmutableArray.CreateBuilder<ReferenceInfo>();
            var referencesByType = references.GroupBy(reference => reference.ReferenceType)
                .ToImmutableDictionary(group => group.Key);

            // We process the references in order by their type. This means we favor transitive references
            // over direct references where possible.
            foreach (var referenceType in _processingOrder)
            {
                if (!referencesByType.ContainsKey(referenceType))
                {
                    continue;
                }

                usedAssemblyLookup = DetermineUnusedReferences(
                    referencesByType[referenceType].ToImmutableArray(),
                    usedAssemblyLookup,
                    unusedReferences);
            }

            return unusedReferences.ToImmutableArray();
        }

        private static ImmutableHashSet<string> DetermineUnusedReferences(
            ImmutableArray<ReferenceInfo> references,
            ImmutableHashSet<string> usedAssemblyLookup,
            ImmutableArray<ReferenceInfo>.Builder unusedReferences)
        {
            // Determine which toplevel references have compilation assemblies in set of used assemblies.
            var toplevelUsedReferences = references.Where(reference =>
                reference.CompilationAssemblies.Any(usedAssemblyLookup.Contains));
            var toplevelUsedAssemblyReferences = toplevelUsedReferences.SelectMany(reference => reference.GetAllCompilationAssemblies());

            // Remove all assemblies that are brought into the compilation from those toplevel used references. When
            // determining transtively used references this will reduce false positives.
            var remainingReferences = references.Except(toplevelUsedReferences);
            var remainingUsedAssemblyLookup = usedAssemblyLookup.Except(toplevelUsedAssemblyReferences);

            // Determine which transtive references have compilation assemblies in the set of remaining used assemblies.
            foreach (var reference in remainingReferences)
            {
                var allCompilationAssemblies = reference.GetAllCompilationAssemblies();
                if (allCompilationAssemblies.IsEmpty)
                {
                    // We will consider References that do not contribute any assemblies to the compilation,
                    // such as Analyzer packages, as used.
                    continue;
                }

                if (!allCompilationAssemblies.Any(remainingUsedAssemblyLookup.Contains))
                {
                    // None of the assemblies brought into this compilation are in the remaining
                    // used assemblies list, so we will consider the reference unused.
                    unusedReferences.Add(reference);
                    continue;
                }

                remainingUsedAssemblyLookup = remainingUsedAssemblyLookup.Except(allCompilationAssemblies);
            }

            return remainingUsedAssemblyLookup;
        }

        public async Task<Project> UpdateReferencesAsync(
            Project project,
            ImmutableArray<ReferenceUpdate> referenceUpdates,
            CancellationToken cancellationToken)
        {
            var referenceCleanupService = project.Solution.Workspace.Services.GetRequiredService<IReferenceCleanupService>();

            await ApplyReferenceUpdatesAsync(referenceCleanupService, project.FilePath!, referenceUpdates, cancellationToken).ConfigureAwait(false);

            return project.Solution.Workspace.CurrentSolution.GetProject(project.Id)!;
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
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

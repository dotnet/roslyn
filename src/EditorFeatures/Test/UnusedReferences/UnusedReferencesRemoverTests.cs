﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnusedReferences;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.UnusedReferences
{
    public class UnusedReferencesRemoverTests
    {
        private const string UsedAssemblyPath = "/libs/Used.dll";
        private const string UnusedAssemblyPath = "/libs/Unused.dll";

        [Fact, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        public void GetUnusedReferences_UsedReferences_AreNotReturned()
        {
            var usedAssemblies = new[] { UsedAssemblyPath };
            var usedReference = AssemblyReference(UsedAssemblyPath);

            var unusedReferences = GetUnusedReferences(usedAssemblies, usedReference);

            Assert.Empty(unusedReferences);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        public void GetUnusedReferences_UnusedReferences_AreReturned()
        {
            var usedAssemblies = new[] { UsedAssemblyPath };
            var unusedReference = PackageReference(UnusedAssemblyPath);

            var unusedReferences = GetUnusedReferences(usedAssemblies, unusedReference);

            Assert.Contains(unusedReference, unusedReferences);
            Assert.Single(unusedReferences);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        public void GetUnusedReferences_TransitivelyUsedReferences_AreNotReturned()
        {
            var usedAssemblies = new[] { UsedAssemblyPath };
            var transitivelyUsedReference = ProjectReference(UnusedAssemblyPath, PackageReference(UsedAssemblyPath));

            var unusedReferences = GetUnusedReferences(usedAssemblies, transitivelyUsedReference);

            Assert.Empty(unusedReferences);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        public void GetUnusedReferences_WhenUsedAssemblyIsAvilableDirectlyAndTransitively_DirectReferencesAreReturned()
        {
            var usedAssemblies = new[] { UsedAssemblyPath };
            var transitivelyUsedReference = ProjectReference(UnusedAssemblyPath, PackageReference(UsedAssemblyPath));
            var directlyUsedReference = PackageReference(UsedAssemblyPath);

            var unusedReferences = GetUnusedReferences(usedAssemblies, transitivelyUsedReference, directlyUsedReference);

            Assert.Contains(transitivelyUsedReference, unusedReferences);
            Assert.Single(unusedReferences);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        public void GetUnusedReferences_ReferencesThatDoNotContributeToCompilation_AreNotReturned()
        {
            var usedAssemblies = new[] { UsedAssemblyPath };
            var analyzerReference = new ReferenceInfo(
                ReferenceType.Package,
                itemSpecification: "Analyzer",
                treatAsUsed: false,
                compilationAssemblies: ImmutableArray<string>.Empty,
                dependencies: ImmutableArray<ReferenceInfo>.Empty);

            var unusedReferences = GetUnusedReferences(usedAssemblies, analyzerReference);

            Assert.Empty(unusedReferences);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        [InlineData(UpdateAction.None, false)]
        [InlineData(UpdateAction.None, true)]
        [InlineData(UpdateAction.TreatAsUnused, false)]
        [InlineData(UpdateAction.TreatAsUsed, true)]
        internal async Task ApplyReferenceUpdates_NoChangeUpdates_AreNotApplied(UpdateAction action, bool treatAsUsed)
        {
            var noChangeUpdate = new ReferenceUpdate(action, PackageReference(UnusedAssemblyPath, treatAsUsed));

            var appliedUpdates = await ApplyReferenceUpdatesAsync(noChangeUpdate);

            Assert.Empty(appliedUpdates);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        [InlineData(UpdateAction.Remove, false)]
        [InlineData(UpdateAction.Remove, true)]
        [InlineData(UpdateAction.TreatAsUnused, true)]
        [InlineData(UpdateAction.TreatAsUsed, false)]
        internal async Task ApplyReferenceUpdates_ChangeUpdates_AreApplied(UpdateAction action, bool treatAsUsed)
        {
            var changeUpdate = new ReferenceUpdate(action, PackageReference(UnusedAssemblyPath, treatAsUsed));

            var appliedUpdates = await ApplyReferenceUpdatesAsync(changeUpdate);

            Assert.Contains(changeUpdate, appliedUpdates);
            Assert.Single(appliedUpdates);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.UnusedReferences)]
        public async Task ApplyReferenceUpdates_MixOfChangeAndNoChangeUpdates_ChangesAreApplied()
        {
            var noChangeUpdate = new ReferenceUpdate(UpdateAction.None, PackageReference(UsedAssemblyPath));
            var changeUpdate = new ReferenceUpdate(UpdateAction.Remove, PackageReference(UnusedAssemblyPath));

            var appliedUpdates = await ApplyReferenceUpdatesAsync(noChangeUpdate, changeUpdate);

            Assert.Contains(changeUpdate, appliedUpdates);
            Assert.Single(appliedUpdates);
        }

        private static ImmutableArray<ReferenceInfo> GetUnusedReferences(string[] usedCompilationAssemblies, params ReferenceInfo[] references)
            => UnusedReferencesRemover.GetUnusedReferences(new(usedCompilationAssemblies), references.ToImmutableArray());

        private static async Task<ImmutableArray<ReferenceUpdate>> ApplyReferenceUpdatesAsync(params ReferenceUpdate[] referenceUpdates)
        {
            var referenceCleanupService = new TestReferenceCleanupService();

            await UnusedReferencesRemover.ApplyReferenceUpdatesAsync(
                referenceCleanupService,
                string.Empty,
                referenceUpdates.ToImmutableArray(),
                CancellationToken.None).ConfigureAwait(false);

            return referenceCleanupService.AppliedUpdates.ToImmutableArray();
        }

        private static ReferenceInfo ProjectReference(string assemblyPath, params ReferenceInfo[] dependencies)
            => ProjectReference(assemblyPath, treatAsUsed: false, dependencies);
        private static ReferenceInfo ProjectReference(string assemblyPath, bool treatAsUsed, params ReferenceInfo[] dependencies)
            => new(ReferenceType.Project,
                itemSpecification: Path.GetFileName(assemblyPath),
                treatAsUsed,
                compilationAssemblies: ImmutableArray.Create(assemblyPath),
                dependencies.ToImmutableArray());

        private static ReferenceInfo PackageReference(string assemblyPath, params ReferenceInfo[] dependencies)
            => PackageReference(assemblyPath, treatAsUsed: false, dependencies);
        private static ReferenceInfo PackageReference(string assemblyPath, bool treatAsUsed, params ReferenceInfo[] dependencies)
            => new(ReferenceType.Package,
                itemSpecification: Path.GetFileName(assemblyPath),
                treatAsUsed,
                compilationAssemblies: ImmutableArray.Create(assemblyPath),
                dependencies.ToImmutableArray());

        private static ReferenceInfo AssemblyReference(string assemblyPath)
            => AssemblyReference(assemblyPath, treatAsUsed: false);
        private static ReferenceInfo AssemblyReference(string assemblyPath, bool treatAsUsed)
            => new(ReferenceType.Assembly,
                itemSpecification: Path.GetFileName(assemblyPath),
                treatAsUsed,
                compilationAssemblies: ImmutableArray.Create(assemblyPath),
                dependencies: ImmutableArray<ReferenceInfo>.Empty);

        private class TestReferenceCleanupService : IReferenceCleanupService
        {
            private readonly List<ReferenceUpdate> _appliedUpdates = new();
            public IReadOnlyList<ReferenceUpdate> AppliedUpdates => _appliedUpdates;

            public Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public Task<bool> TryUpdateReferenceAsync(string projectPath, ReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
            {
                _appliedUpdates.Add(referenceUpdate);
                return Task.FromResult(true);
            }
        }
    }
}

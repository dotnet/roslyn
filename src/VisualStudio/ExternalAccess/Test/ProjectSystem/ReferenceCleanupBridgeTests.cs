// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Internal;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.UnitTests.ProjectSystem;

[UseExportProvider]
public class ReferenceCleanupBridgeTests
{
    [Fact]
    public async Task ExportedWorkspaceServiceUsesUnifiedContract()
    {
        var composition = TestComposition.Default.AddParts(
            typeof(VisualStudioReferenceCleanupService),
            typeof(ExportedUnifiedCleanupService));
        using var workspace = new AdhocWorkspace(composition.GetHostServices(), ServiceLayer.Host);
        var service = workspace.Services.GetRequiredService<IReferenceCleanupService>();

        var reference = (await service.GetProjectReferencesAsync("unified.csproj", CancellationToken.None)).Single();
        Assert.Equal("UnifiedExport", reference.ItemSpecification);
        Assert.True(await service.TryUpdateReferenceAsync("unified.csproj", CreateUpdate(), CancellationToken.None));
    }

    [Fact]
    public async Task UnifiedServiceTranslatesReferencesAndUpdates()
    {
        var operation = new FakeUnifiedOperation();
        var service = CreateImportingConstructor<VisualStudioReferenceCleanupService>(
            new FakeUnifiedCleanupService("UnifiedOnly", operation, ProjectSystemReferenceType.SDK));

        var reference = (await service.GetProjectReferencesAsync("project.csproj", CancellationToken.None)).Single();
        Assert.Equal("UnifiedOnly", reference.ItemSpecification);
        Assert.False(reference.TreatAsUsed);
        Assert.Equal(4, (int)reference.ReferenceType);
        Assert.True(await service.TryUpdateReferenceAsync("project.csproj", CreateUpdate(), CancellationToken.None));
        Assert.True(operation.Applied);
    }

    [Fact]
    public async Task MissingServiceFailsExplicitly()
    {
        var service = CreateImportingConstructor<VisualStudioReferenceCleanupService>((object?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetProjectReferencesAsync("project.csproj", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TryUpdateReferenceAsync("project.csproj", CreateUpdate(), CancellationToken.None));
    }

    private static ReferenceUpdate CreateUpdate()
        => new(UpdateAction.TreatAsUsed, new ReferenceInfo(ReferenceType.Assembly, "Assembly", true, [], []));

    private static T CreateImportingConstructor<T>(params object?[] arguments)
        => (T)typeof(T).GetConstructors().Single().Invoke(arguments);

    [Export(typeof(IProjectSystemReferenceCleanupService))]
    [Shared, PartNotDiscoverable]
    private sealed class ExportedUnifiedCleanupService : IProjectSystemReferenceCleanupService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExportedUnifiedCleanupService()
        {
        }

        public Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create(new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, "UnifiedExport", treatAsUsed: false)));

        public Task<IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(
            string projectPath,
            ProjectSystemReferenceUpdate referenceUpdate,
            CancellationToken cancellationToken)
            => Task.FromResult<IProjectSystemUpdateReferenceOperation>(new FakeUnifiedOperation());
    }

    private sealed class FakeUnifiedCleanupService(
        string itemSpecification,
        FakeUnifiedOperation operation,
        ProjectSystemReferenceType referenceType = ProjectSystemReferenceType.Package) : IProjectSystemReferenceCleanupService
    {
        public Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create(new ProjectSystemReferenceInfo(referenceType, itemSpecification, treatAsUsed: false)));

        public Task<IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(string projectPath, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            Assert.Equal(ProjectSystemUpdateAction.SetTreatAsUsed, referenceUpdate.Action);
            Assert.Equal("Assembly", referenceUpdate.ReferenceInfo.ItemSpecification);
            Assert.True(referenceUpdate.ReferenceInfo.TreatAsUsed);
            return Task.FromResult<IProjectSystemUpdateReferenceOperation>(operation);
        }
    }

    private sealed class FakeUnifiedOperation : IProjectSystemUpdateReferenceOperation
    {
        public bool Applied { get; private set; }

        public Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            Applied = true;
            return Task.FromResult(true);
        }

        public Task<bool> RevertAsync(CancellationToken cancellationToken)
        {
            Applied = false;
            return Task.FromResult(true);
        }
    }
}

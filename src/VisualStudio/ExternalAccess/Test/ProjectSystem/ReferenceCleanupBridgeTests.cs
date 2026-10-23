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
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;
using Xunit;
using Legacy = Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.UnitTests.ProjectSystem;

[UseExportProvider]
public class ReferenceCleanupBridgeTests
{
    [Fact]
    public async Task ExportedWorkspaceServiceUsesLegacyContractWhenUnifiedContractIsUnavailable()
    {
        var composition = TestComposition.Default.AddParts(
            typeof(VisualStudioReferenceCleanupService),
            typeof(ExportedLegacyCleanupService));
        using var workspace = new AdhocWorkspace(composition.GetHostServices(), ServiceLayer.Host);
        var service = workspace.Services.GetRequiredService<IReferenceCleanupService>();

        var reference = (await service.GetProjectReferencesAsync("legacy.csproj", CancellationToken.None)).Single();
        Assert.Equal("LegacyExport", reference.ItemSpecification);
        Assert.True(await service.TryUpdateReferenceAsync("legacy.csproj", CreateUpdate(), CancellationToken.None));
    }

    [Fact]
    public async Task ExportedWorkspaceServiceUsesUnifiedContractWhenLegacyContractIsUnavailable()
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
    public async Task ExportedWorkspaceServicePrefersUnifiedContractWhenBothContractsAreAvailable()
    {
        var composition = TestComposition.Default.AddParts(
            typeof(VisualStudioReferenceCleanupService),
            typeof(ExportedLegacyCleanupService),
            typeof(ExportedUnifiedCleanupService));
        using var workspace = new AdhocWorkspace(composition.GetHostServices(), ServiceLayer.Host);
        var service = workspace.Services.GetRequiredService<IReferenceCleanupService>();

        var reference = (await service.GetProjectReferencesAsync("both.csproj", CancellationToken.None)).Single();
        Assert.Equal("UnifiedExport", reference.ItemSpecification);
        Assert.True(await service.TryUpdateReferenceAsync("both.csproj", CreateUpdate(), CancellationToken.None));
    }

    [Fact]
    public async Task LegacyServiceIsUsedWhenUnifiedServiceIsUnavailable()
    {
        var operation = new FakeLegacyOperation();
        var legacyService = new FakeLegacyCleanupService("Legacy", operation);
        var service = CreateImportingConstructor<VisualStudioReferenceCleanupService>(legacyService, null);

        var references = await service.GetProjectReferencesAsync("legacy.csproj", CancellationToken.None);
        Assert.Equal("Legacy", references.Single().ItemSpecification);
        Assert.True(references.Single().TreatAsUsed);
        Assert.Equal(4, (int)references.Single().ReferenceType);

        var result = await service.TryUpdateReferenceAsync("legacy.csproj", CreateUpdate(), CancellationToken.None);
        Assert.True(result);
        Assert.True(operation.Applied);
        Assert.Equal("legacy.csproj", legacyService.ProjectPath);
        Assert.Equal(Legacy.ProjectSystemUpdateAction.SetTreatAsUsed, legacyService.Update!.Action);
        Assert.Equal("Assembly", legacyService.Update.ReferenceInfo.ItemSpecification);
        Assert.True(legacyService.Update.ReferenceInfo.TreatAsUsed);
        Assert.Equal(1, legacyService.GetOperationCallCount);
    }

    [Fact]
    public async Task UnifiedServiceTakesPrecedenceWhenBothContractsAreAvailable()
    {
        var legacyService = new FakeLegacyCleanupService("Legacy", new FakeLegacyOperation());
        var unifiedOperation = new FakeUnifiedOperation();
        var unifiedService = new FakeUnifiedCleanupService("Unified", unifiedOperation);
        var service = CreateImportingConstructor<VisualStudioReferenceCleanupService>(legacyService, unifiedService);

        var references = await service.GetProjectReferencesAsync("project.csproj", CancellationToken.None);
        Assert.Equal("Unified", references.Single().ItemSpecification);

        Assert.True(await service.TryUpdateReferenceAsync("project.csproj", CreateUpdate(), CancellationToken.None));
        Assert.True(unifiedOperation.Applied);
        Assert.Equal(0, legacyService.GetOperationCallCount);
        Assert.Equal(0, legacyService.GetProjectReferencesCallCount);
    }

    [Fact]
    public async Task UnifiedServiceWorksWithoutLegacyService()
    {
        var operation = new FakeUnifiedOperation();
        var service = CreateImportingConstructor<VisualStudioReferenceCleanupService>(
            null,
            new FakeUnifiedCleanupService("UnifiedOnly", operation, ProjectSystemReferenceType.SDK));

        var reference = (await service.GetProjectReferencesAsync("project.csproj", CancellationToken.None)).Single();
        Assert.Equal("UnifiedOnly", reference.ItemSpecification);
        Assert.False(reference.TreatAsUsed);
        Assert.Equal(4, (int)reference.ReferenceType);
        Assert.True(await service.TryUpdateReferenceAsync("project.csproj", CreateUpdate(), CancellationToken.None));
        Assert.True(operation.Applied);
    }

    [Fact]
    public async Task MissingServicesFailExplicitly()
    {
        var service = CreateImportingConstructor<VisualStudioReferenceCleanupService>(null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetProjectReferencesAsync("project.csproj", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TryUpdateReferenceAsync("project.csproj", CreateUpdate(), CancellationToken.None));
    }

    private static ReferenceUpdate CreateUpdate()
        => new(UpdateAction.TreatAsUsed, new ReferenceInfo(ReferenceType.Assembly, "Assembly", true, [], []));

    private static T CreateImportingConstructor<T>(params object?[] arguments)
        => (T)typeof(T).GetConstructors().Single().Invoke(arguments);

    [Export(typeof(Legacy.IProjectSystemReferenceCleanupService))]
    [Shared, PartNotDiscoverable]
    private sealed class ExportedLegacyCleanupService : Legacy.IProjectSystemReferenceCleanupService2
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExportedLegacyCleanupService()
        {
        }

        public Task<ImmutableArray<Legacy.ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create(new Legacy.ProjectSystemReferenceInfo(
                (Legacy.ProjectSystemReferenceType)4,
                "LegacyExport",
                treatAsUsed: true)));

        public Task<bool> TryUpdateReferenceAsync(string projectPath, Legacy.ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
            => Task.FromException<bool>(new Xunit.Sdk.XunitException("The obsolete direct update method must not be used."));

        public Task<Legacy.IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(
            string projectPath,
            Legacy.ProjectSystemReferenceUpdate referenceUpdate,
            CancellationToken cancellationToken)
            => Task.FromResult<Legacy.IProjectSystemUpdateReferenceOperation>(new FakeLegacyOperation());
    }

    [Export(typeof(IProjectSystemReferenceCleanupService))]
    [Shared, PartNotDiscoverable]
    private sealed class ExportedUnifiedCleanupService : IProjectSystemReferenceCleanupService2
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExportedUnifiedCleanupService()
        {
        }

        public Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create(new ProjectSystemReferenceInfo(ProjectSystemReferenceType.Package, "UnifiedExport", treatAsUsed: false)));

        public Task<bool> TryUpdateReferenceAsync(string projectPath, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
            => Task.FromException<bool>(new Xunit.Sdk.XunitException("The obsolete direct update method must not be used."));

        public Task<IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(
            string projectPath,
            ProjectSystemReferenceUpdate referenceUpdate,
            CancellationToken cancellationToken)
            => Task.FromResult<IProjectSystemUpdateReferenceOperation>(new FakeUnifiedOperation());
    }

    private sealed class FakeLegacyCleanupService(string itemSpecification, FakeLegacyOperation operation) : Legacy.IProjectSystemReferenceCleanupService2
    {
        public int GetProjectReferencesCallCount { get; private set; }
        public int GetOperationCallCount { get; private set; }
        public string? ProjectPath { get; private set; }
        public Legacy.ProjectSystemReferenceUpdate? Update { get; private set; }

        public Task<ImmutableArray<Legacy.ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
        {
            GetProjectReferencesCallCount++;
            ProjectPath = projectPath;
            return Task.FromResult(ImmutableArray.Create(new Legacy.ProjectSystemReferenceInfo(
                (Legacy.ProjectSystemReferenceType)4,
                itemSpecification,
                treatAsUsed: true)));
        }

        public Task<bool> TryUpdateReferenceAsync(string projectPath, Legacy.ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
            => Task.FromException<bool>(new Xunit.Sdk.XunitException("The obsolete direct update method must not be used."));

        public Task<Legacy.IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(string projectPath, Legacy.ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            GetOperationCallCount++;
            ProjectPath = projectPath;
            Update = referenceUpdate;
            return Task.FromResult<Legacy.IProjectSystemUpdateReferenceOperation>(operation);
        }
    }

    private sealed class FakeUnifiedCleanupService(
        string itemSpecification,
        FakeUnifiedOperation operation,
        ProjectSystemReferenceType referenceType = ProjectSystemReferenceType.Package) : IProjectSystemReferenceCleanupService2
    {
        public Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create(new ProjectSystemReferenceInfo(referenceType, itemSpecification, treatAsUsed: false)));

        public Task<bool> TryUpdateReferenceAsync(string projectPath, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
            => Task.FromException<bool>(new Xunit.Sdk.XunitException("The obsolete direct update method must not be used."));

        public Task<IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(string projectPath, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            Assert.Equal(ProjectSystemUpdateAction.SetTreatAsUsed, referenceUpdate.Action);
            Assert.Equal("Assembly", referenceUpdate.ReferenceInfo.ItemSpecification);
            Assert.True(referenceUpdate.ReferenceInfo.TreatAsUsed);
            return Task.FromResult<IProjectSystemUpdateReferenceOperation>(operation);
        }
    }

    private sealed class FakeLegacyOperation : Legacy.IProjectSystemUpdateReferenceOperation
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

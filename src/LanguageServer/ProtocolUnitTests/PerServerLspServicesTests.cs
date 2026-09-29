// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ExportAttribute = System.Composition.ExportAttribute;
using ImportAttribute = System.Composition.ImportAttribute;
using ImportingConstructorAttribute = System.Composition.ImportingConstructorAttribute;
using SharedAttribute = System.Composition.SharedAttribute;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class PerServerLspServicesTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private const string TestContract = "PerServerLspServicesTests.TestContract";

    protected override TestComposition Composition => base.Composition.AddParts(
        typeof(PerServerDependency),
        typeof(PerServerService),
        typeof(DisposablePerServerService),
        typeof(AsyncAndSyncDisposableService),
        typeof(NeverCreatedAsyncDisposableService),
        typeof(AnyOverridableService),
        typeof(CSharpOverridableService),
        typeof(OverrideConsumer),
        typeof(RoslynMultiContractService),
        typeof(TestContractMultiContractService),
        typeof(AllContractsConsumer),
        typeof(TestContractConsumerOfRoslynOnlyService),
        typeof(TestContractLspServiceProvider),
        typeof(StatelessService),
        typeof(StatelessServiceConsumer));

    [Theory, CombinatorialData]
    public async Task ServersWithSameKindGetSeparateInstances(bool mutatingLspWorkspace)
    {
        await using var serverOne = await CreateTestLspServerAsync("", mutatingLspWorkspace);
        await using var serverTwo = await CreateTestLspServerAsync(serverOne.TestWorkspace, initializationOptions: default, LanguageNames.CSharp);

        var serviceOne = serverOne.GetRequiredLspService<PerServerService>();
        var serviceTwo = serverTwo.GetRequiredLspService<PerServerService>();

        Assert.NotSame(serviceOne, serviceTwo);
        Assert.Same(serviceOne, serverOne.GetRequiredLspService<PerServerService>());
        Assert.NotSame(serviceOne.Dependency, serviceTwo.Dependency);

        // Process-wide MEF parts are shared.
        Assert.Same(serviceOne.GlobalOptions, serviceTwo.GlobalOptions);

        // Converted real services are per-server as well, and share the queue within a server.
        Assert.NotSame(serverOne.GetRequiredLspService<SemanticTokensRefreshQueue>(), serverTwo.GetRequiredLspService<SemanticTokensRefreshQueue>());
        Assert.NotSame(serverOne.GetRequiredLspService<SemanticTokensFullHandler>(), serverTwo.GetRequiredLspService<SemanticTokensFullHandler>());
    }

    [Theory, CombinatorialData]
    public async Task InjectedServicesMatchLspServices(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var service = server.GetRequiredLspService<PerServerService>();

        // Per-server services injected via LspService<T> are the same instances LspServices hands out.
        Assert.Same(server.GetRequiredLspService<PerServerDependency>(), service.Dependency);

        // Base services (manually constructed by RoslynLanguageServer) can be imported too.
        Assert.Same(server.GetRequiredLspService<IClientLanguageServerManager>(), service.ClientLanguageServerManager);
        Assert.Same(server.GetRequiredLspService<ILspServices>(), service.LspServices);

        // Real per-server services can be imported.
        Assert.Same(server.GetRequiredLspService<LspWorkspaceManager>(), service.WorkspaceManager);

        // Global MEF parts are the process-wide instance.
        Assert.Same(server.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>(), service.GlobalOptions);
    }

    [Theory, CombinatorialData]
    public async Task ServerKindOverrideIsResolvedPerServer(bool mutatingLspWorkspace)
    {
        await using var csharpServer = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        await using var alwaysActiveServer = await CreateTestLspServerAsync(csharpServer.TestWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer }, LanguageNames.CSharp);
        await using var alwaysActiveServer2 = await CreateTestLspServerAsync(csharpServer.TestWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer }, LanguageNames.CSharp);

        Assert.IsType<CSharpOverridableService>(csharpServer.GetRequiredLspService<OverridableService>());
        Assert.IsType<AnyOverridableService>(alwaysActiveServer.GetRequiredLspService<OverridableService>());

        // The consumer constructor-imports LspService<OverridableService>; it (and the original export) needed no
        // change when the C# server override was added, and it gets the same per-server instance as LspServices.
        Assert.Same(csharpServer.GetRequiredLspService<OverridableService>(), csharpServer.GetRequiredLspService<OverrideConsumer>().Service);
        Assert.Same(alwaysActiveServer.GetRequiredLspService<OverridableService>(), alwaysActiveServer.GetRequiredLspService<OverrideConsumer>().Service);

        // Two servers of the same kind still get separate instances.
        Assert.NotSame(alwaysActiveServer.GetRequiredLspService<OverridableService>(), alwaysActiveServer2.GetRequiredLspService<OverridableService>());
        Assert.NotSame(alwaysActiveServer.GetRequiredLspService<OverrideConsumer>().Service, alwaysActiveServer2.GetRequiredLspService<OverrideConsumer>().Service);
    }

    [Theory, CombinatorialData]
    public async Task ServersGetServicesForTheirLspContract(bool mutatingLspWorkspace)
    {
        await using var roslynServer = await CreateTestLspServerAsync("", mutatingLspWorkspace);
        var provider = roslynServer.TestWorkspace.ExportProvider.GetExportedValue<TestContractLspServiceProvider>();

        await using var testContractServices = provider.CreateServices(WellKnownLspServerKinds.CSharpVisualBasicLspServer, FrozenDictionary<string, ImmutableArray<BaseService>>.Empty);
        await using var testContractServices2 = provider.CreateServices(WellKnownLspServerKinds.CSharpVisualBasicLspServer, FrozenDictionary<string, ImmutableArray<BaseService>>.Empty);

        // The same (all-contracts) consumer gets the implementation that belongs to its server's contract.
        Assert.IsType<RoslynMultiContractService>(roslynServer.GetRequiredLspService<AllContractsConsumer>().Service);
        Assert.IsType<TestContractMultiContractService>(testContractServices.GetRequiredService<AllContractsConsumer>().Service);
        Assert.Same(testContractServices.GetRequiredService<MultiContractService>(), testContractServices.GetRequiredService<AllContractsConsumer>().Service);

        // Two servers with the same contract and kind get separate instances.
        Assert.NotSame(testContractServices.GetRequiredService<AllContractsConsumer>(), testContractServices2.GetRequiredService<AllContractsConsumer>());
        Assert.NotSame(testContractServices.GetRequiredService<MultiContractService>(), testContractServices2.GetRequiredService<MultiContractService>());

        // Roslyn-only services are not visible to other contracts, even through LspService<T>.
        Assert.Null(testContractServices.GetService<PerServerDependency>());
        Assert.Throws<InvalidOperationException>(() => testContractServices.GetRequiredService<TestContractConsumerOfRoslynOnlyService>().Dependency.Value);
    }

    [Theory, CombinatorialData]
    public async Task PerServerServicesAreCleanedUpWithTheirServer(bool mutatingLspWorkspace)
    {
        await using var serverOne = await CreateTestLspServerAsync("", mutatingLspWorkspace);
        await using var serverTwo = await CreateTestLspServerAsync(serverOne.TestWorkspace, initializationOptions: default, LanguageNames.CSharp);

        var neverCreatedCount = NeverCreatedAsyncDisposableService.CreatedCount;
        var injectedDisposable = serverOne.GetRequiredLspService<PerServerService>().Disposable;
        var asyncAndSyncDisposable = serverOne.GetRequiredLspService<AsyncAndSyncDisposableService>();
        var otherServerDisposable = serverTwo.GetRequiredLspService<DisposablePerServerService>();
        var otherServerAsyncAndSyncDisposable = serverTwo.GetRequiredLspService<AsyncAndSyncDisposableService>();

        await serverOne.ShutdownTestServerAsync();
        await serverOne.ExitTestServerAsync();

        // IDisposable is handled by the MEF sharing boundary.
        Assert.True(injectedDisposable.IsDisposed);

        // IAsyncDisposableLspService.DisposeAsync is called exactly once, before the (synchronous) MEF disposal.
        AssertEx.Equal(["disposeAsync", "dispose"], asyncAndSyncDisposable.Events);

        // Services that were never created aren't created just to be cleaned up.
        Assert.Equal(neverCreatedCount, NeverCreatedAsyncDisposableService.CreatedCount);

        // Other servers are unaffected.
        Assert.False(otherServerDisposable.IsDisposed);
        Assert.Empty(otherServerAsyncAndSyncDisposable.Events);
    }

    [Theory, CombinatorialData]
    public async Task StatelessServicesAreSharedAcrossServers(bool mutatingLspWorkspace)
    {
        await using var serverOne = await CreateTestLspServerAsync("", mutatingLspWorkspace);
        await using var serverTwo = await CreateTestLspServerAsync(serverOne.TestWorkspace, initializationOptions: default, LanguageNames.CSharp);

        var stateless = serverOne.GetRequiredLspService<StatelessService>();
        Assert.Same(stateless, serverTwo.GetRequiredLspService<StatelessService>());

        // Per-server consumers can still import it through LspService<T>.
        Assert.Same(stateless, serverTwo.GetRequiredLspService<StatelessServiceConsumer>().Service);

        await serverOne.ShutdownTestServerAsync();
        await serverOne.ExitTestServerAsync();

        // It is owned by the MEF container, not by any server.
        Assert.False(stateless.IsDisposed);
    }

    [Theory, CombinatorialData]
    public async Task ConvertedHandlersAreRegistered(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);

        var handlers = ((LspServices)server.GetRequiredLspService<ILspServices>()).GetMethodHandlers();
        Assert.Contains(handlers, h => h.HandlerTypeRef.TypeName == typeof(SemanticTokensFullHandler).FullName);
        Assert.Contains(handlers, h => h.HandlerTypeRef.TypeName == typeof(SemanticTokensRangeHandler).FullName);
    }

    /// <summary>
    /// Guards against mistakes that MEF does not report as composition errors.
    /// </summary>
    [Fact]
    public void PerServerCompositionIsWellFormed()
    {
        var configuration = Composition.GetCompositionConfiguration();
        Assert.Contains(configuration.Parts, p => p.Definition.Type == typeof(SemanticTokensRefreshQueue));
        Assert.Empty(GetCompositionViolations(Composition));
    }

    [Fact]
    public void CompositionGuardDetectsMistakes()
    {
        Type[] mistakes =
        [
            typeof(AccidentallyNonSharedLspService),
            typeof(GlobalPartImportingLspService),
            typeof(ImplContractImporter),
            typeof(AsyncDisposablePerServerService),
            typeof(StatelessAsyncDisposableService),
            typeof(StatelessServiceImportingLspService),
        ];

        AssertEx.SetEqual(mistakes, GetCompositionViolations(Composition.AddParts(mistakes)));
    }

    private static Type[] GetCompositionViolations(TestComposition composition)
    {
        var configuration = composition.GetCompositionConfiguration();
        return [.. configuration.Parts.Where(IsViolation).Select(p => p.Definition.Type).Distinct()];

        static bool IsViolation(ComposedPart part)
        {
            var definition = part.Definition;
            var type = definition.Type;
            var isPerServer = definition.SharingBoundary == LspServiceComposition.SharingBoundary;
            // vs-mef reports the empty string as the sharing boundary of a globally shared part.
            var isSharedAcrossServers = definition.IsShared && string.IsNullOrEmpty(definition.SharingBoundary);
            var exportsLspService = definition.ExportDefinitions.Any(e => e.Value.ContractName == LspServiceComposition.ContractName);
            var requiresPerServer = part.RequiredSharingBoundaries.Contains(LspServiceComposition.SharingBoundary);

            // An LSP service must be shared, either per-server or across all servers; otherwise every import would
            // create a new instance.
            if (exportsLspService && !isPerServer && !isSharedAcrossServers)
                return true;

            // A part that (transitively) imports per-server parts must itself be per-server; otherwise it fails at
            // runtime when requested.
            if (requiresPerServer && !isPerServer)
                return true;

            // Stateless services are owned by the MEF container, not a server.
            if (isSharedAcrossServers && typeof(IAsyncDisposableLspService).IsAssignableFrom(type))
                return true;

            // The sharing boundary is disposed synchronously, so IAsyncDisposable would be ignored (or blocked on).
            if (isPerServer && typeof(IAsyncDisposable).IsAssignableFrom(type))
                return true;

            // Importing the implementation contract directly would bypass LSP contract filtering and overrides.
            if (type != typeof(LspServerScope) && definition.Imports.Any(i => i.ImportDefinition.ContractName == LspServiceComposition.ContractName))
                return true;

            return false;
        }
    }

    [ExportCSharpVisualBasicLspService(typeof(PerServerDependency)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class PerServerDependency() : ILspService;

    [ExportCSharpVisualBasicLspService(typeof(PerServerService)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class PerServerService(
        IGlobalOptionService globalOptions,
        LspService<PerServerDependency> dependency,
        LspService<DisposablePerServerService> disposable,
        LspService<IClientLanguageServerManager> clientLanguageServerManager,
        LspService<LspServices> lspServices,
        LspService<LspWorkspaceManager> workspaceManager) : ILspService
    {
        public IGlobalOptionService GlobalOptions => globalOptions;
        public PerServerDependency Dependency { get; } = dependency.Value;
        public DisposablePerServerService Disposable { get; } = disposable.Value;
        public IClientLanguageServerManager ClientLanguageServerManager => clientLanguageServerManager.Value;
        public LspServices LspServices => lspServices.Value;
        public LspWorkspaceManager WorkspaceManager => workspaceManager.Value;
    }

    [ExportCSharpVisualBasicLspService(typeof(DisposablePerServerService)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DisposablePerServerService() : ILspService, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [ExportCSharpVisualBasicLspService(typeof(AsyncAndSyncDisposableService)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AsyncAndSyncDisposableService() : IAsyncDisposableLspService, IDisposable
    {
        public List<string> Events { get; } = [];

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Events.Add("disposeAsync");
        }

        public void Dispose() => Events.Add("dispose");
    }

    [ExportCSharpVisualBasicLspService(typeof(NeverCreatedAsyncDisposableService)), Shared(LspServiceComposition.SharingBoundary)]
    internal sealed class NeverCreatedAsyncDisposableService : IAsyncDisposableLspService
    {
        private static int s_createdCount;

        public static int CreatedCount => s_createdCount;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NeverCreatedAsyncDisposableService()
            => Interlocked.Increment(ref s_createdCount);

        public ValueTask DisposeAsync() => default;
    }

    internal abstract class OverridableService : ILspService;

    [ExportCSharpVisualBasicLspService(typeof(OverridableService)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AnyOverridableService() : OverridableService;

    [ExportCSharpVisualBasicLspService(typeof(OverridableService), WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpOverridableService() : OverridableService;

    [ExportCSharpVisualBasicLspService(typeof(OverrideConsumer)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class OverrideConsumer(LspService<OverridableService> service) : ILspService
    {
        public OverridableService Service { get; } = service.Value;
    }

    internal abstract class MultiContractService : ILspService;

    [ExportCSharpVisualBasicLspService(typeof(MultiContractService)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class RoslynMultiContractService() : MultiContractService;

    [ExportLspService(typeof(MultiContractService), TestContract), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestContractMultiContractService() : MultiContractService;

    [ExportLspService(typeof(AllContractsConsumer), ProtocolConstants.AllLspContracts), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AllContractsConsumer(LspService<MultiContractService> service) : ILspService
    {
        public MultiContractService Service => service.Value;
    }

    [ExportLspService(typeof(TestContractConsumerOfRoslynOnlyService), TestContract), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestContractConsumerOfRoslynOnlyService(LspService<PerServerDependency> dependency) : ILspService
    {
        public LspService<PerServerDependency> Dependency => dependency;
    }

    [Export(typeof(TestContractLspServiceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestContractLspServiceProvider(
        [SharingBoundary(LspServiceComposition.SharingBoundary)] ExportFactory<LspServerScope> scopeFactory)
        : AbstractLspServiceProvider(scopeFactory, TestContract);

    [ExportCSharpVisualBasicLspService(typeof(StatelessService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class StatelessService(IGlobalOptionService globalOptions) : ILspService, IDisposable
    {
        public IGlobalOptionService GlobalOptions => globalOptions;
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [ExportCSharpVisualBasicLspService(typeof(StatelessServiceConsumer)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class StatelessServiceConsumer(LspService<StatelessService> service) : ILspService
    {
        public StatelessService Service => service.Value;
    }

    /// <summary>Mistake: exported as an LSP service but not [Shared], so every import would create a new instance.</summary>
#pragma warning disable RS0023 // Intentionally non-shared to verify the guard detects it.
    [ExportCSharpVisualBasicLspService(typeof(AccidentallyNonSharedLspService))]
#pragma warning restore RS0023
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AccidentallyNonSharedLspService() : ILspService;

    /// <summary>Mistake: a process-wide part importing a per-server service.</summary>
    [Export(typeof(GlobalPartImportingLspService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class GlobalPartImportingLspService(LspService<PerServerDependency> dependency)
    {
        public LspService<PerServerDependency> Dependency => dependency;
    }

    /// <summary>Mistake: importing the implementation contract bypasses LSP contract filtering and overrides.</summary>
    [ExportCSharpVisualBasicLspService(typeof(ImplContractImporter)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class ImplContractImporter([Import(LspServiceComposition.ContractName)] PerServerDependency dependency) : ILspService
    {
        public PerServerDependency Dependency => dependency;
    }

    /// <summary>Mistake: the per-server sharing boundary is disposed synchronously.</summary>
    [ExportCSharpVisualBasicLspService(typeof(AsyncDisposablePerServerService)), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AsyncDisposablePerServerService() : ILspService, IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    /// <summary>Mistake: a stateless service is owned by the MEF container, so no server would dispose it.</summary>
    [ExportCSharpVisualBasicLspService(typeof(StatelessAsyncDisposableService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class StatelessAsyncDisposableService() : IAsyncDisposableLspService
    {
        public ValueTask DisposeAsync() => default;
    }

    /// <summary>Mistake: a stateless service can't import per-server services.</summary>
    [ExportCSharpVisualBasicLspService(typeof(StatelessServiceImportingLspService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class StatelessServiceImportingLspService(LspService<PerServerDependency> dependency) : ILspService
    {
        public LspService<PerServerDependency> Dependency => dependency;
    }
}

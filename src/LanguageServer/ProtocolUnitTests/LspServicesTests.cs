// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class LspServicesTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task ReturnsSpecificLspService(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(CSharpPerServerLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is CSharpLspService);

        var perServerLspService = server.GetRequiredLspService<PerServerTestLspService>();
        Assert.IsType<CSharpPerServerLspService>(perServerLspService);
    }

    [Theory, CombinatorialData]
    public async Task SpecificLspServiceOverridesAny(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(AnyLspService), typeof(CSharpPerServerLspService), typeof(AnyPerServerLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is CSharpLspService);

        var perServerLspService = server.GetRequiredLspService<PerServerTestLspService>();
        Assert.IsType<CSharpPerServerLspService>(perServerLspService);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsAnyLspService(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(AnyLspService), typeof(AnyPerServerLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is AnyLspService);

        var perServerLspService = server.GetRequiredLspService<PerServerTestLspService>();
        Assert.IsType<AnyPerServerLspService>(perServerLspService);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsSingleLspServiceImplementingInterface(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(InterfaceLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<ITestLspServiceInterface>();
        Assert.IsType<InterfaceLspService>(lspService);
    }

    [Theory, CombinatorialData]
    public async Task ExactInterfaceLspServiceOverridesImplementingService(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(DirectInterfaceLspService), typeof(InterfaceLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<ITestLspServiceInterface>();
        Assert.IsType<DirectInterfaceLspService>(lspService);
    }

    [Theory, CombinatorialData]
    public async Task MultipleLspServicesImplementingInterfaceThrow(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(InterfaceLspService), typeof(SecondInterfaceLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        Assert.Throws<InvalidOperationException>(() => server.GetRequiredLspService<ITestLspServiceInterface>());
    }

    [Theory, CombinatorialData]
    public async Task DuplicateSpecificServicesThrow(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(CSharpPerServerLspService), typeof(DuplicateCSharpLspService), typeof(DuplicateCSharpPerServerLspService));
        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition));
    }

    [Theory, CombinatorialData]
    public async Task DuplicateAnyServicesThrow(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(AnyLspService), typeof(AnyPerServerLspService), typeof(DuplicateAnyLspService), typeof(DuplicateAnyPerServerLspService));
        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition));
    }

    [Theory, CombinatorialData]
    public async Task ReturnsLspServiceForMatchingServer(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(AlwaysActiveCSharpLspService));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is CSharpLspService);

        await using var server2 = await CreateTestLspServerAsync(server.TestWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer }, LanguageNames.CSharp);

        var lspService2 = server2.GetRequiredLspService<TestLspService>();
        Assert.True(lspService2 is AlwaysActiveCSharpLspService);
    }

    internal class TestLspService : ILspService { }

    internal abstract class PerServerTestLspService : ILspService { }

    internal interface ITestLspServiceInterface : ILspService { }

    [ExportLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpLspService() : TestLspService { }

    [ExportLspService(typeof(PerServerTestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpPerServerLspService() : PerServerTestLspService { }

    [ExportLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AnyLspService() : TestLspService { }

    [ExportLspService(typeof(ITestLspServiceInterface), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DirectInterfaceLspService() : ITestLspServiceInterface { }

    [ExportLspService(typeof(InterfaceLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class InterfaceLspService() : ITestLspServiceInterface { }

    [ExportLspService(typeof(SecondInterfaceLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class SecondInterfaceLspService() : ITestLspServiceInterface { }

    [ExportLspService(typeof(PerServerTestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AnyPerServerLspService() : PerServerTestLspService { }

    [ExportLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateCSharpLspService() : TestLspService { }

    [ExportLspService(typeof(PerServerTestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateCSharpPerServerLspService() : PerServerTestLspService { }

    [ExportLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateAnyLspService() : TestLspService { }

    [ExportLspService(typeof(PerServerTestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared(LspServiceComposition.SharingBoundary)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateAnyPerServerLspService() : PerServerTestLspService { }

    [ExportLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.AlwaysActiveVSLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AlwaysActiveCSharpLspService() : TestLspService { }
}

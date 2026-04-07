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
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(CSharpLspServiceFactory));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is CSharpLspService);

        var lspServiceFromFactory = server.GetRequiredLspService<TestLspServiceFromFactory>();
        Assert.Equal(typeof(CSharpLspServiceFactory).Name, lspServiceFromFactory.FactoryName);
    }

    [Theory, CombinatorialData]
    public async Task SpecificLspServiceOverridesAny(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(AnyLspService), typeof(CSharpLspServiceFactory), typeof(AnyLspServiceFactory));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is CSharpLspService);

        var lspServiceFromFactory = server.GetRequiredLspService<TestLspServiceFromFactory>();
        Assert.Equal(typeof(CSharpLspServiceFactory).Name, lspServiceFromFactory.FactoryName);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsAnyLspService(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(AnyLspService), typeof(AnyLspServiceFactory));
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);

        var lspService = server.GetRequiredLspService<TestLspService>();
        Assert.True(lspService is AnyLspService);

        var lspServiceFromFactory = server.GetRequiredLspService<TestLspServiceFromFactory>();
        Assert.Equal(typeof(AnyLspServiceFactory).Name, lspServiceFromFactory.FactoryName);
    }

    [Theory, CombinatorialData]
    public async Task DuplicateSpecificServicesThrow(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(CSharpLspService), typeof(CSharpLspServiceFactory), typeof(DuplicateCSharpLspService), typeof(DuplicateCSharpLspServiceFactory));
        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateTestLspServerAsync("", mutatingLspWorkspace, initializationOptions: new() { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition));
    }

    [Theory, CombinatorialData]
    public async Task DuplicateAnyServicesThrow(bool mutatingLspWorkspace)
    {
        var composition = base.Composition.AddParts(typeof(AnyLspService), typeof(AnyLspServiceFactory), typeof(DuplicateAnyLspService), typeof(DuplicateAnyLspServiceFactory));
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

    internal sealed record class TestLspServiceFromFactory(string FactoryName) : ILspService { }

    internal class TestLspServiceFactory : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind) => new TestLspServiceFromFactory(this.GetType().Name);
    }

    [ExportStatelessLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpLspService() : TestLspService { }

    [ExportLspServiceFactory(typeof(TestLspServiceFromFactory), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class CSharpLspServiceFactory() : TestLspServiceFactory { }

    [ExportStatelessLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AnyLspService() : TestLspService { }

    [ExportLspServiceFactory(typeof(TestLspServiceFromFactory), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AnyLspServiceFactory() : TestLspServiceFactory { }

    [ExportStatelessLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateCSharpLspService() : TestLspService { }

    [ExportLspServiceFactory(typeof(TestLspServiceFromFactory), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateCSharpLspServiceFactory() : CSharpLspServiceFactory { }

    [ExportStatelessLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateAnyLspService() : TestLspService { }

    [ExportLspServiceFactory(typeof(TestLspServiceFromFactory), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DuplicateAnyLspServiceFactory() : CSharpLspServiceFactory { }

    [ExportStatelessLspService(typeof(TestLspService), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.AlwaysActiveVSLspServer), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class AlwaysActiveCSharpLspService() : TestLspService { }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Threading;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

public sealed partial class ServiceHubServicesTests
{
    [Fact]
    public async Task TestExtensionMessageHandlerService_MultipleRegistrationThrows()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_UnregisterNonRegisteredServiceThrows()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.UnregisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_ExecuteWorkspaceMessageForUnregisteredServiceThrows()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.HandleExtensionWorkspaceMessageAsync(
             workspace.CurrentSolution, "MessageName", "JsonMessage", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_ExecuteDocumentMessageForUnregisteredServiceThrows()
    {
        using var workspace = CreateWorkspace();

        workspace.SetCurrentSolution(solution =>
        {
            return AddProject(solution, LanguageNames.CSharp, ["// empty"]);
        }, WorkspaceChangeKind.SolutionChanged);

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
             workspace.CurrentSolution.Projects.Single().Documents.Single(), "MessageName", "JsonMessage", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForUnregisteredServiceThrows1()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForUnregisteredServiceThrows2()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);
        await extensionMessageHandlerService.UnregisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), errorMessage);
    }

    private static async Task<TestExtensionAssemblyLoaderProvider> GetRemoteAssemblyLoaderProvider(TestWorkspace localWorkspace)
    {
        var client = await InProcRemoteHostClient.GetTestClientAsync(localWorkspace);
        var remoteWorkspace = client.TestData.WorkspaceManager.GetWorkspace();
        var assemblyLoaderProvider = (TestExtensionAssemblyLoaderProvider)remoteWorkspace.Services.GetRequiredService<IExtensionAssemblyLoaderProvider>();
        return assemblyLoaderProvider;
    }

    private static async Task<TestExtensionMessageHandlerFactory> GetRemoteAssemblyHandlerFactory(TestWorkspace localWorkspace)
    {
        var client = await InProcRemoteHostClient.GetTestClientAsync(localWorkspace);
        var remoteWorkspace = client.TestData.WorkspaceManager.GetWorkspace();
        var handlerFactory = (TestExtensionMessageHandlerFactory)remoteWorkspace.Services.GetRequiredService<IExtensionMessageHandlerFactory>();
        return handlerFactory;
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_NoLoaderOrException()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts: [typeof(TestExtensionAssemblyLoaderProvider)]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);

        // Return null for the loader, and no exception.  This is effectively the .Net framework case, and it should
        // report no errors and an empty list of handlers.
        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => default;

        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        var result = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Empty(result.WorkspaceMessageHandlers);
        Assert.Empty(result.DocumentMessageHandlers);
        Assert.Null(result.ExtensionException);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_NoLoader_ExtensionException()
    {
        const string ExpectedExceptionMessage = "IO Error";

        using var localWorkspace = CreateWorkspace(additionalRemoteParts: [typeof(TestExtensionAssemblyLoaderProvider)]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);

        // Return null for the loader, and an exception representing IO issues when trying to do the loading. this
        // should return the exception as an extension exception along with an empty list of handlers.
        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => (null, new Exception(ExpectedExceptionMessage));

        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        var result = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Empty(result.WorkspaceMessageHandlers);
        Assert.Empty(result.DocumentMessageHandlers);
        Assert.NotNull(result.ExtensionException);
        Assert.Equal(ExpectedExceptionMessage, result.ExtensionException.Message);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_CancellationExceptionWhenLoading()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts: [typeof(TestExtensionAssemblyLoaderProvider)]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);

        // Throw a cancellation exception here.  That's legal as per the signature of CreateNewShadowCopyLoader.  It
        // should gracefully cancel, and not cause us to tear down anything.
        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => throw new OperationCanceledException();

        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None));
        Assert.Null(errorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_TestWorkspaceAndDocumentNames()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);
        var handlerFactory = await GetRemoteAssemblyHandlerFactory(localWorkspace);

        handlerFactory.CreateDocumentMessageHandlersCallback =
            (_, _, _) => [new TestHandler<Document>("DocumentMessageName")];
        handlerFactory.CreateWorkspaceMessageHandlersCallback =
            (_, _, _) => [new TestHandler<Solution>("WorkspaceMessageName")];

        // Make a basic loader that just returns null for the assembly.
        var assemblyLoader = new Mock<IExtensionAssemblyLoader>(MockBehavior.Strict);
        assemblyLoader.Setup(loader => loader.LoadFromPath("TempPath")).Returns((Assembly)null);
        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => (assemblyLoader.Object, extensionException: null);

        string errorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => errorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(errorMessage);

        var result = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Null(result.ExtensionException);
        AssertEx.SequenceEqual(["DocumentMessageName"], result.DocumentMessageHandlers);
        AssertEx.SequenceEqual(["WorkspaceMessageName"], result.WorkspaceMessageHandlers);
    }

    [PartNotDiscoverable]
    [ExportWorkspaceService(typeof(IExtensionAssemblyLoaderProvider), ServiceLayer.Test), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class TestExtensionAssemblyLoaderProvider() : IExtensionAssemblyLoaderProvider
    {
        public Func<string, CancellationToken, (IExtensionAssemblyLoader assemblyLoader, Exception extensionException)> CreateNewShadowCopyLoaderCallback { get; set; }

        public (IExtensionAssemblyLoader assemblyLoader, Exception extensionException) CreateNewShadowCopyLoader(string assemblyFolderPath, CancellationToken cancellationToken)
            => CreateNewShadowCopyLoaderCallback.Invoke(assemblyFolderPath, cancellationToken);
    }

    [PartNotDiscoverable]
    [ExportWorkspaceService(typeof(IExtensionMessageHandlerFactory), ServiceLayer.Test), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class TestExtensionMessageHandlerFactory() : IExtensionMessageHandlerFactory
    {
        public Func<Assembly, string, CancellationToken, ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>> CreateWorkspaceMessageHandlersCallback { get; set; }
        public Func<Assembly, string, CancellationToken, ImmutableArray<IExtensionMessageHandlerWrapper<Document>>> CreateDocumentMessageHandlersCallback { get; set; }

        public ImmutableArray<IExtensionMessageHandlerWrapper<Solution>> CreateWorkspaceMessageHandlers(Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken)
            => CreateWorkspaceMessageHandlersCallback.Invoke(assembly, extensionIdentifier, cancellationToken);

        public ImmutableArray<IExtensionMessageHandlerWrapper<Document>> CreateDocumentMessageHandlers(Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken)
            => CreateDocumentMessageHandlersCallback.Invoke(assembly, extensionIdentifier, cancellationToken);
    }

    private sealed class TestHandler<TArgument>(
        string name,
        Func<int, TArgument, CancellationToken, int> executeCallback = null) : IExtensionMessageHandlerWrapper<TArgument>
    {
        public Task<object> ExecuteAsync(object message, TArgument argument, CancellationToken cancellationToken)
            => Task.FromResult((object)executeCallback((int)message, argument, cancellationToken));

        public Type MessageType => typeof(int);

        public Type ResponseType => typeof(int);

        public string Name => name;

        public string ExtensionIdentifier => throw new NotImplementedException();
    }
}

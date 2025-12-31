// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Threading;
using Moq;
using Roslyn.Test.Utilities;
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
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_UnregisterNonRegisteredServiceThrows()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.UnregisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_ExecuteWorkspaceMessageForUnregisteredServiceThrows()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.HandleExtensionWorkspaceMessageAsync(
             workspace.CurrentSolution, "MessageName", "JsonMessage", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
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
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
             workspace.CurrentSolution.Projects.Single().Documents.Single(), "MessageName", "JsonMessage", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForUnregisteredServiceThrows1()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForUnregisteredServiceThrows2()
    {
        using var workspace = CreateWorkspace();

        var extensionMessageHandlerService = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)workspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        await extensionMessageHandlerService.UnregisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
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

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        var result = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
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

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        var result = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
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

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None));
        Assert.Null(fatalRpcErrorMessage);
    }

    private static async Task<ExtensionMessageNames> RegisterTestHandlers(
        TestWorkspace localWorkspace,
        Func<Assembly, string, CancellationToken, ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>> createWorkspaceMessageHandlersCallback,
        Func<Assembly, string, CancellationToken, ImmutableArray<IExtensionMessageHandlerWrapper<Document>>> createDocumentMessageHandlersCallback)
    {
        localWorkspace.SetCurrentSolution(solution =>
        {
            return AddProject(solution, LanguageNames.CSharp, ["// empty"]);
        }, WorkspaceChangeKind.SolutionChanged);

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);
        var handlerFactory = await GetRemoteAssemblyHandlerFactory(localWorkspace);

        handlerFactory.CreateDocumentMessageHandlersCallback = createDocumentMessageHandlersCallback;
        handlerFactory.CreateWorkspaceMessageHandlersCallback = createWorkspaceMessageHandlersCallback;

        // Make a basic loader that just returns null for the assembly.  We're not actually going to try to load
        // anything.  We just need to make sure we can get through the mock loading process.
        var assemblyLoader = new Mock<IExtensionAssemblyLoader>(MockBehavior.Strict);
        assemblyLoader.Setup(loader => loader.LoadFromPath("TempPath")).Returns((Assembly?)null!);
        assemblyLoader.Setup(loader => loader.Unload());

        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => (assemblyLoader.Object, extensionException: null);

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();
        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        var result = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        return result;
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_TestWorkspaceAndDocumentNames()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var result = await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [new TestHandler<Solution>("WorkspaceMessageName")],
            (_, _, _) => [new TestHandler<Document>("DocumentMessageName")]);

        Assert.Null(result.ExtensionException);
        AssertEx.SequenceEqual(["DocumentMessageName"], result.DocumentMessageHandlers);
        AssertEx.SequenceEqual(["WorkspaceMessageName"], result.WorkspaceMessageHandlers);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_ThrowsCancellationToken()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await RegisterTestHandlers(
                localWorkspace,
                (_, _, _) => [new TestHandler<Solution>("WorkspaceMessageName")],
                // Cancellation exception should be reported normally through the entire stack.
                (_, _, _) => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_GetExtensionMessageNamesForRegisteredService_HandlerFactoryThrows()
    {
        const string ExpectedExceptionMessage = "Error Creating Handler";

        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        // Normal extension exception should be caught and reported as data.
        var result = await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => throw new Exception(ExpectedExceptionMessage));

        Assert.NotNull(result.ExtensionException);
        Assert.Equal(ExpectedExceptionMessage, result.ExtensionException.Message);
        Assert.Empty(result.DocumentMessageHandlers);
        Assert.Empty(result.WorkspaceMessageHandlers);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_UnregisteredName()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>("HandlerName")]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // Invoking a handler name that doesn't exist is a bug.
        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "NonRegisteredHandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.NotNull(fatalRpcErrorMessage);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_IncorrectArgumentType()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>("HandlerName")]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // The test handlers only take/receive ints, so passing in a json array should fail.  This is a bug with the
        // handler though, not roslyn/gladstone.  So we should get a normal extension exception.
        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "[]", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.NotNull(result.ExtensionException);
        Assert.IsType<JsonException>(result.ExtensionException);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_IncorrectReturnType()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var handlerWasCalled = false;
        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>(
                "HandlerName",
                (_, _, _) =>
                {
                    handlerWasCalled = true;
                    // Return an invalid value, given that test handlers state they return ints.
                    return "StringNotInt";
                })]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // The test handlers only take/receive ints, so passing in a json array should fail.  This is a bug with the
        // handler though, not roslyn/gladstone.  So we should get a normal extension exception.
        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.True(handlerWasCalled);

        Assert.NotNull(result.ExtensionException);
        Assert.IsType<ArgumentException>(result.ExtensionException);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_HandlerThrowsException()
    {
        const string ExtensionExceptionMessage = "3rd Party Message";

        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var handlerWasCalled = false;
        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>(
                "HandlerName",
                (_, _, _) =>
                {
                    handlerWasCalled = true;
                    throw new Exception(ExtensionExceptionMessage);
                })]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // An unexpected exception thrown by the handler should be reported as an extension exception, and should not be
        // a fatal rpc error.
        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.True(handlerWasCalled);

        Assert.NotNull(result.ExtensionException);
        Assert.Contains(ExtensionExceptionMessage, result.ExtensionException.Message);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_HandlerThrowsException_CanCallAfterwards()
    {
        const string ExtensionExceptionMessage = "3rd Party Message";

        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var handlerWasCalled1 = false;
        var handlerWasCalled2 = false;
        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>(
                "HandlerName",
                (_, _, _) =>
                {
                    if (!handlerWasCalled1)
                    {
                        handlerWasCalled1 = true;
                        throw new Exception(ExtensionExceptionMessage);
                    }
                    else
                    {
                        handlerWasCalled2 = true;
                        return 1;
                    }
                })]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // An unexpected exception thrown by the handler should be reported as an extension exception, and should not be
        // a fatal rpc error.
        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.True(handlerWasCalled1);
        Assert.False(handlerWasCalled2);

        Assert.NotNull(result.ExtensionException);
        Assert.Contains(ExtensionExceptionMessage, result.ExtensionException.Message);

        // Second call should be fine.  Failing the first call doesn't disable the extension.
        result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.True(handlerWasCalled2);
        Assert.Equal("1", result.Response);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_HandlerThrowsCancellationException()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var handlerWasCalled = false;
        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>(
                "HandlerName",
                (_, _, _) =>
                {
                    handlerWasCalled = true;
                    throw new OperationCanceledException();
                })]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // An cancellation exception thrown by the handler should be reported as a normal cancellation exception, and
        // should not be a fatal rpc error.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None));
        Assert.Null(fatalRpcErrorMessage);
        Assert.True(handlerWasCalled);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_ValidateHandlerResponse()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var handlerWasCalled = false;
        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>(
                "HandlerName",
                (_, _, _) =>
                {
                    handlerWasCalled = true;
                    return 1;
                })]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.True(handlerWasCalled);
        Assert.Equal("1", result.Response);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_HandleExtensionMessage_CalledAfterUnregister()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        var handlerWasCalled = false;
        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [],
            (_, _, _) => [new TestHandler<Document>(
                "HandlerName",
                (_, _, _) =>
                {
                    handlerWasCalled = true;
                    return 1;
                })]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.UnregisterExtensionAsync("TempPath", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.False(handlerWasCalled);

        var result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.False(handlerWasCalled);
        Assert.True(result.ExtensionWasUnloaded);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_RegisteringMultipleHandlersWithSameName()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        localWorkspace.SetCurrentSolution(solution =>
        {
            return AddProject(solution, LanguageNames.CSharp, ["// empty"]);
        }, WorkspaceChangeKind.SolutionChanged);

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);
        var handlerFactory = await GetRemoteAssemblyHandlerFactory(localWorkspace);

        handlerFactory.CreateDocumentMessageHandlersCallback = (_, _, _) => [new TestHandler<Document>("HandlerName")];
        handlerFactory.CreateWorkspaceMessageHandlersCallback = (_, _, _) => [];

        // Make a basic loader that just returns null for the assembly.
        var assemblyLoader = new Mock<IExtensionAssemblyLoader>(MockBehavior.Loose);
        assemblyLoader.Setup(loader => loader.LoadFromPath(It.IsAny<string>())).Returns((Assembly?)null!);
        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => (assemblyLoader.Object, extensionException: null);

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        // Multiple registration won't fail.

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();
        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath1", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        await extensionMessageHandlerService.RegisterExtensionAsync("TempPath2", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        var messageNames1 = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath1", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        var messageNames2 = await extensionMessageHandlerService.GetExtensionMessageNamesAsync("TempPath2", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);

        Assert.Contains("HandlerName", messageNames1.DocumentMessageHandlers);
        Assert.Contains("HandlerName", messageNames2.DocumentMessageHandlers);

        // We'll only fail if we try to invoke the handler.
        await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.NotNull(fatalRpcErrorMessage);
        Assert.Contains(nameof(InvalidOperationException), fatalRpcErrorMessage);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_RegisteringMultipleHandlersWithSameName_WorkspaceVersusDocument()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        await RegisterTestHandlers(
            localWorkspace,
            (_, _, _) => [new TestHandler<Solution>("HandlerName", (_, _, _) => 1)],
            (_, _, _) => [new TestHandler<Document>("HandlerName", (_, _, _) => 2)]);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        var result = await extensionMessageHandlerService.HandleExtensionWorkspaceMessageAsync(
            localWorkspace.CurrentSolution,
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.Equal("1", result.Response);

        result = await extensionMessageHandlerService.HandleExtensionDocumentMessageAsync(
            localWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
            "HandlerName", jsonMessage: "0", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Assert.Equal("2", result.Response);
    }

    [Fact]
    public async Task TestExtensionMessageHandlerService_OnlyUnloadWhenAllPathsAreUnregistered()
    {
        using var localWorkspace = CreateWorkspace(additionalRemoteParts:
            [typeof(TestExtensionAssemblyLoaderProvider), typeof(TestExtensionMessageHandlerFactory)]);

        localWorkspace.SetCurrentSolution(solution =>
        {
            return AddProject(solution, LanguageNames.CSharp, ["// empty"]);
        }, WorkspaceChangeKind.SolutionChanged);

        var assemblyLoaderProvider = await GetRemoteAssemblyLoaderProvider(localWorkspace);
        var handlerFactory = await GetRemoteAssemblyHandlerFactory(localWorkspace);

        handlerFactory.CreateDocumentMessageHandlersCallback = (_, _, _) => [];
        handlerFactory.CreateWorkspaceMessageHandlersCallback = (_, _, _) => [];

        var unloadCalled = false;
        var assemblyLoader = new TestExtensionAssemblyLoader(
            loadFromPath: _ => null!,
            unload: () => unloadCalled = true);

        assemblyLoaderProvider.CreateNewShadowCopyLoaderCallback = (_, _) => (assemblyLoader, extensionException: null);

        var extensionMessageHandlerService = localWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();

        // Don't trap the error here.  We want to validate that this crosses the ServiceHub boundary and is reported as
        // a real roslyn/gladstone error.
        string? fatalRpcErrorMessage = null;
        var errorReportingService = (TestErrorReportingService)localWorkspace.Services.GetRequiredService<IErrorReportingService>();
        errorReportingService.OnError = message => fatalRpcErrorMessage = message;

        await extensionMessageHandlerService.RegisterExtensionAsync(@"TempPath\a.dll", CancellationToken.None);
        await extensionMessageHandlerService.GetExtensionMessageNamesAsync(@"TempPath\a.dll", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Contract.ThrowIfTrue(unloadCalled);

        await extensionMessageHandlerService.RegisterExtensionAsync(@"TempPath\b.dll", CancellationToken.None);
        await extensionMessageHandlerService.GetExtensionMessageNamesAsync(@"TempPath\b.dll", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Contract.ThrowIfTrue(unloadCalled);

        await extensionMessageHandlerService.UnregisterExtensionAsync(@"TempPath\a.dll", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Contract.ThrowIfTrue(unloadCalled);

        // Unregistering the second dll should unload the ALC.
        await extensionMessageHandlerService.UnregisterExtensionAsync(@"TempPath\b.dll", CancellationToken.None);
        Assert.Null(fatalRpcErrorMessage);
        Contract.ThrowIfFalse(unloadCalled);
    }

    private sealed class TestExtensionAssemblyLoader(
        Func<string, Assembly>? loadFromPath = null,
        Action? unload = null) : IExtensionAssemblyLoader
    {
        public Assembly LoadFromPath(string assemblyFilePath) => loadFromPath!(assemblyFilePath);

        public void Unload() => unload!();
    }

    [PartNotDiscoverable]
    [ExportWorkspaceService(typeof(IExtensionAssemblyLoaderProvider), ServiceLayer.Test), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class TestExtensionAssemblyLoaderProvider() : IExtensionAssemblyLoaderProvider
    {
        public Func<string, CancellationToken, (IExtensionAssemblyLoader? assemblyLoader, Exception? extensionException)>? CreateNewShadowCopyLoaderCallback { get; set; }

        public (IExtensionAssemblyLoader? assemblyLoader, Exception? extensionException) CreateNewShadowCopyLoader(string assemblyFolderPath, CancellationToken cancellationToken)
            => CreateNewShadowCopyLoaderCallback!.Invoke(assemblyFolderPath, cancellationToken);
    }

    [PartNotDiscoverable]
    [ExportWorkspaceService(typeof(IExtensionMessageHandlerFactory), ServiceLayer.Test), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class TestExtensionMessageHandlerFactory() : IExtensionMessageHandlerFactory
    {
        public Func<Assembly, string, CancellationToken, ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>? CreateWorkspaceMessageHandlersCallback { get; set; }
        public Func<Assembly, string, CancellationToken, ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>? CreateDocumentMessageHandlersCallback { get; set; }

        public ImmutableArray<IExtensionMessageHandlerWrapper<Solution>> CreateWorkspaceMessageHandlers(Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken)
            => CreateWorkspaceMessageHandlersCallback!.Invoke(assembly, extensionIdentifier, cancellationToken);

        public ImmutableArray<IExtensionMessageHandlerWrapper<Document>> CreateDocumentMessageHandlers(Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken)
            => CreateDocumentMessageHandlersCallback!.Invoke(assembly, extensionIdentifier, cancellationToken);
    }

    private sealed class TestHandler<TArgument>(
        string name,
        Func<object?, TArgument, CancellationToken, object?>? executeCallback = null) : IExtensionMessageHandlerWrapper<TArgument>
    {
        public async Task<object?> ExecuteAsync(object? message, TArgument argument, CancellationToken cancellationToken)
            => executeCallback!(message, argument, cancellationToken);

        public Type MessageType => typeof(int);

        public Type ResponseType => typeof(int);

        public string Name => name;

        public string ExtensionIdentifier => throw new NotImplementedException();
    }
}

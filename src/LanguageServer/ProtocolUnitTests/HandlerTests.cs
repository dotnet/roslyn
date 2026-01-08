// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class HandlerTests : AbstractLanguageServerProtocolTests
{
    public HandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition.AddParts(
        typeof(TestDocumentHandler),
        typeof(TestNonMutatingDocumentHandler),
        typeof(TestRequestHandlerWithNoParams),
        typeof(TestNotificationHandlerFactory),
        typeof(TestNotificationWithoutParamsHandlerFactory),
        typeof(TestLanguageSpecificHandler),
        typeof(TestLanguageSpecificHandlerWithDifferentParams),
        typeof(TestConfigurableDocumentHandler));

    [Theory, CombinatorialData]
    public async Task CanExecuteRequestHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });
        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestDocumentHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteRequestHandlerWithNoParams(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var response = await server.ExecuteRequest0Async<string>(TestRequestHandlerWithNoParams.MethodName, CancellationToken.None);
        Assert.Equal(typeof(TestRequestHandlerWithNoParams).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteNotificationHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        await server.ExecuteNotificationAsync(TestNotificationHandler.MethodName, request);
        var response = await server.GetRequiredLspService<TestNotificationHandler>().ResultSource.Task;
        Assert.Equal(typeof(TestNotificationHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteNotificationHandlerWithNoParams(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        await server.ExecuteNotification0Async(TestNotificationWithoutParamsHandler.MethodName);
        var response = await server.GetRequiredLspService<TestNotificationWithoutParamsHandler>().ResultSource.Task;
        Assert.Equal(typeof(TestNotificationWithoutParamsHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteLanguageSpecificHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.fs")
        });
        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestLanguageSpecificHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteLanguageSpecificHandlerWithDifferentRequestTypes(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeTwo(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.vb")
        });
        var response = await server.ExecuteRequestAsync<TestRequestTypeTwo, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestLanguageSpecificHandlerWithDifferentParams).Name, response);
    }

    [Theory, CombinatorialData]
    public Task ThrowsOnInvalidLanguageSpecificHandler(bool mutatingLspWorkspace)
        => Assert.ThrowsAsync<InvalidOperationException>(async () => await CreateTestLspServerAsync("", mutatingLspWorkspace,
            composition: Composition.AddParts(typeof(TestDuplicateLanguageSpecificHandler))));

    [Theory, CombinatorialData]
    public async Task ThrowsIfDeserializationFails(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeThree("value");
        await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(async () => await server.ExecuteRequestAsync<TestRequestTypeThree, string>(TestNonMutatingDocumentHandler.MethodName, request, CancellationToken.None));
        Assert.False(server.GetServerAccessor().HasShutdownStarted());
    }

    [Theory, CombinatorialData]
    public async Task ShutsdownIfDeserializationFailsOnMutatingRequest(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeThree("value");
        await Assert.ThrowsAnyAsync<Exception>(async () => await server.ExecuteRequestAsync<TestRequestTypeThree, string>(TestDocumentHandler.MethodName, request, CancellationToken.None));
        await server.AssertServerShuttingDownAsync();
    }

    [Theory, CombinatorialData]
    public async Task NonMutatingHandlerExceptionNFWIsReported(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new InvalidOperationException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: false, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.True(didReport);
    }

    [Theory, CombinatorialData]
    public async Task NonMutatingHandlerExceptionNFWIsNotReportedForLocalRpcException(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new StreamJsonRpc.LocalRpcException(nameof(HandlerTests)) { ErrorCode = LspErrorCodes.ContentModified });
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: false, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.False(didReport);
    }

    [Theory, CombinatorialData]
    public async Task MutatingHandlerExceptionNFWIsReported(bool mutatingLspWorkspace)
    {
        var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new InvalidOperationException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: true, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        await server.AssertServerShuttingDownAsync();

        Assert.True(didReport);
    }

    [Theory, CombinatorialData]
    public async Task NonMutatingHandlerCancellationExceptionNFWIsNotReported(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new OperationCanceledException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: false, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.False(didReport);
    }

    [Theory, CombinatorialData]
    public async Task MutatingHandlerCancellationExceptionNFWIsNotReported(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new OperationCanceledException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: true, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.False(didReport);
    }

    [Theory, CombinatorialData]
    public async Task TestMutatingHandlerCrashesIfUnableToDetermineLanguage(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Run a mutating request against a file which we have no saved languageId for
        // and where the language cannot be determined from the URI.
        // This should crash the server.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"untitled:untitledFile");
        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = looseFileUri
        });

        await Assert.ThrowsAnyAsync<Exception>(async () => await testLspServer.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None)).ConfigureAwait(false);
        await testLspServer.AssertServerShuttingDownAsync();
    }

    internal sealed record TestRequestTypeOne([property: JsonPropertyName("textDocument"), JsonRequired] TextDocumentIdentifier TextDocumentIdentifier);

    internal sealed record TestRequestTypeTwo([property: JsonPropertyName("textDocument"), JsonRequired] TextDocumentIdentifier TextDocumentIdentifier);

    internal sealed record TestRequestTypeThree([property: JsonPropertyName("someValue")] string SomeValue);

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestDocumentHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestDocumentHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public const string MethodName = nameof(TestDocumentHandler);

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestNonMutatingDocumentHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestNonMutatingDocumentHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public const string MethodName = nameof(TestNonMutatingDocumentHandler);

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestRequestHandlerWithNoParams)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestRequestHandlerWithNoParams() : ILspServiceRequestHandler<string>
    {
        public const string MethodName = nameof(TestRequestHandlerWithNoParams);

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task<string> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    internal sealed class TestNotificationHandler() : ILspServiceNotificationHandler<TestRequestTypeOne>
    {
        public const string MethodName = nameof(TestNotificationHandler);
        public readonly TaskCompletionSource<string> ResultSource = new();

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task HandleNotificationAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            ResultSource.SetResult(this.GetType().Name);
        }
    }

    /// <summary>
    /// Exported via a factory as we need a new instance for each server (the task completion result should be unique per server).
    /// </summary>
    [ExportCSharpVisualBasicLspServiceFactory(typeof(TestNotificationHandler)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestNotificationHandlerFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new TestNotificationHandler();
        }
    }

    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    internal sealed class TestNotificationWithoutParamsHandler() : ILspServiceNotificationHandler
    {
        public const string MethodName = nameof(TestNotificationWithoutParamsHandler);
        public readonly TaskCompletionSource<string> ResultSource = new();

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task HandleNotificationAsync(RequestContext context, CancellationToken cancellationToken)
        {
            ResultSource.SetResult(this.GetType().Name);
        }
    }

    /// <summary>
    /// Exported via a factory as we need a new instance for each server (the task completion result should be unique per server).
    /// </summary>
    [ExportCSharpVisualBasicLspServiceFactory(typeof(TestNotificationWithoutParamsHandler)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestNotificationWithoutParamsHandlerFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new TestNotificationWithoutParamsHandler();
        }
    }

    /// <summary>
    /// Defines a language specific handler with the same method as <see cref="TestDocumentHandler"/>
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(TestLanguageSpecificHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(TestDocumentHandler.MethodName, LanguageNames.FSharp)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestLanguageSpecificHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    /// <summary>
    /// Defines a language specific handler with the same method as <see cref="TestDocumentHandler"/>
    /// but using different request and response types.
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(TestLanguageSpecificHandlerWithDifferentParams)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(TestDocumentHandler.MethodName, LanguageNames.VisualBasic)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestLanguageSpecificHandlerWithDifferentParams() : ILspServiceDocumentRequestHandler<TestRequestTypeTwo, string>
    {
        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeTwo request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeTwo request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    /// <summary>
    /// Defines a language specific handler with the same method and language as <see cref="TestLanguageSpecificHandler"/>
    /// but with different params (an error)
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(TestDuplicateLanguageSpecificHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(TestDocumentHandler.MethodName, LanguageNames.FSharp)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestDuplicateLanguageSpecificHandler() : ILspServiceRequestHandler<string>
    {
        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task<string> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }
}

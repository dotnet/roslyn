// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Client;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.UnitTests;

public class RazorCohostTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => base.Composition
        .AddAssemblies(typeof(RazorCohostLanguageClient).Assembly)
        .AddParts(
            typeof(RazorHandler),
            typeof(RazorCohostCapabilitiesProvider),
            typeof(RazorCohostLanguageClientActivationService),
            typeof(NoOpLspLoggerFactory));

    [WpfFact]
    public async Task TestExternalAccessRazorHandlerInvoked()
    {
        var workspaceXml = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="RazorProj">
                    <AdditionalDocument FilePath="C:\Test.razor"></AdditionalDocument>
                </Project>
            </Workspace>
            """;
        var testWorkspace = CreateWorkspace(workspaceXml);
        var server = await InitializeLanguageServerAsync(testWorkspace);

        var document = testWorkspace.CurrentSolution.Projects.Single().AdditionalDocuments.Single();
        var request = new TextDocumentPositionParams
        {
            TextDocument = new VSTextDocumentIdentifier
            {
                Uri = document.GetURI(),
                ProjectContext = new VSProjectContext
                {
                    Id = document.Project.Id.Id.ToString()
                }
            }
        };

        var response = await server.GetTestAccessor().ExecuteRequestAsync<TextDocumentPositionParams, TestRequest>(RazorHandler.MethodName, request, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(document.GetURI(), response.DocumentUri);
        Assert.Equal(document.Project.Id.Id, response.ProjectId);
    }

    [WpfFact]
    public async Task TestProjectContextHandler()
    {
        var workspaceXml = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="RazorProj">
                    <AdditionalDocument FilePath="C:\Test.razor"></AdditionalDocument>
                </Project>
            </Workspace>
            """;
        var testWorkspace = CreateWorkspace(workspaceXml);
        var server = await InitializeLanguageServerAsync(testWorkspace);

        var document = testWorkspace.CurrentSolution.Projects.Single().AdditionalDocuments.Single();
        var request = new VSGetProjectContextsParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = document.GetURI(),
            }
        };

        var response = await server.GetTestAccessor().ExecuteRequestAsync<VSGetProjectContextsParams, VSProjectContextList?>(VSMethods.GetProjectContextsName, request, CancellationToken.None);

        Assert.NotNull(response);
        var projectContext = Assert.Single(response?.ProjectContexts);
        Assert.Equal(ProtocolConversions.ProjectIdToProjectContextId(document.Project.Id), projectContext.Id);
    }

    [WpfFact]
    public async Task TestDocumentSync()
    {
        var workspaceXml = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="RazorProj">
                    <AdditionalDocument FilePath="C:\Test.razor"></AdditionalDocument>
                </Project>
            </Workspace>
            """;
        var testWorkspace = CreateWorkspace(workspaceXml);
        var server = await InitializeLanguageServerAsync(testWorkspace);

        var document = testWorkspace.CurrentSolution.Projects.Single().AdditionalDocuments.Single();
        var didOpenRequest = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = document.GetURI(),
                Text = "Original text"
            }
        };

        await server.GetTestAccessor().ExecuteRequestAsync<DidOpenTextDocumentParams, NoValue?>(Methods.TextDocumentDidOpenName, didOpenRequest, CancellationToken.None);

        var workspaceManager = server.GetLspServices().GetRequiredService<LspWorkspaceManager>();
        Assert.True(workspaceManager.GetTrackedLspText().TryGetValue(document.GetURI(), out var trackedText));
        Assert.Equal("Original text", trackedText.Text.ToString());

        var didChangeRequest = new DidChangeTextDocumentParams
        {
            TextDocument = new VersionedTextDocumentIdentifier
            {
                Uri = document.GetURI()
            },
            ContentChanges =
            [
                new TextDocumentContentChangeEvent
                {
                    Range = new Roslyn.LanguageServer.Protocol.Range
                    {
                        Start = new Position(0, 0),
                        End = new Position(0, 0)
                    },
                    Text = "Not The "
                }
            ]
        };

        await server.GetTestAccessor().ExecuteRequestAsync<DidChangeTextDocumentParams, object>(Methods.TextDocumentDidChangeName, didChangeRequest, CancellationToken.None);

        Assert.True(workspaceManager.GetTrackedLspText().TryGetValue(document.GetURI(), out trackedText));
        Assert.Equal("Not The Original text", trackedText.Text.ToString());
    }

    private EditorTestWorkspace CreateWorkspace(string workspaceXml)
    {
        var testWorkspace = CreateWorkspace(options: null, mutatingLspWorkspace: false, workspaceKind: null);
        testWorkspace.InitializeDocuments(XElement.Parse(workspaceXml), openDocuments: false);
        return testWorkspace;
    }

    private static async Task<AbstractLanguageServer<RequestContext>> InitializeLanguageServerAsync(EditorTestWorkspace testWorkspace)
    {
        var languageClient = testWorkspace.ExportProvider.GetExportedValues<ILanguageClient>().OfType<RazorCohostLanguageClient>().Single();
        await languageClient.ActivateAsync(CancellationToken.None);

        var server = languageClient.GetTestAccessor().LanguageServer;
        Assert.NotNull(server);

        var serverAccessor = server!.GetTestAccessor();

        await serverAccessor.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = new() }, CancellationToken.None);

        return server;
    }

    internal class TestRequest(Uri documentUri, Guid projectId)
    {
        public Uri DocumentUri => documentUri;
        public Guid ProjectId => projectId;
    }

    [PartNotDiscoverable]
    [RazorMethod(MethodName)]
    [ExportRazorStatelessLspService(typeof(RazorHandler)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    internal class RazorHandler() : AbstractRazorCohostDocumentRequestHandler<TextDocumentPositionParams, TestRequest>
    {
        internal const string MethodName = "testMethod";

        protected override bool MutatesSolutionState => false;

        protected override bool RequiresLSPSolution => true;

        protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(TextDocumentPositionParams request)
        {
            return new RazorTextDocumentIdentifier(request.TextDocument.Uri, (request.TextDocument as VSTextDocumentIdentifier)?.ProjectContext?.Id);
        }

        protected override Task<TestRequest> HandleRequestAsync(TextDocumentPositionParams request, RazorCohostRequestContext context, CancellationToken cancellationToken)
        {
            Assert.NotNull(context.Solution);
            AssertEx.NotNull(context.TextDocument);
            return Task.FromResult(new TestRequest(context.TextDocument.GetURI(), context.TextDocument.Project.Id.Id));
        }
    }

    [PartNotDiscoverable]
    [Export(typeof(ILspServiceLoggerFactory)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    private class NoOpLspLoggerFactory() : ILspServiceLoggerFactory
    {
        public Task<AbstractLspLogger> CreateLoggerAsync(string serverTypeName, JsonRpc jsonRpc, CancellationToken cancellationToken)
            => Task.FromResult((AbstractLspLogger)NoOpLspLogger.Instance);
    }

    [PartNotDiscoverable]
    [Export(typeof(IRazorCohostCapabilitiesProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    private class RazorCohostCapabilitiesProvider() : IRazorCohostCapabilitiesProvider
    {
        public string GetCapabilities(string clientCapabilities)
        {
            return "{ }";
        }
    }

    [PartNotDiscoverable]
    [Export(typeof(IRazorCohostLanguageClientActivationService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    private class RazorCohostLanguageClientActivationService() : IRazorCohostLanguageClientActivationService
    {
        public bool ShouldActivateCohostServer() => true;
    }
}

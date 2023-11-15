// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
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
        var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""RazorProj"">
        <AdditionalDocument FilePath=""C:\Test.razor""></AdditionalDocument>
    </Project>
</Workspace>";

        var testWorkspace = CreateWorkspace(options: null, mutatingLspWorkspace: false, workspaceKind: null);
        testWorkspace.InitializeDocuments(XElement.Parse(workspaceXml), openDocuments: false);

        var languageClient = testWorkspace.ExportProvider.GetExportedValues<ILanguageClient>().OfType<RazorCohostLanguageClient>().Single();
        await languageClient.ActivateAsync(CancellationToken.None);

        var serverAccessor = languageClient.GetTestAccessor().LanguageServer!.GetTestAccessor();

        await serverAccessor.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = new() });

        var document = testWorkspace.CurrentSolution.Projects.Single().AdditionalDocuments.Single();
        var request = new TestRequest(document.GetURI(), document.Project.Id.Id);

        var response = await serverAccessor.ExecuteRequestAsync<TestRequest, TestRequest>(RazorHandler.MethodName, request);

        Assert.NotNull(response);
        Assert.Equal(request.DocumentUri, response.DocumentUri);
        Assert.Equal(request.ProjectId, response.ProjectId);
    }

    internal class TestRequest(Uri documentUri, Guid projectId)
    {
        public Uri DocumentUri => documentUri;
        public Guid ProjectId => projectId;
    }

    [LanguageServerEndpoint(MethodName)]
    [ExportRazorStatelessLspService(typeof(RazorHandler)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    internal class RazorHandler() : AbstractRazorDocumentRequestHandler<TestRequest, TestRequest>
    {
        internal const string MethodName = "testMethod";

        protected override bool MutatesSolutionState => false;

        protected override bool RequiresLSPSolution => true;

        protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(TestRequest request)
        {
            return new RazorTextDocumentIdentifier(request.DocumentUri, request.ProjectId.ToString());
        }

        protected override Task<TestRequest> HandleRequestAsync(TestRequest request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            Assert.NotNull(context.Solution);
            AssertEx.NotNull(context.TextDocument);
            return Task.FromResult(new TestRequest(context.TextDocument.GetURI(), context.TextDocument.Project.Id.Id));
        }
    }

    [Export(typeof(ILspServiceLoggerFactory)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    private class NoOpLspLoggerFactory() : ILspServiceLoggerFactory
    {
        public Task<ILspServiceLogger> CreateLoggerAsync(string serverTypeName, JsonRpc jsonRpc, CancellationToken cancellationToken)
            => Task.FromResult(NoOpLspLogger.Instance);
    }

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

    [Export(typeof(IRazorCohostLanguageClientActivationService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    private class RazorCohostLanguageClientActivationService() : IRazorCohostLanguageClientActivationService
    {
        public bool ShouldActivateCohostServer() => true;
    }
}

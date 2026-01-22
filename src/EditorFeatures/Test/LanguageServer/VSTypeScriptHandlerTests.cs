// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.LanguageServer;

public sealed class VSTypeScriptHandlerTests : AbstractLanguageServerProtocolTests
{
    public VSTypeScriptHandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => EditorTestCompositions.LanguageServerProtocolEditorFeatures
        .AddParts(typeof(TypeScriptHandlerFactory))
        .AddParts(typeof(TestWorkspaceRegistrationService));

    [Fact]
    public async Task TestExternalAccessTypeScriptHandlerInvoked()
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="TypeScript" CommonReferences="true" AssemblyName="TypeScriptProj">
                    <Document FilePath="C:\T.ts"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTsTestLspServerAsync(workspaceXml);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var request = new TSRequest(document.GetURI(), ProtocolConversions.ProjectIdToProjectContextId(document.Project.Id));

        var response = await testLspServer.ExecuteRequestAsync<TSRequest, int>(TypeScriptHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(TypeScriptHandler.Response, response);
    }

    [Fact]
    public async Task TestRoslynTypeScriptHandlerInvoked()
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="TypeScript" CommonReferences="true" AssemblyName="TypeScriptProj">
                    <Document FilePath="C:\T.ts"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTsTestLspServerAsync(workspaceXml, new InitializationOptions());

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var documentPullRequest = new VSInternalDocumentDiagnosticsParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI(), document.Project.Id)
        };

        var response = await testLspServer.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(VSInternalMethods.DocumentPullDiagnosticName, documentPullRequest, CancellationToken.None);
        AssertEx.Empty(response);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1901118")]
    public async Task TestGetSimplifierOptionsOnTypeScriptDocument()
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="TypeScript" CommonReferences="true" AssemblyName="TypeScriptProj">
                    <Document FilePath="C:\T.ts"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTsTestLspServerAsync(workspaceXml);
        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var simplifierOptions = testLspServer.TestWorkspace.GlobalOptions.GetSimplifierOptions(document.Project.Services);
        Assert.Same(SimplifierOptions.CommonDefaults, simplifierOptions);
    }

    private async Task<VSTypeScriptTestLspServer> CreateTsTestLspServerAsync(string workspaceXml, InitializationOptions? options = null)
    {
        var testWorkspace = await CreateWorkspaceAsync(options, mutatingLspWorkspace: false, workspaceKind: null);
        testWorkspace.InitializeDocuments(XElement.Parse(workspaceXml), openDocuments: false);

        return await VSTypeScriptTestLspServer.CreateAsync(testWorkspace, new InitializationOptions(), TestOutputLspLogger);
    }

    private sealed class VSTypeScriptTestLspServer : AbstractTestLspServer<LspTestWorkspace, TestHostDocument, TestHostProject, TestHostSolution>
    {
        public VSTypeScriptTestLspServer(LspTestWorkspace testWorkspace, Dictionary<string, IList<Roslyn.LanguageServer.Protocol.Location>> locations, InitializationOptions options, AbstractLspLogger logger) : base(testWorkspace, locations, options, logger)
        {
        }

        protected override RoslynLanguageServer CreateLanguageServer(Stream inputStream, Stream outputStream, WellKnownLspServerKinds serverKind, AbstractLspLogger logger)
        {
            var capabilitiesProvider = TestWorkspace.ExportProvider.GetExportedValue<DefaultCapabilitiesProvider>();
            var servicesProvider = TestWorkspace.ExportProvider.GetExportedValue<VSTypeScriptLspServiceProvider>();

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter))
            {
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            var languageServer = new RoslynLanguageServer(
                servicesProvider, jsonRpc, messageFormatter.JsonSerializerOptions,
                capabilitiesProvider,
                logger,
                TestWorkspace.Services.HostServices,
                [InternalLanguageNames.TypeScript],
                WellKnownLspServerKinds.RoslynTypeScriptLspServer);

            jsonRpc.StartListening();
            return languageServer;
        }

        public static async Task<VSTypeScriptTestLspServer> CreateAsync(LspTestWorkspace testWorkspace, InitializationOptions options, AbstractLspLogger logger)
        {
            var locations = await GetAnnotatedLocationsAsync(testWorkspace, testWorkspace.CurrentSolution);
            var server = new VSTypeScriptTestLspServer(testWorkspace, locations, options, logger);
            await server.InitializeAsync();
            return server;
        }
    }

    internal sealed record TSRequest([property: JsonConverter(typeof(DocumentUriConverter))] DocumentUri Document, string Project);

    [ExportTypeScriptLspServiceFactory(typeof(TypeScriptHandler)), PartNotDiscoverable, Shared]
    internal sealed class TypeScriptHandlerFactory : AbstractVSTypeScriptRequestHandlerFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptHandlerFactory()
        {
        }

        protected override IVSTypeScriptRequestHandler CreateRequestHandler()
        {
            return new TypeScriptHandler();
        }
    }

    [VSTypeScriptMethod(MethodName)]
    internal sealed class TypeScriptHandler : AbstractVSTypeScriptRequestHandler<TSRequest, int>
    {
        internal static int Response = 1;

        internal const string MethodName = "testMethod";

        protected override bool MutatesSolutionState => false;

        protected override bool RequiresLSPSolution => true;

        protected override TypeScriptTextDocumentIdentifier? GetTypeSciptTextDocumentIdentifier(TSRequest request)
        {
            return new TypeScriptTextDocumentIdentifier(request.Document.GetRequiredParsedUri(), request.Project);
        }

        protected override async Task<int> HandleRequestAsync(TSRequest request, TypeScriptRequestContext context, CancellationToken cancellationToken)
        {
            Assert.NotNull(context.Solution);
            AssertEx.NotNull(context.Document);
            Assert.Equal(context.Document.GetURI(), request.Document);
            return Response;
        }
    }
}

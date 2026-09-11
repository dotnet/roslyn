// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
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

    protected override TestComposition Composition => EditorTestCompositions.LanguageServerProtocolEditorFeatures.AddParts(typeof(VSTypeScriptTestLoggerFactory));

    [Fact]
    public async Task TestRoslynTypeScriptDiagnosticHandlersInvoked()
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
        var legacyDocumentPullRequest = new VSInternalDocumentDiagnosticsParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI(), document.Project.Id)
        };

        var legacyDocumentResponse = await testLspServer.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(VSInternalMethods.DocumentPullDiagnosticName, legacyDocumentPullRequest, CancellationToken.None);
        AssertEx.Empty(legacyDocumentResponse);

        var publicDocumentPullRequest = new DocumentDiagnosticParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI(), document.Project.Id)
        };

        var publicDocumentResponse = await testLspServer.ExecuteRequestAsync<DocumentDiagnosticParams, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>(Methods.TextDocumentDiagnosticName, publicDocumentPullRequest, CancellationToken.None);
        AssertEx.Empty(publicDocumentResponse.First.Items);

        var publicWorkspacePullRequest = new WorkspaceDiagnosticParams
        {
            Identifier = PullDiagnosticCategories.WorkspaceDocumentsAndProject,
            PreviousResultId = []
        };

        var publicWorkspaceResponse = await testLspServer.ExecuteRequestAsync<WorkspaceDiagnosticParams, WorkspaceDiagnosticReport?>(Methods.WorkspaceDiagnosticName, publicWorkspacePullRequest, CancellationToken.None);
        Assert.NotNull(publicWorkspaceResponse);
        AssertEx.Empty(publicWorkspaceResponse.Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestRoslynTypeScriptDiagnosticCapabilities(bool dynamicRegistration)
    {
        var clientCallbackTarget = new ClientCallbackTarget();
        var initializationOptions = new InitializationOptions
        {
            CallInitialized = true,
            ClientCapabilities = new ClientCapabilities
            {
                TextDocument = new TextDocumentClientCapabilities
                {
                    Diagnostic = new DiagnosticSetting
                    {
                        DynamicRegistration = dynamicRegistration,
                    }
                }
            },
            ClientTarget = clientCallbackTarget,
        };

        await using var testLspServer = await CreateTsTestLspServerAsync("<Workspace></Workspace>", initializationOptions);

        var serverCapabilities = Assert.IsType<VSInternalServerCapabilities>(testLspServer.GetServerCapabilities());
        Assert.True(serverCapabilities.SupportsDiagnosticRequests);
        var legacyDiagnosticOptions = Assert.IsType<VSInternalDiagnosticOptions>(serverCapabilities.DiagnosticProvider);
        Assert.True(legacyDiagnosticOptions.SupportsMultipleContextsDiagnostics);
        AssertEx.Equal(
            [
                PullDiagnosticCategories.Task,
                PullDiagnosticCategories.WorkspaceDocumentsAndProject,
                PullDiagnosticCategories.DocumentAnalyzerSyntax,
                PullDiagnosticCategories.DocumentAnalyzerSemantic,
            ],
            Assert.IsType<VSInternalDiagnosticKind[]>(legacyDiagnosticOptions.DiagnosticKinds).Select(kind => kind.Value));

        var publicRegistrations = clientCallbackTarget.Registrations
            .Where(registration => registration.Method == Methods.TextDocumentDiagnosticName)
            .Select(registration => JsonSerializer.Deserialize<DiagnosticRegistrationOptions>((JsonElement)registration.RegisterOptions!, ProtocolConversions.LspJsonSerializerOptions)!)
            .ToArray();

        if (dynamicRegistration)
        {
            Assert.Null(serverCapabilities.DiagnosticOptions);
            Assert.NotEmpty(publicRegistrations);
            Assert.Contains(publicRegistrations, options => options.WorkspaceDiagnostics);
        }
        else
        {
            Assert.Empty(publicRegistrations);
            var diagnosticOptions = Assert.IsType<DiagnosticOptions>(serverCapabilities.DiagnosticOptions?.Value);
            Assert.True(diagnosticOptions.InterFileDependencies);
            Assert.True(diagnosticOptions.WorkspaceDiagnostics);
        }
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

        using var testWorkspace = await CreateWorkspaceAsync(options: null, mutatingLspWorkspace: false, workspaceKind: null);
        testWorkspace.InitializeDocuments(XElement.Parse(workspaceXml), openDocuments: false);

        var document = testWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var simplifierOptions = testWorkspace.GlobalOptions.GetSimplifierOptions(document.Project.Services);
        Assert.Same(SimplifierOptions.CommonDefaults, simplifierOptions);
    }

    private async Task<VSTypeScriptTestLspServer> CreateTsTestLspServerAsync(string workspaceXml, InitializationOptions? options = null)
    {
        var testWorkspace = await CreateWorkspaceAsync(options, mutatingLspWorkspace: false, workspaceKind: null);
        testWorkspace.InitializeDocuments(XElement.Parse(workspaceXml), openDocuments: false);

        return await VSTypeScriptTestLspServer.CreateAsync(testWorkspace, options ?? new InitializationOptions(), TestOutputHelper);
    }

    private sealed class ClientCallbackTarget
    {
        public List<Registration> Registrations { get; } = [];

        [JsonRpcMethod(Methods.ClientRegisterCapabilityName, UseSingleObjectParameterDeserialization = true)]
        public void ClientRegisterCapability(RegistrationParams registrationParams, CancellationToken _)
            => Registrations.AddRange(registrationParams.Registrations);
    }

    private sealed class VSTypeScriptTestLspServer : AbstractTestLspServer<LspTestWorkspace, TestHostDocument, TestHostProject, TestHostSolution>
    {
        public VSTypeScriptTestLspServer(LspTestWorkspace testWorkspace, Dictionary<string, IList<Roslyn.LanguageServer.Protocol.Location>> locations, InitializationOptions options, ITestOutputHelper testOutputHelper) : base(testWorkspace, locations, options, testOutputHelper)
        {
        }

        protected override RoslynLanguageServer CreateLanguageServer(Stream inputStream, Stream outputStream, WellKnownLspServerKinds serverKind)
        {
            var servicesProvider = TestWorkspace.ExportProvider.GetExportedValue<VSTypeScriptLspServiceProvider>();

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter))
            {
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            var languageServer = new RoslynLanguageServer(
                servicesProvider, jsonRpc, messageFormatter.JsonSerializerOptions,
                TestWorkspace.Services.HostServices,
                [InternalLanguageNames.TypeScript],
                WellKnownLspServerKinds.RoslynTypeScriptLspServer);

            jsonRpc.StartListening();
            return languageServer;
        }

        public static async Task<VSTypeScriptTestLspServer> CreateAsync(LspTestWorkspace testWorkspace, InitializationOptions options, ITestOutputHelper testOutputHelper)
        {
            var locations = await GetAnnotatedLocationsAsync(testWorkspace, testWorkspace.CurrentSolution);
            var server = new VSTypeScriptTestLspServer(testWorkspace, locations, options, testOutputHelper);
            await server.InitializeAsync();
            return server;
        }
    }

    internal sealed record TSRequest([property: JsonConverter(typeof(DocumentUriConverter))] DocumentUri Document, string Project);

    [ExportLspServiceFactory(typeof(TestLspLogger), ProtocolConstants.TypeScriptLanguageContract, WellKnownLspServerKinds.Any), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class VSTypeScriptTestLoggerFactory() : TestLspLoggerFactory
    {
    }
}

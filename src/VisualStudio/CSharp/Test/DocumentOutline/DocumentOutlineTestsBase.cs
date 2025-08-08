// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using static Roslyn.Test.Utilities.AbstractLanguageServerProtocolTests;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline;

[UseExportProvider]
public abstract class DocumentOutlineTestsBase
{
    private readonly TestOutputLspLogger _logger;
    protected DocumentOutlineTestsBase(ITestOutputHelper testOutputHelper)
    {
        _logger = new TestOutputLspLogger(testOutputHelper);
    }

    protected sealed class DocumentOutlineTestMocks : IAsyncDisposable
    {
        private readonly EditorTestWorkspace _workspace;
        private readonly IAsyncDisposable _disposable;

        internal DocumentOutlineTestMocks(
            LanguageServiceBrokerCallback<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]> languageServiceBrokerCallback,
            IThreadingContext threadingContext,
            EditorTestWorkspace workspace,
            IAsyncDisposable disposable)
        {
            LanguageServiceBrokerCallback = languageServiceBrokerCallback;
            ThreadingContext = threadingContext;
            _workspace = workspace;
            _disposable = disposable;
            TextBuffer = workspace.Documents.Single().GetTextBuffer();
        }

        internal LanguageServiceBrokerCallback<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]> LanguageServiceBrokerCallback { get; }

        internal IThreadingContext ThreadingContext { get; }

        internal ITextBuffer TextBuffer { get; }

        internal string FilePath
            => _workspace.Documents.Single().FilePath!;

        public ValueTask DisposeAsync()
            => _disposable.DisposeAsync();
    }

    private static readonly TestComposition s_composition = EditorTestCompositions.LanguageServerProtocolEditorFeatures
        .AddParts(typeof(TestDocumentTrackingService))
        .AddParts(typeof(TestWorkspaceRegistrationService))
        .RemoveParts(typeof(MockWorkspaceEventListenerProvider));

    protected async Task<DocumentOutlineTestMocks> CreateMocksAsync(string code)
    {
        var workspace = EditorTestWorkspace.CreateCSharp(code, composition: s_composition);
        var threadingContext = workspace.GetService<IThreadingContext>();

        var testLspServer = await CreateTestLspServerAsync(workspace);

        var mocks = new DocumentOutlineTestMocks(RequestAsync, threadingContext, workspace, testLspServer);
        return mocks;

        async Task<RoslynDocumentSymbol[]?> RequestAsync(Request<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]> request, CancellationToken cancellationToken)
        {
            var docRequest = (DocumentRequest<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]>)request;
            var parameters = docRequest.ParameterFactory(docRequest.TextBuffer.CurrentSnapshot);
            var response = await testLspServer.ExecuteRequestAsync<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]>(request.Method, parameters, cancellationToken);

            return response;
        }
    }

    private async Task<EditorTestLspServer> CreateTestLspServerAsync(EditorTestWorkspace workspace)
    {
        await workspace.ChangeSolutionAsync(
            workspace.CurrentSolution.WithAnalyzerReferences([new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())]));

        return await EditorTestLspServer.CreateAsync(workspace, new InitializationOptions(), _logger);
    }

    internal sealed class EditorTestLspServer : AbstractTestLspServer<EditorTestWorkspace, EditorTestHostDocument, EditorTestHostProject, EditorTestHostSolution>
    {
        private EditorTestLspServer(EditorTestWorkspace testWorkspace, Dictionary<string, IList<LanguageServer.Protocol.Location>> locations, InitializationOptions options, AbstractLspLogger logger) : base(testWorkspace, locations, options, logger)
        {
        }

        public static async Task<EditorTestLspServer> CreateAsync(EditorTestWorkspace testWorkspace, InitializationOptions initializationOptions, AbstractLspLogger logger)
        {
            var locations = await GetAnnotatedLocationsAsync(testWorkspace, testWorkspace.CurrentSolution);
            var server = new EditorTestLspServer(testWorkspace, locations, initializationOptions, logger);
            await server.InitializeAsync();
            return server;
        }
    }

    [DataContract]
    private sealed class NewtonsoftInitializeParams
    {
        [DataMember(Name = "capabilities")]
        internal object? Capabilities { get; set; }
    }
}

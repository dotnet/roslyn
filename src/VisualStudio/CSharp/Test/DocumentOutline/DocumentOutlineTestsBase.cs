﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Microsoft.VisualStudio.Text;
using StreamJsonRpc;
using Xunit.Abstractions;
using static Roslyn.Test.Utilities.AbstractLanguageServerProtocolTests;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline
{
    [UseExportProvider]
    public abstract class DocumentOutlineTestsBase
    {
        private const string PathRoot = "C:\\\ue25b\\";

        private readonly TestOutputLspLogger _logger;
        protected DocumentOutlineTestsBase(ITestOutputHelper testOutputHelper)
        {
            _logger = new TestOutputLspLogger(testOutputHelper);
        }

        protected class DocumentOutlineTestMocks : IAsyncDisposable
        {
            private readonly EditorTestWorkspace _workspace;
            private readonly IAsyncDisposable _disposable;

            internal DocumentOutlineTestMocks(
                LanguageServiceBrokerCallback<DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbolParams, DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbol[]> languageServiceBrokerCallback,
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

            internal LanguageServiceBrokerCallback<DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbolParams, DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbol[]> LanguageServiceBrokerCallback { get; }

            internal IThreadingContext ThreadingContext { get; }

            internal ITextBuffer TextBuffer { get; }

            internal string FilePath
                => PathRoot + _workspace.Documents.Single().FilePath!;

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

            var testLspServer = await CreateTestLspServerAsync(workspace, new InitializationOptions
            {
                // Set the message formatter to use newtonsoft on the client side to match real behavior.
                // Also avoid calling initialize / initialized as the test harness uses types only compatible with STJ.
                // TODO - switch back to STJ with https://github.com/dotnet/roslyn/issues/73317
                ClientMessageFormatter = new JsonMessageFormatter(),
                CallInitialize = false,
                CallInitialized = false
            });

            var mocks = new DocumentOutlineTestMocks(RequestAsync, threadingContext, workspace, testLspServer);
            return mocks;

            async Task<DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbol[]?> RequestAsync(Request<DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbolParams, DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbol[]> request, CancellationToken cancellationToken)
            {
                var docRequest = (DocumentRequest<DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbolParams, DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbol[]>)request;
                var parameters = docRequest.ParameterFactory(docRequest.TextBuffer.CurrentSnapshot);
                var response = await testLspServer.ExecuteRequestAsync<DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbolParams, DocumentSymbolNewtonsoft.NewtonsoftRoslynDocumentSymbol[]>(request.Method, parameters, cancellationToken);

                return response;
            }
        }

        private async Task<TestLspServer> CreateTestLspServerAsync(EditorTestWorkspace workspace, InitializationOptions initializationOptions)
        {
            var solution = workspace.CurrentSolution;

            foreach (var document in workspace.Documents)
            {
                if (document.IsSourceGenerated)
                    continue;

                solution = solution.WithDocumentFilePath(document.Id, PathRoot + document.Name);

                var documentText = await solution.GetRequiredDocument(document.Id).GetTextAsync(CancellationToken.None);
                solution = solution.WithDocumentText(document.Id, SourceText.From(documentText.ToString(), System.Text.Encoding.UTF8));
            }

            foreach (var project in workspace.Projects)
            {
                // Ensure all the projects have a valid file path.
                solution = solution.WithProjectFilePath(project.Id, PathRoot + project.Name);
            }

            solution = solution.WithAnalyzerReferences(new[] { new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()) });
            await workspace.ChangeSolutionAsync(solution);

            // Important: We must wait for workspace creation operations to finish.
            // Otherwise we could have a race where workspace change events triggered by creation are changing the state
            // created by the initial test steps. This can interfere with the expected test state.
            var operations = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var workspaceWaiter = operations.GetWaiter(FeatureAttribute.Workspace);
            await workspaceWaiter.ExpeditedWaitAsync();

            var server = await TestLspServer.CreateAsync(workspace, initializationOptions, _logger);

            // We disable the default test initialize call because the default test harness intialize types only support STJ (not newtonsoft).
            // We only care that initialize has been called with some capability, so call with simple objects.
            // TODO - remove with switch to STJ in https://github.com/dotnet/roslyn/issues/73317
            await server.ExecuteRequestAsync<object, object>(Roslyn.LanguageServer.Protocol.Methods.InitializeName, new NewtonsoftInitializeParams() { Capabilities = new object() }, CancellationToken.None);

            return server;
        }

        [DataContract]
        private class NewtonsoftInitializeParams
        {
            [DataMember(Name = "capabilities")]
            internal object? Capabilities { get; set; }
        }
    }
}

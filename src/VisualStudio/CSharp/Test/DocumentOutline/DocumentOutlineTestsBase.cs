// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using static Roslyn.Test.Utilities.AbstractLanguageServerProtocolTests;
using IAsyncDisposable = System.IAsyncDisposable;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

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
            private readonly TestWorkspace _workspace;
            private readonly IAsyncDisposable _disposable;

            internal DocumentOutlineTestMocks(
                ILanguageServiceBroker2 languageServiceBroker,
                IThreadingContext threadingContext,
                TestWorkspace workspace,
                IAsyncDisposable disposable)
            {
                LanguageServiceBroker = languageServiceBroker;
                ThreadingContext = threadingContext;
                _workspace = workspace;
                _disposable = disposable;
                TextBuffer = workspace.Documents.Single().GetTextBuffer();
            }

            internal ILanguageServiceBroker2 LanguageServiceBroker { get; }

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
            var workspace = TestWorkspace.CreateCSharp(code, composition: s_composition);
            var threadingContext = workspace.GetService<IThreadingContext>();

            var clientCapabilities = new LSP.ClientCapabilities()
            {
                TextDocument = new LSP.TextDocumentClientCapabilities()
                {
                    DocumentSymbol = new LSP.DocumentSymbolSetting()
                    {
                        HierarchicalDocumentSymbolSupport = true
                    }
                }
            };

            var testLspServer = await CreateTestLspServerAsync(workspace, new InitializationOptions { ClientCapabilities = clientCapabilities });
            var languageServiceBrokerMock = new Mock<ILanguageServiceBroker2>(MockBehavior.Strict);
#pragma warning disable CS0618 // Type or member is obsolete
            languageServiceBrokerMock
                .Setup(l => l.RequestAsync(It.IsAny<ITextBuffer>(), It.IsAny<Func<JToken, bool>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<ITextSnapshot, JToken>>(), It.IsAny<CancellationToken>()))
                .Returns<ITextBuffer, Func<JToken, bool>, string, string, Func<ITextSnapshot, JToken>, CancellationToken>(RequestAsync);
#pragma warning restore CS0618 // Type or member is obsolete

            var mocks = new DocumentOutlineTestMocks(languageServiceBrokerMock.Object, threadingContext, workspace, testLspServer);
            return mocks;

            async Task<ManualInvocationResponse?> RequestAsync(ITextBuffer textBuffer, Func<JToken, bool> capabilitiesFilter, string languageServerName, string method, Func<ITextSnapshot, JToken> parameterFactory, CancellationToken cancellationToken)
            {
                var request = parameterFactory(textBuffer.CurrentSnapshot).ToObject<RoslynDocumentSymbolParams>();
                var response = await testLspServer.ExecuteRequestAsync<RoslynDocumentSymbolParams, object[]>(method, request!, cancellationToken);
                return new ManualInvocationResponse(string.Empty, JToken.FromObject(response!));
            }
        }

        private async Task<TestLspServer> CreateTestLspServerAsync(TestWorkspace workspace, InitializationOptions initializationOptions)
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

            return await TestLspServer.CreateAsync(workspace, initializationOptions, _logger);
        }
    }
}

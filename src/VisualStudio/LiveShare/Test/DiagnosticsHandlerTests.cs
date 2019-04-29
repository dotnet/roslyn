// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class DiagnosticsHandlerTests : AbstractLiveShareRequestHandlerTests
    {
        [Fact]
        public async Task TestDiagnosticsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|diagnostic:var|} i = 1;
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var workspace = (TestWorkspace)solution.Workspace;
            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();

            var miscService = new DefaultDiagnosticAnalyzerService(new TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()), diagnosticService);

            DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax);

            var document = solution.Projects.First().Documents.First();
            var analyzer = miscService.CreateIncrementalAnalyzer(workspace);

            await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None);
            await analyzer.AnalyzeDocumentAsync(document, null, InvocationReasons.Empty, CancellationToken.None);

            var diagnosticLocation = ranges["diagnostic"].First();

            var results = await TestHandleAsync<TextDocumentParams, LSP.Diagnostic[]>(solution, CreateTestDocumentParams(diagnosticLocation.Uri));
            var i = 1;
            //AssertCollectionsEqual(new ClassificationSpan[] { CreateClassificationSpan("keyword", classifyLocation.Range) }, results, AssertClassificationsEqual);
        }

        private static TextDocumentParams CreateTestDocumentParams(Uri uri)
            => new TextDocumentParams()
            {
                TextDocument = CreateTextDocumentIdentifier(uri)
            };
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class DiagnosticsHandlerTests : AbstractLiveShareRequestHandlerTests
    {
        [Fact(Skip = "Need easy way to export analyzers for testing.")]
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

            var diagnosticLocation = ranges["diagnostic"].First();

            var _ = await TestHandleAsync<TextDocumentParams, LSP.Diagnostic[]>(solution, CreateTestDocumentParams(diagnosticLocation.Uri));
        }

        private static TextDocumentParams CreateTestDocumentParams(Uri uri)
            => new TextDocumentParams()
            {
                TextDocument = CreateTextDocumentIdentifier(uri)
            };
    }
}

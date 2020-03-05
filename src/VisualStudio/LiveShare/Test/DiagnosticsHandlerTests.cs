// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

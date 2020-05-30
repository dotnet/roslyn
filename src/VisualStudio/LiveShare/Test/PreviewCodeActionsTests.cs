// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class PreviewCodeActionsTests : AbstractLiveShareRequestHandlerTests
    {
        [Fact]
        public async Task TestPreviewCodeActionsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}{|edit:int|} i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = CreateTextEdit("var", locations["edit"].First().Range);

            var results = await TestHandleAsync<RunCodeActionParams, LSP.TextEdit[]>(workspace.CurrentSolution,
                CreateRunCodeActionParams(CSharpAnalyzersResources.Use_implicit_type, locations["caret"].First()),
                RoslynMethods.CodeActionPreviewName);
            AssertJsonEquals(new LSP.TextEdit[] { expected }, results);
        }

        private static LSP.TextEdit CreateTextEdit(string text, LSP.Range range)
            => new LSP.TextEdit()
            {
                NewText = text,
                Range = range
            };
    }
}

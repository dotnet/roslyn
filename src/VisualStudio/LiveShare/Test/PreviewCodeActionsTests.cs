// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = CreateTextEdit("var", locations["edit"].First().Range);

            var results = await TestHandleAsync<RunCodeActionParams, LSP.TextEdit[]>(solution, CreateRunCodeActionParams(CSharpFeaturesResources.Use_implicit_type, locations["caret"].First()));
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

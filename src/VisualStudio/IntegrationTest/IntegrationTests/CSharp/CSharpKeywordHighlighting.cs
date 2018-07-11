// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpKeywordHighlighting : AbstractIdeEditorTest
    {
        public CSharpKeywordHighlighting()
            : base(nameof(CSharpKeywordHighlighting))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ForeachAsync()
        {
            var input = @"class C
{
    void M()
    {
        [|foreach|](var c in """") { if(true) [|break|]; else [|continue|]; }
    }
}";

            MarkupTestFile.GetSpans(input, out var text, out ImmutableArray<TextSpan> spans);

            await VisualStudio.Editor.SetTextAsync(text);

            await VerifyAsync("foreach", spans);
            await VerifyAsync("break", spans);
            await VerifyAsync("continue", spans);
            await VerifyAsync("in", ImmutableArray.Create<TextSpan>());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PreprocessorConditionalsAsync()
        {
            var input = @"
#define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        {|if:#if|} Debug
            CheckConsistency();
            {|else:#if|} Trace
                WriteToLog(this.ToString());
            {|else:#else|}
                Exit();
            {|else:#endif|}
        {|if:#endif|}
        CommitHelper();
    }
}";
            MarkupTestFile.GetSpans(
                input,
                out var text,
                out IDictionary<string, ImmutableArray<TextSpan>> spans);


            await VisualStudio.Editor.SetTextAsync(text);

            await VerifyAsync("#if", spans["if"]);
            await VerifyAsync("#else", spans["else"]);
            await VerifyAsync("#endif", spans["else"]);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PreprocessorRegionsAsync()
        {
            var input = @"
class C
{
    [|#region|] Main
    static void Main()
    {
    }
    [|#endregion|]
}";

            MarkupTestFile.GetSpans(
                input,
                out var text,
                out ImmutableArray<TextSpan> spans);

            await VisualStudio.Editor.SetTextAsync(text);

            await VerifyAsync("#region", spans);
            await VerifyAsync("#endregion", spans);
        }

        private async Task VerifyAsync(string marker, ImmutableArray<TextSpan> expectedKeywordSpans)
        {
            await VisualStudio.Editor.PlaceCaretAsync(marker, charsOffset: -1);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.KeywordHighlighting);

            Assert.Equal(expectedKeywordSpans, await VisualStudio.Editor.GetKeywordHighlightTagsAsync());
        }
    }
}

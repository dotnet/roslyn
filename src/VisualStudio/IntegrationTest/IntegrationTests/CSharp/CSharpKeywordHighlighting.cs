// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpKeywordHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpKeywordHighlighting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpKeywordHighlighting))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Foreach()
        {
            var input = @"class C
{
    void M()
    {
        [|foreach|](var c in """") { if(true) [|break|]; else [|continue|]; }
    }
}";

            Roslyn.Test.Utilities.MarkupTestFile.GetSpans(input, out var text, out ImmutableArray<TextSpan> spans);

            VisualStudio.Editor.SetText(text);

            Verify("foreach", spans);
            Verify("break", spans);
            Verify("continue", spans);
            Verify("in", ImmutableArray.Create<TextSpan>());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PreprocessorConditionals()
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
            Test.Utilities.MarkupTestFile.GetSpans(
                input,
                out var text,
                out IDictionary<string, ImmutableArray<TextSpan>> spans);

            VisualStudio.Editor.SetText(text);

            Verify("#if", spans["if"]);
            Verify("#else", spans["else"]);
            Verify("#endif", spans["else"]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PreprocessorRegions()
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

            Test.Utilities.MarkupTestFile.GetSpans(
                input,
                out var text,
                out ImmutableArray<TextSpan> spans);

            VisualStudio.Editor.SetText(text);

            Verify("#region", spans);
            Verify("#endregion", spans);
        }

        private void Verify(string marker, ImmutableArray<TextSpan> expectedCount)
        {
            VisualStudio.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.KeywordHighlighting);

            Assert.Equal(expectedCount, VisualStudio.Editor.GetKeywordHighlightTags());
        }
    }
}

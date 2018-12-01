// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpKeywordHighlighting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpKeywordHighlighting( )
            : base( nameof(CSharpKeywordHighlighting))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
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

            VisualStudioInstance.Editor.SetText(text);

            VerifyKeywordHighlightTags("foreach", spans);
            VerifyKeywordHighlightTags("break", spans);
            VerifyKeywordHighlightTags("continue", spans);
            VerifyKeywordHighlightTags("in", ImmutableArray.Create<TextSpan>());
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
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


            VisualStudioInstance.Editor.SetText(text);

            VerifyKeywordHighlightTags("#if", spans["if"]);
            VerifyKeywordHighlightTags("#else", spans["else"]);
            VerifyKeywordHighlightTags("#endif", spans["else"]);
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
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

            VisualStudioInstance.Editor.SetText(text);

            VerifyKeywordHighlightTags("#region", spans);
            VerifyKeywordHighlightTags("#endregion", spans);
        }

        private void VerifyKeywordHighlightTags(string marker, ImmutableArray<TextSpan> expectedCount)
        {
            VisualStudioInstance.Editor.PlaceCaret(marker, charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification,
                FeatureAttribute.KeywordHighlighting);

            Assert.AreEqual(expectedCount, VisualStudioInstance.Editor.GetKeywordHighlightTags());
        }
    }
}

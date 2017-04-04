// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Xunit;

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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Foreach()
        {
            Editor.SetText(@"
class C
{
    void M()
    {
        foreach(var c in """") { if(true) break; else continue; }
    }
}");

            Verify("foreach", 3);
            Verify("break", 3);
            Verify("continue", 3);
            Verify("in", 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PreprocessorConditionals()
        {
            Editor.SetText(@"
#define Debug
#undef Trace
class PurchaseTransaction
{
    void Commit() {
        #if Debug
            CheckConsistency();
            #if Trace
                WriteToLog(this.ToString());
            #else
                Exit();
            #endif
        #endif
        CommitHelper();
    }
}");

            Verify("#if", expectedCount: 2);
            Verify("#else", expectedCount: 3);
            Verify("#endif", expectedCount: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PreprocessorRegions()
        {
            Editor.SetText(@"
class C
{
    #region Main
    static void Main()
    {
    }
    #endregion
}");

            Verify("#region", expectedCount: 2);
            Verify("#endregion", expectedCount: 2);
        }

        private void Verify(string marker, int expectedCount)
        {
            Editor.PlaceCaret(marker, charsOffset: -1);
            this.WaitForAsyncOperations(
               FeatureAttribute.SolutionCrawler,
               FeatureAttribute.DiagnosticService,
               FeatureAttribute.Classification,
               FeatureAttribute.KeywordHighlighting);
            Assert.Equal(expectedCount, Editor.GetKeywordHighlightTagCount());
        }
    }
}
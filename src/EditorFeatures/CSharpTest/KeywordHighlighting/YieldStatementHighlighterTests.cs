// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class YieldStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new YieldStatementHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
@"class C {
    IEnumerable<int> Range(int min, int max) {
        while (true) {
            if (min >= max) {
                {|Cursor:[|yield break|];|}
            }

            [|yield return|] min++;
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_2()
        {
            Test(
@"class C {
    IEnumerable<int> Range(int min, int max) {
        while (true) {
            if (min >= max) {
                [|yield break|];
            }

            {|Cursor:[|yield return|]|} min++;
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_3()
        {
            Test(
@"class C {
    IEnumerable<int> Range(int min, int max) {
        while (true) {
            if (min >= max) {
                yield break;
            }

            yield return {|Cursor:min++|};
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_4()
        {
            Test(
@"class C {
    IEnumerable<int> Range(int min, int max) {
        while (true) {
            if (min >= max) {
                [|yield break|];
            }

            [|yield return|] min++;{|Cursor:|}
        }
    }
}");
        }
    }
}

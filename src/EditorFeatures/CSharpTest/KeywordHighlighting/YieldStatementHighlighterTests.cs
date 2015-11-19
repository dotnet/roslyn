// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        public async Task TestExample1_1()
        {
            await TestAsync(
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
        public async Task TestExample1_2()
        {
            await TestAsync(
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
        public async Task TestExample1_3()
        {
            await TestAsync(
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
        public async Task TestExample1_4()
        {
            await TestAsync(
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class CheckedStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new CheckedStatementHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
        @"class C {
    void M() {
        short x = 0;
short y = 100;
while (true) {
    {|Cursor:[|checked|]|} {
        x++;
    }
    unchecked {
        y++;
    }
}
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
        @"class C {
    void M() {
        short x = 0;
short y = 100;
while (true) {
    checked {
        x++;
    }
    {|Cursor:[|unchecked|]|} {
        y++;
    }
}
    }
}
");
        }
    }
}

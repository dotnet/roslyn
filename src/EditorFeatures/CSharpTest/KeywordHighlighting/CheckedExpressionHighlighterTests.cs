// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class CheckedExpressionHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new CheckedExpressionHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
@"class C {
    void M() {
        short x = short.MaxValue;
short y = short.MaxValue;
int z;
try {
    z = {|Cursor:[|checked|]|}((short)(x + y));
}
catch (OverflowException e) {
    z = -1;
}
return z;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_1()
        {
            await TestAsync(
        @"class C {
    void M() {
        short x = short.MaxValue;
short y = short.MaxValue;
int z = {|Cursor:[|unchecked|]|}((short)(x + y));
return z;
    }
}
");
        }
    }
}

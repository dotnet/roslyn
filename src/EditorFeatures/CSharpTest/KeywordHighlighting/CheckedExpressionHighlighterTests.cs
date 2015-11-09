// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_1()
        {
            Test(
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

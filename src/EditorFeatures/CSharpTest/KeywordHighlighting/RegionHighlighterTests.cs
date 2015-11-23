// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class RegionHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new RegionHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
        @"class C {
{|Cursor:[|#region|]|} Main
static void Main() {
}
[|#endregion|]
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_2()
        {
            Test(
        @"class C {
[|#region|] Main
static void Main() {
}
{|Cursor:[|#endregion|]|}
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_1()
        {
            Test(
        @"class C {
{|Cursor:[|#region|]|} Main
static void Main() {
#region body
#endregion
}
[|#endregion|]
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_2()
        {
            Test(
        @"class C {
#region Main
static void Main() {
{|Cursor:[|#region|]|} body
[|#endregion|]
}
#endregion
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_3()
        {
            Test(
        @"class C {
#region Main
static void Main() {
[|#region|] body
{|Cursor:[|#endregion|]|}
}
#endregion
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedExample1_4()
        {
            Test(
        @"class C {
[|#region|] Main
static void Main() {
#region body
#endregion
}
{|Cursor:[|#endregion|]|}
}
");
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        public async Task TestExample1_1()
        {
            await TestAsync(
        @"class C {
{|Cursor:[|#region|]|} Main
static void Main() {
}
[|#endregion|]
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
        @"class C {
[|#region|] Main
static void Main() {
}
{|Cursor:[|#endregion|]|}
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_1()
        {
            await TestAsync(
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
        public async Task TestNestedExample1_2()
        {
            await TestAsync(
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
        public async Task TestNestedExample1_3()
        {
            await TestAsync(
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
        public async Task TestNestedExample1_4()
        {
            await TestAsync(
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

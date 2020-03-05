﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class RegionHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
            => new RegionHighlighter();

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
@"class C
{
    {|Cursor:[|#region|]|} Main
    static void Main()
    {
    }
    [|#endregion|]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
@"class C
{
    [|#region|] Main
    static void Main()
    {
    }
    {|Cursor:[|#endregion|]|}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_1()
        {
            await TestAsync(
@"class C
{
    {|Cursor:[|#region|]|} Main
    static void Main()
    {
        #region body
        #endregion
    }
    [|#endregion|]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_2()
        {
            await TestAsync(
@"class C
{
    #region Main
    static void Main()
    {
        {|Cursor:[|#region|]|} body
        [|#endregion|]
    }
    #endregion
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_3()
        {
            await TestAsync(
@"class C
{
    #region Main
    static void Main()
    {
        [|#region|] body
        {|Cursor:[|#endregion|]|}
    }
    #endregion
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedExample1_4()
        {
            await TestAsync(
@"class C
{
    [|#region|] Main
    static void Main()
    {
        #region body
        #endregion
    }
    {|Cursor:[|#endregion|]|}
}");
        }
    }
}

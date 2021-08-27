﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class TryStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override Type GetHighlighterType()
            => typeof(TryStatementHighlighter);

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        {|Cursor:[|try|]|}
        {
            try
            {
            }
            catch (Exception e)
            {
            }
        }
        [|finally|]
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        try
        {
            {|Cursor:[|try|]|}
            {
            }
            [|catch|] (Exception e)
            {
            }
        }
        finally
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        try
        {
            [|try|]
            {
            }
            {|Cursor:[|catch|]|} (Exception e)
            {
            }
        }
        finally
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        [|try|]
        {
            try
            {
            }
            catch (Exception e)
            {
            }
        }
        {|Cursor:[|finally|]|}
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExceptionFilter1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        try
        {
            {|Cursor:[|try|]|}
            {
            }
            [|catch|] (Exception e) [|when|] (e != null)
            {
            }
        }
        finally
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExceptionFilter2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        try
        {
            [|try|]
            {
            }
            {|Cursor:[|catch|]|} (Exception e) [|when|] (e != null)
            {
            }
        }
        finally
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExceptionFilter3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        try
        {
            [|try|]
            {
            }
            [|catch|] (Exception e) {|Cursor:[|when|]|} (e != null)
            {
            }
        }
        finally
        {
        }
    }
}");
        }
    }
}

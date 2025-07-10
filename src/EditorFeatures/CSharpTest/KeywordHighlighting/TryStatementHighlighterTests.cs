// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting;

[Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
public sealed class TryStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(TryStatementHighlighter);

    [Fact]
    public Task TestExample1_1()
        => TestAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestExample1_2()
        => TestAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestExample1_3()
        => TestAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestExample1_4()
        => TestAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestExceptionFilter1()
        => TestAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestExceptionFilter2()
        => TestAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestExceptionFilter3()
        => TestAsync(
            """
            class C
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
            }
            """);
}

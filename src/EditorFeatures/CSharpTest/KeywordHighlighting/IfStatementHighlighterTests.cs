// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting;

[Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
public sealed class IfStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(IfStatementHighlighter);

    [Fact]
    public Task TestIfStatementWithIfAndSingleElse1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    {|Cursor:[|if|]|} (a < 5)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndSingleElse2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndElseIfAndElse1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    {|Cursor:[|if|]|} (a < 5)
                    {
                        // blah
                    }
                    [|else if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndElseIfAndElse2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    {|Cursor:[|else if|]|} (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndElseIfAndElse3()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    [|else if|] (a == 10)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithElseIfOnDifferentLines1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    {|Cursor:[|if|]|} (a < 5)
                    {
                        // blah
                    }
                    [|else|]
                    [|if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithElseIfOnDifferentLines2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    [|if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithElseIfOnDifferentLines3()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    [|else|]
                    {|Cursor:[|if|]|} (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithElseIfOnDifferentLines4()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    [|else|]
                    [|if|] (a == 10)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndElseIfAndElseTouching1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    {|Cursor:[|if|]|}(a < 5)
                    {
                        // blah
                    }
                    [|else if|](a == 10)
                    {
                        // blah
                    }
                    [|else|]{
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndElseIfAndElseTouching2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|](a < 5)
                    {
                        // blah
                    }
                    {|Cursor:[|else if|]|}(a == 10)
                    {
                        // blah
                    }
                    [|else|]{
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatementWithIfAndElseIfAndElseTouching3()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|](a < 5)
                    {
                        // blah
                    }
                    [|else if|](a == 10)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}{
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestExtraSpacesBetweenElseAndIf1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    {|Cursor:[|if|]|} (a < 5)
                    {
                        // blah
                    }
                    [|else if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestExtraSpacesBetweenElseAndIf2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    {|Cursor:[|else if|]|} (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestExtraSpacesBetweenElseAndIf3()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    [|else if|] (a == 10)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestCommentBetweenElseIf1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    {|Cursor:[|if|]|} (a < 5)
                    {
                        // blah
                    }
                    [|else|] /* test */ [|if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestCommentBetweenElseIf2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|} /* test */ [|if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestCommentBetweenElseIf3()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    [|else|] /* test */ {|Cursor:[|if|]|} (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestCommentBetweenElseIf4()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    [|if|] (a < 5)
                    {
                        // blah
                    }
                    [|else|] /* test */ [|if|] (a == 10)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedIfDoesNotHighlight1()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    int b = 15;
                    {|Cursor:[|if|]|} (a < 5)
                    {
                        // blah
                        if (b < 15)
                            b = 15;
                        else
                            b = 14;
                    }
                    [|else if|] (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedIfDoesNotHighlight2()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    int b = 15;
                    [|if|] (a < 5)
                    {
                        // blah
                        if (b < 15)
                            b = 15;
                        else
                            b = 14;
                    }
                    {|Cursor:[|else if|]|} (a == 10)
                    {
                        // blah
                    }
                    [|else|]
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestNestedIfDoesNotHighlight3()
        => TestAsync(
            """
            public class C
            {
                public void Goo()
                {
                    int a = 10;
                    int b = 15;
                    [|if|] (a < 5)
                    {
                        // blah
                        if (b < 15)
                            b = 15;
                        else
                            b = 14;
                    }
                    [|else if|] (a == 10)
                    {
                        // blah
                    }
                    {|Cursor:[|else|]|}
                    {
                        // blah
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    {|Cursor:[|if|]|} (x)
                    {
                        if (y)
                        {
                            F();
                        }
                        else if (z)
                        {
                            G();
                        }
                        else
                        {
                            H();
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    if (x)
                    {
                        {|Cursor:[|if|]|} (y)
                        {
                            F();
                        }
                        [|else if|] (z)
                        {
                            G();
                        }
                        [|else|]
                        {
                            H();
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    if (x)
                    {
                        [|if|] (y)
                        {
                            F();
                        }
                        {|Cursor:[|else if|]|} (z)
                        {
                            G();
                        }
                        [|else|]
                        {
                            H();
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample2_3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    if (x)
                    {
                        [|if|] (y)
                        {
                            F();
                        }
                        [|else if|] (z)
                        {
                            G();
                        }
                        {|Cursor:[|else|]|}
                        {
                            H();
                        }
                    }
                }
            }
            """);
}

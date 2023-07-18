// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveStatementPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ConsecutiveStatementPlacement
{
    using Verify = CSharpCodeFixVerifier<
        CSharpConsecutiveStatementPlacementDiagnosticAnalyzer,
        ConsecutiveStatementPlacementCodeFixProvider>;

    public class ConsecutiveStatementPlacementTests
    {
        [Fact]
        public async Task TestNotAfterPropertyBlock()
        {
            var code =
                """
                class C
                {
                    int X { get; }
                    int Y { get; }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterMethodBlock()
        {
            var code =
                """
                class C
                {
                    void X() { }
                    void Y() { }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsOnSingleLine()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true) { } return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsOnSingleLineWithComment()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true) { }/*x*/return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsOnMultipleLinesWithCommentBetween1()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        /*x*/ return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsOnMultipleLinesWithCommentBetween2()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        /*x*/ return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsWithSingleBlankLines()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsWithSingleBlankLinesWithSpaces()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsWithMultipleBlankLines()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotAfterStatementsOnMultipleLinesWithPPDirectiveBetween1()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        #pragma warning disable CS0001
                        return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotBetweenBlockAndElseClause()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        else
                        {
                        }
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotBetweenBlockAndOuterBlocker()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                            {
                            }
                        }
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotBetweenBlockAndCase()
        {
            var code =
                """
                class C
                {
                    void M()
                    {
                        switch (0)
                        {
                            case 0:
                            {
                                break;
                            }
                            case 1:
                                break;
                        }
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestBetweenBlockAndStatement1()
        {
            await new Verify.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        [|}|]
                        return;
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        return;
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotBetweenBlockAndStatement1_WhenOptionOff()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        return;
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.TrueWithSilentEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestBetweenSwitchAndStatement1()
        {
            await new Verify.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        switch (0)
                        {
                        [|}|]
                        return;
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        switch (0)
                        {
                        }

                        return;
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestBetweenBlockAndStatement2()
        {
            await new Verify.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        [|}|] // trailing comment
                        return;
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        } // trailing comment

                        return;
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestBetweenBlockAndStatement3()
        {
            await new Verify.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        if (true) { [|}|]
                        return;
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        if (true) { }

                        return;
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestBetweenBlockAndStatement4()
        {
            await new Verify.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        switch (0)
                        {
                        case 0:
                            if (true) { [|}|]
                            return;
                        }
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        switch (0)
                        {
                        case 0:
                            if (true) { }

                            return;
                        }
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await new Verify.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        [|}|]
                        return;
                        if (true)
                        {
                        [|}|]
                        return;
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        return;
                        if (true)
                        {
                        }

                        return;
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestSA1513NegativeCases()
        {
            var code = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                public class Foo
                {
                    private int x;
                    // Valid #1
                    public int Bar
                    {
                        get { return this.x; }
                        set { this.x = value; }
                    }
                    public void Baz()
                    {
                        // Valid #2
                        try
                        {
                            this.x++;
                        }
                        catch (Exception)
                        {
                            this.x = 0;
                        }
                        finally
                        {
                            this.x++;
                        }
                        // Valid #3
                        do
                        {
                            this.x++;
                        }
                        while (this.x < 10);
                        // Valid #4
                        if (this.x > 0)
                        {
                            this.x++;
                        }
                        else
                        {
                            this.x = 0;
                        }
                        // Valid #5
                        var y = new[] { 1, 2, 3 };
                        // Valid #6
                        if (this.x > 0)
                        {
                            if (y != null)
                            {
                                this.x = -this.x;
                            }
                        }
                        // Valid #7
                        if (this.x > 0)
                        {
                            this.x = 0;
                        }
                #if !SOMETHING
                        else        
                        {
                            this.x++;    
                        }
                #endif
                        // Valid #8
                #if !SOMETHING
                        if (this.x > 0)
                        {
                            this.x = 0;
                        }
                #else
                        if (this.x < 0)        
                        {
                            this.x++;    
                        }
                #endif
                        // Valid #9
                        var q1 = 
                            from a in new[] 
                            {
                                1,
                                2,
                                3
                            }
                            from b in new[] { 4, 5, 6}
                            select a*b;
                        // Valid #10
                        var q2 = 
                            from a in new[] 
                            { 
                                1,
                                2,
                                3
                            }
                            let b = new[] 
                            { 
                                a, 
                                a * a, 
                                a * a * a 
                            }
                            select b;
                        // Valid #11
                        var q3 = 
                            from a in new[] 
                            {
                                1,
                                2,
                                3
                            }
                            where a > 0
                            select a;
                        // Valid #12
                        var q4 = 
                            from a in new[] 
                            {
                                new { Number = 1 },
                                new { Number = 2 },
                                new { Number = 3 }
                            }
                            join b in new[] 
                            { 
                                new { Number = 2 },
                                new { Number = 3 },
                                new { Number = 4 }
                            }
                            on a.Number equals b.Number
                            select new { Number1 = a.Number, Number2 = b.Number };
                        // Valid #13
                        var q5 = 
                            from a in new[] 
                            {
                                new { Number = 1 },
                                new { Number = 2 },
                                new { Number = 3 }
                            }
                            orderby a.Number descending
                            select a;
                        // Valid #14
                        var q6 = 
                            from a in new[] 
                            { 
                                1,
                                2,
                                3
                            }
                            group new
                            {
                                Number = a,
                                Square = a * a
                            }
                            by a;
                        // Valid #15
                        var d = new[]
                        {
                            1, 2, 3
                        };
                        // Valid #16
                        this.Qux(i =>
                        {
                            return d[i] * 2;
                        });
                        // Valid #17
                        if (this.x > 2)
                        {
                            this.x = 3;
                        } /* Some comment */
                        // Valid #18
                        int[] testArray;
                        testArray =
                            new[]
                            {
                                1
                            };
                        // Valid #19
                        var z1 = new object[]
                        {
                            new
                            {
                                Id = 12
                            },
                            new
                            {
                                Id = 13
                            }
                        };
                        // Valid #20
                        var z2 = new System.Action[]
                        {
                            () =>
                            {
                                this.x = 3;
                            },
                            () =>
                            {
                                this.x = 4;
                            }
                        };
                        // Valid #21
                        var z3 = new
                        {
                            Value1 = new
                            {   
                                Id = 12
                            },
                            Value2 = new
                            {
                                Id = 13
                            }
                        };
                        // Valid #22
                        var z4 = new System.Collections.Generic.List<object>
                        {
                            new
                            {
                                Id = 12
                            },
                            new
                            {
                                Id = 13
                            }
                        };
                    }
                    public void Qux(Func<int, int> function)
                    {
                        this.x = function(this.x);
                    }
                    public Func<int, int> Quux()
                    {
                        // Valid #23
                #if SOMETHING
                        return null;
                #else
                        return value =>
                        {
                            return value * 2;
                        };
                #endif
                    }
                    // Valid #24 (will be handled by SA1516)
                    public int Corge
                    {
                        get 
                        { 
                            return this.x; 
                        }
                        set { this.x = value; }
                    }
                    // Valid #25 (will be handled by SA1516)
                    public int Grault
                    {
                        set 
                        { 
                            this.x = value; 
                        }
                        get 
                        { 
                            return this.x; 
                        }
                    }
                    // Valid #26 (will be handled by SA1516)
                    public event EventHandler Garply
                    {
                        add
                        {
                        }
                        remove
                        {
                        }
                    }
                    // Valid #27 (will be handled by SA1516)
                    public event EventHandler Waldo
                    {
                        remove
                        {
                        }
                        add
                        {
                        }
                    }
                    // Valid #28 - Test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1020
                    private static IEnumerable<object> Method()
                    {
                        yield return new
                        {
                            prop = "A"
                        };
                    }
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/784
                    public void MultiLineLinqQuery()
                    {
                        var someQuery = (from f in Enumerable.Empty<int>()
                                         where f != 0
                                         select new { Fish = "Face" }).ToList();
                        var someOtherQuery = (from f in Enumerable.Empty<int>()
                                              where f != 0
                                              select new
                                              {
                                                  Fish = "AreFriends",
                                                  Not = "Food"
                                              }).ToList();
                    }
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2306
                    public void MultiLineGroupByLinqQuery()
                    {
                        var someQuery = from f in Enumerable.Empty<int>()
                                        group f by new
                                        {
                                            f,
                                        }
                                        into a
                                        select a;
                        var someOtherQuery = from f in Enumerable.Empty<int>()
                                             group f by new { f }
                                             into a
                                             select a;
                    }
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1049
                    public object[] ExpressionBodiedProperty =>
                        new[]
                        {
                            new object()
                        };
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1049
                    public object[] ExpressionBodiedMethod() =>
                        new[]
                        {
                            new object()
                        };
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1049
                    public object[] GetterOnlyAutoProperty1 { get; } =
                        new[]
                        {
                            new object()
                        };
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1049
                    public object[] GetterOnlyAutoProperty2 { get; } =
                        {
                        };
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1173
                    bool contained =
                        new[]
                        {
                            1,
                            2,
                            3
                        }
                        .Contains(3);
                    // This is a regression test for https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1583
                    public void TestTernaryConstruction()
                    {
                        var target = contained
                            ? new Dictionary<string, string>
                                {
                                    { "target", "_parent" }
                                }
                            : new Dictionary<string, string>();
                    }
                }
                """;

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestSA1513PositiveCases()
        {
            await new Verify.Test
            {
                TestCode = """
                using System;
                using System.Collections.Generic;
                public class Goo
                {
                    private int x;
                    // Invalid #1
                    public int Property1
                    {
                        get
                        {        
                            return this.x;
                        }
                        set
                        {
                            this.x = value;
                        }
                        /* some comment */
                    }
                    // Invalid #2
                    public int Property2
                    {
                        get { return this.x; }
                    }
                    public void Baz()
                    {
                        // Invalid #3
                        switch (this.x)
                        {
                            case 1:
                            {
                                this.x = 1;
                                break;
                            }
                            case 2:
                                this.x = 2;
                                break;
                        }
                        // Invalid #4
                        {
                            var temp = this.x;
                            this.x = temp * temp;
                        [|}|]
                        this.x++;
                        // Invalid #5
                        if (this.x > 1)
                        {
                            this.x = 1;
                        [|}|]
                        if (this.x < 0)
                        {
                            this.x = 0;
                        [|}|]
                        switch (this.x)
                        {
                            // Invalid #6
                            case 0:
                            if (this.x < 0)
                            {
                                this.x = -1;
                            [|}|]
                            break;
                            // Invalid #7
                            case 1:
                            {
                                var temp = this.x * this.x;
                                this.x = temp;
                            [|}|]
                            break;
                        }
                    }
                    public void Example()
                    {
                        new List<Action>
                        {
                            () =>
                            {
                                if (true)
                                {
                                    return;
                                [|}|]
                                return;
                            }
                        };
                    }
                }
                """,
                FixedCode = """
                using System;
                using System.Collections.Generic;
                public class Goo
                {
                    private int x;
                    // Invalid #1
                    public int Property1
                    {
                        get
                        {        
                            return this.x;
                        }
                        set
                        {
                            this.x = value;
                        }
                        /* some comment */
                    }
                    // Invalid #2
                    public int Property2
                    {
                        get { return this.x; }
                    }
                    public void Baz()
                    {
                        // Invalid #3
                        switch (this.x)
                        {
                            case 1:
                            {
                                this.x = 1;
                                break;
                            }
                            case 2:
                                this.x = 2;
                                break;
                        }
                        // Invalid #4
                        {
                            var temp = this.x;
                            this.x = temp * temp;
                        }

                        this.x++;
                        // Invalid #5
                        if (this.x > 1)
                        {
                            this.x = 1;
                        }

                        if (this.x < 0)
                        {
                            this.x = 0;
                        }

                        switch (this.x)
                        {
                            // Invalid #6
                            case 0:
                            if (this.x < 0)
                            {
                                this.x = -1;
                            }

                            break;
                            // Invalid #7
                            case 1:
                            {
                                var temp = this.x * this.x;
                                this.x = temp;
                            }

                            break;
                        }
                    }
                    public void Example()
                    {
                        new List<Action>
                        {
                            () =>
                            {
                                if (true)
                                {
                                    return;
                                }

                                return;
                            }
                        };
                    }
                }
                """,
                Options = { { CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }
    }
}

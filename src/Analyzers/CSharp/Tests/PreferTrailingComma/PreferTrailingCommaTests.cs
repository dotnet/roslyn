// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.PreferTrailingComma;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.PreferTrailingComma
{
    using VerifyCS = CSharpCodeFixVerifier<PreferTrailingCommaDiagnosticAnalyzer, PreferTrailingCommaCodeFixProvider>;

    public class PreferTrailingCommaTests
    {
        [Fact]
        public async Task TestOptionOff()
        {
            await new VerifyCS.Test
            {
                TestCode = @"enum A
{
    A,
    B
}",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferTrailingComma, false },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestOptionOn()
        {
            await new VerifyCS.Test
            {
                TestCode = @"enum A
{
    A,
    [|B|]
}
",
                FixedCode = @"enum A
{
    A,
    B,
}
",
                Options =
                {
                    { CSharpCodeStyleOptions.PreferTrailingComma, true },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestTriviaOnEnumMember()
        {
            var code = @"enum A
{
    A,
    // comment 1
    [|B|] // comment 2
}
";
            var fixedCode = @"enum A
{
    A,
    // comment 1
    B, // comment 2
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestEnumMemberOnSameLine()
        {
            var code = @"enum A
{
    A, [|B|] // comment
}
";
            var fixedCode = @"enum A
{
    A, B, // comment
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestEnumSingleLine()
        {
            var code = "enum A { A, B }";
            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestEmptyEnum()
        {
            var code = @"enum A
{
}
";
            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestNoNextToken()
        {
            var code = @"enum A
{
    A,
    [|B|]{|CS1513:|}";
            var fixedCode = @"enum A
{
    A,
    B,{|CS1513:|}";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestNoNextToken_SingleLine()
        {
            var code = "enum A { A, [|B|]{|CS1513:|}";
            var fixedCode = "enum A { A, B,{|CS1513:|}";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestPropertyPattern()
        {
            var code = @"class C
{
    void M(string s)
    {
        if (s is
            {
                Length: 0,
                [|Length: 0|]
            })
        {
        }
    }
}
";
            var fixedCode = @"class C
{
    void M(string s)
    {
        if (s is
            {
                Length: 0,
                Length: 0,
            })
        {
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestTriviaOnPropertyPattern()
        {
            var code = @"class C
{
    void M(string s)
    {
        if (s is
            {
                Length: 0,
                // comment 1
                [|Length: 0|] // comment 2
            })
        {
        }
    }
}
";
            var fixedCode = @"class C
{
    void M(string s)
    {
        if (s is
            {
                Length: 0,
                // comment 1
                Length: 0, // comment 2
            })
        {
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestPropertyPatternOnSameLine()
        {
            var code = @"class C
{
    void M(string s)
    {
        if (s is { Length: 0, Length: 0 })
        {
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestSwitchExpression()
        {
            var code = @"class C
{
    void M(object o)
    {
        _ = o switch
        {
            string s => 0,
            [|_ => 1|]
        };
    }
}
";
            var fixedCode = @"class C
{
    void M(object o)
    {
        _ = o switch
        {
            string s => 0,
            _ => 1,
        };
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestTriviaOnSwitchExpression()
        {
            var code = @"class C
{
    void M(object o)
    {
        _ = o switch
        {
            string s => 0,
            // comment 1
            [|_ => 1|] // comment 2
        };
    }
}
";
            var fixedCode = @"class C
{
    void M(object o)
    {
        _ = o switch
        {
            string s => 0,
            // comment 1
            _ => 1, // comment 2
        };
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestSwitchExpressionOnSameLine()
        {
            var code = @"class C
{
    void M(object o)
    {
        _ = o switch { string s => 0, _ => 1 };
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestTriviaOnInitializerExpression()
        {
            var code = @"using System;
using System.Collections.Generic;

record C
{
    public int X;
    public int Y;

    void M()
    {
        C c1 = new()
        {
            X = 0,
            // Comment 1
            [|Y = 1|] // Comment 2
        };

        var c2 = new C()
        {
            X = 0,
            // Comment 3
            [|Y = 1|] // Comment 4
        };

        var c3 = c1 with
        {
            X = 0,
            // Comment 5
            [|Y = 1|] // Comment 6
        };

        var arr1 = new int[]
        {
            0,
            // Comment 7
            [|1|] // Comment 8
        };

        var arr2 = new[]
        {
            0,
            // Comment 7
            [|1|] // Comment 8
        };

        ReadOnlySpan<int> arr3 = stackalloc int[2]
        {
            0,
            // Comment 9
            [|1|] // Comment 10
        };

        ReadOnlySpan<int> arr4 = stackalloc[]
        {
            0,
            // Comment 11
            [|1|] // Comment 12
        };

        var list = new List<int>
        {
            0,
            // Comment 13
            [|1|] // Comment 14
        };
    }
}
";
            var fixedCode = @"using System;
using System.Collections.Generic;

record C
{
    public int X;
    public int Y;

    void M()
    {
        C c1 = new()
        {
            X = 0,
            // Comment 1
            Y = 1, // Comment 2
        };

        var c2 = new C()
        {
            X = 0,
            // Comment 3
            Y = 1, // Comment 4
        };

        var c3 = c1 with
        {
            X = 0,
            // Comment 5
            Y = 1, // Comment 6
        };

        var arr1 = new int[]
        {
            0,
            // Comment 7
            1, // Comment 8
        };

        var arr2 = new[]
        {
            0,
            // Comment 7
            1, // Comment 8
        };

        ReadOnlySpan<int> arr3 = stackalloc int[2]
        {
            0,
            // Comment 9
            1, // Comment 10
        };

        ReadOnlySpan<int> arr4 = stackalloc[]
        {
            0,
            // Comment 11
            1, // Comment 12
        };

        var list = new List<int>
        {
            0,
            // Comment 13
            1, // Comment 14
        };
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestInitializerExpressionOnSameLine()
        {
            var code = @"using System;
using System.Collections.Generic;

record C
{
    public int X;
    public int Y;

    void M()
    {
        C c1 = new() { X = 0, Y = 1 };

        var c2 = new C() { X = 0, Y = 1 };

        var c3 = c1 with { X = 0, Y = 1 };

        var arr1 = new int[] { 0, 1 };

        var arr2 = new[] { 0, 1 };

        ReadOnlySpan<int> arr3 = stackalloc int[2] { 0, 1 };

        ReadOnlySpan<int> arr4 = stackalloc[] { 0, 1 };

        var list = new List<int> { 0, 1 };
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestComplexElementInitializerExpression()
        {
            // At the time of writing this test, adding a comma after `2` (comment 5) doesn't compile. So ComplexElementInitializerExpression is not supported.
            // If this changed in the future, the analyzer should support it.
            var code = @"using System.Collections;

class C : IEnumerable
{
    void M()
    {
        var c = new C()
        {
            // Comment 1
            0, // Comment 2
            [|{ // Comment 3
                1, // Comment 4
                2 // Comment 5
            }|] // Comment 6
            // Comment 7
        };
    }

    public void Add(int x) { }
    public void Add(int x, int y) { }
    public IEnumerator GetEnumerator() => null;
}
";
            var fixedCode = @"using System.Collections;

class C : IEnumerable
{
    void M()
    {
        var c = new C()
        {
            // Comment 1
            0, // Comment 2
            { // Comment 3
                1, // Comment 4
                2 // Comment 5
            }, // Comment 6
            // Comment 7
        };
    }

    public void Add(int x) { }
    public void Add(int x, int y) { }
    public IEnumerator GetEnumerator() => null;
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestAnonymousObjectExpression()
        {
            var code = @"class C
{
    void M()
    {
        var c = new
        {
            A = 0,
            [|B = 1|]
        };
    }
}
";
            var fixedCode = @"class C
{
    void M()
    {
        var c = new
        {
            A = 0,
            B = 1,
        };
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestListPattern()
        {
            var code = @"class C
{
    void M(object[] arr)
    {
        if (arr is
            [
                1,
                ..,
                [|2|]
            ])
        {
        }
    }
}
";
            var fixedCode = @"class C
{
    void M(object[] arr)
    {
        if (arr is
            [
                1,
                ..,
                2,
            ])
        {
        }
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact]
        public async Task TestListPattern_Nested()
        {
            var code = @"class C
{
    void M(object[][] arr)
    {
        if (arr is
            [
                [
                    [|0|]
                ],
                ..,
                [|[
                    [|0|]
                ]|]
            ])
        {
        }
    }
}
";
            var fixedCode = @"class C
{
    void M(object[][] arr)
    {
        if (arr is
            [
                [
                    0,
                ],
                ..,
                [
                    0,
                ],
            ])
        {
        }
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }
    }
}

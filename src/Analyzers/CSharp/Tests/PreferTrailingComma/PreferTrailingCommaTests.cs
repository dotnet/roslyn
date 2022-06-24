// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PreferTrailingComma;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
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
                TestState =
                {
                    Sources =
                    {
                        @"enum A
{
    A,
    B
}",
                    },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", @"[*]
csharp_style_prefer_trailing_comma = false"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestOptionOn()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"enum A
{
    A,
    [|B|]
}
",
                    },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", @"[*]
csharp_style_prefer_trailing_comma = true")
                    },
                },
                FixedCode = @"enum A
{
    A,
    B,
}
"
            }.RunAsync();
        }

        [Fact]
        public async Task TestHasTrailingComma()
        {
            var code = @"enum A
{
    A,
    B,
}
";
            await VerifyCS.VerifyCodeFixAsync(code, code);
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
        public async Task TestPropertyPattern()
        {
            var code = @"class C
{
    void M(string s)
    {
        if (s is { Length: 0, [|Length: 0|] })
        {
        }
    }
}
";
            var fixedCode = @"class C
{
    void M(string s)
    {
        if (s is { Length: 0, Length: 0, })
        {
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestPropertyPatternHasTrailingComma()
        {
            var code = @"class C
{
    void M(string s)
    {
        if (s is { Length: 0, Length: 0, })
        {
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(code, code);
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
    }
}

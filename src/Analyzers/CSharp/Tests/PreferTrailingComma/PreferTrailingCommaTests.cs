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
    using VerifyCS = CSharpCodeFixVerifier<PreferTrailingCommaDiagnosticAnalyzer, CodeAnalysis.Testing.EmptyCodeFixProvider>; /* PROTOTYPE: Implement codefix. */

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
    }
}

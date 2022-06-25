// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConstantInterpolatedString;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.ConstantInterpolatedString
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpConstantInterpolatedStringAnalyzer, EmptyCodeFixProvider>;

    public sealed class ConstantInterpolatedStringTests
    {
        private static async Task VerifyCodeFixAsync(string code, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestSimpleSingleConcatenationAsAttributeArgument_NoNeedForInterpolated()
        {
            var code = @"
using System.Diagnostics;

[DebuggerDisplay(""{"" + ""}"")]
public class C { }
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestSimpleSingleConcatenation_NoNeedForInterpolated()
        {
            var code = @"
public class C
{
    public void M()
    {
        const string x = ""a"" + ""b"";
    }
}
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestSimpleSingleConcatenationWithNameOf()
        {
            var code = @"
public class C
{
    public void M()
    {
        const string x = [|nameof(x) + ""}""|];
    }
}
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestMultipleConcatsWithNameOfAndBraces()
        {
            var code = @"
public class C
{
    public void M()
    {
        const string x = [|""{"" + nameof(x) + ""}""|];
    }
}
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestConcatenationAsAttributeArgument()
        {
            var code = @"
using System.Diagnostics;

[DebuggerDisplay([|""{"" + nameof(C) + ""}""|])]
public class C { }
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestConcatenationAsAttributeArgument_StringAndCharLiteralsWithNameOf()
        {
            var code = @"
using System.Diagnostics;

[DebuggerDisplay({|CS0182:""First "" + 'S' + ""econd"" + nameof(C)|})] // error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
public class C { }
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestConcatenationAsAttributeArgument_StringAndCharLiterals()
        {
            var code = @"
using System.Diagnostics;

[DebuggerDisplay({|CS0182:""First "" + 'S' + ""econd""|})] // error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
public class C { }
";
            await VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestConcatenationAsAttributeArgument_StringAndNumericLiterals()
        {
            var code = @"
using System.Diagnostics;

[DebuggerDisplay({|CS0182:""First "" + 2 + ""nd""|})] // error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
public class C { }
";
            await VerifyCodeFixAsync(code, code);
        }
    }
}

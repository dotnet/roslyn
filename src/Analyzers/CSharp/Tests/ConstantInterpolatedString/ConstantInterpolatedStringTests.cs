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
        // Should be C# 10 after release.
        private static readonly LanguageVersion s_minimumSupportedVersion = LanguageVersion.Preview;

        [Fact]
        public async Task TestSimpleSingleConcatenationAsAttributeArgument_NoNeedForInterpolated()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System.Diagnostics;

[DebuggerDisplay(""{"" + ""}"")]
public class C { }
",
                LanguageVersion = s_minimumSupportedVersion,
            }.RunAsync();
        }

        [Fact]
        public async Task TestSimpleSingleConcatenation_NoNeedForInterpolated()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
public class C
{
    public void M()
    {
        const string x = ""a"" + ""b"";
    }
}
",
                LanguageVersion = s_minimumSupportedVersion,
            }.RunAsync();
        }

        [Fact]
        public async Task TestSimpleSingleConcatenationWithNameOf()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
public class C
{
    public void M()
    {
        const string x = [|nameof(x) + ""}""|];
    }
}
",
                LanguageVersion = s_minimumSupportedVersion,
            }.RunAsync();
        }

        [Fact]
        public async Task TestMultipleConcatsWithNameOfAndBraces()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
public class C
{
    public void M()
    {
        const string x = [|""{"" + nameof(x) + ""}""|];
    }
}
",
                LanguageVersion = s_minimumSupportedVersion,
            }.RunAsync();
        }

        [Fact]
        public async Task TestConcatenationAsAttributeArgument()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System.Diagnostics;

[DebuggerDisplay([|""{"" + nameof(C) + ""}""|])]
public class C { }
",
                LanguageVersion = s_minimumSupportedVersion,
            }.RunAsync();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UsePrimaryConstructor;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUsePrimaryConstructorDiagnosticAnalyzer,
    CSharpUsePrimaryConstructorCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUsePrimaryConstructor)]
public partial class UsePrimaryConstructorTests
{
    [Fact]
    public async Task TestInCSharp12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public [|C|](int i)
                    {
                    }
                }
                """,
            FixedCode = """
                class C(int i)
                {
                }
                """,
            LanguageVersion = LanguageVersion.Preview,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public C(int i)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
        }.RunAsync();
    }
}

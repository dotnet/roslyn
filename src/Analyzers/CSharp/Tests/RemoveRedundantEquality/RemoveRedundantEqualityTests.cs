// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveRedundantEquality;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.RemoveRedundantEquality;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveRedundantEquality;

using VerifyCS = CSharpCodeFixVerifier<
   CSharpRemoveRedundantEqualityDiagnosticAnalyzer,
   RemoveRedundantEqualityCodeFixProvider>;

public sealed class RemoveRedundantEqualityTests
{
    [Fact]
    public async Task TestSimpleCaseForEqualsTrue()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|==|] true;
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestSimpleCaseForEqualsFalse_NoDiagnostics()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|==|] false;
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return !x;
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimpleCaseForNotEqualsFalse()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|!=|] false;
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestSimpleCaseForNotEqualsTrue()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|!=|] true;
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return !x;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNullable_NoDiagnostics()
    {
        var code = """
            public class C
            {
                public bool M1(bool? x)
                {
                    return x == true;
                }
            }
            """;
        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task TestWhenConstant_NoDiagnostics()
    {
        var code = """
            public class C
            {
                public const bool MyTrueConstant = true;

                public bool M1(bool x)
                {
                    return x == MyTrueConstant;
                }
            }
            """;
        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task TestOverloadedOperator_NoDiagnostics()
    {
        var code = """
            public class C
            {
                public static bool operator ==(C a, bool b) => false;
                public static bool operator !=(C a, bool b) => true;

                public bool M1(C x)
                {
                    return x == true;
                }
            }
            """;
        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task TestOnLeftHandSide()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return true [|==|] x;
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestInArgument()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return M1(x [|==|] true);
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return M1(x);
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestFixAll()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return true [|==|] x;
                }

                public bool M2(bool x)
                {
                    return x [|!=|] false;
                }

                public bool M3(bool x)
                {
                    return x [|==|] true [|==|] true;
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }

                public bool M2(bool x)
                {
                    return x;
                }

                public bool M3(bool x)
                {
                    return x;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48236")]
    public async Task TestNullableValueTypes_DoesNotCrash()
    {
        var code = """
            public class C
            {
                public bool M1(int? x)
                {
                    return x == null;
                }
            }
            """;
        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task TestSimpleCaseForIsFalse()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|is|] false;
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return !x;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestSimpleCaseForIsTrue1()
    {
        var code = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|is|] true;
                }
            }
            """;
        var fixedCode = """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestSimpleCaseForIsTrue2()
    {
        var code = """
            public class C
            {
                public const bool MyTrueConstant = true;
                public bool M1(bool x)
                {
                    return x is MyTrueConstant;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotForNullableBool()
    {
        var code = """
            public class C
            {
                public bool M1(bool? x)
                {
                    return x is true;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }
}

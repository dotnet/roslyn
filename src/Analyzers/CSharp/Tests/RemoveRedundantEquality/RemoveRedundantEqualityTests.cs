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
    public Task TestSimpleCaseForEqualsTrue()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|==|] true;
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """);

    [Fact]
    public Task TestSimpleCaseForEqualsFalse_NoDiagnostics()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact]
    public Task TestSimpleCaseForNotEqualsFalse()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|!=|] false;
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """);

    [Fact]
    public Task TestSimpleCaseForNotEqualsTrue()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact]
    public Task TestNullable_NoDiagnostics()
        => VerifyCS.VerifyAnalyzerAsync("""
            public class C
            {
                public bool M1(bool? x)
                {
                    return x == true;
                }
            }
            """);

    [Fact]
    public Task TestWhenConstant_NoDiagnostics()
        => VerifyCS.VerifyAnalyzerAsync("""
            public class C
            {
                public const bool MyTrueConstant = true;

                public bool M1(bool x)
                {
                    return x == MyTrueConstant;
                }
            }
            """);

    [Fact]
    public Task TestOverloadedOperator_NoDiagnostics()
        => VerifyCS.VerifyAnalyzerAsync("""
            public class C
            {
                public static bool operator ==(C a, bool b) => false;
                public static bool operator !=(C a, bool b) => true;

                public bool M1(C x)
                {
                    return x == true;
                }
            }
            """);

    [Fact]
    public Task TestOnLeftHandSide()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return true [|==|] x;
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """);

    [Fact]
    public Task TestInArgument()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return M1(x [|==|] true);
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return M1(x);
                }
            }
            """);

    [Fact]
    public Task TestFixAll()
        => VerifyCS.VerifyCodeFixAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48236")]
    public Task TestNullableValueTypes_DoesNotCrash()
        => VerifyCS.VerifyAnalyzerAsync("""
            public class C
            {
                public bool M1(int? x)
                {
                    return x == null;
                }
            }
            """);

    [Fact]
    public Task TestSimpleCaseForIsFalse()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|is|] false;
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

    [Fact]
    public Task TestSimpleCaseForIsTrue1()
        => VerifyCS.VerifyCodeFixAsync("""
            public class C
            {
                public bool M1(bool x)
                {
                    return x [|is|] true;
                }
            }
            """, """
            public class C
            {
                public bool M1(bool x)
                {
                    return x;
                }
            }
            """);

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

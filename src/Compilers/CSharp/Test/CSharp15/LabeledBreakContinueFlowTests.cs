// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class LabeledBreakContinueFlowTests : CSharpTestBase
{
    #region Definite assignment

    [Fact]
    public void DefiniteAssignment_BreakOuter_CarriesAssignmentOutOfLoop()
    {
        var source = """
            class C
            {
                int M(bool b)
                {
                    int x;
                    outer: while (true)
                    {
                        while (true)
                        {
                            x = 1;
                            break outer;
                        }
                    }

                    return x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void DefiniteAssignment_BreakOuter_NotAssignedOnJumpPath_ReportsCS0165()
    {
        var source = """
            class C
            {
                int M(bool b)
                {
                    int x;
                    outer: while (true)
                    {
                        while (true)
                        {
                            if (b)
                                break outer;
                            x = 1;
                        }
                    }

                    return x;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (16,16): error CS0165: Use of unassigned local variable 'x'
            //         return x;
            Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(16, 16));
    }

    [Fact]
    public void DefiniteAssignment_ContinueOuter_SkipsOuterAssignment_ReportsCS0165()
    {
        var source = """
            class C
            {
                void M(bool b)
                {
                    outer: while (b)
                    {
                        int x;
                        while (b)
                        {
                            continue outer;
                        }

                        x = 1;
                        System.Console.WriteLine(x);
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void DefiniteAssignment_OutParameter_AssignedOnlyViaBreakOuter()
    {
        var source = """
            class C
            {
                void M(bool b, out int x)
                {
                    outer: while (true)
                    {
                        while (true)
                        {
                            x = 1;
                            break outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Nullable reference type flow

    [Fact]
    public void Nullable_BreakOuter_NarrowedStateFlowsPastLoop()
    {
        var source = """
            #nullable enable
            class C
            {
                void M(string? s)
                {
                    outer: while (true)
                    {
                        while (true)
                        {
                            if (s != null)
                                break outer;
                        }
                    }

                    s.ToString();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Nullable_BreakOuter_MaybeNullStateFlowsPastLoop_ReportsCS8602()
    {
        var source = """
            #nullable enable
            class C
            {
                void M(string? s, bool b)
                {
                    outer: while (true)
                    {
                        while (true)
                        {
                            if (b)
                                break outer;
                        }
                    }

                    s.ToString();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (15,9): warning CS8602: Dereference of a possibly null reference.
            //         s.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(15, 9));
    }

    [Fact]
    public void Nullable_ContinueOuter_BackEdgeMergesState()
    {
        var source = """
            #nullable enable
            class C
            {
                void M(bool b)
                {
                    string? s = "start";
                    outer: while (b)
                    {
                        s.ToString();
                        s = null;
                        while (b)
                        {
                            s = "inner";
                            continue outer;
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,13): warning CS8602: Dereference of a possibly null reference.
            //             s.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(9, 13));
    }

    #endregion
}

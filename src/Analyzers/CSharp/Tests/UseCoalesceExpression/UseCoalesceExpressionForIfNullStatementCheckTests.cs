// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCoalesceExpression;
using Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCoalesceExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer,
    CSharpUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
public sealed class UseCoalesceExpressionForIfNullStatementCheckTests
{
    [Fact]
    public Task TestLocalDeclaration_ThrowStatement()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    [|if|] (item == null)
                        throw new System.InvalidOperationException();
                }

                object FindItem() => null;
            }
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C ?? throw new System.InvalidOperationException();
                }
            
                object FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_Block()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    [|if|] (item == null)
                    {
                        throw new System.InvalidOperationException();
                    }
                }

                object FindItem() => null;
            }
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C ?? throw new System.InvalidOperationException();
                }
            
                object FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_IsPattern()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    [|if|] (item is null)
                        throw new System.InvalidOperationException();
                }

                object FindItem() => null;
            }
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C ?? throw new System.InvalidOperationException();
                }
            
                object FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_Assignment1()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    [|if|] (item == null)
                        item = new C();
                }

                object FindItem() => null;
            }
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C ?? new C();
                }
            
                object FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_Assignment2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    [|if|] (item == null)
                        item = new();
                }

                object FindItem() => null;
            }
            """,
            FixedCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C ?? new();
                }
            
                object FindItem() => null;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithWrongItemChecked()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(C item1)
                {
                    var item = FindItem() as C;
                    if (item1 == null)
                        throw new System.InvalidOperationException();
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithWrongCondition()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    if (item != null)
                        throw new System.InvalidOperationException();
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithWrongPattern()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    if (item is not null)
                        throw new System.InvalidOperationException();
                }

                object FindItem() => null;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithWrongAssignment()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(C item1)
                {
                    var item = FindItem() as C;
                    if (item == null)
                        item1 = new C();
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithElseBlock()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(C item1)
                {
                    var item = FindItem() as C;
                    if (item == null)
                        item = new C();
                    else
                        item = null;
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithMultipleWhenTrueStatements()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(C item1)
                {
                    var item = FindItem() as C;
                    if (item == null)
                    {
                        item = new C();
                        item = null;
                    }
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithNoWhenTrueStatements()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(C item1)
                {
                    var item = FindItem() as C;
                    if (item == null)
                    {
                    }
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithThrowWithoutExpression()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch
                    {
                        var item = FindItem() as C;
                        if (item == null)
                            throw;
                    }
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithLocalWithoutInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    C item;
                    if ({|CS0165:item|} == null)
                        throw new System.InvalidOperationException();
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithValueTypeInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    object item = 0;
                    if (item == null)
                        item = null;
                }

                object FindItem() => null;
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestLocalDeclaration_NotWithReferenceToVariableInThrow()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    var item = FindItem() as C;
                    if (item is null)
                        throw new System.InvalidOperationException(nameof(item));
                }

                object FindItem() => null;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")]
    public Task TestLocalDeclaration_CastWithParenthesizedExpression()
        => new VerifyCS.Test
        {
            TestCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(object o)
                {
                    I item = o as C;
                    [|if|] (item == null)
                    {
                        item = o as D;
                    }
                }
            }

            class D : I
            {
            }
            """,
            FixedCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(object o)
                {
                    I item = (I)(o as C) ?? o as D;
                }
            }

            class D : I
            {
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")]
    public Task TestLocalDeclaration_CastWithoutParenthesizedExpression()
        => new VerifyCS.Test
        {
            TestCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(C c, D d)
                {
                    I item = c;
                    [|if|] (item == null)
                    {
                        item = d;
                    }
                }
            }

            class D : I
            {
            }
            """,
            FixedCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(C c, D d)
                {
                    I item = (I)c ?? d;
                }
            }

            class D : I
            {
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")]
    public Task TestLocalDeclaration_NoCastWhenEqualSymbol()
        => new VerifyCS.Test
        {
            TestCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(C c1, C c2)
                {
                    I item = c1;
                    [|if|] (item == null)
                    {
                        item = c2;
                    }
                }
            }
            """,
            FixedCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(C c1, C c2)
                {
                    I item = c1 ?? c2;
                }
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")]
    public Task TestLocalDeclaration_NoCastWhenDerivedClass()
        => new VerifyCS.Test
        {
            TestCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(C c, D d)
                {
                    I item = c;
                    [|if|] (item == null)
                    {
                        item = d;
                    }
                }
            }

            class D : C
            {
            }
            """,
            FixedCode =
            """
            interface I
            {
            }

            class C : I
            {
                void M(C c, D d)
                {
                    I item = c ?? d;
                }
            }

            class D : C
            {
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")]
    public Task TestLocalDeclaration_NoCastWhenDerivedClassReversed()
        => new VerifyCS.Test
        {
            TestCode =
            """
            interface I
            {
            }

            class C : D
            {
                void M(C c, D d)
                {
                    I item = c;
                    [|if|] (item == null)
                    {
                        item = d;
                    }
                }
            }

            class D : I
            {
            }
            """,
            FixedCode =
            """
            interface I
            {
            }

            class C : D
            {
                void M(C c, D d)
                {
                    I item = c ?? d;
                }
            }

            class D : I
            {
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70514")]
    public Task TestNotAcrossPreprocessorRegion()
        => new VerifyCS.Test
        {
            TestCode = """
                #define DEBUG

                class C
                {
                    void M()
                    {
                        var item = FindItem() as C;
                #if DEBUG
                        if (item == null)
                            throw new System.InvalidOperationException();
                #endif
                    }

                    object FindItem() => null;
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81987")]
    public Task TestPointer1_A()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                unsafe void M()
                {
                    var item = FindItem();
                    if (item == null)
                        throw new System.InvalidOperationException();
                }

                unsafe int* FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81987")]
    public Task TestPointer1_B()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                unsafe void M(int* item)
                {
                    item = FindItem();
                    if (item == null)
                        throw new System.InvalidOperationException();
                }

                unsafe int* FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81987")]
    public Task TestPointer2_A()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                unsafe void M()
                {
                    var item = FindItem();
                    if (item == null)
                        throw new System.InvalidOperationException();
                }

                unsafe void* FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81987")]
    public Task TestPointer2_B()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                unsafe void M(void* item)
                {
                    item = FindItem();
                    if (item == null)
                        throw new System.InvalidOperationException();
                }

                unsafe void* FindItem() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestNotOfferedWithDirectivesOnIfStatement_LeadingPragma()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                public void M()
                {
            #pragma warning disable
                    var value = M2();
                    // Test
            #pragma warning restore
                    if (value == null)
                    {
                        throw new System.InvalidOperationException();
                    }
                }

                string? M2() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestNotOfferedWithDirectivesOnIfStatement_LeadingRegion()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                public void M()
                {
                    var value = M2();
            #region Test
                    if (value == null)
                    {
                        throw new System.InvalidOperationException();
                    }
            #endregion
                }

                string? M2() => null;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestNotOfferedWithDirectivesOnIfStatement_TrailingDefine()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                public void M()
                {
                    var value = M2();
                    if (value == null)
            #define TEST
                    {
                        throw new System.InvalidOperationException();
                    }
                }

                string? M2() => null;
            }
            """
        }.RunAsync();
}

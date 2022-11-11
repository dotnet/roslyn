' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression.VisualBasicUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionForIfNullStatementCheckCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseCoalesceExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
    Public Class UseCoalesceExpressionForIfNullStatementCheckTests
        <Fact>
        Public Async Function TestLocalDeclaration_ThrowStatement() As Task
            Await New VerifyVB.Test() With {
                .TestCode = """
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
                .FixedCode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C ?? throw new System.InvalidOperationException();
                    }
                
                    object FindItem() => null;
                }
                """
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_Block() As Task
            Await New VerifyVB.Test With {
                .TestCode = """
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
                .FixedCode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C ?? throw new System.InvalidOperationException();
                    }
                
                    object FindItem() => null;
                }
                """
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_IsPattern() As Task
            Await New VerifyVB.Test With {
                .TestCode = """
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
                .FixedCode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C ?? throw new System.InvalidOperationException();
                    }
                
                    object FindItem() => null;
                }
                """
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_Assignment1() As Task
            Await New VerifyVB.Test With {
                .TestCode = """
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
                .FixedCode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C ?? new C();
                    }
                
                    object FindItem() => null;
                }
                """
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_Assignment2() As Task
            Await New VerifyVB.Test with {
                .testcode = """
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
                .fixedcode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C ?? new();
                    }
                
                    object FindItem() => null;
                }
                """
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongItemChecked() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongCondition() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongPattern() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongAssignment() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithElseBlock() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithMultipleWhenTrueStatements() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithNoWhenTrueStatements() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithThrowWithoutExpression() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithLocalWithoutInitializer() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithValueTypeInitializer() As Task
            Dim text = """
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
                """

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function
    End Class
End Namespace

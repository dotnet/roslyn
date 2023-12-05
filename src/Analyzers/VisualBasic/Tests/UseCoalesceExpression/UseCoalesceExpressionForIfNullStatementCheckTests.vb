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
        Public Async Function TestLocalDeclaration_NotWithThrowStatement() As Task
            Dim test = "
                class C
                    sub M()
                        dim item = TryCast(FindItem(), C)
                        if item is nothing
                            throw new System.InvalidOperationException()
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test() With {
                .TestCode = test,
                .FixedCode = test
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_Assignment1() As Task
            Await New VerifyVB.Test With {
                .TestCode = "
class C
    sub M()
        dim item = TryCast(FindItem(), C)
        [|if|] item is nothing
            item = new C()
        end if
    end sub

    function FindItem() as object
        return nothing
    end function
end class
                ",
                .FixedCode = "
class C
    sub M()
        dim item = If(TryCast(FindItem(), C), new C())
    end sub

    function FindItem() as object
        return nothing
    end function
end class
                "
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongItemChecked() As Task
            Dim text = "
                class C
                    sub M(item1 as C)
                        dim item = TryCast(FindItem(), C)
                        if item1 is nothing
                            item = nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongCondition() As Task
            Dim text = "
                class C
                    sub M()
                        dim item = TryCast(FindItem(), C)
                        if item isnot nothing
                            item = nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithWrongAssignment() As Task
            Dim text = "
                class C
                    sub M(item1 as C)
                        dim item = TryCast(FindItem(), C)
                        if item is nothing
                            item1 = new C()
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithElseBlock() As Task
            Dim text = "
                class C
                    sub M(item1 as C)
                        dim item = TryCast(FindItem(), C)
                        if item is nothing
                            item = new C()
                        else
                            item = nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithMultipleWhenTrueStatements() As Task
            Dim text = "
                class C
                    sub M(item1 as C)
                        dim item = TryCast(FindItem(), C)
                        if item is nothing
                            item = new C()
                            item = nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithNoWhenTrueStatements() As Task
            Dim text = "
                class C
                    sub M(item1 as C)
                        dim item = TryCast(FindItem(), C)
                        if item is nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithLocalWithoutInitializer() As Task
            Dim text = "
                class C
                    sub M()
                        dim item as C
                        if item is nothing
                            item = nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestLocalDeclaration_NotWithValueTypeInitializer() As Task
            Dim text = "
                class C
                    sub M()
                        dim item as object = 0
                        if item is nothing
                            item = nothing
                        end if
                    end sub

                    function FindItem() as object
                        return nothing
                    end function
                end class
                "

            Await New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = text
            }.RunAsync()
        End Function
    End Class
End Namespace

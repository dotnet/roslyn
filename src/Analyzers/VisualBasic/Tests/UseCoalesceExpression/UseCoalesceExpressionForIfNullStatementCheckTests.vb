' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression.VisualBasicUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression.VisualBasicUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider)

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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")>
        Public Async Function TestLocalDeclaration_NoCastBaseAssignment() As Task
            Await New VerifyVB.Test With {
                .TestCode = "
interface I
end interface

class C
    implements I

    sub Main(o as object)
        dim item as I = TryCast(o, C)
        [|if|] item is nothing then
            item = TryCast(o, D)
        end if
    end sub
end class

class D
    implements I
end class
                ",
                .FixedCode = "
interface I
end interface

class C
    implements I

    sub Main(o as object)
        dim item as I = If(TryCast(o, C), TryCast(o, D))
    end sub
end class

class D
    implements I
end class
                "
            }.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74460")>
        Public Async Function TestLocalDeclaration_NoCastBaseAssignment1() As Task
            Await New VerifyVB.Test With {
                .TestCode = "
interface I
end interface

class C
    implements I

    sub Main(c as C, d as D)
        dim item as I = c
        [|if|] item is nothing then
            item = d
        end if
    end sub
end class

class D
    implements I
end class
                ",
                .FixedCode = "
interface I
end interface

class C
    implements I

    sub Main(c as C, d as D)
        dim item as I = If(c, d)
    end sub
end class

class D
    implements I
end class
                "
            }.RunAsync()
        End Function
    End Class
End Namespace

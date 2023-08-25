' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedVariable
    Partial Public Class RemoveUnusedVariableTest
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicRemoveUnusedVariableCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariable() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|x as String|]
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariable1() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|x|], c as String
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
        Dim c as String
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable), Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function RemoveUnusedVariableFixAll() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x, c as String
        Dim {|FixAllInDocument:a as String|}
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariableAndComment() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|a|] As Integer ' inline comment also to be deleted. 
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24076"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariableWithAssignment() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|a|] As Integer = 0
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24076"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedWithImplicitConversionAndAssignment() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|a|] As Short = 0
        a = 1
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24076"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedLambda() As Task
            Dim markup =
<File>
Module M
    Public Class C
        Function F() As Integer
            Dim L As Func(Of Integer) = Function()
                                            Dim a As Integer = 0
                                            Dim [|unused|] As Func(Of Integer) = Function()
                                                                                 Dim b As Integer = 0
                                                                                 Return 1
                                                                             End Function
                                            Return 1
                                        End Function
            Return L()
        End Function
    End Class
End Module
</File>
            Dim expected =
<File>
Module M
    Public Class C
        Function F() As Integer
            Dim L As Func(Of Integer) = Function()
                                            Dim a As Integer = 0
                                            Return 1
                                        End Function
            Return L()
        End Function
    End Class
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function JointDeclarationRemoveFirst() As Task
            Dim markup =
<File>
Module M
    Function F() As Integer
        Dim [|a|] As Integer, b As Integer
        Return b
    End Function
End Module
</File>
            Dim expected =
<File>
Module M
    Function F() As Integer
        Dim b As Integer
        Return b
    End Function
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24076"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function JointDeclarationAndAssignmentRemoveFirst() As Task
            Dim markup =
<File>
Module M
    Function F() As Integer
        Dim [|a|] As Integer = 0, b As Integer = 0
        Return b
    End Function
End Module
</File>
            Dim expected =
<File>
Module M
    Function F() As Integer
        Dim b As Integer
        Return b
    End Function
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function JointDeclarationRemoveSecond() As Task
            Dim markup =
<File>
Module M
    Function F() As Integer
        Dim a As Integer, [|b|] As Integer
        Return a
    End Function
End Module
</File>
            Dim expected =
<File>
Module M
    Function F() As Integer
        Dim a As Integer
        Return a
    End Function
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24076"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function JointDeclarationAndAssignmentRemoveSecond() As Task
            Dim markup =
<File>
Module M
    Function F() As Integer
        Dim a As Integer = 0, [|b|] As Integer = 0
        Return a
    End Function
End Module
</File>
            Dim expected =
<File>
Module M
    Function F() As Integer
        Dim a As Integer
        Return a
    End Function
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24076"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable), Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function JointDeclarationAndAssignmentRemoveBoth() As Task
            Dim markup =
<File>
Module M
    Sub F()
        Dim {|FixAllInDocument:a as Integer|} = 0, b As Integer = 0
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub F()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable), Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function RemoveUnusedVariable_FixAllInContainingMember() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x, c as String
        Dim {|FixAllInContainingMember:a as String|}
    End Sub

    Sub M2()
        Dim x, c as String
        Dim a as String
    End Sub
End Module

Class OtherType
    Sub M3()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub

    Sub M2()
        Dim x, c as String
        Dim a as String
    End Sub
End Module

Class OtherType
    Sub M3()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable), Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function RemoveUnusedVariable_FixAllInContainingType_AcrossSingleFile() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x, c as String
        Dim {|FixAllInContainingType:a as String|}
    End Sub

    Sub M2()
        Dim x, c as String
        Dim a as String
    End Sub
End Module

Class OtherType
    Sub M3()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub

    Sub M2()
    End Sub
End Module

Class OtherType
    Sub M3()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable), Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function RemoveUnusedVariable_FixAllInContainingType_AcrossMultipleFiles() As Task
            Dim markup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Sub Main()
        Dim x, c as String
        Dim {|FixAllInContainingType:a as String|}
    End Sub
End Class

Partial Class C
    Sub M2()
        Dim x, c as String
        Dim a as String
    End Sub
End Class

Class OtherType
    Sub OtherMethod()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</Document>
                        <Document>
Partial Class C
    Sub M3()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Sub Main()
    End Sub
End Class

Partial Class C
    Sub M2()
    End Sub
End Class

Class OtherType
    Sub OtherMethod()
        Dim x, c as String
        Dim a as String
    End Sub
End Class
</Document>
                        <Document>
Partial Class C
    Sub M3()
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>.ToString()
            Await TestInRegularAndScript1Async(markup, expected)
        End Function
    End Class
End Namespace

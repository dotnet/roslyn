' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InvertIf

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertIf
    Public Class InvertMultiLineIfTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertMultiLineIfCodeRefactoringProvider()
        End Function

        Public Async Function TestFixOneAsync(initial As String, expected As String) As Task
            Await TestInRegularAndScriptAsync(CreateTreeText(initial), CreateTreeText(expected))
        End Function

        Function CreateTreeText(initial As String) As String
            Return "
Module Module1
    Sub Main()
        Dim a As Boolean = True
        Dim b As Boolean = True
        Dim c As Boolean = True
        Dim d As Boolean = True

" + initial + "
    End Sub

    Private Sub aMethod()

    End Sub

    Private Sub bMethod()

    End Sub

    Private Sub cMethod()

    End Sub

    Private Sub dMethod()

    End Sub

End Module

"
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultiLineIdentifier() As Task
            Await TestFixOneAsync(
"
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If
",
"
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
")
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestElseIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Sub Main()
        If a Then
            aMethod()
        [||]ElseIf b Then
            bMethod()
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepElseIfKeyword() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        If a Then
            aMethod()
        [||]ElseIf b Then
            bMethod()
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnIfElseIfElse() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        I[||]f a Then
            aMethod()
        Else If b Then
            bMethod()
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSelection() As Task
            Await TestFixOneAsync(
"
        [|If a Then
            aMethod()
        Else
            bMethod()
        End If|]
",
"
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotOverlapHiddenPosition1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
#End ExternalSource
        goo()
#ExternalSource File.vb 1 
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
#End ExternalSource
        goo()
#ExternalSource File.vb 1 
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotOverlapHiddenPosition2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
#ExternalSource File.vb 1 
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If
#End ExternalSource
    End Sub
End Module",
"Module Program
    Sub Main()
#ExternalSource File.vb 1 
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
#End ExternalSource
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
#ExternalSource File.vb 1 
            aMethod()
#End ExternalSource
        Else
            bMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        If a Then
            aMethod()
        [||]Else If b Then
#ExternalSource File.vb 1 
            bMethod()
#End ExternalSource
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition3() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
#ExternalSource File.vb 1 
        Else If b Then
            bMethod()
#End ExternalSource
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition4() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
        Else
#ExternalSource File.vb 1 
            bMethod()
#End ExternalSource
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition5() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
#ExternalSource File.vb 1 
            aMethod()
        Else
            bMethod()
#End ExternalSource
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition6() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
#ExternalSource File.vb 1 
        Else
#End ExternalSource
            bMethod()
        End If
    End Sub
End Module")
        End Function



        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultipleStatementsMultiLineIfBlock() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            goo()
            bar()
        Else
            you()
            too()
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        If Not a Then
            you()
            too()
        Else
            goo()
            bar()
        End If
    End Sub
End Module")
        End Function



        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTriviaAfterMultiLineIfBlock() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If ' I will stay put 
    End Sub
End Module",
"Module Program
    Sub Main()
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If ' I will stay put 
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepExplicitLineContinuationTriviaMethod() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        I[||]f a And b _
        Or c Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        If (Not a Or Not b) _
        And Not c Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepTriviaInStatementsInMultiLineIfBlock() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()

        Else
            bMethod()


        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        If Not a Then
            bMethod()
        Else
            aMethod()


        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If x.Length > 0 Then
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If x.Length = 0 Then
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x() As String
        [||]If x.Length > 0 Then
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x() As String
        If x.Length = 0 Then
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero4() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x() As String
        [||]If x.Length > 0x0 Then 
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x() As String
        If x.Length = 0x0 Then 
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero5() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If 0 < x.Length Then
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If 0 = x.Length Then
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotSimplifyToLengthEqualsZero() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If x.Length >= 0 Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If x.Length < 0 Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotSimplifyToLengthEqualsZero2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If x.Length > 0.0 Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If x.Length <= 0.0 Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529748")>
        <WorkItem(530593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530593")>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestColonAfterSingleLineIfWithEmptyElse() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        ' Invert If 
        I[||]f False Then Return Else : Console.WriteLine(1)
    End Sub
End Module",
"Module Program
    Sub Main()
        ' Invert If 
        If True Then Else Return
        Console.WriteLine(1)
    End Sub
End Module")
        End Function


        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnlyOnElseIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        If False Then
            Return
        ElseIf True [||]Then
            Console.WriteLine(""b"")
        Else
            Console.WriteLine(""a"")
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnConditionOfMultiLineIfStatement() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        If [||]False Then
            Return
        Else
            Console.WriteLine(""a"")
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        If [||]True Then
            Console.WriteLine(""a"")
        Else
            Return
        End If
    End Sub
End Module")
        End Function

        <WorkItem(531474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531474")>
        <WpfFact(Skip:="531474"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoNotRemoveTypeCharactersDuringComplexification() As Task
            Dim markup =
<File>
Imports System
    Module Program
        Sub Main()
            Goo(Function(take)
                    [||]If True Then Console.WriteLine("true") Else Console.WriteLine("false")
                    take$.ToString()
                    Return Function() 1
                End Function)
        End Sub
        Sub Goo(Of T)(x As Func(Of String, T))
        End Sub
        Sub Goo(Of T)(x As Func(Of Integer, T))
        End Sub
    End Module
</File>

            Dim expected =
<File>
Imports System
    Module Program
        Sub Main()
            Goo(Function(take)
                    If False Then Console.WriteLine("false") Else Console.WriteLine("true")
                    take$.ToString()
                    Return Function() 1
                End Function)
        End Sub
        Sub Goo(Of T)(x As Func(Of String, T))
        End Sub
        Sub Goo(Of T)(x As Func(Of Integer, T))
        End Sub
    End Module
</File>

            Await TestAsync(markup, expected)
        End Function
    End Class
End Namespace

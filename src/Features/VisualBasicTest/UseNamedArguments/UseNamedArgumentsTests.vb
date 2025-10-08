' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.UseNamedArguments

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseNamedArguments
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)>
    Public Class UseNamedArgumentsTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicUseNamedArgumentsCodeRefactoringProvider()
        End Function

        Private Shared ReadOnly s_vb15Parameters As TestParameters =
            New TestParameters(parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

        <Fact>
        Public Async Function TestFirstArgument() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, arg2 As Integer)
        M([||]1, 2)
    End Sub
End Class",
"Class C
    Sub M(arg1 As Integer, arg2 As Integer)
        M(arg1:=1, arg2:=2)
    End Sub
End Class", parameters:=s_vb15Parameters)
        End Function

        <Fact>
        Public Async Function TestNonFirstArgument() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, arg2 As Integer)
        M(1, [||]2)
    End Sub
End Class",
"Class C
    Sub M(arg1 As Integer, arg2 As Integer)
        M(1, arg2:=2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestDelegate() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        Dim f = Sub (arg)
                End Sub
        f([||]1)
    End Sub
End Class",
"Class C
    Sub M()
        Dim f = Sub (arg)
                End Sub
        f(arg:=1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestConditionalDelegate() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        Dim f = Sub (arg)
                End Sub
        f?([||]1)
    End Sub
End Class",
"Class C
    Sub M()
        Dim f = Sub (arg)
                End Sub
        f?(arg:=1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestConditionalMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 as Integer, arg2 as Integer)
        Me?.M([||]1, 2)
    End Sub
End Class",
"Class C
    Sub M(arg1 as Integer, arg2 as Integer)
        Me?.M(arg1:=1, arg2:=2)
    End Sub
End Class", parameters:=s_vb15Parameters)
        End Function

        <Fact>
        Public Async Function TestConditionalIndexer() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 as String)
        Dim r = arg1?([||]0)
    End Sub
End Class",
"Class C
    Sub M(arg1 as String)
        Dim r = arg1?(index:=0)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestConstructor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub New(arg1 As Integer, arg2 As Integer)
        Dim c = New C([||]1, 2)
    End Sub
End Class",
"Class C
    Sub New(arg1 As Integer, arg2 As Integer)
        Dim c = New C(arg1:=1, arg2:=2)
    End Sub
End Class", parameters:=s_vb15Parameters)
        End Function

        <Fact>
        Public Async Function TestIndexer() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(arg1 as String) As Char
        Return arg1([||]0)
    End Function
End Class",
"Class C
    Function M(arg1 as String) As Char
        Return arg1(index:=0)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnArrayIndexer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Function M(arg1 as Integer() As Integer
        Return arg1([||]0)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnConditionalArrayIndexer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Function M(arg1 as Integer() As Integer
        Return arg1?([||]0)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnEmptyArgumentList() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M()
        M([||])
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnNamedArgument() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(arg as Integer)
        M([||]arg:=1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnParamArray() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(ParamArray arg1 As Integer())
        M([||]1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestEmptyParamArray() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, ParamArray arg2 As Integer())
        M([||]1)
    End Sub
End Class",
"Class C
    Sub M(arg1 As Integer, ParamArray arg2 As Integer())
        M(arg1:=1)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOmittedArguments1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1)
        M([||]1,,3)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOmittedArguments2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1)
        M(1,,[||]3)
    End Sub
End Class",
"Class C
    Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1)
        M(1,, arg3:=3)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnOmittedArgument() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(optional arg1 As Integer=1, optional arg2 As Integer=1)
        M([||], arg2:=2)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnNameOf() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Function M() As String
        Return NameOf([||]M)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnAttribute() As Task
            Await TestMissingInRegularAndScriptAsync(
"<C([||]1)>
Class C
    Inherits System.Attribute
    Public Sub New(arg As Integer)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")>
        Public Async Function TestCaretPositionAtTheEnd1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer)
        M(arg1[||])
    End Sub
End Class",
"Class C
    Sub M(arg1 As Integer)
        M(arg1:=arg1)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")>
        Public Async Function TestCaretPositionAtTheEnd2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, arg2 As Integer)
        M(arg1[||], arg2)
    End Sub
End Class",
"Class C
    Sub M(arg1 As Integer, arg2 As Integer)
        M(arg1:=arg1, arg2:=arg2)
    End Sub
End Class", parameters:=s_vb15Parameters)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")>
        Public Async Function TestCaretPositionAtTheEnd3() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1)
        M(1,[||],3)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")>
        Public Async Function TestCaretPositionAtTheEnd4() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1)
        M(1[||],,3)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")>
        Public Async Function TestCaretPositionAtTheEnd5() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Function M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1) As Integer
        M(1, M(1,[||], 3))
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19758")>
        Public Async Function TestOnTuple() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Linq
Class C
    Sub M(arr as Integer())
        arr.Zip(arr, Function(p1, p2) ([||]p1, p2))
    End Sub
End Class",
"Imports System.Linq
Class C
    Sub M(arr as Integer())
        arr.Zip(arr, resultSelector:=Function(p1, p2) (p1, p2))
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23269")>
        Public Async Function TestCharacterEscape() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M([If] As Integer, [For] As Integer)
        M([If][||], [For])
    End Sub
End Class",
"Class C
    Sub M([If] As Integer, [For] As Integer)
        M([If]:=[If], [For]:=[For])
    End Sub
End Class", parameters:=s_vb15Parameters)
        End Function
    End Class
End Namespace

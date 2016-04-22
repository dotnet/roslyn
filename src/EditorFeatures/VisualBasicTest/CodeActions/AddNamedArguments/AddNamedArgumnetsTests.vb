' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.ReplaceMethodWithProperty
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.AddNamedArguments
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddNamedArguments
    Public Class AddNamedArgumentsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New VisualBasicAddNamedArgumentsCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestLiteralsOnly() As Task
            Await TestAsync(
NewLines("class C \n Sub M(arg1 As Integer, arg2 As Integer) \n [|M|](arg1,2) \n End Sub \n End Class"),
NewLines("class C \n Sub M(arg1 As Integer, arg2 As Integer) \n M(arg1,arg2:=2) \n End Sub \n End Class"),
index:=0)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestDelegate() As Task
            Await TestAsync(
"Class C
Sub M()
Dim f = Sub (arg)
End Sub
[|f|](1)
End Sub
End Class",
"Class C
Sub M()
Dim f = Sub (arg)
End Sub
f(arg:=1)
End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestConditionalDelegate() As Task
            Await TestAsync(
"Class C
Sub M()
Dim f = Sub (arg)
End Sub
[|f|]?(1)
End Sub
End Class",
"Class C
Sub M()
Dim f = Sub (arg)
End Sub
f?(arg:=1)
End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestConditionalMethod() As Task
            Await TestAsync(
NewLines("class C \n sub M(arg1 as Integer, arg2 as Integer) \n Me?.[|M|](1, 2) \n End sub \n End class"),
NewLines("class C \n sub M(arg1 as Integer, arg2 as Integer) \n Me?.M(arg1:=1,arg2:=2) \n End sub \n End class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestConditionalIndexer() As Task
            Await TestAsync(
"Class C
Sub M(arg1 as String)
Dim r = [|arg1?(0)|]
End Sub
End Class",
"Class C
Sub M(arg1 as String)
Dim r = arg1?(index:=0)
End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestConstructor() As Task
            Await TestAsync(
NewLines("class C \n Sub New(arg1 As Integer, arg2 As Integer) \n Dim c = [|New C(1, 2)|] \n End Sub \n End Class"),
NewLines("class C \n Sub New(arg1 As Integer, arg2 As Integer) \n Dim c = New C(arg1:=1,arg2:=2) \n End Sub \n End Class"),
index:=1)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMethod() As Task
            Await TestAsync(
NewLines("class C \n Sub M(arg1 As Integer, arg2 As Integer) \n [|M|](1, 2) \n End Sub \n End Class"),
NewLines("class C \n Sub M(arg1 As Integer, arg2 As Integer) \n M(arg1:=1,arg2:=2) \n End Sub \n End Class"),
index:=1)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestIndexer() As Task
            Await TestAsync(
"Class C
Function M(arg1 as String) As Char
Return [|arg1|](0)
End Function
End Class",
"Class C
Function M(arg1 as String) As Char
Return arg1(index:=0)
End Function
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnArrayIndexer() As Task
            Await TestMissingAsync(
"Class C
Function M(arg1 as Integer()) As Integer
Return [|arg1|](0)
End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnConditionalArrayIndexer() As Task
            Await TestMissingAsync(
"Class C
Function M(arg1 as Integer()) As Integer
Return [|arg1|]?(0)
End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnEmptyArgumentList() As Task
            Await TestMissingAsync(
NewLines("class C \n Sub M() \n [|M|]() \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnAlreadyFixed() As Task
            Await TestMissingAsync(
NewLines("class C \n Sub M(arg as Integer) \n [|M|](arg:=1) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnParamArray() As Task
            Await TestMissingAsync(
NewLines("class C \n Sub M(ParamArray arg1 As Integer()) \n [|M|](1) \n  End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestEmptyParamArray() As Task
            Await TestAsync(
NewLines("class C \n Sub M(arg1 As Integer, ParamArray arg2 As Integer()) \n [|M|](1) \n End Sub \n End Class"),
NewLines("class C \n Sub M(arg1 As Integer, ParamArray arg2 As Integer()) \n M(arg1:=1) \n End Sub \n End Class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestOmittedArguments() As Task
            Await TestAsync(
NewLines("class C \n Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1) \n [|M|](1,,3) \n End Sub \n End Class"),
NewLines("class C \n Sub M(arg1 As Integer, optional arg2 As Integer=1, optional arg3 as Integer=1) \n M(arg1:=1,arg3:=3) \n End Sub \n End Class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnAlreadyFixedWithOmittedArgument() As Task
            Await TestMissingAsync(
NewLines("class C \n Sub M(optional arg1 As Integer=1, optional arg2 As Integer=1) \n [|M|](,arg2:=2) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNamedArguments)>
        Public Async Function TestMissingOnNameOf() As Task
            Await TestMissingAsync(
"class C
Function M() As String
Return [|NameOf(M)|]
End Function
End Class")
        End Function
    End Class
End Namespace

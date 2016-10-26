' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.IntroduceVariable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.IntroduceVariable
    Public Class IntroduceVariableTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New IntroduceVariableCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function Test1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Console.WriteLine([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        Console.WriteLine(V)
    End Sub
End Module",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function Test2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Console.WriteLine([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        Console.WriteLine(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfExpression1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If foo([|1 + 1|]) Then bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If foo(V) Then bar(1 + 1)
    End Sub
End Module",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfExpression2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If foo([|1 + 1|]) Then bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If foo(V) Then bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfStatement1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If foo(1 + 1) Then bar([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If foo(1 + 1) Then
            Const {|Rename:V|} As Integer = 1 + 1
            bar(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfStatement2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If foo(1 + 1) Then bar([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If foo(V) Then bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoIntroduceFieldOnMethodTypeParameter() As Task
            Dim source = "Module Program
    Sub Main(Of T)()
        Foo([|CType(2.ToString(), T)|])
    End Sub
End Module"
            Await TestExactActionSetOfferedAsync(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.Introduce_local_for_0, "CType(2.ToString(), T)"),
                    String.Format(FeaturesResources.Introduce_local_for_all_occurrences_of_0, "CType(2.ToString(), T)")})

            ' Verifies "Introduce field ..." is missing
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoIntroduceFieldOnMethodParameter() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Foo([|x.ToString()|])
    End Sub
End Module"
            Await TestExactActionSetOfferedAsync(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.Introduce_local_for_0, "x.ToString()"),
                    String.Format(FeaturesResources.Introduce_local_for_all_occurrences_of_0, "x.ToString()")})

            ' Verifies "Introduce field ..." is missing
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoRefactoringOnExpressionInAssignmentStatement() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Dim r = [|x.ToString()|]
    End Sub
End Module"
            Await TestMissingAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalGeneratedInInnerBlock1() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        If True Then
            Foo([|x.ToString()|])
        End If
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        If True Then
            Dim {|Rename:v|} As String = x.ToString()
            Foo(v)
        End If
    End Sub
End Module"
            Await TestAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalGeneratedInInnerBlock2() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        If True Then
            Foo([|x.ToString()|])
        End If
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        If True Then
            Dim {|Rename:v|} As String = x.ToString()
            Foo(v)
        End If
    End Sub
End Module"
            Await TestAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromSingleExpressionInAnonType() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Dim f1 = New With {.SomeString = [|x.ToString()|]}
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        Dim {|Rename:v|} As String = x.ToString()
        Dim f1 = New With {.SomeString = v}
    End Sub
End Module"
            Await TestAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromMultipleExpressionsInAnonType() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Dim f1 = New With {.SomeString = [|x.ToString()|], .SomeOtherString = x.ToString()}
        Dim f2 = New With {.SomeString = x.ToString(), .SomeOtherString = x.ToString()}
        Dim str As String = x.ToString()
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        Dim {|Rename:v|} As String = x.ToString()
        Dim f1 = New With {.SomeString = v, .SomeOtherString = v}
        Dim f2 = New With {.SomeString = v, .SomeOtherString = v}
        Dim str As String = v
    End Sub
End Module"
            Await TestAsync(source, expected, index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromInferredFieldInitializer() As Task
            Dim source = "Imports System
Class C
    Sub M()
        Dim a As New With {[|Environment.TickCount|]}
    End Sub
End Class"
            Dim expected = "Imports System
Class C
    Sub M()
        Dim {|Rename:tickCount|} As Integer = Environment.TickCount
        Dim a As New With {tickCount}
    End Sub
End Class"
            Await TestAsync(source, expected, index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromYieldStatement() As Task
            Dim source = "Imports System
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield [|Environment.TickCount * 2|]
    End Function
End Class"
            Dim expected = "Imports System
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim {|Rename:v|} As Integer = Environment.TickCount * 2
        Yield v
    End Function
End Class"
            Await TestAsync(source, expected, index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromWhileStatement() As Task
            Dim source = "Class C
    Sub M()
        Dim x = 1
        While [|x = 1|]
        End While
    End Sub
End Class"
            Dim expected = "Class C
    Sub M()
        Dim x = 1
        Dim {|Rename:v|} As Boolean = x = 1
        While v
        End While
    End Sub
End Class"
            Await TestAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromSingleExpressionInObjectInitializer() As Task
            Dim source = "Module Program
    Structure FooStruct
        Dim FooMember1 As String
    End Structure
    Sub Main(x As Integer)
        Dim f1 = New FooStruct With {.FooMember1 = [|""t"" + ""test""|]}
    End Sub
End Module"
            Dim expected = "Module Program
    Structure FooStruct
        Dim FooMember1 As String
    End Structure
    Sub Main(x As Integer)
        Const {|Rename:V|} As String = ""t"" + ""test"" 
 Dim f1 = New FooStruct With {.FooMember1 = V}
    End Sub
End Module"
            Await TestAsync(source, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromMultipleExpressionsInObjectInitializer() As Task
            Dim code =
<File>
Module Program
    Structure FooStruct
        Dim FooMember1 As String
        Dim FooMember2 As String
    End Structure
    Sub Main(x As Integer)
        Dim f1 = New FooStruct With {.FooMember1 = [|"t" + "test"|], .FooMember2 = "t" + "test"}
        Dim f2 = New FooStruct With {.FooMember1 = "t" + "test", .FooMember2 = "t" + "test"}
        Dim str As String = "t" + "test"
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Structure FooStruct
        Dim FooMember1 As String
        Dim FooMember2 As String
    End Structure
    Sub Main(x As Integer)
        Const {|Rename:V|} As String = "t" + "test"
        Dim f1 = New FooStruct With {.FooMember1 = V, .FooMember2 = V}
        Dim f2 = New FooStruct With {.FooMember1 = V, .FooMember2 = V}
        Dim str As String = V
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=3, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFieldFromMultipleExpressionsInAnonType() As Task
            Dim source = "Class Program
    Dim q = New With {.str = [|""t"" + ""test""|]}
    Dim r = New With {.str = ""t"" + ""test""}
    Sub Foo()
        Dim x = ""t"" + ""test"" 
 End Sub
End Class"
            Dim expected = "Class Program
    Private Const {|Rename:V|} As String = ""t"" + ""test"" 
 Dim q = New With {.str = V}
    Dim r = New With {.str = V}
    Sub Foo()
        Dim x = V
    End Sub
End Class"
            Await TestAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestPrivateFieldFromExpressionInField() As Task
            Dim source = "Class Program
    Dim x = Foo([|2 + 2|])
End Class"
            Dim expected = "Class Program
    Private Const {|Rename:V|} As Integer = 2 + 2
    Dim x = Foo(V)
End Class"
            Await TestAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoLocalFromExpressionInField() As Task
            Dim source = "Class Program
    Dim x = Foo([|2 + 2|])
End Class"
            Await TestExactActionSetOfferedAsync(source, {String.Format(FeaturesResources.Introduce_constant_for_0, "2 + 2"), String.Format(FeaturesResources.Introduce_constant_for_all_occurrences_of_0, "2 + 2")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSharedModifierAbsentInGeneratedModuleFields() As Task
            Dim source = "Module Program
    Dim x = Foo([|2 + y|])
End Module"
            Dim expected = "Module Program
    Private ReadOnly {|Rename:p|} As Object = 2 + y
    Dim x = Foo(p)
End Module"
            Await TestAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLocalInsertLocation() As Task
            Dim source = "Class Program
    Sub Method1()
        Dim v1 As String = ""TEST"" 
 Dim v2 As Integer = 2 + 2
        Foo([|2 + 2|])
    End Sub
End Class"
            Dim expected = "Class Program
    Sub Method1()
        Dim v1 As String = ""TEST"" 
 Dim v2 As Integer = 2 + 2
        Const {|Rename:V|} As Integer = 2 + 2
        Foo(V)
    End Sub
End Class"
            Await TestAsync(source, expected, index:=2)
        End Function

#Region "Parameter context"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestConstantFieldGenerationForParameterSingleOccurrence() As Task
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = "Module Module1
    Sub Foo(Optional x As Integer = [|42|])
    End Sub
End Module"
            Dim expected = "Module Module1
    Private Const {|Rename:V|} As Integer = 42
    Sub Foo(Optional x As Integer = V)
    End Sub
End Module"
            Await TestAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestConstantFieldGenerationForParameterAllOccurrences() As Task
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = "Module Module1
    Sub Bar(Optional x As Integer = 42)
    End Sub
    Sub Foo(Optional x As Integer = [|42|])
    End Sub
End Module"
            Dim expected = "Module Module1
    Private Const {|Rename:V|} As Integer = 42
    Sub Bar(Optional x As Integer = V)
    End Sub
    Sub Foo(Optional x As Integer = V)
    End Sub
End Module"
            Await TestAsync(source, expected, index:=1)
        End Function

#End Region

        <WorkItem(540269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540269")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceDottedExpression() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Console.WriteLine([|Foo.someVariable|])
        Console.WriteLine(Foo.someVariable)
    End Sub
End Module
Friend Class Foo
    Shared Public someVariable As Integer
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim {|Rename:someVariable|} As Integer = Foo.someVariable
        Console.WriteLine(someVariable)
        Console.WriteLine(someVariable)
    End Sub
End Module
Friend Class Foo
    Shared Public someVariable As Integer
End Class",
index:=1)
        End Function

        <WorkItem(540457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo([|2 + 2|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Const {|Rename:V|} As Integer = 2 + 2
            Foo(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <WorkItem(540457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo([|1 + 1|]) Else Bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Const {|Rename:V|} As Integer = 1 + 1
            Foo(V)
        Else
            Bar(1 + 1)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <WorkItem(540457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine3() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo([|1 + 1|]) Else Bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If True Then Foo(V) Else Bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <WorkItem(540457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine4() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo(1 + 1) Else Bar([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Foo(1 + 1)
        Else
            Const {|Rename:V|} As Integer = 1 + 1
            Bar(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <WorkItem(540468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCantExtractMethodTypeParameterToFieldCount() As Task
            Await TestActionCountAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(Of T)(x As Integer)
        Foo([|CType(2.ToString(), T)|])
    End Sub
End Module",
count:=2)
        End Function

        <WorkItem(540468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCantExtractMethodTypeParameterToField() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(Of T)(x As Integer)
        Foo([|CType(2.ToString(), T)|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(Of T)(x As Integer)
        Dim {|Rename:t1|} As T = CType(2.ToString(), T)
        Foo(t1)
    End Sub
End Module")
        End Function

        <WorkItem(540489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540489")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestOnlyFieldsInsideConstructorInitializer() As Task
            Await TestActionCountAsync(
"Class Foo
    Sub New()
        Me.New([|2 + 2|])
    End Sub
    Sub New(v As Integer)
    End Sub
End Class",
count:=2)

            Await TestAsync(
"Class Foo
    Sub New()
        Me.New([|2 + 2|])
    End Sub
    Sub New(v As Integer)
    End Sub
End Class",
"Class Foo
    Private Const {|Rename:V|} As Integer = 2 + 2
    Sub New()
        Me.New(V)
    End Sub
    Sub New(v As Integer)
    End Sub
End Class",
index:=0)
        End Function

        <WorkItem(540485, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540485")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceLocalForConstantExpression() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        Dim s As String() = New String([|10|]) {}
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 10
        Dim s As String() = New String(V) {}
    End Sub
End Module",
index:=3)
        End Function

        <WorkItem(1065689, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065689")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceLocalForConstantExpressionWithTrailingTrivia() As Task
            Await TestAsync(
<File>
Class C
    Private Function GetX() As Object
        Return [|"c d
" + ' comment 1
"a
b" ' comment 2|]
    End Function
End Class
</File>,
<File>
Class C
    Private Function GetX() As Object
        Const {|Rename:V|} As String = "c d
" + ' comment 1
"a
b"
        Return V ' comment 2
    End Function
End Class
</File>,
index:=3,
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceFieldWithTrailingTrivia() As Task
            Await TestAsync(
<File>
Class C
    Private Sub S()
        Dim x = 1 + [|2|] ' comment
    End Sub
End Class
</File>,
<File>
Class C
    Private Const {|Rename:V|} As Integer = 2

    Private Sub S()
        Dim x = 1 + V ' comment
    End Sub
End Class
</File>,
index:=1,
compareTokens:=False)
        End Function

        <WorkItem(540487, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540487")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFormattingForPartialExpression() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim i = [|1 + 2|] + 3
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Const {|Rename:V|} As Integer = 1 + 2
        Dim i = V + 3
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=2, compareTokens:=False)
        End Function

        <WorkItem(540491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540491")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInAttribute1() As Task
            Await TestAsync(
"<Attr([|2 + 2|])>
Class Foo
End Class
Friend Class AttrAttribute
    Inherits Attribute
End Class",
"<Attr(Foo.V)>
Class Foo
    Friend Const {|Rename:V|} As Integer = 2 + 2
End Class
Friend Class AttrAttribute
    Inherits Attribute
End Class",
index:=0)
        End Function

        <WorkItem(540490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540490")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInMyClassNew() As Task
            Await TestAsync(
"Class Foo
    Sub New()
        MyClass.New([|42|])
    End Sub
    Sub New(x As Integer)
    End Sub
End Class",
"Class Foo
    Private Const {|Rename:V|} As Integer = 42
    Sub New()
        MyClass.New(V)
    End Sub
    Sub New(x As Integer)
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo([|2 + 2|]) Else Bar(2 + 2)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Const {|Rename:V|} As Integer = 2 + 2
            Foo(V)
        Else
            Bar(2 + 2)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo([|2 + 2|]) Else Bar(2 + 2)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 2 + 2
        If True Then Foo(V) Else Bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf3() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo(2 + 2) Else Bar([|2 + 2|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Foo(2 + 2)
        Else
            Const {|Rename:V|} As Integer = 2 + 2
            Bar(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf4() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Foo(2 + 2) Else Bar([|2 + 2|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 2 + 2
        If True Then Foo(V) Else Bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <WorkItem(541604, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541604")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestAttribute() As Task
            Await TestAsync(
"<Attr([|2 + 2|])>
Class Foo
End Class
Friend Class AttrAttribute
    Inherits System.Attribute
End Class",
"<Attr(Foo.V)>
Class Foo
    Friend Const {|Rename:V|} As Integer = 2 + 2
End Class
Friend Class AttrAttribute
    Inherits System.Attribute
End Class",
index:=0)
        End Function

        <WorkItem(542092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542092")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestRangeArgumentLowerBound1() As Task
            Await TestMissingAsync("Module M
    Sub Main()
        Dim x() As Integer
        ReDim x([|0|] To 5)
    End Sub
End Module")
        End Function

        <WorkItem(542092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542092")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestRangeArgumentLowerBound2() As Task
            Dim code =
<File>
Module M
    Sub Main()
        Dim x() As Integer
        ReDim x(0 To 5)
        Dim a = [|0|] + 1
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Sub Main()
        Dim x() As Integer
        ReDim x(0 To 5)
        Const {|Rename:V|} As Integer = 0
        Dim a = V + 1
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=3, compareTokens:=False)
        End Function

        <WorkItem(543029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543029"), WorkItem(542963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542963"), WorkItem(542295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542295")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestUntypedExpression() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim q As Object
        If True Then q = [|Sub()
                         End Sub|]
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim q As Object
        If True Then
            Dim {|Rename:p|} As Object = Sub()
                              End Sub
            q = p
        End If
    End Sub
End Module")
        End Function

        <WorkItem(542374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542374")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFieldConstantInAttribute1() As Task
            Await TestAsync(
"<Foo(2 + 3 + 4)>
Module Program
    Dim x = [|2 + 3|] + 4
End Module
Friend Class FooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
"<Foo(2 + 3 + 4)>
Module Program
    Private Const {|Rename:V|} As Integer = 2 + 3
    Dim x = V + 4
End Module
Friend Class FooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
index:=0)
        End Function

        <WorkItem(542374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542374")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFieldConstantInAttribute2() As Task
            Await TestAsync(
"<Foo(2 + 3 + 4)>
Module Program
    Dim x = [|2 + 3|] + 4
End Module
Friend Class FooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
"<Foo(V + 4)>
Module Program
    Friend Const {|Rename:V|} As Integer = 2 + 3
    Dim x = V + 4
End Module
Friend Class FooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
index:=1,
parseOptions:=Nothing)
        End Function

        <WorkItem(542783, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542783")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnAttributeName() As Task
            Await TestMissingAsync(
"<[|Obsolete|]>
Class C
End Class")
        End Function

        <WorkItem(542811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542811")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnFilterClause() As Task
            Await TestMissingAsync(
"Module Program
    Sub Main()
        Try
        Catch ex As Exception When [|+|] 
 End Try
    End Sub
End Module")
        End Function

        <WorkItem(542906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542906")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoIntroduceLocalInAttribute() As Task
            Dim input =
"Module Program \n <Obsolete([|""""|])> \n Sub Main(args As String()) \n End Sub \n End Module"

            Await TestActionCountAsync(
NewLines(input),
count:=2)

            Await TestAsync(
NewLines(input),
"Module Program
    Private Const {|Rename:V|} As String = """"
    <Obsolete(V)>
    Sub Main(args As String())
    End Sub
End Module",
index:=0)
        End Function

        <WorkItem(542947, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542947")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotOnMyBase() As Task
            Await TestMissingAsync(
"Class c1
    Public res As String
    Sub Foo()
        res = ""1"" 
 End Sub
End Class
Class c2
    Inherits c1
    Sub scen1()
        [|MyBase|].Foo()
    End Sub
End Class")
        End Function

        <WorkItem(541966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541966")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNestedMultiLineIf1() As Task
            Dim code =
<File>
Imports System

Module Program
    Sub Main()
        If True Then If True Then Console.WriteLine([|1|]) Else Console.WriteLine(2) Else Console.WriteLine(3)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System

Module Program
    Sub Main()
        If True Then

            If True Then
                Const {|Rename:V|} As Integer = 1
                Console.WriteLine(V)
            Else
                Console.WriteLine(2)
            End If
        Else
            Console.WriteLine(3)
        End If
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=3, compareTokens:=False)
        End Function

        <WorkItem(541966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541966")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNestedMultiLineIf2() As Task
            Dim code =
<File>
Imports System

Module Program
    Sub Main()
        If True Then If True Then Console.WriteLine(1) Else Console.WriteLine([|2|]) Else Console.WriteLine(3)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System

Module Program
    Sub Main()
        If True Then

            If True Then
                Console.WriteLine(1)
            Else
                Const {|Rename:V|} As Integer = 2
                Console.WriteLine(V)
            End If
        Else
            Console.WriteLine(3)
        End If
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=3, compareTokens:=False)
        End Function

        <WorkItem(541966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541966")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNestedMultiLineIf3() As Task
            Dim code =
<File>
Imports System

Module Program
    Sub Main()
        If True Then If True Then Console.WriteLine(1) Else Console.WriteLine(2) Else Console.WriteLine([|3|])
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System

Module Program
    Sub Main()
        If True Then
            If True Then Console.WriteLine(1) Else Console.WriteLine(2)
        Else
            Const {|Rename:V|} As Integer = 3
            Console.WriteLine(V)
        End If
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=3, compareTokens:=False)
        End Function

        <WorkItem(543273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda1() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer) Console.WriteLine([|x + 1|]) ' Introduce local 
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer)
                    Dim {|Rename:v|} As Integer = x + 1
                    Console.WriteLine(v) ' Introduce local 
                End Sub
    End Sub
End Module",
index:=0)
        End Function

        <WorkItem(543273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda2() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer) If True Then Console.WriteLine([|x + 1|]) Else Console.WriteLine()
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer)
                    If True Then
                        Dim {|Rename:v|} As Integer = x + 1
                        Console.WriteLine(v)
                    Else
                        Console.WriteLine()
                    End If
                End Sub
    End Sub
End Module",
index:=0)
        End Function

        <WorkItem(543273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda3() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer) If True Then Console.WriteLine() Else Console.WriteLine([|x + 1|])
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer)
                    If True Then
                        Console.WriteLine()
                    Else
                        Dim {|Rename:v|} As Integer = x + 1
                        Console.WriteLine(v)
                    End If
                End Sub
    End Sub
End Module",
index:=0)
        End Function

        <WorkItem(543273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda4() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer) If True Then Console.WriteLine([|x + 1|]) Else Console.WriteLine(x + 1)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer)
                    Dim {|Rename:v|} As Integer = x + 1
                    If True Then Console.WriteLine(v) Else Console.WriteLine(v)
                End Sub
    End Sub
End Module",
index:=1)
        End Function

        <WorkItem(543299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543299")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda5() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        Dim query = Sub(a) a = New With {Key .Key = Function(ByVal arg As Integer) As Integer
                                                        Return arg
                                                    End Function}.Key.Invoke([|a Or a|])
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim query = Sub(a)
                        Dim {|Rename:v|} As Object = a Or a
                        a = New With {Key .Key = Function(ByVal arg As Integer) As Integer
                                                     Return arg
                                                 End Function}.Key.Invoke(v)
                    End Sub
    End Sub
End Module",
index:=0)
        End Function

        <WorkItem(542762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542762")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotInIntoClause() As Task
            Await TestMissingAsync(
"Imports System.Linq
Module 
 Sub Main()
        Dim x = Aggregate y In New Integer() {1}
        Into [|Count()|]
    End Sub
End Module")
        End Function

        <WorkItem(543289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543289")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotOnAttribute1() As Task
            Await TestMissingAsync(
"Option Explicit Off
Module Program
    <Runtime.CompilerServices.[|Extension|]()> _
    Function Extension(ByVal x As Integer) As Integer
        Return x
    End Function
End Module")
        End Function

        <WorkItem(543289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543289")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotOnAttribute2() As Task
            Await TestMissingAsync(
"Option Explicit Off
Module Program
    <Runtime.CompilerServices.[|Extension()|]> _
    Function Extension(ByVal x As Integer) As Integer
        Return x
    End Function
End Module")
        End Function

        <WorkItem(543461, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543461")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCollectionInitializer() As Task
            Await TestMissingAsync(
"Module Program
    Sub Main(args As String())
        Dim i1 = New Integer() [|{4, 5}|]
    End Sub
End Module")
        End Function

        <WorkItem(543573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543573")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCaseInsensitiveNameConflict() As Task
            Await TestAsync(
"Class M
    Public Function Foo()
        Return [|Me.Foo|] * 0
    End Function
End Class",
"Class M
    Public Function Foo()
        Dim {|Rename:foo1|} As Object = Me.Foo
        Return foo1 * 0
    End Function
End Class")
        End Function

        <WorkItem(543590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery1() As Task
            Await TestAsync(
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Select [|Sample(s)|]
        Return 0
    End Function
End Class",
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s)
                      Select v
        Return 0
    End Function
End Class")
        End Function

        <WorkItem(543590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQueryCount1() As Task
            Await TestActionCountAsync(
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Select [|Sample(s)|]
        Return 0
    End Function
End Class",
count:=2)
        End Function

        <WorkItem(543590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery2() As Task
            Await TestAsync(
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Where [|Sample(s)|] > 21
                      Select Sample(s)
        Return 0
    End Function
End Class",
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s)
                      Where v > 21
                      Select Sample(s)
        Return 0
    End Function
End Class")
        End Function

        <WorkItem(543590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery3() As Task
            Await TestAsync(
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Where [|Sample(s)|] > 21
                      Select Sample(s)
        Return 0
    End Function
End Class",
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s)
                      Where v > 21
                      Select v
        Return 0
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(543590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery4() As Task
            Await TestAsync(
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Where Sample(s) > 21
                      Select [|Sample(s)|]
        Return 0
    End Function
End Class",
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Where Sample(s) > 21 Let {|Rename:v|} = Sample(s)
                      Select v
        Return 0
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery5() As Task
            Await TestAsync(
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1}
                      Where Sample(s) > 21
                      Select [|Sample(s)|]
        Return 0
    End Function
End Class",
"Imports System.Linq
Public Class Base
    Public Function Sample(ByVal arg As Integer) As Integer
        Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s)
                      Where v > 21
                      Select v
        Return 0
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(543529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543529")>
        <WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInStatementlessConstructorParameter() As Task
            Await TestMissingAsync("Class C1
    Sub New(Optional ByRef x As String = [|Nothing|])
    End Sub
End Class")
        End Function

        <WorkItem(543650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543650")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReferenceToAnonymousTypeProperty() As Task
            Await TestMissingAsync(
"Class AM
    Sub M(args As String())
        Dim var1 As New AM
        Dim at1 As New With {var1, .friend = [|.var1|]}
    End Sub
End Class")
        End Function

        <WorkItem(543698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543698")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntegerArrayExpression() As Task
            Await TestAsync(
"Module Program
    Sub Main()
        Return [|New Integer() {}|]
    End Sub
End Module",
"Module Program
    Sub Main()
        Dim {|Rename:v|} As Integer() = New Integer() {}
        Return v
    End Sub
End Module")
        End Function

        <WorkItem(544273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544273")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestAttributeNamedParameter() As Task
            Await TestMissingAsync(
"Class TestAttribute
    Inherits Attribute
    Public Sub New(Optional a As Integer = 42)
    End Sub
End Class
<Test([|a|]:=5)>
Class Foo
End Class")
        End Function

        <WorkItem(544265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544265")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnWrittenToExpression() As Task
            Await TestMissingAsync(
"Module Program
    Sub Main()
        Dim x = New Integer() {1, 2}
        [|x(1)|] = 2
    End Sub
End Module")
        End Function

        <WorkItem(543824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess1() As Task
            Await TestMissingAsync(
"Imports System
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
    Public Property PropInt As Integer
End Class
Public Class C2
    Public Shared Sub Main()
        Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt|].ToString()}
    End Sub
End Class")
        End Function

        <WorkItem(543824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess2() As Task
            Await TestMissingAsync(
"Imports System
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
    Public Property PropInt As Integer
End Class
Public Class C2
    Public Shared Sub Main()
        Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt.ToString|]()}
    End Sub
End Class")
        End Function

        <WorkItem(543824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess3() As Task
            Await TestMissingAsync(
"Imports System
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
    Public Property PropInt As Integer
End Class
Public Class C2
    Public Shared Sub Main()
        Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt.ToString()|]}
    End Sub
End Class")
        End Function

        <WorkItem(543824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess4() As Task
            Dim code =
<File>
Imports System
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
    Public Property PropInt As Integer
End Class
Public Class C2
    Public Shared Sub Main()
        Dim x = 1 + [|New C1() With {.FieldStr = .FieldInt.ToString()}|]
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
    Public Property PropInt As Integer
End Class
Public Class C2
    Public Shared Sub Main()
        Dim {|Rename:c11|} As C1 = New C1() With {.FieldStr = .FieldInt.ToString()}
        Dim x = 1 + c11
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529510")>
        <WpfFact(Skip:="529510"), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoRefactoringOnAddressOfExpression() As Task
            Dim source = "Imports System
Module Module1
    Public Sub Foo(ByVal a1 As Exception)
    End Sub
    Public Sub foo(ByVal a1 As Action(Of ArgumentException))
    End Sub
    Sub Main()
        Foo(New Action(Of Exception)([|AddressOf Foo|]))
    End Sub
End Module"
            Await TestMissingAsync(source)
        End Function

        <WorkItem(529510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529510")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
        Public Async Function TestMissingOnAddressOfInDelegate() As Task
            Await TestMissingAsync(
"Module Module1
    Public Sub Foo(ByVal a1 As Exception)
    End Sub
    Public Sub foo(ByVal a1 As Action(Of ArgumentException))
    End Sub
    Sub Main()
        foo(New Action(Of Exception)([|AddressOf Foo|]))
    End Sub
End Module")
        End Function

        <WorkItem(545168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545168")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
        Public Async Function TestMissingOnXmlName() As Task
            Await TestMissingAsync(
"Module M
    Sub Main()
        Dim x = <[|x|]/>
    End Sub
End Module")
        End Function

        <WorkItem(545262, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545262")>
        <WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInTernaryConditional() As Task
            Await TestMissingAsync("Module Program
    Sub Main(args As String())
        Dim p As Object = Nothing
        Dim Obj1 = If(New With {.a = True}.a, p, [|Nothing|])
    End Sub
End Module")
        End Function

        <WorkItem(545316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545316")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInPropertyInitializer() As Task
            Await TestAsync(
"Module Module1
    Property Prop As New List(Of String) From {[|""One""|], ""two""}
End Module",
"Module Module1
    Private Const {|Rename:V|} As String = ""One"" 
 Property Prop As New List(Of String) From {V, ""two""}
End Module")
        End Function

        <WorkItem(545308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545308")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestDoNotMergeAmpersand() As Task
            Dim code =
<File>
Module Module1
    Public Sub foo(Optional ByVal arg = ([|"a"|]) &amp; "b")
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Private Const {|Rename:V|} As String = "a"

    Public Sub foo(Optional ByVal arg = V &amp; "b")
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545258")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestVenusGeneration1() As Task
            Dim code =
<File>
Class C
    Sub Foo()
#ExternalSource ("Foo", 1)
        Console.WriteLine([|5|])
#End ExternalSource
   End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub Foo()
#ExternalSource ("Foo", 1)
        Const {|Rename:V|} As Integer = 5
        Console.WriteLine(V)
#End ExternalSource
    End Sub
End Class
</File>

            Await TestExactActionSetOfferedAsync(code.NormalizedValue,
                                      {String.Format(FeaturesResources.Introduce_local_constant_for_0, "5"),
                                       String.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5")})

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545258")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestVenusGeneration2() As Task
            Dim code =
<Text>
Class C
#ExternalSource ("Foo", 1)
    Sub Foo()
        If False Then
            Console.WriteLine([|5|])
        End If
    End Sub
#End ExternalSource
End Class
</Text>

            Await TestExactActionSetOfferedAsync(code.NormalizedValue,
                                      {String.Format(FeaturesResources.Introduce_local_constant_for_0, "5"),
                                       String.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5")})
        End Function

        <WorkItem(545258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545258")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestVenusGeneration3() As Task
            Dim code =
<File>
Class C
    Sub Foo()
#ExternalSource ("Foo", 1)
        If False Then
            Console.WriteLine([|5|])
        End If
#End ExternalSource
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub Foo()
#ExternalSource ("Foo", 1)
        If False Then
            Const {|Rename:V|} As Integer = 5
            Console.WriteLine(V)
        End If
#End ExternalSource
    End Sub
End Class
</File>

            Await TestExactActionSetOfferedAsync(code.NormalizedValue,
                                      {String.Format(FeaturesResources.Introduce_local_constant_for_0, "5"),
                                       String.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5")})

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545525, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInvocation() As Task
            Await TestAsync(
"Option Strict On

Class C
    Shared Sub Main()
        Dim x = [|New C().Foo()|](0)
    End Sub
    Function Foo() As Integer()
    End Function
End Class",
"Option Strict On

Class C
    Shared Sub Main()
        Dim {|Rename:v|} As Integer() = New C().Foo()
        Dim x = v(0)
    End Sub
    Function Foo() As Integer()
    End Function
End Class")
        End Function

        <WorkItem(545829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545829")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestOnImplicitMemberAccess() As Task
            Await TestAsync(
"Module Program
    Sub Main()
        With """"
            Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module",
"Module Program
    Sub Main()
        With """"
            Dim {|Rename:getHashCode|} As Integer = .GetHashCode
            Dim x = getHashCode Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module",
parseOptions:=Nothing)

            Await TestAsync(
"Module Program
    Sub Main()
        With """"
            Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module",
"Module Program
    Sub Main()
        With """"
            Dim {|Rename:getHashCode1|} As Integer = .GetHashCode
            Dim x = getHashCode1 Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module",
parseOptions:=GetScriptOptions())
        End Function

        <WorkItem(545702, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545702")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingInRefLocation() As Task
            Dim markup =
<File>
Module A
    Sub Main()
        Foo([|1|])
    End Sub
    Sub Foo(ByRef x As Long)
    End Sub
    Sub Foo(x As String)
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(546139, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546139")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestAcrossPartialTypes() As Task
            Await TestAsync(
"Partial Class C
    Sub foo1(Optional x As String = [|""HELLO""|])
    End Sub
End Class
Partial Class C
    Sub foo3(Optional x As String = ""HELLO"")
    End Sub
End Class",
"Partial Class C
    Private Const {|Rename:V|} As String = ""HELLO"" 
 Sub foo1(Optional x As String = V)
    End Sub
End Class
Partial Class C
    Sub foo3(Optional x As String = V)
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(544669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544669")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFunctionBody1() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        Dim a1 = Function(ByVal x) [|x!foo|]
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim a1 = Function(ByVal x)
                     Dim {|Rename:foo|} As Object = x!foo
                     Return foo
                 End Function
    End Sub
End Module")
        End Function

        <WorkItem(1065689, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065689")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestTrailingTrivia() As Task
            Dim code =
<File>
Module M
    Sub Main()
        Dim a = 1 +
        [|2|] ' comment

        End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Private Const {|Rename:V|} As Integer = 2

    Sub Main()
        Dim a = 1 +
        V ' comment

    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(546815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546815")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInIfStatement() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If [|True|] Then
        End If
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Private Const {|Rename:V|} As Boolean = True
    Sub Main(args As String())
        If V Then
        End If
    End Sub
End Module")
        End Function

        <WorkItem(830928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830928")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceLocalRemovesUnnecessaryCast() As Task
            Await TestAsync(
"Imports System.Collections.Generic
Class C
    Private Shared Sub Main(args As String())
        Dim hSet = New HashSet(Of String)()
        hSet.Add([|hSet.ToString()|])
    End Sub
End Class",
"Imports System.Collections.Generic
Class C
    Private Shared Sub Main(args As String())
        Dim hSet = New HashSet(Of String)()
        Dim {|Rename:v|} As String = hSet.ToString()
        hSet.Add(v)
    End Sub
End Class")
        End Function

        <WorkItem(546691, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546691")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroLocalInSingleLineLambda() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x = Function() [|Sub()
                           End Sub|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim {|Rename:p|} = Sub()
                End Sub
        Dim x = Function() p
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(530720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530720")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultilineLambdaLineBreaks() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim a = Function(c) [|c!foo|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim a = Function(c)
                    Dim {|Rename:foo|} As Object = c!foo
                    Return foo
                End Function
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(531478, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531478")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestEscapeKeywordsIfNeeded1() As Task
            Dim code =
<File>
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Take([|From x In ""|])
    End Sub
    Sub Take(x)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Dim {|Rename:enumerable1|} As IEnumerable(Of Char) = From x In ""
        [Take](enumerable1)
    End Sub
    Sub Take(x)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(632327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632327")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInsertAfterPreprocessor1() As Task
            Dim code =
<File>
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource ("Home\Index.vbhtml", 1)
        Dim i = [|1 + 2|] + 3
        If True Then
            Dim j = 1 + 2 + 3
        End If
#End ExternalSource
    End Sub
End Class
</File>

            Dim expected =
<File>
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource ("Home\Index.vbhtml", 1)
        Const {|Rename:V|} As Integer = 1 + 2
        Dim i = V + 3
        If True Then
            Dim j = 1 + 2 + 3
        End If
#End ExternalSource
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(632327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632327")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInsertAfterPreprocessor2() As Task
            Dim code =
<File>
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource ("Home\Index.vbhtml", 1)
        Dim i = 1 + 2 + 3
        If True Then
            Dim j = [|1 + 2|] + 3
        End If
#End ExternalSource
    End Sub
End Class
</File>

            Dim expected =
<File>
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource ("Home\Index.vbhtml", 1)
        Dim i = 1 + 2 + 3
        If True Then
            Const {|Rename:V|} As Integer = 1 + 2
            Dim j = V + 3
        End If
#End ExternalSource
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(682683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682683")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestDontRemoveParenthesesIfOperatorPrecedenceWouldBeBroken() As Task
            Dim code =
<File>
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 - ([|1|] + 2))
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
 
Module Program
    Sub Main()
        Const {|Rename:V|} As Integer = 1
        Console.WriteLine(5 - (V + 2))
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=2, compareTokens:=False)
        End Function

        <WorkItem(1022458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022458")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestDontSimplifyParentUnlessEntireInnerNodeIsSelected() As Task
            Dim code =
<File>
Imports System
 
Module Program
    Sub Main()
        Dim s = "Text"
        Dim x = 42
        If ([|s.Length|].CompareTo(x) > 0 AndAlso
            s.Length.CompareTo(x) > 0) Then
        End If
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
 
Module Program
    Sub Main()
        Dim s = "Text"
        Dim x = 42
        Dim {|Rename:length|} As Integer = s.Length
        If (length.CompareTo(x) > 0 AndAlso
            length.CompareTo(x) > 0) Then
        End If
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=1, compareTokens:=False)
        End Function

        <WorkItem(939259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939259")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceLocalWithTriviaInMultiLineStatements() As Task
            Dim code =
<File>
Imports System
 
Module Program
    Sub Main()
        Dim x = If(True,
                   [|1|], ' TODO: Comment
                   2)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
 
Module Program
    Sub Main()
        Const {|Rename:V|} As Integer = 1
        Dim x = If(True,
                   V, ' TODO: Comment
                   2)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=3, compareTokens:=False)
        End Function

        <WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnNothingLiteral() As Task
            Await TestMissingAsync(
<File>
Imports System
Module Program
    Sub Main(args As String())
        Main([|Nothing|])
        M(Nothing)
    End Sub

    Sub M(i As Integer)
    End Sub
End Module
</File>)
        End Function

        <WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInParentConditionalAccessExpressions() As Task
            Dim code =
<File>
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = [|F(New C)|]?.F(New C)?.F(New C)
        Return x
    End Function
End Class
</File>
            Dim expected =
<File>
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim {|Rename:c1|} As C = F(New C)
        Dim y = c1?.F(New C)?.F(New C)
        Return x
    End Function
End Class
</File>
            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

        <WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        <WorkItem(3110, "https://github.com/dotnet/roslyn/issues/3110")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingAcrossMultipleParentConditionalAccessExpressions() As Task
            Await TestMissingAsync(
<File>
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = [|F(New C)?.F(New C)|]?.F(New C)
        Return x
    End Function
End Class
</File>)
        End Function

        <WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnInvocationExpressionInParentConditionalAccessExpressions() As Task
            Await TestMissingAsync(
<File>
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = F(New C)?.[|F(New C)|]?.F(New C)
        Return x
    End Function
End Class
</File>)
        End Function

        <WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnMemberBindingExpressionInParentConditionalAccessExpressions() As Task
            Await TestMissingAsync(
<File>
Imports System
Class C
    Sub F()
        Dim s as String = "Text"
        Dim l = s?.[|Length|]
    End Sub
End Class
</File>)
        End Function

        <WorkItem(2026, "https://github.com/dotnet/roslyn/issues/2026")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceAllFromInsideIfBlock() As Task
            Dim code =
<File>
Imports System
Module DataTipInfoGetterModule
    Friend Function GetInfoAsync() As DebugDataTipInfo
        Dim expression As ExpressionSyntax = Nothing

        Dim curr = DirectCast(expression.Parent, ExpressionSyntax)
        If curr Is expression.Parent Then
            Return New DebugDataTipInfo([|expression.Parent|].Span)
        End If

        Return Nothing
    End Function
End Module

Friend Class TextSpan
End Class

Friend Class ExpressionSyntax
    Public Property Parent As ExpressionSyntax
    Public Property Span As TextSpan
End Class

Friend Class DebugDataTipInfo
    Public Sub New(span As Object)
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Module DataTipInfoGetterModule
    Friend Function GetInfoAsync() As DebugDataTipInfo
        Dim expression As ExpressionSyntax = Nothing
        Dim {|Rename:parent|} As ExpressionSyntax = expression.Parent

        Dim curr = DirectCast(parent, ExpressionSyntax)
        If curr Is parent Then
            Return New DebugDataTipInfo(parent.Span)
        End If

        Return Nothing
    End Function
End Module

Friend Class TextSpan
End Class

Friend Class ExpressionSyntax
    Public Property Parent As ExpressionSyntax
    Public Property Span As TextSpan
End Class

Friend Class DebugDataTipInfo
    Public Sub New(span As Object)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, index:=1, compareTokens:=False)
        End Function

        <WorkItem(1065661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065661")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceVariableTextDoesntSpanLines() As Task
            Dim code = "
Class C
    Sub M()
        Dim s = """" + [|""a

b
c""|]
    End Sub
End Class"
            Await TestSmartTagTextAsync(code, String.Format(FeaturesResources.Introduce_local_constant_for_0, """a b c"""), index:=2)
        End Function

        <WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoConstantForInterpolatedStrings1() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim args As String() = Nothing
        Console.WriteLine([|$"{DateTime.Now.ToString()}Text{args(0)}"|])
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim args As String() = Nothing
        Dim {|Rename:v|} As String = $"{DateTime.Now.ToString()}Text{args(0)}"
        Console.WriteLine(v)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoConstantForInterpolatedStrings2() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Console.WriteLine([|$"Text{{s}}"|])
        Console.WriteLine($"Text{{s}}")
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim {|Rename:v|} As String = $"Text{{s}}"
        Console.WriteLine(v)
        Console.WriteLine(v)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, index:=1, compareTokens:=False)
        End Function

        <WorkItem(3147, "https://github.com/dotnet/roslyn/issues/3147")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestHandleFormattableStringTargetTyping1() As Task
            Const code = "
Imports System

" & FormattableStringType & "

Namespace N
    Class C
        Public Sub M()
            Dim f = FormattableString.Invariant([|$""""|])
        End Sub
    End Class
End Namespace"

            Const expected = "
Imports System

" & FormattableStringType & "

Namespace N
    Class C
        Public Sub M()
            Dim {|Rename:v|} As FormattableString = $""""
            Dim f = FormattableString.Invariant(v)
        End Sub
    End Class
End Namespace"

            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInAutoPropertyInitializerEqualsClause() As Task
            Dim code =
<File>
Imports System
Class C
    Property Name As String = [|"Roslyn"|]
End Class
</File>
            Dim expected =
<File>
Imports System
Class C
    Private Const {|Rename:V|} As String = "Roslyn"
    Property Name As String = V
End Class
</File>
            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInAutoPropertyWithCollectionInitializerAfterEqualsClause() As Task
            Dim code =
<File>
Imports System
Class C
    Property Grades As Integer() = [|{90, 73}|]
End Class
</File>
            Dim expected =
<File>
Imports System
Class C
    Private Shared ReadOnly {|Rename:p|} As Integer() = {90, 73}
    Property Grades As Integer() = p
End Class
</File>
            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInAutoPropertyInitializerAsClause() As Task
            Dim code =
<File>
Imports System
Class C
        Public Property Items As New List(Of String) From {[|"M"|], "T", "W"}
End Class
</File>
            Dim expected =
<File>
Imports System
Class C
    Private Const {|Rename:V|} As String = "M"
    Public Property Items As New List(Of String) From {V, "T", "W"}
End Class
</File>
            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInAutoPropertyObjectCreationExpressionWithinAsClause() As Task
            Dim code =
<File>
Imports System
Class C
        Property Orders As New List(Of Object)([|500|])
End Class
</File>
            Dim expected =
<File>
Imports System
Class C
    Private Const {|Rename:V|} As Integer = 500
    Property Orders As New List(Of Object)(V)
End Class
</File>
            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

    End Class
End Namespace

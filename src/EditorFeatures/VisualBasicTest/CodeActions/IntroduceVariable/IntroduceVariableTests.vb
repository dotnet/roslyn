' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.IntroduceVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.IntroduceVariable
    <Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
    <Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
    Public Class IntroduceVariableTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New IntroduceVariableCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return GetNestedActions(actions)
        End Function

        <Fact>
        Public Async Function Test1() As Task
            Await TestInRegularAndScriptAsync(
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
        Const {|Rename:Value|} As Integer = 1 + 1
        Console.WriteLine(Value)
    End Sub
End Module",
index:=2)
        End Function

        <Fact>
        Public Async Function Test2() As Task
            Await TestInRegularAndScriptAsync(
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
        Const {|Rename:Value|} As Integer = 1 + 1
        Console.WriteLine(Value)
    End Sub
End Module",
index:=3)
        End Function

        <Fact>
        Public Async Function TestInSingleLineIfExpression1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If goo([|1 + 1|]) Then bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If goo(V) Then bar(1 + 1)
    End Sub
End Module",
index:=2)
        End Function

        <Fact>
        Public Async Function TestInSingleLineIfExpression2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If goo([|1 + 1|]) Then bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If goo(V) Then bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact>
        Public Async Function TestInSingleLineIfStatement1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If goo(1 + 1) Then bar([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If goo(1 + 1) Then
            Const {|Rename:V|} As Integer = 1 + 1
            bar(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact>
        Public Async Function TestInSingleLineIfStatement2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If goo(1 + 1) Then bar([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If goo(V) Then bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact>
        Public Async Function TestNoIntroduceFieldOnMethodTypeParameter() As Task
            Dim source = "Module Program
    Sub Main(Of T)()
        Goo([|CType(2.ToString(), T)|])
    End Sub
End Module"
            Await TestExactActionSetOfferedAsync(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.Introduce_local_for_0, "CType(2.ToString(), T)"),
                    String.Format(FeaturesResources.Introduce_local_for_all_occurrences_of_0, "CType(2.ToString(), T)")})

            ' Verifies "Introduce field ..." is missing
        End Function

        <Fact>
        Public Async Function TestNoIntroduceFieldOnMethodParameter() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Goo([|x.ToString()|])
    End Sub
End Module"
            Await TestExactActionSetOfferedAsync(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.Introduce_local_for_0, "x.ToString()"),
                    String.Format(FeaturesResources.Introduce_local_for_all_occurrences_of_0, "x.ToString()")})

            ' Verifies "Introduce field ..." is missing
        End Function

        <Fact>
        Public Async Function TestNoRefactoringOnExpressionInAssignmentStatement() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Dim r = [|x.ToString()|]
    End Sub
End Module"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact>
        Public Async Function TestLocalGeneratedInInnerBlock1() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        If True Then
            Goo([|x.ToString()|])
        End If
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        If True Then
            Dim {|Rename:v|} As String = x.ToString()
            Goo(v)
        End If
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact>
        Public Async Function TestLocalGeneratedInInnerBlock2() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        If True Then
            Goo([|x.ToString()|])
        End If
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        If True Then
            Dim {|Rename:v|} As String = x.ToString()
            Goo(v)
        End If
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact>
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
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact>
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
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact>
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
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact>
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
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact>
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
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact>
        Public Async Function TestLocalFromSingleExpressionInObjectInitializer() As Task
            Dim source = "Module Program
    Structure GooStruct
        Dim GooMember1 As String
    End Structure
    Sub Main(x As Integer)
        Dim f1 = New GooStruct With {.GooMember1 = [|""t"" + ""test""|]}
    End Sub
End Module"
            Dim expected = "Module Program
    Structure GooStruct
        Dim GooMember1 As String
    End Structure
    Sub Main(x As Integer)
        Const {|Rename:V|} As String = ""t"" + ""test""
        Dim f1 = New GooStruct With {.GooMember1 = V}
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected, index:=2)
        End Function

        <Fact>
        Public Async Function TestLocalFromMultipleExpressionsInObjectInitializer() As Task
            Dim code =
"
Module Program
    Structure GooStruct
        Dim GooMember1 As String
        Dim GooMember2 As String
    End Structure
    Sub Main(x As Integer)
        Dim f1 = New GooStruct With {.GooMember1 = [|""t"" + ""test""|], .GooMember2 = ""t"" + ""test""}
        Dim f2 = New GooStruct With {.GooMember1 = ""t"" + ""test"", .GooMember2 = ""t"" + ""test""}
        Dim str As String = ""t"" + ""test""
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Structure GooStruct
        Dim GooMember1 As String
        Dim GooMember2 As String
    End Structure
    Sub Main(x As Integer)
        Const {|Rename:V|} As String = ""t"" + ""test""
        Dim f1 = New GooStruct With {.GooMember1 = V, .GooMember2 = V}
        Dim f2 = New GooStruct With {.GooMember1 = V, .GooMember2 = V}
        Dim str As String = V
    End Sub
End Module
"
            Await TestInRegularAndScriptAsync(code, expected, index:=3)
        End Function

        <Fact>
        Public Async Function TestFieldFromMultipleExpressionsInAnonType() As Task
            Dim source = "Class Program
    Dim q = New With {.str = [|""t"" + ""test""|]}
    Dim r = New With {.str = ""t"" + ""test""}
    Sub Goo()
        Dim x = ""t"" + ""test"" 
 End Sub
End Class"
            Dim expected = "Class Program
    Private Const {|Rename:V|} As String = ""t"" + ""test""
    Dim q = New With {.str = V}
    Dim r = New With {.str = V}
    Sub Goo()
        Dim x = V
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact>
        Public Async Function TestPrivateFieldFromExpressionInField() As Task
            Dim source = "Class Program
    Dim x = Goo([|2 + 2|])
End Class"
            Dim expected = "Class Program
    Private Const {|Rename:V|} As Integer = 2 + 2
    Dim x = Goo(V)
End Class"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact>
        Public Async Function TestNoLocalFromExpressionInField() As Task
            Dim source = "Class Program
    Dim x = Goo([|2 + 2|])
End Class"
            Await TestExactActionSetOfferedAsync(source, {String.Format(FeaturesResources.Introduce_constant_for_0, "2 + 2"), String.Format(FeaturesResources.Introduce_constant_for_all_occurrences_of_0, "2 + 2")})
        End Function

        <Fact>
        Public Async Function TestSharedModifierAbsentInGeneratedModuleFields() As Task
            Dim source = "Module Program
    Private ReadOnly y As Integer = 1
    Dim x = Goo([|2 + y|])
End Module"
            Dim expected = "Module Program
    Private ReadOnly y As Integer = 1
    Private ReadOnly {|Rename:v|} As Integer = 2 + y
    Dim x = Goo(v)
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact>
        Public Async Function TestSingleLocalInsertLocation() As Task
            Dim source = "Class Program
    Sub Method1()
        Dim v1 As String = ""TEST"" 
 Dim v2 As Integer = 2 + 2
        Goo([|2 + 2|])
    End Sub
End Class"
            Dim expected = "Class Program
    Sub Method1()
        Dim v1 As String = ""TEST"" 
 Dim v2 As Integer = 2 + 2
        Const {|Rename:V|} As Integer = 2 + 2
        Goo(V)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=2)
        End Function

#Region "Parameter context"

        <Fact>
        Public Async Function TestConstantFieldGenerationForParameterSingleOccurrence() As Task
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = "Module Module1
    Sub Goo(Optional x As Integer = [|42|])
    End Sub
End Module"
            Dim expected = "Module Module1
    Private Const {|Rename:V|} As Integer = 42

    Sub Goo(Optional x As Integer = V)
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact>
        Public Async Function TestConstantFieldGenerationForParameterAllOccurrences() As Task
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = "Module Module1
    Sub Bar(Optional x As Integer = 42)
    End Sub
    Sub Goo(Optional x As Integer = [|42|])
    End Sub
End Module"
            Dim expected = "Module Module1
    Private Const {|Rename:V|} As Integer = 42

    Sub Bar(Optional x As Integer = V)
    End Sub
    Sub Goo(Optional x As Integer = V)
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

#End Region

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540269")>
        Public Async Function TestReplaceDottedExpression() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Console.WriteLine([|Goo.someVariable|])
        Console.WriteLine(Goo.someVariable)
    End Sub
End Module
Friend Class Goo
    Shared Public someVariable As Integer
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim {|Rename:someVariable|} As Integer = Goo.someVariable
        Console.WriteLine(someVariable)
        Console.WriteLine(someVariable)
    End Sub
End Module
Friend Class Goo
    Shared Public someVariable As Integer
End Class",
index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        Public Async Function TestReplaceSingleLineIfWithMultiLine1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo([|2 + 2|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Const {|Rename:V|} As Integer = 2 + 2
            Goo(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        Public Async Function TestReplaceSingleLineIfWithMultiLine2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo([|1 + 1|]) Else Bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Const {|Rename:V|} As Integer = 1 + 1
            Goo(V)
        Else
            Bar(1 + 1)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        Public Async Function TestReplaceSingleLineIfWithMultiLine3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo([|1 + 1|]) Else Bar(1 + 1)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 1 + 1
        If True Then Goo(V) Else Bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540457")>
        Public Async Function TestReplaceSingleLineIfWithMultiLine4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo(1 + 1) Else Bar([|1 + 1|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Goo(1 + 1)
        Else
            Const {|Rename:V|} As Integer = 1 + 1
            Bar(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")>
        Public Async Function TestCantExtractMethodTypeParameterToFieldCount() As Task
            Await TestActionCountAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(Of T)(x As Integer)
        Goo([|CType(2.ToString(), T)|])
    End Sub
End Module",
count:=2)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")>
        Public Async Function TestCantExtractMethodTypeParameterToField() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(Of T)(x As Integer)
        Goo([|CType(2.ToString(), T)|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(Of T)(x As Integer)
        Dim {|Rename:t|} As T = CType(2.ToString(), T)
        Goo(t)
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540489")>
        Public Async Function TestOnlyFieldsInsideConstructorInitializer() As Task
            Await TestActionCountAsync(
"Class Goo
    Sub New()
        Me.New([|2 + 2|])
    End Sub
    Sub New(v As Integer)
    End Sub
End Class",
count:=2)

            Await TestInRegularAndScriptAsync(
"Class Goo
    Sub New()
        Me.New([|2 + 2|])
    End Sub
    Sub New(v As Integer)
    End Sub
End Class",
"Class Goo
    Private Const {|Rename:V|} As Integer = 2 + 2

    Sub New()
        Me.New(V)
    End Sub
    Sub New(v As Integer)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540485")>
        Public Async Function TestIntroduceLocalForConstantExpression() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065689")>
        Public Async Function TestIntroduceLocalForConstantExpressionWithTrailingTrivia() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Private Function GetX() As Object
        Return [|""c d
"" + ' comment 1
""a
b"" ' comment 2|]
    End Function
End Class
",
"
Class C
    Private Function GetX() As Object
        Const {|Rename:V|} As String = ""c d
"" + ' comment 1
""a
b""
        Return V ' comment 2
    End Function
End Class
",
index:=3)
        End Function

        <Fact>
        Public Async Function TestIntroduceFieldWithTrailingTrivia() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Private Sub S()
        Dim x = 1 + [|2|] ' comment
    End Sub
End Class
",
"
Class C
    Private Const {|Rename:V|} As Integer = 2

    Private Sub S()
        Dim x = 1 + V ' comment
    End Sub
End Class
",
index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540487")>
        Public Async Function TestFormattingForPartialExpression() As Task
            Dim code =
"
Module Program
    Sub Main()
        Dim i = [|1 + 2|] + 3
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Sub Main()
        Const {|Rename:V|} As Integer = 1 + 2
        Dim i = V + 3
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=2)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540491")>
        Public Async Function TestInAttribute1() As Task
            Await TestInRegularAndScriptAsync(
"<Attr([|2 + 2|])>
Class Goo
End Class
Friend Class AttrAttribute
    Inherits Attribute
End Class",
"<Attr(Goo.V)>
Class Goo
    Friend Const {|Rename:V|} As Integer = 2 + 2
End Class
Friend Class AttrAttribute
    Inherits Attribute
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540490")>
        Public Async Function TestInMyClassNew() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Sub New()
        MyClass.New([|42|])
    End Sub
    Sub New(x As Integer)
    End Sub
End Class",
"Class Goo
    Private Const {|Rename:X|} As Integer = 42

    Sub New()
        MyClass.New(X)
    End Sub
    Sub New(x As Integer)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSingleToMultiLineIf1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo([|2 + 2|]) Else Bar(2 + 2)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Const {|Rename:V|} As Integer = 2 + 2
            Goo(V)
        Else
            Bar(2 + 2)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact>
        Public Async Function TestSingleToMultiLineIf2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo([|2 + 2|]) Else Bar(2 + 2)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 2 + 2
        If True Then Goo(V) Else Bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact>
        Public Async Function TestSingleToMultiLineIf3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo(2 + 2) Else Bar([|2 + 2|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then
            Goo(2 + 2)
        Else
            Const {|Rename:V|} As Integer = 2 + 2
            Bar(V)
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact>
        Public Async Function TestSingleToMultiLineIf4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Goo(2 + 2) Else Bar([|2 + 2|])
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Const {|Rename:V|} As Integer = 2 + 2
        If True Then Goo(V) Else Bar(V)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541604")>
        Public Async Function TestAttribute() As Task
            Await TestInRegularAndScriptAsync(
"<Attr([|2 + 2|])>
Class Goo
End Class
Friend Class AttrAttribute
    Inherits System.Attribute
End Class",
"<Attr(Goo.V)>
Class Goo
    Friend Const {|Rename:V|} As Integer = 2 + 2
End Class
Friend Class AttrAttribute
    Inherits System.Attribute
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542092")>
        Public Async Function TestRangeArgumentLowerBound1() As Task
            Await TestMissingInRegularAndScriptAsync("Module M
    Sub Main()
        Dim x() As Integer
        ReDim x([|0|] To 5)
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542092")>
        Public Async Function TestRangeArgumentLowerBound2() As Task
            Dim code =
"
Module M
    Sub Main()
        Dim x() As Integer
        ReDim x(0 To 5)
        Dim a = [|0|] + 1
    End Sub
End Module
"

            Dim expected =
"
Module M
    Sub Main()
        Dim x() As Integer
        ReDim x(0 To 5)
        Const {|Rename:V|} As Integer = 0
        Dim a = V + 1
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543029"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542963"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542295")>
        Public Async Function TestUntypedExpression() As Task
            Await TestInRegularAndScriptAsync(
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
            Dim {|Rename:value|} As Object = Sub()
                                  End Sub

            q = value
        End If
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542374")>
        Public Async Function TestFieldConstantInAttribute1() As Task
            Await TestInRegularAndScriptAsync(
"<Goo(2 + 3 + 4)>
Module Program
    Dim x = [|2 + 3|] + 4
End Module
Friend Class GooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
"<Goo(2 + 3 + 4)>
Module Program
    Private Const {|Rename:V|} As Integer = 2 + 3
    Dim x = V + 4
End Module
Friend Class GooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542374")>
        Public Async Function TestFieldConstantInAttribute2() As Task
            Await TestAsync(
"<Goo(2 + 3 + 4)>
Module Program
    Dim x = [|2 + 3|] + 4
End Module
Friend Class GooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
"<Goo(V + 4)>
Module Program
    Friend Const {|Rename:V|} As Integer = 2 + 3
    Dim x = V + 4
End Module
Friend Class GooAttribute
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class",
index:=1,
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542783")>
        Public Async Function TestMissingOnAttributeName() As Task
            Await TestMissingInRegularAndScriptAsync(
"<[|Obsolete|]>
Class C
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542811")>
        Public Async Function TestMissingOnFilterClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        Try
        Catch ex As Exception When [|+|] 
 End Try
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542906")>
        Public Async Function TestNoIntroduceLocalInAttribute() As Task
            Dim input =
"Module Program \n <Obsolete([|""""|])> \n Sub Main(args As String()) \n End Sub \n End Module"

            Await TestActionCountAsync(
NewLines(input),
count:=2)

            Await TestInRegularAndScriptAsync(
NewLines(input),
"Module Program
    Private Const {|Rename:V|} As String = """"

    <Obsolete(V)> 
 Sub Main(args As String()) 
 End Sub 
 End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542947")>
        Public Async Function TestNotOnMyBase() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class c1
    Public res As String
    Sub Goo()
        res = ""1"" 
 End Sub
End Class
Class c2
    Inherits c1
    Sub scen1()
        [|MyBase|].Goo()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541966")>
        Public Async Function TestNestedMultiLineIf1() As Task
            Dim code =
"
Imports System

Module Program
    Sub Main()
        If True Then If True Then Console.WriteLine([|1|]) Else Console.WriteLine(2) Else Console.WriteLine(3)
    End Sub
End Module
"

            Dim expected =
"
Imports System

Module Program
    Sub Main()
        If True Then
            If True Then
                Const {|Rename:Value|} As Integer = 1
                Console.WriteLine(Value)
            Else
                Console.WriteLine(2)
            End If
        Else
            Console.WriteLine(3)
        End If
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541966")>
        Public Async Function TestNestedMultiLineIf2() As Task
            Dim code =
"
Imports System

Module Program
    Sub Main()
        If True Then If True Then Console.WriteLine(1) Else Console.WriteLine([|2|]) Else Console.WriteLine(3)
    End Sub
End Module
"

            Dim expected =
"
Imports System

Module Program
    Sub Main()
        If True Then
            If True Then
                Console.WriteLine(1)
            Else
                Const {|Rename:Value|} As Integer = 2
                Console.WriteLine(Value)
            End If
        Else
            Console.WriteLine(3)
        End If
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541966")>
        Public Async Function TestNestedMultiLineIf3() As Task
            Dim code =
"
Imports System

Module Program
    Sub Main()
        If True Then If True Then Console.WriteLine(1) Else Console.WriteLine(2) Else Console.WriteLine([|3|])
    End Sub
End Module
"

            Dim expected =
"
Imports System

Module Program
    Sub Main()
        If True Then
            If True Then Console.WriteLine(1) Else Console.WriteLine(2)
        Else
            Const {|Rename:Value|} As Integer = 3
            Console.WriteLine(Value)
        End If
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        Public Async Function TestSingleLineLambda1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer) Console.WriteLine([|x + 1|]) ' Introduce local 
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
        Dim a = Sub(x As Integer) Dim {|Rename:value|} As Integer = x + 1
                    Console.WriteLine(value)
                End Sub ' Introduce local 
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        Public Async Function TestSingleLineLambda2() As Task
            Await TestInRegularAndScriptAsync(
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
                        Dim {|Rename:value|} As Integer = x + 1
                        Console.WriteLine(value)
                    Else
                        Console.WriteLine()
                    End If
                End Sub
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        Public Async Function TestSingleLineLambda3() As Task
            Await TestInRegularAndScriptAsync(
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
                        Dim {|Rename:value|} As Integer = x + 1
                        Console.WriteLine(value)
                    End If
                End Sub
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543273")>
        Public Async Function TestSingleLineLambda4() As Task
            Await TestInRegularAndScriptAsync(
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
                    Dim {|Rename:value|} As Integer = x + 1
                    If True Then Console.WriteLine(value) Else Console.WriteLine(value)
                End Sub
    End Sub
End Module",
index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543299")>
        Public Async Function TestSingleLineLambda5() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim query = Sub(a) a = New With {Key .Key = Function(ByVal arg As Integer) As Integer
                                                        Return arg
                                                    End Function}.Key.Invoke([|a Or a|])
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim query = Sub(a) Dim {|Rename:arg1|} As Object = a Or a
                        a = New With {Key .Key = Function(ByVal arg As Integer) As Integer
                                                     Return arg
                                                 End Function}.Key.Invoke(arg1)
                    End Sub
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542762")>
        Public Async Function TestNotInIntoClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Linq
Module 
 Sub Main()
        Dim x = Aggregate y In New Integer() {1}
        Into [|Count()|]
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543289")>
        Public Async Function TestNotOnAttribute1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Explicit Off
Module Program
    <Runtime.CompilerServices.[|Extension|]()> _
    Function Extension(ByVal x As Integer) As Integer
        Return x
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543289")>
        Public Async Function TestNotOnAttribute1CommentsAfterLineContinuation() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Explicit Off
Module Program
    <Runtime.CompilerServices.[|Extension|]()> _ ' Test
    Function Extension(ByVal x As Integer) As Integer
        Return x
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543289")>
        Public Async Function TestNotOnAttribute2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Explicit Off
Module Program
    <Runtime.CompilerServices.[|Extension()|]> _
    Function Extension(ByVal x As Integer) As Integer
        Return x
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543289")>
        Public Async Function TestNotOnAttribute2CommentsAfterLineContinuation() As Task
            Await TestMissingInRegularAndScriptAsync(
"Option Explicit Off
Module Program
    <Runtime.CompilerServices.[|Extension()|]> _ ' Test
    Function Extension(ByVal x As Integer) As Integer
        Return x
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543461")>
        Public Async Function TestCollectionInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim i1 = New Integer() [|{4, 5}|]
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543573")>
        Public Async Function TestCaseInsensitiveNameConflict() As Task
            Await TestInRegularAndScriptAsync(
"Class M
    Public Function Goo()
        Return [|Me.Goo|] * 0
    End Function
End Class",
"Class M
    Public Function Goo()
        Dim {|Rename:goo1|} As Object = Me.Goo
        Return goo1 * 0
    End Function
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        Public Async Function TestQuery1() As Task
            Await TestInRegularAndScriptAsync(
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
        Dim results = From s In New Integer() {1}
                      Let {|Rename:v|} = Sample(s)
                      Select v
        Return 0
    End Function
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        Public Async Function TestQuery2() As Task
            Await TestInRegularAndScriptAsync(
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
        Dim results = From s In New Integer() {1}
                      Let {|Rename:v|} = Sample(s) Where v > 21
                      Select Sample(s)
        Return 0
    End Function
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        Public Async Function TestQuery3() As Task
            Await TestInRegularAndScriptAsync(
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
        Dim results = From s In New Integer() {1}
                      Let {|Rename:v|} = Sample(s) Where v > 21
                      Select v
        Return 0
    End Function
End Class",
index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543590")>
        Public Async Function TestQuery4() As Task
            Await TestInRegularAndScriptAsync(
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
                      Where Sample(s) > 21
                      Let {|Rename:v|} = Sample(s)
                      Select v
        Return 0
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestQuery5() As Task
            Await TestInRegularAndScriptAsync(
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
                      Let {|Rename:v|} = Sample(s)
                      Where v > 21
                      Select v
        Return 0
    End Function
End Class",
index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543529")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")>
        Public Async Function TestInStatementlessConstructorParameter() As Task
            Await TestMissingInRegularAndScriptAsync("Class C1
    Sub New(Optional ByRef x As String = [|Nothing|])
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543650")>
        Public Async Function TestReferenceToAnonymousTypeProperty() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class AM
    Sub M(args As String())
        Dim var1 As New AM
        Dim at1 As New With {var1, .friend = [|.var1|]}
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543698")>
        Public Async Function TestIntegerArrayExpression() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        Return [|New Integer() {}|]
    End Sub
End Module",
"Module Program
    Sub Main()
        Dim {|Rename:integers|} As Integer() = New Integer() {}
        Return integers
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544273")>
        Public Async Function TestAttributeNamedParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class TestAttribute
    Inherits Attribute
    Public Sub New(Optional a As Integer = 42)
    End Sub
End Class
<Test([|a|]:=5)>
Class Goo
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544265")>
        Public Async Function TestMissingOnWrittenToExpression() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        Dim x = New Integer() {1, 2}
        [|x(1)|] = 2
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        Public Async Function TestImplicitMemberAccess1() As Task
            Await TestMissingInRegularAndScriptAsync(
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        Public Async Function TestImplicitMemberAccess2() As Task
            Await TestMissingInRegularAndScriptAsync(
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        Public Async Function TestImplicitMemberAccess3() As Task
            Await TestMissingInRegularAndScriptAsync(
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543824")>
        Public Async Function TestImplicitMemberAccess4() As Task
            Dim code =
"
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
"

            Dim expected =
"
Imports System
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
    Public Property PropInt As Integer
End Class
Public Class C2
    Public Shared Sub Main()
        Dim {|Rename:c1|} As C1 = New C1() With {.FieldStr = .FieldInt.ToString()}
        Dim x = 1 + c1
    End Sub
End Class
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529510")>
        <Fact(Skip:="529510")>
        Public Async Function TestNoRefactoringOnAddressOfExpression() As Task
            Dim source = "Imports System
Module Module1
    Public Sub Goo(ByVal a1 As Exception)
    End Sub
    Public Sub goo(ByVal a1 As Action(Of ArgumentException))
    End Sub
    Sub Main()
        Goo(New Action(Of Exception)([|AddressOf Goo|]))
    End Sub
End Module"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529510")>
        Public Async Function TestMissingOnAddressOfInDelegate() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Module1
    Public Sub Goo(ByVal a1 As Exception)
    End Sub
    Public Sub goo(ByVal a1 As Action(Of ArgumentException))
    End Sub
    Sub Main()
        goo(New Action(Of Exception)([|AddressOf Goo|]))
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545168")>
        Public Async Function TestMissingOnXmlName() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module M
    Sub Main()
        Dim x = <[|x|]/>
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545262")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")>
        Public Async Function TestInTernaryConditional() As Task
            Await TestMissingInRegularAndScriptAsync("Module Program
    Sub Main(args As String())
        Dim p As Object = Nothing
        Dim Obj1 = If(New With {.a = True}.a, p, [|Nothing|])
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545316")>
        Public Async Function TestInPropertyInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Module Module1
    Property Prop As New List(Of String) From {[|""One""|], ""two""}
End Module",
"Module Module1
    Private Const {|Rename:V|} As String = ""One""
    Property Prop As New List(Of String) From {V, ""two""}
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545308")>
        Public Async Function TestDoNotMergeAmpersand() As Task
            Dim code =
"
Module Module1
    Public Sub goo(Optional ByVal arg = ([|""a""|]) & ""b"")
    End Sub
End Module
"

            Dim expected =
"
Module Module1
    Private Const {|Rename:V|} As String = ""a""

    Public Sub goo(Optional ByVal arg = V & ""b"")
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545258")>
        Public Async Function TestVenusGeneration1() As Task
            Dim code =
"
Class C
    Sub Goo()
#ExternalSource (""Goo"", 1)
        Console.WriteLine([|5|])
#End ExternalSource
   End Sub
End Class
"

            Dim expected =
"
Class C
    Sub Goo()
#ExternalSource (""Goo"", 1)
        Const {|Rename:V|} As Integer = 5
        Console.WriteLine(V)
#End ExternalSource
   End Sub
End Class
"

            Await TestExactActionSetOfferedAsync(code,
                {String.Format(FeaturesResources.Introduce_local_constant_for_0, "5"),
                String.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5")})

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545258")>
        Public Async Function TestVenusGeneration2() As Task
            Dim code =
"
Class C
#ExternalSource (""Goo"", 1)
    Sub Goo()
        If False Then
            Console.WriteLine([|5|])
        End If
    End Sub
#End ExternalSource
End Class
"

            Await TestExactActionSetOfferedAsync(code,
                                      {String.Format(FeaturesResources.Introduce_local_constant_for_0, "5"),
                                       String.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5")})
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545258")>
        Public Async Function TestVenusGeneration3() As Task
            Dim code =
"
Class C
    Sub Goo()
#ExternalSource (""Goo"", 1)
        If False Then
            Console.WriteLine([|5|])
        End If
#End ExternalSource
    End Sub
End Class
"

            Dim expected =
"
Class C
    Sub Goo()
#ExternalSource (""Goo"", 1)
        If False Then
            Const {|Rename:V|} As Integer = 5
            Console.WriteLine(V)
        End If
#End ExternalSource
    End Sub
End Class
"

            Await TestExactActionSetOfferedAsync(code,
                                      {String.Format(FeaturesResources.Introduce_local_constant_for_0, "5"),
                                       String.Format(FeaturesResources.Introduce_local_constant_for_all_occurrences_of_0, "5")})

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545525")>
        Public Async Function TestInvocation() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On

Class C
    Shared Sub Main()
        Dim x = [|New C().Goo()|](0)
    End Sub
    Function Goo() As Integer()
    End Function
End Class",
"Option Strict On

Class C
    Shared Sub Main()
        Dim {|Rename:integers|} As Integer() = New C().Goo()
        Dim x = integers(0)
    End Sub
    Function Goo() As Integer()
    End Function
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545829")>
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
            Dim {|Rename:getHashCode|} As Integer = .GetHashCode
            Dim x = getHashCode Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module",
parseOptions:=GetScriptOptions())
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545702")>
        Public Async Function TestMissingInRefLocation() As Task
            Dim markup =
"
Module A
    Sub Main()
        Goo([|1|])
    End Sub
    Sub Goo(ByRef x As Long)
    End Sub
    Sub Goo(x As String)
    End Sub
End Module
"

            Await TestMissingInRegularAndScriptAsync(markup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546139")>
        Public Async Function TestAcrossPartialTypes() As Task
            Await TestInRegularAndScriptAsync(
"Partial Class C
    Sub goo1(Optional x As String = [|""HELLO""|])
    End Sub
End Class
Partial Class C
    Sub goo3(Optional x As String = ""HELLO"")
    End Sub
End Class",
"Partial Class C
    Private Const {|Rename:V|} As String = ""HELLO""

    Sub goo1(Optional x As String = V)
    End Sub
End Class
Partial Class C
    Sub goo3(Optional x As String = V)
    End Sub
End Class",
index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544669")>
        Public Async Function TestFunctionBody1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim a1 = Function(ByVal x) [|x!goo|]
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim a1 = Function(ByVal x)
                     Dim {|Rename:goo|} As Object = x!goo
                     Return goo
                 End Function
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065689")>
        Public Async Function TestTrailingTrivia() As Task
            Dim code =
"
Module M
    Sub Main()
        Dim a = 1 +
        [|2|] ' comment

        End Sub
End Module
"

            Dim expected =
"
Module M
    Private Const {|Rename:V|} As Integer = 2

    Sub Main()
        Dim a = 1 +
        V ' comment

    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546815")>
        Public Async Function TestInIfStatement() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830928")>
        Public Async Function TestIntroduceLocalRemovesUnnecessaryCast() As Task
            Await TestInRegularAndScriptAsync(
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
        Dim {|Rename:item|} As String = hSet.ToString()
        hSet.Add(item)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546691")>
        Public Async Function TestIntroLocalInSingleLineLambda() As Task
            Dim code =
"
Module Program
    Sub Main()
        Dim x = Function() [|Sub()
                           End Sub|]
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Sub Main()
        Dim {|Rename:value|} = Sub()
                    End Sub

        Dim x = Function() value
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530720")>
        Public Async Function TestSingleToMultilineLambdaLineBreaks() As Task
            Dim code =
"
Module Program
    Sub Main()
        Dim a = Function(c) [|c!goo|]
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Sub Main()
        Dim a = Function(c)
                    Dim {|Rename:goo|} As Object = c!goo
                    Return goo
                End Function
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531478")>
        Public Async Function TestEscapeKeywordsIfNeeded1() As Task
            Dim code =
"
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Take([|From x In """"|])
    End Sub
    Sub Take(x)
    End Sub
End Module
"

            Dim expected =
"
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Dim {|Rename:x1|} As IEnumerable(Of Char) = From x In """"
        [Take](x1)
    End Sub
    Sub Take(x)
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632327")>
        Public Async Function TestInsertAfterPreprocessor1() As Task
            Dim code =
"
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource (""Home\Index.vbhtml"", 1)
        Dim i = [|1 + 2|] + 3
        If True Then
            Dim j = 1 + 2 + 3
        End If
#End ExternalSource
    End Sub
End Class
"

            Dim expected =
"
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource (""Home\Index.vbhtml"", 1)
        Const {|Rename:V|} As Integer = 1 + 2
        Dim i = V + 3
        If True Then
            Dim j = 1 + 2 + 3
        End If
#End ExternalSource
    End Sub
End Class
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632327")>
        Public Async Function TestInsertAfterPreprocessor2() As Task
            Dim code =
"
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource (""Home\Index.vbhtml"", 1)
        Dim i = 1 + 2 + 3
        If True Then
            Dim j = [|1 + 2|] + 3
        End If
#End ExternalSource
    End Sub
End Class
"

            Dim expected =
"
Public Class Index_vbhtml
    Public Sub Execute()
#ExternalSource (""Home\Index.vbhtml"", 1)
        Dim i = 1 + 2 + 3
        If True Then
            Const {|Rename:V|} As Integer = 1 + 2
            Dim j = V + 3
        End If
#End ExternalSource
    End Sub
End Class
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682683")>
        Public Async Function TestDoNotRemoveParenthesesIfOperatorPrecedenceWouldBeBroken() As Task
            Dim code =
"
Imports System
 
Module Program
    Sub Main()
        Console.WriteLine(5 - ([|1|] + 2))
    End Sub
End Module
"

            Dim expected =
"
Imports System
 
Module Program
    Sub Main()
        Const {|Rename:V|} As Integer = 1
        Console.WriteLine(5 - (V + 2))
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=2)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022458")>
        Public Async Function TestDoNotSimplifyParentUnlessEntireInnerNodeIsSelected() As Task
            Dim code =
"
Imports System
 
Module Program
    Sub Main()
        Dim s = ""Text""
        Dim x = 42
        If ([|s.Length|].CompareTo(x) > 0 AndAlso
            s.Length.CompareTo(x) > 0) Then
        End If
    End Sub
End Module
"

            Dim expected =
"
Imports System
 
Module Program
    Sub Main()
        Dim s = ""Text""
        Dim x = 42
        Dim {|Rename:length|} As Integer = s.Length

        If (length.CompareTo(x) > 0 AndAlso
            length.CompareTo(x) > 0) Then
        End If
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939259")>
        Public Async Function TestIntroduceLocalWithTriviaInMultiLineStatements() As Task
            Dim code =
"
Imports System
 
Module Program
    Sub Main()
        Dim x = If(True,
                   [|1|], ' TODO: Comment
                   2)
    End Sub
End Module
"

            Dim expected =
"
Imports System
 
Module Program
    Sub Main()
        Const {|Rename:V|} As Integer = 1
        Dim x = If(True,
                   V, ' TODO: Comment
                   2)
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")>
        Public Async Function TestMissingOnNothingLiteral() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Module Program
    Sub Main(args As String())
        Main([|Nothing|])
        M(Nothing)
    End Sub

    Sub M(i As Integer)
    End Sub
End Module
")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        Public Async Function TestInParentConditionalAccessExpressions() As Task
            Dim code =
"
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = [|F(New C)|]?.F(New C)?.F(New C)
        Return x
    End Function
End Class
"
            Dim expected =
"
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim {|Rename:c|} As C = F(New C)
        Dim y = c?.F(New C)?.F(New C)
        Return x
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/3110")>
        Public Async Function TestMissingAcrossMultipleParentConditionalAccessExpressions() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = [|F(New C)?.F(New C)|]?.F(New C)
        Return x
    End Function
End Class
")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        Public Async Function TestMissingOnInvocationExpressionInParentConditionalAccessExpressions() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = F(New C)?.[|F(New C)|]?.F(New C)
        Return x
    End Function
End Class
")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")>
        Public Async Function TestMissingOnMemberBindingExpressionInParentConditionalAccessExpressions() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Class C
    Sub F()
        Dim s as String = ""Text""
        Dim l = s?.[|Length|]
    End Sub
End Class
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2026")>
        Public Async Function TestReplaceAllFromInsideIfBlock() As Task
            Dim code =
"
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
"

            Dim expected =
"
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
"

            Await TestInRegularAndScriptAsync(code, expected, index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065661")>
        Public Async Function TestIntroduceVariableTextDoesntSpanLines1() As Task
            Dim code = "
Class C
    Sub M()
        Dim s = """" + [|""a

b
c""|]
    End Sub
End Class"
            Await TestSmartTagTextAsync(code, String.Format(FeaturesResources.Introduce_local_constant_for_0, """a b c"""), New TestParameters(index:=2))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065661")>
        Public Async Function TestIntroduceVariableTextDoesntSpanLines2() As Task
            Dim code = "
Class C
    Sub M()
        Dim s = """" + [|$""a

b
c""|]
    End Sub
End Class"
            Await TestSmartTagTextAsync(code, String.Format(FeaturesResources.Introduce_local_for_0, "$""a b c"""))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/976")>
        Public Async Function TestNoConstantForInterpolatedStrings1() As Task
            Dim code =
"
Module Program
    Sub Main()
        Dim args As String() = Nothing
        Console.WriteLine([|$""{DateTime.Now.ToString()}Text{args(0)}""|])
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Sub Main()
        Dim args As String() = Nothing
        Dim {|Rename:v|} As String = $""{DateTime.Now.ToString()}Text{args(0)}""
        Console.WriteLine(v)
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/976")>
        Public Async Function TestNoConstantForInterpolatedStrings2() As Task
            Dim code =
"
Module Program
    Sub Main()
        Console.WriteLine([|$""Text{{s}}""|])
        Console.WriteLine($""Text{{s}}"")
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Sub Main()
        Dim {|Rename:v|} As String = $""Text{{s}}""
        Console.WriteLine(v)
        Console.WriteLine(v)
    End Sub
End Module
"

            Await TestInRegularAndScriptAsync(code, expected, index:=1)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3147")>
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
            Dim {|Rename:formattable|} As FormattableString = $""""
            Dim f = FormattableString.Invariant(formattable)
        End Sub
    End Class
End Namespace"

            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/936")>
        Public Async Function TestInAutoPropertyInitializerEqualsClause() As Task
            Dim code =
"
Imports System
Class C
    Property Name As String = [|""Roslyn""|]
End Class
"
            Dim expected =
"
Imports System
Class C
    Private Const {|Rename:V|} As String = ""Roslyn""
    Property Name As String = V
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/936")>
        Public Async Function TestInAutoPropertyWithCollectionInitializerAfterEqualsClause() As Task
            Dim code =
"
Imports System
Class C
    Property Grades As Integer() = [|{90, 73}|]
End Class
"
            Dim expected =
"
Imports System
Class C
    Private Shared ReadOnly {|Rename:value|} As Integer() = {90, 73}
    Property Grades As Integer() = value
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/936")>
        Public Async Function TestInAutoPropertyInitializerAsClause() As Task
            Dim code =
"
Imports System
Class C
        Public Property Items As New List(Of String) From {[|""M""|], ""T"", ""W""}
End Class
"
            Dim expected =
"
Imports System
Class C
    Private Const {|Rename:V|} As String = ""M""
    Public Property Items As New List(Of String) From {V, ""T"", ""W""}
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/936")>
        Public Async Function TestInAutoPropertyObjectCreationExpressionWithinAsClause() As Task
            Dim code =
"
Imports System
Class C
        Property Orders As New List(Of Object)([|500|])
End Class
"
            Dim expected =
"
Imports System
Class C
    Private Const {|Rename:V|} As Integer = 500
    Property Orders As New List(Of Object)(V)
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11777")>
        Public Async Function TestGenerateLocalConflictingName1() As Task
            Await TestInRegularAndScriptAsync(
"class Program
    class MySpan
        public Start as integer
    end class

    sub Method(span as MySpan)
        dim pos as integer = span.Start
        while pos < [|span.Start|]
            dim start as integer = pos
        end while
    end sub
end class",
"class Program
    class MySpan
        public Start as integer
    end class

    sub Method(span as MySpan)
        dim pos as integer = span.Start
        Dim {|Rename:start1|} As Integer = span.Start

        while pos < start1
            dim start as integer = pos
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TupleWithInferredName_LeaveExplicitName() As Task
            Dim code = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, x:=[|C.y|])
    End Sub
End Class
"
            Dim expected = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim {|Rename:y1|} As Integer = C.y
        Dim t = (a, x:=y1)
    End Sub
End Class
"
            Await TestAsync(code, expected, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest))
        End Function

        <Fact>
        Public Async Function TupleWithInferredName_InferredNameBecomesExplicit() As Task
            Dim code = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, [|C.y|])
    End Sub
End Class
"
            Dim expected = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim {|Rename:y1|} As Integer = C.y
        Dim t = (a, y:=y1)
    End Sub
End Class
"
            Await TestAsync(code, expected, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest))
        End Function

        <Fact>
        Public Async Function TupleWithInferredName_AllOccurrences() As Task
            Dim code = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, [|C.y|])
        Dim t2 = (C.y, a)
    End Sub
End Class
"
            Dim expected = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim {|Rename:y1|} As Integer = C.y
        Dim t = (a, y:=y1)
        Dim t2 = (y:=y1, a)
    End Sub
End Class
"
            Await TestAsync(code, expected, index:=1,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest))
        End Function

        <Fact>
        Public Async Function TupleWithInferredName_NoDuplicateNames() As Task
            Dim code = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim t = (C.y, [|C.y|])
    End Sub
End Class
"
            Dim expected = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim {|Rename:y1|} As Integer = C.y
        Dim t = (y1, y1)
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(code, expected, index:=1)
        End Function

        <Fact>
        Public Async Function TupleWithInferredName_NoReservedNames() As Task
            Dim code = "
Class C
    Shared Dim rest As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, [|C.rest|])
    End Sub
End Class
"
            Dim expected = "
Class C
    Shared Dim rest As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim {|Rename:rest1|} As Integer = C.rest
        Dim t = (a, rest1)
    End Sub
End Class
"
            Await TestAsync(code, expected, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest))
        End Function

        <Fact>
        Public Async Function AnonymousTypeWithInferredName_LeaveExplicitName() As Task
            Dim code = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim t = New With {a, [|C.y|]}
    End Sub
End Class
"
            Dim expected = "
Class C
    Shared Dim y As Integer = 2
    Sub M()
        Dim a As Integer = 1
        Dim {|Rename:y1|} As Integer = C.y
        Dim t = New With {a, .y = y1}
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2423")>
        Public Async Function TestPickNameBasedOnArgument1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public sub new(a as string, b as string)
        dim c = new TextSpan([|integer.Parse(a)|], integer.Parse(b))
    end sub
end class

structure TextSpan
    public sub new(start as integer, length as integer)
    end sub
end structure",
"class C
    public sub new(a as string, b as string)
        Dim {|Rename:start|} As Integer = integer.Parse(a)
        dim c = new TextSpan(start, integer.Parse(b))
    end sub
end class

structure TextSpan
    public sub new(start as integer, length as integer)
    end sub
end structure")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2423")>
        Public Async Function TestPickNameBasedOnArgument2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public sub new(a as string, b as string)
        dim c = new TextSpan(integer.Parse(a), [|integer.Parse(b)|])
    end sub
end class

structure TextSpan
    public sub new(start as integer, length as integer)
    end sub
end structure",
"class C
    public sub new(a as string, b as string)
        Dim {|Rename:length|} As Integer = integer.Parse(b)
        dim c = new TextSpan(integer.Parse(a), length)
    end sub
end class

structure TextSpan
    public sub new(start as integer, length as integer)
    end sub
end structure")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10123")>
        Public Async Function TestSimpleParameterName() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Goo([|x|])
    End Sub
End Module"
            Dim expected = "Module Program
    Sub Main(x As Integer)
        Dim {|Rename:x1|} As Integer = x
        Goo(x1)
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10123")>
        Public Async Function TestSimpleParameterName_EmptySelection() As Task
            Dim source = "Module Program
    Sub Main(x As Integer)
        Goo([||]x)
    End Sub
End Module"
            Await TestMissingAsync(source)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10123")>
        Public Async Function TestFieldName_QualifiedWithMe() As Task
            Dim source = "Module Program
    Dim x As Integer
    Sub Main()
        Goo([|x|])
    End Sub
End Module"
            Dim expected = "Module Program
    Dim x As Integer
    Sub Main()
        Dim {|Rename:x1|} As Integer = x
        Goo(x1)
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10123")>
        Public Async Function TestFieldName_QualifiedWithType() As Task
            Dim source = "Module Program
    Shared Dim x As Integer
    Sub Main()
        Goo([|Program.x|])
    End Sub
End Module"
            Dim expected = "Module Program
    Shared Dim x As Integer
    Sub Main()
        Dim {|Rename:x1|} As Integer = Program.x
        Goo(x1)
    End Sub
End Module"
            Await TestInRegularAndScriptAsync(source, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21373")>
        Public Async Function TestInAttribute() As Task
            Dim code = "
Class C
    Public Property Foo()

    <Example([|3 + 3|])>
    Public Property Bar()
End Class
"
            Dim expected = "
Class C
    Private Const {|Rename:V|} As Integer = 3 + 3
    Public Property Foo()

    <Example(V)>
    Public Property Bar()
End Class
"
            Await TestInRegularAndScriptAsync(code, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28266")>
        Public Async Function TestCaretAtEndOfExpression1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub Goo()
        Bar(1[||], 2)
    end sub
end class",
"class C
    Private Const {|Rename:V|} As Integer = 1

    sub Goo()
        Bar(V, 2)
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28266")>
        Public Async Function TestCaretAtEndOfExpression2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub Goo()
        Bar(1, 2[||])
    end sub
end class",
"class C
    Private Const {|Rename:V|} As Integer = 2

    sub Goo()
        Bar(1, V)
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28266")>
        Public Async Function TestCaretAtEndOfExpression3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub Goo()
        Bar(1, (2[||]))
    end sub
end class",
"class C
    Private Const {|Rename:V|} As Integer = (2)

    sub Goo()
        Bar(1, V)
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28266")>
        Public Async Function TestCaretAtEndOfExpression4() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub Goo()
        Bar(1, Bar(2[||]))
    end sub
end class",
"class C
    Private Const {|Rename:V|} As Integer = 2

    sub Goo()
        Bar(1, Bar(V))
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27949")>
        Public Async Function TestWhitespaceSpanInAssignment() As Task
            Await TestMissingAsync("
Class C
    Dim x As Integer = [| |] 0
End Class
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28665")>
        Public Async Function TestWhitespaceSpanInAttribute() As Task
            Await TestMissingAsync("
Class C
    <Example( [| |] )>
    Public Function Foo()
    End Function
End Class
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30207")>
        Public Async Function TestExplicitRecursiveInstanceMemberAccess_ForAllOccurrences() As Task
            Dim source = "
Class C
    Dim c As C
    Sub Foo()
        Dim y = [|c|].c.c
    End Sub
End Class
"
            Dim expected = "
Class C
    Dim c As C
    Sub Foo()
        Dim {|Rename:c1|} As C = c
        Dim y = c1.c.c
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30207")>
        Public Async Function TestImplicitRecursiveInstanceMemberAccess_ForAllOccurrences() As Task
            Dim source = "
Class C
    Dim c As C
    Sub Foo()
        Dim y = [|Me.c|].c.c
    End Sub
End Class
"
            Dim expected = "
Class C
    Dim c As C
    Sub Foo()
        Dim {|Rename:c1|} As C = Me.c
        Dim y = c1.c.c
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30207")>
        Public Async Function TestExpressionOfUndeclaredType() As Task
            Dim source = "
Class C
    Sub Test        
        Dim array As A() = [|A|].Bar()
    End Sub
End Class"
            Await TestMissingAsync(source)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47772")>
        Public Async Function DoNotIntroduceConstantForConstant_Local() As Task
            Dim source = "
Class C
    Sub Test
        Const i As Integer = [|10|]
    End Sub
End Class
"
            Await TestMissingAsync(source)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47772")>
        Public Async Function DoNotIntroduceConstantForConstant_Member() As Task
            Dim source = "
Class C
    Const i As Integer = [|10|]
End Class
"
            Await TestMissingAsync(source)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47772")>
        Public Async Function DoNotIntroduceConstantForConstant_Parentheses() As Task
            Dim source = "
Class C
    Const i As Integer = ([|10|])
End Class
"
            Await TestMissingAsync(source)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47772")>
        Public Async Function DoNotIntroduceConstantForConstant_NotForSubExpression() As Task
            Dim source = "
Class C
    Sub Test
        Const i As Integer = [|10|] + 10
    End Sub
End Class
"
            Dim expected = "
Class C
    Sub Test
        Const {|Rename:V|} As Integer = 10
        Const i As Integer = V + 10
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(source, expected, index:=2)
        End Function
    End Class
End Namespace

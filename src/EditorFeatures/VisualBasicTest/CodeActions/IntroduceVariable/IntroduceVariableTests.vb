Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.IntroduceVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.IntroduceVariable
    Public Class IntroduceVariableTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New IntroduceVariableCodeRefactoringProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function Test1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Console.WriteLine([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n Console.WriteLine(V) \n End Sub \n End Module"),
index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function Test2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Console.WriteLine([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n Console.WriteLine(V) \n End Sub \n End Module"),
index:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfExpression1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo([|1 + 1|]) Then bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If foo(V) Then bar(1 + 1) \n End Sub \n End Module"),
index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfExpression2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo([|1 + 1|]) Then bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If foo(V) Then bar(V) \n End Sub \n End Module"),
index:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfStatement1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo(1 + 1) Then bar([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo(1 + 1) Then \n Const {|Rename:V|} As Integer = 1 + 1 \n bar(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInSingleLineIfStatement2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo(1 + 1) Then bar([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If foo(V) Then bar(V) \n End Sub \n End Module"),
index:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoIntroduceFieldOnMethodTypeParameter() As Task
            Dim source = NewLines("Module Program \n Sub Main(Of T)() \n Foo([|CType(2.ToString(), T)|]) \n End Sub \n End Module")
            Await TestExactActionSetOfferedAsync(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.IntroduceLocalFor, "CType(2.ToString(), T)"),
                    String.Format(FeaturesResources.IntroduceLocalForAllOccurrences, "CType(2.ToString(), T)")})

            ' Verifies "Introduce field ..." is missing
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoIntroduceFieldOnMethodParameter() As Task
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Foo([|x.ToString()|]) \n End Sub \n End Module")
            Await TestExactActionSetOfferedAsync(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.IntroduceLocalFor, "x.ToString()"),
                    String.Format(FeaturesResources.IntroduceLocalForAllOccurrences, "x.ToString()")})

            ' Verifies "Introduce field ..." is missing
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoRefactoringOnExpressionInAssignmentStatement() As Task
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Dim r = [|x.ToString()|] \n End Sub \n End Module")
            Await TestMissingAsync(source)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalGeneratedInInnerBlock1() As Task
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Foo([|x.ToString()|]) \n End If \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Dim {|Rename:v|} As String = x.ToString() \n Foo(v) \n End If \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalGeneratedInInnerBlock2() As Task
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Foo([|x.ToString()|]) \n End If \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Dim {|Rename:v|} As String = x.ToString() \n Foo(v) \n End If \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromSingleExpressionInAnonType() As Task
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Dim f1 = New With {.SomeString = [|x.ToString()|]} \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n Dim {|Rename:v|} As String = x.ToString() \n Dim f1 = New With {.SomeString = v} \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromMultipleExpressionsInAnonType() As Task
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Dim f1 = New With {.SomeString = [|x.ToString()|], .SomeOtherString = x.ToString()} \n Dim f2 = New With {.SomeString = x.ToString(), .SomeOtherString = x.ToString()} \n Dim str As String = x.ToString() \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n Dim {|Rename:v|} As String = x.ToString() \n Dim f1 = New With {.SomeString = v, .SomeOtherString = v} \n Dim f2 = New With {.SomeString = v, .SomeOtherString = v} \n Dim str As String = v \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=1)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromInferredFieldInitializer() As Task
            Dim source = NewLines("Imports System \n Class C \n Sub M() \n Dim a As New With {[|Environment.TickCount|]} \n End Sub \n End Class")
            Dim expected = NewLines("Imports System \n Class C \n Sub M() \n Dim {|Rename:tickCount|} As Integer = Environment.TickCount \n Dim a As New With {tickCount} \n End Sub \n End Class")
            Await TestAsync(source, expected, index:=1)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromYieldStatement() As Task
            Dim source = NewLines("Imports System \n Class C \n Iterator Function F() As IEnumerable(Of Integer) \n Yield [|Environment.TickCount * 2|] \n End Function \n End Class")
            Dim expected = NewLines("Imports System \n Class C \n Iterator Function F() As IEnumerable(Of Integer) \n Dim {|Rename:v|} As Integer = Environment.TickCount * 2 \n Yield v \n End Function \n End Class")
            Await TestAsync(source, expected, index:=1)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromWhileStatement() As Task
            Dim source = NewLines("Class C \n Sub M() \n Dim x = 1 \n While [|x = 1|] \n End While \n End Sub \n End Class")
            Dim expected = NewLines("Class C \n Sub M() \n Dim x = 1 \n Dim {|Rename:v|} As Boolean = x = 1 \n While v \n End While \n End Sub \n End Class")
            Await TestAsync(source, expected, index:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestLocalFromSingleExpressionInObjectInitializer() As Task
            Dim source = NewLines("Module Program \n Structure FooStruct \n Dim FooMember1 As String \n End Structure \n Sub Main(x As Integer) \n Dim f1 = New FooStruct With {.FooMember1 = [|""t"" + ""test""|]} \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Structure FooStruct \n Dim FooMember1 As String \n End Structure \n Sub Main(x As Integer) \n Const {|Rename:V|} As String = ""t"" + ""test"" \n Dim f1 = New FooStruct With {.FooMember1 = V} \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFieldFromMultipleExpressionsInAnonType() As Task
            Dim source = NewLines("Class Program \n Dim q = New With {.str = [|""t"" + ""test""|]} \n Dim r = New With {.str = ""t"" + ""test""} \n Sub Foo() \n Dim x = ""t"" + ""test"" \n End Sub \n End Class")
            Dim expected = NewLines("Class Program \n Private Const {|Rename:V|} As String = ""t"" + ""test"" \n Dim q = New With {.str = V} \n Dim r = New With {.str = V} \n Sub Foo() \n Dim x = V \n End Sub \n End Class")
            Await TestAsync(source, expected, index:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestPrivateFieldFromExpressionInField() As Task
            Dim source = NewLines("Class Program \n Dim x = Foo([|2 + 2|]) \n End Class")
            Dim expected = NewLines("Class Program \n Private Const {|Rename:V|} As Integer = 2 + 2 \n Dim x = Foo(V) \n End Class")
            Await TestAsync(source, expected, index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoLocalFromExpressionInField() As Task
            Dim source = NewLines("Class Program \n Dim x = Foo([|2 + 2|]) \n End Class")
            Await TestExactActionSetOfferedAsync(source, {String.Format(FeaturesResources.IntroduceConstantFor, "2 + 2"), String.Format(FeaturesResources.IntroduceConstantForAllOccurrences, "2 + 2")})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSharedModifierAbsentInGeneratedModuleFields() As Task
            Dim source = NewLines("Module Program \n Dim x = Foo([|2 + y|]) \n End Module")
            Dim expected = NewLines("Module Program \n Private ReadOnly {|Rename:p|} As Object = 2 + y \n Dim x = Foo(p) \n End Module")
            Await TestAsync(source, expected, index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLocalInsertLocation() As Task
            Dim source = NewLines("Class Program \n Sub Method1() \n Dim v1 As String = ""TEST"" \n Dim v2 As Integer = 2 + 2 \n Foo([|2 + 2|]) \n End Sub \n End Class")
            Dim expected = NewLines("Class Program \n Sub Method1() \n Dim v1 As String = ""TEST"" \n Dim v2 As Integer = 2 + 2 \n Const {|Rename:V|} As Integer= 2 + 2 \n Foo(V) \n End Sub \n End Class")
            Await TestAsync(source, expected, index:=2)
        End Function

#Region "Parameter context"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestConstantFieldGenerationForParameterSingleOccurrence() As Task
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = NewLines("Module Module1 \n Sub Foo(Optional x As Integer = [|42|]) \n End Sub \n End Module")
            Dim expected = NewLines("Module Module1 \n Private Const {|Rename:V|} As Integer = 42 \n Sub Foo(Optional x As Integer = V) \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestConstantFieldGenerationForParameterAllOccurrences() As Task
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = NewLines("Module Module1 \n Sub Bar(Optional x As Integer = 42) \n End Sub \n Sub Foo(Optional x As Integer = [|42|]) \n End Sub \n End Module")
            Dim expected = NewLines("Module Module1 \n Private Const {|Rename:V|} As Integer = 42 \n Sub Bar(Optional x As Integer = V) \n End Sub \n Sub Foo(Optional x As Integer = V) \n End Sub \n End Module")
            Await TestAsync(source, expected, index:=1)
        End Function

#End Region

        <WorkItem(540269)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceDottedExpression() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Console.WriteLine([|Foo.someVariable|]) \n Console.WriteLine(Foo.someVariable) \n End Sub \n End Module \n Friend Class Foo \n Shared Public someVariable As Integer \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim {|Rename:someVariable|} As Integer = Foo.someVariable \n Console.WriteLine(someVariable) \n Console.WriteLine(someVariable) \n End Sub \n End Module \n Friend Class Foo \n Shared Public someVariable As Integer \n End Class"),
index:=1)
        End Function

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|2 + 2|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Const {|Rename:V|} As Integer = 2 + 2 \n Foo(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|1 + 1|]) Else Bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Const {|Rename:V|} As Integer = 1 + 1 \n Foo(V) \n Else \n Bar(1 + 1) \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine3() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|1 + 1|]) Else Bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If True Then Foo(V) Else Bar(V) \n End Sub \n End Module"),
index:=3)
        End Function

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReplaceSingleLineIfWithMultiLine4() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo(1 + 1) Else Bar([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Foo(1 + 1) \n Else \n Const {|Rename:V|} As Integer = 1 + 1 \n Bar(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WorkItem(540468)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCantExtractMethodTypeParameterToFieldCount() As Task
            Await TestActionCountAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(Of T)(x As Integer) \n Foo([|CType(2.ToString(), T)|]) \n End Sub \n End Module"),
count:=2)
        End Function

        <WorkItem(540468)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCantExtractMethodTypeParameterToField() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(Of T)(x As Integer) \n Foo([|CType(2.ToString(), T)|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(Of T)(x As Integer) \n Dim {|Rename:t1|} As T = CType(2.ToString(), T) \n Foo(t1) \n End Sub \n End Module"))
        End Function

        <WorkItem(540489)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestOnlyFieldsInsideConstructorInitializer() As Task
            Await TestActionCountAsync(
NewLines("Class Foo \n Sub New() \n Me.New([|2 + 2|]) \n End Sub \n Sub New(v As Integer) \n End Sub \n End Class"),
count:=2)

            Await TestAsync(
NewLines("Class Foo \n Sub New() \n Me.New([|2 + 2|]) \n End Sub \n Sub New(v As Integer) \n End Sub \n End Class"),
NewLines("Class Foo \n Private Const {|Rename:V|} As Integer = 2 + 2 \n Sub New() \n Me.New(V) \n End Sub \n Sub New(v As Integer) \n End Sub \n End Class"),
index:=0)
        End Function

        <WorkItem(540485)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceLocalForConstantExpression() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim s As String() = New String([|10|]) {} \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 10 \n Dim s As String() = New String(V) {} \n End Sub \n End Module"),
index:=3)
        End Function

        <WorkItem(1065689)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(540487)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(540491)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInAttribute1() As Task
            Await TestAsync(
NewLines("<Attr([|2 + 2|])> \n Class Foo \n End Class \n Friend Class AttrAttribute \n Inherits Attribute \n End Class"),
NewLines("<Attr(Foo.V)> \n Class Foo \n Friend Const {|Rename:V|} As Integer = 2 + 2 \n End Class \n Friend Class AttrAttribute \n Inherits Attribute \n End Class"),
index:=0)
        End Function

        <WorkItem(540490)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInMyClassNew() As Task
            Await TestAsync(
NewLines("Class Foo \n Sub New() \n MyClass.New([|42|]) \n End Sub \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("Class Foo \n Private Const {|Rename:V|} As Integer = 42 \n Sub New() \n MyClass.New(V) \n End Sub \n Sub New(x As Integer) \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|2 + 2|]) Else Bar(2 + 2) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Const {|Rename:V|} As Integer = 2 + 2 \n Foo(V) \n Else \n Bar(2 + 2) \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|2 + 2|]) Else Bar(2 + 2) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 2 + 2 \n If True Then Foo(V) Else Bar(V) \n End Sub \n End Module"),
index:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf3() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo(2 + 2) Else Bar([|2 + 2|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Foo(2 + 2) \n Else \n Const {|Rename:V|} As Integer = 2 + 2 \n Bar(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleToMultiLineIf4() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo(2 + 2) Else Bar([|2 + 2|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 2 + 2 \n If True Then Foo(V) Else Bar(V) \n End Sub \n End Module"),
index:=3)
        End Function

        <WorkItem(541604)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestAttribute() As Task
            Await TestAsync(
NewLines("<Attr([|2 + 2|])> \n Class Foo \n End Class \n Friend Class AttrAttribute \n Inherits System.Attribute \n End Class"),
NewLines("<Attr(Foo.V)> \n Class Foo \n Friend Const {|Rename:V|} As Integer = 2 + 2 \n End Class \n Friend Class AttrAttribute \n Inherits System.Attribute \n End Class"),
index:=0)
        End Function

        <WorkItem(542092)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestRangeArgumentLowerBound1() As Task
            Await TestMissingAsync(NewLines("Module M \n Sub Main() \n Dim x() As Integer \n ReDim x([|0|] To 5) \n End Sub \n End Module"))
        End Function

        <WorkItem(542092)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(543029), WorkItem(542963), WorkItem(542295)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestUntypedExpression() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim q As Object \n If True Then q = [|Sub() \n End Sub|] \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim q As Object \n If True Then \n Dim {|Rename:p|} As Object = Sub() \n End Sub \n q = p \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(542374)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFieldConstantInAttribute1() As Task
            Await TestAsync(
NewLines("<Foo(2 + 3 + 4)> \n Module Program \n Dim x = [|2 + 3|] + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("<Foo(2 + 3 + 4)> \n Module Program \n Private Const {|Rename:V|} As Integer = 2 + 3 \n Dim x = V + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
index:=0)
        End Function

        <WorkItem(542374)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFieldConstantInAttribute2() As Task
            Await TestAsync(
NewLines("<Foo(2 + 3 + 4)> \n Module Program \n Dim x = [|2 + 3|] + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("<Foo(V + 4)> \n Module Program \n Friend Const {|Rename:V|} As Integer = 2 + 3 \n Dim x = V + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
index:=1,
parseOptions:=Nothing)
        End Function

        <WorkItem(542783)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnAttributeName() As Task
            Await TestMissingAsync(
NewLines("<[|Obsolete|]> \n Class C \n End Class"))
        End Function

        <WorkItem(542811)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnFilterClause() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n Try \n Catch ex As Exception When [|+|] \n End Try \n End Sub \n End Module"))
        End Function

        <WorkItem(542906)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoIntroduceLocalInAttribute() As Task
            Dim input =
"Module Program \n <Obsolete([|""""|])> \n Sub Main(args As String()) \n End Sub \n End Module"

            Await TestActionCountAsync(
NewLines(input),
count:=2)

            Await TestAsync(
NewLines(input),
NewLines("Module Program \n Private Const {|Rename:V|} As String = """" \n <Obsolete(V)> \n Sub Main(args As String()) \n End Sub \n End Module"),
index:=0)
        End Function

        <WorkItem(542947)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotOnMyBase() As Task
            Await TestMissingAsync(
NewLines("Class c1 \n Public res As String \n Sub Foo() \n res = ""1"" \n End Sub \n End Class \n Class c2 \n Inherits c1 \n Sub scen1() \n [|MyBase|].Foo() \n End Sub \n End Class"))
        End Function

        <WorkItem(541966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(541966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(541966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda1() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) Console.WriteLine([|x + 1|]) ' Introduce local \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n Dim {|Rename:v|} As Integer = x + 1 \n Console.WriteLine(v) ' Introduce local \n End Sub \n End Sub \n End Module"),
index:=0)
        End Function

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda2() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) If True Then Console.WriteLine([|x + 1|]) Else Console.WriteLine() \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n If True Then \n Dim {|Rename:v|} As Integer = x + 1 \n Console.WriteLine(v) \n Else \n Console.WriteLine() \n End If \n End Sub \n End Sub \n End Module"),
index:=0)
        End Function

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda3() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) If True Then Console.WriteLine() Else Console.WriteLine([|x + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n If True Then \n Console.WriteLine() \n Else \n Dim {|Rename:v|} As Integer = x + 1 \n Console.WriteLine(v) \n End If \n End Sub \n End Sub \n End Module"),
index:=0)
        End Function

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda4() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) If True Then Console.WriteLine([|x + 1|]) Else Console.WriteLine(x + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n Dim {|Rename:v|} As Integer = x + 1 \n If True Then Console.WriteLine(v) Else Console.WriteLine(v) \n End Sub \n End Sub \n End Module"),
index:=1)
        End Function

        <WorkItem(543299)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestSingleLineLambda5() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim query = Sub(a) a = New With {Key .Key = Function(ByVal arg As Integer) As Integer \n Return arg \n End Function}.Key.Invoke([|a Or a|]) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim query = Sub(a) \n Dim {|Rename:v|} As Object = a Or a \n a = New With {Key .Key = Function(ByVal arg As Integer) As Integer \n Return arg \n End Function}.Key.Invoke(v) \n End Sub \n End Sub \n End Module"),
index:=0)
        End Function

        <WorkItem(542762)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotInIntoClause() As Task
            Await TestMissingAsync(
NewLines("Imports System.Linq \n Module \n Sub Main() \n Dim x = Aggregate y In New Integer() {1} \n Into [|Count()|] \n End Sub \n End Module"))
        End Function

        <WorkItem(543289)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotOnAttribute1() As Task
            Await TestMissingAsync(
NewLines("Option Explicit Off \n Module Program \n <Runtime.CompilerServices.[|Extension|]()> _ \n Function Extension(ByVal x As Integer) As Integer \n Return x \n End Function \n End Module"))
        End Function

        <WorkItem(543289)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNotOnAttribute2() As Task
            Await TestMissingAsync(
NewLines("Option Explicit Off \n Module Program \n <Runtime.CompilerServices.[|Extension()|]> _ \n Function Extension(ByVal x As Integer) As Integer \n Return x \n End Function \n End Module"))
        End Function

        <WorkItem(543461)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCollectionInitializer() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim i1 = New Integer() [|{4, 5}|] \n End Sub \n End Module"))
        End Function

        <WorkItem(543573)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestCaseInsensitiveNameConflict() As Task
            Await TestAsync(
NewLines("Class M \n Public Function Foo() \n Return [|Me.Foo|] * 0 \n End Function \n End Class"),
NewLines("Class M \n Public Function Foo() \n Dim {|Rename:foo1|} As Object = Me.Foo \n Return foo1 * 0 \n End Function \n End Class"))
        End Function

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery1() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Select v \n Return 0 \n End Function \n End Class"))
        End Function

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQueryCount1() As Task
            Await TestActionCountAsync(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
count:=2)
        End Function

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery2() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where [|Sample(s)|] > 21 \n Select Sample(s) \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Where v > 21 \n Select Sample(s) \n Return 0 \n End Function \n End Class"))
        End Function

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery3() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where [|Sample(s)|] > 21 \n Select Sample(s) \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Where v > 21 \n Select v \n Return 0 \n End Function \n End Class"),
index:=1)
        End Function

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery4() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where Sample(s) > 21 \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where Sample(s) > 21 Let {|Rename:v|} = Sample(s) \n Select v \n Return 0 \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestQuery5() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where Sample(s) > 21 \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Where v > 21 \n Select v \n Return 0 \n End Function \n End Class"),
index:=1)
        End Function

        <WorkItem(543529)>
        <WorkItem(909152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInStatementlessConstructorParameter() As Task
            Await TestMissingAsync(NewLines("Class C1 \n Sub New(Optional ByRef x As String = [|Nothing|]) \n End Sub \n End Class"))
        End Function

        <WorkItem(543650)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestReferenceToAnonymousTypeProperty() As Task
            Await TestMissingAsync(
NewLines("Class AM \n Sub M(args As String()) \n Dim var1 As New AM \n Dim at1 As New With {var1, .friend = [|.var1|]} \n End Sub \n End Class"))
        End Function

        <WorkItem(543698)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntegerArrayExpression() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n Return [|New Integer() {}|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n Dim {|Rename:v|} As Integer() = New Integer() {} \n Return v \n End Sub \n End Module"))
        End Function

        <WorkItem(544273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestAttributeNamedParameter() As Task
            Await TestMissingAsync(
NewLines("Class TestAttribute \n Inherits Attribute \n Public Sub New(Optional a As Integer = 42) \n End Sub \n End Class \n <Test([|a|]:=5)> \n Class Foo \n End Class"))
        End Function

        <WorkItem(544265)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestMissingOnWrittenToExpression() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n Dim x = New Integer() {1, 2} \n [|x(1)|] = 2 \n End Sub \n End Module"))
        End Function

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess1() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Public Class C1 \n Public FieldInt As Long \n Public FieldStr As String \n Public Property PropInt As Integer \n End Class \n Public Class C2 \n Public Shared Sub Main() \n Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt|].ToString()} \n End Sub \n End Class"))
        End Function

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess2() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Public Class C1 \n Public FieldInt As Long \n Public FieldStr As String \n Public Property PropInt As Integer \n End Class \n Public Class C2 \n Public Shared Sub Main() \n Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt.ToString|]()} \n End Sub \n End Class"))
        End Function

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestImplicitMemberAccess3() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Public Class C1 \n Public FieldInt As Long \n Public FieldStr As String \n Public Property PropInt As Integer \n End Class \n Public Class C2 \n Public Shared Sub Main() \n Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt.ToString()|]} \n End Sub \n End Class"))
        End Function

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(529510)>
        <WpfFact(Skip:="529510"), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestNoRefactoringOnAddressOfExpression() As Task
            Dim source = NewLines("Imports System \n Module Module1 \n Public Sub Foo(ByVal a1 As Exception) \n End Sub \n Public Sub foo(ByVal a1 As Action(Of ArgumentException)) \n End Sub \n Sub Main() \n Foo(New Action(Of Exception)([|AddressOf Foo|])) \n End Sub \n End Module")
            Await TestMissingAsync(source)
        End Function

        <WorkItem(529510)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
        Public Async Function TestMissingOnAddressOfInDelegate() As Task
            Await TestMissingAsync(
NewLines("Module Module1 \n Public Sub Foo(ByVal a1 As Exception) \n End Sub \n Public Sub foo(ByVal a1 As Action(Of ArgumentException)) \n End Sub \n Sub Main() \n foo(New Action(Of Exception)([|AddressOf Foo|])) \n End Sub \n End Module"))
        End Function

        <WorkItem(545168)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
        Public Async Function TestMissingOnXmlName() As Task
            Await TestMissingAsync(
NewLines("Module M \n Sub Main() \n Dim x = <[|x|]/> \n End Sub \n End Module"))
        End Function

        <WorkItem(545262)>
        <WorkItem(909152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInTernaryConditional() As Task
            Await TestMissingAsync(NewLines("Module Program \n Sub Main(args As String()) \n Dim p As Object = Nothing \n Dim Obj1 = If(New With {.a = True}.a, p, [|Nothing|]) \n End Sub \n End Module"))
        End Function

        <WorkItem(545316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInPropertyInitializer() As Task
            Await TestAsync(
NewLines("Module Module1 \n Property Prop As New List(Of String) From {[|""One""|], ""two""} \n End Module"),
NewLines("Module Module1 \n Private Const {|Rename:V|} As String = ""One"" \n Property Prop As New List(Of String) From {V, ""two""} \n End Module"))
        End Function

        <WorkItem(545308)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(545258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
                                      {String.Format(FeaturesResources.IntroduceLocalConstantFor, "5"),
                                       String.Format(FeaturesResources.IntroduceLocalConstantForAll, "5")})

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
                                      {String.Format(FeaturesResources.IntroduceLocalConstantFor, "5"),
                                       String.Format(FeaturesResources.IntroduceLocalConstantForAll, "5")})
        End Function

        <WorkItem(545258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
                                      {String.Format(FeaturesResources.IntroduceLocalConstantFor, "5"),
                                       String.Format(FeaturesResources.IntroduceLocalConstantForAll, "5")})

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545525)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInvocation() As Task
            Await TestAsync(
NewLines("Option Strict On \n  \n Class C \n Shared Sub Main() \n Dim x = [|New C().Foo()|](0) \n End Sub \n Function Foo() As Integer() \n End Function \n End Class"),
NewLines("Option Strict On \n  \n Class C \n Shared Sub Main() \n Dim {|Rename:v|} As Integer() = New C().Foo() \n Dim x = v(0) \n End Sub \n Function Foo() As Integer() \n End Function \n End Class"))
        End Function

        <WorkItem(545829)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestOnImplicitMemberAccess() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n With """" \n Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n With """" \n Dim {|Rename:getHashCode|} As Integer = .GetHashCode \n Dim x = getHashCode Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
parseOptions:=Nothing)

            Await TestAsync(
NewLines("Module Program \n Sub Main() \n With """" \n Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n With """" \n Dim {|Rename:getHashCode1|} As Integer = .GetHashCode \n Dim x = getHashCode1 Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
parseOptions:=GetScriptOptions())
        End Function

        <WorkItem(545702)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(546139)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestAcrossPartialTypes() As Task
            Await TestAsync(
NewLines("Partial Class C \n Sub foo1(Optional x As String = [|""HELLO""|]) \n End Sub \n End Class \n Partial Class C \n Sub foo3(Optional x As String = ""HELLO"") \n End Sub \n End Class"),
NewLines("Partial Class C \n Private Const {|Rename:V|} As String = ""HELLO"" \n Sub foo1(Optional x As String = V) \n End Sub \n End Class \n Partial Class C \n Sub foo3(Optional x As String = V) \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(544669)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestFunctionBody1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim a1 = Function(ByVal x) [|x!foo|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim a1 = Function(ByVal x) \n Dim {|Rename:foo|} As Object = x!foo \n Return foo \n End Function \n End Sub \n End Module"))
        End Function

        <WorkItem(1065689)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(546815)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestInIfStatement() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If [|True|] Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private Const {|Rename:V|} As Boolean = True \n Sub Main(args As String()) \n If V Then \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(830928)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceLocalRemovesUnnecessaryCast() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class C \n Private Shared Sub Main(args As String()) \n Dim hSet = New HashSet(Of String)() \n hSet.Add([|hSet.ToString()|]) \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Class C \n Private Shared Sub Main(args As String()) \n Dim hSet = New HashSet(Of String)() \n Dim {|Rename:v|} As String = hSet.ToString() \n hSet.Add(v) \n End Sub \n End Class"))
        End Function

        <WorkItem(546691)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(530720)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(531478)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(632327)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(632327)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(682683)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(1022458)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(939259)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(909152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(1130990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(1130990)>
        <WorkItem(3110, "https://github.com/dotnet/roslyn/issues/3110")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(1130990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(1130990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

        <WorkItem(1065661)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestIntroduceVariableTextDoesntSpanLines() As Task
            Dim code = "
Class C
    Sub M()
        Dim s = """" + [|""a

b
c""|]
    End Sub
End Class"
            Await TestSmartTagTextAsync(code, String.Format(FeaturesResources.IntroduceLocalConstantFor, """a b c"""), index:=2)
        End Function

        <WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Async Function TestHandleFormattableStringTargetTyping1() As Task
            Const code = "
Imports System

" & FormattableStringType & "

Namespace N
    Class C
Public Async Function TestM() As Task
            Dim f = FormattableString.Invariant([|$""""|])
        End Sub
    End Class
End Namespace"

            Const expected = "
Imports System

" & FormattableStringType & "

Namespace N
    Class C
Public Async Function TestM() As Task
            Dim {|Rename:v|} As FormattableString = $""""
            Dim f = FormattableString.Invariant(v)
        End Sub
    End Class
End Namespace"

            Await TestAsync(code, expected, index:=0, compareTokens:=False)
        End Function

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
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

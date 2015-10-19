' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.IntroduceVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.IntroduceVariable
    Public Class IntroduceVariableTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New IntroduceVariableCodeRefactoringProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub Test1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Console.WriteLine([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n Console.WriteLine(V) \n End Sub \n End Module"),
index:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub Test2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Console.WriteLine([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n Console.WriteLine(V) \n End Sub \n End Module"),
index:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInSingleLineIfExpression1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo([|1 + 1|]) Then bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If foo(V) Then bar(1 + 1) \n End Sub \n End Module"),
index:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInSingleLineIfExpression2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo([|1 + 1|]) Then bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If foo(V) Then bar(V) \n End Sub \n End Module"),
index:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInSingleLineIfStatement1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo(1 + 1) Then bar([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo(1 + 1) Then \n Const {|Rename:V|} As Integer = 1 + 1 \n bar(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInSingleLineIfStatement2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If foo(1 + 1) Then bar([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If foo(V) Then bar(V) \n End Sub \n End Module"),
index:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoIntroduceFieldOnMethodTypeParameter()
            Dim source = NewLines("Module Program \n Sub Main(Of T)() \n Foo([|CType(2.ToString(), T)|]) \n End Sub \n End Module")
            TestExactActionSetOffered(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.IntroduceLocalFor, "CType(2.ToString(), T)"),
                    String.Format(FeaturesResources.IntroduceLocalForAllOccurrences, "CType(2.ToString(), T)")})

            ' Verifies "Introduce field ..." is missing
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoIntroduceFieldOnMethodParameter()
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Foo([|x.ToString()|]) \n End Sub \n End Module")
            TestExactActionSetOffered(
                source,
                expectedActionSet:={
                    String.Format(FeaturesResources.IntroduceLocalFor, "x.ToString()"),
                    String.Format(FeaturesResources.IntroduceLocalForAllOccurrences, "x.ToString()")})

            ' Verifies "Introduce field ..." is missing
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoRefactoringOnExpressionInAssignmentStatement()
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Dim r = [|x.ToString()|] \n End Sub \n End Module")
            TestMissing(source)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalGeneratedInInnerBlock1()
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Foo([|x.ToString()|]) \n End If \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Dim {|Rename:v|} As String = x.ToString() \n Foo(v) \n End If \n End Sub \n End Module")
            Test(source, expected, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalGeneratedInInnerBlock2()
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Foo([|x.ToString()|]) \n End If \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n If True Then \n Dim {|Rename:v|} As String = x.ToString() \n Foo(v) \n End If \n End Sub \n End Module")
            Test(source, expected, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromSingleExpressionInAnonType()
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Dim f1 = New With {.SomeString = [|x.ToString()|]} \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n Dim {|Rename:v|} As String = x.ToString() \n Dim f1 = New With {.SomeString = v} \n End Sub \n End Module")
            Test(source, expected, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromMultipleExpressionsInAnonType()
            Dim source = NewLines("Module Program \n Sub Main(x As Integer) \n Dim f1 = New With {.SomeString = [|x.ToString()|], .SomeOtherString = x.ToString()} \n Dim f2 = New With {.SomeString = x.ToString(), .SomeOtherString = x.ToString()} \n Dim str As String = x.ToString() \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Sub Main(x As Integer) \n Dim {|Rename:v|} As String = x.ToString() \n Dim f1 = New With {.SomeString = v, .SomeOtherString = v} \n Dim f2 = New With {.SomeString = v, .SomeOtherString = v} \n Dim str As String = v \n End Sub \n End Module")
            Test(source, expected, index:=1)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromInferredFieldInitializer()
            Dim source = NewLines("Imports System \n Class C \n Sub M() \n Dim a As New With {[|Environment.TickCount|]} \n End Sub \n End Class")
            Dim expected = NewLines("Imports System \n Class C \n Sub M() \n Dim {|Rename:tickCount|} As Integer = Environment.TickCount \n Dim a As New With {tickCount} \n End Sub \n End Class")
            Test(source, expected, index:=1)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromYieldStatement()
            Dim source = NewLines("Imports System \n Class C \n Iterator Function F() As IEnumerable(Of Integer) \n Yield [|Environment.TickCount * 2|] \n End Function \n End Class")
            Dim expected = NewLines("Imports System \n Class C \n Iterator Function F() As IEnumerable(Of Integer) \n Dim {|Rename:v|} As Integer = Environment.TickCount * 2 \n Yield v \n End Function \n End Class")
            Test(source, expected, index:=1)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromWhileStatement()
            Dim source = NewLines("Class C \n Sub M() \n Dim x = 1 \n While [|x = 1|] \n End While \n End Sub \n End Class")
            Dim expected = NewLines("Class C \n Sub M() \n Dim x = 1 \n Dim {|Rename:v|} As Boolean = x = 1 \n While v \n End While \n End Sub \n End Class")
            Test(source, expected, index:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromSingleExpressionInObjectInitializer()
            Dim source = NewLines("Module Program \n Structure FooStruct \n Dim FooMember1 As String \n End Structure \n Sub Main(x As Integer) \n Dim f1 = New FooStruct With {.FooMember1 = [|""t"" + ""test""|]} \n End Sub \n End Module")
            Dim expected = NewLines("Module Program \n Structure FooStruct \n Dim FooMember1 As String \n End Structure \n Sub Main(x As Integer) \n Const {|Rename:V|} As String = ""t"" + ""test"" \n Dim f1 = New FooStruct With {.FooMember1 = V} \n End Sub \n End Module")
            Test(source, expected, index:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestLocalFromMultipleExpressionsInObjectInitializer()
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

            Test(code, expected, index:=3, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestFieldFromMultipleExpressionsInAnonType()
            Dim source = NewLines("Class Program \n Dim q = New With {.str = [|""t"" + ""test""|]} \n Dim r = New With {.str = ""t"" + ""test""} \n Sub Foo() \n Dim x = ""t"" + ""test"" \n End Sub \n End Class")
            Dim expected = NewLines("Class Program \n Private Const {|Rename:V|} As String = ""t"" + ""test"" \n Dim q = New With {.str = V} \n Dim r = New With {.str = V} \n Sub Foo() \n Dim x = V \n End Sub \n End Class")
            Test(source, expected, index:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestPrivateFieldFromExpressionInField()
            Dim source = NewLines("Class Program \n Dim x = Foo([|2 + 2|]) \n End Class")
            Dim expected = NewLines("Class Program \n Private Const {|Rename:V|} As Integer = 2 + 2 \n Dim x = Foo(V) \n End Class")
            Test(source, expected, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoLocalFromExpressionInField()
            Dim source = NewLines("Class Program \n Dim x = Foo([|2 + 2|]) \n End Class")
            TestExactActionSetOffered(source, {String.Format(FeaturesResources.IntroduceConstantFor, "2 + 2"), String.Format(FeaturesResources.IntroduceConstantForAllOccurrences, "2 + 2")})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSharedModifierAbsentInGeneratedModuleFields()
            Dim source = NewLines("Module Program \n Dim x = Foo([|2 + y|]) \n End Module")
            Dim expected = NewLines("Module Program \n Private ReadOnly {|Rename:p|} As Object = 2 + y \n Dim x = Foo(p) \n End Module")
            Test(source, expected, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleLocalInsertLocation()
            Dim source = NewLines("Class Program \n Sub Method1() \n Dim v1 As String = ""TEST"" \n Dim v2 As Integer = 2 + 2 \n Foo([|2 + 2|]) \n End Sub \n End Class")
            Dim expected = NewLines("Class Program \n Sub Method1() \n Dim v1 As String = ""TEST"" \n Dim v2 As Integer = 2 + 2 \n Const {|Rename:V|} As Integer= 2 + 2 \n Foo(V) \n End Sub \n End Class")
            Test(source, expected, index:=2)
        End Sub

#Region "Parameter context"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestConstantFieldGenerationForParameterSingleOccurrence()
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = NewLines("Module Module1 \n Sub Foo(Optional x As Integer = [|42|]) \n End Sub \n End Module")
            Dim expected = NewLines("Module Module1 \n Private Const {|Rename:V|} As Integer = 42 \n Sub Foo(Optional x As Integer = V) \n End Sub \n End Module")
            Test(source, expected, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestConstantFieldGenerationForParameterAllOccurrences()
            ' This is incorrect: the field type should be Integer, not Object
            Dim source = NewLines("Module Module1 \n Sub Bar(Optional x As Integer = 42) \n End Sub \n Sub Foo(Optional x As Integer = [|42|]) \n End Sub \n End Module")
            Dim expected = NewLines("Module Module1 \n Private Const {|Rename:V|} As Integer = 42 \n Sub Bar(Optional x As Integer = V) \n End Sub \n Sub Foo(Optional x As Integer = V) \n End Sub \n End Module")
            Test(source, expected, index:=1)
        End Sub

#End Region

        <WorkItem(540269)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReplaceDottedExpression()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Console.WriteLine([|Foo.someVariable|]) \n Console.WriteLine(Foo.someVariable) \n End Sub \n End Module \n Friend Class Foo \n Shared Public someVariable As Integer \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim {|Rename:someVariable|} As Integer = Foo.someVariable \n Console.WriteLine(someVariable) \n Console.WriteLine(someVariable) \n End Sub \n End Module \n Friend Class Foo \n Shared Public someVariable As Integer \n End Class"),
index:=1)
        End Sub

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReplaceSingleLineIfWithMultiLine1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|2 + 2|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Const {|Rename:V|} As Integer = 2 + 2 \n Foo(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Sub

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReplaceSingleLineIfWithMultiLine2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|1 + 1|]) Else Bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Const {|Rename:V|} As Integer = 1 + 1 \n Foo(V) \n Else \n Bar(1 + 1) \n End If \n End Sub \n End Module"),
index:=2)
        End Sub

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReplaceSingleLineIfWithMultiLine3()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|1 + 1|]) Else Bar(1 + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 1 + 1 \n If True Then Foo(V) Else Bar(V) \n End Sub \n End Module"),
index:=3)
        End Sub

        <WorkItem(540457)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReplaceSingleLineIfWithMultiLine4()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo(1 + 1) Else Bar([|1 + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Foo(1 + 1) \n Else \n Const {|Rename:V|} As Integer = 1 + 1 \n Bar(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Sub

        <WorkItem(540468)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestCantExtractMethodTypeParameterToFieldCount()
            TestActionCount(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(Of T)(x As Integer) \n Foo([|CType(2.ToString(), T)|]) \n End Sub \n End Module"),
count:=2)
        End Sub

        <WorkItem(540468)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestCantExtractMethodTypeParameterToField()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(Of T)(x As Integer) \n Foo([|CType(2.ToString(), T)|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(Of T)(x As Integer) \n Dim {|Rename:t1|} As T = CType(2.ToString(), T) \n Foo(t1) \n End Sub \n End Module"))
        End Sub

        <WorkItem(540489)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestOnlyFieldsInsideConstructorInitializer()
            TestActionCount(
NewLines("Class Foo \n Sub New() \n Me.New([|2 + 2|]) \n End Sub \n Sub New(v As Integer) \n End Sub \n End Class"),
count:=2)

            Test(
NewLines("Class Foo \n Sub New() \n Me.New([|2 + 2|]) \n End Sub \n Sub New(v As Integer) \n End Sub \n End Class"),
NewLines("Class Foo \n Private Const {|Rename:V|} As Integer = 2 + 2 \n Sub New() \n Me.New(V) \n End Sub \n Sub New(v As Integer) \n End Sub \n End Class"),
index:=0)
        End Sub

        <WorkItem(540485)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroduceLocalForConstantExpression()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim s As String() = New String([|10|]) {} \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 10 \n Dim s As String() = New String(V) {} \n End Sub \n End Module"),
index:=3)
        End Sub

        <WorkItem(1065689)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroduceLocalForConstantExpressionWithTrailingTrivia()
            Test(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroduceFieldWithTrailingTrivia()
            Test(
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
        End Sub

        <WorkItem(540487)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestFormattingForPartialExpression()
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

            Test(code, expected, index:=2, compareTokens:=False)
        End Sub

        <WorkItem(540491)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInAttribute1()
            Test(
NewLines("<Attr([|2 + 2|])> \n Class Foo \n End Class \n Friend Class AttrAttribute \n Inherits Attribute \n End Class"),
NewLines("<Attr(Foo.V)> \n Class Foo \n Friend Const {|Rename:V|} As Integer = 2 + 2 \n End Class \n Friend Class AttrAttribute \n Inherits Attribute \n End Class"),
index:=0)
        End Sub

        <WorkItem(540490)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInMyClassNew()
            Test(
NewLines("Class Foo \n Sub New() \n MyClass.New([|42|]) \n End Sub \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("Class Foo \n Private Const {|Rename:V|} As Integer = 42 \n Sub New() \n MyClass.New(V) \n End Sub \n Sub New(x As Integer) \n End Sub \n End Class"),
index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleToMultiLineIf1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|2 + 2|]) Else Bar(2 + 2) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Const {|Rename:V|} As Integer = 2 + 2 \n Foo(V) \n Else \n Bar(2 + 2) \n End If \n End Sub \n End Module"),
index:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleToMultiLineIf2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo([|2 + 2|]) Else Bar(2 + 2) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 2 + 2 \n If True Then Foo(V) Else Bar(V) \n End Sub \n End Module"),
index:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleToMultiLineIf3()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo(2 + 2) Else Bar([|2 + 2|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then \n Foo(2 + 2) \n Else \n Const {|Rename:V|} As Integer = 2 + 2 \n Bar(V) \n End If \n End Sub \n End Module"),
index:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleToMultiLineIf4()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If True Then Foo(2 + 2) Else Bar([|2 + 2|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Const {|Rename:V|} As Integer = 2 + 2 \n If True Then Foo(V) Else Bar(V) \n End Sub \n End Module"),
index:=3)
        End Sub

        <WorkItem(541604)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestAttribute()
            Test(
NewLines("<Attr([|2 + 2|])> \n Class Foo \n End Class \n Friend Class AttrAttribute \n Inherits System.Attribute \n End Class"),
NewLines("<Attr(Foo.V)> \n Class Foo \n Friend Const {|Rename:V|} As Integer = 2 + 2 \n End Class \n Friend Class AttrAttribute \n Inherits System.Attribute \n End Class"),
index:=0)
        End Sub

        <WorkItem(542092)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub RangeArgumentLowerBound1()
            TestMissing(NewLines("Module M \n Sub Main() \n Dim x() As Integer \n ReDim x([|0|] To 5) \n End Sub \n End Module"))
        End Sub

        <WorkItem(542092)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub RangeArgumentLowerBound2()
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

            Test(code, expected, index:=3, compareTokens:=False)
        End Sub

        <WorkItem(543029), WorkItem(542963), WorkItem(542295)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestUntypedExpression()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim q As Object \n If True Then q = [|Sub() \n End Sub|] \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim q As Object \n If True Then \n Dim {|Rename:p|} As Object = Sub() \n End Sub \n q = p \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(542374)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestFieldConstantInAttribute1()
            Test(
NewLines("<Foo(2 + 3 + 4)> \n Module Program \n Dim x = [|2 + 3|] + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("<Foo(2 + 3 + 4)> \n Module Program \n Private Const {|Rename:V|} As Integer = 2 + 3 \n Dim x = V + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
index:=0)
        End Sub

        <WorkItem(542374)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestFieldConstantInAttribute2()
            Test(
NewLines("<Foo(2 + 3 + 4)> \n Module Program \n Dim x = [|2 + 3|] + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("<Foo(V + 4)> \n Module Program \n Friend Const {|Rename:V|} As Integer = 2 + 3 \n Dim x = V + 4 \n End Module \n Friend Class FooAttribute \n Inherits Attribute \n Sub New(x As Integer) \n End Sub \n End Class"),
index:=1,
parseOptions:=Nothing)
        End Sub

        <WorkItem(542783)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestMissingOnAttributeName()
            TestMissing(
NewLines("<[|Obsolete|]> \n Class C \n End Class"))
        End Sub

        <WorkItem(542811)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestMissingOnFilterClause()
            TestMissing(
NewLines("Module Program \n Sub Main() \n Try \n Catch ex As Exception When [|+|] \n End Try \n End Sub \n End Module"))
        End Sub

        <WorkItem(542906)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoIntroduceLocalInAttribute()
            Dim input =
"Module Program \n <Obsolete([|""""|])> \n Sub Main(args As String()) \n End Sub \n End Module"

            TestActionCount(
NewLines(input),
count:=2)

            Test(
NewLines(input),
NewLines("Module Program \n Private Const {|Rename:V|} As String = """" \n <Obsolete(V)> \n Sub Main(args As String()) \n End Sub \n End Module"),
index:=0)
        End Sub

        <WorkItem(542947)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNotOnMyBase()
            TestMissing(
NewLines("Class c1 \n Public res As String \n Sub Foo() \n res = ""1"" \n End Sub \n End Class \n Class c2 \n Inherits c1 \n Sub scen1() \n [|MyBase|].Foo() \n End Sub \n End Class"))
        End Sub

        <WorkItem(541966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNestedMultiLineIf1()
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

            Test(code, expected, index:=3, compareTokens:=False)
        End Sub

        <WorkItem(541966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNestedMultiLineIf2()
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

            Test(code, expected, index:=3, compareTokens:=False)
        End Sub

        <WorkItem(541966)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNestedMultiLineIf3()
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

            Test(code, expected, index:=3, compareTokens:=False)
        End Sub

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleLineLambda1()
            Test(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) Console.WriteLine([|x + 1|]) ' Introduce local \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n Dim {|Rename:v|} As Integer = x + 1 \n Console.WriteLine(v) ' Introduce local \n End Sub \n End Sub \n End Module"),
index:=0)
        End Sub

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleLineLambda2()
            Test(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) If True Then Console.WriteLine([|x + 1|]) Else Console.WriteLine() \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n If True Then \n Dim {|Rename:v|} As Integer = x + 1 \n Console.WriteLine(v) \n Else \n Console.WriteLine() \n End If \n End Sub \n End Sub \n End Module"),
index:=0)
        End Sub

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleLineLambda3()
            Test(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) If True Then Console.WriteLine() Else Console.WriteLine([|x + 1|]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n If True Then \n Console.WriteLine() \n Else \n Dim {|Rename:v|} As Integer = x + 1 \n Console.WriteLine(v) \n End If \n End Sub \n End Sub \n End Module"),
index:=0)
        End Sub

        <WorkItem(543273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleLineLambda4()
            Test(
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) If True Then Console.WriteLine([|x + 1|]) Else Console.WriteLine(x + 1) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main \n Dim a = Sub(x As Integer) \n Dim {|Rename:v|} As Integer = x + 1 \n If True Then Console.WriteLine(v) Else Console.WriteLine(v) \n End Sub \n End Sub \n End Module"),
index:=1)
        End Sub

        <WorkItem(543299)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleLineLambda5()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim query = Sub(a) a = New With {Key .Key = Function(ByVal arg As Integer) As Integer \n Return arg \n End Function}.Key.Invoke([|a Or a|]) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim query = Sub(a) \n Dim {|Rename:v|} As Object = a Or a \n a = New With {Key .Key = Function(ByVal arg As Integer) As Integer \n Return arg \n End Function}.Key.Invoke(v) \n End Sub \n End Sub \n End Module"),
index:=0)
        End Sub

        <WorkItem(542762)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNotInIntoClause()
            TestMissing(
NewLines("Imports System.Linq \n Module \n Sub Main() \n Dim x = Aggregate y In New Integer() {1} \n Into [|Count()|] \n End Sub \n End Module"))
        End Sub

        <WorkItem(543289)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNotOnAttribute1()
            TestMissing(
NewLines("Option Explicit Off \n Module Program \n <Runtime.CompilerServices.[|Extension|]()> _ \n Function Extension(ByVal x As Integer) As Integer \n Return x \n End Function \n End Module"))
        End Sub

        <WorkItem(543289)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNotOnAttribute2()
            TestMissing(
NewLines("Option Explicit Off \n Module Program \n <Runtime.CompilerServices.[|Extension()|]> _ \n Function Extension(ByVal x As Integer) As Integer \n Return x \n End Function \n End Module"))
        End Sub

        <WorkItem(543461)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestCollectionInitializer()
            TestMissing(
NewLines("Module Program \n Sub Main(args As String()) \n Dim i1 = New Integer() [|{4, 5}|] \n End Sub \n End Module"))
        End Sub

        <WorkItem(543573)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestCaseInsensitiveNameConflict()
            Test(
NewLines("Class M \n Public Function Foo() \n Return [|Me.Foo|] * 0 \n End Function \n End Class"),
NewLines("Class M \n Public Function Foo() \n Dim {|Rename:foo1|} As Object = Me.Foo \n Return foo1 * 0 \n End Function \n End Class"))
        End Sub

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestQuery1()
            Test(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Select v \n Return 0 \n End Function \n End Class"))
        End Sub

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestQueryCount1()
            TestActionCount(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
count:=2)
        End Sub

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestQuery2()
            Test(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where [|Sample(s)|] > 21 \n Select Sample(s) \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Where v > 21 \n Select Sample(s) \n Return 0 \n End Function \n End Class"))
        End Sub

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestQuery3()
            Test(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where [|Sample(s)|] > 21 \n Select Sample(s) \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Where v > 21 \n Select v \n Return 0 \n End Function \n End Class"),
index:=1)
        End Sub

        <WorkItem(543590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestQuery4()
            Test(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where Sample(s) > 21 \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where Sample(s) > 21 Let {|Rename:v|} = Sample(s) \n Select v \n Return 0 \n End Function \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestQuery5()
            Test(
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} \n Where Sample(s) > 21 \n Select [|Sample(s)|] \n Return 0 \n End Function \n End Class"),
NewLines("Imports System.Linq \n Public Class Base \n Public Function Sample(ByVal arg As Integer) As Integer \n Dim results = From s In New Integer() {1} Let {|Rename:v|} = Sample(s) \n Where v > 21 \n Select v \n Return 0 \n End Function \n End Class"),
index:=1)
        End Sub

        <WorkItem(543529)>
        <WorkItem(909152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInStatementlessConstructorParameter()
            TestMissing(NewLines("Class C1 \n Sub New(Optional ByRef x As String = [|Nothing|]) \n End Sub \n End Class"))
        End Sub

        <WorkItem(543650)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReferenceToAnonymousTypeProperty()
            TestMissing(
NewLines("Class AM \n Sub M(args As String()) \n Dim var1 As New AM \n Dim at1 As New With {var1, .friend = [|.var1|]} \n End Sub \n End Class"))
        End Sub

        <WorkItem(543698)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntegerArrayExpression()
            Test(
NewLines("Module Program \n Sub Main() \n Return [|New Integer() {}|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n Dim {|Rename:v|} As Integer() = New Integer() {} \n Return v \n End Sub \n End Module"))
        End Sub

        <WorkItem(544273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestAttributeNamedParameter()
            TestMissing(
NewLines("Class TestAttribute \n Inherits Attribute \n Public Sub New(Optional a As Integer = 42) \n End Sub \n End Class \n <Test([|a|]:=5)> \n Class Foo \n End Class"))
        End Sub

        <WorkItem(544265)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestMissingOnWrittenToExpression()
            TestMissing(
NewLines("Module Program \n Sub Main() \n Dim x = New Integer() {1, 2} \n [|x(1)|] = 2 \n End Sub \n End Module"))
        End Sub

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestImplicitMemberAccess1()
            TestMissing(
NewLines("Imports System \n Public Class C1 \n Public FieldInt As Long \n Public FieldStr As String \n Public Property PropInt As Integer \n End Class \n Public Class C2 \n Public Shared Sub Main() \n Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt|].ToString()} \n End Sub \n End Class"))
        End Sub

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestImplicitMemberAccess2()
            TestMissing(
NewLines("Imports System \n Public Class C1 \n Public FieldInt As Long \n Public FieldStr As String \n Public Property PropInt As Integer \n End Class \n Public Class C2 \n Public Shared Sub Main() \n Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt.ToString|]()} \n End Sub \n End Class"))
        End Sub

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestImplicitMemberAccess3()
            TestMissing(
NewLines("Imports System \n Public Class C1 \n Public FieldInt As Long \n Public FieldStr As String \n Public Property PropInt As Integer \n End Class \n Public Class C2 \n Public Shared Sub Main() \n Dim x = 1 + New C1() With {.FieldStr = [|.FieldInt.ToString()|]} \n End Sub \n End Class"))
        End Sub

        <WorkItem(543824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestImplicitMemberAccess4()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(529510)>
        <WpfFact(Skip:="529510"), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoRefactoringOnAddressOfExpression()
            Dim source = NewLines("Imports System \n Module Module1 \n Public Sub Foo(ByVal a1 As Exception) \n End Sub \n Public Sub foo(ByVal a1 As Action(Of ArgumentException)) \n End Sub \n Sub Main() \n Foo(New Action(Of Exception)([|AddressOf Foo|])) \n End Sub \n End Module")
            TestMissing(source)
        End Sub

        <WorkItem(529510)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
        Public Sub TestMissingOnAddressOfInDelegate()
            TestMissing(
NewLines("Module Module1 \n Public Sub Foo(ByVal a1 As Exception) \n End Sub \n Public Sub foo(ByVal a1 As Action(Of ArgumentException)) \n End Sub \n Sub Main() \n foo(New Action(Of Exception)([|AddressOf Foo|])) \n End Sub \n End Module"))
        End Sub

        <WorkItem(545168)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
        Public Sub TestMissingOnXmlName()
            TestMissing(
NewLines("Module M \n Sub Main() \n Dim x = <[|x|]/> \n End Sub \n End Module"))
        End Sub

        <WorkItem(545262)>
        <WorkItem(909152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInTernaryConditional()
            TestMissing(NewLines("Module Program \n Sub Main(args As String()) \n Dim p As Object = Nothing \n Dim Obj1 = If(New With {.a = True}.a, p, [|Nothing|]) \n End Sub \n End Module"))
        End Sub

        <WorkItem(545316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInPropertyInitializer()
            Test(
NewLines("Module Module1 \n Property Prop As New List(Of String) From {[|""One""|], ""two""} \n End Module"),
NewLines("Module Module1 \n Private Const {|Rename:V|} As String = ""One"" \n Property Prop As New List(Of String) From {V, ""two""} \n End Module"))
        End Sub

        <WorkItem(545308)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestDoNotMergeAmpersand()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(545258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestVenusGeneration1()
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

            TestExactActionSetOffered(code.NormalizedValue,
                                      {String.Format(FeaturesResources.IntroduceLocalConstantFor, "5"),
                                       String.Format(FeaturesResources.IntroduceLocalConstantForAll, "5")})

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(545258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestVenusGeneration2()
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

            TestExactActionSetOffered(code.NormalizedValue,
                                      {String.Format(FeaturesResources.IntroduceLocalConstantFor, "5"),
                                       String.Format(FeaturesResources.IntroduceLocalConstantForAll, "5")})
        End Sub

        <WorkItem(545258)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestVenusGeneration3()
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

            TestExactActionSetOffered(code.NormalizedValue,
                                      {String.Format(FeaturesResources.IntroduceLocalConstantFor, "5"),
                                       String.Format(FeaturesResources.IntroduceLocalConstantForAll, "5")})

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(545525)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInvocation()
            Test(
NewLines("Option Strict On \n  \n Class C \n Shared Sub Main() \n Dim x = [|New C().Foo()|](0) \n End Sub \n Function Foo() As Integer() \n End Function \n End Class"),
NewLines("Option Strict On \n  \n Class C \n Shared Sub Main() \n Dim {|Rename:v|} As Integer() = New C().Foo() \n Dim x = v(0) \n End Sub \n Function Foo() As Integer() \n End Function \n End Class"))
        End Sub

        <WorkItem(545829)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestOnImplicitMemberAccess()
            Test(
NewLines("Module Program \n Sub Main() \n With """" \n Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n With """" \n Dim {|Rename:getHashCode|} As Integer = .GetHashCode \n Dim x = getHashCode Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
parseOptions:=Nothing)

            Test(
NewLines("Module Program \n Sub Main() \n With """" \n Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n With """" \n Dim {|Rename:getHashCode1|} As Integer = .GetHashCode \n Dim x = getHashCode1 Xor &H7F3E ' Introduce Local \n End With \n End Sub \n End Module"),
parseOptions:=GetScriptOptions())
        End Sub

        <WorkItem(545702)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestMissingInRefLocation()
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

            TestMissing(markup)
        End Sub

        <WorkItem(546139)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestAcrossPartialTypes()
            Test(
NewLines("Partial Class C \n Sub foo1(Optional x As String = [|""HELLO""|]) \n End Sub \n End Class \n Partial Class C \n Sub foo3(Optional x As String = ""HELLO"") \n End Sub \n End Class"),
NewLines("Partial Class C \n Private Const {|Rename:V|} As String = ""HELLO"" \n Sub foo1(Optional x As String = V) \n End Sub \n End Class \n Partial Class C \n Sub foo3(Optional x As String = V) \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(544669)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestFunctionBody1()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim a1 = Function(ByVal x) [|x!foo|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim a1 = Function(ByVal x) \n Dim {|Rename:foo|} As Object = x!foo \n Return foo \n End Function \n End Sub \n End Module"))
        End Sub

        <WorkItem(1065689)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestTrailingTrivia()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(546815)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestInIfStatement()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n If [|True|] Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private Const {|Rename:V|} As Boolean = True \n Sub Main(args As String()) \n If V Then \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(830928)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroduceLocalRemovesUnnecessaryCast()
            Test(
NewLines("Imports System.Collections.Generic \n Class C \n Private Shared Sub Main(args As String()) \n Dim hSet = New HashSet(Of String)() \n hSet.Add([|hSet.ToString()|]) \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Class C \n Private Shared Sub Main(args As String()) \n Dim hSet = New HashSet(Of String)() \n Dim {|Rename:v|} As String = hSet.ToString() \n hSet.Add(v) \n End Sub \n End Class"))
        End Sub

        <WorkItem(546691)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroLocalInSingleLineLambda()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(530720)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestSingleToMultilineLambdaLineBreaks()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(531478)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub EscapeKeywordsIfNeeded1()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(632327)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InsertAfterPreprocessor1()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(632327)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InsertAfterPreprocessor2()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(682683)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub DontRemoveParenthesesIfOperatorPrecedenceWouldBeBroken()
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

            Test(code, expected, index:=2, compareTokens:=False)
        End Sub

        <WorkItem(1022458)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub DontSimplifyParentUnlessEntireInnerNodeIsSelected()
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

            Test(code, expected, index:=1, compareTokens:=False)
        End Sub

        <WorkItem(939259)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroduceLocalWithTriviaInMultiLineStatements()
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

            Test(code, expected, index:=3, compareTokens:=False)
        End Sub

        <WorkItem(909152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestMissingOnNothingLiteral()
            TestMissing(
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
        End Sub

        <WorkItem(1130990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InParentConditionalAccessExpressions()
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
            Test(code, expected, index:=0, compareTokens:=False)
        End Sub

        <WorkItem(1130990)>
        <WorkItem(3110, "https://github.com/dotnet/roslyn/issues/3110")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub MissingAcrossMultipleParentConditionalAccessExpressions()
            TestMissing(
<File>
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = [|F(New C)?.F(New C)|]?.F(New C)
        Return x
    End Function
End Class
</File>)
        End Sub

        <WorkItem(1130990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub MissingOnInvocationExpressionInParentConditionalAccessExpressions()
            TestMissing(
<File>
Imports System
Class C
    Function F(Of T)(x As T) As T
        Dim y = F(New C)?.[|F(New C)|]?.F(New C)
        Return x
    End Function
End Class
</File>)
        End Sub

        <WorkItem(1130990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub MissingOnMemberBindingExpressionInParentConditionalAccessExpressions()
            TestMissing(
<File>
Imports System
Class C
    Sub F()
        Dim s as String = "Text"
        Dim l = s?.[|Length|]
    End Sub
End Class
</File>)
        End Sub

        <WorkItem(2026, "https://github.com/dotnet/roslyn/issues/2026")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestReplaceAllFromInsideIfBlock()
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

            Test(code, expected, index:=1, compareTokens:=False)
        End Sub

        <WorkItem(1065661)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestIntroduceVariableTextDoesntSpanLines()
            Dim code = "
Class C
    Sub M()
        Dim s = """" + [|""a

b
c""|]
    End Sub
End Class"
            TestSmartTagText(code, String.Format(FeaturesResources.IntroduceLocalConstantFor, """a b c"""), index:=2)
        End Sub

        <WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoConstantForInterpolatedStrings1()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub TestNoConstantForInterpolatedStrings2()
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

            Test(code, expected, index:=1, compareTokens:=False)
        End Sub

        <WorkItem(3147, "https://github.com/dotnet/roslyn/issues/3147")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub HandleFormattableStringTargetTyping1()
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

            Test(code, expected, index:=0, compareTokens:=False)
        End Sub

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InAutoPropertyInitializerEqualsClause()
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
            Test(code, expected, index:=0, compareTokens:=False)
        End Sub

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InAutoPropertyWithCollectionInitializerAfterEqualsClause()
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
            Test(code, expected, index:=0, compareTokens:=False)
        End Sub

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InAutoPropertyInitializerAsClause()
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
            Test(code, expected, index:=0, compareTokens:=False)
        End Sub

        <WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        Public Sub InAutoPropertyObjectCreationExpressionWithinAsClause()
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
            Test(code, expected, index:=0, compareTokens:=False)
        End Sub

    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TypeInferrer
    Partial Public Class TypeInferrerTests
        Inherits TypeInferrerTestBase(Of VisualBasicTestWorkspaceFixture)

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Protected Overrides Sub TestWorker(document As Document, textSpan As TextSpan, expectedType As String, useNodeStartPosition As Boolean)
            Dim root = document.GetSyntaxTreeAsync().Result.GetRoot()
            Dim node = FindExpressionSyntaxFromSpan(root, textSpan)
            Dim typeInference = document.GetLanguageService(Of ITypeInferenceService)()

            Dim inferredType = If(
                useNodeStartPosition,
                typeInference.InferType(document.GetSemanticModelForSpanAsync(New TextSpan(node.SpanStart, 0), CancellationToken.None).Result, node.SpanStart, objectAsDefault:=True, cancellationToken:=CancellationToken.None),
                typeInference.InferType(document.GetSemanticModelForSpanAsync(node.Span, CancellationToken.None).Result, node, objectAsDefault:=True, cancellationToken:=CancellationToken.None))
            Dim typeSyntax = inferredType.GenerateTypeSyntax().NormalizeWhitespace()
            Assert.Equal(expectedType, typeSyntax.ToString())
        End Sub

        Private Sub TestInClass(text As String, expectedType As String)
            text = <text>Class C
    $
End Class</text>.Value.Replace("$", text)
            Test(text, expectedType)
        End Sub

        Private Sub TestInMethod(text As String, expectedType As String, Optional testNode As Boolean = True, Optional testPosition As Boolean = True)
            text = <text>Class C
    Sub M()
        $
    End Sub
End Class</text>.Value.Replace("$", text)
            Test(text, expectedType, testNode:=testNode, testPosition:=testPosition)
        End Sub

        Private Function FindExpressionSyntaxFromSpan(root As SyntaxNode, textSpan As TextSpan) As ExpressionSyntax
            Dim token = root.FindToken(textSpan.Start)
            Dim currentNode = token.Parent
            While currentNode IsNot Nothing
                Dim result As ExpressionSyntax = TryCast(currentNode, ExpressionSyntax)
                If result IsNot Nothing AndAlso result.Span = textSpan Then
                    Return result
                End If

                currentNode = currentNode.Parent
            End While

            Return Nothing
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConditional1()
            TestInMethod("Dim q = If([|Foo()|], 1, 2)", "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConditional2()
            TestInMethod("Dim q = If(a, [|Foo()|], 2)", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConditional3()
            TestInMethod("Dim q = If(a, """", [|Foo()|])", "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestVariableDeclarator1()
            TestInMethod("Dim q As Integer = [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestVariableDeclarator2()
            TestInMethod("Dim q = [|Foo()|]", "System.Object")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Sub TestCoalesce1()
            TestInMethod("Dim q = If([|Foo()|], 1)", "System.Int32?")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Sub TestCoalesce2()
            TestInMethod(<text>Dim b as Boolean?
    Dim q = If(b, [|Foo()|])</text>.Value, "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Sub TestCoalesce3()
            TestInMethod(<text>Dim s As String
    Dim q = If(s, [|Foo()|])</text>.Value, "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Sub TestCoalesce4()
            TestInMethod("Dim q = If([|Foo()|], String.Empty)", "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryExpression1()
            TestInMethod(<text>Dim s As String
    Dim q = s + [|Foo()|]</text>.Value, "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryExpression1_1()
            TestInMethod(<text>Dim s As String
    Dim q = s &amp; [|Foo()|]</text>.Value, "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryExpression2()
            TestInMethod(<text>Dim s
    Dim q = s OrElse [|Foo()|]</text>.Value, "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator1()
            TestInMethod("Dim q = x << [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator2()
            TestInMethod("Dim q = x >> [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator3()
            TestInMethod("Dim q : q <<= [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator4()
            TestInMethod("Dim q : q >>= [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator5()
            TestInMethod("Dim q : [|somefield|] <<= q", "System.Int32", testPosition:=False)
        End Sub

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator6()
            TestInMethod("Dim q : [|somefield|] >>= q", "System.Int32", testPosition:=False)
        End Sub

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator7()
            TestInMethod("Dim q As String : q >>= [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestBinaryOperator8()
            TestInMethod("Dim q As String : [|somefield|] >>= q", "System.Int32", testPosition:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestReturn1()
            TestInClass("Function M() As Integer : Return [|Foo()|] : End Function", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestReturn2()
            TestInMethod("Return [|Foo()|]", "Global.System.Void")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestReturn3()
            TestInClass("Property Prop As Integer : Get : Return [|Foo()|] : End Get : End Property", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(827897)>
        Public Sub TestYieldReturn()
            TestInClass("Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer) : Yield [|abc|] : End Function", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479)>
        Public Sub TestReturnInLambda()
            TestInMethod(<Code>Dim F As System.Func(Of String, Integer) = Function (s)
                                                                       Return [|Foo()|]
                                                                   End Function</Code>.Value, "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479)>
        Public Sub TestInsideLambda2()
            Dim text = <text>Imports System
Class A
  Sub Foo()
    Dim f As Func(Of Integer, Integer) = Function(i)  [|here|]
  End Sub
End Class</text>.Value
            Test(text, "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479)>
        Public Sub TestLambda()
            TestInMethod("Dim f As System.Func(Of String, Integer) = Function (s) [|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestThrow()
            TestInMethod("Throw [|Foo()|]", "Global.System.Exception")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCatch()
            TestInMethod("Try : Catch e As [|Foo|] : End Try", "Global.System.Exception")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestIf()
            TestInMethod("If [|Foo()|] : End If", "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestWhile()
            TestInMethod("While [|Foo()|] : End While", "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestDo()
            TestInMethod("Do : Loop While [|Foo()|]", "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542835)>
        Public Sub TestFor2()
            TestInMethod("For i As Integer = 1 To 2 Step [|Foo|]", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestUsing1()
            TestInMethod("Using [|Foo()|] : End Using", "Global.System.IDisposable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestUsing2()
            TestInMethod("Using i As Integer = [|Foo()|] : End Using", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(544611)>
        Public Sub TestUsing3()
            TestInMethod("Using v = [|Foo()|] : End Using", "Global.System.IDisposable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542838)>
        Public Sub TestForEach()
            TestInMethod("For Each v As Integer in [|Foo()|] : Next", "Global.System.Collections.Generic.IEnumerable(Of System.Int32)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestPrefixExpression1()
            TestInMethod("Dim q = +[|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestPrefixExpression2()
            TestInMethod("Dim q = -[|Foo()|]", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542839)>
        Public Sub TestPrefixExpression3()
            TestInMethod("Dim q = Not [|Foo()|] And 5", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestPrefixExpression4()
            TestInMethod("Dim q = Not [|Foo()|]", "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837)>
        Public Sub TestArrayRankSpecifier1()
            TestInMethod("Dim q As String() = New String([|Foo()|])", "System.Char()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837)>
        Public Sub TestArrayRankSpecifier2()
            TestInMethod("Dim q As String() = New String([|Foo()|]) { }", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestSwitch1()
            TestInMethod("Select Case [|Foo()|] : End Select", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestSwitch2()
            TestInMethod("Select Case [|Foo()|] : Case Else: End Select", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestSwitch3()
            TestInMethod("Select Case [|Foo()|] : Case ""a"": End Select", "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestMethodCall1()
            TestInMethod("Bar([|Foo()|])", "System.Object")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestMethodCall2()
            TestInClass("Sub M() : Bar([|Foo()|]) : End Sub : Sub Bar(i As Integer) : End Sub", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestMethodCall3()
            TestInClass("Sub M() : Bar([|Foo()|]) : End Sub : Sub Bar() : End Sub", "System.Object")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestMethodCall4()
            TestInClass("Sub M() : Bar([|Foo()|]) : End Sub : Sub Bar(i As Integer, s As String) : End Sub", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestMethodCall5()
            TestInClass("Sub M() : Bar(s:=[|Foo()|]) : End Sub : Sub Bar(i As Integer, s As String) : End Sub", "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConstructorCall1()
            TestInMethod("Dim l = New C([|Foo()|])", "System.Object")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConstructorCall2()
            TestInClass("Sub M() : Dim l = New C([|Foo()|]) : End Sub : Sub New(i As Integer) : End Sub", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConstructorCall3()
            TestInClass("Sub M() : Dim l = New C([|Foo()|]) : End Sub : Sub New() : End Sub", "System.Object")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConstructorCall4()
            TestInClass("Sub M() : Dim l = New C([|Foo()|]) : End Sub : Sub New(i As Integer, s As String) : End Sub", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestConstructorCall5()
            TestInClass("Sub M() : Dim l = New C(s:=[|Foo()|]) : End Sub : Sub New(i As Integer, s As String) : End Sub", "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837)>
        Public Sub TestIndexAccess1()
            TestInMethod("Dim i As String() : Dim j = i([|Foo()|])", "System.Int32")
        End Sub

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCollectionInitializer1()
            Dim text = <text>Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = New List(Of Integer)() From { [|Foo()|] }
  End Sub
End Class</text>.Value
            Test(text, "System.Int32", testPosition:=False)
        End Sub

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCollectionInitializer2()
            Dim text = <text>
Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = New Dictionary(Of Integer, String)() From  { { [|Foo()|], String.Empty } }
  End Sub
End Class</text>.Value
            Test(text, "System.Int32", testPosition:=False)
        End Sub

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCollectionInitializer3()
            Dim text = <text>
Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = new Dictionary(Of Integer, String)() From { { 0, [|Foo()|] } }
  End Sub
End Class</text>.Value
            Test(text, "System.String", testPosition:=False)
        End Sub

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCustomCollectionInitializerAddMethod1()
            Dim text = <text>
Class C
    Implements System.Collections.IEnumerable

    Sub M()
        Dim x = New C From {[|a|]}
    End Sub

    Sub Add(i As Integer)
    End Sub

    Sub Add(s As String, b As Boolean)
    End Sub

    Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class
                       </text>.Value
            Test(text, "System.Int32", testPosition:=False)
        End Sub

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCustomCollectionInitializerAddMethod2()
            Dim text = <text>
Class C
    Implements System.Collections.IEnumerable

    Sub M()
        Dim x = New C From {{"test", [|b|]}}
    End Sub

    Sub Add(i As Integer)
    End Sub

    Sub Add(s As String, b As Boolean)
    End Sub

    Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class</text>.Value
            Test(text, "System.Boolean", testPosition:=False)
        End Sub

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestCustomCollectionInitializerAddMethod3()
            Dim text = <text>
Class C
    Implements System.Collections.IEnumerable

    Sub M()
        Dim x = New C From {{[|s|], True}}
    End Sub

    Sub Add(i As Integer)
    End Sub

    Sub Add(s As String, b As Boolean)
    End Sub

    Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class</text>.Value
            Test(text, "System.String", testPosition:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestArrayInference1()
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Foo()
        Dim x As A() = new [|C|]() { }
  End Sub
End Class</text>.Value
            Test(text, "Global.A")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestArrayInference2()
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Foo()
        Dim x As A()() = new [|C|]()() { }
  End Sub
End Class</text>.Value
            Test(text, "Global.A()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestArrayInference3()
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Foo()
        Dim x As A()() = new [|C|]() { }
  End Sub
End Class</text>.Value
            Test(text, "Global.A()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Sub TestDynamic1()
            Dim text = <text>
Class C
  Sub M(i As Dynamic)
    Dim q = i([|Foo()|]);
  End Sub
End Class</text>.Value
            Test(text, "System.Object")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584)>
        Public Sub TestAwaitTaskOfT()
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim x As Integer = Await [|Foo()|]
    End Sub
End Class</text>.Value
            Test(text, "Global.System.Threading.Tasks.Task(Of System.Int32)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584)>
        Public Sub TestAwaitTaskOfTaskOfT()
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim x As Task(Of Integer) = Await [|Foo()|]
    End Sub
End Class</text>.Value
            Test(text, "Global.System.Threading.Tasks.Task(Of Global.System.Threading.Tasks.Task(Of System.Int32))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584)>
        Public Sub TestAwaitTask()
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Await [|Foo()|]
    End Sub
End Class</text>.Value
            Test(text, "Global.System.Threading.Tasks.Task")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(827897)>
        Public Sub TestReturnFromAsyncTaskOfT()
            TestInClass("Async Function M() As System.Threading.Tasks.Task(Of Integer) : Return [|abc|] : End Function", "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(530816)>
        Public Sub TestNamedFieldInitializer()
            Dim text = <text>
Imports System.Linq
Module Module1
    Sub Main()
        Dim vehicle = New Car With {.Color = [|s|]}
    End Sub
End Module
Public Enum Color
    Red
    Blue
End Enum
Public Class Car
    Public Name As String
    Public Color As Color
End Class
</text>.Value
            Test(text, "Global.Color")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840)>
        Public Sub TestAttributeArguments1()
            Dim text = <text>
&lt;AAttribute([|dd|], ee, Y:=ff)&gt;
Class AAttribute
    Inherits System.Attribute

    Public X As Integer
    Public Y As String

    Public Sub New(a As System.DayOfWeek, b As Double)
    End Sub
End Class
</text>.Value
            Test(text, "Global.System.DayOfWeek")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840)>
        Public Sub TestAttributeArguments2()
            Dim text = <text>
&lt;AAttribute(dd, [|ee|], Y:=ff)&gt;
Class AAttribute
    Inherits System.Attribute

    Public X As Integer
    Public Y As String

    Public Sub New(a As System.DayOfWeek, b As Double)
    End Sub
End Class
</text>.Value
            Test(text, "System.Double")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840)>
        Public Sub TestAttributeArguments3()
            Dim text = <text>
&lt;AAttribute(dd, ee, Y:=[|ff|])&gt;
Class AAttribute
    Inherits System.Attribute

    Public X As Integer
    Public Y As String

    Public Sub New(a As System.DayOfWeek, b As Double)
    End Sub
End Class
</text>.Value
            Test(text, "System.String")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388)>
        Public Sub TestCatchFilterClause()
            Dim text = "Try : Catch ex As Exception When [|foo()|]"
            TestInMethod(text, "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388)>
        Public Sub TestCatchFilterClause1()
            Dim text = "Try : Catch ex As Exception When [|foo|]"
            TestInMethod(text, "System.Boolean")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388)>
        Public Sub TestCatchFilterClause2()
            Dim text = "Try : Catch ex As Exception When [|foo|].N"
            TestInMethod(text, "System.Object", testPosition:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(1041260)>
        Public Sub ConditionalInvocation()
            Dim text = "Dim args As String() : args?([|foo|])"
            TestInMethod(text, "System.Int32", testPosition:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        Public Sub TestAwaitExpressionWithChainingMethod()
            Dim text = "Imports System
Imports System.Linq

Module M
    Async Sub T()
        Dim x As Boolean = Await [|F|].ContinueWith(Function(a) True).ContinueWith(Function(a) False)
    End Sub
End Module"
            Test(text, "Global.System.Threading.Tasks.Task(Of System.Boolean)", testPosition:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        Public Sub TestAwaitExpressionWithChainingMethod2()
            Dim text = "Imports System
Imports System.Threading.Tasks

Module M
    Async Sub T()
        Dim x As Boolean = Await [|F|].ConfigureAwait(False)
    End Sub
End Module"
            Test(text, "Global.System.Threading.Tasks.Task(Of System.Boolean)", testPosition:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(3518, "https://github.com/dotnet/roslyn/issues/3518")>
        Public Sub NoTypeAfterInvocationWithCompletionListTagTypeAsFirstParameter()
            Dim text = "Class C
    Sub Test()
        M(5)
        [|x|]
    End Sub

    Sub M(x As Integer)
    End Sub
End Class"
            Test(text, "System.Object", testNode:=False, testPosition:=True)
        End Sub
    End Class
End Namespace

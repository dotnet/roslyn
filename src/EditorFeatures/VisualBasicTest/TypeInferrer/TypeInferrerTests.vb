' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
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

        Private Async Function TestInClassAsync(text As String, expectedType As String) As Tasks.Task
            text = <text>Class C
    $
End Class</text>.Value.Replace("$", text)
            Await TestAsync(text, expectedType)
        End Function

        Private Async Function TestInMethodAsync(text As String, expectedType As String, Optional testNode As Boolean = True, Optional testPosition As Boolean = True) As Tasks.Task
            text = <text>Class C
    Sub M()
        $
    End Sub
End Class</text>.Value.Replace("$", text)
            Await TestAsync(text, expectedType, testNode:=testNode, testPosition:=testPosition)
        End Function

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
        Public Async Function TestConditional1() As Task
            Await TestInMethodAsync("Dim q = If([|Foo()|], 1, 2)", "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConditional2() As Task
            Await TestInMethodAsync("Dim q = If(a, [|Foo()|], 2)", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConditional3() As Task
            Await TestInMethodAsync("Dim q = If(a, """", [|Foo()|])", "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestVariableDeclarator1() As Task
            Await TestInMethodAsync("Dim q As Integer = [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestVariableDeclarator2() As Task
            Await TestInMethodAsync("Dim q = [|Foo()|]", "System.Object")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Async Function TestCoalesce1() As Task
            Await TestInMethodAsync("Dim q = If([|Foo()|], 1)", "System.Int32?")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Async Function TestCoalesce2() As Task
            Await TestInMethodAsync(<text>Dim b as Boolean?
    Dim q = If(b, [|Foo()|])</text>.Value, "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Async Function TestCoalesce3() As Task
            Await TestInMethodAsync(<text>Dim s As String
    Dim q = If(s, [|Foo()|])</text>.Value, "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834)>
        Public Async Function TestCoalesce4() As Task
            Await TestInMethodAsync("Dim q = If([|Foo()|], String.Empty)", "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryExpression1() As Task
            Await TestInMethodAsync(<text>Dim s As String
    Dim q = s + [|Foo()|]</text>.Value, "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryExpression1_1() As Task
            Await TestInMethodAsync(<text>Dim s As String
    Dim q = s &amp; [|Foo()|]</text>.Value, "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryExpression2() As Task
            Await TestInMethodAsync(<text>Dim s
    Dim q = s OrElse [|Foo()|]</text>.Value, "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator1() As Task
            Await TestInMethodAsync("Dim q = x << [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator2() As Task
            Await TestInMethodAsync("Dim q = x >> [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator3() As Task
            Await TestInMethodAsync("Dim q : q <<= [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator4() As Task
            Await TestInMethodAsync("Dim q : q >>= [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator5() As Task
            Await TestInMethodAsync("Dim q : [|somefield|] <<= q", "System.Int32", testPosition:=False)
        End Function

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator6() As Task
            Await TestInMethodAsync("Dim q : [|somefield|] >>= q", "System.Int32", testPosition:=False)
        End Function

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator7() As Task
            Await TestInMethodAsync("Dim q As String : q >>= [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, WorkItem(817192), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator8() As Task
            Await TestInMethodAsync("Dim q As String : [|somefield|] >>= q", "System.Int32", testPosition:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestReturn1() As Task
            Await TestInClassAsync("Function M() As Integer : Return [|Foo()|] : End Function", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestReturn2() As Task
            Await TestInMethodAsync("Return [|Foo()|]", "Global.System.Void")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestReturn3() As Task
            Await TestInClassAsync("Property Prop As Integer : Get : Return [|Foo()|] : End Get : End Property", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(827897)>
        Public Async Function TestYieldReturn() As Task
            Await TestInClassAsync("Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer) : Yield [|abc|] : End Function", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479)>
        Public Async Function TestReturnInLambda() As Task
            Await TestInMethodAsync(<Code>Dim F As System.Func(Of String, Integer) = Function (s)
                                                                       Return [|Foo()|]
                                                                   End Function</Code>.Value, "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479)>
        Public Async Function TestInsideLambda2() As Task
            Dim text = <text>Imports System
Class A
  Sub Foo()
    Dim f As Func(Of Integer, Integer) = Function(i)  [|here|]
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479)>
        Public Async Function TestLambda() As Task
            Await TestInMethodAsync("Dim f As System.Func(Of String, Integer) = Function (s) [|Foo()|]", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestThrow() As Task
            Await TestInMethodAsync("Throw [|Foo()|]", "Global.System.Exception")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCatch() As Task
            Await TestInMethodAsync("Try : Catch e As [|Foo|] : End Try", "Global.System.Exception")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestIf() As Task
            Await TestInMethodAsync("If [|Foo()|] : End If", "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestWhile() As Task
            Await TestInMethodAsync("While [|Foo()|] : End While", "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestDo() As Task
            Await TestInMethodAsync("Do : Loop While [|Foo()|]", "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542835)>
        Public Async Function TestFor2() As Task
            Await TestInMethodAsync("For i As Integer = 1 To 2 Step [|Foo|]", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestUsing1() As Task
            Await TestInMethodAsync("Using [|Foo()|] : End Using", "Global.System.IDisposable")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestUsing2() As Task
            Await TestInMethodAsync("Using i As Integer = [|Foo()|] : End Using", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(544611)>
        Public Async Function TestUsing3() As Task
            Await TestInMethodAsync("Using v = [|Foo()|] : End Using", "Global.System.IDisposable")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542838)>
        Public Async Function TestForEach() As Task
            Await TestInMethodAsync("For Each v As Integer in [|Foo()|] : Next", "Global.System.Collections.Generic.IEnumerable(Of System.Int32)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestPrefixExpression1() As Task
            Await TestInMethodAsync("Dim q = +[|Foo()|]", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestPrefixExpression2() As Task
            Await TestInMethodAsync("Dim q = -[|Foo()|]", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542839)>
        Public Async Function TestPrefixExpression3() As Task
            Await TestInMethodAsync("Dim q = Not [|Foo()|] And 5", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestPrefixExpression4() As Task
            Await TestInMethodAsync("Dim q = Not [|Foo()|]", "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837)>
        Public Async Function TestArrayRankSpecifier1() As Task
            Await TestInMethodAsync("Dim q As String() = New String([|Foo()|])", "System.Char()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837)>
        Public Async Function TestArrayRankSpecifier2() As Task
            Await TestInMethodAsync("Dim q As String() = New String([|Foo()|]) { }", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestSwitch1() As Task
            Await TestInMethodAsync("Select Case [|Foo()|] : End Select", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestSwitch2() As Task
            Await TestInMethodAsync("Select Case [|Foo()|] : Case Else: End Select", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestSwitch3() As Task
            Await TestInMethodAsync("Select Case [|Foo()|] : Case ""a"": End Select", "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall1() As Task
            Await TestInMethodAsync("Bar([|Foo()|])", "System.Object")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall2() As Task
            Await TestInClassAsync("Sub M() : Bar([|Foo()|]) : End Sub : Sub Bar(i As Integer) : End Sub", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall3() As Task
            Await TestInClassAsync("Sub M() : Bar([|Foo()|]) : End Sub : Sub Bar() : End Sub", "System.Object")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall4() As Task
            Await TestInClassAsync("Sub M() : Bar([|Foo()|]) : End Sub : Sub Bar(i As Integer, s As String) : End Sub", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall5() As Task
            Await TestInClassAsync("Sub M() : Bar(s:=[|Foo()|]) : End Sub : Sub Bar(i As Integer, s As String) : End Sub", "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall1() As Task
            Await TestInMethodAsync("Dim l = New C([|Foo()|])", "System.Object")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall2() As Task
            Await TestInClassAsync("Sub M() : Dim l = New C([|Foo()|]) : End Sub : Sub New(i As Integer) : End Sub", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall3() As Task
            Await TestInClassAsync("Sub M() : Dim l = New C([|Foo()|]) : End Sub : Sub New() : End Sub", "System.Object")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall4() As Task
            Await TestInClassAsync("Sub M() : Dim l = New C([|Foo()|]) : End Sub : Sub New(i As Integer, s As String) : End Sub", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall5() As Task
            Await TestInClassAsync("Sub M() : Dim l = New C(s:=[|Foo()|]) : End Sub : Sub New(i As Integer, s As String) : End Sub", "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837)>
        Public Async Function TestIndexAccess1() As Task
            Await TestInMethodAsync("Dim i As String() : Dim j = i([|Foo()|])", "System.Int32")
        End Function

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCollectionInitializer1() As Task
            Dim text = <text>Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = New List(Of Integer)() From { [|Foo()|] }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Int32", testPosition:=False)
        End Function

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCollectionInitializer2() As Task
            Dim text = <text>
Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = New Dictionary(Of Integer, String)() From  { { [|Foo()|], String.Empty } }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Int32", testPosition:=False)
        End Function

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCollectionInitializer3() As Task
            Dim text = <text>
Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = new Dictionary(Of Integer, String)() From { { 0, [|Foo()|] } }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.String", testPosition:=False)
        End Function

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCustomCollectionInitializerAddMethod1() As Task
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
            Await TestAsync(text, "System.Int32", testPosition:=False)
        End Function

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCustomCollectionInitializerAddMethod2() As Task
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
            Await TestAsync(text, "System.Boolean", testPosition:=False)
        End Function

        <WpfFact>
        <WorkItem(529480)>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCustomCollectionInitializerAddMethod3() As Task
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
            Await TestAsync(text, "System.String", testPosition:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestArrayInference1() As Task
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Foo()
        Dim x As A() = new [|C|]() { }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.A")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestArrayInference2() As Task
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Foo()
        Dim x As A()() = new [|C|]()() { }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.A()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestArrayInference3() As Task
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Foo()
        Dim x As A()() = new [|C|]() { }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.A()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestDynamic1() As Task
            Dim text = <text>
Class C
  Sub M(i As Dynamic)
    Dim q = i([|Foo()|]);
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Object")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584)>
        Public Async Function TestAwaitTaskOfT() As Task
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim x As Integer = Await [|Foo()|]
    End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of System.Int32)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584)>
        Public Async Function TestAwaitTaskOfTaskOfT() As Task
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim x As Task(Of Integer) = Await [|Foo()|]
    End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of Global.System.Threading.Tasks.Task(Of System.Int32))")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584)>
        Public Async Function TestAwaitTask() As Task
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Await [|Foo()|]
    End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.System.Threading.Tasks.Task")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(827897)>
        Public Async Function TestReturnFromAsyncTaskOfT() As Task
            Await TestInClassAsync("Async Function M() As System.Threading.Tasks.Task(Of Integer) : Return [|abc|] : End Function", "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(530816)>
        Public Async Function TestNamedFieldInitializer() As Task
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
            Await TestAsync(text, "Global.Color")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840)>
        Public Async Function TestAttributeArguments1() As Task
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
            Await TestAsync(text, "Global.System.DayOfWeek")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840)>
        Public Async Function TestAttributeArguments2() As Task
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
            Await TestAsync(text, "System.Double")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840)>
        Public Async Function TestAttributeArguments3() As Task
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
            Await TestAsync(text, "System.String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388)>
        Public Async Function TestCatchFilterClause() As Task
            Dim text = "Try : Catch ex As Exception When [|foo()|]"
            Await TestInMethodAsync(text, "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388)>
        Public Async Function TestCatchFilterClause1() As Task
            Dim text = "Try : Catch ex As Exception When [|foo|]"
            Await TestInMethodAsync(text, "System.Boolean")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388)>
        Public Async Function TestCatchFilterClause2() As Task
            Dim text = "Try : Catch ex As Exception When [|foo|].N"
            Await TestInMethodAsync(text, "System.Object", testPosition:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(1041260)>
        Public Async Function ConditionalInvocation() As Task
            Dim text = "Dim args As String() : args?([|foo|])"
            Await TestInMethodAsync(text, "System.Int32", testPosition:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        Public Async Function TestAwaitExpressionWithChainingMethod() As Task
            Dim text = "Imports System
Imports System.Linq

Module M
    Async Sub T()
        Dim x As Boolean = Await [|F|].ContinueWith(Function(a) True).ContinueWith(Function(a) False)
    End Sub
End Module"
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of System.Boolean)", testPosition:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        Public Async Function TestAwaitExpressionWithChainingMethod2() As Task
            Dim text = "Imports System
Imports System.Threading.Tasks

Module M
    Async Sub T()
        Dim x As Boolean = Await [|F|].ConfigureAwait(False)
    End Sub
End Module"
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of System.Boolean)", testPosition:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(3518, "https://github.com/dotnet/roslyn/issues/3518")>
        Public Async Function NoTypeAfterInvocationWithCompletionListTagTypeAsFirstParameter() As Task
            Dim text = "Class C
    Sub Test()
        M(5)
        [|x|]
    End Sub

    Sub M(x As Integer)
    End Sub
End Class"
            Await TestAsync(text, "System.Object", testNode:=False, testPosition:=True)
        End Function
    End Class
End Namespace

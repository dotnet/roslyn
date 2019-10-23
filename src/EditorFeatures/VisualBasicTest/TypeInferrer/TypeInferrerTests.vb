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

        Protected Overrides Async Function TestWorkerAsync(document As Document, textSpan As TextSpan, expectedType As String, testMode As TestMode) As Task
            Dim root = Await document.GetSyntaxRootAsync()
            Dim node = FindExpressionSyntaxFromSpan(root, textSpan)
            Dim typeInference = document.GetLanguageService(Of ITypeInferenceService)()

            Dim inferredType As ITypeSymbol

            If testMode = TestMode.Position Then
                inferredType = typeInference.InferType(Await document.GetSemanticModelForSpanAsync(New TextSpan(node.SpanStart, 0), CancellationToken.None), node.SpanStart, objectAsDefault:=True, cancellationToken:=CancellationToken.None)
            Else
                inferredType = typeInference.InferType(Await document.GetSemanticModelForSpanAsync(node.Span, CancellationToken.None), node, objectAsDefault:=True, cancellationToken:=CancellationToken.None)
            End If

            Dim typeSyntax = inferredType.GenerateTypeSyntax().NormalizeWhitespace()
        End Function

        Private Async Function TestInClassAsync(text As String, expectedType As String, mode As TestMode) As Tasks.Task
            text = <text>Class C
    $
End Class</text>.Value.Replace("$", text)
            Await TestAsync(text, expectedType, mode)
        End Function

        Private Async Function TestInMethodAsync(text As String, expectedType As String, mode As TestMode) As Tasks.Task
            text = <text>Class C
    Sub M()
        $
    End Sub
End Class</text>.Value.Replace("$", text)
            Await TestAsync(text, expectedType, mode)
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

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConditional1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = If([|Goo()|], 1, 2)", "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConditional2(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = If(a, [|Goo()|], 2)", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConditional3(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = If(a, """", [|Goo()|])", "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestVariableDeclarator1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q As Integer = [|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestVariableDeclarator2(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = [|Goo()|]", "System.Object", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542834")>
        Public Async Function TestCoalesce1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = If([|Goo()|], 1)", "System.Int32?", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542834")>
        Public Async Function TestCoalesce2(mode As TestMode) As Task
            Await TestInMethodAsync(<text>Dim b as Boolean?
    Dim q = If(b, [|Goo()|])</text>.Value, "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542834")>
        Public Async Function TestCoalesce3(mode As TestMode) As Task
            Await TestInMethodAsync(<text>Dim s As String
    Dim q = If(s, [|Goo()|])</text>.Value, "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542834")>
        Public Async Function TestCoalesce4(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = If([|Goo()|], String.Empty)", "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryExpression1(mode As TestMode) As Task
            Await TestInMethodAsync(<text>Dim s As String
    Dim q = s + [|Goo()|]</text>.Value, "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryExpression1_1(mode As TestMode) As Task
            Await TestInMethodAsync(<text>Dim s As String
    Dim q = s &amp; [|Goo()|]</text>.Value, "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryExpression2(mode As TestMode) As Task
            Await TestInMethodAsync(<text>Dim s
    Dim q = s OrElse [|Goo()|]</text>.Value, "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = x << [|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator2(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = x >> [|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(817192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817192")>
        Public Async Function TestBinaryOperator3(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q : q <<= [|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(817192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817192")>
        Public Async Function TestBinaryOperator4(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q : q >>= [|Goo()|]", "System.Int32", mode)
        End Function

        <Fact, WorkItem(817192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817192"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator5() As Task
            Await TestInMethodAsync("Dim q : [|somefield|] <<= q", "System.Int32", TestMode.Node)
        End Function

        <Fact, WorkItem(817192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817192"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator6() As Task
            Await TestInMethodAsync("Dim q : [|somefield|] >>= q", "System.Int32", TestMode.Node)
        End Function

        <Theory, CombinatorialData, WorkItem(817192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817192"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator7(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q As String : q >>= [|Goo()|]", "System.Int32", mode)
        End Function

        <Fact, WorkItem(817192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817192"), Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestBinaryOperator8() As Task
            Await TestInMethodAsync("Dim q As String : [|somefield|] >>= q", "System.Int32", TestMode.Node)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestReturn1(mode As TestMode) As Task
            Await TestInClassAsync("Function M() As Integer : Return [|Goo()|] : End Function", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestReturn2(mode As TestMode) As Task
            Await TestInMethodAsync("Return [|Goo()|]", "Global.System.Void", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestReturn3(mode As TestMode) As Task
            Await TestInClassAsync("Property Prop As Integer : Get : Return [|Goo()|] : End Get : End Property", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
        Public Async Function TestYieldReturn(mode As TestMode) As Task
            Await TestInClassAsync("Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer) : Yield [|abc|] : End Function", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529479")>
        Public Async Function TestReturnInLambda(mode As TestMode) As Task
            Await TestInMethodAsync(<Code>Dim F As System.Func(Of String, Integer) = Function (s)
                                                                       Return [|Goo()|]
                                                                   End Function</Code>.Value, "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529479")>
        Public Async Function TestInsideLambda2(mode As TestMode) As Task
            Dim text = <text>Imports System
Class A
  Sub Goo()
    Dim f As Func(Of Integer, Integer) = Function(i)  [|here|]
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(529479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529479")>
        Public Async Function TestLambda(mode As TestMode) As Task
            Await TestInMethodAsync("Dim f As System.Func(Of String, Integer) = Function (s) [|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestThrow(mode As TestMode) As Task
            Await TestInMethodAsync("Throw [|Goo()|]", "Global.System.Exception", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCatch(mode As TestMode) As Task
            Await TestInMethodAsync("Try : Catch e As [|Goo|] : End Try", "Global.System.Exception", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestIf(mode As TestMode) As Task
            Await TestInMethodAsync("If [|Goo()|] : End If", "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestWhile(mode As TestMode) As Task
            Await TestInMethodAsync("While [|Goo()|] : End While", "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestDo(mode As TestMode) As Task
            Await TestInMethodAsync("Do : Loop While [|Goo()|]", "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542835, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542835")>
        Public Async Function TestFor2(mode As TestMode) As Task
            Await TestInMethodAsync("For i As Integer = 1 To 2 Step [|Goo|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestUsing1(mode As TestMode) As Task
            Await TestInMethodAsync("Using [|Goo()|] : End Using", "Global.System.IDisposable", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestUsing2(mode As TestMode) As Task
            Await TestInMethodAsync("Using i As Integer = [|Goo()|] : End Using", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(544611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544611")>
        Public Async Function TestUsing3(mode As TestMode) As Task
            Await TestInMethodAsync("Using v = [|Goo()|] : End Using", "Global.System.IDisposable", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542838, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542838")>
        Public Async Function TestForEach(mode As TestMode) As Task
            Await TestInMethodAsync("For Each v As Integer in [|Goo()|] : Next", "Global.System.Collections.Generic.IEnumerable(Of System.Int32)", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestPrefixExpression1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = +[|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestPrefixExpression2(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = -[|Goo()|]", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542839")>
        Public Async Function TestPrefixExpression3(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = Not [|Goo()|] And 5", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestPrefixExpression4(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q = Not [|Goo()|]", "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542837")>
        Public Async Function TestArrayRankSpecifier1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q As String() = New String([|Goo()|])", "System.Char()", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542837")>
        Public Async Function TestArrayRankSpecifier2(mode As TestMode) As Task
            Await TestInMethodAsync("Dim q As String() = New String([|Goo()|]) { }", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestSwitch1(mode As TestMode) As Task
            Await TestInMethodAsync("Select Case [|Goo()|] : End Select", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestSwitch2(mode As TestMode) As Task
            Await TestInMethodAsync("Select Case [|Goo()|] : Case Else: End Select", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestSwitch3(mode As TestMode) As Task
            Await TestInMethodAsync("Select Case [|Goo()|] : Case ""a"": End Select", "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall1(mode As TestMode) As Task
            Await TestInMethodAsync("Bar([|Goo()|])", "System.Object", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall2(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Bar([|Goo()|]) : End Sub : Sub Bar(i As Integer) : End Sub", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall3(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Bar([|Goo()|]) : End Sub : Sub Bar() : End Sub", "System.Object", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall4(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Bar([|Goo()|]) : End Sub : Sub Bar(i As Integer, s As String) : End Sub", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMethodCall5(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Bar(s:=[|Goo()|]) : End Sub : Sub Bar(i As Integer, s As String) : End Sub", "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim l = New C([|Goo()|])", "System.Object", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall2(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Dim l = New C([|Goo()|]) : End Sub : Sub New(i As Integer) : End Sub", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall3(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Dim l = New C([|Goo()|]) : End Sub : Sub New() : End Sub", "System.Object", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall4(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Dim l = New C([|Goo()|]) : End Sub : Sub New(i As Integer, s As String) : End Sub", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestConstructorCall5(mode As TestMode) As Task
            Await TestInClassAsync("Sub M() : Dim l = New C(s:=[|Goo()|]) : End Sub : Sub New(i As Integer, s As String) : End Sub", "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(542837, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542837")>
        Public Async Function TestIndexAccess1(mode As TestMode) As Task
            Await TestInMethodAsync("Dim i As String() : Dim j = i([|Goo()|])", "System.Int32", mode)
        End Function

        <Fact>
        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCollectionInitializer1() As Task
            Dim text = <text>Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = New List(Of Integer)() From { [|Goo()|] }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Int32", TestMode.Node)
        End Function

        <Fact>
        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCollectionInitializer2() As Task
            Dim text = <text>
Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = New Dictionary(Of Integer, String)() From  { { [|Goo()|], String.Empty } }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Int32", TestMode.Node)
        End Function

        <Fact>
        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        <Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestCollectionInitializer3() As Task
            Dim text = <text>
Imports System.Collections.Generic

Class C
  Sub M()
    Dim l = new Dictionary(Of Integer, String)() From { { 0, [|Goo()|] } }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.String", TestMode.Node)
        End Function

        <Fact>
        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
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
            Await TestAsync(text, "System.Int32", TestMode.Node)
        End Function

        <Fact>
        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
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
            Await TestAsync(text, "System.Boolean", TestMode.Node)
        End Function

        <Fact>
        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
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
            Await TestAsync(text, "System.String", TestMode.Node)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestArrayInference1(mode As TestMode) As Task
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Goo()
        Dim x As A() = new [|C|]() { }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.A", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestArrayInference2(mode As TestMode) As Task
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Goo()
        Dim x As A()() = new [|C|]()() { }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.A()", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestArrayInference3(mode As TestMode) As Task
            ' TODO: review this
            Dim text = <text>
Class A
  Sub Goo()
        Dim x As A()() = new [|C|]() { }
  End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.A()", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestDynamic1(mode As TestMode) As Task
            Dim text = <text>
Class C
  Sub M(i As Dynamic)
    Dim q = i([|Goo()|]);
  End Sub
End Class</text>.Value
            Await TestAsync(text, "System.Object", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")>
        Public Async Function TestAwaitTaskOfT(mode As TestMode) As Task
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim x As Integer = Await [|Goo()|]
    End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of System.Int32)", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")>
        Public Async Function TestAwaitTaskOfTaskOfT(mode As TestMode) As Task
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim x As Task(Of Integer) = Await [|Goo()|]
    End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of Global.System.Threading.Tasks.Task(Of System.Int32))", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(553584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553584")>
        Public Async Function TestAwaitTask(mode As TestMode) As Task
            Dim text = <text>
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Await [|Goo()|]
    End Sub
End Class</text>.Value
            Await TestAsync(text, "Global.System.Threading.Tasks.Task", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
        Public Async Function TestReturnFromAsyncTaskOfT(mode As TestMode) As Task
            Await TestInClassAsync("Async Function M() As System.Threading.Tasks.Task(Of Integer) : Return [|abc|] : End Function", "System.Int32", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(530816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530816")>
        Public Async Function TestNamedFieldInitializer(mode As TestMode) As Task
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
            Await TestAsync(text, "Global.Color", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")>
        Public Async Function TestAttributeArguments1(mode As TestMode) As Task
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
            Await TestAsync(text, "Global.System.DayOfWeek", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")>
        Public Async Function TestAttributeArguments2(mode As TestMode) As Task
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
            Await TestAsync(text, "System.Double", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(853840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853840")>
        Public Async Function TestAttributeArguments3(mode As TestMode) As Task
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
            Await TestAsync(text, "System.String", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")>
        Public Async Function TestCatchFilterClause(mode As TestMode) As Task
            Dim text = "Try : Catch ex As Exception When [|goo()|]"
            Await TestInMethodAsync(text, "System.Boolean", mode)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")>
        Public Async Function TestCatchFilterClause1(mode As TestMode) As Task
            Dim text = "Try : Catch ex As Exception When [|goo|]"
            Await TestInMethodAsync(text, "System.Boolean", mode)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(994388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994388")>
        Public Async Function TestCatchFilterClause2() As Task
            Dim text = "Try : Catch ex As Exception When [|goo|].N"
            Await TestInMethodAsync(text, "System.Object", TestMode.Node)
        End Function

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(1041260, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041260")>
        Public Async Function ConditionalInvocation(mode As TestMode) As Task
            Dim text = "Dim args As String() : args?([|goo|])"
            Await TestInMethodAsync(text, "System.Int32", mode)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        Public Async Function TestAwaitExpressionWithChainingMethod() As Task
            Dim text = "Imports System
Imports System.Linq

Module M
    Async Sub T()
        Dim x As Boolean = Await [|F|].ContinueWith(Function(a) True).ContinueWith(Function(a) False)
    End Sub
End Module"
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of System.Object)", TestMode.Node)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        Public Async Function TestAwaitExpressionWithChainingMethod2() As Task
            Dim text = "Imports System
Imports System.Threading.Tasks

Module M
    Async Sub T()
        Dim x As Boolean = Await [|F|].ConfigureAwait(False)
    End Sub
End Module"
            Await TestAsync(text, "Global.System.Threading.Tasks.Task(Of System.Boolean)", TestMode.Node)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
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
            Await TestAsync(text, "System.Object", TestMode.Position)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestWhereCall() As Task
            Dim text =
"imports System.Collections.Generic
class C
    sub Goo()
        [|ints|].Where(function(i) i > 10)
    end sub
end class"
            Await TestAsync(text, "Global.System.Collections.Generic.IEnumerable(Of System.Int32)", TestMode.Node)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestWhereCall2() As Task
            Dim text =
"imports System.Collections.Generic
class C
    sub Goo()
        [|ints|].Where(function(i)
                return i > 10
            end function)
    end sub
end class"
            Await TestAsync(text, "Global.System.Collections.Generic.IEnumerable(Of System.Int32)", TestMode.Node)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestMemberAccess1() As Task
            Dim text =
"imports System.Collections.Generic
class C
    sub Goo()
        dim b as boolean = x.[||]
    end sub
end class"
            Await TestAsync(text, "System.Boolean", TestMode.Position)
        End Function

        <WorkItem(431509, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=431509&_a=edit&triage=true")>
        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function InvocationWithNoArguments() As Task
            Dim text =
"Module Program
    Sub Main(args As String())
        Dim z As IEnumerable(Of Integer)
        [|z|].Select
    End Sub
End Module"
            Await TestAsync(text, "System.Object", TestMode.Position)
        End Function

        <WorkItem(39333, "https://github.com/dotnet/roslyn/issues/39333")>
        <Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)>
        Public Async Function TestInferringAfterAwaitInAsync() As Task
            Dim text =
"Imports System.Threading.Tasks
Class C
    Private Async Function WaitForIt() As Task(Of Boolean)
        Return Await [||]
    End Function
End Class"
            Await TestAsync(text, "Task.FromResult(False)", TestMode.Position)
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression.VisualBasicSimplifyLinqExpressionDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.SimplifyLinqExpression.SimplifyLinqExpressionCodeFixProvider)

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpression)>
    Partial Public Class VisualBasicSimplifyLinqExpressionTests
        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestAllowedMethodTypes(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer)
        Dim test = [|data.Where(Function(x) x = 1).{methodName}()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer)
        Dim test = data.{methodName}(Function(x) x = 1)
    End Sub
End Module"

            Await VerifyVB.VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestWhereWithIndexMethodTypes(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer)
        Dim test = data.Where(Function(x, index) x = index).{methodName}()
    End Sub
End Module"

            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestQueryComprehensionSyntax(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer) = Nothing
        Dim test = [|(From x In data).Where(Function(x) x = 1).{methodName}()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer) = Nothing
        Dim test = (From x In data).{methodName}(Function(x) x = 1)
    End Sub
End Module"
            Await VerifyVB.VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestQueryComprehensionSyntaxNotUsed(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer) = Nothing
        Dim test = (From x In data Where x = 1).{methodName}()
    End Sub
End Module"

            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestMultiLineLambda(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer) = Nothing
        Dim test = [|data.Where(Function(x)
                                  Console.WriteLine(x)
                                  Return x = 1
                              End Function).{methodName}()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim data As IEnumerable(Of Integer) = Nothing
        Dim test = data.{methodName}(Function(x)
                                  Console.WriteLine(x)
                                  Return x = 1
                              End Function)
    End Sub
End Module"
            Await VerifyVB.VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestQueryableIsNotConsidered(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim testvar1 = New List(Of Integer) From {{ 1, 2, 3, 4, 5, 6, 7, 8 }}
        Dim testvar2 = testvar1.AsQueryable()
        Dim output = testvar2.Where(Function(x) x = 4).{methodName}()
    End Sub
End Module"
            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function

        <Theory, CombinatorialData>
        Public Shared Async Function TestNestedLambda(<CombinatorialValues(
                                                        "First",
                                                        "Last",
                                                        "Single",
                                                        "Any",
                                                        "SingleOrDefault",
                                                        "FirstOrDefault",
                                                        "LastOrDefault")>
                                                     firstMethod As String,
                                                      <CombinatorialValues(
                                                        "First",
                                                        "Last",
                                                        "Single",
                                                        "Any",
                                                        "Count",
                                                        "SingleOrDefault",
                                                        "FirstOrDefault",
                                                        "LastOrDefault")>
                                                     secondMethod As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = [|test.Where(Function(x) [|x.Where(Function(c) c.Equals(""!"")).{secondMethod}()|].Equals(""!"")).{firstMethod}()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = test.{firstMethod}(Function(x) x.{secondMethod}(Function(c) c.Equals(""!"")).Equals(""!""))
    End Sub
End Module"
            Await VerifyVB.VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestExplicitEnumerableCall(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = [|Enumerable.Where(test, Function(x) x = 1).{methodName}()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = Enumerable.{methodName}(test, Function(x) x = 1)
    End Sub
End Module"

            Await VerifyVB.VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("Last")>
        <InlineData("Single")>
        <InlineData("Any")>
        <InlineData("Count")>
        <InlineData("SingleOrDefault")>
        <InlineData("FirstOrDefault")>
        <InlineData("LastOrDefault")>
        Public Shared Async Function TestArgumentsInSecondCall(methodName As String) As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim testvar1 = New List(Of Integer) From {{ 1, 2, 3, 4, 5, 6, 7, 8 }}
        Dim output = testvar1.Where(Function(x) x = 4).{methodName}(Function(x) x <> 1)
    End Sub
End Module"
            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function

        <Fact>
        Public Shared Async Function TestUnsupportedFunction() As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic

Module T
    Sub M()
        Dim testvar1 = New List(Of Integer) From {{ 1, 2, 3, 4, 5, 6, 7, 8 }}
        Dim output = testvar1.Where(Function(x) x = 4).Count()
    End Sub
End Module"
            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function

        <Fact>
        Public Shared Async Function TestExpressionTreeInput() As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{""hello"", ""world"", ""!""}}
        Dim queryableData = test.AsQueryable()
        Dim pe = Expression.Parameter(GetType(String), ""place"")
        Dim left As Expression = Expression.Call(pe, GetType(String).GetMethod(""ToLower"", System.Type.EmptyTypes))
        Dim right As Expression = Expression.Constant(""coho winery"")
        Dim e1 = Expression.Equal(left, right)

        left = Expression.Property(pe, GetType(String).GetProperty(""Length""))
        right = Expression.Constant(16, GetType(Integer))
        Dim e2 = Expression.GreaterThan(left, right)

        Dim predicateBody = Expression.OrElse(e1, e2)
        Dim lambda1 = Function(num) num < 5
        Dim result = queryableData.Where(Expression.Lambda(Of Func(Of String, Boolean))(predicateBody, pe)).First()
    End Sub
End Module"
            Await VerifyVB.VerifyAnalyzerAsync(testCode)
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpression)>
    Partial Public Class VisualBasicSimplifyLinqExpressionTests
        <Fact>
        Public Shared Async Function FixAllInDocument() As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = [|test.Where(Function(x) x.Equals(""!"")).Any()|]
        Dim test2 = [|test.Where(Function(x) x.Equals(""!"")).SingleOrDefault()|]
        Dim test3 = [|test.Where(Function(x) x.Equals(""!"")).Last()|]
        Dim test4 = test.Where(Function(x) x.Equals(""!"")).Count()
        Dim test5 = [|test.Where(Function(x) x.Equals(""!"")).FirstOrDefault()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = test.Any(Function(x) x.Equals(""!""))
        Dim test2 = test.SingleOrDefault(Function(x) x.Equals(""!""))
        Dim test3 = test.Last(Function(x) x.Equals(""!""))
        Dim test4 = test.Where(Function(x) x.Equals(""!"")).Count()
        Dim test5 = test.FirstOrDefault(Function(x) x.Equals(""!""))
    End Sub
End Module"
            Await VisualBasicCodeFixVerifier(Of VisualBasicSimplifyLinqExpressionDiagnosticAnalyzer, VisualBasicSimplifyLinqExpressionCodeFixProvider).VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Fact>
        Public Shared Async Function FixAllInDocumentExplicitCall() As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = [|Enumerable.Where(test, Function(x) x.Equals(""!"")).Any()|]
        Dim test2 = [|Enumerable.Where(test, Function(x) x.Equals(""!"")).SingleOrDefault()|]
        Dim test3 = [|Enumerable.Where(test, Function(x) x.Equals(""!"")).Last()|]
        Dim test4 = [|Enumerable.Where(test, Function(x) x.Equals(""!"")).Count()|]
        Dim test5 = [|Enumerable.Where(test, Function(x) x.Equals(""!"")).FirstOrDefault()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = Enumerable.Any(test, Function(x) x.Equals(""!""))
        Dim test2 = Enumerable.SingleOrDefault(test, Function(x) x.Equals(""!""))
        Dim test3 = Enumerable.Last(test, Function(x) x.Equals(""!""))
        Dim test4 = Enumerable.Count(test, Function(x) x.Equals(""!""))
        Dim test5 = Enumerable.FirstOrDefault(test, Function(x) x.Equals(""!""))
    End Sub
End Module"
            Await VisualBasicCodeFixVerifier(Of VisualBasicSimplifyLinqExpressionDiagnosticAnalyzer, VisualBasicSimplifyLinqExpressionCodeFixProvider).VerifyCodeFixAsync(testCode, fixedCode)
        End Function

        <Fact>
        Public Shared Async Function NestedInDocument() As Task
            Dim testCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = [|test.Where(Function(x) x.Equals(""!"")).Any()|]
        Dim test2 = [|test.Where(Function(x) x.Equals(""!"")).SingleOrDefault()|]
        Dim test3 = [|test.Where(Function(x) x.Equals(""!"")).Last()|]
        Dim test4 = [|Enumerable.Where(test, Function(x) x.Equals(""!"")).Count()|]
        Dim test5 = [|test.Where(Function(x) [|x.Where(Function(s) s.Equals(""!"")).FirstOrDefault()|].Equals(""!"")).FirstOrDefault()|]
    End Sub
End Module"
            Dim fixedCode = $"
Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Linq.Expressions

Module T
    Sub M()
        Dim test = New List(Of String) From {{ ""hello"", ""world"", ""!"" }}
        Dim test1 = test.Any(Function(x) x.Equals(""!""))
        Dim test2 = test.SingleOrDefault(Function(x) x.Equals(""!""))
        Dim test3 = test.Last(Function(x) x.Equals(""!""))
        Dim test4 = Enumerable.Count(test, Function(x) x.Equals(""!""))
        Dim test5 = test.FirstOrDefault(Function(x) x.FirstOrDefault(Function(s) s.Equals(""!"")).Equals(""!""))
    End Sub
End Module"
            Await VisualBasicCodeFixVerifier(Of VisualBasicSimplifyLinqExpressionDiagnosticAnalyzer, VisualBasicSimplifyLinqExpressionCodeFixProvider).VerifyCodeFixAsync(testCode, fixedCode)
        End Function
    End Class
End Namespace

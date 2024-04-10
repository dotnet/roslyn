' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseCoalesceExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
    Public Class UseCoalesceExpressionForTernaryConditionalCheckTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer(),
                    New UseCoalesceExpressionForTernaryConditionalCheckCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestOnLeft_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If (x Is Nothing, y, x)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOnLeft_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If(x IsNot Nothing, x, y)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOnRight_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If(Nothing Is x, y, x)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOnRight_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If(Nothing IsNot x, x, y)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestComplexExpression() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If (x.ToString() is Nothing, y, x.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x.ToString(), y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParens1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If ((x Is Nothing), y, x)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParens2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If ((x) Is Nothing, y, x)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParens3() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If (x Is Nothing, y, (x))
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParens4() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If (x Is Nothing, (y), x)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z1 = {|FixAllInDocument:If|} (x is Nothing, y, x)
        Dim z2 = If(x IsNot Nothing, x, y)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z1 = If(x, y)
        Dim z2 = If(x, y)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string, z as string)
        dim w = {|FixAllInDocument:If|} (x isnot Nothing, x, If(y isnot Nothing, y, z))
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string, z as string)
        dim w = If(x, If(y, z))
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17028")>
        Public Async Function TestInExpressionOfT() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System
Imports System.Linq.Expressions

Class C
    Sub M(x as string, y as string)
        dim e as Expression(of Func(of string)) = function() [||]If (x isnot Nothing, x, y)
    End Sub
End Class",
"
Imports System
Imports System.Linq.Expressions

Class C
    Sub M(x as string, y as string)
        dim e as Expression(of Func(of string)) = function() {|Warning:If(x, y)|}
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = [||]If (x Is Nothing, y, x) ' comment
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as string, y as string)
        Dim z = If(x, y) ' comment
    End Sub
End Class")
        End Function
    End Class
End Namespace

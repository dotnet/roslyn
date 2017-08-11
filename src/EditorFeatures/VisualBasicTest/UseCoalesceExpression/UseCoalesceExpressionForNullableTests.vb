' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseCoalesceExpression
    Public Class UseCoalesceExpressionForNullableTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseCoalesceExpressionForNullableDiagnosticAnalyzer(),
                    New UseCoalesceExpressionForNullableCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestOnLeft_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = [||]If (Not x.HasValue, y, x.Value)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestOnLeft_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = [||]If(x.HasValue, x.Value, y)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestComplexExpression() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = [||]If (Not (x + y).HasValue, y, (x + y).Value)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = If((x + y), y)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestParens1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = [||]If ((Not x.HasValue), y, x.Value)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z = If(x, y)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z1 = {|FixAllInDocument:If|} (Not x.HasValue, y, x.Value)
        Dim z2 = If(x.HasValue, x.Value, y)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?)
        Dim z1 = If(x, y)
        Dim z2 = If(x, y)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?, z as Integer?)
        dim w = {|FixAllInDocument:If|} (x.HasValue, x.Value, If(y.HasValue, y.Value, z))
    End Sub
End Class",
"
Imports System

Class C
    Sub M(x as Integer?, y as Integer?, z as Integer?)
        dim w = If(x, If(y, z))
    End Sub
End Class")
        End Function

        <WorkItem(17028, "https://github.com/dotnet/roslyn/issues/17028")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
        Public Async Function TestInExpressionOfT() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System
Imports System.Linq.Expressions

Class C
    Sub M(x as integer?, y as integer)
        dim e as Expression(of Func(of integer)) = function() [||]If (x.HasValue, x.Value, y)
    End Sub
End Class",
"
Imports System
Imports System.Linq.Expressions

Class C
    Sub M(x as integer?, y as integer)
        dim e as Expression(of Func(of integer)) = function() {|Warning:If(x, y)|}
    End Sub
End Class")
        End Function
    End Class
End Namespace

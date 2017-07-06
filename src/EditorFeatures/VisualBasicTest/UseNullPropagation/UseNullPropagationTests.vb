' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseNullPropagation
    Partial Public Class UseNullPropagationTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseNullPropagationDiagnosticAnalyzer(),
                    New VisualBasicUseNullPropagationCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestLeft_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestMissingInVB12() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.ToString())
    End Sub
End Class", New TestParameters(VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestRight_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Nothing Is o, Nothing, o.ToString()
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestLeft_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o IsNot Nothing, o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestRight_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Nothing IsNot o, o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestIndexer() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o(0))
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?(0)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestConditionalAccess() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.B?.C)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.B?.C
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestMemberAccess() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.B)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.B
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestMissingOnSimpleMatch() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestParenthesizedCondition() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If ((o Is Nothing), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v1 = {|FixAllInDocument:If|} (o Is Nothing, Nothing, o.ToString())
        Dim v2 = If (o IsNot Nothing, o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v1 = o?.ToString()
        Dim v2 = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    void M(object o1, object o2)
        Dim v1 = {|FixAllInDocument:If|} (o1 Is Nothing, Nothing, o1.ToString(If(o2 Is Nothing, Nothing, o2.ToString()))
    End Sub
End Class",
"
Imports System

Class C
    void M(object o1, object o2)
        Dim v1 = o1?.ToString(o2?.ToString())
    End Sub
End Class")
        End Function
    End Class
End Namespace

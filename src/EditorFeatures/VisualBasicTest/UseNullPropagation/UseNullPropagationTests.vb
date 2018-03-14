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
        Public Async Function TestWithNullableType() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = [||]If (c IsNot Nothing, c.f, Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = c?.f
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndObjectCast() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = [||]If (DirectCast(c, Object) IsNot Nothing, c.f, Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = c?.f
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestNullable1() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Function M(o As String) As Integer?
        return [||]If (o Is Nothing, Nothing, o.Length)
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestNullable2() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Sub M(o As String)
        Dim x = [||]If (o Is Nothing, Nothing, o.Length)
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEquals1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (ReferenceEquals(o, Nothing), Nothing, o.ToString())
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEquals2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (ReferenceEquals(Nothing, o), Nothing, o.ToString())
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValue1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (ReferenceEquals(o, other), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValue2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (ReferenceEquals(other, o), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithObject1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(o, Nothing), Nothing, o.ToString())
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithObject2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(Nothing, o), Nothing, o.ToString())
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValueWithObject1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Object.ReferenceEquals(o, other), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValueWithObject2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Object.ReferenceEquals(other, o), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithOmittedArgument1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(o, ), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithOmittedArgument2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(, Nothing), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEquals1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not ReferenceEquals(o, Nothing), o.ToString(), Nothing)
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEquals2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not ReferenceEquals(Nothing, o), o.ToString(), Nothing)
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not ReferenceEquals(o, other), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not ReferenceEquals(other, o), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not Object.ReferenceEquals(o, Nothing), o.ToString(), Nothing)
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not Object.ReferenceEquals(Nothing, o), o.ToString(), Nothing)
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not Object.ReferenceEquals(o, other), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not Object.ReferenceEquals(other, o), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestEqualsWithLogicalNot() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not (o Is Nothing), o.ToString(), Nothing)
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

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestNotEqualsWithLogicalNot() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not (o IsNot Nothing), Nothing, o.ToString())
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


        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestEqualsOtherValueWithLogicalNot() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not (o Is other), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <WorkItem(23043, "https://github.com/dotnet/roslyn/issues/23043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
        Public Async Function TestNotEqualsOtherValueWithLogicalNot() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not (o IsNot other), Nothing, o.ToString())
    End Sub
End Class")
        End Function

    End Class
End Namespace

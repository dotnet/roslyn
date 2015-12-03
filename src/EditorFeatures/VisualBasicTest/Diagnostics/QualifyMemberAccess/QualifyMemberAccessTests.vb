' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Qualify
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.QualifyMemberAccess

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.QualifyMemberAccess
    Public Class QualifyMemberAccessTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicQualifyMemberAccessDiagnosticAnalyzer(), New VisualBasicQualifyMemberAccessCodeFixProvider())
        End Function

        Private Function TestAsyncWithOption(code As String, expected As String, opt As PerLanguageOption(Of Boolean)) As Task
            Return TestAsync(code, expected, options:=[Option](opt, True))
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_LHS() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Me.i = 1 : End Sub : End Class",
SimplificationOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_RHS() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : Dim x = [|i|] : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Dim x = Me.i : End Sub : End Class",
SimplificationOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_MethodArgument() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M(ii As Integer) : M([|i|]) : End Sub : End Class",
"Class C : Dim i As Integer : Sub M(ii As Integer) : M(Me.i) : End Sub : End Class",
SimplificationOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : Dim s = [|i|].ToString() : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Dim s = Me.i.ToString() : End Sub : End Class",
SimplificationOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Dim s As String : Sub M() : Dim x = [|s|]?.ToString() : End Sub : End Class",
"Class C : Dim s As String : Sub M() : Dim x = Me.s?.ToString() : End Sub : End Class",
SimplificationOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_LHS() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Me.i = 1 : End Sub : End Class",
SimplificationOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_RHS() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : Dim x = [|i|] : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Dim x = Me.i : End Sub : End Class",
SimplificationOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_MethodArgument() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M(ii As Integer) : M([|i|]) : End Sub : End Class",
"Class C : Property i As Integer : Sub M(ii As Integer) : M(Me.i) : End Sub : End Class",
SimplificationOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : Dim s = [|i|].ToString() : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Dim s = Me.i.ToString() : End Sub : End Class",
SimplificationOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Property s As String : Sub M() : Dim x = [|s|]?.ToString() : End Sub : End Class",
"Class C : Property s As String : Sub M() : Dim x = Me.s?.ToString() : End Sub : End Class",
SimplificationOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_FunctionCallWithReturnType() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As Integer : Return [|M|]() : End Function : End Class",
"Class C : Function M() As Integer : Return Me.M() : End Function : End Class",
SimplificationOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As String : Return [|M|]().ToString() : End Function : End Class",
"Class C : Function M() As String : Return Me.M().ToString() : End Function : End Class",
SimplificationOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As String : Return [|M|]()?.ToString() : End Function : End Class",
"Class C : Function M() As String : Return Me.M()?.ToString() : End Function : End Class",
SimplificationOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_EventSubscription1() As Task
            Await TestAsyncWithOption("
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, AddressOf [|Handler|]
    End Function
End Class",
"
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, AddressOf Me.Handler
    End Function
End Class",
SimplificationOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_EventSubscription2() As Task
            Await TestAsyncWithOption("
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, New EventHandler(AddressOf [|Handler|])
    End Function
End Class",
"
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, New EventHandler(AddressOf Me.Handler)
    End Function
End Class",
SimplificationOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyEventAccess_AddHandler() As Task
            Await TestAsyncWithOption("
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler [|e|], AddressOf Handler
    End Function
End Class",
"
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler Me.e, AddressOf Handler
    End Function
End Class",
SimplificationOptions.QualifyEventAccess)
        End Function

    End Class
End Namespace

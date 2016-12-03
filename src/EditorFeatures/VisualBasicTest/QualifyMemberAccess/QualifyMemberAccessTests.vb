﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QualifyMemberAccess
    Partial Public Class QualifyMemberAccessTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicQualifyMemberAccessDiagnosticAnalyzer(), New VisualBasicQualifyMemberAccessCodeFixProvider())
        End Function

        Private Function TestAsyncWithOption(code As String, expected As String, opt As PerLanguageOption(Of CodeStyleOption(Of Boolean))) As Task
            Return TestAsyncWithOptionAndNotification(code, expected, opt, NotificationOption.Error)
        End Function

        Private Function TestAsyncWithOptionAndNotification(code As String, expected As String, opt As PerLanguageOption(Of CodeStyleOption(Of Boolean)), notification As NotificationOption) As Task
            Return TestAsync(code, expected, options:=[Option](opt, True, notification))
        End Function

        Private Function TestMissingAsyncWithOption(code As String, opt As PerLanguageOption(Of CodeStyleOption(Of Boolean))) As Task
            Return TestMissingAsyncWithOptionAndNotification(code, opt, NotificationOption.Error)
        End Function

        Private Function TestMissingAsyncWithOptionAndNotification(code As String, opt As PerLanguageOption(Of CodeStyleOption(Of Boolean)), notification As NotificationOption) As Task
            Return TestMissingAsync(code, options:=[Option](opt, True, notification))
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_LHS() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Me.i = 1 : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_RHS() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : Dim x = [|i|] : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Dim x = Me.i : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_MethodArgument() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M(ii As Integer) : M([|i|]) : End Sub : End Class",
"Class C : Dim i As Integer : Sub M(ii As Integer) : M(Me.i) : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : Dim s = [|i|].ToString() : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Dim s = Me.i.ToString() : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Dim s As String : Sub M() : Dim x = [|s|]?.ToString() : End Sub : End Class",
"Class C : Dim s As String : Sub M() : Dim x = Me.s?.ToString() : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_OnAutoPropertyBackingField() As Task
            Await TestAsyncWithOption(
"Class C : Property I As Integer : Sub M() : [|_I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me._I = 1 : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_OnBase() As Task
            Await TestAsyncWithOption("
Class Base
    Protected i As Integer
End Class
Class Derived
    Inherits Base
    Sub M()
        [|i|] = 1
    End Sub
End Class
",
"
Class Base
    Protected i As Integer
End Class
Class Derived
    Inherits Base
    Sub M()
        Me.i = 1
    End Sub
End Class
",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption(
"Class C : Dim i As Integer : Sub M(c As C) : c.[|i|] = 1 : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_NotSuggestedOnSharedWithMe() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared i As Integer : Sub M() : Me.[|i|] = 1 : End Sub : End Class",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyFieldAccess_NotSuggestedInModule() As Task
            Await TestMissingAsyncWithOption(
"Module C : Dim i As Integer : Sub M() : [|i|] = 1 : End Sub : End Module",
CodeStyleOptions.QualifyFieldAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_LHS() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Me.i = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_RHS() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : Dim x = [|i|] : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Dim x = Me.i : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_MethodArgument() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M(ii As Integer) : M([|i|]) : End Sub : End Class",
"Class C : Property i As Integer : Sub M(ii As Integer) : M(Me.i) : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : Dim s = [|i|].ToString() : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Dim s = Me.i.ToString() : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Property s As String : Sub M() : Dim x = [|s|]?.ToString() : End Sub : End Class",
"Class C : Property s As String : Sub M() : Dim x = Me.s?.ToString() : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_OnBase() As Task
            Await TestAsyncWithOption("
Class Base
    Protected Property i As Integer
End Class
Class Derived
    Inherits Base
    Sub M()
        [|i|] = 1
    End Sub
End Class
",
"
Class Base
    Protected Property i As Integer
End Class
Class Derived
    Inherits Base
    Sub M()
        Me.i = 1
    End Sub
End Class
",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption(
"Class C : Property i As Integer : Sub M(c As C) : c.[|i|] = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyPropertyAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared Property i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_FunctionCallWithReturnType() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As Integer : Return [|M|]() : End Function : End Class",
"Class C : Function M() As Integer : Return Me.M() : End Function : End Class",
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As String : Return [|M|]().ToString() : End Function : End Class",
"Class C : Function M() As String : Return Me.M().ToString() : End Function : End Class",
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As String : Return [|M|]()?.ToString() : End Function : End Class",
"Class C : Function M() As String : Return Me.M()?.ToString() : End Function : End Class",
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
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
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
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
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7584"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_OnBase() As Task
            Await TestAsyncWithOption("
Class Base
    Protected Sub Method()
    End Sub
End Class
Class Derived
    Inherits Base
    Sub M()
        [|Method|]()
    End Sub
End Class
",
"
Class Base
    Protected Sub Method()
    End Sub
End Class
Class Derived
    Inherits Base
    Sub M()
        Me.Method()
    End Sub
End Class
",
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption(
"Class C : Sub M(c As C) : c.[|M|]() : End Sub : End Class",
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMethodAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared Sub Method() : End Sub : Sub M() : [|Method|]() : End Sub : End Class",
CodeStyleOptions.QualifyMethodAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
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
CodeStyleOptions.QualifyEventAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7587"), Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyEventAccess_OnBase() As Task
            Await TestAsyncWithOption("
Imports System
Class Base
    Protected Event e As EventHandler
End Class
Class Derived
    Inherits Base
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler [|e|], AddressOf Handler
    End Function
End Class",
"
Imports System
Class Base
    Protected Event e As EventHandler
End Class
Class Derived
    Inherits Base
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler Me.e, AddressOf Handler
    End Function
End Class",
CodeStyleOptions.QualifyEventAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyEventAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption("
Imports System
Class C
    Event e As EventHandler
    Sub M(c As C)
        AddHandler c.[|e|], AddressOf Handler
    End Sub
    Sub Handler(sender As Object, args As EventArgs)
    End Function
End Class",
CodeStyleOptions.QualifyEventAccess)
        End Function

        <WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyEventAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption("
Imports System
Class C
    Shared Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler [|e|], AddressOf Handler
    End Function
End Class",
CodeStyleOptions.QualifyEventAccess)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMemberAccessNotPresentOnNotificationOptionNone() As Task
            Await TestMissingAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.None)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMemberAccessOnNotificationOptionInfo() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.Suggestion)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMemberAccessOnNotificationOptionWarning() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.Warning)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        Public Async Function QualifyMemberAccessOnNotificationOptionError() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions.QualifyPropertyAccess, NotificationOption.Error)
        End Function

    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QualifyMemberAccess
    <Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
    Partial Public Class QualifyMemberAccessTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicQualifyMemberAccessDiagnosticAnalyzer(),
                    New VisualBasicQualifyMemberAccessCodeFixProvider())
        End Function

        Private Function TestAsyncWithOption(code As String, expected As String, opt As PerLanguageOption2(Of CodeStyleOption2(Of Boolean))) As Task
            Return TestAsyncWithOptionAndNotification(code, expected, opt, NotificationOption2.Error)
        End Function

        Private Function TestAsyncWithOptionAndNotification(code As String, expected As String, opt As PerLanguageOption2(Of CodeStyleOption2(Of Boolean)), notification As NotificationOption2) As Task
            Return TestInRegularAndScriptAsync(code, expected, options:=[Option](opt, True, notification))
        End Function

        Private Function TestMissingAsyncWithOption(code As String, opt As PerLanguageOption2(Of CodeStyleOption2(Of Boolean))) As Task
            Return TestMissingAsyncWithOptionAndNotification(code, opt, NotificationOption2.Error)
        End Function

        Private Function TestMissingAsyncWithOptionAndNotification(code As String, opt As PerLanguageOption2(Of CodeStyleOption2(Of Boolean)), notification As NotificationOption2) As Task
            Return TestMissingInRegularAndScriptAsync(code,
                New TestParameters(options:=[Option](opt, True, notification)))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_LHS() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Me.i = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_RHS() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : Dim x = [|i|] : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Dim x = Me.i : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_MethodArgument() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M(ii As Integer) : M([|i|]) : End Sub : End Class",
"Class C : Dim i As Integer : Sub M(ii As Integer) : M(Me.i) : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Dim i As Integer : Sub M() : Dim s = [|i|].ToString() : End Sub : End Class",
"Class C : Dim i As Integer : Sub M() : Dim s = Me.i.ToString() : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Dim s As String : Sub M() : Dim x = [|s|]?.ToString() : End Sub : End Class",
"Class C : Dim s As String : Sub M() : Dim x = Me.s?.ToString() : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_OnAutoPropertyBackingField() As Task
            Await TestAsyncWithOption(
"Class C : Property I As Integer : Sub M() : [|_I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me._I = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
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
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyFieldAccess_InObjectInitializer() As Task
            Await TestAsyncWithOption("
Class C
    Protected i As Integer = 1
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Class
",
"
Class C
    Protected i As Integer = 1
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { Me.i }
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyFieldAccess_InCollectionInitializer() As Task
            Await TestAsyncWithOption("
Class C
    Protected i As Integer = 1
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Class
",
"
Class C
    Protected i As Integer = 1
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { Me.i }
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption(
"Class C : Dim i As Integer : Sub M(c As C) : c.[|i|] = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_NotSuggestedOnSharedWithMe() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared i As Integer : Sub M() : Me.[|i|] = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyFieldAccess_NotSuggestedInModule() As Task
            Await TestMissingAsyncWithOption(
"Module C : Dim i As Integer : Sub M() : [|i|] = 1 : End Sub : End Module",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyFieldAccess_NotSuggestedOnLocalVarInObjectInitializer() As Task
            Await TestMissingAsyncWithOption(
"Class C
    Sub M()
        Dim i = 1
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Module",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyFieldAccess_NotSuggestedOnLocalVarInCollectionInitializer() As Task
            Await TestMissingAsyncWithOption(
"Class C
    Sub M()
        Dim i = 1
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Module",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_LHS() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Me.i = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_RHS() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : Dim x = [|i|] : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Dim x = Me.i : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_MethodArgument() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M(ii As Integer) : M([|i|]) : End Sub : End Class",
"Class C : Property i As Integer : Sub M(ii As Integer) : M(Me.i) : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Property i As Integer : Sub M() : Dim s = [|i|].ToString() : End Sub : End Class",
"Class C : Property i As Integer : Sub M() : Dim s = Me.i.ToString() : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Property s As String : Sub M() : Dim x = [|s|]?.ToString() : End Sub : End Class",
"Class C : Property s As String : Sub M() : Dim x = Me.s?.ToString() : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
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
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyPropertyAccess_InObjectInitializer() As Task
            Await TestAsyncWithOption("
Class C
    Protected Property i As Integer
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Class
",
"
Class C
    Protected Property i As Integer
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { Me.i }
    End Sub
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyPropertyAccess_InCollectionInitializer() As Task
            Await TestAsyncWithOption("
Class C
    Protected Property i As Integer
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Class
",
"
Class C
    Protected Property i As Integer
    Sub M()
        Dim test = New System.Collections.Generic.List(Of Integer) With { Me.i }
    End Sub
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption(
"Class C : Property i As Integer : Sub M(c As C) : c.[|i|] = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyPropertyAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared Property i As Integer : Sub M() : [|i|] = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_FunctionCallWithReturnType() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As Integer : Return [|M|]() : End Function : End Class",
"Class C : Function M() As Integer : Return Me.M() : End Function : End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_ChainedAccess() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As String : Return [|M|]().ToString() : End Function : End Class",
"Class C : Function M() As String : Return Me.M().ToString() : End Function : End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_ConditionalAccess() As Task
            Await TestAsyncWithOption(
"Class C : Function M() As String : Return [|M|]()?.ToString() : End Function : End Class",
"Class C : Function M() As String : Return Me.M()?.ToString() : End Function : End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_EventSubscription1() As Task
            Await TestAsyncWithOption("
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, AddressOf [|Handler|]
    End Sub
End Class",
"
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, AddressOf Me.Handler
    End Sub
End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_EventSubscription2() As Task
            Await TestAsyncWithOption("
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, New EventHandler(AddressOf [|Handler|])
    End Sub
End Class",
"
Imports System
Class C
    Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler e, New EventHandler(AddressOf Me.Handler)
    End Sub
End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
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
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_NotSuggestedOnInstance() As Task
            Await TestMissingAsyncWithOption(
"Class C : Sub M(c As C) : c.[|M|]() : End Sub : End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyMethodAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption(
"Class C : Shared Sub Method() : End Sub : Sub M() : [|Method|]() : End Sub : End Class",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyMethodAccess_NotSuggestedOnLocalVarInObjectInitializer() As Task
            Await TestMissingAsyncWithOption(
"Class C
    Sub M()
        Dim i = 1
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Module",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28509")>
        Public Async Function QualifyMethodAccess_NotSuggestedOnLocalVarInCollectionInitializer() As Task
            Await TestMissingAsyncWithOption(
"Class C
    Sub M()
        Dim i = 1
        Dim test = New System.Collections.Generic.List(Of Integer) With { [|i|] }
    End Sub
End Module",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7587")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
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
CodeStyleOptions2.QualifyEventAccess)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/7587")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
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
CodeStyleOptions2.QualifyEventAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
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
CodeStyleOptions2.QualifyEventAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7065")>
        Public Async Function QualifyEventAccess_NotSuggestedOnShared() As Task
            Await TestMissingAsyncWithOption("
Imports System
Class C
    Shared Event e As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        AddHandler [|e|], AddressOf Handler
    End Function
End Class",
CodeStyleOptions2.QualifyEventAccess)
        End Function

        <Fact>
        Public Async Function QualifyMemberAccessOnNotificationOptionSilent() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Silent)
        End Function

        <Fact>
        Public Async Function QualifyMemberAccessOnNotificationOptionInfo() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Suggestion)
        End Function

        <Fact>
        Public Async Function QualifyMemberAccessOnNotificationOptionWarning() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Warning)
        End Function

        <Fact>
        Public Async Function QualifyMemberAccessOnNotificationOptionError() As Task
            Await TestAsyncWithOptionAndNotification(
"Class C : Property I As Integer : Sub M() : [|I|] = 1 : End Sub : End Class",
"Class C : Property I As Integer : Sub M() : Me.I = 1 : End Sub : End Class",
CodeStyleOptions2.QualifyPropertyAccess, NotificationOption2.Error)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")>
        Public Async Function DoNotReportToQualify_IfMyBaseQualificationOnField() As Task
            Await TestMissingAsyncWithOption("
Class Base
    Protected Field As Integer
End Class
Class Derived
    Inherits Base
    Sub M()
        [|MyBase.Field|] = 0
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")>
        Public Async Function DoNotReportToQualify_IfMyClassQualificationOnField() As Task
            Await TestMissingAsyncWithOption("
Class C
    Private ReadOnly Field As Integer
    Sub M()
        [|MyClass.Field|] = 0
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")>
        Public Async Function DoNotReportToQualify_IfMyBaseQualificationOnProperty() As Task
            Await TestMissingAsyncWithOption("
Class Base
    Protected Overridable ReadOnly Property P As Integer
End Class
Class Derived
    Inherits Base
    Protected Overrides ReadOnly Property P As Integer
        Get
            Return [|MyBase.P|]
        End Get
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")>
        Public Async Function DoNotReportToQualify_IfMyClassQualificationOnProperty() As Task
            Await TestMissingAsyncWithOption("
Class C
    Dim i As Integer
    Protected Overridable ReadOnly Property P As Integer
    Sub M()
        Me.i = [|MyClass.P|]
    End Sub
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")>
        Public Async Function DoNotReportToQualify_IfMyBaseQualificationOnMethod() As Task
            Await TestMissingAsyncWithOption("
Class Base
    Protected Overridable Sub M
    End Sub
End Class
Class Derived
    Inherits Base
    Protected Overrides Sub M()
        Get
            Return [|MyBase.M|]()
        End Get
End Class
",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17711")>
        Public Async Function DoNotReportToQualify_IfMyClassQualificationOnMethod() As Task
            Await TestMissingAsyncWithOption("
Class C
    Protected Overridable Sub M()
    End Sub
    Sub M2()
        [|MyClass.M|]()
    End Sub
End Class
",
CodeStyleOptions2.QualifyMethodAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")>
        Public Async Function DoNotReportToQualify_IfInStaticContext1() As Task
            Await TestMissingAsyncWithOption("
Class C
    Private Value As String

    Shared Sub Test()
        Console.WriteLine([|Value|])
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21519")>
        Public Async Function DoNotReportToQualify_IfInStaticContext2() As Task
            Await TestMissingAsyncWithOption("
Class C
    Private Value As String
    Private Shared Field As String = NameOf([|Value|])
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32093")>
        Public Async Function DoNotReportToQualify_IfInBaseConstructor() As Task
            Await TestMissingAsyncWithOption("
Public Class Base
    Public ReadOnly Property Foo As String
    Public Sub New(ByVal foo As String)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Public Sub New()
        MyBase.New(NameOf([|Foo|]))
    End Sub
End Class",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22776")>
        Public Async Function DoNotReportToQualify_InObjectInitializer1() As Task
            Await TestMissingAsyncWithOption("
class C
    Public Foo As Integer

    Sub Bar()
        Dim c = New C() With { [|.Foo = 1|] }
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22776")>
        Public Async Function DoNotReportToQualify_InObjectInitializer2() As Task
            Await TestMissingAsyncWithOption("
class C
    Public Property Foo As Integer

    Sub Bar()
        Dim c = New C() With { [|.Foo|] = 1 }
    End Sub
End Class
",
CodeStyleOptions2.QualifyFieldAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")>
        Public Async Function DoNotReportToQualify_InAttribute1() As Task
            Await TestMissingAsyncWithOption("
Imports System

Class MyAttribute
    Inherits Attribute

    Public Sub New(name as String)
    End Sub
End Class

<My(NameOf([|Goo|]))>
Class C
    Private Property Goo As String
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")>
        Public Async Function DoNotReportToQualify_InAttribute2() As Task
            Await TestMissingAsyncWithOption("
Imports System

Class MyAttribute
    Inherits Attribute

    Public Sub New(name as String)
    End Sub
End Class

Class C
    <My(NameOf([|Goo|]))>
    Private Property Goo As String
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")>
        Public Async Function DoNotReportToQualify_InAttribute3() As Task
            Await TestMissingAsyncWithOption("
Imports System

Class MyAttribute
    Inherits Attribute

    Public Sub New(name as String)
    End Sub
End Class

Class C
    Private Property Goo As String

    <My(NameOf([|Goo|]))>
    Private Bar As Integer
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26893")>
        Public Async Function DoNotReportToQualify_InAttribute4() As Task
            Await TestMissingAsyncWithOption("
Imports System

Class MyAttribute
    Inherits Attribute

    Public Sub New(name as String)
    End Sub
End Class

Class C
    Private Property Goo As String

    Sub X(<My(NameOf([|Goo|]))>v as integer)
    End Sub    
End Class
",
CodeStyleOptions2.QualifyPropertyAccess)
        End Function
    End Class
End Namespace

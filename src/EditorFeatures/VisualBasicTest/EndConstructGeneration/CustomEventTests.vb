' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class CustomEventTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterCustomEvent() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
    Custom Event foo As System.EventHandler
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
    Custom Event foo As System.EventHandler
        AddHandler(value As EventHandler)

        End AddHandler
        RemoveHandler(value As EventHandler)

        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)

        End RaiseEvent
    End Event
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterCustomEventWithImportsStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Imports System
Class c1
    Custom Event foo As EventHandler
End Class",
                beforeCaret:={2, -1},
                after:="Imports System
Class c1
    Custom Event foo As EventHandler
        AddHandler(value As EventHandler)

        End AddHandler
        RemoveHandler(value As EventHandler)

        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)

        End RaiseEvent
    End Event
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterCustomEventWithMissingDelegateType() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Imports System
Class c1
    Custom Event foo As FooHandler
End Class",
                beforeCaret:={2, -1},
                after:="Imports System
Class c1
    Custom Event foo As FooHandler
        AddHandler(value As FooHandler)

        End AddHandler
        RemoveHandler(value As FooHandler)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterCustomEventWithNonDelegateType() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Imports System
Class c1
    Custom Event foo As Object
End Class",
                beforeCaret:={2, -1},
                after:="Imports System
Class c1
    Custom Event foo As Object
        AddHandler(value As Object)

        End AddHandler
        RemoveHandler(value As Object)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterCustomEventWithGenericType() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Imports System
Class c1
    Custom Event foo As EventHandler(Of ConsoleCancelEventArgs)
End Class",
                beforeCaret:={2, -1},
                after:="Imports System
Class c1
    Custom Event foo As EventHandler(Of ConsoleCancelEventArgs)
        AddHandler(value As EventHandler(Of ConsoleCancelEventArgs))

        End AddHandler
        RemoveHandler(value As EventHandler(Of ConsoleCancelEventArgs))

        End RemoveHandler
        RaiseEvent(sender As Object, e As ConsoleCancelEventArgs)

        End RaiseEvent
    End Event
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DoNotApplyAfterCustomEventAlreadyTerminated() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Imports System
Class c1
    Custom Event foo As EventHandler(Of ConsoleCancelEventArgs)
        AddHandler(value As EventHandler(Of ConsoleCancelEventArgs))

        End AddHandler
        RemoveHandler(value As EventHandler(Of ConsoleCancelEventArgs))

        End RemoveHandler
        RaiseEvent(sender As Object, e As ConsoleCancelEventArgs)

        End RaiseEvent
    End Event
End Class",
                caret:={2, -1})
        End Function
    End Class
End Namespace

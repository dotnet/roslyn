' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Sub TestApplyAfterCustomEvent()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Custom Event foo As System.EventHandler",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "    Custom Event foo As System.EventHandler",
                        "        AddHandler(value As EventHandler)",
                        "",
                        "        End AddHandler",
                        "        RemoveHandler(value As EventHandler)",
                        "",
                        "        End RemoveHandler",
                        "        RaiseEvent(sender As Object, e As EventArgs)",
                        "",
                        "        End RaiseEvent",
                        "    End Event",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterCustomEventWithImportsStatement()
            VerifyStatementEndConstructApplied(
                before:={"Imports System",
                         "Class c1",
                         "    Custom Event foo As EventHandler",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Imports System",
                        "Class c1",
                        "    Custom Event foo As EventHandler",
                        "        AddHandler(value As EventHandler)",
                        "",
                        "        End AddHandler",
                        "        RemoveHandler(value As EventHandler)",
                        "",
                        "        End RemoveHandler",
                        "        RaiseEvent(sender As Object, e As EventArgs)",
                        "",
                        "        End RaiseEvent",
                        "    End Event",
                        "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterCustomEventWithMissingDelegateType()
            VerifyStatementEndConstructApplied(
                before:={"Imports System",
                         "Class c1",
                         "    Custom Event foo As FooHandler",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Imports System",
                        "Class c1",
                        "    Custom Event foo As FooHandler",
                        "        AddHandler(value As FooHandler)",
                        "",
                        "        End AddHandler",
                        "        RemoveHandler(value As FooHandler)",
                        "",
                        "        End RemoveHandler",
                        "        RaiseEvent()",
                        "",
                        "        End RaiseEvent",
                        "    End Event",
                        "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterCustomEventWithNonDelegateType()
            VerifyStatementEndConstructApplied(
                before:={"Imports System",
                         "Class c1",
                         "    Custom Event foo As Object",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Imports System",
                        "Class c1",
                        "    Custom Event foo As Object",
                        "        AddHandler(value As Object)",
                        "",
                        "        End AddHandler",
                        "        RemoveHandler(value As Object)",
                        "",
                        "        End RemoveHandler",
                        "        RaiseEvent()",
                        "",
                        "        End RaiseEvent",
                        "    End Event",
                        "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterCustomEventWithGenericType()
            VerifyStatementEndConstructApplied(
                before:={"Imports System",
                         "Class c1",
                         "    Custom Event foo As EventHandler(Of ConsoleCancelEventArgs)",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Imports System",
                        "Class c1",
                        "    Custom Event foo As EventHandler(Of ConsoleCancelEventArgs)",
                        "        AddHandler(value As EventHandler(Of ConsoleCancelEventArgs))",
                        "",
                        "        End AddHandler",
                        "        RemoveHandler(value As EventHandler(Of ConsoleCancelEventArgs))",
                        "",
                        "        End RemoveHandler",
                        "        RaiseEvent(sender As Object, e As ConsoleCancelEventArgs)",
                        "",
                        "        End RaiseEvent",
                        "    End Event",
                        "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyAfterCustomEventAlreadyTerminated()
            VerifyStatementEndConstructNotApplied(
                text:={"Imports System",
                       "Class c1",
                       "    Custom Event foo As EventHandler(Of ConsoleCancelEventArgs)",
                       "        AddHandler(value As EventHandler(Of ConsoleCancelEventArgs))",
                       "",
                       "        End AddHandler",
                       "        RemoveHandler(value As EventHandler(Of ConsoleCancelEventArgs))",
                       "",
                       "        End RemoveHandler",
                       "        RaiseEvent(sender As Object, e As ConsoleCancelEventArgs)",
                       "",
                       "        End RaiseEvent",
                       "    End Event",
                       "End Class"},
                caret:={2, -1})
        End Sub
    End Class
End Namespace

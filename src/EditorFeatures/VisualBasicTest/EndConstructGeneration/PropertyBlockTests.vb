' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class PropertyBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForAutoProperty() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    Property foo As Integer",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForAutoPropertyWithEmptyParens() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    Property foo() As Integer",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WorkItem(530329)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMustInheritProperty() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"MustInherit Class C",
                       "    MustOverride Property foo(x as integer) As Integer",
                       "End Class"},
            caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyForPropertyWithParameters()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Property foo(i As Integer) As Integer",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "    Property foo(i As Integer) As Integer",
                        "        Get",
                        "",
                        "        End Get",
                        "        Set(value As Integer)",
                        "",
                        "        End Set",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForReadOnlyProperty() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    ReadOnly Property foo As Integer",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForReadOnlyPropertyAfterExistingGet() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    ReadOnly Property foo As Integer",
                       "        Get",
                       "",
                       "        End Get",
                       "    End Property",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForReadOnlyWithSecondGetPropertyAfterExistingGet() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    ReadOnly Property foo As Integer",
                       "        Get",
                       "",
                       "        End Get",
                       "",
                       "        Get",
                       "    End Property",
                       "End Class"},
                caret:={6, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForWriteOnlyProperty() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    WriteOnly Property foo As Integer",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyOnGetForRegularProperty()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Property foo As Integer",
                         "        Get",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "    Property foo As Integer",
                        "        Get",
                        "",
                        "        End Get",
                        "        Set(value As Integer)",
                        "",
                        "        End Set",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyOnSetForRegularProperty()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Property foo As Integer",
                         "        Set",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "    Property foo As Integer",
                        "        Set(value As Integer)",
                        "",
                        "        End Set",
                        "        Get",
                        "",
                        "        End Get",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForReadOnlyPropertyIfEndPropertyMissingWhenInvokedAfterProperty() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    ReadOnly Property foo As Integer",
                       "        Get",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyOnGetForRegularPropertyWithSetPresent()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Property foo As Integer",
                         "        Get",
                         "",
                         "        Set(ByVal value As Integer)",
                         "",
                         "        End Set",
                         "    End Property",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "    Property foo As Integer",
                        "        Get",
                        "",
                        "        End Get",
                        "",
                        "        Set(ByVal value As Integer)",
                        "",
                        "        End Set",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForWriteOnlyPropertyWithTypeCharacter() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    WriteOnly Property foo$",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536376)>
        Public Sub TestApplyForPropertyWithIndexer()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Property foo(arg as Integer) As Integer",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "    Property foo(arg as Integer) As Integer",
                        "        Get",
                        "",
                        "        End Get",
                        "        Set(value As Integer)",
                        "",
                        "        End Set",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536391)>
        Public Async Function DontApplyForDuplicateGet() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    ReadOnly Property foo As Integer",
                       "        Get",
                       "",
                       "        End Get",
                       "        Get",
                       "    End Property",
                       "End Class"},
                caret:={5, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536391)>
        Public Async Function DontApplyForDuplicateSet() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    WriteOnly Property foo As Integer",
                       "        Set(ByVal value As Integer)",
                       "",
                       "        End Set",
                       "        Set",
                       "    End Property",
                       "End Class"},
                caret:={5, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536391)>
        Public Async Function DontApplyForSetInReadOnly() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    ReadOnly Property foo As Integer",
                       "        Set",
                       "    End Property",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WorkItem(536391)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForGetInReadOnly() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    WriteOnly Property foo As Integer",
                       "        Get",
                       "    End Property",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInternationalCharacter() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "    WriteOnly Property fooæ",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WorkItem(544197)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyInsideAnInterface() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Interface IFoo",
                       "    Property Foo(x As Integer) As String",
                       "End Interface"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2096, "https://github.com/dotnet/roslyn/issues/2096")>
        Public Sub DontGenerateSetForReadonlyProperty()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Readonly Property foo(arg as Integer) As Integer",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "    Readonly Property foo(arg as Integer) As Integer",
                        "        Get",
                        "",
                        "        End Get",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2096, "https://github.com/dotnet/roslyn/issues/2096")>
        Public Sub DontGenerateGetForWriteonlyProperty()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "    Writeonly Property foo(arg as Integer) As Integer",
                         "End Class"},
                beforeCaret:={1, -1},
                after:={"Class c1",
                        "    Writeonly Property foo(arg as Integer) As Integer",
                        "        Set(value As Integer)",
                        "",
                        "        End Set",
                        "    End Property",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub
    End Class
End Namespace

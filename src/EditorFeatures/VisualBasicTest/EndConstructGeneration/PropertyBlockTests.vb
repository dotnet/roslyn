' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class PropertyBlockTests
        <WpfFact>
        Public Async Function DoNotApplyForAutoProperty() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    Property goo As Integer
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForAutoPropertyWithEmptyParens() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    Property goo() As Integer
End Class",
                caret:={1, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530329")>
        Public Async Function DoNotApplyForMustInheritProperty() As Task
            VerifyStatementEndConstructNotApplied(
                text:="MustInherit Class C
    MustOverride Property goo(x as integer) As Integer
End Class",
            caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyForPropertyWithParameters() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Property goo(i As Integer) As Integer
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
    Property goo(i As Integer) As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForReadOnlyProperty() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForReadOnlyPropertyAfterExistingGet() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get
    End Property
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForReadOnlyWithSecondGetPropertyAfterExistingGet() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get

        Get
    End Property
End Class",
                caret:={6, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForWriteOnlyProperty() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyOnGetForRegularProperty() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Property goo As Integer
        Get
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
    Property goo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyOnSetForRegularProperty() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Property goo As Integer
        Set
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
    Property goo As Integer
        Set(value As Integer)

        End Set
        Get

        End Get
    End Property
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForReadOnlyPropertyIfEndPropertyMissingWhenInvokedAfterProperty() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyOnGetForRegularPropertyWithSetPresent() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Property goo As Integer
        Get

        Set(ByVal value As Integer)

        End Set
    End Property
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
    Property goo As Integer
        Get

        End Get

        Set(ByVal value As Integer)

        End Set
    End Property
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForWriteOnlyPropertyWithTypeCharacter() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo$
End Class",
                caret:={1, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536376")>
        Public Async Function TestApplyForPropertyWithIndexer() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Property goo(arg as Integer) As Integer
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
    Property goo(arg as Integer) As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Async Function DoNotApplyForDuplicateGet() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get
        Get
    End Property
End Class",
                caret:={5, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Async Function DoNotApplyForDuplicateSet() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
        Set(ByVal value As Integer)

        End Set
        Set
    End Property
End Class",
                caret:={5, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Async Function DoNotApplyForSetInReadOnly() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Set
    End Property
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Async Function DoNotApplyForGetInReadOnly() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
        Get
    End Property
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInternationalCharacter() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property gooæ
End Class",
                caret:={1, -1})
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544197")>
        Public Async Function DoNotApplyInsideAnInterface() As Task
            VerifyStatementEndConstructNotApplied(
                text:="Interface IGoo
    Property Goo(x As Integer) As String
End Interface",
                caret:={1, -1})
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2096")>
        Public Async Function TestDoNotGenerateSetForReadonlyProperty() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Readonly Property goo(arg as Integer) As Integer
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
    Readonly Property goo(arg as Integer) As Integer
        Get

        End Get
    End Property
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2096")>
        Public Async Function TestDoNotGenerateGetForWriteonlyProperty() As Task
            VerifyStatementEndConstructApplied(
                before:="Class c1
    Writeonly Property goo(arg as Integer) As Integer
End Class",
                beforeCaret:={1, -1},
                after:="Class c1
    Writeonly Property goo(arg as Integer) As Integer
        Set(value As Integer)

        End Set
    End Property
End Class",
                afterCaret:={3, -1})
        End Sub
    End Class
End Namespace

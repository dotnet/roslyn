' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class PropertyBlockTests
        <WpfFact>
        Public Sub DoNotApplyForAutoProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    Property goo As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForAutoPropertyWithEmptyParens()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    Property goo() As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530329")>
        Public Sub DoNotApplyForMustInheritProperty()
            VerifyStatementEndConstructNotApplied(
                text:="MustInherit Class C
    MustOverride Property goo(x as integer) As Integer
End Class",
            caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyForPropertyWithParameters()
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
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForReadOnlyProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForReadOnlyPropertyAfterExistingGet()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForReadOnlyWithSecondGetPropertyAfterExistingGet()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get

        Get
    End Property
End Class",
                caret:={6, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForWriteOnlyProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyOnGetForRegularProperty()
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
        End Sub

        <WpfFact>
        Public Sub TestApplyOnSetForRegularProperty()
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
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForReadOnlyPropertyIfEndPropertyMissingWhenInvokedAfterProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub TestApplyOnGetForRegularPropertyWithSetPresent()
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
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForWriteOnlyPropertyWithTypeCharacter()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo$
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536376")>
        Public Sub TestApplyForPropertyWithIndexer()
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
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DoNotApplyForDuplicateGet()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get
        Get
    End Property
End Class",
                caret:={5, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DoNotApplyForDuplicateSet()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
        Set(ByVal value As Integer)

        End Set
        Set
    End Property
End Class",
                caret:={5, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DoNotApplyForSetInReadOnly()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Set
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DoNotApplyForGetInReadOnly()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
        Get
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInternationalCharacter()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property gooæ
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544197")>
        Public Sub DoNotApplyInsideAnInterface()
            VerifyStatementEndConstructNotApplied(
                text:="Interface IGoo
    Property Goo(x As Integer) As String
End Interface",
                caret:={1, -1})
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2096")>
        Public Sub TestDoNotGenerateSetForReadonlyProperty()
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
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2096")>
        Public Sub TestDoNotGenerateGetForWriteonlyProperty()
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

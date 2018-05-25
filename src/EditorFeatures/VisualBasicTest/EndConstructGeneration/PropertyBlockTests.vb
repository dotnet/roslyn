' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class PropertyBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForAutoProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    Property goo As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForAutoPropertyWithEmptyParens()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    Property goo() As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WorkItem(530329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530329")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForMustInheritProperty()
            VerifyStatementEndConstructNotApplied(
                text:="MustInherit Class C
    MustOverride Property goo(x as integer) As Integer
End Class",
            caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForReadOnlyProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForReadOnlyPropertyAfterExistingGet()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get

        End Get
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForReadOnlyWithSecondGetPropertyAfterExistingGet()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForWriteOnlyProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForReadOnlyPropertyIfEndPropertyMissingWhenInvokedAfterProperty()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Get
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForWriteOnlyPropertyWithTypeCharacter()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo$
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536376, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536376")>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DontApplyForDuplicateGet()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DontApplyForDuplicateSet()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(536391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        Public Sub DontApplyForSetInReadOnly()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    ReadOnly Property goo As Integer
        Set
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WorkItem(536391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536391")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForGetInReadOnly()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property goo As Integer
        Get
    End Property
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInternationalCharacter()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
    WriteOnly Property gooæ
End Class",
                caret:={1, -1})
        End Sub

        <WorkItem(544197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544197")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyInsideAnInterface()
            VerifyStatementEndConstructNotApplied(
                text:="Interface IGoo
    Property Goo(x As Integer) As String
End Interface",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2096, "https://github.com/dotnet/roslyn/issues/2096")>
        Public Sub TestDontGenerateSetForReadonlyProperty()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        <WorkItem(2096, "https://github.com/dotnet/roslyn/issues/2096")>
        Public Sub TestDontGenerateGetForWriteonlyProperty()
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

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InterfaceKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceInNamespace()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Interface")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceInNamespaceFollowsTypeDeclaration()
            Dim code =
<File>
Namespace N1
    Class C1

    End Class
    |
End Namespace
</File>
            VerifyRecommendationsContain(code, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceInInterface()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Interface")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceFollowsInterface()
            Dim code =
<File>
Interface I1
End Interface
|
</File>
            VerifyRecommendationsContain(code, "Interface")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceFollowsMismatchedEnd()
            Dim code =
<File>
Interface I1
End Class
|
End Interface
</File>
            VerifyRecommendationsContain(code, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotInEnum()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceInStructure()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterPartial()
            VerifyRecommendationsContain(<File>Partial |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterPublicInFile()
            VerifyRecommendationsContain(<File>Public |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterPublicInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceMissingAfterProtectedInFile()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceExistsAfterProtectedInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterFriendInFile()
            VerifyRecommendationsContain(<File>Friend |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterFriendInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterPrivateInFile()
            VerifyRecommendationsMissing(<File>Private |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterPrivateInNestedClass()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterPrivateInNamespace()
            VerifyRecommendationsMissing(<File>
Namespace Foo
    Private |
End Namespace</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterProtectedFriendInFile()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterProtectedFriendInClass()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterOverloads()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterNotOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterMustOverride()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterMustOverrideOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterNotOverridableOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterMustInherit()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceAfterNotInheritable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InterfaceNotAfterShared()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Interface")
        End Sub

        <WorkItem(547254)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterAsync()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Interface")
        End Sub
    End Class
End Namespace

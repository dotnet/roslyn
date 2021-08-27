﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EnumKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Enum")
        End Sub

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumFollowsEnumTest()
            Dim code =
<File>
Enum E
End Enum
|
</File>
            VerifyRecommendationsContain(code, "Enum")
        End Sub

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumFollowsEnumWithinClassTest()
            Dim code =
<File>
Class C
    Enum E
    End Enum
    |
End Class
</File>
            VerifyRecommendationsContain(code, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInNamespaceTest()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Enum")
        End Sub

        <Fact, WorkItem(530727, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530727")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumFollowsMismatchedEndTest()
            Dim code =
<File>
Interface I1
End Class
|
End Interface
</File>
            VerifyRecommendationsContain(code, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterPartialTest()
            VerifyRecommendationsMissing(<File>Partial |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterPublicInFileTest()
            VerifyRecommendationsContain(<File>Public |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterPublicInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumMissingAfterProtectedInFileTest()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumExistsAfterProtectedInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterFriendInFileTest()
            VerifyRecommendationsContain(<File>Friend |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterFriendInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterPrivateInFileTest()
            VerifyRecommendationsMissing(<File>Private |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterPrivateInNestedClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterPrivateInNamespaceTest()
            VerifyRecommendationsMissing(<File>
Namespace Goo
    Private |
End Namespace</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterProtectedFriendInFileTest()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterProtectedFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterSharedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Enum")
        End Sub
    End Class
End Namespace

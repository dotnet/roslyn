' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EnumKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Enum")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumFollowsEnum()
            Dim code =
<File>
Enum E
End Enum
|
</File>
            VerifyRecommendationsContain(code, "Enum")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumFollowsEnumWithinClass()
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
        Public Sub EnumNotInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInNamespace()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInInterface()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Enum")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumFollowsMismatchedEnd()
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
        Public Sub EnumNotInEnum()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInStructure()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterPartial()
            VerifyRecommendationsMissing(<File>Partial |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterPublicInFile()
            VerifyRecommendationsContain(<File>Public |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterPublicInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumMissingAfterProtectedInFile()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumExistsAfterProtectedInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterFriendInFile()
            VerifyRecommendationsContain(<File>Friend |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterFriendInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterPrivateInFile()
            VerifyRecommendationsMissing(<File>Private |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterPrivateInNestedClass()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterPrivateInNamespace()
            VerifyRecommendationsMissing(<File>
Namespace Foo
    Private |
End Namespace</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterProtectedFriendInFile()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterProtectedFriendInClass()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterOverloads()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterNotOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterMustOverride()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterMustOverrideOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterNotOverridableOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterMustInherit()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumAfterNotInheritable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Enum")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EnumNotAfterShared()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Enum")
        End Sub
    End Class
End Namespace

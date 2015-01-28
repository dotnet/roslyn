' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ModuleKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleInFile()
            VerifyRecommendationsContain(<File>|</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleInNamespace()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotInInterface()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotInEnum()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotInStructure()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleAfterPartial()
            VerifyRecommendationsContain(<File>Partial |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleAfterPublic()
            VerifyRecommendationsContain(<File>Public |</File>, "Module")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleAfterEndModule()
            Dim code =
<File>
Module M1

End Module
|
</File>
            VerifyRecommendationsContain(code, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleFollowsDelegateDeclaration()
            Dim code =
<File>
Delegate Sub DelegateType()
|
</File>
            VerifyRecommendationsContain(code, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterPublicInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>Public |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterProtectedInFile()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterProtectedInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleAfterFriendInFile()
            VerifyRecommendationsContain(<File>Friend |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterFriendInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>Friend |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterPrivateInFile()
            VerifyRecommendationsMissing(<File>Private |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterPrivateInNestedClass()
            VerifyRecommendationsMissing(<ClassDeclaration>Private |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterPrivateInNamespace()
            VerifyRecommendationsMissing(<NamespaceDeclaration>Private |</NamespaceDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterProtectedFriendInFile()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterProtectedFriendInClass()
            VerifyRecommendationsMissing(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterOverloads()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterNotOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterMustOverride()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterMustOverrideOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterNotOverridableOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterMustInherit()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterNotInheritable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ModuleNotAfterShared()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Module")
        End Sub

    End Class
End Namespace

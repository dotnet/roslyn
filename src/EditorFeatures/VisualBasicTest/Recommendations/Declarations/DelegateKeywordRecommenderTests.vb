' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class DelegateKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateInNamespace()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateInInterface()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotInEnum()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateInStructure()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterPartial()
            VerifyRecommendationsMissing(<File>Partial |</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateAfterPublicInFile()
            VerifyRecommendationsContain(<File>Public |</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateAfterPublicInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateMissingAfterProtectedInFile()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateExistsAfterProtectedInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateAfterFriendInFile()
            VerifyRecommendationsContain(<File>Friend |</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateAfterFriendInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterPrivateInFile()
            VerifyRecommendationsMissing(<File>Private |</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateAfterPrivateInNestedClass()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterPrivateInNamespace()
            VerifyRecommendationsMissing(<File>
Namespace Foo
    Private |
End Namespace</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterProtectedFriendInFile()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateAfterProtectedFriendInClass()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterOverloads()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterNotOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterMustOverride()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterMustOverrideOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterNotOverridableOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterMustInherit()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterNotInheritable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Delegate")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DelegateNotAfterShared()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Delegate")
        End Sub
    End Class
End Namespace

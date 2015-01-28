' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ClassKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Class")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassFollowsClass()
            Dim code =
<File>
Class C1

End Class
|
</File>
            VerifyRecommendationsContain(code, "Class")
        End Sub

        <Fact, WorkItem(530727)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassPrecedesClass()
            Dim code =
<File>
|
Class C1

End Class
</File>
            VerifyRecommendationsContain(code, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassFollowsDelegateDeclaration()
            Dim code =
<File>
Delegate Sub DelegateType()
|
</File>
            VerifyRecommendationsContain(code, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassInNamespace()
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassInInterface()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotInEnum()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassInStructure()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterPartial()
            VerifyRecommendationsContain(<File>Partial |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterPublicInFile()
            VerifyRecommendationsContain(<File>Public |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterPublicInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassMissingAfterProtectedInFile()
            VerifyRecommendationsMissing(<File>Protected |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassExistsAfterProtectedInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterFriendInFile()
            VerifyRecommendationsContain(<File>Friend |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterFriendInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterPrivateInFile()
            VerifyRecommendationsMissing(<File>Private |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterPrivateInNestedClass()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterPrivateInNamespace()
            VerifyRecommendationsMissing(<File>
Namespace Foo
    Private |
End Namespace</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterProtectedFriendInFile()
            VerifyRecommendationsMissing(<File>Protected Friend |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterProtectedFriendInClass()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterOverloads()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterNotOverridable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterMustOverride()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterMustOverrideOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterNotOverridableOverrides()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterMustInherit()
            VerifyRecommendationsContain(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassAfterNotInheritable()
            VerifyRecommendationsContain(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassNotAfterShared()
            VerifyRecommendationsMissing(<ClassDeclaration>Shared |</ClassDeclaration>, "Class")
        End Sub

        <WorkItem(547254)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterAsync()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Class")
        End Sub
    End Class
End Namespace

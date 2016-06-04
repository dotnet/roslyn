' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class IteratorKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeywordsAfterIteratorTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Iterator |</ClassDeclaration>,
                                            "Friend", "Function", "Private", "Property", "Protected", "Protected Friend", "Public")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInInterfaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterPrivateTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterProtectedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterProtectedFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterFriendProtectedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend Protected |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterPublicTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterShadowsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shadows |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterWithEventsTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WithEvents |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterFunctionTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Function |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterSubTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterPropertyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Property |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterReadOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterAsyncTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterDeclareTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterClassTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Class |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Enum |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterInterfaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Interface |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterStructureTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Structure |</ClassDeclaration>, "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterWriteOnlySharedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly Shared |</ClassDeclaration>, "Iterator")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Iterator")
        End Function
    End Class
End Namespace

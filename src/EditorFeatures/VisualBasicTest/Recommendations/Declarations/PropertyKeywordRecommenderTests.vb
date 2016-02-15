' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class PropertyKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterPartialTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterPublicTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterProtectedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterPrivateTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterNotOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterDefaultTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterReadOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function PropertyAfterOverridesModifierTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides Public |</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterExitInFinallyBlockTest() As Task
            Dim code =
<ClassDeclaration>
Property P() As Integer
    Get
        Try
        Finally
            Exit |
</ClassDeclaration>

            Await VerifyRecommendationsMissingAsync(code, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExitTest() As Task
            Dim code =
<ClassDeclaration>
Property P As Integer
    Get
        Exit |
</ClassDeclaration>

            Await VerifyRecommendationsContainAsync(code, "Property")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>
Property P As Integer
    Get
        Exit 
 |
</ClassDeclaration>, "Property")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>
Property P As Integer
    Get
        Exit _
 |
</ClassDeclaration>, "Property")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterIteratorTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Iterator |</ClassDeclaration>, "Property")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Property")
        End Function
    End Class
End Namespace

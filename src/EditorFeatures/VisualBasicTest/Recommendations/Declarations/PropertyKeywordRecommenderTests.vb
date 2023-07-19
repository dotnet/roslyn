' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class PropertyKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub PropertyInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterMustOverrideOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterNotOverridableOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterDefaultTest()
            VerifyRecommendationsContain(<ClassDeclaration>Default |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterReadOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterWriteOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub PropertyAfterOverridesModifierTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides Public |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<ClassDeclaration>
Property P() As Integer
    Get
        Try
        Finally
            Exit |
</ClassDeclaration>

            VerifyRecommendationsMissing(code, "Property")
        End Sub

        <Fact>
        Public Sub AfterExitTest()
            Dim code =
<ClassDeclaration>
Property P As Integer
    Get
        Exit |
</ClassDeclaration>

            VerifyRecommendationsContain(code, "Property")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>
Property P As Integer
    Get
        Exit 
 |
</ClassDeclaration>, "Property")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>
Property P As Integer
    Get
        Exit _
 |
</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>
Property P As Integer
    Get
        Exit _ ' Test
 |
</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        Public Sub AfterIteratorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Iterator |</ClassDeclaration>, "Property")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Property")
        End Sub
    End Class
End Namespace

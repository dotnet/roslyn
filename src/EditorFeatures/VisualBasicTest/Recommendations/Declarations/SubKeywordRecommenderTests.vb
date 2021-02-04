' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class SubKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterPartialTest()
            VerifyRecommendationsContain(<ClassDeclaration>Partial |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterMustOverrideOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterNotOverridableOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Sub")
        End Sub

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubInDelegateCreationTest()
            Dim code = <ModuleDeclaration>
Module Program
    Sub Main(args As String())
        Dim f1 As New Goo2( |
    End Sub
 
    Delegate Sub Goo2()
 
    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</ModuleDeclaration>

            VerifyRecommendationsContain(code, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAfterOverridesModifierTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides Public |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<ClassDeclaration>
Sub M()
    Try
    Finally
        Exit |
</ClassDeclaration>

            VerifyRecommendationsMissing(code, "Sub")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>
Sub M()
        Exit 
|
</ClassDeclaration>, "Sub")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>
Sub M()
        Exit _
|
</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>
Sub M()
        Exit _ ' Test
|
</ClassDeclaration>, "Sub")
        End Sub

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterAsyncTest()
            VerifyRecommendationsContain(<ClassDeclaration>Async |</ClassDeclaration>, "Sub")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterIteratorTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Iterator |</ClassDeclaration>, "Sub")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Sub")
        End Sub

        <WorkItem(20837, "https://github.com/dotnet/roslyn/issues/20837")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExtensionAttribute()
            VerifyRecommendationsContain(<ClassDeclaration>&lt;Extension&gt; |</ClassDeclaration>, "Sub")
        End Sub
    End Class
End Namespace

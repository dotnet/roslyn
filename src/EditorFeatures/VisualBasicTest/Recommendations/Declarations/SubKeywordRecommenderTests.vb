' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class SubKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotInMethodDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotInNamespaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<EnumDeclaration>|</EnumDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>|</ModuleDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterPartialTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Partial |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterPublicTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Public |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterProtectedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterPrivateTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Private |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterProtectedFriendTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterOverloadsTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overloads |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overridable |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterNotOverridableTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterMustOverrideTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterMustOverrideOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterNotOverridableOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterDefaultTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Default |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterMustInheritTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterNotInheritableTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterNarrowingTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterWideningTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Widening |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterReadOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterWriteOnlyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubNotAfterCustomTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Custom |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterSharedTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Shared |</ClassDeclaration>, "Sub")
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubInDelegateCreationTest() As Task
            Dim code = <ModuleDeclaration>
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo2( |
    End Sub
 
    Delegate Sub Foo2()
 
    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</ModuleDeclaration>


            Await VerifyRecommendationsContainAsync(code, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterOverridesTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAfterOverridesModifierTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Overrides Public |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterExitInFinallyBlockTest() As Task
            Dim code =
<ClassDeclaration>
Sub M()
    Try
    Finally
        Exit |
</ClassDeclaration>

            Await VerifyRecommendationsMissingAsync(code, "Sub")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>
Sub M()
        Exit 
|
</ClassDeclaration>, "Sub")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>
Sub M()
        Exit _
|
</ClassDeclaration>, "Sub")
        End Function

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterAsyncTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterIteratorTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Iterator |</ClassDeclaration>, "Sub")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Sub")
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class DimKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub DimInMethodDeclarationTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterStaticInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>Static |</MethodBody>, "Dim")
        End Sub

        <Fact>
        Public Sub DimInMultiLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
End Sub</MethodBody>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Sub() |</MethodBody>, "Dim")
        End Sub

        <Fact>
        Public Sub DimInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotInInterfaceTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterPartialTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Partial |</ClassDeclaration>, "Dim")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545036")>
        Public Sub DimNotAfterDimTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterOverloadsTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overloads |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overrides |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Overridable |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterNotOverridableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterMustOverrideTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterMustOverrideOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterNotOverridableOverridesTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterReadOnlyTest()
            VerifyRecommendationsContain(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Dim")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542720")>
        Public Sub DimInSingleLineIfTest()
            VerifyRecommendationsContain(<MethodBody>If True Then Di|</MethodBody>, "Dim")
        End Sub

        <Fact>
        Public Sub DimAfterSingleLineLambdaTest()
            Dim code =
<MethodBody>
Dim X = Function() True
|
</MethodBody>

            VerifyRecommendationsContain(code, "Dim")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Dim")
        End Sub
    End Class
End Namespace

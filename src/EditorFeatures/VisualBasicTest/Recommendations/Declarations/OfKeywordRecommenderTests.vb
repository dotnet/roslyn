' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class OfKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfAfterPossibleMethodTypeParamTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotAfterMethodTypeParamTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo(Of T)(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfDefinitelyInMethodTypeParamTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo(|)(x As Integer)</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfAfterPossibleDelegateTypeParamTest()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub Goo(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotAfterDelegateTypeParamTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Delegate Sub Goo(Of T)(|</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfDefinitelyInDelegateTypeParamTest()
            VerifyRecommendationsContain(<ClassDeclaration>Delegate Sub Goo(|)(x As Integer)</ClassDeclaration>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInClassDeclarationTypeParamTest()
            VerifyRecommendationsContain(<File>Class Goo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInStructureDeclarationTypeParamTest()
            VerifyRecommendationsContain(<File>Structure Goo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInInterfaceDeclarationTypeParamTest()
            VerifyRecommendationsContain(<File>Interface Goo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotInEnumDeclarationTest()
            ' This is invalid code, so make sure we don't show it
            VerifyRecommendationsMissing(<File>Enum Goo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotInModuleDeclarationTest()
            VerifyRecommendationsMissing(<File>Module Goo(|</File>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInVariableDeclaration1Test()
            VerifyRecommendationsMissing(<MethodBody>Dim f As Goo(|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInVariableDeclaration2Test()
            VerifyRecommendationsContain(<MethodBody>Dim f As New Goo(|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfNotInRealArraySpecifierTest()
            VerifyRecommendationsMissing(<MethodBody>Dim f(|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInMethodCallTest()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Of")
        End Sub

        <WorkItem(541636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541636")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OfInGenericArrayBoundRankSpecifierTest()
            VerifyRecommendationsContain(<MethodBody>Dim i As List(|</MethodBody>, "Of")
        End Sub

        <WorkItem(541636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541636")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoOfInNonGenericArrayBoundRankSpecifierTest()
            VerifyRecommendationsMissing(<MethodBody>Dim i As Integer(|</MethodBody>, "Of")
        End Sub

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInNonGenericDelegateCreationTest()
            Dim code =
<File>
Class C
    Delegate Sub Goo()

    Sub Main(args As String())
        Dim f1 As New Goo(|
    End Sub
End Class
</File>

            VerifyRecommendationsMissing(code, "Of")
        End Sub

        <WorkItem(529552, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529552")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InGenericDelegateCreationTest()
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Delegate Sub Goo(Of C)()

    Sub Main(args As String())
        Dim f1 As New Goo(|
    End Sub
End Class
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "Of")
        End Sub

        <WorkItem(529552, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529552")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InPotentiallyGenericDelegateCreationTest()
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Delegate Sub Goo()
    Delegate Sub Goo(Of C)()

    Sub Main(args As String())
        Dim f1 As New Goo(|
    End Sub
End Class
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "Of")
        End Sub

        <WorkItem(529552, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529552")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInNonGenericDelegateCreationWithGenericTypeOfSameNameTest()
            Dim code =
<File>
Class Goo(Of U)
End Class
Class C
    Delegate Sub Goo()

    Sub Main(args As String())
        Dim f1 As New Goo(|
    End Sub
End Class
</File>

            VerifyRecommendationsMissing(code, "Of")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterEolTest()
            VerifyRecommendationsContain(
<MethodBody>Goo(
|</MethodBody>, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InImplementsClauseTest()
            Dim code =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class G(Of U)
    Implements IEquatable(Of U)

    Public Function Equals(other As U) As Boolean Implements IEquatable(|
        Throw New NotImplementedException()
    End Function
End Class
</File>

            VerifyRecommendationsContain(code, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInheritsStatementTest()
            Dim code =
<File>
Class G(Of T)
End Class

Class DG
    Inherits G(|
End Class
</File>

            VerifyRecommendationsContain(code, "Of")
        End Sub
    End Class
End Namespace

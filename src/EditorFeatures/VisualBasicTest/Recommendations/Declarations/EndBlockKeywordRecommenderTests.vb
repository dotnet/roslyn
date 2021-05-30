﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EndBlockKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubInBrokenMethodBodyTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo()
|</ClassDeclaration>, "End Sub")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterCompletedMethodBodyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Sub Goo()
End Sub
|</ClassDeclaration>, "End")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterMustOverrideMethodDeclaration1Test()
            VerifyRecommendationsMissing(<ClassDeclaration>
MustOverride Sub Goo()
|</ClassDeclaration>, "End")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterMustOverrideMethodDeclaration2Test()
            VerifyRecommendationsMissing(<ClassDeclaration>
MustOverride Sub Goo()
|</ClassDeclaration>, "End Sub")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndPropertyAfterIncompleteProperty1Test()
            VerifyRecommendationsContain(<ClassDeclaration>Property goo As Integer
Get
End Get
Set(value As Integer)
End Set
|</ClassDeclaration>, "End Property")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndPropertyAfterIncompleteProperty2Test()
            VerifyRecommendationsContain(<ClassDeclaration>Property goo As Integer
Get
End Get
Set(value As Integer)
End Set
End |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim goo = Sub()
|</MethodBody>, "End Sub")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndIfInMethodBody1Test()
            VerifyRecommendationsContain(<MethodBody>If True Then
|</MethodBody>, "End", "End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndIfInMethodBody2Test()
            VerifyRecommendationsContain(<MethodBody>If True Then
End |</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndWithInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>With goo
|</MethodBody>, "End With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndWhileInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>While goo
|</MethodBody>, "End While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSelectInMethodBody1Test()
            VerifyRecommendationsContain(<MethodBody>Select goo
|</MethodBody>, "End Select")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSelectInMethodBody2Test()
            VerifyRecommendationsContain(<MethodBody>Select goo
Case 1
|</MethodBody>, "End Select")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSelectInMethodBody3Test()
            VerifyRecommendationsContain(<MethodBody>Select goo
Case 1
Case Else
|</MethodBody>, "End Select")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSyncLockInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>SyncLock goo
|</MethodBody>, "End SyncLock")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModuleInFile1Test()
            VerifyRecommendationsContain(<File>Module Goo
|</File>, {"End Module"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModuleInFile2Test()
            VerifyRecommendationsContain(<File>
Module Goo
End |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndInterfaceInFile1Test()
            VerifyRecommendationsContain(<File>Interface IGoo
|</File>, {"End Interface"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndInterfaceInFile2Test()
            VerifyRecommendationsContain(<File>
Interface IGoo
End |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndClassInFile1Test()
            VerifyRecommendationsContain(<File>Class Goo
|</File>, {"End Class"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndClassInFile2Test()
            VerifyRecommendationsContain(<File>
Class Goo
End |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndStructureInFile1Test()
            VerifyRecommendationsContain(<File>Structure Goo
|</File>, {"End Structure"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndStructureInFile2Test()
            VerifyRecommendationsContain(<File>
Structure Goo
End |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndEnumInFile1Test()
            VerifyRecommendationsContain(<File>Enum Goo
|</File>, {"End Enum"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndEnumInFile2Test()
            VerifyRecommendationsContain(<File>
Enum Goo
End |</File>, "Enum")
        End Sub

        <WorkItem(539311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539311")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndBlockMissingInPreprocessorTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>
Module M
    Sub Goo()
        #If t|
    End Sub
End Module
</ClassDeclaration>, {"End Module", "End Sub"})
        End Sub

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubSuggestFunctionTest()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Goo()
End |</ClassDeclaration>, "Function", "Sub")
        End Sub

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <WorkItem(530599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530599")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <Fact>
        Public Sub EndFunctionDoesNotSuggestEndSubTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Function Goo()
|</ClassDeclaration>, "End Sub")
        End Sub

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndFunctionSuggestSubTest()
            VerifyRecommendationsContain(<ClassDeclaration>Function Goo()
End |</ClassDeclaration>, "Function", "Sub")
        End Sub

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubNotClassSuggestedTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Goo()
|</ClassDeclaration>, "End Class", "End Module", "End Structure", "End Interface")
        End Sub

        <WorkItem(969097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969097")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndClassPairingsTest()
            VerifyRecommendationsMissing(<File>Class Goo()
End |</File>, "Module", "Interface", "Structure")
        End Sub

        <WorkItem(969097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969097")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModulePairingsTest()
            VerifyRecommendationsMissing(<File>Module Goo()
End |</File>, "Class", "Interface", "Structure")
        End Sub

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModuleNotSubSuggestedTest()
            VerifyRecommendationsMissing(<File>Module Goo()
|</File>, "End Sub", "End Function")
        End Sub
    End Class
End Namespace


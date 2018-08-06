' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EndBlockKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubInBrokenMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Goo()
|</ClassDeclaration>, "End Sub")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterCompletedMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Sub Goo()
End Sub
|</ClassDeclaration>, "End")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterMustOverrideMethodDeclaration1Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
MustOverride Sub Goo()
|</ClassDeclaration>, "End")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterMustOverrideMethodDeclaration2Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
MustOverride Sub Goo()
|</ClassDeclaration>, "End Sub")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndPropertyAfterIncompleteProperty1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property goo As Integer
Get
End Get
Set(value As Integer)
End Set
|</ClassDeclaration>, "End Property")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndPropertyAfterIncompleteProperty2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property goo As Integer
Get
End Get
Set(value As Integer)
End Set
End |</ClassDeclaration>, "Property")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim goo = Sub()
|</MethodBody>, "End Sub")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndIfInMethodBody1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
|</MethodBody>, "End", "End If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndIfInMethodBody2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then
End |</MethodBody>, "If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndWithInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>With goo
|</MethodBody>, "End With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndWhileInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>While goo
|</MethodBody>, "End While")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSelectInMethodBody1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select goo
|</MethodBody>, "End Select")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSelectInMethodBody2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select goo
Case 1
|</MethodBody>, "End Select")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSelectInMethodBody3Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select goo
Case 1
Case Else
|</MethodBody>, "End Select")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSyncLockInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>SyncLock goo
|</MethodBody>, "End SyncLock")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModuleInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Module Goo
|</File>, {"End Module"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModuleInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Module Goo
End |</File>, "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndInterfaceInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface IGoo
|</File>, {"End Interface"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndInterfaceInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Interface IGoo
End |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndClassInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Goo
|</File>, {"End Class"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndClassInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Goo
End |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndStructureInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Structure Goo
|</File>, {"End Structure"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndStructureInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Structure Goo
End |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndEnumInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Enum Goo
|</File>, {"End Enum"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndEnumInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Enum Goo
End |</File>, "Enum")
        End Function

        <WorkItem(539311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539311")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndBlockMissingInPreprocessorTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>
Module M
    Sub Goo()
        #If t|
    End Sub
End Module
</ClassDeclaration>, {"End Module", "End Sub"})
        End Function

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubSuggestFunctionTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Goo()
End |</ClassDeclaration>, "Function", "Sub")
        End Function

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <WorkItem(530599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530599")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndFunctionDoesNotSuggestEndSubTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Function Goo()
|</ClassDeclaration>, "End Sub")
        End Function

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndFunctionSuggestSubTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Goo()
End |</ClassDeclaration>, "Function", "Sub")
        End Function

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubNotClassSuggestedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Goo()
|</ClassDeclaration>, "End Class", "End Module", "End Structure", "End Interface")
        End Function

        <WorkItem(969097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969097")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndClassPairingsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Class Goo()
End |</File>, "Module", "Interface", "Structure")
        End Function

        <WorkItem(969097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969097")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModulePairingsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Module Goo()
End |</File>, "Class", "Interface", "Structure")
        End Function

        <WorkItem(540069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540069")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModuleNotSubSuggestedTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Module Goo()
|</File>, "End Sub", "End Function")
        End Function
    End Class
End Namespace


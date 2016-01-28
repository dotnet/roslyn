' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EndBlockKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubInBrokenMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo()
|</ClassDeclaration>, "End Sub")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterCompletedMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Sub Foo()
End Sub
|</ClassDeclaration>, "End")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterMustOverrideMethodDeclaration1Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
MustOverride Sub Foo()
|</ClassDeclaration>, "End")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterMustOverrideMethodDeclaration2Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
MustOverride Sub Foo()
|</ClassDeclaration>, "End Sub")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndPropertyAfterIncompleteProperty1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property foo As Integer
Get
End Get
Set(value As Integer)
End Set
|</ClassDeclaration>, "End Property")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndPropertyAfterIncompleteProperty2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Property foo As Integer
Get
End Get
Set(value As Integer)
End Set
End |</ClassDeclaration>, "Property")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim foo = Sub()
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
            Await VerifyRecommendationsContainAsync(<MethodBody>With foo
|</MethodBody>, "End With")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndWhileInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>While foo
|</MethodBody>, "End While")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSelectInMethodBody1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select foo
|</MethodBody>, "End Select")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSelectInMethodBody2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select foo
Case 1
|</MethodBody>, "End Select")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSelectInMethodBody3Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select foo
Case 1
Case Else
|</MethodBody>, "End Select")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSyncLockInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>SyncLock foo
|</MethodBody>, "End SyncLock")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModuleInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Module Foo
|</File>, {"End Module"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModuleInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Module Foo
End |</File>, "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndInterfaceInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface IFoo
|</File>, {"End Interface"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndInterfaceInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Interface IFoo
End |</File>, "Interface")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndClassInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo
|</File>, {"End Class"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndClassInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
End |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndStructureInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Structure Foo
|</File>, {"End Structure"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndStructureInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Structure Foo
End |</File>, "Structure")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndEnumInFile1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>Enum Foo
|</File>, {"End Enum"})
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndEnumInFile2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
Enum Foo
End |</File>, "Enum")
        End Function

        <WorkItem(539311)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndBlockMissingInPreprocessorTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>
Module M
    Sub Foo()
        #If t|
    End Sub
End Module
</ClassDeclaration>, {"End Module", "End Sub"})
        End Function

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubSuggestFunctionTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Sub Foo()
End |</ClassDeclaration>, "Function", "Sub")
        End Function

        <WorkItem(540069)>
        <WorkItem(530599)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndFunctionDoesNotSuggestEndSubTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Function Foo()
|</ClassDeclaration>, "End Sub")
        End Function

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndFunctionSuggestSubTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Function Foo()
End |</ClassDeclaration>, "Function", "Sub")
        End Function

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndSubNotClassSuggestedTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Sub Foo()
|</ClassDeclaration>, "End Class", "End Module", "End Structure", "End Interface")
        End Function

        <WorkItem(969097)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndClassPairingsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Class Foo()
End |</File>, "Module", "Interface", "Structure")
        End Function

        <WorkItem(969097)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModulePairingsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Module Foo()
End |</File>, "Class", "Interface", "Structure")
        End Function

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndModuleNotSubSuggestedTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Module Foo()
|</File>, "End Sub", "End Function")
        End Function
    End Class
End Namespace


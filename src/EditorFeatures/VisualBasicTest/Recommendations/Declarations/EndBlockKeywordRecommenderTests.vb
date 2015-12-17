' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class EndBlockKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubInBrokenMethodBody()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo()
|</ClassDeclaration>, "End Sub")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterCompletedMethodBody()
            VerifyRecommendationsMissing(<ClassDeclaration>
Sub Foo()
End Sub
|</ClassDeclaration>, "End")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterMustOverrideMethodDeclaration1()
            VerifyRecommendationsMissing(<ClassDeclaration>
MustOverride Sub Foo()
|</ClassDeclaration>, "End")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterMustOverrideMethodDeclaration2()
            VerifyRecommendationsMissing(<ClassDeclaration>
MustOverride Sub Foo()
|</ClassDeclaration>, "End Sub")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndPropertyAfterIncompleteProperty1()
            VerifyRecommendationsContain(<ClassDeclaration>Property foo As Integer
Get
End Get
Set(value As Integer)
End Set
|</ClassDeclaration>, "End Property")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndPropertyAfterIncompleteProperty2()
            VerifyRecommendationsContain(<ClassDeclaration>Property foo As Integer
Get
End Get
Set(value As Integer)
End Set
End |</ClassDeclaration>, "Property")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubInLambda()
            VerifyRecommendationsContain(<MethodBody>Dim foo = Sub()
|</MethodBody>, "End Sub")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndIfInMethodBody1()
            VerifyRecommendationsContain(<MethodBody>If True Then
|</MethodBody>, "End", "End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndIfInMethodBody2()
            VerifyRecommendationsContain(<MethodBody>If True Then
End |</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndWithInMethodBody()
            VerifyRecommendationsContain(<MethodBody>With foo
|</MethodBody>, "End With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndWhileInMethodBody()
            VerifyRecommendationsContain(<MethodBody>While foo
|</MethodBody>, "End While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSelectInMethodBody1()
            VerifyRecommendationsContain(<MethodBody>Select foo
|</MethodBody>, "End Select")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSelectInMethodBody2()
            VerifyRecommendationsContain(<MethodBody>Select foo
Case 1
|</MethodBody>, "End Select")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSelectInMethodBody3()
            VerifyRecommendationsContain(<MethodBody>Select foo
Case 1
Case Else
|</MethodBody>, "End Select")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSyncLockInMethodBody()
            VerifyRecommendationsContain(<MethodBody>SyncLock foo
|</MethodBody>, "End SyncLock")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModuleInFile1()
            VerifyRecommendationsContain(<File>Module Foo
|</File>, {"End Module"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModuleInFile2()
            VerifyRecommendationsContain(<File>
Module Foo
End |</File>, "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndInterfaceInFile1()
            VerifyRecommendationsContain(<File>Interface IFoo
|</File>, {"End Interface"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndInterfaceInFile2()
            VerifyRecommendationsContain(<File>
Interface IFoo
End |</File>, "Interface")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndClassInFile1()
            VerifyRecommendationsContain(<File>Class Foo
|</File>, {"End Class"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndClassInFile2()
            VerifyRecommendationsContain(<File>
Class Foo
End |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndStructureInFile1()
            VerifyRecommendationsContain(<File>Structure Foo
|</File>, {"End Structure"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndStructureInFile2()
            VerifyRecommendationsContain(<File>
Structure Foo
End |</File>, "Structure")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndEnumInFile1()
            VerifyRecommendationsContain(<File>Enum Foo
|</File>, {"End Enum"})
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndEnumInFile2()
            VerifyRecommendationsContain(<File>
Enum Foo
End |</File>, "Enum")
        End Sub

        <WorkItem(539311)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndBlockMissingInPreprocessor()
            VerifyRecommendationsMissing(
<ClassDeclaration>
Module M
    Sub Foo()
        #If t|
    End Sub
End Module
</ClassDeclaration>, {"End Module", "End Sub"})
        End Sub

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubSuggestFunction()
            VerifyRecommendationsContain(<ClassDeclaration>Sub Foo()
End |</ClassDeclaration>, "Function", "Sub")
        End Sub

        <WorkItem(540069)>
        <WorkItem(530599)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndFunctionDoesNotSuggestEndSub()
            VerifyRecommendationsMissing(<ClassDeclaration>Function Foo()
|</ClassDeclaration>, "End Sub")
        End Sub

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndFunctionSuggestSub()
            VerifyRecommendationsContain(<ClassDeclaration>Function Foo()
End |</ClassDeclaration>, "Function", "Sub")
        End Sub

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndSubNotClassSuggested()
            VerifyRecommendationsMissing(<ClassDeclaration>Sub Foo()
|</ClassDeclaration>, "End Class", "End Module", "End Structure", "End Interface")
        End Sub

        <WorkItem(969097)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndClassPairings()
            VerifyRecommendationsMissing(<File>Class Foo()
End |</File>, "Module", "Interface", "Structure")
        End Sub

        <WorkItem(969097)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModulePairings()
            VerifyRecommendationsMissing(<File>Module Foo()
End |</File>, "Class", "Interface", "Structure")
        End Sub

        <WorkItem(540069)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EndModuleNotSubSuggested()
            VerifyRecommendationsMissing(<File>Module Foo()
|</File>, "End Sub", "End Function")
        End Sub

    End Class
End Namespace


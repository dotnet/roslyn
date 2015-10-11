' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideNamespaceDeclaration

        ''' <summary>
        ''' Declarations outside of any namespace in the file are considered to be in the project's root namespace
        ''' </summary>
        Private Sub VerifyContains(ParamArray recommendations As String())
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, recommendations)
            VerifyRecommendationsContain(<File>|</File>, recommendations)
            VerifyRecommendationsContain(
<File>Imports System
|</File>, recommendations)
        End Sub

        ''' <summary>
        ''' Declarations outside of any namespace in the file are considered to be in the project's root namespace
        ''' </summary>
        Private Sub VerifyMissing(ParamArray recommendations As String())
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, recommendations)
            VerifyRecommendationsMissing(<File>|</File>, recommendations)
            VerifyRecommendationsMissing(
<File>Imports System
|</File>, recommendations)
        End Sub

        <WorkItem(530100)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AccessibilityModifiers()
            VerifyContains("Public", "Friend")
            VerifyMissing("Protected", "Private", "Protected Friend")
        End Sub

        <WorkItem(530100)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassModifiers()
            VerifyContains("MustInherit", "NotInheritable", "Partial")
        End Sub
    End Class
End Namespace

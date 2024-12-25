' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class InsideNamespaceDeclaration
        Inherits RecommenderTests

        ''' <summary>
        ''' Declarations outside of any namespace in the file are considered to be in the project's root namespace
        ''' </summary>
        Private Shared Sub VerifyContains(ParamArray recommendations As String())
            VerifyRecommendationsContain(<NamespaceDeclaration>|</NamespaceDeclaration>, recommendations)
            VerifyRecommendationsContain(<File>|</File>, recommendations)
            VerifyRecommendationsContain(
<File>Imports System
|</File>, recommendations)
        End Sub

        ''' <summary>
        ''' Declarations outside of any namespace in the file are considered to be in the project's root namespace
        ''' </summary>
        Private Shared Sub VerifyMissing(ParamArray recommendations As String())
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, recommendations)
            VerifyRecommendationsMissing(<File>|</File>, recommendations)
            VerifyRecommendationsMissing(
<File>Imports System
|</File>, recommendations)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530100")>
        Public Sub AccessibilityModifiersTest()
            VerifyContains("Public", "Friend")
            VerifyMissing("Protected", "Private", "Protected Friend")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530100")>
        Public Sub ClassModifiersTest()
            VerifyContains("MustInherit", "NotInheritable", "Partial")
        End Sub
    End Class
End Namespace

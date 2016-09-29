' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations.ModifierKeywordRecommenderTests
    Public Class InsideNamespaceDeclaration

        ''' <summary>
        ''' Declarations outside of any namespace in the file are considered to be in the project's root namespace
        ''' </summary>
        Private Async Function VerifyContainsAsync(ParamArray recommendations As String()) As Task
            Await VerifyRecommendationsContainAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, recommendations)
            Await VerifyRecommendationsContainAsync(<File>|</File>, recommendations)
            Await VerifyRecommendationsContainAsync(
<File>Imports System
|</File>, recommendations)
        End Function

        ''' <summary>
        ''' Declarations outside of any namespace in the file are considered to be in the project's root namespace
        ''' </summary>
        Private Async Function VerifyMissingAsync(ParamArray recommendations As String()) As Task
            Await VerifyRecommendationsMissingAsync(<NamespaceDeclaration>|</NamespaceDeclaration>, recommendations)
            Await VerifyRecommendationsMissingAsync(<File>|</File>, recommendations)
            Await VerifyRecommendationsMissingAsync(
<File>Imports System
|</File>, recommendations)
        End Function

        <WorkItem(530100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530100")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AccessibilityModifiersTest() As Task
            Await VerifyContainsAsync("Public", "Friend")
            Await VerifyMissingAsync("Protected", "Private", "Protected Friend")
        End Function

        <WorkItem(530100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530100")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassModifiersTest() As Task
            Await VerifyContainsAsync("MustInherit", "NotInheritable", "Partial")
        End Function
    End Class
End Namespace

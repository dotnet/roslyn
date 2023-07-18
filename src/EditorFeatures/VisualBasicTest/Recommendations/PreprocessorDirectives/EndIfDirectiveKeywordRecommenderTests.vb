' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class EndIfDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub HashEndIfNotInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#End If")
        End Sub

        <Fact>
        Public Sub HashEndIfInFileAfterIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
|</File>, "#End If")
        End Sub

        <Fact>
        Public Sub HashEndIfInFileAfterElseIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
|</File>, "#End If")
        End Sub

        <Fact>
        Public Sub HashEndIfNotInFileAfterElse1Test()
            VerifyRecommendationsContain(<File>
#If True Then
#Else
|</File>, "#End If")
        End Sub

        <Fact>
        Public Sub HashEndIfNotInFileAfterElse2Test()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#End If")
        End Sub

        <Fact>
        Public Sub IfAfterHashEndIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
#End |</File>, "If")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957458")>
        Public Sub NotIfWithEndPartiallyTypedTest()
            VerifyRecommendationsMissing(<File>
#If True Then
#En |</File>, "If")
        End Sub
    End Class
End Namespace

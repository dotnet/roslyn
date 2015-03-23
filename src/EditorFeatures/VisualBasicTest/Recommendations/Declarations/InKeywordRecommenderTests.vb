' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInForEach1()
            VerifyRecommendationsContain(<MethodBody>For Each x |</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInForEach2()
            VerifyRecommendationsContain(<MethodBody>For Each x As Foo |</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInFromQuery1()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x |</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInFromQuery2()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x As Foo |</MethodBody>, "In")
        End Sub

        <WorkItem(543231)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInFromQuery3()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = From x As Integer |</MethodBody>, "In")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>For Each x 
|</MethodBody>, "In")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>For Each x _
|</MethodBody>, "In")
        End Sub
    End Class
End Namespace

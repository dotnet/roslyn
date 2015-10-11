' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class UntilAndWhileKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAfterDo()
            VerifyRecommendationsContain(<MethodBody>Do |</MethodBody>, "Until")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterDo()
            VerifyRecommendationsContain(<MethodBody>Do |</MethodBody>, "While")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAfterLoop()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop |</MethodBody>, "Until")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterLoop()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop |</MethodBody>, "While")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAndWhileMissingInDoLoopTopTestBlock()
            VerifyRecommendationsMissing(<MethodBody>
Do Until True
Loop |</MethodBody>, "While", "Until")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAndWhileMissingAfterInvalidLoop()
            VerifyRecommendationsMissing(<MethodBody>
Loop |</MethodBody>, "While", "Until")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>Do 
|</MethodBody>, "Until")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>Do _
|</MethodBody>, "Until")
        End Sub
    End Class
End Namespace

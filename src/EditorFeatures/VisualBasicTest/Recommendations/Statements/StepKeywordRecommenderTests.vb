' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class StepKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StepInForLoop()
            VerifyRecommendationsContain(<MethodBody>For i = 1 To 10 |</MethodBody>, "Step")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StepInForLoopAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
    For i = 1 To 10 _
_
|</MethodBody>, "Step")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StepInForLoopNotAfterEOL()
            VerifyRecommendationsMissing(
<MethodBody>
    For i = 1 To 10 
|</MethodBody>, "Step")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub StepInForLoopNotAfterEOLWithLineContinuation()
            VerifyRecommendationsMissing(
<MethodBody>
    For i = 1 To 10 _

|</MethodBody>, "Step")
        End Sub
    End Class
End Namespace

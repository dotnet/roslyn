' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class IfKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfInMultiLineLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfInSingleLineLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub() |</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterElseInMultiLine1()
            VerifyRecommendationsContain(<MethodBody>
If True Then
Else |
End If</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterElseInMultiLine2()
            VerifyRecommendationsContain(<MethodBody>
If True Then
Else If
Else |
End If</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterElseInSingleLineIf()
            VerifyRecommendationsContain(<MethodBody>If True Then Stop Else |</MethodBody>, "If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterExternalSourceDirective()
            VerifyRecommendationsContain(
<MethodBody>
#ExternalSource ("file", 1)
|
#End ExternalSource
</MethodBody>, "If")
        End Sub
    End Class
End Namespace

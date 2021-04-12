﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class GoToKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GoToAfterOnErrorTest()
            VerifyRecommendationsContain(<MethodBody>On Error |</MethodBody>, "GoTo")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GoToNotAfterOnErrorInLambdaTest()
            VerifyRecommendationsAreExactly(<MethodBody>
Dim x = Sub()
            On Error |
        End Sub</MethodBody>, Array.Empty(Of String)())
        End Sub
    End Class
End Namespace

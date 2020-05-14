﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class InferOptionsRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionsAfterOptionInferTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<File>Option Infer |</File>, "On", "Off")
        End Function
    End Class
End Namespace

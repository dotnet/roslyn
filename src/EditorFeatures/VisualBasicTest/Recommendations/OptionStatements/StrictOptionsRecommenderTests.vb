' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class StrictOptionsRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionsAfterOptionStrictTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<File>Option Strict |</File>, "On", "Off")
        End Function
    End Class
End Namespace

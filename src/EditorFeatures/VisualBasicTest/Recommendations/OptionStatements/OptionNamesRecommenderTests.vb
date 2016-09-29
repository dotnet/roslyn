' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class OptionNamesRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionNamesAfterOptionTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<File>Option |</File>, "Compare", "Explicit", "Infer", "Strict")
        End Function
    End Class
End Namespace

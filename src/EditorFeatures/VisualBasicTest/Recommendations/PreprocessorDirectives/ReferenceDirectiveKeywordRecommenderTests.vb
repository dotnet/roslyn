' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ReferenceDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(37911, "https://github.com/dotnet/roslyn/issues/37911")>
        Public Async Function NotInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#R")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <WorkItem(37911, "https://github.com/dotnet/roslyn/issues/37911")>
        Public Async Function AppearsInScriptingContext() As Task
            Await VerifyRecommendationsContainAsync(<File Script="True">|</File>, "#R")
        End Function
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

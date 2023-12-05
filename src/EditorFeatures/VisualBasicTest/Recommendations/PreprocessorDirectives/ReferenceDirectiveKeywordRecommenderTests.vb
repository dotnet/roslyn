' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ReferenceDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37911")>
        Public Sub NotInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#R")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37911")>
        Public Sub AppearsInScriptingContext()
            VerifyRecommendationsContain(<File Script="True">|</File>, "#R")
        End Sub
    End Class
End Namespace

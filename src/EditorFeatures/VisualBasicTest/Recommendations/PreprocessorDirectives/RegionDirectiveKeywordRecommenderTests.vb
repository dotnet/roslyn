' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class RegionDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashRegionInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "#Region")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashRegionInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Function()
|
End Function</ClassDeclaration>, "#Region")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInEnumBlockMemberDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                             Enum goo
                                                |
                                            End enum
                                         </File>, "#Region")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashEndTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Region "goo"

#End |</File>, "#Region")
        End Function

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashRegionTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Region")
        End Function
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class IfDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashIfInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "#If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashIfInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "#If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInEnumBlockMemberDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                             Enum foo
                                                |
                                            End enum
                                         </File>, "#If")
        End Function

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashRegionTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#If")
        End Function
    End Class
End Namespace

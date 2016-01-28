' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ConstDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashConstInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "#Const")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashConstInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "#Const")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInEnumBlockMemberDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                             Enum foo
                                                 |
                                             End enum
                                         </File>, "#Const")
        End Function

        <WorkItem(544629)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashConstAfterSingleNonMatchingCharacterTest() As Task
            Await VerifyRecommendationsContainAsync(<File>a|</File>, "#Const")
        End Function

        <WorkItem(544629)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashConstAfterPartialConstWithoutHashTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Con|</File>, "#Const")
        End Function

        <WorkItem(722, "https://github.com/dotnet/roslyn/issues/722")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashConstTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>#Const |</File>, "#Const")
        End Function

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashRegionTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Const")
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ConstDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "#Const")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "#Const")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnumBlockMemberDeclarationTest()
            VerifyRecommendationsMissing(<File>
                                             Enum goo
                                                 |
                                             End enum
                                         </File>, "#Const")
        End Sub

        <WorkItem(544629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544629")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstAfterSingleNonMatchingCharacterTest()
            VerifyRecommendationsContain(<File>a|</File>, "#Const")
        End Sub

        <WorkItem(544629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544629")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstAfterPartialConstWithoutHashTest()
            VerifyRecommendationsContain(<File>Con|</File>, "#Const")
        End Sub

        <WorkItem(722, "https://github.com/dotnet/roslyn/issues/722")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashConstTest()
            VerifyRecommendationsMissing(<File>#Const |</File>, "#Const")
        End Sub

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashRegionTest()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Const")
        End Sub
    End Class
End Namespace

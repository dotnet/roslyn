' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ConstDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub HashConstInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "#Const")
        End Sub

        <Fact>
        Public Sub HashConstInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "#Const")
        End Sub

        <Fact>
        Public Sub NotInEnumBlockMemberDeclarationTest()
            VerifyRecommendationsMissing(<File>
                                             Enum goo
                                                 |
                                             End enum
                                         </File>, "#Const")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544629")>
        Public Sub HashConstAfterSingleNonMatchingCharacterTest()
            VerifyRecommendationsContain(<File>a|</File>, "#Const")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544629")>
        Public Sub HashConstAfterPartialConstWithoutHashTest()
            VerifyRecommendationsContain(<File>Con|</File>, "#Const")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/722")>
        Public Sub NotAfterHashConstTest()
            VerifyRecommendationsMissing(<File>#Const |</File>, "#Const")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6389")>
        Public Sub NotAfterHashRegionTest()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Const")
        End Sub
    End Class
End Namespace

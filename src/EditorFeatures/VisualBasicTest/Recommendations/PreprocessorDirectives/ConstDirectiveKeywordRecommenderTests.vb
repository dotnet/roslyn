' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ConstDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstInFile()
            VerifyRecommendationsContain(<File>|</File>, "#Const")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "#Const")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnumBlockMemberDeclaration()
            VerifyRecommendationsMissing(<File>
                                             Enum foo
                                                 |
                                             End enum
                                         </File>, "#Const")
        End Sub

        <WorkItem(544629)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstAfterSingleNonMatchingCharacter()
            VerifyRecommendationsContain(<File>a|</File>, "#Const")
        End Sub

        <WorkItem(544629)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstAfterPartialConstWithoutHash()
            VerifyRecommendationsContain(<File>Con|</File>, "#Const")
        End Sub

        <WorkItem(722, "https://github.com/dotnet/roslyn/issues/722")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashConst()
            VerifyRecommendationsMissing(<File>#Const |</File>, "#Const")
        End Sub

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashRegion()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Const")
        End Sub
    End Class
End Namespace

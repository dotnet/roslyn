' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class IfDirectiveKeywordRecommenderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashIfInFile()
            VerifyRecommendationsContain(<File>|</File>, "#If")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashIfInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "#If")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnumBlockMemberDeclaration()
            VerifyRecommendationsMissing(<File>
                                             Enum foo
                                                |
                                            End enum
                                         </File>, "#If")
        End Sub

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashRegion()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#If")
        End Sub
    End Class
End Namespace

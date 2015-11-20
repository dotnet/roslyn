' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class RegionDirectiveKeywordRecommenderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashRegionInFile()
            VerifyRecommendationsContain(<File>|</File>, "#Region")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashRegionInLambda()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function()
|
End Function</ClassDeclaration>, "#Region")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnumBlockMemberDeclaration()
            VerifyRecommendationsMissing(<File>
                                             Enum foo
                                                |
                                            End enum
                                         </File>, "#Region")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashEnd()
            VerifyRecommendationsMissing(<File>
#Region "foo"

#End |</File>, "#Region")
        End Sub

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashRegion()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Region")
        End Sub
    End Class
End Namespace

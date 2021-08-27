' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class WarningDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEnableWarningInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "#Enable Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashDisableWarningInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "#Disable Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Enable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Disable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWarningInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashWarningInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEnableWarningInCodeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function()
|
End Function</ClassDeclaration>, "#Enable Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WarningAfterEnableTest()
            VerifyRecommendationsContain(<File>
#Enable |
                                         </File>, "Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WarningAfterDisableTest()
            VerifyRecommendationsContain(<File>
#Disable |
                                         </File>, "Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoEnableAfterEnableTest()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "Enable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoDisableAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Enable Warning |
                                         </File>, "Disable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableAfterEnableTest()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "#Disable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Enable Warning |
                                         </File>, "#Enable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableWarningAfterEnableTest()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "#Disable Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableWarningAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Disable Warning |
                                         </File>, "#Enable Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWarningAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Disable Warning |
                                         </File>, "Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoDisableAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "Disable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "#Enable")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableWarningAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "#Disable Warning")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWarningAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "Warning")
        End Sub

        <WorkItem(1020079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020079")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnumTest()
            VerifyRecommendationsMissing(<File>
Enum E
    A
    |
End Enum
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Sub

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashRegionTest()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Sub
    End Class
End Namespace

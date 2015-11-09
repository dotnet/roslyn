' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class WarningDirectiveKeywordRecommenderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEnableWarningInFile()
            VerifyRecommendationsContain(<File>|</File>, "#Enable Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashDisableWarningInFile()
            VerifyRecommendationsContain(<File>|</File>, "#Disable Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#Enable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#Disable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWarningInFile()
            VerifyRecommendationsMissing(<File>|</File>, "Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashWarningInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEnableWarningInCode()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function()
|
End Function</ClassDeclaration>, "#Enable Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WarningAfterEnable()
            VerifyRecommendationsContain(<File>
#Enable |
                                         </File>, "Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WarningAfterDisable()
            VerifyRecommendationsContain(<File>
#Disable |
                                         </File>, "Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoEnableAfterEnable()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "Enable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoDisableAfterWarning()
            VerifyRecommendationsMissing(<File>
#Enable Warning |
                                         </File>, "Disable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableAfterEnable()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "#Disable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableAfterWarning()
            VerifyRecommendationsMissing(<File>
#Enable Warning |
                                         </File>, "#Enable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableWarningAfterEnable()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "#Disable Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableWarningAfterWarning()
            VerifyRecommendationsMissing(<File>
#Disable Warning |
                                         </File>, "#Enable Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWarningAfterWarning()
            VerifyRecommendationsMissing(<File>
#Disable Warning |
                                         </File>, "Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoDisableAfterIf()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "Disable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashEnableAfterIf()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "#Enable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHashDisableWarningAfterIf()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "#Disable Warning")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWarningAfterIf()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "Warning")
        End Sub

        <WorkItem(1020079)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnum()
            VerifyRecommendationsMissing(<File>
Enum E
    A
    |
End Enum
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Sub

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashRegion()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Sub

    End Class
End Namespace

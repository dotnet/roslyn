' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class WarningDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub HashEnableWarningInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "#Enable Warning")
        End Sub

        <Fact>
        Public Sub HashDisableWarningInFileTest()
            VerifyRecommendationsContain(<File>|</File>, "#Disable Warning")
        End Sub

        <Fact>
        Public Sub NoHashEnableInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Enable")
        End Sub

        <Fact>
        Public Sub NoHashDisableInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Disable")
        End Sub

        <Fact>
        Public Sub NoWarningInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "Warning")
        End Sub

        <Fact>
        Public Sub NoHashWarningInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Warning")
        End Sub

        <Fact>
        Public Sub HashEnableWarningInCodeTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = Function()
|
End Function</ClassDeclaration>, "#Enable Warning")
        End Sub

        <Fact>
        Public Sub WarningAfterEnableTest()
            VerifyRecommendationsContain(<File>
#Enable |
                                         </File>, "Warning")
        End Sub

        <Fact>
        Public Sub WarningAfterDisableTest()
            VerifyRecommendationsContain(<File>
#Disable |
                                         </File>, "Warning")
        End Sub

        <Fact>
        Public Sub NoEnableAfterEnableTest()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "Enable")
        End Sub

        <Fact>
        Public Sub NoDisableAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Enable Warning |
                                         </File>, "Disable")
        End Sub

        <Fact>
        Public Sub NoHashDisableAfterEnableTest()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "#Disable")
        End Sub

        <Fact>
        Public Sub NoHashEnableAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Enable Warning |
                                         </File>, "#Enable")
        End Sub

        <Fact>
        Public Sub NoHashDisableWarningAfterEnableTest()
            VerifyRecommendationsMissing(<File>
#Enable |
                                         </File>, "#Disable Warning")
        End Sub

        <Fact>
        Public Sub NoHashEnableWarningAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Disable Warning |
                                         </File>, "#Enable Warning")
        End Sub

        <Fact>
        Public Sub NoWarningAfterWarningTest()
            VerifyRecommendationsMissing(<File>
#Disable Warning |
                                         </File>, "Warning")
        End Sub

        <Fact>
        Public Sub NoDisableAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "Disable")
        End Sub

        <Fact>
        Public Sub NoHashEnableAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "#Enable")
        End Sub

        <Fact>
        Public Sub NoHashDisableWarningAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "#Disable Warning")
        End Sub

        <Fact>
        Public Sub NoWarningAfterIfTest()
            VerifyRecommendationsMissing(<File>
#If |
                                         </File>, "Warning")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020079")>
        Public Sub NotInEnumTest()
            VerifyRecommendationsMissing(<File>
Enum E
    A
    |
End Enum
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6389")>
        Public Sub NotAfterHashRegionTest()
            VerifyRecommendationsMissing(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Sub
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class WarningDirectiveKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEnableWarningInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "#Enable Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashDisableWarningInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "#Disable Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashEnableInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#Enable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashDisableInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#Disable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWarningInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashWarningInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEnableWarningInCodeTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim x = Function()
|
End Function</ClassDeclaration>, "#Enable Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WarningAfterEnableTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#Enable |
                                         </File>, "Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WarningAfterDisableTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#Disable |
                                         </File>, "Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoEnableAfterEnableTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Enable |
                                         </File>, "Enable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoDisableAfterWarningTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Enable Warning |
                                         </File>, "Disable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashDisableAfterEnableTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Enable |
                                         </File>, "#Disable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashEnableAfterWarningTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Enable Warning |
                                         </File>, "#Enable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashDisableWarningAfterEnableTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Enable |
                                         </File>, "#Disable Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashEnableWarningAfterWarningTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Disable Warning |
                                         </File>, "#Enable Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWarningAfterWarningTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Disable Warning |
                                         </File>, "Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoDisableAfterIfTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If |
                                         </File>, "Disable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashEnableAfterIfTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If |
                                         </File>, "#Enable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHashDisableWarningAfterIfTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If |
                                         </File>, "#Disable Warning")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWarningAfterIfTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If |
                                         </File>, "Warning")
        End Function

        <WorkItem(1020079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020079")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInEnumTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Enum E
    A
    |
End Enum
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Function

        <WorkItem(6389, "https://github.com/dotnet/roslyn/issues/6389")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashRegionTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
                                         Class C

                                             #Region |

                                         End Class
                                         </File>, "#Enable Warning", "#Disable Warning")
        End Function
    End Class
End Namespace

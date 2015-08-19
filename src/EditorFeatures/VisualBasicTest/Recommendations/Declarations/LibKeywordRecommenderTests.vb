' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class LibKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LibAfterNameInSub()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub foo |</ClassDeclaration>, "Lib")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LibAfterNameInFunction()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Function foo |</ClassDeclaration>, "Lib")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LibNotAfterLibKeyword()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub foo Lib |</ClassDeclaration>, "Lib")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare Sub foo 
|</ClassDeclaration>, "Lib")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare Sub foo _
|</ClassDeclaration>, "Lib")
        End Sub
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ReferenceDirectiveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestReferenceDirectiveRecommendedInScriptFile()
            VerifyRecommendationsContain(TestOptions.Script, <File>|</File>, "#R")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestReferenceDirectiveNotRecommendedInRegularFile()
            VerifyRecommendationsMissing(TestOptions.Regular, <File>|</File>, "#R")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestReferenceDirectiveNotRecommendedInMethod()
            VerifyRecommendationsMissing(TestOptions.Script, <MethodBody>|</MethodBody>, "#R")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestReferenceDirectiveNotRecommendedAfterDeclaration()
            VerifyRecommendationsMissing(TestOptions.Script,
<File>
Dim x = 1
|
</File>, "#R")
        End Sub
    End Class
End Namespace

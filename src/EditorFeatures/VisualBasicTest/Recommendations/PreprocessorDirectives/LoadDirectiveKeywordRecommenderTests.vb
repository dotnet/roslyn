' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class LoadDirectiveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestLoadDirectiveRecommendedInScriptFile()
            VerifyRecommendationsContain(TestOptions.Script, <File>|</File>, "#Load")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestLoadDirectiveNotRecommendedInRegularFile()
            VerifyRecommendationsMissing(TestOptions.Regular, <File>|</File>, "#Load")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestLoadDirectiveNotRecommendedInMethod()
            VerifyRecommendationsMissing(TestOptions.Script, <MethodBody>|</MethodBody>, "#Load")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestLoadDirectiveNotRecommendedAfterDeclaration()
            VerifyRecommendationsMissing(TestOptions.Script,
<File>
Dim x = 1
|
</File>, "#Load")
        End Sub
    End Class
End Namespace

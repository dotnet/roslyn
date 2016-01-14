' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ShebangDirectiveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestShebangDirectiveRecommendedInScriptFile()
            VerifyRecommendationsContain(TestOptions.Script, <File>|</File>, "#!")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestShebangDirectiveNotRecommendedIfNotAtTheStartOFTheFile()
            VerifyRecommendationsMissing(TestOptions.Script, <File> |</File>, "#!")
            VerifyRecommendationsMissing(TestOptions.Script,
<File>
            |</File>, "#!")
            VerifyRecommendationsMissing(TestOptions.Script,
<File>' Comment.
            |</File>, "#!")
            VerifyRecommendationsMissing(TestOptions.Script, <File># |</File>, "#!")
            VerifyRecommendationsMissing(TestOptions.Script, <File> |</File>, "#!")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestShebangDirectiveNotRecommendedInRegularFile()
            VerifyRecommendationsMissing(TestOptions.Regular, <File>|</File>, "#!")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestShebangDirectiveNotRecommendedInMethod()
            VerifyRecommendationsMissing(TestOptions.Script, <MethodBody>|</MethodBody>, "#!")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TestShebangDirectiveNotRecommendedAfterDeclaration()
            VerifyRecommendationsMissing(TestOptions.Script,
<File>
Dim x = 1
|
</File>, "#!")
        End Sub
    End Class
End Namespace

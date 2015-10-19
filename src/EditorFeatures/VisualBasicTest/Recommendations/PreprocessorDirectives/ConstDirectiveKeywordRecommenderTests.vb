' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ConstDirectiveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstInFile()
            VerifyRecommendationsContain(<File>|</File>, "#Const")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "#Const")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInEnumBlockMemberDeclaration()
            VerifyRecommendationsMissing(<File>
                                             Enum foo
                                                 |
                                             End enum
                                         </File>, "#Const")
        End Sub

        <WpfFact>
        <WorkItem(544629)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstAfterSingleNonMatchingCharacter()
            VerifyRecommendationsContain(<File>a|</File>, "#Const")
        End Sub

        <WpfFact>
        <WorkItem(544629)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashConstAfterPartialConstWithoutHash()
            VerifyRecommendationsContain(<File>Con|</File>, "#Const")
        End Sub

        <WpfFact>
        <WorkItem(722, "https://github.com/dotnet/roslyn/issues/722")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashConst()
            VerifyRecommendationsMissing(<File>#Const |</File>, "#Const")
        End Sub
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AliasKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AliasAfterLibNameInSub()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub foo Lib "Foo" |</ClassDeclaration>, "Alias")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AliasAfterLibNameInFunction()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Function foo Lib "Foo" |</ClassDeclaration>, "Alias")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AliasNotAfterLibKeyword()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub foo Lib |</ClassDeclaration>, Array.Empty(Of String)())
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NothingAfterBrokenAlias()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub foo Lib "Foo" Alais |</ClassDeclaration>, Array.Empty(Of String)())
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoAliasAfterEol()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare Function foo Lib "Foo" 
    |</ClassDeclaration>, "Alias")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AliasAfterExplicitLineContinuation()
            VerifyRecommendationsAreExactly(
<ClassDeclaration>Declare Function foo Lib "Foo" _
|</ClassDeclaration>, "Alias")
        End Sub
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class SyncLockKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SyncLockInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "SyncLock")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SyncLockInMultiLineLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "SyncLock")

        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SyncLockInSingleLineLambda()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "SyncLock")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SyncLockInSingleLineFunctionLambda()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Function() |
                                         </ClassDeclaration>, "SyncLock")
        End Sub
    End Class
End Namespace

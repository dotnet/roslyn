' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class SyncLockKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub SyncLockInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "SyncLock")
        End Sub

        <Fact>
        Public Sub SyncLockInMultiLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "SyncLock")

        End Sub

        <Fact>
        Public Sub SyncLockInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "SyncLock")
        End Sub

        <Fact>
        Public Sub SyncLockInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Function() |
                                         </ClassDeclaration>, "SyncLock")
        End Sub
    End Class
End Namespace

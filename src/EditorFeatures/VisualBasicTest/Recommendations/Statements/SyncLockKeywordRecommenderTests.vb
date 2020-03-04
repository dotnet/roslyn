' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class SyncLockKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SyncLockInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "SyncLock")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SyncLockInMultiLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "SyncLock")

        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SyncLockInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "SyncLock")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SyncLockInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Private _member = Function() |
                                         </ClassDeclaration>, "SyncLock")
        End Function
    End Class
End Namespace

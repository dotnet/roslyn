' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

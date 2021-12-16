' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class AwaitKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InSynchronousMethodTest()
            VerifyRecommendationsContain(<File>
Class C
     Sub Goo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InMethodStatementTest()
            VerifyRecommendationsContain(<File>
Class C
    Async Sub Goo()
        |
    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InMethodExpressionTest()
            VerifyRecommendationsContain(<File>
Class C
    Async Sub Goo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInCatchTest()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInCatchExceptionFilterTest()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Goo()
        Try
        Catch When Err = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InCatchNestedDelegateTest()
            VerifyRecommendationsContain(<File>
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = Function() |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInFinallyTest()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Goo()
        Try
        Finally
            Dim z = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInSyncLockTest()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Goo()
        SyncLock True
            Dim z = |
        End SyncLock
    End Sub
End Class
                                         </File>, "Await")
        End Sub
    End Class
End Namespace


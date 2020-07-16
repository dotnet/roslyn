' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class AwaitKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InSynchronousMethodTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
     Sub Goo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InMethodStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
    Async Sub Goo()
        |
    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InMethodExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
    Async Sub Goo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInCatchTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInCatchExceptionFilterTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class C
    Async Sub Goo()
        Try
        Catch When Err = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InCatchNestedDelegateTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = Function() |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInFinallyTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class C
    Async Sub Goo()
        Try
        Finally
            Dim z = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInSyncLockTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class C
    Async Sub Goo()
        SyncLock True
            Dim z = |
        End SyncLock
    End Sub
End Class
                                         </File>, "Await")
        End Function
    End Class
End Namespace


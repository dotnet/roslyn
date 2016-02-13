' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class AwaitKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InSynchronousMethodTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
     Sub Foo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InMethodStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
    Async Sub Foo()
        |
    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InMethodExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class C
    Async Sub Foo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInCatchTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class C
    Async Sub Foo()
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
    Async Sub Foo()
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
    Async Sub Foo()
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
    Async Sub Foo()
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
    Async Sub Foo()
        SyncLock True
            Dim z = |
        End SyncLock
    End Sub
End Class
                                         </File>, "Await")
        End Function
    End Class
End Namespace


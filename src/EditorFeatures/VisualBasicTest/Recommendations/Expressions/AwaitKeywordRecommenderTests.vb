' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class AwaitKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InSynchronousMethod()
            VerifyRecommendationsContain(<File>
Class C
     Sub Foo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InMethodStatement()
            VerifyRecommendationsContain(<File>
Class C
    Async Sub Foo()
        |
    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InMethodExpression()
            VerifyRecommendationsContain(<File>
Class C
    Async Sub Foo()
        Dim z = |
    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInCatch()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Foo()
        Try
        Catch
            Dim z = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInCatchExceptionFilter()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Foo()
        Try
        Catch When Err = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InCatchNestedDelegate()
            VerifyRecommendationsContain(<File>
Class C
    Async Sub Foo()
        Try
        Catch
            Dim z = Function() |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInFinally()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Foo()
        Try
        Finally
            Dim z = |
        End Try

    End Sub
End Class
                                         </File>, "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInSyncLock()
            VerifyRecommendationsMissing(<File>
Class C
    Async Sub Foo()
        SyncLock True
            Dim z = |
        End SyncLock
    End Sub
End Class
                                         </File>, "Await")
        End Sub
    End Class
End Namespace


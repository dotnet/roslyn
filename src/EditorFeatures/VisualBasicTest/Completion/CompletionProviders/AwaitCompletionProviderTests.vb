' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class AwaitCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(AwaitCompletionProvider)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InSynchronousMethodTest()
            VerifyItemExistsAsync("
Class C
     Sub Goo()
        Dim z = $$
    End Sub
End Class
", "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InMethodStatementTest()
            VerifyItemExistsAsync("
Class C
    Async Sub Goo()
        $$
    End Sub
End Class
", "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InMethodExpressionTest()
            VerifyItemExistsAsync("
Class C
    Async Sub Goo()
        Dim z = $$
    End Sub
End Class
", "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInCatchTest()
            VerifyItemExistsAsync("
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = $$
        End Try

    End Sub
End Class
", "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInCatchExceptionFilterTest()
            VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Catch When Err = $$
        End Try

    End Sub
End Class
")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InCatchNestedDelegateTest()
            VerifyItemExistsAsync("
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = Function() $$
        End Try

    End Sub
End Class
", "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInFinallyTest()
            VerifyItemExistsAsync("
Class C
    Async Sub Goo()
        Try
        Finally
            Dim z = $$
        End Try

    End Sub
End Class
", "Await")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInSyncLockTest()
            VerifyItemExistsAsync("
Class C
    Async Sub Goo()
        SyncLock True
            Dim z = $$
        End SyncLock
    End Sub
End Class
", "Await")
        End Sub
    End Class
End Namespace

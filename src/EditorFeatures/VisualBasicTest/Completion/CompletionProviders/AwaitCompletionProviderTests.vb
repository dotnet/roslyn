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

        Protected Async Function VerifyAwaitKeyword(markup As String, Optional makeContainerAsync As Boolean = False, Optional includeConfigureAwait As Boolean = False) As Task
            Await VerifyItemExistsAsync(markup, "Await", inlineDescription:=If(makeContainerAsync, FeaturesResources.Make_containing_scope_async, Nothing))
            If includeConfigureAwait Then
                Await VerifyItemExistsAsync(markup, "Awaitf", inlineDescription:=If(makeContainerAsync, FeaturesResources.Make_containing_scope_async, Nothing))
            Else
                Await VerifyItemIsAbsentAsync(markup, "Awaitf")
            End If
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InSynchronousMethodTest() As Task
            Await VerifyAwaitKeyword("
Class C
     Sub Goo()
        Dim z = $$
    End Sub
End Class
", makeContainerAsync:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InMethodStatementTest() As Task
            Await VerifyAwaitKeyword("
Class C
    Async Sub Goo()
        $$
    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InMethodExpressionTest() As Task
            Await VerifyAwaitKeyword("
Class C
    Async Sub Goo()
        Dim z = $$
    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInCatchTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInCatchExceptionFilterTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Catch When Err = $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InCatchNestedDelegateTest() As Task
            Await VerifyAwaitKeyword("
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = Function() $$
        End Try

    End Sub
End Class
", makeContainerAsync:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInFinallyTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Finally
            Dim z = $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInSyncLockTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        SyncLock True
            Dim z = $$
        End SyncLock
    End Sub
End Class
")
        End Function
    End Class
End Namespace

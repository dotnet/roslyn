' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Navigation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockNavigableItemsPresenter
        Implements INavigableItemsPresenter

        Private ReadOnly _callback As Action(Of IList(Of INavigableItem))

        Public Sub New(callback As Action(Of IList(Of INavigableItem)))
            _callback = callback
        End Sub

        Public Sub DisplayResult(title As String, items As IEnumerable(Of INavigableItem)) Implements INavigableItemsPresenter.DisplayResult
            _callback(items.ToList())
        End Sub
    End Class
End Namespace

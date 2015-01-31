' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Navigation

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.GoToDefinition
    Friend Class MockNavigableItemsPresenter
        Implements INavigableItemsPresenter

        Private _callback As Action(Of IList(Of INavigableItem))

        Public Sub New(Optional callback As Action(Of IList(Of INavigableItem)) = Nothing)
            _callback = callback
        End Sub

        Public Sub DisplayResult(items As IEnumerable(Of INavigableItem)) Implements INavigableItemsPresenter.DisplayResult
            If _callback IsNot Nothing Then
                _callback(items.ToList())
            End If
        End Sub
    End Class
End Namespace

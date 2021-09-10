' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestSignatureHelpPresenterSession
        Implements ISignatureHelpPresenterSession

        Private ReadOnly _testState As IIntelliSenseTestState

        Public TriggerSpan As ITrackingSpan
        Public SignatureHelpItems As IList(Of SignatureHelpItem)
        Public SelectedItem As SignatureHelpItem
        Public SelectedParameter As Integer?
        Private presented As Boolean

        Public ReadOnly Property EditorSessionIsActive As Boolean Implements ISignatureHelpPresenterSession.EditorSessionIsActive
            Get
                Return presented
            End Get
        End Property

        Public Event Dismissed As EventHandler(Of EventArgs) Implements ISignatureHelpPresenterSession.Dismissed
        Public Event ItemSelected As EventHandler(Of SignatureHelpItemEventArgs) Implements ISignatureHelpPresenterSession.ItemSelected

        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Sub PresentItems(triggerSpan As ITrackingSpan,
                                signatureHelpItems As IList(Of SignatureHelpItem),
                                selectedItem As SignatureHelpItem,
                                selectedParameter As Integer?) Implements ISignatureHelpPresenterSession.PresentItems
            _testState.CurrentSignatureHelpPresenterSession = Me
            Me.TriggerSpan = triggerSpan
            Me.SignatureHelpItems = signatureHelpItems
            Me.SelectedItem = selectedItem
            Me.SelectedParameter = selectedParameter
            Me.presented = True
        End Sub

        Public Sub Dismiss() Implements ISignatureHelpPresenterSession.Dismiss
            _testState.CurrentSignatureHelpPresenterSession = Nothing
            Me.presented = False
        End Sub

        Public Sub SetSelectedItem(item As SignatureHelpItem)
            Me.SelectedItem = item
            RaiseEvent ItemSelected(Me, New SignatureHelpItemEventArgs(item))
        End Sub

        Public Sub SelectPreviousItem() Implements ISignatureHelpPresenterSession.SelectPreviousItem
            Navigate(-1)
        End Sub

        Public Sub SelectNextItem() Implements ISignatureHelpPresenterSession.SelectNextItem
            Navigate(1)
        End Sub

        Private Sub Navigate(count As Integer)
            SetSelectedItem(SignatureHelpItems((SignatureHelpItems.IndexOf(Me.SelectedItem) + count + SignatureHelpItems.Count) Mod SignatureHelpItems.Count))
        End Sub
    End Class
End Namespace

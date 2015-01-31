' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestSignatureHelpPresenterSession
        Implements ISignatureHelpPresenterSession

        Private ReadOnly testState As IIntelliSenseTestState

        Public TriggerSpan As ITrackingSpan
        Public SignatureHelpItems As IList(Of SignatureHelpItem)
        Public SelectedItem As SignatureHelpItem
        Public SelectedParameter As Integer?

        Public Event Dismissed As EventHandler(Of EventArgs) Implements ISignatureHelpPresenterSession.Dismissed
        Public Event ItemSelected As EventHandler(Of SignatureHelpItemEventArgs) Implements ISignatureHelpPresenterSession.ItemSelected

        Public Sub New(testState As IIntelliSenseTestState)
            Me.testState = testState
        End Sub

        Public Sub PresentItems(triggerSpan As ITrackingSpan,
                                signatureHelpItems As IList(Of SignatureHelpItem),
                                selectedItem As SignatureHelpItem,
                                selectedParameter As Integer?) Implements ISignatureHelpPresenterSession.PresentItems
            testState.CurrentSignatureHelpPresenterSession = Me
            Me.TriggerSpan = triggerSpan
            Me.SignatureHelpItems = signatureHelpItems
            Me.SelectedItem = selectedItem
            Me.SelectedParameter = selectedParameter
        End Sub

        Public Sub Dismiss() Implements ISignatureHelpPresenterSession.Dismiss
            testState.CurrentSignatureHelpPresenterSession = Nothing
        End Sub

        Sub SetSelectedItem(item As SignatureHelpItem)
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

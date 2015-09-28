' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestCompletionPresenterSession
        Implements ICompletionPresenterSession

        Private ReadOnly _testState As IIntelliSenseTestState

        Public TriggerSpan As ITrackingSpan
        Public ReadOnly Property CompletionItems As IList(Of CompletionItem)
            Get
                Return GetSelectedModel().Items
            End Get
        End Property
        Public Property SelectedItem As CompletionItem
            Get
                Return GetSelectedModel().SelectedItem
            End Get
            Set(value As CompletionItem)
                Dim oldModel = GetSelectedModel()
                Dim index = Array.IndexOf(models, oldModel)
                models(index) = New CompletionPresentationData(oldModel.Items, value, oldModel.PresetBuilder, oldModel.IsSoftSelected, oldModel.ModelId, "Test", True)
            End Set
        End Property
        Public Property IsSoftSelected As Boolean
            Get
                Return GetSelectedModel().IsSoftSelected
            End Get
            Set(value As Boolean)
                Dim oldModel = GetSelectedModel()
                Dim index = Array.IndexOf(models, oldModel)
                models(index) = New CompletionPresentationData(oldModel.Items, oldModel.SelectedItem, oldModel.PresetBuilder, value, oldModel.ModelId, "Test", True)
            End Set
        End Property

        Public ReadOnly Property Builder As CompletionItem
            Get
                Return GetSelectedModel().PresetBuilder
            End Get
        End Property

        Public Function GetSelectedModel() As CompletionPresentationData
            Dim selectedModels = models.Where(Function(m) m.IsSelectedList)
            Assert.True(selectedModels.Count() <= 1)

            Return selectedModels.FirstOrDefault()
        End Function

        Public Event Dismissed As EventHandler(Of EventArgs) Implements ICompletionPresenterSession.Dismissed
        Public Event ItemSelected As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenterSession.ItemSelected
        Public Event ItemCommitted As EventHandler(Of CompletionItemEventArgs) Implements ICompletionPresenterSession.ItemCommitted
        Public Event CompletionListSelected As EventHandler(Of CompletionListSelectedEventArgs) Implements ICompletionPresenterSession.CompletionListSelected

        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Sub PresentModels(triggerSpan As ITrackingSpan, data As ImmutableArray(Of CompletionPresentationData), suggestionMode As Boolean) Implements ICompletionPresenterSession.PresentModels
            _testState.CurrentCompletionPresenterSession = Me
            Me.TriggerSpan = triggerSpan
            Me.models = data.ToArray()
        End Sub

        Public Sub Dismiss() Implements ICompletionPresenterSession.Dismiss
            _testState.CurrentCompletionPresenterSession = Nothing
        End Sub

        Public Sub SetSelectedItem(item As CompletionItem)
            Me.SelectedItem = item
            RaiseEvent ItemSelected(Me, New CompletionItemEventArgs(item))
        End Sub

        Private Function GetFilteredItemAt(index As Integer) As CompletionItem
            index = Math.Max(0, Math.Min(CompletionItems.Count - 1, index))
            Return CompletionItems(index)
        End Function

        Private Sub SelectPreviousItem() Implements ICompletionPresenterSession.SelectPreviousItem
            If IsSoftSelected Then
                IsSoftSelected = False
            Else
                SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) - 1))
            End If
        End Sub

        Private Sub SelectNextItem() Implements ICompletionPresenterSession.SelectNextItem
            If IsSoftSelected Then
                IsSoftSelected = False
            Else
                SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) + 1))
            End If
        End Sub

        Private Const s_itemsPerPage = 9
        Public models As CompletionPresentationData()

        Public Sub SelectPreviousPageItem() Implements ICompletionPresenterSession.SelectPreviousPageItem
            SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) - s_itemsPerPage))
        End Sub

        Public Sub SelectNextPageItem() Implements ICompletionPresenterSession.SelectNextPageItem
            SetSelectedItem(GetFilteredItemAt(CompletionItems.IndexOf(SelectedItem) + s_itemsPerPage))
        End Sub

        Friend Sub SelectTab(v As String)
            Dim modelToSelect = models.First(Function(m) m.Title = v)
            RaiseEvent CompletionListSelected(Me, New CompletionListSelectedEventArgs(GetSelectedModel()?.ModelId, modelToSelect.ModelId))
        End Sub

        Friend Sub SelectNoTab()
            RaiseEvent CompletionListSelected(Me, New CompletionListSelectedEventArgs(GetSelectedModel()?.ModelId, Nothing))
        End Sub
    End Class
End Namespace

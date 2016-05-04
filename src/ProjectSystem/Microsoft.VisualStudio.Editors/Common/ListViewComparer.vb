' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.Common

    ''' ;ListViewComparer
    ''' <summary>
    ''' IComparer for ListView. 
    ''' - Sort the ListView based on the current column or the first column if current column values are equal.
    ''' - Shared method to handle a column click event and sort the list view.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ListViewComparer
        Implements IComparer

        ' which column is used to sort the list view
        Private _sortColumn As Integer

        Private _sorting As SortOrder = SortOrder.Ascending

        '@ <Summary>
        '@  Which column should be used to sort the list. Start from 0
        '@ </Summary>
        Public Property SortColumn() As Integer
            Get
                Return _sortColumn
            End Get
            Set(ByVal value As Integer)
                _sortColumn = Value
            End Set
        End Property

        '@ <Summary>
        '@  which order, Ascending or Descending
        '@ </Summary>
        Public Property Sorting() As SortOrder
            Get
                Return _sorting
            End Get
            Set(ByVal value As SortOrder)
                _sorting = Value
            End Set
        End Property

        '@ <Summary>
        '@  Compare two list items
        '@ </Summary>
        Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Dim ret As Integer = String.Compare(GetColumnValue(x, _sortColumn), GetColumnValue(y, _sortColumn), StringComparison.OrdinalIgnoreCase)
            If ret = 0 AndAlso _sortColumn <> 0 Then
                ret = String.Compare(GetColumnValue(x, 0), GetColumnValue(y, 0), StringComparison.OrdinalIgnoreCase)
            End If
            If _sorting = SortOrder.Descending Then
                ret = -ret
            End If
            Return ret
        End Function

        '@ <Summary>
        '@  Get String Value of one column
        '@ </Summary>
        Private Function GetColumnValue(ByVal obj As Object, ByVal column As Integer) As String
            If TypeOf obj Is ListViewItem Then
                Dim listItem As ListViewItem = CType(obj, ListViewItem)
                Return listItem.SubItems.Item(column).Text
            End If

            Debug.Fail("RefComparer: obj was not an ListViewItem")
            Return String.Empty
        End Function

        Public Shared Sub HandleColumnClick(ByVal listView As ListView, ByVal comparer As ListViewComparer, _
                ByVal e As ColumnClickEventArgs)
            Dim focusedItem As ListViewItem = listView.FocusedItem

            If e.Column <> comparer.SortColumn Then
                comparer.SortColumn = e.Column
                listView.Sorting = SortOrder.Ascending
            Else
                If listView.Sorting = SortOrder.Ascending Then
                    listView.Sorting = SortOrder.Descending
                Else
                    listView.Sorting = SortOrder.Ascending
                End If
            End If
            comparer.Sorting = listView.Sorting
            listView.Sort()

            If focusedItem IsNot Nothing Then
                listView.FocusedItem = focusedItem
            ElseIf listView.SelectedItems.Count > 0 Then
                listView.FocusedItem = listView.SelectedItems(0)
            End If
            If listView.FocusedItem IsNot Nothing Then
                listView.EnsureVisible(listView.FocusedItem.Index)
            End If
        End Sub
    End Class
End Namespace

'******************************************************************************
'* ResourceEditorView_EditorState.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports System
Imports System.Collections
Imports System.Collections.Specialized
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Partial Class ResourceEditorView

        ''' <summary>
        ''' A class that contains the current editor state (current category, grid column widths, etc.).
        '''   This is useful for when the resx file is changed by another editor and we have to reload.
        '''   We can use this saved state to put the editor into close to the original state after
        '''   the reload (which completely diposes the old designers and creates new ones).
        ''' </summary>
        ''' <remarks>
        ''' EditorState is a private class of ResourceEditorView.  This gives it access to all private
        '''   members of ResourceEditorView, but we can still keep it in a separate file through
        '''   the use of partial classes.
        ''' </remarks>
        Friend NotInheritable Class EditorState
            Private m_StatePersisted As Boolean

            'Current category name.  We save by name instead of reference in case the category no longer exists, etc.
            Private m_CurrentCategoryName As String

            'Names of the currently selected resources (in the listview or stringtable, whichever is showing).
            Private m_SelectedResourceNames() As String

            'Widths of the columns in the string table (whether or not the stringtable is currently showing)
            Private m_StringTableColumnWidths() As Integer

            'Current cell in the string table (if string table is currently showing)
            Private m_StringTableCurrentCellAddress As Point

            'Current listview view (thumbnail, icons, etc.) for each category, hashed by category name (whether or not these categories are showing)
            Private m_ResourceViewHash As New ListDictionary

            'Current sorter for each category, hashed by category name (whether or not these categories are showing)
            Private m_CategorySorter As New ListDictionary

            'Widths of the columns in the listview's details view (whether or not the listview is currently showing)
            Private m_ListViewColumnWidths() As Integer




            ''' <summary>
            ''' Public constructor.  Sets it to an empty stte.
            ''' </summary>
            ''' <remarks></remarks>
            Public Sub New()
                m_StatePersisted = False
            End Sub

            ''' <summary>
            ''' Returns whether or not state has actually been persisted into this object.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property StatePersisted() As Boolean
                Get
                    Return m_StatePersisted
                End Get
            End Property

            ''' <summary>
            ''' Clears all the editor state into a non-persisted state.
            ''' </summary>
            ''' <remarks></remarks>
            Private Sub Clear()
                m_StatePersisted = False
                m_CurrentCategoryName = Nothing
                m_SelectedResourceNames = Nothing
                m_StringTableColumnWidths = Nothing
                m_StringTableCurrentCellAddress = New Point(0, 0)
                m_ResourceViewHash.Clear()
                m_CategorySorter.Clear()
                m_ListViewColumnWidths = Nothing
            End Sub

            ''' <summary>
            ''' Persists state from a given resource editor view into this object.
            ''' </summary>
            ''' <param name="View">Resource editor view object to save state from.</param>
            ''' <remarks></remarks>
            Public Sub PersistStateFrom(ByVal View As ResourceEditorView)
                Debug.Assert(View IsNot Nothing, "View can't be Nothing in EditorState")

                Try
                    Clear()

                    'Current category
                    If View.m_CurrentCategory IsNot Nothing Then
                        m_CurrentCategoryName = View.m_CurrentCategory.ProgrammaticName
                    End If

                    'Selected resources (don't include selected cells, just actual selected rows in the string table)
                    Dim SelectedResources() As Resource = View.GetSelectedResources()
                    If SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0 Then
                        'For the string table, 
                        ReDim m_SelectedResourceNames(SelectedResources.Length - 1)
                        For i As Integer = 0 To SelectedResources.Length - 1
                            m_SelectedResourceNames(i) = SelectedResources(i).Name
                        Next
                    End If

                    'String table column widths
                    ReDim m_StringTableColumnWidths(View.StringTable.ColumnCount - 1)
                    For i As Integer = 0 To View.StringTable.ColumnCount - 1
                        m_StringTableColumnWidths(i) = View.StringTable.Columns(i).Width
                    Next

                    'Current cell in the string table (if shown)
                    If View.StringTable.Visible Then
                        m_StringTableCurrentCellAddress = View.StringTable.CurrentCellAddress
                    End If

                    'ListView column widths
                    ReDim m_ListViewColumnWidths(View.ResourceListView.Columns.Count - 1)
                    For i As Integer = 0 To View.ResourceListView.Columns.Count - 1
                        m_ListViewColumnWidths(i) = View.ResourceListView.Columns(i).Width
                    Next

                    'ResourceView mode for all categories
                    For Each Category As Category In View.m_Categories
                        m_ResourceViewHash.Add(Category.ProgrammaticName, Category.ResourceView)
                        m_CategorySorter.Add(Category.ProgrammaticName, Category.Sorter)
                    Next

                    m_StatePersisted = True
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Exception depersisting editor state - state will not be restored.  " & ex.ToString())
                    Clear()

                    'Exception can be safely ignored.  They just won't get the state restored later.
                    Exit Sub
                End Try
            End Sub

            ''' <summary>
            ''' Sets up the given resource editor view instance with the state which has previously been saved in
            '''   this object.
            ''' </summary>
            ''' <param name="View">The resource editor view instance to depersist the old state into</param>
            ''' <remarks></remarks>
            Public Sub DepersistStateInto(ByVal View As ResourceEditorView)
                If m_StatePersisted Then
                    Try
                        Debug.Assert(View IsNot Nothing, "View can't be Nothing in EditorState")

                        'String table column widths
                        If m_StringTableColumnWidths IsNot Nothing AndAlso m_StringTableColumnWidths.Length = View.StringTable.ColumnCount Then
                            For i As Integer = 0 To m_StringTableColumnWidths.Length - 1
                                Try
                                    View.StringTable.Columns(i).Width = m_StringTableColumnWidths(i)
                                Catch ex As Exception
                                    RethrowIfUnrecoverable(ex)
                                    'Ignore exceptions if unable to set a column width (columns can have minimum widths)
                                    Debug.Fail("Unable to set stringtable column width in restoring editor state: " & ex.ToString)
                                End Try
                            Next
                        End If

                        'ListView column widths
                        If m_ListViewColumnWidths IsNot Nothing AndAlso m_ListViewColumnWidths.Length = View.ResourceListView.Columns.Count Then
                            For i As Integer = 0 To m_ListViewColumnWidths.Length - 1
                                Try
                                    View.ResourceListView.Columns(i).Width = m_ListViewColumnWidths(i)
                                Catch ex As Exception
                                    RethrowIfUnrecoverable(ex)
                                    'Ignore exceptions
                                    Debug.Fail("Unable to set listview column width in restoring editor state: " & ex.ToString)
                                End Try
                            Next
                        End If

                        'Current category
                        Dim CurrentCategory As Category = View.m_Categories(m_CurrentCategoryName)
                        If CurrentCategory IsNot Nothing Then
                            View.SwitchToCategory(CurrentCategory)
                        End If

                        'ResourceView mode for all categories
                        Dim NeedsRefresh As Boolean = False
                        For Each Entry As DictionaryEntry In m_ResourceViewHash
                            Dim Category As Category = View.m_Categories(CStr(Entry.Key))
                            If Category IsNot Nothing Then
                                View.ChangeResourceViewForCategory(Category, DirectCast(Entry.Value, ResourceListView.ResourceView))
                            End If
                        Next

                        For Each Entry As DictionaryEntry In m_CategorySorter
                            Dim Category As Category = View.m_Categories(CStr(Entry.Key))
                            If Category IsNot Nothing Then
                                View.ChangeSorterForCategory(Category, DirectCast(Entry.Value, IComparer))
                            End If
                        Next

                        'Selected resources
                        If m_SelectedResourceNames IsNot Nothing Then
                            'We have to search for the resources by name.  Some of them may no longer exist.
                            '  We'll select the ones we can still find.
                            Dim ResourcesToSelect As New ArrayList
                            For Each Name As String In m_SelectedResourceNames
                                Dim Resource As Resource = View.m_ResourceFile.FindResource(Name)
                                If Resource IsNot Nothing Then
                                    ResourcesToSelect.Add(Resource)
                                End If
                            Next
                            View.HighlightResources(ResourcesToSelect, SelectInPropertyGrid:=True)
                        End If

                        'Current cell in the string table (if shown)
                        If View.StringTable.Visible Then
                            m_StringTableCurrentCellAddress = View.StringTable.CurrentCellAddress
                        End If

                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        Debug.Fail("Exception saving editor state - state will not be saved.  " & ex.ToString)
                        Clear()

                        'Exception can be safely ignored.  They just won't get the state restored later.
                        Exit Sub
                    End Try
                End If
            End Sub
        End Class

    End Class

End Namespace


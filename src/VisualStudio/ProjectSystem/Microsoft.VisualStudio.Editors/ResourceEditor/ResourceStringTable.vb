'******************************************************************************
'* ResourceStringTable.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports System
Imports System.Collections
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.Math
Imports System.Security.Permissions
Imports System.Windows.Forms
Imports VB = Microsoft.VisualBasic
Imports Microsoft.VisualStudio.PlatformUI

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is a virtualized grid capable of displaying Resource items as strings.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceStringTable
        Inherits DesignerFramework.DesignerDataGridView

        'Column indices
        Friend Const COLUMN_NAME As Integer = 0
        Friend Const COLUMN_TYPE As Integer = 1 'This column is always there, but may be hidden
        Friend Const COLUMN_VALUE As Integer = 2
        Friend Const COLUMN_COMMENT As Integer = 3

        Private Const ROW_BORDER_HEIGHT As Integer = 2

        'A list of all Resource entries that are displayed in this string table (with the exception
        '  of the uncommitted resource, if any)
        Private m_VirtualResourceList As New ArrayList

        'The row that is currently being removed, if any.
        Private m_RemovingRow As DataGridViewRow

        'The ResourceFile used to populate this grid.
        Private m_ResourceFile As ResourceFile

        'True if this is the first time the grid is being populated.
        Private m_FirstTimeShowingStringTable As Boolean = True

        'A Resource which is created when the user starts entering data, but
        '  hasn't committed any of that data yet.
        Private m_UncommittedResource As Resource

        'This is the last "uncommitted" row that was just committed.  If the user presses
        '  ESC, this row will be deleted again by the grid, and we'll need to uncommit it
        '  by removing it from the ResourceFile again.
        Private m_LastCommittedResource As Resource

        'In one edit action, we should only try to check out once...
        ' CONSIDER: should we implement it on the DesignerLoader, so we can support other controls as well?
        Private m_InEditActions As Integer

        'Indicates whether check-out has failed once in this action...
        Private m_CheckOutFailedInTheAction As Boolean


        'Default column width percentages for each column.  Yes, this adds up to more than 100%
        '  if the type column is visible, but that's okay.
        Private Const DefaultColumnWidthPercentage_Name As Integer = 20
        Private Const DefaultColumnWidthPercentage_Type As Integer = 25
        Private Const DefaultColumnWidthPercentage_Value As Integer = 50
        Private Const DefaultColumnWidthPercentage_Comment As Integer = 30

        'Minimum scrolling widths
        Private Const ColumnMinScrollingWidth_Name As Integer = 100
        Private Const ColumnMinScrollingWidth_Type As Integer = 60
        Private Const ColumnMinScrollingWidth_Value As Integer = 150
        Private Const ColumnMinScrollingWidth_Comment As Integer = 80

        'The error glyphs don't show up if the row height is below the glyph size.  We don't want that
        '  to happen, so restrict the minimum height.
        Private Const RowMinimumHeight As Integer = 20

        ' Current sorter
        Private m_Sorter As StringTableSorter

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            InitializeColumns()

            AllowUserToAddRows = False
            AllowUserToDeleteRows = False
            Me.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            Me.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            Me.DefaultCellStyle.WrapMode = DataGridViewTriState.True

            ' when the NullValue set to empty string, DataGridView will compare input string with it by using String.Compare.
            '  Because the Chinese minority characters have zero weight, it always convert it to 'null/DBNull', which blocks the user
            ' to input those characters. (Devdiv bug: 105667).  It is a workaround to block user to input null value.
            '  Our code has already handled the empty string, so we don't need the extra NullValue process in the DataGridView.
            Me.DefaultCellStyle.NullValue = Nothing

            VirtualMode = True
        End Sub





#Region "Properties"

        ''' <summary>
        '''  Return true if the user selected whole lines in the DataGridView...
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property InLineSelectionMode() As Boolean
            Get
                If MyBase.SelectionMode = DataGridViewSelectionMode.FullRowSelect Then
                    Return True
                End If

                Dim cells As DataGridViewSelectedCellCollection = MyBase.SelectedCells

                If cells.Count > 0 Then
                    Dim rows As New Generic.Dictionary(Of Integer, Integer)
                    For Each cell As DataGridViewCell In cells
                        If rows.ContainsKey(cell.RowIndex) Then
                            rows(cell.RowIndex) = rows(cell.RowIndex) + 1
                        Else
                            rows(cell.RowIndex) = 1
                        End If
                    Next

                    Dim visibleColumnCount As Integer
                    If TypeColumnVisible Then
                        visibleColumnCount = 4
                    Else
                        visibleColumnCount = 3
                    End If

                    For Each i As Integer In rows.Values
                        If i < visibleColumnCount Then
                            Return False
                        End If
                    Next
                    Return True
                End If
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Indicates whether or not the "Type" column is visible to the user.  This
        '''   column is always present, but its visibility can be turned on/off.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property TypeColumnVisible() As Boolean
            Get
                Debug.Assert(Me.ColumnCount >= COLUMN_TYPE, "Columns not set up properly?")
                Return Me.Columns(COLUMN_TYPE).Visible
            End Get
            Set(ByVal Value As Boolean)
                Debug.Assert(Me.ColumnCount >= COLUMN_TYPE, "Columns not set up properly?")
                Debug.Assert(RowCountVirtual = 0, "Shouldn't be changing TypeColumnVisible after it's already been populated with data")
                Me.Columns(COLUMN_TYPE).Visible = Value
            End Set
        End Property


        ''' <summary>
        ''' Return the ResourceEditorView which is the parent of this
        '''   control.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property ParentView() As ResourceEditorView
            Get
                If TypeOf Me.Parent Is ResourceEditorView Then
                    Return DirectCast(Me.Parent, ResourceEditorView)
                Else
                    Debug.Fail("Not parented to a ResourceEditorView?")
                    Throw New System.InvalidOperationException
                End If
            End Get
        End Property


        ''' <summary>
        ''' Gets the ResourceFile that was used to populate this grid.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Can be called only after population</remarks>
        Private ReadOnly Property ResourceFile() As ResourceFile
            Get
                Debug.Assert(m_ResourceFile IsNot Nothing, "Has Populate not yet been called?  m_ResourceFile is nothing")
                Return m_ResourceFile
            End Get
        End Property

        
#End Region




#Region "Initialization"

        ''' <summary>
        ''' Sets up all the columns properly
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub InitializeColumns()
            'There are four columns in the string table:
            '
            '  Name
            '  Type (hidden for "Strings" view, visible in "Other" view)
            '  Value
            '  Comment


            ' ==== Name Column

            Dim ColumnWidth As Integer
            ColumnWidth = DpiHelper.LogicalToDeviceUnitsX(ColumnMinScrollingWidth_Name)
            Dim NameColumn As New DataGridViewTextBoxColumn
            With NameColumn
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                .CellTemplate = New ResourceStringTextBoxCell()
                .FillWeight = DefaultColumnWidthPercentage_Name
                .MinimumWidth = ColumnWidth
                .Name = SR.GetString(SR.RSE_ResourceNameColumn)
                .Width = ColumnWidth
                Debug.Assert(COLUMN_NAME = Me.Columns.GetColumnCount(DataGridViewElementStates.Visible), "COLUMN_NAME constant is not correct")
                .HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            End With
            Me.Columns.Add(NameColumn)

            ' ==== Type Column

            ColumnWidth = DpiHelper.LogicalToDeviceUnitsX(ColumnMinScrollingWidth_Type)
            Dim TypeColumn As New DataGridViewTextBoxColumn
            With TypeColumn
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                .CellTemplate = New ResourceStringTextBoxCell()
                .FillWeight = DefaultColumnWidthPercentage_Type
                .MinimumWidth = ColumnWidth
                .Name = SR.GetString(SR.RSE_TypeColumn)
                .ReadOnly = True 'Can't modify the Type column - just for info
                .Width = ColumnWidth
                Debug.Assert(COLUMN_TYPE = Me.Columns.GetColumnCount(DataGridViewElementStates.Visible), "COLUMN_TYPE constant is not correct")
                .HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            End With
            Me.Columns.Add(TypeColumn)

            ' ==== Value Column

            '... By specifying DataGridViewTextBoxColumn here, we're indicating that user-added rows
            '      (i.e., through the "new row" at the bottom of the DataGridView) will be of type
            '      ResourceStringTextBoxCell.
            ColumnWidth = DpiHelper.LogicalToDeviceUnitsX(ColumnMinScrollingWidth_Value)
            Dim ValueColumn As New DataGridViewTextBoxColumn
            With ValueColumn
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                .CellTemplate = New ResourceStringTextBoxCell()
                .FillWeight = DefaultColumnWidthPercentage_Value
                .MinimumWidth = ColumnWidth
                .Name = SR.GetString(SR.RSE_ResourceColumn)
                .Width = ColumnWidth
                Debug.Assert(COLUMN_VALUE = Me.Columns.GetColumnCount(DataGridViewElementStates.Visible), "COLUMN_VALUE constant is not correct")
                .HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            End With
            Me.Columns.Add(ValueColumn)

            ' ==== Comment Column
            ColumnWidth = DpiHelper.LogicalToDeviceUnitsX(ColumnMinScrollingWidth_Comment)
            Dim CommentColumn As New DataGridViewTextBoxColumn
            With CommentColumn
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                .CellTemplate = New ResourceStringTextBoxCell()
                .FillWeight = DefaultColumnWidthPercentage_Comment
                .MinimumWidth = ColumnWidth
                .Name = SR.GetString(SR.RSE_CommentColumn)
                .Width = ColumnWidth
                Debug.Assert(COLUMN_COMMENT = Me.Columns.GetColumnCount(DataGridViewElementStates.Visible), "COLUMN_COMMENT constant is not correct")
                .HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            End With
            Me.Columns.Add(CommentColumn)

            '... We need to turn off autosizing of columns because that would mean users would not 
            '      be allowed to change the column widths.


        End Sub


        ''' <summary>
        ''' Populates the grid with all resources from a ResourceFile which are in the
        '''   given category.
        ''' </summary>
        ''' <param name="ResourceFile">The source of the Resources</param>
        ''' <param name="CategoryToFilterOn">Which category of resources to show.</param>
        ''' <remarks></remarks>
        Public Sub Populate(ByVal ResourceFile As ResourceFile, ByVal CategoryToFilterOn As Category)
            m_ResourceFile = ResourceFile
            Rows.Clear()
            m_VirtualResourceList.Clear()

            'First, create a sorted list of resources that we want to display
            Dim ResourcesToDisplay As New ArrayList
            Dim Categories As CategoryCollection = ResourceFile.RootComponent.RootDesigner.GetView().Categories
            For Each Entry As DictionaryEntry In ResourceFile
                Dim Resource As Resource = DirectCast(Entry.Value, Resource)
                If Resource.GetCategory(Categories) Is CategoryToFilterOn Then
                    If Resource.ResourceTypeEditor.DisplayInStringTable Then
                        ResourcesToDisplay.Add(Resource)
                    Else
                        Debug.Fail("Huh?  All resources that get here should be displayable in a string table")
                        'Skip (defensive)
                    End If
                End If
            Next

            ' restore sort order
            m_Sorter = TryCast(CategoryToFilterOn.Sorter, StringTableSorter)
            If m_Sorter Is Nothing Then
                m_Sorter = New StringTableSorter(0, False)
                CategoryToFilterOn.Sorter = m_Sorter
            End If

            ' restore sort UI
            For i As Integer = 0 To Me.Columns.Count - 1
                If i <> m_Sorter.ColumnIndex Then
                    Me.Columns(i).HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None
                ElseIf m_Sorter.InReverseOrder Then
                    Me.Columns(i).HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.Descending
                Else
                    Me.Columns(i).HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.Ascending
                End If
            Next

            '... Sort the list alphabetically to start with (when we add new entries, we'll add them to
            '  the list at the end)
            ResourcesToDisplay.Sort(m_Sorter)

            'Now add these resources into the table
            AddResourcesHelper(ResourcesToDisplay)

            Me.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            Me.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        End Sub


        ''' <summary>
        ''' Clear all entries
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Clear()
            Rows.Clear()
            m_VirtualResourceList.Clear()
            m_UncommittedResource = Nothing
        End Sub


        ''' <summary>
        ''' Creates a new row with blank values
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CreateNewResourceRow() As DataGridViewRow
            Dim NewRow As New DataGridViewRow

            'The error glyphs don't show up if the row height is below the glyph size.  We don't want that
            '  to happen, so restrict the minimum height.
            NewRow.MinimumHeight = Math.Max(DpiHelper.LogicalToDeviceUnitsY(RowMinimumHeight), Me.Font.Height + DpiHelper.LogicalToDeviceUnitsY(ROW_BORDER_HEIGHT))

            'Build up the new row (with blank values) cell by cell

            '==== Name column

            Dim NameCell As New ResourceStringTextBoxCell
            NewRow.Cells.Add(NameCell)

            '==== Type column
            Dim TypeCell As New ResourceStringTextBoxCell
            NewRow.Cells.Add(TypeCell)

            '==== Value column

            Dim ValueCell As New ResourceStringTextBoxCell
            NewRow.Cells.Add(ValueCell)

            '==== Comment column
            Dim CommentCell As New ResourceStringTextBoxCell
            NewRow.Cells.Add(CommentCell)

            Return NewRow
        End Function


#End Region



#Region "General UI"

        ''' <summary>
        ''' Display a messagebox (delegates to ParentView)
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="Buttons"></param>
        ''' <param name="Icon"></param>
        ''' <param name="DefaultButton"></param>
        ''' <param name="HelpLink"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function DsMsgBox(ByVal Message As String, _
                    ByVal Buttons As MessageBoxButtons, _
                    ByVal Icon As MessageBoxIcon, _
                    Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                    Optional ByVal HelpLink As String = Nothing) As DialogResult
            If ParentView IsNot Nothing Then
                Return ParentView.DsMsgBox(Message, Buttons, Icon, DefaultButton, HelpLink)
            End If
        End Function


        ''' <summary>
        ''' Commits all pending changes that the user has made in the grid.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub CommitPendingChanges()
            If IsCurrentCellDirty Then
                'We have a cell dirty.  We should commit it.  However, we need to verify that it's okay to commit
                '  (e.g., validate the Name, etc.), so we'll validate the cell first.
                Dim Cancel As Boolean = False
                Cancel = Not ValidateCell(CurrentCell.RowIndex, CurrentCell.ColumnIndex, CStr(CurrentCell.EditedFormattedValue))
                If Not Cancel Then
                    'The cell has validated successfully.  Go ahead and commit the changes.
                    EndEdit(DataGridViewDataErrorContexts.Commit)
                Else
                    'Otherwise, we need to abort the changes.  This isn't ideal behavior, but it's better than forcing the
                    '  user to be stuck in the resource editor.
                    CancelEdit()
                    EndEdit(DataGridViewDataErrorContexts.InitialValueRestoration )
                End If
            Else If IsCurrentCellInEditMode Then
                ' We should leave EditMode anyway (we could be in ReadOnly mode after F5)
                EndEdit(DataGridViewDataErrorContexts.InitialValueRestoration )
            End If
        End Sub


        ''' <summary>
        ''' Invalidates the row for a particular resource, causing it to be redrawn with
        '''   new data at the next paint.
        ''' </summary>
        ''' <param name="Resource">The Resource whose row should be invalidated.</param>
        ''' <remarks></remarks>
        Public Sub InvalidateResource(ByVal Resource As Resource)
            Dim RowIndex As Integer = GetRowIndexFromResource(Resource)
            If RowIndex >= 0 Then
                InvalidateRow(RowIndex)
            End If
        End Sub

        ''' <summary>
        ''' When the font has changed, adjust the height of rows, so it could show at least one line of text
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnFontChanged(ByVal e As EventArgs)
            MyBase.OnFontChanged(e)

            ' replace the RowTemplate, so new row would be initialized to the right size
            Me.RowTemplate = CreateNewResourceRow()
        End Sub
#End Region


#Region "Validation"

        ''' <summary>
        ''' Called for eror handling.  Shouldn't ever be called in our case, added just
        '''   to assert in case.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDataError(ByVal displayErrorDialogIfNoHandler As Boolean, ByVal e As System.Windows.Forms.DataGridViewDataErrorEventArgs)
            MyBase.OnDataError(displayErrorDialogIfNoHandler, e)

            Debug.Fail("DataError fired - do we need to handle this somehow?")
        End Sub


        ''' <summary>
        ''' Validates the data a user has typed into a single cell.
        ''' This only validates cell values that are never allowed to be committed at all.
        '''   Currently that means empty Name or bad string conversion (e.g., the user
        '''   tried to set a Boolean value in the Other category to "Tru").  Thus no
        '''   task list integration is necessary here.
        ''' </summary>
        ''' <param name="RowIndex">Row of the cell to validate</param>
        ''' <param name="ColumnIndex">Column of the cell to validate</param>
        ''' <param name="FormattedValue">The string value which the user has typed</param>
        ''' <param name="Exception">The exception to show if the validation fails</param>
        ''' <returns>True if the cell validates, or False if it fails.</returns>
        ''' <remarks></remarks>
        Protected Function ValidateCell(ByVal RowIndex As Integer, ByVal ColumnIndex As Integer, ByVal FormattedValue As String, Optional ByRef Exception As Exception = Nothing) As Boolean
            Dim Cancel As Boolean = False

            If Rows(RowIndex).ReadOnly OrElse Rows.SharedRow(RowIndex).Cells(ColumnIndex).ReadOnly Then
                'No reason to do any checking for cells that we don't allow the user to change
                Return True
            End If

            'Figure out which Resource this corresponds to
            Dim Resource As Resource = GetResourceFromRowIndex(RowIndex, True)
            If Resource Is Nothing Then
                Debug.Fail("Resource not associated with row")
                Return True
            End If

            '... and call the proper validation on it
            Select Case ColumnIndex
                Case COLUMN_NAME
                    Return Resource.ValidateName(FormattedValue, Resource.Name, , Exception)

                Case COLUMN_VALUE
                    Return Resource.ValidateValueAsString(FormattedValue, , Exception)

                Case Else
                    Return True
            End Select

            Debug.Fail("Shouldn't reach here")
        End Function


        ''' <summary>
        ''' This is called by the grid when a cell needs to be validated.  We try to 
        '''  validate it, and if the validation fails, we show a messagebox.
        ''' </summary>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCellValidating(ByVal e As System.Windows.Forms.DataGridViewCellValidatingEventArgs)
            MyBase.OnCellValidating(e)

            Dim originalValue As String = GetCellStringValue(e.RowIndex, e.ColumnIndex)
            Dim Exception As Exception = Nothing

            ' we should ignore the problem if it is the original value, or the cursor will be locked inside such cell
            If String.Compare(CStr(e.FormattedValue), originalValue, StringComparison.Ordinal) = 0 OrElse _
                ValidateCell(e.RowIndex, e.ColumnIndex, CStr(e.FormattedValue), Exception) Then
                'Validation succeeded
                e.Cancel = False
            Else
                'Bad data.  Show message box.
                ParentView.DsMsgBox(Exception)
                e.Cancel = True
            End If
        End Sub


#End Region


#Region "Source code control"

        ''' <summary>
        ''' Before we rush into edit mode on a single cell click, we make sure that everything is checked out OK...
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Friend Overrides Sub OnCellClickBeginEdit(ByVal e As System.ComponentModel.CancelEventArgs)
            MyBase.OnCellClickBeginEdit(e)
            If Not e.Cancel Then
                Dim DesignerLoader As DesignerFramework.BaseDesignerLoader = ParentView.RootDesigner.DesignerLoader
                If DesignerLoader IsNot Nothing Then
                    e.Cancel = Not DesignerLoader.OkToEdit()
                End If
            End If
        End Sub


        ''' <summary>
        ''' Occurs when the user starts to edit values in the grid.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCellBeginEdit(ByVal e As System.Windows.Forms.DataGridViewCellCancelEventArgs)
            ' First of all, we should never enter edit mode if we are in read-only mode...
            If ParentView.ReadOnlyMode Then
                e.Cancel = True
                Return
            End If

            'As soon as they try to start changing a cell, we need to attempt to check out the resx file, if it hasn't
            '  already been done.  That way if there's a failure checking it out, we let them know as soon as possible.
            If ParentView.ReadOnlyMode Then
                e.Cancel = True
                Return
            End If

            Try
                ParentView.OnItemBeginEdit()
            Catch ex As Exception
                'Cancel and show an error messagebox (actually, DsMsgBox knows not to show anything if it's dealing
                'with a CheckoutCanceled exception; the user is already aware he canceled)
                e.Cancel = True
                ParentView.DsMsgBox(ex)
            End Try

            'If the user starts editing a cell, go ahead and expand its height so all the text is visible.  We don't do this
            '  by default for everything because it would force all rows to be retrieved, thus defeating the purpose of using
            '  virtual mode.  This at least is a compromise.
            ExpandRowHeightIfNeeded(e.RowIndex)

            MyBase.OnCellBeginEdit(e)
        End Sub

        ''' <summary>
        ''' Expands the height of the row to show all its text
        ''' </summary>
        ''' <param name="rowIndex"></param>
        ''' <remarks></remarks>
        Private Sub ExpandRowHeightIfNeeded(ByVal rowIndex As Integer)
            Dim preferredHeight As Integer = Me.Rows(rowIndex).GetPreferredHeight(rowIndex, DataGridViewAutoSizeRowMode.AllCells, True)
            Dim currentHeight As Integer = Me.Rows(rowIndex).Height
            If preferredHeight > currentHeight Then
                Dim newHeight As Integer = preferredHeight

                'Not greater than the datagrid view's height
                Dim maxHeight As Integer = Me.ClientSize.Height - Me.ColumnHeadersHeight
                newHeight = Math.Min(maxHeight, newHeight)

                If newHeight > currentHeight Then
                    '... and not smaller than the current height
                    Me.Rows(rowIndex).Height = newHeight
                End If
            End If
        End Sub

        ''' <summary>
        ''' Occurs when the user end to edit values in the grid.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCellEndEdit(ByVal e As System.Windows.Forms.DataGridViewCellEventArgs)
            ParentView.OnItemEndEdit()
            MyBase.OnCellEndEdit(e)
        End Sub

        ''' <summary>
        ''' Occurs when the user starts to change something...
        '''  We should check whether we can edit the cell early before a new row is added.
        ''' </summary>
        Protected Overrides Sub OnCurrentCellDirtyStateChanged(ByVal e As EventArgs)
            If IsCurrentCellDirty Then
                If Not m_CheckOutFailedInTheAction Then
                    ' Make sure the file has been checked out...
                    Try
                        ParentView.RootDesigner.DesignerLoader.ManualCheckOut()
                    Catch ex As Exception
                        'Cancel and show an error messagebox (actually, DsMsgBox knows not to show anything if it's dealing
                        'with a CheckoutCanceled exception; the user is already aware he canceled)
                        ParentView.DsMsgBox(ex)

                        m_CheckOutFailedInTheAction = True
                    End Try
                End If

                If m_CheckOutFailedInTheAction Then
                    If Me.EditingControl IsNot Nothing Then
                        Me.EditingControl.Select()
                    End If

                    CancelEdit()
                    Return
                End If
            End If
            MyBase.OnCurrentCellDirtyStateChanged(e)
        End Sub

        ''' <summary>
        ''' Occurs before we start an edit or non-edit action.
        '''  We should only try to check out one time for one action...
        ''' CONSIDER: should we implement this on the DesignerLoader?
        ''' </summary>
        Friend Sub StartOneAction()
            If m_InEditActions = 0 Then
                m_CheckOutFailedInTheAction = False
            End If
            m_InEditActions = m_InEditActions + 1
        End Sub

        ''' <summary>
        ''' Occurs before we end an edit or non-edit action.
        '''  We should only try to check out one time for one action...
        ''' CONSIDER: should we implement this on the DesignerLoader?
        ''' </summary>
        Friend Sub EndOneAction()
            m_InEditActions = m_InEditActions - 1
            Debug.Assert(m_InEditActions >= 0, "we should never call EndOneAction more than StartOneAction!")
        End Sub

#End Region


#Region "Adding/Removing Resources"


        ''' <summary>
        ''' Adds a set of Resources to the grid (i.e., resources that were added since calling Populate).
        ''' </summary>
        ''' <param name="Resources">The Resources to add.  They must already be present in the ResourceFile.</param>
        ''' <remarks></remarks>
        Public Sub AddResources(ByVal Resources As IList)
            UnselectAll()
            Debug.Assert(m_VirtualResourceList.Count = RowCountVirtual)

            AddResourcesHelper(Resources)

            MyBase.Invalidate()
        End Sub


        ''' <summary>
        ''' Helper to add a set of Resources to the grid.
        ''' </summary>
        ''' <param name="ResourcesToAdd">Resources to add.  They must already be present in the resourceFile</param>
        ''' <remarks>Does not handle selection or anything except just physically adding the new resource rows.</remarks>
        Private Sub AddResourcesHelper(ByVal ResourcesToAdd As IList)
            Debug.Assert(ResourceFile IsNot Nothing, "Must call Populate() first")

            ' create a RowTemplate, so new row would be initialized to the right size
            Me.RowTemplate = CreateNewResourceRow()

            Dim RowCountOriginal As Integer = RowCountVirtual

            'First, populate our store of virtual entries
            For Each Resource As Resource In ResourcesToAdd
                If Not ResourceFile.Contains(Resource) Then
                    Debug.Fail("Trying to add a resource to the string table which isn't in the ResourceFile")
                    Exit Sub 'defensive
                End If

                Debug.Assert(Resource.ResourceTypeEditor.DisplayInStringTable())
                m_VirtualResourceList.Add(Resource)
            Next

            '... Then add rows for each virtual entry

            'Note: The DataGridView's virtual mode includes a concept of "shared rows."  The idea is that
            '  you don't want to have to allocate a DataGridViewRow (plus cells) for all virtual elements
            '  in the grid, since that defeats the main purpose of using virtual mode in the first place.
            'To use it, you have to use Rows.AddCopy/AddCopies instead of Rows.Add.  This is easy in the
            '  case of a table of simple string resources, because the rows' attributes are all the same.
            '  But in the "Other" resource category, some cells are different (read-only for instance),
            '  or in the future we might have custom DataGridView cells.  In these cases, keeping rows
            '  as shared as possible is more difficult.  Since we are unlikely to have so many resources
            '  in the "Other" category that this makes a difference, we'll keep all simple string rows
            '  shared, but we won't bother trying to share non-simple rows.

            'The row index of the last row that was sharable (i.e., it had no unusual attributes).  When
            '  we add the next shared row, we'll use this as an index.
            'The value of -1 means there is no previous sharable row.
            Dim IndexOfFirstSharableRow As Integer = -1
            Dim SharedRowsToAdd As Integer = 0
            Dim RowIndex As Integer = RowCountOriginal

            For Each Resource As Resource In ResourcesToAdd
                'Add a new row.  If it's a simple row, we'll simply add a copy of the first
                '  sharable row.  Otherwise, we'll add a completely new row.  See note at beginning of method.
                Dim RowIsSharable As Boolean = True
                Dim StringResourceTypeEditor As ResourceTypeEditorStringBase = DirectCast(Resource.ResourceTypeEditor, ResourceTypeEditorStringBase)

                'Right now, the only thing that makes a row unsharable for us is if it is read-only.
                '  In the future, if we have custom DataGridView cell types, then that would make them unsharable, too.
                If Not StringResourceTypeEditor.StringValueCanBeEdited Then
                    RowIsSharable = False
                End If

                'Add the new shared or non-shared row
                If RowIsSharable Then
                    If IndexOfFirstSharableRow < 0 Then
                        'This is the first sharable row.  We need to go ahead and create it.
                        '  Then we can make copies of it as needed.
                        Debug.Assert(SharedRowsToAdd = 0)
                        IndexOfFirstSharableRow = RowIndex
                        Rows.Add(CreateNewResourceRow())
                    Else
                        'We will note that we need to create a shared row, but we want to
                        '  accumulte and add them at once.
                        SharedRowsToAdd += 1
                    End If
                Else
                    'We can't share this row, so we'll have to add it separately.


                    'First add any copied rows that we've been accumulating
                    If SharedRowsToAdd > 0 Then
                        Debug.Assert(IndexOfFirstSharableRow >= 0, "IndexOfFirstSharableRow should have already been set")
                        Rows.AddCopies(IndexOfFirstSharableRow, SharedRowsToAdd)
                        SharedRowsToAdd = 0
                    End If

                    Dim NewUnsharableRow As DataGridViewRow = CreateNewResourceRow()

                    If Not StringResourceTypeEditor.StringValueCanBeEdited Then
                        'If the user isn't allowed to edit this resource, then make the resource cell read-only.
                        NewUnsharableRow.Cells(COLUMN_VALUE).ReadOnly = True
                    End If
                    Rows.Add(NewUnsharableRow)
                End If

                RowIndex += 1
            Next

            '... Add any remaining shared rows that we've accumulated
            If SharedRowsToAdd > 0 Then
                Rows.AddCopies(IndexOfFirstSharableRow, SharedRowsToAdd)
                SharedRowsToAdd = 0
            End If

            'Verify we've added the correct number of rows
            Debug.Assert(RowCountVirtual = ResourcesToAdd.Count + RowCountOriginal)
            Debug.Assert(Not (AllowUserToAddRows AndAlso RowCountVirtual <> Rows.Count - 1), "Incorrect number of rows added (AllowUserToAddRows is True)")
            Debug.Assert(Not (Not AllowUserToAddRows AndAlso RowCountVirtual <> Rows.Count), "Incorrect number of rows added (AllowUserToAddRows is False)")
        End Sub

        ''' <summary>
        ''' Removes a set of resources from this grid, if they're in it.  
        '''  Does *not* remove the resource from the resource file, just from this control's view.
        ''' </summary>
        ''' <param name="Resources">The set of Resources to remove.  Must already be present in this grid.</param>
        ''' <remarks></remarks>
        Public Sub RemoveResources(ByVal Resources As IList)
            UnselectAll()

            Debug.Assert(m_RemovingRow Is Nothing)
            Try
                For Each Resource As Resource In Resources
                    If m_VirtualResourceList.Contains(Resource) Then
                        Dim Row As DataGridViewRow = GetRowFromResource(Resource)
                        m_RemovingRow = Row

                        'Must cancel any edits before removing rows.
                        If Not CancelEdit() Then
                            Debug.Fail("CancelEdit failed")
                        End If

                        Rows.Remove(Row)
                        m_VirtualResourceList.Remove(Resource)
                    End If
                Next
            Finally
                m_RemovingRow = Nothing
            End Try

            MyBase.Invalidate()
        End Sub

#End Region


#Region "Virtualization methods"

        ''' <summary>
        ''' Gets the number of Resource rows in this grid.  Does not include the "addnew" row at the bottom.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property RowCountVirtual() As Integer
            Get
                Return m_VirtualResourceList.Count
            End Get
        End Property

        ''' <summary>
        ''' Do not use.  Use RowCountVirtual instead.
        ''' 
        ''' If you really need the number of rows in the grid, including the add/new row at
        '''   the bottom, then use MyBase.RowCount.  But note that that includes the add/new row
        '''   and is therefore *not* a correct count of the resources in the grid
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Marked invisible so it's less likely to be accidentally used.</remarks>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shadows ReadOnly Property RowCount() As Integer
            Get
                Debug.Fail("Don't use this function - use RowCountVirtual instead - it doesn't include the add/new row at the bottom, just the actual entries")
                Return RowCountVirtual 'defensive
            End Get
        End Property


        ''' <summary>
        ''' Finds the row index of a given Resource.
        ''' </summary>
        ''' <param name="SearchResource">The Resource to find.</param>
        ''' <returns>The row index of that Resource, or -1 if not found.</returns>
        ''' <remarks></remarks>
        Public Function GetRowIndexFromResource(ByVal SearchResource As Resource) As Integer
            Debug.Assert(SearchResource IsNot Nothing)
            Dim IndexFound As Integer = m_VirtualResourceList.IndexOf(SearchResource)
            Return IndexFound
        End Function


        ''' <summary>
        ''' Finds the Row that contains a given Resource.
        ''' </summary>
        ''' <param name="SearchResource">The resource to find.</param>
        ''' <returns>The row that contains that resource, or Nothing if not found.</returns>
        ''' <remarks>This function unshares the row that it returns.</remarks>
        Public Function GetRowFromResource(ByVal SearchResource As Resource) As DataGridViewRow
            Dim FoundIndex As Integer = GetRowIndexFromResource(SearchResource)
            If FoundIndex >= 0 Then
                Return Me.Rows(FoundIndex)
            Else
                Return Nothing
            End If
        End Function


        ''' <summary>
        ''' Gets the Resource that is in the given row index.
        ''' </summary>
        ''' <param name="RowIndex">The index of the row to get the resource from.</param>
        ''' <param name="AllowUncommittedRow">If True, then RowIndex is allowed to be past the rows which actually contain a valid resource.  In this case, it is assumed that the uncommitted resource (the one created for the add/new row but not yet added to the ResourceFile) is intended, and is returned.</param>
        ''' <returns>The Resource for that row, or possibly the uncommitted Resource (if AllowUncommittedRow = True).  Returns Nothing if there's a problem (and asserts).</returns>
        ''' <remarks></remarks>
        Public Function GetResourceFromRowIndex(ByVal RowIndex As Integer, ByVal AllowUncommittedRow As Boolean) As Resource
            If RowIndex >= 0 AndAlso RowIndex < RowCountVirtual Then
                Return DirectCast(m_VirtualResourceList(RowIndex), Resource)
            Else
                If AllowUncommittedRow Then
                    Debug.Assert(m_UncommittedResource IsNot Nothing)
                    Return m_UncommittedResource
                End If

                Debug.Fail("GetResourceFromRowIndex: RowIndex out of bounds (AllowUncommittedRow=False)")
                Return Nothing 'Defensive
            End If
        End Function


        ''' <summary>
        ''' Retrieves the Resource associated with a particular grid row.
        ''' </summary>
        ''' <param name="Row"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetResourceFromRow(ByVal Row As DataGridViewRow) As Resource
            Debug.Assert(Not Row Is Nothing, "Row is Nothing")
            Debug.Assert(Row.Index >= 0 AndAlso Row.Index < Rows.Count, "Huh?")
            Return GetResourceFromRowIndex(Row.Index, True)
        End Function


        ''' <summary>
        ''' Called by the grid when its need data for a particular grid cell.
        ''' </summary>
        ''' <param name="e">Event args.</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCellValueNeeded(ByVal e As DataGridViewCellValueEventArgs)
            MyBase.OnCellValueNeeded(e)
            e.Value = GetCellStringValue(e.RowIndex, e.ColumnIndex)
        End Sub


        ''' <summary>
        '''  Get Cell String value from the original resource object
        ''' </summary>
        ''' <param name="RowIndex"></param>
        ''' <param name="ColumnIndex"></param>
        ''' <remarks></remarks>
        Private Function GetCellStringValue(ByVal RowIndex As Integer, ByVal ColumnIndex As Integer) As String
            Dim Resource As Resource = GetResourceFromRowIndex(RowIndex, True)
            If Resource Is Nothing Then
                Return String.Empty
            End If
            Return ResourceStringTable.GetResourceCellStringValue(Resource, ColumnIndex)
        End Function

        ''' <summary>
        '''  Get Cell String value from the original resource object
        ''' </summary>
        ''' <param name="Resource"></param>
        ''' <param name="ColumnIndex"></param>
        ''' <remarks></remarks>
        Friend Shared Function GetResourceCellStringValue(ByVal Resource As Resource, ByVal ColumnIndex As Integer) As String
            Dim Value As String = String.Empty

            'Return the requested data for this resource.
            Select Case ColumnIndex

                Case COLUMN_NAME
                    Value = Resource.Name
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValueNeeded: Name: " & Value)

                Case COLUMN_TYPE
                    Value = Resource.FriendlyValueTypeName
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValueNeeded: Type: " & Value)

                Case COLUMN_VALUE
                    'Get the string-formatted value.
                    Dim StringResourceEditor As ResourceTypeEditorStringBase = DirectCast(Resource.ResourceTypeEditor, ResourceTypeEditorStringBase)
                    Try
                        Value = StringResourceEditor.StringGetFormattedCellValue(Resource, Resource.GetValue())
                    Catch ex As Exception
                        Common.RethrowIfUnrecoverable(ex)
                    End Try
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValueNeeded: Value: " & Value)

                Case COLUMN_COMMENT
                    Value = Resource.Comment
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValueNeeded: Comment: " & Value)

                Case Else
                    Debug.Fail("Unexpected column in OnCellValueNeeded")
            End Select
            Return Value
        End Function


        ''' <summary>
        ''' Called by the grid when it needs the error text for a particular grid cell.
        ''' </summary>
        ''' <param name="e">Event args.</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCellErrorTextNeeded(ByVal e As DataGridViewCellErrorTextNeededEventArgs)
            MyBase.OnCellErrorTextNeeded(e)

            If e.ColumnIndex < 0 OrElse e.RowIndex < 0 OrElse e.RowIndex >= RowCountVirtual Then
                'Ignore
                Exit Sub
            End If

            Dim Resource As Resource = GetResourceFromRowIndex(e.RowIndex, True)
            If Resource Is Nothing Then
                Return 'defensive
            End If

            'Return the requested data for this resource.
            Select Case e.ColumnIndex

                Case COLUMN_NAME
                    e.ErrorText = ResourceFile.GetResourceTaskMessage(Resource, ResourceEditor.ResourceFile.ResourceTaskType.BadName)
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellErrorTextNeeded: Name: " & CStr(e.ErrorText))

                Case COLUMN_TYPE
                    e.ErrorText = ""

                Case COLUMN_VALUE
                    'Get the string-formatted value.
                    Dim StringResourceEditor As ResourceTypeEditorStringBase = DirectCast(Resource.ResourceTypeEditor, ResourceTypeEditorStringBase)
                    e.ErrorText = ResourceFile.GetResourceTaskMessage(Resource, ResourceEditor.ResourceFile.ResourceTaskType.CantInstantiateResource)
                    Try
                        Dim value As Object = StringResourceEditor.StringGetFormattedCellValue(Resource, Resource.GetValue())
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True)
                        If e.ErrorText = "" Then
                            'If there wasn't already an error message stored, use the exception we just got.
                            e.ErrorText = ex.Message
                        End If
                    End Try
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellErrorTextNeeded: Value: " & CStr(e.ErrorText))

                Case COLUMN_COMMENT
                    e.ErrorText = ResourceFile.GetResourceTaskMessage(Resource, ResourceEditor.ResourceFile.ResourceTaskType.CommentsNotSupportedInThisFile)
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellErrorTextNeeded: Comment: " & CStr(e.ErrorText))

                Case Else
                    Debug.Fail("Unexpected column in OnCellErrorTextNeeded")
            End Select
        End Sub


        ''' <summary>
        ''' Called by the grid when the user has made a change to data and that data
        '''   needs to be pushed to the virtualized store.
        ''' </summary>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCellValuePushed(ByVal e As DataGridViewCellValueEventArgs)
            MyBase.OnCellValuePushed(e)

            If m_RemovingRow Is Rows(e.RowIndex) Then
                'If we are removing cells, we want to ignore changes to those cells, because the validation
                '  logic can get bypassed, and hey, we're deleting them anyway.
                Exit Sub
            End If

            ' Make sure the file has been checked out...
            Try
                ParentView.RootDesigner.DesignerLoader.ManualCheckOut()
            Catch ex As Exception
                'Cancel and show an error messagebox (actually, DsMsgBox knows not to show anything if it's dealing
                'with a CheckoutCanceled exception; the user is already aware he canceled)
                ParentView.DsMsgBox(ex)
                Return
            End Try

            'Process any changes the user might have made to the value in this cell.

            '... First, find the resx resource associated with this row
            Dim Resource As Resource = GetResourceFromRowIndex(e.RowIndex, True)
            If Resource Is Nothing Then
                Debug.Fail("Resource was nothing")
                Exit Sub 'defensive
            End If

            Debug.Assert(Not Rows(e.RowIndex).Cells(e.ColumnIndex).ReadOnly AndAlso Not Rows(e.RowIndex).ReadOnly)

            Dim value As Object = e.Value

            If value Is Nothing OrElse TypeOf value Is System.DBNull Then
                value = String.Empty
            End If

            '... Which column was changed?
            Select Case e.ColumnIndex
                Case COLUMN_NAME
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValuePushed: Name: " & CStr(value))

                    Dim NewName As String = CStr(value)
                    Dim NewParsedName As String = "" 'May be different from NewName in that blanks are automatically trimmed

                    If Not Resource.ValidateName(CStr(value), Resource.Name, NewParsedName) Then
                        Debug.Fail("OnCellValuePushed(): Value in Name cell is not valid - this should have been caught in CellValidating")
                        Exit Sub 'defensive
                    End If

                    'Go ahead and commit the change to our resource file (this call will
                    '  invalidate the UI, causing it to get updated)
                    Resource.Name = NewParsedName

                Case COLUMN_VALUE
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValuePushed: Value: " & CStr(value))

                    Dim NewParsedValue As Object = Nothing
                    If Resource.ValidateValueAsString(CStr(value), NewParsedValue) Then
                        'Go ahead and commit the change to our resource file (this call will
                        '  invalidate the UI, causing it to get updated)
                        Resource.SetValue(NewParsedValue)
                    Else
                        Debug.Fail("ValidateCellValue failed in OnCellValuePushed() - why wasn't this caught in Validating event?")
                    End If

                Case COLUMN_COMMENT
                    Debug.WriteLineIf(Switches.RSEVirtualStringTable.TraceVerbose, "RSEVirtualStringTable: OnCellValuePushed: Comment: " & CStr(value))

                    'Go ahead and commit the change to our resource file (this call will
                    '  invalidate the UI, causing it to get updated)
                    Resource.Comment = CStr(value)

                Case Else
                    Debug.Fail("Unrecognized column")
            End Select
        End Sub


        ''' <summary>
        ''' Called by the grid when it needs to create a new row (i.e., the user has
        '''   typed data into the add/new row, causing a new row to be created).  We
        '''   use this to create a new Resource to hold the data for that row.  This
        '''   resource is known as the uncommitted resource (because it hasn't actually
        '''   been added to the ResourceFile yet, and won't be until some of its data
        '''   is committed).
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnNewRowNeeded(ByVal e As System.Windows.Forms.DataGridViewRowEventArgs)
            MyBase.OnNewRowNeeded(e)

            'If we don't set a MinimumHeight of at least 20, the error glyphs won't
            '  show up at all.
            'See CreateNewResourceRow().  Currently you can't create a row for OnNewRowNeeded, there's
            '  supposed to be a template row to be able to use for this later in m3, if needed.
            e.Row.MinimumHeight = Math.Max(DpiHelper.LogicalToDeviceUnitsY(RowMinimumHeight), Me.Font.Height + DpiHelper.LogicalToDeviceUnitsY(ROW_BORDER_HEIGHT))

            If ResourceFile Is Nothing Then
                Debug.Fail("No resource file")
                Exit Sub
            End If

            Dim UniqueName As String = ResourceFile.GetUniqueName(ResourceTypeEditors.String)
            Dim NewResource As Resource = New Resource(m_ResourceFile, UniqueName, Nothing, "", ParentView)
            m_UncommittedResource = NewResource
        End Sub


        ''' <summary>
        ''' Called by the grid when it needs to know if a row is dirty.
        '''
        ''' By overriding this method this way, we change the commit level from 
        '''  the default of row-based to cell-based.  Since we have a mature Undo
        '''  implementation, we want simple cell-based commit, or else things would
        '''  become more complicated/confusing.  If we wanted row-based commit,
        '''  we would need to remove this and handle RowEnter and CancelRowEdit.
        ''' </summary>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnRowDirtyStateNeeded(ByVal e As QuestionEventArgs)
            MyBase.OnRowDirtyStateNeeded(e)

            'Only return true if the current cell is dirty (gives us cell-based commit)
            e.Response = Me.IsCurrentCellDirty
        End Sub

#End Region


#Region "Add/new row implementation (allows user to add new rows at the bottom of the grid)"

        ''' <summary>
        ''' Commit the uncommitted row by adding it to the ResourceFile.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CommitTheUncommittedRow()
            If m_UncommittedResource IsNot Nothing Then
                Debug.Assert(Not ResourceFile.Contains(m_UncommittedResource), "The uncommitted resource is already in the resource list!")
                If ResourceFile.Contains(m_UncommittedResource.Name) Then
                    'The Name that was picked is no longer unique.  Need to patch it up now to a definitely unique value
                    m_UncommittedResource.Name = ResourceFile.GetUniqueName(ResourceTypeEditors.String)
                End If

                'Add the new resource
                ResourceFile.AddResource(m_UncommittedResource)
                Try
                    m_VirtualResourceList.Add(m_UncommittedResource)
                Catch ex As Exception
                    'We must keep these two lists in sync.  If the second failed, revert the first
                    ResourceFile.RemoveResource(m_UncommittedResource, False)
                    Throw
                End Try

                'This is the last "uncommitted" row that was just committed.  If the user presses
                '  ESC, this row will be deleted again by the grid, and we'll need to uncommit it
                '  by removing it from the ResourceFile again.
                m_LastCommittedResource = m_UncommittedResource

                'No more uncommitted resource.
                m_UncommittedResource = Nothing

                'Now that the resource has been committed, we can go ahead and select it
                '  into the properties window.
                ParentView.PropertyGridUpdate()
            End If
        End Sub


        ''' <summary>
        ''' This is called by the grid whenever the user adds a new row by typing in the
        '''   add/new row at the bottom of the grid.  We'll use this opportunity to commit
        '''   the uncommitted resource.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnUserAddedRow(ByVal e As System.Windows.Forms.DataGridViewRowEventArgs)
            MyBase.OnUserAddedRow(e)

            If Not AllowUserToAddRows Then
                Debug.Fail("")
                Exit Sub
            End If

            'The user has actually started typing, so we know they intend to keep this new row.
            '  So it's time to commit it.
            CommitTheUncommittedRow()
        End Sub


        ''' <summary>
        ''' Called by the grid when the user deletes a row from the grid, including when
        '''   they are typing in the add/new row and press ESC (which causes that row to
        '''   be deleted again if that's the only cell they had typed into).
        ''' In our case, we handle the Delete key manually, so we have AllowUserToDeleteRows=False,
        '''   so this only gets called in the case of deleting the uncommitted row by the
        '''   user pressing ESC.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnUserDeletingRow(ByVal e As System.Windows.Forms.DataGridViewRowCancelEventArgs)
            MyBase.OnUserDeletingRow(e)

            If Not AllowUserToAddRows Then
                Debug.Fail("This event shouldn't have fired if we can't add rows.")
                Exit Sub
            End If
            Debug.Assert(Not AllowUserToDeleteRows, "This routine wasn't written to support this - needs to be reworked")

            If e.Row.Index = MyBase.RowCount - 1 AndAlso m_VirtualResourceList.Count = MyBase.RowCount - 1 Then
                Debug.Assert(m_UncommittedResource Is Nothing, "m_UncommittedResource should have been nothing")
                Dim ResourceToUncommit As Resource = DirectCast(m_VirtualResourceList(m_VirtualResourceList.Count - 1), Resource)
                If ResourceToUncommit Is m_LastCommittedResource Then
                    'We've verified that we're deleting the newly-added row.  Remove it now.
                    m_VirtualResourceList.RemoveAt(m_VirtualResourceList.Count - 1)
                    ResourceFile.RemoveResource(ResourceToUncommit, False)
                    m_UncommittedResource = ResourceToUncommit

                    ParentView.PropertyGridUpdate()
                Else
                    Debug.Fail("Trying to uncommit the wrong row")
                End If
            Else
                Debug.Fail("Unexpected conditions in UserDeletingRow - not removing resource")
            End If
        End Sub


        ''' <summary>
        ''' Places the cursor on the bottom row of the string table, ready for the user to enter a new string.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub NewString()
            If AllowUserToAddRows Then
                UnselectAll()
                CurrentCell = Rows(MyBase.RowCount - 1).Cells(COLUMN_NAME)
                BeginEdit(True)
            Else
                Debug.Fail("NewString() shouldn't be called when you can't add new rows")
            End If
        End Sub

#End Region


#Region "Selecting and highlighting resources"

        ''' <summary>
        ''' Highlights (selects) the specified field for the specified resource and scrolls it into view.
        ''' </summary>
        ''' <param name="Resource">The Resource to highlight</param>
        ''' <param name="Field">The field in the resource's row to highlight.</param>
        ''' <remarks></remarks>
        Friend Sub HighlightResource(ByVal Resource As Resource, ByVal Field As FindReplace.Field)
            Dim Row As DataGridViewRow = GetRowFromResource(Resource)
            If Row IsNot Nothing Then
                Dim CellIndex As Integer
                Select Case Field
                    Case FindReplace.Field.Name
                        CellIndex = COLUMN_NAME
                    Case FindReplace.Field.Value
                        CellIndex = COLUMN_VALUE
                    Case FindReplace.Field.Comment
                        CellIndex = COLUMN_COMMENT
                    Case Else
                        Debug.Fail("Unexpected field in HighlightResource")
                        HighlightResources(New Resource() {Resource}) 'defensive
                End Select

                Row.Cells(CellIndex).Selected = True

                'Also cause the cell to scroll into view
                CurrentCell = Row.Cells(CellIndex)
            Else
                Debug.Fail("Couldn't find row")
            End If
        End Sub


        ''' <summary>
        ''' Highlights (selects) the specified resource and scrolls it into view.
        ''' </summary>
        ''' <param name="Resources">The Resources to highlight</param>
        ''' <remarks></remarks>
        Friend Sub HighlightResources(ByVal Resources As ICollection)
            Dim firstOne As Boolean = True
            For Each Resource As Resource In Resources
                Dim Row As DataGridViewRow = GetRowFromResource(Resource)
                If Row IsNot Nothing Then
                    If firstOne Then
                        'Also cause the cell to scroll into view
                        CurrentCell = Row.Cells(0)
                        firstOne = False
                    End If
                    Row.Selected = True
                Else
                    Debug.Fail("Couldn't find row")
                End If
            Next
        End Sub


        ''' <summary>
        ''' Unselect all resources in this grid.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub UnselectAll()
            For Each Row As DataGridViewRow In SelectedRows
                Row.Selected = False
            Next
            For Each Cell As DataGridViewCell In SelectedCells
                Cell.Selected = False
            Next
        End Sub


        ''' <summary>
        ''' Gets all resources currently selected in this grid.
        ''' </summary>
        ''' <returns>An array of selected Resources (returns an empty array instead of Nothing)</returns>
        ''' <remarks></remarks>
        Public Function GetSelectedResources() As Resource()
            Dim indexArray As Integer() = Nothing
            Dim rowCount As Integer = 0
            Dim needRemoveDup As Boolean = False

            If MyBase.SelectionMode = DataGridViewSelectionMode.FullRowSelect Then

                'Add all rows which are selected in their entirety
                Dim rows As DataGridViewSelectedRowCollection = MyBase.SelectedRows
                If rows.Count > 0 Then
                    ReDim indexArray(rows.Count - 1)
                    For Each Row As DataGridViewRow In rows
                        Debug.Assert(Row IsNot Nothing)
                        If Row.Index < RowCountVirtual Then
                            indexArray(rowCount) = Row.Index
                            rowCount = rowCount + 1
                        End If
                    Next
                End If
            Else
                Dim cells As DataGridViewSelectedCellCollection = MyBase.SelectedCells

                If cells.Count > 0 Then
                    ReDim indexArray(cells.Count - 1)
                    For Each cell As DataGridViewCell In cells
                        If cell.RowIndex < RowCountVirtual Then
                            indexArray(rowCount) = cell.RowIndex
                            rowCount = rowCount + 1
                        End If
                    Next
                End If
                needRemoveDup = True
            End If

            If rowCount > 0 Then
                If rowCount > 1 Then
                    Array.Sort(indexArray, 0, rowCount)

                    ' Remove dups...
                    If needRemoveDup Then
                        Dim i As Integer = 0
                        For j As Integer = 1 To rowCount - 1
                            If indexArray(i) <> indexArray(j) Then
                                i = i + 1
                                indexArray(i) = indexArray(j)
                            End If
                        Next
                        rowCount = i + 1
                    End If
                End If

                Dim SelectedResources(rowCount - 1) As Resource
                For k As Integer = 0 To rowCount - 1
                    SelectedResources(k) = GetResourceFromRowIndex(indexArray(k), False)
                Next

                Return SelectedResources
            End If

            'Never return Nothing
            Return New Resource() {}
        End Function


        ''' <summary>
        ''' Gets the find/replace field for the current cell.  Returns Name if there's no current cell or
        '''   if it isn't a column that has a FindReplace.Field match.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetCurrentCellFindField() As FindReplace.Field
            If CurrentCell IsNot Nothing Then
                Select Case CurrentCell.ColumnIndex
                    Case COLUMN_NAME
                        Return FindReplace.Field.Name
                    Case COLUMN_VALUE
                        Return FindReplace.Field.Value
                    Case COLUMN_COMMENT
                        Return FindReplace.Field.Comment
                End Select
            End If
            Return FindReplace.Field.Name
        End Function

        ''' <summary>
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If IsCurrentCellDirty Then
                ' If validation failed, we shouldn't leave the cell...
                ' We should put the focus back to the EditingControl
                If Me.EditingControl IsNot Nothing Then
                    Me.EditingControl.Select()
                End If
                Return
            End If

            ParentView.PropertyGridUpdate()
        End Sub

#End Region

#Region "Sorting"
        ''' <summary>
        ''' Called when the user clicks on column header
        '''   We need sort the whole list
        ''' </summary>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnColumnHeaderMouseClick(ByVal e As DataGridViewCellMouseEventArgs)
            MyBase.OnColumnHeaderMouseClick(e)

            If e.ColumnIndex <> m_Sorter.ColumnIndex Then
                SortOnColumn(e.ColumnIndex, False)
            Else
                SortOnColumn(e.ColumnIndex, Not m_Sorter.InReverseOrder)
            End If
        End Sub

        ''' <summary>
        '''  Restore Sorter -- used when we need restore view state
        ''' </summary>
        ''' <param name="originalSorter"></param>
        ''' <remarks></remarks>
        Friend Sub RestoreSorter(ByVal originalSorter As IComparer)
            Dim stringSorter As StringTableSorter = TryCast(originalSorter, StringTableSorter)
            If stringSorter IsNot Nothing Then
                SortOnColumn(stringSorter.ColumnIndex, stringSorter.InReverseOrder)
            Else
                Debug.Fail("We only support StringTableSorter")
            End If
        End Sub

        ''' <summary>
        '''  Reorder the whole list based on one column
        ''' </summary>
        ''' <param name="columnIndex"></param>
        ''' <param name="inReverseOrder"></param>
        ''' <remarks></remarks>
        Private Sub SortOnColumn(ByVal columnIndex As Integer, ByVal inReverseOrder As Boolean)
            Using (New WaitCursor)
                Dim currentCellColumnIndex As Integer = -1
                Dim currentResource As Resource = Nothing
                Dim selectedResources() As Resource

                ' we save the information about current selection, so we can restore it later
                selectedResources = GetSelectedResources()

                If IsCurrentCellDirty Then
                    Return
                End If

                If IsCurrentCellInEditMode Then
                    EndEdit(DataGridViewDataErrorContexts.CurrentCellChange)
                End If

                Dim cell As DataGridViewCell = CurrentCell
                If cell IsNot Nothing Then
                    currentResource = GetResourceFromRowIndex(cell.RowIndex, True)
                    currentCellColumnIndex = cell.ColumnIndex
                ElseIf selectedResources IsNot Nothing AndAlso selectedResources.Length > 0 Then
                    currentResource = selectedResources(0)
                    currentCellColumnIndex = 0
                End If

                ClearSelection()


                If columnIndex <> m_Sorter.ColumnIndex Then
                    Me.Columns(m_Sorter.ColumnIndex).HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None
                    m_Sorter.ColumnIndex = columnIndex
                End If

                m_Sorter.InReverseOrder = inReverseOrder

                If m_Sorter.InReverseOrder Then
                    Me.Columns(columnIndex).HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.Descending
                Else
                    Me.Columns(columnIndex).HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.Ascending
                End If

                ' Sort the virtual list...ReferenceList.Sort()
                m_VirtualResourceList.Sort(m_Sorter)

                Me.Refresh()

                ' Restore current position...
                If currentResource IsNot Nothing AndAlso currentCellColumnIndex >= 0 Then
                    Dim rowIndex As Integer = m_VirtualResourceList.IndexOf(currentResource)
                    If rowIndex >= 0 Then
                        CurrentCell = Rows(rowIndex).Cells(currentCellColumnIndex)
                    End If
                End If

                ' Restore selection
                If selectedResources IsNot Nothing AndAlso selectedResources.Length > 0 Then
                    HighlightResources(selectedResources)
                End If

                ParentView.RootDesigner.InvalidateFindLoop(False)
            End Using
        End Sub

        ''' <summary>
        ''' A helper class to sort the resource list in the Data Grid View
        ''' </summary>
        Private Class StringTableSorter
            Implements IComparer

            ' which column is used to sort the list
            Private m_columnIndex As Integer
            Private m_reverseOrder As Boolean

            Public Sub New(ByVal columnIndex As Integer, ByVal reverseOrder As Boolean)
                m_columnIndex = columnIndex
                m_reverseOrder = reverseOrder
            End Sub

            ''' <Summary>
            ''' which column is used to sort the list 
            ''' </Summary>
            Friend Property ColumnIndex() As Integer
                Get
                    Return m_columnIndex
                End Get
                Set(ByVal value As Integer)
                    m_columnIndex = value
                End Set
            End Property

            ''' <Summary>
            ''' whether it is in reverseOrder
            ''' </Summary>
            Friend Property InReverseOrder() As Boolean
                Get
                    Return m_reverseOrder
                End Get
                Set(ByVal value As Boolean)
                    m_reverseOrder = value
                End Set
            End Property

            ''' <Summary>
            '''  Compare two list items
            ''' </Summary>
            Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
                Dim ret As Integer = String.Compare(GetColumnValue(x, m_columnIndex), GetColumnValue(y, m_columnIndex), StringComparison.CurrentCultureIgnoreCase)
                If ret = 0 AndAlso m_columnIndex <> COLUMN_NAME Then
                    ret = String.Compare(GetColumnValue(x, COLUMN_NAME), GetColumnValue(y, COLUMN_NAME), StringComparison.CurrentCultureIgnoreCase)
                End If
                If m_reverseOrder Then
                    ret = -ret
                End If
                Return ret
            End Function

            ''' <Summary>
            '''  Get String Value of one column
            ''' </Summary>
            Private Function GetColumnValue(ByVal obj As Object, ByVal column As Integer) As String
                If TypeOf obj Is Resource Then
                    Dim value As String = Nothing
                    value = ResourceStringTable.GetResourceCellStringValue(DirectCast(obj, Resource), column)
                    If value IsNot Nothing Then
                        Return value
                    End If
                Else
                    Debug.Fail("DetailViewSorter: obj was not a Resource")
                End If
                Return String.Empty
            End Function


        End Class

#End Region

        ''' <summary>
        ''' Delete values from selected cells
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub ClearSelectedCells()
            Dim cells As DataGridViewSelectedCellCollection = MyBase.SelectedCells
            If cells.Count = 0 Then
                Return
            End If

            Dim failedCount As Integer = 0
            For Each cell As DataGridViewCell In cells
                Dim resource As Resource = GetResourceFromRowIndex(cell.RowIndex, True)
                Select Case cell.ColumnIndex
                    Case COLUMN_VALUE
                        Dim NewParsedValue As Object = Nothing
                        If resource.ValidateValueAsString(String.Empty, NewParsedValue) Then
                            resource.SetValue(NewParsedValue)
                        Else
                            failedCount = failedCount + 1
                        End If

                    Case COLUMN_COMMENT
                        resource.Comment = String.Empty
                End Select
            Next

            If failedCount > 0 Then
                If failedCount = cells.Count Then
                    ' throw an exception to abort the transaction...
                    Throw New InvalidOperationException(SR.GetString(SR.RSE_Err_CantBeEmpty))
                End If

                DsMsgBox(SR.GetString(SR.RSE_Err_CantBeEmpty), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

            Refresh()
        End Sub

        ''' <summary>
        '''  Try to paste a string to current cell
        ''' </summary>
        ''' <param name="simpleString"></param>
        Friend Sub PasteStringToCurrentCell(ByVal simpleString As String)
            simpleString = TrimPastedString(simpleString, True, True)

            Dim cell As DataGridViewCell = CurrentCell
            Debug.Assert(cell IsNot Nothing, "Why there is no current cell")
            Debug.Assert(Not cell.ReadOnly, "We shouldn't paste into a readonly cell")
            If cell IsNot Nothing AndAlso Not cell.ReadOnly Then
                Dim resource As Resource = GetResourceFromRowIndex(cell.RowIndex, True)
                Dim ex As Exception = Nothing
                Select Case cell.ColumnIndex
                    Case COLUMN_NAME
                        If resource.ValidateName(simpleString, resource.Name, , ex) Then
                            resource.Name = simpleString
                        End If
                    Case COLUMN_VALUE
                        Dim NewParsedValue As Object = Nothing
                        If resource.ValidateValueAsString(simpleString, NewParsedValue, ex) Then
                            resource.SetValue(NewParsedValue)
                        End If

                    Case COLUMN_COMMENT
                        resource.Comment = simpleString
                End Select

                If ex IsNot Nothing Then
                    ParentView.DsMsgBox(ex)
                End If
            End If
        End Sub

        ''' <summary>
        ''' a helper function to trim white space from the string pasted from the clipboard
        ''' </summary>
        Private Shared Function TrimPastedString(ByVal pasteString As String, ByVal trimHead As Boolean, ByVal trimTail As Boolean) As String
            If trimHead Then
                ' CONSIDER: should we keep the leading space in multiple line pasting?
                pasteString = pasteString.TrimStart()
            End If

            If trimTail Then
                pasteString = pasteString.TrimEnd()
            End If
            Return pasteString
        End Function

        ''' <summary>
        ''' Process windows message to handle special keys...
        ''' </summary>
        ''' <returns></returns>
        ''' <param name="m"></param>
        ''' <remarks></remarks>
        Public Overrides Function PreProcessMessage(ByRef m As Message) As Boolean
            If m.Msg = Interop.win.WM_KEYDOWN AndAlso CInt(m.WParam) = Keys.F2 Then
                Return MyBase.ProcessF2Key(Keys.F2 Or ModifierKeys)
            End If
            Return MyBase.PreProcessMessage(m)
        End Function

        ''' <summary>
        ''' ResourceStringTextBoxCell
        '''  We override the EditType property to replace DataGridViewTextBoxEditingControl with ResourceStringTextBoxEditingControl
        ''' </summary>
        Friend Class ResourceStringTextBoxCell
            Inherits DesignerFramework.DesignerDataGridView.EditOnClickDataGridViewTextBoxCell

            ''' <summary>
            ''' EditType returns the type of editor to edit one cell in the grid
            '''  We override it to replace the default DataGridViewTextBoxEditingControl with ResourceStringTextBoxEditingControl
            '''  so we can handle some events to handle CheckOut correctly...
            ''' </summary>
            Public Overrides ReadOnly Property EditType() As Type
                Get
                    Return GetType(ResourceStringTextBoxEditingControl)
                End Get
            End Property
        End Class

        ''' <summary>
        '''  customized TextBox in the ResourceStringTable. The TextBox is used to edit cell value.
        '''  However, it fires serveral OnDirty events when the customer types one character in the cell. We depend on this event to check out the file.
        '''  We don't want to prompt check out multiple times in a single event. But we don't know whether those OnDirty is caused by single action.
        '''  We overrides the EditingControl to see whether all those events are caused by one window message.
        ''' </summary>
        Friend Class ResourceStringTextBoxEditingControl
            Inherits DataGridViewTextBoxEditingControl

            ''' <summary>
            ''' Check whether the edit control should process the key
            ''' </summary>
            ''' <param name="keyData"></param>
            ''' <param name="dataGridViewWantsInputKey"></param>
            ''' <return></return>
            ''' <remarks></remarks>
            Public Overrides Function EditingControlWantsInputKey(ByVal keyData As Keys, ByVal dataGridViewWantsInputKey As Boolean) As Boolean
                ' The following code is added to fix devdiv bug 874
                ' we want to let the multiline editbox to handle up/down key, when the current position is not the first/last line in the control
                '  The default logic of this editbox doesn't handle wrapped text correctly.
                Dim firstCharIndexInCurrentLine As Integer = GetFirstCharIndexOfCurrentLine ()
                Select Case (keyData And Keys.KeyCode)
                    Case Keys.Down
                        Dim currentLineNumber As Integer = GetLineFromCharIndex(firstCharIndexInCurrentLine)
                        ' GetFirstCharIndexFromLine will return -1 when the linenumber exceeds the number of lines in the control
                        Return (GetFirstCharIndexFromLine(currentLineNumber + 1) > 0)
                    Case Keys.Up
                        Return (firstCharIndexInCurrentLine > 0)
                End Select

                Return MyBase.EditingControlWantsInputKey(keyData, dataGridViewWantsInputKey)
            End Function

            ''' <summary>
            ''' </summary>
            ''' <param name="m"></param>
            ''' <remarks></remarks>
            <SecurityPermission(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
            Protected Overrides Sub WndProc(ByRef m As Message)
                ' CONSIDER: should we take care of other events?
                Select Case m.Msg
                    Case Interop.win.WM_CHAR
                        Dim StringTable As ResourceStringTable = TryCast(EditingControlDataGridView, ResourceStringTable)
                        If StringTable IsNot Nothing Then
                            StringTable.StartOneAction()
                            Try
                                MyBase.WndProc(m)
                            Finally
                                StringTable.EndOneAction()
                            End Try
                            Return
                        End If

                    Case Interop.win.WM_PASTE
                        Dim dataObject As IDataObject = Clipboard.GetDataObject()
                        If dataObject IsNot Nothing AndAlso dataObject.GetDataPresent(GetType(String)) Then
                            Dim pasteString As String = CStr(dataObject.GetData(GetType(String)))
                            If pasteString <> "" Then
                                ' We want to trim the white space out. However, when the user is pasting in a middle of a exisiting string, we don't want to trim the space out.
                                pasteString = TrimPastedString(pasteString, Me.SelectionStart = 0, Me.SelectionStart + Me.SelectionLength >= Me.Text.Length)
                                MyBase.Paste(pasteString)
                            End If
                            Return
                        End If

                End Select

                MyBase.WndProc(m)
            End Sub
        End Class

    End Class

End Namespace


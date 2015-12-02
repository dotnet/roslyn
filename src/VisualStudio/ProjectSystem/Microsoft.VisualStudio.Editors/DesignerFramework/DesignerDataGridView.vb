'******************************************************************************
'* DesignerDataGridView.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Strict On
Option Explicit On

Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Math
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.PlatformUI

Namespace Microsoft.VisualStudio.Editors.DesignerFramework


    ''' <summary>
    ''' In case we're building an editor, the editor's view will contain some user controls built from FX.
    '''   These user controls handles context menu in a different way. To show the context menu the correct way,
    '''   we inherit from the FX's control and override their WndProc.
    '''   
    '''   DesignerDataGridView is our control inherited from DataGridView.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class DesignerDataGridView
        Inherits System.Windows.Forms.DataGridView



        ' ContextMenuShow will be raised when this list view needs to show its context menu.
        ' The derived control simply needs to handle this event to know when to show a
        '   context menu
        Public Event ContextMenuShow(ByVal sender As Object, ByVal e As MouseEventArgs)

        ' Give clients a chance to prevent the "edit on click" caused by some custom cell types
        Public Event CellClickBeginEdit(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)

        'Backing property fields
        Private m_DFAutoSizeColumnWidths As Boolean
        Private m_ColumnMinimumScrollingWidths() As Integer

        'Current percentage of the total width of the control that each column takes up, as a decimal
        '  between 0 and 1.0
        Private m_ColumnWidthPercentages() As Double

        'True iff we are changing a column's width programmatically
        Private m_ColumnWidthChangingProgrammatically As Boolean

        'True iff the last time the control resized, we weren't able to contract all of the column
        '  widths to the proper values because we hit at least one column's minimum scrolling width.
        Private m_CurrentGridSizeTooSmall As Boolean

        ' In the multiple selection mode, we shouldn't enter editMode automatically
        Private m_InMultiSelectionMode As Boolean

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            Me.BackColor = SystemColors.Window
            Me.ForeColor = SystemColors.WindowText
            ' Make sure the row headers have enough space to display the glyphs in HDPI
            Me.RowHeadersWidth = DpiHelper.LogicalToDeviceUnitsX(MyBase.RowHeadersWidth)
        End Sub

        ''' <summary>
        ''' Return whether the DataGridView is in MultiSelection Mode...
        ''' </summary>
        Friend ReadOnly Property InMultiSelectionMode() As Boolean
            Get
                Return m_InMultiSelectionMode
            End Get
        End Property

        ''' <summary>
        ''' Indicate that we are about to begin edit due to a click on a cell.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Friend Overridable Sub OnCellClickBeginEdit(ByVal e As System.ComponentModel.CancelEventArgs)
            RaiseEvent CellClickBeginEdit(Me, e)
        End Sub


        ''' <summary>
        ''' We override Control.WndProc to raise the ContextMenuShow event.
        ''' </summary>
        ''' <param name="m">Windows message passed in by window.</param>
        ''' <remarks>Implementation based on sources\ndp\fx\src\WinForms\Managed\System\WinForms\Control.cs</remarks>
        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            ' We only handle the context menu specially.
            Select Case m.Msg
                Case Interop.win.WM_CONTEXTMENU
                    Debug.WriteLineIf(Switches.DFContextMenu.TraceVerbose, "WM_CONTEXTMENU")
                    Dim EventArgs As MouseEventArgs = DesignUtil.GetContextMenuMouseEventArgs(Me, m)
                    RaiseEvent ContextMenuShow(Me, EventArgs)
                Case Else
                    MyBase.WndProc(m)
            End Select
        End Sub

        ''' <summary>
        ''' We want to catch right mouse down so we can change the currently selected cell
        ''' before showing the context menu... 
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnMouseDown(ByVal e As System.Windows.Forms.MouseEventArgs)
            Dim ht As HitTestInfo = Me.HitTest(e.X, e.Y)

            If (Control.ModifierKeys And (Keys.Control Or Keys.Shift)) <> 0 Then
                m_InMultiSelectionMode = True
            End If

            Try

                ' If we are right-clicking on a cell that isn't selected, then we should
                ' clear the previous selection and select the new cell... (a'la Excel)
                If e.Button = Windows.Forms.MouseButtons.Right Then
                    Try
                        If ht.Type = DataGridViewHitTestType.Cell Then
                            ' Select new cell 
                            Dim clickedCell As DataGridViewCell = Me.Rows(ht.RowIndex).Cells(ht.ColumnIndex)
                            If Not clickedCell.Selected Then
                                Me.CurrentCell = clickedCell
                            End If
                        ElseIf ht.Type = DataGridViewHitTestType.RowHeader Then
                            If IsCurrentCellInEditMode _
                                OrElse (Not m_InMultiSelectionMode AndAlso Not Me.Rows(ht.RowIndex).Selected) _
                            Then
                                ' Clear the current cell so we make sure that we have validated it...
                                Me.CurrentCell = Nothing
                                ClearSelection()
                            End If
                            Me.Rows(ht.RowIndex).Selected = True
                        Else
                            ' We didn't click on any cell - let's select no cell. This makes sure that we commit
                            ' the current cell before we show the context menu
                            Me.CurrentCell = Nothing
                        End If
                    Catch ex As InvalidOperationException
                        ' We don't really care if we failed to set the current cell... If we didn't
                        ' then it was probably a validation failure and we shouldn't show the context 
                        ' menu!
                    End Try
                End If

                MyBase.OnMouseDown(e)

                If IsCurrentCellInEditMode Then
                    Try
                        If e.Button = Windows.Forms.MouseButtons.Left Then
                            If ht.Type = DataGridViewHitTestType.None Then
                                ' Clear the current cell so we make sure that we have validated it...
                                Me.CurrentCell = Nothing
                            ElseIf ht.Type = DataGridViewHitTestType.RowHeader Then
                                If ht.RowIndex = Me.CurrentCell.RowIndex Then
                                    Me.CurrentCell = Nothing
                                    Me.Rows(ht.RowIndex).Selected = True
                                ElseIf Not IsCurrentCellDirty Then
                                    EndEdit(DataGridViewDataErrorContexts.CurrentCellChange)
                                End If
                            End If
                        End If
                    Catch ex As InvalidOperationException
                        ' We don't really care if we failed to set the current cell... If we didn't
                        ' then it was probably a validation failure and we shouldn't show the context 
                        ' menu!
                    End Try
                End If

                If Not IsCurrentCellInEditMode Then
                    If ht.Type = DataGridViewHitTestType.None Then
                        If Not m_InMultiSelectionMode Then
                            ClearSelection()
                        End If
                    End If
                End If

            Finally
                m_InMultiSelectionMode = False
            End Try
        End Sub


        ''' <summary>
        ''' We want to filter out Ctrl+0 for "our" datagridviews
        ''' (they normally do a clear cell, which is bad for comboboxcolumns...)
        ''' </summary>
        ''' <param name="m"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ProcessKeyMessage(ByRef m As System.Windows.Forms.Message) As Boolean
            Dim ke As New KeyEventArgs(CType(CInt(m.WParam) Or ModifierKeys, Keys))

            If _
                ke.KeyCode = Keys.D0 _
                AndAlso ke.Control _
                AndAlso CurrentCell IsNot Nothing _
                AndAlso GetType(DataGridViewComboBoxCell).IsAssignableFrom(CurrentCell.GetType()) _
            Then
                Return True
            Else
                Return MyBase.ProcessKeyMessage(m)
            End If
        End Function



        ''' <summary>
        ''' We want to filter out Ctrl+0 for "our" datagridviews
        ''' (they normally do a clear cell, which is bad for comboboxcolumns...)
        ''' </summary>
        ''' <param name="keyData"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <System.Security.Permissions.UIPermission(System.Security.Permissions.SecurityAction.LinkDemand, Window:=System.Security.Permissions.UIPermissionWindow.AllWindows)> _
        Protected Overrides Function ProcessDialogKey(ByVal keyData As Keys) As Boolean
            Const CtrlD0 As Keys = Keys.D0 Or Keys.Control

            Dim key As Keys = (keyData And Keys.KeyCode)

            If (keyData And CtrlD0) = CtrlD0 Then
                Return False
            Else
                Return MyBase.ProcessDialogKey(keyData)
            End If
        End Function

#Region "Hybrid edit mode cell templates"

        ''' <summary>
        ''' Extremely simple cell template that enables a hybrid of EditCellOnF2OrKeyDown and EditOnEnter mode
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class EditOnClickDataGridViewComboBoxCell
            Inherits DataGridViewComboBoxCell

            Protected Overrides Sub OnEnter(ByVal rowIndex As Integer, ByVal throughMouseClick As Boolean)
                Dim dfxdgv As DesignerDataGridView = TryCast(Me.DataGridView, DesignerDataGridView)
                Dim e As New System.ComponentModel.CancelEventArgs(False)

                If dfxdgv IsNot Nothing Then
                    If dfxdgv.InMultiSelectionMode Then
                        MyBase.OnEnter(rowIndex, throughMouseClick)
                        Return
                    End If
                    dfxdgv.OnCellClickBeginEdit(e)
                End If

                If e.Cancel Then
                    ' The datagridview wanted us to use the "normal" select-on-click behavior                    
                    MyBase.OnEnter(rowIndex, throughMouseClick)
                Else
                    ' The default implementation sets a flag that indicates that the next mouse click should be 
                    ' ignored!
                    '
                    ' We don't do anything here, which means that it will get into edit mode on the next
                    ' mouse click :)
                End If
            End Sub

            Public Overrides Property [ReadOnly]() As Boolean
                Get
                    Return MyBase.ReadOnly
                End Get
                Set(ByVal value As Boolean)
                    MyBase.ReadOnly = value
                    If value Then
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                    Else
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    End If
                End Set
            End Property
        End Class

        ''' <summary>
        ''' Extremely simple cell template that enables a hybrid of EditCellOnF2OrKeyDown and EditOnEnter mode
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class EditOnClickDataGridViewTextBoxCell
            Inherits DataGridViewTextBoxCell

            Protected Overrides Sub OnEnter(ByVal rowIndex As Integer, ByVal throughMouseClick As Boolean)
                Dim dfxdgv As DesignerDataGridView = TryCast(Me.DataGridView, DesignerDataGridView)
                Dim e As New System.ComponentModel.CancelEventArgs(False)

                If dfxdgv IsNot Nothing Then
                    If dfxdgv.InMultiSelectionMode Then
                        MyBase.OnEnter(rowIndex, throughMouseClick)
                        Return
                    End If
                    dfxdgv.OnCellClickBeginEdit(e)
                End If

                If e.Cancel Then
                    ' The datagridview wanted us to use the "normal" select-on-click behavior                    
                    MyBase.OnEnter(rowIndex, throughMouseClick)
                Else
                    ' The default implementation sets a flag that indicates that the next mouse click should be 
                    ' ignored!
                    '
                    ' We don't do anything here, which means that it will get into edit mode on the next
                    ' mouse click :)
                End If
            End Sub
        End Class

#End Region
    End Class

End Namespace

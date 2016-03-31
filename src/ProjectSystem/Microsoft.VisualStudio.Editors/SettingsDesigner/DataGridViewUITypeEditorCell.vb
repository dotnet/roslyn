Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Design
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Cell type for displaying UI Type editors in DataGridView 
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DataGridViewUITypeEditorCell
        Inherits DataGridViewCell

        Private m_ServiceProvider As IServiceProvider

        ''' <summary>
        ''' For edit on keydown or mouse click we sometimes need to
        ''' ignore a mouse click...
        ''' </summary>
        ''' <remarks></remarks>
        Private m_ignoreNextMouseClick As Boolean

#Region "DataGridViewCell overrides"

        ''' <summary>
        ''' Return the custom editing control used by this type
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property EditType() As Type
            Get
                Return GetType(DataGridViewUITypeEditorEditingControl)
            End Get
        End Property

        ''' <summary>
        ''' Retrun a formatted representation of the value
        ''' Consider: figure out how to make serialization localized in this case...
        ''' </summary>
        ''' <param name="value"></param>
        ''' <param name="rowIndex"></param>
        ''' <param name="cellStyle"></param>
        ''' <param name="context"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function GetFormattedValue(ByVal value As Object, ByVal rowIndex As Integer, ByRef cellStyle As System.Windows.Forms.DataGridViewCellStyle, ByVal valueTypeConverter As TypeConverter, ByVal formattedValueTypeConverter As TypeConverter, ByVal context As System.Windows.Forms.DataGridViewDataErrorContexts) As Object
            If (context And DataGridViewDataErrorContexts.Display) <> 0 AndAlso _
                value IsNot Nothing AndAlso _
                value.GetType().Equals(GetType(Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString)) Then
                'Begin
                Return DirectCast(value, Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString).ConnectionString
            Else
                Dim serializer As New SettingsValueSerializer()
                Return serializer.Serialize(value, Threading.Thread.CurrentThread.CurrentCulture)
            End If
        End Function

        ''' <summary>
        ''' Type of formatted value
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property FormattedValueType() As Type
            Get
                Return GetType(String)
            End Get
        End Property

        ''' <summary>
        ''' Parse (deserialize) formatted value into instance of the correct type
        ''' </summary>
        ''' <param name="FormattedValue"></param>
        ''' <param name="CellStyle"></param>
        ''' <param name="valueTypeConverter"></param>
        ''' <param name="formattedTypeConverter"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function ParseFormattedValue(ByVal FormattedValue As Object, _
                                                 ByVal CellStyle As DataGridViewCellStyle, _
                                                 ByVal valueTypeConverter As TypeConverter, _
                                                 ByVal formattedTypeConverter As TypeConverter) As Object
            If Not TypeOf FormattedValue Is String Then
                Debug.Fail("Unknown formatted value type!")
                Throw Common.CreateArgumentException("FormattedValue")
            End If
            Dim StrFormattedValue As String = DirectCast(FormattedValue, String)
            Dim parsedValue As Object = Nothing
            Dim sgv As SettingsDesignerView.SettingsGridView = TryCast(Me.DataGridView, SettingsDesignerView.SettingsGridView)
            Dim oldCommittingChanges As Boolean
            If sgv IsNot Nothing Then
                ' Deserializing the setting may pop UI, which in turn may cause an active designer change (if hosted in the
                ' app designer). We better indicate that we are committing our changes...
                oldCommittingChanges = sgv.CommittingChanges
                sgv.CommittingChanges = True
            End If
            Try
                Dim serializer As New SettingsValueSerializer()
                parsedValue = serializer.Deserialize(ValueType, StrFormattedValue, Threading.Thread.CurrentThread.CurrentCulture)
            Finally
                If sgv IsNot Nothing Then
                    sgv.CommittingChanges = oldCommittingChanges
                End If
            End Try
            If parsedValue Is Nothing AndAlso StrFormattedValue <> "" Then
                Throw New FormatException()
            End If
            Return parsedvalue
        End Function

        ''' <summary>
        ''' Calculate preferred size
        ''' </summary>
        ''' <param name="g"></param>
        ''' <param name="cellStyle"></param>
        ''' <param name="rowIndex"></param>
        ''' <param name="constraintSize"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function GetPreferredSize(ByVal g As System.Drawing.Graphics, ByVal cellStyle As System.Windows.Forms.DataGridViewCellStyle, ByVal rowIndex As Integer, ByVal constraintSize As System.Drawing.Size) As System.Drawing.Size
            Dim FormattedValue As String = DirectCast(GetFormattedValue(GetValue(rowIndex), rowIndex, cellStyle, Nothing, Nothing, DataGridViewDataErrorContexts.Formatting Or DataGridViewDataErrorContexts.Display), String)
            Dim preferredStringSize As SizeF = g.MeasureString(FormattedValue, cellStyle.Font)
            Return New System.Drawing.Size(CInt(preferredStringSize.Width + 40), CInt(preferredStringSize.Height))
        End Function


        ''' <summary>
        ''' Paint the value column
        ''' </summary>
        ''' <param name="graphics"></param>
        ''' <param name="clipBounds"></param>
        ''' <param name="cellBounds"></param>
        ''' <param name="rowIndex"></param>
        ''' <param name="cellState"></param>
        ''' <param name="value"></param>
        ''' <param name="formattedValue"></param>
        ''' <param name="errorText"></param>
        ''' <param name="cellStyle"></param>
        ''' <param name="advancedBorderStyle"></param>
        ''' <param name="paintParts"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Paint(ByVal graphics As System.Drawing.Graphics, ByVal clipBounds As System.Drawing.Rectangle, ByVal cellBounds As System.Drawing.Rectangle, ByVal rowIndex As Integer, ByVal cellState As System.Windows.Forms.DataGridViewElementStates, ByVal value As Object, ByVal formattedValue As Object, ByVal errorText As String, ByVal cellStyle As System.Windows.Forms.DataGridViewCellStyle, ByVal advancedBorderStyle As System.Windows.Forms.DataGridViewAdvancedBorderStyle, ByVal paintParts As System.Windows.Forms.DataGridViewPaintParts)
            MyBase.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts)


            Dim DrawForeColor As Color
            Dim DrawBackColor As Color
            If (cellState And DataGridViewElementStates.Selected) = DataGridViewElementStates.Selected Then
                DrawForeColor = cellStyle.SelectionForeColor
                DrawBackColor = cellStyle.SelectionBackColor
            Else
                DrawForeColor = cellStyle.ForeColor
                DrawBackColor = cellStyle.BackColor
            End If

            ' Clear the background...
            Using Brush As SolidBrush = New SolidBrush(DrawBackColor)
                graphics.FillRectangle(Brush, cellBounds)
            End Using
            PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle)

            Dim StringBounds As Rectangle

            ' If we support preview (paint) value, paint away!
            If UITypeEditor IsNot Nothing AndAlso UITypeEditor.GetPaintValueSupported Then
                Using ForegroundPen As New Pen(DrawForeColor)
                    Const BorderMargin As Integer = 1
                    Dim DrawRect As New Rectangle(cellBounds.X + BorderMargin, cellBounds.Y + BorderMargin, cellBounds.Height - 4 * BorderMargin, cellBounds.Height - 4 * BorderMargin)
                    If value IsNot Nothing Then
                        UITypeEditor.PaintValue(value, graphics, DrawRect)
                    End If
                    graphics.DrawRectangle(ForegroundPen, DrawRect)
                    ForegroundPen.Dispose()
                    StringBounds = New Rectangle(cellBounds.X + DrawRect.Width + BorderMargin, cellBounds.Y, cellBounds.Width - DrawRect.Width, cellBounds.Height)
                End Using
            Else
                StringBounds = cellBounds
            End If


            ' Draw the formatted value
            If formattedValue IsNot Nothing Then
                Dim sf As New StringFormat
                sf.LineAlignment = StringAlignment.Center
                Using ForeColorBrush As New SolidBrush(DrawForeColor)
                    graphics.DrawString(formattedValue.ToString(), cellStyle.Font, ForeColorBrush, StringBounds, sf)
                End Using
                ' Consider: consider using a "(null)" value string....
            End If

            Dim IsCurrentCell As Boolean = DataGridView.CurrentCellAddress.X = Me.ColumnIndex AndAlso DataGridView.CurrentCellAddress.Y = Me.RowIndex
            If IsCurrentCell Then
                ControlPaint.DrawFocusRectangle(graphics, cellBounds, DrawForeColor, DrawBackColor)
            End If
        End Sub

        ''' <summary>
        ''' Begin editing when someone clicks on this cell
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks>Not consistent with all edit modes in the DataGridView</remarks>
        Protected Overrides Sub OnMouseClick(ByVal e As DataGridViewCellMouseEventArgs)
            If DataGridView Is Nothing Then
                Return
            End If

            ' Clicking in the currently selected cell should start edit...
            Dim ptCurrentCell As Point = DataGridView.CurrentCellAddress
            If ptCurrentCell.X = e.ColumnIndex AndAlso ptCurrentCell.Y = e.RowIndex AndAlso e.Button = MouseButtons.Left Then
                If m_ignoreNextMouseClick Then
                    m_ignoreNextMouseClick = False
                ElseIf (Control.ModifierKeys And (Keys.Control Or Keys.Shift)) = 0 Then
                    Me.DataGridView.BeginEdit(True)
                End If
            End If
        End Sub

        Public Overrides Function KeyEntersEditMode(ByVal e As System.Windows.Forms.KeyEventArgs) As Boolean
            ' This code was copied from the DataGridViewTextBoxCell 
            If (((Char.IsLetterOrDigit(Microsoft.VisualBasic.ChrW(e.KeyCode)) AndAlso Not (e.KeyCode >= Keys.F1 AndAlso e.KeyCode <= Keys.F24)) OrElse _
                 (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.Divide) OrElse _
                 (e.KeyCode >= Keys.OemSemicolon AndAlso e.KeyCode <= Keys.Oem102) OrElse _
                 (e.KeyCode = Keys.Space AndAlso Not e.Shift)) AndAlso _
                Not e.Alt AndAlso _
                Not e.Control) _
            Then
                Return True
            End If
            Return MyBase.KeyEntersEditMode(e)
        End Function

        ''' <summary>
        ''' Give the editing control whatever info it needs to do its job!
        ''' </summary>
        ''' <param name="InitialFormattedValue"></param>
        ''' <param name="CellStyle"></param>
        ''' <remarks></remarks>
        Public Overrides Sub InitializeEditingControl(ByVal RowIndex As Integer, ByVal InitialFormattedValue As Object, ByVal CellStyle As DataGridViewCellStyle)
            Debug.Assert(DataGridView IsNot Nothing AndAlso DataGridView.EditingControl IsNot Nothing AndAlso TypeOf DataGridView.EditingControl Is DataGridViewUITypeEditorEditingControl)
            MyBase.InitializeEditingControl(RowIndex, InitialFormattedValue, CellStyle)
            Dim Ctrl As DataGridViewUITypeEditorEditingControl = DirectCast(DataGridView.EditingControl, DataGridViewUITypeEditorEditingControl)
            Ctrl.ServiceProvider = Me.ServiceProvider
            Ctrl.ValueType = Me.ValueType
            Ctrl.Value = Me.Value
            Ctrl.Font = CellStyle.Font
        End Sub

        ''' <summary>
        ''' Query the datagridview if it is OK to start editing?
        ''' </summary>
        ''' <param name="rowIndex"></param>
        ''' <param name="throughMouseClick"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnEnter(ByVal rowIndex As Integer, ByVal throughMouseClick As Boolean)
            MyBase.OnEnter(rowIndex, throughMouseClick)
            If throughMouseClick Then
                Dim dfxdgv As DesignerFramework.DesignerDataGridView = TryCast(Me.DataGridView, DesignerFramework.DesignerDataGridView)
                Dim ec As New System.ComponentModel.CancelEventArgs(False)

                If dfxdgv IsNot Nothing Then
                    If dfxdgv.InMultiSelectionMode Then
                        Return
                    End If
                    dfxdgv.OnCellClickBeginEdit(ec)
                End If

                ' If the datagridview isn't ready to be edited, we want to ignore the next mouse click...
                m_ignoreNextMouseClick = ec.Cancel
            End If
        End Sub

        ''' <summary>
        ''' Whenever the focus leaves this cell, we have to reset the ignore next mouse click...
        ''' </summary>
        ''' <param name="rowIndex"></param>
        ''' <param name="throughMouseClick"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnLeave(ByVal rowIndex As Integer, ByVal throughMouseClick As Boolean)
            MyBase.OnLeave(rowIndex, throughMouseClick)
            m_ignoreNextMouseClick = False
        End Sub

#End Region

        Friend Property ServiceProvider() As IServiceProvider
            Get
                Return m_ServiceProvider
            End Get
            Set(ByVal Value As IServiceProvider)
                m_ServiceProvider = Value
            End Set
        End Property


        Private ReadOnly Property UITypeEditor() As UITypeEditor
            Get
                If Me.ValueType IsNot Nothing Then
                    Return DirectCast(TypeDescriptor.GetEditor(ValueType, GetType(UITypeEditor)), UITypeEditor)
                End If
                Return Nothing
            End Get
        End Property

    End Class

End Namespace

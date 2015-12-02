'------------------------------------------------------------------------------
' <copyright from='2003' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'

Imports Microsoft.VisualStudio.Editors.Common
Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.ComponentModel
Imports System.Drawing.Design
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Control to host UI type editor. Set the value and value type of the value to edit
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class TypeEditorHostControl
        Inherits System.Windows.Forms.UserControl
        Implements IWindowsFormsEditorService, IServiceProvider

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer. 
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call 
            Me.PreviewPanel.BackColor = ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_WINDOW, System.Drawing.SystemColors.Window, UseVSTheme:=False)
            Me.BackColor = ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_WINDOW, System.Drawing.SystemColors.Window, UseVSTheme:=False)
            m_editControls = New Control() {ValueTextBox, ValueComboBox}
            Me.EditControl = ValueTextBox
        End Sub

        'UserControl1 overrides dispose to clean up the component list. 
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer 
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer 
        'It can be modified using the Windows Form Designer. 
        'Do not modify it using the code editor. 
        Private WithEvents ValueTextBox As System.Windows.Forms.TextBox
        Private WithEvents ShowEditorButton As ComboBoxDotDotDotButton
        Private WithEvents ValueComboBox As System.Windows.Forms.ComboBox
        Private WithEvents PreviewPanel As System.Windows.Forms.Panel
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(TypeEditorHostControl))
            Me.ValueTextBox = New TypeEditorHostControlTextBox
            Me.ShowEditorButton = New ComboBoxDotDotDotButton
            Me.PreviewPanel = New System.Windows.Forms.Panel
            Me.ValueComboBox = New System.Windows.Forms.ComboBox
            Me.SuspendLayout()
            '
            'ValueTextBox
            '
            resources.ApplyResources(Me.ValueTextBox, "ValueTextBox")
            Me.ValueTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None
            Me.ValueTextBox.Name = "ValueTextBox"
            '
            'ShowEditorButton
            '
            resources.ApplyResources(Me.ShowEditorButton, "ShowEditorButton")
            Me.ShowEditorButton.BackColor = Common.ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_THREEDFACE,
                                                                        System.Drawing.SystemColors.ButtonFace, UseVSTheme:=False)
            Me.ShowEditorButton.Name = "ShowEditorButton"
            Me.ShowEditorButton.UseVisualStyleBackColor = False
            '
            'PreviewPanel
            '
            resources.ApplyResources(Me.PreviewPanel, "PreviewPanel")
            Me.PreviewPanel.Name = "PreviewPanel"
            '
            'ValueComboBox
            '
            Me.ValueComboBox.FormattingEnabled = True
            resources.ApplyResources(Me.ValueComboBox, "ValueComboBox")
            Me.ValueComboBox.Name = "ValueComboBox"
            '
            'TypeEditorHostControl
            '
            Me.Controls.Add(Me.ValueComboBox)
            Me.Controls.Add(Me.PreviewPanel)
            Me.Controls.Add(Me.ShowEditorButton)
            Me.Controls.Add(Me.ValueTextBox)
            Me.Name = "TypeEditorHostControl"
            resources.ApplyResources(Me, "$this")
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

#Region "Private fields"
        Private m_TypeEditor As UITypeEditor
        Private m_TypeConverter As TypeConverter
        Private m_InnerValue As Object
        Private m_InnerValueType As Type

        ''' <summary>
        ''' Flag indicating that the text in the editing control is changed, and
        ''' that we need to re-parse it if the user wants to get the deserialized
        ''' value
        ''' </summary>
        ''' <remarks></remarks>
        Private m_textValueDirty As Boolean

        ' Indicating if we are currently showing a UI type editor
        Private m_IsShowingUITypeEditor As Boolean

        Private m_currentEditControl As Control
        Private m_editControls() As Control

        ' Holder window for drop-downs...
        Private m_Dialog As DropDownHolder

        ' Flag to avoid fireing value change notifications when we programatically set the
        ' text...
        Private m_ignoreTextChangeEvents As Boolean

#End Region

#Region "Value & value type properties for value to edit"

        Public Property ValueType() As Type
            Get
                Return m_InnerValueType
            End Get
            Set(ByVal Value As Type)
                m_InnerValueType = Value

                ' Let's try and get a UITypeEditor for this type! 
                m_TypeEditor = GetSpecificEditorForType(Value)
                If m_TypeEditor Is Nothing Then
                    If Value Is GetType(String) Then
                        ' We'll use the multiline string editor for strings...
                        m_TypeEditor = New System.ComponentModel.Design.MultilineStringEditor()
                    Else
                        m_TypeEditor = CType(TypeDescriptor.GetEditor(Value, GetType(UITypeEditor)), UITypeEditor)
                    End If
                End If

                ' Cache a type converter 
                m_TypeConverter = TypeDescriptor.GetConverter(Value)

                ' If we have a type editor, let's see if it supports preview of 
                ' the value... 
                Dim PreviewSupported As Boolean = False
                If Not m_TypeEditor Is Nothing Then
                    PreviewSupported = m_TypeEditor.GetPaintValueSupported()
                End If
                PreviewPanel.Visible = PreviewSupported

                ' We should show the "button" if (and only if) we have a valid 
                ' UITypeEditor!
                Dim ShowEditorButtonVisible As Boolean = False
                If Not m_TypeEditor Is Nothing Then
                    Select Case m_TypeEditor.GetEditStyle()
                        Case UITypeEditorEditStyle.DropDown
                            ShowEditorButton.PaintStyle = ComboBoxDotDotDotButton.PaintStyles.DropDown
                            ShowEditorButtonVisible = True
                        Case UITypeEditorEditStyle.Modal
                            ShowEditorButton.PaintStyle = ComboBoxDotDotDotButton.PaintStyles.DotDotDot
                            ShowEditorButtonVisible = True
                    End Select
                End If
                ShowEditorButton.Visible = ShowEditorButtonVisible

                ' If we are showing something that supports GetStandardValues, but doesn't have a UITypeEditor, we'll 
                ' show a nice combobox instead of that boring edit box!
                If (Not ShowEditorButtonVisible) _
                    AndAlso m_TypeConverter IsNot Nothing _
                    AndAlso m_TypeConverter.GetStandardValuesSupported() _
                    AndAlso m_TypeConverter.GetStandardValues().Count() > 0 _
                Then
                    EditControl = ValueComboBox
                    ValueComboBox.Items.Clear()
                    For Each stdValue As Object In m_TypeConverter.GetStandardValues()
                        ValueComboBox.Items.Add(stdValue)
                    Next
                Else
                    EditControl = ValueTextBox
                End If

                ' If the preview panel is visible, we've gotta make sure we draw the
                ' new value on it! 
                If PreviewPanel.Visible Then
                    PreviewPanel.Invalidate()
                End If
            End Set
        End Property

        Public Property Value() As Object
            Get
                If TextValueDirty Then
                    m_InnerValue = ParseValue(EditControl.Text, ValueType)
                    TextValueDirty = False
                End If
                Return m_InnerValue
            End Get
            Set(ByVal Value As Object)
                If Value IsNot Nothing Then
                    ValueType = Value.GetType()
                End If
                m_InnerValue = Value
                Text = FormatValue(Value)
                ' If the preview panel is visible, we've gotta make sure we draw the
                ' new value on it! 
                If PreviewPanel.Visible Then
                    PreviewPanel.Invalidate()
                End If
            End Set
        End Property

        Protected ReadOnly Property InnerValue() As Object
            Get
                Return m_InnerValue
            End Get
        End Property

        Protected Overridable Function GetSpecificEditorForType(ByVal KnownType As Type) As UITypeEditor
            Return Nothing
        End Function

        Protected Overridable Function FormatValue(ByVal ValueToFormat As Object) As String
            Return m_TypeConverter.ConvertToString(ValueToFormat)
        End Function

        Protected Overridable Function ParseValue(ByVal SerializedValue As String, ByVal ValueType As Type) As Object
            Return m_TypeConverter.ConvertFromString(SerializedValue)
        End Function

#End Region

#Region "Paint & layout of control"
        ''' <summary>
        ''' Paint the preview panel
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub PreviewPanel_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles PreviewPanel.Paint
            If Not m_TypeEditor Is Nothing Then
                If m_TypeEditor.GetPaintValueSupported Then
                    Using ForegroundPen As New Pen(Me.ForeColor)
                        Dim DrawRect As New Rectangle(1, 1, PreviewPanel.ClientRectangle.Width - 4, PreviewPanel.ClientRectangle.Height - 4)
                        If Value IsNot Nothing Then
                            m_TypeEditor.PaintValue(Value, e.Graphics, DrawRect)
                        End If
                        e.Graphics.DrawRectangle(ForegroundPen, DrawRect)
                    End Using
                End If
            End If
        End Sub

        ''' <summary>
        ''' Layout contained controls
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnLayout(ByVal e As LayoutEventArgs)
            MyBase.OnLayout(e)

            ' Left position of text box - will be bumped by preview panel if showing
            Dim TextBoxLeft As Integer = 0

            ' Width of text box - will be changed if preview panel and/or browse button
            ' is showing. Initially assume the text box is the only control showing
            Dim TextBoxWidth As Integer = Me.Width

            ' All controls have the same height!
            PreviewPanel.Height = Me.Height
            EditControl.Height = Me.Height

            ShowEditorButton.Height = System.Windows.Forms.SystemInformation.VerticalScrollBarThumbHeight + 2 * ShowEditorButton.FlatAppearance.BorderSize
            ShowEditorButton.Width = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 2 * ShowEditorButton.FlatAppearance.BorderSize

            ' Let's make the preview panel nice and square...
            PreviewPanel.Width = PreviewPanel.Height

            ' If the preview panel is showing, bump the text box right
            ' and decrease it's width
            If PreviewPanel.Visible Then
                TextBoxWidth -= PreviewPanel.Width
                TextBoxLeft += PreviewPanel.Width
            End If

            ' If we show the browse button, decrease the text box width
            If ShowEditorButton.Visible Then
                TextBoxWidth -= ShowEditorButton.Width
            End If

            ' Position controls
            Me.EditControl.Left = TextBoxLeft
            Me.EditControl.Width = TextBoxWidth

            ShowEditorButton.Top = 0
            Me.ShowEditorButton.Left = EditControl.Left + EditControl.Width
        End Sub

#End Region

#Region "Misc. control event handlers"
        ''' <summary>
        ''' Use the UI Type editor to edit the current value
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ShowEditorButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ShowEditorButton.Click
            Debug.Assert(Not m_TypeEditor Is Nothing)
            ShowUITypeEditor()
        End Sub

        ''' <summary>
        ''' Display the associated type editor if not already showing
        ''' </summary>
        ''' <remarks></remarks>
        <System.Security.SecurityCritical()> _
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()> _
        Private Sub ShowUITypeEditor()
            If m_TypeEditor IsNot Nothing Then
                If m_IsShowingUITypeEditor Then
                    Return
                End If

                m_IsShowingUITypeEditor = True
                Try
                    ' If this is a type that implements IList, we try to create a new instance before 
                    ' passing it to the UITypeEditor. Not doing so will show the UITypeEditor, everything will
                    ' look fine, but when closing the UITypeEditor, it will still return nothing :(
                    Dim passedNewInstanceToEditor As Boolean = False
                    Dim existingValue As Object = Value
                    If Value Is Nothing AndAlso GetType(Collections.IList).IsAssignableFrom(ValueType) Then
                        Try
                            existingValue = System.Activator.CreateInstance(ValueType)
                            passedNewInstanceToEditor = True
                        Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                        End Try
                    End If
                    Dim editedValue As Object = m_TypeEditor.EditValue(Context, Me, existingValue)

                    ' If we created a new instance to pass to the UITypeEditor, and the user didn't add any 
                    ' items, then we set the value back to nothing...
                    If passedNewInstanceToEditor Then
                        Dim valueAsIList As Collections.IList = TryCast(editedValue, Collections.IList)
                        If valueAsIList IsNot Nothing AndAlso valueAsIList.Count = 0 Then
                            editedValue = Nothing
                        End If
                    End If
                    Value = editedValue
                    OnValueChanged()
                    Switches.TracePDFocus(TraceLevel.Warning, "SettingsDesignerView.TypeEditorHostControl.ShowUITypeEditor.Me.Focus()")
                    Me.Focus()
                Catch Ex As Exception When Not TypeOf Ex Is Threading.ThreadAbortException _
                    AndAlso Not TypeOf Ex Is AccessViolationException _
                    AndAlso Not TypeOf Ex Is StackOverflowException

                    Dim sp As IServiceProvider = Microsoft.VisualStudio.Editors.VBPackage.Instance
                    Microsoft.VisualStudio.Editors.DesignerFramework.DesignerMessageBox.Show( _
                                       sp, _
                                       "", _
                                       Ex, _
                                       Microsoft.VisualStudio.Editors.DesignerFramework.DesignUtil.GetDefaultCaption(sp))
                Finally
                    m_IsShowingUITypeEditor = False
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Whenever the value in the textbox changes we may want to reset our value
        ''' PERF: will this cause perf problems?
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub TextChangedHandler(ByVal sender As Object, ByVal e As EventArgs) Handles ValueTextBox.TextChanged, ValueComboBox.TextChanged
            If Not m_ignoreTextChangeEvents Then
                TextValueDirty = True
                OnValueChanged()
            End If
        End Sub

        ''' <summary>
        ''' Alt-Down should show the ui type editor if we have a drop-down type editor associated with 
        ''' the current type...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub KeyDownHandler(ByVal sender As Object, ByVal e As KeyEventArgs) Handles ValueTextBox.KeyDown
            If m_TypeEditor IsNot Nothing Then
                If m_TypeEditor.GetEditStyle() = UITypeEditorEditStyle.DropDown Then
                    If e.Alt AndAlso (e.KeyCode And Keys.KeyCode) = Keys.Down Then
                        If Not m_IsShowingUITypeEditor Then
                            ShowUITypeEditor()
                            e.Handled = True
                            Return
                        End If
                    End If
                ElseIf m_TypeEditor.GetEditStyle() = UITypeEditorEditStyle.Modal Then
                    If (e.KeyCode And Keys.KeyCode) = Keys.Enter Then
                        If Not m_IsShowingUITypeEditor Then
                            ShowUITypeEditor()
                            e.Handled = True
                            Return
                        End If
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Grab the enter key if we have the focus on the uitypeeditor button
        ''' and the current UI Type editor is a modal editor...
        ''' </summary>
        ''' <param name="keyData"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function IsInputKey(ByVal keyData As System.Windows.Forms.Keys) As Boolean
            If keyData = Keys.Enter Then
                If ShowEditorButton.Focused AndAlso m_TypeEditor IsNot Nothing AndAlso m_TypeEditor.GetEditStyle() = UITypeEditorEditStyle.Modal Then
                    Return True
                End If
            End If
            Return MyBase.IsInputKey(keyData)
        End Function

#End Region

#Region "IWindowsFormsEditorService"

        Public Sub CloseDropDown() Implements System.Windows.Forms.Design.IWindowsFormsEditorService.CloseDropDown
            Debug.Assert(m_Dialog.Controls.Count = 1)
            m_Dialog.Hide()
        End Sub

        Private Sub DropDownHolderSizeChanged(ByVal sender As Object, ByVal e As EventArgs)
            DropDownHolderSize(TryCast(sender, System.Windows.Forms.Control))
        End Sub

        Private Sub DropDownHolderSize(ByVal control As System.Windows.Forms.Control)
            If m_Dialog IsNot Nothing AndAlso control IsNot Nothing Then

                ' Calculate size & position
                Dim currentScreen As System.Windows.Forms.Screen = System.Windows.Forms.Screen.FromControl(Me)

                ' Get preferred size & position of control...
                Dim dialogSize As Size = New Size(control.PreferredSize.Width + 2, control.PreferredSize.Height + 2)
                Dim UpperLeft As Point = PointToScreen(New Point(Me.Width - dialogSize.Width, Me.ShowEditorButton.Height))

                ' If the dialog gets clipped at the bottom of the screen, let's try to reposition it above the
                ' edit control...
                If UpperLeft.Y + dialogSize.Height > currentScreen.WorkingArea.Bottom Then
                    UpperLeft.Y = UpperLeft.Y - Me.Height - dialogSize.Height
                End If

                ' If the dialog gets clipped at the right of the screen, let's try to move it left...
                If UpperLeft.X + dialogSize.Width > currentScreen.WorkingArea.Right Then
                    UpperLeft.X = currentScreen.WorkingArea.Right - dialogSize.Width
                End If

                ' If, after all this moving, we are above/to the left of the screen, let's move
                ' it right/down again
                UpperLeft.X = Math.Max(UpperLeft.X, currentScreen.WorkingArea.Left)
                UpperLeft.Y = Math.Max(UpperLeft.Y, currentScreen.WorkingArea.Top)

                ' If the dialog wants to be larger than the screen, shrink it!
                dialogSize.Height = Math.Min(currentScreen.WorkingArea.Height, dialogSize.Height)
                dialogSize.Width = Math.Min(currentScreen.WorkingArea.Width, dialogSize.Width)

                m_Dialog.Size = dialogSize
                m_Dialog.Left = UpperLeft.X
                m_Dialog.Top = UpperLeft.Y
            End If
        End Sub

        Public Sub DropDownControl(ByVal control As System.Windows.Forms.Control) Implements System.Windows.Forms.Design.IWindowsFormsEditorService.DropDownControl
            If m_Dialog Is Nothing Then
                m_Dialog = New DropDownHolder
            End If

            ' Let's make sure we don't have any child controls in this guy! 
            m_Dialog.Controls.Clear()
            DropDownHolderSize(control)
            AddHandler control.SizeChanged, AddressOf DropDownHolderSizeChanged
            m_Dialog.Editor = control
            m_Dialog.TopLevel = True
            m_Dialog.Owner = Me.ParentForm
            m_Dialog.ShowInTaskbar = False
            m_Dialog.Show()
            m_Dialog.Activate()
            While m_Dialog.Visible
                Application.DoEvents()
                Microsoft.VisualStudio.Editors.Interop.NativeMethods.MsgWaitForMultipleObjects(0, IntPtr.Zero, True, 250, Microsoft.VisualStudio.Editors.Interop.win.QS_ALLINPUT)
            End While
            RemoveHandler control.SizeChanged, AddressOf DropDownHolderSizeChanged
        End Sub

        Public Function ShowDialog(ByVal dialog As System.Windows.Forms.Form) As System.Windows.Forms.DialogResult Implements System.Windows.Forms.Design.IWindowsFormsEditorService.ShowDialog
            Dim UiSvc As IUIService = DirectCast(GetService(GetType(IUIService)), IUIService)
            If Not UiSvc Is Nothing Then
                Return UiSvc.ShowDialog(dialog)
            Else
                Return dialog.ShowDialog(ShowEditorButton)
            End If
        End Function

#End Region

#Region "Private helper properties"
        ''' <summary>
        ''' Get access to my current type converter
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Can return NULL if no type converter is available!</remarks>
        Private ReadOnly Property TypeConverter() As TypeConverter
            Get
                Return m_TypeConverter
            End Get
        End Property

#End Region
        ''' <summary>
        ''' Are we currently showing the UI type editor?
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsShowingUITypeEditor() As Boolean
            Get
                Return m_IsShowingUITypeEditor
            End Get
        End Property

        Protected Overridable Sub OnValueChanged()
        End Sub

#Region "IServiceProvider implementation"
        Public Function IServiceProvider_GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
            If serviceType.Equals(GetType(IWindowsFormsEditorService)) Then
                Return Me
            Else
                Return GetService(serviceType)
            End If
        End Function
#End Region


        ''' <summary>
        ''' Host the UI type editor control given to use. 
        ''' </summary>
        ''' <remarks></remarks>
        Private Class DropDownHolder
            Inherits Form

            Public Sub New()
                ' We don't want this guy showing up in the task bar...
                Me.ShowInTaskbar = False
            End Sub

            ''' <summary>
            ''' Override default create parameters for window
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Protected Overrides ReadOnly Property CreateParams() As CreateParams
                Get
                    Dim BaseParams As CreateParams = MyBase.CreateParams

                    Dim Params As New CreateParams
                    Params.ClassStyle = 0
                    Params.Style = Constants.WS_VISIBLE Or Constants.WS_POPUP Or Constants.WS_BORDER
                    Params.ExStyle = Constants.WS_EX_TOPMOST Or Constants.WS_EX_TOOLWINDOW
                    Params.Height = Me.Height
                    Params.Width = Me.Width
                    Params.X = Me.Left
                    Params.Y = Me.Top
                    Return Params
                End Get
            End Property

            ''' <summary>
            ''' Hide the form
            ''' </summary>
            ''' <remarks>Additional common "cleanup" code before closing the window goes here</remarks>
            Protected Sub HideForm()
                Me.Hide()
            End Sub

            ''' <summary>
            ''' 
            ''' </summary>
            ''' <param name="e"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnDeactivate(ByVal e As System.EventArgs)
                Me.HideForm()
            End Sub

            ''' <summary>
            ''' Get/set the UI type editor that I'm hosting...
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public Property Editor() As Control
                Get
                    If Me.Controls.Count = 1 Then
                        Return Me.Controls.Item(0)
                    Else
                        Return Nothing
                    End If
                End Get
                Set(ByVal Value As Control)
                    Controls.Clear()
                    If Not Value Is Nothing Then
                        Value.Dock = DockStyle.Fill
                        Controls.Add(Value)
                    End If
                End Set
            End Property

            ''' <summary>
            ''' Pressing the escape key should close the window
            ''' </summary>
            ''' <param name="m"></param>
            ''' <remarks></remarks>
            Protected Overrides Function ProcessKeyPreview(ByRef m As System.Windows.Forms.Message) As Boolean
                If m.Msg = Microsoft.VisualStudio.Editors.Interop.NativeMethods.WM_KEYDOWN Then
                    If CType(m.WParam.ToInt32(), Keys) = Keys.Escape Then
                        Me.HideForm()
                        Return True
                    End If
                End If
                Return MyBase.ProcessKeyEventArgs(m)
            End Function
        End Class

        ''' <summary>
        ''' Whenever the selected index changes in the ValueComboBox, we update the value!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ValueComboBox_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ValueComboBox.SelectedIndexChanged
            m_InnerValue = ValueComboBox.SelectedItem
            TextValueDirty = False
            OnValueChanged()
        End Sub

        ''' <summary>
        ''' Is the text in the currently selected edit control dirty?
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Property TextValueDirty() As Boolean
            Get
                Return m_textValueDirty
            End Get
            Set(ByVal value As Boolean)
                m_textValueDirty = value
            End Set
        End Property

        ''' <summary>
        ''' Map the SelectAll procedure to the currently selected edit control
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SelectAll()
            If TypeOf EditControl Is ComboBox Then
                DirectCast(EditControl, ComboBox).SelectAll()
            Else
                Debug.Assert(TypeOf EditControl Is TextBox, "Unkown edit type!? " & EditControl.GetType().FullName)
                DirectCast(EditControl, TextBox).SelectAll()
            End If
        End Sub

        ''' <summary>
        ''' Map the selection length property to the currently selected edit control
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property SelectionLength() As Integer
            Get
                If ValueComboBox.Visible Then
                    Return ValueComboBox.SelectionLength
                Else
                    Debug.Assert(ValueTextBox.Visible, "No edit control visible!?")
                    Return ValueTextBox.SelectionLength
                End If
            End Get
            Set(ByVal value As Integer)
                If ValueComboBox.Visible Then
                    ValueComboBox.SelectionLength = value
                Else
                    Debug.Assert(ValueTextBox.Visible, "No edit control visible!?")
                    ValueTextBox.SelectionLength = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Map the selection start property to the currently selected edit control
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property SelectionStart() As Integer
            Get
                If ValueComboBox.Visible Then
                    Return ValueComboBox.SelectionStart
                Else
                    Debug.Assert(ValueTextBox.Visible, "No edit control visible!?")
                    Return ValueTextBox.SelectionStart
                End If
            End Get
            Set(ByVal value As Integer)
                If ValueComboBox.Visible Then
                    ValueComboBox.SelectionStart = value
                Else
                    Debug.Assert(ValueTextBox.Visible, "No edit control visible!?")
                    ValueTextBox.SelectionStart = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Map the selected text to the currently selected edit control
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property SelectedText() As String
            Get
                If ValueComboBox.Visible Then
                    Return ValueComboBox.SelectedText
                Else
                    Debug.Assert(ValueTextBox.Visible, "No edit control visible!?")
                    Return ValueTextBox.SelectedText
                End If
            End Get
            Set(ByVal value As String)
                If ValueComboBox.Visible Then
                    ValueComboBox.SelectedText = value
                Else
                    Debug.Assert(ValueTextBox.Visible, "No edit control visible!?")
                    ValueTextBox.SelectedText = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Get the text in the edit control
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides Property Text() As String
            Get
                Return EditControl.Text
            End Get
            Set(ByVal value As String)
                Dim savedIgnoreTextChangeEvents As Boolean = m_ignoreTextChangeEvents
                Try
                    m_ignoreTextChangeEvents = True
                    EditControl.Text = value
                Finally
                    m_ignoreTextChangeEvents = savedIgnoreTextChangeEvents
                End Try
            End Set
        End Property

        ''' <summary>
        ''' Get the currently active edit control (textbox or combobox)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend Property EditControl() As Control
            Get
                If m_currentEditControl IsNot Nothing Then
                    Return m_currentEditControl
                Else
                    Return ValueTextBox
                End If
            End Get
            Set(ByVal value As Control)
                For Each ctrl As Control In m_editControls
                    If ctrl Is value Then
                        ctrl.Visible = True
                    Else
                        ctrl.Visible = False
                    End If
                Next
                m_currentEditControl = value
            End Set
        End Property

        ''' <summary>
        ''' Get an instance of a ITypeDescriptorContext to pass into the UITypeEditor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overridable ReadOnly Property Context() As System.ComponentModel.ITypeDescriptorContext
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' We want to special-handle a couple of keyboard messages from the textbox...
        ''' </summary>
        ''' <remarks></remarks>
        Private Class TypeEditorHostControlTextBox
            Inherits TextBox

            ''' <summary>
            ''' This code was mainly ripped from DataGridViewTextBoxEditingControl...
            ''' </summary>
            ''' <param name="m"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            <System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags:=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)> _
            Protected Overrides Function ProcessKeyEventArgs(ByRef m As Message) As Boolean
                Select Case CType(CInt(m.WParam), Keys)
                    Case Keys.Enter
                        ' REGISB: Check if WM_IME_CHAR needs to be treated the same.
                        If m.Msg = Interop.NativeMethods.WM_CHAR AndAlso Not ModifierKeys = Keys.Shift Then
                            ' Ignore the Enter key and don't add it to the textbox content. This happens when failing validation brings
                            ' up a dialog box for example.
                            ' Shift-Enter for multiline textboxes need to be accepted however.
                            Return True
                        End If
                    Case Keys.LineFeed
                        ' REGISB: Check if WM_IME_CHAR needs to be treated the same.
                        If m.Msg = Interop.NativeMethods.WM_CHAR AndAlso ModifierKeys = Keys.Control Then
                            ' Ignore linefeed character when user hits Ctrl-Enter to commit the cell.
                            Return True
                        End If
                    Case Keys.A
                        If (m.Msg = Interop.NativeMethods.WM_KEYDOWN AndAlso ModifierKeys = Keys.Control) Then
                            SelectAll()
                            Return True
                        End If
                End Select
                Return MyBase.ProcessKeyEventArgs(m)
            End Function
        End Class

        ''' <summary>
        ''' Custom button that alternates between ... and combobox drop down
        ''' </summary>
        ''' <remarks></remarks>
        Private Class ComboBoxDotDotDotButton
            Inherits Button

            ' Valid paint styles
            Public Enum PaintStyles
                DotDotDot = 0
                DropDown = 1
            End Enum

            Private Const DotDotDotString As String = "..."

            ' Current style to draw
            Private _paintStyle As PaintStyles = PaintStyles.DotDotDot

            ' Hot or not
            Private _drawHot As Boolean

            ''' <summary>
            ''' Do we want to look like a browse button or like a 
            ''' combobox dropdown?
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public Property PaintStyle() As PaintStyles
                Get
                    Return _paintStyle
                End Get
                Set(ByVal value As PaintStyles)
                    Select Case value
                        Case PaintStyles.DotDotDot, PaintStyles.DropDown
                            ' Everything is cool
                        Case Else
                            Throw Common.CreateArgumentException("value")
                    End Select
                    _paintStyle = value
                    Invalidate()
                End Set
            End Property

            ''' <summary>
            ''' Keep track on when the mouse is over us
            ''' </summary>
            ''' <param name="e"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnMouseEnter(ByVal e As System.EventArgs)
                _drawHot = True
                Invalidate()
                MyBase.OnMouseEnter(e)
            End Sub

            ''' <summary>
            ''' Keep track on when the mouse is over us
            ''' </summary>
            ''' <param name="e"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnMouseLeave(ByVal e As System.EventArgs)
                _drawHot = False
                Invalidate()
                MyBase.OnMouseLeave(e)
            End Sub

            ''' <summary>
            ''' Custom paint ... or combobox drop down...
            ''' </summary>
            ''' <param name="pevent"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnPaint(ByVal pevent As System.Windows.Forms.PaintEventArgs)
                MyBase.OnPaint(pevent)
                Select Case PaintStyle
                    Case PaintStyles.DotDotDot
                        Dim drawRect As Rectangle = Me.ClientRectangle
                        drawRect.Offset(Me.FlatAppearance.BorderSize, 0)
                        TextRenderer.DrawText(pevent.Graphics, DotDotDotString, Me.Font, drawRect, Me.ForeColor)
                    Case PaintStyles.DropDown
                        If ComboBoxRenderer.IsSupported Then
                            Dim drawstyle As VisualStyles.ComboBoxState
                            If _drawHot Then
                                drawstyle = VisualStyles.ComboBoxState.Hot
                            Else
                                drawstyle = VisualStyles.ComboBoxState.Normal
                            End If
                            ComboBoxRenderer.DrawDropDownButton(pevent.Graphics, Me.ClientRectangle, drawstyle)
                        Else
                            ControlPaint.DrawComboButton(pevent.Graphics, Me.ClientRectangle, ButtonState.Normal)
                        End If
                End Select
            End Sub
        End Class

    End Class
End Namespace

Imports System.Drawing.Design
Imports System.Windows.Forms
Imports Microsoft.VSDesigner.VSDesignerPackage

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Editing control for UI type editor in DataGridView
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DataGridViewUITypeEditorEditingControl
        Inherits TypeEditorHostControl
        Implements IDataGridViewEditingControl

        Private m_DataGridView As DataGridView
        Private m_RowIndex As Integer
        Private m_valueChanged As Boolean

#Region "DataGridView event handlers"

        ''' <summary>
        ''' Make sure we forward any KeyPress events to our ValueTextBox
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' When using the EnterOnKeyPress edit mode on the DataGridView, the DataGridView will get the
        ''' keypress event first, and forward it to the editing control if it should enter edit mode. The problem 
        ''' is that it sends it directly to the editing control, and not to the editing controls focused child 
        ''' control where we want it to end up.... 
        '''</remarks>
        Protected Overrides Sub OnKeyPress(ByVal e As KeyPressEventArgs)
            MyBase.OnKeyPress(e)

            SelectedText = e.KeyChar
            SelectionStart += 1
            SelectionLength = 0
        End Sub

        ''' <summary>
        ''' Don't allow the designer to shift focus to another cell if I'm currently showing a UI Type editor
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' Because of the way we are showing the UI Type editor drop-down, there may be cases where we could end up leaving
        ''' the cell before the UI Type editor control has had the chance to push its value to the current cell
        '''</remarks>
        Private Sub CellValidatingHandler(ByVal sender As Object, ByVal e As DataGridViewCellValidatingEventArgs)
            If IsShowingUITypeEditor Then
                e.Cancel = True
            End If
        End Sub

        ''' <summary>
        ''' Stop listening for DataGridView events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisconnectDataGridViewEventHandlers()
            If DataGridView IsNot Nothing Then
                RemoveHandler DataGridView.CellValidating, AddressOf Me.CellValidatingHandler
            End If
        End Sub

        ''' <summary>
        ''' Start listening for DataGridView events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectDataGridViewEventHandlers()
            If DataGridView IsNot Nothing Then
                AddHandler DataGridView.CellValidating, AddressOf Me.CellValidatingHandler
            End If
        End Sub
#End Region


#Region "IDataGridViewEditingControl implementation"
        ''' <summary>
        ''' Set the datagridview instance I'm showing in
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property DataGridView() As System.Windows.Forms.DataGridView Implements System.Windows.Forms.IDataGridViewEditingControl.EditingControlDataGridView
            Get
                Return m_DataGridView
            End Get
            Set(ByVal Value As System.Windows.Forms.DataGridView)
                DisconnectDataGridViewEventHandlers()
                m_DataGridView = Value
                ConnectDataGridViewEventHandlers()
            End Set
        End Property

        ''' <summary>
        ''' Set the formatted representation of my current value
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Will set my value to the deserialized value - nothing if deserialization fails</remarks>
        Public Property FormattedValue() As Object Implements System.Windows.Forms.IDataGridViewEditingControl.EditingControlFormattedValue
            Get
                Return GetFormattedValue(System.Windows.Forms.DataGridViewDataErrorContexts.Formatting)
            End Get
            Set(ByVal Value As Object)
                Debug.Assert(TypeOf Value Is String, String.Format("Why did someone try to set my formatted value to an object of type {0}? Expected type string!", Value.GetType().ToString()))
                Text = DirectCast(Value, String)
            End Set
        End Property

        Public ReadOnly Property IDataGridViewEditingPanel_Cursor() As System.Windows.Forms.Cursor Implements System.Windows.Forms.IDataGridViewEditingControl.EditingPanelCursor
            Get
                If m_DataGridView IsNot Nothing Then
                    Return m_DataGridView.Cursor
                Else
                    Return System.Windows.Forms.Cursor.Current
                End If
            End Get
        End Property

        Public Function GetFormattedValue(ByVal context As System.Windows.Forms.DataGridViewDataErrorContexts) As Object Implements System.Windows.Forms.IDataGridViewEditingControl.GetEditingControlFormattedValue
            If (context And DataGridViewDataErrorContexts.Parsing) <> 0 AndAlso Me.ValueType.Equals(GetType(SerializableConnectionString)) Then
                Dim scs As SerializableConnectionString = TryCast(Me.Value, SerializableConnectionString)
                If scs Is Nothing Then
                    scs = New SerializableConnectionString
                End If
                scs.ConnectionString = Text
                Dim serializer As New SettingsValueSerializer()
                Return serializer.Serialize(scs, Threading.Thread.CurrentThread.CurrentCulture)
            Else
                Return Text
            End If
        End Function

        Public ReadOnly Property RepositionOnValueChange() As Boolean Implements System.Windows.Forms.IDataGridViewEditingControl.RepositionEditingControlOnValueChange
            Get
                Return False
            End Get
        End Property

        Public Property RowIndex() As Integer Implements System.Windows.Forms.IDataGridViewEditingControl.EditingControlRowIndex
            Get
                Return m_RowIndex
            End Get
            Set(ByVal Value As Integer)
                m_RowIndex = Value
            End Set
        End Property

        Public Function IDataGridViewEditingControl_IsInputKey(ByVal keyData As Keys, ByVal dataGridViewWantsInputKey As Boolean) As Boolean Implements System.Windows.Forms.IDataGridViewEditingControl.EditingControlWantsInputKey
            ' This code was copied from the DataGridViewTextBoxEditingControl
            Select Case (keyData And Keys.KeyCode)
                Case Keys.Right
                    ' If the end of the selection is at the end of the string
                    ' let the DataGridView treat the key message
                    If Not (SelectionLength = 0 AndAlso SelectionStart = Text.Length) Then
                        Return True
                    End If
                Case Keys.Left
                    ' If the end of the selection is at the begining of the string
                    ' or if the entire text is selected and we did not start editing
                    ' send this character to the dataGridView, else process the key event
                    If Not (SelectionLength = 0 AndAlso SelectionStart = 0) Then
                        Return True
                    End If
                Case Keys.Down
                    If TypeOf EditControl Is ComboBox AndAlso DirectCast(EditControl, ComboBox).DroppedDown Then
                        Return True
                    Else
                        ' If the end of the selection is on the last line of the text then 
                        ' send this character to the dataGridView, else process the key event
                        Dim EndPos As Integer = SelectionStart + SelectionLength
                        If Text.IndexOf("\r\n", EndPos) <> -1 Then
                            Return True
                        End If
                    End If
                Case Keys.Up
                    ' If the end of the selection is on the first line of the text then 
                    ' send this character to the dataGridView, else process the key event
                    If TypeOf EditControl Is ComboBox AndAlso DirectCast(EditControl, ComboBox).DroppedDown Then
                        Return True
                    Else
                        If Not (Text.IndexOf("\r\n") < 0 OrElse SelectionStart + SelectionLength < Text.IndexOf("\r\n")) Then
                            Return True
                        End If
                    End If
                Case Keys.Home, Keys.End
                    If TypeOf EditControl Is ComboBox AndAlso DirectCast(EditControl, ComboBox).DroppedDown Then
                        Return True
                    Else
                        If SelectionLength <> Text.Length Then
                            Return True
                        End If
                    End If
                Case Keys.Prior, Keys.Next
                    If TypeOf EditControl Is ComboBox AndAlso DirectCast(EditControl, ComboBox).DroppedDown Then
                        Return True
                    Else
                        If Me.ValueChanged Then
                            Return True
                        End If
                    End If
                Case Keys.Delete
                    If SelectionLength > 0 OrElse SelectionStart < Text.Length Then
                        Return True
                    End If
                Case Keys.Enter
                    If (keyData And (Keys.Control Or Keys.Shift Or Keys.Alt)) = Keys.Shift Then
                        Return True
                    End If
            End Select
            If Me.IsInputKey(keyData) Then
                Return True
            End If
            Return Not dataGridViewWantsInputKey
        End Function

        Public Sub PrepareForEdit(ByVal selectAll As Boolean) Implements System.Windows.Forms.IDataGridViewEditingControl.PrepareEditingControlForEdit
            If selectAll Then
                Me.SelectAll()
            End If
        End Sub

        Public Sub UseCellStyle(ByVal dataGridViewCellStyle As System.Windows.Forms.DataGridViewCellStyle) Implements System.Windows.Forms.IDataGridViewEditingControl.ApplyCellStyleToEditingControl
            Me.Font = dataGridViewCellStyle.Font
            Me.BackColor = dataGridViewCellStyle.BackColor
            Me.ForeColor = dataGridViewCellStyle.ForeColor
        End Sub

        Public Property ValueChanged() As Boolean Implements System.Windows.Forms.IDataGridViewEditingControl.EditingControlValueChanged
            Get
                Return m_valueChanged
            End Get
            Set(ByVal Value As Boolean)
                m_valueChanged = Value
                If m_DataGridView.CurrentCellAddress.X = -1 OrElse m_DataGridView.CurrentCellAddress.Y = -1 Then
                    Debug.Assert(m_DataGridView.IsCurrentCellInEditMode, "Why did the value change when we aren't in edit mode!?")
                    m_DataGridView.CurrentCell = m_DataGridView.Rows(RowIndex).Cells(3)
                    Debug.Assert(m_DataGridView.CurrentCell IsNot Nothing AndAlso _
                                 TypeOf m_DataGridView.CurrentCell Is DataGridViewUITypeEditorCell, _
                                 "Wrong cell type - was expecting a DataGridViewUITypeEditorCell")
                End If
                m_DataGridView.NotifyCurrentCellDirty(ValueChanged)
            End Set
        End Property
#End Region


#Region "Service provider stuff"
        Private m_ServiceProvider As IServiceProvider
        Friend Property ServiceProvider() As IServiceProvider
            Get
                Return m_ServiceProvider
            End Get
            Set(ByVal Value As IServiceProvider)
                m_ServiceProvider = Value
            End Set
        End Property

        Protected Overrides Function GetService(ByVal service As System.Type) As Object
            If m_ServiceProvider IsNot Nothing Then
                Dim requestedService As Object = m_ServiceProvider.GetService(service)
                If requestedService IsNot Nothing Then
                    Return requestedService
                End If
            End If
            Return MyBase.GetService(service)
        End Function
#End Region


        Protected Overrides Function FormatValue(ByVal ValueToFormat As Object) As String
            If ValueToFormat IsNot Nothing AndAlso ValueToFormat.GetType().Equals(GetType(SerializableConnectionString)) Then
                Return DirectCast(ValueToFormat, SerializableConnectionString).ConnectionString
            Else
                Dim serializer As New SettingsValueSerializer()
                Return serializer.Serialize(ValueToFormat, Threading.Thread.CurrentThread.CurrentCulture)
            End If
        End Function

        Protected Overrides Function ParseValue(ByVal SerializedRepresentation As String, ByVal ValueType As Type) As Object
            If ValueType Is GetType(SerializableConnectionString) Then
                Dim retVal As SerializableConnectionString
                If Me.InnerValue IsNot Nothing Then
                    retVal = DirectCast(Me.InnerValue, SerializableConnectionString)
                Else
                    retVal = New SerializableConnectionString
                End If
                retVal.ConnectionString = SerializedRepresentation
                Return retVal
            Else
                Dim serializer As New SettingsValueSerializer()
                Return serializer.Deserialize(ValueType, SerializedRepresentation, Threading.Thread.CurrentThread.CurrentCulture)
            End If
        End Function

        Protected Overrides Sub OnValueChanged()
            ValueChanged = True
        End Sub

        Protected Overrides Function GetSpecificEditorForType(ByVal KnownType As Type) As UITypeEditor
            If KnownType Is GetType(SerializableConnectionString) Then
                Return New ConnectionStringUITypeEditor
            ElseIf KnownType Is GetType(System.Collections.Specialized.StringCollection) Then
                Return New StringArrayEditorForStringCollections()
            Else
                Return MyBase.GetSpecificEditorForType(KnownType)
            End If
        End Function

        ''' <summary>
        ''' ITypeDescriptorContext to pass into UITypeEditors providing services and access to the current instance
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Context() As System.ComponentModel.ITypeDescriptorContext
            Get
                Return New EditContext(TryCast(Me.DataGridView.CurrentRow.Tag, DesignTimeSettingInstance))
            End Get
        End Property

        ''' <summary>
        ''' ITypeDescriptorContext to pass into UITypeEditors providing services and access to the current instance
        ''' </summary>
        ''' <remarks></remarks>
        Private Class EditContext
            Implements System.ComponentModel.ITypeDescriptorContext

            Private _instance As DesignTimeSettingInstance

            Public Sub New(ByVal instance As DesignTimeSettingInstance)
                _instance = instance
            End Sub

            ''' <summary>
            ''' The container (site) that the current instance is hosted in
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Container() As System.ComponentModel.IContainer Implements System.ComponentModel.ITypeDescriptorContext.Container
                Get
                    If _instance IsNot Nothing AndAlso _instance.Site IsNot Nothing Then
                        Return _instance.Site.Container
                    Else
                        Return Nothing
                    End If
                End Get
            End Property

            ''' <summary>
            ''' The instance this value is associated with
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Instance() As Object Implements System.ComponentModel.ITypeDescriptorContext.Instance
                Get
                    Return _instance
                End Get
            End Property

            ''' <summary>
            ''' Let the IComponentChangeService handle component change notifications
            ''' </summary>
            ''' <remarks></remarks>
            Public Sub OnComponentChanged() Implements System.ComponentModel.ITypeDescriptorContext.OnComponentChanged

            End Sub

            ''' <summary>
            ''' We don't have any objections to things changing...
            ''' </summary>
            ''' <remarks></remarks>
            Public Function OnComponentChanging() As Boolean Implements System.ComponentModel.ITypeDescriptorContext.OnComponentChanging
                Return True
            End Function

            ''' <summary>
            ''' Get the associated instance's value property descriptor
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property PropertyDescriptor() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ITypeDescriptorContext.PropertyDescriptor
                Get
                    Return System.ComponentModel.TypeDescriptor.GetProperties(GetType(DesignTimeSettingInstance)).Item("Value")
                End Get
            End Property

            ''' <summary>
            ''' Provide services to the UITypeEditor...
            ''' </summary>
            ''' <param name="serviceType"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Function GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
                Return Nothing
            End Function
        End Class

    End Class
End Namespace
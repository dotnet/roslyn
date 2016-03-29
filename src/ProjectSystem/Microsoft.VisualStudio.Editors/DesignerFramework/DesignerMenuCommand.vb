'******************************************************************************
'* DesignerMenuCommand.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    '**************************************************************************
    ';DesignerMenuCommand
    '
    'Remarks:
    '   This class is based on Microsoft.VSDesigner.DesignerFramework.DesignerMenuCommand.
    '       (wizard\vsdesigner\designer\microsoft\vsdesigner\DesignerFramework).
    '   It represents a shell menu, context menu, or tool box item.
    '   It inherits from System.ComponentModel.Design.MenuCommand and provides additional events
    '       to verify the status (checked, enabled) of the menu command at run-time.
    '   It also calls the root designer to refresh the status of all the menu commands 
    '       owned by the root designer after each Invoke.
    '**************************************************************************
    Friend Class DesignerMenuCommand
        Inherits Microsoft.VisualStudio.Shell.OleMenuCommand

        '= PUBLIC =============================================================
        ';Properties
        '==========

        '**************************************************************************
        ';OleStatus
        '
        'Summary:
        '   Gets the OLE command status code for this menu item.
        'Returns:
        '   An integer containing a mixture of status flags that reflect the state of this menu item.
        'Remarks:
        '   We also update the status of this menu item in this property based on 
        '   m_AlwaysCheckStatus and m_StatusValid flag.
        '**************************************************************************
        Public Overrides ReadOnly Property OleStatus() As Integer
            Get
                If m_AlwaysCheckStatus OrElse Not m_StatusValid Then
                    UpdateStatus()
                End If
                Return MyBase.OleStatus
            End Get
        End Property

        ';Methods
        '==========

        '**************************************************************************
        ';Invoke
        '
        'Summary:
        '   Invokes the command.
        'Remarks:
        '   After invoking the command, we also call RefreshMenuStatus on the RootDesigner,
        '   which refreshes the status of all the menus the designer knows about.
        '**************************************************************************
        Public Overrides Sub Invoke()
            MyBase.Invoke()

            If Not (m_RootDesigner Is Nothing) Then
                ' Refresh the status of all the menus for the current designer.
                m_RootDesigner.RefreshMenuStatus()
            End If
        End Sub 'Invoke

        Public Overrides Sub Invoke(ByVal inArg As Object, ByVal outArg As System.IntPtr)
            MyBase.Invoke(inArg, outArg)

            If Not (m_RootDesigner Is Nothing) Then
                ' Refresh the status of all the menus for the current designer.
                m_RootDesigner.RefreshMenuStatus()
            End If
        End Sub

        Public Overrides Sub Invoke(ByVal inArg As Object)
            MyBase.Invoke(inArg)

            If Not (m_RootDesigner Is Nothing) Then
                ' Refresh the status of all the menus for the current designer.
                m_RootDesigner.RefreshMenuStatus()
            End If
        End Sub

        '= FRIEND =============================================================
        ';Constructors
        '==========

        '**************************************************************************
        ';New
        '
        'Summary:
        '   Constructs a new designer menu item.
        'Params:
        '   RootDesigner: The root designer that owns this menu item (may be Nothing)
        '   CommandID: The command ID of this item. It comes from Constants.MenuConstants (and its value must match
        '       one of the constants in designerui\VisualStudioEditorsUI.h).
        '   CommandHadler: The event handler to handle this menu item.
        '   CommandEnabledHandler: The event handler to check if this menu item should be enabled or not.
        '   CommandCheckedHandler: The event handler to check if this menu item should be checked or not.
        '   CommandVisibleHandler: The event handler to check if this menu item should be visible or not.
        '   AlwaysCheckStatus: True to always call the handlers to check for status. False to only call when the status
        '       is marked invalid.
        '   CommandText: If specified (and the TEXTMENUCHANGES flag is set for the command in the CTC file) you can 
        '       supplies your own text for the command. 
        '**************************************************************************
        Friend Sub New(ByVal RootDesigner As BaseRootDesigner, ByVal CommandID As CommandID, _
                        ByVal CommandHandler As EventHandler, _
                        Optional ByVal CommandEnabledHandler As CheckCommandStatusHandler = Nothing, _
                        Optional ByVal CommandCheckedHandler As CheckCommandStatusHandler = Nothing, _
                        Optional ByVal CommandVisibleHandler As CheckCommandStatusHandler = Nothing, _
                        Optional ByVal AlwaysCheckStatus As Boolean = False, _
                        Optional ByVal CommandText As String = Nothing)

            MyBase.New(CommandHandler, CommandID)

            Me.m_RootDesigner = RootDesigner
            Me.m_CommandEnabledHandler = CommandEnabledHandler
            Me.m_CommandCheckedHandler = CommandCheckedHandler
            Me.m_CommandVisibleHandler = CommandVisibleHandler
            Me.m_AlwaysCheckStatus = AlwaysCheckStatus
            If CommandText <> "" Then
                Me.Text = CommandText
            End If
            Visible = True
            Enabled = True

            RefreshStatus()
        End Sub 'New

        ';Methods
        '==========

        '**************************************************************************
        ';RefreshStatus
        '
        'Summary:
        '   Refresh the status of the command.
        '**************************************************************************
        Friend Sub RefreshStatus()
            m_StatusValid = False
            OnCommandChanged(EventArgs.Empty)
        End Sub 'RefreshStatus

        '= PROTECTED ==========================================================

        '= PRIVATE ============================================================

        '**************************************************************************
        ';UpdateStatus
        '
        'Summary:
        '   Calls the command status handlers (if any) to set the status of the command.
        '**************************************************************************
        Private Sub UpdateStatus()
            If Not (Me.m_CommandEnabledHandler Is Nothing) Then
                Enabled = m_CommandEnabledHandler(Me)
            End If
            If Not (Me.m_CommandCheckedHandler Is Nothing) Then
                Checked = m_CommandCheckedHandler(Me)
            End If
            If Not (Me.m_CommandVisibleHandler Is Nothing) Then
                Visible = m_CommandVisibleHandler(Me)
            End If
            m_StatusValid = True
        End Sub 'UpdateStatus

        Private m_RootDesigner As BaseRootDesigner ' Pointer to the RootDesigner allowing refreshing all menu commands.
        Private m_CommandEnabledHandler As CheckCommandStatusHandler ' Handler to check if the command should be enabled.
        Private m_CommandCheckedHandler As CheckCommandStatusHandler ' Handler to check if the command should be checked.
        Private m_CommandVisibleHandler As CheckCommandStatusHandler ' Handler to check if the command should be hidden.
        Private m_AlwaysCheckStatus As Boolean ' True to always check the status of the command after every call. False otherwise.
        Private m_StatusValid As Boolean ' Whether the status of the command is still valid.
    End Class 'DesignerMenuCommand

    Delegate Function CheckCommandStatusHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean


    ''' <summary>
    ''' A combobox control on a MSO command bar needs two commands, one to actually execute the command
    ''' and another to fill the combobox with items. This is a helper class that you can register with 
    ''' the OleMenuCommandService in order to fill your combobox
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class DesignerCommandBarComboBoxFiller
        Inherits DesignerMenuCommand

        Public Delegate Function ItemsGetter() As String()

        Private m_getter As ItemsGetter

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="designer">Root designer associated with this command</param>
        ''' <param name="commandId">CommandID with GUID/id as specified for the command in the CTC file</param>
        ''' <param name="getter">Delegate that returns a list of strings to fill the combobox with</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal designer As BaseRootDesigner, ByVal commandId As CommandID, ByVal getter As ItemsGetter)
            MyBase.New(designer, commandId, AddressOf CommandHandler)

            If getter Is Nothing Then
                Debug.Fail("You must specify a getter for this to work...")
                Throw New ArgumentNullException()
            End If
            Me.Visible = True
            Me.Enabled = True
            m_getter = getter
        End Sub

        ''' <summary>
        ''' Mapping from the Exec to the getter delegate
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub InstanceCommandHandler(ByVal e As Microsoft.VisualStudio.Shell.OleMenuCmdEventArgs)
            If e Is Nothing Then
                Throw New ArgumentNullException
            End If

            If m_getter IsNot Nothing Then
                Dim items As String() = m_getter()
                Marshal.GetNativeVariantForObject(items, e.OutValue)
            End If
        End Sub

        ''' <summary>
        ''' Since we can't pass an instance method from our own class to our base's constructor, we 
        ''' have a shared method that forwards the call to the actual instance command handler...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Shared Sub CommandHandler(ByVal sender As Object, ByVal e As EventArgs)
            Dim oleEventArgs As Microsoft.VisualStudio.Shell.OleMenuCmdEventArgs = TryCast(e, Microsoft.VisualStudio.Shell.OleMenuCmdEventArgs)
            Dim cmdSender As DesignerCommandBarComboBoxFiller = TryCast(sender, DesignerCommandBarComboBoxFiller)
            If cmdSender Is Nothing OrElse oleEventArgs Is Nothing Then
                Throw New InvalidOperationException()
            End If
            cmdSender.InstanceCommandHandler(oleEventArgs)
        End Sub

    End Class

    ''' <summary>
    ''' MSO command bar combobox command helper
    ''' Will handle get/set of the current text in the combobox
    ''' </summary>
    ''' <remarks>
    ''' You also need to add an instance of a DesignerCommandBarComboBoxFiller in order to fill the 
    ''' combobox with items.... This class only handles the current selection!
    ''' </remarks>
    Friend Class DesignerCommandBarComboBox
        Inherits DesignerMenuCommand

        Public Delegate Function CurrentTextGetter() As String
        Public Delegate Sub CurrentTextSetter(ByVal value As String)

        Private m_currentTextGetter As CurrentTextGetter
        Private m_currentTextSetter As CurrentTextSetter

        ''' <summary>
        ''' Construct for the combobox command handler
        ''' </summary>
        ''' <param name="designer"></param>
        ''' <param name="commandId"></param>
        ''' <param name="currentTextGetter">Delegate to get the current text in the combobox</param>
        ''' <param name="currentTextSetter">Delegate to set the current text in the combobox</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal designer As BaseRootDesigner, ByVal commandId As CommandID, ByVal currentTextGetter As CurrentTextGetter, ByVal currentTextSetter As CurrentTextSetter, ByVal enabledHandler As CheckCommandStatusHandler)
            MyBase.New(designer, commandId, AddressOf CommandHandler, enabledHandler)
            If currentTextGetter Is Nothing OrElse currentTextSetter Is Nothing Then
                Debug.Fail("You must specify a getter and setter method")
                Throw New ArgumentNullException()
            End If
            Me.Visible = True
            Me.Enabled = True
            m_currentTextGetter = currentTextGetter
            m_currentTextSetter = currentTextSetter
        End Sub

        ''' <summary>
        ''' Mapping from the Exec to the getter delegate
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub InstanceCommandHandler(ByVal e As Microsoft.VisualStudio.Shell.OleMenuCmdEventArgs)
            If e.InValue Is Nothing Then
                ' Request to get the current text...
                Marshal.GetNativeVariantForObject(m_currentTextGetter(), e.OutValue)
            Else
                ' Request to set the text
                If Not TypeOf e.InValue Is String Then
                    Throw New InvalidOperationException()
                End If
                m_currentTextSetter(DirectCast(e.InValue, String))
            End If
        End Sub

        ''' <summary>
        ''' Since we can't pass an instance method from our own class to our base's constructor, we 
        ''' have a shared method that forwards the call to the actual instance command handler...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Shared Sub CommandHandler(ByVal sender As Object, ByVal e As EventArgs)
            Dim oleEventArgs As Microsoft.VisualStudio.Shell.OleMenuCmdEventArgs = TryCast(e, Microsoft.VisualStudio.Shell.OleMenuCmdEventArgs)
            Dim cboSender As DesignerCommandBarComboBox = TryCast(sender, DesignerCommandBarComboBox)

            If oleEventArgs Is Nothing OrElse cboSender Is Nothing Then
                Throw New InvalidOperationException()
            End If

            cboSender.InstanceCommandHandler(oleEventArgs)
        End Sub

    End Class
	
    ''' <summary>    
    ''' This class is used to replace the command handlers when the designer surface is closed. When the
    ''' designer is opened again this imposter handler is being replaced with actual handlers.
    ''' </summary>
    ''' <remarks>
    ''' This handler acts as a place holder command handler when the actual handler which is bound to the
    ''' UI is deleted as the UI is closed.
    ''' </remarks>
    Friend Class ImposterDesignerMenuCommand
        Inherits DesignerMenuCommand

        ''' <summary>
        ''' Constructs an intance of an ImposterDesignerMenuCommand
        ''' </summary>
        ''' <param name="commandId">Id of the command.</param>
        ''' <remarks>Sets the command invisible and disabled.</remarks>
        Public Sub New(ByVal commandId As CommandID)
            MyBase.New(Nothing, commandId, AddressOf CommandHandler)
            Me.Visible = False
            Me.Enabled = False
        End Sub

        ''' <summary>        
        ''' Command handler of the command.
        ''' </summary>
        ''' <param name="sender">Sender of the event.</param>
        ''' <param name="e">Argument of the event.</param>
        ''' <remarks>This handler is never invoked since the command is disabled.</remarks>
        Private Shared Sub CommandHandler(ByVal sender As Object, ByVal e As EventArgs)
        End Sub
    End Class	

    ''' <summary>
    ''' Helper class to handle a group of commands where only one should be checked (latched)
    ''' (similar to how radio buttons work)
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class LatchedCommandGroup

        Private m_commands As New Dictionary(Of Integer, MenuCommand)

        ''' <summary>
        ''' Add a command to the group
        ''' </summary>
        ''' <param name="Id">A unique (within the group) id of the command</param>
        ''' <param name="Command">The command to add</param>
        ''' <remarks></remarks>
        Public Sub Add(ByVal Id As Integer, ByVal Command As MenuCommand)
            m_commands(id) = Command
        End Sub

        ''' <summary>
        ''' Get the collection of commands that are in this group
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property Commands() As System.Collections.ICollection
            Get
                Return m_commands.Values
            End Get
        End Property

        ''' <summary>
        ''' Make the command passed in the only checked command in the group
        ''' </summary>
        ''' <param name="CommandToCheck"></param>
        ''' <remarks>Will uncheck all commands if the command passed in was not in the group...</remarks>
        Public Sub Check(ByVal CommandToCheck As MenuCommand)
            For Each Command As MenuCommand In m_commands.Values
                If Command Is CommandToCheck Then
                    Command.Checked = True
                Else
                    Command.Checked = False
                End If
            Next
        End Sub

        ''' <summary>
        ''' Make the command associated with the given ID the only checked command in the group
        ''' </summary>
        ''' <param name="Id"></param>
        ''' <remarks></remarks>
        Public Sub Check(ByVal Id As Integer)
            Dim CommandToCheck As MenuCommand = Nothing
            If Not m_commands.TryGetValue(Id, CommandToCheck) Then
                Throw New ArgumentOutOfRangeException
            End If
            Check(CommandToCheck)
        End Sub
    End Class
End Namespace

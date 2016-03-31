Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework

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
    Public Class DesignerMenuCommand
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
<CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")> _
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

        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")> _
        Public Overrides Sub Invoke()
            MyBase.Invoke()

            If Not (m_RootDesigner Is Nothing) Then
                ' Refresh the status of all the menus for the current designer.
                m_RootDesigner.RefreshMenuStatus()
            End If
        End Sub 'Invoke
        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")> _
        Public Overrides Sub Invoke(ByVal inArg As Object, ByVal outArg As System.IntPtr)
            MyBase.Invoke(inArg, outArg)

            If Not (m_RootDesigner Is Nothing) Then
                ' Refresh the status of all the menus for the current designer.
                m_RootDesigner.RefreshMenuStatus()
            End If
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")> _
        Public Overrides Sub Invoke(ByVal inArg As Object)
            MyBase.Invoke(inArg)

            If Not (m_RootDesigner Is Nothing) Then
                ' Refresh the status of all the menus for the current designer.
                m_RootDesigner.RefreshMenuStatus()
            End If
        End Sub

        '= Public =============================================================
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
        Public Sub New(ByVal RootDesigner As BaseRootDesigner, ByVal CommandID As CommandID, _
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
        Public Sub RefreshStatus()
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

    Public Delegate Function CheckCommandStatusHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean


End Namespace

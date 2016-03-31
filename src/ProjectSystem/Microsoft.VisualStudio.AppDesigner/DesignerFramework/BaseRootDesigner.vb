Option Strict On
Option Explicit On

Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics

Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework

    ''' <summary>
    '''  BaseRootDesigner can be used as a base class for other RootDesigner. It handles
    '''  -   Menu commands
    ''' </summary>
    ''' <remarks>
    ''' To hooks up menu items (including tool box, context menu, etc...)
    ''' -----------------------------------------------------------------
    ''' 1.    Define the menus in designerui directory
    '''       (see VSIP documents | Advanced VSPackage Support | Implementing Menu and Toolbar Commands).
    '''       -   Add some unique ID for your menu, group and command into VisualStudioEditorsID.h.
    '''       -   Add those groups, menus and commands into Menus.ctc file (see Command Table Format).
    ''' 2.    Define those constants in vbpackage\Contants.vb - MenuConstants.
    '''       Only expose the final CommandID (combination of the GUID and the command ID).
    ''' 3.    BaseRootDesigner exposes utilities methods to allow you registering menus with the shells, 
    '''       and showing context menus. 
    '''       a. For each command menu:
    '''           - Defines an EventHandler to handle the invoke of that command.
    '''           - Optional: defines functions to check if the command is Enabled or Checked.
    '''           - Defines a DesignerMenuCommand for that command.
    '''       b. Register the commands using BaseRootDesigner.RegisterMenuCommands.
    ''' 4.    In case it is a context menu, use BaseRootDesigner.ShowContextMenu to show it from the designer view.
    ''' </remarks>
    Public MustInherit Class BaseRootDesigner
        Inherits System.ComponentModel.Design.ComponentDesigner
        Implements IServiceProvider

        '= PUBLIC =============================================================
        ';Methods
        '==========


        Protected Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                RemoveMenuCommands()
            End If

            MyBase.Dispose(Disposing)
        End Sub

        '= Public =============================================================

        ''' <summary>
        '''  Exposes GetService from ComponentDesigner to other classes in this assembly to get a service.
        ''' </summary>
        ''' <param name="ServiceType">The type of the service being asked for.</param>
        ''' <returns>The requested service, if it exists.</returns>
        Public Shadows Function GetService(ByVal ServiceType As Type) As Object Implements IServiceProvider.GetService
            Return MyBase.GetService(ServiceType)
        End Function

        ''' <summary>
        '''  Returns a cached ISelectionService.
        ''' </summary>
        ''' <value>The cached ISelectionService.</value>
        Public ReadOnly Property SelectionService() As ISelectionService
            Get
                If m_SelectionService Is Nothing Then
                    SyncLock m_SyncLockObject
                        If m_SelectionService Is Nothing Then
                            m_SelectionService = CType(MyBase.GetService(GetType(ISelectionService)), ISelectionService)
                            Debug.Assert(m_SelectionService IsNot Nothing, "Cannot get ISelectionService!!!")
                        End If
                    End SyncLock
                End If
                Return m_SelectionService
            End Get
        End Property

        ''' <summary>
        '''   Registers a list of menu commands to the shell, also registers common menu commands
        '''   owned by the BaseRootDesigner if specified.
        ''' </summary>
        ''' <param name="MenuCommands">An array list containing the menu commands to add.</param>
        ''' <param name="KeepRegisteredMenuCommands">
        '''  TRUE to keep previously registered menu commands for this designer.
        '''  FALSE otherwise, the root designer will clear its menu commands list and add the new one.
        ''' </param>
        ''' <param name="AddCommonMenuCommands">TRUE to add the common menu commands owned by BaseRootDesigner, 
        '''      FALSE otherwise.</param>
        ''' <remarks>Child root designers call this method to register their own menu commands. 
        '''      See ResourceEditorRootDesigner.</remarks>
        Public Sub RegisterMenuCommands(ByVal MenuCommands As ArrayList, _
                Optional ByVal KeepRegisteredMenuCommands As Boolean = True, _
                Optional ByVal AddCommonMenuCommands As Boolean = True)
            ' Clear the list of menu commands if specified.
            If Not KeepRegisteredMenuCommands Then
                For Each MenuCommand As MenuCommand In Me.MenuCommands
                    Me.MenuCommandService.RemoveCommand(MenuCommand)
                Next
                Me.MenuCommands.Clear()
            End If

            ' Add the common menu commands if specified.
            If AddCommonMenuCommands Then
                Me.AddCommonMenuCommands()
            End If

            ' Register the new ones
            For Each MenuCommand As MenuCommand In MenuCommands
                Me.MenuCommandService.AddCommand(MenuCommand)
                Me.MenuCommands.Add(MenuCommand)
            Next
        End Sub

        Public Sub RemoveMenuCommands()
            'Iterate backwards to avoid problems removing while iterating
            For i As Integer = MenuCommands.Count - 1 To 0 Step -1
                Dim MenuCommand As MenuCommand = DirectCast(Me.MenuCommands(i), MenuCommand)
                Me.MenuCommandService.RemoveCommand(MenuCommand)
                Me.MenuCommands.RemoveAt(i)
            Next
            Debug.Assert(Me.MenuCommands.Count = 0)
        End Sub

        ''' <summary>
        ''' Shows the specified context menu at the specified position.
        ''' </summary>
        ''' <param name="ContextMenuID">The id of the context menu, usually from Constants.MenuConstants.</param>
        ''' <param name="X">The X coordinate to show the context menu.</param>
        ''' <param name="Y">The Y coordinate to show the context menu.</param>
        ''' <remarks>We don't expose the menu command service so other classes would not call 
        '''      AddCommand, RemoveCommand, etc... easily.</remarks>
        Public Sub ShowContextMenu(ByVal ContextMenuID As CommandID, ByVal X As Integer, ByVal Y As Integer)
            Me.MenuCommandService.ShowContextMenu(ContextMenuID, X, Y)
        End Sub

        ''' <summary>
        '''  Refreshes the status of all the menus of the current designer. 
        '''  This is called from DesignerMenuCommand after each invoke.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub RefreshMenuStatus()
            For Each MenuItem As MenuCommand In MenuCommands
                Debug.Assert(MenuItem IsNot Nothing, "MenuItem IsNot Nothing!")
                If TypeOf MenuItem Is DesignerMenuCommand Then
                    CType(MenuItem, DesignerMenuCommand).RefreshStatus()
                End If
            Next
        End Sub

        '= PROTECTED ==========================================================

        '= PRIVATE ============================================================

        ''' <summary>
        '''  Returns the menu command service that allows adding, removing, finding command 
        '''  as well as showing context menu.
        ''' </summary>
        ''' <value>The IMenuCommandService from the shell.</value>
        ''' <remarks>Don't want to expose this one to other classes to encourage using RegisterMenuCommands.</remarks>
        Private ReadOnly Property MenuCommandService() As IMenuCommandService
            Get
                If m_MenuCommandService Is Nothing Then
                    SyncLock m_SyncLockObject
                        If m_MenuCommandService Is Nothing Then
                            m_MenuCommandService = CType(Me.GetService(GetType(IMenuCommandService)), IMenuCommandService)
                            Debug.Assert(Not m_MenuCommandService Is Nothing, "Cannot get menu command service!")
                        End If
                    End SyncLock
                End If
                Return m_MenuCommandService
            End Get
        End Property

        ''' <summary>
        '''  Returns an arraylist containing all the current registered commands from this designer.
        ''' </summary>
        ''' <value>An ArrayList contains MenuCommand.</value>
        Private ReadOnly Property MenuCommands() As ArrayList
            Get
                Return m_MenuCommands
            End Get
        End Property

        ''' <summary>
        '''  Adds common menu commands owned by BaseRootDesigner.
        ''' </summary>
        ''' <remarks>Not currently used since we don't have any common menu commands yet.</remarks>
        Private Sub AddCommonMenuCommands()
        End Sub

        ' All the menu commands this designer exposes. Use MenuCommands to access this.
        Private m_MenuCommands As New ArrayList
        ' Pointer to the IMenuCommandService.
        Private m_MenuCommandService As IMenuCommandService = Nothing
        ' Pointer to ISelectionService
        Private m_SelectionService As ISelectionService = Nothing
        ' SyncLock object used to lazy initialized private fields.
        Private m_SyncLockObject As New Object

    End Class
End Namespace

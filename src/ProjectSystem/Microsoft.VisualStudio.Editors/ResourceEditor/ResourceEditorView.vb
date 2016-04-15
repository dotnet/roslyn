' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Option Compare Binary
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports EnvDTE
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Common.DTEUtils
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.Win32
Imports VB = Microsoft.VisualBasic

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This class contains the actual top-level UI surface for the resource
    '''   editor.  It is created by ResourceEditorRootDesigner.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ResourceEditorView
        Inherits Microsoft.VisualStudio.Editors.DesignerFramework.BaseDesignerView
        Implements Resource.ITypeResolutionContextProvider
        Implements IVsBroadcastMessageEvents
        Implements IVsWindowPaneCommit


#Region "Instance Fields"

        'A file watcher instance to listen for changes in the files we have links to.
        Private _fileWatcher As FileWatcher

        'Standard title for messageboxes, etc.
        Private Shared ReadOnly s_messageBoxCaption As String = SR.GetString(SR.RSE_ResourceEditor)

        'The project guid for C++ (Project.Kind)
        Private ReadOnly _projectGuid_CPlusPlus As New Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942")

        'The project guid for VB (Project.Kind)
        Private ReadOnly _projectGuid_VB As New Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F")

        'Indicates whether the UI has actually been initialized yet (useful because many events fire during
        '  form load, and we may not be ready to handle them  yet)
        Private _UIInitialized As Boolean

        'The set of categories handled by this instance of the resource editor.
        Private _categories As New CategoryCollection

        'The root designer associated with this resource editor instance
        Private _rootDesigner As ResourceEditorRootDesigner

        'The ResourceFile being displayed/edited.
        Private _resourceFile As ResourceFile

        'The currently-displayed category.  This can be Nothing during initialization or even afterwards
        '  if there was an error loading the editor.
        Private _currentCategory As Category

        'Temporary files which can be cleaned up on the next clipboard flush.
        Private _deleteFilesOnClipboardFlush As New ArrayList

        'Temporary files which can be cleaned up when the editor exists.
        Private _deleteFoldersOnEditorExit As New ArrayList

        'The set of internal resources that we cache for this instance of the resource editor
        Private _cachedResources As CachedResourcesForView

        'We need to be able to control the property grid selection sometimes, so we have this
        '  flag to disable it when needed.
        Private _disablePropertyGridSelect As Boolean

        'Programmatic category names (non-localized)
        Private ReadOnly _categoryNameStrings As String = "Strings"
        Private ReadOnly _categoryNameImages As String = "Images"
        Private ReadOnly _categoryNameIcons As String = "Icons"
        Private ReadOnly _categoryNameAudio As String = "Audio"
        Private ReadOnly _categoryNameFiles As String = "Files"
        Private ReadOnly _categoryNameOther As String = "Other"

        'Category instances for this resource editor instance
        Private WithEvents _categoryStrings As Category
        Private WithEvents _categoryImages As Category
        Private WithEvents _categoryIcons As Category
        Private WithEvents _categoryAudio As Category
        Private WithEvents _categoryFiles As Category
        Private WithEvents _categoryOther As Category

        'A cached pointer to the type resolution service to use for this project.  This may be expensive to calculate.
        '  Value may be Nothing, even if m_TypeResolutionServiceIsCached is True.
        Private _typeResolutionServiceCache As ITypeResolutionService

        'Indicates whether m_TypeResolutionServiceCache contains a cached value.
        Private _typeResolutionServiceIsCached As Boolean

        'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
        Private _cookieBroadcastMessages As UInteger

        ' Groups of commands that are supposed to work as radio buttons (i.e.
        ' only one of the commands checked at a time)
        Private _categoryLatchedCommandGroup As LatchedCommandGroup
        Private _viewsLatchedCommandGroup As LatchedCommandGroup

        ' The command that shows up on the command on the Views menu controller
        Private _buttonViewsCommand As DesignerMenuCommand

        'If true, we are editing a label (name label) or a field
        Private _inEditingItem As Boolean

        ' all menu commands supported by this designer....
        Private _menuCommands As ArrayList

        ' ReadOnly Mode
        Private _readOnlyMode As Boolean

        ' Undo status
        Private _inUndoing As Boolean
        Private _categoryAffected As Category
        Private _resourcesAffected As Hashtable

        ' Registry path
        Private _registryRoot As String

        ' Pad around the stringTable/ListView
        Private Const s_DESIGNER_PADDING As Integer = 14
        Private Const s_DESIGNER_PADDING_TOP As Integer = 23

        ' the seperator character to save multiple extensions in one string
        Private Const s_SAFE_EXTENSION_SEPERATOR As Char = "|"c

#End Region


#Region "Shared Fields"

        'A cached set of default, commonly-used assemblies to handle all our intrinsic type editors
        '  plus other commonly-found types in .resx files.
        'This list is used for resolving types in a .resx file when the .resx file is opened
        '  outside of the context of a project.
        'Note: if we allow adding new type editors, this array will need to be updated with
        '  the assemblies of any types handled by these type editors.
        '
        'System, mscorlib, System.Drawing, System.Windows.Forms, System.Data
        Private Shared s_defaultAssemblyReferences() As AssemblyName =
            {
                GetType(System.CodeDom.MemberAttributes).Assembly.GetName(),
                GetType(System.Int32).Assembly.GetName(),
                 GetType(System.Drawing.Bitmap).Assembly.GetName(),
                GetType(System.Windows.Forms.Form).Assembly.GetName(),
                GetType(System.Data.DataSet).Assembly.GetName()
            }


#End Region


#Region "Controls which *are* initialized in InitializeComponents"

        'An instance of the ResourceStringTable class.  Used for displaying strings.
        Friend WithEvents StringTable As Microsoft.VisualStudio.Editors.ResourceEditor.ResourceStringTable

        'An instance of the ResourceListView class.  Used for displaying resources with thumbnails.
        Private WithEvents _resourceListView As Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView

        ' Hosting control for our commandbar
        Private _toolbarPanel As DesignerToolbarPanel

#End Region


#Region "Constructors/destructors"


        ''' <summary>
        ''' Constructor needed for form designer (should use the constructor with a service provider)
        ''' </summary>
        ''' <remarks></remarks>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        Public Sub New()
            Me.New(Nothing)
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ServiceProvider As IServiceProvider)
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            SetFonts(ServiceProvider)

            InitializeResourceCategories()
            InitializeUI(ServiceProvider)
        End Sub


        ''' <summary>
        ''' Overrides Dispose()
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                'Turn off listening to virtual notifications immediately.
                If _resourceListView IsNot Nothing Then
                    _resourceListView.DisableItemRetrieval = True
                End If

                If _accessModifierCombobox IsNot Nothing Then
                    _accessModifierCombobox.Dispose()
                End If

                If _cookieBroadcastMessages <> 0 Then
                    Dim VsShell As IVsShell = DirectCast(RootDesigner.GetService(GetType(IVsShell)), IVsShell)
                    If VsShell IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(VsShell.UnadviseBroadcastMessages(_cookieBroadcastMessages))
                    End If
                End If

                If _components IsNot Nothing Then
                    _components.Dispose()
                End If

                If Not _resourceFile Is Nothing Then
                    _resourceFile.Dispose()
                End If

                If Not _fileWatcher Is Nothing Then
                    Debug.Assert(_fileWatcher.DirectoryWatchersCount = 0, "All file watch entries should already have been cleared")
                    _fileWatcher.Dispose()
                    _fileWatcher = Nothing
                End If

                'Delete any remaining temporary files and folders
                DeleteTemporaryFiles(_deleteFilesOnClipboardFlush)
                _deleteFilesOnClipboardFlush.Clear()

                DeleteTemporaryFolders(_deleteFoldersOnEditorExit)
                _deleteFoldersOnEditorExit.Clear()

                If _cachedResources IsNot Nothing Then
                    _cachedResources.Dispose()
                End If

                _typeResolutionServiceCache = Nothing

            End If

            'Will dispose of the controls as well.
            MyBase.Dispose(Disposing)
        End Sub

#End Region

        Private _accessModifierCombobox As ResourceEditorAccessModifierCombobox

#Region "Private Class for the 'Access modifier' dropdown'"

        Friend Class ResourceEditorAccessModifierCombobox
            Inherits AccessModifierCombobox

            Private _isInDevice20Project As Boolean
            Private Const s_framework_2_0 As Integer = 2

            Public Sub New(ByVal useVbMyResXCodeGenerator As Boolean, ByVal allowNoCodeGeneration As Boolean, ByVal rootDesigner As BaseRootDesigner, ByVal serviceProvider As IServiceProvider, ByVal projectItem As EnvDTE.ProjectItem, ByVal namespaceToOverrideIfCustomToolIsEmpty As String)
                MyBase.New(rootDesigner, serviceProvider, projectItem, namespaceToOverrideIfCustomToolIsEmpty)

                _isInDevice20Project = IsInDevice20Project(rootDesigner)

                AddCodeGeneratorEntry(AccessModifierConverter.Access.Friend, IIf(useVbMyResXCodeGenerator, s_VBMYCUSTOMTOOL, s_STANDARDCUSTOMTOOL))
                If Not _isInDevice20Project Then
                    ' public generator is not supported in Device 2.0 projects
                    AddCodeGeneratorEntry(AccessModifierConverter.Access.Public, IIf(useVbMyResXCodeGenerator, s_VBMYCUSTOMTOOLPUBLIC, s_STANDARDCUSTOMTOOLPUBLIC))
                End If

                If allowNoCodeGeneration Then
                    'An empty custom tool gives us "No Code Generation".  Anything non-empty will be considered
                    '  custom and will disable the combobox.
                    AddCodeGeneratorEntry(SR.GetString(SR.RSE_NoCodeGeneration), "")
                End If

                'Ensure we don't disable the Access Modifier combobox just because the custom tool is set to the
                '  My.Resources version of the custom tool when the standard version was expected, or vice versa.
                '  This way all the resx generators are recognized.
                AddRecognizedCustomToolValue(s_VBMYCUSTOMTOOL)
                AddRecognizedCustomToolValue(s_VBMYCUSTOMTOOLPUBLIC)
                AddRecognizedCustomToolValue(s_STANDARDCUSTOMTOOL)
                AddRecognizedCustomToolValue(s_STANDARDCUSTOMTOOLPUBLIC)
            End Sub

            Public Shadows Function GetMenuCommandsToRegister() As ICollection
                Return MyBase.GetMenuCommandsToRegister(
                    Constants.MenuConstants.CommandIDRESXAccessModifierCombobox,
                    Constants.MenuConstants.CommandIDRESXGetAccessModifierOptions)
            End Function

            Protected Overrides Function IsDesignerEditable() As Boolean
                'UNDONE: test SCC checkout
                Dim view As ResourceEditorView = CType(CType(RootDesigner, IRootDesigner).GetView(ViewTechnology.Default), ResourceEditorView)
                If view Is Nothing Then
                    Debug.Fail("Failed to get the resource editor view")
                    Return False
                End If

                Return Not view.ReadOnlyMode
            End Function

            Private Function IsInDevice20Project(ByVal rootDesigner As BaseRootDesigner) As Boolean
                Dim hierarchy As IVsHierarchy = DirectCast(rootDesigner.GetService(GetType(IVsHierarchy)), IVsHierarchy)
                If hierarchy IsNot Nothing Then
                    If Common.ShellUtil.IsDeviceProject(hierarchy) Then
                        Try
                            Dim frameworkVersionNumber As UInteger = Utils.GetProjectTargetFrameworkVersion(hierarchy)
                            Dim majorVersionNumber As UInteger = frameworkVersionNumber >> 16
                            Return majorVersionNumber <= s_framework_2_0
                        Catch ex As NotSupportedException
                        Catch ex As NotImplementedException
                        End Try
                    End If
                End If
                Return False
            End Function

            Protected Overrides Function ShouldBeEnabled() As Boolean
                If _isInDevice20Project Then
                    Return False
                End If
                Return MyBase.ShouldBeEnabled()
            End Function
        End Class

#End Region

#Region "Properties"


        ''' <summary>
        ''' Retrieves the set of categories handled by this instance of the resource editor.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property Categories() As CategoryCollection
            Get
                Return _categories
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the file watcher for this instance of the resource editor.  Creates one if necessary.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property FileWatcher() As FileWatcher
            Get
                If _fileWatcher Is Nothing Then
                    If Me.Handle.Equals(0) Then
                        Debug.Fail("Had to manually create control handle - is that okay?")
                        Me.CreateControl()
                    End If
                    _fileWatcher = New FileWatcher(Me)
                End If
                Return _fileWatcher
            End Get
        End Property

        ''' <summary>
        ''' Return true when we are editing a name label
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property IsInEditing() As Boolean
            Get
                Return _inEditingItem
            End Get
        End Property

        ''' <summary>
        ''' Return true when we are undoing/redoing
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property IsUndoing() As Boolean
            Get
                Return _inUndoing
            End Get
        End Property


        ''' <summary>
        ''' ReadOnly Mode
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend Property ReadOnlyMode() As Boolean
            Get
                Return _readOnlyMode
            End Get
            Set(ByVal value As Boolean)
                If _readOnlyMode <> value Then
                    _readOnlyMode = value
                    RefreshCommandStatus()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets the RootDesigner associated with this resource editor instance.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property RootDesigner() As ResourceEditorRootDesigner
            Get
                Debug.Assert(Not _rootDesigner Is Nothing, "Can't call RootDesigner before SetRootDesigner() - we don't have a root designer cached yet")
                Return _rootDesigner
            End Get
        End Property


        ''' <summary>
        ''' Gets the ResourceFile that is being viewed/edited in this view.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ResourceFile() As ResourceFile
            Get
                Return _resourceFile
            End Get
        End Property


        ''' <summary>
        ''' The component being edited - ResourceEditorRootComponent
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property RootComponent() As ResourceEditorRootComponent
            Get
                If _rootDesigner IsNot Nothing AndAlso _rootDesigner.Component IsNot Nothing Then
                    Return _rootDesigner.Component
                Else
                    Debug.Fail("No Component?")
                    Return Nothing
                End If
            End Get
        End Property


        Public ReadOnly Property CurrentCategory() As Category
            Get
                Debug.Assert(_currentCategory IsNot Nothing)
                Return _currentCategory
            End Get
        End Property

        ''' <summary>
        ''' Return the root Registry key of the Resource Editor
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly Property RegistryRoot() As String
            Get
                If _registryRoot Is Nothing Then
                    Dim localRegistry As ILocalRegistry2 = DirectCast(RootDesigner.GetService(GetType(ILocalRegistry)), ILocalRegistry2)
                    Debug.Assert(localRegistry IsNot Nothing, "why we can not find ILocalRegistry2")
                    If localRegistry IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(localRegistry.GetLocalRegistryRoot(_registryRoot))
                        _registryRoot = _registryRoot & "\ManagedResourcesEditor"
                    End If
                End If
                Return _registryRoot
            End Get
        End Property

        ''' <summary>
        '''  We save a list of file extensions in the registry. All extensions were approved by the customer that they don't want to see
        '''  a warning dialog when they double-click the item to open it.
        ''' </summary>
        ''' <remarks></remarks>
        Private Property SafeExtensions() As String
            Get
                Dim registryPath As String = RegistryRoot
                If registryPath IsNot Nothing Then
                    Try
                        Using registryKey As RegistryKey = Registry.CurrentUser.OpenSubKey(registryPath)
                            If registryKey IsNot Nothing Then
                                Dim value As Object = registryKey.GetValue("SafeExtensions", "")
                                Return TryCast(value, String)
                            End If
                        End Using
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        Debug.Fail(ex.Message)
                        'Catch ex As Object
                        '    Debug.Fail("Unexpected, non-CLS compliant exception")
                        '    Throw ex
                    End Try
                End If
                Return String.Empty
            End Get
            Set(ByVal value As String)
                Dim registryPath As String = RegistryRoot
                If registryPath IsNot Nothing Then
                    Try
                        Dim registryKey As RegistryKey = Registry.CurrentUser.OpenSubKey(registryPath, True)
                        If registryKey Is Nothing Then
                            registryKey = Registry.CurrentUser.CreateSubKey(registryPath)
                        End If
                        If registryKey IsNot Nothing Then
                            Using registryKey
                                registryKey.SetValue("SafeExtensions", value, RegistryValueKind.String)
                            End Using
                        End If
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        Debug.Fail(ex.Message)
                        'Catch ex As Object
                        '    Debug.Fail("Unexpected, non-CLS compliant exception")
                        '    Throw ex
                    End Try
                End If
            End Set
        End Property

#End Region


#Region " Windows Form Designer generated code "

        'Required by the Windows Form Designer
        Private _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        '<System.Diagnostics.DebuggerStepThrough()> 
        Private Sub InitializeComponent()
            Me.StringTable = New Microsoft.VisualStudio.Editors.ResourceEditor.ResourceStringTable
            Me._resourceListView = New Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView

            Me.SuspendLayout()
            '
            'ResourceListView
            '
            Me._resourceListView.BackColor = ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_WINDOW, SystemColors.Window, UseVSTheme:=False)
            Me._resourceListView.Text = "ResourceListView"

            _cachedResources = New CachedResourcesForView(Me._resourceListView.BackColor)

            '
            'StringTable
            '
            Me.StringTable.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.StringTable.Location = New System.Drawing.Point(71, 65)
            Me.StringTable.Name = "StringTable"
            Me.StringTable.Size = New System.Drawing.Size(690, 429)
            Me.StringTable.TabIndex = 1
            Me.StringTable.Text = "StringTable"
            Me.StringTable.BackgroundColor = ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_THREEDFACE, SystemColors.ButtonFace, UseVSTheme:=False)
            '
            ' m_toolbarPanel
            '
            _toolbarPanel = New DesignerToolbarPanel()
            _toolbarPanel.Dock = DockStyle.Top
            _toolbarPanel.Name = "ToolbarPanel"
            _toolbarPanel.Text = "ToolbarPanel"
            '
            'ResourceEditorView
            '
            Me.Controls.Add(Me.StringTable)
            Me.Controls.Add(Me._resourceListView)
            Me.Controls.Add(_toolbarPanel)
            Me.Name = "ResourceEditorView"
            Me.Text = "ResourceEditorView"
            Me.Size = New System.Drawing.Size(740, 518)
            Me.Padding = New System.Windows.Forms.Padding(0, 0, 0, 0)
            Me.BackColor = Common.ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_THREEDFACE, SystemColors.ButtonFace, UseVSTheme:=False)

            Me.ResumeLayout(False)

        End Sub

#End Region


#Region "UI Initialization"


        ''' <summary>
        ''' Initializes all UI elements.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub InitializeUI(ByVal ServiceProvider As IServiceProvider)
            Me.AllowDrop = True
            StringTable.RowHeadersWidth = DpiHelper.LogicalToDeviceUnitsX(35)
            _UIInitialized = True
        End Sub


        ''' <summary>
        ''' Gets the environment font for the shell.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetEnvironmentFont(ByVal ServiceProvider As IServiceProvider) As Font
            Dim UIService As IUIService = DirectCast(ServiceProvider.GetService(GetType(IUIService)), IUIService)
            If UIService IsNot Nothing Then
                Dim Font As Font = DirectCast(UIService.Styles("DialogFont"), Font)
                Debug.Assert(Font IsNot Nothing, "Unable to get dialog font from IUIService")
                Return Font
            Else
                Debug.Fail("Unable to get IUIService for dialog font")
                Return Nothing
            End If
        End Function


        ''' <summary>
        ''' Initialize the fonts in the resource editor from the environment (or from the resx file,
        '''   if hard-coded there).
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetFonts(ByVal ServiceProvider As IServiceProvider)
            Dim MainFont, StringTableFont, ListViewFont As Font
            'First try to get the environment font
            MainFont = GetEnvironmentFont(ServiceProvider)

            'Now check the resx file for overrides
            StringTableFont = Utility.GetFontFromResources(SR.RSE_Font_StringTable)
            ListViewFont = Utility.GetFontFromResources(SR.RSE_Font_ListView)
            'Now set the fonts for only those items which have been overridden.  Controls will automatically
            '  get their font from their parent if we don't set them manually.
            If MainFont IsNot Nothing Then
                Me.Font = MainFont
            End If
            If StringTableFont IsNot Nothing Then
                StringTable.Font = StringTableFont
            End If
            If ListViewFont IsNot Nothing Then
                _resourceListView.Font = ListViewFont
            End If
        End Sub



#End Region


#Region "Other Initialization"

        ''' <summary>
        ''' Notifies the view of its corresponding RootDesigner.  Some initialization must wait
        '''   until we get this information.  Can't be passed through the constructor, because
        '''   that can cause problems with reentrant code.
        ''' </summary>
        ''' <param name="RootDesigner"></param>
        ''' <remarks></remarks>
        Public Sub SetRootDesigner(ByVal RootDesigner As ResourceEditorRootDesigner)
            'Cache off the pointer to our root designer (our parent)
            _rootDesigner = RootDesigner

            Dim isEditingResWFile As Boolean = Me.RootDesigner.IsEditingResWFile()

            If Not isEditingResWFile Then

                'We don't allow a "(No code generation)" option for the default resx file, because it doesn't make
                '  sense.  Also, if we allowed it, the resource picker could add back the custom tool property, but
                '  it would be the wrong one (the default resx single file generator).
                Dim allowNoCodeGeneration As Boolean = Not IsDefaultResXFile()

                _accessModifierCombobox = New ResourceEditorAccessModifierCombobox(
                    ShouldUseVbMyResXCodeGenerator(),
                    allowNoCodeGeneration,
                    RootDesigner,
                    RootDesigner,
                    GetResXProjectItem(),
                    IIf(IsVBProject(GetProject()), s_CUSTOMTOOLNAMESPACE, Nothing))
            End If

            'Add our menus (can't do that until we know the root designer)
            Me.RegisterMenuCommands(isEditingResWFile)

            'Hook up for broadcast messages
            Dim VSShell As IVsShell = DirectCast(RootDesigner.GetService(GetType(IVsShell)), IVsShell)
            If VSShell IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(VSShell.AdviseBroadcastMessages(Me, _cookieBroadcastMessages))
            Else
                Debug.Fail("Unable to get IVsShell for broadcast messages")
            End If
        End Sub


        ''' <summary>
        ''' Tells the view to starting displaying resources from a ResourceFile.  Can only be called once.
        ''' </summary>
        ''' <param name="ResourceFile">The ResourceFile to load.</param>
        ''' <remarks></remarks>
        Public Sub SetResourceFile(ByVal ResourceFile As ResourceFile)
            Debug.Assert(Not (ResourceFile Is Nothing))
            If Not _resourceFile Is Nothing Then
                _resourceFile.Dispose()
            End If
            _resourceFile = ResourceFile

            'Display the first category that is not empty
            Dim CategoryToDisplay As Category = _categoryStrings
            For Each Category As Category In _categories
                If Category.ResourceCount > 0 Then
                    CategoryToDisplay = Category
                    Exit For
                End If
            Next

            PopulateResources(CategoryToDisplay)
        End Sub


        ''' <summary>
        ''' Register the resource editor's menu commands (context menus)
        ''' </summary>
        ''' <param name="isEditingResWFile"></param>
        ''' <remarks>Called from SetResourceFile.</remarks>
        Private Sub RegisterMenuCommands(isEditingResWFile As Boolean)
            'Protect against recursively invoking this
            Static InThisMethod As Boolean
            If InThisMethod Then
                Debug.Fail("RegisterMenuCommands was invoked recursively")
                Exit Sub
            End If

            InThisMethod = True
            Try
                _menuCommands = New ArrayList

                'Play
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDResXPlay, AddressOf MenuPlay, AddressOf MenuPlayEnabledHandler,
                    AlwaysCheckStatus:=True))

                'Open/Open With...
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97Open, AddressOf MenuOpen, AddressOf MenuOpenOpenWithEnabledHandler,
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97OpenWith, AddressOf MenuOpenWith, AddressOf MenuOpenOpenWithEnabledHandler,
                    AlwaysCheckStatus:=True))

                'Cut/Copy/Paste/Remove/Rename
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidCut, AddressOf MenuCut, AddressOf MenuCutEnabledHandler,
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidCopy, AddressOf MenuCopy, AddressOf MenuCopyEnabledHandler,
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidPaste, AddressOf MenuPaste, AddressOf MenuPasteEnabledHandler,
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidRemove, AddressOf MenuRemove, AddressOf MenuRemoveEnabledHandler,
                    CommandVisibleHandler:=AddressOf MenuRemoveVisibleHandler,
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDCOMMONRemoveRow, AddressOf Me.MenuRemoveRow, AddressOf Me.MenuRemoveRowEnabledHandler,
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidRename, AddressOf MenuRename, AddressOf MenuRenameEnabledHandler, _
                    AlwaysCheckStatus:=True))
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidEditLabel, AddressOf MenuEditLabel, AddressOf MenuEditLabelEnabledHandler, _
                    AlwaysCheckStatus:=True))

                'Select All
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidSelectAll, AddressOf MenuSelectAll, AddressOf MenuSelectAllEnableHandler))

                'Add menu items
                If isEditingResWFile Then
                    ' Only 'Add Default Resource' is allowed resw files
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddDefaultResource, AddressOf Me.ButtonFixedAdd_Click, AddressOf Me.MenuAddEnabledHandler))
                Else
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddFixedMenuCommand, AddressOf Me.ButtonFixedAdd_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddExistingFile, AddressOf ButtonAdd_ExistingFile_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewString, AddressOf ButtonAdd_NewString_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewTextFile, AddressOf ButtonAdd_NewTextFile_Click, AddressOf Me.MenuAddEnabledHandler))

                    Dim NewIconCommand As DesignerMenuCommand = New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewIcon, AddressOf ButtonAdd_NewIcon_Click, AddressOf Me.MenuAddEnabledHandler)
                    If Microsoft.VisualStudio.Editors.PropertyPages.VSProductSKU.IsExpress() Then
                        ' NOTE: we disable "Add New Icon" in the express SKU, because the icon editor (a part of native resource designer) does not exist in the express SKUs...
                        ' see vswhidbey 456776
                        NewIconCommand.Visible = False
                    End If
                    _menuCommands.Add(NewIconCommand)

                    'Add.New Image menu items
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewImagePNG, AddressOf ButtonAdd_NewImage_PNG_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewImageBMP, AddressOf ButtonAdd_NewImage_BMP_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewImageGIF, AddressOf ButtonAdd_NewImage_GIF_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewImageJPEG, AddressOf ButtonAdd_NewImage_JPEG_Click, AddressOf Me.MenuAddEnabledHandler))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXAddNewImageTIFF, AddressOf ButtonAdd_NewImage_TIFF_Click, AddressOf Me.MenuAddTiffEnabledHandler))

                    'Resource type menu items
                    _categoryLatchedCommandGroup = New LatchedCommandGroup
                    For Each Category As Category In Categories
                        _menuCommands.Add(Category.CommandToShow)
                        _categoryLatchedCommandGroup.Add(Category.CommandToShow.CommandID.ID, Category.CommandToShow)
                    Next Category

                    'Views menu items
                    _buttonViewsCommand = New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXViewsFixedMenuCommand, AddressOf Me.ButtonFixedView_Click)
                    _menuCommands.Add(_buttonViewsCommand)
                    _viewsLatchedCommandGroup = New LatchedCommandGroup
                    _viewsLatchedCommandGroup.Add(Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.List, New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXViewsList, AddressOf ButtonViews_List_Click, AddressOf ViewsMenuItemsEnabledHandler, AlwaysCheckStatus:=True))
                    _viewsLatchedCommandGroup.Add(Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Details, New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXViewsDetails, AddressOf ButtonViews_Details_Click, AddressOf ViewsMenuItemsEnabledHandler, AlwaysCheckStatus:=True))
                    _viewsLatchedCommandGroup.Add(Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Thumbnail, New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXViewsThumbnails, AddressOf ButtonViews_Thumbnail_Click, AddressOf ViewsMenuItemsEnabledHandler, AlwaysCheckStatus:=True))
                    For Each Command As MenuCommand In _viewsLatchedCommandGroup.Commands
                        _menuCommands.Add(Command)
                    Next

                    'Import/Export
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDResXImport, AddressOf MenuImport, AddressOf MenuImportEnabledHandler, _
                        AlwaysCheckStatus:=True))
                    _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDResXExport, AddressOf MenuExport, AddressOf MenuExportEnabledHandler, _
                        AlwaysCheckStatus:=True))

                    'Access modifier combobox
                    _menuCommands.AddRange(_accessModifierCombobox.GetMenuCommandsToRegister())

                End If

                'Delete
                '
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd97cmdidDelete, AddressOf MenuRemove, AddressOf MenuDeleteEnabledHandler, _
                    CommandVisibleHandler:=AddressOf MenuDeleteVisibleHandler, _
                    AlwaysCheckStatus:=True))

                'Edit cell -- leave it here because of VB profile...
                Dim EditCellCommand As DesignerMenuCommand = New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDCOMMONEditCell, AddressOf MenuEditLabel)
                'We don't actually want "Edit" to be visible in the menus.  We simply want to be able to have something to bind the F2 key to
                '  so that pressing F2 in the string table starts editing (the shell seems to steal this key from the grid).  So make it
                '  invisible
                EditCellCommand.Visible = False
                _menuCommands.Add(EditCellCommand)

                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDRESXGenericRemove, AddressOf MenuGenericRemove, AddressOf MenuGenericRemoveEnabledHandler, _
                    AlwaysCheckStatus:=True))

                ' CancelEdit
                _menuCommands.Add(New DesignerMenuCommand(Me.RootDesigner, Constants.MenuConstants.CommandIDVSStd2kECMD_CANCEL, AddressOf Me.MenuCancelEdit, AddressOf Me.MenuCancelEditEnableHandler))

                'Register them
                Me.RootDesigner.RegisterMenuCommands(_menuCommands)

                Dim toolbarID As UInteger = Constants.MenuConstants.IDM_VS_TOOLBAR_Resources

                ' For a ResW file, use a different toolbar that doesn't have commands for unsupported resource types
                If isEditingResWFile Then
                    toolbarID = Constants.MenuConstants.IDM_VS_TOOLBAR_Resources_ResW
                End If

                _toolbarPanel.SetToolbar(DirectCast(RootDesigner.GetService(GetType(IVsUIShell)), IVsUIShell),
                                          Constants.MenuConstants.GUID_RESX_MenuGroup,
                                          toolbarID)
                _toolbarPanel.Activate(Me.Handle)

            Finally
                InThisMethod = False
            End Try
        End Sub


        ''' <summary>
        ''' Create instances of all the categories
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub InitializeResourceCategories()
            _categoryStrings = New Category( _
                _categoryNameStrings, SR.GetString(SR.RSE_Cat_Strings), _
                Category.Display.StringTable, _
                New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeStrings, AddressOf MenuResourceTypeStrings), _
                AddressOf ButtonAdd_NewString_Click, _
                ResourceTypeEditors.String _
                )
            _categoryStrings.AllowNewEntriesInStringTable = True
            _categories.Add(_categoryStrings)

            _categoryImages = New Category( _
                _categoryNameImages, SR.GetString(SR.RSE_Cat_Images), _
                Category.Display.ListView, _
                New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeImages, AddressOf MenuResourceTypeImages), _
                AddressOf ButtonAdd_NewImage_BMP_Click, _
                ResourceTypeEditors.Bitmap _
                )
            _categoryImages.ResourceView = Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Thumbnail
            _categories.Add(_categoryImages)

            If Microsoft.VisualStudio.Editors.PropertyPages.VSProductSKU.IsExpress() Then
                ' NOTE: we disable "Add New Icon" in the express SKU, because the icon editor (a part of native resource designer) does not exist in the express SKUs...
                _categoryIcons = New Category( _
                    _categoryNameIcons, SR.GetString(SR.RSE_Cat_Icons), _
                    Category.Display.ListView, _
                    New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeIcons, AddressOf MenuResourceTypeIcons), _
                    AddressOf Me.ButtonAdd_ExistingFile_Click, _
                    ResourceTypeEditors.Icon)
            Else
                _categoryIcons = New Category( _
                    _categoryNameIcons, SR.GetString(SR.RSE_Cat_Icons), _
                    Category.Display.ListView, _
                    New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeIcons, AddressOf MenuResourceTypeIcons), _
                    AddressOf ButtonAdd_NewIcon_Click, _
                    ResourceTypeEditors.Icon)
            End If

            _categoryIcons.ResourceView = Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.List
            _categories.Add(_categoryIcons)

            _categoryAudio = New Category( _
                _categoryNameAudio, SR.GetString(SR.RSE_Cat_Audio), _
                Category.Display.ListView, _
                New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeAudio, AddressOf MenuResourceTypeAudio), _
                AddressOf Me.ButtonAdd_ExistingFile_Click, _
                ResourceTypeEditors.Audio)
            _categoryAudio.ResourceView = Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Thumbnail
            _categories.Add(_categoryAudio)

            _categoryFiles = New Category( _
                _categoryNameFiles, SR.GetString(SR.RSE_Cat_Files), _
                Category.Display.ListView, _
                New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeFiles, AddressOf MenuResourceTypeFiles), _
                AddressOf Me.ButtonAdd_ExistingFile_Click, _
                ResourceTypeEditors.TextFile, _
                ResourceTypeEditors.BinaryFile)
            _categoryFiles.ResourceView = Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Thumbnail
            _categories.Add(_categoryFiles)

            _categoryOther = New Category( _
                _categoryNameOther, SR.GetString(SR.RSE_Cat_Other), _
                Category.Display.StringTable, _
                New DesignerMenuCommand(Nothing, Constants.MenuConstants.CommandIDRESXResTypeOther, AddressOf MenuResourceTypeOther), _
                Nothing, _
                ResourceTypeEditors.NonStringConvertible, _
                ResourceTypeEditors.StringConvertible, _
                ResourceTypeEditors.Nothing)
            _categoryOther.ShowTypeColumnInStringTable = True
            _categories.Add(_categoryOther)
        End Sub

#End Region


#Region "Layout/Resize/Settings change"


        'CONSIDER: is there a Layout event?  Use that instead?
        ''' <summary>
        ''' Received when the resource editor view window is resized.  Cause a layout.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub OnResize(ByVal e As System.EventArgs)
            MyBase.OnResize(e)

            LayOutResourceEditor()
        End Sub



        ''' <summary>
        ''' Lays out the resource editor's controls.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub LayOutResourceEditor()
            Static InLayout As Boolean = False
            If InLayout Then
                Debug.Fail("Recursive call to LayoutResourceEditor")
                Exit Sub
            End If
            InLayout = True
            Try
                'String table and resource listview
                ' We leave some white space around them
                StringTable.Location = New Point(s_DESIGNER_PADDING, _toolbarPanel.Bottom + s_DESIGNER_PADDING_TOP)
                StringTable.Size = New Size(Me.Width - 2 * s_DESIGNER_PADDING, Me.Height - _toolbarPanel.Height - s_DESIGNER_PADDING - s_DESIGNER_PADDING_TOP)
                _resourceListView.Location = StringTable.Location
                _resourceListView.Size = StringTable.Size
            Finally
                InLayout = False
            End Try
        End Sub


        ''' <summary>
        ''' Receives broadcast messages passed on by the VS shell
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <param name="wParam"></param>
        ''' <param name="lParam"></param>
        ''' <remarks></remarks>
        Public Function OnBroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr) As Integer Implements Shell.Interop.IVsBroadcastMessageEvents.OnBroadcastMessage
            If msg = Editors.Interop.win.WM_SETTINGCHANGE Then
                If RootDesigner IsNot Nothing Then
                    SetFonts(RootDesigner)
                End If
            End If
        End Function


#End Region




#Region "Populate with data from the resources file"

        ''' <summary>
        ''' Display all resources from the specified category in the string table or list view.
        ''' </summary>
        ''' <param name="NewCategory">The category to show resources from.</param>
        ''' <remarks>Completely repopulates every time it's called, even if NewCategory is the current category.
        ''' Can be used to "refresh" the view.</remarks>
        Private Sub PopulateResources(ByVal NewCategory As Category)
            'Make sure current edits are saved
            CommitPendingChanges()

            Using (New WaitCursor)
                'Switch to the new category
                _currentCategory = NewCategory

                If _categoryLatchedCommandGroup IsNot Nothing Then
                    _categoryLatchedCommandGroup.Check(NewCategory.CommandToShow)
                End If

                'Clear old state

                '...First clear the old listview and stringtable items, if any, so we stop getting RetrieveVirtualItems events
                _resourceListView.Clear()
                StringTable.Clear()

                PropertyGridUnselectAll()

                'Now set up either a string table or a listview control, depending on what
                '  category we're going to be showing
                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        'Retrieve the default or last ResourceView used in this category.
                        _resourceListView.View = NewCategory.ResourceView

                        'Populate...
                        _resourceListView.Populate(_resourceFile, NewCategory)
                        StringTable.Visible = False
                        _resourceListView.Visible = True

                        If _buttonViewsCommand IsNot Nothing Then
                            _buttonViewsCommand.Enabled = True
                        End If

                        ' set focus correctly
                        _resourceListView.Select()
                        If _resourceListView.VirtualListSize > 0 Then
                            ' set FocusedItem to indicate the Focus
                            HighlightResource(_resourceListView.GetResourceFromVirtualIndex(0), True)
                        End If
                    Case Category.Display.StringTable
                        'Should the "Type" column be visible?
                        If NewCategory.ShowTypeColumnInStringTable Then
                            StringTable.TypeColumnVisible = True
                        Else
                            StringTable.TypeColumnVisible = False
                        End If

                        'Enable/disable user adding new rows
                        StringTable.AllowUserToAddRows = NewCategory.AllowNewEntriesInStringTable

                        'Populate...
                        StringTable.Populate(_resourceFile, NewCategory)
                        _resourceListView.Visible = False
                        If _buttonViewsCommand IsNot Nothing Then
                            _buttonViewsCommand.Enabled = False
                        End If
                        StringTable.Visible = True

                        ' set focus correctly
                        StringTable.Select()

                        'String table has issues with not selecting the cell at first (may be a bug)
                        PropertyGridUpdate()
                    Case Else
                        Debug.Fail("Unrecognized CategoryDisplay")
                End Select

                If _viewsLatchedCommandGroup IsNot Nothing Then
                    _viewsLatchedCommandGroup.Check(_currentCategory.ResourceView)
                End If
            End Using
        End Sub

#End Region


#Region "General UI"

        ''' <summary>
        ''' Changes the ResourceView for the current category.  Updates the display.
        ''' </summary>
        ''' <param name="View"></param>
        ''' <remarks></remarks>
        Private Sub ChangeResourceView(ByVal View As ResourceListView.ResourceView)
            If _currentCategory Is Nothing Then
                Debug.Fail("m_CurrentCategory Is Nothing")
                Exit Sub
            End If

            If _currentCategory.ResourceView <> View Then 'Only re-populate if it's different.
                _currentCategory.ResourceView = View
                PopulateResources(_currentCategory)
            End If
        End Sub

        ''' <summary>
        ''' Changes the ResourceView used in the given category.  This is stuff like "Thumbnails", "List",
        '''   "Icons".  If the given category is the current category, it updates the current view.  Otherwise
        '''   it just saves the view until the next time that category is displayed.
        ''' </summary>
        ''' <param name="Category">The category to change.</param>
        ''' <param name="View">The ResourceView to change to for that category.</param>
        ''' <remarks></remarks>
        Private Sub ChangeResourceViewForCategory(ByVal Category As Category, ByVal View As ResourceListView.ResourceView)
            Debug.Assert(_currentCategory IsNot Nothing)
            If Category Is _currentCategory Then
                ChangeResourceView(View)
            Else
                Category.ResourceView = View
            End If
        End Sub

        ''' <summary>
        ''' Change sort order used in the given category.
        ''' </summary>
        ''' <param name="CategoryUpdated">The category to change.</param>
        ''' <param name="Sorter"></param>
        ''' <remarks></remarks>
        Private Sub ChangeSorterForCategory(ByVal CategoryUpdated As Category, ByVal Sorter As IComparer)
            Debug.Assert(_currentCategory IsNot Nothing)
            If CategoryUpdated Is _currentCategory Then
                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        _resourceListView.RestoreSorter(Sorter)
                    Case Category.Display.StringTable
                        StringTable.RestoreSorter(Sorter)
                    Case Else
                        Debug.Fail("Unrecognized CategoryDisplay")
                End Select
            Else
                CategoryUpdated.Sorter = Sorter
            End If
        End Sub

        ''' <summary>
        ''' Commits all pending changes in the string table or listview.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub CommitPendingChanges()
            If _currentCategory Is Nothing Then
                'Ignore if this is fired before categories are set up (InitializeComponent)
                Exit Sub
            End If

            Select Case _currentCategory.CategoryDisplay
                Case Category.Display.ListView
                    _resourceListView.CommitPendingChanges()
                Case Category.Display.StringTable
                    StringTable.CommitPendingChanges()
                Case Else
                    Debug.Fail("Unrecognized CategoryDisplay")
            End Select
        End Sub

        ''' <summary>
        ''' Ping the toolbar panel so that it gets included in the search for
        ''' toolbars when translating accellerators/pressing shift-alt
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateToolbarFocus()
            If _toolbarPanel IsNot Nothing Then
                _toolbarPanel.Activate(Me.Handle)
            End If
        End Sub

        Friend Sub OnDesignerWindowActivated(ByVal activated As Boolean)
            If _accessModifierCombobox IsNot Nothing Then
                _accessModifierCombobox.OnDesignerWindowActivated(activated)
            End If
            If activated Then
                UpdateToolbarFocus()
            End If
        End Sub

        ''' <summary>
        ''' Retrieves the window that should be used as the owner of all dialogs and messageboxes.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetDialogOwnerWindow() As IWin32Window
            Dim UIService As IUIService = DirectCast(RootDesigner.GetService(GetType(IUIService)), IUIService)
            If UIService IsNot Nothing Then
                Return UIService.GetDialogOwnerWindow()
            Else
                Debug.Fail("Couldn't get IUIService")
                Return Nothing
            End If
        End Function


        ''' <summary>
        ''' Invalidates a given resource so taht it is redrawn on the next paint.
        ''' </summary>
        ''' <param name="Resource">The resource to invalidate</param>
        ''' <param name="InvalidateThumbnail">If True, the resource's thumbnail and other info is also invalidated.</param>
        ''' <remarks></remarks>
        Public Sub InvalidateResource(ByVal Resource As Resource, ByVal InvalidateThumbnail As Boolean)
            If _currentCategory IsNot Nothing Then
                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        Me._resourceListView.InvalidateResource(Resource, InvalidateThumbnail)
                    Case Category.Display.StringTable
                        StringTable.InvalidateResource(Resource)
                    Case Else
                        Debug.Fail("Unknown Display category!!!")
                End Select
            End If
        End Sub


        ''' <summary>
        ''' Displays a message box using the Visual Studio-approved manner.
        ''' </summary>
        ''' <param name="Message">The message text.</param>
        ''' <param name="Buttons">Which buttons to show</param>
        ''' <param name="Icon">the icon to show</param>
        ''' <param name="DefaultButton">Which button should be default?</param>
        ''' <param name="HelpLink">The help link</param>
        ''' <returns>One of the DialogResult values</returns>
        ''' <remarks></remarks>
        Public Overridable Function DsMsgBox(ByVal Message As String, _
                ByVal Buttons As MessageBoxButtons, _
                ByVal Icon As MessageBoxIcon, _
                Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                Optional ByVal HelpLink As String = Nothing) As DialogResult
            Return DesignerFramework.DesignerMessageBox.Show(DirectCast(Me.RootDesigner, IServiceProvider), Message, ResourceEditorView.s_messageBoxCaption, _
                Buttons, Icon, DefaultButton, HelpLink)
        End Function


        ''' <summary>
        ''' Displays a message box for an exception using the Visual Studio-approved manner.
        ''' </summary>
        ''' <param name="ex">The exception whose message we want to display.</param>
        ''' <remarks></remarks>
        Public Overridable Sub DsMsgBox(ByVal ex As Exception)
            DesignerFramework.DesignerMessageBox.Show(DirectCast(Me.RootDesigner, IServiceProvider), ex, ResourceEditorView.s_messageBoxCaption)
        End Sub


        ''' <summary>
        ''' Switch to the specified category, if it's not already the current category.
        ''' </summary>
        ''' <param name="Category"></param>
        ''' <remarks></remarks>
        Private Sub SwitchToCategory(ByVal Category As Category)
            If _currentCategory IsNot Nothing AndAlso _currentCategory IsNot Category Then
                PopulateResources(Category)
            End If
        End Sub


#End Region


#Region "IVsWindowPaneCommit implementation"
        ''' <summary>
        ''' This gets called before executing any menu command, and thus will allow us to commit on F5, 
        '''   editor close, etc.
        ''' </summary>
        ''' <param name="pfCommitFailed"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IVsWindowPaneCommit_CommitPendingEdit(ByRef pfCommitFailed As Integer) As Integer Implements IVsWindowPaneCommit.CommitPendingEdit
            CommitPendingChanges()
            pfCommitFailed = 0
            Return NativeMethods.S_OK
        End Function
#End Region


#Region "Selection"

        ''' <summary>
        ''' Gets the currently-selected Resources, whether we're showing a listview or
        '''   a string table.
        ''' </summary>
        ''' <returns>The list of resources selected.</returns>
        ''' <remarks>This is guaranteed to not return Nothing</remarks>
        Public Function GetSelectedResources() As Resource()
            'Current category can be Nothing if there was an error loading the resx, but the menus are enabled
            If _currentCategory IsNot Nothing Then
                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        Return _resourceListView.GetSelectedResources()
                    Case Category.Display.StringTable
                        Return StringTable.GetSelectedResources()
                    Case Else
                        Debug.Fail("Unexpected categorydisplay")
                End Select
            End If

            Return New Resource() {}
        End Function


        ''' <summary>
        ''' Unselects all resources in the current view.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub UnselectAllResources()
            CommitPendingChanges()

            If _currentCategory IsNot Nothing Then
                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        _resourceListView.UnselectAll()
                    Case Category.Display.StringTable
                        StringTable.UnselectAll()
                    Case Else
                        Debug.Fail("Unexpected CategoryDisplay")
                End Select
            End If
        End Sub


        ''' <summary>
        ''' Highlights (selects and ensures visible) the given set of resources.
        ''' </summary>
        ''' <param name="Resources">Resources to highlight</param>
        ''' <param name="SelectInPropertyGrid">If true, the property grid is updated with the new selection.</param>
        ''' <remarks></remarks>
        Public Sub HighlightResources(ByVal Resources As ICollection, ByVal SelectInPropertyGrid As Boolean)
            HighlightResourceHelper(Resources, 0, True, HighlightEntireResource:=True)
        End Sub


        ''' <summary>
        ''' This function selects and ensures that a single, given resource is visible and selected.  If possible
        '''   (if it's a string table), it highlights only the field specified.  Otherwise, it highlights the
        '''   entire resource.
        '''   It will change the currently-shown category and scroll the given resource into view, if
        '''   necessary.
        ''' </summary>
        ''' <param name="Resource">The Resource to highligh</param>
        ''' <param name="Field">The field in the resource's row to highlight.</param>
        ''' <param name="SelectInPropertyGrid">If true, update the property grid with the new selection</param>
        ''' <remarks></remarks>
        Public Sub HighlightResource(ByVal Resource As Resource, ByVal Field As FindReplace.Field, ByVal SelectInPropertyGrid As Boolean)
            If Resource IsNot Nothing Then
                HighlightResourceHelper(New Resource() {Resource}, Field, SelectInPropertyGrid, HighlightEntireResource:=False)
            End If
        End Sub


        ''' <summary>
        ''' This function selects and ensures that a single, given resource is visible and selected.
        '''   It will change the currently-shown category and scroll the given resource into view, if
        '''   necessary.
        ''' </summary>
        ''' <param name="Resource">The Resource to highligh</param>
        ''' <param name="SelectInPropertyGrid">If true, update the property grid with the new selection</param>
        ''' <remarks></remarks>
        Friend Sub HighlightResource(ByVal Resource As Resource, ByVal SelectInPropertyGrid As Boolean)
            If Resource IsNot Nothing Then
                HighlightResourceHelper(New Resource() {Resource}, 0, SelectInPropertyGrid, HighlightEntireResource:=True)
            End If
        End Sub


        ''' <summary>
        ''' Helper function for HighlightResource.
        ''' </summary>
        ''' <param name="Resources">The Resource to highligh</param>
        ''' <param name="Field">The field in the resource's row to highlight, if not HighlightEntireResource and if it's a stringtable.</param>
        ''' <param name="SelectInPropertyGrid">If true, update the property grid with the new selection</param>
        ''' <param name="HighlightEntireResource">If true, Field is ignored and the entire resource is highlighted.</param>
        ''' <remarks></remarks>
        Private Sub HighlightResourceHelper(ByVal Resources As ICollection, ByVal Field As FindReplace.Field, ByVal SelectInPropertyGrid As Boolean, ByVal HighlightEntireResource As Boolean)
            Dim NewCategory As Category = Nothing
            Dim ResourceCollection As New ArrayList()
            For Each Resource As Resource In Resources
                If _resourceFile.Contains(Resource) Then
                    Dim CategoryOfResource As Category = Resource.GetCategory(_categories)
                    If NewCategory Is Nothing Then
                        NewCategory = CategoryOfResource
                        ResourceCollection.Add(Resource)
                    ElseIf NewCategory Is CategoryOfResource Then
                        ResourceCollection.Add(Resource)
                    End If
                Else
                    Debug.Fail("HighlightResource: couldn't find resource")
                End If
            Next

            'Changing selections in the listview cause selection events, which causes property browser selection,
            '  which we don't want in such an uncontrolled manner.  So we disable that here and do it manually
            '  afterwards.
            Dim OldDontSelectInPropertyGrid As Boolean = _disablePropertyGridSelect
            _disablePropertyGridSelect = True
            Try

                If NewCategory IsNot Nothing Then
                    'Show that category if not already shown
                    SwitchToCategory(NewCategory)
                    Debug.Assert(NewCategory Is _currentCategory)
                End If

                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        _resourceListView.HighlightResources(ResourceCollection)
                    Case Category.Display.StringTable
                        If HighlightEntireResource OrElse ResourceCollection.Count <> 1 Then
                            StringTable.HighlightResources(ResourceCollection)
                        Else
                            StringTable.HighlightResource(DirectCast(ResourceCollection(0), Resource), Field)
                        End If
                    Case Else
                        Debug.Fail("Unexpected CategoryDisplay")
                End Select
            Finally
                _disablePropertyGridSelect = OldDontSelectInPropertyGrid
            End Try

            If SelectInPropertyGrid Then
                PropertyGridUpdate()
            End If
        End Sub


        ''' <summary>
        ''' Navigates to a particular resource.  I.e., makes sure the resx file
        '''   has focus, and highlights the given resource.  Used for instance
        '''   when the user double-clicks on an error in the task list.
        ''' </summary>
        ''' <param name="Resource">The resource to navigate to</param>
        ''' <remarks></remarks>
        Public Sub NavigateToResource(ByVal Resource As Resource)
            If ResourceFile.Contains(Resource) Then
                'First, make sure our resx file has focus
                Dim WindowFrame As IVsWindowFrame = CType(RootDesigner.GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
                If WindowFrame IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(WindowFrame.Show())
                Else
                    Debug.Fail("Couldn't get WindowFrame")
                End If

                UnselectAllResources()
                HighlightResource(Resource, True)
            Else
                Debug.Fail("Trying to navigate from a Resource which is no longer valid.")
            End If
        End Sub
#End Region


#Region "Integration with Visual Studio's Properties Window"

        Private _propertyGridNeedUpdate As Boolean


        ''' <summary>
        ''' Set the selection in the properties window to the currently-selected set of resources.
        ''' </summary>
        ''' <remarks>For performance reason, we post a message, and do a batch updating later</remarks>
        Public Sub PropertyGridUpdate()
            If Not _propertyGridNeedUpdate Then
                _propertyGridNeedUpdate = True
                Microsoft.VisualStudio.Editors.Interop.NativeMethods.PostMessage(Handle, Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_UPDATE_PROPERTY_GRID, 0, 0)
            End If
        End Sub

        ''' <summary>
        ''' Set the selection in the properties window to the currently-selected set of resources.
        '''  It is the real code to do updating when we receive the message posted by us.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub OnWmUpdatePropertyGrid()
            If _propertyGridNeedUpdate Then
                Try
                    PropertyGridSelect(GetSelectedResources())
                Finally
                    _propertyGridNeedUpdate = False
                End Try
            End If
        End Sub

        ''' <summary>
        '''  Select the specified resources using ISelectionService into the property grid.  Any resources
        '''    which were selected before this call and are not in the set of Resources to select will
        '''    be unselected.
        ''' </summary>
        ''' <param name="Resources">An array of Resource to be selected.</param>
        ''' <remarks>
        ''' </remarks>
        Private Sub PropertyGridSelect(ByVal Resources() As Resource)
            'CONSIDER: Perf optimization: do delay-adding of resources to the component list.  Currently we just add all of them all the time.  This would
            '  probably involve some changes to the undo code as well.
            Debug.Assert(Resources IsNot Nothing, "Cannot select Nothing!!!")

            RootDesigner.RefreshMenuStatus()

            If Not _disablePropertyGridSelect Then
                Try
                    ' Select the resources using ISelectionService.
                    Using New WaitCursor
                        Dim selectionService As ISelectionService = Me.RootDesigner.SelectionService

                        ' For performance reasons, we check whether the selection has changed, otherwise, we don't update it
                        Dim needUpdate As Boolean = True
                        If selectionService.SelectionCount = Resources.Length Then
                            needUpdate = False
                            For Each Resource As Resource In Resources
                                If Not selectionService.GetComponentSelected(Resource) Then
                                    needUpdate = True
                                    Exit For
                                End If
                            Next
                        End If
                        If needUpdate Then
                            selectionService.SetSelectedComponents(Resources, SelectionTypes.Replace)
                        End If
                    End Using
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Exception in property grid select: " & ex.ToString)
                End Try
            End If
        End Sub


        ''' <summary>
        ''' Unselects all resources from the property grid.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PropertyGridUnselectAll()
            PropertyGridSelect(New Resource() {})
        End Sub

        ''' <summary>
        ''' Called when the index is changed in the listview.  Update selection.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ResourceListView_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles _resourceListView.SelectedIndexChanged
            Try
                PropertyGridUpdate()
                RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=False)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail(ex.ToString)
            End Try
        End Sub


        ''' <summary>
        ''' Called when a row is selected/deselected in the string table.  Update selection.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>This handles the case when user select a row, and when we hightlight a row in search.</remarks>
        Private Sub StringTable_RowStateChanged(ByVal sender As Object, ByVal e As DataGridViewRowStateChangedEventArgs) _
                    Handles StringTable.RowStateChanged
            Try
                If e.StateChanged = DataGridViewElementStates.Selected Then
                    PropertyGridUpdate()
                    RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=False)
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail(ex.ToString)
            End Try
        End Sub


        ''' <summary>
        ''' Called when the user navigates to a different cell in the string table.  Update selection (we need this
        '''  because GetSelectedResources() can return the current cell as if it were a selected row).
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub StringTable_CellEnter(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles StringTable.CellEnter
            Try
                PropertyGridUpdate()
                RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=False)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail(ex.ToString)
            End Try
        End Sub


        ''' <summary>
        ''' Delegate for calling into HighlightResources.  Used by DelayedPropertyGridUpdate.
        ''' </summary>
        ''' <param name="Resources"></param>
        ''' <param name="SelectInPropertyGrid"></param>
        ''' <remarks></remarks>
        Private Delegate Sub HighlightResourcesDelegate(ByVal Resources As ICollection, ByVal SelectInPropertyGrid As Boolean)


        ''' <summary>
        ''' Updates the property grid in a delayed manner (essentially posts a message to itself to update at the next
        '''   convenient moment.  Can sometimes be necessary when things have really changed in the properties that are
        '''   being shown for a set of resources and simply doing PropertyGridUpdate() doesn't give Visual Studio enough
        '''   incentive, er, time, to notice the changes.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub DelayedPropertyGridUpdate()
            Dim SelectedResources() As Resource = GetSelectedResources()

            'Unselect all resources for now.  They will be re-selected after the delay.
            UnselectAllResources()

            'Do a control invoke to cause the delay.  This synchronizes through the message pump, so HighlightResources will
            '  be called with these arguments after all current messages have been dispatched.
            BeginInvoke(New HighlightResourcesDelegate(AddressOf HighlightResources), New Object() {SelectedResources, True})
        End Sub

#End Region

        ''' <summary>
        ''' Prevent the grid from going into edit mode unless we have checked out all the required files...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub StringTable_CellBeginEdit(ByVal sender As Object, ByVal e As DataGridViewCellCancelEventArgs) Handles StringTable.CellBeginEdit
            Try
                RootDesigner.DesignerLoader.ManualCheckOut()
            Catch ex As CheckoutException
                e.Cancel = True
            End Try
        End Sub

#Region "Adding/Removing resources"

        ''' <summary>
        ''' Given a set of filenames/paths, add those files into the resource editor as new resources, including adding
        '''   the files to the project if appropriate.
        ''' </summary>
        ''' <param name="SourceFileNames">The files (with paths) to add.</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <param name="AlwaysAddNew">If True, we always add new item. Or we will only update the old item. 
        '''   This will be ignored if CopyFileIfExists is True.</param>
        ''' <param name="AddToProject">If True, then files pointed to by linked resources will get added to the project (according to semantics defined for that project)</param>
        ''' <param name="FixInvalidIdentifiers">If true, resource names which are not programatically valid will be automatically fixed as ResGen would do it.</param>
        ''' <returns>An array of the resources that were added.</returns>
        ''' <remarks>Caller must catch and display exceptions</remarks>
        Public Function AddOrUpdateResourcesFromFiles(ByVal SourceFileNames() As String, ByVal CopyFileIfExists As Boolean, Optional ByVal AlwaysAddNew As Boolean = True, Optional ByVal AddToProject As Boolean = True, Optional ByVal FixInvalidIdentifiers As Boolean = False) As Resource()
            If SourceFileNames Is Nothing OrElse SourceFileNames.Length = 0 Then
                Return New Resource() {}
            End If

            Using New WaitCursor
                Using New ProjectBatchEdit(GetVsHierarchy())
                    'Create new resources for each file (don't add them to our ResourceFile yet)
                    Dim NewResources(SourceFileNames.Length - 1) As Resource
                    For FileIndex As Integer = 0 To SourceFileNames.Length - 1
                        NewResources(FileIndex) = CreateNewResourceFromFile(SourceFileNames(FileIndex))
                    Next

                    'Validate the resource names before we start adding them to the project
                    Resource.CheckResourceIdentifiers(Me.ResourceFile, NewResources, Fix:=FixInvalidIdentifiers, CheckForDuplicateNames:=False)

                    'Then add them to the editor.
                    If AddToProject AndAlso Not CopyFileIfExists AndAlso Not AlwaysAddNew Then
                        ' we will replace the file in the project with the external file, and don't add a new resource item if it is already there
                        Dim ResourcesReadyToAdd As New ArrayList(NewResources.Length) 'Resources which needed to be added
                        Dim OldResources As New ArrayList(NewResources.Length)
                        For i As Integer = 0 To NewResources.Length - 1
                            Dim Resource As Resource = NewResources(i)
                            Dim currentPath As String = Resource.AbsoluteLinkPathAndFileName
                            If File.Exists(currentPath) Then
                                Dim Message As String = String.Empty
                                Dim HelpID As String = String.Empty

                                ' The Resolution Service is needed when we verify the item
                                Resource.SetTypeResolutionContext(Me)

                                ' We should check whether the project can accept the item before adding the external file to the project
                                If Not IsValidResourseItem(Resource, Message, HelpID) Then
                                    DsMsgBox(SR.GetString(SR.RSE_Err_CantAddUnsupportedResource_1Arg, Resource.Name) & VB.vbCrLf & VB.vbCrLf & Message, MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpID)
                                    Continue For
                                End If

                                ' We try to add file to the project...
                                Dim FinalPathAndFileName As String = ResourcesFolderService.AddFileToProject( _
                                    SR.GetString(SR.RSE_ResourceEditor), _
                                    GetProject(), _
                                    GetResXProjectItem(), _
                                    GetDialogOwnerWindow(), _
                                    Resource.AbsoluteLinkPathAndFileName, _
                                    False)

                                If FinalPathAndFileName = "" Then
                                    'The user canceled (because a file already exists and s/he didn't want to replace it).
                                    'Continue to the next resource.
                                    Resource.Dispose()
                                Else
                                    'The file have been copied into the project.  We should check whether there is an old item linking to it
                                    If File.Exists(FinalPathAndFileName) Then
                                        Resource.SetLink(FinalPathAndFileName)
                                        If String.Compare(Resource.AbsoluteLinkPathAndFileName, currentPath, StringComparison.Ordinal) = 0 Then
                                            ' It was the file which was already in the project ... we always add a new item...
                                            ResourcesReadyToAdd.Add(Resource)
                                        Else
                                            Dim oldItem As Resource = _resourceFile.FindLinkResource(FinalPathAndFileName)
                                            If oldItem IsNot Nothing Then
                                                Resource.Dispose()
                                                OldResources.Add(oldItem)
                                            Else
                                                ResourcesReadyToAdd.Add(Resource)
                                            End If
                                        End If
                                    Else
                                        Debug.Fail("File was supposedly copied to project, but can't find the file at the new location: " & FinalPathAndFileName)
                                        'Skip this file.
                                        Resource.Dispose()
                                    End If
                                End If
                            Else
                                'The source file doesn't exist.  See if the user wants to continue or to cancel.
                                If DsMsgBox(SR.GetString(SR.RSE_Err_CantFindResourceFile_1Arg, Resource.AbsoluteLinkPathAndFileName) & VB.vbCrLf & VB.vbCrLf & SR.GetString(SR.RSE_Dlg_ContinueAnyway), MessageBoxButtons.YesNo, MessageBoxIcon.Error, , _
                                        HelpIDs.Err_CantFindResourceFile) _
                                        = DialogResult.No Then
                                    'User canceled - cancel entire add operation for all files.
                                    Return New Resource() {}
                                End If
                                ResourcesReadyToAdd.Add(Resource)
                            End If
                        Next
                        If ResourcesReadyToAdd.Count > 0 Then
                            'Note: we have already added the item to the project...
                            AddResources(ResourcesReadyToAdd, False, False)
                            OldResources.AddRange(ResourcesReadyToAdd)
                        End If

                        ' update selection...
                        UnselectAllResources()
                        HighlightResources(OldResources, True)

                        Return CType(OldResources.ToArray(GetType(Resource)), Resource())
                    Else
                        ' Always Add...
                        AddResources(NewResources, CopyFileIfExists, AddToProject)
                        Return NewResources
                    End If
                End Using

            End Using
        End Function


        ''' <summary>
        ''' Given a set of Resource instances that aren't in a ResourceFile yet, add them to our
        '''   ResourceFile and display them.  Also add the files to the project, if appropriate, and changes
        '''   the file they are point to to be the new file in the project (if the file was copied).
        ''' </summary>
        ''' <param name="NewResources">The resource instances to add.</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <remarks>
        ''' Forces all names to be unique, if necessary.
        ''' Caller must catch and display exceptions
        ''' </remarks>
        Public Sub AddResources(ByVal NewResources As ICollection, ByVal CopyFileIfExists As Boolean, ByVal AddToProject As Boolean)
            If NewResources Is Nothing OrElse NewResources.Count = 0 Then
                Exit Sub
            End If

            CommitPendingChanges()

            'Verify that all the resource names are valid before adding anything to the project.
            Resource.CheckResourceIdentifiers(Me.ResourceFile, NewResources, Fix:=False, CheckForDuplicateNames:=False)

            'Set up the type resolution context for all the resources in the list.  If they came from copy/paste or drag/drop, they
            '  won't have it set up yet, and we'll need that in order to make changes (like the link path)
            For Each Resource As Resource In NewResources
                Resource.SetTypeResolutionContext(Me)
                Resource.SetTypeNameConverter(Me.ResourceFile)
            Next

            ' Verify whether the resouce items are supported by current platform
            ' NOTE: some projects like device projects do not support TIFF files. We don't want the user to add them into the resource, and hit the issue at runtime.
            '  We validate resource here, because a tiff item could be added from an external file, or copy from another resouce editor...
            For Each NewResource As Resource In NewResources
                Dim Message As String = String.Empty
                Dim HelpID As String = String.Empty

                If Not IsValidResourseItem(NewResource, Message, HelpID) Then
                    DsMsgBox(SR.GetString(SR.RSE_Err_CantAddUnsupportedResource_1Arg, NewResource.Name) & VB.vbCrLf & VB.vbCrLf & Message, MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpID)
                    Return
                End If
            Next

            'Try to check out the resx file now if it needs checking out.  This keeps us from adding the
            '  file to the Resources directory only to have the resources not be added to the resx because
            '  checkout fails.  This throws an exception if it fails.
            RootDesigner.DesignerLoader.ManualCheckOut()

            Using New WaitCursor
                'Unselect all current items first
                UnselectAllResources()

                'Add the files pointed to by linked resources into the project.
                Dim ResourcesReadyToAdd As New ArrayList(NewResources.Count) 'Resources which weren't canceled by the user.
                Dim projectBuildBatch As ProjectBatchEdit = Nothing

                Try
                    For Each Resource As Resource In NewResources
                        If AddToProject AndAlso Resource.IsLink Then
                            If projectBuildBatch Is Nothing Then
                                ' We should start a batch process if necessary...
                                projectBuildBatch = New ProjectBatchEdit(GetVsHierarchy())
                            End If

                            If Not File.Exists(Resource.AbsoluteLinkPathAndFileName) Then
                                'The source file doesn't exist.  See if the user wants to continue or to cancel.
                                If DsMsgBox(SR.GetString(SR.RSE_Err_CantFindResourceFile_1Arg, Resource.AbsoluteLinkPathAndFileName) & VB.vbCrLf & VB.vbCrLf & SR.GetString(SR.RSE_Dlg_ContinueAnyway), MessageBoxButtons.YesNo, MessageBoxIcon.Error, , _
                                        HelpIDs.Err_CantFindResourceFile) _
                                        = DialogResult.No Then
                                    'User canceled - cancel entire add operation for all files.
                                    Exit Sub
                                Else
                                    'The user said to continue.  Go ahead and add the resource, but don't try to add the file to the project.
                                    ResourcesReadyToAdd.Add(Resource)
                                End If
                            Else
                                'Add the file to the project (if appropriate, according to the project)
                                Dim FinalPathAndFileName As String = ResourcesFolderService.AddFileToProject( _
                                    SR.GetString(SR.RSE_ResourceEditor), _
                                    GetProject(), _
                                    GetResXProjectItem(), _
                                    GetDialogOwnerWindow(), _
                                    Resource.AbsoluteLinkPathAndFileName, _
                                    CopyFileIfExists)

                                If FinalPathAndFileName = "" Then
                                    'The user canceled (because a file already exists and s/he didn't want to replace it).
                                    'Continue to the next resource.
                                Else
                                    'The file might have been copied into the project.  We need to update the link to point to the
                                    '  possibly new location.
                                    If File.Exists(FinalPathAndFileName) Then
                                        'Did the filename change?  (This will happen on copy if there was already a file in
                                        '  the Resources folder with that same name, etc.)
                                        '  If so, change the ID to match the new filename
                                        ' NOTE: we only do this if the resource name matches the filename.
                                        If Not Path.GetFileNameWithoutExtension(FinalPathAndFileName).Equals(Path.GetFileNameWithoutExtension(Resource.AbsoluteLinkPathAndFileName), _
                                                StringComparison.OrdinalIgnoreCase) AndAlso _
                                                Path.GetFileNameWithoutExtension(Resource.AbsoluteLinkPathAndFileName).Equals(Resource.Name, StringComparison.OrdinalIgnoreCase) _
                                        Then
                                            Resource.Name = Path.GetFileNameWithoutExtension(FinalPathAndFileName)
                                        End If

                                        Resource.SetLink(FinalPathAndFileName)
                                        ResourcesReadyToAdd.Add(Resource)
                                    Else
                                        Debug.Fail("File was supposedly copied to project, but can't find the file at the new location: " & FinalPathAndFileName)
                                        'Skip this file.
                                    End If
                                End If
                            End If
                        Else
                            'If non-linked (or if AddToProject = False), just add the resource to the list of resources to add to the ResourceFile
                            ResourcesReadyToAdd.Add(Resource)
                        End If
                    Next

                    'Force all names to be unique, both among themselves and within the ResourceFile

                    '... First, we create a case-insensitive hashtable with all the resource names in the ResourceFile
                    Dim ResourceNameTable As Hashtable = System.Collections.Specialized.CollectionsUtil.CreateCaseInsensitiveHashtable(ResourceFile.ResourcesHashTable)
                    '... Then we add to this list as we check for uniqueness and make name changes...
                    For Each Resource As Resource In ResourcesReadyToAdd
                        Dim NewResourceName As String = Resource.Name
                        Dim Append As Integer = 0
                        While ResourceNameTable.ContainsKey(NewResourceName)
                            'Munge the name and try again
                            Append += 1
                            NewResourceName = Resource.Name & Append
                        End While

                        'Rename the resource and add it to the name table
                        Resource.NameWithoutUndo = NewResourceName
                        ResourceNameTable.Add(NewResourceName, Resource)
                    Next

                    'And finally, add them to the resource file and our view.
                    'Do all the resource adds as a single transaction so it shows up as a single Undo/Redo
                    If ResourcesReadyToAdd.Count > 0 Then
                        Using Transaction As DesignerTransaction = RootDesigner.DesignerHost.CreateTransaction(SR.GetString(SR.RSE_Undo_AddResources_1Arg, CStr(ResourcesReadyToAdd.Count)))

                            'Add all our new resources to our internal list (but don't add them to the UI yet - that would cause too much flicker)
                            'This call will mark ourselves as dirty.
                            _resourceFile.AddResources(ResourcesReadyToAdd)

                            Transaction.Commit()
                        End Using

                        AddAndHighlightResourcesInView(ResourcesReadyToAdd)
                    End If

                Finally
                    If projectBuildBatch IsNot Nothing Then
                        projectBuildBatch.Dispose()
                    End If
                End Try

            End Using

        End Sub


        ''' <summary>
        ''' Given a set of resources which are in our ResourceFile but not displayed
        '''   in our view yet, add them to our view.
        ''' </summary>
        ''' <param name="NewResources">The resources to add to our display.</param>
        ''' <remarks></remarks>
        Private Sub AddAndHighlightResourcesInView(ByVal NewResources As IList)
            If NewResources Is Nothing OrElse NewResources.Count = 0 Then
                Exit Sub
            End If

            CommitPendingChanges()

            'If there are a lot of resources (and they can be in different categories), we don't want 
            '  to cause a lot of UI flicker by continuing scrolling the view to the new resource
            '  and/or swapping categories for each resource.  
            '  We also want to ensure that all the new resources appear at the bottom of the
            '  string table or listview, highlighted.  To accomplish this, we first figure out
            '  which category we want to end up displaying at the end (we must pick one from the
            '  categories represented in the new resources), then we switch to that category, and
            '  then we add the resources in that category all at once.

            'If none of the new resources were added to the current category, we'll switch to one of the
            '  categories that did have resources added.  Otherwise we prefer to stick with the current
            '  category.
            Dim SwitchToDifferentCategory As Boolean = True
            For Each Resource As Resource In NewResources
                Debug.Assert(_resourceFile.Contains(Resource), "Resource should have been added to resource file beforehand")
                If Resource.GetCategory(_categories) Is _currentCategory Then
                    SwitchToDifferentCategory = False
                    Exit For
                End If
            Next
            If SwitchToDifferentCategory Then
                SwitchToCategory(DirectCast(NewResources(0), Resource).GetCategory(_categories))
            End If

            'Figure out which resources are in the currently (possibly switched category)
            Dim NewResourcesInCurrentCategory As New ArrayList
            For Each Resource As Resource In NewResources
                If Resource.GetCategory(_categories) Is _currentCategory Then
                    NewResourcesInCurrentCategory.Add(Resource)
                End If
            Next

            If SwitchToDifferentCategory Then
                'Since we switched to another category, the view was populated with all resources in that
                '  category, including the ones in NewResources (since they already exist in the ResourceFile).
                '  So, there's no need to try to add them to the current view - they're already there now.

                'Nothing to do.
            Else
                'Since we didn't switch categories, the resources in NewResources haven't been added to the
                '  current listview or stringtable.  Add them now.

                'Now add the new resources in bulk to the current grid or listview, and highlight them.
                Select Case _currentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        _resourceListView.AddResources(NewResourcesInCurrentCategory)
                    Case Category.Display.StringTable
                        StringTable.AddResources(NewResourcesInCurrentCategory)
                    Case Else
                        Debug.Fail("Unexpected CategoryDisplay")
                End Select
            End If

            'Go ahead and highlight the resources that are in the current category
            HighlightResources(NewResourcesInCurrentCategory, True)
        End Sub


        ''' <summary>
        ''' Removes a set of resources from the ResourceFile.
        ''' </summary>
        ''' <param name="Resources">The resources to remove.</param>
        ''' <remarks></remarks>
        Public Sub RemoveResources(ByVal Resources As ICollection)
            CommitPendingChanges()

            If Resources Is Nothing OrElse Resources.Count = 0 Then
                Exit Sub
            End If

            Try
                'Make sure we can check out the resx file first.
                RootDesigner.DesignerLoader.ManualCheckOut()

                'First, remove all resources from the table or list that belong in the current category.  Need to
                '  do this first to ensure we don't have the virtual listview trying to get info on missing resources.
                Dim ResourcesInCurrentCategory As New ArrayList
                For Each Resource As Resource In Resources
                    If Resource.GetCategory(_categories) Is _currentCategory Then
                        ResourcesInCurrentCategory.Add(Resource)
                    End If
                Next
                RemoveResourcesFromView(ResourcesInCurrentCategory)

                'Then remove them from the ResourceFile.
                'Do the resource removes as a single transaction so it shows up as a single Undo/Redo
                Using Transaction As DesignerTransaction = RootDesigner.DesignerHost.CreateTransaction(SR.GetString(SR.RSE_Undo_RemoveResources_1Arg, CStr(Resources.Count)))

                    'Finally, remove the resources from the resource file and also dispose of them
                    For Each Resource As Resource In Resources
                        'This call will mark the designer as dirty.
                        _resourceFile.RemoveResource(Resource, DisposeResource:=True)
                    Next

                    Transaction.Commit()
                End Using
            Catch ex As Exception
                DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Removes a set of resources from the currently-displayed table or listview, if they're
        '''   in it.  Does not remove them from the ResourceFile.
        ''' </summary>
        ''' <param name="Resources"></param>
        ''' <remarks></remarks>
        Private Sub RemoveResourcesFromView(ByVal Resources As IList)
            CommitPendingChanges()

            Select Case _currentCategory.CategoryDisplay
                Case Category.Display.ListView
                    _resourceListView.RemoveResources(Resources)
                Case Category.Display.StringTable
                    StringTable.RemoveResources(Resources)
                Case Else
                    Debug.Fail("Unrecognized categorydisplay")
            End Select

            UnselectAllResources()
        End Sub


        ''' <summary>
        ''' This is called when resources have been added to our ResourceFile "behind our
        '''   backs (i.e., by the Undo engine).  We simply need to add the resources
        '''   to our view, since they're in the ResourceFile but not yet displayed.
        ''' </summary>
        ''' <param name="Resource">The resource which has been added.</param>
        ''' <remarks></remarks>
        Public Sub OnResourceAddedExternally(ByVal Resource As Resource)
            AddAndHighlightResourcesInView(New Resource() {Resource})
            OnResourceTouched(Resource)
        End Sub


        ''' <summary>
        ''' This is called when resources have been removed from our ResourceFile "behind our
        '''   backs (i.e., by the Undo engine).  We simply need to remove any of these
        '''   which is currently in our view.
        ''' </summary>
        ''' <param name="Resource">The resource which has been removed.</param>
        ''' <remarks></remarks>
        Public Sub OnResourceRemovedExternally(ByVal Resource As Resource)
            If _inUndoing Then
                Dim resourceCategory As Category = Resource.GetCategory(_categories)
                If resourceCategory IsNot _categoryAffected Then
                    _categoryAffected = resourceCategory
                    If _resourcesAffected IsNot Nothing Then
                        _resourcesAffected.Clear()
                    End If
                ElseIf _resourcesAffected IsNot Nothing Then
                    _resourcesAffected.Remove(Resource)
                End If
            End If

            'CONSIDER: need better flicker-free method?
            RemoveResourcesFromView(New Resource() {Resource})
        End Sub

        ''' <summary>
        ''' This is called when resources have been updated
        ''' </summary>
        ''' <param name="Resource">The resource which has been renamed.</param>
        ''' <remarks></remarks>
        Friend Sub OnResourceTouched(ByVal Resource As Resource)
            If _inUndoing Then
                Dim resourceCategory As Category = Resource.GetCategory(_categories)
                If resourceCategory IsNot _categoryAffected Then
                    _categoryAffected = resourceCategory
                    If _resourcesAffected IsNot Nothing Then
                        _resourcesAffected.Clear()
                    Else
                        _resourcesAffected = New Hashtable()
                    End If
                    _resourcesAffected.Add(Resource, Nothing)
                ElseIf Not _resourcesAffected.Contains(Resource) Then
                    _resourcesAffected.Add(Resource, Nothing)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Given a file and path, create a single resource from it.  Do not add it to a 
        '''   ResourceFile, and do not add the file to the project.
        ''' </summary>
        ''' <param name="ResourceSourceFilePath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CreateNewResourceFromFile(ByVal ResourceSourceFilePath As String) As Resource
            'We don't bother trying to make the name unique among those in ResourceFile, because we'll do that in AddResources.
            Dim ResourceName As String = GetResourceNameFromFileName(ResourceSourceFilePath)

            'Figure out which type editor to use.
            Dim Extension As String = IO.Path.GetExtension(ResourceSourceFilePath)
            Dim TypeEditor As ResourceTypeEditor = GetResourceTypeEditorForFileExtension(Extension)
            If TypeEditor Is Nothing Then
                Debug.Fail("Why didn't the binary file type editor pick up this file extension?  It should handle everything.")
                Throw New NotSupportedException
            End If

            Dim ResourceTypeName As String = TypeEditor.GetDefaultResourceTypeName(_resourceFile)
            Debug.Assert(ResourceTypeName <> "", "GetDefaultResourceTypeName() didn't return anything")
            Dim NewResource As Resource = New Resource(_resourceFile, ResourceName, Nothing, ResourceSourceFilePath, ResourceTypeName, Me)
            Debug.Assert(NewResource.ResourceTypeEditor.Equals(TypeEditor))

            Return NewResource
        End Function


        ''' <summary>
        ''' Given a filename, returns an ID based on that name
        ''' </summary>
        ''' <param name="FileName">The filename to base the resource name on.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GetResourceNameFromFileName(ByVal FileName As String) As String
            Return Path.GetFileNameWithoutExtension(GetFileNameInActualCase(FileName))
        End Function


        ''' <summary>
        ''' Given a filename extension, determines which Resource Type Editor is the preferred editor to
        '''  handle this file extension.  This essentially determines what type of resource to expect
        '''  based on the file extension (which is the right thing to do - Visual Studio is fuly based
        '''  on file extensions).
        ''' </summary>
        ''' <param name="Extension">The filename extension (including the period prefix, e.g. ".txt") to check</param>
        ''' <returns>The ResourceTypeEditor found, or Nothing if none found</returns>
        ''' <remarks></remarks>
        Private Function GetResourceTypeEditorForFileExtension(ByVal Extension As String) As ResourceTypeEditor
            Debug.Assert(Extension = "" OrElse Extension.Chars(0) = "."c)

            'Query each known resource type editor one by one to ask if it handles this resource type, and at
            '  what priority.  The type editor with the highest priority wins.  If multiple type editors have the
            '  same priority, an arbitrary one is picked.
            Dim HighestPriority As Integer = ResourceTypeEditor.ExtensionPriorities.NotHandled
            Dim MatchingEditor As ResourceTypeEditor = Nothing
            For Each Category As Category In _categories
                For Each Editor As ResourceTypeEditor In Category.AssociatedResourceTypeEditors
                    Try
                        Dim Priority As Integer = Editor.GetExtensionPriority(Extension)
                        If Priority > 0 AndAlso Priority >= HighestPriority Then
                            Debug.Assert(MatchingEditor Is Nothing OrElse HighestPriority <> Priority, "Warning: multiple resource type editors returned the same priority for extension " & Extension)
                            MatchingEditor = Editor
                            HighestPriority = Priority
                            Exit For
                        End If
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        Debug.WriteLine("GetResourceTypeEditorForFileExtension: Exception thrown: " & ex.Message)
                        'Swallow the error and continue to the next candidate resource type editor
                    End Try
                Next
            Next

            Return MatchingEditor
        End Function

        ''' <summary>
        ''' Checks to see whether we can add a resource to this file
        ''' </summary>
        ''' <remarks></remarks>
        Private Function IsValidResourseItem(ByVal NewResource As Resource, ByRef Message As String, ByRef HelpID As String) As Boolean
            Return NewResource.ResourceTypeEditor.IsResourceItemValid(NewResource, _resourceFile, Message, HelpID)
        End Function

#End Region


#Region "Drag-drop, Cut, Copy, Cancel edit, Delete and Paste Menu Commands"

        ''' <summary>
        ''' Enabled handler for the Paste menu.  Determines if the menu item should be enabled or not.
        ''' </summary>
        ''' <param name="MenuCommand">The Paste menu.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuPasteEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            If ReadOnlyMode OrElse IsInEditing Then
                Return False        ' if one editBox is activated, let it handle this...
            End If

            Dim ActualEffect As DragDropEffects
            Dim ActualFormat As ResourceEditorDataFormats
            Dim Supported As Boolean = DataFormatSupported(Clipboard.GetDataObject, DragDropEffects.Copy, ActualEffect, ActualFormat)

            If Supported AndAlso ActualFormat = ResourceEditorDataFormats.WindowsExplorer Then
                'If this is a drop from Windows Explorer, we want to verify that there are no directories in the list.  If there
                '  are, we disable the paste.  We probably wouldn't do what the user would expect when dropping a folder onto the
                '  resource editor (we can't create folders under Resources), so best to disable this scenario.
                Dim FileNames() As String = DirectCast(Clipboard.GetDataObject.GetData(DataFormats.FileDrop), String())
                For Each FileName As String In FileNames
                    If Directory.Exists(FileName) Then
                        'Found a directory - disable
                        Return False
                    End If
                Next
            End If

            Return Supported
        End Function

        ''' <summary>
        ''' The remove row command should only be enabled if we are currently
        ''' showing the string table, and there is 1 or more cells selected
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuRemoveRowEnabledHandler(ByVal command As DesignerMenuCommand) As Boolean
            If ReadOnlyMode Then
                Return False
            End If

            If CurrentCategory.CategoryDisplay = Category.Display.StringTable Then
                command.Visible = True
                For Each c As DataGridViewCell In StringTable.SelectedCells
                    If c.RowIndex < StringTable.RowCountVirtual Then
                        Return True
                    End If
                Next
                Return False
            Else
                command.Visible = False
                Return False
            End If
        End Function

        ''' <summary>
        ''' Returns true iff the copy commands should be enabled
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuCopyShouldBeEnabled() As Boolean
            If IsInEditing Then
                ' we should let the textBox to handle everything....
                Return False
            End If

            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay = Category.Display.StringTable AndAlso Not StringTable.InLineSelectionMode Then
                Dim cells As DataGridViewSelectedCellCollection = StringTable.SelectedCells
                If cells.Count = 1 Then
                    Dim cell As DataGridViewCell = cells(0)
                    If cell.ColumnIndex = ResourceStringTable.COLUMN_VALUE Then
                        Dim Resource As Resource = StringTable.GetResourceFromRowIndex(cell.RowIndex, True)
                        If Resource IsNot Nothing AndAlso Not (TypeOf Resource.ResourceTypeEditor Is ResourceTypeEditorString) Then
                            Return False
                        End If
                    End If

                    Dim value As Object = cell.Value
                    If value IsNot Nothing Then
                        Try
                            Return (Not String.IsNullOrEmpty(value.ToString()))
                        Catch ex As Exception
                        End Try
                    End If
                End If
                Return False
            End If

            Dim SelectedResources() As Resource = GetSelectedResources()
            Return SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0
        End Function

        ''' <summary>
        ''' Returns true iff the delete commands should be enabled
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuDeleteShouldBeEnabled() As Boolean
            If ReadOnlyMode OrElse IsInEditing Then
                ' we should let the textBox to handle everything....
                Return False
            End If

            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay = Category.Display.StringTable AndAlso Not StringTable.InLineSelectionMode Then
                Dim cells As DataGridViewSelectedCellCollection = StringTable.SelectedCells
                If cells.Count = 0 Then
                    Return False
                End If

                For Each cell As DataGridViewCell In cells
                    If cell.ReadOnly OrElse cell.ColumnIndex = ResourceStringTable.COLUMN_NAME OrElse cell.RowIndex >= StringTable.RowCountVirtual Then   ' We can not remove name field
                        Return False
                    End If
                    If cell.ColumnIndex = ResourceStringTable.COLUMN_VALUE Then
                        Dim Resource As Resource = StringTable.GetResourceFromRowIndex(cell.RowIndex, True)
                        If Resource IsNot Nothing AndAlso Not (TypeOf Resource.ResourceTypeEditor Is ResourceTypeEditorString) Then
                            Return False
                        End If
                    End If
                Next
                Return True
            End If

            Dim SelectedResources() As Resource = GetSelectedResources()
            Return SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0
        End Function

        ''' <summary>
        ''' Enabled handler for the cmdidDelete command
        ''' </summary>
        ''' <param name="menucommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuDeleteEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return Not ReadOnlyMode AndAlso MenuDeleteShouldBeEnabled()
        End Function

        ''' <summary>
        ''' Visible handler for the cmdidRemove command
        ''' </summary>
        ''' <param name="menucommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuDeleteVisibleHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay = Category.Display.StringTable
        End Function

        ''' <summary>
        ''' Enabled handler for the cmdidRemove command
        ''' </summary>
        ''' <param name="menucommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuRemoveEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            ' we should let the textBox to handle everything when IsInEditing = True
            Return Not IsInEditing AndAlso RemoveButtonShouldBeEnabled()
        End Function

        ''' <summary>
        ''' Visible handler for the cmdidRemove command
        ''' </summary>
        ''' <param name="menucommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuRemoveVisibleHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay <> Category.Display.StringTable
        End Function

        ''' <summary>
        ''' Enabled handler for the Cut menus.  Determines if the menu items should be enabled or not.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuCutEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return Not ReadOnlyMode AndAlso MenuCopyShouldBeEnabled() AndAlso MenuDeleteShouldBeEnabled()
        End Function

        ''' <summary>
        ''' Enabled handler for the Copy menus.  Determines if the menu items should be enabled or not.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuCopyEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return MenuCopyShouldBeEnabled()
        End Function

        ''' <summary>
        ''' The Cancel Edit command is never enabled. 
        ''' </summary>
        ''' <param name="menucommand">Ignored</param>
        ''' <returns>False</returns>
        ''' <remarks>
        ''' We never enable this command because we are currently trying to commit all pending edits in our 
        ''' IVsWindowPaneCommit_CommitPendingEdit implementation, which means that we'll try to commit the broken cell before
        ''' our command handler will be executed. By registering this command with the ESC keybinding, and always disable it,
        ''' we basically unbind the keyboard shortcut and let the DataGridView do it's built-in thing (which happens to be the 
        ''' right thing :)            
        ''' </remarks>
        Private Function MenuCancelEditEnableHandler(ByVal menucommand As DesignerMenuCommand) As Boolean
            Return False
        End Function

        ''' <summary>
        ''' Cancel the current edit
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>See MenuCancelEditEnableHandler as to why this should never be enabled</remarks>
        Private Sub MenuCancelEdit(ByVal sender As Object, ByVal e As EventArgs)
            Debug.Fail("We should never enable the CancelEdit command - we should let the datagrid do it's work!")
        End Sub

        ''' <summary>
        ''' Enables or disables buttons as appropriate according to the current
        '''   selection state
        ''' </summary>
        ''' <remarks></remarks>
        Private Function RemoveButtonShouldBeEnabled() As Boolean
            If ReadOnlyMode Then
                ' we should let the textBox to handle everything....
                Return False
            Else
                Dim SelectedResources() As Resource = GetSelectedResources()
                Return SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0
            End If
        End Function


        ''' <summary>
        ''' Enabled handler for the Rename menus.  Determines if the menu items should be enabled or not.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuRenameEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay = Category.Display.ListView Then
                MenuCommand.Visible = True
                Return Not ReadOnlyMode AndAlso GetSelectedResources().Length = 1
            Else
                MenuCommand.Visible = False
                Return False
            End If
        End Function

        Private Function MenuGenericRemoveEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return RemoveButtonShouldBeEnabled()
        End Function

        Private Sub MenuGenericRemove(ByVal sender As Object, ByVal e As EventArgs)
            RemoveResources(GetSelectedResources())
        End Sub

        ''' <summary>
        ''' Handles the Remove menu command
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MenuRemove(ByVal sender As Object, ByVal e As EventArgs)
            If CurrentCategory.CategoryDisplay = Category.Display.StringTable AndAlso Not StringTable.InLineSelectionMode Then
                ClearCells()
            Else
                RemoveResources(GetSelectedResources())
            End If
        End Sub

        ''' <summary>
        ''' Clear cells in StringTable
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ClearCells()
            If CurrentCategory.CategoryDisplay <> Category.Display.StringTable Then
                Return
            End If

            Dim cells As DataGridViewSelectedCellCollection = StringTable.SelectedCells
            If cells.Count = 0 Then
                Return
            End If

            Try
                'Make sure we can check out the resx file first.
                RootDesigner.DesignerLoader.ManualCheckOut()

                Using Transaction As DesignerTransaction = RootDesigner.DesignerHost.CreateTransaction(SR.GetString(SR.RSE_Undo_DeleteResourceCell, cells.Count))
                    StringTable.ClearSelectedCells()
                    Transaction.Commit()
                End Using
            Catch ex As Exception
                DsMsgBox(ex)
            End Try
        End Sub

        ''' <summary>
        ''' Handle the remove row command
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuRemoveRow(ByVal sender As Object, ByVal e As EventArgs)
            RemoveResources(GetSelectedResources())
        End Sub


        ''' <summary>
        ''' Handles the Rename menu command
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuRename(ByVal sender As Object, ByVal e As EventArgs)
            Dim SelectedResources() As Resource = GetSelectedResources()
            If SelectedResources.Length = 1 Then
                _resourceListView.BeginLabelEdit(SelectedResources(0))
            End If
        End Sub


        ''' <summary>
        ''' Handles the Cut menu command
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuCut(ByVal sender As Object, ByVal e As EventArgs)
            MenuCopy(sender, e)
            MenuRemove(sender, e)
        End Sub


        ''' <summary>
        ''' Handles the Copy menu command
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuCopy(ByVal sender As Object, ByVal e As EventArgs)
            If CurrentCategory.CategoryDisplay = Category.Display.StringTable AndAlso Not StringTable.InLineSelectionMode Then
                Dim cells As DataGridViewSelectedCellCollection = StringTable.SelectedCells
                If cells.Count = 1 Then
                    Dim cell As DataGridViewCell = cells(0)
                    CopyValueToClipboard(cell.Value)
                End If
            Else
                CopyResourcesToClipboard(GetSelectedResources())
            End If
        End Sub


        ''' <summary>
        ''' Handles the Paste menu command
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MenuPaste(ByVal sender As Object, ByVal e As EventArgs)
            Dim ActualEffect As DragDropEffects
            Dim ActualFormat As ResourceEditorDataFormats

            'Get data from the Clipboard
            Dim Data As IDataObject = Clipboard.GetDataObject

            '... and paste
            If DataFormatSupported(Data, DragDropEffects.Copy, ActualEffect, ActualFormat) Then
                Try
                    DragDropPaste(Data, DragDropEffects.Copy, ActualEffect:=ActualEffect)
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    DsMsgBox(ex)
                End Try
            End If
        End Sub

#End Region


#Region "Drag-drop, Cut, Copy and Paste Menu Implementation"


#Region "Clipboard Formats"

        ''' <summary>
        ''' An enum for the clipboard formats supported by the resource editor.
        ''' </summary>
        ''' <remarks></remarks>
        Private Enum ResourceEditorDataFormats
            Resources 'Our own internal format, used between instances of the resource editor and intra-editor
            SolutionExplorer 'Visual Studio solution explorer
            WindowsExplorer
            Csv 'Comma-separated values (Excel and others)
            UnicodeText 'Tab-separated values (Notepad etc)
        End Enum


        '**** WARNING:
        '  
        '  Windows apparently has a limit to the size of clipboard format names, so we need to make sure we 
        '  don't make them too long, or you'll get strange sporadic failures
        '

        'Gets a unique drag-drop, copy/paste data format for the resource editor.  This format is visible across all instances of the
        '  resource editor in any project (as long as they are the same version - can't copy between two different versions of 
        '  Visual Studio, because the format name is qualified with the assembly's version.
        Private ReadOnly _CF_RESOURCES As String = "MS.VS.Editors.Resources " + GetType(ResourceEditorDataFormats).Assembly.GetName().Version.ToString()

        'Gets a unique drag-drop, copy/paste data format for the resource editor that is visible only between the same
        '  instance of the resource editor.  To do this, we simply append the HashCode of this resource editor's view instance
        '  to the format name.  Other instances of the resource editor will use a different format name because of the
        '  hashcode, and therefore won't see this format.
        Private ReadOnly _CF_RESOURCES_THIS_INSTANCE As String = _CF_RESOURCES & "," & CStr(Me.GetHashCode())

        'Clipboard formats for dragging from the Visual Studio solution explorer

        'This drag/drop format is used by reference-based projects (e.g., C++), where the items in a project are a reference to a file elsewhere on
        '  disk.  The project item is not actually stored in the project itself.
        Private Const s_CF_VSREFPROJECTITEMS As String = "CF_VSREFPROJECTITEMS"

        'This drag/drop format is used by storage-based projects (e.g., C#, VB, J#), where the items in a project are generally stored in the project's
        '  directories rather than simply being references.
        Private Const s_CF_VSSTGPROJECTITEMS As String = "CF_VSSTGPROJECTITEMS"

        'Comma-separated values (Excel and other apps)
        Private ReadOnly _CF_CSV As String = DataFormats.CommaSeparatedValue

        'Unicode text (tab-separated values) for Notepad and other apps
        Private ReadOnly _CF_UNICODE As String = DataFormats.UnicodeText

#End Region

#Region "Private class - ResourcesDataFormat - clipboard format for intra-editor and between resource editor instances"

        ''' <summary>
        ''' Clipboard format for intra-editor and between resource editor instances.  Essentially just a list of
        '''   serialized Resources.
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Private NotInheritable Class ResourcesDataFormat
            'List of resources
            Private _resources() As Resource

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Resources"></param>
            ''' <remarks></remarks>
            Public Sub New(ByVal Resources() As Resource)
                _resources = Resources
            End Sub

            ''' <summary>
            ''' Retrives the list of resources stored in this format.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Resources() As Resource()
                Get
                    Return _resources
                End Get
            End Property

        End Class

#End Region

#Region "Copy sourcing"

        ''' <summary>
        ''' Given a set of Resources, creates a data object containing that set of resources in all
        '''   formats that we support as a source.
        ''' </summary>
        ''' <param name="Resources">The set of resources</param>
        ''' <returns>A data object with our supported source formats.</returns>
        ''' <remarks></remarks>
        Private Function CreateDataObjectFromResources(ByVal Resources() As Resource) As IDataObject
            Dim Data As New DataObject

            '1) Create a structure with our raw resources data (our preferred format)
            Dim ResourcesData As New ResourcesDataFormat(Resources)

            '... then package it into a serialized blob
            Dim Formatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            Dim Stream As New MemoryStream
            Formatter.Serialize(Stream, ResourcesData)

            '... and stuff into a DataObject
            Stream.Seek(0, SeekOrigin.Begin)
            Dim Bytes(CInt(Stream.Length) - 1) As Byte
            Stream.Read(Bytes, 0, CInt(Stream.Length))
            Data.SetData(_CF_RESOURCES, False, Bytes)


            '2) Visual Studio solution explorer formats
            '
            'We don't actually add source the solution explorer formats - the solution explorer 
            '  consumes the Windows Explorer file drop format, so that's good enough
            '  for our needs as far as sourcing goes (when the Solution explorer is the
            '  source, it doesn't give us the file drop format, so we'll have to understand
            '  its native formats as a target in DataFormatSupported)


            '3) Windows Explorer format (file drop)
            'This is just a list of filenames.  For non-linked resources, we export them to a temporary 
            '  file.  We only support items that can be saved to a file.
            'We make a copy of linked resources as well, because if you drag from resource editor to Windows
            '  explorer, Windows will think "move" and try to delete the original source files.  I can't do 
            '  anything about this except to make a temporary copy so that it doesn't matter.
            Dim ResourceFileNames As New ArrayList
            Dim TempFolder As String = Nothing
            For Each Resource As Resource In Resources
                If TempFolder = "" Then
                    TempFolder = GetTemporarySubfolder()
                End If

                'Save all the resources' files into a temporary folder.
                Dim TempFileName As String = ""
                Try
                    If Resource.IsLink Then
                        'Linked file - copy the target of the link to a temp
                        TempFileName = GetTemporaryFile( _
                            AutoDelete.DeleteOnClipboardFlush, _
                            ParentDirectory:=TempFolder, _
                            PreferredFileName:=Path.GetFileName(Resource.AbsoluteLinkPathAndFileName))
                        File.Copy(Resource.AbsoluteLinkPathAndFileName, TempFileName)

                        'Make sure the file is not read-only, or Windows might ask the user if they're sure they
                        '  want to move a read-only file.
                        Dim TempFileInfo As New FileInfo(TempFileName)
                        TempFileInfo.Attributes = DirectCast(TempFileInfo.Attributes And (-1 Xor FileAttributes.ReadOnly), FileAttributes)
                    Else
                        If Resource.ResourceTypeEditor.CanSaveResourceToFile(Resource) Then
                            'Non-linked.  Export the resource to a temp.
                            TempFileName = GetTemporaryFile( _
                                AutoDelete.DeleteOnClipboardFlush, _
                                ParentDirectory:=TempFolder, _
                                PreferredFileName:=CreateLegalFileName(Resource.Name) _
                                    & Resource.ResourceTypeEditor.GetResourceFileExtension(Resource))
                            Resource.ResourceTypeEditor.SaveResourceToFile(Resource, TempFileName)
                        Else
                            'There was an error retrieving the value for the resource.  Skip it and move on to the next.
                        End If
                    End If

                    If TempFileName <> "" Then
                        ResourceFileNames.Add(TempFileName)
                    End If
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)

                    'Swallow the exception and move on to the next resource
                    Debug.WriteLine("Failed trying to copy linked or non-linked resource " & Resource.Name & " to temporary file " & TempFileName & " - ignoring and moving to next resource" _
                        & VB.vbCrLf & ex.Message)
                End Try
            Next
            'Build a list of filenames out of the saved/exported files.
            If ResourceFileNames.Count > 0 Then
                Dim FileNamesArray(ResourceFileNames.Count - 1) As String
                For i As Integer = 0 To ResourceFileNames.Count - 1
                    FileNamesArray(i) = DirectCast(ResourceFileNames(i), String)
                Next

                '... and add to the data format as the FileDrop format.
                Data.SetData(DataFormats.FileDrop, False, FileNamesArray)
            End If


            ' 4) CSV (for Excel and other apps)
            ' 
            '  This is for text-convertible resources only
            Dim CsvText As String = CsvEncoder.EncodeResources(Resources, CsvEncoder.EncodingType.Csv)
            If CsvText <> "" Then
                Data.SetData(_CF_CSV, True, CsvText)
            End If


            ' 5) UnicodeText (tab-delimited) (for NotePad and other apps)
            ' 
            '  This is for text-convertible resources only
            Dim UnicodeText As String = CsvEncoder.EncodeResources(Resources, CsvEncoder.EncodingType.TabDelimited)
            If UnicodeText <> "" Then
                Data.SetData(_CF_UNICODE, True, UnicodeText)
            End If


            ' 6) We place this format on the clipboard so we can tell when we get a drop
            '      whether the data came from this instance of the resource editor or not.
            '      Since we have a different name for this format for each editor instance,
            '      we can tell it came from us simply by querying for our version of this format's
            '      name.
            Data.SetData(_CF_RESOURCES_THIS_INSTANCE, False, True)

            'And we're done
            Return Data
        End Function


        ''' <summary>
        ''' Copies a set of resources to the clipboard in all formats sourced by us.
        ''' </summary>
        ''' <param name="Resources">The resources to copy</param>
        ''' <remarks></remarks>
        Private Sub CopyResourcesToClipboard(ByVal Resources() As Resource)
            'The old temporary files from the last clipboard or drag/drop operation should no longer be needed - delete them now
            DeleteTemporaryFiles(_deleteFilesOnClipboardFlush)
            _deleteFilesOnClipboardFlush.Clear()

            'The False argument here means that the data will *not* be available after this instance of the resource
            '  editor is shut down (that would require us to keep around temporary files for non-linked resources
            '  that were copied into the Windows Explorer file drop format)
            Clipboard.SetDataObject(CreateDataObjectFromResources(Resources), True)
        End Sub

        ''' <summary>
        ''' Copies a single string to the clipboard in text format
        ''' </summary>
        ''' <param name="value">The value to copy</param>
        ''' <remarks></remarks>
        Private Sub CopyValueToClipboard(ByVal value As Object)
            'The old temporary files from the last clipboard or drag/drop operation should no longer be needed - delete them now
            DeleteTemporaryFiles(_deleteFilesOnClipboardFlush)
            _deleteFilesOnClipboardFlush.Clear()

            If value IsNot Nothing Then
                Clipboard.SetText(value.ToString)
            Else
                Clipboard.Clear()
            End If
        End Sub

#End Region

#Region "Drag/drop"

        ''' <summary>
        ''' Given whether copy or move is allowed in the current drag/drop, determine which we will
        '''   actually do, depending on those settings plus whether the CTRL, SHIFT and ALT keys are
        '''   pressed or not.
        ''' </summary>
        ''' <param name="CopyAllowed">Whether a copy is currently allowed.</param>
        ''' <param name="MoveAllowed">Whether a move is currently allowed.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function DetermineCopyOrMove(ByVal CopyAllowed As Boolean, ByVal MoveAllowed As Boolean) As DragDropEffects
            'Shift pressed - want to move
            If (Control.ModifierKeys And Keys.Shift) <> 0 AndAlso MoveAllowed Then
                Return DragDropEffects.Move
            End If

            'Ctrl pressed - want to copy
            If (Control.ModifierKeys And Keys.Control) <> 0 AndAlso CopyAllowed Then
                Return DragDropEffects.Copy
            End If

            'If both move and copy are allowed, and the user hasn't pressed either shift or control, then default to
            '  move.
            If MoveAllowed Then
                Return DragDropEffects.Move
            ElseIf CopyAllowed Then
                Return DragDropEffects.Copy
            End If

            Return DragDropEffects.None
        End Function


        ''' <summary>
        ''' Given a Data object during drag/drop, plus the currently-allowed copy/move effects,
        '''   determine which format and copy/move we would choose if the user were to 
        '''   drop the data now.
        ''' </summary>
        ''' <param name="Data">The Data object from the drag/drop source</param>
        ''' <param name="AllowedEffects">Allowed effects for this drag (determined by the source)</param>
        ''' <param name="ActualEffect">[Out] The copy/move effect that we choose</param>
        ''' <param name="ActualFormat">[Out] The format that we choose.</param>
        ''' <returns>True if we were able to select a format to copy or move</returns>
        ''' <remarks></remarks>
        Private Function DataFormatSupported(ByVal Data As IDataObject, ByVal AllowedEffects As DragDropEffects, ByRef ActualEffect As DragDropEffects, ByRef ActualFormat As ResourceEditorDataFormats) As Boolean
            Dim CopyAllowed As Boolean = (AllowedEffects And DragDropEffects.Copy) <> 0
            Dim MoveAllowed As Boolean = (AllowedEffects And DragDropEffects.Move) <> 0

            If Not CopyAllowed AndAlso Not MoveAllowed Then
                Return False
            End If

            '***
            '*** Formats must be checked here in order of decreasing preference.
            '***

            '1) Preferred format: our internal ResourcesDataFormat format
            If (CopyAllowed OrElse MoveAllowed) AndAlso Data.GetDataPresent(_CF_RESOURCES) Then
                'Allow copy or move
                ActualEffect = DetermineCopyOrMove(CopyAllowed, MoveAllowed)
                ActualFormat = ResourceEditorDataFormats.Resources
                Return True
            End If

            '2) Project explorer drag/drop format (copy only)
            If CopyAllowed AndAlso _
                    (Data.GetDataPresent(s_CF_VSREFPROJECTITEMS) OrElse Data.GetDataPresent(s_CF_VSSTGPROJECTITEMS)) Then
                ActualEffect = DragDropEffects.Copy
                ActualFormat = ResourceEditorDataFormats.SolutionExplorer 'Note that we don't care if it was CF_VSSTGPROJECTITEMS or CF_VSREFPROJECTITEMS
                Return True
            End If

            '3) Drag/drop from Windows Explorer (copy only)
            If CopyAllowed AndAlso Data.GetDataPresent(DataFormats.FileDrop) Then
                ActualEffect = DragDropEffects.Copy
                ActualFormat = ResourceEditorDataFormats.WindowsExplorer
                Return True
            End If

            '4) UnicodeText format (intended for Notepad etc)
            ' We also pick up the UnicodeText first for EXCEL, because the CSV doesn't support unicode characters. (vswhidbey 587937)
            If (CopyAllowed OrElse MoveAllowed) AndAlso Data.GetDataPresent(_CF_UNICODE) Then
                'Allow copy or move
                ActualEffect = DetermineCopyOrMove(CopyAllowed, MoveAllowed)
                ActualFormat = ResourceEditorDataFormats.UnicodeText
                Return True
            End If

            '5) CSV format
            If (CopyAllowed OrElse MoveAllowed) AndAlso Data.GetDataPresent(_CF_CSV) Then
                'Allow copy or move
                ActualEffect = DetermineCopyOrMove(CopyAllowed, MoveAllowed)
                ActualFormat = ResourceEditorDataFormats.Csv
                Return True
            End If

            Return False
        End Function


        ''' <summary>
        ''' Handles setting the drag/drop effect for drag/drop events
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub SetDragDropEffect(ByVal e As DragEventArgs)
            Dim ActualFormat As ResourceEditorDataFormats
            Call DataFormatSupported(e.Data, e.AllowedEffect, e.Effect, ActualFormat)
        End Sub


        ''' <summary>
        ''' Called when the mouse is moved over our view during a drag/drop operation
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDragOver(ByVal e As System.Windows.Forms.DragEventArgs)
            MyBase.OnDragOver(e)
            SetDragDropEffect(e)
        End Sub


        ''' <summary>
        ''' Called when the mouse is moved into our view during a drag/drop operation
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDragEnter(ByVal e As System.Windows.Forms.DragEventArgs)
            MyBase.OnDragEnter(e)
            Me.RootDesigner.DesignerHost.Activate()
            UnselectAllResources()
            SetDragDropEffect(e)
        End Sub

        ''' <summary>
        ''' Handles a drag/drop drop.
        ''' </summary>
        ''' <remarks>Upon return of this function, e.Effect must be set to the action that was actually taken</remarks>
        Protected Overrides Sub OnDragDrop(ByVal e As System.Windows.Forms.DragEventArgs)
            MyBase.OnDragDrop(e)

            Try
                DragDropPaste(e.Data, e.AllowedEffect, ActualEffect:=e.Effect)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                DsMsgBox(ex)
                e.Effect = DragDropEffects.None
            Finally
                'We don't ever want to return DragDropEffects.None as the result action.  Why?  Because if we do,
                '  Visual Studio thinks that we aren't interested in the drop, and it captures the drop itself (and opens up
                '  the files in editor windows).  We don't want this to happen, even if our drop failed, so we'll dummy up
                '  a fake effect here - Copy should be safe in that most sources won't do anything additional for it - and return 
                '  that instead.
                If e.Effect = DragDropEffects.None Then
                    e.Effect = DragDropEffects.Copy
                End If
            End Try
        End Sub


        ''' <summary>
        ''' Occurs when the user begins dragging an item in the listview.
        '''   We use this to start sourcing a drag/drop operation from the listview.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ResourceListView_ItemDrag(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemDragEventArgs) Handles _resourceListView.ItemDrag
            Dim ResourcesToDrag() As Resource = GetSelectedResources()
            Dim Data As IDataObject = CreateDataObjectFromResources(ResourcesToDrag)
            Dim EffectThatTookPlace As DragDropEffects = _resourceListView.DoDragDrop(Data, DragDropEffects.Copy Or DragDropEffects.Move)
            If (EffectThatTookPlace And DragDropEffects.Move) <> 0 Then
                'The data was moved.  The target should have already copied the data.  Now we need to actually Remove the resources
                '  that were involved in the move, to complete the action.
                RemoveResources(ResourcesToDrag)
            End If
        End Sub


#End Region

#Region "Drag/drop and paste dual implementation"

        ''' <summary>
        ''' Handles a drop or a paste of a Data object.
        ''' </summary>
        ''' <param name="Data">The data object from the source</param>
        ''' <param name="AllowedEffects">The drag/drop effects which allowed (set to Copy to do a Paste)</param>
        ''' <param name="ActualEffect">The actual effect which took place (copy or move)</param>
        ''' <remarks>Caller must catch exceptions and display error message</remarks>
        Private Sub DragDropPaste(ByVal Data As IDataObject, ByVal AllowedEffects As DragDropEffects, ByRef ActualEffect As DragDropEffects)
            Dim ActualFormat As ResourceEditorDataFormats
            If Not DataFormatSupported(Data, AllowedEffects, ActualEffect, ActualFormat) Then
                'No data that we're interested in.
                ActualEffect = DragDropEffects.None
                Exit Sub
            End If

            RootDesigner.DesignerLoader.ManualCheckOut()    ' we should check-out before doing any change...

            Using New WaitCursor
                Try
                    Select Case ActualFormat
                        Case ResourceEditorDataFormats.Resources
                            DragDropPasteFromResourceEditor(Data, True, ActualEffect)
                        Case ResourceEditorDataFormats.WindowsExplorer
                            DragDropPasteFromWindowsExplorer(Data, False)
                        Case ResourceEditorDataFormats.SolutionExplorer
                            DragDropPasteFromSolutionExplorer(Data, False)
                        Case ResourceEditorDataFormats.Csv
                            DragDropPasteFromCsv(Data)
                        Case ResourceEditorDataFormats.UnicodeText
                            DragDropPasteFromUnicodeText(Data)
                        Case Else
                            Debug.Fail("Unexpected format")
                            ActualEffect = DragDropEffects.None
                    End Select
                Catch ex As Exception
                    ActualEffect = DragDropEffects.None
                    Throw
                End Try
            End Using
        End Sub


        ''' <summary>
        ''' Handles a drop or a paste of a Data object using the solution explorer formats
        ''' </summary>
        ''' <param name="Data">The data object from the source</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <remarks>Caller must catch exceptions and display error message</remarks>
        Private Sub DragDropPasteFromSolutionExplorer(ByVal Data As IDataObject, ByVal CopyFileIfExists As Boolean)
            'Look for either CF_VSREFPROJECTITEMS or CF_VSSTGPROJECTITEMS
            Dim DataStream As Stream = DirectCast(Data.GetData(s_CF_VSREFPROJECTITEMS), Stream)
            If DataStream Is Nothing Then
                DataStream = DirectCast(Data.GetData(s_CF_VSSTGPROJECTITEMS), Stream)
            End If

            If DataStream IsNot Nothing Then
                Dim FileInfos() As DraggedFileInfo = GetFileListFromVsHDropHandle(DataStream)
                Dim FileNames(FileInfos.Length - 1) As String
                For i As Integer = 0 To FileInfos.Length - 1
                    FileNames(i) = FileInfos(i).FilePath
                Next

                'FixInvalidIdentifiers:=True because we want to fix invalid identifiers in the copy/paste and drag/drop scenarios
                AddOrUpdateResourcesFromFiles(FileNames, CopyFileIfExists, CopyFileIfExists, FixInvalidIdentifiers:=True)
            End If
        End Sub


        ''' <summary>
        ''' Handles a drop or a paste of a Data object using the Windows Explorer format
        ''' </summary>
        ''' <param name="Data">The data object from the source</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <remarks>Caller must catch exceptions and display error message</remarks>
        Private Sub DragDropPasteFromWindowsExplorer(ByVal Data As IDataObject, ByVal CopyFileIfExists As Boolean)
            Dim FileNames() As String = DirectCast(Data.GetData(DataFormats.FileDrop), String())

            'Filter out any directories and ignore them.  We probably wouldn't do what the user would expect when 
            '  dropping a folder onto the resource editor (we can't create folders under Resources), so best to 
            '  disable this scenario.  For drag/drop, we couldn't actually disable the drop (because the VS shell
            '  has the file drop supported style), so the best we can do is ignore any directories when they are
            '  dropped.
            Dim FilteredFileNames As New ArrayList
            For Each FileName As String In FileNames
                If Directory.Exists(FileName) Then
                    'Directory - ignore
                Else
                    FilteredFileNames.Add(FileName)
                End If
            Next
            FilteredFileNames.CopyTo(FileNames, 0)
            ReDim Preserve FileNames(FilteredFileNames.Count - 1)

            'FixInvalidIdentifiers:=True because we want to fix invalid identifiers in the copy/paste and drag/drop scenarios
            AddOrUpdateResourcesFromFiles(FileNames, CopyFileIfExists, CopyFileIfExists, FixInvalidIdentifiers:=True)
        End Sub



        ''' <summary>
        ''' Handles a drop or a paste of a Data object using the CSV format (strings only)
        ''' </summary>
        ''' <param name="Data">The data object from the source</param>
        ''' <remarks>Caller must catch exceptions and display error message</remarks>
        Private Sub DragDropPasteFromCsv(ByVal Data As IDataObject)
            Dim CsvText As String = GetTextFromIDataObject(Data, _CF_CSV)

            ' Special case: We will try to paste the string to one cell if only one cell is selected in the string table...
            If CurrentCategory.CategoryDisplay = Category.Display.StringTable AndAlso _
                    StringTable.CurrentCell IsNot Nothing AndAlso _
                    StringTable.CurrentCell.Selected AndAlso _
                    StringTable.SelectedCells.Count = 1 AndAlso _
                    Not StringTable.CurrentCell.ReadOnly AndAlso _
                    Not StringTable.CurrentCell.OwningRow.ReadOnly Then

                Dim simpleString As String = String.Empty
                If CsvEncoder.IsSimpleString(CsvText, CsvEncoder.EncodingType.Csv, simpleString) Then
                    StringTable.PasteStringToCurrentCell(simpleString)
                    Return
                End If
            End If

            Dim Resources() As Resource = CsvEncoder.DecodeResources(CsvText, Me, CsvEncoder.EncodingType.Csv)
            AddResources(Resources, CopyFileIfExists:=False, AddToProject:=False)
        End Sub


        ''' <summary>
        ''' Handles a drop or a paste of a Data object using the UnicodeText format (strings only)
        ''' </summary>
        ''' <param name="Data">The data object from the source</param>
        ''' <remarks>Caller must catch exceptions and display error message</remarks>
        Private Sub DragDropPasteFromUnicodeText(ByVal Data As IDataObject)
            Dim Text As String = GetTextFromIDataObject(Data, _CF_UNICODE)

            'Special case: We will try to paste the string to one cell if only one cell is selected in the string table...
            If CurrentCategory.CategoryDisplay = Category.Display.StringTable AndAlso _
                    StringTable.CurrentCell IsNot Nothing AndAlso _
                    StringTable.CurrentCell.Selected AndAlso _
                    StringTable.SelectedCells.Count = 1 AndAlso _
                    Not StringTable.CurrentCell.ReadOnly AndAlso _
                    Not StringTable.CurrentCell.OwningRow.ReadOnly Then

                Dim simpleString As String = String.Empty
                If CsvEncoder.IsSimpleString(Text, CsvEncoder.EncodingType.TabDelimited, simpleString) Then
                    StringTable.PasteStringToCurrentCell(simpleString)
                    Return
                End If
            End If

            Dim Resources() As Resource = CsvEncoder.DecodeResources(Text, Me, CsvEncoder.EncodingType.TabDelimited)
            AddResources(Resources, CopyFileIfExists:=False, AddToProject:=False)
        End Sub


        ''' <summary>
        ''' Decodes text data from an IDataObject
        ''' </summary>
        ''' <param name="Data"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetTextFromIDataObject(ByVal Data As IDataObject, ByVal ClipboardFormat As String) As String
            Dim RawData As Object = Data.GetData(ClipboardFormat)
            Dim Text As String = ""
            If TypeOf RawData Is String Then
                Text = DirectCast(RawData, String)
            ElseIf TypeOf RawData Is MemoryStream Then
                Dim StreamReader As New StreamReader(DirectCast(RawData, MemoryStream), System.Text.Encoding.Default)
                Text = StreamReader.ReadToEnd()
                If Text.Length > 0 AndAlso Text(Text.Length - 1) = VB.Chr(0) Then
                    'Remove trailing null byte
                    Text = Text.Substring(0, Text.Length - 1)
                End If
            End If

            Return Text
        End Function


        ''' <summary>
        ''' Handles a drop or a paste of a Data object using the Resource Editor private formats
        ''' </summary>
        ''' <param name="Data">The data object from the source</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <param name="ActualEffect">The actual effect which took place (copy or move)</param>
        ''' <remarks>Caller must catch exceptions and display error message</remarks>
        Private Sub DragDropPasteFromResourceEditor(ByVal Data As IDataObject, ByVal CopyFileIfExists As Boolean, ByRef ActualEffect As DragDropEffects)
            If Data.GetDataPresent(_CF_RESOURCES_THIS_INSTANCE) AndAlso ActualEffect = DragDropEffects.Move Then
                'We are trying to move resources from this resource editor instance to itself.  There's really nothing for 
                '  us to do in this case.
                ActualEffect = DragDropEffects.None
                Return
            End If

            'Decode the data format
            Dim RawBytes() As Byte = DirectCast(Data.GetData(_CF_RESOURCES), Byte())
            Dim MemoryStream As New MemoryStream(RawBytes)
            Dim Formatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            Dim ResourcesData As ResourcesDataFormat = DirectCast(Formatter.Deserialize(MemoryStream), ResourcesDataFormat)

            'Okay, we have our copied resources, let's add them
            AddResources(ResourcesData.Resources, CopyFileIfExists, AddToProject:=True)
        End Sub


        ''' <summary>
        ''' Represents a single file drop info from a drag/drop initiated from the Visual Studio solution explorer
        ''' </summary>
        ''' <remarks></remarks>
        Private Structure DraggedFileInfo
            Public FilePath As String
            Public Guid As String
            Public ProjectFile As String
        End Structure


        ''' <summary>
        ''' Given an HDROP from the Visual Studio solution explorer (CF_VSREFPROJECTITEMS or CF_VSSTGPROJECTITEMS), this
        '''   parses the information from the drop and returns it.
        ''' </summary>
        ''' <param name="HDropStream"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetFileListFromVsHDropHandle(ByVal HDropStream As Stream) As DraggedFileInfo()
            Dim Files() As DraggedFileInfo
            Const MAX_PATH As Integer = 260
            Const FILEINFO_LENGTH As Integer = 2 * MAX_PATH + 41     ' FILEINFO include two pathes and a GUID


            If HDropStream.Length <= Integer.MaxValue Then
                Dim StreamLength As Integer = CInt(HDropStream.Length)
                Dim hDrop As IntPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(StreamLength)
                Dim hDropHandleRef As New HandleRef(Nothing, hDrop)
                Try
                    Dim RawData(StreamLength - 1) As Byte
                    HDropStream.Read(RawData, 0, StreamLength)
                    System.Runtime.InteropServices.Marshal.Copy(RawData, 0, hDrop, RawData.Length)

                    'Get the number of files in the drop structure
                    Dim Count As Integer = Editors.Interop.NativeMethods.DragQueryFile(hDropHandleRef.Handle, -1, Nothing, 0)
                    If Count > 0 Then
                        ReDim Files(Count - 1)

                        For i As Integer = 0 To Count - 1
                            Dim FileNameAndInfo As String = VB.StrDup(FILEINFO_LENGTH, VB.Chr(0))
                            Dim CharLen As Integer = Editors.Interop.NativeMethods.DragQueryFile(hDropHandleRef.Handle, i, FileNameAndInfo, FileNameAndInfo.Length)
                            If FileNameAndInfo.Length < CharLen Then
                                ' Alloc enough space and give another try...
                                FileNameAndInfo = VB.StrDup(CharLen + 1, VB.Chr(0))
                                CharLen = Editors.Interop.NativeMethods.DragQueryFile(hDropHandleRef.Handle, i, FileNameAndInfo, FileNameAndInfo.Length)
                            End If
                            If FileNameAndInfo.Length > CharLen Then
                                FileNameAndInfo = FileNameAndInfo.Substring(0, CharLen)
                            End If

                            'The Vs project file drop info contains entries that look like this:
                            '
                            '   {6D0F06C6-1EA7-4AED-89EF-CC60E26E1015}|WindowsApplication157\WindowsApplication157.vbproj|c:\temp\windowsapplication157\windowsapplication157\prairie wind.bmp
                            '
                            'We need to parse this stuff out

                            Dim SplitInfo As String() = FileNameAndInfo.Split("|"c)
                            Dim Info As New DraggedFileInfo
                            With Info
                                .Guid = SplitInfo(0)
                                .ProjectFile = SplitInfo(1)
                                .FilePath = SplitInfo(2)
                            End With
                            Files(i) = Info
                        Next

                        Return Files
                    End If
                Finally
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(hDrop)
                End Try
            End If

            ReDim Files(-1)
            Return Files
        End Function

#End Region


#End Region


#Region "Play"


        ''' <summary>
        ''' Enabled handler for the Play menu command.  Determines if the menu item should be enabled/visible.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item for Play.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuPlayEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            'Play should be only be visible in the Audio category
            If _currentCategory IsNot Nothing AndAlso _currentCategory.ProgrammaticName.Equals(_categoryNameAudio, StringComparison.OrdinalIgnoreCase) Then
                MenuCommand.Visible = True
            Else
                MenuCommand.Visible = False
                Return False
            End If

            Return CanPlayResources(GetSelectedResources())
        End Function


        ''' <summary>
        ''' Given a set of resources, determines if they can be played as audio.
        ''' </summary>
        ''' <param name="Resources"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CanPlayResources(ByVal Resources() As Resource) As Boolean
            'The must be exactly one resource selected
            If Resources Is Nothing OrElse Resources.Length <> 1 Then
                Return False
            End If

            'Finally, it must use the audio resource type editor
            If Not Resources(0).ResourceTypeEditor.Equals(ResourceTypeEditors.Audio) Then
                Return False
            End If

            Return True
        End Function


        ''' <summary>
        ''' Handler for the "Play" menu command
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MenuPlay(ByVal sender As Object, ByVal e As EventArgs)
            Dim SelectedResources As Resource() = GetSelectedResources()
            If Not CanPlayResources(SelectedResources) Then
                Exit Sub
            End If

            Try
                Dim SoundResource As Resource = SelectedResources(0)
                Dim Player As System.Media.SoundPlayer
                Dim AudioStream As MemoryStream = Nothing

                If SoundResource.IsLink Then
                    Player = New System.Media.SoundPlayer(SoundResource.AbsoluteLinkPathAndFileName)
                Else
                    Dim SoundResourceValue As Object = SoundResource.GetValue()
                    If TypeOf SoundResourceValue Is Byte() Then
                        AudioStream = New MemoryStream(DirectCast(SoundResourceValue, Byte()))
                    ElseIf TypeOf SoundResourceValue Is MemoryStream Then
                        AudioStream = DirectCast(SoundResourceValue, MemoryStream)

                        'Need to seek to the beginning.  With our streams we always assume we want the full data in the stream.
                        AudioStream.Seek(0, SeekOrigin.Begin)
                    Else
                        Debug.Fail("Unexpected resource type for audio")
                        Return
                    End If

                    Player = New System.Media.SoundPlayer(AudioStream)
                End If

                Player.Play()

                'The player leaves the stream at the end.  Seek back to the beginning to avoid possible future problems using the same stream.
                If AudioStream IsNot Nothing Then
                    AudioStream.Seek(0, SeekOrigin.Begin)
                End If

            Catch ex As Exception
                DsMsgBox(SR.GetString(SR.RSE_Err_CantPlay_1Arg, ex.Message), MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_CantPlay)
            End Try
        End Sub

#End Region


#Region "Open/Open With"


        ''' <summary>
        ''' Occurs on a double-click of an item (or ENTER).  We respond with our default action, Menu.Open for most items, or witih
        '''   Play for audio items.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ResourceListView_ItemActivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles _resourceListView.ItemActivate
            If CanPlayResources(GetSelectedResources()) Then
                MenuPlay(sender, e)
            Else
                MenuOpen(sender, e)
            End If
        End Sub


        ''' <summary>
        ''' Handler for the "Open" menu command
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MenuOpen(ByVal sender As Object, ByVal e As EventArgs)
            Try
                For Each Resource As Resource In GetSelectedResources()
                    EditOrOpenWith(Resource, UseOpenWithDialog:=False)
                Next
            Catch ex As UserCanceledException
            Catch ex As Exception
                DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Handler for the "Open With..." menu command
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MenuOpenWith(ByVal sender As Object, ByVal e As EventArgs)
            Try
                For Each Resource As Resource In GetSelectedResources()
                    EditOrOpenWith(Resource, UseOpenWithDialog:=True)
                Next
            Catch ex As UserCanceledException
            Catch ex As Exception
                DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Enabled handler for the Open/Open With menu commands.  Determines if the menu item should be enabled.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item for Open or Open/With.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuOpenOpenWithEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            'Open/Open With should be shown only in the listview mode
            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay <> Category.Display.ListView Then
                MenuCommand.Visible = False
                Return False
            Else
                MenuCommand.Visible = True
            End If

            'It should be enabled only if there is at least one resource selected
            Dim SelectedResources() As Resource = GetSelectedResources()
            Return SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0
        End Function


        ''' <summary>
        ''' Does an "Open" or "Open With" operation on a resource.
        ''' </summary>
        ''' <param name="Resource">The Resource to open</param>
        ''' <param name="UseOpenWithDialog">True if an Open With is intended, otherwise does Open.</param>
        ''' <remarks>
        ''' The Open command asks Visual Studio to open the default editor based on the file extension.  The
        '''   user can set the default from the Open With dialog (either brought up by the resource editor
        '''   or from the solution explorer).
        ''' The Open With command shows all editors registered for this file extension.  Everything here
        '''   is based on Visual Studio's interpretation of file extension.  Nothing magic.
        ''' This function does *not* show a messagebox for errors.  The caller should do that.
        ''' </remarks>
        Private Sub EditOrOpenWith(ByVal Resource As Resource, ByVal UseOpenWithDialog As Boolean)
            If Resource Is Nothing Then
                Debug.Fail("OnResourceEditorOrOpenWith: Resource should not be nothing")
                Return
            End If

            If Not Resource.IsLink Then
                'It's a non-linked resource.  Ask the user if s/he wants to change it to linked so we can
                '  open it.
                If DsMsgBox(SR.GetString(SR.RSE_Err_CantEditEmbeddedResource), MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2, HelpIDs.Dlg_OpenEmbedded) = DialogResult.OK Then
                    'User said yes.
                    Resource.PersistenceMode = ResourceEditor.Resource.ResourcePersistenceMode.Linked
                    Debug.Assert(Resource.IsLink)
                Else
                    'User said No, don't change it.
                    Exit Sub
                End If
            End If

            Dim ResourceFilePath As String = Resource.AbsoluteLinkPathAndFileName
            Debug.Assert(Resource.AbsoluteLinkPathAndFileName <> "")
            Dim ResourceFullPathTolerant As String = Common.GetFullPathTolerant(ResourceFilePath)
            Debug.Assert(ResourceFullPathTolerant <> "")

            'Before we actually try opening the file, let's see if it still exists.  If we try to open a non-existing file,
            '  we can get a nasty, unfriendly COM exception.
            If Not File.Exists(ResourceFilePath) Then
                DsMsgBox(SR.GetString(SR.RSE_Err_CantFindResourceFile_1Arg, ResourceFilePath), MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_CantFindResourceFile)
                Exit Sub
            End If

            ' Pop up a Warning message if it could be unsafe to open the item in an external editor
            If Not UseOpenWithDialog Then
                Dim fileExtension As String = Path.GetExtension(ResourceFilePath)
                If Not String.IsNullOrEmpty(fileExtension) Then
                    Dim isSafe As Boolean = False

                    ' first of all, we check the white list for the resource type. If the extension matches one of them, we won't pop up a warning dialog.
                    Dim safeList As String() = Resource.ResourceTypeEditor.GetSafeFileExtensionList()
                    If safeList IsNot Nothing Then
                        For Each safeExtension As String In safeList
                            If String.Equals(fileExtension, safeExtension, StringComparison.OrdinalIgnoreCase) Then
                                isSafe = True
                                Exit For
                            End If
                        Next
                    End If

                    If Not isSafe Then
                        ' Check the registry setting, which remembers a list of extensions that the customer told us not to pop up a warning dialog again...
                        Dim extraSafeExtensions As String = SafeExtensions
                        If Not String.IsNullOrEmpty(extraSafeExtensions) Then
                            Dim extensions As String() = extraSafeExtensions.Split(New Char() {s_SAFE_EXTENSION_SEPERATOR})
                            For Each safeExtension As String In extensions
                                If String.Equals(fileExtension, safeExtension, StringComparison.OrdinalIgnoreCase) Then
                                    isSafe = True
                                    Exit For
                                End If
                            Next
                        End If

                        If Not isSafe Then
                            ' we should pop a warning dialog now...
                            Using warningDialog As New OpenFileWarningDialog(RootDesigner.DesignerHost, ResourceFullPathTolerant)
                                If warningDialog.ShowDialog() <> DialogResult.OK Then
                                    Exit Sub
                                End If

                                ' If the customer doesn't want to be asked for that extension again, we should remember that...
                                If Not warningDialog.AlwaysCheckForThisExtension Then
                                    If String.IsNullOrEmpty(extraSafeExtensions) Then
                                        extraSafeExtensions = fileExtension
                                    Else
                                        extraSafeExtensions = extraSafeExtensions & s_SAFE_EXTENSION_SEPERATOR & fileExtension
                                    End If
                                    SafeExtensions = extraSafeExtensions
                                End If
                            End Using
                        End If
                    End If

                End If
            End If

            Dim OpenDocumentService As Shell.Interop.IVsUIShellOpenDocument = CType(RootDesigner.GetService(GetType(Shell.Interop.IVsUIShellOpenDocument)), Shell.Interop.IVsUIShellOpenDocument)
            Dim Hierarchy As Shell.Interop.IVsUIHierarchy = Nothing
            Dim ItemId As UInteger
            Dim WindowFrame As Shell.Interop.IVsWindowFrame = Nothing
            Dim ServiceProvider As OLE.Interop.IServiceProvider = Nothing

            Try
                Dim OpenLogView As Guid
                If UseOpenWithDialog Then
                    'This will cause the Open With... dialog to appear, allowing the user to choose which editor he wants to use
                    OpenLogView = Editors.Interop.LOGVIEWID.LOGVIEWID_UserChooseView
                Else
                    'This causes the default editor (using its primary view type) for the extension to be used.
                    OpenLogView = Editors.Interop.LOGVIEWID.LOGVIEWID_Primary
                End If

                'This will first try to open the file in a project that contains that file (if the file is in the Resources directory of a project, for
                '  example, or if it's linked into a project).  If it's not found in a specific project, it will open it in the miscellaneous files 
                '  project.
                'And those are the semantics that we want...
                VSErrorHandler.ThrowOnFailure(OpenDocumentService.OpenDocumentViaProject(ResourceFullPathTolerant, OpenLogView, ServiceProvider, Hierarchy, ItemId, WindowFrame))
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                If TypeOf ex Is System.Runtime.InteropServices.COMException Then
                    If CType(ex, System.Runtime.InteropServices.COMException).ErrorCode = Editors.Interop.win.OLE_E_PROMPTSAVECANCELLED Then
                        'We get this error when the user cancels the Open With dialog.  Obviously, we ignore this error and cancel.
                        Exit Sub
                    End If
                End If

                DsMsgBox(ex)
            End Try

            'If the file was opened in an intrinsic editor (as opposed to an external editor), then WindowFrame will 
            '  have a non-Nothing value.
            If Not WindowFrame Is Nothing Then
                'Okay, it was an intrinsic editor.  We are responsible for making sure the editor is visible.
                VSErrorHandler.ThrowOnFailure(WindowFrame.Show())
            End If
        End Sub

#End Region


#Region "Import/Export commands"

        ''' <summary>
        ''' Handles the Export menu command
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuExport(ByVal sender As Object, ByVal e As EventArgs)
            'CONSIDER: persist per resource editor instance
            Static StickyExportPath As String 'Remember where the used last exported, so we'll bring up the dialog there by default

            Try
                'Export the selected resources if it's legal.
                Dim SelectedResources() As Resource = GetSelectedResources()
                If SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0 Then
                    If CanImportOrExportResources(SelectedResources) Then
                        If SelectedResources.Length = 1 Then
                            'If it's just one resource we're exporting, we'll bring up the file save dialog.
                            Dim ResourceToExportFrom As Resource = SelectedResources(0)
                            Dim Title As String = SR.GetString(SR.RSE_DlgTitle_Export_1Arg, ResourceToExportFrom.Name)
                            Dim SuggestedFileName As String = GetSuggestedFileNameForResource(ResourceToExportFrom)
                            Dim Filter As String = ResourceToExportFrom.ResourceTypeEditor.GetSaveFileDialogFilter(Path.GetExtension(SuggestedFileName))
                            Dim FilterIndex As Integer = 0
                            Dim UserCanceled As Boolean
                            Dim FileAndPathToExportTo As String = ShowSaveFileDialog( _
                                UserCanceled, _
                                Title, Filter, FilterIndex, _
                                SuggestedFileName, StickyExportPath)
                            If Not UserCanceled AndAlso FileAndPathToExportTo <> "" Then
                                StickyExportPath = Path.GetDirectoryName(FileAndPathToExportTo)

                                'Export the sucker.
                                ExportResources(SelectedResources, Path.GetDirectoryName(FileAndPathToExportTo), SingleFileName:=Path.GetFileName(FileAndPathToExportTo))
                            End If
                        Else
                            'For multiple resources, we bring up the browser dialog, and don't let them control
                            '  the individual filenames.
                            Dim Title As String = SR.GetString(SR.RSE_Dlg_ExportMultiple)
                            Dim UserCanceled As Boolean
                            Dim PathToExportTo As String = ShowFolderBrowserDialog(UserCanceled, Title, ShowNewFolderButton:=True, DefaultPath:=StickyExportPath)
                            If Not UserCanceled AndAlso PathToExportTo <> "" Then
                                StickyExportPath = PathToExportTo

                                'Export the group to the specified folder.
                                ExportResources(SelectedResources, PathToExportTo)
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Command handler for the Import menu.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuImport(ByVal sender As Object, ByVal e As EventArgs)
            Dim SelectedResources() As Resource = GetSelectedResources()

            'Last location that the user chose to import resources
            Static StickyImportPath As String

            'We can only import into (exactly) a single resource at a time...
            If SelectedResources IsNot Nothing AndAlso SelectedResources.Length = 1 Then
                Try
                    ' If we can't check out the resource file, do not pop up any dialog...
                    RootDesigner.DesignerLoader.ManualCheckOut()
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    'Report errors to the user.
                    DsMsgBox(ex)
                    Return
                End Try

                'Is it valid to import into this resource?
                If CanImportOrExportResources(SelectedResources) Then
                    'Okay, we've got our resource.  Now we need to ask the user what file to import from.
                    Dim ResourceToImportTo As Resource = SelectedResources(0)
                    Dim Title As String = SR.GetString(SR.RSE_DlgTitle_Import_1Arg, ResourceToImportTo.Name)
                    Dim Filter As String = ResourceToImportTo.ResourceTypeEditor.GetOpenFileDialogFilter(_resourceFile)
                    Dim FilterIndex As Integer = 0
                    Dim UserCanceled As Boolean
                    Dim Files() As String = ShowOpenFileDialog(UserCanceled, Title, Filter, FilterIndex, MultiSelect:=False, DefaultPath:=StickyImportPath)
                    If Not UserCanceled AndAlso Files IsNot Nothing AndAlso Files.Length = 1 Then
                        Dim FileAndPathToImportFrom As String = Files(0)
                        Debug.Assert(File.Exists(FileAndPathToImportFrom))

                        'Go ahead and import into the selected file.  This is simply a matter of reading
                        '  the resource from the file, and then setting it into the resource's
                        '  Value property.
                        Try
                            Dim NewResourceValue As Object = ResourceToImportTo.ResourceTypeEditor.LoadResourceFromFile(FileAndPathToImportFrom, Me._resourceFile)

                            ' We should test whether the resource item is valid first...
                            Using NewResourceItem As New Resource(_resourceFile, "Test", "", NewResourceValue, Me)
                                Dim Message As String = String.Empty
                                Dim HelpID As String = String.Empty
                                If Not IsValidResourseItem(NewResourceItem, Message, HelpID) Then
                                    DsMsgBox(SR.GetString(SR.RSE_Err_CantAddUnsupportedResource_1Arg, ResourceToImportTo.Name) & VB.vbCrLf & VB.vbCrLf & Message, MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpID)
                                    Return
                                End If
                            End Using

                            ResourceToImportTo.SetValue(NewResourceValue)

                            'BUGFIX:Dev11#35824 Save the directory where the user import resources for the next time.
                            StickyImportPath = Path.GetDirectoryName(FileAndPathToImportFrom)
                        Catch ex As Exception
                            RethrowIfUnrecoverable(ex)
                            'Report errors to the user.
                            DsMsgBox(ex)
                        End Try
                    End If
                End If
            End If
        End Sub


        ''' <summary>
        ''' Indicates whether all resources in a set of resources can be imported or exported
        ''' </summary>
        ''' <param name="Resources">The set of resources.</param>
        ''' <returns>True iff all resources in the set can be imported or exported.</returns>
        ''' <remarks></remarks>
        Private Function CanImportOrExportResources(ByVal Resources() As Resource) As Boolean
            For Each Resource As Resource In Resources
                'We can import or export non-string, non-resxref, non-linked resources.  Can't import
                '  stuff from the "other" category or stuff that doesn't have a defined extension.
                If _
                    Not Resource.IsResXNullRef _
                    AndAlso Not TypeOf Resource.ResourceTypeEditor Is ResourceTypeEditorStringBase _
                    AndAlso Not Resource.IsLink _
                    AndAlso Not Resource.GetCategory(_categories) Is _categoryOther _
                Then
                    If Resource.ResourceTypeEditor.TryCanSaveResourceToFile(Resource) Then
                        'This one's okay
                        Continue For
                    End If
                End If

                Return False
            Next

            Return True
        End Function


        ''' <summary>
        ''' Menu handler for the Import command.  Determines whether it's enabled and/or visible
        ''' </summary>
        ''' <param name="MenuCommand">The import DesignerMenuCommand</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuImportEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            'Only visible if we're in the listview
            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay <> Category.Display.ListView Then
                MenuCommand.Visible = False
                Return False
            Else
                MenuCommand.Visible = True
            End If

            If ReadOnlyMode Then
                Return False
            End If

            'We can only import to a single resource at a time
            Dim SelectedResources() As Resource = GetSelectedResources()
            Return SelectedResources IsNot Nothing _
                AndAlso SelectedResources.Length = 1 _
                AndAlso CanImportOrExportResources(SelectedResources)
        End Function


        ''' <summary>
        ''' Menu handler for the Export command.  Determines whether it's enabled and/or visible
        ''' </summary>
        ''' <param name="MenuCommand">The import DesignerMenuCommand</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuExportEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            'Only visible if we're in the listview
            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay <> Category.Display.ListView Then
                MenuCommand.Visible = False
                Return False
            Else
                MenuCommand.Visible = True
            End If

            'We can export multiple resources at a time
            Dim SelectedResources() As Resource = GetSelectedResources()
            Return SelectedResources IsNot Nothing _
                AndAlso SelectedResources.Length > 0 _
                AndAlso CanImportOrExportResources(SelectedResources)
        End Function


        ''' <summary>
        ''' Given a Resource, get a filename to suggest to the user for saving that resource, based on its name.
        ''' </summary>
        ''' <param name="Resource">The Resource to get the name from.</param>
        ''' <returns></returns>
        ''' <remarks>Only suggests only legal filenames.</remarks>
        Public Shared Function GetSuggestedFileNameForResource(ByVal Resource As Resource) As String
            Debug.Assert(Resource IsNot Nothing)
            If Resource.IsResXNullRef Then
                Debug.Fail("Shouldn't be calling GetSuggestedFileNameForResource on a ResXNullRef resource")
                Return "ResXNullRef"
            ElseIf Resource.IsLink Then
                Return Path.GetFileName(Resource.AbsoluteLinkPathAndFileName)
            Else
                Dim Extension As String = ""
                Try
                    Extension = Resource.ResourceTypeEditor.GetResourceFileExtension(Resource)
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)

                    'If an error, simply don't add an extension.
                End Try
                Return CreateLegalFileName(Resource.Name) & Extension
            End If
        End Function


        ''' <summary>
        ''' Asks the user if it's okay to replace all of the given files.
        ''' </summary>
        ''' <param name="FileNames"></param>
        ''' <returns>True if the user says Yes, otherwise False.</returns>
        ''' <remarks></remarks>
        Public Function QueryUserToReplaceFiles(ByVal FileNames As IList) As Boolean
            If FileNames.Count = 0 Then
                Debug.Fail("QueryUserToReplaceFiles: there weren't any files passed in")
                Return False
            ElseIf FileNames.Count = 1 Then
                Return DialogResult.Yes = DsMsgBox(SR.GetString(SR.RSE_Dlg_ReplaceExistingFile, CStr(FileNames(0))), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2)
            Else
                'Build up a list of the files to show the user
                Dim ExistingFiles As String = ""
                For Each FileName As String In FileNames
                    ExistingFiles &= VB.vbCrLf & FileName
                Next

                Return DialogResult.Yes = DsMsgBox(SR.GetString(SR.RSE_Dlg_ReplaceExistingFiles) & VB.vbCrLf & ExistingFiles, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2)
            End If
        End Function


        ''' <summary>
        ''' Exports a set of resources to a specified destination folder.
        ''' </summary>
        ''' <param name="Resources">The resources to export</param>
        ''' <param name="FolderPath">The directory to export all resources to.</param>
        ''' <param name="SingleFileName">If specified, and if there is only a single resource in the list, then this filename will be used to export to (in FolderPath).
        '''   Otherwise, the individual filenames will be derived automatically from the resource names.</param>
        ''' <remarks></remarks>
        Private Sub ExportResources(ByVal Resources() As Resource, ByVal FolderPath As String, Optional ByVal SingleFileName As String = Nothing)
            Debug.Assert(SingleFileName = "" OrElse VB.UBound(Resources) = 0, "ExportResources: SingleFileName is only allowed if exporting a single resource")
            If Resources Is Nothing OrElse Resources.Length = 0 Then
                Exit Sub
            End If

            Try
                'Determine the list of filenames to use
                Dim FileNames As New ArrayList(Resources.Length)
                For Each Resource As Resource In Resources
                    Debug.Assert(Not Resource.IsResXNullRef AndAlso Not Resource.IsLink)

                    Dim FileName As String
                    If SingleFileName <> "" Then
                        FileName = SingleFileName
                    Else
                        FileName = GetSuggestedFileNameForResource(Resource)
                    End If

                    'Ensure that all the filenames are unique among the group
                    If FileNames.Contains(FileName) Then
                        Dim Append As Integer = 0
                        Dim MungedFileName As String
                        Do
                            Append += 1
                            MungedFileName = Path.GetFileNameWithoutExtension(FileName) & CStr(Append) & Path.GetExtension(FileName)
                        Loop While FileNames.Contains(MungedFileName)
                        FileName = MungedFileName
                    End If

                    FileNames.Add(FileName)
                Next

                Dim serviceProvider As IServiceProvider = DirectCast(Me.RootDesigner, IServiceProvider)

                'See if any of the files already exist
                Dim ExistingFiles As New List(Of String)
                For Each FileName As String In FileNames
                    Dim FilePath As String = Path.Combine(FolderPath, FileName)
                    If File.Exists(FilePath) Then
                        ExistingFiles.Add(FilePath)
                    End If
                Next
                If ExistingFiles.Count > 0 Then
                    'Yes - ask the user if it's okay to replace them.
                    If Not QueryUserToReplaceFiles(ExistingFiles.ToArray) Then
                        'User said No - cancel.
                        Exit Sub
                    End If

                    ' Try to checkout ...
                    DesignerFramework.SourceCodeControlManager.QueryEditableFiles(serviceProvider, ExistingFiles, False, False)
                End If

                'Export the resources.
                For ResourceIndex As Integer = 0 To Resources.Length - 1
                    Dim Resource As Resource = Resources(ResourceIndex)
                    Dim FileAndPathToExportTo As String = Path.Combine(FolderPath, DirectCast(FileNames(ResourceIndex), String))

                    If File.Exists(FileAndPathToExportTo) Then
                        ' Try to see whether we can update. If the customer checkout some files, we should only update them.
                        ExistingFiles.Clear()
                        ExistingFiles.Add(FileAndPathToExportTo)

                        If Not DesignerFramework.SourceCodeControlManager.QueryEditableFiles(serviceProvider, ExistingFiles, False, True) Then
                            Continue For
                        End If

                        File.Delete(FileAndPathToExportTo)
                    End If

                    'Save the resource
                    Resource.ResourceTypeEditor.SaveResourceToFile(Resource, FileAndPathToExportTo)
                Next
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                DsMsgBox(ex)
            End Try
        End Sub


#End Region


#Region "Select All command"

        ''' <summary>
        ''' Handler for the "Select All" menu command
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub MenuSelectAll(ByVal sender As Object, ByVal e As EventArgs)
            If IsInEditing Then
                CommitPendingChanges()
            End If

            Using New WaitCursor
                Select Case CurrentCategory.CategoryDisplay
                    Case Category.Display.ListView
                        _resourceListView.SelectAll()
                    Case Category.Display.StringTable
                        StringTable.SelectAll()
                    Case Else
                        Debug.Fail("Unexpected category display")
                End Select
            End Using
        End Sub

        ''' <summary>
        ''' Enabled handler for the Cut and Copy menus.  Determines if the menu items should be enabled or not.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuSelectAllEnableHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Select Case CurrentCategory.CategoryDisplay
                Case Category.Display.ListView
                    Return _resourceListView.VirtualListSize > 0
                Case Category.Display.StringTable
                    Return StringTable.RowCountVirtual > 0
                Case Else
                    Debug.Fail("Unexpected category display")
            End Select
            Return False
        End Function

#End Region


#Region "EditBox related"

        ''' <summary>
        ''' The function was called by child-page when an editBox is activated
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnItemBeginEdit()
            _inEditingItem = True
            RefreshCommandStatus()
        End Sub

        ''' <summary>
        ''' The function was called by child-page when an editBox is dismissed
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnItemEndEdit()
            _inEditingItem = False
            RefreshCommandStatus()
        End Sub

        ''' <summary>
        ''' The function forces to refresh the status of all commonds...
        '''  The reason is that the selection isn't updated when we activate an editBox to edit the name label in the listView, so the status of the menu items won't be updated as well
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RefreshCommandStatus()
            If _menuCommands IsNot Nothing Then
                For Each command As DesignerMenuCommand In _menuCommands
                    command.RefreshStatus()
                Next
            End If
        End Sub
#End Region


#Region "Keyboard shortcuts"

        ''' <summary>
        ''' Begins editing of a cell in the string table, and edit a label in the list view
        '''  We handle a global 'EditLabel' command to bind 'F2'
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuEditLabel(ByVal sender As Object, ByVal e As EventArgs)
            If CurrentCategory.CategoryDisplay = Category.Display.StringTable Then
                StringTable.BeginEdit(False)
            ElseIf CurrentCategory.CategoryDisplay = Category.Display.ListView Then
                ' EditCell was binding to the F2 key, which we want to start editing the resource name...
                '  CONSIDER: update the COMMAND name for the resource designer...
                Dim SelectedResources() As Resource = GetSelectedResources()
                If SelectedResources.Length = 1 Then
                    _resourceListView.BeginLabelEdit(SelectedResources(0))
                End If
            End If
        End Sub

        ''' <summary>
        ''' Enabled handler for the EditLabel menus.  Determines if the menu items should be enabled or not.
        '''  the menu item is usually invisible, we use this to handle F2
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuEditLabelEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            If _currentCategory IsNot Nothing Then
                If _currentCategory.CategoryDisplay = Category.Display.ListView Then
                    Return Not ReadOnlyMode AndAlso GetSelectedResources().Length = 1
                ElseIf CurrentCategory.CategoryDisplay = Category.Display.StringTable Then
                    Return Not ReadOnlyMode AndAlso _
                        StringTable.CurrentCell IsNot Nothing AndAlso _
                        Not StringTable.CurrentCell.ReadOnly AndAlso _
                        Not StringTable.CurrentCell.OwningRow.ReadOnly
                End If
            End If
            Return False
        End Function

#End Region


#Region "Category buttons"

        ''' <summary>
        ''' This single handler handles whenever the number of resources in a category
        '''   has transitioned to or from zero.  This handles it for all categories.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Category_XXX_ResourcesExistChanged(ByVal sender As Object, ByVal e As System.EventArgs) _
        Handles _
            _categoryAudio.ResourcesExistChanged, _
            _categoryFiles.ResourcesExistChanged, _
            _categoryIcons.ResourcesExistChanged, _
            _categoryImages.ResourcesExistChanged, _
            _categoryOther.ResourcesExistChanged, _
            _categoryStrings.ResourcesExistChanged
        End Sub


        ''' <summary>
        ''' Command handler for "Strings" resource type menu item
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuResourceTypeStrings(ByVal sender As Object, ByVal e As System.EventArgs)
            SwitchToCategory(_categoryStrings)
        End Sub


        ''' <summary>
        ''' Command handler for "Images" resource type menu item
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuResourceTypeImages(ByVal sender As Object, ByVal e As System.EventArgs)
            SwitchToCategory(_categoryImages)
        End Sub


        ''' <summary>
        ''' Command handler for "Icons" resource type menu item
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuResourceTypeIcons(ByVal sender As Object, ByVal e As System.EventArgs)
            SwitchToCategory(_categoryIcons)
        End Sub


        ''' <summary>
        ''' Command handler for "Audio" resource type menu item
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuResourceTypeAudio(ByVal sender As Object, ByVal e As System.EventArgs)
            SwitchToCategory(_categoryAudio)
        End Sub


        ''' <summary>
        ''' Command handler for "Files" resource type menu item
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuResourceTypeFiles(ByVal sender As Object, ByVal e As System.EventArgs)
            SwitchToCategory(_categoryFiles)
        End Sub


        ''' <summary>
        ''' Command handler for "Other" resource type menu item
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuResourceTypeOther(ByVal sender As Object, ByVal e As System.EventArgs)
            SwitchToCategory(_categoryOther)
        End Sub

#End Region


#Region "Add Existing menu item (add new resource from existing file)"

        ''' <summary>
        ''' Called when the "Add/Existing" button is pressed.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_ExistingFile_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim Title As String = SR.GetString(SR.RSE_DlgTitle_AddExisting)
            Dim FilterIndex As Integer = 0
            Dim Filter As String = GetFileDialogFilterForCategories(_categories, _currentCategory, FilterIndex)
            Dim UserCanceled As Boolean

            'Last location that the user chose to add existing resource.
            Static StickyAddExistingFilePath As String

            CommitPendingChanges()

            Try
                ' If we can't check out the resource file, do not pop up any dialog...
                RootDesigner.DesignerLoader.ManualCheckOut()

                'Ask the user to point to the file(s) to add
                Dim FilesToAdd() As String = ShowOpenFileDialog(UserCanceled, Title, Filter, FilterIndex, MultiSelect:=True, DefaultPath:=StickyAddExistingFilePath)

                If UserCanceled OrElse FilesToAdd Is Nothing OrElse FilesToAdd.Length = 0 Then
                    Exit Sub
                End If

                ' BUGFIX: Dev11#35824: Save the directory where the user add existing resource file for the next time.
                StickyAddExistingFilePath = Path.GetDirectoryName(CType(FilesToAdd(0), String))

                '... and them them as resources.
                'NOTE: we update the existing item as bug 382459 said.
                'FixInvalidIdentifiers:=True because we want to fix invalid identifiers in the copy/paste and drag/drop scenarios
                AddOrUpdateResourcesFromFiles(FilesToAdd, CopyFileIfExists:=False, AlwaysAddNew:=False, FixInvalidIdentifiers:=True)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                DsMsgBox(ex)
            End Try
        End Sub

#End Region


#Region "Add Button menus items (add new resources of a given type)"

        ''' <summary>
        ''' Check whether "add item" menu should be enabled
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuAddEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return Not ReadOnlyMode
        End Function

        ''' <summary>
        ''' Add the default type of resource for the current view
        ''' The handler for this is owned by each category
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonFixedAdd_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            If Me.CurrentCategory.AddCommand IsNot Nothing Then
                Me.CurrentCategory.AddCommand.Invoke(sender, e)
            End If
        End Sub

        ''' <summary>
        ''' Check whether "add TIFF" menu should be enabled
        ''' </summary>
        ''' <param name="MenuCommand">The menu item to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuAddTiffEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return Not ReadOnlyMode AndAlso Not RootComponent.IsInsideDeviceProject
        End Function

        ''' <summary>
        ''' Handles the "Add.New String" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewString_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            CommitPendingChanges()

            'We simply switch to the string table and place the cursor in the bottom row, which is the add/new row.
            SwitchToCategory(_categoryStrings)
            StringTable.NewString()
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Image.BMP Image" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewImage_BMP_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.Bitmap, ResourceTypeEditorBitmap.EXT_BMP)

            ' Remember the type of the last added image
            Me.CurrentCategory.AddCommand = AddressOf Me.ButtonAdd_NewImage_BMP_Click
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Image.GIF Image" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewImage_GIF_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.Bitmap, ResourceTypeEditorBitmap.EXT_GIF)

            ' Remember the type of the last added image
            Me.CurrentCategory.AddCommand = AddressOf Me.ButtonAdd_NewImage_GIF_Click
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Image.JPEG Image" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewImage_JPEG_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.Bitmap, ResourceTypeEditorBitmap.EXT_JPG)

            ' Remember the type of the last added image
            Me.CurrentCategory.AddCommand = AddressOf Me.ButtonAdd_NewImage_JPEG_Click
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Image.PNG" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewImage_PNG_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.Bitmap, ResourceTypeEditorBitmap.EXT_PNG)

            ' Remember the type of the last added image
            Me.CurrentCategory.AddCommand = AddressOf Me.ButtonAdd_NewImage_PNG_Click
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Image.TIFF Image" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewImage_TIFF_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.Bitmap, ResourceTypeEditorBitmap.EXT_TIF)

            ' Remember the type of the last added image
            Me.CurrentCategory.AddCommand = AddressOf Me.ButtonAdd_NewImage_TIFF_Click
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Icon" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewIcon_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.Icon, ResourceTypeEditorIcon.EXT_ICO)
        End Sub


        ''' <summary>
        ''' Handles the "Add.New Text File" ToolStripMenuItem
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_NewTextFile_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            QueryAddNewLinkedResource(ResourceTypeEditors.TextFile, ResourceTypeEditorTextFile.EXT_TXT)
        End Sub


        ''' <summary>
        ''' Gets the location on disk where a new file should be created.  If we're in a project with a Resources
        '''   folder, that will be the default location (without asking the user).  Otherwise, the user will be
        '''   asked the correct location.
        ''' </summary>
        ''' <param name="FileNameOnly">The filename (no path) of the new file.</param>
        ''' <param name="UserCancel">[Out] Set to true if the user cancels the operation.</param>
        ''' <returns>The full path to the file where it should be saved.</returns>
        ''' <remarks>Caller is responsible for displaying any error messageboxes</remarks>
        Public Function GetSaveLocationForNewProjectFile(ByVal TypeEditor As ResourceTypeEditor, ByVal FileNameOnly As String, ByRef UserCancel As Boolean) As String
            'Last location that the user chose to save a file through this method.  Remembered as a sticky setting.
            Static StickyDestinationPath As String

            Dim NewResourceFilePath As String 'The file and path to save the new resource
            Dim ProjectResourcesPath As String = ResourcesFolderService.GetAddFileDestinationPath(GetProject(), GetResXProjectItem(), CreateDirectoryIfDoesntExist:=True)
            If ProjectResourcesPath = "" Then
                'We must not be in a project.  We'll have to ask the user where to put the new file.

                'Ask the user where to save the file.
                NewResourceFilePath = ShowSaveFileDialog( _
                    UserCancel, _
                    SR.GetString(SR.RSE_DlgTitle_AddNew), _
                    TypeEditor.GetSaveFileDialogFilter(Path.GetExtension(FileNameOnly)), _
                    0, _
                    FileNameOnly, _
                    StickyDestinationPath)
                If UserCancel OrElse NewResourceFilePath = "" Then
                    Return ""
                End If
            Else
                NewResourceFilePath = Path.Combine(ProjectResourcesPath, FileNameOnly)
            End If

            Debug.Assert(NewResourceFilePath = Path.GetFullPath(NewResourceFilePath), "Should have already been an absolute path")
            NewResourceFilePath = Path.GetFullPath(NewResourceFilePath)

            'Save the directory where the user saved the file for the next time.
            StickyDestinationPath = Path.GetDirectoryName(NewResourceFilePath)

            Return NewResourceFilePath
        End Function


        ''' <summary>
        ''' Give a resource type editor, allows the user to create a new resource file of any type
        '''   in that resource type editor.  Shows a save file dialog to allow the user to specify
        '''   where to place the new resource.
        ''' </summary>
        ''' <param name="TypeEditor">The resource type editor for the type(s) of file being created.</param>
        ''' <remarks></remarks>
        Private Sub QueryAddNewLinkedResource(ByVal TypeEditor As ResourceTypeEditor, ByVal ResourceFileExtension As String)
            Try
                ' If we can't check out the resource file, do not pop up any dialog...
                RootDesigner.DesignerLoader.ManualCheckOut()

                '
                'First, get the ID for the new file
                Dim UserCancel As Boolean
                Dim NewResourceName As String = DialogQueryName.QueryAddNewResourceName(RootDesigner, ResourceFile.GetUniqueName(TypeEditor), UserCancel)
                If UserCancel Then
                    Exit Sub
                End If

                Debug.Assert(NewResourceName.Length > 0, "Why we get an empty name")

                'Get the suggested filename
                Dim FileNameOnly As String = CreateLegalFileName(NewResourceName) & ResourceFileExtension

                'Then get the path to save the file to.  If we're in a project, this is normally placed
                '  in the resources folder automatically, without asking the user.
                Dim NewResourceFilePath As String = GetSaveLocationForNewProjectFile( _
                    TypeEditor, _
                    FileNameOnly, _
                    UserCancel)
                If UserCancel Then
                    Exit Sub
                End If

                If File.Exists(NewResourceFilePath) Then
                    'Ask permission to overwrite the file.
                    If QueryUserToReplaceFiles(New String() {NewResourceFilePath}) Then
                        'User said yes.
                        File.Delete(NewResourceFilePath)
                    Else
                        Exit Sub
                    End If
                End If

                Using New WaitCursor
                    CommitPendingChanges()

                    'Write out a blank resource into the new file
                    Try
                        TypeEditor.CreateNewResourceFile(NewResourceFilePath)
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        DsMsgBox(SR.GetString(SR.RSE_Err_CantCreateNewResource_2Args, NewResourceFilePath, ex.Message), MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_CantCreateNewResource)
                        Exit Sub
                    End Try

                    'If the new file is in any subdirectory of the project system on disk, then
                    '  we want to add the file to the project.
                    'Special case: We also do this always for C++.  C++ is currently the only known
                    '  managed reference-based project, and it would be logical for C++ users to 
                    '  select a file location outside of the project but expect the file to be linked
                    '  into the project (that's their normal mode of operation).  Since there's no way
                    '  to tell whether a particular project is referenced-based or not, we need to
                    '  simply special-case C++ here.
                    Dim Project As Project = GetProject()
                    Dim AddToProject As Boolean = False
                    If Project IsNot Nothing AndAlso _
                        (ResourcesFolderService.IsFileInProjectSubdirectories(Project, NewResourceFilePath) _
                        OrElse New Guid(Project.Kind).Equals(_projectGuid_CPlusPlus)) _
                    Then
                        AddToProject = True
                    End If

                    'And add the newly-created resource to the ResourceFile.
                    Dim NewResources() As Resource = AddOrUpdateResourcesFromFiles(New String() {NewResourceFilePath}, CopyFileIfExists:=False, AlwaysAddNew:=True, AddToProject:=AddToProject, FixInvalidIdentifiers:=False)

                    'We must manually flush our buffer and run the custom tool on it before trying to load the new file 
                    '  into an editor.  Otherwise it won't be flushed until the next time the resource editor is activated and
                    '  and deactivated again.  
                    Try
                        Me.RootDesigner.DesignerLoader.RunSingleFileGenerator(True)
                    Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                        DsMsgBox(ex)
                    End Try

                    'Finally, attempt to open up the file in its editor
                    Debug.Assert(NewResources.Length = 1)
                    Dim NewResource As Resource = NewResources(0)
                    Debug.Assert(NewResource IsNot Nothing)
                    Try
                        EditOrOpenWith(NewResource, UseOpenWithDialog:=False)
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True)

                        'Ignore errors trying to open the editor for the file.  That's confusing to the user, because it
                        '  makes it appear that the resource wasn't successfully added, but it was.  Let them double-click
                        '  it if they want to edit, at which point they'll get the error and it will be less confusing.
                        Debug.Fail("Got an exception trying to open the new resource in a separate editor - ignoring and continuing" & Microsoft.VisualBasic.vbCrLf & ex.ToString)
                    End Try
                End Using
            Catch ex As Exception
                DsMsgBox(ex)
            End Try
        End Sub

#End Region


#Region "View Buttons"

        ''' <summary>
        ''' Dummy handler for the case when the user clicks on the Views drop down icon
        ''' which should essentially be a no-op
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonFixedView_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        End Sub

        ''' <summary>
        ''' Handles the "List" view button.  Changes the listview to "List" mode.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonViews_List_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            ChangeResourceView(Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.List)
        End Sub


        ''' <summary>
        ''' Handles the "Details" view button.  Changes the listview to "Details" mode.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonViews_Details_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            ChangeResourceView(Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Details)
        End Sub


        ''' <summary>
        ''' Handles the "Thumbnail" view button.  Changes the listview to "Thumbnail" mode.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonViews_Thumbnail_Click(ByVal sender As Object, ByVal e As System.EventArgs)
            ChangeResourceView(Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ResourceView.Thumbnail)
        End Sub

        ''' <summary>
        ''' Enabled handler for the Open/Open With menu commands.  Determines if the menu item should be enabled.
        ''' </summary>
        ''' <param name="MenuCommand">The menu item for Open or Open/With.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ViewsMenuItemsEnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            'Only visible/enabled if we're in a listview
            If _currentCategory IsNot Nothing AndAlso _currentCategory.CategoryDisplay = Category.Display.ListView Then
                MenuCommand.Visible = True
                Return True
            Else
                MenuCommand.Visible = False
                Return False
            End If
        End Function

#End Region


#Region "Handling context menus"


        ''' <summary>
        ''' Shows a context menu, given mouse event args.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ShowContextMenu(ByVal sender As Object, ByVal e As MouseEventArgs)
            If e.Button = System.Windows.Forms.MouseButtons.Right Then
                Try
                    Me.RootDesigner.ShowContextMenu(Constants.MenuConstants.ResXContextMenuID, e.X, e.Y)
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail(ex.ToString)
                End Try
            Else
                Debug.Fail("Wrong mouse button!")
            End If
        End Sub


        ''' <summary>
        ''' Called when the listview should show a context menu.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ResourceListView_ContextMenuShow(ByVal sender As Object, ByVal e As MouseEventArgs) Handles _resourceListView.ContextMenuShow
            ShowContextMenu(sender, e)
        End Sub


        ''' <summary>
        ''' Called when the string table should show a context menu.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub StringTable_ContextMenuShow(ByVal sender As Object, ByVal e As MouseEventArgs) Handles StringTable.ContextMenuShow
            ShowContextMenu(sender, e)
        End Sub


#End Region


#Region "File dialog utilities"

        ''' <summary>
        ''' Display a "browse folder" dialog.
        ''' </summary>
        ''' <param name="UserCanceled">[Out] Returns True if the user canceled the operation.</param>
        ''' <param name="Title">The title for the dialog</param>
        ''' <param name="ShowNewFolderButton">Whether to show the "New Folder" button</param>
        ''' <param name="DefaultPath">The path to start out with.</param>
        ''' <returns>The selected folder, or Nothing if the user canceled.</returns>
        ''' <remarks></remarks>
        Public Function ShowFolderBrowserDialog(ByRef UserCanceled As Boolean, ByVal Title As String, ByVal ShowNewFolderButton As Boolean, Optional ByVal DefaultPath As String = Nothing) As String
            UserCanceled = False
            Dim Dialog As New FolderBrowserDialog
            With Dialog
                .Description = Title
                .ShowNewFolderButton = ShowNewFolderButton
                If DefaultPath <> "" Then
                    .SelectedPath = DefaultPath
                End If

                If .ShowDialog(GetDialogOwnerWindow()) = DialogResult.Cancel Then
                    UserCanceled = True
                    Return Nothing
                End If

                Return .SelectedPath
            End With
        End Function


        'CONSIDER: use ShowDialog() return
        ''' <summary>
        ''' Shows an "Open File" dialog.
        ''' </summary>
        ''' <param name="UserCanceled">[Out] Returns True if the user canceled the operation.</param>
        ''' <param name="Title">The dialog title.</param>
        ''' <param name="Filter">The filter string to use.</param>
        ''' <param name="FilterIndex">The filter index to start out with.</param>
        ''' <param name="MultiSelect">Whether the user can select multiple files.</param>
        ''' <param name="DefaultPath">The initial path for the dialog to open</param>
        ''' <returns>The selected file/path, or Nothing if the user canceled.</returns>
        ''' <remarks></remarks>
        Public Function ShowOpenFileDialog(ByRef UserCanceled As Boolean, ByVal Title As String, ByVal Filter As String, ByVal FilterIndex As Integer, ByVal MultiSelect As Boolean, Optional ByVal DefaultPath As String = Nothing) As String()
            Try
                ' BUGFIX: Dev11#35824: Initialize Open File Dialog on the DefaultPath.  Nothing means devenv location.
                Dim fileNames As ArrayList = Utils.GetFilesViaBrowse(RootDesigner.DesignerHost, Me.Handle, DefaultPath, Title, Filter, CUInt(FilterIndex), MultiSelect, Nothing, True)

                If fileNames Is Nothing OrElse fileNames.Count = 0 Then
                    UserCanceled = True
                    Return Nothing
                End If

                Return CType(fileNames.ToArray(GetType(String)), String())
            Catch ex As COMException When ex.ErrorCode = Editors.Interop.NativeMethods.HRESULT_FROM_WIN32(Editors.Interop.win.FNERR_BUFFERTOOSMALL)
                ' We didn't provide enough buffer for file names. It passes the limitation of the designer.
                Throw NewException(SR.GetString(SR.RSE_Err_MaxFilesLimitation), HelpIDs.Err_MaxFilesLimitation, ex)
            End Try
        End Function


        ''' <summary>
        ''' Shows a "Save File" dialog.
        ''' </summary>
        ''' <param name="UserCanceled">[Out] Returns True if the user canceled the operation.</param>
        ''' <param name="Title">The dialog title.</param>
        ''' <param name="Filter">The filter string to use.</param>
        ''' <param name="FilterIndex">The filter index to start out with.</param>
        ''' <param name="DefaultFileName">The default file name.</param>
        ''' <param name="DefaultPath">The path to start out in.  Nothing is okay.</param>
        ''' <param name="OverwritePrompt">If true, Windows will ask the user to overwrite the file if it already exists.</param>
        ''' <returns>The selected file/path, or Nothing if the user canceled.</returns>
        ''' <remarks>Caller is responsible for displaying any error messageboxes</remarks>
        Public Function ShowSaveFileDialog(ByRef UserCanceled As Boolean, ByVal Title As String, ByVal Filter As String, _
                ByVal FilterIndex As Integer, _
                Optional ByVal DefaultFileName As String = Nothing, _
                Optional ByVal DefaultPath As String = Nothing, Optional ByVal OverwritePrompt As Boolean = False) As String

            UserCanceled = True

            Dim newFileName As String = Utils.GetNewFileNameViaBrowse(DirectCast(Me.RootDesigner, IServiceProvider), _
                    Me.Handle, DefaultPath, Title, Filter, CUInt(FilterIndex), DefaultFileName, OverwritePrompt)

            If newFileName <> "" Then
                UserCanceled = False
            End If

            Return newFileName
        End Function


        ''' <summary>
        ''' Given a list of categories, builds a complete list of filters for an open file dialog.
        ''' </summary>
        ''' <param name="Categories">The list of categories to build filters from (uses all resource type editors associated with this category that define filters)</param>
        ''' <param name="DefaultCategory">(Optional).  If present and non-Nothing, indicates which category should be the default.  This is done by checking the ByRef FilterIndexForDefaultCategory value on return.</param>
        ''' <param name="FilterIndexForDefaultCategory">[Out] (Optional) Returns the filter index which corresponds to the filter for DefaultCategory, or to the
        '''    *.* filter if there was no filter for the default category.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetFileDialogFilterForCategories(ByVal Categories As CategoryCollection, Optional ByVal DefaultCategory As Category = Nothing, Optional ByRef FilterIndexForDefaultCategory As Integer = 0) As String
            Dim Filter As New StringBuilder
            FilterIndexForDefaultCategory = -1

            'Now go ahead and add the categories' type editors' filters one by one
            Dim FilterIndex As Integer = 0
            For Each Category As Category In Categories
                For Each TypeEditor As ResourceTypeEditor In Category.AssociatedResourceTypeEditors
                    Dim FilterString As String = TypeEditor.GetOpenFileDialogFilter(_resourceFile)
                    If FilterString <> "" Then
                        If Filter.Length <> 0 Then
                            Filter.Append("|")
                        End If
                        Filter.Append(FilterString)
                        If Category Is DefaultCategory AndAlso FilterIndexForDefaultCategory <= 0 Then
                            FilterIndexForDefaultCategory = FilterIndex  'The Filter index is 0-indexed
                        End If
                        FilterIndex += 1
                    End If
                Next
            Next

            'Add "All files" on the end
            '
            'If we haven't decided on a filter index yet, then use the index for the *.* string we're adding now.
            If FilterIndexForDefaultCategory < 0 Then
                FilterIndexForDefaultCategory = FilterIndex '0-indexed
            End If
            '... then add *.*
            Filter.Append("|" & SR.GetString(SR.RSE_Filter_All) & " (*.*)|*.*")

            Return Filter.ToString
        End Function

#End Region


#Region "Temporary files and folders management"

        'Options for when to delete temporary files/folders that were created.
        Private Enum AutoDelete
            No
            DeleteOnClipboardFlush
            'DeleteAtExit
        End Enum


        ''' <summary>
        ''' Tries to delete a list of temporary files.
        ''' </summary>
        ''' <param name="FilesToDelete"></param>
        ''' <remarks>Swallows exceptions if the files can't be deleted.  Does not display any UI.</remarks>
        Private Shared Sub DeleteTemporaryFiles(ByVal FilesToDelete As IList)
            For Each FileName As String In FilesToDelete
                Try
                    If File.Exists(FileName) Then
                        File.Delete(FileName)
                    End If
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Unable to delete temporary file: " & FileName & VB.vbCrLf & ex.Message)
                End Try
            Next
        End Sub


        ''' <summary>
        ''' Tries to delete a list of temporary folders.
        ''' </summary>
        ''' <param name="FoldersToDelete"></param>
        ''' <remarks>Swallows exceptions if the folders can't be deleted.  Does not display any UI.</remarks>
        Private Shared Sub DeleteTemporaryFolders(ByVal FoldersToDelete As IList)
            For Each FolderName As String In FoldersToDelete
                Try
                    If Directory.Exists(FolderName) Then
                        Directory.Delete(FolderName)
                    End If
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Unable to delete temporary folder: " & FolderName & VB.vbCrLf & ex.Message)
                End Try
            Next
        End Sub


        ''' <summary>
        ''' Create a subdirectory in the user's temporary folder and returns it.  It will
        '''   will automatically be deleted when the editor exits.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetTemporarySubfolder() As String
            'We want to start with a fairly unique name, so we'll call GetTempFileName().  But
            '  Windows actually creates this for us as a file.  So we'll try deleting it and
            '  creating a directory in its place.
            Dim NewFolder As String = Path.GetTempFileName()
            Try
                File.Delete(NewFolder)
                Directory.CreateDirectory(NewFolder)
                _deleteFoldersOnEditorExit.Add(NewFolder)
                Return NewFolder
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail("Couldn't create subfolder under TEMP directory - using TEMP directory itself instead")
            End Try

            'Had trouble creating a subfolder of TEMP.  Just return TEMP instead.
            '  Do *not* add to the list of folders to delete.
            Return Path.GetTempPath
        End Function


        ''' <summary>
        ''' Creates and returns a temporary file.
        ''' </summary>
        ''' <param name="AutoDelete">Settings for when/if to automatically delete this file.</param>
        ''' <param name="ParentDirectory">The parent folder to create the file in.  If not specified, it's created in the user's temporary folder.</param>
        ''' <param name="PreferredFileName">If specified, tries to use this filename for the file.  Must contain only valid characters.</param>
        ''' <returns>The path to the created temporary file.</returns>
        ''' <remarks></remarks>
        Private Function GetTemporaryFile(ByVal AutoDelete As AutoDelete, Optional ByVal ParentDirectory As String = "", Optional ByVal PreferredFileName As String = "") As String
            Dim TempFileName As String

            If PreferredFileName <> "" OrElse ParentDirectory <> "" Then
                'A preferred filename or a parent directory was specified.

                If ParentDirectory = "" Then
                    ParentDirectory = Path.GetTempPath()
                End If
                If PreferredFileName = "" Then
                    PreferredFileName = "RSETemp.tmp"
                End If

                Dim Append As Integer
                TempFileName = Path.Combine(ParentDirectory, PreferredFileName)
                While File.Exists(TempFileName)
                    Append += 1
                    TempFileName = Path.Combine(ParentDirectory, Path.GetFileNameWithoutExtension(PreferredFileName) & Append & Path.GetExtension(PreferredFileName))
                End While
            Else
                'No prefererences.  Simply ask Windows for a temporary file, any temporary file.
                TempFileName = Path.GetTempFileName()
            End If

            'Remember files which we need to auto-delete.
            Select Case AutoDelete
                Case ResourceEditorView.AutoDelete.DeleteOnClipboardFlush
                    _deleteFilesOnClipboardFlush.Add(TempFileName)
                Case Else
                    Debug.Fail("Unexpected autodelete")
            End Select

            Return TempFileName
        End Function

#End Region


#Region "Internal Resources (those used by the resource editor in its own UI)"

#Region "Private class - resources cached by this resource editor instance"

        Friend NotInheritable Class CachedResourcesForView
            Implements IDisposable

            Private _errorGlyphLarge As Image
            Private _errorGlyphSmall As Image
            Private _errorGlyphState As Image
            Private _sortUpGlyph As Image
            Private _sortDownGlyph As Image
            Private _imageService As IVsImageService2
            Private _backgroundColor As Color

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <remarks></remarks>
            Public Sub New(background As Color)
                _backgroundColor = background
                _errorGlyphLarge = Utils.GetImageFromImageService(KnownMonikers.Blank, 96, 96, background)
                _errorGlyphSmall = Utils.GetImageFromImageService(KnownMonikers.Blank, 16, 16, background)
                _errorGlyphState = Utils.GetImageFromImageService(KnownMonikers.StatusError, 12, 12, background)
                _sortUpGlyph = Utils.GetImageFromImageService(KnownMonikers.GlyphUp, 12, 12, background)
                _sortDownGlyph = Utils.GetImageFromImageService(KnownMonikers.GlyphDown, 12, 12, background)
            End Sub

            ''' <summary>
            ''' IDisposable.Dispose
            ''' </summary>
            ''' <remarks></remarks>
            Public Overloads Sub Dispose() Implements IDisposable.Dispose
                Dispose(Disposing:=True)
            End Sub


            ''' <summary>
            ''' Dispose the instance.
            ''' </summary>
            ''' <param name="Disposing"></param>
            ''' <remarks></remarks>
            Public Overloads Sub Dispose(ByVal Disposing As Boolean)
                If Disposing Then
                    If _errorGlyphLarge IsNot Nothing Then
                        _errorGlyphLarge.Dispose()
                        _errorGlyphLarge = Nothing
                    End If
                    If _errorGlyphSmall IsNot Nothing Then
                        _errorGlyphSmall.Dispose()
                        _errorGlyphSmall = Nothing
                    End If
                    If _errorGlyphState IsNot Nothing Then
                        _errorGlyphState.Dispose()
                        _errorGlyphState = Nothing
                    End If
                    If _sortUpGlyph IsNot Nothing Then
                        _sortUpGlyph.Dispose()
                        _sortUpGlyph = Nothing
                    End If
                    If _sortDownGlyph IsNot Nothing Then
                        _sortDownGlyph.Dispose()
                        _sortDownGlyph = Nothing
                    End If
                End If
            End Sub

            ''' <summary>
            ''' The error glyph used in a large thumbnail
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property ErrorGlyphLarge() As Image
                Get
                    Return _errorGlyphLarge
                End Get
            End Property

            ''' <summary>
            ''' The error glyph used in a small thumbnail (details or list view)
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property ErrorGlyphSmall() As Image
                Get
                    Return _errorGlyphSmall
                End Get
            End Property

            ''' <summary>
            ''' The error glyph used next to a thumbnail as a state image
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property ErrorGlyphState() As Image
                Get
                    Return _errorGlyphState
                End Get
            End Property

            ''' <summary>
            ''' The arrow glyph used in the ListView to show that it is column being sorted
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property SortUpGlyph() As Image
                Get
                    Return _sortUpGlyph
                End Get
            End Property

            ''' <summary>
            ''' The arrow glyph used in the ListView to show that it is column being sorted
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property SortDownGlyph() As Image
                Get
                    Return _sortDownGlyph
                End Get
            End Property
        End Class

#End Region

        ''' <summary>
        ''' Retrieves the set of internal resources that we cache for this instance of the resource editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property CachedResources() As CachedResourcesForView
            Get
                Return _cachedResources
            End Get
        End Property

#End Region


#Region "Project-related utilities"

        ''' <summary>
        ''' Gets the DTE ProjectItem corresponding to the ResX file being edited.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>There's always a ProjectItem, even if you're not opened in a projec, 'cause then you're really opened in the Miscellaneous Files project.</remarks>
        Public Function GetResXProjectItem() As ProjectItem
            'Retrieve the ProjectItem service that we proffered in our designer loader.
            Dim ResXProjectItem As ProjectItem = TryCast(RootDesigner.GetService(GetType(ProjectItem)), ProjectItem)
            If ResXProjectItem IsNot Nothing Then
                Return ResXProjectItem
            Else
                Debug.Fail("ResXProjectItem not found!")
                Return Nothing
            End If
        End Function


        ''' <summary>
        ''' Returns the DTE Project associated with the ResX file being edited.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>If the ResX file isn't opened in the context of a project, this will return the
        '''   Miscellaneous Files project (so in reality it is always opened in the context of
        '''   a project).</remarks>
        Friend Function GetProject() As Project
            'Retrieve the ProjectItem service that we proffered in our designer loader.
            Dim ResXProjectItem As ProjectItem = GetResXProjectItem()
            If ResXProjectItem IsNot Nothing Then
                Dim Project As Project = ResXProjectItem.ContainingProject
                Debug.Assert(Project IsNot Nothing)

                ResourcesFolderService.Trace("Found project: " & Project.Name)
                Return Project
            End If

            ResourcesFolderService.Trace("Unable to locate project - this shouldn't happen, because the Miscellaneous Files project should be picked up if it's not in any other current project")
            Return Nothing
        End Function


        ''' <summary>
        ''' Retrieves the IVsHierarchy interface for the project the resx file is in
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GetVsHierarchy() As IVsHierarchy
            Dim Hierarchy As IVsHierarchy = DirectCast(RootComponent.RootDesigner.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            Debug.Assert(Hierarchy IsNot Nothing, "Couldn't find IVsHierarchy as a service from our root designer - should have been added from our designerloader")
            Return Hierarchy
        End Function


        ''' <summary>
        ''' Retrieves the designer loader associated with this designer
        ''' </summary>
        ''' <returns>The designer loader if found, or else Nothing</returns>
        ''' <remarks>Overridable for unit testing.</remarks>
        Friend Overridable Function GetDesignerLoader() As ResourceEditorDesignerLoader
            Dim DesignerLoaderService As IDesignerLoaderService = DirectCast(RootDesigner().GetService(GetType(IDesignerLoaderService)), IDesignerLoaderService)
            If DesignerLoaderService Is Nothing Then
                Debug.Fail("Unable to retrieve IDesignerLoaderService")
                Return Nothing
            End If

            Dim DesignerLoader As ResourceEditorDesignerLoader = TryCast(DesignerLoaderService, ResourceEditorDesignerLoader)
            If DesignerLoader Is Nothing Then
                Debug.Fail("Unable to cast IDesignerLoaderService to ResourceEditorDesignerLoader")
                Return Nothing
            End If

            Return DesignerLoader
        End Function


        ''' <summary>
        ''' Retrieves the itemid for the resx file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GetVsItemId() As UInteger
            Dim DesignerLoader As ResourceEditorDesignerLoader = GetDesignerLoader()
            If DesignerLoader IsNot Nothing Then
                Return DesignerLoader.ProjectItemid()
            End If

            Return VSITEMID.NIL
        End Function

#End Region


#Region "Resource.ITypeResolutionContextProvider implementation"


        ''' <summary>
        ''' Retrieve a type resolution service for the project that the .resx file is
        '''   opened in.  This is used for resolving type names in the .resx file (because they
        '''   will no longer be fully qualified in Whidbey) according to the project references.
        ''' </summary>
        ''' <returns>The ITypeResolutionService for the project the .resx file was opened in, or else Nothing if it was opened outside the context of a project.</returns>
        ''' <remarks></remarks>
        Public Function GetTypeResolutionService() As System.ComponentModel.Design.ITypeResolutionService Implements Resource.ITypeResolutionContextProvider.GetTypeResolutionService
            If _typeResolutionServiceIsCached Then
                Return _typeResolutionServiceCache
            End If

            Try
                Dim Project As Project = GetProject()
                If Project.UniqueName.Equals(EnvDTE.Constants.vsMiscFilesProjectUniqueName, StringComparison.OrdinalIgnoreCase) Then
                    'This is the Miscellaneous Files project.  That means our resx file is opened outside of the context of a "real"
                    '  project.  We don't want to get references from here, that wouldn't help us any.  Instead return Nothing so that
                    '  we can use a default set of assembly references.
                    _typeResolutionServiceCache = Nothing
                Else
                    Dim Hierarchy As IVsHierarchy = GetVsHierarchy()
                    If Hierarchy Is Nothing Then
                        Return Nothing
                    End If

                    Dim DynamicTypeService As DynamicTypeService = DirectCast(RootComponent.RootDesigner.GetService(GetType(DynamicTypeService)), DynamicTypeService)
                    If DynamicTypeService IsNot Nothing Then
                        _typeResolutionServiceCache = DynamicTypeService.GetTypeResolutionService(Hierarchy)
                    End If
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail("Exception trying to retrieve project references: " & ex.ToString())
                _typeResolutionServiceCache = Nothing
            End Try

            _typeResolutionServiceIsCached = True
            Return _typeResolutionServiceCache 'Value may be Nothing, even if cached.
        End Function


        ''' <summary>
        ''' Retrieves a default set of assemblies to be used for resolving types in the case that 
        '''   a .resx file is opened outside the context of a project.  This is a list of
        '''   assemblies commonly found in .resx files.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function GetDefaultAssemblyReferences() As AssemblyName()
            Return s_defaultAssemblyReferences
        End Function

#End Region


#Region "Disambiguate TextBox UNDO vs shell UNDO"

        'When the Edit.Undo/Redo commands on the shell are enabled (i.e., not when a resx file is first opened,
        '  but as soon as any edits have been made), then the shell grabs CTRL+Z away from the textbox.  Thus the
        '  textbox's undo doesn't work, and instead of changing back just the most recently changed text in the 
        '  textbox for the current edit, the last fully-committed change in the resx is undone instead (quite
        '  unexpected for the user).  We can't simply add our own menu handling for UNDO/REDO, because the shell
        '  routes it to its own undo manager before trying to route it to us (thus we never get it).  To handle this,
        '  we instead implement IOleCommandTarget on our view helper (which we've set up to be our root designer),
        '  because the view helper gets a crack at the command before the undo manager does.
        'The root designer delegates the IOleCommandTarget calls to us here.


        ''' <summary>
        ''' Handles IOleCommandTarget from the view helper (our root designer).
        ''' </summary>
        ''' <param name="CommandGroupGuid">The Guid for the command.</param>
        ''' <param name="CommandId">The command ID for the command.</param>
        ''' <param name="Handled">[out] Set to True if this routine handles the given command exec.</param>
        ''' <remarks>
        ''' Note that many commands will be routed through this call, so do not assume it will be ours.  Only handle
        '''   them if they are actually ours (and then set Handled to True).
        ''' </remarks>
        Public Sub HandleViewHelperCommandExec(ByVal CommandGroupGuid As Guid, ByVal CommandId As UInteger, ByRef Handled As Boolean)
            If StringTable IsNot Nothing AndAlso StringTable.EditingControl IsNot Nothing Then
                Dim EditingTextBox As System.Windows.Forms.TextBox = TryCast(StringTable.EditingControl, System.Windows.Forms.TextBox)
                If EditingTextBox IsNot Nothing Then
                    'The user is currently editing text in the string table.  Take over UNDO/REDO execution from the
                    '  shell and hand it to the textbox instead.

                    If CommandGroupGuid.Equals(Constants.MenuConstants.guidVSStd97) Then
                        Select Case CommandId
                            Case Constants.MenuConstants.cmdidUndo
                                'UNDO/REDO.  Send EM_UNDO to the textbox
                                'The textbox doesn't actually support REDO, but we don't want the shell to 
                                '  grab it, either.  The textbox's UNDO is single-layer, which means pressing
                                '  CTRL+Z first does an UNDO, then a REDO.  So we'll do the same for both
                                '  undo and redo - send EM_UNDO.
                                Dim TextBoxHandleRef As New HandleRef(EditingTextBox, EditingTextBox.Handle)
                                Editors.Interop.NativeMethods.SendMessage(TextBoxHandleRef, Editors.Interop.win.EM_UNDO, 0, 0)
                                Handled = True
                            Case Constants.MenuConstants.cmdidRedo
                                Handled = True
                        End Select
                    End If
                Else
                    Debug.Fail("Unexpected control type in the datagridview in the resource editor")
                End If
            End If
        End Sub

#End Region


#Region "Set and check custom tool and namespace"


        'The custom tool name to use for stand-alone resx files.
        Private Const s_STANDARDCUSTOMTOOL As String = "ResXFileCodeGenerator"
        Private Const s_STANDARDCUSTOMTOOLPUBLIC As String = "PublicResXFileCodeGenerator"

        'The custom tool name to use for the default resx file in VB projects
        Private Const s_VBMYCUSTOMTOOL As String = "VbMyResourcesResXFileCodeGenerator"
        Private Const s_VBMYCUSTOMTOOLPUBLIC As String = "PublicVbMyResourcesResXFileCodeGenerator"

        'The namespace to use
        Private Const s_CUSTOMTOOLNAMESPACE As String = "My.Resources"


        ''' <summary>
        ''' Returns true iff the resx file being edited is the default resx file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsDefaultResXFile() As Boolean
            Dim VsSpecialFiles As IVsProjectSpecialFiles = TryCast(GetVsHierarchy(), IVsProjectSpecialFiles)
            If VsSpecialFiles Is Nothing Then
                'This project doesn't implement this interface (e.g., the misc. files project does not)
                Return False
            End If

            Dim DefResXItemId As UInteger
            Dim DefResXFileName As String = Nothing
            Try
                VSErrorHandler.ThrowOnFailure(VsSpecialFiles.GetFile(__PSFFILEID2.PSFFILEID_AssemblyResource, 0, DefResXItemId, DefResXFileName))
            Catch ex As FileNotFoundException
                'Ignore this error - it may indicate that the default resx file is in the project but is missing from disk.
                Return False
            Catch ex As NotSupportedException
                Return False
            Catch ex As NotImplementedException
                Return False
            Catch ex As COMException When ex.ErrorCode = win.DISP_E_MEMBERNOTFOUND OrElse ex.ErrorCode = win.OLECMDERR_E_NOTSUPPORTED
                'Ignore this, if the project does not support this (like SmartPhone project)...
                Return False
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)

                'If we hit some other unexpected error, we don't want to bomb out, as not being able to load the
                '  designer would be worse than having me not recognize the default resx file.
                Debug.Fail("Unexpected exception in IsDefaultResXFile - returning False")
                Return False
            End Try

            If DefResXItemId = VSITEMID.NIL Then
                'This project doesn't have a default resx file currently
                Return False
            End If

            If GetVsItemId() = DefResXItemId Then
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Tells whether the given file-name is a localized .resx file by comparing the text
        ''' between the next-to-last and last period characters to see if that is a valid culture.
        ''' If it is, this returns True, else it returns False.
        ''' An example is Form1.en-US.resx  [en-US is a valid CultureInfo]
        ''' </summary>
        ''' <param name="fileName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function IsLocalizedResXFile(ByVal fileName As String) As Boolean

            Dim isLocalizedFileName As Boolean = False

            If fileName IsNot Nothing AndAlso Utility.HasResourceFileExtension(fileName) Then

                Dim idx As Integer = fileName.Substring(0, fileName.Length - 5).LastIndexOf("."c)
                If idx > 0 Then
                    Dim cultureString As String = fileName.Substring(idx + 1, fileName.Length - 6 - idx)
                    Try
                        Dim cultureInfo As New CultureInfo(cultureString)
                        If cultureInfo IsNot Nothing Then
                            isLocalizedFileName = True
                        End If
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                    End Try
                End If
            End If

            Return isLocalizedFileName

        End Function

        ''' <summary>
        ''' Returns true if the given project is a C++ project
        ''' </summary>
        ''' <param name="Project"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsCppProject(ByVal Project As Project) As Boolean
            Return Project IsNot Nothing AndAlso (New Guid(Project.Kind).Equals(_projectGuid_CPlusPlus))
        End Function

        ''' <summary>
        ''' Returns true if the given project is a VB project (or a VB for smart devices project)
        ''' </summary>
        ''' <param name="Project"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsVBProject(ByVal Project As Project) As Boolean
            Return Project IsNot Nothing AndAlso (New Guid(Project.Kind).Equals(_projectGuid_VB))
        End Function

        ''' <summary>
        ''' Returns true iff this the default resx file for a VB project
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ShouldUseVbMyResXCodeGenerator() As Boolean
            Return IsVBProject(GetProject()) AndAlso IsDefaultResXFile()
        End Function



        ''' <summary>
        ''' Gets the current value of the Custom Tool property for this resx file.  If the project
        '''   does not support a Custom Tool property, returns Nothing.
        ''' </summary>
        ''' <remarks></remarks>
        Public Function GetCustomToolCurrentValue() As String
            Dim ProjectItem As ProjectItem = GetResXProjectItem()

            Dim ToolProperty As [Property] = GetProjectItemProperty(ProjectItem, PROJECTPROPERTY_CUSTOMTOOL)
            If ToolProperty Is Nothing Then
                'No custom tool property in this project
                Return Nothing
            End If

            Dim CurrentCustomTool As String = TryCast(ToolProperty.Value, String)
            If CurrentCustomTool Is Nothing Then
                CurrentCustomTool = String.Empty
            End If

            Return CurrentCustomTool
        End Function

#End Region


#Region "Global Symbol Rename"

        ''' <summary>
        ''' Attemnpts to call global rename on a resource that has been renamed, if a strongly-typed
        '''   class is being generated for the resx file.
        ''' </summary>
        ''' <param name="OldName">The old name of the resource that was renamed</param>
        ''' <param name="NewName">The new name of the resource</param>
        ''' <remarks></remarks>
        Friend Sub CallGlobalRename(ByVal OldName As String, ByVal NewName As String)
            Dim ProjItem As ProjectItem = GetResXProjectItem()

            ' We only rename the symbol if the current custom tool is our file generator...
            Dim GeneratedClassName As String = GetStronglyGeneratedClassName()
            If Not String.IsNullOrEmpty(GeneratedClassName) Then
                'Find the code element for the generated class
                Dim FindSettingClassFilter As New SettingsDesigner.ProjectUtils.KnownClassName(GeneratedClassName, SettingsDesigner.ProjectUtils.ClassOrModule.Either)
                Dim GeneratedClassCodeElement As CodeElement = SettingsDesigner.ProjectUtils.FindElement(ProjItem, _
                                                                ExpandChildElements:=False, _
                                                                ExpandChildItems:=True, _
                                                                Filter:=FindSettingClassFilter)

                If GeneratedClassCodeElement Is Nothing Then
                    Debug.Fail("We should find a CodeElement for the generated class!? Well, if there isn't one, we can't rename a property on it...")
                    Return
                End If

                'Convert names to language independent identifier using the same routine that will be used in generating the
                '  resx file's strongly-named class code
                Dim FixedOldName As String = Nothing, FixedNewName As String = Nothing
                If Not Resource.ValidateName(RootComponent.ResourceFile, OldName, Nothing, FixedOldName, FixInvalidIDs:=True, CheckForDuplicateNames:=False) OrElse FixedOldName = "" Then
                    'This can happen if the old name was already present in the resx file (manually edited, etc.).  Can't rename
                    '  if the old name was bad
                    Return
                End If
                If Not Resource.ValidateName(RootComponent.ResourceFile, NewName, Nothing, FixedNewName, FixInvalidIDs:=True, CheckForDuplicateNames:=False) OrElse FixedNewName = "" Then
                    Debug.Fail("CallGlobalRename: new name failed validation - this should have been caught earlier")
                    Return
                End If

                OldName = FixedOldName
                NewName = FixedNewName

                If OldName.Equals(NewName, StringComparison.Ordinal) Then
                    'If we're renaming to the same name (e.g., renaming "A B" to "A_B" - the fixed-up names will be the same),
                    '  there's nothing to do (if we try to do this, an exception will be thrown)
                    Return
                End If

                'Find the code element for the property definition of the resource in the generated class that we're looking for
                Dim PropertyDefinitionCodeElement2 As EnvDTE80.CodeElement2 = Nothing

                Dim FindPropertyFilter As New SettingsDesigner.ProjectUtils.FindPropertyFilter(GeneratedClassCodeElement, OldName)
                Dim PropertyDefinitionCodeElement As CodeElement = SettingsDesigner.ProjectUtils.FindElement(ProjItem, ExpandChildElements:=True, ExpandChildItems:=True, Filter:=FindPropertyFilter)
                If PropertyDefinitionCodeElement IsNot Nothing Then
                    PropertyDefinitionCodeElement2 = TryCast(PropertyDefinitionCodeElement, EnvDTE80.CodeElement2)
                    Debug.Assert(PropertyDefinitionCodeElement2 IsNot Nothing, "Failed to get CodeElement2 interface from CodeElement - CodeModel doesn't support ReplaceSymbol?")
                Else
                    ' Try to find get_Property because some languages do not support property directly (like J#)
                    Dim FindFunctionFilter As New SettingsDesigner.ProjectUtils.FindFunctionFilter(GeneratedClassCodeElement, "get_" & OldName)
                    Dim FunctionCodeElement As CodeElement = SettingsDesigner.ProjectUtils.FindElement(ProjItem, ExpandChildElements:=True, ExpandChildItems:=True, Filter:=FindFunctionFilter)
                    If FunctionCodeElement IsNot Nothing Then
                        OldName = "get_" & OldName
                        NewName = "get_" & NewName
                        PropertyDefinitionCodeElement2 = TryCast(FunctionCodeElement, EnvDTE80.CodeElement2)
                        Debug.Assert(PropertyDefinitionCodeElement2 IsNot Nothing, "Failed to get CodeElement2 interface from CodeElement - CodeModel doesn't support ReplaceSymbol?")
                    End If
                End If

                If PropertyDefinitionCodeElement2 IsNot Nothing Then
                    'Tell the code model to rename this property, which will also remain all references to it elsewhere in code.
                    '  Note that the property's code still refers to the old resource name, but we don't care because
                    '  this class will get regenerated anyway.
                    Try
                        ResourceEditorRefactorNotify.AllowSymbolRename = True
                        PropertyDefinitionCodeElement2.RenameSymbol(NewName)
                    Catch ex As COMException When ex.ErrorCode = Common.CodeModelUtils.HR_E_CSHARP_USER_CANCEL _
                                                  OrElse ex.ErrorCode = NativeMethods.E_ABORT _
                                                  OrElse ex.ErrorCode = NativeMethods.OLECMDERR_E_CANCELED _
                                                  OrElse ex.ErrorCode = NativeMethods.E_FAIL
                        ' We should ignore if the customer cancels this or we can not build the project...
                    Catch ex As Exception
                        Common.Utils.RethrowIfUnrecoverable(ex, True)
                        DsMsgBox(ex)
                    Finally
                        ResourceEditorRefactorNotify.AllowSymbolRename = False
                    End Try
                End If
            End If
        End Sub


        ''' <summary>
        ''' Gets the fully qualified name for the strongly-generated class name, but only if the single file
        '''   generator is our own.  Otherwise returns empty.
        ''' </summary>
        ''' <returns>The name of the strongly-typed generated class, if any, otherwise String.Empty.</returns>
        ''' <remarks></remarks>
        Friend Function GetStronglyGeneratedClassName() As String

            If RootComponent.RootDesigner.IsEditingResWFile() Then
                ' Code gen is not supported currently for resw files
                Return String.Empty
            End If

            Dim ProjItem As ProjectItem = GetResXProjectItem()
            Dim ToolProperty As [Property] = GetProjectItemProperty(ProjItem, PROJECTPROPERTY_CUSTOMTOOL)
            If ToolProperty Is Nothing Then
                'This project type has no CustomTool property, so there is no strongly-generated
                '  class.  Return Nothing
                Return String.Empty
            End If

            Dim CurrentCustomTool As String = TryCast(ToolProperty.Value, String)
            Dim Hierarchy As IVsHierarchy = GetVsHierarchy()
            Dim ItemId As UInteger = GetVsItemId()
            Dim UsingVbMyCustomTool As Boolean

            If CurrentCustomTool.Equals(s_STANDARDCUSTOMTOOL, StringComparison.OrdinalIgnoreCase) Then
                'This uses our standard single file generator.  We know how it works, so we can go ahead and
                '  attempt a rename.
            ElseIf CurrentCustomTool.Equals(s_VBMYCUSTOMTOOL, StringComparison.OrdinalIgnoreCase) Then
                'This uses our special My.VB file generator.  We can attempt a rename.
                UsingVbMyCustomTool = True
            Else
                'We don't recognize this generator, so we don't want to try renaming, so we return empty string.
                Return String.Empty
            End If


            Dim ClassName As String = GetGeneratedClassNameFromFileName(Path.GetFileName(RootDesigner.GetResXFileNameAndPath()))
            Dim Project As Project = GetProject()
            Dim RootNamespace As String = GetRootNamespace(Hierarchy, ItemId, Project)

            'Now get the custom tool namespace
            Dim CustomToolNamespace As String = String.Empty
            Dim CustomToolNamespaceProperty As [Property] = GetProjectItemProperty(ProjItem, PROJECTPROPERTY_CUSTOMTOOLNAMESPACE)
            If CustomToolNamespaceProperty IsNot Nothing Then
                CustomToolNamespace = TryCast(CustomToolNamespaceProperty.Value, String)
            End If

            If Not String.IsNullOrEmpty(CustomToolNamespace) Then
                CustomToolNamespace = DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(CustomToolNamespace)

                Dim codeModel As EnvDTE.CodeModel = Project.CodeModel
                ' NOTE: VB will append the custom setting after the RootNamespace (which is done by compiler), but other language does not...
                If codeModel IsNot Nothing AndAlso String.Compare(codeModel.Language, EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB, StringComparison.OrdinalIgnoreCase) = 0 Then
                    'Build up the qualified name
                    Return CombineNamespaces(CombineNamespaces(RootNamespace, CustomToolNamespace), ClassName)
                Else
                    Return CombineNamespaces(CustomToolNamespace, ClassName)
                End If
            End If

            Dim defaultNamespace As String = GetDefaultNamespace(Hierarchy, ItemId, Project)
            If String.IsNullOrEmpty(defaultNamespace) Then
                defaultNamespace = RootNamespace
            End If

            Return CombineNamespaces(defaultNamespace, ClassName)
        End Function


        ''' <summary>
        ''' Gets the namespace that will be generated for the given hierarchy/itemid
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="Project"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetDefaultNamespace(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal Project As Project) As String
            Dim objDefaultNamespace As Object = Nothing
            Hierarchy.GetProperty(ItemId, __VSHPROPID.VSHPROPID_DefaultNamespace, objDefaultNamespace)

            Dim DefaultNamespace As String = String.Empty
            If TypeOf objDefaultNamespace Is String Then
                DefaultNamespace = DirectCast(objDefaultNamespace, String)
                DefaultNamespace = DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(DefaultNamespace)
            End If

            Return DefaultNamespace
        End Function

        ''' <summary>
        ''' Gets the namespace that will be generated for the given hierarchy/itemid
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="Project"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetRootNamespace(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal Project As Project) As String
            Dim RootNamespace As String = String.Empty
            If Project IsNot Nothing Then
                Try
                    Dim RootNamespaceProperty As [Property] = GetProjectProperty(Project, "RootNamespace")
                    If RootNamespaceProperty IsNot Nothing Then
                        RootNamespace = TryCast(RootNamespaceProperty.Value, String)
                        RootNamespace = DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(RootNamespace)
                    End If
                Catch ex As COMException
                    Debug.Fail("Assertion trying to get RootNamespace: " & ex.Message)
                End Try
            End If

            Return RootNamespace
        End Function


        ''' <summary>
        ''' Given a filename, determines what the generated class name would be
        ''' </summary>
        ''' <param name="FileName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GetGeneratedClassNameFromFileName(ByVal FileName As String) As String
            Return DesignerFramework.DesignUtil.GenerateValidLanguageIndependentIdentifier(Path.GetFileNameWithoutExtension(FileName))
        End Function

#End Region


#Region "Undo State"

        ''' <summary>
        ''' handle Undo Event, it will be called when undo is started
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnUndoing()
            _inUndoing = True
            _categoryAffected = Nothing
            If _resourcesAffected IsNot Nothing Then
                _resourcesAffected.Clear()
            End If
        End Sub

        ''' <summary>
        ''' handle Undo Event, it will be called when undo is done
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnUndone()
            _inUndoing = False

            If _categoryAffected IsNot Nothing Then
                If _categoryAffected IsNot _currentCategory Then
                    SwitchToCategory(_categoryAffected)
                    _categoryAffected = Nothing
                End If

                UnselectAllResources()
                If _resourcesAffected IsNot Nothing Then
                    HighlightResources(_resourcesAffected.Keys, True)
                    _resourcesAffected.Clear()
                End If
            End If
        End Sub

#End Region


#Region "WndProc handling"

        ''' <summary>
        ''' Handle custom window messages...
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub WndProc(ByRef m As Message)
            Select Case m.Msg
                Case Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_UPDATE_PROPERTY_GRID
                    OnWmUpdatePropertyGrid()
                    Return
            End Select
            MyBase.WndProc(m)
        End Sub

#End Region


#Region "Debugging code"

#If DEBUG Then
        Protected Overrides Sub OnLayout(ByVal levent As System.Windows.Forms.LayoutEventArgs)
            Switches.TracePDPerf("OnLayout BEGIN: ResourceEditorView.OnLayout()")
            MyBase.OnLayout(levent)
            Switches.TracePDPerf("   OnLayout END: ResourceEditorView.OnLayout()")
        End Sub
#End If

#End Region

    End Class

End Namespace


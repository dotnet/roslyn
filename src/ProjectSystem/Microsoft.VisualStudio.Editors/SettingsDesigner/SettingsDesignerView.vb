Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Configuration
Imports System.Globalization
Imports System.Web.ClientServices.Providers
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.Xml

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Editors.PropertyPages
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VSDesigner.VSDesignerPackage

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The user control that allows the user to interact with the designed component
    ''' For each row in the grid, the Tag property should be set to a corresponding 
    ''' DesignTimeSettingInstance
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class SettingsDesignerView
        Inherits Microsoft.VisualStudio.Editors.DesignerFramework.BaseDesignerView
        Implements IVsWindowPaneCommit
        Implements IVsBroadcastMessageEvents

        Private Const NameColumnNo As Integer = 0
        Private Const TypeColumnNo As Integer = 1
        Private Const ScopeColumnNo As Integer = 2
        Private Const ValueColumnNo As Integer = 3
        Private m_menuCommands As ArrayList
        Private m_accessModifierCombobox As SettingsDesignerAccessModifierCombobox

#Region "Nested Class for the 'Access modifier' dropdown'"

        Friend Class SettingsDesignerAccessModifierCombobox
            Inherits AccessModifierCombobox

            Public Sub New(ByVal rootDesigner As BaseRootDesigner, ByVal serviceProvider As IServiceProvider, ByVal projectItem As EnvDTE.ProjectItem, ByVal namespaceToOverrideIfCustomToolIsEmpty As String)
                MyBase.New(rootDesigner, serviceProvider, projectItem, namespaceToOverrideIfCustomToolIsEmpty)

                AddCodeGeneratorEntry(AccessModifierConverter.Access.Friend, SettingsSingleFileGenerator.SingleFileGeneratorName)
                AddCodeGeneratorEntry(AccessModifierConverter.Access.Public, PublicSettingsSingleFileGenerator.SingleFileGeneratorName)

                'Make sure both the internal and public custom tool values are "recognized"
                AddRecognizedCustomToolValue(SettingsSingleFileGenerator.SingleFileGeneratorName)
                AddRecognizedCustomToolValue(PublicSettingsSingleFileGenerator.SingleFileGeneratorName)
            End Sub

            Public Shadows Function GetMenuCommandsToRegister() As ICollection
                Return MyBase.GetMenuCommandsToRegister( _
                    Constants.MenuConstants.CommandIDSettingsDesignerAccessModifierCombobox, _
                    Constants.MenuConstants.CommandIDSettingsDesignerGetAccessModifierOptions)
            End Function

            Protected Overrides Function IsDesignerEditable() As Boolean
                'UNDONE: test SCC checkout
                Dim designerLoader As SettingsDesignerLoader = TryCast(RootDesigner.GetService(GetType(IDesignerLoaderService)), SettingsDesignerLoader)
                If designerLoader Is Nothing Then
                    Debug.Fail("Failed to get the designer loader")
                    Return False
                End If

                Return designerLoader.InDesignMode AndAlso Not DesignerLoader.IsReadOnly
            End Function
        End Class

        ''' <summary>
        ''' Wrapper class that has the ability to indicate to the settings designer view
        ''' that it is a really bad time to change the current cell while already committing
        ''' changes...
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class SettingsGridView
            Inherits DesignerDataGridView

            Private m_committingChanges As Boolean

            Friend Property CommittingChanges() As Boolean
                Get
                    Return m_committingChanges
                End Get
                Set(ByVal value As Boolean)
                    m_committingChanges = value
                End Set
            End Property
        End Class

#End Region

        ' The "actual" grid containing all settings
        Friend WithEvents m_SettingsGridView As SettingsGridView

        ' Padding used to calculate width of comboboxes to avoid getting the text
        ' truncated...
        Private Const InternalComboBoxPadding As Integer = 10

        Friend WithEvents DataGridViewTextBoxColumn1 As System.Windows.Forms.DataGridViewTextBoxColumn
        Friend WithEvents DataGridViewComboBoxColumn1 As System.Windows.Forms.DataGridViewComboBoxColumn
        Friend WithEvents DataGridViewComboBoxColumn2 As System.Windows.Forms.DataGridViewComboBoxColumn
        Friend WithEvents DescriptionLinkLabel As VSThemedLinkLabel

        Private m_suppressValidationUI As Boolean
        Friend WithEvents SettingsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Private m_isReportingError As Boolean
        Private m_isShowingTypePicker As Boolean
        Private m_toolbarPanel As DesignerToolbarPanel

        ' Does the current language support partial classes? If yes, enable the ViewCode button...
        Private m_viewCodeEnabled As Boolean
        Private m_cachedCodeProvider As CodeDom.Compiler.CodeDomProvider

        ' Does the project system support user scoped settings?
        Private m_projectSystemSupportsUserScope As Boolean

        'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
        Private m_CookieBroadcastMessages As UInteger

        ' Prevent recursive validation (sometimes we do things in cell validated that causes the
        ' focus to move, which causes additional cellvalidated events)
        Private m_inCellValidated As Boolean

        ''' <summary>
        ''' Cached instance of the type name resoloution service
        ''' </summary>
        ''' <remarks></remarks>
        Private m_typeNameResolver As SettingTypeNameResolutionService

        ''' <summary>
        ''' Cached instance of the setting type cache service
        ''' </summary>
        ''' <remarks></remarks>
        Private m_settingTypeCache As SettingsTypeCache

        ''' <summary>
        ''' Cached instance of the setting value cache
        ''' </summary>
        ''' <remarks></remarks>
        Private m_valueCache As SettingsValueCache


#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            SuspendLayout()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            Me.SettingsTableLayoutPanel.SuspendLayout()

            m_SettingsGridView.Columns(NameColumnNo).HeaderText = SR.GetString(SR.SD_GridViewNameColumnHeaderText)
            m_SettingsGridView.Columns(NameColumnNo).CellTemplate = New DesignerFramework.DesignerDataGridView.EditOnClickDataGridViewTextBoxCell()
            m_SettingsGridView.Columns(TypeColumnNo).HeaderText = SR.GetString(SR.SD_GridViewTypeColumnHeaderText)
            m_SettingsGridView.Columns(TypeColumnNo).CellTemplate = New DesignerFramework.DesignerDataGridView.EditOnClickDataGridViewComboBoxCell()
            m_SettingsGridView.Columns(ScopeColumnNo).HeaderText = SR.GetString(SR.SD_GridViewScopeColumnHeaderText)
            m_SettingsGridView.Columns(ScopeColumnNo).CellTemplate = New DesignerFramework.DesignerDataGridView.EditOnClickDataGridViewComboBoxCell()

            Dim TypeEditorCol As New DataGridViewUITypeEditorColumn
            TypeEditorCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
            TypeEditorCol.FillWeight = 100.0!
            TypeEditorCol.HeaderText = SR.GetString(SR.SD_GridViewValueColumnHeaderText)
            TypeEditorCol.MinimumWidth = DpiHelper.LogicalToDeviceUnitsX(System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 2) ' Add 2 for left/right borders...
            TypeEditorCol.Resizable = DataGridViewTriState.True
            TypeEditorCol.SortMode = DataGridViewColumnSortMode.Automatic
            TypeEditorCol.Width = DpiHelper.LogicalToDeviceUnitsX(200)
            m_SettingsGridView.Columns.Add(TypeEditorCol)


            m_SettingsGridView.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            m_SettingsGridView.Text = "m_SettingsGridView"
            m_SettingsGridView.DefaultCellStyle.NullValue = ""

            ScopeColumn.Items.Add(DesignTimeSettingInstance.SettingScope.Application)
            ScopeColumn.Items.Add(DesignTimeSettingInstance.SettingScope.User)

            SetLinkLabelText()

            m_SettingsGridView.ColumnHeadersHeight = m_SettingsGridView.Rows(0).GetPreferredHeight(0, DataGridViewAutoSizeRowMode.AllCells, False)
            m_toolbarPanel = New DesignerToolbarPanel
            m_toolbarPanel.Name = "ToolbarPanel"
            m_toolbarPanel.Text = "ToolbarPanel"
            Me.SettingsTableLayoutPanel.Controls.Add(Me.m_toolbarPanel, 0, 0)
            Me.SettingsTableLayoutPanel.ResumeLayout()
            ResumeLayout()
        End Sub

        ''' <summary>
        ''' Dispose my resources
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If m_accessModifierCombobox IsNot Nothing Then
                    m_accessModifierCombobox.Dispose()
                End If

                If m_CookieBroadcastMessages <> 0 Then
                    Dim VsShell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                    If VsShell IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(VsShell.UnadviseBroadcastMessages(m_CookieBroadcastMessages))
                        m_CookieBroadcastMessages = 0
                    End If
                End If

                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
                ' Forget about any component change service
                Me.ChangeService = Nothing

                ' Remove any dependencies on the current settings instance...
                Me.Settings = Nothing

            End If
            ' Don't forget to let my base dispose itself
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SettingsDesignerView))
            Me.m_SettingsGridView = New SettingsGridView
            Me.BackColor = Common.ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_THREEDFACE, System.Drawing.SystemColors.ButtonFace, UseVSTheme:=False)
            Me.DescriptionLinkLabel = New VSThemedLinkLabel
            Me.DataGridViewTextBoxColumn1 = New System.Windows.Forms.DataGridViewTextBoxColumn
            Me.DataGridViewComboBoxColumn1 = New System.Windows.Forms.DataGridViewComboBoxColumn
            Me.DataGridViewComboBoxColumn2 = New System.Windows.Forms.DataGridViewComboBoxColumn
            Me.SettingsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            CType(Me.m_SettingsGridView, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SettingsTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'm_SettingsGridView
            '
            resources.ApplyResources(Me.m_SettingsGridView, "m_SettingsGridView")
            Me.m_SettingsGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None
            Me.m_SettingsGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
            Me.m_SettingsGridView.BackgroundColor = Common.ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_THREEDFACE, System.Drawing.SystemColors.ButtonFace, UseVSTheme:=False)
            Me.m_SettingsGridView.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable
            Me.m_SettingsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.m_SettingsGridView.Columns.Add(Me.DataGridViewTextBoxColumn1)
            Me.m_SettingsGridView.Columns.Add(Me.DataGridViewComboBoxColumn1)
            Me.m_SettingsGridView.Columns.Add(Me.DataGridViewComboBoxColumn2)
            resources.ApplyResources(Me.m_SettingsGridView, "m_SettingsGridView")
            Me.m_SettingsGridView.Margin = New System.Windows.Forms.Padding(14)
            Me.m_SettingsGridView.Name = "m_SettingsGridView"
            '
            'DataGridViewTextBoxColumn1
            '
            resources.ApplyResources(Me.DataGridViewTextBoxColumn1, "DataGridViewTextBoxColumn1")
            Me.DataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
            Me.DataGridViewTextBoxColumn1.MinimumWidth = DpiHelper.LogicalToDeviceUnitsX(100)
            Me.DataGridViewTextBoxColumn1.Name = "GridViewNameTextBoxColumn"
            Me.DataGridViewComboBoxColumn1.Width = DpiHelper.LogicalToDeviceUnitsX(100)
            '
            'DataGridViewComboBoxColumn1
            '
            resources.ApplyResources(Me.DataGridViewComboBoxColumn1, "DataGridViewComboBoxColumn1")
            Me.DataGridViewComboBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            Me.DataGridViewComboBoxColumn1.MinimumWidth = DpiHelper.LogicalToDeviceUnitsX(100)
            Me.DataGridViewComboBoxColumn1.Name = "GridViewTypeComboBoxColumn"
            Me.DataGridViewComboBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic
            Me.DataGridViewComboBoxColumn1.Width = DpiHelper.LogicalToDeviceUnitsX(100)
            '
            'DataGridViewComboBoxColumn2
            '
            Me.DataGridViewComboBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
            resources.ApplyResources(Me.DataGridViewComboBoxColumn2, "DataGridViewComboBoxColumn2")
            Me.DataGridViewComboBoxColumn2.MaxDropDownItems = 2
            Me.DataGridViewComboBoxColumn2.MinimumWidth = DpiHelper.LogicalToDeviceUnitsX(100)
            Me.DataGridViewComboBoxColumn2.Name = "GridViewScopeComboBoxColumn"
            Me.DataGridViewComboBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic
            Me.DataGridViewComboBoxColumn2.ValueType = GetType(DesignTimeSettingInstance.SettingScope)
            Me.DataGridViewComboBoxColumn2.Width = DpiHelper.LogicalToDeviceUnitsX(100)
            '
            'DescriptionLinkLabel
            '
            resources.ApplyResources(Me.DescriptionLinkLabel, "DescriptionLinkLabel")
            Me.DescriptionLinkLabel.Margin = New System.Windows.Forms.Padding(14, 23, 14, 9)
            Me.DescriptionLinkLabel.Name = "DescriptionLinkLabel"
            Me.DescriptionLinkLabel.TabStop = True
            '
            'SettingsTableLayoutPanel
            '
            Me.SettingsTableLayoutPanel.ColumnCount = 1
            Me.SettingsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.SettingsTableLayoutPanel.Controls.Add(Me.m_SettingsGridView, 0, 2)
            Me.SettingsTableLayoutPanel.Controls.Add(Me.DescriptionLinkLabel, 0, 1)
            resources.ApplyResources(Me.SettingsTableLayoutPanel, "SettingsTableLayoutPanel")
            Me.SettingsTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.SettingsTableLayoutPanel.Name = "SettingsTableLayoutPanel"
            Me.SettingsTableLayoutPanel.RowCount = 3
            Me.SettingsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.SettingsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.SettingsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            '
            'SettingsDesignerView
            '
            Me.Controls.Add(Me.SettingsTableLayoutPanel)
            Me.AutoScaleMode = Windows.Forms.AutoScaleMode.Font
            Me.Margin = New System.Windows.Forms.Padding(0)
            Me.Name = "SettingsDesignerView"
            Me.Padding = New System.Windows.Forms.Padding(0)
            resources.ApplyResources(Me, "$this")
            CType(Me.m_SettingsGridView, System.ComponentModel.ISupportInitialize).EndInit()
            Me.SettingsTableLayoutPanel.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub

#End Region

#Region "Private fields"

        ''' <summary>
        ''' Reference to "our" root designer
        ''' </summary>
        ''' <remarks></remarks>
        Private m_RootDesigner As SettingsDesigner

        ''' <summary>
        ''' The settings we show in the grid
        ''' </summary>
        ''' <remarks></remarks>
        Private m_SettingsProperty As DesignTimeSettings

        ' Private cached service
        Private m_designerLoader As SettingsDesignerLoader

        ' Cached IVsHierarchy
        Private m_hierarchy As IVsHierarchy

#End Region

        ''' <summary>
        ''' Set the designer associated with this view
        ''' </summary>
        ''' <param name="Designer"></param>
        ''' <remarks>
        ''' When setting the designer, a complete refresh of the view is performed.
        ''' </remarks>
        Public Sub SetDesigner(ByVal Designer As SettingsDesigner)
            Dim types As System.Collections.Generic.IEnumerable(Of Type)
            If m_RootDesigner IsNot Nothing Then
                UnregisterMenuCommands(m_RootDesigner)
            End If
            m_RootDesigner = Designer

            Debug.Assert(Designer IsNot Nothing)
            Debug.Assert(DesignerLoader IsNot Nothing)
            Debug.Assert(DesignerLoader.ProjectItem IsNot Nothing)
            Debug.Assert(DesignerLoader.VsHierarchy IsNot Nothing)
            m_accessModifierCombobox = New SettingsDesignerAccessModifierCombobox( _
                Designer, _
                Designer, _
                DesignerLoader.ProjectItem, _
                IIf(Common.Utils.IsVbProject(DesignerLoader.VsHierarchy), SettingsSingleFileGenerator.MyNamespaceName, Nothing))

            m_valueCache = DirectCast(GetService(GetType(SettingsValueCache)), SettingsValueCache)
            m_settingTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)

            m_typeNameResolver = DirectCast(GetService(GetType(SettingTypeNameResolutionService)), SettingTypeNameResolutionService)
            Debug.Assert(m_typeNameResolver IsNot Nothing, "The settings designer loader should have added a typenameresolver component!")

            ' Add all the (currently) known types 
            TypeColumn.Items.Clear()
            types = m_settingTypeCache.GetWellKnownTypes()
            For Each t As Type In types
                TypeColumn.Items.Add(m_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(t.FullName))
            Next

            ' Make sure the "normal" types are sorted...
            TypeColumn.Sorted = True
            TypeColumn.Sorted = False

            ' Add the "connection string" pseudo type
            TypeColumn.Items.Add(m_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(SettingsSerializer.CultureInvariantVirtualTypeNameWebReference))
            TypeColumn.Items.Add(m_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString))
            TypeColumn.Items.Add(SR.GetString(SR.SD_ComboBoxItem_BrowseType))
            TypeColumn.Width = DpiHelper.LogicalToDeviceUnitsX(TypeColumn.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, False) + System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + InternalComboBoxPadding)


            ScopeColumn.Width = DpiHelper.LogicalToDeviceUnitsX(ScopeColumn.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, False) + System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + InternalComboBoxPadding)

            'Hook up for broadcast messages
            If m_CookieBroadcastMessages = 0 Then
                Dim VSShell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                If VSShell IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(VSShell.AdviseBroadcastMessages(Me, m_CookieBroadcastMessages))
                Else
                    Debug.Fail("Unable to get IVsShell for broadcast messages")
                End If
            End If
            Me.SetFonts()

            If Designer.Settings IsNot Nothing AndAlso Designer.Settings.Site IsNot Nothing Then
                m_hierarchy = DirectCast(Designer.Settings.Site.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            Else
                m_hierarchy = Nothing
            End If

            If m_hierarchy IsNot Nothing Then
                m_projectSystemSupportsUserScope = Not Common.ShellUtil.IsWebProject(m_hierarchy)
            Else
                m_projectSystemSupportsUserScope = True
            End If

            Settings = Designer.Settings


            ' ...get new changes service...
            ChangeService = DirectCast(Designer.GetService(GetType(IComponentChangeService)), IComponentChangeService)

            Dim VsUIShell As IVsUIShell = DirectCast(Designer.GetService(GetType(IVsUIShell)), IVsUIShell)

            ' Register menu commands...
            RegisterMenuCommands(Designer)
            m_toolbarPanel.SetToolbar(VsUIShell, Constants.MenuConstants.GUID_SETTINGSDESIGNER_MenuGroup, Constants.MenuConstants.IDM_VS_TOOLBAR_Settings)
            m_toolbarPanel.BringToFront()

            Me.DescriptionLinkLabel.SetThemedColor(TryCast(VsUIShell, IVsUIShell5))

        End Sub

        ''' <summary>
        ''' Gets the environment font for the shell.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetEnvironmentFont() As System.Drawing.Font
            If UIService IsNot Nothing Then
                Dim Font As System.Drawing.Font = DirectCast(UIService.Styles("DialogFont"), System.Drawing.Font)
                Debug.Assert(Font IsNot Nothing, "Unable to get dialog font from IUIService")
                Return Font
            Else
                Debug.Fail("Unable to get IUIService for dialog font")
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Set the localized text and link part of the link label
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetLinkLabelText()
            Dim fullText As String = SR.GetString(SR.SD_FullDescriptionText)
            Dim linkText As String = SR.GetString(SR.SD_LinkPartOfDescriptionText)

            ' Adding two spaces and including the first space in the link due to VsWhidbey 482875
            DescriptionLinkLabel.Text = fullText & "  " & linkText

            DescriptionLinkLabel.Links.Clear()

            ' Adding one to the length of the linkText 'cause we have included one of the two leading spaces
            ' in the link (see above)
            DescriptionLinkLabel.Links.Add(fullText.Length() + 1, linkText.Length + 1)
        End Sub

        ''' <summary>
        ''' Pop up the appropriate help context when the user clicks on the description link
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DescriptionLinkLabel_LinkClicked(ByVal sender As Object, ByVal e As LinkLabelLinkClickedEventArgs) Handles DescriptionLinkLabel.LinkClicked
            DesignerFramework.DesignUtil.DisplayTopicFromF1Keyword(m_RootDesigner, HelpIDs.SettingsDesignerDescription)
        End Sub


        ''' <summary>
        ''' Initialize the fonts in the resource editor from the environment (or from the resx file,
        '''   if hard-coded there).
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetFonts()
            Dim DialogFont As System.Drawing.Font = GetEnvironmentFont()
            If DialogFont IsNot Nothing Then
                Me.Font = DialogFont
            End If

            Common.Utils.SetComboBoxColumnDropdownWidth(TypeColumn)
            Common.Utils.SetComboBoxColumnDropdownWidth(ScopeColumn)
        End Sub

        ''' <summary>
        ''' The settings we are currently displaying in the grid...
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Property Settings() As DesignTimeSettings
            Get
                Return m_SettingsProperty
            End Get
            Set(ByVal Value As DesignTimeSettings)
                ' Setting the settings to the same instance is a NOOP
                If Value IsNot Settings Then
                    ' Store this guy for later use!
                    m_SettingsProperty = Value

                    If Settings IsNot Nothing Then
                        RefreshGrid()
                        ' We want to give the name column a reasonable start width. If we did this by using auto fill/fill weight, 
                        ' changing the value column would change the size of the name column, which looks weird. We'll just default the
                        ' size to 1/3 of the value column's width and leave it at that (the user can resize if (s)he wants to)
                        m_SettingsGridView.Columns(NameColumnNo).Width = CInt(TypeColumn.Width / 2)
                    End If
                End If
            End Set
        End Property

        Private m_ChangeService As IComponentChangeService
        ''' <summary>
        ''' Our cached component change service
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Will unhook event handlers from old component changed service and hook up handlers
        ''' to the new service
        '''</remarks>
        Friend Property ChangeService() As IComponentChangeService
            Get
                Return m_ChangeService
            End Get
            Set(ByVal Value As IComponentChangeService)
                If Not Value Is m_ChangeService Then
                    UnSubscribeChangeServiceNotifications()
                    m_ChangeService = Value
                    SubscribeChangeServiceNotifications()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Hook up component changed/added/removed event handlers
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SubscribeChangeServiceNotifications()
            If ChangeService IsNot Nothing Then
                AddHandler ChangeService.ComponentChanged, AddressOf Me.ComponentChangedHandler
                AddHandler ChangeService.ComponentRemoved, AddressOf Me.ComponentRemovedHandler
                AddHandler ChangeService.ComponentAdded, AddressOf Me.ComponentAddedHandler
            End If
        End Sub

        ''' <summary>
        ''' Unhook component changed/added/removed event handlers
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UnSubscribeChangeServiceNotifications()
            If ChangeService IsNot Nothing Then
                RemoveHandler ChangeService.ComponentChanged, AddressOf Me.ComponentChangedHandler
                RemoveHandler ChangeService.ComponentRemoved, AddressOf Me.ComponentRemovedHandler
                RemoveHandler ChangeService.ComponentAdded, AddressOf Me.ComponentAddedHandler
            End If
        End Sub

        ''' <summary>
        ''' A component in our hosts container has changed
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentChangedHandler(ByVal Sender As Object, ByVal e As System.ComponentModel.Design.ComponentChangedEventArgs)
            ' There is a slight possibility that we'll be called after the designer has been disposed (a web reference
            ' rename can cause a project file checkout, which may cause a project reload, which will dispose us and
            ' once it is our turn to get the component change notification, we are already disposed)
            ' 
            ' Fortunately, the fix is easy - we only need to bail if we have already been disposed....
            If IsDisposed Then
                Return
            End If

            If TypeOf e.Component Is DesignTimeSettingInstance Then
                Dim Instance As DesignTimeSettingInstance = DirectCast(e.Component, DesignTimeSettingInstance)
                Dim Row As DataGridViewRow = RowFromComponent(Instance)
                If Row Is Nothing Then
                    Debug.Fail("ComponentChanged: Failed to find row...")
                Else
                    Me.SetUIRowValues(Row, Instance)
                End If
            End If
        End Sub

        ''' <summary>
        ''' A component was removed from our hosts container
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentRemovedHandler(ByVal Sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs)
            If TypeOf e.Component Is DesignTimeSettingInstance Then
                ' This was a setting instance - let's find the corresponding row and
                ' remove it from the grid
                Dim Row As DataGridViewRow = RowFromComponent(DirectCast(e.Component, DesignTimeSettingInstance))
                If Row IsNot Nothing Then
                    If Row.Index = m_SettingsGridView.RowCount - 1 Then
                        'This is the "new row" - it can't be removed 
                        Row.Tag = Nothing
                    Else
                        ' If we are currently editing something while the row is removed,
                        ' then we should cancel the edit. 
                        ' If not, we may run into issues like described in DevDiv 85344
                        If m_settingsGridView.IsCurrentCellInEditMode Then
                            m_settingsGridView.CancelEdit()
                        End If
                        m_SettingsGridView.Rows.Remove(Row)
                    End If
                End If
            Else
                ' This wasn't a remove of a setting instance!
                Debug.Fail("Unknown component removed?")
            End If
        End Sub

        ''' <summary>
        ''' A component was added to our hosts container
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentAddedHandler(ByVal Sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs)
            If GetType(DesignTimeSettingInstance).IsAssignableFrom(CType(e.Component, Object).GetType()) Then
                ' A component was added - let's get the corresponding row from the grid...
                Dim Row As DataGridViewRow = RowFromComponent(DirectCast(e.Component, DesignTimeSettingInstance))

                If Row IsNot Nothing Then
                    ' This component was already showing...
                    Return
                Else
                    ' No row corresponding to this settings - we better add one!
                    Debug.Assert(m_SettingsGridView.RowCount >= 1)
                    ' We'll have to create a new row!
                    Dim NewRowIndex As Integer
                    NewRowIndex = m_SettingsGridView.Rows.Add()
                    ' Now, we don't want the last row, since that is the special "New row"
                    ' Let's grab the second last row...
                    Row = m_SettingsGridView.Rows(NewRowIndex)
                    Debug.Assert(NewRowIndex = m_SettingsGridView.RowCount - 2, "Why wasn't the new row added last?")
                    Row.Tag = e.Component
                End If
                SetUIRowValues(Row, DirectCast(e.Component, DesignTimeSettingInstance))
            Else
                ' This wasn't a remove of a setting instance!
                Debug.Fail("Unknown component type added!")
            End If
        End Sub

        ''' <summary>
        ''' Get a row from a component
        ''' </summary>
        ''' <param name="Instance"></param>
        ''' <returns></returns>
        ''' <remarks>O(n) running time</remarks>
        Private Function RowFromComponent(ByVal Instance As DesignTimeSettingInstance) As DataGridViewRow
            For Each Row As DataGridViewRow In m_SettingsGridView.Rows
                If Row.Tag Is Instance Then
                    Return Row
                End If
            Next
            Return Nothing
        End Function

        ''' <summary>
        ''' Get the instance from the current row, creating one if nescessary
        ''' </summary>
        ''' <param name="Row"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ComponentFromRow(ByVal Row As DataGridViewRow) As DesignTimeSettingInstance
            If Row.Tag IsNot Nothing Then
                Debug.Assert(TypeOf Row.Tag Is DesignTimeSettingInstance, "Unknown tag of this object!")
                Return DirectCast(Row.Tag, DesignTimeSettingInstance)
            Else
                Dim NewInstance As New DesignTimeSettingInstance
                Row.Tag = NewInstance
                NewInstance.SetName(Row.Cells(NameColumnNo).FormattedValue.ToString())
                NewInstance.SetScope(CType(Row.Cells(ScopeColumnNo).Value, DesignTimeSettingInstance.SettingScope))
                If NewInstance.Name = "" Then
                    NewInstance.SetName(Settings.CreateUniqueName())
                End If
                Settings.Add(NewInstance)
                Return NewInstance
            End If
        End Function

        ''' <summary>
        ''' We can't allow commit of pending changes in some cases:
        ''' 1. We are showing an error dialog
        ''' 2. We are showing the type picker dialog
        ''' 3. We are showing a UI type editor
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property AllowCommitPendingChanges() As Boolean
            Get
                If m_isReportingError OrElse m_isShowingTypePicker OrElse m_inCellValidated OrElse m_SettingsGridView.CommittingChanges Then
                    Return False
                End If

                Dim ctrl As DataGridViewUITypeEditorEditingControl = TryCast(m_SettingsGridView.EditingControl, DataGridViewUITypeEditorEditingControl)
                If ctrl IsNot Nothing AndAlso ctrl.IsShowingUITypeEditor Then
                    Return False
                End If
                Return True
            End Get
        End Property

        ''' <summary>
        ''' The function forces to refresh the status of all commands.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub RefreshCommandStatus()
            If m_menuCommands IsNot Nothing Then
                For Each command As DesignerMenuCommand In m_menuCommands
                    command.RefreshStatus()
                Next
            End If
        End Sub

        ''' <summary>
        ''' Commit any pending changes
        ''' </summary>
        ''' <remarks></remarks>
        Public Function CommitPendingChanges(ByVal suppressValidationUI As Boolean, ByVal cancelOnValidationFailure As Boolean) As Boolean
            Dim savedSuppressValidationUI As Boolean = m_suppressValidationUI
            Dim succeeded As Boolean = False
            Try
                m_suppressValidationUI = suppressValidationUI
                If m_SettingsGridView.IsCurrentCellInEditMode Then
                    Debug.Assert(m_SettingsGridView.CurrentCell IsNot Nothing, "Grid in editing mode with no current cell???")
                    Try
                        If Not AllowCommitPendingChanges Then
                            succeeded = False
                        ElseIf ValidateCell(m_SettingsGridView.CurrentCell.EditedFormattedValue, m_SettingsGridView.CurrentCell.RowIndex, m_SettingsGridView.CurrentCell.ColumnIndex) Then
                            Dim oldSelectedCell As DataGridViewCell = m_SettingsGridView.CurrentCell
                            m_SettingsGridView.CurrentCell = Nothing
                            If oldSelectedCell IsNot Nothing Then
                                oldSelectedCell.Selected = True
                            End If
                            succeeded = True
                        Else
                            If cancelOnValidationFailure Then
                                m_SettingsGridView.CancelEdit()
                            End If
                        End If
                    Catch Ex As Exception
                        If _
                            Ex Is GetType(System.Threading.ThreadAbortException) OrElse _
                            Ex Is GetType(System.StackOverflowException) Then
                            Throw
                        End If
                        Debug.Assert(Ex IsNot GetType(NullReferenceException) AndAlso Ex IsNot GetType(OutOfMemoryException), _
                            String.Format("CommitPendingChanges caught exception {0}", Ex))
                    End Try
                Else
                    succeeded = True
                End If
            Finally
                m_suppressValidationUI = savedSuppressValidationUI
            End Try

            ' If someone tells us to commit our pending changes, we have to make sure that the designer loader flushes.
            ' If we don't do this, the global settings object may come along and read stale data from the docdata's buffer...
            If DesignerLoader IsNot Nothing Then
                DesignerLoader.Flush()
            End If

            Return succeeded
        End Function


#Region "Column accessors"

        ''' <summary>
        ''' Type safe accessor for the type column
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property TypeColumn() As DataGridViewComboBoxColumn
            Get
                Return DirectCast(m_SettingsGridView.Columns(TypeColumnNo), DataGridViewComboBoxColumn)
            End Get
        End Property

        ''' <summary>
        ''' Type safe accessor for the type column
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property ScopeColumn() As DataGridViewComboBoxColumn
            Get
                Return DirectCast(m_SettingsGridView.Columns(ScopeColumnNo), DataGridViewComboBoxColumn)
            End Get
        End Property

#End Region

#Region "Private helper functions"


        ''' <summary>
        ''' Completely refresh grid (remove current rows and re-create them from settings
        ''' in associated designer
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RefreshGrid()
            If m_SettingsGridView.RowCount() > 0 Then
                m_SettingsGridView.Rows.Clear()
            End If

            For Each Instance As DesignTimeSettingInstance In Settings
                AddRow(Instance)
            Next
        End Sub

        ''' <summary>
        ''' Add a row to the grid, associate it with the the setting instance and update the UI
        ''' </summary>
        ''' <param name="Instance"></param>
        ''' <remarks></remarks>
        Private Sub AddRow(ByVal Instance As DesignTimeSettingInstance)
            If Not m_SettingsGridView.IsHandleCreated Then
                m_SettingsGridView.CreateControl()
            End If
            Debug.Assert(m_SettingsGridView.IsHandleCreated)
            Dim NewRowNo As Integer = m_SettingsGridView.Rows.Add()
            Dim NewRow As DataGridViewRow = m_SettingsGridView.Rows(NewRowNo)
            NewRow.Tag = Instance
            SetUIRowValues(NewRow, Instance)
        End Sub

        ''' <summary>
        ''' Make sure the user sees the properties as the are set in the setting instance
        ''' </summary>
        ''' <param name="Row"></param>
        ''' <remarks></remarks>
        Private Sub SetUIRowValues(ByVal Row As DataGridViewRow, ByVal Instance As DesignTimeSettingInstance)
            Row.Cells(NameColumnNo).Value = Instance.Name
            Row.Cells(NameColumnNo).ReadOnly = DesignTimeSettingInstance.IsNameReadOnly(Instance)

            ' Update type combobox, adding the instance's type if it isn't already included in the
            ' list
            Dim TypeCell As DataGridViewComboBoxCell = CType(Row.Cells(TypeColumnNo), DataGridViewComboBoxCell)
            Dim SettingTypeDisplayType As String = m_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(Instance.SettingTypeName)
            If Not TypeColumn.Items.Contains(SettingTypeDisplayType) Then
                TypeColumn.Items.Insert(TypeColumn.Items.Count() - 1, SettingTypeDisplayType)
                Common.Utils.SetComboBoxColumnDropdownWidth(TypeColumn)
            End If
            TypeCell.Value = SettingTypeDisplayType
            UpdateComboBoxCell(TypeCell)
            TypeCell.ReadOnly = DesignTimeSettingInstance.IsTypeReadOnly(Instance)

            Row.Cells(ScopeColumnNo).ReadOnly = DesignTimeSettingInstance.IsScopeReadOnly(instance, m_projectSystemSupportsUserScope)

            Row.Cells(ScopeColumnNo).Value = Instance.Scope

            UpdateUIValueColumn(Row)
        End Sub

        ''' <summary>
        ''' Update the value column of the current row and set the correct visual style
        ''' </summary>
        ''' <param name="row"></param>
        ''' <remarks></remarks>
        Private Sub UpdateUIValueColumn(ByVal row As DataGridViewRow)
            Dim Instance As DesignTimeSettingInstance = CType(row.Tag, DesignTimeSettingInstance)
            Dim Cell As DataGridViewUITypeEditorCell = CType(row.Cells(ValueColumnNo), DataGridViewUITypeEditorCell)
            If Instance IsNot Nothing Then
                Dim settingType As System.Type = m_settingTypeCache.GetSettingType(Instance.SettingTypeName)
                If settingType IsNot Nothing AndAlso Not SettingTypeValidator.IsTypeObsolete(settingType) Then
                    Cell.ValueType = settingType
                    Cell.Value = m_valueCache.GetValue(settingType, Instance.SerializedValue)
                Else
                    Cell.ValueType = GetType(String)
                    Cell.Value = Instance.SerializedValue
                End If

                Cell.ServiceProvider = Me.Settings.Site
            Else
                ' If we don't have an instance for this row, the value should be an
                ' empty string
                Cell.ValueType = GetType(String)
                Cell.Value = ""
            End If
            m_SettingsGridView.InvalidateCell(Cell)
        End Sub

        ''' <summary>
        ''' Update the current cell to reflect the current value
        ''' </summary>
        ''' <param name="CellToUpdate"></param>
        ''' <remarks>The editing control isn't correctly updated if the cell changes "under" it</remarks>
        Private Sub UpdateComboBoxCell(ByVal CellToUpdate As DataGridViewComboBoxCell)
            If CellToUpdate Is m_SettingsGridView.CurrentCell AndAlso m_SettingsGridView.EditingControl IsNot Nothing Then
                If Not System.DBNull.Value.Equals(CellToUpdate.Value) Then
                    CellToUpdate.InitializeEditingControl(CellToUpdate.RowIndex, CellToUpdate.Value, CellToUpdate.Style)
                End If
            End If
        End Sub

        ''' <summary>
        ''' If files under source control, prompt the user if (s)he wants to check them out.
        ''' </summary>
        ''' <returns>True if not under source control or if the check out succeeded, false otherwise</returns>
        ''' <remarks></remarks>
        Private Function EnsureCheckedOut() As Boolean
            If DesignerLoader Is Nothing Then
                Debug.Fail("Failed to get the IDesignerLoaderService from out settings site (or the IDesignerLoaderService wasn't a SettingsDesignerLoader :(")
                Return False
            End If

            Try
                Return DesignerLoader.EnsureCheckedOut()
            Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                Debug.Fail(String.Format("SettingsDesignerView::EnsureCheckedOut: Caught exception {0}", ex))
                Throw
            End Try
        End Function

        Private Function InDesignMode() As Boolean
            If DesignerLoader Is Nothing Then
                Debug.Fail("Failed to get the IDesignerLoaderService from out settings site (or the IDesignerLoaderService wasn't a SettingsDesignerLoader :(")
                Return False
            End If

            Try
                Return DesignerLoader.InDesignMode
            Catch ex As Exception
                Debug.Fail(String.Format("SettingsDesignerView::InDesignMode: Caught exception {0}", ex))
                Throw
            End Try
        End Function

#End Region

#Region "Selection service"

        Private m_SelectionServiceProperty As ISelectionService
        Private ReadOnly Property SelectionService() As ISelectionService
            Get
                If m_SelectionServiceProperty Is Nothing Then
                    m_SelectionServiceProperty = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
                End If
                Return m_SelectionServiceProperty
            End Get
        End Property

        Private Sub m_SettingsGridView_CellStateChanged(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellStateChangedEventArgs) Handles m_SettingsGridView.CellStateChanged
            If SelectionService IsNot Nothing Then
                Dim SelectedComponents As New Hashtable()
                For Each cell As DataGridViewCell In m_SettingsGridView.SelectedCells
                    Dim Row As DataGridViewRow = m_SettingsGridView.Rows(cell.RowIndex)
                    If Row.Tag IsNot Nothing Then
                        SelectedComponents(Row.Tag) = True
                    End If
                Next

                SelectionService.SetSelectedComponents(SelectedComponents.Keys, SelectionTypes.Replace)
            End If
        End Sub

        Private Sub m_SettingsGridView_RowStateChanged(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewRowStateChangedEventArgs) Handles m_SettingsGridView.RowStateChanged
            If SelectionService IsNot Nothing Then
                Dim SelectedComponents As New Hashtable()

                For Each Row As DataGridViewRow In m_SettingsGridView.SelectedRows
                    If Row.Tag IsNot Nothing Then
                        SelectedComponents(Row.Tag) = True
                    End If
                Next
                SelectionService.SetSelectedComponents(SelectedComponents.Keys, SelectionTypes.Replace)
            End If
        End Sub


#End Region


#Region "Control event handlers"

        ''' <summary>
        ''' If multiple rows are selected, we need to wrap 'em all in a undo transaction...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_UserDeletingRow(ByVal sender As Object, ByVal e As DataGridViewRowCancelEventArgs) Handles m_SettingsGridView.UserDeletingRow
            If m_SettingsGridView.SelectedRows.Count = 0 AndAlso e.Row.IsNewRow Then
                ' The user cancelled an edit of the new row - we should not prevent the 
                ' datagridview from doing its magic and delete the new row...
                Return
            End If

            ' Make sure everything is checked out...
            If Not Me.EnsureCheckedOut() Then
                e.Cancel = True
                Return
            End If

            ' We handle the delete explicitly here
            Me.RemoveRows(m_SettingsGridView.SelectedRows)

            ' And cancel the "automatic" delete that is about to happen. The RemoveRows call should have 
            ' already taken care of this :)
            e.Cancel = True
        End Sub


        ''' <summary>
        ''' The user has deleted a row from the grid - let's make sure that we delete the corresponding
        ''' setting instance from the designed component...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_UserDeletedRow(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewRowEventArgs) Handles m_SettingsGridView.UserDeletedRow
            Dim InstanceToDelete As DesignTimeSettingInstance = CType(e.Row.Tag, DesignTimeSettingInstance)
            If InstanceToDelete IsNot Nothing Then
                Settings.Remove(InstanceToDelete)
            Else
                Debug.WriteLine("No Setting instance associated with deleted row!?")
            End If
        End Sub

        ''' <summary>
        ''' Helper method to validate a cell
        ''' </summary>
        ''' <param name="FormattedValue"></param>
        ''' <param name="RowIndex"></param>
        ''' <param name="ColumnIndex"></param>
        ''' <remarks></remarks>
        Private Function ValidateCell(ByVal FormattedValue As Object, ByVal RowIndex As Integer, ByVal ColumnIndex As Integer) As Boolean
            Dim Instance As DesignTimeSettingInstance = TryCast(m_SettingsGridView.Rows(RowIndex).Tag, DesignTimeSettingInstance)
            Select Case ColumnIndex
                Case NameColumnNo
                    Debug.Assert(TypeOf FormattedValue Is String, "Unknown type of formatted value for name")
                    Return ValidateName(DirectCast(FormattedValue, String), Instance)
                Case TypeColumnNo
                    ' We don't want to commit the "Browse..." value at any time... we also don't want to allow an empty string for type name
                    Debug.Assert(TypeOf FormattedValue Is String, "Unknown type of formatted value for name")
                    Return Not (TryCast(FormattedValue, String) = "" OrElse String.Equals(SR.GetString(SR.SD_ComboBoxItem_BrowseType), TryCast(FormattedValue, String), StringComparison.Ordinal))
                Case ScopeColumnNo
                    Return TryCast(FormattedValue, String) <> ""
                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' Validate cell contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_CellValidating(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellValidatingEventArgs) Handles m_SettingsGridView.CellValidating
            ' We can get into this when delay-disposing due to project reloads...
            If Me.Disposing Then Return

            If e.RowIndex = m_SettingsGridView.NewRowIndex Then
                ' Don't validate the new row...
                Return
            End If

            e.Cancel = Not ValidateCell(e.FormattedValue, e.RowIndex, e.ColumnIndex)
        End Sub

        Private Function ValidateName(ByVal NewName As String, ByVal Instance As DesignTimeSettingInstance) As Boolean
            ' If it was a valid name before, let's assume is still is :)
            If Instance IsNot Nothing AndAlso DesignTimeSettings.EqualIdentifiers(NewName, Instance.Name) Then
                Return True
            End If

            If NewName = "" Then
                If Not m_suppressValidationUI Then
                    ReportError(SR.GetString(SR.SD_ERR_NameEmpty), HelpIDs.Err_NameBlank)
                End If
                Return False
            End If

            If Not Settings.IsUniqueName(NewName, IgnoreThisInstance:=Instance) Then
                ' There is already a setting with this name...
                If Not m_suppressValidationUI Then
                    ReportError(SR.GetString(SR.SD_ERR_DuplicateName_1Arg, NewName), HelpIDs.Err_DuplicateName)
                End If
                Return False
            End If

            If Not Settings.IsValidName(NewName) Then
                If Not m_suppressValidationUI Then
                    ReportError(SR.GetString(SR.SD_ERR_InvalidIdentifier_1Arg, NewName), HelpIDs.Err_InvalidName)
                End If
                Return False
            End If

            ' Everything is cool!
            Return True
        End Function

        ''' <summary>
        ''' Committing whatever change the user has done to the current cell
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_CellValidated(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles m_SettingsGridView.CellValidated
            If Me.Disposing Then
                Return
            End If

            If m_inCellValidated Then
                Return
            End If
            Try
                m_inCellValidated = True
                Dim Row As DataGridViewRow = m_SettingsGridView.Rows(e.RowIndex)

                Dim cell As DataGridViewCell = Row.Cells(e.ColumnIndex)
                Debug.Assert(cell IsNot Nothing, "Couldn't get current cell?")

                If Not m_SettingsGridView.IsCurrentRowDirty Then
                    ' This suxz, but since it seems that we get a validated event when 
                    ' the *current selected cell* changes, and not after *end edit*, we
                    ' check if the grid view thinks the current row is dirty!
                    ' CONSIDER: move this code to CellEndEdit event handler!
                    Return
                End If

                If Not InDesignMode() Then
                    Return
                End If

                If Not EnsureCheckedOut() Then
                    Debug.Fail("We shouldn't have to check out here since that was done when entering edit mode!?")
                    Return
                End If

                Dim Instance As DesignTimeSettingInstance = ComponentFromRow(Row)
                Debug.Assert(Instance IsNot Nothing, "No DesignTimSetting associated with this row!?")

                Dim CellText As String = CStr(cell.EditedFormattedValue)

                '
                ' There is a slim, slim chance that the project will be reloaded when changing a property. 
                ' Currently, the only known time when that happens is when you rename a web reference typed setting
                ' (there is a corresponding property in the project file that will be set) but there is no way to 
                ' determine if anyone else is listening to ComponentChanged/ComponentChanging and doing something that
                ' will cause the project to be checked out. 
                ' 
                ' We'll take not of this fact by entering a protected ProjectCheckoutSection 
                '
                EnterProjectCheckoutSection()

                Try
                    Select Case e.ColumnIndex
                        Case NameColumnNo
                            Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing name of setting " & Instance.Name)
                            ' Don't use SetName since that won't fire a component change notification...
                            Instance.NameProperty.SetValue(Instance, CellText)
                        Case TypeColumnNo
                            ' Changing the type is a remove/add operation
                            If Not CellText.Equals(Instance.SettingTypeName, StringComparison.Ordinal) AndAlso Not CellText.Equals(SR.GetString(SR.SD_ComboBoxItem_BrowseType), StringComparison.Ordinal) Then
                                ChangeSettingType(Row, CellText)
                            End If
                        Case ScopeColumnNo
                            Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing scope of setting " & Instance.Name)
                            Instance.ScopeProperty.SetValue(Instance, cell.Value)
                        Case ValueColumnNo
                            ' It seems that we get a cell validated event even if we haven't edited the text in the cell....
                            If Not String.Equals(Instance.SerializedValue, CellText, StringComparison.Ordinal) Then
                                ' We only set the value in if the text in the validated cell
                                ' is different from the previous value
                                Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing value of setting " & Instance.Name)
                                Dim serializer As New SettingsValueSerializer()
                                Instance.SerializedValueProperty.SetValue(Instance, serializer.Serialize(cell.Value, System.Globalization.CultureInfo.InvariantCulture))
                            End If
                    End Select
                Catch ex As System.Exception
                    ' We only "expect" checkout exceptions, but we may want to know about other exceptions as well...
                    Debug.Assert(TypeOf ex Is System.ComponentModel.Design.CheckoutException, String.Format("Unknown exception {0} caught while changing property", ex))

                    ' Try & tell the user that something went wrong...
                    If Not ProjectReloadedDuringCheckout Then
                        If Settings IsNot Nothing AndAlso Settings.Site IsNot Nothing Then
                            DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, "", ex, DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site))
                        End If
                        ' And make sure that the UI reflects the actual values of the corresponding setting...
                        SetUIRowValues(Row, Instance)
                    End If
                Finally
                    LeaveProjectCheckoutSection()
                End Try
            Finally
                m_inCellValidated = False
            End Try
        End Sub

        Private Sub m_SettingsGridView_CellFormatting(ByVal sender As Object, ByVal e As DataGridViewCellFormattingEventArgs) Handles m_SettingsGridView.CellFormatting
            ' If the column is the Scope column, check the
            ' value.
            If e.ColumnIndex = ScopeColumnNo Then
                If Not System.DBNull.Value.Equals(e.Value) AndAlso e.Value IsNot Nothing Then
                    Dim row As DataGridViewRow = m_SettingsGridView.Rows(e.RowIndex)
                    Dim instance As DesignTimeSettingInstance = TryCast(row.Tag, DesignTimeSettingInstance)
                    e.Value = DesignTimeSettingInstance.ScopeConverter.ConvertToLocalizedString(instance, CType(e.Value, DesignTimeSettingInstance.SettingScope))
                    e.FormattingApplied = True
                End If
            End If
        End Sub

        Private Sub m_SettingsGridView_OnEditingControlShowing(ByVal sender As Object, ByVal e As DataGridViewEditingControlShowingEventArgs) Handles m_SettingsGridView.EditingControlShowing
            ' Work-around for VsWhidbey 228617
            Dim tb As TextBox = TryCast(e.Control, TextBox)
            If tb IsNot Nothing Then
                tb.Multiline = False
                tb.AcceptsReturn = False
            End If
        End Sub

        ''' <summary>
        ''' We want to prevent us from going into edit mode on the first click...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_OnCellClickBeginEdit(ByVal sender As Object, ByVal e As CancelEventArgs) Handles m_SettingsGridView.CellClickBeginEdit
            If DesignerLoader IsNot Nothing Then
                e.Cancel = Not DesignerLoader.OkToEdit()
            End If
        End Sub

        ''' <summary>
        ''' The user has started editing a cell. We've gotta make sure that:
        '''  1. The file is checked out if under source control
        '''  2. The cell style in the Value column is "Default"
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_CellBeginEdit(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellCancelEventArgs) Handles m_SettingsGridView.CellBeginEdit
            ' Check out , but we can't check out if resource file is readonly
            If (Not InDesignMode()) OrElse (DesignerLoader.IsReadOnly) OrElse (Not EnsureCheckedOut()) Then
                e.Cancel = True
            Else
                Select Case e.ColumnIndex
                    Case NameColumnNo
                        '
                        ' If this is the name of a web reference setting, we need to check out the project file since the name of
                        ' the setting that contains the web service URL is stored in the project file.
                        ' In a perfect world, the disco code generator (or even the IVsRefactorNotify 
                        If e.RowIndex <> m_SettingsGridView.NewRowIndex Then
                            Dim instance As DesignTimeSettingInstance = ComponentFromRow(m_SettingsGridView.Rows(e.RowIndex))
                            If instance IsNot Nothing AndAlso instance.SettingTypeName.Equals(SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                                If DesignerLoader IsNot Nothing _
                                    AndAlso DesignerLoader.ProjectItem IsNot Nothing _
                                    AndAlso DesignerLoader.ProjectItem.ContainingProject IsNot Nothing _
                                    AndAlso DesignerLoader.ProjectItem.ContainingProject.FullName <> "" _
                                    AndAlso Settings IsNot Nothing _
                                    AndAlso Settings.Site IsNot Nothing _
                                Then
                                    Dim files As New Collections.Generic.List(Of String)
                                    files.Add(DesignerLoader.ProjectItem.ContainingProject.FullName)
                                    If Not DesignerFramework.SourceCodeControlManager.QueryEditableFiles(Settings.Site, files, False, False) Then
                                        e.Cancel = True
                                    End If
                                End If
                            End If
                        End If
                    Case ValueColumnNo
                        Dim cell As DataGridViewUITypeEditorCell = TryCast( _
                                        m_SettingsGridView.Rows(e.RowIndex).Cells(e.ColumnIndex), _
                                        DataGridViewUITypeEditorCell)

                        ' If the type has been invalidated, we need to make sure that we treat it as a string...
                        ' We know that our internal serializable connection strings can never be invalidated, so we don't 
                        ' need to check for invalidated connection string types...
                        If cell IsNot Nothing AndAlso cell.ValueType IsNot Nothing AndAlso cell.ValueType IsNot GetType(SerializableConnectionString) Then
                            Dim reresolvedSettingType As Type = m_settingTypeCache.GetSettingType(cell.ValueType.FullName)

                            If reresolvedSettingType IsNot Nothing Then
                                If SettingTypeValidator.IsTypeObsolete(reresolvedSettingType) Then
                                    Dim formattedValue As Object = cell.FormattedValue
                                    cell.ValueType = GetType(String)
                                    cell.Value = formattedValue
                                End If
                            End If
                        End If
                End Select
            End If
        End Sub


        ''' <summary>
        ''' Get access to a UI service - useful to pop up message boxes and getting fonts
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property UIService() As IUIService
            Get
                Dim Result As IUIService
                Result = CType(GetService(GetType(IUIService)), IUIService)

                Debug.Assert(Result IsNot Nothing, "Failed to get IUIService")
                Return Result
            End Get
        End Property

        ''' <summary>
        ''' The user has added a row to the grid
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Shouldn't have to do this, but it seems that the tag property of the new row is copied from the previous row</remarks>
        Private Sub m_SettingsGridView_UserAddedRow(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewRowEventArgs) Handles m_SettingsGridView.UserAddedRow
            e.Row.Tag = Nothing
        End Sub

        ''' <summary>
        ''' We've gotta fill out the default values for the new row!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_SettingsGridView_DefaultValuesNeeded(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewRowEventArgs) Handles m_SettingsGridView.DefaultValuesNeeded
            Dim SampleInstance As New DesignTimeSettingInstance()
            If Not m_projectSystemSupportsUserScope Then
                SampleInstance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
            End If
            SampleInstance.SetName(Settings.CreateUniqueName())
            SetUIRowValues(e.Row, SampleInstance)
        End Sub

        ''' <summary>
        ''' Change the type of the setting on the current row
        ''' </summary>
        ''' <param name="Row"></param>
        ''' <remarks></remarks>
        Private Sub ChangeSettingType(ByVal Row As DataGridViewRow, ByVal TypeDisplayName As String)
            Dim addingNewSetting As Boolean = (Row.Tag Is Nothing)

            ' Get the current setting instance.
            Dim Instance As DesignTimeSettingInstance = ComponentFromRow(Row)

            ' Let's get the display name for the new type... 
            Dim newTypeName As String = m_typeNameResolver.TypeDisplayNameToPersistedSettingTypeName(TypeDisplayName)
            Dim newType As Type = m_settingTypeCache.GetSettingType(newTypeName)

            ' Only change type of this setting if the display name of the types are different...
            If addingNewSetting OrElse Not String.Equals(Instance.SettingTypeName, newTypeName, StringComparison.Ordinal) Then
                Using Transaction As New SettingsDesignerUndoTransaction(Settings.Site, SR.GetString(SR.SD_UndoTran_TypeChanged))
                    Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing type of setting " & Instance.Name)
                    Instance.TypeNameProperty.SetValue(Instance, newTypeName)
                    If newType IsNot Nothing Then
                        Dim newValue As Object = m_valueCache.GetValue(newType, Instance.SerializedValue)
                        Dim serializer As New SettingsValueSerializer()

                        ' If we don't have a value, and the new type is a value type, we want to 
                        ' give an "empty" default value for the value type to avoid run time type
                        ' cast exceptions in the users code for C# (DevDiv 24835)
                        If newValue Is Nothing AndAlso GetType(ValueType).IsAssignableFrom(newType) Then
                            Try
                                newValue = System.Activator.CreateInstance(newType)
                            Catch ex As System.Exception
                                ' We gave it a shot... but unfortunately, we didn't succeed...
                                ' It is now up to the user to specify an appropriate default value
                            End Try
                        End If
                        Instance.SerializedValueProperty.SetValue(Instance, serializer.Serialize(newValue, System.Globalization.CultureInfo.InvariantCulture))
                    End If

                    ' If we changed the type to a connection string, we should also make sure that the scope is application...
                    If newType Is GetType(SerializableConnectionString) AndAlso Instance.Scope <> DesignTimeSettingInstance.SettingScope.Application Then
                        Instance.ScopeProperty.SetValue(Instance, DesignTimeSettingInstance.SettingScope.Application)
                    End If
                    Transaction.Commit()
                End Using

                If newType IsNot Nothing AndAlso _
                   m_settingTypeCache.IsWellKnownType(newType) Then
                    '
                    ' Try to add a reference to the type (if not already in the project)
                    '
                    Try
                        If DesignerLoader IsNot Nothing Then
                            Dim dteProj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(DesignerLoader.VsHierarchy)
                            Dim vsLangProj As VSLangProj.VSProject = Nothing
                            If dteProj IsNot Nothing Then
                                vsLangProj = TryCast(dteProj.Object, VSLangProj.VSProject)
                            End If

                            If vsLangProj IsNot Nothing AndAlso vsLangProj.References.Find(newType.Assembly.GetName().Name) Is Nothing Then
                                vsLangProj.References.Add(newType.Assembly.GetName().Name)
                            End If
                        End If
                    Catch ex As Exception
                        ' Well, we mostly tried to be nice to the user and automatically add the reference here... 
                        ' If we fail, the user will see an annoying error about undefined types, but it shouldn't be the
                        ' end of the world...
                        If Not TypeOf ex Is CheckoutException Then
                            Debug.Fail("Failed to add reference to assembly contining type " & newTypeName)
                        End If
                    End Try
                End If
            End If

            ' We always need to update the UI, since the user may have selected the same type as the setting already was
            ' in the type browser dialog, and not updating here would keep the dialog showing "browse..."
            SetUIRowValues(Row, ComponentFromRow(Row))
        End Sub

        ''' <summary>
        ''' To find out when the user clicks on the "browse..." item in the types combo box,
        ''' we have got to "commit" (end edit) the value everytime the cell gets dirty!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Kind of hacky...</remarks>
        Private Sub m_SettingsGridView_CurrentCellDirtyStateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_SettingsGridView.CurrentCellDirtyStateChanged
            If m_SettingsGridView.CurrentCellAddress.X = TypeColumnNo Then
                Dim cell As DataGridViewCell = m_SettingsGridView.CurrentCell
                If cell IsNot Nothing Then
                    If SR.GetString(SR.SD_ComboBoxItem_BrowseType).Equals(cell.EditedFormattedValue) Then
                        TypeComboBoxSelectedIndexChanged()
                    ElseIf TryCast(cell.EditedFormattedValue, String) = "" Then
                        m_SettingsGridView.CancelEdit()
                    Else
                        ' If we don't have a setting associated with the current row, we force create one
                        ' by getting the component from the row (if we don't do this, there won't be an undo
                        ' unit for this - the settings won't be created until we leave the cell)
                        Dim row As DataGridViewRow = m_SettingsGridView.Rows(m_SettingsGridView.CurrentCellAddress.Y)
                        If row IsNot Nothing Then
                            ComponentFromRow(row)
                        End If

                        m_SettingsGridView.CommitEdit(DataGridViewDataErrorContexts.Commit)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Whenever the datagridview finds something to complain about, it will call the DataError 
        ''' event handler
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub m_SettingsGridView_OnDataError(ByVal sender As Object, ByVal e As DataGridViewDataErrorEventArgs) Handles m_SettingsGridView.DataError
            If Not m_suppressValidationUI Then
                Select Case e.ColumnIndex
                    Case ValueColumnNo
                        ReportError(SR.GetString(SR.SD_ERR_InvalidValue_2Arg, m_SettingsGridView.CurrentCell.GetEditedFormattedValue(e.RowIndex, DataGridViewDataErrorContexts.Display), m_SettingsGridView.Rows(m_SettingsGridView.CurrentCell.RowIndex).Cells(TypeColumnNo).FormattedValue), HelpIDs.Err_FormatValue)
                    Case NameColumnNo
                        ReportError(SR.GetString(SR.SD_ERR_InvalidIdentifier_1Arg, m_SettingsGridView.CurrentCell.GetEditedFormattedValue(e.RowIndex, DataGridViewDataErrorContexts.Display)), HelpIDs.Err_InvalidName)
                    Case Else
                        ' For some reason, we get data errors when we don't have a value for a specific row 
                        ' (i.e. not set type or scope). We'll just ignore these for now...
                End Select
            End If
            e.Cancel = True
        End Sub

#End Region

        ''' <summary>
        ''' The user wants to view (and maybe add) code that extends the generated settings class
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        Private Sub ViewCode()
            Dim Hierarchy As IVsHierarchy = DirectCast(Settings.Site.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            Dim ProjectItem As EnvDTE.ProjectItem = DirectCast(Settings.Site.GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
            Dim VSMDCodeDomProvider As IVSMDCodeDomProvider = DirectCast(Settings.Site.GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
            If Hierarchy Is Nothing OrElse ProjectItem Is Nothing OrElse VSMDCodeDomProvider Is Nothing Then
                ReportError(SR.GetString(SR.SD_CODEGEN_FAILEDOPENCREATEEXTENDINGFILE), HelpIDs.Err_ViewCode)
            Else
                Try
                    If ProjectItem.ProjectItems Is Nothing OrElse ProjectItem.ProjectItems.Count = 0 Then
                        ' If we don't have any subitems, we better try & run the custom tool...
                        Dim vsProjectItem As VSLangProj.VSProjectItem = TryCast(ProjectItem.Object, VSLangProj.VSProjectItem)
                        If vsProjectItem IsNot Nothing Then
                            vsProjectItem.RunCustomTool()
                        End If
                    End If
                    Dim FullyQualifedClassName As String = SettingsDesigner.FullyQualifiedGeneratedTypedSettingsClassName(Hierarchy, VSITEMID.NIL, Settings, ProjectItem)
                    Dim suggestedFileName As String = ""
                    If Settings.UseSpecialClassName AndAlso Utils.IsVbProject(Hierarchy) AndAlso SettingsDesigner.IsDefaultSettingsFile(Hierarchy, Me.DesignerLoader.ProjectItemid) Then
                        suggestedFileName = "Settings"
                    End If
                    ProjectUtils.OpenAndMaybeAddExtendingFile(FullyQualifedClassName, suggestedFileName, Settings.Site, Hierarchy, ProjectItem, CType(VSMDCodeDomProvider.CodeDomProvider, System.CodeDom.Compiler.CodeDomProvider), Me)
                Catch ex As Exception
                    If Settings IsNot Nothing AndAlso Settings.Site IsNot Nothing Then
                        ' We better tell the user that something went wrong (if we still have a settings/settings.site that is)
                        DesignerFramework.DesignerMessageBox.Show(Settings.Site, ex, DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site))
                    End If
                End Try
            End If
        End Sub

        Private Sub RemoveRows(ByVal rowsToDelete As ICollection)
            Dim undoTran As SettingsDesignerUndoTransaction = Nothing
            Try
                If rowsToDelete.Count > 1 Then
                    ' If there is more than one row to delete, we need to wrap this in an undo transaction...
                    undoTran = New SettingsDesignerUndoTransaction(Settings.Site, SR.GetString(SR.SD_UndoTran_RemoveMultipleSettings_1Arg, rowsToDelete.Count))
                End If
                For Each row As DataGridViewRow In rowsToDelete
                    If row.Tag IsNot Nothing Then
                        ' Removing the setting will fire a ComponentRemoved, which
                        ' will remove the row from the grid...
                        Settings.Remove(DirectCast(row.Tag, DesignTimeSettingInstance))
                    End If
                Next
                If undoTran IsNot Nothing Then
                    ' Commit undo transaction (if any)
                    undoTran.Commit()
                End If
            Finally
                If undoTran IsNot Nothing Then
                    undoTran.Dispose()
                End If
            End Try

        End Sub

#Region "Context menus"

        ''' <summary>
        ''' Remove event handler for showing context menu
        ''' </summary>
        ''' <param name="Designer"></param>
        ''' <remarks></remarks>
        Private Sub UnregisterMenuCommands(ByVal Designer As SettingsDesigner)
            RemoveHandler m_SettingsGridView.ContextMenuShow, AddressOf Designer.ShowContextMenu
        End Sub

        ''' <summary>
        ''' Register the settings designer menu commands(context menus)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RegisterMenuCommands(ByVal Designer As SettingsDesigner)
            'Protect against recursively invoking this
            Static InThisMethod As Boolean
            If InThisMethod Then
                Debug.Fail("RegisterMenuCommands was invoked recursively")
                Exit Sub
            End If

            InThisMethod = True
            Try
                m_menuCommands = New ArrayList
                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDCOMMONEditCell, AddressOf MenuEditCell, AddressOf Me.MenuEditCellEnableHandler, _
                    alwayscheckstatus:=True))
                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDCOMMONAddRow, AddressOf MenuAddSetting, AddressOf Me.MenuAddSettingEnableHandler, commandtext:=SR.GetString(SR.SD_MNU_AddSettingText)))
                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDCOMMONRemoveRow, AddressOf Me.MenuRemove, AddressOf Me.MenuRemoveEnableHandler, _
                    alwayscheckstatus:=True, commandtext:=SR.GetString(SR.SD_MNU_RemoveSettingText)))

                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDSettingsDesignerViewCode, AddressOf Me.MenuViewCode, AddressOf Me.MenuViewCodeEnableHandler))
                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDSettingsDesignerSynchronize, AddressOf Me.MenuSynchronizeUserConfig, AddressOf Me.MenuSynchronizeUserConfigEnableHandler))
                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDSettingsDesignerLoadWebSettings, AddressOf Me.MenuLoadWebSettingsFromAppConfig, AddressOf Me.MenuLoadWebSettingsFromAppConfigEnableHandler, AlwaysCheckStatus:=True))
                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDVSStd2kECMD_CANCEL, AddressOf Me.MenuCancelEdit, AddressOf Me.MenuCancelEditEnableHandler))

                m_menuCommands.Add(New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDVSStd97cmdidViewCode, AddressOf Me.MenuViewCode, AddressOf Me.MenuViewCodeEnableHandler))
                'Delete
                '
                'We don't actually have a Delete command (the AddressOf MenuRemove is a dummy, since DesignerMenuCommand wants something
                '  for this argumnet).
                'We only have this command here because we need to be able to make the "Delete" command in the main menu hidden.  We
                '  use Remove instead of Delete.
                Dim DeleteCommand As DesignerMenuCommand = New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDVSStd97cmdidDelete, AddressOf MenuRemove)
                m_menuCommands.Add(DeleteCommand)
                '
                '... So, make Edit.Delete in Devenv's menus invisible always for our editor.
                DeleteCommand.Visible = False
                DeleteCommand.Enabled = False

                'Add the "Access modifier" combobox menu commands
                m_menuCommands.AddRange(m_accessModifierCombobox.GetMenuCommandsToRegister())

                Designer.RegisterMenuCommands(m_menuCommands)

                AddHandler m_SettingsGridView.ContextMenuShow, AddressOf Designer.ShowContextMenu
            Finally
                InThisMethod = False
            End Try
        End Sub

        Friend Sub OnDesignerWindowActivated(ByVal activated As Boolean)
            m_accessModifierCombobox.OnDesignerWindowActivated(activated)
            If activated Then
                UpdateToolbarFocus()
            End If
        End Sub

        ''' <summary>
        ''' Tell the shell that our toolbar wants to be included in the translation of
        ''' accelerators/alt-shift navigation
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateToolbarFocus()
            If m_toolbarPanel IsNot Nothing Then
                m_toolbarPanel.Activate(Me.Handle)
            End If
        End Sub

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
        ''' Should the remove menu item be enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuRemoveEnableHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            ' If we are not in design mode or we are read-only we shouldn't allow 
            ' removal of rows...
            If Not InDesignMode() Or DesignerLoader.IsReadOnly Then
                Return False
            End If

            ' If we are currently in edit mode, we can't allow users to remove rows, since that may
            ' prove problematic if the current row is invalid (we don't 
            If m_SettingsGridView.IsCurrentCellInEditMode Then
                Return False
            End If

            For Each cell As DataGridViewCell In m_SettingsGridView.SelectedCells
                If cell.RowIndex <> m_SettingsGridView.NewRowIndex Then
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Is the EditCell command enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuEditCellEnableHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Return m_SettingsGridView.CurrentCell IsNot Nothing AndAlso Not m_SettingsGridView.IsCurrentCellInEditMode AndAlso InDesignMode() AndAlso Not m_SettingsGridView.CurrentCell.ReadOnly AndAlso Not DesignerLoader.IsReadOnly
        End Function

        ''' <summary>
        ''' Indicate if the view code button should be enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        ''' <remarks></remarks>
        ''' <returns></returns>
        Private Function MenuViewCodeEnableHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            If DesignerLoader.IsReadOnly Then
                return false
            End If

            If m_cachedCodeProvider Is Nothing Then
                ' Let's see if we support partial classes?
                '
                Dim VSMDCodeDomProvider As Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider = _
                            DirectCast(GetService(GetType(Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider)), Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider)
                If VSMDCodeDomProvider IsNot Nothing Then
                    m_cachedCodeProvider = TryCast(VSMDCodeDomProvider.CodeDomProvider, CodeDom.Compiler.CodeDomProvider)
                    m_viewCodeEnabled = m_cachedCodeProvider IsNot Nothing AndAlso m_cachedCodeProvider.Supports(CodeDom.Compiler.GeneratorSupport.PartialTypes)
                End If
            End If
            Return m_viewCodeEnabled
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
        ''' View the "code-beside" file
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuViewCode(ByVal sender As Object, ByVal e As EventArgs)
            ViewCode()
        End Sub

        ''' <summary>
        ''' Is the Synchronize command on the settings designer toolbar enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuSynchronizeUserConfigEnableHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            If DesignerLoader.IsReadOnly Then
                Return False
            End If

            If m_hierarchy IsNot Nothing Then
                Dim proj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(m_hierarchy)
                Return Me.InDesignMode() _
                    AndAlso proj IsNot Nothing _
                    AndAlso proj.ConfigurationManager IsNot Nothing
            End If
            Return False
        End Function

        ''' <summary>
        ''' Is the Load Web Settings command on the settings designer toolbar enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuLoadWebSettingsFromAppConfigEnableHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            If DesignerLoader.IsReadOnly Then
                Return False
            End If

            If m_hierarchy IsNot Nothing Then
                Dim proj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(m_hierarchy)
                If Me.InDesignMode() _
                    AndAlso proj IsNot Nothing _
                    AndAlso proj.ConfigurationManager IsNot Nothing _
                    AndAlso Settings IsNot Nothing _
                    AndAlso Settings.Site IsNot Nothing Then
                    Try
                        Dim doc As XmlDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(CType(Settings.Site, IServiceProvider), m_hierarchy, False)

                        ' DevDiv Bugs 198406
                        ' If the application is targetting .Net 3.5 SP1, client subset, then disable "Load Web Settings" menu button because only a subset 
                        ' of the Full .Net Framework assemblies will be available to this application, in particular the client 
                        ' subset will NOT include System.Web.Extentions.dll

                        If Utils.IsClientFrameworkSubset(m_hierarchy) Then
                            Return False
                        End If

                        Return Not String.IsNullOrEmpty(ServicesPropPageAppConfigHelper.WebSettingsHost(doc))
                    Catch ex As XmlException
                        'The xml's bad: just disable the button
                    End Try
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Delete any and all user.config files...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuSynchronizeUserConfig(ByVal sender As Object, ByVal e As EventArgs)
            Dim allDeletedFiles As New Generic.List(Of String)
            Dim allDeletedDirectories As New Generic.List(Of String)

            Dim OneOrMoreFailed As Boolean = False


            If Me.DesignerLoader IsNot Nothing AndAlso Me.DesignerLoader.VsHierarchy IsNot Nothing Then
                Dim configDirs As Generic.List(Of String) = Nothing
                Dim filesToDelete As Generic.List(Of String) = Nothing

                Try
                    configDirs = SettingsDesigner.FindUserConfigDirectories(DesignerLoader.VsHierarchy)
                    filesToDelete = SettingsDesigner.FindUserConfigFiles(configDirs)
                Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                End Try

                If filesToDelete Is Nothing OrElse filesToDelete.Count = 0 Then
                    ' Couldn't find any files to delete - let's tell the user...
                    If configDirs Is Nothing Then
                        configDirs = New Generic.List(Of String)
                    End If
                    Dim dirs As String = String.Join(VisualBasic.vbNewLine, configDirs.ToArray())
                    DesignerMessageBox.Show(Settings.Site, SR.GetString(SR.SD_SyncFilesNoFilesFound_1Arg, dirs), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Information, HelpLink:=HelpIDs.Err_SynchronizeUserConfig)
                Else
                    Dim fileList As String
                    Const FilesToShow As Integer = 15
                    fileList = String.Join(VisualBasic.vbNewLine, filesToDelete.ToArray(), 0, Math.Min(FilesToShow, filesToDelete.Count))
                    If filesToDelete.Count > FilesToShow Then
                        fileList = fileList & VisualBasic.vbNewLine & "..."
                    End If

                    If DesignerMessageBox.Show(Settings.Site, SR.GetString(SR.SD_SyncFiles_1Arg, fileList), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OKCancel, MessageBoxIcon.Information, HelpLink:=HelpIDs.Err_SynchronizeUserConfig) = DialogResult.OK Then
                        If Not SettingsDesigner.DeleteFilesAndDirectories(filesToDelete, Nothing) Then
                            DesignerMessageBox.Show(Settings.Site, SR.GetString(SR.SD_SyncFilesOneOrMoreFailed), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Warning, HelpLink:=HelpIDs.Err_SynchronizeUserConfig)
                        End If
                    End If
                End If
            End If

        End Sub

        ''' <summary>
        ''' Load web settings from app.config file
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuLoadWebSettingsFromAppConfig(ByVal sender As Object, ByVal e As EventArgs)
            If m_hierarchy IsNot Nothing Then
                Dim proj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(m_hierarchy)
                If Me.InDesignMode() _
                    AndAlso proj IsNot Nothing _
                    AndAlso proj.ConfigurationManager IsNot Nothing _
                    AndAlso Settings IsNot Nothing _
                    AndAlso Settings.Site IsNot Nothing Then

                    Try
                        Dim doc As XmlDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(CType(Settings.Site, IServiceProvider), m_hierarchy, False)
                        If doc IsNot Nothing Then
                            Dim authenticationUrl As String = ServicesPropPageAppConfigHelper.AuthenticationServiceUrl(doc)
                            Dim authenticationHost As String = ServicesPropPageAppConfigHelper.AuthenticationServiceHost(doc)
                            Using servicesAuthForm As New ServicesAuthenticationForm(authenticationUrl, authenticationHost, CType(Settings.Site, IServiceProvider))
                                If ServicesPropPageAppConfigHelper.WindowsAuthSelected(doc) Then
                                    'DevDiv Bugs 121204, according the manuva, Windows Auth always happens
                                    'automatically so we can just treat this case like anonymous and skip going
                                    'to the Auth Service
                                    servicesAuthForm.LoadAnonymously = True
                                Else
                                    Dim result As DialogResult = servicesAuthForm.ShowDialog()
                                    If result = DialogResult.Cancel Then Exit Sub
                                    'TODO: What exceptions do we need to catch?
                                End If
                                If servicesAuthForm.LoadAnonymously OrElse _
                                    (Not servicesAuthForm.LoadAnonymously AndAlso ClientFormsAuthenticationMembershipProvider.ValidateUser(servicesAuthForm.UserName, servicesAuthForm.Password, servicesAuthForm.AuthenticationUrl)) Then
                                    Dim webSettingsUrl As String = ServicesPropPageAppConfigHelper.WebSettingsUrl(doc)
                                    Dim Collection As SettingsPropertyCollection = ClientSettingsProvider.GetPropertyMetadata(webSettingsUrl)

                                    Dim badNames As New List(Of String)
                                    Dim unreferencedTypes As New List(Of String)
                                    Using Transaction As New SettingsDesignerUndoTransaction(Settings.Site, SR.GetString(SR.SD_UndoTran_TypeChanged))
                                        RemoveAllWebProviderSettings()

                                        For Each settingsProp As SettingsProperty In Collection
                                            If (Not servicesAuthForm.LoadAnonymously) OrElse AllowsAnonymous(settingsProp) Then
                                                If Not Settings.IsUniqueName(settingsProp.Name) Then
                                                    badNames.Add(settingsProp.Name)
                                                Else
                                                    Dim newInstance As New DesignTimeSettingInstance
                                                    If settingsProp.PropertyType Is Nothing Then
                                                        unreferencedTypes.Add(settingsProp.Name)
                                                    Else
                                                        newInstance.SetName(settingsProp.Name)
                                                        newInstance.SetSettingTypeName(settingsProp.PropertyType.FullName)
                                                        newInstance.SetProvider(ServicesPropPageAppConfigHelper.ClientSettingsProviderName)
                                                        'TODO: Is this the right string value
                                                        If settingsProp.DefaultValue IsNot Nothing Then
                                                            newInstance.SetSerializedValue(settingsProp.DefaultValue.ToString())
                                                        End If
                                                        If settingsProp.IsReadOnly Then
                                                            newInstance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
                                                        End If
                                                        Settings.Add(newInstance)
                                                    End If
                                                End If
                                            End If
                                        Next
                                        Transaction.Commit()
                                    End Using

                                    ShowErrorIfThereAreUnreferencedTypes(unreferencedTypes)
                                    ShowErrorIfThereAreDuplicateNames(badNames)

                                    'TODO: This doesn't seem to be public, and http://ddindex is down...
                                    'Catch actionNotSupported As System.ServiceModel.ActionNotSupportedException
                                    '    DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, "", actionNotSupported, DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site))
                                Else
                                    DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, SR.GetString(SR.SD_ERR_CantAuthenticate), DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Error)
                                End If
                            End Using
                        End If
                    Catch innerException As XmlException
                        Dim ex As New XmlException(SR.GetString(SR.PPG_Services_InvalidAppConfigXml))
                        DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, "", ex, DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site))
                    End Try
                End If
            End If
        End Sub

        ''' <summary>
        ''' Whether this SettingsProperty allows anonymous access
        ''' </summary>
        ''' <param name="settingsProp">The SettingsProperty to check for AllowsAnonymous attribute</param>
        ''' <remarks></remarks>
        Private Shared Function AllowsAnonymous(ByVal settingsProp As SettingsProperty) As Boolean
            If settingsProp IsNot Nothing AndAlso settingsProp.Attributes IsNot Nothing AndAlso _
            settingsProp.Attributes.ContainsKey("AllowAnonymous") Then
                Dim value As Object = settingsProp.Attributes("AllowAnonymous")
                Return value IsNot Nothing AndAlso value.Equals(True)
            End If
            Return False
        End Function


        ''' <summary>
        ''' If there are unreferenced types, display an error dialog
        ''' </summary>
        ''' <param name="badNames">List of the bad names</param>
        ''' <remarks></remarks>
        Private Sub ShowErrorIfThereAreUnreferencedTypes(ByVal badNames As List(Of String))
            If badNames.Count > 0 Then
                Dim displayString As String = String.Join(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator, badNames.ToArray())
                DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.SD_ERR_UnreferencedTypeNameList_1Arg), displayString), DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub

        ''' <summary>
        ''' If there are duplicate names, display an error dialog
        ''' </summary>
        ''' <param name="badNames">List of the bad names</param>
        ''' <remarks></remarks>
        Private Sub ShowErrorIfThereAreDuplicateNames(ByVal badNames As List(Of String))
            If badNames.Count > 0 Then
                Dim displayString As String = String.Join(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator, badNames.ToArray())
                DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.SD_ERR_DuplicateNameList_1Arg), displayString), DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub

        ''' <summary>
        ''' Remove all the WebProvider settings
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RemoveAllWebProviderSettings()
            Dim settingsToRemove As New List(Of DesignTimeSettingInstance)
            Dim setting As DesignTimeSettingInstance

            For Each setting In Settings
                If DesignTimeSettingInstance.IsWebProvider(setting) Then
                    settingsToRemove.Add(setting)
                End If
            Next

            For Each setting In settingsToRemove
                Settings.Remove(setting)
            Next
        End Sub

        ''' <summary>
        ''' Should the Add setting menu command be enabled?
        ''' </summary>
        ''' <param name="menucommand"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MenuAddSettingEnableHandler(ByVal menucommand As DesignerMenuCommand) As Boolean
            Return InDesignMode() AndAlso Not DesignerLoader.IsReadOnly
        End Function

        ''' <summary>
        ''' Add a new setting to the grid
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuAddSetting(ByVal sender As Object, ByVal e As EventArgs)
            m_SettingsGridView.CurrentCell = m_SettingsGridView.Rows(m_SettingsGridView.Rows.Count - 1).Cells(NameColumnNo)
            Debug.Assert(m_SettingsGridView.CurrentRow.Tag Is Nothing, "Adding a new setting failed - there is already a setting associated with the new row!?")
            m_SettingsGridView.BeginEdit(True)
        End Sub

        ''' <summary>
        ''' Start editing the current cell
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuEditCell(ByVal sender As Object, ByVal e As EventArgs)
            If InDesignMode() Then
                If m_SettingsGridView.CurrentCell IsNot Nothing AndAlso Not m_SettingsGridView.IsCurrentCellInEditMode Then
                    If EnsureCheckedOut() Then
                        m_SettingsGridView.BeginEdit(False)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub MenuRemove(ByVal Sender As Object, ByVal e As EventArgs)
            ' Gotta check out files before removing anything...
            If Not EnsureCheckedOut() Then
                Return
            End If

            Dim rowsToDelete As New Generic.Dictionary(Of DataGridViewRow, Boolean)

            ' Find all rows with containing a selected cell
            For Each cell As DataGridViewCell In m_SettingsGridView.SelectedCells
                rowsToDelete(m_SettingsGridView.Rows(cell.RowIndex)) = True
            Next

            RemoveRows(rowsToDelete.Keys)
        End Sub

#End Region

        ''' <summary>
        ''' The SettingsDesigner will forward IOleCommandTarget calls to it's view. In this case, we never actually implement any
        ''' commands, so we always return FALSE to indicate that we haven't done anything
        ''' </summary>
        ''' <param name="pguidCmdGroup"></param>
        ''' <param name="nCmdID"></param>
        ''' <param name="nCmdexecopt"></param>
        ''' <param name="pvaIn"></param>
        ''' <param name="pvaOut"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function HandleCommand(ByRef pguidCmdGroup As System.Guid, ByVal nCmdID As UInteger, ByVal nCmdexecopt As UInteger, ByVal pvaIn As System.IntPtr, ByVal pvaOut As System.IntPtr) As Boolean
            Return False
        End Function

#Region "IVsWindowPaneCommit implementation"
        Public Function IVsWindowPaneCommit_CommitPendingEdit(ByRef pfCommitFailed As Integer) As Integer Implements IVsWindowPaneCommit.CommitPendingEdit
            If CommitPendingChanges(False, False) Then
                pfCommitFailed = 0
            Else
                pfCommitFailed = 1
            End If
            Return NativeMethods.S_OK
        End Function
#End Region

        Private Sub ReportError(ByVal Message As String, ByVal HelpLink As String)
            ' Work around for VsWhidbey 224085 (app designer stealing the focus)
            Dim hwndFocus As IntPtr = NativeMethods.GetFocus()
            ' We also need to indicate that we are showing a modal dialog box so we don't try and commit 
            ' any pending changes 'cause of the change of active window...
            Dim savedReportingError As Boolean = m_isReportingError
            Try
                m_isReportingError = True
                DesignerFramework.DesignUtil.ReportError(Settings.Site, Message, HelpLink)
            Finally
                m_isReportingError = savedReportingError
                ' Work around for VsWhidbey 224085 (app designer stealing my focus)
                If hwndFocus <> IntPtr.Zero Then
                    Switches.TracePDFocus(TraceLevel.Warning, "[disabled] SettingsDesignerView.ReportError focus hack: NativeMethods.SetFocus(hwndFocus)")
                    'NativeMethods.SetFocus(hwndFocus) - disabled this hack, it causes problems now that project designer is handling focus better
                End If
            End Try
        End Sub

        ''' <summary>
        ''' When the "Browse" item in the types combobox is selected, we want to pop a 
        ''' the type picker dialog...
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub TypeComboBoxSelectedIndexChanged()
            Dim ptCurrent As System.Drawing.Point = m_SettingsGridView.CurrentCellAddress

            If ptCurrent.X <> TypeColumnNo Then
                Debug.Fail("We shouldn't browse for a type when the current cell isn't the type cell!")
                Return
            End If
            If m_isShowingTypePicker Then
                Return
            End If

            Try
                m_isShowingTypePicker = True
                If Not System.DBNull.Value.Equals(m_SettingsGridView.CurrentCell.Value) Then
                    Dim TypePickerDlg As New TypePickerDialog(Settings.Site, Me.DesignerLoader.VsHierarchy, Me.DesignerLoader.ProjectItemid)

                    TypePickerDlg.SetProjectReferencedAssemblies()

                    If UIService.ShowDialog(TypePickerDlg) = DialogResult.OK Then
                        ChangeSettingType(m_SettingsGridView.Rows(ptCurrent.Y), TypePickerDlg.TypeName)
                    Else
                        ' The user clicked cancel in the dialog - let's cancel this edit
                        m_SettingsGridView.CancelEdit()
                    End If
                End If
            Finally
                m_isShowingTypePicker = False
            End Try
        End Sub

        ''' <summary>
        ''' If we want to sort the value column, we should sort the formatted values, not the
        ''' values themselves. 
        ''' If we want to sort the scope column, we should also sort the formatted value in order
        ''' to group web applications together...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' We are digging up the editedformattedvalue from the cell rather than serialize
        ''' the value that we get passed in every for perf. reasons...
        ''' </remarks>
        Private Sub m_SettingsGridView_SortCompare(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewSortCompareEventArgs) Handles m_SettingsGridView.SortCompare
            If e.Column.Index = ValueColumnNo OrElse e.Column.Index = ScopeColumnNo Then
                Dim strVal1 As String = TryCast(m_SettingsGridView.Rows(e.RowIndex1).Cells(e.Column.Index).EditedFormattedValue, String)
                Dim strVal2 As String = TryCast(m_SettingsGridView.Rows(e.RowIndex2).Cells(e.Column.Index).EditedFormattedValue, String)
                If strVal1 Is Nothing Then strVal1 = ""
                If strVal2 Is Nothing Then strVal2 = ""
                e.SortResult = System.StringComparer.CurrentCulture().Compare(strVal1, strVal2)
                e.Handled = True
            End If
        End Sub

        ''' <summary>
        ''' Receives broadcast messages passed on by the VS shell
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <param name="wParam"></param>
        ''' <param name="lParam"></param>
        ''' <remarks></remarks>
        Private Function OnBroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr) As Integer Implements Shell.Interop.IVsBroadcastMessageEvents.OnBroadcastMessage
            If msg = Interop.win.WM_SETTINGCHANGE Then
                SetFonts()
            End If
        End Function

        ''' <summary>
        ''' Helper method to get a service from either our settings object or from out root designer
        ''' </summary>
        ''' <param name="service"></param>
        ''' <remarks></remarks>
        Protected Overrides Function GetService(ByVal service As System.Type) As Object
            Dim svc As Object = Nothing
            If Me.Settings IsNot Nothing AndAlso Me.Settings.Site IsNot Nothing Then
                svc = Me.Settings.Site.GetService(service)
            End If

            If svc Is Nothing AndAlso m_RootDesigner IsNot Nothing Then
                svc = m_RootDesigner.GetService(service)
            End If

            If svc Is Nothing Then
                Return MyBase.GetService(service)
            Else
                Return svc
            End If
        End Function

        ''' <summary>
        ''' Calculate an appropriate row height for added rows...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub OnRowAdded(ByVal sender As Object, ByVal e As DataGridViewRowsAddedEventArgs) Handles m_SettingsGridView.RowsAdded
            Dim newRow As DataGridViewRow = m_SettingsGridView.Rows(e.RowIndex)
            newRow.Height = newRow.GetPreferredHeight(e.RowIndex, DataGridViewAutoSizeRowMode.AllCells, True)
        End Sub

        ''' <summary>
        ''' Whenever the font changes, we have to resize the row headers...
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnFontChanged(ByVal e As EventArgs)
            MyBase.OnFontChanged(e)
            m_SettingsGridView.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells)
        End Sub

        Protected Overrides Sub OnLayout(ByVal levent As System.Windows.Forms.LayoutEventArgs)
            Switches.TracePDPerf("OnLayout BEGIN: SettingsDesignerView.OnLayout()")
            MyBase.OnLayout(levent)
            Switches.TracePDPerf("   OnLayout END: SettingsDesignerView.OnLayout()")
        End Sub

        Private ReadOnly Property DesignerLoader() As SettingsDesignerLoader
            Get
                If m_designerLoader Is Nothing Then
                    m_designerLoader = TryCast(GetService(GetType(IDesignerLoaderService)), SettingsDesignerLoader)
                End If
                Return m_designerLoader
            End Get
        End Property


    End Class

End Namespace

'******************************************************************************
'* PropPageDesignerView.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Editors.AppDesCommon.Utils
Imports Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner
Imports Microsoft.VisualStudio.Shell.Design
Imports System
Imports System.Drawing
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Security.Permissions
Imports Microsoft.VisualBasic
Imports OleInterop = Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.ApplicationDesigner
Imports Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Editors.AppDesDesignerFramework
Imports Microsoft.VisualStudio.Editors.PropertyPages
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.PlatformUI
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports VSITEMID=Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' This is the UI for the PropertyPageDesigner
    ''' The view implements IVsProjectDesignerPageSite to allow the property page to 
    ''' notify us of property changes.  The page then sends private change notifications
    ''' which let us bubble the notifcation into the standard component changed mechanism.
    ''' This will cause the normal undo mechanism to be invoked.
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class PropPageDesignerView
        Inherits System.Windows.Forms.UserControl
        Implements IVsProjectDesignerPageSite
        Implements IVsWindowPaneCommit
        Implements IVsEditWindowNotify
        Implements IServiceProvider



#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()
            Me.SuspendLayout()

            Me.Text = "Property Page Designer View"    ' For Debug

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            Me.BackColor = PropertyPages.PropPageUserControlBase.PropPageBackColor

            ' Scale the width of the Configuration/Platform combo boxes
            Me.ConfigurationComboBox.Width = DpiHelper.LogicalToDeviceUnitsX(ConfigurationComboBox.Width)
            Me.PlatformComboBox.Width = DpiHelper.LogicalToDeviceUnitsX(PlatformComboBox.Width)

            'Start out with the assumption that the configuration/platform comboboxes
            '  are invisible, otherwise they will flicker visible before being turned off.
            Me.ConfigurationPanel.Visible = False

            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub

        Public WithEvents ConfigDividerLine As System.Windows.Forms.Label
        Public WithEvents PlatformComboBox As System.Windows.Forms.ComboBox
        Public WithEvents PlatformLabel As System.Windows.Forms.Label
        Public WithEvents PropPageDesignerViewLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Public WithEvents ConfigurationFlowLayoutPanel As System.Windows.Forms.FlowLayoutPanel
        Public WithEvents ConfigurationTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Public WithEvents PLatformTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Public WithEvents ConfigurationPanel As System.Windows.Forms.TableLayoutPanel

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PropPageDesignerView))
            Me.ConfigurationComboBox = New System.Windows.Forms.ComboBox
            Me.PlatformLabel = New System.Windows.Forms.Label
            Me.PlatformComboBox = New System.Windows.Forms.ComboBox
            Me.ConfigDividerLine = New System.Windows.Forms.Label
            Me.ConfigurationLabel = New System.Windows.Forms.Label
            Me.PropertyPagePanel = New ScrollablePanel
            Me.PropPageDesignerViewLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.ConfigurationPanel = New System.Windows.Forms.TableLayoutPanel
            Me.ConfigurationFlowLayoutPanel = New System.Windows.Forms.FlowLayoutPanel
            Me.ConfigurationTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.PLatformTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.PropPageDesignerViewLayoutPanel.SuspendLayout()
            Me.ConfigurationPanel.SuspendLayout()
            Me.ConfigurationFlowLayoutPanel.SuspendLayout()
            Me.ConfigurationTableLayoutPanel.SuspendLayout()
            Me.PLatformTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'ConfigurationComboBox
            '
            resources.ApplyResources(Me.ConfigurationComboBox, "ConfigurationComboBox")
            Me.ConfigurationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ConfigurationComboBox.FormattingEnabled = True
            Me.ConfigurationComboBox.Name = "ConfigurationComboBox"
            '
            'PlatformLabel
            '
            resources.ApplyResources(Me.PlatformLabel, "PlatformLabel")
            Me.PlatformLabel.Name = "PlatformLabel"
            '
            'PlatformComboBox
            '
            resources.ApplyResources(Me.PlatformComboBox, "PlatformComboBox")
            Me.PlatformComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.PlatformComboBox.FormattingEnabled = True
            Me.PlatformComboBox.Name = "PlatformComboBox"
            '
            'ConfigDividerLine
            '
            resources.ApplyResources(Me.ConfigDividerLine, "ConfigDividerLine")
            Me.ConfigDividerLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
            Me.ConfigDividerLine.Name = "ConfigDividerLine"
            '
            'ConfigurationLabel
            '
            resources.ApplyResources(Me.ConfigurationLabel, "ConfigurationLabel")
            Me.ConfigurationLabel.Name = "ConfigurationLabel"
            '
            'PropertyPagePanel
            '
            resources.ApplyResources(Me.PropertyPagePanel, "PropertyPagePanel")
            Me.PropertyPagePanel.Name = "PropertyPagePanel"
            '
            'PropPageDesignerViewLayoutPanel
            '
            resources.ApplyResources(Me.PropPageDesignerViewLayoutPanel, "PropPageDesignerViewLayoutPanel")
            Me.PropPageDesignerViewLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.PropPageDesignerViewLayoutPanel.Controls.Add(Me.ConfigurationPanel, 0, 0)
            Me.PropPageDesignerViewLayoutPanel.Controls.Add(Me.PropertyPagePanel, 0, 1)
            Me.PropPageDesignerViewLayoutPanel.Name = "PropPageDesignerViewLayoutPanel"
            Me.PropPageDesignerViewLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.PropPageDesignerViewLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            '
            'ConfigurationPanel
            '
            resources.ApplyResources(Me.ConfigurationPanel, "ConfigurationPanel")
            Me.ConfigurationPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.ConfigurationPanel.Controls.Add(Me.ConfigDividerLine, 1, 1)
            Me.ConfigurationPanel.Controls.Add(Me.ConfigurationFlowLayoutPanel, 0, 0)
            Me.ConfigurationPanel.Name = "ConfigurationPanel"
            Me.ConfigurationPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.ConfigurationPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'ConfigurationFlowLayoutPanel
            '
            resources.ApplyResources(Me.ConfigurationFlowLayoutPanel, "ConfigurationFlowLayoutPanel")
            Me.ConfigurationFlowLayoutPanel.CausesValidation = False
            Me.ConfigurationFlowLayoutPanel.Controls.Add(Me.ConfigurationTableLayoutPanel)
            Me.ConfigurationFlowLayoutPanel.Controls.Add(Me.PLatformTableLayoutPanel)
            Me.ConfigurationFlowLayoutPanel.Name = "ConfigurationFlowLayoutPanel"
            '
            'ConfigurationTableLayoutPanel
            '
            resources.ApplyResources(Me.ConfigurationTableLayoutPanel, "ConfigurationTableLayoutPanel")
            Me.ConfigurationTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.ConfigurationTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.ConfigurationTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10.0!))
            Me.ConfigurationTableLayoutPanel.Controls.Add(Me.ConfigurationComboBox, 1, 0)
            Me.ConfigurationTableLayoutPanel.Controls.Add(Me.ConfigurationLabel, 0, 0)
            Me.ConfigurationTableLayoutPanel.Name = "ConfigurationTableLayoutPanel"
            Me.ConfigurationTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'PLatformTableLayoutPanel
            '
            resources.ApplyResources(Me.PLatformTableLayoutPanel, "PLatformTableLayoutPanel")
            Me.PLatformTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.PLatformTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.PLatformTableLayoutPanel.Controls.Add(Me.PlatformLabel, 0, 0)
            Me.PLatformTableLayoutPanel.Controls.Add(Me.PlatformComboBox, 1, 0)
            Me.PLatformTableLayoutPanel.Name = "PLatformTableLayoutPanel"
            Me.PLatformTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'PropPageDesignerView
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.PropPageDesignerViewLayoutPanel)
            Me.Name = "PropPageDesignerView"
            Me.PropPageDesignerViewLayoutPanel.ResumeLayout(False)
            Me.PropPageDesignerViewLayoutPanel.PerformLayout()
            Me.ConfigurationPanel.ResumeLayout(False)
            Me.ConfigurationPanel.PerformLayout()
            Me.ConfigurationFlowLayoutPanel.ResumeLayout(False)
            Me.ConfigurationFlowLayoutPanel.PerformLayout()
            Me.ConfigurationTableLayoutPanel.ResumeLayout(False)
            Me.ConfigurationTableLayoutPanel.PerformLayout()
            Me.PLatformTableLayoutPanel.ResumeLayout(False)
            Me.PLatformTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region

        'The currently-loaded page and its site
        Private m_LoadedPage As OleInterop.IPropertyPage
        Private m_LoadedPageSite As ApplicationDesigner.PropertyPageSite

        'True once we have been initialized completely.
        Private m_fInitialized As Boolean = False

        'If true, we ignore the selected index changed event
        Private m_IgnoreSelectedIndexChanged As Boolean = False

        Private m_ErrorControl As Control 'Displayed error control, if any

        Public Const SW_HIDE As Integer = 0
        Public Const SW_SHOWNORMAL As Integer = 1
        Public Const SW_SHOW As Integer = 5

        Public WithEvents ConfigurationLabel As System.Windows.Forms.Label
        Public WithEvents PropertyPagePanel As ScrollablePanel
        Public WithEvents ConfigurationComboBox As System.Windows.Forms.ComboBox

        Private m_RootDesigner As PropPageDesignerRootDesigner
        Private m_ProjectHierarchy As IVsHierarchy

        ' The ConfigurationState object from the project designer.  This is shared among all the prop page designers
        '   for this project designer.
        Private m_ConfigurationState As ConfigurationState

        'True if we should check for simplified config mode having changed (used to keep from checking multiple times in a row)
        Private m_NeedToCheckForModeChanges As Boolean

        'The number of undo units that were available when the page was in a clean state.
        Private m_UndoUnitsOnStackAtCleanState As Integer = 0

        ' The UndoEngine for this designer
        Private WithEvents m_UndoEngine As UndoEngine

        ' The DesignerHost for this designer
        Private WithEvents m_DesignerHost As IDesignerHost

        'True iff the property page is currently activated
        Private m_IsPageActivated As Boolean

        'True iff the property page is hosted through native SetParent and not as a Windows Form child control
        Private m_IsNativeHostedPropertyPage As Boolean

#Region "Constructor"

        ''' <summary>
        ''' View constructor 
        ''' </summary>
        ''' <param name="RootDesigner"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal RootDesigner As PropPageDesignerRootDesigner)
            Me.New()
#If DEBUG Then
            PropPageDesignerViewCount += 1
            InstanceCount += 1
            MyInstanceCount = InstanceCount
#End If
            SetSite(RootDesigner)
        End Sub

#End Region


#Region "Dispose/IDisposable"
        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                Try
                    If m_RootDesigner IsNot Nothing Then
                        m_RootDesigner.RemoveMenuCommands()
                    End If

                    m_UndoEngine = Nothing
                    UnLoadPage()
                    If Not (components Is Nothing) Then
                        components.Dispose()
                    End If
                    m_ConfigurationState = Nothing
                Catch
                    'Don't throw here trying to cleanup
                End Try
#If DEBUG Then
                PropPageDesignerViewCount -= 1
#End If
            End If
            MyBase.Dispose(disposing)
        End Sub
#End Region


#If DEBUG Then
        'These are placed here to prevent screwing up the WinForms designer
        'resulting from the #if DEBUG
        Private Shared PropPageDesignerViewCount As Integer = 0
        Private Shared InstanceCount As Integer
        Private MyInstanceCount As Integer
#End If

        ''' <summary>
        ''' Get DesignerHost
        ''' </summary>
        Public ReadOnly Property DesignerHost() As IDesignerHost
            Get
                Return TryCast(GetService(GetType(IDesignerHost)), IDesignerHost)
            End Get
        End Property


        ''' <summary>
        ''' Property page we host
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property PropPage() As OleInterop.IPropertyPage
            Get
                Return m_LoadedPage
            End Get
        End Property

        Private m_IsConfigPage As Boolean
        Public Property IsConfigPage() As Boolean
            Get
                Return m_IsConfigPage
            End Get
            Set(ByVal Value As Boolean)
                m_IsConfigPage = Value
            End Set
        End Property


        Private m_DTEProject As EnvDTE.Project

        Public ReadOnly Property DTEProject() As EnvDTE.Project
            Get
                Return m_DTEProject
            End Get
        End Property


        ''' <summary>
        ''' True iff the property page is hosted through native SetParent and not as a Windows Form child control.
        ''' Returns False if the property page is not currently activated
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsNativeHostedPropertyPageActivated() As Boolean
            Get
                Return m_IsPageActivated AndAlso m_IsNativeHostedPropertyPage
            End Get
        End Property

        ''' <summary>
        ''' Gets the browse object for the project.  This is what is passed to SetObjects for
        '''   non-config-dependent pages
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetProjectBrowseObject() As Object
            Dim BrowseObject As Object = Nothing
            VSErrorHandler.ThrowOnFailure(m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_BrowseObject, BrowseObject))
            Return BrowseObject
        End Function


        Private m_VsCfgProvider As IVsCfgProvider2
        Private ReadOnly Property VsCfgProvider() As IVsCfgProvider2
            Get
                If m_VsCfgProvider Is Nothing Then
                    Dim Value As Object = Nothing

                    VSErrorHandler.ThrowOnFailure(m_ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, Value))

                    m_VsCfgProvider = CType(Value, IVsCfgProvider2)
                End If
                Return m_VsCfgProvider
            End Get
        End Property

        ''' <summary>
        ''' Initialization routine called by the ApplicationDesignerView when the page is first activated
        ''' </summary>
        ''' <param name="DTEProject"></param>
        ''' <param name="PropPage"></param>
        ''' <param name="PropPageSite"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="IsConfigPage"></param>
        ''' <remarks></remarks>
        Public Sub Init(ByVal DTEProject As EnvDTE.Project, ByVal PropPage As OleInterop.IPropertyPage, ByVal PropPageSite As ApplicationDesigner.PropertyPageSite, ByVal Hierarchy As IVsHierarchy, ByVal IsConfigPage As Boolean)
            Debug.Assert(m_DTEProject Is Nothing, "Init() called twice?")

            Debug.Assert(DTEProject IsNot Nothing, "DTEProject is Nothing")
            Debug.Assert(PropPage IsNot Nothing)
            Debug.Assert(PropPageSite IsNot Nothing)
            Debug.Assert(Hierarchy IsNot Nothing)

            m_DTEProject = DTEProject
            m_LoadedPage = PropPage
            m_LoadedPageSite = PropPageSite
            m_ProjectHierarchy = Hierarchy
            Me.IsConfigPage = IsConfigPage

            Me.SuspendLayout()
            Me.ConfigurationPanel.SuspendLayout()
            Me.PropertyPagePanel.SuspendLayout()

            SetDialogFont()

            Dim menuCommands As New Collections.ArrayList()
            Dim cutCmd As New AppDesDesignerFramework.DesignerMenuCommand(m_RootDesigner, Microsoft.VisualStudio.Editors.Constants.MenuConstants.CommandIDVSStd97cmdidCut, AddressOf DisabledMenuCommandHandler)
            cutCmd.Enabled = False
            menuCommands.Add(cutCmd)

            m_RootDesigner.RegisterMenuCommands(menuCommands)

            ' Get the ConfigurationState object from the project designer
            m_ConfigurationState = DirectCast(m_LoadedPageSite.GetService(GetType(ConfigurationState)), ConfigurationState)
            If m_ConfigurationState Is Nothing Then
                Debug.Fail("Couldn't get ConfigurationState service")
                Throw New Package.InternalException
            End If
            If IsConfigPage Then
                AddHandler m_ConfigurationState.SelectedConfigurationChanged, AddressOf ConfigurationState_SelectedConfigurationChanged
                AddHandler m_ConfigurationState.ConfigurationListAndSelectionChanged, AddressOf ConfigurationState_ConfigurationListAndSelectionChanged

                'Note: we only hook this up for config pages because the situations where we (currently) need to clear the undo/redo stack only
                '  affects config pages (when a config/platform is deleted or renamed).
                AddHandler m_ConfigurationState.ClearConfigPageUndoRedoStacks, AddressOf ConfigurationState_ClearConfigPageUndoRedoStacks
            End If

            'This notification is needed by config and non-config pages
            AddHandler m_ConfigurationState.SimplifiedConfigModeChanged, AddressOf ConfigurationState_SimplifiedConfigModeChanged

            'Scale the comboboxes widths if necessary, for High-DPI
            Me.ConfigurationComboBox.Size = DpiHelper.LogicalToDeviceUnits(Me.ConfigurationComboBox.Size)
            Me.PlatformComboBox.Size = DpiHelper.LogicalToDeviceUnits(Me.PlatformComboBox.Size)

            'Set up configuration/platform comboboxes
            SetConfigDropdownVisibility()
            UpdateConfigLists() 'This is done initially for config and non-config pages

            'Set the initial dropdown selections
            If IsConfigPage Then
                ChangeSelectedComboBoxIndicesWithoutNotification(m_ConfigurationState.SelectedConfigIndex, m_ConfigurationState.SelectedPlatformIndex)
            End If

            'Populate the page initially
            SetObjectsForSelectedConfigs()

            Me.ActivatePage(PropPage)

            Me.ConfigurationPanel.ResumeLayout(True)
            Me.PropertyPagePanel.ResumeLayout(True)
            Me.ResumeLayout(True)

            'PERF: no need to call UpdatePageSize here - Activate() is already passed in a rectangle to 
            '  move the control to initially
            'UpdatePageSize() 

            m_NeedToCheckForModeChanges = False
            m_fInitialized = True

            If m_UndoEngine Is Nothing Then
                m_UndoEngine = DirectCast(GetService(GetType(UndoEngine)), UndoEngine)
            End If

            If m_DesignerHost Is Nothing Then
                m_DesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
            End If
        End Sub


        ''' <summary>
        ''' Occurs after an undo or redo operation has completed.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_UndoEngine_Undone(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_UndoEngine.Undone
            'Tell the project designer it needs to refresh its dirty status
            If m_LoadedPageSite IsNot Nothing Then
                Dim AppDesignerView As ApplicationDesignerView = TryCast(m_LoadedPageSite.GetService(GetType(ApplicationDesignerView)), ApplicationDesignerView)
                If AppDesignerView IsNot Nothing Then
                    AppDesignerView.DelayRefreshDirtyIndicators()
                End If
            End If
        End Sub

        ''' <summary>
        ''' Update dirty state of the appdesigner (if any) after a transaction is closed
        '''  Somtimes the we may have tried to update the dirty state when a transaction is open (i.e. when opening files
        '''  from SCC). It is not possible for us to update the dirty state while a transaction is active, so we have to wait
        '''  until the transaction is closed. 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_DesignerHost_TransactionClosed(ByVal sender As Object, ByVal e As DesignerTransactionCloseEventArgs) Handles m_DesignerHost.TransactionClosed
            If m_LoadedPageSite IsNot Nothing Then
                Dim AppDesignerView As ApplicationDesignerView = TryCast(m_LoadedPageSite.GetService(GetType(ApplicationDesignerView)), ApplicationDesignerView)
                If AppDesignerView IsNot Nothing Then
                    AppDesignerView.DelayRefreshDirtyIndicators()
                End If
            End If
        End Sub

        ''' <summary>
        ''' Show the property page 
        ''' </summary>
        ''' <param name="PropPage"></param>
        ''' <remarks></remarks>
        Public Sub ActivatePage(ByVal PropPage As OleInterop.IPropertyPage)
            Common.Switches.TracePDPerfBegin("PropPageDesignerView.ActivatePage")
            If PropPage Is Nothing Then
                'Property page failed to load - just give empty page
            Else
                'Set the Undo site before activation
                If TypeOf PropPage Is IVsProjectDesignerPage Then
                    CType(PropPage, IVsProjectDesignerPage).SetSite(Me)
                End If

                'Activate the page
                Debug.Assert(Not Me.Handle.Equals(IntPtr.Zero), "Window not yet created")
                'Force creation of the control to get hwnd 
                If Me.Handle.Equals(IntPtr.Zero) Then
                    Me.CreateControl()
                End If

                Try

                    ' Check the minimum size for the control and make sure that we show scrollbars
                    ' if the PropertyPagePanel becomes smaller...
                    Dim Info As OleInterop.PROPPAGEINFO() = New OleInterop.PROPPAGEINFO(0) {}
                    If PropPage IsNot Nothing Then
                        PropPage.GetPageInfo(Info)
                        PropertyPagePanel.AutoScrollMinSize = New Size(Info(0).SIZE.cx + Me.Padding.Right + Me.Padding.Left, Info(0).SIZE.cy + Me.Padding.Top + Me.Padding.Bottom)
                    End If

                    PropPage.Activate(Me.PropertyPagePanel.Handle, New OleInterop.RECT() {GetPageRect()}, 0)

                    PropPage.Show(SW_SHOW)
                    'UpdateWindowStyles(Me.Handle)
                    'Me.MinimumSize = GetMaxSize()

                    ' Dev10 Bug 905047
                    ' Explicitly initialize the UI cue state so that focus and keyboard cues work.
                    ' We need to do this explicitly since this UI isn't a dialog (where the state
                    ' would have been automatically initialized)
                    InitializeStateOfUICues()

                    ' It is a managed control, we should update AutoScrollMinSize
                    If PropertyPagePanel.Controls.Count > 0 Then
                        Dim controlSize As Size = PropertyPagePanel.Controls(0).Size
                        PropertyPagePanel.AutoScrollMinSize = New Size( _
                                Math.Min(controlSize.Width + Me.Padding.Right + Me.Padding.Left, PropertyPagePanel.AutoScrollMinSize.Width), _
                                Math.Min(controlSize.Height + Me.Padding.Top + Me.Padding.Bottom, PropertyPagePanel.AutoScrollMinSize.Height))
                    End If

                    m_IsPageActivated = True

                    'Is the control hosted natively via SetParent?
                    If PropertyPagePanel.Controls.Count > 0 Then
                        m_IsNativeHostedPropertyPage = False
                    Else
                        m_IsNativeHostedPropertyPage = True
                    End If

                    If m_IsNativeHostedPropertyPage Then
                        'Try to set initial focus to the property page, not the configuration panel
                        FocusFirstOrLastPropertyPageControl(True)
                    End If

                    SetUndoRedoCleanState()

                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    'There was a problem displaying the property page.  Show the error control.
                    DisplayErrorControl(ex)
                End Try
            End If
            Common.Switches.TracePDPerfEnd("PropPageDesignerView.ActivatePage")
        End Sub

        ''' <summary>
        ''' Display the error control instead of a property page
        ''' </summary>
        ''' <param name="ex">The exception to retrieve the error message from</param>
        ''' <remarks></remarks>
        Private Sub DisplayErrorControl(ByVal ex As Exception)

            UnLoadPage()
            Me.PropertyPagePanel.SuspendLayout()
            Me.ConfigurationPanel.Visible = False
            If TypeOf (ex) Is PropertyPageException AndAlso Not DirectCast(ex, PropertyPageException).ShowHeaderAndFooterInErrorControl Then
                m_ErrorControl = New ErrorControl(ex.Message)
            Else
                m_ErrorControl = New ErrorControl(SR.GetString(SR.APPDES_ErrorLoadingPropPage) & vbCrLf & AppDesCommon.DebugMessageFromException(ex))
            End If
            m_ErrorControl.Dock = DockStyle.Fill
            m_ErrorControl.Visible = True
            Me.PropertyPagePanel.Controls.Add(m_ErrorControl)
            Me.PropertyPagePanel.ResumeLayout(True)
        End Sub

        ''' <summary>
        ''' Fixup the window styles so the mnemonics work on the new property page window
        ''' </summary>
        ''' <param name="Hwnd"></param>
        ''' <remarks></remarks>
        Private Sub UpdateWindowStyles(ByVal Hwnd As IntPtr)
            Dim HwndPage As IntPtr = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetTopWindow(Hwnd)
            Dim StyleValue, PreviousStyle As IntPtr
            Dim PreviousExStyle As IntPtr
            Dim ExStyleValue As Long

            If (Not HwndPage.Equals(IntPtr.Zero)) Then
                PreviousStyle = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetWindowLong(HwndPage, Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GWL_STYLE)
                StyleValue = New IntPtr(PreviousStyle.ToInt64() And (Not (Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.DS_CONTROL Or Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.WS_TABSTOP)))

                Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.SetWindowLong(HwndPage, Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GWL_STYLE, StyleValue)

                PreviousExStyle = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetWindowLong(HwndPage, Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GWL_EXSTYLE)
                '// if WS_EX_CONTROLPARENT isn't on, then mnemonics for buttons on the frame
                '// won't work if your focus is inside the sheet
                ExStyleValue = PreviousExStyle.ToInt64() Or Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.WS_EX_CONTROLPARENT
                Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.SetWindowLong(HwndPage, Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GWL_EXSTYLE, New IntPtr(ExStyleValue))
            End If
        End Sub

        ''' <summary>
        ''' Hide and deactivate the property page
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub UnLoadPage()
            m_IsPageActivated = False
            m_IsNativeHostedPropertyPage = False

            If m_LoadedPage IsNot Nothing Then
                'Store in local and clear member first in case of throw by deactivate
                Dim Page As OleInterop.IPropertyPage = m_LoadedPage
                m_LoadedPage = Nothing
                Try
                    Page.SetObjects(0, Nothing)
                    Page.Deactivate()
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Debug.WriteLine("Exception during m_LoadedPage.Deactivate")
                End Try
            End If
            If m_ErrorControl IsNot Nothing Then
                Controls.Remove(m_ErrorControl)
                m_ErrorControl.Dispose()
                m_ErrorControl = Nothing
            End If

            If m_ConfigurationState IsNot Nothing Then
                RemoveHandler m_ConfigurationState.SelectedConfigurationChanged, AddressOf ConfigurationState_SelectedConfigurationChanged
                RemoveHandler m_ConfigurationState.ConfigurationListAndSelectionChanged, AddressOf ConfigurationState_ConfigurationListAndSelectionChanged
                RemoveHandler m_ConfigurationState.ClearConfigPageUndoRedoStacks, AddressOf ConfigurationState_ClearConfigPageUndoRedoStacks
                RemoveHandler m_ConfigurationState.SimplifiedConfigModeChanged, AddressOf ConfigurationState_SimplifiedConfigModeChanged
            End If
        End Sub

        ''' <summary>
        ''' Our site - always of type PropPageDesignerRootDesigner
        ''' </summary>
        ''' <param name="RootDesigner"></param>
        ''' <remarks></remarks>
        Private Sub SetSite(ByVal RootDesigner As PropPageDesignerRootDesigner) 'Implements OLE.Interop.IObjectWithSite.SetSite
            m_RootDesigner = RootDesigner

            'We used to use a lighter color than SystemColors.Control (and we would get it from the color service).
            '  But that causes issues because a) we can't control disabled control colors, b) we have problems when our
            '  pages are hosted in another property page frame with different colors (e.g., C++), and c) we have problems
            '  when hosting a native property page over which we have no color control.
            'So now we simply use SystemColors.Control all the time (constant on PropPageUserControlBase).
            'Me.BackColor = GetPropertyPageBackColor(System.Drawing.SystemColors.ControlLight)

            Me.ConfigurationPanel.BackColor = Me.BackColor
            Me.PropertyPagePanel.BackColor = Me.BackColor
        End Sub

        ''' <summary>
        ''' GetService helper
        ''' </summary>
        ''' <param name="ServiceType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shadows Function GetService(ByVal ServiceType As Type) As Object Implements IServiceProvider.GetService
            Dim Service As Object
            Service = m_RootDesigner.GetService(ServiceType)
            Return Service
        End Function

        ''' <summary>
        ''' Get the size of the hosting client rect for sizing the property page 
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetPageRect() As OleInterop.RECT
            Dim ClientRect As New OleInterop.RECT
            ' We should use DisplayRectangle.Left/Top here, so the child page could work with auto-scroll
            With ClientRect
                .left = Me.PropertyPagePanel.DisplayRectangle.Left
                .top = Me.PropertyPagePanel.DisplayRectangle.Top
                .right = Me.PropertyPagePanel.ClientSize.Width + .left
                .bottom = Me.PropertyPagePanel.ClientSize.Height + .top
            End With
            Return ClientRect
        End Function

        ''' <summary>
        ''' Update the hosted property page size
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub UpdatePageSize()
            If m_LoadedPage IsNot Nothing Then
                Dim RectArray As OleInterop.RECT() = New OleInterop.RECT() {GetPageRect()}
                Common.Switches.TracePDPerfBegin("PropPageDesignerView.UpdatePageSize (" _
                    & RectArray(0).right - RectArray(0).left & ", " & RectArray(0).bottom - RectArray(0).top & ")")
                m_LoadedPage.Move(RectArray)
                Common.Switches.TracePDPerfEnd("PropPageDesignerView.UpdatePageSize")
            End If
        End Sub

        Protected Overrides Sub OnLayout(ByVal e As LayoutEventArgs)
            Common.Switches.TracePDPerfBegin(e, "PropPageDesignerView.OnLayout()")
            MyBase.OnLayout(e)

            ' Hard coded to change the size of the LayoutPanel to fit our clientSize. Otherwise, it will pick its own size...
            If DisplayRectangle <> Rectangle.Empty Then
                PropPageDesignerViewLayoutPanel.Size = Me.ClientSize
                UpdatePageSize()
            End If
            Common.Switches.TracePDPerfEnd("PropPageDesignerView.OnLayout()")
        End Sub



#Region "IVsProjectDesignerPageSite"

        ''' <summary>
        ''' This is part of the undo host code for the property page.  
        ''' We pass this interface to the property pages implementation of IVsProjectDesignerPage.SetSite
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub OnPropertyChanged(ByVal Component As Component, ByVal PropDesc As PropertyDescriptor, ByVal OldValue As Object, ByVal NewValue As Object)
            Dim ChangeService As IComponentChangeService = Nothing

            ChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)
            ChangeService.OnComponentChanged(Component, PropDesc, OldValue, NewValue)
        End Sub


        ''' <summary>
        ''' If a property page hosted by the Project Designer wants to support automatic Undo/Redo, it must call
        '''   call this method on the IVsProjectDesignerPageSite after a property value is changed.
        ''' </summary>
        ''' <param name="propertyName">The name of the property whose value has changed.</param>
        ''' <param name="propertyDescriptor">A PropertyDescriptor that describes the given property.</param>
        ''' <param name="oldValue">The previous value of the property.</param>
        ''' <param name="newValue">The new value of the property.</param>
        Public Sub IVsProjectDesignerPageSite_OnPropertyChanged(ByVal PropertyName As String, ByVal PropertyDescriptor As PropertyDescriptor, ByVal OldValue As Object, ByVal NewValue As Object) Implements IVsProjectDesignerPageSite.OnPropertyChanged
            'Note: we wrap the property descriptor here because it allows us to intercept the GetValue/SetValue calls and therefore
            '  more finely control the undo/redo process.
            OnPropertyChanged(m_RootDesigner.Component, New PropertyPagePropertyDescriptor(PropertyDescriptor, PropertyName), OldValue, NewValue)
        End Sub


        ''' <summary>
        ''' This is part of the undo host code for the property page.  
        ''' We pass this interface to the property pages implementation of IVsProjectDesignerPage.SetSite
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub OnPropertyChanging(ByVal Component As Component, ByVal PropDesc As PropertyDescriptor)
            Dim ChangeService As IComponentChangeService = Nothing
            Dim oldValue As Object = Nothing

            ChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)

            If PropDesc Is Nothing Then
                Debug.Fail("We should not be here")
                ChangeService.OnComponentChanging(Component, Nothing)
            Else
                ChangeService.OnComponentChanging(Component, PropDesc)
            End If
        End Sub


        ''' <summary>
        ''' If a property page hosted by the Project Designer wants to support automatic Undo/Redo, it must call
        '''   call this method on the IVsProjectDesignerPageSite before a property value is changed.  This allows 
        '''   the site to query for the current value of the property and save it for later use in handling Undo/Redo.
        ''' </summary>
        ''' <param name="propertyName">The name of the property whose value is about to change.</param>
        ''' <param name="propertyDescriptor">A PropertyDescriptor that describes the given property.</param>
        Public Sub IVsProjectDesignerPageSite_OnPropertyChanging(ByVal PropertyName As String, ByVal PropertyDescriptor As PropertyDescriptor) Implements IVsProjectDesignerPageSite.OnPropertyChanging
            'Note: we wrap the property descriptor here because it allows us to intercept the GetValue/SetValue calls and therefore
            '  more finely control the undo/redo process.
            OnPropertyChanging(m_RootDesigner.Component, New PropertyPagePropertyDescriptor(PropertyDescriptor, PropertyName))
        End Sub


        ''' <summary>
        ''' Retrieves a transaction which can be used to group multiple property changes into a single transaction, so that
        '''   they appear to the user as a single Undo/Redo unit.  The transaction must be committed or cancelled after the
        '''   property changes are made.
        ''' </summary>
        ''' <param name="description">The localized description string to use for the transaction.  This will appear as the
        '''   description for the Undo/Redo unit.</param>
        ''' <returns></returns>
        Public Function GetTransaction(ByVal Description As String) As System.ComponentModel.Design.DesignerTransaction Implements IVsProjectDesignerPageSite.GetTransaction
            Dim DesignerHost As IDesignerHost
            DesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
            Return DesignerHost.CreateTransaction(Description)
        End Function

#End Region

        ''' <summary>
        ''' Set font for controls on the Configuration panel
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub SetDialogFont()
            Me.Font = GetDialogFont()
        End Sub

        ''' <summary>
        ''' Pick font to use in this dialog page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property GetDialogFont() As Font
            Get
                Dim uiSvc As IUIService = CType(GetService(GetType(IUIService)), IUIService)
                If uiSvc IsNot Nothing Then
                    Return CType(uiSvc.Styles("DialogFont"), Font)
                End If

                Debug.Fail("Couldn't get a IUIService... cheating instead :)")

                Return Form.DefaultFont
            End Get
        End Property


        'Standard title for messageboxes, etc.
        Private ReadOnly MessageBoxCaption As String = SR.GetString(SR.APPDES_Title)


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
        Public Function DsMsgBox(ByVal Message As String, _
                ByVal Buttons As MessageBoxButtons, _
                ByVal Icon As MessageBoxIcon, _
                Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                Optional ByVal HelpLink As String = Nothing) As DialogResult

            Return AppDesDesignerFramework.DesignerMessageBox.Show(m_RootDesigner, Message, Me.MessageBoxCaption, _
                Buttons, Icon, DefaultButton, HelpLink)
        End Function


        ''' <summary>
        ''' Displays a designer error message
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <remarks></remarks>
        Public Sub ShowErrorMessage(ByVal Message As String, Optional ByVal HelpLink As String = Nothing)
            DsMsgBox(Message, MessageBoxButtons.OK, MessageBoxIcon.Error, HelpLink:=HelpLink)
        End Sub


#Region "Configuration/Platform Comboboxes and related code"

        ''' <summary>
        ''' Sets whether or not the configuration/platform dropdowns are visible
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetConfigDropdownVisibility()
            Me.ConfigurationPanel.Visible = Not m_ConfigurationState.IsSimplifiedConfigMode()

            If IsConfigPage Then
                Me.ConfigurationPanel.Enabled = True
            Else
                'Non-configuration pages should have the configuration panel visible but disabled, and the text should be "N/A"
                Me.ConfigurationPanel.Enabled = False

                Me.ConfigurationComboBox.Items.Add(SR.GetString(SR.PPG_NotApplicable))
                Me.ConfigurationComboBox.SelectedIndex = 0
                Me.PlatformComboBox.Items.Add(SR.GetString(SR.PPG_NotApplicable))
                Me.PlatformComboBox.SelectedIndex = 0
            End If

            'Update layout with the change in visibility
            PerformLayout()
        End Sub


        ''' <summary>
        ''' Changes the indices of the configuration and platform comboboxes, without causing a notify to be sent to the
        '''   ConfigurationState
        ''' </summary>
        ''' <param name="NewSelectedConfigIndex">New index into the ConfigurationComboBox</param>
        ''' <param name="NewSelectedPlatformIndex">New index into the PlatformComboBox</param>
        ''' <remarks></remarks>
        Private Sub ChangeSelectedComboBoxIndicesWithoutNotification(ByVal NewSelectedConfigIndex As Integer, ByVal NewSelectedPlatformIndex As Integer)
            Debug.Assert(Not m_IgnoreSelectedIndexChanged)

            Dim OldIgnoreSelectedIndexChanged As Boolean = m_IgnoreSelectedIndexChanged
            m_IgnoreSelectedIndexChanged = True
            Try
                ConfigurationComboBox.SelectedIndex = NewSelectedConfigIndex
                PlatformComboBox.SelectedIndex = NewSelectedPlatformIndex
            Finally
                m_IgnoreSelectedIndexChanged = OldIgnoreSelectedIndexChanged
            End Try
        End Sub

        ''' <summary>
        ''' Occurs when the user selects an item in the configuration combobox or the platform combobox
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub SelectedConfigurationOrPlatformIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) _
                        Handles ConfigurationComboBox.SelectedIndexChanged, PlatformComboBox.SelectedIndexChanged, PlatformComboBox.SelectedIndexChanged

            If m_fInitialized AndAlso Not m_IgnoreSelectedIndexChanged Then
                Debug.Assert(IsConfigPage)
                If IsConfigPage Then
                    'Notify the ConfigurationState of the change.  It will in turn notify us via SelectedConfigurationChanged
                    m_ConfigurationState.ChangeSelection(ConfigurationComboBox.SelectedIndex, PlatformComboBox.SelectedIndex, FireNotifications:=True)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Fired when the selected configuration is changed on another property page or in the
        '''   configuration manager.
        ''' </summary>
        ''' <remarks>
        ''' Listener must update their selection state by querying SelectedConfigIndex 
        '''   and SelectedPlatformIndex.
        ''' </remarks>>
        Private Sub ConfigurationState_SelectedConfigurationChanged()
            Debug.Assert(IsConfigPage)
            If IsConfigPage Then
                'Update combobox selections
                ChangeSelectedComboBoxIndicesWithoutNotification(m_ConfigurationState.SelectedConfigIndex, m_ConfigurationState.SelectedPlatformIndex)

                '... and tell the page to update based on the new selection 'CONSIDER delaying this call until we're the active designer
                SetObjectsForSelectedConfigs()
            End If
        End Sub


        ''' <summary>
        ''' Raised when the configuration/platform lists have changed.  Note that this
        '''   will *not* be followed by a SelectedConfigurationChanged event, but the
        '''   listener should still update the selection as well as their lists.
        ''' </summary>
        ''' <remarks>
        ''' Listener must update their selection state as well as their lists, by 
        '''   querying ConfigurationDropdownEntries, PlatformDropdownEntries, 
        '''   SelectedConfigIndex and SelectedPlatformIndex.
        ''' </remarks>
        Private Sub ConfigurationState_ConfigurationListAndSelectionChanged()
            Debug.Assert(IsConfigPage)
            If IsConfigPage Then
                'Update out list
                UpdateConfigLists()

                '... and our selection state
                ChangeSelectedComboBoxIndicesWithoutNotification(m_ConfigurationState.SelectedConfigIndex, m_ConfigurationState.SelectedPlatformIndex)

                '.. and tell the page to update based on the new selection 'CONSIDER delaying this call until we're the active designer
                SetObjectsForSelectedConfigs()
            End If
        End Sub


        ''' <summary>
        ''' Raised when the undo/redo stack of a property page should be cleared because of
        '''   changes to configurations/platforms that are not currently supported by our
        '''   undo/redo story.
        ''' </summary>
        Private Sub ConfigurationState_ClearConfigPageUndoRedoStacks()
            Common.Switches.TracePDUndo("Clearing undo/redo stack for page """ & GetPageTitle() & """")
            ClearUndoStackForPage()
        End Sub


        ''' <summary>
        ''' Raised when the value of the SimplifiedConfigMode property changes.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConfigurationState_SimplifiedConfigModeChanged()
            SetConfigDropdownVisibility()

            'This may change the objects selected.  Also, pages might have UI that depends on this setting, so get everything to update...
            SetObjectsForSelectedConfigs()
        End Sub


        ''' <summary>
        ''' Check if the simplified configs mode property has changed (we do this on WM_SETFOCUS, since there's no notification
        '''   of a change)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CheckForModeChanges()
            If m_ConfigurationState IsNot Nothing AndAlso m_fInitialized AndAlso m_NeedToCheckForModeChanges Then
                m_ConfigurationState.CheckForModeChanges()
                m_NeedToCheckForModeChanges = False
            End If
        End Sub


        ''' <summary>
        ''' Updates the configuration and platform combobox dropdown lists and selects the first entry in each list
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateConfigLists()
            If Not IsConfigPage Then
                Exit Sub
            End If

            'Populate the dropdowns
            ConfigurationComboBox.Items.Clear()
            PlatformComboBox.Items.Clear()
            ConfigurationComboBox.BeginUpdate()
            PlatformComboBox.BeginUpdate()
            Try
                For Each ConfigEntry As ConfigurationState.DropdownItem In m_ConfigurationState.ConfigurationDropdownEntries
                    ConfigurationComboBox.Items.Add(ConfigEntry.DisplayName)
                Next
                For Each PlatformEntry As ConfigurationState.DropdownItem In m_ConfigurationState.PlatformDropdownEntries
                    PlatformComboBox.Items.Add(PlatformEntry.DisplayName)
                Next
            Finally
                ConfigurationComboBox.EndUpdate()
                PlatformComboBox.EndUpdate()
            End Try

            'Select the first entry in each combobox
            ChangeSelectedComboBoxIndicesWithoutNotification(0, 0)
        End Sub


        ''' <summary>
        ''' Returns the currently selected config combobox item
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetSelectedConfigItem() As ConfigurationState.DropdownItem
            Debug.Assert(Me.ConfigurationComboBox.SelectedIndex >= 0)
            Debug.Assert(Me.ConfigurationComboBox.Items.Count = m_ConfigurationState.ConfigurationDropdownEntries.Length, _
                "The combobox is not in sync")
            Dim ConfigItem As ConfigurationState.DropdownItem = m_ConfigurationState.ConfigurationDropdownEntries(ConfigurationComboBox.SelectedIndex)
            Debug.Assert(ConfigItem IsNot Nothing)
            Return ConfigItem
        End Function


        ''' <summary>
        ''' Returns the currently selected platform combobox item
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetSelectedPlatformItem() As ConfigurationState.DropdownItem
            Debug.Assert(Me.PlatformComboBox.SelectedIndex >= 0)
            Debug.Assert(Me.PlatformComboBox.Items.Count = m_ConfigurationState.PlatformDropdownEntries.Length, _
                "The combobox is not in sync")
            Dim PlatformItem As ConfigurationState.DropdownItem = m_ConfigurationState.PlatformDropdownEntries(PlatformComboBox.SelectedIndex)
            Debug.Assert(PlatformItem IsNot Nothing)
            Return PlatformItem
        End Function


        ''' <summary>
        ''' Determines the set of currently-selected configurations by inspecting the configuration comboboxes, 
        '''   and passes that set of objects to the loaded property page
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetObjectsForSelectedConfigs()
            CommitPendingChanges()

            If Not IsConfigPage Then
                'Use the project's browse object for SetObjects
                CallPageSetObjects(New Object() {GetProjectBrowseObject()})
            Else
                'If here, then we are a config-dependent page...

                Dim SelectedConfigIndex As Integer = Me.ConfigurationComboBox.SelectedIndex
                Debug.Assert(SelectedConfigIndex <> -1, "Selection should be set for config name")
                Dim SelectedConfigItem As ConfigurationState.DropdownItem = GetSelectedConfigItem()

                Dim SelectedPlatformIndex As Integer = Me.PlatformComboBox.SelectedIndex
                Debug.Assert(SelectedPlatformIndex <> -1, "Selection should be set for platfofm")
                Dim SelectedPlatformItem As ConfigurationState.DropdownItem = GetSelectedPlatformItem()

                Dim AllConfigurations As Boolean = False
                If SelectedConfigItem.SelectionType = ConfigurationState.SelectionTypes.All Then
                    'User selected "All Configurations"
                    AllConfigurations = True
                ElseIf Not Me.ConfigurationComboBox.Visible Then
                    ' When the config panel is hidden we should update all configs/platforms
                    AllConfigurations = True
                End If

                Dim AllPlatforms As Boolean = False
                If SelectedPlatformItem.SelectionType = ConfigurationState.SelectionTypes.All Then
                    'User selected "All Platforms"
                    AllPlatforms = True
                ElseIf Not Me.ConfigurationComboBox.Visible Then
                    ' When the config panel is hidden we should update all configs/platforms
                    AllPlatforms = True
                End If

                'Find all matching config/platform combinations
                Dim ConfigObjects As Object() = Nothing

                If AllConfigurations AndAlso AllPlatforms Then
                    'All configurations and platforms
                    Dim Configs() As IVsCfg = m_ConfigurationState.GetAllConfigs()

                    'Must have an array of object, not IVsCfg
                    ConfigObjects = New Object(Configs.Length - 1) {}
                    Configs.CopyTo(ConfigObjects, 0)
                ElseIf Not AllConfigurations AndAlso Not AllPlatforms Then
                    'A single config/platform combination selected
                    Dim Cfg As IVsCfg = Nothing
                    If VSErrorHandler.Succeeded(VsCfgProvider.GetCfgOfName(SelectedConfigItem.Name, SelectedPlatformItem.Name, Cfg)) Then
                        ConfigObjects = New Object() {Cfg}
                    Else
                        ShowErrorMessage(SR.GetString(SR.PPG_ConfigNotFound_2Args, SelectedConfigItem.Name, SelectedPlatformItem.Name))
                    End If
                Else
                    'Use the DTE to find all the configs with a certain config name or platform name, then
                    '  look up the IVsCfg for those that were found
                    Dim DTEConfigs As EnvDTE.Configurations

                    If AllConfigurations Then
                        Debug.Assert(SelectedPlatformItem.SelectionType <> ConfigurationState.SelectionTypes.All)
                        DTEConfigs = DTEProject.ConfigurationManager.Platform(SelectedPlatformItem.Name)
                    Else
                        Debug.Assert(AllPlatforms)
                        Debug.Assert(SelectedConfigItem.SelectionType <> ConfigurationState.SelectionTypes.All)
                        DTEConfigs = DTEProject.ConfigurationManager.ConfigurationRow(SelectedConfigItem.Name)
                    End If
                    Debug.Assert(DTEConfigs IsNot Nothing AndAlso DTEConfigs.Count > 0)

                    Dim Cfg As IVsCfg = Nothing
                    ConfigObjects = New Object(DTEConfigs.Count - 1) {}
                    For i As Integer = 0 To DTEConfigs.Count - 1
                        Dim DTEConfig As EnvDTE.Configuration = DTEConfigs.Item(i + 1) '1-indexed
                        If VSErrorHandler.Succeeded(VsCfgProvider.GetCfgOfName(DTEConfig.ConfigurationName, DTEConfig.PlatformName, Cfg)) Then
                            ConfigObjects(i) = Cfg
                        Else
                            ShowErrorMessage(SR.GetString(SR.PPG_ConfigNotFound_2Args, SelectedConfigItem.Name, SelectedPlatformItem.Name))
                            ConfigObjects = Nothing
                            Exit For
                        End If
                    Next
                End If

                'Finally, call SetObjects with the selected configs

                If ConfigObjects Is Nothing OrElse ConfigObjects.Length = 0 Then
                    'There was an error collecting this info - unload the page
                    UnLoadPage()
                    Return
                End If

                CallPageSetObjects(ConfigObjects)
            End If
        End Sub


        ''' <summary>
        ''' Passes the given set of objects (configurations, etc.) to the property page via its IPropertyPage2.SetObjects method.
        '''   (For config-dependent pages, this is currently IVsCfg objects, for other pages a browse object,
        '''    but it could theoretically be just about anything with the project context necessary
        '''    for the page properties).
        ''' </summary>
        ''' <param name="Objects"></param>
        ''' <remarks></remarks>
        Private Sub CallPageSetObjects(ByVal Objects() As Object)
            Dim Count As UInteger = 0
            If Objects IsNot Nothing Then
                Debug.Assert(Objects.Length <= UInteger.MaxValue, "Whoa!  Muchos objects!")
                Debug.Assert(TypeOf Objects Is Object(), "Objects must be an array of Object, not an array of anything else!")
                Count = CUInt(Objects.Length)
            End If

            Debug.Assert(PropPage IsNot Nothing, "PropPage is Nothing")
            PropPage.SetObjects(Count, Objects)
        End Sub

#End Region


        ''' <summary>
        ''' Fired when the configuration combobox is dropped down.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ConfigurationComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles ConfigurationComboBox.DropDown
            'Set the drop-down width to handle all the text entries in it
            AppDesCommon.SetComboBoxDropdownWidth(ConfigurationComboBox)
        End Sub


        ''' <summary>
        ''' Fired when the configuration combobox is dropped down.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub PlatformComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles PlatformComboBox.DropDown
            'Set the drop-down width to handle all the text entries in it
            AppDesCommon.SetComboBoxDropdownWidth(PlatformComboBox)
        End Sub


#Region "Undo/Redo handling"

        ''' <summary>
        ''' Gets the current value of the property on the specified PropPageDesignerRootComponent instance.
        ''' Used by the undo/redo mechanism to save current property values so that they can be 
        '''   automatically changed later via undo or redo.
        ''' </summary>
        ''' <param name="PropertyName">The name of the property on the current page to get the value of.</param>
        ''' <returns>The value of the property on the specified component instance.</returns>
        Public Function GetProperty(ByVal PropertyName As String) As Object
            If PropertyName = "" Then
                Throw New ArgumentException
            End If

            Common.Switches.TracePDUndo("PropPageDesignerView.GetProperty(" & PropertyName & ")")

            Dim PropPageUndo As IVsProjectDesignerPage = TryCast(m_LoadedPage, IVsProjectDesignerPage)
            Debug.Assert(PropPageUndo IsNot Nothing)
            If PropPageUndo IsNot Nothing Then
                Dim Value As Object = Nothing

                'Only worry about multiple-value undo/redo for config-dependent pages...
                If IsConfigPage Then
                    If PropPageUndo.SupportsMultipleValueUndo(PropertyName) Then
                        Dim Objects As Object() = Nothing
                        Dim Values As Object() = Nothing

                        Common.Switches.TracePDUndo("  Multi-value undo/redo supported by property.")
                        Try
                            'This page supports multiple-value undo/redo for this property (i.e., restoring different
                            '  values for each config), so attempt to get those values.
                            Dim GetValuesSucceeded As Boolean = PropPageUndo.GetPropertyMultipleValues(PropertyName, Objects, Values)
                            If GetValuesSucceeded AndAlso Objects IsNot Nothing AndAlso Values IsNot Nothing Then
                                'Package them up into a serializable class from which we can unpack them later during undo.
                                Dim SelectedConfigurationName As String = IIf(GetSelectedConfigItem().SelectionType = ConfigurationState.SelectionTypes.All, "", GetSelectedConfigItem().Name)
                                Dim SelectedPlatformName As String = IIf(GetSelectedPlatformItem().SelectionType = ConfigurationState.SelectionTypes.All, "", GetSelectedPlatformItem().Name)
                                Value = New MultipleValuesStore(VsCfgProvider, Objects, Values, SelectedConfigurationName, SelectedPlatformName)
                            Else
                                'GetPropertyMultipleValues returned Nothing.  Try for a single value later.
                            End If
                        Catch ex As NotSupportedException
                            Debug.Fail("Prop page said it supported multiple value undo, but then failed with not supported.  Reverting to single-value undo/redo.")
                            'Ignore error and try single value instead
                        Catch ex As ArgumentException
                            'Most likely this indicates that Objects were not IVsCfg (this could be the case for non-config-dependent pages).  We shouldn't 
                            '  have tried to call the multi-value undo stuff in this case, but if it does happen, let's tolerate 
                            '  it by reverting to single-value undo behavior.  MultipleValues will have already asserted in this case, so 
                            '  we don't need to unless this assumption is wrong.
                            Debug.Assert(Objects IsNot Nothing AndAlso Objects.Length = 1 AndAlso Not TypeOf Objects(0) Is IVsCfg, _
                                "Unexpected exception in MultipleValues constructor.  Reverting to single-value undo/redo.")
                        End Try
                    Else
                        Common.Switches.TracePDUndo("  Multi-value undo/redo not supported.")
                    End If
                Else
                    Common.Switches.TracePDUndo("  Not a Config page, no multi-value undo.")
                    Debug.Assert(Not PropPageUndo.SupportsMultipleValueUndo(PropertyName), _
                        "A property on a config-independent page supports multiple-value undo/redo.  That means the page contains a config-dependent property.  And that doesn't seem right, does it?" _
                        & vbCrLf & "PropertyName = " & PropertyName)
                End If

                'Getting multiple-value undo wasn't supported or didn't succeed.  Try getting just a single value.
                If Value Is Nothing Then
                    Value = PropPageUndo.GetProperty(PropertyName)
                End If

                'If any of the values being serialized is an unmanaged enum, the serialization stream will
                '  contain a reference to the dll, which the deserializer may have trouble deserializing.  So
                '  instead simply convert these to their underyling types (usually integer) to avoid complications.
                If TypeOf Value Is MultipleValuesStore Then
                    Dim Store As MultipleValuesStore = DirectCast(Value, MultipleValuesStore)
                    For i As Integer = 0 To Store.Values.Length - 1
                        ConvertEnumToUnderlyingType(Store.Values(i))
                    Next
                Else
                    ConvertEnumToUnderlyingType(Value)
                End If

                'Done.
                Common.Switches.TracePDUndo("  Value=" & AppDesCommon.DebugToString(Value))
                Return Value
            Else
                Debug.Fail("PropertyPagePropertyDescriptor.GetValue() called with unexpected Component type.  Expected that this is also set up through the PropPageDesignerView (implementing IProjectDesignerPropertyPageUndoSite)")
                Throw New ArgumentException
            End If
        End Function


        ''' <summary>
        ''' Sets the value of a property on the active page to a different value.  Used during undo/redo.
        ''' </summary>
        ''' <param name="PropertyName">The property name of the property to be set</param>
        ''' <param name="Value">The value to set it to</param>
        ''' <remarks>
        ''' This method gets called by the serialization store dealing Undo/Redo operations.
        ''' </remarks>
        Public Sub SetProperty(ByVal PropertyName As String, ByVal Value As Object)
            If String.IsNullOrEmpty(PropertyName) Then
                Throw CreateArgumentException("PropertyName")
            End If

            Common.Switches.TracePDUndo("PropPageDesignerView.SetProperty(""" & PropertyName & """, " & AppDesCommon.DebugToString(Value) & ")")

            Dim PropPageUndo As IVsProjectDesignerPage = TryCast(m_LoadedPage, IVsProjectDesignerPage)
            Debug.Assert(PropPageUndo IsNot Nothing)
            If PropPageUndo IsNot Nothing Then
                'Is it a set of different values for multiple configurations?
                Dim MultiValues As MultipleValuesStore = TryCast(Value, MultipleValuesStore)
                If MultiValues IsNot Nothing Then
                    'Yes - multiple values need to be undone.
                    Common.Switches.TracePDUndo("  Multi-value undo/redo.")

                    Debug.Assert(IsConfigPage, "How did we get multiple properties values for undo/redo for a non-config-dependent page?")
                    If Not PropPageUndo.SupportsMultipleValueUndo(PropertyName) Then
                        Debug.Fail("Property page that supported multi-value undo when saving the value doesn't support them now that we want to do the undo/redo?")
                    Else
                        'We are about to do an Undo or Redo.  Since the undo/redo data carries the configs/platforms that
                        '  were in effect when the original change was made, and we are about to revert to those changes,
                        '  we first need to select those same configurations again.
                        ReselectConfigurationsForUndoRedo(MultiValues)

                        'Tell the property page to set the new (or old) values
                        Dim Objects As Object() = MultiValues.GetObjects(VsCfgProvider)
                        Try
                            PropPageUndo.SetPropertyMultipleValues(PropertyName, Objects, MultiValues.Values)
                        Catch ex As NotSupportedException
                            Debug.Fail("Property page threw not supported exception trying to undo/redo multi-value change")
                        End Try
                    End If
                Else
                    'Nope - single value.  Since this is config-independent, no need to change the configuration/platform dropdowns
                    '  in this case.
                    PropPageUndo.SetProperty(PropertyName, Value)
                End If
            End If
        End Sub


        ''' <summary>
        ''' If the object passed in is an enum, then convert it to its underlying type
        ''' </summary>
        ''' <param name="Value">[inout] The value to check and convert in place.</param>
        ''' <remarks></remarks>
        Private Sub ConvertEnumToUnderlyingType(ByRef Value As Object)
            If Value IsNot Nothing AndAlso Value.GetType().IsEnum Then
                Value = Convert.ChangeType(Value, System.Type.GetTypeCode(Value.GetType().UnderlyingSystemType))
            End If
        End Sub

        ''' <summary>
        ''' Returns the selection state of the configuration and platform comboboxes to the pre-undo/redo state.
        ''' <param name="SelectedConfigName">The selected configuration in the drop-down combobox.  Empty string indicates "All Configurations".</param>
        ''' <param name="SelectedPlatformName">The selected platform in the drop-down combobox.  Empty string indicates "All Platforms".</param>
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ReselectConfigurationsForUndoRedo(ByVal MultiValues As MultipleValuesStore)
            If Not IsConfigPage Then
                Exit Sub
            End If

            Debug.Assert(MultiValues IsNot Nothing)

            Dim SelectAllConfigs As Boolean = (MultiValues.SelectedConfigName = "")
            Dim SelectAllPlatforms As Boolean = (MultiValues.SelectedPlatformName = "")

            m_ConfigurationState.ChangeSelection( _
                MultiValues.SelectedConfigName, IIf(SelectAllConfigs, ConfigurationState.SelectionTypes.All, ConfigurationState.SelectionTypes.Normal), _
                MultiValues.SelectedPlatformName, IIf(SelectAllPlatforms, ConfigurationState.SelectionTypes.All, ConfigurationState.SelectionTypes.Normal), _
                PreferExactMatch:=False, FireNotifications:=True)
        End Sub

#End Region


#If 0 Then
        Protected Function GetPropertyPageBackColor(ByVal DefaultColor As Color) As Color
            Dim bkcolor As Color = DefaultColor
            Dim VsUIShell2 As IVsUIShell2 = VsUIShell2Service()
            If VsUIShell2 IsNot Nothing Then
                Dim agbrValue As UInteger
                VSErrorHandler.ThrowOnFailure(VsUIShell2.GetVSSysColorEx(VSSYSCOLOREX.VSCOLOR_TOOLBOX_GRADIENTLIGHT, agbrValue))
                Try
                    bkcolor = ColorRefToColor(agbrValue)
                Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                    'Eat this if the color isn't supported
                    Debug.Fail(ex.ToString())
                End Try
            End If
            Return bkcolor
        End Function

        Private Function ColorRefToColor(ByVal abgrValue As UInteger) As Color
            Return Color.FromArgb(CInt(abgrValue And &HFFUI), CInt((abgrValue And &HFF00UI) >> 8), CInt((abgrValue And &HFF0000UI) >> 16))
        End Function

        'Service for getting color info from the VS Shell
        Private m_UIShell2Service As IVsUIShell2

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <Browsable(False), _
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Public ReadOnly Property VsUIShell2Service() As IVsUIShell2
            Get
                If (m_UIShell2Service Is Nothing) Then
                    Dim VsUiShell As IVsUIShell
                    VsUiShell = TryCast(GetService(GetType(IVsUIShell)), IVsUIShell)
                    If VsUiShell Is Nothing AndAlso VBPackage.Instance IsNot Nothing Then
                        VsUiShell = TryCast(VBPackage.Instance.GetService(GetType(IVsUIShell)), IVsUIShell)
                    End If
                    If VsUiShell IsNot Nothing Then
                        m_UIShell2Service = TryCast(VsUiShell, IVsUIShell2)
                    End If
                End If
                Return m_UIShell2Service
            End Get
        End Property
#End If


        ''' <summary>
        ''' Commits any pending changes on the page
        ''' </summary>
        ''' <returns>return False if it failed</returns>
        ''' <remarks></remarks>
        Private Function CommitPendingChanges() As Boolean
            Common.Switches.TracePDPerfBegin("PropPageDesignerView.CommitPendingChanges")
            Try
                If m_LoadedPageSite IsNot Nothing Then
                    If Not m_LoadedPageSite.CommitPendingChanges() Then
                        Return False
                    End If
                End If

                ' It is time to do all pending validations...
                Dim vbPropertyPage As IVsProjectDesignerPage = TryCast(m_LoadedPage, IVsProjectDesignerPage)
                If vbPropertyPage IsNot Nothing Then
                    If Not vbPropertyPage.FinishPendingValidations() Then
                        Return False
                    End If
                End If

                Return True
            Finally
                Common.Switches.TracePDPerfEnd("PropPageDesignerView.CommitPendingChanges")
            End Try
        End Function

#Region "IVsWindowPaneCommit"
        ''' <summary>
        ''' This function is called on F5, build, etc., when any pending changes need to be performed on, say, a textbox
        '''   that the user has started typing into but hasn't committed by moving to another control.  We use this to force
        '''   an immediate apply.
        ''' </summary>
        ''' <param name="pfCommitFailed">[Out] Set to non-zero to indicate that the action should be canceled because the commit failed.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IVsWindowPaneCommit_CommitPendingEdit(ByRef pfCommitFailed As Integer) As Integer Implements IVsWindowPaneCommit.CommitPendingEdit
            pfCommitFailed = 0
            If Not CommitPendingChanges() Then
                pfCommitFailed = 1
            End If
            Return NativeMethods.S_OK
        End Function
#End Region


#Region "Message routing"


        ''' <summary>
        ''' Override this to enable tabbing from the property page designer (configuration panel) to
        '''   a native-hosted property page's controls.
        ''' </summary>
        ''' <param name="forward"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ProcessTabKey(ByVal forward As Boolean) As Boolean
            Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "PropPageDesignerView.ProcessTabKey")

            If m_IsNativeHostedPropertyPage Then
                'Try tabbing to another control in the property page designer view
                If (SelectNextControl(ActiveControl, forward, True, True, False)) Then
                    Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ...PropPageDesignerView.SelectNextControl handled it")
                    Return True
                End If

                If m_LoadedPage IsNot Nothing Then
                    'We hit the last tabbable control in the property page designer, set focus to the first (or last)
                    '  control in the property page itself.
                    Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "  ...Setting focus to " & IIf(forward, "first", "last") & " control on the page")
                    If Not FocusFirstOrLastPropertyPageControl(forward) Then
                        'No focusable controls in the property page (could be disabled), set focus to the
                        '  property page designer again
                        Return SelectNextControl(ActiveControl, forward, True, True, True)
                    End If
                    Return True
                End If
            Else
                If (SelectNextControl(ActiveControl, forward, True, True, True)) Then
                    Return True
                End If
            End If

            Return False
        End Function


        'For debug tracing
        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")> _
        Public Overrides Function PreProcessMessage(ByRef msg As System.Windows.Forms.Message) As Boolean
            Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "PropPageDesignerView.PreProcessMessage", msg)
            Return MyBase.PreProcessMessage(msg)
        End Function


        ''' <summary>
        ''' Override Control's ProcessDialogChar in order to allow mnemonics to work.
        ''' </summary>
        ''' <param name="charCode"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ProcessDialogChar(ByVal charCode As Char) As Boolean
            'Control's version of this function only calls ProcessMnemonic if the window
            '  is top-level, but the control is not top-level as far as WinForms is concerned.
            '  We'll ensure that it's always called for us.
            If charCode <> " "c AndAlso ProcessMnemonic(charCode) Then
                Return True
            End If

            'If we're hosting a control in native, ProcessMnemonic will not have seen
            '  the native control, so we need to give the property page a crack at it
            If m_IsNativeHostedPropertyPage AndAlso m_IsPageActivated Then
                If charCode <> " "c AndAlso m_LoadedPage IsNot Nothing Then

                    'CONSIDER: theoretically we should allow non-alt accelerators, but only
                    '  if it's not an input key for the currently-active control, and there's
                    '  no way to get that info without late binding, so we'll just only
                    '  accept ALT accelerators.
                    If (Control.ModifierKeys And Keys.Alt) <> 0 Then
                        Dim PropertyPageHwnd As IntPtr = GetPropertyPageTopHwnd()
                        Dim msg As OLE.Interop.MSG() = {New OLE.Interop.MSG}
                        With msg(0)
                            .hwnd = PropertyPageHwnd
                            .message = win.WM_SYSCHAR
                            .wParam = New IntPtr(AscW(charCode))
                        End With
                        If m_LoadedPage.TranslateAccelerator(msg) = NativeMethods.S_OK Then
                            Return True
                        End If
                    End If
                End If
            End If

            Return MyBase.ProcessDialogChar(charCode)
        End Function


        ''' <summary>
        ''' Sets the focus to the first (or last) control in the property page.
        ''' </summary>
        ''' <param name="First"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function FocusFirstOrLastPropertyPageControl(ByVal First As Boolean) As Boolean
            'Make sure to set the active control as well as doing SetFocus(), or else when
            '  devenv gets focus the focus will not go back to the correct control. 
            Me.ActiveControl = PropertyPagePanel
            Return Utils.FocusFirstOrLastTabItem(GetPropertyPageTopHwnd(), First)
        End Function

#End Region

        ''' <summary>
        ''' Retrieves the top-most HWND of the property page hosted inside the property page panel
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetPropertyPageTopHwnd() As IntPtr
            If PropertyPagePanel.Handle.Equals(IntPtr.Zero) Then
                Return IntPtr.Zero
            End If

            Return AppDesInterop.NativeMethods.GetWindow(PropertyPagePanel.Handle, AppDesInterop.win.GW_CHILD)
        End Function


        Public Sub OnActivated(ByVal activated As Boolean) Implements IVsEditWindowNotify.OnActivated
            Common.Switches.TracePDPerfBegin("PropPageDesignerView.OnActivated")
            ' It is time to do all pending validations...
            Dim vbPropertyPage As IVsProjectDesignerPage = TryCast(m_LoadedPage, IVsProjectDesignerPage)
            If vbPropertyPage IsNot Nothing Then
                vbPropertyPage.OnActivated(activated)

                If activated Then
                    ' When an existing page is reactivated (i.e. switching back from something else),
                    ' reinitialize the ui cue state (This is like reopening a dialog where the state
                    ' is reinitialized)
                    InitializeStateOfUICues()
                End If
            End If
            If activated AndAlso m_RootDesigner IsNot Nothing Then
                m_RootDesigner.RefreshMenuStatus()
            End If

            Common.Switches.TracePDPerfEnd("PropPageDesignerView.OnActivated")
        End Sub


        ''' <summary>
        ''' Clears all undo and redo entries for this page
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ClearUndoStackForPage()
            'Only need to do this if undo is enabled
            Dim UndoEngine As UndoEngine = TryCast(GetService(GetType(UndoEngine)), UndoEngine)
            Debug.Assert(UndoEngine IsNot Nothing, "Unable to get UndoEngine")
            If UndoEngine IsNot Nothing Then
                Debug.Assert(Not UndoEngine.UndoInProgress, "Trying to clear Undo stack while undo is in progress")
                If Not UndoEngine.UndoInProgress Then
                    Dim UndoManager As OLE.Interop.IOleUndoManager = TryCast(GetService(GetType(OLE.Interop.IOleUndoManager)), OLE.Interop.IOleUndoManager)
                    Debug.Assert(UndoManager IsNot Nothing, "Unable to get undo manager to clear the undo stack for the property page")
                    If UndoManager IsNot Nothing Then
                        Try
                            UndoManager.DiscardFrom(Nothing) 'Causes it to clear all entries in the undo and redo stacks
                        Catch ex As COMException
                            Debug.Fail("Unable to clear the undo stack, perhaps a unit was open or in progress, or it is disabled?  Exception:" & vbCrLf & ex.ToString)
                        End Try
                    End If
                End If
            End If
        End Sub


        ''' <summary>
        ''' Overridden WndProc
        ''' </summary>
        ''' <param name="m"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            Dim isSetFocusMessage As Boolean = False
            If m.Msg = AppDesInterop.win.WM_SETFOCUS Then
                isSetFocusMessage = True
            End If

            If isSetFocusMessage AndAlso m_fInitialized Then
                ' NOTE: we stop auto-scroll to the active control when the whole view gets Focus, so we won't change the scollbar posistion when the user switches between application and editors in the VS
                '  We should auto-scroll to the posistion when the page is just loaded.
                '  We should still scroll to the right view when focus is moving within the page.
                ' This is for vswhidbey: #517826
                PropertyPagePanel.StopAutoScrollToControl(True)
                Try
                    MyBase.WndProc(m)
                Finally
                    PropertyPagePanel.StopAutoScrollToControl(False)
                End Try
            Else
                MyBase.WndProc(m)
            End If

            If isSetFocusMessage Then
                'Since there's no notification of tools.option changes, on WM_SETFOCUS we check if the
                '  user has changed the simplified configs mode and update the page.
                If m_ConfigurationState IsNot Nothing AndAlso IsHandleCreated AndAlso Not m_NeedToCheckForModeChanges Then
                    m_NeedToCheckForModeChanges = True
                    BeginInvoke(New MethodInvoker(AddressOf CheckForModeChanges)) 'Make sure we're not in the middle of something when doing the check...
                End If
            End If
        End Sub


        ''' <summary>
        ''' Retrieves the title of the loaded property page
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetPageTitle() As String
            Dim Info As OleInterop.PROPPAGEINFO() = New OleInterop.PROPPAGEINFO(0) {}
            If m_LoadedPage IsNot Nothing Then
                m_LoadedPage.GetPageInfo(Info)
                Return Info(0).pszTitle
            End If

            Return ""
        End Function


        ''' <summary>
        ''' Sets the undo/redo level to a "clean" state
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SetUndoRedoCleanState()
            Dim CurrentUndoUnitsAvailable As Integer
            If TryGetUndoUnitsAvailable(CurrentUndoUnitsAvailable) Then
                'The page will be considered "clean" in the sense of undo/redo whenever
                '  the number of undo units available matches the number available right now.
                m_UndoUnitsOnStackAtCleanState = CurrentUndoUnitsAvailable
            Else
                Debug.Fail("SetUndoRedoCleanState(): unable to get undo units available")
                m_UndoUnitsOnStackAtCleanState = 0
            End If

            'For pages that don't support Undo/Redo, reset their flag
            m_LoadedPageSite.HasBeenSetDirty = False
        End Sub


        ''' <summary>
        ''' Determines whether the page is dirty in the sense of, "no current changes and the undo stack is at the same
        '''   place as when the user first loaded the page."
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ShouldShowDirtyIndicator() As Boolean
            If PropPage Is Nothing Then
                ' This can happen if an exception happened during property page initialization
                Return False
            End If
            If m_DesignerHost IsNot Nothing AndAlso m_DesignerHost.InTransaction Then
                ' We will be called when the transaction closes...
                '
                Return False
            ElseIf TypeOf PropPage Is IVsProjectDesignerPage Then
                Dim UndoUnitsAvailable As Integer
                If TryGetUndoUnitsAvailable(UndoUnitsAvailable) Then
                    Return UndoUnitsAvailable <> m_UndoUnitsOnStackAtCleanState
                Else
                    'This can happen if the property page didn't load properly, etc.
                    Common.Switches.TracePDUndo("*** ShouldShowDirtyIndicator: Returning FALSE because GetUndoUnitsAvailable failed (possibly couldn't get UndoEngine)")
                    Return False
                End If
            Else
                'Pages which do not support undo/redo simply show the asterisk if they are dirty
                If Me.PropPage.IsPageDirty() = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK Then
                    'Page is dirty
                    Return True
                End If

                ' ... or if the page has been marked dirty.  Some pages use immediate apply by sending us dirty+validate status,
                '  and those will get immediately set back to "clean".  This allows us to remember that they've actually been
                '  set as dirty by the user until the project designer is saved.
                If m_LoadedPageSite.HasBeenSetDirty Then
                    Return True
                End If

                Return False
            End If
        End Function

        Private Sub InitializeStateOfUICues()

            ' Passing UIS_INITIALIZE lets the OS decide what the initial state of the cue
            ' (whether they are hidden or not).  The cue flags are being bit shifted since
            ' WM_UPDATEUISTATE expects them in the hi order word of the wParam
            Dim updateUIStateWParam As Integer = NativeMethods.UIS_INITIALIZE Or
                                                 NativeMethods.UISF_HIDEFOCUS << 16 Or
                                                 NativeMethods.UISF_HIDEACCEL << 16

            NativeMethods.SendMessage(Me.Handle, NativeMethods.WM_UPDATEUISTATE, New IntPtr(updateUIStateWParam), IntPtr.Zero)

        End Sub

#Region "Dummy disabled menu commands to let the proppages get keystrokes such as Ctrl+X"

        ''' <summary>
        ''' No-op command handler 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub DisabledMenuCommandHandler(ByVal sender As Object, ByVal e As EventArgs)
        End Sub
#End Region
        ''' <summary>
        ''' Returns the number of undo units currently available
        ''' </summary>
        ''' <returns>True if the function successfully retrieves the # of undo units available.  False if it
        '''   fails (e.g., the Undo engine is not available, which can happen when the property page didn't 
        '''   load properly, etc.)</returns>
        ''' <remarks></remarks>
        Private Function TryGetUndoUnitsAvailable(ByRef UndoUnitsAvailable As Integer) As Boolean
            UndoUnitsAvailable = 0

            Dim UndoEngine As UndoEngine = TryCast(GetService(GetType(UndoEngine)), UndoEngine)
            If UndoEngine IsNot Nothing Then
                Debug.Assert(Not UndoEngine.UndoInProgress, "Trying to get undo units while undo in progress")
                If Not UndoEngine.UndoInProgress Then
                    Dim UndoManager As OLE.Interop.IOleUndoManager = TryCast(GetService(GetType(OLE.Interop.IOleUndoManager)), OLE.Interop.IOleUndoManager)
                    Debug.Assert(UndoManager IsNot Nothing, "Unable to get IOleUndoManager from UneoEngine")
                    If UndoManager IsNot Nothing Then
                        Dim EnumUnits As Microsoft.VisualStudio.OLE.Interop.IEnumOleUndoUnits = Nothing
                        UndoManager.EnumUndoable(EnumUnits)
                        If EnumUnits IsNot Nothing Then
                            Dim cUnits As Integer = 0
                            While True
                                Dim Units(0) As Microsoft.VisualStudio.OLE.Interop.IOleUndoUnit
                                Dim cReturned As UInteger
                                If VSErrorHandler.Failed(EnumUnits.Next(1, Units, cReturned)) OrElse cReturned = 0 Then
                                    UndoUnitsAvailable = cUnits
                                    Return True
                                Else
                                    Debug.Assert(cReturned = 1)
                                    cUnits += 1
                                End If
                            End While
                        End If
                    End If
                End If
            End If

            Return False
        End Function

#If DEBUG Then
        Private Sub ConfigurationPanel_SizeChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ConfigurationPanel.SizeChanged
            Common.Switches.TracePDFocus(TraceLevel.Info, "ConfigurationPanel_SizeChanged: " & ConfigurationPanel.Size.ToString())
        End Sub
#End If

        ''' <summary>
        ''' Call when the property page panel gets focus
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub PropertyPagePanel_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles PropertyPagePanel.GotFocus
            If m_IsNativeHostedPropertyPage Then
                'Since PropertyPagePanel has no child controls that WinForms knows about, we need to
                '  manually forward focus to the child.
                NativeMethods.SetFocus(NativeMethods.GetWindow(PropertyPagePanel.Handle, win.GW_CHILD))
            End If
        End Sub

        ''' <summary>
        '''   We need disable scroll to the active control when the user switches application. 
        '''   We can do this by overriding ScrollToControl function, which is used to calculate prefered viewport position when one control is activated.
        ''' </summary>
        Public Class ScrollablePanel
            Inherits Panel

            ' whether we should disable auto-scroll the viewport to show the active control
            Private m_stopAutoScrollToControl As Boolean

            ''' <summary>
            ''' change whether we should disable auto-scroll the viewport to show the active control
            ''' </summary>
            ''' <param name="needStop"></param>
            Public Sub StopAutoScrollToControl(ByVal needStop As Boolean)
                m_stopAutoScrollToControl = needStop
            End Sub

            ''' <summary>
            ''' We overrides ScrollToControl to stop auto-scroll the viewport to show an active control when the customer switches between applications
            '''  The function is called to calculate the viewport to show the control. When we enable it, we let the base class to handle this correctly.
            '''  When we need disable the action, we simply return the current position of the view port, so the panel will not scroll automatically.
            ''' </summary>
            ''' <param name="activeControl"></param>
            Protected Overrides Function ScrollToControl(ByVal activeControl As Control) As Point
                If m_stopAutoScrollToControl Then
                    Return DisplayRectangle.Location
                Else
                    Return MyBase.ScrollToControl(activeControl)
                End If
            End Function
        End Class
    End Class

End Namespace

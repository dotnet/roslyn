Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Drawing
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Forms
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.WCFReference.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class ReferencePropPage
        Inherits PropPageUserControlBase
        'Inherits UserControl
        Implements VSLangProj._dispReferencesEvents
        Implements VSLangProj._dispImportsEvents
        Implements ISelectionContainer
        Implements IVsWCFReferenceEvents

        Const REFCOLUMN_NAME As Integer = 0
        Const REFCOLUMN_TYPE As Integer = 1
        Const REFCOLUMN_VERSION As Integer = 2
        Const REFCOLUMN_COPYLOCAL As Integer = 3
        Const REFCOLUMN_PATH As Integer = 4
        Const REFCOLUMN_MAX As Integer = 4

        Friend WithEvents AddUserImportButton As System.Windows.Forms.Button
        Friend WithEvents UpdateUserImportButton As System.Windows.Forms.Button
        Friend WithEvents UserImportTextBox As System.Windows.Forms.TextBox
        'To contain list of VSLangProj.Reference objects
        Private m_RefreshListsAfterApply As Boolean

        Private m_ReferencesEventsCookie As NativeMethods.ConnectionPointCookie
        Private m_ImportsEventsCookie As NativeMethods.ConnectionPointCookie
        Private m_UpdatingReferences As Boolean
        Private m_UpdatingImportList As Boolean

        Private m_designerHost As IDesignerHost
        Private m_trackSelection As ITrackSelection
        Private m_holdSelectionChange As Integer

        Private m_delayUpdatingItems As Queue
        Private m_columnWidthUpdated As Boolean

        Private m_ignoreImportEvent As Boolean
        Friend WithEvents addRemoveButtonsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents referenceButtonsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ReferencePageSplitContainer As System.Windows.Forms.SplitContainer
        Friend WithEvents addUserImportTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Private m_needRefreshImportList As Boolean
        Private m_importListSelectedItem As String = Nothing
        Private m_hidingImportListSelectedItem As Boolean

        ' helper object to sort the reference list
        Private m_ReferenceSorter As ListViewComparer

        Private m_ReferenceGroupManager As IVsWCFReferenceManager

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Scale buttons
            addSplitButton.Size = DpiHelper.LogicalToDeviceUnits(addSplitButton.Size)
            RemoveReference.Size = DpiHelper.LogicalToDeviceUnits(RemoveReference.Size)
            UpdateReferences.Size = DpiHelper.LogicalToDeviceUnits(UpdateReferences.Size)

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
            MyBase.PageRequiresScaling = False

            'support sorting
            m_ReferenceSorter = New ListViewComparer()
            ReferenceList.ListViewItemSorter = m_ReferenceSorter
            m_ReferenceSorter.Sorting = SortOrder.Ascending
            ReferenceList.Sorting = SortOrder.Ascending
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub CleanupCOMReferences()

            Me.UnadviseReferencesEvents()
            Me.UnadviseWebReferencesEvents()
            Me.UnadviseServiceReferencesEvents()
            Me.UnadviseImportsEvents()

            MyBase.CleanupCOMReferences()
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        Friend WithEvents ReferenceList As System.Windows.Forms.ListView
        Friend WithEvents RemoveReference As System.Windows.Forms.Button
        Friend WithEvents UpdateReferences As System.Windows.Forms.Button
        Friend WithEvents ColHdr_RefName As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Path As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Type As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Version As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_CopyLocal As System.Windows.Forms.ColumnHeader
        Friend WithEvents ImportList As System.Windows.Forms.CheckedListBox
        Friend WithEvents ReferenceListLabel As System.Windows.Forms.Label
        Friend WithEvents ImportsListLabel As System.Windows.Forms.Label
        Friend WithEvents UnusedReferences As System.Windows.Forms.Button
        Friend WithEvents addSplitButton As Microsoft.VisualStudio.Editors.Common.SplitButton
        Friend WithEvents addContextMenuStrip As ContextMenuStrip
        Friend WithEvents referenceToolStripMenuItem As ToolStripMenuItem
        Friend WithEvents webReferenceToolStripMenuItem As ToolStripMenuItem
        Friend WithEvents serviceReferenceToolStripMenuItem As ToolStripMenuItem
        Friend WithEvents ReferencePageTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        ' Used in workaround as suggested in VSWhidbey 95812
        ' to fix bug VSWhidbey 63759


        '<System.Diagnostics.DebuggerStepThrough()> 
        Friend WithEvents ReferencePathsButton As System.Windows.Forms.Button
        Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ReferencePropPage))
            Me.ReferenceListLabel = New System.Windows.Forms.Label
            Me.ReferenceList = New System.Windows.Forms.ListView
            Me.ColHdr_RefName = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns"))
            Me.ColHdr_Type = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns1"))
            Me.ColHdr_Version = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns2"))
            Me.ColHdr_CopyLocal = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns3"))
            Me.ColHdr_Path = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns4"))
            Me.ReferencePageTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.referenceButtonsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.UnusedReferences = New System.Windows.Forms.Button
            Me.ReferencePathsButton = New System.Windows.Forms.Button
            Me.addRemoveButtonsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.RemoveReference = New System.Windows.Forms.Button
            Me.addSplitButton = New Microsoft.VisualStudio.Editors.Common.SplitButton
            Me.addContextMenuStrip = New System.Windows.Forms.ContextMenuStrip(Me.components)
            Me.referenceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.webReferenceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.serviceReferenceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.UpdateReferences = New System.Windows.Forms.Button
            Me.ImportsListLabel = New System.Windows.Forms.Label
            Me.AddUserImportButton = New System.Windows.Forms.Button
            Me.UserImportTextBox = New System.Windows.Forms.TextBox
            Me.ImportList = New System.Windows.Forms.CheckedListBox
            Me.addUserImportTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.UpdateUserImportButton = New System.Windows.Forms.Button
            Me.ReferencePageSplitContainer = New System.Windows.Forms.SplitContainer
            Me.ReferencePageTableLayoutPanel.SuspendLayout()
            Me.referenceButtonsTableLayoutPanel.SuspendLayout()
            Me.addRemoveButtonsTableLayoutPanel.SuspendLayout()
            Me.addContextMenuStrip.SuspendLayout()
            Me.addUserImportTableLayoutPanel.SuspendLayout()
            Me.ReferencePageSplitContainer.Panel1.SuspendLayout()
            Me.ReferencePageSplitContainer.Panel2.SuspendLayout()
            Me.ReferencePageSplitContainer.SuspendLayout()
            Me.SuspendLayout()
            '
            'ReferenceListLabel
            '
            resources.ApplyResources(Me.ReferenceListLabel, "ReferenceListLabel")
            Me.ReferenceListLabel.Margin = New System.Windows.Forms.Padding(0)
            Me.ReferenceListLabel.Name = "ReferenceListLabel"
            '
            'ReferenceList
            '
            Me.ReferenceList.AutoArrange = False
            Me.ReferenceList.BackColor = System.Drawing.SystemColors.Window
            Me.ReferenceList.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColHdr_RefName, Me.ColHdr_Type, Me.ColHdr_Version, Me.ColHdr_CopyLocal, Me.ColHdr_Path})
            Me.ReferencePageTableLayoutPanel.SetColumnSpan(Me.ReferenceList, 2)
            resources.ApplyResources(Me.ReferenceList, "ReferenceList")
            Me.ReferenceList.FullRowSelect = True
            Me.ReferenceList.HideSelection = False
            Me.ReferenceList.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.ReferenceList.Name = "ReferenceList"
            Me.ReferenceList.ShowItemToolTips = True
            '
            'ColHdr_RefName
            '
            resources.ApplyResources(Me.ColHdr_RefName, "ColHdr_RefName")
            '
            'ColHdr_Type
            '
            resources.ApplyResources(Me.ColHdr_Type, "ColHdr_Type")
            '
            'ColHdr_Version
            '
            resources.ApplyResources(Me.ColHdr_Version, "ColHdr_Version")
            '
            'ColHdr_CopyLocal
            '
            resources.ApplyResources(Me.ColHdr_CopyLocal, "ColHdr_CopyLocal")
            '
            'ColHdr_Path
            '
            resources.ApplyResources(Me.ColHdr_Path, "ColHdr_Path")
            '
            'ReferencePageTableLayoutPanel
            '
            resources.ApplyResources(Me.ReferencePageTableLayoutPanel, "ReferencePageTableLayoutPanel")
            Me.ReferencePageTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.ReferencePageTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.referenceButtonsTableLayoutPanel, 1, 0)
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.ReferenceListLabel, 0, 0)
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.ReferenceList, 0, 1)
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.addRemoveButtonsTableLayoutPanel, 0, 2)
            Me.ReferencePageTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.ReferencePageTableLayoutPanel.Name = "ReferencePageTableLayoutPanel"
            Me.ReferencePageTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.ReferencePageTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.ReferencePageTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'referenceButtonsTableLayoutPanel
            '
            resources.ApplyResources(Me.referenceButtonsTableLayoutPanel, "referenceButtonsTableLayoutPanel")
            Me.referenceButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.referenceButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.referenceButtonsTableLayoutPanel.Controls.Add(Me.UnusedReferences, 0, 0)
            Me.referenceButtonsTableLayoutPanel.Controls.Add(Me.ReferencePathsButton, 1, 0)
            Me.referenceButtonsTableLayoutPanel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 3)
            Me.referenceButtonsTableLayoutPanel.Name = "referenceButtonsTableLayoutPanel"
            Me.referenceButtonsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'UnusedReferences
            '
            'UnusedReferences has been broken for some time now and no one has reported its failure.
            'Hence we are no longer supporting it. Instead of removing the associated code we are 
            'making the button invisible.
            resources.ApplyResources(Me.UnusedReferences, "UnusedReferences")
            Me.UnusedReferences.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UnusedReferences.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.UnusedReferences.Name = "UnusedReferences"
            Me.UnusedReferences.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            Me.UnusedReferences.Visible = False
            '
            'ReferencePathsButton
            '
            resources.ApplyResources(Me.ReferencePathsButton, "ReferencePathsButton")
            Me.ReferencePathsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.ReferencePathsButton.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
            Me.ReferencePathsButton.Name = "ReferencePathsButton"
            Me.ReferencePathsButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'addRemoveButtonsTableLayoutPanel
            '
            resources.ApplyResources(Me.addRemoveButtonsTableLayoutPanel, "addRemoveButtonsTableLayoutPanel")
            Me.ReferencePageTableLayoutPanel.SetColumnSpan(Me.addRemoveButtonsTableLayoutPanel, 2)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.addRemoveButtonsTableLayoutPanel.Controls.Add(Me.RemoveReference, 1, 0)
            Me.addRemoveButtonsTableLayoutPanel.Controls.Add(Me.addSplitButton, 0, 0)
            Me.addRemoveButtonsTableLayoutPanel.Controls.Add(Me.UpdateReferences, 2, 0)
            Me.addRemoveButtonsTableLayoutPanel.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.addRemoveButtonsTableLayoutPanel.Name = "addRemoveButtonsTableLayoutPanel"
            Me.addRemoveButtonsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'RemoveReference
            '
            resources.ApplyResources(Me.RemoveReference, "RemoveReference")
            Me.RemoveReference.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.RemoveReference.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
            Me.RemoveReference.Name = "RemoveReference"
            Me.RemoveReference.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'addSplitButton
            '
            resources.ApplyResources(Me.addSplitButton, "addSplitButton")
            Me.addSplitButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.addSplitButton.ContextMenuStrip = Me.addContextMenuStrip
            Me.addSplitButton.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.addSplitButton.Name = "addSplitButton"
            Me.addSplitButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'addContextMenuStrip
            '
            resources.ApplyResources(Me.addContextMenuStrip, "addContextMenuStrip")
            Me.addContextMenuStrip.GripMargin = New System.Windows.Forms.Padding(2)
            Me.addContextMenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.referenceToolStripMenuItem, Me.serviceReferenceToolStripMenuItem, Me.webReferenceToolStripMenuItem})
            Me.addContextMenuStrip.Name = "addContextMenuStrip"
            '
            'referenceToolStripMenuItem
            '
            Me.referenceToolStripMenuItem.Name = "referenceToolStripMenuItem"
            resources.ApplyResources(Me.referenceToolStripMenuItem, "referenceToolStripMenuItem")
            '
            'webReferenceToolStripMenuItem
            '
            Me.webReferenceToolStripMenuItem.Name = "webReferenceToolStripMenuItem"
            resources.ApplyResources(Me.webReferenceToolStripMenuItem, "webReferenceToolStripMenuItem")
            '
            'serviceReferenceToolStripMenuItem
            '
            Me.serviceReferenceToolStripMenuItem.Name = "serviceReferenceToolStripMenuItem"
            resources.ApplyResources(Me.serviceReferenceToolStripMenuItem, "serviceReferenceToolStripMenuItem")
            '
            'UpdateReferences
            '
            resources.ApplyResources(Me.UpdateReferences, "UpdateReferences")
            Me.UpdateReferences.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UpdateReferences.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
            Me.UpdateReferences.Name = "UpdateReferences"
            Me.UpdateReferences.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'ImportsListLabel
            '
            resources.ApplyResources(Me.ImportsListLabel, "ImportsListLabel")
            Me.addUserImportTableLayoutPanel.SetColumnSpan(Me.ImportsListLabel, 4)
            Me.ImportsListLabel.Margin = New System.Windows.Forms.Padding(0, 3, 3, 0)
            Me.ImportsListLabel.Name = "ImportsListLabel"
            '
            'AddUserImportButton
            '
            resources.ApplyResources(Me.AddUserImportButton, "AddUserImportButton")
            Me.AddUserImportButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.AddUserImportButton.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
            Me.AddUserImportButton.Name = "AddUserImportButton"
            Me.AddUserImportButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'UserImportTextBox
            '
            resources.ApplyResources(Me.UserImportTextBox, "UserImportTextBox")
            Me.addUserImportTableLayoutPanel.SetColumnSpan(Me.UserImportTextBox, 2)
            Me.UserImportTextBox.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.UserImportTextBox.Name = "UserImportTextBox"
            '
            'ImportList
            '
            resources.ApplyResources(Me.ImportList, "ImportList")
            Me.addUserImportTableLayoutPanel.SetColumnSpan(Me.ImportList, 3)
            Me.ImportList.FormattingEnabled = True
            Me.ImportList.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.ImportList.Name = "ImportList"
            Me.ImportList.SelectionMode = SelectionMode.One
            '
            'addUserImportTableLayoutPanel
            '
            resources.ApplyResources(Me.addUserImportTableLayoutPanel, "addUserImportTableLayoutPanel")
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.AddUserImportButton, 2, 1)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.UpdateUserImportButton, 3, 2)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.ImportsListLabel, 0, 0)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.ImportList, 0, 2)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.UserImportTextBox, 0, 1)
            Me.addUserImportTableLayoutPanel.Name = "addUserImportTableLayoutPanel"
            Me.addUserImportTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.addUserImportTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.addUserImportTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            '
            'UpdateUserImportButton
            '
            resources.ApplyResources(Me.UpdateUserImportButton, "UpdateUserImportButton")
            Me.UpdateUserImportButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UpdateUserImportButton.Name = "UpdateUserImportButton"
            Me.UpdateUserImportButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'ReferencePageSplitContainer
            '
            Me.ReferencePageSplitContainer.BackColor = System.Drawing.SystemColors.Control
            Me.ReferencePageSplitContainer.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.ReferencePageSplitContainer, "ReferencePageSplitContainer")
            Me.ReferencePageSplitContainer.Name = "ReferencePageSplitContainer"
            '
            'ReferencePageSplitContainer.Panel1
            '
            Me.ReferencePageSplitContainer.Panel1.BackColor = System.Drawing.SystemColors.Control
            Me.ReferencePageSplitContainer.Panel1.Controls.Add(Me.ReferencePageTableLayoutPanel)
            '
            'ReferencePageSplitContainer.Panel2
            '
            Me.ReferencePageSplitContainer.Panel2.BackColor = System.Drawing.SystemColors.Control
            Me.ReferencePageSplitContainer.Panel2.Controls.Add(Me.addUserImportTableLayoutPanel)
            '
            'ReferencePropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.BackColor = System.Drawing.SystemColors.Control
            Me.Controls.Add(Me.ReferencePageSplitContainer)
            Me.MinimumSize = New System.Drawing.Size(538, 480)
            Me.Name = "ReferencePropPage"
            Me.ReferencePageSplitContainer.Panel1MinSize = 160
            Me.ReferencePageSplitContainer.Panel2MinSize = 160
            Me.ReferencePageTableLayoutPanel.ResumeLayout(False)
            Me.ReferencePageTableLayoutPanel.PerformLayout()
            Me.referenceButtonsTableLayoutPanel.ResumeLayout(False)
            Me.referenceButtonsTableLayoutPanel.PerformLayout()
            Me.addRemoveButtonsTableLayoutPanel.ResumeLayout(False)
            Me.addRemoveButtonsTableLayoutPanel.PerformLayout()
            Me.addContextMenuStrip.ResumeLayout(False)
            Me.addUserImportTableLayoutPanel.ResumeLayout(False)
            Me.addUserImportTableLayoutPanel.PerformLayout()
            Me.ReferencePageSplitContainer.Panel1.ResumeLayout(False)
            Me.ReferencePageSplitContainer.Panel1.PerformLayout()
            Me.ReferencePageSplitContainer.Panel2.ResumeLayout(False)
            Me.ReferencePageSplitContainer.Panel2.PerformLayout()
            Me.ReferencePageSplitContainer.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub

#End Region


        Protected Overrides Sub EnableAllControls(ByVal _enabled As Boolean)
            MyBase.EnableAllControls(_enabled)

            ReferenceList.Enabled = _enabled
            addSplitButton.Enabled = _enabled
            RemoveReference.Enabled = _enabled
            UpdateReferences.Enabled = _enabled
            UnusedReferences.Enabled = _enabled
            GetPropertyControlData("ImportList").EnableControls(_enabled)
        End Sub

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() { _
                        New PropertyControlData(1, "ImportList", Me.ImportList, AddressOf Me.ImportListSet, AddressOf Me.ImportListGet, ControlDataFlags.UserPersisted) _
                        }
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <Summary>
        ''' The designer host of this page
        ''' NOTE: we currently get the designer host from the propertyPageDesignerView, it is a workaround. The right solution should be the parent page pass in the right serviceProvider when it creates/initializes this page
        ''' </Summary>
        Private ReadOnly Property DesignerHost() As IDesignerHost
            Get
                If m_designerHost Is Nothing Then
                    Dim designerView As PropPageDesigner.PropPageDesignerView = FindPropPageDesignerView()
                    Debug.Assert(designerView IsNot Nothing, "why we can not find the designerView")
                    If designerView IsNot Nothing Then
                        m_designerHost = designerView.DesignerHost
                        Debug.Assert(m_designerHost IsNot Nothing, "why we can not find DesignerHost")
                    End If
                End If
                Return m_designerHost
            End Get
        End Property

        ''' <summary>
        ''' Property to return the selected-item of the ImportList which is smart about whether or
        ''' not we are hiding the selection currently to work around the by-design CheckedListBox
        ''' behavior of visually looking like it has focus when it really doesn't.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property ImportListSelectedItem() As String
            Get
                Debug.Assert(ImportList.SelectedItems.Count <= 1, "the ImportList is not set up to support multiple selection")

                If ImportList.SelectedItems.Count = 1 Then
                    Return DirectCast(ImportList.SelectedItem, String)
                ElseIf m_importListSelectedItem IsNot Nothing Then
                    Return m_importListSelectedItem
                End If

                Return String.Empty

            End Get
        End Property

        ''' <Summary>
        ''' ITrackSelection -- we are using this service to push objects to the propertyPage.
        '''  We should get this service from DesignerHost, but not other service provider. Each designer has its own ITrackSelection
        ''' </Summary>
        Private ReadOnly Property TrackSelection() As ITrackSelection
            Get
                If m_trackSelection Is Nothing Then
                    Dim host As IDesignerHost = DesignerHost
                    If host IsNot Nothing Then
                        m_trackSelection = CType(host.GetService(GetType(ITrackSelection)), ITrackSelection)
                        Debug.Assert(m_trackSelection IsNot Nothing, "Why we can not find ITrackSelection Service")
                    End If
                End If
                Return m_trackSelection
            End Get
        End Property


        Public Overrides Function ReadUserDefinedProperty(ByVal PropertyName As String, ByRef Value As Object) As Boolean
            If PropertyName = "ImportList" Then
                Value = GetCurrentImportsList()
                Return True
            End If
            Return False
        End Function

        Public Overrides Function WriteUserDefinedProperty(ByVal PropertyName As String, ByVal Value As Object) As Boolean
            If PropertyName = "ImportList" Then
                Debug.Assert(TypeOf Value Is String(), "Invalid value type")
                SaveImportedNamespaces(DirectCast(Value, String()))
                Return True
            End If
            Return False
        End Function

        Public Overrides Function GetUserDefinedPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            If PropertyName = "ImportList" Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String()))
            End If

            Debug.Fail("Unexpected user-defined property descriptor name")
            Return Nothing
        End Function

        ''' <summary>
        ''' Called when the control layout code wants to know the Preferred size of this page
        ''' </summary>
        ''' <param name="proposedSize"></param>
        ''' <remarks>We need implement this, because split panel doesn't support AutoSize well</remarks>
        Public Overrides Function GetPreferredSize(ByVal proposedSize As System.Drawing.Size) As System.Drawing.Size
            Dim preferredSize As Size = MyBase.GetPreferredSize(proposedSize)
            Dim referenceAreaPreferredSize As Size = System.Drawing.Size.Empty
            Dim importsAreaPreferredSize As Size = System.Drawing.Size.Empty

            If ReferencePageTableLayoutPanel IsNot Nothing Then
                referenceAreaPreferredSize = ReferencePageTableLayoutPanel.GetPreferredSize(New Size(proposedSize.Width, ReferencePageTableLayoutPanel.Height))
            End If
            If addUserImportTableLayoutPanel IsNot Nothing Then
                importsAreaPreferredSize = addUserImportTableLayoutPanel.GetPreferredSize(New Size(proposedSize.Width, importsAreaPreferredSize.Height))
            End If

            ' NOTE: 6 is 2 times of the margin we used. The exactly number is not important, because it actually does not make any difference on the page.
            Return New Size(Math.Max(preferredSize.Width, Math.Max(referenceAreaPreferredSize.Width, importsAreaPreferredSize.Width) + 6), _
                    Math.Max(preferredSize.Height, referenceAreaPreferredSize.Height + importsAreaPreferredSize.Height + 6))
        End Function

        Protected Overrides Sub WndProc(ByRef m As Message)
            Try
                Dim processedDelayRefreshMessage As Boolean = False
                Select Case m.Msg
                    Case Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_REFERENCES_REFRESH
                        ProcessDelayUpdateItems()
                        processedDelayRefreshMessage = True
                    Case Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_IMPORTCHANGED
                        SetDirty(Me.ImportList)
                        processedDelayRefreshMessage = True
                    Case Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_IMPORTS_REFRESH
                        Try
                            PopulateImportsList(True)
                        Finally
                            m_needRefreshImportList = False
                        End Try
                        processedDelayRefreshMessage = True
                    Case Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_SERVICEREFERENCES_REFRESH
                        RefreshServiceReferences()
                        processedDelayRefreshMessage = True
                End Select

                If processedDelayRefreshMessage Then
                    Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(Microsoft.Internal.Performance.CodeMarkerEvent.perfMSVSEditorsReferencePagePostponedUIRefreshDone)
                End If
            Catch ex As COMException
                ' The message pump in the background compiler could process our pending message, and when the compiler is running, we would get E_PENDING failure
                ' we want to post the message back to try it again.  To prevent spinning the main thread, we ask a background thread to post the message back after a short period of time
                If ex.ErrorCode = NativeMethods.E_PENDING Then
                    Dim delayMessage As New System.Threading.Timer(AddressOf DelayPostingMessage, m.Msg, 200, System.Threading.Timeout.Infinite)
                    Return
                End If
                Throw
            End Try

            MyBase.WndProc(m)
        End Sub

        ''' <summary>
        ''' We cannot process the UI refereshing message when compiler is running. However the compiler continuely pump messages.
        '''  It is a workaround to use background thread to wait for the compiler to finish.
        ''' Note: it rarely happens. (It happens we have a post message when a third party start the compiler and wait for something.)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DelayPostingMessage(ByVal messageId As Object)
            If Not IsDisposed Then
                Microsoft.VisualStudio.Editors.Interop.NativeMethods.PostMessage(Handle, CInt(messageId), 0, 0)
            End If
        End Sub


        ''' <summary>
        ''' Called when the page is activated or deactivated
        ''' </summary>
        ''' <param name="activated"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnPageActivated(ByVal activated As Boolean)
            MyBase.OnPageActivated(activated)
            If IsActivated Then
                PostRefreshImportListMessage()
            End If
        End Sub

        ''' <summary>
        ''' ;ReferenceToListViewItem 
        ''' Creates a four column listview item from the information of a project reference.
        ''' These columns are: Reference Name, Type (COM/.NET/UNKNOWN), Version, Copy Local (Yes/No), Path
        ''' </summary>
        ''' <param name="ref">Reference to take extract information from</param>
        ''' <param name="refObject">Internal Reference Object, which we push to the property grid</param>
        ''' <returns>ListViewItem object containing information from reference</returns>
        ''' <remarks>Helper for RefreshReferenceList() and UnusedReferencePropPage</remarks>
        Friend Shared Function ReferenceToListViewItem(ByVal ref As VSLangProj.Reference, ByVal refObject As Object) As ListViewItem

            Debug.Assert(ref IsNot Nothing)

            Dim lvi As ListViewItem
            Dim CopyLocalText As String

            If ref.Type = VSLangProj.prjReferenceType.prjReferenceTypeActiveX AndAlso ref.Description <> "" Then
                'For COM references with a nice description, use this
                '(like "Microsoft Office 10.0 Object Library" instead of "Office")
                lvi = New ListViewItem(ref.Description)
            Else
                lvi = New ListViewItem(ref.Name)
            End If

            lvi.Tag = refObject

            lvi.Checked = ref.CopyLocal
            CopyLocalText = ref.CopyLocal.ToString(CultureInfo.CurrentUICulture)

            If ref.Type = VSLangProj.prjReferenceType.prjReferenceTypeActiveX Then
                lvi.SubItems.Add("COM")
            ElseIf ref.Type = VSLangProj.prjReferenceType.prjReferenceTypeAssembly Then
                lvi.SubItems.Add(".NET")
            Else
                lvi.SubItems.Add("UNKNOWN") 'Type
            End If
            lvi.SubItems.Add(ref.Version.ToString()) 'Version
            lvi.SubItems.Add(CopyLocalText) 'CopyLocal column

            ' We should put an error message there if we can not resolve the reference...
            Dim path As String = ref.Path
            If String.IsNullOrEmpty(path) Then
                path = SR.GetString(SR.PropPage_ReferenceNotFound)
            End If

            lvi.SubItems.Add(path)

            Return lvi

        End Function

        ''' <summary>
        ''' WebReferenceToListViewItem 
        ''' Creates a four column listview item from the information of a web reference.
        ''' These columns are: Reference Name, Type (COM/.NET/UNKNOWN), Version, Copy Local (Yes/No), Path
        ''' </summary>
        ''' <param name="webref">WebReference project item</param>
        ''' <param name="refObject">Internal Reference Object</param>
        ''' <returns>ListViewItem object containing information from reference</returns>
        ''' <remarks>Helper for RefreshReferenceList()</remarks>
        Private Function WebReferenceToListViewItem(ByVal webRef As EnvDTE.ProjectItem, ByVal refObject As Object) As ListViewItem
            Debug.Assert(webRef IsNot Nothing)

            Dim lvi As ListViewItem

            lvi = New ListViewItem(webRef.Name)
            lvi.Tag = refObject

            lvi.SubItems.Add("WEB") 'Type
            lvi.SubItems.Add("") ' Version
            lvi.SubItems.Add("") ' Copy Local
            lvi.SubItems.Add(CStr(webRef.Properties.Item("WebReference").Value)) 'Path

            Return lvi
        End Function

        ''' <summary>
        ''' ServiceReferenceToListViewItem 
        ''' Creates a four column listview item from the information of a web reference.
        ''' These columns are: Reference Name, Type (COM/.NET/UNKNOWN), Version, Copy Local (Yes/No), Path
        ''' </summary>
        ''' <param name="serviceReference">service reference component</param>
        ''' <returns>ListViewItem object containing information from reference</returns>
        ''' <remarks>Helper for RefreshReferenceList()</remarks>
        Private Function ServiceReferenceToListViewItem(ByVal serviceReference As ServiceReferenceComponent) As ListViewItem
            Debug.Assert(serviceReference IsNot Nothing)

            Dim lvi As ListViewItem

            lvi = New ListViewItem(serviceReference.[Namespace])
            lvi.Tag = serviceReference

            lvi.SubItems.Add("SERVICE") 'Type
            lvi.SubItems.Add("") ' Version
            lvi.SubItems.Add("") ' Copy Local

            Dim referencePath As String
            Try
                referencePath = serviceReference.ServiceReferenceURL
            Catch ex As Exception
                ' show the error message, if the reference is broken
                referencePath = ex.Message
            End Try

            lvi.SubItems.Add(referencePath) 'Path

            Return lvi
        End Function

        ''' <summary>
        ''' Refreshes the reference listviews (both regular and web references), based on the list of references ReferenceListData.
        ''' </summary>
        ''' <param name="ReferenceListData">reference object lists</param>
        ''' <remarks></remarks>
        Private Sub RefreshReferenceList(ByVal ReferenceListData As ArrayList)

            ReferenceList.BeginUpdate()
            Try
                ReferenceList.View = View.Details
                ReferenceList.Items.Clear()

                'For Each ref As VSLangProj.Reference In theVSProject.References
                For refIndex As Integer = 0 To ReferenceListData.Count - 1
                    Dim refObject As Object = ReferenceListData(refIndex)
                    If TypeOf refObject Is ReferenceComponent Then
                        Debug.Assert(Not IsImplicitlyAddedReference(CType(refObject, ReferenceComponent).CodeReference), "Implicitly added references should have been filtered out and never displayed in our list")
                        ReferenceList.Items.Add(ReferenceToListViewItem(CType(refObject, ReferenceComponent).CodeReference, refObject))
                    ElseIf TypeOf refObject Is WebReferenceComponent Then
                        ReferenceList.Items.Add(WebReferenceToListViewItem(CType(refObject, WebReferenceComponent).WebReference, refObject))
                    ElseIf TypeOf refObject Is ServiceReferenceComponent Then
                        ReferenceList.Items.Add(ServiceReferenceToListViewItem(CType(refObject, ServiceReferenceComponent)))
                    End If
                Next

                ReferenceList.Sort()

            Finally
                ReferenceList.EndUpdate()
            End Try

            If Not m_columnWidthUpdated Then
                SetReferenceListColumnWidths(Me, Me.ReferenceList, 0)
                m_columnWidthUpdated = True
            End If

            ReferenceList.Refresh()

            EnableReferenceGroup()

        End Sub

        ''' <summary>
        ''' Populates the Reference object of all references (regular and web) currently in the project, and also 
        '''   calls RefreshReferenceList() to update the listviews with those objects
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PopulateReferenceList()

            Dim theVSProject As VSLangProj.VSProject
            Dim ReferenceCount As Integer
            Dim ref As VSLangProj.Reference

            theVSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            ReferenceCount = theVSProject.References.Count

            HoldSelectionChange(True)
            Try
                Dim ReferenceListData As New System.Collections.ArrayList(ReferenceCount)

                For refIndex As Integer = 0 To ReferenceCount - 1
                    ref = theVSProject.References.Item(refIndex + 1) '1-based

                    'Don't worry about implicitly-added references (these can't be removed, and don't
                    '  show up in the solution explorer, so we don't want to show them in the property
                    '  pages, either - for VB, this is currently mscorlib and ms.vb.dll)
                    If Not IsImplicitlyAddedReference(ref) Then
                        ReferenceListData.Add(New ReferenceComponent(ref))
                    End If
                Next refIndex

                If theVSProject.WebReferencesFolder IsNot Nothing Then
                    For Each webRef As EnvDTE.ProjectItem In theVSProject.WebReferencesFolder.ProjectItems
                        ' we need check whether the project item is a web reference.
                        ' user could add random items under this folder
                        If IsWebReferenceItem(webRef) Then
                            ReferenceListData.Add(New WebReferenceComponent(Me, webRef))
                        End If
                    Next
                End If

                If m_ReferenceGroupManager Is Nothing Then
                    Dim referenceManagerFactory As IVsWCFReferenceManagerFactory = CType(ServiceProvider.GetService(GetType(SVsWCFReferenceManagerFactory)), IVsWCFReferenceManagerFactory)
                    If referenceManagerFactory IsNot Nothing Then
                        Dim vsHierarchy As IVsHierarchy = ShellUtil.VsHierarchyFromDTEProject(ServiceProvider, DTEProject)
                        If vsHierarchy IsNot Nothing AndAlso Utils.IsServiceReferenceValidInProject(vsHierarchy) AndAlso referenceManagerFactory.IsReferenceManagerSupported(vsHierarchy) <> 0 Then
                            m_ReferenceGroupManager = referenceManagerFactory.GetReferenceManager(vsHierarchy)
                        End If
                    End If
                End If

                If m_ReferenceGroupManager IsNot Nothing Then
                    Dim collection As IVsWCFReferenceGroupCollection = m_ReferenceGroupManager.GetReferenceGroupCollection()
                    For i As Integer = 0 To collection.Count() - 1
                        Dim referenceGroup As IVsWCFReferenceGroup = collection.Item(i)
                        ReferenceListData.Add(New ServiceReferenceComponent(collection, referenceGroup))
                    Next
                End If

                RefreshReferenceList(ReferenceListData)

                m_delayUpdatingItems = Nothing
            Finally
                HoldSelectionChange(False)
            End Try

            PushSelection()
        End Sub

        ''' <summary>
        ''' check whether a project item is really a web reference
        ''' </summary>
        ''' <param name="webRef"></param>
        ''' <return></return>
        ''' <remarks></remarks>
        Private Function IsWebReferenceItem(ByVal webRef As EnvDTE.ProjectItem) As Boolean
            Dim webRefProperty As EnvDTE.Property = Nothing
            Dim properties As EnvDTE.Properties = webRef.Properties
            If properties IsNot Nothing Then
                Try
                    webRefProperty = properties.Item("WebReferenceInterface")
                Catch ex As ArgumentException
                    ' Ignore those items which is actually not web reference (but random items added by user into the directory.)
                End Try
            End If
            Return (webRefProperty IsNot Nothing)
        End Function

        Function GetReferencedNamespaceList() As IList(Of String)
            Dim threadedWaitDialogFactory = DirectCast(ServiceProvider.GetService(GetType(SVsThreadedWaitDialogFactory)), IVsThreadedWaitDialogFactory)
            Dim threadedWaitDialog2 As IVsThreadedWaitDialog2 = Nothing
            ErrorHandler.ThrowOnFailure(threadedWaitDialogFactory.CreateInstance(threadedWaitDialog2))

            Dim threadedWaitDialog3 = DirectCast(threadedWaitDialog2, IVsThreadedWaitDialog3)
            Dim cancellationTokenSource As New CancellationTokenSource
            Dim cancellationCallback As New CancellationCallback(cancellationTokenSource)
            threadedWaitDialog3.StartWaitDialogWithCallback(
                SR.GetString(SR.PropPage_ImportedNamespacesTitle),
                SR.GetString(SR.PropPage_ComputingReferencedNamespacesMessage),
                szProgressText:=Nothing,
                varStatusBmpAnim:=Nothing,
                szStatusBarText:=Nothing,
                fIsCancelable:=True,
                iDelayToShowDialog:=1,
                fShowProgress:=True,
                iTotalSteps:=0,
                iCurrentStep:=0,
                pCallback:=cancellationCallback)

            Try
                Dim componentModel = DirectCast(ServiceProvider.GetService(GetType(SComponentModel)), IComponentModel)
                Dim visualStudioWorkspace = componentModel.GetService(Of VisualStudioWorkspace)
                Dim solution = visualStudioWorkspace.CurrentSolution

                For Each projectId In solution.ProjectIds
                    ' We need to find the project that matches by IVsHierarchy
                    If visualStudioWorkspace.GetHierarchy(projectId) Is ProjectHierarchy Then
                        Dim compilationTask = solution.GetProject(projectId).GetCompilationAsync(cancellationTokenSource.Token)
                        compilationTask.Wait(cancellationTokenSource.Token)
                        Dim compilation = compilationTask.Result

                        Dim namespaceNames As New List(Of String)
                        Dim namespacesToProcess As New Stack(Of INamespaceSymbol)
                        namespacesToProcess.Push(compilation.GlobalNamespace)

                        Do While namespacesToProcess.Count > 0
                            cancellationTokenSource.Token.ThrowIfCancellationRequested()

                            Dim namespaceToProcess = namespacesToProcess.Pop()

                            For Each childNamespace In namespaceToProcess.GetNamespaceMembers()
                                If NamespaceIsReferenceableFromCompilation(childNamespace, compilation) Then
                                    namespaceNames.Add(childNamespace.ToDisplayString())
                                End If

                                namespacesToProcess.Push(childNamespace)
                            Next
                        Loop

                        namespaceNames.Sort(CaseInsensitiveComparison.Comparer)
                        Return namespaceNames
                    End If
                Next

                ' Return empty list if an error occurred
                Return New String() {}
            Catch ex As OperationCanceledException
                ' Return empty list if we canceled
                Return New String() {}
            Finally
                Dim canceled As Integer = 0
                threadedWaitDialog3.EndWaitDialog(canceled)
            End Try
        End Function

        Private Class CancellationCallback
            Implements IVsThreadedWaitDialogCallback

            Private ReadOnly cancellationTokenSource As CancellationTokenSource

            Public Sub New(cancellationTokenSource As CancellationTokenSource)
                Me.cancellationTokenSource = cancellationTokenSource
            End Sub

            Public Sub OnCanceled() Implements IVsThreadedWaitDialogCallback.OnCanceled
                cancellationTokenSource.Cancel()
            End Sub
        End Class

        Private Function NamespaceIsReferenceableFromCompilation([namespace] As INamespaceSymbol, compilation As Compilation) As Boolean
            For Each type In [namespace].GetTypeMembers()
                If type.CanBeReferencedByName Then
                    If type.DeclaredAccessibility = CodeAnalysis.Accessibility.Public Then
                        Return True
                    End If

                    If type.ContainingAssembly.Equals(compilation.Assembly) OrElse type.ContainingAssembly.GivesAccessTo(compilation.Assembly) Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

        Private Sub PopulateImportsList(ByVal InitSelections As Boolean, Optional ByVal RemoveInvalidEntries As Boolean = False)
            Dim Namespaces As IList(Of String)
            Dim UserImports As String()

            If ServiceProvider Is Nothing Then
                'We may be tearing down...
                Return
            End If

            ' get namespace list earlier to prevent reentrance in this function to cause duplicated items in the list
            Namespaces = GetReferencedNamespaceList()
            UserImports = GetCurrentImportsList()

            ' Gotta make a copy of the currently selected items so I can re-select 'em after
            ' I have repopulated the list...
            Dim currentlySelectedItems As New System.Collections.Specialized.StringCollection
            For Each SelectedItem As String In ImportList.SelectedItems
                currentlySelectedItems.Add(SelectedItem)
            Next
            Dim TopIndex As Integer = ImportList.TopIndex

            'CurrentList is a dictionary whose keys are all the items which are
            '  currently in the imports listbox or are in the referenced namespaces 
            '  of the project or are imports added by the user.
            'The value of the entry is True if it is a reference namespace or user
            '  import.
            Dim CurrentListMap As New Dictionary(Of String, Boolean)

            'Initialize CurrentListMap to include keys from everything currently
            '  in the listbox.  We'll next mark as true only those that the compiler
            '  and project actually know about.
            For Each cItem As String In ImportList.Items
                CurrentListMap.Add(cItem, False)
            Next

            'Create a combined list of referenced namespaces and user-defined imports
            Dim NamespacesAndUserImports As New List(Of String)
            NamespacesAndUserImports.AddRange(Namespaces)
            NamespacesAndUserImports.AddRange(UserImports)

            'For each item of NamespacesAndUserImports, make sure the item is in the
            '   imports listbox, and also set its entry in CurrentListMap to True so
            '   we know it's a current namespace or user import
            For Each name As String In NamespacesAndUserImports
                If name.Length > 0 Then
                    If Not CurrentListMap.ContainsKey(name) Then
                        'Not already in the listbox - add it
                        ImportList.Items.Add(name)
                        CurrentListMap.Add(name, True)
                    Else
                        CurrentListMap.Item(name) = True
                    End If
                End If
            Next name

            If RemoveInvalidEntries Then
                For Each item As KeyValuePair(Of String, Boolean) In CurrentListMap
                    If item.Value = False Then
                        'The item is not in the referenced namespaces and it's not in the
                        '  user-defined imports list (i.e., the namespace no longer exists, or
                        '  it's a user-import that the user has previously unchecked)
                        ImportList.Items.Remove(item.Key)
                    End If
                Next
            End If

            If InitSelections Then
                CheckCurrentImports()
            End If

            For Each item As String In currentlySelectedItems
                Dim itemIndex As Integer = ImportList.Items.IndexOf(item)
                If itemIndex <> -1 Then
                    ImportList.SetSelected(itemIndex, True)
                End If
            Next

            If TopIndex < ImportList.Items.Count Then
                ImportList.TopIndex = TopIndex
            End If

            EnableImportGroup()
        End Sub

        Private Sub AddNamespaceToImportList(ByVal _namespace As String)
            If ImportList.Items.IndexOf(_namespace) = -1 Then
                ImportList.Items.Add(_namespace)
            End If
        End Sub

        Private Sub SelectNamespaceInImportList(ByVal _namespace As String, ByVal MoveToTop As Boolean)
            Dim index As Integer
            index = ImportList.Items.IndexOf(_namespace)
            If index = -1 AndAlso Not MoveToTop Then
                'We skip this step if MoveToTop is true so we avoid adding then moving 
                'This should only be able to occur if a namespace
                '  is not in the references
                AddNamespaceToImportList(_namespace)
                index = ImportList.Items.IndexOf(_namespace)
            End If
            Try
                m_UpdatingImportList = True
                If MoveToTop Then
                    If index <> -1 Then
                        ImportList.Items.RemoveAt(index)
                    End If
                    ImportList.Items.Insert(0, _namespace)
                    ImportList.SetItemChecked(0, True)
                Else
                    ImportList.SetItemChecked(index, True)
                End If
            Finally
                m_UpdatingImportList = False
            End Try
        End Sub

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PreInitPage()
            MyBase.PreInitPage()

            Me.PopulateReferenceList()
            Me.PopulateImportsList(False)

            Me.AdviseReferencesEvents(CType(DTEProject.Object, VSLangProj.VSProject))
            Me.AdviseWebReferencesEvents()
            Me.AdviseServiceReferencesEvents()
            Me.AdviseImportsEvents(CType(DTEProject.Object, VSLangProj.VSProject))
        End Sub

        Private Function GetCurrentImportsList(ByVal _Imports As VSLangProj.Imports) As String()
            Dim UserImports As String()
            Dim ImportsCount As Integer

            ImportsCount = _Imports.Count
            If ImportsCount <= 0 Then
                UserImports = New String() {}
            Else
                UserImports = New String(ImportsCount - 1) {}

                For index As Integer = 0 To UBound(UserImports)
                    UserImports(index) = _Imports.Item(index + 1)
                Next
            End If
            Return UserImports
        End Function

        Private Function GetCurrentImportsList() As String()
            Dim theVSProject As VSLangProj.VSProject
            Dim _Imports As VSLangProj.Imports

            theVSProject = CType(DTEProject.Object, VSLangProj.VSProject)

            _Imports = theVSProject.Imports
            Return GetCurrentImportsList(_Imports)
        End Function

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
            EnableReferenceGroup()
            EnableImportGroup()

            ' make the import-panel act as if it lost focus so that the selected-row color
            '   of the Imports CheckedListBox does not look like it is focused
            '
            ImportPanel_Leave(Me, System.EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Take a snapshot of the user defined imports
        ''' </summary>
        ''' <returns>A dictionary with import name/is namespace pairs</returns>
        ''' <remarks></remarks>
        Private Function GetUserDefinedImportsSnapshot() As IDictionary(Of String, Boolean)
            ' First, we get a collection of referenced namespaces that is fast to 
            ' search...
            Dim ReferencedNamespaces As New Hashtable
            For Each ReferencedNamespace As String In GetReferencedNamespaceList()
                If ReferencedNamespace <> "" Then
                    ReferencedNamespaces.Add(ReferencedNamespace, Nothing)
                End If
            Next
            ' We save all currently imported namespaces
            ' Each import is stored in the hashtable with the
            ' value set to "True" if it is a namespace known to the compiler
            Dim UserDefinedImports As New Dictionary(Of String, Boolean)
            For Each UserImport As String In GetCurrentImportsList()
                UserDefinedImports.Add(UserImport, ReferencedNamespaces.Contains(UserImport))
            Next
            Return UserDefinedImports
        End Function

        ''' <summary>
        ''' Remove any user imported namespaces that were known to the compilerat the time the ImportsSnapshot
        ''' was taken
        ''' </summary>
        ''' <param name="ImportsSnapshot">
        ''' A snapshot of the project imports taken sometime before... 
        ''' </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function TrimUserImports(ByVal ImportsSnapshot As IDictionary(Of String, Boolean)) As String()
            ' Let's give the compiler time to update the namespace list - it looks like we may
            ' have a race-condition here, but I can't find out why.... and o
            System.Threading.Thread.Sleep(10)

            ' First, we get a collection of referenced namespaces that is fast to 
            ' search...
            Dim ReferencedNamespaces As New Hashtable
            For Each ReferencedNamespace As String In GetReferencedNamespaceList()
                If ReferencedNamespace <> "" Then
                    ReferencedNamespaces.Add(ReferencedNamespace, Nothing)
                End If
            Next

            Dim ResultList As New List(Of String)
            Dim snapshot As IEnumerable(Of KeyValuePair(Of String, Boolean)) = ImportsSnapshot
            For Each PreviousImportEntry As KeyValuePair(Of String, Boolean) In snapshot
                If PreviousImportEntry.Value Then
                    ' This was a namespace known to the compiler before whatever references were removed...
                    ' Only add it to the result if it is still known!
                    If ReferencedNamespaces.Contains(PreviousImportEntry.Key) Then
                        ResultList.Add(PreviousImportEntry.Key)
                    End If
                Else
                    ResultList.Add(PreviousImportEntry.Key)
                End If
            Next
            Return ResultList.ToArray()
        End Function

        Private Sub RemoveReference_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RemoveReference.Click
            RemoveSelectedReference()
        End Sub

        Private Sub RemoveSelectedReference()
            Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            Dim ItemIndices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices
            Dim ItemIndex As Integer
            Dim ref As ReferenceComponent
            Dim refComponent As IReferenceComponent
            Dim ReferenceRemoved As Boolean = False 'True if one or more references was actually removed

            If ItemIndices.Count = 0 Then
                Return
            End If

            Using New WaitCursor
                Dim ImportsSnapshot As IDictionary(Of String, Boolean) = Nothing

                Using New ProjectBatchEdit(ProjectHierarchy)
                    Try
                        Dim errorString As String = Nothing
                        Dim refName As String = String.Empty

                        m_UpdatingReferences = True
                        ReferenceList.BeginUpdate()

                        For i As Integer = ItemIndices.Count - 1 To 0 Step -1
                            Dim err As String = Nothing
                            ItemIndex = ItemIndices(i)

                            If ImportsSnapshot Is Nothing Then
                                ' Since we are going to remove a reference, and we haven't taken a snapshot of
                                ' the user imports before, we better do it now!
                                ImportsSnapshot = GetUserDefinedImportsSnapshot()
                            End If
                            'Remove from project references

                            EnterProjectCheckoutSection()
                            Try
                                refComponent = TryCast(ReferenceList.Items(ItemIndex).Tag, IReferenceComponent)
                                If refComponent IsNot Nothing Then
                                    ref = TryCast(refComponent, ReferenceComponent)
                                    If ref IsNot Nothing Then
                                        If IsImplicitlyAddedReference(ref.CodeReference) Then
                                            Debug.Fail("Implicitly added references should have been filtered out and never displayed in our list")
                                            Continue For
                                        End If
                                    End If

                                    refName = refComponent.GetName()
                                    refComponent.Remove()
                                    ReferenceRemoved = True
                                Else
                                    Debug.Fail("Unknown reference item")
                                End If

                                'Remove from local storage
                                ReferenceList.Items.RemoveAt(ItemIndex)

                            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                                If ProjectReloadedDuringCheckout Then
                                    ' If the Project could be reloaded, we should return ASAP, because the designer has been disposed
                                    Return
                                End If

                                If Common.IsCheckoutCanceledException(ex) Then
                                    'User already saw a message, no need to show an error message.  Also, don't
                                    '  want to continue trying to remove references.
                                    Exit For
                                Else
                                    ' some reference can not be removed (like mscorlib)
                                    err = SR.GetString(SR.PPG_Reference_CanNotRemoveReference, refName, ex.Message)
                                End If
                            Finally
                                LeaveProjectCheckoutSection()
                            End Try

                            If err IsNot Nothing Then
                                If errorString Is Nothing Then
                                    errorString = err
                                Else
                                    errorString = errorString + err
                                End If
                            End If
                        Next

                        If errorString IsNot Nothing Then
                            ShowErrorMessage(errorString)
                        End If

                    Finally
                        ' If the Project is reloaded, don't do anything as the page is disposed. VSWhidbey: 595444
                        If Not ProjectReloadedDuringCheckout Then
                            ReferenceList.EndUpdate()

                            ' Update buttons...
                            EnableReferenceGroup()
                            EnableImportGroup()
                            m_UpdatingReferences = False
                        End If
                    End Try
                End Using

                If ReferenceRemoved Then
                    ' Now, we better remove any user imports that is no longer 
                    ' known to the compiler...
                    If ImportsSnapshot IsNot Nothing Then
                        SaveImportedNamespaces(TrimUserImports(ImportsSnapshot))
                    End If

                    'RemoveInvalidEntries=True here because so that we can remove imports
                    '  that correspond to the removed references, instead of just unchecking
                    '  them.  This will also clean up any other invalid unchecked imports in 
                    '  the list, which might be a minor surprise to the user, but shouldn't be
                    '  too bad, and is the safest fix at this point in the schedule.
                    PopulateImportsList(InitSelections:=True, RemoveInvalidEntries:=True)
                    SetDirty(Me.ImportList)
                End If
            End Using

        End Sub

        Private Sub addContextMenuStrip_Opening(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles addContextMenuStrip.Opening
            Dim vsHierarchy As IVsHierarchy = ShellUtil.VsHierarchyFromDTEProject(ServiceProvider, DTEProject)
            If vsHierarchy IsNot Nothing Then
                webReferenceToolStripMenuItem.Visible = Utils.IsWebReferenceSupportedByDefaultInProject(vsHierarchy)
                serviceReferenceToolStripMenuItem.Visible = Utils.IsServiceReferenceValidInProject(vsHierarchy)
            End If
        End Sub

        Private Sub referenceToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles referenceToolStripMenuItem.Click, addSplitButton.Click
            Dim UIHier As IVsUIHierarchy
            If TypeOf ProjectHierarchy Is IVsUIHierarchy Then
                Try
                    UIHier = CType(ProjectHierarchy, IVsUIHierarchy)

                    Const ECMD_ADDREFERENCE As Integer = 1113

                    Dim CmdCount As UInteger = 1
                    Dim cmd As OLE.Interop.OLECMD() = New OLE.Interop.OLECMD(0) {}
                    Dim hr As Integer

                    cmd(0).cmdID = ECMD_ADDREFERENCE
                    cmd(0).cmdf = 0
                    Dim guidVSStd2k As New System.Guid(&H1496A755, &H94DE, &H11D0, &H8C, &H3F, &H0, &HC0, &H4F, &HC2, &HAA, &HE2)

                    VSErrorHandler.ThrowOnFailure(UIHier.QueryStatusCommand(VSITEMID.ROOT, guidVSStd2k, CmdCount, cmd, Nothing))

                    'Adding a reference requires a project file checkout.  Do this now to avoid nasty checkout issues.

                    Dim ProjectReloaded As Boolean = False
                    CheckoutProjectFile(ProjectReloaded)
                    If ProjectReloaded Then
                        Return
                    End If

                    hr = UIHier.ExecCommand(VSITEMID.ROOT, guidVSStd2k, ECMD_ADDREFERENCE, 0, Nothing, System.IntPtr.Zero)
                Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                    ShowErrorMessage(ex)
                End Try

                'Refresh the references
                ProcessDelayUpdateItems()
            End If
        End Sub

        Private Sub webReferenceToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles webReferenceToolStripMenuItem.Click
            Dim AddWebRefDlg As IVsAddWebReferenceDlg2
            Dim DiscoveryResult As IDiscoveryResult = Nothing

            AddWebRefDlg = CType(ServiceProvider.GetService(GetType(IVsAddWebReferenceDlg)), IVsAddWebReferenceDlg2)

            Dim Cancelled As Integer
            Dim url As String = Nothing
            Dim newName As String = Nothing

            Try
                'Adding a reference requires a project file checkout.  Do this now to avoid nasty checkout issues.
                Dim ProjectReloaded As Boolean = False
                CheckoutProjectFile(ProjectReloaded)
                If ProjectReloaded Then
                    Return
                End If

                VSErrorHandler.ThrowOnFailure(AddWebRefDlg.AddWebReferenceDlg(Nothing, url, newName, DiscoveryResult, Cancelled))
                If Cancelled = 0 Then
                    'CONSIDER: Shouldn't this be cached and applied by 'Apply' button?
                    Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
                    Dim item As EnvDTE.ProjectItem = theVSProject.AddWebReference(url)
                    If String.Compare(item.Name, newName, StringComparison.Ordinal) <> 0 Then
                        item.Name = newName
                    End If
                    Me.PopulateReferenceList()
                    Me.PopulateImportsList(True)
                End If
            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                If Not Common.IsCheckoutCanceledException(ex) Then
                    ShowErrorMessage(SR.GetString(SR.PPG_Reference_AddWebReference, ex.Message))
                End If
            End Try
        End Sub


        Private Sub serviceReferenceToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles serviceReferenceToolStripMenuItem.Click
            Dim AddServiceRefDlg As IVsAddWebReferenceDlg3

            AddServiceRefDlg = CType(ServiceProvider.GetService(GetType(SVsAddWebReferenceDlg3)), IVsAddWebReferenceDlg3)

            Debug.Assert(AddServiceRefDlg IsNot Nothing, "Why we couldn't find ASR dialog service")
            If AddServiceRefDlg IsNot Nothing Then
                Dim result As IVsAddWebReferenceResult = Nothing
                Dim Cancelled As Integer = 0
                Dim serviceType As ServiceReferenceType = ServiceReferenceType.SRT_WCFReference Or ServiceReferenceType.SRT_ASMXReference

                Try
                    AddServiceRefDlg.ShowAddWebReferenceDialog( _
                                ShellUtil.VsHierarchyFromDTEProject(ServiceProvider, DTEProject), _
                                Nothing, _
                                serviceType, _
                                Nothing, _
                                Nothing, _
                                Nothing, _
                                result, _
                                Cancelled)
                    If Cancelled = 0 Then
                        Debug.Assert(Not m_UpdatingReferences, "We shouldn't be in another updating session")

                        m_UpdatingReferences = True
                        Try
                            result.Save()
                        Finally
                            m_UpdatingReferences = False
                        End Try

                        Me.PopulateReferenceList()
                        Me.PopulateImportsList(True)

                        Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(Microsoft.Internal.Performance.CodeMarkerEvent.perfMSVSEditorsReferencePageWCFAdded)
                    End If
                Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                    If Not Common.IsCheckoutCanceledException(ex) Then
                        ShowErrorMessage(SR.GetString(SR.PPG_Reference_AddWebReference, ex.Message))
                    End If
                End Try
            End If

        End Sub

        Private Sub EnableReferenceGroup()
            Dim items As ListView.SelectedListViewItemCollection = Me.ReferenceList.SelectedItems

            Dim removeReferencesButtonEnabled As Boolean = (items.Count > 0)

            ' if the remove-reference button is enabled AND if it is going to be disabled AND if
            '   the button contains focus, then the logical place to put focus is on the ReferenceList
            '   ListView so the user can continue to interact with references
            '
            If (RemoveReference.Enabled AndAlso Not removeReferencesButtonEnabled AndAlso RemoveReference.ContainsFocus) Then
                ActiveControl = ReferenceList
            End If

            'Enable/Disable Remove button
            Me.RemoveReference.Enabled = removeReferencesButtonEnabled

            'Enable/Disable Update button (valid for Web references only)
            For i As Integer = 0 To items.Count - 1
                If TryCast(items(i).Tag, IUpdatableReferenceComponent) Is Nothing Then
                    UpdateReferences.Enabled = False
                    Return
                End If
            Next

            UpdateReferences.Enabled = (items.Count > 0)
        End Sub

        Private Sub EnableImportGroup()
            Dim EnableAddImportButton As Boolean = False
            Dim EnableUpdateUserImportButton As Boolean = False
            Dim ScrubbedUserImportText As String = UserImportTextBox.Text.Trim()

            If ScrubbedUserImportText <> "" Then
                ' Check if the item already exists in the list box - if so, don't allow users to
                ' add/update the item
                ' We can't use the listbox.items.contains method, since that would be a case-sensitive
                ' lookup, and we don't want that!
                Dim itemAlreadyExists As Boolean = False

                Dim userImportId As New ImportIdentity(ScrubbedUserImportText)
                For Each KnownItem As String In ImportList.Items
                    If userImportId.Equals(New ImportIdentity(KnownItem)) Then
                        itemAlreadyExists = True
                        Exit For
                    End If
                Next

                If Not itemAlreadyExists Then
                    ' The "Add user imports" button should be enabled iff:
                    ' * The text in the add user import textbox isn't empty AND
                    ' * The import list box doesn't already contain this item
                    EnableAddImportButton = True


                    ' The "Update user import" button should be enabled iff:
                    ' * There is only one item selected in the imports list box
                    ' * The list of known namespaces retreived from the compiler doesn't 
                    '   contain the currently selected item in the imports list box
                    '   (we can't modify those imports...)
                    Debug.Assert(ImportListSelectedItem IsNot Nothing, "ImportListSelectedItem should not return Nothing")
                    If ((ImportListSelectedItem IsNot Nothing) AndAlso (ImportListSelectedItem.Length > 0)) Then
                        EnableUpdateUserImportButton = True
                        Dim selectedItemIdentity As New ImportIdentity(DirectCast(ImportListSelectedItem, String))
                        For Each NamespaceKnownByTheCompiler As String In GetReferencedNamespaceList()
                            If selectedItemIdentity.Equals(New ImportIdentity(NamespaceKnownByTheCompiler)) Then
                                EnableUpdateUserImportButton = False
                                Exit For
                            End If
                        Next
                    End If
                Else
                    'The item's key is already in the list (for aliased imports, this means that the alias is in the
                    '  list).  There's one other case where we want to enable the Update User Import button - the
                    '  case where they want to change an aliased or XML import.  I.e., suppose they have "a=MS.VB" in 
                    '  the list and they want to change it to "a=MS.VB.Compatibility".  In this case, itemAlreadyExists
                    '  is true because the key "a" is already in the list.  So, if the key of the selected item
                    '  is the same as the key of the item in the textbox, and the full text of the two is not the
                    '  same, then we enable the Update User Import button.
                    Debug.Assert(ImportListSelectedItem IsNot Nothing, "ImportListSelectedItem should not return Nothing")
                    If ((ImportListSelectedItem IsNot Nothing) AndAlso (ImportListSelectedItem.Length > 0)) Then
                        Dim selectedItemIdentity As New ImportIdentity(DirectCast(ImportListSelectedItem, String))
                        If userImportId.Equals(selectedItemIdentity) _
                                    AndAlso Not ScrubbedUserImportText.Equals(ImportListSelectedItem, _
                                                                                StringComparison.Ordinal) Then
                            EnableUpdateUserImportButton = True
                        End If
                    End If
                End If
            End If

            AddUserImportButton.Enabled = EnableAddImportButton

            ' if the update-user-import button is enabled AND if it is going to be disabled AND if
            '   the button contains focus, then the logical place to put focus is on the ImportList
            '   CheckedListBox so the user can continue to interact with imports
            '
            If (UpdateUserImportButton.Enabled AndAlso Not EnableUpdateUserImportButton AndAlso UpdateUserImportButton.ContainsFocus) Then
                ActiveControl = ImportList
            End If
            UpdateUserImportButton.Enabled = EnableUpdateUserImportButton
        End Sub

        Private Sub ReferenceList_ItemActivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles ReferenceList.ItemActivate
            Dim items As ListView.SelectedListViewItemCollection = Me.ReferenceList.SelectedItems
            If items.Count > 0 Then
                DTE.ExecuteCommand("View.PropertiesWindow", String.Empty)
            End If
        End Sub

        Private Sub ReferenceList_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles ReferenceList.KeyDown
            If e.KeyCode = Keys.Delete Then
                Dim items As ListView.SelectedListViewItemCollection = Me.ReferenceList.SelectedItems
                If items.Count > 0 Then
                    RemoveSelectedReference()
                End If
            End If
        End Sub

        Private Sub ReferenceList_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ReferenceList.SelectedIndexChanged
            Me.EnableReferenceGroup()

            PushSelection()
        End Sub

        Private Sub ReferenceList_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles ReferenceList.Enter
            PushSelection()
        End Sub

        ''' <Summary>
        '''  When the customer clicks a column header, we should sort the reference list
        ''' </Summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ReferenceList_ColumnClick(ByVal sender As Object, ByVal e As ColumnClickEventArgs) Handles ReferenceList.ColumnClick
            ListViewComparer.HandleColumnClick(ReferenceList, m_ReferenceSorter, e)
        End Sub

        Private Sub UpdateReferences_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles UpdateReferences.Click
            Using New WaitCursor
                Dim items As ListView.SelectedListViewItemCollection = Me.ReferenceList.SelectedItems
                For Each item As ListViewItem In items
                    Dim referenceComponent As IUpdatableReferenceComponent = TryCast(item.Tag, IUpdatableReferenceComponent)
                    If referenceComponent IsNot Nothing Then
                        Try
                            referenceComponent.Update()
                        Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                            If Not Common.IsCheckoutCanceledException(ex) Then
                                ShowErrorMessage(SR.GetString(SR.PPG_Reference_FailedToUpdateWebReference, CType(referenceComponent, IReferenceComponent).GetName(), ex.Message))
                            End If
                        End Try
                    End If
                Next
            End Using
        End Sub


        Private Sub UnusedReferences_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UnusedReferences.Click
            ' Take a snapshot of the user imports...
            Dim ImportsSnapshot As IDictionary(Of String, Boolean) = GetUserDefinedImportsSnapshot()

            If ShowChildPage(SR.GetString(SR.PropPage_UnusedReferenceTitle), GetType(UnusedReferencePropPage)) = DialogResult.OK Then
                If SaveImportedNamespaces(TrimUserImports(ImportsSnapshot)) Then
                    'RemoveInvalidEntries=True here because so that we can remove imports
                    '  that correspond to the removed references, instead of just unchecking
                    '  them.  This will also clean up any other invalid unchecked imports in 
                    '  the list, which might be a minor surprise to the user, but shouldn't be
                    '  too bad, and is the safest fix at this point in the schedule.
                    PopulateImportsList(InitSelections:=True, RemoveInvalidEntries:=True)
                    SetDirty(Me.ImportList)
                End If
            End If
        End Sub

        Private Sub ReferencePathsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ReferencePathsButton.Click
            ShowChildPage(SR.GetString(SR.PPG_ReferencePaths_Title), GetType(ReferencePathsPropPage))
        End Sub

        Private Sub ImportList_ItemCheck(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckEventArgs) Handles ImportList.ItemCheck
            'Don't apply yet, this event is fired before the actual value has been updated
            If Not m_UpdatingImportList Then
                Microsoft.VisualStudio.Editors.Interop.NativeMethods.PostMessage(Handle, Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_IMPORTCHANGED, e.Index, 0)
            End If
        End Sub

        ''' <summary>
        ''' Delegate for calling into RestoreImportListSelection.  Used by ImportPanel_Enter.
        ''' </summary>
        ''' <remarks></remarks>
        Private Delegate Sub RestoreImportListSelectionDelegate()

        ''' <summary>
        ''' In order to see the blue selection color, we need to restore the selection of the CheckedListBox when
        ''' focus comes into the control. When focus leaves the control, we remove the selection so that the
        ''' blue selection color is not shown [when it shows, it's visually confusing as to whether or not the control
        ''' still has focus].
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ImportPanel_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles addUserImportTableLayoutPanel.Enter
            ' We restore the selection through a message pump. 
            ' The reason is vswhidbey 496909.
            ' When the user clicks an item in the ListBox to select it, we will get OnEnter first, then we noticed the selection change.
            ' We call BeginInvoke here, because it will go through the message loop to make sure we have a chance to handle the selection change event.
            BeginInvoke(New RestoreImportListSelectionDelegate(AddressOf RestoreImportListSelection))
        End Sub

        ''' <summary>
        ''' RestoreImportListSelection is called, when focus comes back into the ImportList area. We restore the selection.
        ''' However, if ImportList has already got a selection. We will know the user actually clicks (mouse) one item of the list.
        ''' In that case, we shouldn't restore the old selection (wswhibey 496909)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RestoreImportListSelection()
            m_hidingImportListSelectedItem = True
            Try
                If (m_importListSelectedItem IsNot Nothing) Then
                    If (ImportList.SelectedItem Is Nothing) Then
                        ImportList.SelectedItem = m_importListSelectedItem
                    End If
                    m_importListSelectedItem = Nothing
                End If
            Finally
                m_hidingImportListSelectedItem = False
            End Try
        End Sub

        ''' <summary>
        ''' In order to hide the blue selection color, we need to cache and remove the selection of the CheckedListBox when
        ''' focus leaves the control. When focus comes back into the control, we restore the selection so that the
        ''' blue selection color is shown.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ImportPanel_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles addUserImportTableLayoutPanel.Leave
            m_hidingImportListSelectedItem = True
            Try
                m_importListSelectedItem = DirectCast(Me.ImportList.SelectedItem, String)
                ImportList.SelectedItem = Nothing
            Finally
                m_hidingImportListSelectedItem = False
            End Try
        End Sub

        '''<summary>
        ''' Imported namespaces are currently added/removed one at a time
        '''    All removes are processed first, and then adds 
        '''</summary>
        ''' <param name="NewImportList">the imported list being saved</param>
        ''' <return>return true if any value was changed...</return>
        ''' <remarks>
        ''' CONSIDER: This is how the msvbprj code did it, and it may not work well
        '''         for other compilers (assuming this page is later shared)
        '''</remarks>
        Private Function SaveImportedNamespaces(ByVal NewImportList As String()) As Boolean
            Dim theVSProject As VSLangProj.VSProject
            Dim _Imports As VSLangProj.Imports
            Dim index, ListIndex As Integer
            Dim _Namespace As String
            Dim valueUpdated As Boolean = False

            theVSProject = CType(DTEProject.Object, VSLangProj.VSProject)

            _Imports = theVSProject.Imports

            Debug.Assert(Not m_ignoreImportEvent, "why m_ignoreImportEvent = TRUE?")
            Try
                m_ignoreImportEvent = True

                'For backward compatibility we remove all non-imported ones from the current Imports before adding any new ones
                Dim CurrentImports As String() = GetCurrentImportsList(_Imports)

                index = CurrentImports.Length
                For index = _Imports.Count To 1 Step -1
                    _Namespace = CurrentImports(index - 1)
                    ListIndex = LookupInStringArray(NewImportList, _Namespace)
                    If ListIndex = -1 Then
                        Debug.WriteLine("Removing reference: " & _Imports.Item(index))
                        Try
                            _Imports.Remove(index)
                            valueUpdated = True
                        Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                            If Common.IsCheckoutCanceledException(ex) Then
                                'Exit early - no need to show any UI, they've already seen it
                                Return valueUpdated
                            ElseIf TypeOf ex Is COMException Then
                                ShowErrorMessage(SR.GetString(SR.PPG_Reference_RemoveImportsFailUnexpected, _Namespace, Hex(DirectCast(ex, COMException).ErrorCode)))
                                Debug.Fail("Unexpected error when removing imports")
                            Else
                                ShowErrorMessage(SR.GetString(SR.PPG_Reference_RemoveImportsFailUnexpected, _Namespace, ex.Message))
                                Debug.Fail("Unexpected error when removing imports")
                            End If
                        End Try
                    End If
                Next index

                'Now add anything new
                For ListIndex = 0 To UBound(NewImportList)
                    _Namespace = NewImportList(ListIndex)
                    'Add it if not already in the list
                    index = LookupInStringArray(CurrentImports, _Namespace)
                    If index = -1 Then
                        Debug.WriteLine("Adding reference: " & _Namespace)
                        Try
                            _Imports.Add(_Namespace)
                            valueUpdated = True
                        Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                            If Common.IsCheckoutCanceledException(ex) Then
                                'Exit early - no need to show any UI, they've already seen it
                                Return valueUpdated
                            ElseIf TypeOf ex Is COMException Then
                                ShowErrorMessage(SR.GetString(SR.PPG_Reference_RemoveImportsFailUnexpected, _Namespace, Hex(DirectCast(ex, COMException).ErrorCode)))
                                Debug.Fail("Unexpected error when removing imports")
                            Else
                                ShowErrorMessage(SR.GetString(SR.PPG_Reference_RemoveImportsFailUnexpected, _Namespace, ex.Message))
                                Debug.Fail("Unexpected error when removing imports")
                            End If
                        End Try
                    End If
                Next
            Finally
                m_ignoreImportEvent = False
            End Try
            Return valueUpdated
        End Function

        Private Function LookupInStringArray(ByVal StringArray As String(), ByVal Text As String) As Integer
            For i As Integer = 0 To StringArray.Length - 1
                If String.Compare(StringArray(i), Text) = 0 Then
                    Return i
                End If
            Next
            Return -1
        End Function

        'Get the user selected values and update the project's Imports list
        Private Function ImportListGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            'Now add anything new
            Dim CheckedItems As CheckedListBox.CheckedItemCollection = ImportList.CheckedItems

            Dim List As String() = New String(CheckedItems.Count - 1) {}

            For ListIndex As Integer = 0 To List.Length - 1
                List(ListIndex) = DirectCast(CheckedItems.Item(ListIndex), String)
            Next

            value = List
            'Return True so base class sets the property
            Return True
        End Function

        Private Sub CheckCurrentImports(ByVal UserImports As String(), ByVal updateSelection As Boolean)
            'Check the user imports and sort them
            Dim SaveState As Boolean = m_fInsideInit
            Dim lastUpdatedNamespace As String = Nothing

            m_fInsideInit = True
            Try
                'Uncheck previously checked
                Dim selectedItem As New System.Collections.Specialized.StringCollection
                Try
                    m_UpdatingImportList = True
                    For Each Index As Integer In ImportList.CheckedIndices
                        If Array.IndexOf(UserImports, ImportList.Items(Index)) < 0 Then
                            ImportList.SetItemChecked(Index, False)
                            lastUpdatedNamespace = CStr(ImportList.Items(Index))
                        Else
                            selectedItem.Add(CStr(ImportList.Items(Index)))
                        End If
                    Next
                Finally
                    m_UpdatingImportList = False
                End Try

                Dim needAdjustOrder As Boolean = Not ContainsFocus

                'Now check ones we need to
                For UserIndex As Integer = UBound(UserImports) To 0 Step -1
                    If Not selectedItem.Contains(UserImports(UserIndex)) Then
                        SelectNamespaceInImportList(UserImports(UserIndex), needAdjustOrder)
                        lastUpdatedNamespace = UserImports(UserIndex)
                    End If
                Next

                If updateSelection AndAlso lastUpdatedNamespace IsNot Nothing Then
                    Dim lastChangedIndex As Integer = ImportList.Items.IndexOf(lastUpdatedNamespace)
                    If lastChangedIndex >= 0 Then
                        ImportList.TopIndex = lastChangedIndex
                        ImportList.SelectedIndex = lastChangedIndex
                        EnableImportGroup()
                    End If
                End If
            Finally
                m_fInsideInit = SaveState
            End Try
        End Sub

        Private Sub CheckCurrentImports()
            CheckCurrentImports(GetCurrentImportsList(), False)
        End Sub

        Private Function ImportListSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            Dim UserImports As String() = DirectCast(value, String()) 'GetCurrentImportsList()
            'ControlData()(0).InitialValue = UserImports 'Save import list values
            CheckCurrentImports(UserImports, True)
            Return True
        End Function

        Protected Overrides Sub OnApplyComplete(ByVal ApplySuccessful As Boolean)
            'Refress the lists
            m_fInsideInit = True
            Try
                Me.PopulateReferenceList()
                Me.PopulateImportsList(True)
            Finally
                m_fInsideInit = False
            End Try
        End Sub

        ''' <summary>
        ''' ;SetReferenceListColumnWidths
        ''' The Listview class does not support individual column widths, so we do it via sendmessage.
        ''' The ColOffset is used to support both ReferencePropPage and UnusedReferencePropPage, which have list
        ''' views with columns that are offset.
        ''' </summary>
        ''' <param name="owner">Control which owns the listview</param>
        ''' <param name="ReferenceList">The listview control to set column widths</param>
        ''' <param name="ColOffset">Offset to "Reference Name" column</param>
        ''' <remarks></remarks>
        Friend Shared Sub SetReferenceListColumnWidths(ByRef owner As Control, ByRef ReferenceList As ListView, ByVal ColOffset As Integer)
            Dim _handle As IntPtr = ReferenceList.Handle

            ' By default size all columns by size of column header text
            Dim AutoSizeMethod As Integer() = New Integer(REFCOLUMN_MAX) {Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE_USEHEADER, Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE_USEHEADER, Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE_USEHEADER, Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE_USEHEADER, Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE_USEHEADER}

            If ReferenceList.Items.Count > 0 Then
                ' If there are elements in the listview, size the name, version, and path columns by item text if not empty
                With ReferenceList.Items(0)
                    ' For the first column, if not offset, check the .text property, otherwise check the subitems
                    If (ColOffset = 0 AndAlso .Text <> "") OrElse _
                        (ColOffset > 0 AndAlso .SubItems(REFCOLUMN_NAME + ColOffset).Text <> "") Then
                        AutoSizeMethod(REFCOLUMN_NAME) = Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE
                    End If

                    If (.SubItems.Count > REFCOLUMN_VERSION + ColOffset AndAlso .SubItems(REFCOLUMN_VERSION + ColOffset).Text <> "") Then
                        AutoSizeMethod(REFCOLUMN_VERSION) = Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE
                    End If

                    If (.SubItems.Count > REFCOLUMN_PATH + ColOffset AndAlso .SubItems(REFCOLUMN_PATH + ColOffset).Text <> "") Then
                        AutoSizeMethod(REFCOLUMN_PATH) = Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVSCW_AUTOSIZE
                    End If
                End With
            End If

            ' Do actual sizing
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.SendMessage(New HandleRef(owner, _handle), Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_NAME + ColOffset, AutoSizeMethod(REFCOLUMN_NAME))
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.SendMessage(New HandleRef(owner, _handle), Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_TYPE + ColOffset, AutoSizeMethod(REFCOLUMN_TYPE))
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.SendMessage(New HandleRef(owner, _handle), Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_VERSION + ColOffset, AutoSizeMethod(REFCOLUMN_VERSION))
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.SendMessage(New HandleRef(owner, _handle), Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_COPYLOCAL + ColOffset, AutoSizeMethod(REFCOLUMN_COPYLOCAL))
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.SendMessage(New HandleRef(owner, _handle), Microsoft.VisualStudio.Editors.Interop.NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_PATH + ColOffset, AutoSizeMethod(REFCOLUMN_PATH))
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            If Me.ImportList.Focused Then
                Return HelpKeywords.VBProjPropImports
            End If
            Return HelpKeywords.VBProjPropReference
        End Function

        Private Sub ImportList_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles ImportList.Validated
        End Sub

        ''' <summary>
        ''' Add the text in the user import text box as a new project level import.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub AddUserImportButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AddUserImportButton.Click
            Debug.Assert(UserImportTextBox.Text.Trim().Length > 0, "Why was the AddUserImportButton enabled when the UserImport text was empty?")
            ' Get the current list
            Dim CurrentImports As String() = GetCurrentImportsList()
            Dim ScrubbedUserImport As String = UserImportTextBox.Text.Trim()

            'Make place for one more item...
            ReDim Preserve CurrentImports(CurrentImports.Length)

            '...add the new item...
            CurrentImports(CurrentImports.Length - 1) = ScrubbedUserImport

            '...and store it!
            If SaveImportedNamespaces(CurrentImports) Then
                'Add the item to the top of the listbox before updating the list, or else
                '  it will end up at the bottom.
                If ImportList.Items.IndexOf(ScrubbedUserImport) < 0 Then
                    ImportList.Items.Insert(0, ScrubbedUserImport)
                    ImportList.SelectedIndex = 0
                Else
                    Debug.Fail("The new item shouldn't have already been in the listbox")
                End If

                PopulateImportsList(True)
                SetDirty(Me.ImportList)

                ' Let's make sure the new item is visible & selected!
                Dim newIndex As Integer = ImportList.Items.IndexOf(ScrubbedUserImport)
                ImportList.TopIndex = newIndex
                ImportList.SelectedIndex = newIndex
                EnableImportGroup()
            End If
        End Sub

        ''' <summary>
        ''' Update imports button / fill in imports text box with appropriate info everytime the seleced index
        ''' changes
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ImportList_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ImportList.SelectedIndexChanged

            If Not m_hidingImportListSelectedItem Then
                m_importListSelectedItem = Nothing
                UserImportTextBox.Text = ImportListSelectedItem
                EnableImportGroup()
            End If

        End Sub

        ''' <summary>
        ''' Update the currently selected project level import
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub UpdateUserImportButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UpdateUserImportButton.Click

            Debug.Assert(ImportList.SelectedItems.Count <= 1 AndAlso _
                        ImportListSelectedItem IsNot Nothing AndAlso _
                        ImportListSelectedItem.Length > 0, "Why do we try to update more than one selected item?!")

            Dim UserImports As String() = GetCurrentImportsList()
            Dim UserImportToUpdate As String = ImportListSelectedItem
            Dim ScrubbedUpdatedUserImport As String = UserImportTextBox.Text.Trim()

            Debug.Assert(UserImportToUpdate IsNot Nothing, "ImportListSelectedItem should not return Nothing")
            If (UserImportToUpdate Is Nothing) Then
                UserImportToUpdate = String.Empty
            End If

            Dim UserImportUpdated As Boolean = False
            For pos As Integer = 0 To UserImports.Length - 1
                If UserImports(pos).Equals(UserImportToUpdate, StringComparison.OrdinalIgnoreCase) Then
                    UserImports(pos) = ScrubbedUpdatedUserImport
                    UserImportUpdated = True
                    Exit For
                End If
            Next

            If UserImportUpdated AndAlso SaveImportedNamespaces(UserImports) Then
                'Modify the value in-place in the listbox
                Dim currentIndex As Integer = ImportList.Items.IndexOf(UserImportToUpdate)
                If currentIndex >= 0 Then
                    ImportList.Items(currentIndex) = ScrubbedUpdatedUserImport
                Else
                    Debug.Fail("Why didn't we find the old item?")
                End If

                PopulateImportsList(True)
                SetDirty(Me.ImportList)
            End If

            ' Let's make sure the updated item is still selected...
            ' The PopulateImportsList failed to reset it, 'cause updating an import is really a remove/add operation
            Dim updatedItemIndex As Integer = ImportList.Items.IndexOf(ScrubbedUpdatedUserImport)
            If updatedItemIndex <> -1 Then
                ImportList.SetSelected(updatedItemIndex, True)
            End If
        End Sub

        ''' <summary>
        ''' The import buttons state depend on the contents of this text box
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub UserImportTextBox_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles UserImportTextBox.TextChanged
            EnableImportGroup()
        End Sub

        Private Sub AdviseReferencesEvents(ByVal vsProject As VSLangProj.VSProject)
            If vsProject IsNot Nothing AndAlso m_ReferencesEventsCookie Is Nothing Then
                Dim projectEvents As VSLangProj.VSProjectEvents = vsProject.Events
                Dim referencesEvents As VSLangProj.ReferencesEvents = projectEvents.ReferencesEvents
                m_ReferencesEventsCookie = New NativeMethods.ConnectionPointCookie(referencesEvents, Me, GetType(VSLangProj._dispReferencesEvents))
            End If
        End Sub

        Private Sub UnadviseReferencesEvents()
            If m_ReferencesEventsCookie IsNot Nothing Then
                m_ReferencesEventsCookie.Disconnect()
                m_ReferencesEventsCookie = Nothing
            End If
        End Sub

        Private Sub AdviseImportsEvents(ByVal vsProject As VSLangProj.VSProject)
            If vsProject IsNot Nothing AndAlso m_ImportsEventsCookie Is Nothing Then
                Dim projectEvents As VSLangProj.VSProjectEvents = vsProject.Events
                Dim importsEvents As VSLangProj.ImportsEvents = projectEvents.ImportsEvents
                m_ImportsEventsCookie = New NativeMethods.ConnectionPointCookie(importsEvents, Me, GetType(VSLangProj._dispImportsEvents))
            End If
        End Sub

        Private Sub UnadviseImportsEvents()
            If m_ImportsEventsCookie IsNot Nothing Then
                m_ImportsEventsCookie.Disconnect()
                m_ImportsEventsCookie = Nothing
            End If
        End Sub

        ' We post a message to refresh our UI later, because the project's reference list hasn't been updated when we get message from them.
        Private Sub PostRefreshReferenceListMessage()
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.PostMessage(Handle, Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_REFERENCES_REFRESH, 0, 0)
        End Sub

        ' We post a message to refresh the imports list
        Private Sub PostRefreshImportListMessage()
            If Not m_needRefreshImportList Then
                m_needRefreshImportList = True
                Microsoft.VisualStudio.Editors.Interop.NativeMethods.PostMessage(Handle, Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_IMPORTS_REFRESH, 0, 0)
            End If
        End Sub

        ' We post a message to refresh our UI later, because the project's reference list hasn't been updated when we get message from them.
        Private Sub PostRefreshServiceReferenceListMessage()
            Microsoft.VisualStudio.Editors.Interop.NativeMethods.PostMessage(Handle, Microsoft.VisualStudio.Editors.Common.WmUserConstants.WM_REFPAGE_SERVICEREFERENCES_REFRESH, 0, 0)
        End Sub

#Region "VSLangProj._dispReferencesEvents"
        ' We monitor Reference collection events to update our lists...
        Public Sub ReferenceAdded(ByVal reference As VSLangProj.Reference) Implements VSLangProj._dispReferencesEvents.ReferenceAdded
            If Not m_UpdatingReferences Then
                If Not IsImplicitlyAddedReference(reference) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceAdded, reference)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub

        Public Sub ReferenceChanged(ByVal reference As VSLangProj.Reference) Implements VSLangProj._dispReferencesEvents.ReferenceChanged
            If Not m_UpdatingReferences Then
                AddDelayUpdateItem(ReferenceUpdateType.ReferenceChanged, reference)
                PostRefreshReferenceListMessage()
            End If
        End Sub

        Public Sub ReferenceRemoved(ByVal reference As VSLangProj.Reference) Implements VSLangProj._dispReferencesEvents.ReferenceRemoved
            If Not m_UpdatingReferences Then
                If Not IsImplicitlyAddedReference(reference) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceRemoved, reference)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub
#End Region

#Region "VSLangProj._dispImportsEvents"
        Public Sub ImportAdded(ByVal importNamespace As String) Implements VSLangProj._dispImportsEvents.ImportAdded
            ' We always post a refresh message when the window becomes activated. So ignore it if we are not activated.
            If Not m_ignoreImportEvent AndAlso IsActivated Then
                PostRefreshImportListMessage()
            End If
        End Sub

        Public Sub ImportRemoved(ByVal importNamespace As String) Implements VSLangProj._dispImportsEvents.ImportRemoved
            ' We always post a refresh message when the window becomes activated. So ignore it if we are not activated.
            If Not m_ignoreImportEvent AndAlso IsActivated Then
                PostRefreshImportListMessage()
            End If
        End Sub
#End Region

#Region "EnvDTE.ProjectItemsEvents"
        ' We monitor ProjectItems collection events to update our lists...
        ' We only pay attention to the WebReference items inside the project the reference page works with...
        Private WithEvents projectItemEvents As EnvDTE.ProjectItemsEvents

        Public Sub ProjectItemEvents_ItemAdded(ByVal projectItem As EnvDTE.ProjectItem) Handles projectItemEvents.ItemAdded
            If Not m_UpdatingReferences AndAlso projectItem.ContainingProject Is DTEProject Then
                Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
                If theVSProject.WebReferencesFolder Is projectItem.Collection.Parent AndAlso IsWebReferenceItem(projectItem) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceAdded, projectItem)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub

        Public Sub ProjectItemEvents_ItemRemoved(ByVal projectItem As EnvDTE.ProjectItem) Handles projectItemEvents.ItemRemoved
            If Not m_UpdatingReferences AndAlso projectItem.ContainingProject Is DTEProject Then
                AddDelayUpdateItem(ReferenceUpdateType.ReferenceRemoved, projectItem)
                PostRefreshReferenceListMessage()
            End If
        End Sub

        Public Sub ProjectItemEvents_ItemRenamed(ByVal projectItem As EnvDTE.ProjectItem, ByVal oldName As String) Handles projectItemEvents.ItemRenamed
            If Not m_UpdatingReferences AndAlso projectItem.ContainingProject Is DTEProject Then
                Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
                If theVSProject.WebReferencesFolder Is projectItem.Collection.Parent AndAlso IsWebReferenceItem(projectItem) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceChanged, projectItem)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub

        Private Sub AdviseWebReferencesEvents()
            If projectItemEvents Is Nothing Then
                projectItemEvents = CType(DTE.Events.GetObject("VBProjectItemsEvents"), EnvDTE.ProjectItemsEvents)
            End If
        End Sub

        Private Sub UnadviseWebReferencesEvents()
            projectItemEvents = Nothing
        End Sub
#End Region

#Region "ReferenceManagerEvents"
        Private m_ServiceReferenceEventCookie As UInteger
        Private m_ServiceReferenceEventHooked As Boolean

        Private Sub AdviseServiceReferencesEvents()
            If m_ReferenceGroupManager IsNot Nothing AndAlso Not m_ServiceReferenceEventHooked Then
                m_ReferenceGroupManager.AdviseWCFReferenceEvents(Me, m_ServiceReferenceEventCookie)
                m_ServiceReferenceEventHooked = True
            End If
        End Sub

        Private Sub UnadviseServiceReferencesEvents()
            If m_ReferenceGroupManager IsNot Nothing AndAlso m_ServiceReferenceEventHooked Then
                m_ReferenceGroupManager.UnadviseWCFReferenceEvents(m_ServiceReferenceEventCookie)
                m_ServiceReferenceEventHooked = False
            End If
        End Sub

        Private Sub ServiceReference_OnReferenceGroupCollectionChanging() Implements IVsWCFReferenceEvents.OnReferenceGroupCollectionChanging
        End Sub

        Private Sub OnReferenceGroupCollectionChanged() Implements IVsWCFReferenceEvents.OnReferenceGroupCollectionChanged
            If Not m_UpdatingReferences Then
                PostRefreshServiceReferenceListMessage()
            End If
        End Sub

        Private Sub OnMetadataChanging(ByVal pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnMetadataChanging
        End Sub

        Private Sub OnMetadataChanged(ByVal pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnMetadataChanged
        End Sub

        Private Sub OnReferenceGroupPropertiesChanging(ByVal pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnReferenceGroupPropertiesChanging
        End Sub

        Private Sub OnReferenceGroupPropertiesChanged(ByVal pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnReferenceGroupPropertiesChanged
            AddDelayUpdateItem(ReferenceUpdateType.ReferenceChanged, pReferenceGroup)
            PostRefreshReferenceListMessage()
        End Sub

        Private Sub OnConfigurationChanged() Implements IVsWCFReferenceEvents.OnConfigurationChanged
        End Sub

        ''' <summary>
        ''' Reference all service references in the list.
        ''' We actually compare the original list and new list to generate DelayUpdateItem and process them later.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RefreshServiceReferences()
            If m_ReferenceGroupManager IsNot Nothing Then
                Dim collection As IVsWCFReferenceGroupCollection = m_ReferenceGroupManager.GetReferenceGroupCollection()
                Dim newReferences As New ArrayList()
                For j As Integer = 0 To collection.Count - 1
                    newReferences.Add(collection.Item(j))
                Next
                For i As Integer = 0 To ReferenceList.Items.Count - 1
                    Dim serviceCompo As ServiceReferenceComponent = TryCast(ReferenceList.Items(i).Tag, ServiceReferenceComponent)

                    If serviceCompo IsNot Nothing Then
                        Dim oldReference As IVsWCFReferenceGroup = serviceCompo.ReferenceGroup
                        Dim newIndex As Integer = newReferences.IndexOf(oldReference)
                        If newIndex < 0 Then
                            AddDelayUpdateItem(ReferenceUpdateType.ReferenceRemoved, oldReference)
                        Else
                            newReferences.RemoveAt(newIndex)
                        End If
                    End If
                Next

                For Each newRef As IVsWCFReferenceGroup In newReferences
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceAdded, newRef)
                Next

                ProcessDelayUpdateItems()
            End If
        End Sub
#End Region

#Region "ReferenceUpdateItem"
        Private Enum ReferenceUpdateType
            ReferenceAdded
            ReferenceChanged
            ReferenceRemoved
        End Enum

        ''' <Summary>
        ''' This is the structure we used to save information when we receive Reference/WebReference change event.
        ''' We save the changes in a collection, and do a batch process to update our UI later.
        '''  We record Reference/WebReferenc changes with the same class. But only one of the Reference and WebReference property contains value, while the other one contains Nothing
        ''' </Summary>
        Private Class ReferenceUpdateItem
            Private m_updateType As ReferenceUpdateType
            Private m_reference As VSLangProj.Reference
            Private m_webReference As EnvDTE.ProjectItem
            Private m_serviceReference As IVsWCFReferenceGroup

            Friend Sub New(ByVal updateType As ReferenceUpdateType, ByVal reference As VSLangProj.Reference)
                m_updateType = updateType
                m_reference = reference
            End Sub

            Friend Sub New(ByVal updateType As ReferenceUpdateType, ByVal item As EnvDTE.ProjectItem)
                m_updateType = updateType
                m_webReference = item
            End Sub

            Friend Sub New(ByVal updateType As ReferenceUpdateType, ByVal item As IVsWCFReferenceGroup)
                m_updateType = updateType
                m_serviceReference = item
            End Sub

            Friend ReadOnly Property UpdateType() As ReferenceUpdateType
                Get
                    Return m_updateType
                End Get
            End Property

            Friend ReadOnly Property Reference() As VSLangProj.Reference
                Get
                    Return m_reference
                End Get
            End Property

            Friend ReadOnly Property WebReference() As EnvDTE.ProjectItem
                Get
                    Return m_webReference
                End Get
            End Property

            Friend ReadOnly Property ServiceReference() As IVsWCFReferenceGroup
                Get
                    Return m_serviceReference
                End Get
            End Property
        End Class
#End Region

        ''' <Summary>
        ''' We save information in a collection when we receive Reference change event.
        ''' </Summary>
        Private Overloads Sub AddDelayUpdateItem(ByVal updateType As ReferenceUpdateType, ByVal reference As VSLangProj.Reference)
            If m_delayUpdatingItems Is Nothing Then
                m_delayUpdatingItems = New Queue
            End If
            m_delayUpdatingItems.Enqueue(New ReferenceUpdateItem(updateType, reference))
        End Sub

        ''' <Summary>
        ''' We save information in a collection when we receive WebReference change event.
        ''' </Summary>
        Private Overloads Sub AddDelayUpdateItem(ByVal updateType As ReferenceUpdateType, ByVal item As EnvDTE.ProjectItem)
            If m_delayUpdatingItems Is Nothing Then
                m_delayUpdatingItems = New Queue
            End If
            m_delayUpdatingItems.Enqueue(New ReferenceUpdateItem(updateType, item))
        End Sub

        ''' <Summary>
        ''' We save information in a collection when we receive ServiceReference change event.
        ''' </Summary>
        Private Overloads Sub AddDelayUpdateItem(ByVal updateType As ReferenceUpdateType, ByVal item As IVsWCFReferenceGroup)
            If m_delayUpdatingItems Is Nothing Then
                m_delayUpdatingItems = New Queue
            End If
            m_delayUpdatingItems.Enqueue(New ReferenceUpdateItem(updateType, item))
        End Sub

        ''' <Summary>
        ''' We  save information in a collection when we receive Reference/WebReference change event.
        ''' We will do a batch process to update our UI later.
        '''  In some cases, we call ProcessDelayUpdateItems to do the process after we finish the UI action.
        ''' But in most case, we post a window message, and do the process later. It prevents us to access the object when it is not ready.
        ''' </Summary>
        Private Sub ProcessDelayUpdateItems()
            If m_delayUpdatingItems IsNot Nothing Then
                Dim updateComponents As New ArrayList()

                ReferenceList.BeginUpdate()
                HoldSelectionChange(True)
                Try
                    While m_delayUpdatingItems.Count > 0
                        Dim updateItem As ReferenceUpdateItem = CType(m_delayUpdatingItems.Dequeue(), ReferenceUpdateItem)
                        If updateItem.UpdateType = ReferenceUpdateType.ReferenceAdded Then
                            ' add a new item...
                            Dim newName As String
                            Dim listViewItem As ListViewItem
                            Dim newCompo As Object

                            If updateItem.Reference IsNot Nothing Then
                                newName = updateItem.Reference.Name
                                newCompo = New ReferenceComponent(updateItem.Reference)
                                listViewItem = ReferenceToListViewItem(updateItem.Reference, newCompo)
                                Debug.Assert(Not IsImplicitlyAddedReference(updateItem.Reference), "Implicitly added references should have been filtered out beforehand")
                            ElseIf updateItem.WebReference IsNot Nothing Then
                                newName = updateItem.WebReference.Name
                                newCompo = New WebReferenceComponent(Me, updateItem.WebReference)
                                listViewItem = WebReferenceToListViewItem(updateItem.WebReference, newCompo)
                            Else
                                Debug.Assert(updateItem.ServiceReference IsNot Nothing)
                                Dim service As New ServiceReferenceComponent(m_ReferenceGroupManager.GetReferenceGroupCollection(), updateItem.ServiceReference)
                                newName = service.[Namespace]
                                newCompo = service
                                listViewItem = ServiceReferenceToListViewItem(service)
                            End If

                            ' first -- find the right position to insert...
                            Dim i As Integer
                            For i = 0 To ReferenceList.Items.Count - 1
                                Dim curItem As ListViewItem = ReferenceList.Items(i)
                                If ReferenceList.ListViewItemSorter.Compare(curItem, listViewItem) > 0 Then
                                    Exit For
                                End If
                            Next

                            If i < ReferenceList.Items.Count Then
                                ReferenceList.Items.Insert(i, listViewItem)
                            Else
                                ReferenceList.Items.Add(listViewItem)
                            End If
                            updateComponents.Add(newCompo)
                        Else
                            ' Remove/update -- find the original item in the list first
                            For i As Integer = ReferenceList.Items.Count - 1 To 0 Step -1
                                Dim refCompo As ReferenceComponent = TryCast(ReferenceList.Items(i).Tag, ReferenceComponent)
                                Dim webCompo As WebReferenceComponent = TryCast(ReferenceList.Items(i).Tag, WebReferenceComponent)
                                Dim serviceCompo As ServiceReferenceComponent = TryCast(ReferenceList.Items(i).Tag, ServiceReferenceComponent)

                                If refCompo IsNot Nothing AndAlso refCompo.CurrentObject Is updateItem.Reference OrElse _
                                   webCompo IsNot Nothing AndAlso webCompo.WebReference Is updateItem.WebReference OrElse _
                                   serviceCompo IsNot Nothing AndAlso serviceCompo.ReferenceGroup Is updateItem.ServiceReference Then
                                    If updateItem.UpdateType = ReferenceUpdateType.ReferenceRemoved Then
                                        '(Note: we don't want to call IsImplicitlyAddedReference on the reference in this case, because it has already
                                        '  been deleted and therefore that call will throw.)
                                        ReferenceList.Items.RemoveAt(i)
                                    Else
                                        ' Update -- refresh our UI if any properties changed...
                                        If refCompo IsNot Nothing Then
                                            Debug.Assert(Not IsImplicitlyAddedReference(updateItem.Reference), "Implicitly added references should have been filtered out beforehand")
                                            ReferenceList.Items(i) = ReferenceToListViewItem(updateItem.Reference, refCompo)
                                            updateComponents.Add(refCompo)
                                        ElseIf webCompo IsNot Nothing Then
                                            ReferenceList.Items(i) = WebReferenceToListViewItem(updateItem.WebReference, webCompo)
                                            updateComponents.Add(webCompo)
                                        Else
                                            Debug.Assert(serviceCompo IsNot Nothing)
                                            ReferenceList.Items(i) = ServiceReferenceToListViewItem(serviceCompo)
                                            updateComponents.Add(serviceCompo)
                                        End If
                                    End If
                                    Exit For
                                End If
                            Next
                        End If
                    End While

                    ' we will update the selection area if there is new item inserted...
                    If (updateComponents.Count > 0) Then
                        Dim indices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices()
                        indices.Clear()
                        For Each compo As Object In updateComponents
                            For j As Integer = 0 To ReferenceList.Items.Count - 1
                                If ReferenceList.Items(j).Tag Is compo Then
                                    If Not indices.Contains(j) Then
                                        indices.Add(j)
                                    End If
                                    Exit For
                                End If
                            Next
                        Next
                    End If
                    m_delayUpdatingItems = Nothing
                Finally
                    ReferenceList.EndUpdate()
                    HoldSelectionChange(False)
                End Try

                ReferenceList.Refresh()
                PopulateImportsList(True)

                EnableReferenceGroup()
                PushSelection()
            End If
        End Sub

        ''' <Summary>
        ''' This function will be called when the customer change the property on the propertyPage, we need update our UI as well...
        ''' </Summary>
        Friend Sub OnWebReferencePropertyChanged(ByVal webReference As WebReferenceComponent)
            HoldSelectionChange(True)
            Try
                For i As Integer = 0 To ReferenceList.Items.Count - 1
                    If ReferenceList.Items(i).Tag Is webReference Then
                        ReferenceList.Items(i) = WebReferenceToListViewItem(webReference.WebReference, webReference)

                        Dim indices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices()
                        indices.Clear()
                        indices.Add(i)

                        Exit For
                    End If
                Next
            Finally
                HoldSelectionChange(False)
            End Try

            PushSelection()
        End Sub

#Region "ISelectionContainer"
        ' This is the interface we implement to push the object to the propertyGrid...

        ' get the number of the objects in the whole collection or only selected objects
        Private Function CountObjects(ByVal flags As UInteger, ByRef pc As UInteger) As Integer Implements ISelectionContainer.CountObjects
            If flags = 1 Then   ' GETOBJS_ALL
                pc = CUInt(ReferenceList.Items.Count)
            ElseIf flags = 2 Then ' GETOBJS_SELECTED
                pc = CUInt(ReferenceList.SelectedIndices.Count)
            Else
                Return NativeMethods.E_INVALIDARG
            End If
            Return NativeMethods.S_OK
        End Function

        ' get objects in the whole collection or only selected objects
        Private Function GetObjects(ByVal flags As UInteger, ByVal cObjects As UInteger, ByVal objects As Object()) As Integer Implements ISelectionContainer.GetObjects
            If flags = 1 Then   ' GETOBJS_ALL
                For i As Integer = 0 To Math.Min(ReferenceList.Items.Count, CInt(cObjects)) - 1
                    objects(i) = ReferenceList.Items(i).Tag
                Next
            ElseIf flags = 2 Then ' GETOBJS_SELECTED
                Dim selectedItems As ListView.SelectedListViewItemCollection = ReferenceList.SelectedItems
                For i As Integer = 0 To Math.Min(selectedItems.Count, CInt(cObjects)) - 1
                    objects(i) = selectedItems.Item(i).Tag
                Next
            Else
                Return NativeMethods.E_INVALIDARG
            End If
            Return NativeMethods.S_OK
        End Function

        ' select objects -- it will be called when the customer changes selection on the dropdown box of the propertyGrid
        Private Function SelectObjects(ByVal ucSelected As UInteger, ByVal objects As Object(), ByVal flags As UInteger) As Integer Implements ISelectionContainer.SelectObjects
            HoldSelectionChange(True)
            Try
                ReferenceList.Select()

                Dim indices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices()
                indices.Clear()
                For i As Integer = 0 To CInt(ucSelected) - 1
                    For j As Integer = 0 To ReferenceList.Items.Count - 1
                        If objects(i) Is ReferenceList.Items(j).Tag Then
                            indices.Add(j)
                        End If
                    Next
                Next
            Finally
                HoldSelectionChange(False)
            End Try

            Return NativeMethods.S_OK
        End Function

#End Region

        ' This is a state when we shouldn't push the selection to the propertyGrid.
        ' We can not push the selection when the propertyGrid calls us to change the selection, and sometime, we hold it to prevent refreshing the propertyGrid when we do something...
        Private Sub HoldSelectionChange(ByVal needHold As Boolean)
            If needHold Then
                m_holdSelectionChange = m_holdSelectionChange + 1
            Else
                m_holdSelectionChange = m_holdSelectionChange - 1
            End If
        End Sub

        ''' <summary>
        ''' Push selection to the propertyGrid
        ''' </summary>
        Private Sub PushSelection()
            If m_holdSelectionChange <= 0 Then
                Dim vsTrackSelection As ITrackSelection = TrackSelection
                If vsTrackSelection IsNot Nothing Then
                    vsTrackSelection.OnSelectChange(Me)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Searches up the parent chain for an ApplicationDesignerView, if there is one.
        ''' </summary>
        ''' <returns>The PropPageUserControlBase which hosts this property page, if any, or else Nothing.</returns>
        ''' <remarks></remarks>
        Private Function FindPropPageDesignerView() As PropPageDesigner.PropPageDesignerView
            Dim parentWindow As Control = Parent
            While parentWindow IsNot Nothing
                If TypeOf parentWindow Is PropPageDesigner.PropPageDesignerView Then
                    Return DirectCast(parentWindow, PropPageDesigner.PropPageDesignerView)
                Else
                    parentWindow = parentWindow.Parent
                End If
            End While
            Return Nothing
        End Function

    End Class

    Friend Interface IReferenceComponent
        Function GetName() As String
        Sub Remove()
    End Interface
    Friend Interface IUpdatableReferenceComponent
        Sub Update()
    End Interface

    ''' <summary>
    ''' Parses and compares identities of VB Imports statements.
    ''' For XML imports, the identity is the XML namespace name (could be empty).
    ''' For VB imports, the identity is the alias name if present and the namespace itself otherwise.
    ''' </summary>
    Friend Structure ImportIdentity
        Implements IEquatable(Of ImportIdentity)

        Private Const AliasGroupName As String = "Alias"
        Private Const AliasGroup As String = "(?<" & AliasGroupName & ">[^=""'\s]+)"

        ' Regular expression for parsing XML imports statement (<xmlns[:Alias]='url'>).
        Private Shared xmlImportRegex As New Regex( _
            "^\s*\<\s*[xX][mM][lL][nN][sS]\s*(:\s*" & AliasGroup & ")?\s*=\s*(""[^""]*""|'[^']*')\s*\>\s*$", _
            RegexOptions.Compiled)

        ' Regular expression for parsing VB alias imports statement (Alias=Namespace).
        Private Shared vbImportRegex As New Regex( _
            "^\s*" & AliasGroup & "\s*=\s*.*$", _
            RegexOptions.Compiled)

        ' Kind of import - VB regular, VB Alias, xmlns.
        Private Enum ImportKind
            VBNamespace
            VBAlias
            XmlNamespace
        End Enum

        ' Kind of the import (see above).
        Private ReadOnly kind As ImportKind
        ' The identity of the import used for comparison.
        Private ReadOnly identity As String

        ''' <summary>
        ''' Creates a new instance of the <see cref="ImportIdentity"/> structure.
        ''' </summary>
        ''' <param name="import">The imports statement (without 'Imports' keyword)</param>
        Public Sub New(ByVal import As String)
            Debug.Assert(import IsNot Nothing)

            ' Trim the string to get rid of leading/trailing spaces.
            import = import.Trim()

            ' Try to match against XML imports syntax first.
            Dim m As Match = xmlImportRegex.Match(import)
            If m.Success Then
                ' If succeeded, set identity to the alias (namespace).
                kind = ImportKind.XmlNamespace
                identity = m.Groups(AliasGroupName).Value
            Else
                ' If failed, match against VB alias import syntax.
                m = vbImportRegex.Match(import)
                If m.Success Then
                    ' If succeeded, use alias as identity.
                    kind = ImportKind.VBAlias
                    identity = m.Groups(AliasGroupName).Value
                Else
                    ' Otherwise use the whole import string as identity (namespace or invalid syntax).
                    kind = ImportKind.VBNamespace
                    identity = import
                End If
            End If

            Debug.Assert(identity IsNot Nothing)
        End Sub

        ''' <summary>
        ''' Returns whether this instance of <see cref="ImportIdentity"/> is the same (has same identity)
        ''' as another given imports.
        ''' </summary>
        ''' <param name="other">The imports to compare to.</param>
        ''' <returns>True if both imports are XML (both are non-XML) and identities much case-sensitive (case-insensitive).</returns>
        ''' <remarks>Checks a</remarks>
        Public Shadows Function Equals(ByVal other As ImportIdentity) As Boolean Implements IEquatable(Of ImportIdentity).Equals
            Return kind = other.kind AndAlso _
                identity.Equals(other.identity, _
                    Utils.IIf(kind = ImportKind.XmlNamespace, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase))
        End Function
    End Structure
End Namespace

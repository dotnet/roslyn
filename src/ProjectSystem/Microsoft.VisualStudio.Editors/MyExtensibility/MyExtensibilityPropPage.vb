' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#Const WINFORMEDITOR = False ' Set to True to open in WinForm Editor. Remember to set it back.

Option Strict On
Option Explicit On
Imports System.ComponentModel.Design
Imports System.Windows.Forms
#If Not WINFORMEDITOR Then
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.MyExtensibility
#End If

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' ;MyExtensibilityPropPage
    ''' <summary>
    ''' Property pages for VB My namespace extensions in Application Designer.
    ''' </summary>
    ''' <remarks>
    ''' Initialization for a property page is done by overriding PreInit
    ''' </remarks>
    Friend Class MyExtensibilityPropPage
#If False Then ' Change to True to edit in designer
        Inherits UserControl

        Public Sub New()
            Me.InitializeComponent()
        End Sub
#Else
        Inherits PropPageUserControlBase

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
            Debug.Assert(Me.ProjectHierarchy IsNot Nothing)

            _projectService = MyExtensibilitySolutionService.Instance.GetProjectService(Me.ProjectHierarchy)
            Debug.Assert(_projectService IsNot Nothing)

            Dim vsMenuService As IMenuCommandService = _
                TryCast( _
                MyExtensibilitySolutionService.Instance.GetService(GetType(IMenuCommandService)), _
                IMenuCommandService)
            Debug.Assert(vsMenuService IsNot Nothing, "Could not get vsMenuService!")
            Me.listViewExtensions.MenuCommandService = vsMenuService

            Me.RefreshExtensionsList()

            ' Resize each columns based on its content.
            If Me.listViewExtensions.Items.Count > 0 Then
                For i As Integer = 0 To Me.listViewExtensions.Columns.Count - 1
                    Me.listViewExtensions.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent)
                Next
            End If
        End Sub

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
        End Sub

        ''' <summary>
        ''' F1 support.
        ''' </summary>
        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpIDs.VBProjPropMyExtensions
        End Function

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            ' Support sorting.
            _comparer = New ListViewComparer()
            _comparer.SortColumn = 0
            _comparer.Sorting = SortOrder.Ascending
            Me.listViewExtensions.ListViewItemSorter = _comparer
            Me.listViewExtensions.Sorting = SortOrder.Ascending

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            Me.linkLabelHelp.SetThemedColor(VsUIShell5Service)
        End Sub

#Region "Event handlers"

        Private Sub buttonAdd_Click(ByVal sender As Object, ByVal e As EventArgs) _
                Handles buttonAdd.Click
            Me.AddExtension()
        End Sub

        Private Sub buttonRemove_Click(ByVal sender As Object, ByVal e As EventArgs) _
                Handles buttonRemove.Click
            Me.RemoveExtension()
        End Sub

        Private Sub listViewExtensions_AddExtension(ByVal sender As Object, ByVal e As EventArgs) _
                Handles listViewExtensions.AddExtension
            Me.AddExtension()
        End Sub

        Private Sub listViewExtensions_ColumnClick(ByVal sender As Object, ByVal e As ColumnClickEventArgs) _
                Handles listViewExtensions.ColumnClick
            ListViewComparer.HandleColumnClick(Me.listViewExtensions, _comparer, e)
        End Sub

        Private Sub listViewExtensions_RemoveExtension(ByVal sender As Object, ByVal e As EventArgs) _
                Handles listViewExtensions.RemoveExtension
            Me.RemoveExtension()
        End Sub

        Private Sub listViewExtensions_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
                Handles listViewExtensions.SelectedIndexChanged
            EnableButtonRemove()
        End Sub

        Private Sub m_ProjectService_ExtensionChanged() Handles _projectService.ExtensionChanged
            Me.RefreshExtensionsList()
        End Sub

        Private Sub linklabelHelp_LinkClicked( _
                ByVal sender As Object, ByVal e As LinkLabelLinkClickedEventArgs) _
                Handles linkLabelHelp.LinkClicked
            DesignUtil.DisplayTopicFromF1Keyword(ServiceProvider, HelpIDs.Dlg_AddMyNamespaceExtensions)
        End Sub
#End Region

        ''' ;AddExtension
        ''' <summary>
        ''' Launch the Add extension dialog.
        ''' </summary>
        Private Sub AddExtension()
            Debug.Assert(_projectService IsNot Nothing)
            _projectService.AddExtensionsFromPropPage()
        End Sub

        ''' ;EnableButtonRemove
        ''' <summary>
        ''' Enable / disalbe buttonRemove depending on the selected items in the list view.
        ''' </summary>
        Private Sub EnableButtonRemove()
            Me.buttonRemove.Enabled = Me.listViewExtensions.SelectedItems.Count > 0
        End Sub

        ''' ;ExtensionProjectItemGroupToListViewItem
        ''' <summary>
        ''' Return the ListViewItem for the given extension code file.
        ''' </summary>
        Private Function ExtensionProjectItemGroupToListViewItem(ByVal extensionProjectFile As MyExtensionProjectItemGroup) _
                As ListViewItem
            Debug.Assert(extensionProjectFile IsNot Nothing)

            Dim listItem As New ListViewItem(extensionProjectFile.DisplayName)
            listItem.Tag = extensionProjectFile
            listItem.SubItems.Add(extensionProjectFile.ExtensionVersion.ToString())
            listItem.SubItems.Add(extensionProjectFile.ExtensionDescription)

            Return listItem
        End Function

        ''' ;RefreshExtensionsList
        ''' <summary>
        ''' Refresh the extensions list view.
        ''' </summary>
        Private Sub RefreshExtensionsList()
            Me.listViewExtensions.Items.Clear()
            Dim extProjItemGroups As List(Of MyExtensionProjectItemGroup) = _
                _projectService.GetExtensionProjectItemGroups()
            If extProjItemGroups IsNot Nothing Then
                For Each extProjItemGroup As MyExtensionProjectItemGroup In extProjItemGroups
                    Me.listViewExtensions.Items.Add(ExtensionProjectItemGroupToListViewItem(extProjItemGroup))
                Next
                Me.listViewExtensions.Sort()
            End If
            EnableButtonRemove()
        End Sub

        ''' ;RemoveExtension
        ''' <summary>
        ''' Remove the selected extensions.
        ''' </summary>
        Private Sub RemoveExtension()
            Debug.Assert(Me.listViewExtensions.SelectedItems.Count > 0)

            Dim extProjItemGroups As New List(Of MyExtensionProjectItemGroup)
            For Each item As ListViewItem In Me.listViewExtensions.SelectedItems
                Dim extProjItemGroup As MyExtensionProjectItemGroup = TryCast(item.Tag, MyExtensionProjectItemGroup)
                If extProjItemGroup IsNot Nothing Then
                    extProjItemGroups.Add(extProjItemGroup)
                End If
            Next

            Debug.Assert(_projectService IsNot Nothing)
            _projectService.RemoveExtensionsFromPropPage(extProjItemGroups)

            Me.RefreshExtensionsList()
        End Sub

        Private WithEvents _projectService As MyExtensibilityProjectService = Nothing
        Private _comparer As ListViewComparer
#End If

#Region "Windows Form Designer generated code"
        Friend WithEvents labelDescription As System.Windows.Forms.Label
        Friend WithEvents linkLabelHelp As VSThemedLinkLabel
        Friend WithEvents listViewExtensions As MyExtensionListView
        Friend WithEvents colHeaderExtensionName As System.Windows.Forms.ColumnHeader
        Friend WithEvents tableLayoutAddRemoveButtons As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents buttonRemove As System.Windows.Forms.Button
        Friend WithEvents buttonAdd As System.Windows.Forms.Button
        Private _components As System.ComponentModel.IContainer
        Friend WithEvents colHeaderExtensionVersion As System.Windows.Forms.ColumnHeader
        Friend WithEvents colHeaderExtensionDescription As System.Windows.Forms.ColumnHeader

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MyExtensibilityPropPage))
            Me.tableLayoutOverarching = New System.Windows.Forms.TableLayoutPanel
            Me.labelDescription = New System.Windows.Forms.Label
            Me.linkLabelHelp = New VSThemedLinkLabel
            Me.listViewExtensions = New MyExtensionListView
            Me.colHeaderExtensionName = New System.Windows.Forms.ColumnHeader
            Me.colHeaderExtensionVersion = New System.Windows.Forms.ColumnHeader
            Me.colHeaderExtensionDescription = New System.Windows.Forms.ColumnHeader
            Me.tableLayoutAddRemoveButtons = New System.Windows.Forms.TableLayoutPanel
            Me.buttonRemove = New System.Windows.Forms.Button
            Me.buttonAdd = New System.Windows.Forms.Button
            Me.tableLayoutOverarching.SuspendLayout()
            Me.tableLayoutAddRemoveButtons.SuspendLayout()
            Me.SuspendLayout()
            '
            'tableLayoutOverarching
            '
            resources.ApplyResources(Me.tableLayoutOverarching, "tableLayoutOverarching")
            Me.tableLayoutOverarching.Controls.Add(Me.labelDescription, 0, 0)
            Me.tableLayoutOverarching.Controls.Add(Me.linkLabelHelp, 0, 1)
            Me.tableLayoutOverarching.Controls.Add(Me.listViewExtensions, 0, 2)
            Me.tableLayoutOverarching.Controls.Add(Me.tableLayoutAddRemoveButtons, 0, 3)
            Me.tableLayoutOverarching.Name = "tableLayoutOverarching"
            '
            'labelDescription
            '
            resources.ApplyResources(Me.labelDescription, "labelDescription")
            Me.labelDescription.Name = "labelDescription"
            '
            'linkLabelHelp
            '
            resources.ApplyResources(Me.linkLabelHelp, "linkLabelHelp")
            Me.linkLabelHelp.Name = "linkLabelHelp"
            Me.linkLabelHelp.TabStop = True
            '
            'listViewExtensions
            '
            Me.listViewExtensions.AutoArrange = False
            Me.listViewExtensions.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colHeaderExtensionName, Me.colHeaderExtensionVersion, Me.colHeaderExtensionDescription})
            resources.ApplyResources(Me.listViewExtensions, "listViewExtensions")
            Me.listViewExtensions.FullRowSelect = True
            Me.listViewExtensions.HideSelection = False
            Me.listViewExtensions.Name = "listViewExtensions"
            Me.listViewExtensions.ShowItemToolTips = True
            Me.listViewExtensions.UseCompatibleStateImageBehavior = False
            Me.listViewExtensions.View = System.Windows.Forms.View.Details
            '
            'colHeaderExtensionName
            '
            resources.ApplyResources(Me.colHeaderExtensionName, "colHeaderExtensionName")
            '
            'colHeaderExtensionVersion
            '
            resources.ApplyResources(Me.colHeaderExtensionVersion, "colHeaderExtensionVersion")
            '
            'colHeaderExtensionDescription
            '
            resources.ApplyResources(Me.colHeaderExtensionDescription, "colHeaderExtensionDescription")
            '
            'tableLayoutAddRemoveButtons
            '
            resources.ApplyResources(Me.tableLayoutAddRemoveButtons, "tableLayoutAddRemoveButtons")
            Me.tableLayoutAddRemoveButtons.Controls.Add(Me.buttonRemove, 1, 0)
            Me.tableLayoutAddRemoveButtons.Controls.Add(Me.buttonAdd, 0, 0)
            Me.tableLayoutAddRemoveButtons.Name = "tableLayoutAddRemoveButtons"
            '
            'buttonRemove
            '
            resources.ApplyResources(Me.buttonRemove, "buttonRemove")
            Me.buttonRemove.Name = "buttonRemove"
            Me.buttonRemove.UseVisualStyleBackColor = True
            '
            'buttonAdd
            '
            resources.ApplyResources(Me.buttonAdd, "buttonAdd")
            Me.buttonAdd.Name = "buttonAdd"
            Me.buttonAdd.UseVisualStyleBackColor = True
            '
            'MyExtensibilityPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.tableLayoutOverarching)
            Me.Name = "MyExtensibilityPropPage"
            Me.tableLayoutOverarching.ResumeLayout(False)
            Me.tableLayoutOverarching.PerformLayout()
            Me.tableLayoutAddRemoveButtons.ResumeLayout(False)
            Me.tableLayoutAddRemoveButtons.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents tableLayoutOverarching As System.Windows.Forms.TableLayoutPanel
#End Region

    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.ComponentModel
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;AddMyExtensionsDialog
    ''' <summary>
    ''' Dialog for adding My namespace extensions to VB project.
    ''' </summary>
    ''' <remarks>
    ''' - To edit this in WinForms Designer: change #If False to #If True.
    ''' </remarks>
    Friend Class AddMyExtensionsDialog
#If False Then ' Change to True to edit this in WinForms Designer.
        Inherits System.Windows.Forms.Form

        Public Sub New()
            MyBase.New()
            Me.InitializeComponent()
        End Sub
#Else
        Inherits Microsoft.VisualStudio.Editors.DesignerFramework.BaseDialog

        Private Sub New()
        End Sub

        ''' ;New
        ''' <summary>
        ''' Construct the dialog with the given service provider and extension templates list.
        ''' </summary>
        Public Sub New(ByVal serviceProvider As IServiceProvider, _
                ByVal extensionTemplates As List(Of MyExtensionTemplate))
            MyBase.New(serviceProvider)
            Me.InitializeComponent()

            Me.F1Keyword = HelpIDs.Dlg_AddMyNamespaceExtensions

            _extensionTemplates = extensionTemplates

            If _extensionTemplates IsNot Nothing Then
                For Each extensionTemplate As MyExtensionTemplate In _extensionTemplates
                    Me.listViewExtensions.Items.Add(ExtensionTemplateToListViewItem(extensionTemplate))
                Next
            End If

            _comparer = New ListViewComparer()
            _comparer.SortColumn = 0
            _comparer.Sorting = SortOrder.Ascending
            Me.listViewExtensions.ListViewItemSorter = _comparer
            Me.listViewExtensions.Sorting = _comparer.Sorting
            Me.listViewExtensions.Sort()

            EnableButtonOK()
        End Sub
#End If

        ''' ;ExtensionTemplatesToAdd
        ''' <summary>
        ''' The selected extension templates to add to the project.
        ''' </summary>
        Public ReadOnly Property ExtensionTemplatesToAdd() As List(Of MyExtensionTemplate)
            Get
                Return _extensionTemplatesToAdd
            End Get
        End Property

#Region "Event handlers"
        Private Sub listViewExtensions_ColumnClick(ByVal sender As Object, ByVal e As ColumnClickEventArgs) _
                Handles listViewExtensions.ColumnClick
            ListViewComparer.HandleColumnClick(Me.listViewExtensions, _comparer, e)
        End Sub

        Private Sub listViewExtensions_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) _
                Handles listViewExtensions.DoubleClick
            AddExtensions()
        End Sub

        Private Sub listViewExtensions_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
                Handles listViewExtensions.SelectedIndexChanged
            EnableButtonOK()
        End Sub

        Private Sub buttonOK_Click(ByVal sender As Object, ByVal e As EventArgs) _
                Handles buttonOK.Click
            Debug.Assert(Me.listViewExtensions.SelectedItems.Count > 0)
            AddExtensions()
        End Sub

        ''' <summary>
        ''' Click handler for the Help button. DevDiv Bugs 69458.
        ''' </summary>
        Private Sub AddMyExtensionDialog_HelpButtonClicked( _
                ByVal sender As Object, ByVal e As CancelEventArgs) _
                Handles MyBase.HelpButtonClicked
            e.Cancel = True
            Me.ShowHelp()
        End Sub
#End Region

        ''' ;AddExtensions
        ''' <summary>
        ''' Put the selected extensions to ExtensionTemplatesToAdd, set DialogResult to OK
        ''' and close the dialog.
        ''' </summary>
        Private Sub AddExtensions()
            If Me.listViewExtensions.SelectedItems.Count > 0 Then
                _extensionTemplatesToAdd = New List(Of MyExtensionTemplate)

                For Each item As ListViewItem In Me.listViewExtensions.SelectedItems
                    Dim extensionTemplate As MyExtensionTemplate = TryCast(item.Tag, MyExtensionTemplate)
                    If extensionTemplate IsNot Nothing Then
                        _extensionTemplatesToAdd.Add(extensionTemplate)
                    End If
                Next

                Me.DialogResult = Windows.Forms.DialogResult.OK
                Me.Close()
            End If
        End Sub

        ''' ;EnableButtonOK
        ''' <summary>
        ''' Enable/disable buttonOK depending on the selected items on the list view.
        ''' </summary>
        Private Sub EnableButtonOK()
            Me.buttonOK.Enabled = Me.listViewExtensions.SelectedItems.Count > 0
        End Sub

        ''' ;ExtensionTemplateToListViewItem
        ''' <summary>
        ''' Return a ListViewItem for the given extension template.
        ''' </summary>
        Private Shared Function ExtensionTemplateToListViewItem( _
                ByVal extensionTemplate As MyExtensionTemplate) As ListViewItem
            Debug.Assert(extensionTemplate IsNot Nothing, "extensionTemplate is NULL!")

            Dim item As New ListViewItem(extensionTemplate.DisplayName)
            item.Tag = extensionTemplate
            item.SubItems.Add(extensionTemplate.Version.ToString())
            item.SubItems.Add(extensionTemplate.Description)
            Return item
        End Function

        Private _extensionTemplates As List(Of MyExtensionTemplate)
        Private _extensionTemplatesToAdd As List(Of MyExtensionTemplate)
        Private _comparer As ListViewComparer

#Region "Windows Forms Designer generated code"

        Friend WithEvents listViewExtensions As System.Windows.Forms.ListView
        Friend WithEvents buttonCancel As System.Windows.Forms.Button
        Friend WithEvents buttonOK As System.Windows.Forms.Button
        Friend WithEvents tableLayoutOKCancelButtons As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents colHeaderExtensionName As System.Windows.Forms.ColumnHeader
        Friend WithEvents colHeaderExensionDescription As System.Windows.Forms.ColumnHeader
        Friend WithEvents colHeaderExtensionVersion As System.Windows.Forms.ColumnHeader
        Friend WithEvents tableLayoutOverarching As System.Windows.Forms.TableLayoutPanel

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AddMyExtensionsDialog))
            Me.tableLayoutOverarching = New System.Windows.Forms.TableLayoutPanel
            Me.tableLayoutOKCancelButtons = New System.Windows.Forms.TableLayoutPanel
            Me.buttonOK = New System.Windows.Forms.Button
            Me.buttonCancel = New System.Windows.Forms.Button
            Me.listViewExtensions = New System.Windows.Forms.ListView
            Me.colHeaderExtensionName = New System.Windows.Forms.ColumnHeader
            Me.colHeaderExtensionVersion = New System.Windows.Forms.ColumnHeader
            Me.colHeaderExensionDescription = New System.Windows.Forms.ColumnHeader
            Me.tableLayoutOverarching.SuspendLayout()
            Me.tableLayoutOKCancelButtons.SuspendLayout()
            Me.SuspendLayout()
            '
            'tableLayoutOverarching
            '
            resources.ApplyResources(Me.tableLayoutOverarching, "tableLayoutOverarching")
            Me.tableLayoutOverarching.Controls.Add(Me.tableLayoutOKCancelButtons, 0, 1)
            Me.tableLayoutOverarching.Controls.Add(Me.listViewExtensions, 0, 0)
            Me.tableLayoutOverarching.Name = "tableLayoutOverarching"
            '
            'tableLayoutOKCancelButtons
            '
            resources.ApplyResources(Me.tableLayoutOKCancelButtons, "tableLayoutOKCancelButtons")
            Me.tableLayoutOKCancelButtons.Controls.Add(Me.buttonOK, 0, 0)
            Me.tableLayoutOKCancelButtons.Controls.Add(Me.buttonCancel, 1, 0)
            Me.tableLayoutOKCancelButtons.Name = "tableLayoutOKCancelButtons"
            '
            'buttonOK
            '
            resources.ApplyResources(Me.buttonOK, "buttonOK")
            Me.buttonOK.Name = "buttonOK"
            Me.buttonOK.UseVisualStyleBackColor = True
            '
            'buttonCancel
            '
            resources.ApplyResources(Me.buttonCancel, "buttonCancel")
            Me.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.buttonCancel.Name = "buttonCancel"
            Me.buttonCancel.UseVisualStyleBackColor = True
            '
            'listViewExtensions
            '
            Me.listViewExtensions.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colHeaderExtensionName, Me.colHeaderExtensionVersion, Me.colHeaderExensionDescription})
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
            'colHeaderExensionDescription
            '
            resources.ApplyResources(Me.colHeaderExensionDescription, "colHeaderExensionDescription")
            '
            'AddMyExtensionsDialog
            '
            Me.AcceptButton = Me.buttonOK
            Me.CancelButton = Me.buttonCancel
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.tableLayoutOverarching)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "AddMyExtensionsDialog"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            Me.tableLayoutOverarching.ResumeLayout(False)
            Me.tableLayoutOverarching.PerformLayout()
            Me.tableLayoutOKCancelButtons.ResumeLayout(False)
            Me.tableLayoutOKCancelButtons.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region

    End Class

End Namespace

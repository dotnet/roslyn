'--------------------------------------------------------------------
' <copyright file="AssemblyOptionDialog.vb" company="Microsoft">
'    Copyright (c) Microsoft Corporation.  All rights reserved.
'    Information Contained Herein Is Proprietary and Confidential.
' </copyright>
'--------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Reflection
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil
Imports Res = My.Resources.MyExtensibilityRes

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;AssemblyOptionDialog
    ''' <summary>
    ''' Asking the user to add/remove the extension templates / code files
    ''' and remember the option for the trigger assembly.
    ''' </summary>
    Friend Class AssemblyOptionDialog
#If False Then ' To edit in WinForms Designer: Change False -> True and checkboxOption to System.Windows.Forms.Checkbox
        Inherits System.Windows.Forms.Form

        Public Sub New()
            MyBase.New()
            Me.InitializeComponent()
        End Sub
#Else
        Inherits Microsoft.VisualStudio.Editors.DesignerFramework.BaseDialog

        ''' ;GetAssemblyOptionDialog
        ''' <summary>
        ''' Shared method to return an instance of Add / Remove extension templates / code files dialog.
        ''' </summary>
        Public Shared Function GetAssemblyOptionDialog( _
                ByVal assemblyName As String, _
                ByVal serviceProvider As IServiceProvider, _
                ByVal objects As IList, _
                ByVal extensionAction As AddRemoveAction) As AssemblyOptionDialog

            Debug.Assert(Not String.IsNullOrEmpty(assemblyName), "NULL or empty: assemblyName!")
            Debug.Assert(serviceProvider IsNot Nothing, "NULL serviceProvider!")
            Debug.Assert(objects IsNot Nothing AndAlso objects.Count > 0, "Nothing to display!")
            Debug.Assert(extensionAction = AddRemoveAction.Add OrElse extensionAction = AddRemoveAction.Remove, "Invalid ExtensionAction!")

            assemblyName = GetAssemblyName(assemblyName)

            Dim dialog As New AssemblyOptionDialog(serviceProvider, objects)
            If extensionAction = AddRemoveAction.Add Then
                dialog.Text = Res.AssemblyOptionDialog_Add_Text
                dialog.labelQuestion.Text = String.Format(Res.AssemblyOptionDialog_Add_Question, assemblyName)
            Else
                dialog.Text = Res.AssemblyOptionDialog_Remove_Text
                dialog.labelQuestion.Text = String.Format(Res.AssemblyOptionDialog_Remove_Question, assemblyName)
            End If

            dialog.checkBoxOption.Text = String.Format(Res.AssemblyOptionDialog_Option, assemblyName)
            Return dialog
        End Function

        ''' ;GetAssemblyName
        ''' <summary>
        ''' Return the assembly name from the assembly full name.
        ''' </summary>
        Private Shared Function GetAssemblyName(ByVal assemblyFullName As String) As String
            If StringIsNullEmptyOrBlank(assemblyFullName) Then
                Return String.Empty
            End If
            Return (New AssemblyName(assemblyFullName)).Name
        End Function

        Private Sub New()
        End Sub

        Private Sub New(ByVal serviceProvider As IServiceProvider, _
                ByVal objects As IList)
            MyBase.New(serviceProvider)
            Me.InitializeComponent()

            Me.F1Keyword = HelpIDs.Dlg_AddMyNamespaceExtensions

            Debug.Assert(objects IsNot Nothing, "Nothing to display!")
            For Each listObject As Object In objects
                Dim namedObject As INamedDescribedObject = TryCast(listObject, INamedDescribedObject)
                Debug.Assert(namedObject IsNot Nothing, "Invalid object in list!")
                If namedObject IsNot Nothing Then
                    Me.listBoxItems.Items.Add(namedObject.DisplayName)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Click handler for the Help button. DevDiv Bugs 110807.
        ''' </summary>
        Private Sub AssemblyOptionDialog_HelpButtonClicked( _
                ByVal sender As Object, ByVal e As CancelEventArgs) _
                Handles MyBase.HelpButtonClicked
            e.Cancel = True
            Me.ShowHelp()
        End Sub
#End If

        Public ReadOnly Property OptionChecked() As Boolean
            Get
                Return Me.checkBoxOption.Checked
            End Get
        End Property

        Private Sub buttonYes_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonYes.Click
            Me.Close()
            Me.DialogResult = System.Windows.Forms.DialogResult.Yes
        End Sub

#Region "Windows Form Designer generated code"
        Friend WithEvents labelQuestion As System.Windows.Forms.Label
        Friend WithEvents tableLayoutOverarching As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents listBoxItems As System.Windows.Forms.ListBox
        Friend WithEvents tableLayoutYesNoButtons As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents buttonYes As System.Windows.Forms.Button
        Friend WithEvents checkBoxOption As DesignerFramework.WrapCheckBox
        Friend WithEvents buttonNo As System.Windows.Forms.Button
        Private components As System.ComponentModel.IContainer

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AssemblyOptionDialog))
            Me.tableLayoutOverarching = New System.Windows.Forms.TableLayoutPanel
            Me.labelQuestion = New System.Windows.Forms.Label
            Me.listBoxItems = New System.Windows.Forms.ListBox
            Me.tableLayoutYesNoButtons = New System.Windows.Forms.TableLayoutPanel
            Me.buttonYes = New System.Windows.Forms.Button
            Me.buttonNo = New System.Windows.Forms.Button
            Me.checkBoxOption = New DesignerFramework.WrapCheckBox
            Me.tableLayoutOverarching.SuspendLayout()
            Me.tableLayoutYesNoButtons.SuspendLayout()
            Me.SuspendLayout()
            '
            'tableLayoutOverarching
            '
            resources.ApplyResources(Me.tableLayoutOverarching, "tableLayoutOverarching")
            Me.tableLayoutOverarching.Controls.Add(Me.labelQuestion, 0, 0)
            Me.tableLayoutOverarching.Controls.Add(Me.checkBoxOption, 0, 2)
            Me.tableLayoutOverarching.Controls.Add(Me.tableLayoutYesNoButtons, 0, 3)
            Me.tableLayoutOverarching.Controls.Add(Me.listBoxItems, 0, 1)
            Me.tableLayoutOverarching.Name = "tableLayoutOverarching"
            '
            'labelQuestion
            '
            resources.ApplyResources(Me.labelQuestion, "labelQuestion")
            Me.labelQuestion.Name = "labelQuestion"
            '
            'listBoxItems
            '
            resources.ApplyResources(Me.listBoxItems, "listBoxItems")
            Me.listBoxItems.FormattingEnabled = True
            Me.listBoxItems.Name = "listBoxItems"
            '
            'tableLayoutYesNoButtons
            '
            resources.ApplyResources(Me.tableLayoutYesNoButtons, "tableLayoutYesNoButtons")
            Me.tableLayoutYesNoButtons.Controls.Add(Me.buttonYes, 0, 0)
            Me.tableLayoutYesNoButtons.Controls.Add(Me.buttonNo, 1, 0)
            Me.tableLayoutYesNoButtons.Name = "tableLayoutYesNoButtons"
            '
            'buttonYes
            '
            resources.ApplyResources(Me.buttonYes, "buttonYes")
            Me.buttonYes.Name = "buttonYes"
            Me.buttonYes.UseVisualStyleBackColor = True
            '
            'buttonNo
            '
            Me.buttonNo.DialogResult = System.Windows.Forms.DialogResult.Cancel
            resources.ApplyResources(Me.buttonNo, "buttonNo")
            Me.buttonNo.Name = "buttonNo"
            Me.buttonNo.UseVisualStyleBackColor = True
            '
            'checkBoxOption
            '
            resources.ApplyResources(Me.checkBoxOption, "checkBoxOption")
            Me.checkBoxOption.Name = "checkBoxOption"
            Me.checkBoxOption.UseVisualStyleBackColor = True
            '
            'AssemblyOptionDialog
            '
            Me.AcceptButton = Me.buttonYes
            Me.CancelButton = Me.buttonNo
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.tableLayoutOverarching)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "AssemblyOptionDialog"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            Me.tableLayoutOverarching.ResumeLayout(False)
            Me.tableLayoutOverarching.PerformLayout()
            Me.tableLayoutYesNoButtons.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub
#End Region

    End Class
End Namespace
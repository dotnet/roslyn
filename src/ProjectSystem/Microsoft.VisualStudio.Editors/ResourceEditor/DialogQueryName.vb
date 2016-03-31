Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor


    ''' <summary>
    ''' Requests a new resouce name from the user.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DialogQueryName
        Inherits BaseDialog
        'Inherits System.Windows.Forms.Form


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ServiceProvider As IServiceProvider)
            MyBase.New(ServiceProvider)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            F1Keyword = HelpIDs.Dlg_QueryName
        End Sub

#Region " Windows Form Designer generated code "

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Friend WithEvents ButtonCancel As System.Windows.Forms.Button
        Friend WithEvents ButtonAdd As System.Windows.Forms.Button
        Friend WithEvents LabelDescription As System.Windows.Forms.Label
        Friend WithEvents TextBoxName As System.Windows.Forms.TextBox
        Friend WithEvents addCancelTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(DialogQueryName))
            Me.LabelDescription = New System.Windows.Forms.Label
            Me.TextBoxName = New System.Windows.Forms.TextBox
            Me.ButtonCancel = New System.Windows.Forms.Button
            Me.ButtonAdd = New System.Windows.Forms.Button
            Me.addCancelTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.addCancelTableLayoutPanel.SuspendLayout()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'LabelDescription
            '
            resources.ApplyResources(Me.LabelDescription, "LabelDescription")
            Me.LabelDescription.Name = "LabelDescription"
            '
            'TextBoxName
            '
            resources.ApplyResources(Me.TextBoxName, "TextBoxName")
            Me.TextBoxName.Name = "TextBoxName"
            '
            'ButtonCancel
            '
            resources.ApplyResources(Me.ButtonCancel, "ButtonCancel")
            Me.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.ButtonCancel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.ButtonCancel.Name = "ButtonCancel"
            '
            'ButtonAdd
            '
            resources.ApplyResources(Me.ButtonAdd, "ButtonAdd")
            Me.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.ButtonAdd.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.ButtonAdd.Name = "ButtonAdd"
            '
            'addCancelTableLayoutPanel
            '
            resources.ApplyResources(Me.addCancelTableLayoutPanel, "addCancelTableLayoutPanel")
            Me.addCancelTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.addCancelTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.addCancelTableLayoutPanel.Controls.Add(Me.ButtonAdd, 0, 0)
            Me.addCancelTableLayoutPanel.Controls.Add(Me.ButtonCancel, 1, 0)
            Me.addCancelTableLayoutPanel.Name = "addCancelTableLayoutPanel"
            Me.addCancelTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280.0!))
            Me.overarchingTableLayoutPanel.Controls.Add(Me.LabelDescription, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.addCancelTableLayoutPanel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TextBoxName, 0, 1)
            Me.overarchingTableLayoutPanel.Margin = New System.Windows.Forms.Padding(9)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'DialogQueryName
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.Padding = New System.Windows.Forms.Padding(9, 9, 9, 0)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "DialogQueryName"
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ShowIcon = False
            Me.addCancelTableLayoutPanel.ResumeLayout(False)
            Me.addCancelTableLayoutPanel.PerformLayout()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.AcceptButton = Me.ButtonAdd
            Me.CancelButton = Me.ButtonCancel
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub

#End Region



        'Set to true if the user cancels the dialog
        Private m_Canceled As Boolean

        ' RootDesigner
        Private m_RootDesigner As ResourceEditorRootDesigner



        ''' <summary>
        ''' Requests a new resouce name from the user.
        ''' </summary>
        ''' <param name="SuggestedName">The default name to show in the dialog when it is first shown.</param>
        ''' <param name="UserCancel">[Out] True iff the user canceled the dialog.</param>
        ''' <returns>The Name selected by the user.</returns>
        ''' <remarks></remarks>
        Public Shared Function QueryAddNewResourceName(ByVal RootDesigner As ResourceEditorRootDesigner, ByVal SuggestedName As String, ByRef UserCancel As Boolean) As String
            Dim Dialog As New DialogQueryName(RootDesigner)
            With Dialog
                Try
                    .TextBoxName.Text = SuggestedName
                    .ActiveControl = .TextBoxName
                    .TextBoxName.SelectionStart = 0
                    .TextBoxName.SelectionLength = .TextBoxName.Text.Length()
                    .m_Canceled = True
                    .m_RootDesigner = RootDesigner
                    .ShowDialog()
                    If .m_Canceled Then
                        UserCancel = True
                        Return Nothing
                    Else
                        Return .TextBoxName.Text
                    End If
                Finally
                    .Dispose()
                End Try
            End With
        End Function


        ''' <summary>
        ''' Click handler for the Add button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonAdd_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ButtonAdd.Click
            Dim ResourceView As ResourceEditorView = m_RootDesigner.GetView()
            Debug.Assert(ResourceView IsNot Nothing, "Why there is no view?")
            If ResourceView IsNot Nothing Then
                Dim NewResourceName As String = Me.TextBoxName.Text
                Dim Exception As Exception = Nothing
                If String.IsNullOrEmpty(NewResourceName) Then
                    ResourceView.DsMsgBox(SR.GetString(SR.RSE_Err_NameBlank), MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_NameBlank)
                ElseIf Not Resource.ValidateName(ResourceView.ResourceFile, NewResourceName, String.Empty, NewResourceName, Exception) Then
                    ResourceView.DsMsgBox(Exception)
                Else
                    m_Canceled = False
                    Close()
                End If

                'Set focus back to the textbox for the user to change the entry
                TextBoxName.Focus()
                TextBoxName.SelectionStart = 0
                TextBoxName.SelectionLength = TextBoxName.Text.Length
            End If
        End Sub


        ''' <summary>
        ''' Click handler for the Cancel button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ButtonCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ButtonCancel.Click
            Close()
        End Sub

        ''' <summary>
        ''' Click handler for the Help button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub DialogQueryName_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
    End Class

End Namespace

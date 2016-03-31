Imports System.Text
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.Drawing

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class BuildEventCommandLineDialog
        Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            'Apply Vista Theme to list view
            Common.DTEUtils.ApplyListViewThemeStyles(TokenList.Handle)

            'When we load the macros panel is hidden so don't show the Insert button
            SetInsertButtonState(False)

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





        Friend WithEvents OKButton As System.Windows.Forms.Button

        Friend WithEvents InsertButton As System.Windows.Forms.Button

        Friend WithEvents Cancel_Button As System.Windows.Forms.Button









        Friend WithEvents MacrosPanel As System.Windows.Forms.Panel
        Friend WithEvents CommandLinePanel As System.Windows.Forms.Panel
        Friend WithEvents HideMacrosButton As System.Windows.Forms.Button
        Friend WithEvents ShowMacrosButton As System.Windows.Forms.Button
        Friend WithEvents CommandLine As System.Windows.Forms.TextBox
        Friend WithEvents TokenList As System.Windows.Forms.ListView
        Friend WithEvents Macro As System.Windows.Forms.ColumnHeader
        Friend WithEvents Value As System.Windows.Forms.ColumnHeader
        Friend WithEvents insertOkCancelTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel





        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(BuildEventCommandLineDialog))
            Me.InsertButton = New System.Windows.Forms.Button
            Me.OKButton = New System.Windows.Forms.Button
            Me.Cancel_Button = New System.Windows.Forms.Button
            Me.CommandLine = New System.Windows.Forms.TextBox
            Me.ShowMacrosButton = New System.Windows.Forms.Button
            Me.MacrosPanel = New System.Windows.Forms.Panel
            Me.HideMacrosButton = New System.Windows.Forms.Button
            Me.TokenList = New System.Windows.Forms.ListView
            Me.Macro = New System.Windows.Forms.ColumnHeader
            Me.Value = New System.Windows.Forms.ColumnHeader
            Me.CommandLinePanel = New System.Windows.Forms.Panel
            Me.insertOkCancelTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.MacrosPanel.SuspendLayout()
            Me.insertOkCancelTableLayoutPanel.SuspendLayout()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'InsertButton
            '
            resources.ApplyResources(Me.InsertButton, "InsertButton")
            Me.InsertButton.Name = "InsertButton"
            '
            'OKButton
            '
            resources.ApplyResources(Me.OKButton, "OKButton")
            Me.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.OKButton.Name = "OKButton"
            '
            'Cancel_Button
            '
            resources.ApplyResources(Me.Cancel_Button, "Cancel_Button")
            Me.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.Cancel_Button.Name = "Cancel_Button"
            '
            'CommandLine
            '
            Me.CommandLine.AcceptsReturn = True
            resources.ApplyResources(Me.CommandLine, "CommandLine")
            Me.CommandLine.Name = "CommandLine"
            '
            'ShowMacrosButton
            '
            resources.ApplyResources(Me.ShowMacrosButton, "ShowMacrosButton")
            Me.ShowMacrosButton.Name = "ShowMacrosButton"
            '
            'MacrosPanel
            '
            resources.ApplyResources(Me.MacrosPanel, "MacrosPanel")
            Me.MacrosPanel.Controls.Add(Me.HideMacrosButton)
            Me.MacrosPanel.Controls.Add(Me.TokenList)
            Me.MacrosPanel.Name = "MacrosPanel"
            '
            'HideMacrosButton
            '
            resources.ApplyResources(Me.HideMacrosButton, "HideMacrosButton")
            Me.HideMacrosButton.Name = "HideMacrosButton"
            '
            'TokenList
            '
            resources.ApplyResources(Me.TokenList, "TokenList")
            Me.TokenList.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.Macro, Me.Value})
            Me.TokenList.MultiSelect = False
            Me.TokenList.Name = "TokenList"
            Me.TokenList.ShowItemToolTips = True
            Me.TokenList.UseCompatibleStateImageBehavior = False
            Me.TokenList.View = System.Windows.Forms.View.Details
            '
            'Macro
            '
            resources.ApplyResources(Me.Macro, "Macro")
            '
            'Value
            '
            resources.ApplyResources(Me.Value, "Value")
            '
            'CommandLinePanel
            '
            resources.ApplyResources(Me.CommandLinePanel, "CommandLinePanel")
            Me.CommandLinePanel.Name = "CommandLinePanel"
            '
            'insertOkCancelTableLayoutPanel
            '
            resources.ApplyResources(Me.insertOkCancelTableLayoutPanel, "insertOkCancelTableLayoutPanel")
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.ShowMacrosButton, 2, 0)
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.InsertButton, 0, 1)
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.OKButton, 1, 1)
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.Cancel_Button, 2, 1)
            Me.insertOkCancelTableLayoutPanel.Name = "insertOkCancelTableLayoutPanel"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CommandLine, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.MacrosPanel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.insertOkCancelTableLayoutPanel, 0, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'BuildEventCommandLineDialog
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.Cancel_Button
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "BuildEventCommandLineDialog"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            Me.MacrosPanel.ResumeLayout(False)
            Me.MacrosPanel.PerformLayout()
            Me.insertOkCancelTableLayoutPanel.ResumeLayout(False)
            Me.insertOkCancelTableLayoutPanel.PerformLayout()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

        Private Shared m_DefaultInstance As BuildEventCommandLineDialog
        Private Shared m_SyncObject As New Object

#End Region

        Private m_CommandLine As String
        Private m_Tokens() As String
        Private m_Values() As String
        Private m_DTE As EnvDTE.DTE
        Private m_serviceProvider As IServiceProvider
        Private m_Page As PropPageUserControlBase

        Private m_szIntialFormSize As Size
        Private m_helpTopic As String

        Public Function SetFormTitleText(ByVal TitleText As String) As Boolean
            Me.Text = TitleText
            Return True
        End Function

        Public Function SetTokensAndValues(ByVal Tokens() As String, ByVal Values() As String) As Boolean
            m_Tokens = Tokens
            m_Values = Values

            Return ParseAndPopulateTokens()
        End Function

        Public WriteOnly Property DTE() As EnvDTE.DTE
            Set(ByVal Value As EnvDTE.DTE)
                m_DTE = Value
            End Set
        End Property

        Public WriteOnly Property Page() As PropPageUserControlBase
            Set(ByVal Value As PropPageUserControlBase)
                m_Page = Value
            End Set
        End Property

        Public Property EventCommandLine() As String
            Get
                Return m_CommandLine
            End Get
            Set(ByVal Value As String)
                m_CommandLine = Value
                Me.CommandLine.Text = m_CommandLine

                Me.CommandLine.Focus()
                Me.CommandLine.SelectedText = ""
                Me.CommandLine.SelectionStart = Len(m_CommandLine)
                Me.CommandLine.SelectionLength = 0
            End Set
        End Property

        Public Property HelpTopic() As String
            Get
                If m_helpTopic Is Nothing Then
                    If m_Page IsNot Nothing AndAlso m_Page.IsVBProject() Then
                        m_helpTopic = HelpKeywords.VBProjPropBuildEventsBuilder
                    Else
                        m_helpTopic = HelpKeywords.CSProjPropBuildEventsBuilder
                    End If
                End If

                Return m_helpTopic
            End Get
            Set(ByVal value As String)
                m_helpTopic = value
            End Set
        End Property

        Private Property ServiceProvider() As IServiceProvider
            Get
                If m_serviceProvider Is Nothing AndAlso m_DTE IsNot Nothing Then
                    Dim isp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = CType(m_DTE, Microsoft.VisualStudio.OLE.Interop.IServiceProvider)
                    If isp IsNot Nothing Then
                        m_serviceProvider = New Microsoft.VisualStudio.Shell.ServiceProvider(isp)
                    End If
                End If
                Return m_serviceProvider
            End Get
            Set(ByVal value As IServiceProvider)
                m_serviceProvider = value
            End Set
        End Property

        Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
            '// Store the command line
            m_CommandLine = Me.CommandLine.Text

            Me.Close()
        End Sub

        Private Sub CancelButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
            Me.Close()
        End Sub

        Private Sub UpdateDialog_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            InvokeHelp()
            e.Cancel = True
        End Sub

        Private Function ParseAndPopulateTokens() As Boolean
            '// Walk through the array and add each row to the listview
            Dim i As Integer
            Dim NameItem As ListViewItem

            For i = 0 To m_Tokens.Length - 1
                NameItem = New ListViewItem(m_Tokens(i))

                NameItem.SubItems.Add(m_Values(i))
                Me.TokenList.Items.Add(NameItem)
            Next

            Return True
        End Function

        Private Sub HideMacrosButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles HideMacrosButton.Click
            ShowCollapsedForm()
        End Sub

        Private Sub ShowMacrosButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ShowMacrosButton.Click
            ShowExpandedForm()
        End Sub

        Private Sub BuildEventCommandLineDialog_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
            InitializeControlLocations()

            '// Never let them resize to something smaller than the default form size
            Me.MinimumSize = Me.Size
        End Sub

        Private Function InitializeControlLocations() As Boolean
            ShowCollapsedForm()
        End Function

        Private Function ShowCollapsedForm() As Boolean
            '// Show the ShowMacros button
            Me.ShowMacrosButton.Visible = True

            Me.MacrosPanel.Visible = False
            overarchingTableLayoutPanel.RowStyles.Item(1).SizeType = SizeType.AutoSize
            Me.Height = Me.Height - MacrosPanel.Height

            '// Disable and hide the Insert button
            SetInsertButtonState(False)

            Return True
        End Function

        Private Function ShowExpandedForm() As Boolean
            '// Hide this button
            Me.ShowMacrosButton.Visible = False

            Me.MacrosPanel.Visible = True
            overarchingTableLayoutPanel.RowStyles.Item(1).SizeType = SizeType.Percent
            Me.Height = Me.Height + MacrosPanel.Height

            '// Show the Insert button
            SetInsertButtonState(True)
            Return True
        End Function

        Private Sub InsertButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles InsertButton.Click
            AddCurrentMacroToCommandLine()
        End Sub

        Private Sub TokenList_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TokenList.SelectedIndexChanged
            SetInsertButtonEnableState()
        End Sub



        Private Sub TokenList_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles TokenList.DoubleClick
            AddCurrentMacroToCommandLine()
        End Sub

        Private Function AddCurrentMacroToCommandLine() As Boolean
            Dim selectedRowsCollection As ListView.SelectedListViewItemCollection
            Dim selectedItem As ListViewItem
            Dim textToInsertStringBuilder As StringBuilder = New StringBuilder()

            selectedRowsCollection = Me.TokenList.SelectedItems
            For Each selectedItem In selectedRowsCollection
                textToInsertStringBuilder.Append("$(" + selectedItem.Text + ")")
            Next

            Me.CommandLine.SelectedText = textToInsertStringBuilder.ToString()

            Return True
        End Function

        Private Sub InvokeHelp()
            If Not IsNothing(m_Page) Then
                m_Page.Help(HelpTopic)
            Else
                ' NOTE: the m_Page is nothing for deploy project, we need keep those code ...
                Try
                    Dim sp As IServiceProvider = ServiceProvider
                    If sp IsNot Nothing Then
                        Dim vshelp As VsHelp.Help = CType(sp.GetService(GetType(VsHelp.Help)), VsHelp.Help)
                        vshelp.DisplayTopicFromF1Keyword(HelpTopic)
                    Else
                        System.Diagnostics.Debug.Fail("Can not find ServiceProvider")
                    End If

                Catch ex as System.Exception
                    System.Diagnostics.Debug.Fail("Unexpected exception during Help invocation " + ex.Message)
                End Try
            End If
        End Sub

        Private Sub BuildEventCommandLineDialog_HelpRequested(ByVal sender As System.Object, ByVal hlpevent As System.Windows.Forms.HelpEventArgs) Handles MyBase.HelpRequested
            InvokeHelp()
        End Sub

        Private Function SetInsertButtonEnableState() As Boolean
            Dim selectedRowsCollection As ListView.SelectedListViewItemCollection

            selectedRowsCollection = Me.TokenList.SelectedItems
            If selectedRowsCollection.Count > 0 Then
                Me.InsertButton.Enabled = True
            Else
                Me.InsertButton.Enabled = False
            End If
        End Function

        Private Function SetInsertButtonState(ByVal bEnable As Boolean) As Boolean
            'Me.InsertButton.Enabled = bEnable
            SetInsertButtonEnableState()

            Me.InsertButton.Visible = bEnable
            Return True
        End Function

        ''' <Summary>
        ''' We shadow the original ShowDialog, because the right way to show dialog in VS is to use the IUIService. So the font/size will be set correctly.
        ''' The caller should pass a valid serviceProvider here. The dialog also hold it to invoke the help system
        ''' </Summary>
        Public Shadows Function ShowDialog(ByVal sp As IServiceProvider) As DialogResult
            If sp IsNot Nothing Then
                ServiceProvider = sp
            End If

            If ServiceProvider IsNot Nothing Then
                Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                If uiService IsNot Nothing Then
                    Return uiService.ShowDialog(Me)
                End If
            End If
            Return MyBase.ShowDialog()
        End Function
    End Class
End Namespace

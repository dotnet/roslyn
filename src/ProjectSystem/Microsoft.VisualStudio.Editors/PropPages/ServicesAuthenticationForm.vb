' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics.CodeAnalysis
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Friend Class ServicesAuthenticationForm
        Inherits System.Windows.Forms.Form

        Private _serviceProvider As IServiceProvider
        Private _authenticationUrl As String

        <SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")> _
        Public Sub New(ByVal authenticationUrl As String, ByVal authenticationHost As String, ByVal serviceProvider As IServiceProvider)
            InitializeComponent()
            AuthenticationServiceUrl.Text = authenticationHost
            _authenticationUrl = authenticationUrl
            _serviceProvider = serviceProvider
        End Sub


        <SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")> _
        Public ReadOnly Property AuthenticationUrl() As String
            Get
                Return _authenticationUrl
            End Get
        End Property

        Public ReadOnly Property UserName() As String
            Get
                Return UserNameTextBox.Text
            End Get
        End Property

        Public ReadOnly Property Password() As String
            Get
                Return PasswordTextBox.Text
            End Get
        End Property

        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso _components IsNot Nothing Then
                    _components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub
        Friend WithEvents InnerTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AuthenticationServiceUrlLabel As System.Windows.Forms.Label
        Friend WithEvents AuthenticationServiceUrl As System.Windows.Forms.TextBox
        Friend WithEvents UserNameLabel As System.Windows.Forms.Label
        Friend WithEvents UserNameTextBox As System.Windows.Forms.TextBox
        Friend WithEvents PasswordLabel As System.Windows.Forms.Label
        Friend WithEvents PasswordTextBox As System.Windows.Forms.TextBox
        Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents InfoLabel As System.Windows.Forms.Label
        Friend WithEvents OKCancelTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents OKButton As System.Windows.Forms.Button
        Friend WithEvents Cancel As System.Windows.Forms.Button

        'Required by the Windows Form Designer
        Private _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ServicesAuthenticationForm))
            Me.InnerTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.AuthenticationServiceUrlLabel = New System.Windows.Forms.Label
            Me.AuthenticationServiceUrl = New System.Windows.Forms.TextBox
            Me.UserNameLabel = New System.Windows.Forms.Label
            Me.UserNameTextBox = New System.Windows.Forms.TextBox
            Me.PasswordLabel = New System.Windows.Forms.Label
            Me.PasswordTextBox = New System.Windows.Forms.TextBox
            Me.OKCancelTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.OKButton = New System.Windows.Forms.Button
            Me.Cancel = New System.Windows.Forms.Button
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            Me.InfoLabel = New System.Windows.Forms.Label
            Me.InnerTableLayoutPanel.SuspendLayout()
            Me.OKCancelTableLayoutPanel.SuspendLayout()
            Me.TableLayoutPanel1.SuspendLayout()
            Me.SuspendLayout()
            '
            'InnerTableLayoutPanel
            '
            resources.ApplyResources(Me.InnerTableLayoutPanel, "InnerTableLayoutPanel")
            Me.InnerTableLayoutPanel.Controls.Add(Me.AuthenticationServiceUrlLabel, 0, 1)
            Me.InnerTableLayoutPanel.Controls.Add(Me.AuthenticationServiceUrl, 0, 2)
            Me.InnerTableLayoutPanel.Controls.Add(Me.UserNameLabel, 0, 3)
            Me.InnerTableLayoutPanel.Controls.Add(Me.UserNameTextBox, 0, 4)
            Me.InnerTableLayoutPanel.Controls.Add(Me.PasswordLabel, 0, 5)
            Me.InnerTableLayoutPanel.Controls.Add(Me.PasswordTextBox, 0, 6)
            Me.InnerTableLayoutPanel.Controls.Add(Me.OKCancelTableLayoutPanel, 0, 7)
            Me.InnerTableLayoutPanel.Controls.Add(Me.TableLayoutPanel1, 0, 0)
            Me.InnerTableLayoutPanel.Name = "InnerTableLayoutPanel"
            '
            'AuthenticationServiceUrlLabel
            '
            resources.ApplyResources(Me.AuthenticationServiceUrlLabel, "AuthenticationServiceUrlLabel")
            Me.AuthenticationServiceUrlLabel.Name = "AuthenticationServiceUrlLabel"
            '
            'AuthenticationServiceUrl
            '
            resources.ApplyResources(Me.AuthenticationServiceUrl, "AuthenticationServiceUrl")
            Me.AuthenticationServiceUrl.Enabled = False
            Me.AuthenticationServiceUrl.Name = "AuthenticationServiceUrl"
            '
            'UserNameLabel
            '
            resources.ApplyResources(Me.UserNameLabel, "UserNameLabel")
            Me.UserNameLabel.Name = "UserNameLabel"
            '
            'UserNameTextBox
            '
            resources.ApplyResources(Me.UserNameTextBox, "UserNameTextBox")
            Me.UserNameTextBox.Name = "UserNameTextBox"
            '
            'PasswordLabel
            '
            resources.ApplyResources(Me.PasswordLabel, "PasswordLabel")
            Me.PasswordLabel.Name = "PasswordLabel"
            '
            'PasswordTextBox
            '
            resources.ApplyResources(Me.PasswordTextBox, "PasswordTextBox")
            Me.PasswordTextBox.Name = "PasswordTextBox"
            Me.PasswordTextBox.UseSystemPasswordChar = True
            '
            'OKCancelTableLayoutPanel
            '
            resources.ApplyResources(Me.OKCancelTableLayoutPanel, "OKCancelTableLayoutPanel")
            Me.OKCancelTableLayoutPanel.Controls.Add(Me.OKButton, 0, 0)
            Me.OKCancelTableLayoutPanel.Controls.Add(Me.Cancel, 1, 0)
            Me.OKCancelTableLayoutPanel.Name = "OKCancelTableLayoutPanel"
            '
            'OKButton
            '
            resources.ApplyResources(Me.OKButton, "OKButton")
            Me.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.OKButton.Name = "OKButton"
            Me.OKButton.UseVisualStyleBackColor = True
            '
            'Cancel
            '
            resources.ApplyResources(Me.Cancel, "Cancel")
            Me.Cancel.Name = "Cancel"
            Me.Cancel.UseVisualStyleBackColor = True
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(Me.TableLayoutPanel1, "TableLayoutPanel1")
            Me.TableLayoutPanel1.Controls.Add(Me.InfoLabel, 0, 0)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
            '
            'InfoLabel
            '
            resources.ApplyResources(Me.InfoLabel, "InfoLabel")
            Me.InfoLabel.Name = "InfoLabel"
            '
            'ServicesAuthenticationForm
            '
            Me.AcceptButton = Me.OKButton
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.Cancel
            Me.Controls.Add(Me.InnerTableLayoutPanel)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "ServicesAuthenticationForm"
            Me.ShowIcon = False
            Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show
            Me.InnerTableLayoutPanel.ResumeLayout(False)
            Me.InnerTableLayoutPanel.PerformLayout()
            Me.OKCancelTableLayoutPanel.ResumeLayout(False)
            Me.OKCancelTableLayoutPanel.PerformLayout()
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")> _
        Private Sub ShowHelp()
            Try
                If _serviceProvider IsNot Nothing Then
                    Dim vshelp As VsHelp.Help = CType(_serviceProvider.GetService(GetType(VsHelp.Help)), VsHelp.Help)
                    vshelp.DisplayTopicFromF1Keyword(HelpKeywords.VBProjPropSettingsLogin)
                Else
                    System.Diagnostics.Debug.Fail("Can not find ServiceProvider")
                End If
            Catch ex As System.Exception
                System.Diagnostics.Debug.Fail("Unexpected exception during Help invocation " + ex.Message)
            End Try
        End Sub

        Private Sub ServiceAuthenticationForm_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            ShowHelp()
        End Sub

        Private Sub ServiceAuthenticationForm_HelpRequested(ByVal sender As System.Object, ByVal hlpevent As System.Windows.Forms.HelpEventArgs) Handles Me.HelpRequested
            ShowHelp()
        End Sub

        Private _loadAnonymous As Boolean
        Public Property LoadAnonymously() As Boolean
            Get
                Return _loadAnonymous
            End Get
            Set(ByVal value As Boolean)
                _loadAnonymous = value
            End Set
        End Property

        Private Sub Cancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel.Click
            _loadAnonymous = True
            Me.DialogResult = System.Windows.Forms.DialogResult.OK
        End Sub

        Protected Overrides Function ProcessDialogKey(ByVal keyData As System.Windows.Forms.Keys) As Boolean
            If keyData = Keys.Escape Then
                Me.Close()
            End If

            Return MyBase.ProcessDialogKey(keyData)
        End Function

    End Class
End Namespace
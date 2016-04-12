' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AutoAddImportsCollisionDialog
        Private _importMnemonic As Nullable(Of Char) = Nothing
        Private _doNotImportMnemonic As Nullable(Of Char) = Nothing
        Private _lastFocus As Control
        Private _helpCallBack As IVBAddImportsDialogHelpCallback

        Public Sub New(ByVal [namespace] As String, ByVal identifier As String, ByVal minimallyQualifiedName As String, ByVal callBack As IVBAddImportsDialogHelpCallback, ByVal isp As IServiceProvider)
            MyBase.New(isp)
            _lastFocus = Me
            _helpCallBack = callBack
            InitializeComponent()
            Me.SuspendLayout()
            Try
                SetNavigationInfo(m_okButton, m_cancelButton, m_rbQualifyCurrentLine)
                SetNavigationInfo(m_cancelButton, m_rbImportsAnyways, m_okButton)
                SetNavigationInfo(m_rbImportsAnyways, m_rbQualifyCurrentLine, m_cancelButton)
                SetNavigationInfo(m_rbQualifyCurrentLine, m_okButton, m_rbImportsAnyways)

                m_lblMain.Text = String.Format(My.Resources.AddImports.AddImportsMainTextFormatString, [namespace], identifier, minimallyQualifiedName)
                m_lblMain.AutoSize = True

                Dim importAnywaysText As String = String.Format(My.Resources.AddImports.ImportsAnywaysFormatString, [namespace], [identifier], minimallyQualifiedName)
                _importMnemonic = ProcessMnemonicString(importAnywaysText)
                m_lblImportsAnyways.Text = importAnywaysText
                m_lblImportsAnyways.AutoSize = True

                Dim qualifyCurrentText As String = String.Format(My.Resources.AddImports.QualifyCurrentLineFormatString, [namespace], [identifier], minimallyQualifiedName)
                _doNotImportMnemonic = ProcessMnemonicString(qualifyCurrentText)
                m_lblQualifyCurrentLine.Text = qualifyCurrentText
                m_lblQualifyCurrentLine.AutoSize = True

                m_layoutPanel.AutoSize = True
                Me.AutoSize = True
                Me.ActiveControl = m_okButton
            Finally
                Me.ResumeLayout()
            End Try
        End Sub

        Protected Overrides Sub OnLoad(ByVal e As EventArgs)
            MyBase.OnLoad(e)
            FixupForRadioButtonLimitations(m_rbImportsAnyways, m_lblImportsAnyways)
            FixupForRadioButtonLimitations(m_rbQualifyCurrentLine, m_lblQualifyCurrentLine)
            Refresh()
        End Sub


        Private Sub FixupForRadioButtonLimitations(ByVal radioButtonToLayout As RadioButton, ByVal dummyLabel As Label)

            radioButtonToLayout.AutoSize = False
            radioButtonToLayout.Text = dummyLabel.Text


            ' need to add 4 since radiobuttons have defualt padding of 2px.
            radioButtonToLayout.Height = dummyLabel.Height + 4
            ' Don't set width, that is done by setting the columnspan

            m_layoutPanel.Controls.Remove(dummyLabel)
            m_layoutPanel.SetColumnSpan(radioButtonToLayout, 4) ' will set the Width appropriately
        End Sub


        Private Sub ButtonClick(ByVal sender As Object, ByVal e As EventArgs) Handles m_cancelButton.Click, m_okButton.Click
            Me.Close()
        End Sub

        Private Sub ClickHelpButton(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            e.Cancel = True
            OnHelpRequested(New HelpEventArgs(Point.Empty))
        End Sub

        Private Sub RequestHelp(ByVal sender As Object, ByVal hlpevent As System.Windows.Forms.HelpEventArgs) Handles Me.HelpRequested
            If (_helpCallBack IsNot Nothing) Then
                _helpCallBack.InvokeHelp()
            End If
        End Sub

        Public ReadOnly Property ShouldImportAnyways() As Boolean
            Get
                Return m_rbImportsAnyways.Checked
            End Get
        End Property
    End Class
End Namespace
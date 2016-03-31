Imports Microsoft.VisualStudio.Editors.AppDesDesignerFramework
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Public NotInheritable Class PropPageHostDialog
        Inherits BaseDialog
        'Inherits Form

        Private m_propPage As PropPageUserControlBase
        Public WithEvents Cancel As System.Windows.Forms.Button
        Public WithEvents OK As System.Windows.Forms.Button
        Public WithEvents okCancelTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Public WithEvents overArchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Private m_FirstFocusHandled As Boolean

        ''' <summary>
        ''' Gets the F1 keyword to push into the user context for this property page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides Property F1Keyword() As String
            Get
                Dim keyword As String = MyBase.F1Keyword
                If String.IsNullOrEmpty(keyword) AndAlso m_propPage IsNot Nothing Then
                    Return DirectCast(m_propPage, IPropertyPageInternal).GetHelpContextF1Keyword()
                End If
                Return keyword
            End Get
            Set(ByVal Value As String)
                MyBase.F1Keyword = Value
            End Set
        End Property

        Public Property PropPage() As PropPageUserControlBase
            Get
                Return m_propPage
            End Get
            Set(ByVal Value As PropPageUserControlBase)
                Me.SuspendLayout()
                If m_propPage IsNot Nothing Then
                    'Remove previous page if any
                    overArchingTableLayoutPanel.Controls.Remove(m_propPage)
                End If
                m_propPage = Value
                If m_propPage IsNot Nothing Then
                    'm_propPage.SuspendLayout()
                    Me.BackColor = Value.BackColor
                    Me.MinimumSize = System.Drawing.Size.Empty
                    Me.AutoSize = True

                    If (m_propPage.PageResizable) Then
                        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable
                    Else
                        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
                    End If

                    m_propPage.Margin = New System.Windows.Forms.Padding(0, 0, 0, 3)
                    m_propPage.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
                    m_propPage.TabIndex = 0
                    'overArchingTableLayoutPanel.SuspendLayout()
                    overArchingTableLayoutPanel.Controls.Add(m_propPage, 0, 0)
                    'overArchingTableLayoutPanel.ResumeLayout(False)

                    'm_propPage.ResumeLayout(False)
                End If
                Me.ResumeLayout(False)
                Me.PerformLayout()
                SetFocusToPage()
            End Set
        End Property

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

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PropPageHostDialog))
            Me.OK = New System.Windows.Forms.Button
            Me.Cancel = New System.Windows.Forms.Button
            Me.okCancelTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.overArchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.okCancelTableLayoutPanel.SuspendLayout()
            Me.overArchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'OK
            '
            resources.ApplyResources(Me.OK, "OK")
            Me.OK.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.OK.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.OK.Name = "OK"
            '
            'Cancel
            '
            resources.ApplyResources(Me.Cancel, "Cancel")
            Me.Cancel.CausesValidation = False
            Me.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.Cancel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.Cancel.Name = "Cancel"
            '
            'okCancelTableLayoutPanel
            '
            resources.ApplyResources(Me.okCancelTableLayoutPanel, "okCancelTableLayoutPanel")
            Me.okCancelTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.okCancelTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.okCancelTableLayoutPanel.Controls.Add(Me.Cancel, 1, 0)
            Me.okCancelTableLayoutPanel.Controls.Add(Me.OK, 0, 0)
            Me.okCancelTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 6, 0, 0)
            Me.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"
            Me.okCancelTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'overArchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overArchingTableLayoutPanel, "overArchingTableLayoutPanel")
            Me.overArchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overArchingTableLayoutPanel.Controls.Add(Me.okCancelTableLayoutPanel, 0, 1)
            Me.overArchingTableLayoutPanel.Name = "overArchingTableLayoutPanel"
            Me.overArchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overArchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'PropPageHostDialog
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overArchingTableLayoutPanel)
            Me.Padding = New System.Windows.Forms.Padding(12, 12, 12, 12)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "PropPageHostDialog"
            ' Do not scale, the proppage will handle it. If we set AutoScale here, the page will expand twice, and becomes way huge
            'Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            Me.okCancelTableLayoutPanel.ResumeLayout(False)
            Me.okCancelTableLayoutPanel.PerformLayout()
            Me.overArchingTableLayoutPanel.ResumeLayout(False)
            Me.overArchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ServiceProvider As System.IServiceProvider, ByVal F1Keyword As String)
            MyBase.New(ServiceProvider)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            Me.F1Keyword = F1Keyword

            Me.AcceptButton = Me.OK
            Me.CancelButton = Me.Cancel
        End Sub

        Protected Overrides Sub OnShown(ByVal e As EventArgs)
            MyBase.OnShown(e)

            If Me.MinimumSize.IsEmpty Then
                Me.MinimumSize = Me.Size
                Me.AutoSize = False
            End If
        End Sub

        Private Sub Cancel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Cancel.Click
            PropPage.RestoreInitialValues()
            Me.Close()
        End Sub

        Private Sub OK_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles OK.Click
            'Save the changes if current values
            Try
                'No errors in the values, apply & close the dialog
                If PropPage.IsDirty Then
                    PropPage.Apply()
                End If
                Me.Close()
            Catch ex As ValidationException
                m_propPage.ShowErrorMessage(ex)
                ex.RestoreFocus()
                Return
            Catch ex As SystemException
                m_propPage.ShowErrorMessage(ex)
                Return
            Catch ex As Exception
                Debug.Fail(ex.Message)
                AppDesCommon.RethrowIfUnrecoverable(ex)
                m_propPage.ShowErrorMessage(ex)
                Return
            End Try
        End Sub

        Protected Overrides Sub OnFormClosing(ByVal e As FormClosingEventArgs)
            If e.CloseReason = CloseReason.None Then
                ' That happens when the user clicks the OK button, but validation failed
                ' That is how we block the user leave when something wrong.
                e.Cancel = True
            ElseIf Me.DialogResult <> System.Windows.Forms.DialogResult.OK Then
                ' If the user cancelled the edit, we should restore the initial values...
                PropPage.RestoreInitialValues()
            End If
        End Sub

        Public Sub SetFocusToPage()
            If Not m_FirstFocusHandled AndAlso m_propPage IsNot Nothing Then
                m_FirstFocusHandled = True
                For i As Integer = 0 To m_propPage.Controls.Count - 1
                    With m_propPage.Controls.Item(i)
                        If .CanFocus() Then
                            .Focus()
                            Return
                        End If
                    End With
                Next i
            End If
        End Sub

        Private Sub PropPageHostDialog_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
    End Class

End Namespace


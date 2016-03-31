Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    'UNDONE: help id

    ''' <summary>
    ''' Display an error control with an error icon, a text message, and an Edit Xaml button
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class AppDotXamlErrorControl

        Public Event EditXamlClicked()

        Public Sub New()
            ' This call is required by the Windows Form Designer.
            InitializeComponent()
        End Sub

        Public Sub New(ByVal errorText As String)
            Me.New()
            Me.ErrorText = errorText
        End Sub

        Private Sub EditXamlButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles EditXamlButton.Click
            RaiseEvent EditXamlClicked()
        End Sub

        Public Property ErrorText() As String
            Get
                Return Me.ErrorControl.Text
            End Get
            Set(ByVal value As String)
                Me.ErrorControl.Text = value
            End Set
        End Property

        Private Sub AppDotXamlErrorControl_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.TableLayoutPanel1.Width = System.Math.Max(Me.Width, 400)
            Me.TableLayoutPanel1.Height = ErrorControl.Height + Me.EditXamlButton.Height + 100
        End Sub
    End Class

End Namespace

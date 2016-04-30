Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Partial Public Class SpecialFileCustomView
        Inherits System.Windows.Forms.UserControl

        <System.Diagnostics.DebuggerNonUserCode()> _
        Public Sub New()
            MyBase.New()

            ' This call is required by the Component Designer.
            InitializeComponent()

        End Sub

        'Control overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Control Designer
        Private components As System.ComponentModel.IContainer

        Public WithEvents LinkLabel As VsThemedLinkLabel

        ' NOTE: The following procedure is required by the Component Designer
        ' It can be modified using the Component Designer.  Do not modify it
        ' using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
            Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SpecialFileCustomView))
            Me.LinkLabel = New FocusableLinkLabel
            Me.SuspendLayout()
            '
            'LinkLabel
            '
            resources.ApplyResources(Me.LinkLabel, "LinkLabel")
            Me.LinkLabel.Name = "LinkLabel"
            Me.LinkLabel.TabStop = True
            '
            'SpecialFileCustomView
            '
            Me.Controls.Add(Me.LinkLabel)
            Me.Name = "SpecialFileCustomView"
            resources.ApplyResources(Me, "$this")
            Me.ResumeLayout(False)

        End Sub

        ''' <summary>
        ''' Overrides the default behavior of the LinkLabel to show focus
        ''' </summary>
        Private Class FocusableLinkLabel
            Inherits VsThemedLinkLabel

            ''' <summary>
            ''' Overrides the default behavior of the LinkLabel to show focus
            ''' </summary>
            Protected Overrides ReadOnly Property ShowFocusCues() As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

    End Class

End Namespace

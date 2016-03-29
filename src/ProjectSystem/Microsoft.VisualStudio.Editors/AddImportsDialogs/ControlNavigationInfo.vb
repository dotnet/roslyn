Imports System.Windows.Forms
Imports System

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class ControlNavigationInfo
        Public ReadOnly NextControl As Control
        Public ReadOnly PreviousControl As Control

        Public Sub New(ByVal NextControl As Control, ByVal PreviousControl As Control)
            Me.NextControl = NextControl
            Me.PreviousControl = PreviousControl
        End Sub
    End Class
End Namespace
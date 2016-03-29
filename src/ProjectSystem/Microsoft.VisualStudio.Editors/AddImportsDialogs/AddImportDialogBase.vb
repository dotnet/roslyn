Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Diagnostics
Imports VsShell = Microsoft.VisualStudio.Shell.Interop
Imports VsErrorHandler = Microsoft.VisualStudio.ErrorHandler

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend MustInherit Class AddImportDialogBase
        Inherits Form

        Private ReadOnly m_ServiceProvider As IServiceProvider

        Sub New(ByVal serviceProvider As IServiceProvider)
            If serviceProvider Is Nothing Then
                Throw New ArgumentNullException("serviceProvider")
            End If        
            m_ServiceProvider = serviceProvider
        End Sub

        Protected Overrides Sub OnLoad(ByVal e As EventArgs)
            ' Set the Font according to the system settings.
            Me.Font = DialogFont
            MyBase.OnLoad(e)
        End Sub

        Protected ReadOnly Property DialogFont() As Font
            Get
                Dim hostLocale As VsShell.IUIHostLocale2 = CType(m_ServiceProvider.GetService(GetType(VsShell.SUIHostLocale)), VsShell.IUIHostLocale2)
                If hostLocale IsNot Nothing Then
                    Dim fonts(1) As VsShell.UIDLGLOGFONT
                    If VSErrorHandler.Succeeded(hostLocale.GetDialogFont(fonts)) Then
                        Return Font.FromLogFont(fonts(0))
                    End If
                End If
                Debug.Fail("Couldn't get a IUIService... cheating instead :)")
                Return Form.DefaultFont
            End Get
        End Property
    End Class
End Namespace
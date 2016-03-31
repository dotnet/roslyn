Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AddImportsDialogService
        Implements IVBAddImportsDialogService

        ' Package Service Provider
        Private m_ServiceProvider As IServiceProvider

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="packageServiceProvider"></param>
        ''' <remarks></remarks>
        Friend Sub New(ByVal packageServiceProvider As IServiceProvider)
            If packageServiceProvider Is Nothing Then
                Throw New ArgumentNullException("packageServiceProvider")
            End If
            m_ServiceProvider = packageServiceProvider
        End Sub

        Public Function ShowDialog(ByVal [namespace] As String, ByVal identifier As String, byval minimallyQualifiedName as String, ByVal dialogType As AddImportsDialogType, ByVal helpCallBack As IVBAddImportsDialogHelpCallback) As AddImportsResult Implements IVBAddImportsDialogService.ShowDialog
            Select Case dialogType
                Case AddImportsDialogType.AddImportsCollisionDialog
                    Using d As New AutoAddImportsCollisionDialog([namespace], identifier, minimallyQualifiedName, helpCallBack, m_ServiceProvider)
                        Dim result As DialogResult = d.ShowDialog

                        If (result = DialogResult.Cancel) Then
                            Return AddImportsResult.AddImports_Cancel
                        ElseIf (d.ShouldImportAnyways) Then
                            Return AddImportsResult.AddImports_ImportsAnyways
                        Else
                            Return AddImportsResult.AddImports_QualifyCurrentLine
                        End If
                    End Using
                Case AddImportsDialogType.AddImportsExtensionCollisionDialog
                    Using d As New AutoAddImportsExtensionCollisionDialog([namespace], identifier, minimallyQualifiedName, helpCallBack, m_ServiceProvider)
                        Dim result As DialogResult = d.ShowDialog

                        If result = DialogResult.Cancel Then
                            Return AddImportsResult.AddImports_Cancel
                        Else
                            Return AddImportsResult.AddImports_QualifyCurrentLine
                        End If
                    End Using
                Case Else
                    Throw New InvalidOperationException("Unexpected Dialog Type")
            End Select
        End Function
    End Class
End Namespace

Imports System
Imports System.Diagnostics
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.IO

Imports Microsoft.VisualStudio.shell
Imports Microsoft.VisualStudio.shell.Interop
Imports Microsoft.VisualStudio.ole.Interop
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VSDesigner.VSDesignerPackage
Imports Microsoft.VSDesigner.Common

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Generator for strongly typed settings wrapper class
    ''' </summary>
    ''' <remarks></remarks>
    <Guid("3b4c204a-88a2-3af8-bcfd-cfcb16399541")> _
    Public Class SettingsSingleFileGenerator
        Inherits SettingsSingleFileGeneratorBase

        Public Const SingleFileGeneratorName As String = "SettingsSingleFileGenerator"

    End Class
End Namespace

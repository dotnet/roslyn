Imports System.Runtime.InteropServices
Imports System

Namespace Microsoft.VisualStudio.Editors.AddImports

    <Guid("544D52A6-04C6-4771-863D-EFB1542C8025")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IVBAddImportsDialogHelpCallback
        Sub InvokeHelp()
    End Interface

    Friend Enum AddImportsResult
        AddImports_Cancel = 1
        AddImports_ImportsAnyways = 2
        AddImports_QualifyCurrentLine = 3
    End Enum

    Friend Enum AddImportsDialogType
        AddImportsCollisionDialog = 1
        AddImportsExtensionCollisionDialog = 2
    End Enum

    <Guid("71CC3B66-3E89-45eb-BDDA-D6A5599F4C20")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IVBAddImportsDialogService
        Function ShowDialog _
        ( _
            ByVal [namespace] As String, _
            ByVal identifier As String, _
            byval minimallyQualifiedName as String, _
            ByVal dialogType As AddImportsDialogType, _
            ByVal helpCallBack As IVBAddImportsDialogHelpCallback _
        ) As AddImportsResult
    End Interface
End Namespace

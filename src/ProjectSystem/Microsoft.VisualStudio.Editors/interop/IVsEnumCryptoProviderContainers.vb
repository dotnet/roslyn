#If 0 Then
Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports System

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport(), Guid("af855397-c4dc-478b-abd4-c3dbb3759e72"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    Friend Interface IVsEnumCryptoProviderContainers
        <PreserveSig()> _
        Function [Next](<[In]()> ByVal celt As UInteger, <Out(), MarshalAs(UnmanagedType.LPArray, arraysubtype:=UnmanagedType.BStr, sizeParamIndex:=0)> ByVal Containers As String(), <Out()> ByRef celtFetched As UInteger) As Integer
        <PreserveSig()> _
                Function Reset() As Integer
    End Interface

End Namespace
#End If

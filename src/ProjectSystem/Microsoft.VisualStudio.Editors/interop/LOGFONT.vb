#If 0 Then
Imports System
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <System.Runtime.InteropServices.ComVisible(False), StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)> _
    Friend NotInheritable Class LOGFONT
        Public lfHeight As Integer
        Public lfWidth As Integer
        Public lfEscapement As Integer
        Public lfOrientation As Integer
        Public lfWeight As Integer
        Public lfItalic As Byte
        Public lfUnderline As Byte
        Public lfStrikeOut As Byte
        Public lfCharSet As Byte
        Public lfOutPrecision As Byte
        Public lfClipPrecision As Byte
        Public lfQuality As Byte
        Public lfPitchAndFamily As Byte
        <MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst:=32)> _
        Public lfFaceName As String
    End Class

End Namespace
#End If

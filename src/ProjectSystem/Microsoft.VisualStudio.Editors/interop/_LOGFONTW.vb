#If 0 Then
Imports Microsoft.VisualBasic
Imports System.Runtime.InteropServices

Imports System
Imports UnmanagedType = System.Runtime.InteropServices.UnmanagedType

'// This rather incredible list of imports is because the ToLOGFONT method.
Imports System.Diagnostics
Imports System.Text
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.Interop

    '// Special Unicode-only version of LOGFONT
    '// C#r: noAutoOffset
    <StructLayout(LayoutKind.Sequential), CLSCompliantAttribute(False)> _
    Friend NotInheritable Class _LOGFONTW

        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)> _
        Public lfHeight As Integer
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)> _
        Public lfWidth As Integer
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)> _
        Public lfEscapement As Integer
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)> _
        Public lfOrientation As Integer
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)> _
        Public lfWeight As Integer
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfItalic As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfUnderline As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfStrikeOut As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfCharSet As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfOutPrecision As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfClipPrecision As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfQuality As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)> _
        Public lfPitchAndFamily As Byte
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName0 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName1 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName2 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName3 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName4 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName5 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName6 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName7 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName8 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName9 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName10 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName11 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName12 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName13 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName14 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName15 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName16 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName17 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName18 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName19 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName20 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName21 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName22 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName23 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName24 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName25 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName26 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName27 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName28 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName29 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName30 As Short
        <System.Runtime.InteropServices.MarshalAs(UnmanagedType.I2)> _
        Public lfFaceName31 As Short

        Public Function ToLOGFONT() As LOGFONT
            Dim lfUnicode As _LOGFONTW = Me

            '// No way today to define a char array in IDL that is COM+ compatible, so
            '// we've got to read everything in here!
            '//
            Dim sb As StringBuilder = New StringBuilder
            Dim ch As Integer = 0

            Do While (ch < 32)
                Dim fi As FieldInfo = (GetType(_LOGFONTW)).GetField("lfFaceName" + ch.ToString())
                If (fi Is Nothing) Then
                    Exit Do
                End If

                Dim o As Object = fi.GetValue(Me)
                Try
                    '// C#r : ShawnB/SreeramN - Converting a variant short to a character
                    '// COM+ bug#: 16742.

                    '//variant vTemp = Convert.ChangeType(o, GetType(Int32))
                    Dim charVal As Short = CShort(o)
                    If (charVal = 0) Then
                        Exit Do
                    End If
                    sb.Append(ChrW(charVal))
                Catch ex As InvalidCastException
                    Return Nothing
                End Try
                ch += 1
            Loop

            '// Copy one LOGFONT to the other...
            Dim lfAuto As LOGFONT = New LOGFONT
            'Dim lfAuto As Microsoft.VisualStudio.Editors.Interop.LOGFONT = new Microsoft.VisualStudio.Editors.Interop.LOGFONT()
            lfAuto.lfHeight = lfUnicode.lfHeight
            lfAuto.lfWidth = lfUnicode.lfWidth
            lfAuto.lfEscapement = lfUnicode.lfEscapement
            lfAuto.lfOrientation = lfUnicode.lfOrientation
            lfAuto.lfWeight = lfUnicode.lfWeight
            lfAuto.lfItalic = lfUnicode.lfItalic
            lfAuto.lfUnderline = lfUnicode.lfUnderline
            lfAuto.lfStrikeOut = lfUnicode.lfStrikeOut
            lfAuto.lfCharSet = lfUnicode.lfCharSet
            lfAuto.lfOutPrecision = lfUnicode.lfOutPrecision
            lfAuto.lfClipPrecision = lfUnicode.lfClipPrecision
            lfAuto.lfQuality = lfUnicode.lfQuality
            lfAuto.lfPitchAndFamily = lfUnicode.lfPitchAndFamily
            lfAuto.lfFaceName = sb.ToString()

            Return lfAuto

        End Function

        Friend Function ToLOGFONT_Internal() As LOGFONT
            Dim lfUnicode As _LOGFONTW = Me

            '// No way today to define a char array in IDL that is COM+ compatible, so
            '// we've got to read everything in here!
            '//
            Dim sb As StringBuilder = New StringBuilder
            Dim ch As Integer = 0

            Do While (ch < 32)
                Dim fi As FieldInfo = (GetType(_LOGFONTW)).GetField("lfFaceName" + ch.ToString())
                If (fi Is Nothing) Then
                    Exit Do
                End If
                Dim o As Object = fi.GetValue(Me)
                Try
                    Dim charVal As Short = CShort(o)
                    If (charVal = 0) Then
                        Exit Do
                    End If
                    sb.Append(ChrW(charVal))
                Catch ex As InvalidCastException
                    Return Nothing
                End Try
                ch += 1
            Loop

            '// Copy one LOGFONT to the other...
            Dim lfAuto As LOGFONT = New LOGFONT
            lfAuto.lfHeight = lfUnicode.lfHeight
            lfAuto.lfWidth = lfUnicode.lfWidth
            lfAuto.lfEscapement = lfUnicode.lfEscapement
            lfAuto.lfOrientation = lfUnicode.lfOrientation
            lfAuto.lfWeight = lfUnicode.lfWeight
            lfAuto.lfItalic = lfUnicode.lfItalic
            lfAuto.lfUnderline = lfUnicode.lfUnderline
            lfAuto.lfStrikeOut = lfUnicode.lfStrikeOut
            lfAuto.lfCharSet = lfUnicode.lfCharSet
            lfAuto.lfOutPrecision = lfUnicode.lfOutPrecision
            lfAuto.lfClipPrecision = lfUnicode.lfClipPrecision
            lfAuto.lfQuality = lfUnicode.lfQuality
            lfAuto.lfPitchAndFamily = lfUnicode.lfPitchAndFamily
            lfAuto.lfFaceName = sb.ToString()

            Return lfAuto
        End Function

    End Class

End Namespace
#End If

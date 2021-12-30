' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary> 
    ''' Module implements Dev11 class CRC32 used in anonymous type GetHashCode implementation
    ''' See [...\Language\Shared\crc32.cpp] for details
    ''' </summary>
    Friend Module CRC32

        Public Function ComputeCRC32(names() As String) As UInt32
            Debug.Assert(names.Length > 0)
            Dim crc32 As UInt32 = &HFFFFFFFF

            For Each name In names
                crc32 = Crc32Update(crc32, s_encoding.GetBytes(CaseInsensitiveComparison.ToLower(name)))
            Next

            Return crc32
        End Function

        Private Function Crc32Update(crc32 As UInt32, bytes() As Byte) As UInt32
            For Each b In bytes
                crc32 = s_CRC32_LOOKUP_TABLE(CByte(crc32) Xor b) Xor (crc32 >> 8)
            Next
            Return crc32
        End Function

        ''' <summary>
        ''' This is actually calculating the reverse CRC
        ''' computing the reverse CRC of 0 gives the table entry above
        ''' </summary>
        Private Function CalcEntry(crc As UInt32) As UInt32
            For i = 0 To 7
                If (crc And 1) <> 0 Then
                    crc = (crc >> 1) Xor s_CRC32_poly
                Else
                    crc >>= 1
                End If
            Next
            Return crc
        End Function

        Private Function InitCrc32Table() As UInt32()
            Dim table(255) As UInt32
            For i As UInteger = 0 To 255
                Dim entry As UInt32 = CalcEntry(i)
                table(CInt(i)) = entry
            Next
            Return table
        End Function

        Private ReadOnly s_CRC32_LOOKUP_TABLE As UInt32() = InitCrc32Table()
        Private Const s_CRC32_poly As UInt32 = &HEDB88320
        Private ReadOnly s_encoding As New System.Text.UnicodeEncoding(False, False)

    End Module

End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Module Extensions

    <Extension()>
    Function GetCode(xml As XElement) As String
        Dim code = xml.Value

        If code.First() = vbLf Then
            code = code.Remove(0, 1)
        End If

        If code.Last() = vbLf Then
            code = code.Remove(code.Length - 1)
        End If

        Return code.Replace(vbLf, vbCrLf)
    End Function

End Module

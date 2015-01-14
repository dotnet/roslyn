' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

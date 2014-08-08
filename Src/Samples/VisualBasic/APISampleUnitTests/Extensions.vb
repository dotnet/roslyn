' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

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

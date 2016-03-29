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

Imports System
Imports System.IO
Imports System.Reflection

Module Module1

    Sub Main()
        For Each test In GetType(TreeTransformTests).GetMethods(BindingFlags.Public Or BindingFlags.Static)
            Console.Write(test.Name)

            Dim result = CBool(test.Invoke(Nothing, Nothing))

            If result Then
                Console.WriteLine(" : Passed")
            Else
                Console.WriteLine(" : Failed")
            End If
        Next

        Console.Read()
    End Sub

End Module

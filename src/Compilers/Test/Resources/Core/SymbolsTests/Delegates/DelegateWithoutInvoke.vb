' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' decompile this class to IL and remove the Invoke methods for the two classes
' DelegateSubWithoutInvoke and DelegateFunctionWithoutInvoke
' the resulting IL can be compiled with ilasm  test.il /exe /output=test.exe again

Public Class DelegateWithoutInvoke
    Public Delegate Sub DelegateSubWithoutInvoke(p As String)
    Public Delegate Function DelegateFunctionWithoutInvoke(p As String) As String
    Public Delegate Function DelegateGenericFunctionWithoutInvoke(Of T)(p As T) As T

    Public SubDel As DelegateSubWithoutInvoke = AddressOf DelSubImpl
    Public FuncDel As DelegateFunctionWithoutInvoke = AddressOf DelFuncImpl

    Public Shared Sub DelSubImpl(p As String)
        Console.WriteLine("DelegateWithoutInvoke.DelSubImpl called " + p)
    End Sub

    Public Shared Function DelFuncImpl(p As String) As String
        Return "DelegateWithoutInvoke.DelFuncImpl called " + p
    End Function
End Class

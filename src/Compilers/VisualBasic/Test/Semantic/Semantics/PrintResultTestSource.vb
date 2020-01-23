' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization

Module PrintHelper

    Function GetCultureInvariantString(val As Object) As String
        If val Is Nothing Then
            Return Nothing
        End If

        Dim vType = val.GetType()
        Dim valStr = val.ToString()
        If vType Is GetType(DateTime) Then
            valStr = DirectCast(val, DateTime).ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture)
        ElseIf vType Is GetType(Single) Then
            valStr = DirectCast(val, Single).ToString(CultureInfo.InvariantCulture)
        ElseIf vType Is GetType(Double) Then
            valStr = DirectCast(val, Double).ToString(CultureInfo.InvariantCulture)
        ElseIf vType Is GetType(Decimal) Then
            valStr = DirectCast(val, Decimal).ToString(CultureInfo.InvariantCulture)
        End If

        Return valStr
    End Function

    Sub PrintResult(val As Boolean)
        Console.WriteLine("Boolean: {0}", val)
    End Sub
    Sub PrintResult(val As SByte)
        Console.WriteLine("SByte: {0}", val)
    End Sub
    Sub PrintResult(val As Byte)
        Console.WriteLine("Byte: {0}", val)
    End Sub
    Sub PrintResult(val As Short)
        Console.WriteLine("Short: {0}", val)
    End Sub
    Sub PrintResult(val As UShort)
        Console.WriteLine("UShort: {0}", val)
    End Sub
    Sub PrintResult(val As Integer)
        Console.WriteLine("Integer: {0}", val)
    End Sub
    Sub PrintResult(val As UInteger)
        Console.WriteLine("UInteger: {0}", val)
    End Sub
    Sub PrintResult(val As Long)
        Console.WriteLine("Long: {0}", val)
    End Sub
    Sub PrintResult(val As ULong)
        Console.WriteLine("ULong: {0}", val)
    End Sub
    Sub PrintResult(val As Decimal)
        Console.WriteLine("Decimal: {0}", GetCultureInvariantString(val))
    End Sub
    Sub PrintResult(val As Single)
        Console.WriteLine("Single: {0}", GetCultureInvariantString(val))
    End Sub
    Sub PrintResult(val As Double)
        Console.WriteLine("Double: {0}", GetCultureInvariantString(val))
    End Sub
    Sub PrintResult(val As Date)
        Console.WriteLine("Date: {0}", GetCultureInvariantString(val))
    End Sub
    Sub PrintResult(val As Char)
        Console.WriteLine("Char: [{0}]", val)
    End Sub
    Sub PrintResult(val As Char())
        Console.WriteLine("Char(): {0}", New String(val))
    End Sub
    Sub PrintResult(val As String)
        Console.WriteLine("String: [{0}]", val)
    End Sub
    Sub PrintResult(val As Object)
        Console.WriteLine("Object: [{0}]", val)
    End Sub
    Sub PrintResult(val As Guid)
        Console.WriteLine("Guid: {0}", val)
    End Sub
    Sub PrintResult(val As ValueType)
        Dim pval = GetCultureInvariantString(val)
        Console.WriteLine("ValueType: [{0}]", pval)
    End Sub
    Sub PrintResult(val As IComparable)
        Console.WriteLine("IComparable: [{0}]", val)
    End Sub

    ' =================================================================

    Sub PrintResult(expr As String, val As Boolean)
        System.Console.WriteLine("[{1}] Boolean: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As SByte)
        System.Console.WriteLine("[{1}] SByte: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As Byte)
        System.Console.WriteLine("[{1}] Byte: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As Short)
        System.Console.WriteLine("[{1}] Short: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As UShort)
        System.Console.WriteLine("[{1}] UShort: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As Integer)
        System.Console.WriteLine("[{1}] Integer: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As UInteger)
        System.Console.WriteLine("[{1}] UInteger: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As Long)
        System.Console.WriteLine("[{1}] Long: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As ULong)
        System.Console.WriteLine("[{1}] ULong: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As Decimal)
        System.Console.WriteLine("[{1}] Decimal: {0}", GetCultureInvariantString(val), expr)
    End Sub
    Sub PrintResult(expr As String, val As Single)
        System.Console.WriteLine("[{1}] Single: {0}", GetCultureInvariantString(val), expr)
    End Sub
    Sub PrintResult(expr As String, val As Double)
        System.Console.WriteLine("[{1}] Double: {0}", GetCultureInvariantString(val), expr)
    End Sub
    Sub PrintResult(expr As String, val As Date)
        System.Console.WriteLine("[{1}] Date: {0}", GetCultureInvariantString(val), expr)
    End Sub
    Sub PrintResult(expr As String, val As Char)
        System.Console.WriteLine("[{1}] Char: {0}", val, expr)
    End Sub
    Sub PrintResult(expr As String, val As String)
        System.Console.WriteLine("[{0}] String: [{1}]", expr, val)
    End Sub
    Sub PrintResult(expr As String, val As Object)
        System.Console.WriteLine("[{1}] Object: {0}", GetCultureInvariantString(val), expr)
    End Sub
    Sub PrintResult(expr As String, val As System.TypeCode)
        System.Console.WriteLine("[{1}] TypeCode: {0}", val, expr)
    End Sub

End Module

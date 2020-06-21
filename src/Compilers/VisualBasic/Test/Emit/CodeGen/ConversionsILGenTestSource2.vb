' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict Off
Imports System
Imports System.Collections.Generic
Imports System.Globalization

Module Module1


    Sub Main()
        System.Console.WriteLine("Conversions from Nothing literal:")

        PrintResultBo(Nothing)
        PrintResultSB(Nothing)
        PrintResultBy(Nothing)
        PrintResultSh(Nothing)
        PrintResultUs(Nothing)
        PrintResultIn(Nothing)
        PrintResultUI(Nothing)
        PrintResultLo(Nothing)
        PrintResultUL(Nothing)
        PrintResultSi(Nothing)
        PrintResultDo(Nothing)
        PrintResultDe(Nothing)
        PrintResultDa(Nothing)
        'PrintResultCh(Nothing)
        PrintResultSt(Nothing)
        PrintResultOb(Nothing)
        PrintResultGuid(Nothing)
        PrintResultIComparable(Nothing)
        PrintResultValueType(Nothing)
        PrintResultSt(GenericParamTestHelperOfString.NothingToT())
        PrintResultGuid(GenericParamTestHelperOfGuid.NothingToT())
    End Sub


    Class GenericParamTestHelper(Of T)
        Public Shared Function ObjectToT(val As Object) As T
            Return val
        End Function
        Public Shared Function TToObject(val As T) As Object
            Return val
        End Function
        Public Shared Function TToIComparable(val As T) As IComparable
            Return val
        End Function
        Public Shared Function IComparableToT(val As IComparable) As T
            Return val
        End Function

        Public Shared Function NothingToT() As T
            Dim val As T
            val = Nothing
            Return val
        End Function
    End Class

    Class GenericParamTestHelperOfString
        Inherits GenericParamTestHelper(Of String)
    End Class

    Class GenericParamTestHelperOfGuid
        Inherits GenericParamTestHelper(Of Guid)
    End Class

    Sub PrintResultBo(val As Boolean)
        System.Console.WriteLine("Boolean: {0}", val)
    End Sub
    Sub PrintResultSB(val As SByte)
        System.Console.WriteLine("SByte: {0}", val)
    End Sub
    Sub PrintResultBy(val As Byte)
        System.Console.WriteLine("Byte: {0}", val)
    End Sub
    Sub PrintResultSh(val As Short)
        System.Console.WriteLine("Short: {0}", val)
    End Sub
    Sub PrintResultUs(val As UShort)
        System.Console.WriteLine("UShort: {0}", val)
    End Sub
    Sub PrintResultIn(val As Integer)
        System.Console.WriteLine("Integer: {0}", val)
    End Sub
    Sub PrintResultUI(val As UInteger)
        System.Console.WriteLine("UInteger: {0}", val)
    End Sub
    Sub PrintResultLo(val As Long)
        System.Console.WriteLine("Long: {0}", val)
    End Sub
    Sub PrintResultUL(val As ULong)
        System.Console.WriteLine("ULong: {0}", val)
    End Sub
    Sub PrintResultDe(val As Decimal)
        System.Console.WriteLine("Decimal: {0}", val)
    End Sub
    Sub PrintResultSi(val As Single)
        System.Console.WriteLine("Single: {0}", val)
    End Sub
    Sub PrintResultDo(val As Double)
        System.Console.WriteLine("Double: {0}", val)
    End Sub
    Sub PrintResultDa(val As Date)
        System.Console.WriteLine("Date: {0}", val.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
    End Sub
    Sub PrintResultCh(val As Char)
        System.Console.WriteLine("Char: [{0}]", val)
    End Sub
    Sub PrintResultSZCh(val As Char())
        System.Console.WriteLine("Char(): {0}", New String(val))
    End Sub
    Sub PrintResultSt(val As String)
        System.Console.WriteLine("String: [{0}]", val)
    End Sub
    Sub PrintResultOb(val As Object)
        System.Console.WriteLine("Object: [{0}]", val)
    End Sub
    Sub PrintResultGuid(val As System.Guid)
        System.Console.WriteLine("Guid: {0}", val)
    End Sub
    Sub PrintResultIComparable(val As IComparable)
        System.Console.WriteLine("IComparable: [{0}]", val)
    End Sub
    Sub PrintResultValueType(val As ValueType)
        System.Console.WriteLine("ValueType: [{0}]", val)
    End Sub
    Sub PrintResultIEOfChar(val As IEnumerable(Of Char))
        System.Console.WriteLine("IEnumerable(Of Char): {0}", val)
    End Sub
    Sub PrintResultICOfChar(val As ICollection(Of Char))
        System.Console.WriteLine("ICollection(Of Char): {0}", val)
    End Sub
    Sub PrintResultIListOfChar(val As IList(Of Char))
        System.Console.WriteLine("IList(Of Char): {0}", val)
    End Sub
End Module

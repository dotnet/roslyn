' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict Off
Imports System
Imports System.Collections.Generic
Imports System.Globalization

Module Module1

    Dim cul As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
    Sub Main()
        Dim BoFalse As Boolean
        Dim BoTrue As Boolean
        Dim SB As SByte
        Dim By As Byte
        Dim Sh As Short
        Dim US As UShort
        Dim [In] As Integer
        Dim UI As UInteger
        Dim Lo As Long
        Dim UL As ULong
        Dim De As Decimal
        Dim Si As Single
        Dim [Do] As Double
        Dim SBZero As SByte
        Dim ByZero As Byte
        Dim ShZero As Short
        Dim USZero As UShort
        Dim [InZero] As Integer
        Dim UIZero As UInteger
        Dim LoZero As Long
        Dim ULZero As ULong
        Dim DeZero As Decimal
        Dim SiZero As Single
        Dim [DoZero] As Double
        Dim St As String
        Dim StDate As String
        Dim Ob As Object
        Dim ObDate As Object
        Dim ObChar As Object
        Dim ObString As Object
        Dim ObGuid As Object
        Dim ObNothing As Object
        'Dim Tc As System.TypeCode
        Dim Da As Date
        Dim Ch As Char
        Dim guidVal As Guid
        Dim IEnumerableString As IEnumerable(Of Char)
        Dim IComparableGuid As IComparable
        Dim szChar As Char()

        BoFalse = False
        BoTrue = True
        SB = 1
        By = 2
        Sh = 3
        US = 4
        [In] = 5
        UI = 6
        Lo = 7
        UL = 8
        Si = 10
        [Do] = 11
        De = 9D
        St = "12"
        Ob = 13
        Da = #8:30:00 AM#
        StDate = "8:30"
        ObDate = Da
        Ch = "c"c
        ObChar = Ch
        ObString = St

        SBZero = 0
        ByZero = 0
        ShZero = 0
        USZero = 0
        [InZero] = 0
        UIZero = 0
        LoZero = 0
        ULZero = 0
        SiZero = 0
        [DoZero] = 0
        DeZero = 0

        guidVal = New Guid("eb32bf0d-6a19-4095-96b7-b4db556a5d48")
        ObGuid = guidVal
        IEnumerableString = St
        IComparableGuid = guidVal
        szChar = St.ToCharArray()

        Console.WriteLine("Conversions from Type Parameter:")
        PrintResultOb(GenericParamTestHelperOfString.TToObject(St))
        PrintResultOb(GenericParamTestHelperOfGuid.TToObject(guidVal))
        PrintResultIComparable(GenericParamTestHelperOfString.TToIComparable(St))
        PrintResultIComparable(GenericParamTestHelperOfGuid.TToIComparable(guidVal))

        Console.WriteLine()
        Console.WriteLine("Conversions to Type Parameter:")
        PrintResultGuid(GenericParamTestHelperOfGuid.ObjectToT(ObGuid))
        PrintResultIComparable(GenericParamTestHelperOfGuid.TToIComparable(guidVal))
        PrintResultSt(GenericParamTestHelperOfString.ObjectToT(ObString))
        PrintResultIComparable(GenericParamTestHelperOfString.TToIComparable(St))

        Console.WriteLine()
        Console.WriteLine("Conversions from Value types to Reference types")

        PrintResultOb(SB)
        PrintResultOb(By)
        PrintResultOb(Sh)
        PrintResultOb(US)
        PrintResultOb([In])
        PrintResultOb(UI)
        PrintResultOb(Lo)
        PrintResultOb(UL)
        PrintResultOb(Si)
        PrintResultOb([Do])
        PrintResultOb(De)
        PrintResultOb(Da.ToString("M/d/yyyy h:mm:ss tt", cul))
        PrintResultOb(Ch)
        PrintResultOb(guidVal)

        PrintResultValueType(SB)
        PrintResultValueType(By)
        PrintResultValueType(Sh)
        PrintResultValueType(US)
        PrintResultValueType([In])
        PrintResultValueType(UI)
        PrintResultValueType(Lo)
        PrintResultValueType(UL)
        PrintResultValueType(Si)
        PrintResultValueType([Do])
        PrintResultValueType(De)
        PrintResultValueType(Da)
        PrintResultValueType(Ch)
        PrintResultValueType(guidVal)

        PrintResultIComparable(SB)
        PrintResultIComparable(By)
        PrintResultIComparable(Sh)
        PrintResultIComparable(US)
        PrintResultIComparable([In])
        PrintResultIComparable(UI)
        PrintResultIComparable(Lo)
        PrintResultIComparable(UL)
        PrintResultIComparable(Si)
        PrintResultIComparable([Do])
        PrintResultIComparable(De)
        PrintResultIComparable(Da.ToString("M/d/yyyy h:mm:ss tt", cul))
        PrintResultIComparable(Ch)
        PrintResultIComparable(guidVal)

        Console.WriteLine()
        Console.WriteLine("Conversions from ref type to Char()")

        PrintResultSZCh(IEnumerableString)

        Console.WriteLine()
        Console.WriteLine("Conversions from ref type to ref type")

        PrintResultOb(St)
        PrintResultOb(IEnumerableString)
        PrintResultOb(IComparableGuid)
        PrintResultIComparable(St)
        PrintResultIComparable(IEnumerableString)
        PrintResultSt(IEnumerableString)
        PrintResultIComparable(ObGuid)
        PrintResultIEOfChar(szChar)
        PrintResultICOfChar(szChar)
        PrintResultIListOfChar(szChar)

        Console.WriteLine()
        Console.WriteLine("Conversions from ref types to value types")

        PrintResultGuid(ObGuid)
        PrintResultGuid(IComparableGuid)
        PrintResultGuid(ObNothing)

        PrintResultBo(ObNothing)
        PrintResultSB(ObNothing)
        PrintResultBy(ObNothing)
        PrintResultSh(ObNothing)
        PrintResultUs(ObNothing)
        PrintResultIn(ObNothing)
        PrintResultUI(ObNothing)
        PrintResultLo(ObNothing)
        PrintResultUL(ObNothing)
        PrintResultSi(ObNothing)
        PrintResultDo(ObNothing)
        PrintResultDe(ObNothing)
        PrintResultDa(ObNothing)
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
    End Class

    Class GenericParamTestHelperOfString
        Inherits GenericParamTestHelper(Of String)
    End Class

    Class GenericParamTestHelperOfGuid
        Inherits GenericParamTestHelper(Of Guid)
    End Class

    Sub PrintResultBo(val As Boolean)
        Console.WriteLine("Boolean: {0}", val)
    End Sub
    Sub PrintResultSB(val As SByte)
        Console.WriteLine("SByte: {0}", val)
    End Sub
    Sub PrintResultBy(val As Byte)
        Console.WriteLine("Byte: {0}", val)
    End Sub
    Sub PrintResultSh(val As Short)
        Console.WriteLine("Short: {0}", val)
    End Sub
    Sub PrintResultUs(val As UShort)
        Console.WriteLine("UShort: {0}", val)
    End Sub
    Sub PrintResultIn(val As Integer)
        Console.WriteLine("Integer: {0}", val)
    End Sub
    Sub PrintResultUI(val As UInteger)
        Console.WriteLine("UInteger: {0}", val)
    End Sub
    Sub PrintResultLo(val As Long)
        Console.WriteLine("Long: {0}", val)
    End Sub
    Sub PrintResultUL(val As ULong)
        Console.WriteLine("ULong: {0}", val)
    End Sub
    Sub PrintResultDe(val As Decimal)
        Console.WriteLine("Decimal: {0}", val)
    End Sub
    Sub PrintResultSi(val As Single)
        Console.WriteLine("Single: {0}", val)
    End Sub
    Sub PrintResultDo(val As Double)
        Console.WriteLine("Double: {0}", val)
    End Sub
    Sub PrintResultDa(val As Date)
        Console.WriteLine("Date: {0}", val.ToString("M/d/yyyy h:mm:ss tt", cul))
    End Sub
    Sub PrintResultCh(val As Char)
        Console.WriteLine("Char: {0}", val)
    End Sub
    Sub PrintResultSZCh(val As Char())
        Console.WriteLine("Char(): {0}", New String(val))
    End Sub
    Sub PrintResultSt(val As String)
        Console.WriteLine("String: {0}", val)
    End Sub
    Sub PrintResultOb(val As Object)
        Console.WriteLine("Object: {0}", val)
    End Sub
    Sub PrintResultGuid(val As System.Guid)
        Console.WriteLine("Guid: {0}", val)
    End Sub
    Sub PrintResultIComparable(val As IComparable)
        Console.WriteLine("IComparable: {0}", val)
    End Sub
    Sub PrintResultValueType(val As ValueType)
        Dim pval = val.ToString()
        If val.GetType() Is GetType(System.DateTime) Then
            pval = DirectCast(val, DateTime).ToString("M/d/yyyy h:mm:ss tt", cul)
        End If
        Console.WriteLine("ValueType: {0}", pval)
    End Sub
    Sub PrintResultIEOfChar(val As IEnumerable(Of Char))
        Console.WriteLine("IEnumerable(Of Char): {0}", val)
    End Sub
    Sub PrintResultICOfChar(val As ICollection(Of Char))
        Console.WriteLine("ICollection(Of Char): {0}", val)
    End Sub
    Sub PrintResultIListOfChar(val As IList(Of Char))
        Console.WriteLine("IList(Of Char): {0}", val)
    End Sub
End Module

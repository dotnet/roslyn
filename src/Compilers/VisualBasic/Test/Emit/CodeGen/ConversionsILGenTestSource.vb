' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict Off
Imports System
Imports System.Collections.Generic
Imports System.Globalization

Module Module1

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
        Dim Tc As System.TypeCode
        Dim Da As Date
        Dim Ch As Char
        Dim guidVal As Guid
        Dim IEnumerableString As IEnumerable(Of Char)

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

        guidVal = New Guid()
        ObGuid = guidVal
        IEnumerableString = St

        System.Console.WriteLine("Conversions to Boolean:")
        PrintResultBo(SB)
        PrintResultBo(By)
        PrintResultBo(Sh)
        PrintResultBo(US)
        PrintResultBo([In])
        PrintResultBo(UI)
        PrintResultBo(Lo)
        PrintResultBo(UL)
        PrintResultBo(Si)
        PrintResultBo([Do])
        PrintResultBo(De)

        PrintResultBo(SBZero)
        PrintResultBo(ByZero)
        PrintResultBo(ShZero)
        PrintResultBo(USZero)
        PrintResultBo([InZero])
        PrintResultBo(UIZero)
        PrintResultBo(LoZero)
        PrintResultBo(ULZero)
        PrintResultBo(SiZero)
        PrintResultBo([DoZero])
        PrintResultBo(DeZero)

        System.Console.WriteLine()
        System.Console.WriteLine("Conversions from Boolean:")
        PrintResultSB(BoFalse)
        PrintResultBy(BoFalse)
        PrintResultSh(BoFalse)
        PrintResultUs(BoFalse)
        PrintResultIn(BoFalse)
        PrintResultUI(BoFalse)
        PrintResultLo(BoFalse)
        PrintResultUL(BoFalse)
        PrintResultSi(BoFalse)
        PrintResultDo(BoFalse)
        PrintResultDe(BoFalse)

        PrintResultSB(BoTrue)
        PrintResultBy(BoTrue)
        PrintResultSh(BoTrue)
        PrintResultUs(BoTrue)
        PrintResultIn(BoTrue)
        PrintResultUI(BoTrue)
        PrintResultLo(BoTrue)
        PrintResultUL(BoTrue)
        PrintResultSi(BoTrue)
        PrintResultDo(BoTrue)
        PrintResultDe(BoTrue)

        System.Console.WriteLine()
        System.Console.WriteLine("Conversions between numeric types:")

        PrintResultSB(SB)
        PrintResultSB(By)
        PrintResultSB(Sh)
        PrintResultSB(US)
        PrintResultSB([In])
        PrintResultSB(UI)
        PrintResultSB(Lo)
        PrintResultSB(UL)
        PrintResultSB(Si)
        PrintResultSB([Do])
        PrintResultSB(2.5F)
        PrintResultSB(3.5R)
        PrintResultSB(De)

        PrintResultBy(SB)
        PrintResultBy(By)
        PrintResultBy(Sh)
        PrintResultBy(US)
        PrintResultBy([In])
        PrintResultBy(UI)
        PrintResultBy(Lo)
        PrintResultBy(UL)
        PrintResultBy(Si)
        PrintResultBy([Do])
        PrintResultBy(2.5F)
        PrintResultBy(3.5R)
        PrintResultBy(De)

        PrintResultSh(SB)
        PrintResultSh(By)
        PrintResultSh(Sh)
        PrintResultSh(US)
        PrintResultSh([In])
        PrintResultSh(UI)
        PrintResultSh(Lo)
        PrintResultSh(UL)
        PrintResultSh(Si)
        PrintResultSh([Do])
        PrintResultSh(2.5F)
        PrintResultSh(3.5R)
        PrintResultSh(De)

        PrintResultUS(SB)
        PrintResultUS(By)
        PrintResultUS(Sh)
        PrintResultUS(US)
        PrintResultUS([In])
        PrintResultUS(UI)
        PrintResultUS(Lo)
        PrintResultUS(UL)
        PrintResultUS(Si)
        PrintResultUS([Do])
        PrintResultUS(2.5F)
        PrintResultUS(3.5R)
        PrintResultUS(De)

        PrintResultIn(SB)
        PrintResultIn(By)
        PrintResultIn(Sh)
        PrintResultIn(US)
        PrintResultIn([In])
        PrintResultIn(UI)
        PrintResultIn(Lo)
        PrintResultIn(UL)
        PrintResultIn(Si)
        PrintResultIn([Do])
        PrintResultIn(2.5F)
        PrintResultIn(3.5R)
        PrintResultIn(De)

        PrintResultUI(SB)
        PrintResultUI(By)
        PrintResultUI(Sh)
        PrintResultUI(US)
        PrintResultUI([In])
        PrintResultUI(UI)
        PrintResultUI(Lo)
        PrintResultUI(UL)
        PrintResultUI(Si)
        PrintResultUI([Do])
        PrintResultUI(2.5F)
        PrintResultUI(3.5R)
        PrintResultUI(De)

        PrintResultLo(SB)
        PrintResultLo(By)
        PrintResultLo(Sh)
        PrintResultLo(US)
        PrintResultLo([In])
        PrintResultLo(UI)
        PrintResultLo(Lo)
        PrintResultLo(UL)
        PrintResultLo(Si)
        PrintResultLo([Do])
        PrintResultLo(2.5F)
        PrintResultLo(3.5R)
        PrintResultLo(De)

        PrintResultUL(SB)
        PrintResultUL(By)
        PrintResultUL(Sh)
        PrintResultUL(US)
        PrintResultUL([In])
        PrintResultUL(UI)
        PrintResultUL(Lo)
        PrintResultUL(UL)
        PrintResultUL(Si)
        PrintResultUL([Do])
        PrintResultUL(2.5F)
        PrintResultUL(3.5R)
        PrintResultUL(De)

        PrintResultSi(SB)
        PrintResultSi(By)
        PrintResultSi(Sh)
        PrintResultSi(US)
        PrintResultSi([In])
        PrintResultSi(UI)
        PrintResultSi(Lo)
        PrintResultSi(UL)
        PrintResultSi(Si)
        PrintResultSi([Do])
        PrintResultSi(2.5F)
        PrintResultSi(3.5R)
        PrintResultSi(De)

        PrintResultDo(SB)
        PrintResultDo(By)
        PrintResultDo(Sh)
        PrintResultDo(US)
        PrintResultDo([In])
        PrintResultDo(UI)
        PrintResultDo(Lo)
        PrintResultDo(UL)
        PrintResultDo(Si)
        PrintResultDo([Do])
        PrintResultDo(2.5F)
        PrintResultDo(3.5R)
        PrintResultDo(De)

        PrintResultDe(SB)
        PrintResultDe(By)
        PrintResultDe(Sh)
        PrintResultDe(US)
        PrintResultDe([In])
        PrintResultDe(UI)
        PrintResultDe(Lo)
        PrintResultDe(UL)
        PrintResultDe(Si)
        PrintResultDe([Do])
        PrintResultDe(2.5F)
        PrintResultDe(3.5R)
        PrintResultDe(De)

        System.Console.WriteLine()
        System.Console.WriteLine("Conversions to String:")

        PrintResultSt(BoFalse)
        PrintResultSt(BoTrue)
        PrintResultSt(SB)
        PrintResultSt(By)
        PrintResultSt(Sh)
        PrintResultSt(US)
        PrintResultSt([In])
        PrintResultSt(UI)
        PrintResultSt(Lo)
        PrintResultSt(UL)
        PrintResultSt(Si)
        PrintResultSt([Do])
        PrintResultSt(De)
        PrintResultSt(Da.ToString("h:mm:ss tt", cul))
        PrintResultSt(Ch)
        PrintResultSt(Ob)
        PrintResultSt(St.ToCharArray())

        System.Console.WriteLine()
        System.Console.WriteLine("Conversions from String:")

        PrintResultBo("False")
        PrintResultBo("True")
        PrintResultSB(St)
        PrintResultBy(St)
        PrintResultSh(St)
        PrintResultUs(St)
        PrintResultIn(St)
        PrintResultUI(St)
        PrintResultLo(St)
        PrintResultUL(St)
        PrintResultSi(St)
        PrintResultDo(St)
        PrintResultDe(St)
        PrintResultDa(StDate)
        PrintResultCh(St)
        PrintResultSZCh(St)

        System.Console.WriteLine()
        System.Console.WriteLine("Conversions from Object:")

        PrintResultBo(Ob)
        PrintResultSB(Ob)
        PrintResultBy(Ob)
        PrintResultSh(Ob)
        PrintResultUs(Ob)
        PrintResultIn(Ob)
        PrintResultUI(Ob)
        PrintResultLo(Ob)
        PrintResultUL(Ob)
        PrintResultSi(Ob)
        PrintResultDo(Ob)
        PrintResultDe(Ob)
        PrintResultDa(ObDate)
        PrintResultCh(ObChar)
        PrintResultSZCh(ObString)
        ' The string return will change based on system locale
        ' Apply ToString("h:mm:ss tt", cul) on ObData defeat the purpose of this scenario
        'PrintResultSt(GenericParamTestHelperOfString.ObjectToT(ObDate))

    End Sub

    Dim cul As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
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
        System.Console.WriteLine("Decimal: {0}", val.ToString(cul))
    End Sub
    Sub PrintResultSi(val As Single)
        System.Console.WriteLine("Single: {0}", val.ToString(cul))
    End Sub
    Sub PrintResultDo(val As Double)
        System.Console.WriteLine("Double: {0}", val.ToString(cul))
    End Sub
    Sub PrintResultDa(val As Date)
        System.Console.WriteLine("Date: {0}", val.ToString("M/d/yyyy h:mm:ss tt", cul))
    End Sub
    Sub PrintResultCh(val As Char)
        System.Console.WriteLine("Char: {0}", val)
    End Sub
    Sub PrintResultSZCh(val As Char())
        System.Console.WriteLine("Char(): {0}", New String(val))
    End Sub
    Sub PrintResultSt(val As String)
        System.Console.WriteLine("String: {0}", val)
    End Sub
    Sub PrintResultOb(val As Object)
        System.Console.WriteLine("Object: {0}", val)
    End Sub
    Sub PrintResultGuid(val As System.Guid)
        System.Console.WriteLine("Guid: {0}", val)
    End Sub
    Sub PrintResultIComparable(val As IComparable)
        System.Console.WriteLine("IComparable: {0}", val)
    End Sub
    Sub PrintResultValueType(val As ValueType)
        System.Console.WriteLine("ValueType: {0}", val)
    End Sub
End Module

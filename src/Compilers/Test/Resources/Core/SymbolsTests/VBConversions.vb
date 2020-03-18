' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


' vbc /t:library /optionstrict- /vbruntime* /nowarn VBConversions.vb

Option Explicit On

MustInherit Class TestBase(Of TT1, TT2, TT3, TT4, TT5, TT6, TT7, TT8, TT9,
                              TT10, TT11, TT12, TT13, TT14, TT15, TT16, TT17, TT18, TT19, TT20, TT21)

#If Not SKIP Then

    MustOverride Sub M4(Of MT1, MT2 As Class,
              MT3 As MT1, MT4 As {Class, MT1}, MT5 As MT2, MT6 As {Class, MT2},
              MT7, MT8 As {MT7, TT1})(
        a As MT1(),
        b As MT2(),
        c As MT3(),
        d As MT4(),
        e As MT5(),
        f As MT6(),
        g As MT7(),
        h As MT8(),
        i As Class8(),
        j As Class9(),
        k As Class11(),
        l As Class8(,),
        m As Class9(,),
        n As Class8()(),
        o As Class9()(),
        p As Interface5(),
        q As Structure1(),
        r As Integer(),
        s As Long(),
        t As Enum1(),
        u As Enum2(),
        v As Enum4(),
        w As System.ValueType(),
        x As Object()
    )


    MustOverride Sub M5(Of MT1 As TT1, MT2 As MT1,
                           MT3 As TT2, MT4 As TT3,
                           MT5 As TT4, MT6 As TT5,
                           MT7 As TT6, MT8 As TT7,
                           MT9 As TT1, MT10 As TT8,
                           MT11 As TT8, MT12 As Class1,
                           MT13 As Class1, MT14,
                           MT15 As {MT14, TT9}, MT16 As TT9,
                           MT17 As TT9)(
        a As MT1(),
        b As MT2(),
        c As MT3(),
        d As MT4(),
        e As MT5(),
        f As MT6(),
        g As MT7(),
        h As MT8(),
        i As MT9(),
        j As Integer(),
        k As UInteger(),
        l As Enum1(),
        m As Structure1(),
        n As MT10(),
        o As MT11(),
        p As Class1(),
        q As MT12(),
        r As MT13(),
        s As System.ValueType(),
        t As MT14(),
        u As MT15(),
        v As MT16(),
        w As MT17()
    )


    MustOverride Sub M7(Of _
        MT1 As TT9,
        MT2 As TT10,
        MT3 As TT11,
        MT4 As TT12,
        MT5 As TT13,
        MT6 As TT14,
        MT7 As TT15,
        MT8 As TT16,
        MT9 As TT17,
        MT10 As TT18,
        MT11 As TT19,
        MT12 As TT15,
        MT13 As TT20,
        MT14 As TT8,
        MT15 As TT21)(
        a As MT1,
        b As MT2,
        c As MT3,
        d As MT4,
        e As MT5,
        f As MT6,
        g As MT7,
        h As MT8,
        i As MT9,
        j As MT10,
        k As MT11,
        l As MT12,
        m As MT13,
        n As MT14,
        o As Interface1,
        p As Object,
        q As System.Enum,
        r As Enum1,
        s As Enum2,
        t As Integer(),
        u As UInteger(),
        v As Class9(),
        w As Structure1,
        x As System.Collections.IEnumerable,
        y As System.Collections.Generic.IList(Of Class9),
        z As MT15
    )


    MustOverride Sub M8(Of MT1, MT2 As Class,
              MT3 As MT1, MT4 As {Class, MT1}, MT5 As MT2, MT6 As {Class, MT2},
              MT7, MT8 As {MT7, TT1})(
        a As MT1,
        b As MT2,
        c As MT3,
        d As MT4,
        e As MT5,
        f As MT6,
        g As MT7,
        h As MT8
    )


    MustOverride Sub M9(Of MT1 As TT1, MT2 As MT1,
                           MT3 As TT2, MT4 As TT3,
                           MT5 As TT4, MT6 As TT5,
                           MT7 As TT6, MT8 As TT7,
                           MT9 As TT1, MT10 As TT8,
                           MT11 As TT8, MT12 As Class1,
                           MT13 As Class1, MT14,
                           MT15 As {MT14, TT9}, MT16 As TT9,
                           MT17 As TT9)(
        a As MT1,
        b As MT2,
        c As MT3,
        d As MT4,
        e As MT5,
        f As MT6,
        g As MT7,
        h As MT8,
        i As MT9,
        j As Integer,
        k As UInteger,
        l As Enum1,
        m As Structure1,
        n As MT10,
        o As MT11,
        p As Class1,
        q As MT12,
        r As MT13,
        s As System.ValueType,
        t As MT14,
        u As MT15,
        v As MT16,
        w As MT17
    )

#End If

End Class

Class Test
    Inherits TestBase(Of Integer, UInteger, Long, Enum1, Enum2, Enum4, Enum5, Structure1, System.ValueType, 
                      System.Object, System.Enum, Enum1, Enum2, Enum4, Integer(), UInteger(), Class8(), Class9(), 
                      Class10(), Class11, Class9(,))

#If Not SKIP Then

    Sub M1(
         a As Class1,
         b As Class1,
         c As Class2,
         d As Class1(),
         e As Class1(),
         f As Class2(),
         g As Class2.Class3(Of Integer),
         h As Class2.Class3(Of Integer),
         i As Class2.Class3(Of Byte),
         j As Class4(Of Integer),
         k As Class4(Of Integer),
         l As Class4(Of Byte),
         m As Class4(Of Integer).Class5(Of Integer),
         n As Class4(Of Integer).Class5(Of Integer),
         o As Class4(Of Byte).Class5(Of Integer),
         p As Class4(Of Integer).Class5(Of Byte),
         q As Class4(Of Integer).Class6,
         r As Class4(Of Integer).Class6,
         s As Class4(Of Byte).Class6,
         t As Class4(Of Integer).Class6.Class7(Of Integer),
         u As Class4(Of Integer).Class6.Class7(Of Integer),
         v As Class4(Of Byte).Class6.Class7(Of Integer),
         w As Class4(Of Integer).Class6.Class7(Of Byte)
    )
        ' Identity
        a = b
        'a = c 'error BC30311: Value of type 'Class2' cannot be converted to 'Class1'.
        'a = d 'error BC30311: Value of type '1-dimensional array of Class1' cannot be converted to 'Class1'. 
        d = e
        'd = f 'error BC30332: Value of type '1-dimensional array of Class2' cannot be converted to '1-dimensional array of Class1' because 'Class2' is not derived from 'Class1'. 
        g = h
        'g = i 'error BC30311: Value of type 'Class2.Class3(Of Byte)' cannot be converted to 'Class2.Class3(Of Integer)'.
        'g = j 'error BC30311: Value of type 'Class4(Of Integer)' cannot be converted to 'Class2.Class3(Of Integer)'.
        j = k
        'j = l 'error BC30311: Value of type 'Class4(Of Byte)' cannot be converted to 'Class4(Of Integer)'. 
        m = n
        'm = o 'error BC30311: Value of type 'Class4(Of Byte).Class5(Of Integer)' cannot be converted to 'Class4(Of Integer).Class5(Of Integer)'.
        'm = p 'error BC30311: Value of type 'Class4(Of Integer).Class5(Of Byte)' cannot be converted to 'Class4(Of Integer).Class5(Of Integer)'. 
        q = r
        'q = s 'error BC30311: Value of type 'Class4(Of Byte).Class6' cannot be converted to 'Class4(Of Integer).Class6'.
        t = u
        't = v 'error BC30311: Value of type 'Class4(Of Byte).Class6.Class7(Of Integer)' cannot be converted to 'Class4(Of Integer).Class6.Class7(Of Integer)'. 
        't = w 'error BC30311: Value of type 'Class4(Of Integer).Class6.Class7(Of Byte)' cannot be converted to 'Class4(Of Integer).Class6.Class7(Of Integer)'. 
    End Sub

    Sub M2(
          a As Enum1,
          b As Enum1,
          c As Enum2,
          d As Enum3,
          e As Integer,
          f As Long,
          g As Short,
          h As Enum4
    )
        a = b
        a = c 'error BC30512: Option Strict On disallows implicit conversions from 'Enum2' to 'Enum1'.
        a = d 'error BC30512: Option Strict On disallows implicit conversions from 'Enum3' to 'Enum1'.
        a = e 'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Enum1'.
        a = f 'error BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Enum1'.
        a = g 'error BC30512: Option Strict On disallows implicit conversions from 'Short' to 'Enum1'.
        a = h 'error BC30512: Option Strict On disallows implicit conversions from 'Enum4' to 'Enum1'.
        e = a
        e = c 'error BC30512: Option Strict On disallows implicit conversions from 'Enum2' to 'Integer'.
        e = d
        f = a
        f = c
        f = d
        g = a 'error BC30512: Option Strict On disallows implicit conversions from 'Enum1' to 'Short'.
        g = c 'error BC30512: Option Strict On disallows implicit conversions from 'Enum2' to 'Short'.
        g = d
    End Sub


    Sub M3(
          a As Object,
          b As Class8,
          c As Class9,
          d As Class10,
          e As Class11,
          f As System.Array,
          g As Integer(),
          h As Integer(,),
          i As Interface1,
          j As Interface2,
          k As Interface3,
          l As Interface4,
          m As Interface5,
          n As Interface6,
          o As Interface7,
          p As System.Collections.IEnumerable,
          q As System.Collections.Generic.IList(Of Integer),
          r As System.Collections.Generic.ICollection(Of Integer),
          s As System.Collections.Generic.IEnumerable(Of Integer),
          t As System.Collections.Generic.IList(Of Long),
          u As Class9(),
          v As System.Collections.Generic.IList(Of Class8),
          w As System.Collections.Generic.IList(Of Class11),
          x As System.Action
    )
        '§8.8 Widening Conversions
        '•	From a reference type to a base type.
        '§8.9 Narrowing Conversions
        '•	From a reference type to a more derived type.

        a = a
        a = d
        b = b
        b = c
        b = d
        c = d
        d = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Class10'.
        c = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Class9'.
        d = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Class10'.
        d = c 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Class10'.
        'c = e 'error BC30311: Value of type 'Class11' cannot be converted to 'Class9'.
        'e = c 'error BC30311: Value of type 'Class9' cannot be converted to 'Class11'.

        a = g
        f = g
        a = h
        f = h
        g = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to '1-dimensional array of Integer'.
        g = f 'error BC30512: Option Strict On disallows implicit conversions from 'System.Array' to '1-dimensional array of Integer'.
        h = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to '2-dimensional array of Integer'.
        h = f 'error BC30512: Option Strict On disallows implicit conversions from 'System.Array' to '2-dimensional array of Integer'.

        'Add test for Void
        ' void <-> void
        ' object <-> void

        '§8.8 Widening Conversions
        '•	From a reference type to an interface type, provided that the type 
        '   implements the interface or a variant compatible interface.
        '§8.9 Narrowing Conversions
        '•	From a class type to an interface type, provided the class type does not implement 
        '   the interface type or an interface type variant compatible with it.
        '•	From an interface type to another interface type, provided there is no inheritance 
        '   relationship between the two types and provided they are not variant compatible.

        i = d
        j = d
        k = d
        l = c
        l = d
        m = b
        m = c
        m = d
        n = d
        p = g
        p = h
        q = g
        r = g
        s = g
        v = u
        i = i
        i = j
        i = k
        i = o
        n = o

        i = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface1'.
        i = c 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface1'.
        i = e 'error BC30512: Option Strict On disallows implicit conversions from 'Class11' to 'Interface1'.
        'warning BC42322: Runtime errors might occur when converting 'Class11' to 'Interface1'.
        j = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface2'.
        j = c 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface2'.
        k = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface3'.
        k = c 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface3'.
        l = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface4'.
        n = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface6'.
        n = c 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface6'.
        o = b 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface7'.
        o = c 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface7'.
        o = d 'error BC30512: Option Strict On disallows implicit conversions from 'Class10' to 'Interface7'.
        'q = h 'error BC30311: Value of type '2-dimensional array of Integer' cannot be converted to 'System.Collections.Generic.IList(Of Integer)'.
        'r = h 'error BC30311: Value of type '2-dimensional array of Integer' cannot be converted to 'System.Collections.Generic.ICollection(Of Integer)'.
        's = h 'error BC30311: Value of type '2-dimensional array of Integer' cannot be converted to 'System.Collections.Generic.IEnumerable(Of Integer)'.
        t = g 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to 'System.Collections.Generic.IList(Of Long)'.
        w = u 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class9' to 'System.Collections.Generic.IList(Of Class11)'.
        i = l 'error BC30512: Option Strict On disallows implicit conversions from 'Interface4' to 'Interface1'.
        o = x 'error BC30512: Option Strict On disallows implicit conversions from 'System.Action' to 'Interface7'.
        'warning BC42322: Runtime errors might occur when converting 'System.Action' to 'Interface7'.

        '§8.8 Widening Conversions
        '•	From an interface type to Object.
        '§8.9 Narrowing Conversions
        '•	From an interface type to a class type. 

        a = o
        x = o 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'System.Action'.
        'warning BC42322: Runtime errors might occur when converting 'Interface7' to 'System.Action'.
        e = o 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'Class11'.
        'warning BC42322: Runtime errors might occur when converting 'Interface7' to 'Class11'.
        'g = o 'error BC30311: Value of type 'Interface7' cannot be converted to '1-dimensional array of Integer'.
        'h = o 'error BC30311: Value of type 'Interface7' cannot be converted to '2-dimensional array of Integer'.
        'u = o 'error BC30311: Value of type 'Interface7' cannot be converted to '1-dimensional array of Class9'.
        g = p 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.IEnumerable' to '1-dimensional array of Integer'.
        h = p 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.IEnumerable' to '2-dimensional array of Integer'.
        g = q 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Integer)' to '1-dimensional array of Integer'.
        'h = q 'error BC30311: Value of type 'System.Collections.Generic.IList(Of Integer)' cannot be converted to '2-dimensional array of Integer'.
        g = t 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Long)' to '1-dimensional array of Integer'.
        'h = t 'error BC30311: Value of type 'System.Collections.Generic.IList(Of Long)' cannot be converted to '2-dimensional array of Integer'.
        g = w 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class11)' to '1-dimensional array of Integer'.
        'h = w 'error BC30311: Value of type 'System.Collections.Generic.IList(Of Class11)' cannot be converted to '2-dimensional array of Integer'.

        'Add tests for Module2
        ' Module2 <-> Interface
        ' Module2 <-> Object

        '§8.8 Widening Conversions
        '•	From an interface type to a variant compatible interface type.
        '•	From a delegate type to a variant compatible delegate type.

    End Sub


    Overrides Sub M4(Of MT1, MT2 As Class,
              MT3 As MT1, MT4 As {Class, MT1}, MT5 As MT2, MT6 As {Class, MT2},
              MT7, MT8 As {MT7, Integer})(
        a As MT1(),
        b As MT2(),
        c As MT3(),
        d As MT4(),
        e As MT5(),
        f As MT6(),
        g As MT7(),
        h As MT8(),
        i As Class8(),
        j As Class9(),
        k As Class11(),
        l As Class8(,),
        m As Class9(,),
        n As Class8()(),
        o As Class9()(),
        p As Interface5(),
        q As Structure1(),
        r As Integer(),
        s As Long(),
        t As Enum1(),
        u As Enum2(),
        v As Enum4(),
        w As System.ValueType(),
        x As Object()
    )
        '§8.8 Widening Conversions
        '•	From an array type S with an element type SE to an array type T with an element type TE, provided all of the following are true:
        '   •	S and T differ only in element type.
        '   •	Both SE and TE are reference types or are type parameters known to be a reference type.
        '   •	A widening reference, array, or type parameter conversion exists from SE to TE.
        '§8.9 Narrowing Conversions
        '•	From an array type S with an element type SE, to an array type T with an element type TE, 
        '   provided that all of the following are true:
        '   •	S and T differ only in element type.
        '   •	Both SE and TE are reference types or are type parameters not known to be value types.
        '   •	A narrowing reference, array, or type parameter conversion exists from SE to TE.

        a = a
        l = l
        n = n
        a = d
        b = f
        i = j
        i = k
        l = m
        n = o
        p = i
        x = i
        x = w
        a = c 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT3' to '1-dimensional array of MT1'.
        c = a 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT3'.
        d = a 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT4'.
        b = e 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT5' to '1-dimensional array of MT2'.
        e = b 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT2' to '1-dimensional array of MT5'.
        f = b 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT2' to '1-dimensional array of MT6'.
        g = h 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT8' to '1-dimensional array of MT7'.
        h = g 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT7' to '1-dimensional array of MT8'.
        j = i 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class8' to '1-dimensional array of Class9'.
        k = i 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class8' to '1-dimensional array of Class11'.
        m = l 'error BC30512: Option Strict On disallows implicit conversions from '2-dimensional array of Class8' to '2-dimensional array of Class9'.
        o = n 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of 1-dimensional array of Class8' to '1-dimensional array of 1-dimensional array of Class9'.
        i = p 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Interface5' to '1-dimensional array of Class8'.
        i = x 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Object' to '1-dimensional array of Class8'.
        w = x 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Object' to '1-dimensional array of System.ValueType'.
        'a = b 'error BC30332: Value of type '1-dimensional array of MT2' cannot be converted to '1-dimensional array of MT1' because 'MT2' is not derived from 'MT1'.
        'b = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT2' because 'MT1' is not derived from 'MT2'.
        'b = d 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of MT2' because 'MT4' is not derived from 'MT2'.
        'd = b 'error BC30332: Value of type '1-dimensional array of MT2' cannot be converted to '1-dimensional array of MT4' because 'MT2' is not derived from 'MT4'.
        'a = g 'error BC30332: Value of type '1-dimensional array of MT7' cannot be converted to '1-dimensional array of MT1' because 'MT7' is not derived from 'MT1'.
        'g = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT7' because 'MT1' is not derived from 'MT7'.
        'j = k 'error BC30332: Value of type '1-dimensional array of Class11' cannot be converted to '1-dimensional array of Class9' because 'Class11' is not derived from 'Class9'.
        'i = l 'error BC30414: Value of type '2-dimensional array of Class8' cannot be converted to '1-dimensional array of Class8' because the array types have different numbers of dimensions.
        'l = i 'error BC30414: Value of type '1-dimensional array of Class8' cannot be converted to '2-dimensional array of Class8' because the array types have different numbers of dimensions.
        'l = n 'error BC30332: Value of type '1-dimensional array of 1-dimensional array of Class8' cannot be converted to '2-dimensional array of Class8' because '1-dimensional array of Class8' is not derived from 'Class8'.
        'n = l 'error BC30332: Value of type '2-dimensional array of Class8' cannot be converted to '1-dimensional array of 1-dimensional array of Class8' because 'Class8' is not derived from '1-dimensional array of Class8'.
        'p = q 'error BC30332: Value of type '1-dimensional array of Structure1' cannot be converted to '1-dimensional array of Interface5' because 'Structure1' is not derived from 'Interface5'.
        'q = p 'error BC30332: Value of type '1-dimensional array of Interface5' cannot be converted to '1-dimensional array of Structure1' because 'Interface5' is not derived from 'Structure1'.
        'q = w 'error BC30332: Value of type '1-dimensional array of System.ValueType' cannot be converted to '1-dimensional array of Structure1' because 'System.ValueType' is not derived from 'Structure1'.
        'w = q 'error BC30333: Value of type '1-dimensional array of Structure1' cannot be converted to '1-dimensional array of System.ValueType' because 'Structure1' is not a reference type.


        '§8.8 Widening Conversions
        '•	From an array type S with an enumerated element type SE to an array type T with an element type TE, 
        '   provided all of the following are true:
        '   •	S and T differ only in element type.
        '   •	TE is the underlying type of SE.
        '§8.9 Narrowing Conversions
        '•	From an array type S with an element type SE to an array type T with an enumerated element type TE, 
        '   provided all of the following are true:
        '   •	S and T differ only in element type.
        '   •	SE is the underlying type of TE.

        r = t
        s = u
        t = r 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to '1-dimensional array of Enum1'.
        u = s 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Long' to '1-dimensional array of Enum2'.
        t = v 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum4' to '1-dimensional array of Enum1'.
        v = t 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum1' to '1-dimensional array of Enum4'.
        'r = s 'error BC30332: Value of type '1-dimensional array of Long' cannot be converted to '1-dimensional array of Integer' because 'Long' is not derived from 'Integer'.
        's = r 'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of Long' because 'Integer' is not derived from 'Long'.
        'r = u 'error BC30332: Value of type '1-dimensional array of Enum2' cannot be converted to '1-dimensional array of Integer' because 'Enum2' is not derived from 'Integer'.
        'u = r 'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of Enum2' because 'Integer' is not derived from 'Enum2'.
        't = u 'error BC30332: Value of type '1-dimensional array of Enum2' cannot be converted to '1-dimensional array of Enum1' because 'Enum2' is not derived from 'Enum1'.
        'u = t 'error BC30332: Value of type '1-dimensional array of Enum1' cannot be converted to '1-dimensional array of Enum2' because 'Enum1' is not derived from 'Enum2'.

    End Sub


    Public Overloads Overrides Sub M5(Of _
        MT1 As Integer,
        MT2 As MT1,
        MT3 As UInteger,
        MT4 As Long,
        MT5 As Enum1,
        MT6 As Enum2,
        MT7 As Enum4,
        MT8 As Enum5,
        MT9 As Integer,
        MT10 As Structure1,
        MT11 As Structure1,
        MT12 As Class1,
        MT13 As Class1,
        MT14,
        MT15 As {MT14, System.ValueType}, MT16 As System.ValueType,
        MT17 As System.ValueType)(
        a() As MT1,
        b() As MT2,
        c() As MT3,
        d() As MT4,
        e() As MT5,
        f() As MT6,
        g() As MT7,
        h() As MT8,
        i() As MT9,
        j As Integer(),
        k As UInteger(),
        l As Enum1(),
        m As Structure1(),
        n As MT10(),
        o As MT11(),
        p As Class1(),
        q As MT12(),
        r As MT13(),
        s As System.ValueType(),
        t As MT14(),
        u As MT15(),
        v As MT16(),
        w As MT17()
    )
        a = b
        b = a ' error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT2'.

        j = a
        j = e
        l = e
        a = e 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT5' to '1-dimensional array of MT1'.
        e = a 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT5'.
        e = g 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT7' to '1-dimensional array of MT5'.
        g = e 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT5' to '1-dimensional array of MT7'.
        a = j 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to '1-dimensional array of MT1'.
        a = l 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum1' to '1-dimensional array of MT1'.
        l = a 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of Enum1'.
        e = j 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to '1-dimensional array of MT5'.
        e = l 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum1' to '1-dimensional array of MT5'.
        'a = i 'error BC30332: Value of type '1-dimensional array of MT9' cannot be converted to '1-dimensional array of MT1' because 'MT9' is not derived from 'MT1'.
        'a = c 'error BC30332: Value of type '1-dimensional array of MT3' cannot be converted to '1-dimensional array of MT1' because 'MT3' is not derived from 'MT1'.
        'c = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT3' because 'MT1' is not derived from 'MT3'.
        'a = d 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of MT1' because 'MT4' is not derived from 'MT1'.
        'd = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT4' because 'MT1' is not derived from 'MT4'.
        'a = f 'error BC30332: Value of type '1-dimensional array of MT6' cannot be converted to '1-dimensional array of MT1' because 'MT6' is not derived from 'MT1'.
        'f = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT6' because 'MT1' is not derived from 'MT6'.
        'a = h 'error BC30332: Value of type '1-dimensional array of MT8' cannot be converted to '1-dimensional array of MT1' because 'MT8' is not derived from 'MT1'.
        'h = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT8' because 'MT1' is not derived from 'MT8'.
        'e = f 'error BC30332: Value of type '1-dimensional array of MT6' cannot be converted to '1-dimensional array of MT5' because 'MT6' is not derived from 'MT5'.
        'f = e 'error BC30332: Value of type '1-dimensional array of MT5' cannot be converted to '1-dimensional array of MT6' because 'MT5' is not derived from 'MT6'.
        'e = h 'error BC30332: Value of type '1-dimensional array of MT8' cannot be converted to '1-dimensional array of MT5' because 'MT8' is not derived from 'MT5'.
        'h = e 'error BC30332: Value of type '1-dimensional array of MT5' cannot be converted to '1-dimensional array of MT8' because 'MT5' is not derived from 'MT8'.
        'a = k 'error BC30332: Value of type '1-dimensional array of UInteger' cannot be converted to '1-dimensional array of MT1' because 'UInteger' is not derived from 'MT1'.
        'k = a 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of UInteger' because 'MT1' is not derived from 'UInteger'.
        'e = k 'error BC30332: Value of type '1-dimensional array of UInteger' cannot be converted to '1-dimensional array of MT5' because 'UInteger' is not derived from 'MT5'.
        'k = e 'error BC30332: Value of type '1-dimensional array of MT5' cannot be converted to '1-dimensional array of UInteger' because 'MT5' is not derived from 'UInteger'.
        'j = k 'error BC30332: Value of type '1-dimensional array of UInteger' cannot be converted to '1-dimensional array of Integer' because 'UInteger' is not derived from 'Integer'.
        'k = j 'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of UInteger' because 'Integer' is not derived from 'UInteger'.

        m = n
        p = q
        n = m 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Structure1' to '1-dimensional array of MT10'.
        q = p 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class1' to '1-dimensional array of MT12'.
        s = u 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT15' to '1-dimensional array of System.ValueType'.
        u = s 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of System.ValueType' to '1-dimensional array of MT15'.
        t = u 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT15' to '1-dimensional array of MT14'.
        u = t 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT14' to '1-dimensional array of MT15'.
        s = v 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT16' to '1-dimensional array of System.ValueType'.
        v = s 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of System.ValueType' to '1-dimensional array of MT16'.
        'n = o 'error BC30332: Value of type '1-dimensional array of MT11' cannot be converted to '1-dimensional array of MT10' because 'MT11' is not derived from 'MT10'.
        'o = n 'error BC30332: Value of type '1-dimensional array of MT10' cannot be converted to '1-dimensional array of MT11' because 'MT10' is not derived from 'MT11'.
        'q = r 'error BC30332: Value of type '1-dimensional array of MT13' cannot be converted to '1-dimensional array of MT12' because 'MT13' is not derived from 'MT12'.
        'r = q 'error BC30332: Value of type '1-dimensional array of MT12' cannot be converted to '1-dimensional array of MT13' because 'MT12' is not derived from 'MT13'.
        'v = w 'error BC30332: Value of type '1-dimensional array of MT17' cannot be converted to '1-dimensional array of MT16' because 'MT17' is not derived from 'MT16'.
        'w = v 'error BC30332: Value of type '1-dimensional array of MT16' cannot be converted to '1-dimensional array of MT17' because 'MT16' is not derived from 'MT17'.
    End Sub


    Sub M6(Of _
        MT1,
        MT2 As Structure,
        MT3 As Class,
        MT4 As Interface3,
        MT5 As Interface3,
        MT6 As {Class, Interface3},
        MT7 As {Class, Interface3},
        MT8 As Class10,
        MT9 As Class12,
        MT10 As MT1,
        MT11 As MT10,
        MT12 As Class9,
        MT13 As MT12,
        MT14 As MT1)(
        a As Object,
        b As MT1,
        c As MT2,
        d As MT3,
        e As Interface3,
        f As MT4,
        g As MT5,
        h As MT6,
        i As MT7,
        j As Interface1,
        k As MT8,
        l As Class10,
        m As Class8,
        n As Class12,
        o As MT9,
        p As System.ValueType,
        q As MT10,
        r As MT11,
        s As MT13,
        t As Class9,
        u As Interface7,
        v As MT14
    )
        '§8.8 Widening Conversions
        '•	From a type parameter to Object.
        '§8.9 Narrowing Conversions
        '•	From Object to a type parameter.
        a = b
        a = c
        a = d
        b = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT1'.
        c = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT2'.
        d = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT3'.

        '§8.8 Widening Conversions
        '•	From a type parameter to an interface type constraint or any interface variant compatible with an interface type constraint.
        e = f
        e = h
        f = e 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'MT4'.
        h = e 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'MT6'.
        'f = g 'error BC30311: Value of type 'MT5' cannot be converted to 'MT4'.
        'g = f 'error BC30311: Value of type 'MT4' cannot be converted to 'MT5'.
        'h = i 'error BC30311: Value of type 'MT7' cannot be converted to 'MT6'.
        'i = h 'error BC30311: Value of type 'MT6' cannot be converted to 'MT7'.

        '§8.8 Widening Conversions
        '•	From a type parameter to an interface implemented by a class constraint.
        e = k
        j = k
        j = f
        k = e 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'MT8'.
        k = j 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT8'.
        f = j 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT4'.

        '§8.8 Widening Conversions
        '•	From a type parameter to an interface variant compatible with an interface implemented by a class constraint.

        '§8.8 Widening Conversions
        '•	From a type parameter to a class constraint, or a base type of the class constraint.
        '§8.9 Narrowing Conversions
        '•	From a class constraint, or a base type of the class constraint to a type parameter.
        l = k
        m = k
        p = c
        k = l 'error BC30512: Option Strict On disallows implicit conversions from 'Class10' to 'MT8'.
        k = m 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'MT8'.
        c = p 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'MT2'.
        'n = k 'error BC30311: Value of type 'MT8' cannot be converted to 'Class12'.
        'k = n 'error BC30311: Value of type 'Class12' cannot be converted to 'MT8'.
        'k = o 'error BC30311: Value of type 'MT9' cannot be converted to 'MT8'.
        'o = k 'error BC30311: Value of type 'MT8' cannot be converted to 'MT9'.

        '§8.8 Widening Conversions
        '•	From a type parameter T to a type parameter constraint TX, or anything TX has a widening conversion to.
        '§8.9 Narrowing Conversions
        '•	From a type parameter constraint TX to a type parameter T, or from anything that has narrowing conversion to TX.
        b = q
        b = r
        t = s
        m = s
        q = b 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT10'.
        r = b 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT11'.
        s = t 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'MT13'.
        s = m 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'MT13'.
        'l = s 'error BC30311: Value of type 'MT13' cannot be converted to 'Class10'.
        's = l 'error BC30311: Value of type 'Class10' cannot be converted to 'MT13'.


        '§8.9 Narrowing Conversions
        '•	From a type parameter to an interface type, provided the type parameter is not constrained 
        '   to that interface or constrained to a class that implements that interface.
        u = k 'error BC30512: Option Strict On disallows implicit conversions from 'MT8' to 'Interface7'.
        k = u 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT8'.
        u = f 'error BC30512: Option Strict On disallows implicit conversions from 'MT4' to 'Interface7'.
        f = u 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT4'.


        'TODO: Test with explicit structure constraint or a sealed class constraint

        '§8.9 Narrowing Conversions
        '•	From an interface type to a type parameter.
        u = b 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'Interface7'.
        b = u 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT1'.
        u = c 'error BC30512: Option Strict On disallows implicit conversions from 'MT2' to 'Interface7'.
        c = u 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT2'.


        ' Misc
        'v = q 'error BC30311: Value of type 'MT10' cannot be converted to 'MT14'.
        'q = v 'error BC30311: Value of type 'MT14' cannot be converted to 'MT10'.

    End Sub




    Public Overrides Sub M7(
        Of MT1 As System.ValueType,
            MT2 As Object,
            MT3 As System.Enum,
            MT4 As Enum1,
            MT5 As Enum2,
            MT6 As Enum4,
            MT7 As Integer(),
            MT8 As UInteger(),
            MT9 As Class8(),
            MT10 As Class9(),
            MT11 As Class10(),
            MT12 As Integer(),
            MT13 As Class11,
            MT14 As Structure1,
            MT15 As Class9(,))(
        a As MT1,
        b As MT2,
        c As MT3,
        d As MT4,
        e As MT5,
        f As MT6,
        g As MT7,
        h As MT8,
        i As MT9,
        j As MT10,
        k As MT11,
        l As MT12,
        m As MT13,
        n As MT14,
        o As Interface1,
        p As Object,
        q As System.Enum,
        r As Enum1,
        s As Enum2,
        t As Integer(),
        u As UInteger(),
        v As Class9(),
        w As Structure1,
        x As System.Collections.IEnumerable,
        y As System.Collections.Generic.IList(Of Class9),
        z As MT15
    )
        p = a
        p = b
        q = c
        q = d
        r = d
        t = g
        v = j
        v = k
        w = n
        x = i
        y = j
        y = k
        a = p 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT1'.
        b = p 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT2'.
        c = q 'error BC30512: Option Strict On disallows implicit conversions from 'System.Enum' to 'MT3'.
        d = q 'error BC30512: Option Strict On disallows implicit conversions from 'System.Enum' to 'MT4'.
        d = r 'error BC30512: Option Strict On disallows implicit conversions from 'Enum1' to 'MT4'.
        g = t 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to 'MT7'.
        j = v 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class9' to 'MT10'.
        k = v 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class9' to 'MT11'.
        n = w 'error BC30512: Option Strict On disallows implicit conversions from 'Structure1' to 'MT14'.
        i = x 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.IEnumerable' to 'MT9'.
        y = i 'error BC30512: Option Strict On disallows implicit conversions from 'MT9' to 'System.Collections.Generic.IList(Of Class9)'.
        i = y 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT9'.
        j = y 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT10'
        k = y 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT11'
        y = z 'error BC30512: Option Strict On disallows implicit conversions from 'MT15' to 'System.Collections.Generic.IList(Of Class9)'
        z = y 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT15'
        n = o 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT14'.
        o = n 'error BC30512: Option Strict On disallows implicit conversions from 'MT14' to 'Interface1'.
        m = o 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT13'.
        o = m 'error BC30512: Option Strict On disallows implicit conversions from 'MT13' to 'Interface1'.
        d = o 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT4'.
        o = d 'error BC30512: Option Strict On disallows implicit conversions from 'MT4' to 'Interface1'.
        'r = e 'error BC30311: Value of type 'MT5' cannot be converted to 'Enum1'.
        'e = r 'error BC30311: Value of type 'Enum1' cannot be converted to 'MT5'.
        's = d 'error BC30311: Value of type 'MT4' cannot be converted to 'Enum2'.
        'd = s 'error BC30311: Value of type 'Enum2' cannot be converted to 'MT4'.
        'r = f 'error BC30311: Value of type 'MT6' cannot be converted to 'Enum1'.
        'f = r 'error BC30311: Value of type 'Enum1' cannot be converted to 'MT6'.
        't = h 'error BC30311: Value of type 'MT8' cannot be converted to '1-dimensional array of Integer'.
        'h = t 'error BC30311: Value of type '1-dimensional array of Integer' cannot be converted to 'MT8'.
        'v = i 'error BC30311: Value of type 'MT9' cannot be converted to '1-dimensional array of Class9'.
        'i = v 'error BC30311: Value of type '1-dimensional array of Class9' cannot be converted to 'MT9'.
        'a = b 'error BC30311: Value of type 'MT2' cannot be converted to 'MT1'.
        'b = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT2'.
        'g = h 'error BC30311: Value of type 'MT8' cannot be converted to 'MT7'.
        'h = g 'error BC30311: Value of type 'MT7' cannot be converted to 'MT8'.
        'g = l 'error BC30311: Value of type 'MT12' cannot be converted to 'MT7'.
        'l = g 'error BC30311: Value of type 'MT7' cannot be converted to 'MT12'.
        'c = d 'error BC30311: Value of type 'MT4' cannot be converted to 'MT3'.
        'd = c 'error BC30311: Value of type 'MT3' cannot be converted to 'MT4'.
        'i = j 'error BC30311: Value of type 'MT10' cannot be converted to 'MT9'.
        'j = i 'error BC30311: Value of type 'MT9' cannot be converted to 'MT10'.
        'a = n 'error BC30311: Value of type 'MT14' cannot be converted to 'MT1'.
        'n = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT14'.
        'd = f 'error BC30311: Value of type 'MT6' cannot be converted to 'MT4'.
        'f = d 'error BC30311: Value of type 'MT4' cannot be converted to 'MT6'.

    End Sub



    Public Overrides Sub M8(Of _
                              MT1,
                               MT2 As Class,
                               MT3 As MT1,
                               MT4 As {Class, MT1},
                               MT5 As MT2,
                               MT6 As {Class, MT2},
                               MT7,
                               MT8 As {MT7, Integer})(
        a As MT1,
        b As MT2,
        c As MT3,
        d As MT4,
        e As MT5,
        f As MT6,
        g As MT7,
        h As MT8
    )
        a = a
        a = d
        b = f
        a = c
        b = e
        g = h
        c = a 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT3'.
        d = a 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT4'.
        e = b 'error BC30512: Option Strict On disallows implicit conversions from 'MT2' to 'MT5'.
        f = b 'error BC30512: Option Strict On disallows implicit conversions from 'MT2' to 'MT6'.
        h = g 'error BC30512: Option Strict On disallows implicit conversions from 'MT7' to 'MT8'.
        'a = b 'error BC30311: Value of type 'MT2' cannot be converted to 'MT1'.
        'b = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT2'.
        'b = d 'error BC30311: Value of type 'MT4' cannot be converted to 'MT2'.
        'd = b 'error BC30311: Value of type 'MT2' cannot be converted to 'MT4'.
        'a = g 'error BC30311: Value of type 'MT7' cannot be converted to 'MT1'.
        'g = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT7'.
    End Sub


    Public Overrides Sub M9(Of MT1 As Integer, MT2 As MT1, MT3 As UInteger, MT4 As Long, MT5 As Enum1, MT6 As Enum2, MT7 As Enum4, MT8 As Enum5, MT9 As Integer, MT10 As Structure1, MT11 As Structure1, MT12 As Class1, MT13 As Class1, MT14, MT15 As {MT14, System.ValueType}, MT16 As System.ValueType, MT17 As System.ValueType)(a As MT1, b As MT2, c As MT3, d As MT4, e As MT5, f As MT6, g As MT7, h As MT8, i As MT9, j As Integer, k As UInteger, l As Enum1, m As Structure1, n As MT10, o As MT11, p As Class1, q As MT12, r As MT13, s As System.ValueType, t As MT14, u As MT15, v As MT16, w As MT17)

        a = b
        j = a
        j = e
        l = e
        m = n
        p = q
        s = u
        t = u
        s = v
        b = a 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT2'.
        a = j 'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'MT1'.
        e = j 'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'MT5'.
        e = l 'error BC30512: Option Strict On disallows implicit conversions from 'Enum1' to 'MT5'.
        n = m 'error BC30512: Option Strict On disallows implicit conversions from 'Structure1' to 'MT10'.
        q = p 'error BC30512: Option Strict On disallows implicit conversions from 'Class1' to 'MT12'.
        u = s 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'MT15'.
        u = t 'error BC30512: Option Strict On disallows implicit conversions from 'MT14' to 'MT15'.
        v = s 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'MT16'.
        'a = e 'error BC30311: Value of type 'MT5' cannot be converted to 'MT1'.
        'e = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT5'.
        'e = g 'error BC30311: Value of type 'MT7' cannot be converted to 'MT5'.
        'g = e 'error BC30311: Value of type 'MT5' cannot be converted to 'MT7'.
        'a = l 'error BC30311: Value of type 'Enum1' cannot be converted to 'MT1'.
        'l = a 'error BC30311: Value of type 'MT1' cannot be converted to 'Enum1'.
        'a = i 'error BC30311: Value of type 'MT9' cannot be converted to 'MT1'.
        'a = c 'error BC30311: Value of type 'MT3' cannot be converted to 'MT1'.
        'c = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT3'.
        'a = d 'error BC30311: Value of type 'MT4' cannot be converted to 'MT1'.
        'd = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT4'.
        'a = f 'error BC30311: Value of type 'MT6' cannot be converted to 'MT1'.
        'f = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT6'.
        'a = h 'error BC30311: Value of type 'MT8' cannot be converted to 'MT1'.
        'h = a 'error BC30311: Value of type 'MT1' cannot be converted to 'MT8'.
        'e = f 'error BC30311: Value of type 'MT6' cannot be converted to 'MT5'.
        'f = e 'error BC30311: Value of type 'MT5' cannot be converted to 'MT6'.
        'e = h 'error BC30311: Value of type 'MT8' cannot be converted to 'MT5'.
        'h = e 'error BC30311: Value of type 'MT5' cannot be converted to 'MT8'.
        'a = k 'error BC30311: Value of type 'UInteger' cannot be converted to 'MT1'.
        'k = a 'error BC30311: Value of type 'MT1' cannot be converted to 'UInteger'.
        'e = k 'error BC30311: Value of type 'UInteger' cannot be converted to 'MT5'.
        'k = e 'error BC30311: Value of type 'MT5' cannot be converted to 'UInteger'.
        'n = o 'error BC30311: Value of type 'MT11' cannot be converted to 'MT10'.
        'o = n 'error BC30311: Value of type 'MT10' cannot be converted to 'MT11'.
        'q = r 'error BC30311: Value of type 'MT13' cannot be converted to 'MT12'.
        'r = q 'error BC30311: Value of type 'MT12' cannot be converted to 'MT13'.
        'v = w 'error BC30311: Value of type 'MT17' cannot be converted to 'MT16'.
        'w = v 'error BC30311: Value of type 'MT16' cannot be converted to 'MT17'.
    End Sub


    Sub M10(
        a As Object,
        b As System.ValueType,
        c As System.Enum,
        d As Interface1,
        e As Interface7,
        f As Structure2,
        g As Structure1,
        h As Enum1,
        i As Interface3
    )
        '§8.8 Widening Conversions
        '•	From a value type to a base type.
        '•	From a value type to an interface type that the type implements.
        '§8.9 Narrowing Conversions
        '•	From a reference type to a more derived value type.
        '•	From an interface type to a value type, provided the value type implements the interface type. 

        a = f
        b = f
        a = h
        b = h
        c = h
        d = f
        i = f
        f = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Structure2'.
        f = b 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'Structure2'.
        h = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Enum1'.
        h = b 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'Enum1'.
        h = c 'error BC30512: Option Strict On disallows implicit conversions from 'System.Enum' to 'Enum1'.
        f = d 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'Structure2'.
        f = i 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'Structure2'.
        'c = f 'error BC30311: Value of type 'Structure2' cannot be converted to 'System.Enum'.
        'f = c 'error BC30311: Value of type 'System.Enum' cannot be converted to 'Structure2'.
        'd = h 'error BC30311: Value of type 'Enum1' cannot be converted to 'Interface1'.
        'h = d 'error BC30311: Value of type 'Interface1' cannot be converted to 'Enum1'.
        'e = f 'error BC30311: Value of type 'Structure2' cannot be converted to 'Interface7'.
        'f = e 'error BC30311: Value of type 'Interface7' cannot be converted to 'Structure2'.
        'f = g 'error BC30311: Value of type 'Structure1' cannot be converted to 'Structure2'.
        'g = f 'error BC30311: Value of type 'Structure2' cannot be converted to 'Structure1'.
        'f = h 'error BC30311: Value of type 'Enum1' cannot be converted to 'Structure2'.
        'h = f 'error BC30311: Value of type 'Structure2' cannot be converted to 'Enum1'.

    End Sub


    Sub M11(
        a As Object,
        b As ValueType,
        c As Structure2,
        d As Structure2?,
        e As Interface1,
        f As Interface3,
        g As Interface7,
        h As Integer,
        i As Integer?,
        j As Long,
        k As Long?
    )
        '§8.8 Widening Conversions
        '•	From a type T to the type T?.
        '•	From a type T? to a type S?, where there is a widening conversion from the type T to the type S.
        '•	From a type T to a type S?, where there is a widening conversion from the type T to the type S.
        '•	From a type T? to an interface type that the type T implements.
        '§8.9 Narrowing Conversions
        '•	From a type T? to a type T.
        '•	From a type T? to a type S?, where there is a narrowing conversion from the type T to the type S.
        '•	From a type T to a type S?, where there is a narrowing conversion from the type T to the type S.
        '•	From a type S? to a type T, where there is a conversion from the type S to the type T.

        d = d
        a = d
        b = d
        d = c
        e = d
        f = d
        i = h
        k = i
        d = a 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Structure2?'.
        d = b 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'Structure2?'.
        c = d 'error BC30512: Option Strict On disallows implicit conversions from 'Structure2?' to 'Structure2'.
        d = e 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'Structure2?'.
        d = f 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'Structure2?'.
        h = i 'error BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Integer'.
        i = k 'error BC30512: Option Strict On disallows implicit conversions from 'Long?' to 'Integer?'.
        i = j 'error BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Integer?'.
        j = i 'error BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Long'.
        'c = i 'error BC30311: Value of type 'Integer?' cannot be converted to 'Structure2'.
        'i = c 'error BC30311: Value of type 'Structure2' cannot be converted to 'Integer?'.
        'd = h 'error BC30311: Value of type 'Integer' cannot be converted to 'Structure2?'.
        'h = d 'error BC30311: Value of type 'Structure2?' cannot be converted to 'Integer'.
        'd = i 'error BC30311: Value of type 'Integer?' cannot be converted to 'Structure2?'.
        'i = d 'error BC30311: Value of type 'Structure2?' cannot be converted to 'Integer?'.
    End Sub


    Sub M12(
        a As String,
        b As Char,
        c As Char()
    )
        '§8.8 Widening Conversions
        '•	From Char() to String.
        '§8.9 Narrowing Conversions
        '•	From String to Char().
        a = b
        a = c
        b = a 'error BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        c = a 'error BC30512: Option Strict On disallows implicit conversions from 'String' to '1-dimensional array of Char'.
        'b = c 'error BC30311: Value of type '1-dimensional array of Char' cannot be converted to 'Char'.
        'c = b 'error BC30311: Value of type 'Char' cannot be converted to '1-dimensional array of Char'.

    End Sub

#End If

    Sub M13(Of MT1, MT2, MT3 As MT1, MT4 As Class, MT5 As Class8, MT6 As Class9, MT7 As Class12, MT8 As {Class8, MT6})(
        a As Object,
        b As ValueType,
        c As Integer,
        d As Long,
        e As Enum1,
        f As Enum2,
        g As Enum4,
        h As Class8(),
        i As Class9(),
        j As Class11(),
        k As MT1,
        l As MT2,
        m As MT3,
        n As MT1(),
        o As MT2(),
        p As MT2(,),
        q As MT4,
        r As MT5,
        s As MT6,
        t As MT7,
        u As Integer(),
        v As MT4(),
        w As MT8
    )

        a = DirectCast(b, Object)
        a = DirectCast(c, Object)
        b = DirectCast(a, ValueType)
        b = DirectCast(c, ValueType)
        c = DirectCast(a, Integer)
        c = DirectCast(b, Integer)
        c = DirectCast(c, Integer)
        'c = DirectCast(d, Integer) 'error BC30311: Value of type 'Long' cannot be converted to 'Integer'.
        c = DirectCast(e, Integer)
        d = DirectCast(d, Long)
        'd = DirectCast(c, Long) 'error BC30311: Value of type 'Integer' cannot be converted to 'Long'.
        e = DirectCast(e, Enum1)
        'e = DirectCast(f, Enum1) 'error BC30311: Value of type 'Enum2' cannot be converted to 'Enum1'.
        e = DirectCast(g, Enum1)
        f = DirectCast(f, Enum2)
        'f = DirectCast(g, Enum2) ' error BC30311: Value of type 'Enum4' cannot be converted to 'Enum2'.
        h = DirectCast(i, Class8())
        i = DirectCast(h, Class9())
        'i = DirectCast(j, Class9()) ' error BC30332: Value of type '1-dimensional array of Class11' cannot be converted to '1-dimensional array of Class9' because 'Class11' is not derived from 'Class9'.
        k = DirectCast(k, MT1)
        'k = DirectCast(l, MT1) ' error BC30311: Value of type 'MT2' cannot be converted to 'MT1'.
        k = DirectCast(m, MT1)
        'k = DirectCast(q, MT1) ' error BC30311: Value of type 'MT4' cannot be converted to 'MT1'.
        'l = DirectCast(k, MT2) ' Value of type 'MT1' cannot be converted to 'MT2'.
        m = DirectCast(k, MT3)
        'n = DirectCast(o, MT1()) ' Value of type '1-dimensional array of MT2' cannot be converted to '1-dimensional array of MT1' because 'MT2' is not derived from 'MT1'.
        'n = DirectCast(p, MT1()) ' error BC30332: Value of type '2-dimensional array of MT2' cannot be converted to '1-dimensional array of MT1' because 'MT2' is not derived from 'MT1'.
        'n = DirectCast(u, MT1()) ' error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of MT1' because 'Integer' is not derived from 'MT1'.
        'q = DirectCast(k, MT4) ' error BC30311: Value of type 'MT1' cannot be converted to 'MT4'.
        'q = DirectCast(b, MT4) ' error BC30311: Value of type 'System.ValueType' cannot be converted
        'q = DirectCast(c, MT4) ' error BC30311: Value of type 'Integer' cannot be converted to 'MT4'
        'r = DirectCast(s, MT5) ' error BC30311: Value of type 'MT6' cannot be converted to 'MT5'.
        'r = DirectCast(t, MT5) ' error BC30311: Value of type 'MT7' cannot be converted to 'MT5'.
        'r = DirectCast(w, MT5) ' error BC30311: Value of type 'MT8' cannot be converted to 'MT5'.
        's = DirectCast(r, MT6) ' error BC30311: Value of type 'MT5' cannot be converted to 'MT6'.
        's = DirectCast(t, MT6) ' error BC30311: Value of type 'MT7' cannot be converted to 'MT6'.
        s = DirectCast(w, MT6)
        't = DirectCast(r, MT7) ' error BC30311: Value of type 'MT5' cannot be converted to 'MT7'.
        't = DirectCast(s, MT7) ' error BC30311: Value of type 'MT6' cannot be converted to 'MT7'.
        't = DirectCast(w, MT7) ' error BC30311: Value of type 'MT8' cannot be converted to 'MT7'.
        'u = DirectCast(n, Integer()) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of Integer' because 'MT1' is not derived from 'Integer'.
        'u = DirectCast(v, Integer()) 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of Integer' because 'MT4' is not derived from 'Integer'.
        'v = DirectCast(u, MT4())     'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of MT4' because 'Integer' is not derived from 'MT4'.

        a = DirectCast(Nothing, Object)
        b = DirectCast(Nothing, ValueType)
        c = DirectCast(Nothing, Integer)
        c = DirectCast(0, Integer)
        'c = DirectCast(0L, Integer) 'error BC30311: Value of type 'Long' cannot be converted to 'Integer'.
        'd = DirectCast(0, Long) ' error BC30311: Value of type 'Integer' cannot be converted to 'Long'.
        d = DirectCast(0L, Long)
        e = DirectCast(0, Enum1)
        'e = DirectCast(0L, Enum1) ' error BC30311: Value of type 'Long' cannot be converted to 'Enum1'.
        e = DirectCast(Nothing, Enum1)
        k = DirectCast(Nothing, MT1)
        q = DirectCast(Nothing, MT4)


        a = TryCast(b, Object)
        a = TryCast(c, Object)
        b = TryCast(a, ValueType)
        b = TryCast(c, ValueType)
        'c = TryCast(a, Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'c = TryCast(b, Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'c = TryCast(c, Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'c = TryCast(d, Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'c = TryCast(e, Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'd = TryCast(d, Long)    ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
        'd = TryCast(c, Long)    ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
        'e = TryCast(e, Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
        'e = TryCast(f, Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
        'e = TryCast(g, Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
        'f = TryCast(f, Enum2)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum2' is a value type.
        'f = TryCast(g, Enum2)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum2' is a value type.
        h = TryCast(i, Class8())
        i = TryCast(h, Class9())
        'i = TryCast(j, Class9()) ' error BC30332: Value of type '1-dimensional array of Class11' cannot be converted to '1-dimensional array of Class9' because 'Class11' is not derived from 'Class9'.
        'k = TryCast(k, MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
        'k = TryCast(l, MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
        'k = TryCast(m, MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
        'k = TryCast(q, MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
        'l = TryCast(k, MT2)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT2' has no class constraint.
        'm = TryCast(k, MT3)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT3' has no class constraint.
        n = TryCast(o, MT1())
        n = TryCast(p, MT1())
        n = TryCast(u, MT1())
        q = TryCast(k, MT4)
        q = TryCast(b, MT4)
        q = TryCast(c, MT4)
        r = TryCast(s, MT5)
        r = TryCast(t, MT5)
        r = TryCast(w, MT5)
        s = TryCast(r, MT6)
        's = TryCast(t, MT6) ' error BC30311: Value of type 'MT7' cannot be converted to 'MT6'.
        s = TryCast(w, MT6)
        t = TryCast(r, MT7)
        't = TryCast(s, MT7) ' error BC30311: Value of type 'MT6' cannot be converted to 'MT7'.
        't = TryCast(w, MT7) ' error BC30311: Value of type 'MT8' cannot be converted to 'MT7'.
        u = TryCast(n, Integer())
        'u = TryCast(v, Integer()) 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of Integer' because 'MT4' is not derived from 'Integer'.
        'v = TryCast(u, MT4())     'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of MT4' because 'Integer' is not derived from 'MT4'.

        a = TryCast(Nothing, Object)
        a = TryCast(0, Object)
        b = TryCast(Nothing, ValueType)
        'c = TryCast(Nothing, Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type. 
        'c = TryCast(0, Integer)       ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'c = TryCast(0L, Integer)      ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
        'd = TryCast(0, Long)          ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
        'd = TryCast(0L, Long)         ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
        'e = TryCast(0, Enum1)         ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
        'e = TryCast(0L, Enum1)        ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
        'e = TryCast(Nothing, Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
        'k = TryCast(Nothing, MT1)     ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
        q = TryCast(Nothing, MT4)

    End Sub

End Class

Structure Structure1
    Implements Interface5
End Structure

Structure Structure2
    Implements Interface3
End Structure

Class Class8
    Implements Interface5
End Class

Class Class9
    Inherits Class8
    Implements Interface4
End Class

Class Class10
    Inherits Class9
    Implements Interface3, Interface6
End Class

NotInheritable Class Class11
    Inherits Class8
End Class

Class Class12
    Inherits Class8
End Class

Interface Interface1
End Interface

Interface Interface2
    Inherits Interface1
End Interface

Interface Interface3
    Inherits Interface2
End Interface

Interface Interface4
End Interface

Interface Interface5
End Interface

Interface Interface6
End Interface

Interface Interface7
    Inherits Interface1, Interface6
End Interface

Module Module2

End Module

Enum Enum1 As Integer
    e
End Enum

Enum Enum2 As Long
    e
End Enum

Enum Enum3 As Short
    e
End Enum

Enum Enum4 As Integer
    e
End Enum

Enum Enum5 As UInteger
    e
End Enum

Class Class1
End Class

Class Class2
    Class Class3(Of T)
    End Class
End Class

Class Class4(Of T)

    Class Class5(Of S)
    End Class

    Class Class6
        Class Class7(Of S)
        End Class
    End Class
End Class


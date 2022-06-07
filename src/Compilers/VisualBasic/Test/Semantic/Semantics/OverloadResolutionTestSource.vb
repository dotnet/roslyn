' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


Imports System.Runtime.CompilerServices

Public Class TestClass1
    Shared Sub M1()
    End Sub

    Shared Sub M2(Of T)()
    End Sub

    Shared Sub M3(ParamArray x() As Integer)
    End Sub

    Shared Sub M4(x As Integer, y As TestClass1)
    End Sub

    Shared Sub M5(x As Integer, ParamArray y() As Integer)
    End Sub

    Shared Sub M6(x As Single)
    End Sub

    Shared Sub M7(x As Single, y As Single)
    End Sub

    Shared Sub M8(ByRef x As Double)
    End Sub

    Shared Sub M9(ByRef x As Object)
    End Sub

    Shared Sub M10(ByRef x As Single)
    End Sub

    Public Shared ShortField As Short
    Public Shared DoubleField As Double
    Public Shared ObjectField As Object

    Shared Sub M11(ParamArray x() As Object)
    End Sub

    Shared Sub M12(x As Integer, y As TestClass1)
    End Sub

    Shared Sub M12(x As Integer, y As Integer)
    End Sub

    Shared Sub M13(a As Object, ParamArray b As Object())
    End Sub

    Shared Sub M13(a As Object, b As Object, ParamArray c As Object())
    End Sub

    Shared Sub M14(a As Integer)
    End Sub

    Shared Sub M14(a As Long)
    End Sub

    Shared Sub M15(a As Integer)
    End Sub

    Shared Sub M15(a As System.TypeCode)
    End Sub

    Shared Sub M16(a As Short)
    End Sub

    Shared Sub M16(a As System.TypeCode)
    End Sub

    Shared Sub M17(a As Byte)
    End Sub
    Shared Sub M17(a As SByte)
    End Sub

    Shared Sub M18(a As Short)
    End Sub
    Shared Sub M18(a As UShort)
    End Sub

    Shared Sub M19(a As Integer)
    End Sub
    Shared Sub M19(a As UInteger)
    End Sub

    Shared Sub M20(a As Long)
    End Sub
    Shared Sub M20(a As ULong)
    End Sub

    Shared Sub M21(a As Integer)
    End Sub
    Shared Sub M21(a As ULong)
    End Sub

    Shared Sub M22(a As Byte, b As ULong)
    End Sub
    Shared Sub M22(a As SByte, b As Long)
    End Sub

    Shared Sub M23(a As Long)
    End Sub
    Shared Sub M23(a As Short)
    End Sub

    Shared Sub M24(a As Long, b As Short)
    End Sub
    Shared Sub M24(a As Short, b As Integer)
    End Sub

    Shared Sub M25(a As Short)
    End Sub
    Shared Sub M25(a As Integer)
    End Sub
    Shared Sub M25(a As SByte)
    End Sub

    Shared Sub M26(a As Short, b As Integer)
    End Sub
    Shared Sub M26(a As Integer, b As Short)
    End Sub

    Shared Sub M27(a As Short)
    End Sub

    Shared Sub g(ByVal a1 As Long)
    End Sub
    Shared Sub g(ByVal a2 As ULong)
    End Sub
    Shared Sub g(ByVal a3 As ULong?)
    End Sub

    Sub SM(x As Integer, y As Double, ParamArray z() As Integer)
    End Sub

    Sub SM1(x As Integer, y As Double, ParamArray z() As Integer)
    End Sub

End Class

Class Base
    Shared Sub M1(x As Integer)
    End Sub

    Shared Sub M2(x As Integer, Optional y As Integer = 0, Optional z As String = "0")
    End Sub

    Shared Sub M3(x As Integer, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub

    Shared Sub M4(x As Integer, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub

    Shared Sub M5(a As Object)
    End Sub

    Shared Sub M6(a As Object, ParamArray b As Object())
    End Sub

    Shared Sub M7(a As Object, b As Object)
    End Sub

    Shared Sub M8(a As Object, b As Object())
    End Sub

    Shared Sub M9(a As Integer, b As TestClass1, Optional c As Integer() = Nothing)
    End Sub
End Class

Interface I1
End Interface

Class Derived
    Inherits Base
    Implements I1

    Overloads Shared Sub M1(x As Integer, ParamArray y() As Integer)
    End Sub

    Overloads Shared Sub M2(x As Integer, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub

    Overloads Shared Sub M3(u As Integer, Optional v As Integer = 0, Optional z As Integer = 0)
    End Sub

    Overloads Shared Sub M4(u As Integer, Optional v As Integer = 0, Optional w As Integer = 0)
    End Sub

    Overloads Shared Sub M5(a As Object, ParamArray b As Object())
    End Sub

    Overloads Shared Sub M6(a As Object, b As Object())
    End Sub

    Overloads Shared Sub M7(a As Object, ParamArray b As Object())
    End Sub

    Overloads Shared Sub M8(a As Object, ParamArray b As Object())
    End Sub

    Overloads Shared Sub M9(b As Integer, a As TestClass1, ParamArray c As Integer())
    End Sub
End Class

Module BaseExt
    <Extension()> _
    Sub M10(b As Base, x As Integer)
    End Sub

End Module

Module DerivedExt
    <Extension()> _
    Sub M10(d As Derived, x As Integer)
    End Sub

    <Extension()> _
    Sub M11(c As Derived, y As Integer)
    End Sub

    <Extension()> _
    Sub M12(c As Derived, y As Integer)
    End Sub
End Module

Module Ext

    <Extension()> _
    Sub M11(i As I1, x As Integer)
    End Sub

    <Extension()> _
    Sub M12(Of T)(x As T, y As Integer)
    End Sub

    <Extension()> _
    Sub M13(Of T, U)(x As T, y As U, z As U)
    End Sub

    <Extension()> _
    Sub M13(Of T, U)(x As T, y As U, z As T)
    End Sub

    <Extension()> _
    Sub M14(Of T, U)(x As T, y As U, z As T)
    End Sub

    <Extension()> _
    Sub M15(Of U)(x As Integer, y As U, z As U)
    End Sub

    <Extension()>
    Sub SM(this As TestClass1, y As Double, x As Integer)
    End Sub

    <Extension()>
    Sub SM1(this As TestClass1, y As Double, x As Integer)
    End Sub

    <Extension()>
    Sub SM1(this As TestClass1, y As Object, x As Short)
    End Sub

End Module

Module Ext1
    <Extension()> _
    Sub M14(Of T, U)(x As T, y As U, z As T)
    End Sub
End Module


Class TestClass2(Of T)
    Sub S1(Of U)(x As U, y As T)
    End Sub

    Sub S1(Of U)(x As U, y As U)
    End Sub

    Sub S2(x As Integer, y As T)
    End Sub

    Sub S2(x As T, y As T)
    End Sub

    Sub S3(Of U)(x As U, y As T, z As U, v As T)
    End Sub

    Sub S3(Of U)(x As U, y As U, z As T, v As Integer)
    End Sub

    Sub S4(Of U)(x As U, y As Integer(), z As TestClass2(Of U), v As U)
    End Sub

    Sub S4(Of U)(x As U, y As U(), z As TestClass2(Of Integer), v As Integer)
    End Sub

    Sub S5(x As Integer, y As TestClass2(Of T))
    End Sub

    Sub S5(x As T, y As TestClass2(Of T()))
    End Sub

    Sub S5(x As T, y As TestClass2(Of Integer()))
    End Sub

    Sub S6(x As T, ParamArray y As T())
    End Sub

    Sub S6(x As T, ParamArray y As Integer())
    End Sub
End Class

Module Module1

    Sub BasicTests()
        Dim intVal As Integer = -0
        Dim intArray() As Integer = Nothing
        Dim TestClass1Val As New TestClass1()
        Dim shortVal As Short = -0
        Dim doubleVal As Double = -0
        Const doubleConst As Double = 0
        Dim objectVal As Object = Nothing
        Dim objectArray() As Object = Nothing
        Dim stringVal As String = "0"
        Dim ushortVal As UShort = 10

        TestClass1.M1()
        'TestClass1.M1(Of TestClass1)() 'error BC32045: 'Public Shared Sub M1()' has no type parameters and so cannot have type arguments.
        'TestClass1.M1(Nothing) 'error BC30057: Too many arguments to 'Public Shared Sub M1()'.
        'TestClass1.M2() 'error BC32050: Type parameter 'T' for 'Public Shared Sub M2(Of T)()' cannot be inferred.
        TestClass1.M2(Of TestClass1)()
        TestClass1.M3()
        TestClass1.M3(intVal)
        TestClass1.M3(intArray)
        TestClass1.M3(Nothing)
        TestClass1.M4(intVal, TestClass1Val)

        'error BC30311: Value of type 'TestClass1' cannot be converted to 'Integer'.
        'TestClass1.M4(TestClass1Val, TestClass1Val)

        'error BC30311: Value of type 'Integer' cannot be converted to 'TestClass1'.
        'TestClass1.M4(intVal, intVal)

        TestClass1.M4(intVal, y:=TestClass1Val)
        TestClass1.M4(x:=intVal, y:=TestClass1Val)
        TestClass1.M4(y:=TestClass1Val, x:=intVal)

        'error BC30311: Value of type 'Integer' cannot be converted to 'TestClass1'.
        'TestClass1.M4(y:=intVal, x:=intVal)

        'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'error BC30274: Parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)' already has a matching argument.
        'TestClass1.M4(intVal, x:=intVal)

        'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'error BC32021: Parameter 'x' in 'Public Shared Sub M4(x As Integer, y As TestClass1)' already has a matching omitted argument.
        'TestClass1.M4(, x:=intVal)

        'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'error BC30274: Parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)' already has a matching argument.
        'TestClass1.M4(x:=intVal, x:=intVal)

        'error BC30455: Argument not specified for parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'error BC30272: 'z' is not a parameter of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'TestClass1.M4(z:=intVal, y:=TestClass1Val)

        'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'error BC30272: 'z' is not a parameter of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'TestClass1.M4(z:=TestClass1Val, x:=intVal)

        'error BC30455: Argument not specified for parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'TestClass1.M4(, TestClass1Val)

        'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
        'TestClass1.M4(intVal, )

        'error BC30587: Named argument cannot match a ParamArray parameter.
        'TestClass1.M3(x:=intArray)

        'error BC30587: Named argument cannot match a ParamArray parameter.
        'TestClass1.M3(x:=intVal)

        'error BC30588: Omitted argument cannot match a ParamArray parameter.
        'TestClass1.M5(intVal, )

        'error BC30241: Named argument expected.
        'TestClass1.M4(x:=intVal, TestClass1Val)

        'error BC30057: Too many arguments to 'Public Shared Sub M2(Of T)()'.
        'TestClass1.M2(Of TestClass1)(intVal)

        TestClass1.M6(shortVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M6(doubleVal)

        TestClass1.M6(doubleConst)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        'TestClass1.M6(objectVal)

        TestClass1.M7(shortVal, shortVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M7(doubleVal, shortVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M7(shortVal, doubleVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M7(doubleVal, doubleVal)

        TestClass1.M7(doubleConst, shortVal)
        TestClass1.M7(shortVal, doubleConst)
        TestClass1.M7(doubleConst, doubleConst)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'TestClass1.M7(objectVal, shortVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'TestClass1.M7(shortVal, objectVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'TestClass1.M7(objectVal, objectVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M7(objectVal, doubleVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M7(doubleConst, doubleVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'TestClass1.M7(objectVal, doubleConst)

        'error BC32029: Option Strict On disallows narrowing from type 'Double' to type 'Short' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
        'TestClass1.M8(TestClass1.ShortField)
        TestClass1.M8((shortVal))

        'error BC32029: Option Strict On disallows narrowing from type 'Object' to type 'Short' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
        'TestClass1.M9(TestClass1.ShortField)
        TestClass1.M9((shortVal))

        TestClass1.M10(doubleConst)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M10(TestClass1.DoubleField)

        'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'TestClass1.M10(TestClass1.ObjectField)

        'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
        'TestClass1.M10((doubleVal))

        'Option Strict On disallows implicit conversions from 'Object' to 'Single'.
        'TestClass1.M10((objectVal))

        TestClass1.M11(objectVal)
        TestClass1.M11(objectArray)

        TestClass1.M12(intVal, intVal)

        TestClass1.M13(intVal)
        TestClass1.M13(intVal, intVal)
        TestClass1.M13(intVal, intVal, intVal)

        Derived.M1(intVal)

        'Derived.M2(intVal, z:=stringVal) ' Should bind to Base.M2

        Derived.M3(intVal, z:=intVal)

        'error BC30272: 'z' is not a parameter of 'Public Shared Overloads Sub M4(u As Integer, [v As Integer = 0], [w As Integer = 0])'.
        'Derived.M4(intVal, z:=intVal)

        Derived.M5(a:=objectVal) ' Should bind to Base.M5
        Derived.M6(a:=objectVal) ' Should bind to Base.M6

        Derived.M7(objectVal, objectVal) ' Should bind to Base.M7

        Derived.M8(objectVal, objectVal) ' Should bind to Derived.M8

        Derived.M9(a:=TestClass1Val, b:=intVal) ' Should bind to Derived.M9

        'error BC30311: Value of type 'Integer' cannot be converted to 'TestClass1'.
        'error BC30311: Value of type 'TestClass1' cannot be converted to 'Integer'.
        'Derived.M9(a:=intVal, b:=TestClass1Val)

        Derived.M9(Nothing, Nothing) ' Should bind to Derived.M9

        Dim b As New Base()
        Dim d As New Derived()

        ' Calls BaseExt.M
        b.M10(intVal)

        ' Calls DerivedExt.M 
        d.M10(intVal)

        ' Calls derived.M11(...), because I1.M11(...) is hidden since it extends
        ' an interface.
        d.M11(intVal)

        ' Calls derived.M12 since T.M12 target type is more generic.
        d.M12(10)

        Dim tc2 As TestClass2(Of Integer) = New TestClass2(Of Integer)

        tc2.S1(10, 10)    ' Calls S1(U, T)
        tc2.S2(10, 10)    ' Calls S2(Integer, T)

        'M13(Of T, U)(x As T, y As U, z As T)
        intVal.M13(intVal, intVal)

        ' error BC30521: Overload resolution failed because no accessible 'S3' is most specific for these arguments:
        'Public Sub S3(Of Integer)(x As Integer, y As Integer, z As Integer, v As Integer)': Not most specific.
        'Public Sub S3(Of Integer)(x As Integer, y As Integer, z As Integer, v As Integer)': Not most specific.
        'tc2.S3(intVal, intVal, intVal, intVal)

        'error BC30521: Overload resolution failed because no accessible 'M14' is most specific for these arguments:
        'Extension(method) 'Public Sub M14(Of Integer)(y As Integer, z As Integer)' defined in 'Ext1': Not most specific.
        'Extension(method) 'Public Sub M14(Of Integer)(y As Integer, z As Integer)' defined in 'Ext': Not most specific.
        'intVal.M14(intVal, intVal)

        'error BC30521: Overload resolution failed because no accessible 'S4' is most specific for these arguments:
        'Public Sub S4(Of Integer)(x As Integer, y() As Integer, z As TestClass2(Of Integer), v As Integer)': Not most specific.
        'Public Sub S4(Of Integer)(x As Integer, y() As Integer, z As TestClass2(Of Integer), v As Integer)': Not most specific.
        'tc2.S4(intVal, Nothing, Nothing, intVal)

        'error BC30521: Overload resolution failed because no accessible 'S5' is most specific for these arguments:
        'Public Sub S5(x As Integer, y As TestClass2(Of Integer()))': Not most specific.
        'Public Sub S5(x As Integer, y As TestClass2(Of Integer))': Not most specific.
        'tc2.S5(intVal, Nothing)

        intVal.M15(intVal, intVal)

        'S6(x As T, ParamArray y As Integer())
        tc2.S6(intVal, intVal)

        'M14(a As Integer)
        TestClass1.M14(shortVal)

        'M15(a As Integer)
        TestClass1.M15(0)

        'M16(a As Short)
        TestClass1.M16(0L)

        'M16(a As System.TypeCode) 
        TestClass1.M16(0)

        'Byte
        TestClass1.M17(Nothing)

        'Short
        TestClass1.M18(Nothing)

        'Integer
        TestClass1.M19(Nothing)

        'Long
        TestClass1.M20(Nothing)

        'Integer
        TestClass1.M21(ushortVal)

        'error BC30521: Overload resolution failed because no accessible 'M22' is most specific for these arguments:
        'Public Shared Sub M22(a As SByte, b As Long)': Not most specific.
        'Public Shared Sub M22(a As Byte, b As ULong)': Not most specific.
        'TestClass1.M22(Nothing, Nothing)

        'M23(a As Long)
        TestClass1.M23(intVal)

        'Option strict ON
        ' error BC30518: Overload resolution failed because no accessible 'M23' can be called with these arguments:
        'Public Shared Sub M23(a As Short)': Option Strict On disallows implicit conversions from 'Object' to 'Short'.
        'Public Shared Sub M23(a As Long)': Option Strict On disallows implicit conversions from 'Object' to 'Long'.
        'TestClass1.M23(objectVal)

        'Option strict OFF: late call
        'TestClass1.M23(objectVal)

        'Option strict OFF
        'warning BC42016: Implicit conversion from 'Object' to 'Short'.
        'TestClass1.M24(objectVal, intVal)

        'Option strict ON
        'F:\ddp\Roslyn\Main\Open\Compilers\VisualBasic\Test\Semantics\OverloadResolutionTestSource.vb(549) : error BC30518: Overload resolution failed because no accessible 'M24' can be called with these arguments:
        'Public Shared Sub M24(a As Short, b As Integer)': Option Strict On disallows implicit conversions from 'Object' to 'Short'.
        'Public Shared Sub M24(a As Long, b As Short)': Option Strict On disallows implicit conversions from 'Object' to 'Long'.
        'Public Shared Sub M24(a As Long, b As Short)': Option Strict On disallows implicit conversions from 'Integer' to 'Short'.
        'TestClass1.M24(objectVal, intVal)

        'M25(a As SByte)
        TestClass1.M25(-1L)

        'BC30518: Overload resolution failed because no accessible 'M26' can be called with these arguments:
        'Public Shared Sub M26(a As Integer, b As Short)': Option Strict On disallows implicit conversions from 'Double' to 'Short'.
        'Public Shared Sub M26(a As Short, b As Integer)': Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        'TestClass1.M26(-1L, doubleVal)

        'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Short'.
        'TestClass1.M27(intVal)

        'Sub M14(a As Long)
        TestClass1.M14(0L)

        TestClass1.M27(Integer.MaxValue)

        'error BC30439: Constant expression not representable in type 'Short'.
        'TestClass1.M27(Integer.MaxValue)

        'error BC30439: Constant expression not representable in type 'Short'.
        'TestClass1.M27(Double.MaxValue)

        'error BC30519: Overload resolution failed because no accessible 'M26' can be called without a narrowing conversion:
        'Public Shared Sub M26(a As Integer, b As Short)': Argument matching parameter 'b' narrows from 'Integer' to 'Short'.
        'Public Shared Sub M26(a As Short, b As Integer)': Argument matching parameter 'a' narrows from 'Integer' to 'Short'.
        'TestClass1.M26(intVal, intVal)

        'Overflow On - Sub M26(a As Integer, b As Short)
        TestClass1.M26(Integer.MaxValue, intVal)

        'Overflow Off
        TestClass1.M27(Integer.MaxValue)

        'Overflow Off
        'error BC30439: Constant expression not representable in type 'Short'.
        TestClass1.M27(Double.MaxValue)

        'Overflow Off
        'error BC30519: Overload resolution failed because no accessible 'M26' can be called without a narrowing conversion:
        'Public Shared Sub M26(a As Integer, b As Short)': Argument matching parameter 'b' narrows from 'Integer' to 'Short'.
        'Public Shared Sub M26(a As Short, b As Integer)': Argument matching parameter 'a' narrows from 'Integer' to 'Short'.
        'TestClass1.M26(Integer.MaxValue, intVal)

        'error BC30521: Overload resolution failed because no accessible 'g' is most specific for these arguments
        'TestClass1.g(1UI)

        'Should bind to extension method
        TestClass1Val.SM(x:=intVal, y:=objectVal)

        'error BC30519: Overload resolution failed because no accessible 'SM1' can be called without a narrowing conversion:
        'Extension(method) 'Public Sub SM1(y As Object, x As Short)' defined in 'Ext': Argument matching parameter 'x' narrows from 'Integer' to 'Short'.
        'Extension(method) 'Public Sub SM1(y As Double, x As Integer)' defined in 'Ext': Argument matching parameter 'y' narrows from 'Object' to 'Double'.
        TestClass1Val.SM1(x:=intVal, y:=objectVal)

    End Sub

End Module

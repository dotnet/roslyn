' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Resources.Proprietary
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenTests
        Inherits BasicTestBase

        <WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")>
        <Fact()>
        Public Sub Bug776642a()
            ' ILVerify: Unexpected type on the stack. { Offset = 16, Found = readonly address of '[...]OuterStruct', Expected = address of '[...]OuterStruct' }
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        M(New Object() {})
    End Sub

    Sub M(args As Object())
        args = New Object() {Nothing}
        Console.WriteLine((((DirectCast(args(0), OuterStruct)).z).y).x)
    End Sub
End Module

Structure TwoInteger
    Public x As Integer
    Public y As Integer
End Structure

Structure DoubleAndStruct
    Public x As Double
    Public y As TwoInteger
End Structure

Structure OuterStruct
    Public z As DoubleAndStruct
End Structure
    </file>
</compilation>, verify:=Verification.FailsILVerify).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "Object"
  IL_0006:  starg.s    V_0
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldelem.ref
  IL_000b:  unbox      "OuterStruct"
  IL_0010:  ldflda     "OuterStruct.z As DoubleAndStruct"
  IL_0015:  ldflda     "DoubleAndStruct.y As TwoInteger"
  IL_001a:  ldfld      "TwoInteger.x As Integer"
  IL_001f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0024:  ret
}
]]>)
        End Sub

        <WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")>
        <Fact()>
        Public Sub Bug776642b()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        M(New Object() {})
    End Sub

    Sub M(args As Object())
        args = New Object() {Nothing}
        Console.WriteLine((((DirectCast(args(0), OuterStruct)).z).y).x)
    End Sub
End Module

Structure TwoInteger
    Public x As Integer
    Public y As Integer
End Structure

Structure DoubleAndStruct
    Public x As Double
    Public y As TwoInteger
End Structure

Class OuterStruct
    Public z As DoubleAndStruct
End Class
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "Object"
  IL_0006:  starg.s    V_0
  IL_0008:  ldarg.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldelem.ref
  IL_000b:  castclass  "OuterStruct"
  IL_0010:  ldflda     "OuterStruct.z As DoubleAndStruct"
  IL_0015:  ldflda     "DoubleAndStruct.y As TwoInteger"
  IL_001a:  ldfld      "TwoInteger.x As Integer"
  IL_001f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Bug776642a_ref()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        M(New Object() {})
    End Sub

    Sub M(args As Object())
        args = New Object() {new OuterStruct(1)}
        Console.WriteLine((((DirectCast(args(0), OuterStruct)).z).y).x)
    End Sub
End Module

Structure TwoInteger
    Public x As Integer
    Public y As Integer
End Structure

Class DoubleAndStruct
    Public x As Double
    Public y As TwoInteger
End Class

Structure OuterStruct
    public sub new(i as integer)
        z = new DoubleAndStruct()
    end sub

    Public z As DoubleAndStruct
End Structure
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  4
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "Object"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     "Sub OuterStruct..ctor(Integer)"
  IL_000e:  box        "OuterStruct"
  IL_0013:  stelem.ref
  IL_0014:  starg.s    V_0
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4.0
  IL_0018:  ldelem.ref
  IL_0019:  unbox      "OuterStruct"
  IL_001e:  ldfld      "OuterStruct.z As DoubleAndStruct"
  IL_0023:  ldflda     "DoubleAndStruct.y As TwoInteger"
  IL_0028:  ldfld      "TwoInteger.x As Integer"
  IL_002d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0032:  ret
}
]]>)
        End Sub

        <WorkItem(776642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776642")>
        <Fact()>
        Public Sub Bug776642_shared()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        M(New Object() {})
    End Sub

    Sub M(args As Object())
        Console.WriteLine(((OuterStruct.z).y).x)
    End Sub
End Module

Structure TwoInteger
    Public x As Integer
    Public y As Integer
End Structure

Structure DoubleAndStruct
    Public x As Double
    Public y As TwoInteger
End Structure

Class OuterStruct
    Public Shared z As DoubleAndStruct
End Class
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldsflda    "OuterStruct.z As DoubleAndStruct"
  IL_0005:  ldflda     "DoubleAndStruct.y As TwoInteger"
  IL_000a:  ldfld      "TwoInteger.x As Integer"
  IL_000f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0014:  ret
}
]]>)
        End Sub

        <WorkItem(545724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545724")>
        <Fact()>
        Public Sub Bug14352()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Console
Module Module1
    Sub Main()
        Dim o1_1 As New C1(Of Double)(False)
        Dim o1_2 As New C1(Of Double)(True)
        If o1_1 AndAlso o1_2 Then
            WriteLine("Failed - 1")
        Else
            WriteLine("Passed - 1")
        End If
    End Sub
    Class C1(Of T)
        Public m_Value As Boolean
        Sub New()
        End Sub
        Sub New(ByVal Value As Boolean)
            m_Value = Value
        End Sub
        Shared Operator And(ByVal arg1 As C1(Of Double), ByVal arg2 As C1(Of T)) As C1(Of T)
            Return New C1(Of T)(arg1.m_Value And arg2.m_Value)
        End Operator
        Shared Operator IsFalse(ByVal arg1 As C1(Of T)) As Boolean
            Return Not arg1.m_Value
        End Operator
        Shared Operator IsTrue(ByVal arg1 As C1(Of T)) As Boolean
            Return arg1.m_Value
        End Operator
    End Class
End Module
    </file>
</compilation>, expectedOutput:="Passed - 1")
        End Sub

        <WorkItem(578074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578074")>
        <WorkItem(32576, "https://github.com/dotnet/roslyn/issues/32576")>
        <Fact>
        Public Sub PreserveZeroDigitsInDecimal()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Console
Module Form1
    Sub Main(args As String())
        TST()
        Console.Write(" ")
        TST2()
    End Sub

    Sub TST(Optional d As Decimal = 0.0000d)
        Console.Write(d.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub

    Sub TST2(Optional d As Decimal = -0.0000000D)
        Console.Write(d.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="0.0000 0.0000000")
        End Sub

        <Fact()>
        Public Sub TestRedimWithArrayType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class RedimTest
    Public Shared Sub Main()
        Dim o As Integer()

        ReDim o(3)
        o(1) = 234

        ReDim Preserve o(5)
        System.Console.WriteLine(o(1))
        System.Console.WriteLine(o(4))

        ReDim o(2), o(3), o(4)
        System.Console.WriteLine(o(1))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
234
0
0
]]>).
            VerifyIL("RedimTest.Main",
            <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  4
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4     0xea
  IL_000d:  stelem.i4
  IL_000e:  ldc.i4.6
  IL_000f:  newarr     "Integer"
  IL_0014:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0019:  castclass  "Integer()"
  IL_001e:  dup
  IL_001f:  ldc.i4.1
  IL_0020:  ldelem.i4
  IL_0021:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0026:  ldc.i4.4
  IL_0027:  ldelem.i4
  IL_0028:  call       "Sub System.Console.WriteLine(Integer)"
  IL_002d:  ldc.i4.3
  IL_002e:  newarr     "Integer"
  IL_0033:  pop
  IL_0034:  ldc.i4.4
  IL_0035:  newarr     "Integer"
  IL_003a:  pop
  IL_003b:  ldc.i4.5
  IL_003c:  newarr     "Integer"
  IL_0041:  ldc.i4.1
  IL_0042:  ldelem.i4
  IL_0043:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0048:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestRedimWithParamArrayProperty()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module RedimTest
    Sub Main()
        ReDim Preserve X(0)
    End Sub

    Dim s As String()
    Property X(ParamArray a As String()) As Integer()
        Get
            s = a
            Return New Integer() {}
        End Get
        Set(ByVal value As Integer())
            Console.WriteLine(s Is a)
        End Set
    End Property
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[False]]>).
            VerifyIL("RedimTest.Main",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     "String"
  IL_0006:  ldc.i4.0
  IL_0007:  newarr     "String"
  IL_000c:  call       "Function RedimTest.get_X(ParamArray String()) As Integer()"
  IL_0011:  ldc.i4.1
  IL_0012:  newarr     "Integer"
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_001c:  castclass  "Integer()"
  IL_0021:  call       "Sub RedimTest.set_X(ParamArray String(), Integer())"
  IL_0026:  ret
}
]]>)
        End Sub

        <WorkItem(546809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546809")>
        <Fact()>
        Public Sub Bug16872()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Enum E
    A
    B
    C
End Enum

Public MustInherit Class Base
    Public MustOverride ReadOnly Property Kind As E
End Class

Public Class DerivedA
    Inherits Base

    Public Overrides ReadOnly Property Kind As E
        Get
            Return E.A
        End Get
    End Property
End Class

Public Class DerivedB
    Inherits Base

    Public Overrides ReadOnly Property Kind As E
        Get
            Return E.B
        End Get
    End Property

    Public ReadOnly Property IsGood As Boolean
        Get
            Return True
        End Get
    End Property
End Class

Module Program
    Sub Main(args As String())
        Dim o As Base = New DerivedA
        Dim b As Boolean = (o.Kind = E.B) AndAlso (DirectCast(o, DerivedB).IsGood)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Base V_0) //o
  IL_0000:  newobj     "Sub DerivedA..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   "Function Base.get_Kind() As E"
  IL_000c:  ldc.i4.1
  IL_000d:  bne.un.s   IL_001c
  IL_000f:  ldloc.0
  IL_0010:  castclass  "DerivedB"
  IL_0015:  callvirt   "Function DerivedB.get_IsGood() As Boolean"
  IL_001a:  br.s       IL_001d
  IL_001c:  ldc.i4.0
  IL_001d:  pop
  IL_001e:  ret
}
]]>)
        End Sub

        <WorkItem(529861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529861")>
        <Fact()>
        Public Sub Bug14632a()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Globalization
Module Program
    Sub Main()
        Console.WriteLine((0e-28d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0.0000000000000000000000000000
        Console.WriteLine((0e-29d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0.0000000000000000000000000000
        Console.WriteLine((0e-30d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((0e-40d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((0e-50d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((0e-100d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((0e-1000d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((0e-10000d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine()
        Console.WriteLine((1e-28d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0.0000000000000000000000000001
        Console.WriteLine((1e-29d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0.0000000000000000000000000000
        Console.WriteLine((1e-30d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((1e-40d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((1e-50d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((1e-100d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((1e-1000d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
        Console.WriteLine((1e-10000d).ToString(CultureInfo.InvariantCulture)) ' Dev11: 0
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000

0.0000000000000000000000000001
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
0.0000000000000000000000000000
]]>)

        End Sub

        <WorkItem(529861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529861")>
        <WorkItem(568475, "DevDiv")>
        <Fact()>
        Public Sub Bug14632b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Console.WriteLine(0e28d)
        Console.WriteLine()
        Console.WriteLine(0.000000e28d)
        Console.WriteLine(0.000000e29d)
        Console.WriteLine(0.000000e30d)
        Console.WriteLine(0.000000e34d)
    End Sub
End Module
    </file>
</compilation>)

            Dim d As Decimal = 0
            If (Decimal.TryParse("0E1", Globalization.NumberStyles.AllowExponent, Nothing, d)) Then
                compilation.AssertNoErrors()
            Else
                compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30036: Overflow.
        Console.WriteLine(0e28d)
                          ~~~~~
BC30036: Overflow.
        Console.WriteLine(0.000000e28d)
                          ~~~~~~~~~~~~
BC30036: Overflow.
        Console.WriteLine(0.000000e29d)
                          ~~~~~~~~~~~~
BC30036: Overflow.
        Console.WriteLine(0.000000e30d)
                          ~~~~~~~~~~~~
BC30036: Overflow.
        Console.WriteLine(0.000000e34d)
                          ~~~~~~~~~~~~
]]></errors>)
            End If

        End Sub

        <WorkItem(529861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529861")>
        <Fact()>
        Public Sub Bug14632c()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Console.WriteLine(0.000001e28d)
        Console.WriteLine(0.000001e29d)
        Console.WriteLine(0.000001e30d)
        Console.WriteLine(0.000001e34d)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
10000000000000000000000
100000000000000000000000
1000000000000000000000000
10000000000000000000000000000
]]>)
        End Sub

        ''' <summary>
        ''' Breaking change: native compiler considers
        ''' digits &lt; 1e-49 when rounding.
        ''' </summary>
        <WorkItem(568494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568494")>
        <WorkItem(568520, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568520")>
        <WorkItem(32576, "https://github.com/dotnet/roslyn/issues/32576")>
        <WorkItem(375, "https://github.com/dotnet/roslyn/issues/375")>
        <Fact>
        Public Sub DecimalLiteral_BreakingChange()
            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Module M
    Dim cul As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
    Sub Main()
        Console.WriteLine(3.0500000000000000000001e-27d.ToString(cul)) ' 3.05e-27d + 1e-49d [Dev11/Roslyn rounds]
        Console.WriteLine(3.05000000000000000000001e-27d.ToString(cul)) ' 3.05e-27d + 1e-50d [Dev11 rounds, Roslyn does not]
        Console.WriteLine()
        Console.WriteLine(5.00000000000000000001e-29d.ToString(cul)) ' 5.0e-29d + 1e-49d [Dev11/Roslyn rounds]
        Console.WriteLine(5.0000000000000000000000000000001e-29d.ToString(cul)) ' 5.0e-29d + 1e-60d [Dev11 rounds, Roslyn does not]
        Console.WriteLine()
        Console.WriteLine((-5.00000000000000000001e-29d).ToString(cul)) ' -5.0e-29d + 1e-49d [Dev11/Roslyn rounds]
        Console.WriteLine((-5.0000000000000000000000000000001e-29d).ToString(cul)) ' -5.0e-29d + 1e-60d [Dev11 rounds, Roslyn does not]
        Console.WriteLine()
        '                          10        20        30        40        50        60        70        80        90       100
        Console.WriteLine(.1000000000000000000000000000500000000000000000000000000000000000000000000000000000000000000000000001d.ToString(cul)) ' [Dev11 rounds, Roslyn does not]
    End Sub
End Module
    ]]></file>
</compilation>
            If (ExecutionConditionUtil.IsDesktop) Then
                CompileAndVerify(source, references:=XmlReferences, expectedOutput:=<![CDATA[
0.0000000000000000000000000031
0.0000000000000000000000000030

0.0000000000000000000000000001
0.0000000000000000000000000000

-0.0000000000000000000000000001
0.0000000000000000000000000000

0.1000000000000000000000000000
]]>)
            ElseIf ExecutionConditionUtil.IsCoreClr Then
                CompileAndVerify(source, references:=XmlReferences, expectedOutput:=<![CDATA[
0.0000000000000000000000000031
0.0000000000000000000000000031

0.0000000000000000000000000001
0.0000000000000000000000000001

-0.0000000000000000000000000001
-0.0000000000000000000000000001

0.1000000000000000000000000001
]]>)
            End If
        End Sub

        <Fact()>
        Public Sub DecimalZero()
            CompileAndVerify(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Linq

Module M
    Sub Main()
        Dump(0E0D)
        Dump(0.0E0D)
        Dump(0.00E0D)
        Console.WriteLine()

        Dump(0E-1D)
        Dump(0E-10D)
        Dump(-0E-10D)
        Dump(0.00E-10D)
        Dump(0E-100D) ' differs from Dev11
        Console.WriteLine()

        Dump(decimal.Negate(0E0D))
        Dump(decimal.Negate(0.0E0D))
        Dump(decimal.Negate(0.00E0D))
        Console.WriteLine()

        Dump(decimal.Negate(0E-1D))
        Dump(decimal.Negate(0E-10D))
        Dump(decimal.Negate(-0E-10D))
        Dump(decimal.Negate(0.00E-10D))
        Dump(decimal.Negate(0E-100D)) ' differs from Dev11
    End Sub

    Function ToHexString(d As Decimal)
        Return String.Join("", Decimal.GetBits(d).Select(Function(word) String.Format("{0:x8}", word)))
    End Function

    Sub Dump(d As Decimal)
        Console.WriteLine(ToHexString(d))
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences, expectedOutput:=<![CDATA[
00000000000000000000000000000000
00000000000000000000000000010000
00000000000000000000000000020000

00000000000000000000000000010000
000000000000000000000000000a0000
000000000000000000000000800a0000
000000000000000000000000000c0000
000000000000000000000000001c0000

00000000000000000000000080000000
00000000000000000000000080010000
00000000000000000000000080020000

00000000000000000000000080010000
000000000000000000000000800a0000
000000000000000000000000000a0000
000000000000000000000000800c0000
000000000000000000000000801c0000
]]>)
        End Sub

        ''' <summary>
        ''' Breaking change: native compiler allows 0eNm where N > 0.
        ''' (The native compiler ignores sign and scale in 0eNm if N > 0
        ''' and represents such cases as 0e0m.)
        ''' </summary>
        <WorkItem(568475, "DevDiv")>
        <Fact()>
        Public Sub DecimalZero_BreakingChange()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Module M
    Sub Main()
        Console.WriteLine(0E1D)
        Console.WriteLine(0E10D)
        Console.WriteLine(-0E10D)
        Console.WriteLine(0.00E10D)
        Console.WriteLine(-0.00E10D)
        Console.WriteLine(0E100D) ' Dev11: BC30036: Overflow.
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim d As Decimal = 0
            If (Decimal.TryParse("0E1", Globalization.NumberStyles.AllowExponent, Nothing, d)) Then
                compilation.AssertNoErrors
            Else
                compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30036: Overflow.
        Console.WriteLine(0E1D)
                          ~~~~
BC30036: Overflow.
        Console.WriteLine(0E10D)
                          ~~~~~
BC30036: Overflow.
        Console.WriteLine(-0E10D)
                           ~~~~~
BC30036: Overflow.
        Console.WriteLine(0.00E10D)
                          ~~~~~~~~
BC30036: Overflow.
        Console.WriteLine(-0.00E10D)
                           ~~~~~~~~
BC30036: Overflow.
        Console.WriteLine(0E100D) ' Dev11: BC30036: Overflow.
                          ~~~~~~
]]></errors>)
            End If
        End Sub

        <Fact()>
        Public Sub TestChainedReadonlyFieldsAccessWithPropertyGetter()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure Array(Of T)
    Default Public Property Item(i As Integer) As T
        Get
            Return Nothing
        End Get
        Set(value As T)
        End Set
    End Property
End Structure

Structure Field
    Public Property Type As String
End Structure

Structure Descr
    Public ReadOnly Fields As Array(Of Field)
End Structure

Class Container
    Public ReadOnly D As Descr
End Class

Class Member
    Public ReadOnly C As Container

    Public ReadOnly Property P As String
        Get
            Return Me.C.D.Fields(123).Type
        End Get
    End Property

    Shared Sub Main(args() As String)
        Console.WriteLine("Done")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[Done]]>).
            VerifyIL("Member.get_P",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (Array(Of Field) V_0,
  Field V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Member.C As Container"
  IL_0006:  ldfld      "Container.D As Descr"
  IL_000b:  ldfld      "Descr.Fields As Array(Of Field)"
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   123
  IL_0015:  call       "Function Array(Of Field).get_Item(Integer) As Field"
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  call       "Function Field.get_Type() As String"
  IL_0022:  ret
}
]]>)
        End Sub

        <WorkItem(545120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545120")>
        <Fact()>
        Public Sub Bug13399a()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class S
    Public X As String
    Public Y As Object
End Class

Public Module Program
    Public Sub Main(args() As String)
        Dim i As Integer = 0
        Dim a, b As New S() With {
            .Y = Function() As String
                     With .ToString()
                         i += 1
                         Return .ToString &amp; ":" &amp; i
                     End With
                 End Function.Invoke(), .X = .Y
        }
        Console.WriteLine(a.X &amp;  "-" &amp; b.X)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="S:1-S:2")
        End Sub

        <WorkItem(545120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545120")>
        <Fact()>
        Public Sub Bug13399b()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Structure S
    Public X As String
    Public Y As Object
End Structure

Public Module Program
    Public Sub Main(args() As String)
        Dim i As Integer = 0
        Dim a, b As New S() With {
            .Y = Function() As String
                     With .ToString()
                         i += 1
                         Return .ToString &amp; ":" &amp; i
                     End With
                 End Function.Invoke(), .X = .Y
        }
        Console.WriteLine(a.X &amp;  "-" &amp; b.X)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="S:1-S:2")
        End Sub

        <Fact()>
        Public Sub TestRedimWithParamArrayProperty2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module RedimTest
    Sub Main()
        ReDim Preserve X(Y)(0)
    End Sub

    Property Y As String

    Dim s As String()
    Property X(ParamArray a As String()) As Integer()
        Get
            s = a
            Return New Integer() {}
        End Get
        Set(ByVal value As Integer())
            Console.WriteLine(s Is a)
        End Set
    End Property
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[False]]>).
            VerifyIL("RedimTest.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  5
  .locals init (String V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "String"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  call       "Function RedimTest.get_Y() As String"
  IL_000d:  dup
  IL_000e:  stloc.0
  IL_000f:  stelem.ref
  IL_0010:  ldc.i4.1
  IL_0011:  newarr     "String"
  IL_0016:  dup
  IL_0017:  ldc.i4.0
  IL_0018:  ldloc.0
  IL_0019:  stelem.ref
  IL_001a:  call       "Function RedimTest.get_X(ParamArray String()) As Integer()"
  IL_001f:  ldc.i4.1
  IL_0020:  newarr     "Integer"
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_002a:  castclass  "Integer()"
  IL_002f:  call       "Sub RedimTest.set_X(ParamArray String(), Integer())"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestByRefMethodWithParamArrayProperty()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        M(X)
    End Sub

    Sub M(ByRef p As Integer())
        p = New Integer(100) {}
    End Sub

    Dim s As String()

    Property X(ParamArray a As String()) As Integer()
        Get
            s = a
            Return New Integer() {}
        End Get
        Set(ByVal value As Integer())
            Console.WriteLine(s Is a)
        End Set
    End Property
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[False]]>).
            VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (Integer() V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     "String"
  IL_0006:  call       "Function M.get_X(ParamArray String()) As Integer()"
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Sub M.M(ByRef Integer())"
  IL_0013:  ldc.i4.0
  IL_0014:  newarr     "String"
  IL_0019:  ldloc.0
  IL_001a:  call       "Sub M.set_X(ParamArray String(), Integer())"
  IL_001f:  ret
}
]]>)
        End Sub

        <WorkItem(545404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545404")>
        <Fact()>
        Public Sub Bug13798()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections
Class CLS
    Implements IEnumerable

    Public Shared Sub Main(args() As String)
        Dim x = New CLS() From {1, 2, 3}
    End Sub

    Partial Private Sub Add(i As Integer)
    End Sub

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class
    </file>
</compilation>,
expectedOutput:="").
            VerifyIL("CLS.Main",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  newobj     "Sub CLS..ctor()"
  IL_0005:  pop
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub PropertiesWithInconsistentAccessors()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class D3
    Inherits D2

    Public Overrides WriteOnly Property P_rw_r_w As Integer
        Set(value As Integer)
            MyBase.P_rw_r_w = value
        End Set
    End Property

    Public Overrides ReadOnly Property P_rw_rw_r As Integer
        Get
            Return MyBase.P_rw_rw_r
        End Get
    End Property

    Public Overrides WriteOnly Property P_rw_rw_w As Integer
        Set(value As Integer)
            MyBase.P_rw_rw_w = value
        End Set
    End Property

    Public Sub Test()
        Dim tmp As Integer

        tmp = Me.P_rw_r_w
        tmp = MyClass.P_rw_r_w
        tmp = MyBase.P_rw_r_w

        Me.P_rw_r_w = tmp
        MyClass.P_rw_r_w = tmp
        MyBase.P_rw_r_w = tmp

        tmp = Me.P_rw_rw_r
        tmp = MyClass.P_rw_rw_r
        tmp = MyBase.P_rw_rw_r

        Me.P_rw_rw_r = tmp
        MyClass.P_rw_rw_r = tmp
        MyBase.P_rw_rw_r = tmp

        tmp = Me.P_rw_rw_w
        tmp = MyClass.P_rw_rw_w
        tmp = MyBase.P_rw_rw_w

        Me.P_rw_rw_w = tmp
        MyClass.P_rw_rw_w = tmp
        MyBase.P_rw_rw_w = tmp

    End Sub

End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ClassesWithReadWriteProperties.Value, TestOptions.ReleaseDll).
            VerifyIL("D3.Test",
            <![CDATA[
{
  // Code size      127 (0x7f)
  .maxstack  2
  .locals init (Integer V_0) //tmp
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function D1.get_P_rw_r_w() As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function D1.get_P_rw_r_w() As Integer"
  IL_000d:  stloc.0
  IL_000e:  ldarg.0
  IL_000f:  call       "Function D1.get_P_rw_r_w() As Integer"
  IL_0014:  stloc.0
  IL_0015:  ldarg.0
  IL_0016:  ldloc.0
  IL_0017:  callvirt   "Sub D3.set_P_rw_r_w(Integer)"
  IL_001c:  ldarg.0
  IL_001d:  ldloc.0
  IL_001e:  call       "Sub D3.set_P_rw_r_w(Integer)"
  IL_0023:  ldarg.0
  IL_0024:  ldloc.0
  IL_0025:  call       "Sub D2.set_P_rw_r_w(Integer)"
  IL_002a:  ldarg.0
  IL_002b:  callvirt   "Function D3.get_P_rw_rw_r() As Integer"
  IL_0030:  stloc.0
  IL_0031:  ldarg.0
  IL_0032:  call       "Function D3.get_P_rw_rw_r() As Integer"
  IL_0037:  stloc.0
  IL_0038:  ldarg.0
  IL_0039:  call       "Function D2.get_P_rw_rw_r() As Integer"
  IL_003e:  stloc.0
  IL_003f:  ldarg.0
  IL_0040:  ldloc.0
  IL_0041:  callvirt   "Sub D1.set_P_rw_rw_r(Integer)"
  IL_0046:  ldarg.0
  IL_0047:  ldloc.0
  IL_0048:  call       "Sub D1.set_P_rw_rw_r(Integer)"
  IL_004d:  ldarg.0
  IL_004e:  ldloc.0
  IL_004f:  call       "Sub D1.set_P_rw_rw_r(Integer)"
  IL_0054:  ldarg.0
  IL_0055:  callvirt   "Function D1.get_P_rw_rw_w() As Integer"
  IL_005a:  stloc.0
  IL_005b:  ldarg.0
  IL_005c:  call       "Function D1.get_P_rw_rw_w() As Integer"
  IL_0061:  stloc.0
  IL_0062:  ldarg.0
  IL_0063:  call       "Function D1.get_P_rw_rw_w() As Integer"
  IL_0068:  stloc.0
  IL_0069:  ldarg.0
  IL_006a:  ldloc.0
  IL_006b:  callvirt   "Sub D3.set_P_rw_rw_w(Integer)"
  IL_0070:  ldarg.0
  IL_0071:  ldloc.0
  IL_0072:  call       "Sub D3.set_P_rw_rw_w(Integer)"
  IL_0077:  ldarg.0
  IL_0078:  ldloc.0
  IL_0079:  call       "Sub D2.set_P_rw_rw_w(Integer)"
  IL_007e:  ret
}
]]>)
        End Sub

        Public Shared ReadOnly AttributesWithReadWriteProperties As XCData = <![CDATA[
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 FF 7F 00 00 00 00 )

  .method public hidebysig newslot specialname virtual
          instance int32  get_P_rw_r_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method B::get_P_rw_r_w

  .method public hidebysig newslot specialname virtual
          instance void  set_P_rw_r_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method B::set_P_rw_r_w

  .method public hidebysig newslot specialname virtual
          instance int32  get_P_rw_rw_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method B::get_P_rw_rw_w

  .method public hidebysig newslot specialname virtual
          instance void  set_P_rw_rw_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method B::set_P_rw_rw_w

  .method public hidebysig newslot specialname virtual
          instance int32  get_P_rw_rw_r() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method B::get_P_rw_rw_r

  .method public hidebysig newslot specialname virtual
          instance void  set_P_rw_rw_r(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method B::set_P_rw_rw_r

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

  .property instance int32 P_rw_r_w()
  {
    .get instance int32 B::get_P_rw_r_w()
    .set instance void B::set_P_rw_r_w(int32)
  } // end of property B::P_rw_r_w
  .property instance int32 P_rw_rw_w()
  {
    .set instance void B::set_P_rw_rw_w(int32)
    .get instance int32 B::get_P_rw_rw_w()
  } // end of property B::P_rw_rw_w
  .property instance int32 P_rw_rw_r()
  {
    .get instance int32 B::get_P_rw_rw_r()
    .set instance void B::set_P_rw_rw_r(int32)
  } // end of property B::P_rw_rw_r
} // end of class B

.class public auto ansi beforefieldinit D1
       extends B
{
  .method public hidebysig specialname virtual
          instance int32  get_P_rw_r_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D1::get_P_rw_r_w

  .method public hidebysig specialname virtual
          instance int32  get_P_rw_rw_w() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D1::get_P_rw_rw_w

  .method public hidebysig specialname virtual
          instance void  set_P_rw_rw_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D1::set_P_rw_rw_w

  .method public hidebysig specialname virtual
          instance int32  get_P_rw_rw_r() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D1::get_P_rw_rw_r

  .method public hidebysig specialname virtual
          instance void  set_P_rw_rw_r(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D1::set_P_rw_rw_r

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void B::.ctor()
    IL_0006:  ret
  } // end of method D1::.ctor

  .property instance int32 P_rw_r_w()
  {
    .get instance int32 D1::get_P_rw_r_w()
  } // end of property D1::P_rw_r_w
  .property instance int32 P_rw_rw_w()
  {
    .get instance int32 D1::get_P_rw_rw_w()
    .set instance void D1::set_P_rw_rw_w(int32)
  } // end of property D1::P_rw_rw_w
  .property instance int32 P_rw_rw_r()
  {
    .get instance int32 D1::get_P_rw_rw_r()
    .set instance void D1::set_P_rw_rw_r(int32)
  } // end of property D1::P_rw_rw_r
} // end of class D1

.class public auto ansi beforefieldinit D2
       extends D1
{
  .method public hidebysig specialname virtual
          instance void  set_P_rw_r_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D2::set_P_rw_r_w

  .method public hidebysig specialname virtual
          instance void  set_P_rw_rw_w(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method D2::set_P_rw_rw_w

  .method public hidebysig specialname virtual
          instance int32  get_P_rw_rw_r() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method D2::get_P_rw_rw_r

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void D1::.ctor()
    IL_0006:  ret
  } // end of method D2::.ctor

  .property instance int32 P_rw_r_w()
  {
    .set instance void D2::set_P_rw_r_w(int32)
  } // end of property D2::P_rw_r_w
  .property instance int32 P_rw_rw_w()
  {
    .set instance void D2::set_P_rw_rw_w(int32)
  } // end of property D2::P_rw_rw_w
  .property instance int32 P_rw_rw_r()
  {
    .get instance int32 D2::get_P_rw_rw_r()
  } // end of property D2::P_rw_rw_r
} // end of class D2

.class public auto ansi beforefieldinit XXX
       extends [mscorlib]System.Object
{
  .custom instance void D2::.ctor() = ( 01 00 03 00 54 08 08 50 5F 72 77 5F 72 5F 77 01   // ....T..P_rw_r_w.
                                        00 00 00 54 08 09 50 5F 72 77 5F 72 77 5F 77 02   // ...T..P_rw_rw_w.
                                        00 00 00 54 08 09 50 5F 72 77 5F 72 77 5F 72 03   // ...T..P_rw_rw_r.
                                        00 00 00 )
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XXX::.ctor

} // end of class XXX
]]>

        <Fact()>
        Public Sub AttributesWithInconsistentPropertiesAccessors()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Reflection
Imports System.Collections.Generic

&lt;D2(P_rw_r_w:=1, P_rw_rw_w:=2)&gt;
Class AttrTest
End Class

Module M
    Sub Main()
        PrintAttributeData(New AttrTest().GetType().GetCustomAttributesData())
        PrintAttributeData(New XXX().GetType().GetCustomAttributesData())
    End Sub

    Sub PrintAttributeData(a As IList(Of CustomAttributeData))
        For Each ad In a
            Console.WriteLine(ad.Constructor.ToString() &amp; "(.." &amp; ad.ConstructorArguments.Count.ToString() &amp; "..)")
            For Each na In ad.NamedArguments
                Console.WriteLine(na.MemberInfo.ToString() &amp; ":=" &amp; na.TypedValue.ToString())
            Next
        Next
    End Sub
End Module

    </file>
</compilation>

            CompileWithCustomILSource(vbSource, AttributesWithReadWriteProperties.Value,
                                      options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                                      expectedOutput:=<![CDATA[
Void .ctor()(..0..)
Int32 P_rw_r_w:=(Int32)1
Int32 P_rw_rw_w:=(Int32)2
Void .ctor()(..0..)
Int32 P_rw_r_w:=(Int32)1
Int32 P_rw_rw_w:=(Int32)2
Int32 P_rw_rw_r:=(Int32)3
]]>.Value.Replace(vbLf, Environment.NewLine))
        End Sub

        <Fact()>
        Public Sub TestByRefMethodWithParamArrayProperty2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        M(X(New String(1) {}))
    End Sub

    Sub M(ByRef p As Integer())
        p = New Integer(100) {}
    End Sub

    Dim s As String()

    Property X(ParamArray a As String()) As Integer()
        Get
            s = a
            Return New Integer() {}
        End Get
        Set(ByVal value As Integer())
            Console.WriteLine(s Is a)
        End Set
    End Property
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[True]]>).
            VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (Integer() V_0)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     "String"
  IL_0006:  dup
  IL_0007:  call       "Function M.get_X(ParamArray String()) As Integer()"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub M.M(ByRef Integer())"
  IL_0014:  ldloc.0
  IL_0015:  call       "Sub M.set_X(ParamArray String(), Integer())"
  IL_001a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestByRefMethodWithByRefParamArrayProperty()
            Dim ilSource = <![CDATA[
.class public auto ansi sealed TestModule
       extends [mscorlib]System.Object
{
  .field private static string[] s

  .method public specialname static int32[]
          get_X(string[]& a) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] int32[] X)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stsfld     string[] TestModule::s
    IL_0007:  ldc.i4.0
    IL_0008:  newarr     [mscorlib]System.Int32
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method RedimTest::get_X

  .method public specialname static void
          set_X(string[]& a,
                int32[] 'value') cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldsfld     string[] TestModule::s
    IL_0006:  ldarg.0
    IL_0007:  ceq
    IL_0009:  call       void [mscorlib]System.Console::WriteLine(bool)
    IL_000e:  nop
    IL_000f:  nop
    IL_0010:  ret
  } // end of method RedimTest::set_X

  .property int32[] X(string[]&)
  {
    .set void TestModule::set_X(string[]&,
                                             int32[])
    .get int32[] TestModule::get_X(string[]&)
  } // end of property RedimTest::X
} // end of class ClassLibrary1.RedimTest
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        M(TestModule.X)
    End Sub

    Sub M(ByRef p As Integer())
        p = New Integer(100) {}
    End Sub
End Module
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("M.Main",
            <![CDATA[
                                               {
                                                  // Code size       38 (0x26)
                                                  .maxstack  2
                                                  .locals init (Integer() V_0,
                                                  String() V_1)
                                                  IL_0000:  ldc.i4.0
                                                  IL_0001:  newarr     "String"
                                                  IL_0006:  stloc.1
                                                  IL_0007:  ldloca.s   V_1
                                                  IL_0009:  call       "Function TestModule.get_X(ByRef ParamArray String()) As Integer()"
                                                  IL_000e:  stloc.0
                                                  IL_000f:  ldloca.s   V_0
                                                  IL_0011:  call       "Sub M.M(ByRef Integer())"
                                                  IL_0016:  ldc.i4.0
                                                  IL_0017:  newarr     "String"
                                                  IL_001c:  stloc.1
                                                  IL_001d:  ldloca.s   V_1
                                                  IL_001f:  ldloc.0
                                                  IL_0020:  call       "Sub TestModule.set_X(ByRef ParamArray String(), Integer())"
                                                  IL_0025:  ret
                                               }
                                               ]]>)
        End Sub

        <WorkItem(546853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546853")>
        <Fact()>
        Public Sub CallingVirtualFinalMethod()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance bool  get_M1() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  }

  .method public hidebysig newslot specialname virtual final instance bool  get_M2() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

  .property instance bool M1()
  {
    .get instance bool B::get_M1()
  }

  .property instance bool M2()
  {
    .get instance bool B::get_M2()
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Module C
  Sub S()
    Dim b  As Boolean = (New B()).M1 AndAlso (New B()).M2
  End Sub
End Module
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.S",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  newobj     "Sub B..ctor()"
  IL_0005:  callvirt   "Function B.get_M1() As Boolean"
  IL_000a:  brfalse.s  IL_0018
  IL_000c:  newobj     "Sub B..ctor()"
  IL_0011:  call       "Function B.get_M2() As Boolean"
  IL_0016:  br.s       IL_0019
  IL_0018:  ldc.i4.0
  IL_0019:  pop
  IL_001a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestToStringOnStruct()
            Dim ilSource = <![CDATA[
.class sequential ansi sealed public Struct1
         extends [mscorlib]System.ValueType
{
    .method public hidebysig virtual instance string
            ToString() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      .locals init (string V_0)
      IL_0000:  nop
      IL_0001:  ldstr      "Struct1 "
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0009
      IL_0009:  ldloc.0
      IL_000a:  ret
    }
}
.class sequential ansi sealed public Struct2
         extends [mscorlib]System.ValueType
{
    .method public strict virtual instance string
            ToString() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      .locals init (string V_0)
      IL_0000:  nop
      IL_0001:  ldstr      "Struct2 "
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0009
      IL_0009:  ldloc.0
      IL_000a:  ret
    }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim s1 As New Struct1()
        Console.Write(s1.ToString())
        Dim s2 As New Struct2()
        Console.Write(s2.ToString())
    End Sub
End Module
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (Struct1 V_0, //s1
  Struct2 V_1) //s2
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Struct1"
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. "Struct1"
  IL_0010:  callvirt   "Function Object.ToString() As String"
  IL_0015:  call       "Sub System.Console.Write(String)"
  IL_001a:  ldloca.s   V_1
  IL_001c:  initobj    "Struct2"
  IL_0022:  ldloca.s   V_1
  IL_0024:  constrained. "Struct2"
  IL_002a:  callvirt   "Function Object.ToString() As String"
  IL_002f:  call       "Sub System.Console.Write(String)"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestRedimWithObjectAndProperty()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class RedimTest
    Public Shared Sub Main()
        ReDim Func.Prop()(2)
        DirectCast(Func.Prop, Object())(1) = "stored value"
        ReDim Preserve Func.Prop()(5)
        System.Console.WriteLine(DirectCast(Func.Prop, Object())(1))
    End Sub

    Public Shared Function Func() As RedimTest
        Return instance
    End Function

    Private Shared instance As RedimTest = New RedimTest()

    Public _Prop As Object
    Public Property Prop As Object
        Get
            System.Console.WriteLine("Reading")
            Return _Prop
        End Get
        Set(value As Object)
            System.Console.WriteLine("Writing")
            _Prop = value
        End Set
    End Property
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
Writing
Reading
Reading
Writing
Reading
stored value
]]>).
            VerifyIL("RedimTest.Main",
            <![CDATA[
{
// Code size      100 (0x64)
.maxstack  3
.locals init (RedimTest V_0)
IL_0000:  call       "Function RedimTest.Func() As RedimTest"
IL_0005:  ldc.i4.3
IL_0006:  newarr     "Object"
IL_000b:  callvirt   "Sub RedimTest.set_Prop(Object)"
IL_0010:  call       "Function RedimTest.Func() As RedimTest"
IL_0015:  callvirt   "Function RedimTest.get_Prop() As Object"
IL_001a:  castclass  "Object()"
IL_001f:  ldc.i4.1
IL_0020:  ldstr      "stored value"
IL_0025:  stelem.ref
IL_0026:  call       "Function RedimTest.Func() As RedimTest"
IL_002b:  dup
IL_002c:  stloc.0
IL_002d:  ldloc.0
IL_002e:  callvirt   "Function RedimTest.get_Prop() As Object"
IL_0033:  castclass  "System.Array"
IL_0038:  ldc.i4.6
IL_0039:  newarr     "Object"
IL_003e:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
IL_0043:  callvirt   "Sub RedimTest.set_Prop(Object)"
IL_0048:  call       "Function RedimTest.Func() As RedimTest"
IL_004d:  callvirt   "Function RedimTest.get_Prop() As Object"
IL_0052:  castclass  "Object()"
IL_0057:  ldc.i4.1
IL_0058:  ldelem.ref
IL_0059:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
IL_005e:  call       "Sub System.Console.WriteLine(Object)"
IL_0063:  ret
}
]]>)

        End Sub

        <WorkItem(529442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529442")>
        <Fact>
        Public Sub ExplicitStandardModuleAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

&lt;Global.Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute()&gt;
Public Module Module1
    Public Sub Main()
        Console.WriteLine(
            Attribute.GetCustomAttributes(
                GetType(Module1),
                GetType(Global.Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute)
            ).Length
        )
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="2")
        End Sub

        <Fact()>
        Public Sub EmitObjectGetTypeCallOnStruct()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
End Structure

Class MainClass
    Public Shared Sub Main()
        System.Console.WriteLine((New S1()).GetType())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
S1
]]>).
            VerifyIL("MainClass.Main",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S1"
  IL_0008:  ldloc.0
  IL_0009:  box        "S1"
  IL_000e:  call       "Function Object.GetType() As System.Type"
  IL_0013:  call       "Sub System.Console.WriteLine(Object)"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmitCallToOverriddenToStringOnStruct()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Structure S1
    Public Overrides Function ToString() As String
        Return "123"
    End Function
End Structure

Class MainClass
    Public Shared Sub Main()
        Dim s As S1 = New S1()
        System.Console.WriteLine(s.ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
123
]]>).
            VerifyIL("MainClass.Main",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (S1 V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S1"
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. "S1"
  IL_0010:  callvirt   "Function Object.ToString() As String"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmitInterfaceMethodOnStruct()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Interface I
    Sub M()
End Interface

Public Structure S1
    Implements I

    Public Sub M() Implements I.M
        System.Console.WriteLine("S1:M")
    End Sub
End Structure

Class MainClass
    Public Shared Sub Main()
        Dim s As S1 = New S1()
        s.M()
        DirectCast(s, I).M()
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
S1:M
S1:M
]]>).
            VerifyIL("MainClass.Main",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  1
  .locals init (S1 V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S1"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Sub S1.M()"
  IL_000f:  ldloc.0
  IL_0010:  box        "S1"
  IL_0015:  castclass  "I"
  IL_001a:  callvirt   "Sub I.M()"
  IL_001f:  ret
}
]]>)
        End Sub

        <WorkItem(531085, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531085")>
        <Fact()>
        Public Sub EmitMyBaseCall()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Base1
    Protected memberee As Integer = 0
    Protected Property xyz As Integer
    Default Protected Property this(num As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
Class Derived : Inherits Base1
    Public Sub inc()
        MyBase.memberee += 1
        MyBase.xyz += 1
        MyBase.this(0) += 1
    End Sub
End Class
Module Program
    Sub Main(args As String())
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyIL("Derived.inc",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (Integer& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "Base1.memberee As Integer"
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldind.i4
  IL_000a:  ldc.i4.1
  IL_000b:  add.ovf
  IL_000c:  stind.i4
  IL_000d:  ldarg.0
  IL_000e:  ldarg.0
  IL_000f:  call       "Function Base1.get_xyz() As Integer"
  IL_0014:  ldc.i4.1
  IL_0015:  add.ovf
  IL_0016:  call       "Sub Base1.set_xyz(Integer)"
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldarg.0
  IL_001e:  ldc.i4.0
  IL_001f:  call       "Function Base1.get_this(Integer) As Integer"
  IL_0024:  ldc.i4.1
  IL_0025:  add.ovf
  IL_0026:  call       "Sub Base1.set_this(Integer, Integer)"
  IL_002b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmitObjectToStringOnSimpleType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class MainClass
    Public Shared Sub Main()
        Dim x as Integer = 123
        Console.WriteLine(x.ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[123]]>).
            VerifyIL("MainClass.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Function Integer.ToString() As String"
  IL_000a:  call       "Sub System.Console.WriteLine(String)"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmitObjectMethodOnSpecialByRefType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class MainClass
    Public Shared Sub Main()
    End Sub
    Sub M(tr As System.TypedReference)
        Dim i As Integer = tr.GetHashCode()
    End Sub
End Class
    </file>
</compilation>).
            VerifyIL("MainClass.M",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarga.s   V_1
  IL_0002:  call       "Function System.TypedReference.GetHashCode() As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EmitNonVirtualInstanceEnumMethodCallOnEnum()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Enum Shade
    White
    Gray
    Black
End Enum

Class MainClass
    Public Shared Sub Main()
        Dim v As Shade = Shade.Gray
        System.Console.WriteLine(v.GetType())
        System.Console.WriteLine(v.HasFlag(Shade.Black))
        System.Console.WriteLine(v.ToString("G"))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
Shade
False
Gray
]]>).
            VerifyIL("MainClass.Main",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (Shade V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        "Shade"
  IL_0008:  call       "Function Object.GetType() As System.Type"
  IL_000d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0012:  ldloc.0
  IL_0013:  box        "Shade"
  IL_0018:  ldc.i4.2
  IL_0019:  box        "Shade"
  IL_001e:  call       "Function System.Enum.HasFlag(System.Enum) As Boolean"
  IL_0023:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0028:  ldloc.0
  IL_0029:  box        "Shade"
  IL_002e:  ldstr      "G"
  IL_0033:  call       "Function System.Enum.ToString(String) As String"
  IL_0038:  call       "Sub System.Console.WriteLine(String)"
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestTernaryConditionalOperatorInterfaceRegression()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Interface IA
End Interface
Public Interface IB
    Function f() As Integer
End Interface
Public Class AB1
    Implements IA, IB
    Public Function f() As Integer Implements IB.f
        Return 42
    End Function
End Class
Public Class AB2
    Implements IA, IB
    Public Function f() As Integer Implements IB.f
        Return 1
    End Function
End Class
Class MainClass
    Public Shared Function g(p As Boolean) As Integer
        Return (If(p, DirectCast(New AB1(), IB), DirectCast(New AB2(), IB))).f()
    End Function
    Public Shared Sub Main()
        System.Console.WriteLine(g(True))
        System.Console.WriteLine(g(False))
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
42
1
]]>).
            VerifyIL("MainClass.g",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (IB V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  newobj     "Sub AB2..ctor()"
  IL_0008:  br.s       IL_0011
  IL_000a:  newobj     "Sub AB1..ctor()"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Function IB.f() As Integer"
  IL_0016:  ret
}
]]>)
        End Sub

        <WorkItem(546809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546809")>
        <Fact()>
        Public Sub TestBinaryConditionalOperator_16872a()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Interface I
End Interface

Class C1
    Implements I
End Class

Class C2
    Implements I
End Class

Module Program
    Sub Main(args As String())
        Console.WriteLine(F().ToString())
    End Sub

    Public Function F() As I
        Dim i As I = F1()
        Return If(i, F2())
    End Function

    Public Function F1() As C1
        Return New C1()
    End Function

    Public Function F2() As C2
        Return New C2()
    End Function
End Module
    </file>
</compilation>,
expectedOutput:="C1").
            VerifyIL("Program.F",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (I V_0)
  IL_0000:  call       "Function Program.F1() As C1"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_0010
  IL_000a:  pop
  IL_000b:  call       "Function Program.F2() As C2"
  IL_0010:  ret
}
]]>)
        End Sub

        <WorkItem(634407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634407")>
        <Fact()>
        Public Sub TestTernary_Null()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Security

<Assembly: SecurityTransparent()>

Class Program
    Private Shared Function C() As Boolean
        Return True
    End Function

    Public Shared Sub Main()
        Dim f1 As Exception() = Nothing

        Dim oo = If(C(), f1, TryCast(Nothing, IEnumerable(Of Object)))
        Console.WriteLine(oo)

        Dim oo1 = If(C(), DirectCast(DirectCast(Nothing, IEnumerable(Of Object)), IEnumerable(Of Object)), f1)
        Console.WriteLine(oo1)
    End Sub
End Class
]]>
    </file>
</compilation>, expectedOutput:="").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (System.Exception() V_0) //f1
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.C() As Boolean"
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  ldnull
  IL_000a:  br.s       IL_000d
  IL_000c:  ldloc.0
  IL_000d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0012:  call       "Function Program.C() As Boolean"
  IL_0017:  brtrue.s   IL_001c
  IL_0019:  ldloc.0
  IL_001a:  br.s       IL_001d
  IL_001c:  ldnull
  IL_001d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0022:  ret
}
]]>)
        End Sub

        <WorkItem(546809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546809")>
        <Fact()>
        Public Sub TestBinaryConditionalOperator_16872b()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Interface I
End Interface

Class C1
    Implements I
End Class

Class C2
    Implements I
End Class

Module Program
    Sub Main(args As String())
        Console.WriteLine(F().ToString())
    End Sub

    Public Function F() As I
        Dim i As I = F1()
        Return If(i Is Nothing, F2(), i)
    End Function

    Public Function F1() As C1
        Return New C1()
    End Function

    Public Function F2() As C2
        Return New C2()
    End Function
End Module
    </file>
</compilation>,
expectedOutput:="C1").
            VerifyIL("Program.F",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (I V_0) //i
  IL_0000:  call       "Function Program.F1() As C1"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_000b
  IL_0009:  ldloc.0
  IL_000a:  ret
  IL_000b:  call       "Function Program.F2() As C2"
  IL_0010:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestBinaryConditionalOperatorInterfaceRegression()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Interface IA
End Interface
Public Interface IB
    Function f() As Integer
End Interface
Public Class AB1
    Implements IA, IB
    Public Function f() As Integer Implements IB.f
        Return 42
    End Function
End Class
Public Class AB2
    Implements IA, IB
    Public Function f() As Integer Implements IB.f
        Return 1
    End Function
End Class
Class MainClass
    Public Shared Function g() As Integer
        Return (If(DirectCast(New AB1(), IB), DirectCast(New AB2(), IB))).f()
    End Function
    Public Shared Sub Main()
        System.Console.WriteLine(g())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
42
]]>).
            VerifyIL("MainClass.g",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (IB V_0)
  IL_0000:  newobj     "Sub AB1..ctor()"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0010
  IL_0008:  pop
  IL_0009:  newobj     "Sub AB2..ctor()"
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  callvirt   "Function IB.f() As Integer"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestTernaryConditionalExpression()

            CompileAndVerify(
<compilation>
    <file name="a.vb">Imports System
Imports System.Globalization

Class EmitTest
    Shared Function GetCultureInvariantString(val As Object) As String
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

    Public Shared Sub Main()
        WriteResult("Test01", Test01(New EmitTest))
        WriteResult("Test02", Test02(New EmitTest))
        WriteResult("Test03", Test03(False))
        WriteResult("Test04", Test04)
        WriteResult("Test05", Test05)
        WriteResult("Test06", Test06)
        WriteResult("Test07", Test07("s"))
    End Sub

    Public Shared Sub WriteResult(name As String, result As Object)
        Dim val = GetCultureInvariantString(result)
        Console.WriteLine("{0}:  {1}", name.PadLeft(10),
                                 If(result Is Nothing, "&lt;Nothing&gt;", String.Format("{0}||{1}", val, result.GetType.FullName)))
    End Sub

    Public Property ObjProp As Object
    Public Property StrProp As String

    Public Shared Function Test01(a As EmitTest) As Object
        Return If(0, a.ObjProp, a.StrProp)
    End Function

    Public Shared Function Test02(a As EmitTest) As Object
        Return If(True, #1/1/1#, a.StrProp)
    End Function

    Public Shared Function Test03(b As Boolean) As Object
        Dim ch As Char() = Nothing
        Return If(b, ch, "if-false")
    End Function

    Public Shared Function Test04() As Object
        Return If(True, Nothing, #1/1/2000#)
    End Function

    Public Shared Function Test05() As Object
        Return If(False, 1, Nothing)
    End Function

    Public Shared Function Test06() As Object
        Return If(1.55, 1 / 0, 1)
    End Function

    Public Shared Function Test07(s As String) As Object
        Return If(true, s + "tr", Nothing)
    End Function

    Interface I1
    End Interface

    Class CLS
        Implements I1
    End Class

End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
    Test01:  <Nothing>
    Test02:  1/1/0001 12:00:00 AM||System.DateTime
    Test03:  if-false||System.String
    Test04:  1/1/0001 12:00:00 AM||System.DateTime
    Test05:  0||System.Int32
    Test06:  Infinity||System.Double
    Test07:  str||System.String
]]>).
            VerifyIL("EmitTest.Test07",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "tr"
  IL_0006:  call       "Function String.Concat(String, String) As String"
  IL_000b:  ret
}
]]>).
            VerifyIL("EmitTest.Test06",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldc.r8     Infinity
  IL_0009:  box        "Double"
  IL_000e:  ret
}
]]>).
            VerifyIL("EmitTest.Test05",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  box        "Integer"
  IL_0006:  ret
}
]]>).
            VerifyIL("EmitTest.Test04",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsfld     "Date.MinValue As Date"
  IL_0005:  box        "Date"
  IL_000a:  ret
}
]]>).
            VerifyIL("EmitTest.Test03",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Char() V_0) //ch
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  brtrue.s   IL_000b
  IL_0005:  ldstr      "if-false"
  IL_000a:  ret
  IL_000b:  ldloc.0
  IL_000c:  newobj     "Sub String..ctor(Char())"
  IL_0011:  ret
}
]]>).
            VerifyIL("EmitTest.Test02",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsfld     "Date.MinValue As Date"
  IL_0005:  box        "Date"
  IL_000a:  ret
}
]]>).
            VerifyIL("EmitTest.Test01",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function EmitTest.get_StrProp() As String"
  IL_0006:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub TestBinaryConditionalExpression()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class EmitTest

    Public Shared Sub Main()
        WriteResult("Test01", Test01)
        WriteResult("Test02", Test02)
        WriteResult("Test03", Test03)
        WriteResult("Test04", Test04("z"))
        WriteResult("Test05", Test05)
        WriteResult("Test06", Test06("xyz"))
        WriteResult("Test07", Test07(New EmitTest))
        WriteResult("Test08", Test08(New EmitTest))
        WriteResult("Test09", Test09(New EmitTest))
        WriteResult("Test10", Test10)
        WriteResult("Test11", Test11)
        WriteResult("Test12", Test12(New EmitTest, "a"))
        WriteResult("Test13", Test13(New EmitTest, "a"))
        WriteResult("Test14", Test14(Nothing))
        WriteResult("Test15", Test15)
        WriteResult("Test16", Test16)
    End Sub

   Shared cul = System.Globalization.CultureInfo.InvariantCulture
    Public Shared Sub WriteResult(name As String, result As Object)
        If result IsNot Nothing AndAlso result.GetType() Is GetType(System.DateTime) Then
            System.Console.WriteLine("{0}:  {1}", name.PadLeft(10), String.Format("{0}||{1}",
                        CType(result, System.DateTime).ToString("M/d/yyyy h:mm:ss tt", cul), result.GetType.FullName))
        Else
            System.Console.WriteLine("{0}:  {1}", name.PadLeft(10),
                                 If(result Is Nothing, "&lt;Nothing&gt;", String.Format("{0}||{1}", result, result.GetType.FullName)))
        End If
    End Sub

    Public Property ObjProp As Object
    Public Property StrProp As String

    Public Shared Function Test01() As Date
        Return If(Nothing, #12:00:00 AM#)
    End Function

    Public Shared Function Test02() As String
        Return If("abc", #12:00:00 AM#)
    End Function

    Public Shared Function Test03() As String
        Return If("cde", Nothing)
    End Function

    Public Shared Function Test04(a As String) As String
        Return If(Nothing, "a" + a)
    End Function

    Public Shared Function Test05() As Object
        Return If(Nothing, CType(CType(Nothing, String), Object))
    End Function

    Public Shared Function Test06(a As String) As String
        Return If(a, "No Value")
    End Function

    Public Shared Function Test07(a As EmitTest) As Object
        Return If(a.ObjProp, New Object)
    End Function

    Public Shared Function Test08(a As EmitTest) As Object
        Return If(a.StrProp, #12:00:00 AM#)
    End Function

    Public Shared Function Test09(a As EmitTest) As String
        Return If(a.StrProp + "abc", "xyz")
    End Function

    Public Shared Function Test10() As Object
        Dim a As String = "abc"
        Return If(a, "No Value")
    End Function

    Public Shared Function Test11() As I1
        Dim i As I1 = Nothing
        Dim c As CLS = new CLS
        Return If(c, i)
    End Function

    Public Property CharArrProp As Char()

    Public Shared Function Test12(a As EmitTest, s As String) As String
        Return If(a.CharArrProp, s + "b")
    End Function

    Public Shared Function Test13(a As EmitTest, s As String) As String
        Return If(s + "b", a.CharArrProp)
    End Function

    Public Shared Function Test14(a As Char()) As String
        Return If(a, "b")
    End Function

    Public Shared Function Test15() As String
        Dim a As Char() = Nothing
        Return If(a, "b")
    End Function

    Public Shared Function Test16() As Object
        Return If(CType(Nothing, String), "else-branch")
    End Function

    Interface I1
    End Interface

    Class CLS
        Implements I1
    End Class

End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
    Test01:  1/1/0001 12:00:00 AM||System.DateTime
    Test02:  abc||System.String
    Test03:  cde||System.String
    Test04:  az||System.String
    Test05:  <Nothing>
    Test06:  xyz||System.String
    Test07:  System.Object||System.Object
    Test08:  1/1/0001 12:00:00 AM||System.DateTime
    Test09:  abc||System.String
    Test10:  abc||System.String
    Test11:  EmitTest+CLS||EmitTest+CLS
    Test12:  ab||System.String
    Test13:  ab||System.String
    Test14:  b||System.String
    Test15:  b||System.String
    Test16:  else-branch||System.String
]]>).
            VerifyIL("EmitTest.Test16",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldstr      "else-branch"
  IL_0005:  ret
}
]]>).
            VerifyIL("EmitTest.Test15",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Char() V_0) //a
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brtrue.s   IL_000b
  IL_0005:  ldstr      "b"
  IL_000a:  ret
  IL_000b:  ldloc.0
  IL_000c:  newobj     "Sub String..ctor(Char())"
  IL_0011:  ret
}
]]>).
            VerifyIL("EmitTest.Test14",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldstr      "b"
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  newobj     "Sub String..ctor(Char())"
  IL_000f:  ret
}
]]>).
            VerifyIL("EmitTest.Test13",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      "b"
  IL_0006:  call       "Function String.Concat(String, String) As String"
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_001a
  IL_000e:  pop
  IL_000f:  ldarg.0
  IL_0010:  callvirt   "Function EmitTest.get_CharArrProp() As Char()"
  IL_0015:  newobj     "Sub String..ctor(Char())"
  IL_001a:  ret
}
]]>).
            VerifyIL("EmitTest.Test12",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Char() V_0)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function EmitTest.get_CharArrProp() As Char()"
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  brtrue.s   IL_0016
  IL_000a:  ldarg.1
  IL_000b:  ldstr      "b"
  IL_0010:  call       "Function String.Concat(String, String) As String"
  IL_0015:  ret
  IL_0016:  ldloc.0
  IL_0017:  newobj     "Sub String..ctor(Char())"
  IL_001c:  ret
}
]]>).
            VerifyIL("EmitTest.Test11",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (EmitTest.I1 V_0) //i
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  newobj     "Sub EmitTest.CLS..ctor()"
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000c
  IL_000a:  pop
  IL_000b:  ldloc.0
  IL_000c:  ret
}
]]>).
            VerifyIL("EmitTest.Test10",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldstr      "abc"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000e
  IL_0008:  pop
  IL_0009:  ldstr      "No Value"
  IL_000e:  ret
}
]]>).
            VerifyIL("EmitTest.Test09",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function EmitTest.get_StrProp() As String"
  IL_0006:  ldstr      "abc"
  IL_000b:  call       "Function String.Concat(String, String) As String"
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_0019
  IL_0013:  pop
  IL_0014:  ldstr      "xyz"
  IL_0019:  ret
}
]]>).
            VerifyIL("EmitTest.Test08",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function EmitTest.get_StrProp() As String"
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  pop
  IL_000a:  ldsfld     "Date.MinValue As Date"
  IL_000f:  box        "Date"
  IL_0014:  ret
}
]]>).
            VerifyIL("EmitTest.Test07",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function EmitTest.get_ObjProp() As Object"
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000f
  IL_0009:  pop
  IL_000a:  newobj     "Sub Object..ctor()"
  IL_000f:  ret
}
]]>).
            VerifyIL("EmitTest.Test06",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000a
  IL_0004:  pop
  IL_0005:  ldstr      "No Value"
  IL_000a:  ret
}
]]>).
            VerifyIL("EmitTest.Test05",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}
]]>).
            VerifyIL("EmitTest.Test04",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldstr      "a"
  IL_0005:  ldarg.0
  IL_0006:  call       "Function String.Concat(String, String) As String"
  IL_000b:  ret
}
]]>).
            VerifyIL("EmitTest.Test03",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldstr      "cde"
  IL_0005:  ret
}
]]>).
            VerifyIL("EmitTest.Test02",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldstr      "abc"
  IL_0005:  ret
}
]]>).
            VerifyIL("EmitTest.Test01",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     "Date.MinValue As Date"
  IL_0005:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub TestBinaryConditionalExpression2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Class EmitTest

    Public Shared Sub Main()
        WriteResult("Test01", Test01)
        WriteResult("Test02", Test02)
        WriteResult("Test03", Test03("+"))
        WriteResult("Test04", Test04("+"))
    End Sub

    Public Shared Sub WriteResult(name As String, result As Object)
        System.Console.WriteLine("{0}:  {1}", name.PadLeft(10),
                                 If(result Is Nothing, "&lt;Nothing&gt;", String.Format("{0}||{1}", result, result.GetType.FullName)))
    End Sub

    Public Shared Function Test01() As SS
        Return If("+", New SS("-"))
    End Function

    Public Shared Function Test02() As SS
        Dim s As String = "+"
        Return If(s, New SS("-"))
    End Function

    Public Shared Function Test03(s As String) As SS
        Return If(s, New SS("-"))
    End Function

    Public Shared Function Test04(s As String) As SS
        Return If(s &amp; "+", New SS("-"))
    End Function

End Class

Structure SS
    Public V As String

    Public Sub New(s As String)
        Me.V = s
    End Sub

    Public Overrides Function ToString() As String
        Return V
    End Function

    Public Shared Widening Operator CType(s As String) As SS
        Return New SS(s)
    End Operator
End Structure

    </file>
</compilation>,
expectedOutput:=<![CDATA[
    Test01:  +||SS
    Test02:  +||SS
    Test03:  +||SS
    Test04:  ++||SS
]]>).
            VerifyIL("EmitTest.Test01",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "+"
  IL_0005:  call       "Function SS.op_Implicit(String) As SS"
  IL_000a:  ret
}
]]>).
            VerifyIL("EmitTest.Test02",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (String V_0) //s
  IL_0000:  ldstr      "+"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  ldstr      "-"
  IL_000e:  newobj     "Sub SS..ctor(String)"
  IL_0013:  ret
  IL_0014:  ldloc.0
  IL_0015:  call       "Function SS.op_Implicit(String) As SS"
  IL_001a:  ret
}
]]>).
            VerifyIL("EmitTest.Test03",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldstr      "-"
  IL_0008:  newobj     "Sub SS..ctor(String)"
  IL_000d:  ret
  IL_000e:  ldarg.0
  IL_000f:  call       "Function SS.op_Implicit(String) As SS"
  IL_0014:  ret
}
]]>).
            VerifyIL("EmitTest.Test04",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "+"
  IL_0006:  call       "Function String.Concat(String, String) As String"
  IL_000b:  dup
  IL_000c:  stloc.0
  IL_000d:  brtrue.s   IL_001a
  IL_000f:  ldstr      "-"
  IL_0014:  newobj     "Sub SS..ctor(String)"
  IL_0019:  ret
  IL_001a:  ldloc.0
  IL_001b:  call       "Function SS.op_Implicit(String) As SS"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestConditionalRequiringBox()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Sub Main()
        Dim a As Integer = 1
        Dim i As IGoo(Of String) = Nothing
        Dim e As New GooVal()

        i = If(a > 1, i, e)
        System.Console.Write(i.Goo())
    End Sub

    Interface IGoo(Of T) : Function Goo() As T : End Interface

    Structure GooVal : Implements IGoo(Of String)
        Public Function Goo() As String Implements IGoo(Of String).Goo
            Return "Val "
        End Function
    End Structure
End Module</file>
</compilation>,
        expectedOutput:="Val ").
        VerifyIL("Program.Main",
            <![CDATA[{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (Program.IGoo(Of String) V_0, //i
  Program.GooVal V_1) //e
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    "Program.GooVal"
  IL_000b:  ldc.i4.1
  IL_000c:  bgt.s      IL_001b
  IL_000e:  ldloc.1
  IL_000f:  box        "Program.GooVal"
  IL_0014:  castclass  "Program.IGoo(Of String)"
  IL_0019:  br.s       IL_001c
  IL_001b:  ldloc.0
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  callvirt   "Function Program.IGoo(Of String).Goo() As String"
  IL_0023:  call       "Sub System.Console.Write(String)"
  IL_0028:  ret
}]]>)
        End Sub

        <Fact>
        Public Sub TestCoalesceRequiringBox()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Sub Main()
        Dim i As IGoo(Of String) = Nothing
        Dim e As New GooVal()
        Dim n? As GooVal = e

        i = If(i, e)
        System.Console.Write(i.Goo())

        i = Nothing
        i = If(n, i)
        System.Console.Write(i.Goo())
    End Sub

    Interface IGoo(Of T) : Function Goo() As T : End Interface

    Structure GooVal : Implements IGoo(Of String)
        Public Function Goo() As String Implements IGoo(Of String).Goo
            Return "Val "
        End Function
    End Structure
End Module</file>
</compilation>,
        expectedOutput:="Val Val ").
        VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       90 (0x5a)
  .maxstack  2
  .locals init (Program.IGoo(Of String) V_0, //i
  Program.GooVal V_1, //e
  Program.GooVal? V_2) //n
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  initobj    "Program.GooVal"
  IL_000a:  ldloca.s   V_2
  IL_000c:  ldloc.1
  IL_000d:  call       "Sub Program.GooVal?..ctor(Program.GooVal)"
  IL_0012:  ldloc.0
  IL_0013:  dup
  IL_0014:  brtrue.s   IL_0022
  IL_0016:  pop
  IL_0017:  ldloc.1
  IL_0018:  box        "Program.GooVal"
  IL_001d:  castclass  "Program.IGoo(Of String)"
  IL_0022:  stloc.0
  IL_0023:  ldloc.0
  IL_0024:  callvirt   "Function Program.IGoo(Of String).Goo() As String"
  IL_0029:  call       "Sub System.Console.Write(String)"
  IL_002e:  ldnull
  IL_002f:  stloc.0
  IL_0030:  ldloca.s   V_2
  IL_0032:  call       "Function Program.GooVal?.get_HasValue() As Boolean"
  IL_0037:  brtrue.s   IL_003c
  IL_0039:  ldloc.0
  IL_003a:  br.s       IL_004d
  IL_003c:  ldloca.s   V_2
  IL_003e:  call       "Function Program.GooVal?.GetValueOrDefault() As Program.GooVal"
  IL_0043:  box        "Program.GooVal"
  IL_0048:  castclass  "Program.IGoo(Of String)"
  IL_004d:  stloc.0
  IL_004e:  ldloc.0
  IL_004f:  callvirt   "Function Program.IGoo(Of String).Goo() As String"
  IL_0054:  call       "Sub System.Console.Write(String)"
  IL_0059:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestNullCoalesce_NullableWithDefault_Optimization()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Structure S
        public _a As Integer
        public _b As System.Guid

        Public Sub New(a as Integer, b As System.Guid)
            _a = a
            _b = b
        End Sub

        Public Overrides Function ToString() As String
            Return (_a, _b).ToString()
        End Function
    End Structure

    Public Function CoalesceInt32(x As Integer?) As Integer
        Return If(x, 0)
    End Function

    Public Function CoalesceGeneric(Of T As Structure)(x As T?) As T
        Return If(x, CType(Nothing, T))
    End Function

    public Function CoalesceTuple(x As (a As Boolean, b As System.Guid)?) As (a As Boolean, b As System.Guid)
        Return If(x, CType(Nothing, (a As Boolean, b As System.Guid)))
    End Function

    public Function CoalesceUserStruct(x As S?) As S
        Return If(x, CType(Nothing, S))
    End Function

    public Function CoalesceStructWithImplicitConstructor(x As S?) As S
        Return If(x, New S())
    End Function

    public Sub Main()
        System.Console.WriteLine(CoalesceInt32(42))
        System.Console.WriteLine(CoalesceInt32(Nothing))
        System.Console.WriteLine(CoalesceGeneric(Of System.Guid)(new System.Guid("44ed2f0b-c2fa-4791-81f6-97222fffa466")))
        System.Console.WriteLine(CoalesceGeneric(Of System.Guid)(Nothing))
        System.Console.WriteLine(CoalesceTuple((true, new System.Guid("1c95cef0-1aae-4adb-a43c-54b2e7c083a0"))))
        System.Console.WriteLine(CoalesceTuple(Nothing))
        System.Console.WriteLine(CoalesceUserStruct(new S(42, new System.Guid("8683f371-81b4-45f6-aaed-1c665b371594"))))
        System.Console.WriteLine(CoalesceUserStruct(Nothing))
        System.Console.WriteLine(CoalesceStructWithImplicitConstructor(new S()))
        System.Console.WriteLine(CoalesceStructWithImplicitConstructor(Nothing))
    End Sub
End Module</file>
</compilation>,
            references:={ValueTupleRef, SystemRuntimeFacadeRef},
            expectedOutput:=<![CDATA[
42
0
44ed2f0b-c2fa-4791-81f6-97222fffa466
00000000-0000-0000-0000-000000000000
(True, 1c95cef0-1aae-4adb-a43c-54b2e7c083a0)
(False, 00000000-0000-0000-0000-000000000000)
(42, 8683f371-81b4-45f6-aaed-1c665b371594)
(0, 00000000-0000-0000-0000-000000000000)
(0, 00000000-0000-0000-0000-000000000000)
(0, 00000000-0000-0000-0000-000000000000)
]]>).
            VerifyIL("Program.CoalesceInt32",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0007:  ret
}]]>).
            VerifyIL("Program.CoalesceGeneric(Of T)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function T?.GetValueOrDefault() As T"
  IL_0007:  ret
}]]>).
            VerifyIL("Program.CoalesceTuple",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function System.ValueTuple(Of Boolean, System.Guid)?.GetValueOrDefault() As System.ValueTuple(Of Boolean, System.Guid)"
  IL_0007:  ret
}]]>).
            VerifyIL("Program.CoalesceUserStruct",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Program.S?.GetValueOrDefault() As Program.S"
  IL_0007:  ret
}]]>).
            VerifyIL("Program.CoalesceStructWithImplicitConstructor",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Program.S?.GetValueOrDefault() As Program.S"
  IL_0007:  ret
}]]>)
        End Sub

        <Fact()>
        Public Sub TestNullCoalesce_NullableWithConvertedDefault_Optimization()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function CoalesceDifferentTupleNames(x As (a As Boolean, b As System.Guid, c As String)?) As (a As Boolean, b As System.Guid, c As String)
        Return If(x, CType(Nothing, (c As Boolean, d As System.Guid, e As String)))
    End Function

    Public Sub Main()
        System.Console.WriteLine(CoalesceDifferentTupleNames((true, new System.Guid("533d4d3b-5013-461e-ae9e-b98eb593d761"), "value")))
        System.Console.WriteLine(CoalesceDifferentTupleNames(Nothing))
    End Sub
End Module</file>
</compilation>,
            references:={ValueTupleRef, SystemRuntimeFacadeRef},
            expectedOutput:=<![CDATA[
(True, 533d4d3b-5013-461e-ae9e-b98eb593d761, value)
(False, 00000000-0000-0000-0000-000000000000, )
]]>).
            VerifyIL("Program.CoalesceDifferentTupleNames",
            <![CDATA[
 {
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function System.ValueTuple(Of Boolean, System.Guid, String)?.GetValueOrDefault() As System.ValueTuple(Of Boolean, System.Guid, String)"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableWithNonDefault()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function CoalesceWithNonDefault1(x As Integer?) As Integer
        Return If(x, 2)
    End Function

    Public Function CoalesceWithNonDefault2(x As Integer?, y As Integer) As Integer
        Return If(x, y)
    End Function

    Public Function CoalesceWithNonDefault3(x As Integer?, y As Integer?) As Integer?
        Return If(x, y)
    End Function

    Public Function CoalesceWithNonDefault4(x As Integer?) As Integer?
        Return If(x, Nothing)
    End Function

    Public Sub WriteLine(value As Object)
        System.Console.WriteLine(If(value?.ToString, "*Nothing*"))
    End Sub

    Public Sub Main
        WriteLine(CoalesceWithNonDefault1(42))
        WriteLine(CoalesceWithNonDefault1(Nothing))
        WriteLine(CoalesceWithNonDefault2(12, 34))
        WriteLine(CoalesceWithNonDefault2(Nothing, 34))
        WriteLine(CoalesceWithNonDefault3(123, 456))
        WriteLine(CoalesceWithNonDefault3(123, Nothing))
        WriteLine(CoalesceWithNonDefault3(Nothing, 456))
        WriteLine(CoalesceWithNonDefault3(Nothing, Nothing))
        WriteLine(CoalesceWithNonDefault4(42))
        WriteLine(CoalesceWithNonDefault4(Nothing))
    End Sub
End Module</file>
</compilation>,
            expectedOutput:=<![CDATA[
42
2
12
34
123
123
456
*Nothing*
42
*Nothing*
]]>).
            VerifyIL("Program.CoalesceWithNonDefault1",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.2
  IL_0003:  call       "Function Integer?.GetValueOrDefault(Integer) As Integer"
  IL_0008:  ret
}
]]>).
            VerifyIL("Program.CoalesceWithNonDefault2",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  call       "Function Integer?.GetValueOrDefault(Integer) As Integer"
  IL_0008:  ret
}
]]>).
            VerifyIL("Program.CoalesceWithNonDefault3",
            <![CDATA[
 {
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  ldarg.1
  IL_000a:  ret
  IL_000b:  ldarg.0
  IL_000c:  ret
}
]]>).
            VerifyIL("Program.CoalesceWithNonDefault4",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer? V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0007:  brtrue.s   IL_0013
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "Integer?"
  IL_0011:  ldloc.0
  IL_0012:  ret
  IL_0013:  ldarg.0
  IL_0014:  ret
}
            ]]>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableWithNonDefault_ByRefParameter()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function CoalesceWithNonDefault(x As Integer?, ByRef y As Integer) As Integer
        Return If(x, y)
    End Function
End Module</file>
</compilation>)

            ' Dereferencing might throw, so no `GetValueOrDefault(defaultValue)` optimization here
            verifier.VerifyIL("Program.CoalesceWithNonDefault",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  ldarg.1
  IL_000a:  ldind.i4
  IL_000b:  ret
  IL_000c:  ldarga.s   V_0
  IL_000e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0013:  ret
}
]]>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableWithNonDefault_Local()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function CoalesceWithNonDefault(x As Integer?) As Integer
        Dim y = 3
        Dim z = If(x, y)
        Return y + z
    End Function
End Module</file>
</compilation>).
            VerifyIL("Program.CoalesceWithNonDefault",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (Integer V_0, //y
                Integer V_1) //z
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldarga.s   V_0
  IL_0004:  ldloc.0
  IL_0005:  call       "Function Integer?.GetValueOrDefault(Integer) As Integer"
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  add.ovf
  IL_000e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestNullCoalesce_NonNullableWithDefault_NoOptimization()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function CoalesceNonNullableWithDefault(x As String)
        Return If(x, Nothing)
    End Function

    Public Sub WriteLine(value As Object)
        System.Console.WriteLine(If(value?.ToString, "*Nothing*"))
    End Sub

    Public Sub Main()
        WriteLine(CoalesceNonNullableWithDefault("value"))
        WriteLine(CoalesceNonNullableWithDefault(Nothing))
    End Sub
End Module</file>
</compilation>,
            expectedOutput:=<![CDATA[
value
*Nothing*
]]>).
            VerifyIL("Program.CoalesceNonNullableWithDefault",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0006
  IL_0004:  pop
  IL_0005:  ldnull
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableDefault_MissingGetValueOrDefault()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function Coalesce(x As Integer?)
        Return If(x, 0)
    End Function
End Module</file>
</compilation>)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault)
            compilation.AssertTheseEmitDiagnostics()

            Dim verifier = CompileAndVerify(compilation)

            ' We gracefully fallback to calling `GetValueOrDefault(defaultValue)` member
            verifier.VerifyIL("Program.Coalesce",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       "Function Integer?.GetValueOrDefault(Integer) As Integer"
  IL_0008:  box        "Integer"
  IL_000d:  ret
}
]]>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableDefault_MissingGetValueOrDefaultAndGetValueOrDefaultWithADefaultValueParameter()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function Coalesce(x As Integer?)
        Return If(x, 0)
    End Function
End Module</file>
</compilation>)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefaultDefaultValue)
            compilation.AssertTheseEmitDiagnostics(
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Nullable`1.GetValueOrDefault' is not defined.
        Return If(x, 0)
                  ~
</errors>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableWiNonDefault_MissingGetValueOrDefaultWithADefaultValueParameter()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function Coalesce(x As Integer?)
        Return If(x, 2)
    End Function
End Module</file>
</compilation>)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefaultDefaultValue)
            compilation.AssertTheseEmitDiagnostics()

            Dim verifier = CompileAndVerify(compilation)

            ' We gracefully fallback to less efficient implementation with branching
            verifier.VerifyIL("Program.Coalesce",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  ldc.i4.2
  IL_000a:  br.s       IL_0013
  IL_000c:  ldarga.s   V_0
  IL_000e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0013:  box        "Integer"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56007")>
        Public Sub TestNullCoalesce_NullableWiNonDefault_MissingGetValueOrDefaultAndGetValueOrDefaultWithADefaultValueParameter()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb">
Public Module Program
    Public Function Coalesce(x As Integer?)
        Return If(x, 2)
    End Function
End Module</file>
</compilation>)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefaultDefaultValue)
            compilation.AssertTheseEmitDiagnostics(
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Nullable`1.GetValueOrDefault' is not defined.
        Return If(x, 2)
                  ~
</errors>)
        End Sub

        <Fact()>
        Public Sub TestIfExpressionWithNullable()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class EmitTest

    Public Shared Sub Main()
        WriteResult("Test01", Test01(123))
    End Sub

    Public Shared Sub WriteResult(name As String, result As Object)
        System.Console.WriteLine("{0}:  {1}", name.PadLeft(10),
                                 If(result Is Nothing, "&lt;Nothing&gt;", String.Format("{0}||{1}", result, result.GetType.FullName)))
    End Sub

    Public Shared Function Test01(a As Integer?) As Object
        Return If(Nothing, a)
    End Function

End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
Test01:  123||System.Int32
]]>)
        End Sub

        <Fact()>
        Public Sub IfStatement1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        Dim cond As Boolean
        Dim cond2 As Boolean
        Dim cond3 As Boolean

        cond = True
        cond2 = True
        cond3 = True

        If cond Then
            Console.WriteLine("ThenPart")
        Else If cond2
            Console.WriteLine("ElseIf1Part")
        Else If cond3
            Console.WriteLine("ElseIf2Part")
        Else
            Console.WriteLine("ElsePart")
        End If

        Console.WriteLine("After")
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="ThenPart" & Environment.NewLine & "After" & Environment.NewLine).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (Boolean V_0, //cond2
  Boolean V_1) //cond3
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  stloc.1
  IL_0005:  brfalse.s  IL_0013
  IL_0007:  ldstr      "ThenPart"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  br.s       IL_003b
  IL_0013:  ldloc.0
  IL_0014:  brfalse.s  IL_0022
  IL_0016:  ldstr      "ElseIf1Part"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  br.s       IL_003b
  IL_0022:  ldloc.1
  IL_0023:  brfalse.s  IL_0031
  IL_0025:  ldstr      "ElseIf2Part"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  br.s       IL_003b
  IL_0031:  ldstr      "ElsePart"
  IL_0036:  call       "Sub System.Console.WriteLine(String)"
  IL_003b:  ldstr      "After"
  IL_0040:  call       "Sub System.Console.WriteLine(String)"
  IL_0045:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestVarianceConversionsDDS()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Imports System.Linq.Expressions
Imports System.Linq
Imports System.Collections
Imports System.Collections.Generic
Imports System.Security

<Assembly: SecurityTransparent()>

Namespace TernaryAndVarianceConversion
    Delegate Sub CovariantDelegateWithVoidReturn(Of Out T)()
    Delegate Function CovariantDelegateWithValidReturn(Of Out T)() As T

    Delegate Sub ContravariantDelegateVoidReturn(Of In T)()
    Delegate Sub ContravariantDelegateWithValidInParm(Of In T)(inVal As T)

    Interface ICovariantInterface(Of Out T)
        Sub CovariantInterfaceMethodWithVoidReturn()
        Function CovariantInterfaceMethodWithValidReturn() As T
        ReadOnly Property CovariantInterfacePropertyWithValidGetter As T
        Sub Test()
    End Interface

    Interface IContravariantInterface(Of In T)
        Sub ContravariantInterfaceMethodWithVoidReturn()
        Sub ContravariantInterfaceMethodWithValidInParm(inVal As T)
        WriteOnly Property ContravariantInterfacePropertyWithValidSetter As T
        Sub Test()
    End Interface

    Class CovariantInterfaceImpl(Of T) : Implements ICovariantInterface(Of T)
        Public Sub CovariantInterfaceMethodWithVoidReturn() Implements ICovariantInterface(Of T).CovariantInterfaceMethodWithVoidReturn

        End Sub
        Public Function CovariantInterfaceMethodWithValidReturn() As T Implements ICovariantInterface(Of T).CovariantInterfaceMethodWithValidReturn
            Return Nothing
        End Function

        Public ReadOnly Property CovariantInterfacePropertyWithValidGetter As T Implements ICovariantInterface(Of T).CovariantInterfacePropertyWithValidGetter
            Get
                Return Nothing
            End Get
        End Property

        Public Sub Test() Implements ICovariantInterface(Of T).Test
            Console.WriteLine("{0}", GetType(T))
        End Sub
    End Class

    Class ContravariantInterfaceImpl(Of T) : Implements IContravariantInterface(Of T)
        Public Sub ContravariantInterfaceMethodWithVoidReturn() Implements IContravariantInterface(Of T).ContravariantInterfaceMethodWithVoidReturn

        End Sub

        Public Sub ContravariantInterfaceMethodWithValidInParm(inVal As T) Implements IContravariantInterface(Of T).ContravariantInterfaceMethodWithValidInParm

        End Sub

        Public WriteOnly Property ContravariantInterfacePropertyWithValidSetter As T Implements IContravariantInterface(Of T).ContravariantInterfacePropertyWithValidSetter
            Set(value As T)

            End Set
        End Property

        Public Sub Test() Implements IContravariantInterface(Of T).Test
            Console.WriteLine("{0}", GetType(T))
        End Sub
    End Class

    Class Animal

    End Class

    Class Mammal : Inherits Animal

    End Class

    Class Program
        Shared Sub Test(testFlag As Boolean)
            Console.WriteLine("Testing with ternary test flag == {0}", testFlag)

            ' Repro case for bug 7196
            Dim EnumerableOfObject As IEnumerable(Of Object) =
                                    If(testFlag,
                                     Enumerable.Repeat(Of String)("string", 1),
                                     Enumerable.Empty(Of Object)())

            Console.WriteLine("{0}", EnumerableOfObject.Count())


            ' Covariant implicit conversion for delegates
            Dim covariantDelegateWithVoidReturnOfAnimal As CovariantDelegateWithVoidReturn(Of Animal) = Sub() Console.WriteLine("{0}", GetType(Animal))
            Dim covariantDelegateWithVoidReturnOfMammal As CovariantDelegateWithVoidReturn(Of Mammal) = Sub() Console.WriteLine("{0}", GetType(Mammal))
            Dim covariantDelegateWithVoidReturnOfAnimalTest As CovariantDelegateWithVoidReturn(Of Animal)
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfMammal, covariantDelegateWithVoidReturnOfAnimal)
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfAnimal, covariantDelegateWithVoidReturnOfMammal)
            covariantDelegateWithVoidReturnOfAnimalTest()

            Dim covariantDelegateWithValidReturnOfAnimal As CovariantDelegateWithValidReturn(Of Animal) = Function()
                                                                                                              Console.WriteLine("{0}", GetType(Animal))
                                                                                                              Return Nothing
                                                                                                          End Function

            Dim covariantDelegateWithValidReturnOfMammal As CovariantDelegateWithValidReturn(Of Mammal) = Function()
                                                                                                              Console.WriteLine("{0}", GetType(Mammal))
                                                                                                              Return Nothing
                                                                                                          End Function

            Dim covariantDelegateWithValidReturnOfAnimalTest As CovariantDelegateWithValidReturn(Of Animal)
            covariantDelegateWithValidReturnOfAnimalTest = If(testFlag, covariantDelegateWithValidReturnOfMammal, covariantDelegateWithValidReturnOfAnimal)
            covariantDelegateWithValidReturnOfAnimalTest()
            covariantDelegateWithValidReturnOfAnimalTest = If(testFlag, covariantDelegateWithValidReturnOfAnimal, covariantDelegateWithValidReturnOfMammal)
            covariantDelegateWithValidReturnOfAnimalTest()

            ' Contravariant implicit conversion for delegates
            Dim contravariantDelegateVoidReturnOfAnimal As ContravariantDelegateVoidReturn(Of Animal) = Sub() Console.WriteLine("{0}", GetType(Animal))
            Dim contravariantDelegateVoidReturnOfMammal As ContravariantDelegateVoidReturn(Of Mammal) = Sub() Console.WriteLine("{0}", GetType(Mammal))
            Dim contravariantDelegateVoidReturnOfMammalTest As ContravariantDelegateVoidReturn(Of Mammal)
            contravariantDelegateVoidReturnOfMammalTest = If(testFlag, contravariantDelegateVoidReturnOfMammal, contravariantDelegateVoidReturnOfAnimal)
            contravariantDelegateVoidReturnOfMammalTest()
            contravariantDelegateVoidReturnOfMammalTest = If(testFlag, contravariantDelegateVoidReturnOfAnimal, contravariantDelegateVoidReturnOfMammal)
            contravariantDelegateVoidReturnOfMammalTest()

            Dim contravariantDelegateWithValidInParmOfAnimal As ContravariantDelegateWithValidInParm(Of Animal) = Sub(t As Animal)
                                                                                                                      Console.WriteLine("{0}", GetType(Animal))
                                                                                                                  End Sub

            Dim contravariantDelegateWithValidInParmOfMammal As ContravariantDelegateWithValidInParm(Of Mammal) = Sub(t As Mammal)
                                                                                                                      Console.WriteLine("{0}", GetType(Mammal))
                                                                                                                  End Sub
            Dim contravariantDelegateWithValidInParmOfMammalTest As ContravariantDelegateWithValidInParm(Of Mammal)
            contravariantDelegateWithValidInParmOfMammalTest = If(testFlag, contravariantDelegateWithValidInParmOfMammal, contravariantDelegateWithValidInParmOfAnimal)
            contravariantDelegateWithValidInParmOfMammalTest(Nothing)
            contravariantDelegateWithValidInParmOfMammalTest = If(testFlag, contravariantDelegateWithValidInParmOfAnimal, contravariantDelegateWithValidInParmOfMammal)
            contravariantDelegateWithValidInParmOfMammalTest(Nothing)

            ' Covariant implicit conversion for interfaces
            Dim covariantInterfaceOfAnimal As ICovariantInterface(Of Animal) = New CovariantInterfaceImpl(Of Animal)
            Dim covariantInterfaceOfMammal As ICovariantInterface(Of Mammal) = New CovariantInterfaceImpl(Of Mammal)()
            Dim covariantInterfaceOfAnimalTest As ICovariantInterface(Of Animal)
            covariantInterfaceOfAnimalTest = If(testFlag, covariantInterfaceOfMammal, covariantInterfaceOfAnimal)
            covariantInterfaceOfAnimalTest.Test()
            covariantInterfaceOfAnimalTest = If(testFlag, covariantInterfaceOfAnimal, covariantInterfaceOfMammal)
            covariantInterfaceOfAnimalTest.Test()

            ' Contravariant implicit conversion for interfaces
            Dim contravariantInterfaceOfAnimal As IContravariantInterface(Of Animal) = New ContravariantInterfaceImpl(Of Animal)()
            Dim contravariantInterfaceOfMammal As IContravariantInterface(Of Mammal) = New ContravariantInterfaceImpl(Of Mammal)
            Dim contravariantInterfaceOfMammalTest As IContravariantInterface(Of Mammal)
            contravariantInterfaceOfMammalTest = If(testFlag, contravariantInterfaceOfMammal, contravariantInterfaceOfAnimal)
            contravariantInterfaceOfMammalTest.Test()
            contravariantInterfaceOfMammalTest = If(testFlag, contravariantInterfaceOfAnimal, contravariantInterfaceOfMammal)
            contravariantInterfaceOfMammalTest.Test()

            ' With explicit casting
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, DirectCast(DirectCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal)), CovariantDelegateWithVoidReturn(Of Animal)), covariantDelegateWithVoidReturnOfAnimal)
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfAnimal, DirectCast(DirectCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal)), CovariantDelegateWithVoidReturn(Of Animal)))
            covariantDelegateWithVoidReturnOfAnimalTest()

            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, TryCast(TryCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal)), CovariantDelegateWithVoidReturn(Of Animal)), covariantDelegateWithVoidReturnOfAnimal)
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfAnimal, TryCast(TryCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal)), CovariantDelegateWithVoidReturn(Of Animal)))
            covariantDelegateWithVoidReturnOfAnimalTest()

            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, CType(TryCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal)), CovariantDelegateWithVoidReturn(Of Animal)), covariantDelegateWithVoidReturnOfAnimal)
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfAnimal, DirectCast(CType(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal)), CovariantDelegateWithVoidReturn(Of Animal)))
            covariantDelegateWithVoidReturnOfAnimalTest()


            ' With parens
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, (covariantDelegateWithVoidReturnOfMammal), covariantDelegateWithVoidReturnOfAnimal)
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfAnimal, (covariantDelegateWithVoidReturnOfMammal))
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, (DirectCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal))), covariantDelegateWithVoidReturnOfAnimal)
            covariantDelegateWithVoidReturnOfAnimalTest()
            covariantDelegateWithVoidReturnOfAnimalTest = If(testFlag, covariantDelegateWithVoidReturnOfAnimal, (DirectCast(covariantDelegateWithVoidReturnOfMammal, CovariantDelegateWithVoidReturn(Of Animal))))
            covariantDelegateWithVoidReturnOfAnimalTest()

            ' Bug 291602
            Dim intarr = {1, 2, 3}
            Dim intlist As IList(Of Integer) = New List(Of Integer)(intarr)
            Dim intternary As IList(Of Integer) = If(testFlag, intarr, intlist)
            Console.WriteLine(intternary)

        End Sub

        Public Shared Sub Main()
            Test(True)
            Test(False)
        End Sub
    End Class
End Namespace


        ]]>
    </file>
</compilation>,
expectedOutput:=<![CDATA[Testing with ternary test flag == True
1
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
System.Int32[]
Testing with ternary test flag == False
0
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
System.Collections.Generic.List`1[System.Int32]
]]>)
        End Sub

        <Fact()>
        Public Sub ShiftMask()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        Dim s As Integer = 2
        Dim v As Integer = 123
        v = v >> s
        Console.Write(v)

        Dim v1 As Long = 123
        v1 = v1 &lt;&lt; s
            Console.Write(v1)
        End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="30492").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (Integer V_0) //s
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.s   123
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.s   31
  IL_0007:  and
  IL_0008:  shr
  IL_0009:  call       "Sub System.Console.Write(Integer)"
  IL_000e:  ldc.i4.s   123
  IL_0010:  conv.i8
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.s   63
  IL_0014:  and
  IL_0015:  shl
  IL_0016:  call       "Sub System.Console.Write(Long)"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub DoLoop1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        dim breakLoop as Boolean
        breakLoop = true
        Do While breakLoop
            Console.WriteLine("Iterate")
            breakLoop = false
        Loop
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="Iterate").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Boolean V_0) //breakLoop
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0010
  IL_0004:  ldstr      "Iterate"
  IL_0009:  call       "Sub System.Console.WriteLine(String)"
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  brtrue.s   IL_0004
  IL_0013:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub DoLoop2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        dim breakLoop as Boolean
        breakLoop = true
        Do Until breakLoop
            Console.WriteLine("Iterate")
            breakLoop = false
        Loop
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Boolean V_0) //breakLoop
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0010
  IL_0004:  ldstr      "Iterate"
  IL_0009:  call       "Sub System.Console.WriteLine(String)"
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  brfalse.s  IL_0004
  IL_0013:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub DoLoop3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        dim breakLoop as Boolean
        breakLoop = true
        Do
            Console.WriteLine("Iterate")
            breakLoop = false
        Loop While breakLoop
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
Iterate
]]>).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Boolean V_0) //breakLoop
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldstr      "Iterate"
  IL_0007:  call       "Sub System.Console.WriteLine(String)"
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  brtrue.s   IL_0002
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub DoLoop4()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub M()
        dim breakLoop as Boolean
        breakLoop = true
        Do
            Console.WriteLine("Iterate")
            breakLoop = false
        Loop Until breakLoop
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Boolean V_0) //breakLoop
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldstr      "Iterate"
  IL_0007:  call       "Sub System.Console.WriteLine(String)"
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  brfalse.s  IL_0002
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub DoLoop5()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()
        Do
            Console.WriteLine("Iterate")
        Loop
    End Sub
End Class
    </file>
</compilation>).
            VerifyIL("C.M",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      "Iterate"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  br.s       IL_0000
}
]]>)
        End Sub

        <Fact()>
        Public Sub ExitContinueDoLoop1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        dim breakLoop as Boolean
        dim continueLoop as Boolean
        breakLoop = True: continueLoop = true
        Do While breakLoop
            Console.WriteLine("Stmt1")
            If continueLoop Then
                Console.WriteLine("Continuing")
                continueLoop = false
                Continue Do
            End If
            Console.WriteLine("Exiting")
            Exit Do
            Console.WriteLine("Stmt2")
        Loop
        Console.WriteLine("After Loop")
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
Stmt1
Continuing
Stmt1
Exiting
After Loop
]]>).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (Boolean V_0, //breakLoop
           Boolean V_1) //continueLoop
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_002d
  IL_0006:  ldstr      "Stmt1"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ldloc.1
  IL_0011:  brfalse.s  IL_0021
  IL_0013:  ldstr      "Continuing"
  IL_0018:  call       "Sub System.Console.WriteLine(String)"
  IL_001d:  ldc.i4.0
  IL_001e:  stloc.1
  IL_001f:  br.s       IL_002d
  IL_0021:  ldstr      "Exiting"
  IL_0026:  call       "Sub System.Console.WriteLine(String)"
  IL_002b:  br.s       IL_0030
  IL_002d:  ldloc.0
  IL_002e:  brtrue.s   IL_0006
  IL_0030:  ldstr      "After Loop"
  IL_0035:  call       "Sub System.Console.WriteLine(String)"
  IL_003a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub PartialMethod1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class CLS
    Partial Private Shared Sub PS()
    End Sub

    Shared Sub TST()
        PS()
    End Sub

    Shared Sub Main()
        Dim t = (New CLS()).GetType()
        Dim m = t.GetMethod("PS", Reflection.BindingFlags.Static Or Reflection.BindingFlags.NonPublic)
        Console.WriteLine(If(m Is Nothing, "Nothing", m.ToString()))
    End Sub
End Class

    </file>
</compilation>, expectedOutput:="Nothing").
            VerifyIL("CLS.TST",
            <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub PartialMethod2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Interface I
    Sub S()
End Interface

Class C
    Implements I

    Partial Private Sub S()
    End Sub

    Private Sub S() Implements I.S
        Console.WriteLine("Private Sub S() Implements I.S")
    End Sub

    Shared Sub Main(args As String())
        Dim i As I = New C
        i.S()
    End Sub
End Class

    </file>
</compilation>, expectedOutput:="Private Sub S() Implements I.S")
        End Sub

        <Fact()>
        Public Sub PartialMethod_NonGeneric_Generic()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C(Of V)

    Partial Private Sub S(s As Integer)
    End Sub

    Private Sub S(s As V)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(ByVal z As V)
        Me.S(z)
    End Sub

End Class

Module M222
    Sub Main(args As String())
        Dim c As New C(Of Integer)
        c.Goo(1)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="Success")
        End Sub

        <Fact()>
        Public Sub PartialMethod_Generic_Generic()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C(Of V)

    Partial Private Sub S(s As V)
    End Sub

    Private Sub S(s As V)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(ByVal z As V)
        Me.S(z)
    End Sub

End Class

Module M222
    Sub Main(args As String())
        Dim c As New C(Of Integer)
        c.Goo(1)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="Success")
        End Sub

        <Fact()>
        Public Sub PartialMethod_Generic_NonGeneric()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C(Of V)

    Partial Private Sub S(s As V)
    End Sub

    Private Sub S(s As Integer)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(ByVal z As V)
        Me.S(z)
    End Sub

End Class

Module M222
    Sub Main(args As String())
        Dim c As New C(Of Integer)
        c.Goo(1)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub PartialMethod_Generic_NonGeneric2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure C(Of V)

    Partial Private Sub S(s As V)
    End Sub

    Private Sub S(s As Integer)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(ByVal z As Integer)
        Me.S(z)
    End Sub

End Structure

Module M222
    Sub Main(args As String())
        Dim c As New C(Of Integer)
        c.Goo(1)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="Success")
        End Sub

        <Fact()>
        Public Sub PartialMethod_NonGeneric_Generic_Method()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Partial Private Sub S(s As Integer)
    End Sub

    Private Sub S(Of V)(s As V)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(Of V)(ByVal z As V)
        Me.S(z)
    End Sub
End Class

Module MMM
    Sub Main(args As String())
        Dim c As New C
        c.Goo(1)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="Success")
        End Sub

        <Fact()>
        Public Sub PartialMethod_NonGeneric_Generic_Method2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure C
    Partial Private Sub S(s As Integer)
    End Sub

    Private Sub S(Of V)(s As V)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(ByVal z As Integer)
        Me.S(z)
    End Sub
End Structure

Module MMM
    Sub Main(args As String())
        Dim c As New C
        c.Goo(1)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub PartialMethod_Generic_NonGeneric_Method()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Partial Private Sub S(Of V)(s As V)
    End Sub

    Private Sub S(s As Integer)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(Of V)(ByVal z As V)
        Me.S(z)
    End Sub
End Class

Module MMM
    Sub Main(args As String())
        Dim c As New C
        c.Goo(1)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub PartialMethod_Generic_NonGeneric_Method2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure C
    Partial Private Sub S(Of V)(s As V)
    End Sub

    Private Sub S(s As Integer)
        Console.WriteLine("Success")
    End Sub

    Public Sub Goo(ByVal z As Integer)
        Me.S(z)
    End Sub
End Structure

Module MMM
    Sub Main(args As String())
        Dim c As New C
        c.Goo(1)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Success")
        End Sub

        <Fact()>
        Public Sub PartialMethod_ComplexGenerics()
            Dim vbCompilation = CreateVisualBasicCompilation("PartialMethod_ComplexGenerics",
            <![CDATA[Imports System
Imports System.Collections.Generic
Imports A = C1(Of Integer)

Module Program
    Sub Main(args As String())
        Dim x = New C1(Of A).C2(Of A)
        Dim a1 = New c2(Of Integer)
        Dim a2 = New c2(Of A)
        Dim a3 As C1(Of A).I(Of A) = Nothing
        Dim a4 = New Dictionary(Of C1(Of A), C1(Of A).I(Of A))()
        Dim a5 = New ArgumentException
        Dim a6 = New c2(Of ArgumentException)
        x.Bar(a1, a1, a2,
              a1, a3,
              a1, a1, a4,
              a5, a6)
    End Sub
End Module

Partial Class C1(Of T) : Implements C1(Of A).I(Of A)
    Interface I(Of J)
    End Interface
    Partial Class C2(Of K As T)
        Partial Private Sub Goo(Of U As {A, T, I(Of K)}, V As {U, A})(x As A, y As T, z As C1(Of T),
                                                                      aa As K, bb As I(Of K),
                                                                      cc As U, dd As V, ee As IDictionary(Of C1(Of U), I(Of V)),
                                                                      ff As Exception, yy As C1(Of ArgumentException))
        End Sub

        Sub Bar(Of U As {A, T, I(Of K)}, V As {U, A})(x As A, y As T, z As C1(Of T),
                                                                      aa As K, bb As I(Of K),
                                                                      cc As U, dd As V, ee As IDictionary(Of C1(Of U), I(Of V)),
                                                                      ff As Exception, yy As C1(Of ArgumentException))
            Goo(x, y, z, aa, bb, cc, dd, ee, ff, yy)
        End Sub
    End Class
End Class

Class c2(Of T) : Inherits C1(Of T)
End Class

Partial Class C1(Of T)
    Partial Class C2(Of K As T)
        Private Sub Goo(Of U As {T, I(Of K), C1(Of Integer)}, V As {C1(Of Integer), U})(x As C1(Of Integer), y As T, z As C1(Of T),
                                                                      aa As K, bb As I(Of K),
                                                                      cc As U, dd As V, ee As IDictionary(Of C1(Of U), I(Of V)),
                                                                      ff As Exception, yy As C1(Of ArgumentException))
            Console.WriteLine("Success")
        End Sub
    End Class
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[Success
]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <WorkItem(544432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544432")>
        <Fact()>
        Public Sub PartialMethod_InNestedStructsAndModules()
            Dim vbCompilation = CreateVisualBasicCompilation("PartialMethod_InNestedStructsAndModules",
            <![CDATA[Imports System

Public Module M
    Private Sub Goo(Of V)(ByRef x As V,
                          ParamArray y() As Integer)
        Console.WriteLine("M.Goo - Integer")
    End Sub
    Partial Private Sub Goo(Of V)(ByRef x As V,
                                  ParamArray y() As Long)
    End Sub

    Sub Main()
        'Structures
        I(Of Integer).S(Of Integer).Test(Of Integer)()

        'Modules
        Dim x = 1
        Dim y = 1L
        Goo(x, x, x, x)
        Goo(x, x, x, y)
    End Sub

    Partial Private Sub Goo(Of V)(ByRef x As V,
                                  ParamArray y() As Integer)
    End Sub
    Private Sub Goo(Of V)(ByRef x As V,
                          ParamArray y() As Long)
        Console.WriteLine("M.Goo - Long")
    End Sub
End Module

Interface I(Of T)
    Partial Structure S(Of U As T)
        Partial Private Sub Goo(Of V As {T, U})(ByRef x As C(Of S(Of V)),
                                                ParamArray y() As Integer)
        End Sub
        Private Sub Goo(Of V As {T, U})(ByRef x As C(Of S(Of V)),
                                        ParamArray y() As Long)
            Console.WriteLine("S.Goo - Long")
        End Sub
    End Structure
    Class C(Of W As Structure)
    End Class
    Partial Structure S(Of U As T)
        Private Sub Goo(Of V As {T, U})(ByRef x As C(Of S(Of V)),
                                        ParamArray y() As Integer)
            Console.WriteLine("S.Goo - Integer")
        End Sub
        Partial Private Sub Goo(Of V As {T, U})(ByRef x As C(Of S(Of V)),
                                                ParamArray y() As Long)
        End Sub
        Shared Sub Test(Of J As U)()
            Dim s = New I(Of T).S(Of U)
            Dim c = New I(Of T).C(Of S(Of J))
            Dim x = 1
            Dim y = 1L
            s.Goo(c, x, x, x)
            s.Goo(c, x, x, y)
        End Sub
    End Structure
End Interface]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[S.Goo - Integer
S.Goo - Long
M.Goo - Integer
M.Goo - Long
]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub EmitCallToConstructorWithParamArrayParameters()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class Base
        Sub New(ParamArray x() As Object)
            Console.WriteLine("Base.New(): " + x.Length.ToString())
        End Sub
    End Class
    Class Derived
        Inherits Base
    End Class
    Public Sub Main()
        Dim a As New Derived()
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Base.New(): 0]]>).
            VerifyIL("M1.Derived..ctor",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  newarr     "Object"
  IL_0007:  call       "Sub M1.Base..ctor(ParamArray Object())"
  IL_000c:  ret
}
]]>)
        End Sub

        ' Test access to a parameter (both simple and byref)
        <Fact()>
        Public Sub Parameter1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Sub Goo(xParam as Integer, ByRef yParam As Long)
        Console.WriteLine(xParam)
        Console.WriteLine(yParam)
        xParam = 17
        yParam = 189
    End Sub

    Sub Main()
        Dim x as Integer
        Dim y as Long
        x = 143
        y = 16442
        Console.WriteLine("x = {0}", x)
        Console.WriteLine("y = {0}", y)
        Goo(x,y)
        Console.WriteLine("x = {0}", x)
        Console.WriteLine("y = {0}", y)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
x = 143
y = 16442
143
16442
x = 143
y = 189
]]>).
            VerifyIL("M1.Goo",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0006:  ldarg.1
  IL_0007:  ldind.i8
  IL_0008:  call       "Sub System.Console.WriteLine(Long)"
  IL_000d:  ldc.i4.s   17
  IL_000f:  starg.s    V_0
  IL_0011:  ldarg.1
  IL_0012:  ldc.i4     0xbd
  IL_0017:  conv.i8
  IL_0018:  stind.i8
  IL_0019:  ret
}
]]>)
        End Sub

        ' Test that parameterless functions are invoked when in argument lists
        <Fact()>
        Public Sub ParameterlessFunctionCall()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Function SayHi as string
        return "hi"
    End Function

    Sub Main()
        Console.WriteLine(SayHi)
        Console.WriteLine(SayHi())
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
hi
hi
]]>).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  call       "Function M1.SayHi() As String"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  call       "Function M1.SayHi() As String"
  IL_000f:  call       "Sub System.Console.WriteLine(String)"
  IL_0014:  ret
}
]]>)
        End Sub

        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub ParameterByRefVal()
            CompileAndVerify(
           <compilation>
               <file name="First.1a.vb"><![CDATA[
Imports System

Namespace VBN
    Friend Structure SBar
        Public Field1 As Decimal
        Public Field2 As SGoo
    End Structure
End Namespace
]]></file>
               <file name="Second.2b.vb"><![CDATA[
Option Strict Off

Imports System
Imports VBN

Module Program

            Sub Main(args As String())
                Dim sss As VBN.SGoo, ccc As SBar = New SBar()
                sss = New SGoo()
                Dim val As Short = 9
                sss.M1(val)
                ' () - ByRef
                sss.M1((val))
                sss.M1(((val)))
                Dim ary1() As Short
                ary1 = New Short(0) {6}
                Dim ary2 As UShort() = New UShort(3) {0, 8, 2, 3}
                sss.M1(ary1, ary2, ary1(0), ary2(2))
                console.write("{0},{1},{2},{3}|", ary1(2), ary2(1), ary1(0), ary2(2))

                Dim v2 As UInteger = 1221
                ' call UShort
                sss.M2(v2)
                console.write("{0}|", v2)
                sss.M2("121")

                ccc.Field2 = sss
                ccc.Field1 = 123.456D
                Dim v3 As Short() = New Short() {0, 1, -1, -127}
                ccc.Field2.M2(ccc, v3(3))
                console.write("{0},{1} |", ccc.Field1.ToString(System.Globalization.CultureInfo.InvariantCulture), v3(3))
                ccc.Field2.M2(ccc)
                console.write("D:{0}|", ccc.Field1.ToString(System.Globalization.CultureInfo.InvariantCulture))

                'Dim jag(,)() As SByte = New SByte(1, 1)() {{New SByte() {1}, New SByte() {2}}, {New SByte() {3}, New SByte() {4}}}
                Dim jag()() As SByte = New SByte(2)() {New SByte() {-1, 1}, New SByte() {2}, New SByte() {3, -3, -0}}
                ccc.Field2.M1(jag(1)(0), jag(2)(2))
                console.write("J:{0},{1}", jag(1)(0), jag(2)(2))
            End Sub
        End Module
]]></file>
               <file name="Third.3c.vb"><![CDATA[
Imports System

Namespace VBN

            Structure SGoo
                Public Sub M1(ByVal x As Object)
                    console.write("O:{0}|", x)
                End Sub
                Public Sub M1(ByRef x As Integer)
                    console.write("I:{0}|", x)
                End Sub
                Public Sub M1(ByRef x As Short(), ByVal y() As UShort, ByRef z As Short, ByRef p As UShort)
                    x = New Short() {-1, -2, -3}
                    y(1) = 1
                    z = 7
                    p = Nothing
                End Sub

                Public Sub M1(ByRef x As SByte, ByVal y As Long)
                    If (x <= SByte.MaxValue - 2 AndAlso y >= -1) Then
                        x = 1 + x + 1
                        y = y - 1 - 1
                    Else
                        x = System.Math.Abs(y) - 2 * x
                        y = x + x
                    End If
                End Sub
                Public Sub M1(ByRef x As Object, ByVal y As String)

                End Sub

                Public Sub M2(ByVal x As SBar, ByRef y As SByte)
                    x.Field1 = 9.876543
                    If (y >= SByte.MinValue + 1) Then
                        y = y - 1
                    End If
                End Sub

                Public Sub M2(ByRef x As SBar)
                    x.Field1 = 9.876543
                End Sub

                Public Sub M2(ByRef x As UShort)
                    x = X + 1
                    If (x <= 123) Then
                        console.write("{0}|", x)
                    End If
                End Sub
            End Structure
        End Namespace
]]></file>
           </compilation>,
            expectedOutput:="O:9|I:9|I:9|-3,1,-1,0|1222|122|123.456,-128 |D:9.876543|J:4,0")
        End Sub

        <Fact()>
        Public Sub ReturnStatementInSub1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub S1()
      dim f as boolean = true
      if f then
         console.WriteLine("true")
         return
      else
        console.WriteLine("false")
        return
      end if
      console.writeline("end")
    End Sub

End Module
    </file>
</compilation>).
            VerifyIL("M1.S1",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brfalse.s  IL_000e
  IL_0003:  ldstr      "true"
  IL_0008:  call       "Sub System.Console.WriteLine(String)"
  IL_000d:  ret
  IL_000e:  ldstr      "false"
  IL_0013:  call       "Sub System.Console.WriteLine(String)"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReturnStatementInFunction1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Function F1() as boolean
     dim f as boolean = true
      if f then
         console.WriteLine("true")
         return true
      else
        console.WriteLine("false")
        return false
      end if
      console.writeline("end")
      return false
    End Function

End Module
    </file>
</compilation>).
            VerifyIL("M1.F1",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (Boolean V_0) //F1
  IL_0000:  ldc.i4.1
  IL_0001:  brfalse.s  IL_0011
  IL_0003:  ldstr      "true"
  IL_0008:  call       "Sub System.Console.WriteLine(String)"
  IL_000d:  ldc.i4.1
  IL_000e:  stloc.0
  IL_000f:  br.s       IL_001d
  IL_0011:  ldstr      "false"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ldc.i4.0
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReturnStatementInDo()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Function F1(x as integer) as integer
      do while true
        return x
      loop
    End Function
End Module
    </file>
</compilation>).
            VerifyIL("M1.F1",
            <![CDATA[
{
// Code size        4 (0x4)
.maxstack  1
.locals init (Integer V_0) //F1
IL_0000:  ldarg.0
IL_0001:  stloc.0
IL_0002:  ldloc.0
IL_0003:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultipleLocals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()

        dim a1 as integer = 100, b1, b2, b3 as string, c1 as boolean = false
        Console.Write(a1)
        Console.Write(b1 &amp; b2 &amp; b3)
        Console.Write(c1)

    End Sub
End Class
    </file>
</compilation>).
            VerifyIL("C.M",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (Integer V_0, //a1
  String V_1, //b1
  String V_2, //b2
  String V_3) //b3
  IL_0000:  ldc.i4.s   100
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.Write(Integer)"
  IL_000a:  ldloc.1
  IL_000b:  ldloc.2
  IL_000c:  ldloc.3
  IL_000d:  call       "Function String.Concat(String, String, String) As String"
  IL_0012:  call       "Sub System.Console.Write(String)"
  IL_0017:  call       "Sub System.Console.Write(Boolean)"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LocalArray1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub M()
        dim a?()(,) as integer

        dim b(2) as integer
        ' should be error BC31087
        dim c as integer(,)
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Integer"
  IL_0006:  pop
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LocalArray2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()
        Dim z As Integer()(,) = New Integer(3)(,) {}
    End Sub
End Class
    </file>
</compilation>).
            VerifyIL("C.M",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     "Integer(,)"
  IL_0006:  pop
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(538660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538660")>
        <Fact()>
        Public Sub LocalArray3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
    Dim z1 As Integer() = New Integer(2) {1,2,3}
    Dim Z2 as string() = new string() {"a", "b"}
    Console.Write(z1(2))
    Console.Write(z2(1))
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"),
expectedOutput:="3b").VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (Integer() V_0) //z1
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  stloc.0
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     "String"
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  ldstr      "a"
  IL_001f:  stelem.ref
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldstr      "b"
  IL_0027:  stelem.ref
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.2
  IL_002a:  ldelem.i4
  IL_002b:  call       "Sub System.Console.Write(Integer)"
  IL_0030:  ldc.i4.1
  IL_0031:  ldelem.ref
  IL_0032:  call       "Sub System.Console.Write(String)"
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LocalArrayAssign1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub M()
        Dim z1(1) As string
        z1(0) = "hello"
        z1(1) = "world"
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  4
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     "String"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      "hello"
  IL_000d:  stelem.ref
  IL_000e:  ldc.i4.1
  IL_000f:  ldstr      "world"
  IL_0014:  stelem.ref
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ArrayWithTypeChars()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class MyArray
    Shared fidx As UInteger

    Shared Sub Main()
        fidx = 2
        Const idx As Long = 9
        Dim local As ULong = 5
        Dim B04@(local), B05%(5 - 2), B06&amp;(3 - 1)
        B04(0) = 1.234D
        Console.WriteLine(B04(0).ToString(System.Globalization.CultureInfo.InvariantCulture))

        B05%(1) = -12345
        Console.WriteLine(B05%(1))

        B06(0) = -1%
        Console.WriteLine(B06&amp;(0))
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
1.234
-12345
-1
]]>)
        End Sub

        <Fact>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        <WorkItem(529849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529849")>
        Public Sub ArrayWithTypeCharsWithStaticLocals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class MyArray
    Shared fidx As UInteger
    Shared cul As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
    Shared Sub Main()
        fidx = 2
        Const idx As Long = 9
        Dim local As ULong = 5
        Static B01#(3), B02!(idx), B03$(fidx)
        B01(0) = 1.1#
        B01#(2) = 2.2!
        Console.WriteLine(B01(0).ToString("G15", cul))
        Console.WriteLine(B01(2).ToString("G15", cul))

        B02!(idx - 1) = 0.123
        Console.WriteLine(B02(idx - 1).ToString("G6", cul))

        B03$(fidx - 1) = "c c"
        Console.WriteLine(B03(1))
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
1.1
2.20000004768372
0.123
c c
]]>)
        End Sub

        <WorkItem(538660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538660")>
        <Fact()>
        Public Sub ArrayOneDimension()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Namespace N

    Public Class MC
        Public CF As String
    End Class
    Friend Structure MS
        Public SF As Mc
    End Structure

    Class MyArray
        Shared fidx As ULong

        Shared Sub Main()
            fidx = 2
            Const idx As Long = 9
            Dim local As UInteger = 5
            ' 12
            Dim a1 As SByte() = New SByte(local) {}
            a1(0) = 123
            a1(fidx + 1) = a1(0)
            Console.WriteLine(a1(3))
            ' 124
            Dim lmc As MC = New MC()
            lmc.CF = "XXX"
            Dim a2 As MC() = New MC(1 + 1) {lmc, New MC(), Nothing}
            Console.WriteLine(a2(0).CF)

            Dim a4(0) As Date
            a4(0) = #12:24:35 PM#
            Console.WriteLine(a4(0).ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        End Sub
    End Class
End Namespace
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
123
XXX
1/1/0001 12:24:35 PM
]]>)
        End Sub

        <WorkItem(538660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538660")>
        <WorkItem(529849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529849")>
        <Fact>
        Public Sub ArrayOneDimensionWithStaticLocals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Namespace N

    Public Class MC
        Public CF As String
    End Class
    Friend Structure MS
        Public SF As Mc
    End Structure

    Class MyArray
        Shared fidx As ULong

        Shared Sub Main()
            fidx = 2
            Const idx As Long = 9

            Static lms As MS = New MS()
            lms.SF = New MC()
            lms.SF.CF = "12345"
            Static a3 As MS()
            a3 = New MS(idx - 7) {New MS(), lms, lms}
            Console.WriteLine(a3(1).SF.CF)
        End Sub
    End Class
End Namespace
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
12345
]]>)
        End Sub

        <Fact()>
        Public Sub ConstantExpressions()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System.Globalization

Module Module1


    Sub Main()
        PrintResultBo(False)
        PrintResultBo(True)
        PrintResultSB(1)
        PrintResultBy(2)
        PrintResultSh(3)
        PrintResultUs(4)
        PrintResultIn(5)
        PrintResultUI(6)
        PrintResultLo(7)
        PrintResultUL(8)
        'PrintResultDe(-9D)
        PrintResultDe(0D)
        'PrintResultDe(-1D)
        PrintResultDe(1D)
        PrintResultDe(14D)
        PrintResultDe(79228162514264337593543950335D)
        'PrintResultDe(-79228162514264337593543950335D)
        'PrintResultDe(-9D)
        PrintResultDe(0D)
        'PrintResultDe(-1D)
        PrintResultDe(1D)
        PrintResultDe(14D)
        PrintResultDe(79228162514264337593543950335D)
        'PrintResultDe(-79228162514264337593543950335D)
        PrintResultSi(10)
        PrintResultDo(11)
        PrintResultSt("12")
        PrintResultOb(13)
        PrintResultDa(#8/23/1970 3:45:39 AM#)
        PrintResultDa(#12:00:00 AM#)
        PrintResultDa(#8/23/1970 3:45:39 AM#)
        PrintResultDa(#12:00:00 AM#)
        PrintResultCh("v"c)
    End Sub

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
        System.Console.WriteLine("Char: {0}", val)
    End Sub
    Sub PrintResultSt(val As String)
        System.Console.WriteLine("String: {0}", val)
    End Sub
    Sub PrintResultOb(val As Object)
        System.Console.WriteLine("Object: {0}", val)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Boolean: False
Boolean: True
SByte: 1
Byte: 2
Short: 3
UShort: 4
Integer: 5
UInteger: 6
Long: 7
ULong: 8
Decimal: 0
Decimal: 1
Decimal: 14
Decimal: 79228162514264337593543950335
Decimal: 0
Decimal: 1
Decimal: 14
Decimal: 79228162514264337593543950335
Single: 10
Double: 11
String: 12
Object: 13
Date: 8/23/1970 3:45:39 AM
Date: 1/1/0001 12:00:00 AM
Date: 8/23/1970 3:45:39 AM
Date: 1/1/0001 12:00:00 AM
Char: v
]]>)
        End Sub

        <Fact()>
        Public Sub Conversions01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <%= EmitResourceUtil.ConversionsILGenTestSource %>
    </file>
</compilation>,
            expectedOutput:=EmitResourceUtil.ConversionsILGenTestBaseline)

        End Sub

        <Fact()>
        Public Sub Conversions02()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <%= EmitResourceUtil.ConversionsILGenTestSource1 %>
    </file>
</compilation>,
            expectedOutput:=EmitResourceUtil.ConversionsILGenTestBaseline1)

        End Sub

        <Fact()>
        Public Sub Conversions03()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <%= EmitResourceUtil.ConversionsILGenTestSource2 %>
    </file>
</compilation>,
            expectedOutput:=<![CDATA[
Conversions from Nothing literal:
Boolean: False
SByte: 0
Byte: 0
Short: 0
UShort: 0
Integer: 0
UInteger: 0
Long: 0
ULong: 0
Single: 0
Double: 0
Decimal: 0
Date: 1/1/0001 12:00:00 AM
String: []
Object: []
Guid: 00000000-0000-0000-0000-000000000000
IComparable: []
ValueType: []
String: []
Guid: 00000000-0000-0000-0000-000000000000
]]>)
        End Sub

        <Fact()>
        Public Sub VirtualCalls()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
public class M1
    public class Boo1
        public I1 As Integer
        public I2 As Integer

        public Sub New()
        End Sub
    End Class

    public class Boo(Of T)
        public Sub New()
        End Sub

        public Sub Moo(x as T)

        Dim s1 as string = "hello"

        ' regular virtual
         System.Console.Write(s1.GetType())

         DIm iii as integer = 123
         ' constrained nongeneric
         System.Console.Write(iii.GetType())

         ' regular call
         System.Console.Write(iii.ToString())

         ' constrained generic
         Dim s as String = x.ToString()

         System.Console.WriteLine(s)
        End Sub
    End class

    public Shared Sub Main()
        DIm x as Boo1 = new Boo1()

        Dim b as Boo(of Boo1) = new Boo(of Boo1)()
        b.Moo(x)
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:="System.StringSystem.Int32123M1+Boo1").
            VerifyIL("M1.Boo(Of T).Moo",
            <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (Integer V_0) //iii
  IL_0000:  ldstr      "hello"
  IL_0005:  callvirt   "Function Object.GetType() As System.Type"
  IL_000a:  call       "Sub System.Console.Write(Object)"
  IL_000f:  ldc.i4.s   123
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  box        "Integer"
  IL_0018:  call       "Function Object.GetType() As System.Type"
  IL_001d:  call       "Sub System.Console.Write(Object)"
  IL_0022:  ldloca.s   V_0
  IL_0024:  call       "Function Integer.ToString() As String"
  IL_0029:  call       "Sub System.Console.Write(String)"
  IL_002e:  ldarga.s   V_1
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Function Object.ToString() As String"
  IL_003b:  call       "Sub System.Console.WriteLine(String)"
  IL_0040:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub BugInBranchShortening()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Module Program

    Function moo() As Integer
        console.writeline("hello")
        Return 5
    End Function

    Sub Main(args As String())
        Dim x As Integer = 0, xx = New Integer()
        Dim y As Double = 234
        Dim g As Long = 1234

        Do While x < 5
            If x < 1 Then
                Dim s As String = "-1"
                Console.Write(s)
            ElseIf x < 2 Then
                Dim s As String = "-2"
                Console.Write(s)
            ElseIf x < 3 Then
                Dim s As String = "-3"
                Console.Write(s)
            Else
                Dim s As String = "Else"
                Dim blah As String = "Should only see in Else, but even Dev10 does not get this right"
                Console.Write(s)
                Console.Write(s)
            End If

            x = x + 1
        Loop

        Console.WriteLine()
    End Sub
End Module

]]></file>
</compilation>,
    expectedOutput:="-1-2-3ElseElseElseElse")
        End Sub

        <Fact()>
        Public Sub ShortCircuitingOperators()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program

    Sub Main(args As String())
        Dim x As Integer = 0, y As Double = 1234.567, z As Long = 12345

        Do While x < 5 AndAlso z > 10
            If x < 1 OrElse z > 1233 Then
                Dim s As String = "-1"
                Console.Write(s)
            ElseIf x < 2 AndAlso z > 9 OrElse y > 12.34 Then
                Dim s As String = "-2"
                Console.Write(s)
            Else
                Dim s As String = "Else"
                Console.Write(s)
            End If

            x = x + 1
            z = z \ 10
            y = y / 10
        Loop

    End Sub
End Module
]]></file>
</compilation>,
    expectedOutput:="-1-1-2Else")
        End Sub

        <Fact()>
        Public Sub DifferentCaseScopeLocals()
            CompileAndVerify(
<compilation>
    <file name="xxx.yyy.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports VBN

Module Program

    Sub Main(args As String())
        Dim x As VBN.IGoo(Of Char, Char) = Nothing, y As CGoo(Of Char, Char), z As SGoo = Nothing, local As Boolean = True
        y = New CGoo(Of Char, Char)()
        Do
            If y IsNot Nothing OrElse x IsNot Nothing Then
                'x = DirectCast(y, IGoo(Of Char, Char))
                y.ImplF1("q"c, "c"c)
                Dim w As String = "If"
                y = Nothing
            ElseIf x Is Nothing AndAlso y Is Nothing AndAlso z.Field2 Is Nothing Then
                z = New SGoo()
                z.Field2 = New CGoo(Of SGoo, UShort)
                z.Field2.ImplF2("Aa", "Bb", "Cc")
                Dim w& = 11223344
                Console.Write(w)
            Else
                SGoo.Field1 = 9D
                Console.Write(SGoo.Field1)
                Dim w As Boolean = False
                Console.Write(w)
                local = False
                z.Field2.ImplF2("Booooooo")
            End If
        Loop While local
    End Sub
End Module
]]></file>
    <file name="Abc.123.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Namespace VBN
    Public Interface IGoo(Of T, V)
        Sub F1(ByRef x As T, ByVal y As V)
        Function F2(ParamArray ary As String()) As String
    End Interface

    Class CGoo(Of T, V)
        'Implements IGoo(Of T, V)
        Public Sub ImplF1(ByRef x As T, ByVal y As V) 'Implements IGoo(Of T, V).F1
            console.Write(String.Format("-ImpF1:{0},{1}-", x, y))
        End Sub
        Public Function ImplF2(ParamArray ary As String()) As String 'Implements IGoo(Of T, V).F2
            If (ary IsNot Nothing) Then
                Console.write("-ImpF2:{0}-", ary(0))
            Else
                console.write("-ImpF2:NoParam-")
            End If
            Return "F2"
        End Function
    End Class

    Friend Structure SGoo
        Public Shared Field1 As Decimal
        Public Field2 As CGoo(Of SGoo, UShort)
    End Structure

End Namespace
]]></file>
</compilation>,
    expectedOutput:="-ImpF1:q,c--ImpF2:Aa-112233449False-ImpF2:Booooooo-")
        End Sub

        <Fact()>
        Public Sub EnumBackingField()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Enum Enum1
        A = 1
        B
        C
    End Enum

    Sub ChangeIt(ByRef x As Integer)
        x = 3
    End Sub

    Sub Main()
        Dim e1 as Enum1 = Enum1.A
        e1.value__ = 2
        ChangeIt(e1.value__)
        System.Console.WriteLine(e1.ToString())
    End Sub
End Module
]]></file>
</compilation>,
    expectedOutput:="C")
        End Sub

#Region "Regressions"

        <WorkItem(543277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543277")>
        <Fact()>
        Public Sub Bug10931_1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System

Module Program
    Sub Main(args As String())
        Dim b = Function() Sub() Return
        Goo(Of Func(Of Action(Of Integer)))(b)()(1)
    End Sub

    Function Goo(Of T)(x As T) As T
        Return x
    End Function
End Module
    </file>
</compilation>,
    expectedOutput:="")
        End Sub

        <WorkItem(538751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538751")>
        <Fact()>
        Public Sub AssertCondBranchWithLogicOperator()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

Module Program

    Public Function CompareDouble(ByVal arg1 As Double, ByVal arg2 As Double) As Boolean
        Dim delta As Double = 12.1234566

        If delta < arg2 Or delta > arg1 Then
            Return True
        Else
            Return False
        End If
    End Function

    Sub Main(args As String())
        Dim arg1 As Double = 12.123456
        Dim arg2 As Double = 12.1234567

        Console.WriteLine(CompareDouble(arg1, arg2))
    End Sub
End Module
]]></file>
</compilation>,
    expectedOutput:="True")
        End Sub

        <WorkItem(538752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538752")>
        <Fact()>
        Public Sub StructureDefaultAccessibilityPublic()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

Namespace VBN

    Structure myStruct
        Dim x As Integer
        Sub M()
            Console.Write("PublicSub")
        End Sub
    End Structure

    Module Program

        Structure AnotherStruct
            Dim y As String
            Function Func(ByVal x As Integer) As Integer
                Return x
            End Function
        End Structure

        Sub Main()
            Dim s1 As myStruct
            s1.x = 400
            s1.M()

            Dim s2 As AnotherStruct
            s2.y = "TestString"

            Console.Write(s2.Func(s1.x))
        End Sub
    End Module

End Namespace
]]></file>
</compilation>,
    expectedOutput:="PublicSub400")
        End Sub

        <WorkItem(538800, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538800")>
        <Fact>
        Public Sub ObjectComparisonWithNoReferenceToVBRuntime()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class Program
    Sub Main()
        Dim o As Object = Nothing
        If o = o Then
            Console.Write("Pass")
        End If
    End Sub
End Class
]]></file>
</compilation>, OutputKind.DynamicallyLinkedLibrary).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_MissingRuntimeHelper, "o = o").WithArguments("Microsoft.VisualBasic.CompilerServices.Operators.ConditionalCompareObjectEqual"))
        End Sub

        <Fact>
        Public Sub ObjectComparisonWithNoReferenceToVBRuntime_1()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Program
    Sub Main()
        Dim o As Object = Nothing
        o = (o = o)
    End Sub
End Class
]]></file>
</compilation>, OutputKind.DynamicallyLinkedLibrary).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_MissingRuntimeHelper, "o = o").WithArguments("Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual"))
        End Sub

        <Fact>
        Public Sub ObjectComparisonWithNoReferenceToVBRuntime_2()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Program
    Sub Main()
        Dim o As Object = Nothing
        Dim b As Boolean = (o = o)
    End Sub
End Class
]]></file>
</compilation>, OutputKind.DynamicallyLinkedLibrary).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_MissingRuntimeHelper, "o = o").WithArguments("Microsoft.VisualBasic.CompilerServices.Operators.ConditionalCompareObjectEqual"))
        End Sub

        <Fact>
        Public Sub ObjectComparisonWithNoReferenceToVBRuntime_3()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Program
    Sub Main()
        Dim o As Object = Nothing
        Dim b As Boolean? = (o = o)
    End Sub
End Class
]]></file>
</compilation>, OutputKind.DynamicallyLinkedLibrary).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_MissingRuntimeHelper, "o = o").WithArguments("Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual"))
        End Sub

        <WorkItem(538792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538792")>
        <Fact>
        Public Sub InvokeGenericSharedMethods()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Public Class c1(Of T) ' Generics
    Public Shared Sub test()
        Console.WriteLine("Pass")
    End Sub
End Class
Module Program
    Sub Main()
        c1(Of String).test()
    End Sub
End Module
]]></file>
</compilation>,
            expectedOutput:="Pass")
        End Sub

        <WorkItem(538865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538865")>
        <Fact>
        Public Sub TestGetObjectValueCalls()
            ' ILVerify null ref
            ' Tracked by https//github.com/dotnet/roslyn/issues/58652
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Class TestClass1
    Sub New(x As Object)
    End Sub
End Class

Module Module1
    Sub M()
        Dim o As Object
        Dim a As Object() = Nothing
        o = a(1)
    End Sub

    Sub PassByRef(ByRef x As Object)
    End Sub

    Sub PassByVal(x As Object)
    End Sub

    Sub Test1(o As Object)
        PassByRef(o)
    End Sub

    Sub Test2(o As Object)
        PassByVal(o)
    End Sub

    Sub Test3(o As Object)
        Dim t3 As TestClass1
        t3 = New TestClass1(o)
    End Sub

    Sub Test4(o As Object)
        PassByRef((o))
    End Sub

    Function Test4_1(o As Object) As Object
        Return PropertiesWithByRef.P2(o)
    End Function

    Function Test4_2(o As Object) As Object
        Return PropertiesWithByRef.P2((o))
    End Function

    Sub Test5(o As Object)
        o = (o ^ o)
    End Sub
    Sub Test6(o As Object)
        o = (-o)
    End Sub
    Sub Test7(o As Object)
        o = (+o)
    End Sub
    Sub Test8(o As Object)
        o = ((((o / o))))
    End Sub
    Sub Test9(o As Object)
        o = (o Mod o)
    End Sub
    Sub Test10(o As Object)
        o = (o \ o)
    End Sub
    Sub Test11(o As Object)
        o = (o &amp; o)
    End Sub
    Sub Test12(o As Object)
        o = (Not o)
    End Sub
    Sub Test13(o As Object)
        o = (o And o)
    End Sub
    Sub Test14(o As Object)
        o = o AndAlso o
    End Sub
    Sub Test15(o As Object)
        o = (o Or o)
    End Sub
    Sub Test16(o As Object)
        o = o OrElse o
    End Sub
    Sub Test17(o As Object)
        o = (o Xor o)
    End Sub
    Sub Test18(o As Object)
        o = (o * o)
    End Sub
    Sub Test19(o As Object)
        o = (o + o)
    End Sub
    Sub Test20(o As Object)
        o = (o - o)
    End Sub
    Sub Test21(o As Object)
        o = (o &lt;&lt; o)
    End Sub
    Sub Test22(o As Object)
    o = (o &gt;&gt; o)
    End Sub

    Sub Test23(o As Object)
        o = (DirectCast(Nothing, String))
    End Sub
    Sub Test23_1(o As Object)
        o = TryCast(TryCast(Nothing, String), Object)
    End Sub
    Sub Test23_2(o As Object)
        o = CType(CType(Nothing, String), Object)
    End Sub

    Sub Test24(o As Object)
        o = (DirectCast("x", Object))
    End Sub
    Sub Test25(o As Object)
        o = (TryCast("x", Object))
    End Sub
    Sub Test26(o As Object)
        o = (CType("x", Object))
    End Sub
    Sub Test27(o As Object)
        o = (DirectCast(DirectCast(o, ValueType), Object))
    End Sub
    Sub Test28(o As Object)
        o = (DirectCast(New Guid(), Object))
    End Sub
    Sub Test29(o As Object)
        o = (CType(New Guid(), Object))
    End Sub
    Sub Test30(o As Object)
        o = (TryCast(New Guid(), Object))
    End Sub
    Sub Test31(Of T)(o As Object, x As T)
        o = (DirectCast(x, Object))
    End Sub
    Sub Test32(Of T)(o As Object, x As T)
        o = (CType(x, Object))
    End Sub
    Sub Test33(Of T)(o As Object, x As T)
        o = (TryCast(x, Object))
    End Sub
End Module

Module Program1
    Sub Test1()
        Dim x As Object = 1
        Dim s As Object
        s = x
    End Sub

    Sub Test2()
        Dim x As Object = 1
        Dim s As Object = x
    End Sub

    Sub Test3()
        Dim x As Object = 1
        Dim s As Object = If(fun1(), x, 2)
    End Sub

    Sub Test4()
        Dim x, y As New Object
    End Sub

    Sub Test5()
        Dim x As Object = New Object
    End Sub

    Sub Test6()
        Dim x As Object = 1
        Dim y As Integer = P2(x)
    End Sub

    Sub Test7()
        Dim x As System.ValueType = Nothing
        PassByRef1(x)
    End Sub

    Sub Test8()
        Dim x As System.Guid = Nothing
        PassByRef1(x)
    End Sub

    Sub Test9()
        Dim x As Object = Nothing
        PassByRef2(x)
    End Sub

    Sub Test10()
        Dim x As Object = Nothing
        PassByRef3(x)
    End Sub

    Sub Test11()
        PassByRef1(P1)
    End Sub

    Sub PassByRef1(ByRef x As Object)
    End Sub

    Sub PassByRef2(ByRef x As System.ValueType)
    End Sub

    Sub PassByRef3(ByRef x As System.Guid)
    End Sub

    Private Function fun1() As Object
        return Nothing
    End Function

    Property P1 As Object

    ReadOnly Property P2(x As Object) As Integer
        Get
            Return 1
        End Get
    End Property

End Module
    </file>
</compilation>, references:={TestReferences.SymbolsTests.PropertiesWithByRef}, verify:=Verification.FailsILVerify)

            verifier.VerifyIL("Module1.M",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldc.i4.1
  IL_0002:  ldelem.ref
  IL_0003:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0008:  pop
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Sub Module1.PassByRef(ByRef Object)"
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  call       "Sub Module1.PassByVal(Object)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  newobj     "Sub TestClass1..ctor(Object)"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.PassByRef(ByRef Object)"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_1",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function PropertiesWithByRef.get_P2(ByRef Object) As Object"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_2",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function PropertiesWithByRef.get_P2(ByRef Object) As Object"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test5",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ExponentObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.NegateObject(Object) As Object"
  IL_0006:  starg.s    V_0
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test7",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.PlusObject(Object) As Object"
  IL_0006:  starg.s    V_0
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test8",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.DivideObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test9",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ModObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test10",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.IntDivideObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test11",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.ConcatenateObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test12",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.NotObject(Object) As Object"
  IL_0006:  starg.s    V_0
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test13",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AndObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test14",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0006:  brfalse.s  IL_0010
  IL_0008:  ldarg.0
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_000e:  br.s       IL_0011
  IL_0010:  ldc.i4.0
  IL_0011:  box        "Boolean"
  IL_0016:  starg.s    V_0
  IL_0018:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test15",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.OrObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test16",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0006:  brtrue.s   IL_0010
  IL_0008:  ldarg.0
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_000e:  br.s       IL_0011
  IL_0010:  ldc.i4.1
  IL_0011:  box        "Boolean"
  IL_0016:  starg.s    V_0
  IL_0018:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test17",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.XorObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test18",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.MultiplyObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test19",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test20",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.SubtractObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test21",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.LeftShiftObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test22",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.RightShiftObject(Object, Object) As Object"
  IL_0007:  starg.s    V_0
  IL_0009:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test23",
            <![CDATA[
{
  // Code size        4 (0x4)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  starg.s    V_0
  IL_0003:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test23_1",
            <![CDATA[
{
  // Code size        4 (0x4)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  starg.s    V_0
  IL_0003:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test23_2",
            <![CDATA[
{
  // Code size        4 (0x4)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  starg.s    V_0
  IL_0003:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test24",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldstr      "x"
  IL_0005:  starg.s    V_0
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test25",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldstr      "x"
  IL_0005:  starg.s    V_0
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test26",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldstr      "x"
  IL_0005:  starg.s    V_0
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test27",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  castclass  "System.ValueType"
  IL_0006:  starg.s    V_0
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test28",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.Guid V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.Guid"
  IL_0008:  ldloc.0
  IL_0009:  box        "System.Guid"
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test29",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.Guid V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.Guid"
  IL_0008:  ldloc.0
  IL_0009:  box        "System.Guid"
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test30",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.Guid V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.Guid"
  IL_0008:  ldloc.0
  IL_0009:  box        "System.Guid"
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test31",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        "T"
  IL_0006:  starg.s    V_0
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test32",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        "T"
  IL_0006:  starg.s    V_0
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test33",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        "T"
  IL_0006:  isinst     "Object"
  IL_000b:  starg.s    V_0
  IL_000d:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test1",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test2",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test3",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Object V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  stloc.0
  IL_0007:  call       "Function Program1.fun1() As Object"
  IL_000c:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0011:  brtrue.s   IL_001b
  IL_0013:  ldc.i4.2
  IL_0014:  box        "Integer"
  IL_0019:  br.s       IL_001c
  IL_001b:  ldloc.0
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  pop
  IL_0022:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test4",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  pop
  IL_000b:  newobj     "Sub Object..ctor()"
  IL_0010:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0015:  pop
  IL_0016:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test5",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test6",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  call       "Function Program1.get_P2(Object) As Integer"
  IL_0010:  pop
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("Program1.get_P1",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     "Program1._P1 As Object"
  IL_0005:  ret
}
]]>)

            verifier.VerifyIL("Program1.set_P1",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  stsfld     "Program1._P1 As Object"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test7",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Sub Program1.PassByRef1(ByRef Object)"
  IL_0009:  ldloc.0
  IL_000a:  castclass  "System.ValueType"
  IL_000f:  pop
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test8",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (System.Guid V_0,
  Object V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.Guid"
  IL_0008:  ldloc.0
  IL_0009:  box        "System.Guid"
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Sub Program1.PassByRef1(ByRef Object)"
  IL_0016:  ldloc.1
  IL_0017:  dup
  IL_0018:  brtrue.s   IL_0026
  IL_001a:  pop
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    "System.Guid"
  IL_0023:  ldloc.0
  IL_0024:  br.s       IL_002b
  IL_0026:  unbox.any  "System.Guid"
  IL_002b:  pop
  IL_002c:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test9",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.ValueType V_0)
  IL_0000:  ldnull
  IL_0001:  castclass  "System.ValueType"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Program1.PassByRef2(ByRef System.ValueType)"
  IL_000e:  ldloc.0
  IL_000f:  pop
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test10",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (System.Guid V_0,
  System.Guid V_1)
  IL_0000:  ldnull
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0010
  IL_0004:  pop
  IL_0005:  ldloca.s   V_1
  IL_0007:  initobj    "System.Guid"
  IL_000d:  ldloc.1
  IL_000e:  br.s       IL_0015
  IL_0010:  unbox.any  "System.Guid"
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       "Sub Program1.PassByRef3(ByRef System.Guid)"
  IL_001d:  ldloc.0
  IL_001e:  box        "System.Guid"
  IL_0023:  pop
  IL_0024:  ret
}
]]>)

            verifier.VerifyIL("Program1.Test11",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  call       "Function Program1.get_P1() As Object"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       "Sub Program1.PassByRef1(ByRef Object)"
  IL_0012:  ldloc.0
  IL_0013:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0018:  call       "Sub Program1.set_P1(Object)"
  IL_001d:  ret
}
]]>)
        End Sub

        <WorkItem(540882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540882")>
        <Fact>
        Public Sub TestGetObjectValueCalls2()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Module Module1
    Sub S()
        Dim x As New Object
    End Sub
    Function Func1(x As Object) As Object
        Return X
    End Function
    Function Func2() As Object
        Dim x As New Object
        Return X
    End Function
End Module
    </file>
</compilation>)

            verifier.VerifyIL("Module1.S",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.Func2",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  ret
}
]]>)

            verifier.VerifyIL("Module1.Func1",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>)

            verifier.VerifyIL("Module1.Func2",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  ret
}
]]>)
        End Sub

        <WorkItem(538853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538853")>
        <Fact>
        Public Sub Bug4597()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Function Count() as integer
        return 1
    End Function

    Sub M()
        Dim arr As Integer() = New Integer(2147483647) {}
        Dim arr1 As Integer() = New Integer(Count()) {}
        Dim arr2 As Integer() = New Integer(-1) {}
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldc.i4     0x80000000
  IL_0005:  newarr     "Integer"
  IL_000a:  pop
  IL_000b:  call       "Function M1.Count() As Integer"
  IL_0010:  ldc.i4.1
  IL_0011:  add.ovf
  IL_0012:  newarr     "Integer"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  newarr     "Integer"
  IL_001e:  pop
  IL_001f:  ret
}
]]>)
        End Sub

        <WorkItem(538852, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538852")>
        <Fact>
        Public Sub Bug4596()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Function Count() as integer
        return 1
    End Function

    Sub M()
        Dim by As Byte() = New Byte() {0, 127, 223, 128, 220}
        Dim d As Double() = New Double() {1.1F, 2.2F, -3.3F / 0, 4.4F, -5.5F}
        Dim b As Boolean() = New Boolean() {True, False, True, False, True}
        Dim c As Char() = New Char() {"a"c, "b"c, "c"c, "d"c, "e"c}
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithModuleName("MODULE")).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "Byte"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.5BD81897B38CE00BCF990B5AED9316FE43E7A3854DA09401C14DF4AF21B2F90D"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  pop
  IL_0012:  ldc.i4.5
  IL_0013:  newarr     "Double"
  IL_0018:  dup
  IL_0019:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=40 <PrivateImplementationDetails>.F9D819AD50107F52959882DF3549DE08003AC644DCC12AFEA2B9BD936EE7D325"
  IL_001e:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0023:  pop
  IL_0024:  ldc.i4.5
  IL_0025:  newarr     "Boolean"
  IL_002a:  dup
  IL_002b:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=5 <PrivateImplementationDetails>.A4E9167DC11A5B8BA7E09C85BAFDEA0B6E0B399CE50086545509017050B33097"
  IL_0030:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0035:  pop
  IL_0036:  ldc.i4.5
  IL_0037:  newarr     "Char"
  IL_003c:  dup
  IL_003d:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.DC0F42A41F058686A364AF5B6BD49175C5B2CF3C4D5AE95417448BE3517B4008"
  IL_0042:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0047:  pop
  IL_0048:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayInitFromBlobEnum()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module M1
    Sub M()
        Dim x As System.TypeCode() = {
                        System.TypeCode.Boolean,
                        System.TypeCode.Byte,
                        System.TypeCode.Char,
                        System.TypeCode.DateTime,
                        System.TypeCode.DBNull}
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  4
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "System.TypeCode"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.3
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.6
  IL_000d:  stelem.i4
  IL_000e:  dup
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.4
  IL_0011:  stelem.i4
  IL_0012:  dup
  IL_0013:  ldc.i4.3
  IL_0014:  ldc.i4.s   16
  IL_0016:  stelem.i4
  IL_0017:  dup
  IL_0018:  ldc.i4.4
  IL_0019:  ldc.i4.2
  IL_001a:  stelem.i4
  IL_001b:  pop
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayInitFromBlobEnumNetFx45()
            ' In NetFx 4.5 and higher, we can use fast literal initialization for enums
            CompileAndVerify(CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M1
    Sub M()
        Dim x As System.TypeCode() = {
                        System.TypeCode.Boolean,
                        System.TypeCode.Byte,
                        System.TypeCode.Char,
                        System.TypeCode.DateTime,
                        System.TypeCode.DBNull}
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithModuleName("MODULE"))).VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "System.TypeCode"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.095C4722BB84316FEEE41ADFDAFED13FECA8836A520BDE3AB77C52F5C9F6B620"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  pop
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TryCastAndPropertyAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Clazz
    Public Property Prop As String
End Class
Module M1
    Sub M()
        Dim c As New Clazz()
        Dim d = TryCast(c.Prop, Object)
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("M1.M",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  newobj     "Sub Clazz..ctor()"
  IL_0005:  callvirt   "Function Clazz.get_Prop() As String"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)
        End Sub

        ''' <summary>
        ''' Won't fix :(
        ''' </summary>
        ''' <remarks></remarks>
        <Fact, WorkItem(527773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527773")>
        Public Sub ConstantLiteralToDecimal()
            CompileAndVerify(
<compilation>
    <file name="a.b.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

Module Program

    Sub Main()

        Print(0) ' ldc.i4.0 - decimal.ctor(int32)
        Print(1) ' ldc.i4.1 - decimal.ctor(int32)
        Print(8) ' ldc.i4.8 - decimal.ctor(int32)
        Print(-1) ' ldc.i4.m1 - decimal.ctor(int32)
        Print(-128) ' ldc.i4.s - decimal.ctor(int32)
        Print(2147483647) ' ldc.i4 -  - decimal.ctor(int32)
        Print(-2147483648) ' ldc.i4 -  - decimal.ctor(int32)
        Print(4294967295) ' ldc.i4 -  - decimal.ctor(uint32)
        Print(9223372036854775807) ' ldc.i8 - decimal.ctor(int64)
        Print(-9223372036854775807) ' ldc.i8 - decimal.ctor(int64)
        Print(18446744073709551615UL) ' ldc.i8 - decimal.ctor(uint64)
        Print(-79228162514264337593543950335D) ' decimal.ctor(int32, int32, int32, bool, byte)
        Print(12345.679F)  '? ldc.r4 - decimal.ctor(Single)

    End Sub

     Public Sub Print(ByVal val As Decimal)
        Console.WriteLine(val)
    End Sub
End Module
]]></file>
</compilation>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      192 (0xc0)
  .maxstack  5
  IL_0000:  ldsfld     "Decimal.Zero As Decimal"
  IL_0005:  call       "Sub Program.Print(Decimal)"
  IL_000a:  ldsfld     "Decimal.One As Decimal"
  IL_000f:  call       "Sub Program.Print(Decimal)"
  IL_0014:  ldc.i4.8
  IL_0015:  conv.i8
  IL_0016:  newobj     "Sub Decimal..ctor(Long)"
  IL_001b:  call       "Sub Program.Print(Decimal)"
  IL_0020:  ldsfld     "Decimal.MinusOne As Decimal"
  IL_0025:  call       "Sub Program.Print(Decimal)"
  IL_002a:  ldc.i4.s   -128
  IL_002c:  conv.i8
  IL_002d:  newobj     "Sub Decimal..ctor(Long)"
  IL_0032:  call       "Sub Program.Print(Decimal)"
  IL_0037:  ldc.i4     0x7fffffff
  IL_003c:  conv.i8
  IL_003d:  newobj     "Sub Decimal..ctor(Long)"
  IL_0042:  call       "Sub Program.Print(Decimal)"
  IL_0047:  ldc.i4     0x80000000
  IL_004c:  conv.i8
  IL_004d:  newobj     "Sub Decimal..ctor(Long)"
  IL_0052:  call       "Sub Program.Print(Decimal)"
  IL_0057:  ldc.i4.m1
  IL_0058:  conv.u8
  IL_0059:  newobj     "Sub Decimal..ctor(Long)"
  IL_005e:  call       "Sub Program.Print(Decimal)"
  IL_0063:  ldc.i4.m1
  IL_0064:  ldc.i4     0x7fffffff
  IL_0069:  ldc.i4.0
  IL_006a:  ldc.i4.0
  IL_006b:  ldc.i4.0
  IL_006c:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0071:  call       "Sub Program.Print(Decimal)"
  IL_0076:  ldc.i4.m1
  IL_0077:  ldc.i4     0x7fffffff
  IL_007c:  ldc.i4.0
  IL_007d:  ldc.i4.1
  IL_007e:  ldc.i4.0
  IL_007f:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0084:  call       "Sub Program.Print(Decimal)"
  IL_0089:  ldc.i4.m1
  IL_008a:  ldc.i4.m1
  IL_008b:  ldc.i4.0
  IL_008c:  ldc.i4.0
  IL_008d:  ldc.i4.0
  IL_008e:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_0093:  call       "Sub Program.Print(Decimal)"
  IL_0098:  ldc.i4.m1
  IL_0099:  ldc.i4.m1
  IL_009a:  ldc.i4.m1
  IL_009b:  ldc.i4.1
  IL_009c:  ldc.i4.0
  IL_009d:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_00a2:  call       "Sub Program.Print(Decimal)"
  IL_00a7:  ldc.i4     0x85f0d5ff
  IL_00ac:  ldc.i4     0x7048
  IL_00b1:  ldc.i4.0
  IL_00b2:  ldc.i4.0
  IL_00b3:  ldc.i4.s   10
  IL_00b5:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_00ba:  call       "Sub Program.Print(Decimal)"
  IL_00bf:  ret
}
]]>)
        End Sub

#End Region

        <Fact>
        Public Sub Enum1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Enum E1
    x = 42
    y
End Enum

Module M1
    Sub Main()
        Console.Write(E1.x)
        Console.Write(E1.y)
        Console.Write(System.Enum.Parse(E1.y.GetType(), "42"))
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="4243x").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldc.i4.s   42
  IL_0002:  call       "Sub System.Console.Write(Integer)"
  IL_0007:  ldc.i4.s   43
  IL_0009:  call       "Sub System.Console.Write(Integer)"
  IL_000e:  ldc.i4.s   43
  IL_0010:  box        "E1"
  IL_0015:  call       "Function Object.GetType() As System.Type"
  IL_001a:  ldstr      "42"
  IL_001f:  call       "Function System.Enum.Parse(System.Type, String) As Object"
  IL_0024:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0029:  call       "Sub System.Console.Write(Object)"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallsOnConst()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Sub Main()
        Console.Write(42.ToString())
        Console.Write(42.GetType())
        Console.Write(Integer.MaxValue.ToString())
        Console.Write(Integer.MaxValue.GetType())
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:=" 42System.Int322147483647System.Int32").
            VerifyIL("M1.Main",
            <![CDATA[
{
// Code size       71 (0x47)
.maxstack  1
.locals init (Integer V_0)
IL_0000:  ldc.i4.s   42
IL_0002:  stloc.0
IL_0003:  ldloca.s   V_0
IL_0005:  call       "Function Integer.ToString() As String"
IL_000a:  call       "Sub System.Console.Write(String)"
IL_000f:  ldc.i4.s   42
IL_0011:  box        "Integer"
IL_0016:  call       "Function Object.GetType() As System.Type"
IL_001b:  call       "Sub System.Console.Write(Object)"
IL_0020:  ldc.i4     0x7fffffff
IL_0025:  stloc.0
IL_0026:  ldloca.s   V_0
IL_0028:  call       "Function Integer.ToString() As String"
IL_002d:  call       "Sub System.Console.Write(String)"
IL_0032:  ldc.i4     0x7fffffff
IL_0037:  box        "Integer"
IL_003c:  call       "Function Object.GetType() As System.Type"
IL_0041:  call       "Sub System.Console.Write(Object)"
IL_0046:  ret
}
]]>)
        End Sub

        <WorkItem(539920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539920")>
        <Fact>
        Public Sub TestNestedFunctionLambdas()
            CompileAndVerify(
<compilation>
    <file name="Program.vb">
Imports System

Module Program
    Sub Main()
        Goo(Function(x) Function() x)
    End Sub

    Sub Goo(x As Func(Of String, Func(Of String)))
        Console.WriteLine(x.Invoke("ABC").Invoke().ToLower())
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="abc")
        End Sub

        <WorkItem(540121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540121")>
        <Fact>
        Public Sub TestEmitBinaryTrueAndFalseExpressionForDebug()
            CompileAndVerify(
<compilation>
    <file name="Module1.vb">
Module Module1
    Sub Main()
        If True And False Then
        End If
    End Sub
End Module
    </file>
</compilation>,
                expectedOutput:="")
        End Sub

        <WorkItem(540121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540121")>
        <Fact>
        Public Sub BooleanAndOrInDebug()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module M1
    Sub Main()
        Dim b1 As Boolean = True
        Dim b2 As Boolean = False
        If b1 And b2 Then
        End If
        If False Or True Then
        End If
        If False And True Then
        End If
    End Sub
End Module
            </file>
</compilation>,
                options:=TestOptions.DebugExe,
                expectedOutput:="")

            c.VerifyIL("M1.Main", "
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (Boolean V_0, //b1
                Boolean V_1, //b2
                Boolean V_2,
                Boolean V_3,
                Boolean V_4)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
 -IL_0003:  ldc.i4.0
  IL_0004:  stloc.1
 -IL_0005:  ldloc.0
  IL_0006:  ldloc.1
  IL_0007:  and
  IL_0008:  stloc.2
 ~IL_0009:  ldloc.2
  IL_000a:  brfalse.s  IL_000d
 -IL_000c:  nop
 -IL_000d:  nop
 -IL_000e:  ldc.i4.1
  IL_000f:  stloc.3
 -IL_0010:  nop
 -IL_0011:  nop
 -IL_0012:  ldc.i4.0
  IL_0013:  stloc.s    V_4
  IL_0015:  br.s       IL_0017
 -IL_0017:  nop
 -IL_0018:  ret
}
", sequencePoints:="M1.Main")
        End Sub

        <Fact>
        Public Sub ForLoopSimple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
        For i as integer = 1 to 10 step 1
            i = i + 1
            Console.Write(i)
        Next
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="246810").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "Sub System.Console.Write(Integer)"
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  add.ovf
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.s   10
  IL_0013:  ble.s      IL_0002
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ForLoopSimple1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Dim i as integer = 42
    Sub Main()
        For i = 1 to 10 step 1
            i = i + 1
            Console.Write(i)
        Next
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="246810").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  stsfld     "M1.i As Integer"
  IL_0006:  ldsfld     "M1.i As Integer"
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stsfld     "M1.i As Integer"
  IL_0012:  ldsfld     "M1.i As Integer"
  IL_0017:  call       "Sub System.Console.Write(Integer)"
  IL_001c:  ldsfld     "M1.i As Integer"
  IL_0021:  ldc.i4.1
  IL_0022:  add.ovf
  IL_0023:  stsfld     "M1.i As Integer"
  IL_0028:  ldsfld     "M1.i As Integer"
  IL_002d:  ldc.i4.s   10
  IL_002f:  ble.s      IL_0006
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ForLoopSimple2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Dim i as integer = 42
    Sub Main()
        For i = 1 to 20 step 1
            if i > 10
                exit for
            End if
            if i mod 2 = 0
                Console.Write(i)
                Continue For
            end if
        Next
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="246810").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  stsfld     "M1.i As Integer"
  IL_0006:  ldsfld     "M1.i As Integer"
  IL_000b:  ldc.i4.s   10
  IL_000d:  bgt.s      IL_0037
  IL_000f:  ldsfld     "M1.i As Integer"
  IL_0014:  ldc.i4.2
  IL_0015:  rem
  IL_0016:  brtrue.s   IL_0022
  IL_0018:  ldsfld     "M1.i As Integer"
  IL_001d:  call       "Sub System.Console.Write(Integer)"
  IL_0022:  ldsfld     "M1.i As Integer"
  IL_0027:  ldc.i4.1
  IL_0028:  add.ovf
  IL_0029:  stsfld     "M1.i As Integer"
  IL_002e:  ldsfld     "M1.i As Integer"
  IL_0033:  ldc.i4.s   20
  IL_0035:  ble.s      IL_0006
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ForLoopNonConst()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Dim p1 As Integer
    Sub Main()
        p1 = 3

        For p1 = 6 To p1 * 4 Step p1 - 2
            Console.Write(p1)
        Next

        Console.Write(" ")
        Console.Write(p1)
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="6789101112 13").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Integer V_0,
  Integer V_1)
  IL_0000:  ldc.i4.3
  IL_0001:  stsfld     "M1.p1 As Integer"
  IL_0006:  ldsfld     "M1.p1 As Integer"
  IL_000b:  ldc.i4.4
  IL_000c:  mul.ovf
  IL_000d:  stloc.0
  IL_000e:  ldsfld     "M1.p1 As Integer"
  IL_0013:  ldc.i4.2
  IL_0014:  sub.ovf
  IL_0015:  stloc.1
  IL_0016:  ldc.i4.6
  IL_0017:  stsfld     "M1.p1 As Integer"
  IL_001c:  br.s       IL_0034
  IL_001e:  ldsfld     "M1.p1 As Integer"
  IL_0023:  call       "Sub System.Console.Write(Integer)"
  IL_0028:  ldsfld     "M1.p1 As Integer"
  IL_002d:  ldloc.1
  IL_002e:  add.ovf
  IL_002f:  stsfld     "M1.p1 As Integer"
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4.s   31
  IL_0037:  shr
  IL_0038:  ldsfld     "M1.p1 As Integer"
  IL_003d:  xor
  IL_003e:  ldloc.1
  IL_003f:  ldc.i4.s   31
  IL_0041:  shr
  IL_0042:  ldloc.0
  IL_0043:  xor
  IL_0044:  ble.s      IL_001e
  IL_0046:  ldstr      " "
  IL_004b:  call       "Sub System.Console.Write(String)"
  IL_0050:  ldsfld     "M1.p1 As Integer"
  IL_0055:  call       "Sub System.Console.Write(Integer)"
  IL_005a:  ret
}
]]>)
        End Sub

        <WorkItem(540443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540443")>
        <Fact>
        Public Sub LoadReadOnlyField()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class A
    Public ReadOnly Value As Integer
    Public Sub New(v As Integer)
        Value = v
        Value.ToString
    End Sub
    Public Overrides Function ToString() As String
        Return Value.ToString()
    End Function

    private sub goo(byref x as integer)
    end sub
    private sub moo()
        goo(Value)
    end sub
End Class
    </file>
</compilation>).
                VerifyIL("A.ToString",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "A.Value As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function Integer.ToString() As String"
  IL_000e:  ret
}
]]>).
VerifyIL("A..ctor",
            <![CDATA[
{
// Code size       26 (0x1a)
.maxstack  2
IL_0000:  ldarg.0
IL_0001:  call       "Sub Object..ctor()"
IL_0006:  ldarg.0
IL_0007:  ldarg.1
IL_0008:  stfld      "A.Value As Integer"
IL_000d:  ldarg.0
IL_000e:  ldflda     "A.Value As Integer"
IL_0013:  call       "Function Integer.ToString() As String"
IL_0018:  pop
IL_0019:  ret
}
]]>).
VerifyIL("A.moo",
            <![CDATA[
{
// Code size       16 (0x10)
.maxstack  2
.locals init (Integer V_0)
IL_0000:  ldarg.0
IL_0001:  ldarg.0
IL_0002:  ldfld      "A.Value As Integer"
IL_0007:  stloc.0
IL_0008:  ldloca.s   V_0
IL_000a:  call       "Sub A.goo(ByRef Integer)"
IL_000f:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ConstByRef()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Dim i as integer = 42
    Sub Main()
        Goo(Integer.MaxValue)
    End Sub

    Sub Goo(ByRef x as integer)
        x = 42
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="").
            VerifyIL("M1.Main",
            <![CDATA[
{
// Code size       14 (0xe)
.maxstack  1
.locals init (Integer V_0)
IL_0000:  ldc.i4     0x7fffffff
IL_0005:  stloc.0
IL_0006:  ldloca.s   V_0
IL_0008:  call       "Sub M1.Goo(ByRef Integer)"
IL_000d:  ret
}
]]>)
        End Sub

        ' Constructor initializers don't bind yet
        <WorkItem(7926, "DevDiv_Projects/Roslyn")>
        <WorkItem(541123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541123")>
        <Fact>
        Public Sub StructDefaultConstructorInitializer()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Structure S
    Public Sub New(x As Integer)
        Me.New() ' Note: not allowed in Dev10
    End Sub
End Structure
    </file>
</compilation>).
            VerifyIL("S..ctor(Integer)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub StructConstructorCallWithSideEffects()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C1
    Public Sub New(s As String)
        Console.WriteLine("C1.New(""" + s + """)")
    End Sub
    Public Property P As String
        Get
            Console.WriteLine("C1.P.Get")
            Return Nothing
        End Get
        Set(value As String)
            Console.WriteLine("C1.P.Set")
        End Set
    End Property
End Class

Structure S1
    Public Shared FLD As C1 = New C1("S1.FLD")
    Public Sub New(x As C1)
        Me.New(x.P)
        Console.WriteLine("S1.New(x As C1)")
    End Sub
    Public Sub New(ByRef x As String)
        Console.WriteLine("S1.New(x As String)")
    End Sub

    Shared Sub Main()
        Dim s1 As S1 = New S1(FLD)
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
C1.New("S1.FLD")
C1.P.Get
S1.New(x As String)
C1.P.Set
S1.New(x As C1)
]]>).
            VerifyIL("S1..cctor()",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      "S1.FLD"
  IL_0005:  newobj     "Sub C1..ctor(String)"
  IL_000a:  stsfld     "S1.FLD As C1"
  IL_000f:  ret
}
]]>).
            VerifyIL("S1..ctor(C1)",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (C1 V_0,
  String V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  callvirt   "Function C1.get_P() As String"
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       "Sub S1..ctor(ByRef String)"
  IL_0011:  ldloc.0
  IL_0012:  ldloc.1
  IL_0013:  callvirt   "Sub C1.set_P(String)"
  IL_0018:  ldstr      "S1.New(x As C1)"
  IL_001d:  call       "Sub System.Console.WriteLine(String)"
  IL_0022:  ret
}
]]>).
            VerifyIL("S1..ctor(ByRef String)",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ldstr      "S1.New(x As String)"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ret
}
]]>)
        End Sub

        <WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        <Fact>
        Public Sub StructConstructorWithOptionalParametersCSVB()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;

public struct SymbolModifiers
{
    public SymbolModifiers(bool isStatic = true, bool isSupa = true)
    {
        Console.Write("isStatic = " + isStatic.ToString() + ", isSupa = " + isSupa.ToString() + "; ");
    }
}
]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Class S
    Shared Sub Main(args() As String)
        Dim z1 = New SymbolModifiers(isSupa:=False)
        Dim z2 = New SymbolModifiers(isStatic:=False)
        Dim z3 = New SymbolModifiers()
    End Sub
End Class
]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation})
            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="isStatic = True, isSupa = False; isStatic = False, isSupa = True; ")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        <Fact>
        Public Sub StructConstructorWithOptionalParametersVBVB()

            Dim vbCompilationA = CreateVisualBasicCompilation("VBDllA",
            <![CDATA[
Imports System
Public Structure STRUCT
    Public Sub New(Optional ByVal x As Integer = 0)
        Console.WriteLine("Public Sub New(Optional ByVal x As Integer = 0)")
    End Sub
End Structure
]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            vbCompilationA.VerifyDiagnostics()

            Dim vbCompilationB = CreateVisualBasicCompilation("VBExeB",
            <![CDATA[
Class S
    Shared Sub Main(args() As String)
        Dim z1 = New STRUCT(1)
        Dim z2 = New STRUCT()
    End Sub
End Class
]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={vbCompilationA})
            Dim vbexeVerifier = CompileAndVerify(vbCompilationB,
                                                 expectedOutput:="Public Sub New(Optional ByVal x As Integer = 0)")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        <Fact>
        Public Sub StructConstructorWithOptionalParametersVBVB2()

            Dim vbCompilationA = CreateVisualBasicCompilation("VBDllA",
            <![CDATA[
Imports System
Public Structure STRUCT
    Public Sub New(ParamArray x() As Integer)
        Console.WriteLine("ParamArray x() As Integer")
    End Sub
End Structure
]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            vbCompilationA.VerifyDiagnostics()

            Dim vbCompilationB = CreateVisualBasicCompilation("VBExeB",
            <![CDATA[
Class S
    Shared Sub Main(args() As String)
        Dim z1 = New STRUCT(1)
        Dim z2 = New STRUCT()
    End Sub
End Class
]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={vbCompilationA})
            Dim vbexeVerifier = CompileAndVerify(vbCompilationB,
                                                 expectedOutput:="ParamArray x() As Integer")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub OverridingFunctionsOverloadedOnOptionalParameters()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class base
    Public Overridable Sub f(x As Integer)
        Console.WriteLine("BASE: Public Overridable Sub f(x As Integer)")
    End Sub
    Public Overridable Sub f(x As Integer, Optional y As Integer = 0)
        Console.WriteLine("BASE: Public Overridable Sub f(x As Integer, Optional y As Integer = 0)")
    End Sub
    Public Overridable Sub f(x As Integer, Optional y As String = "")
        Console.WriteLine("BASE: Public Overridable Sub f(x As Integer, Optional y As String = """")")
    End Sub
End Class

Class derived
    Inherits base
    Public Overrides Sub f(x As Integer)
        Console.WriteLine("DERIVED: Public Overridable Sub f(x As Integer)")
    End Sub
    Public Overrides Sub f(x As Integer, Optional y As Integer = 0)
        Console.WriteLine("DERIVED: Public Overridable Sub f(x As Integer, Optional y As Integer = 0)")
    End Sub
    Public Overrides Sub f(x As Integer, Optional y As String = "")
        Console.WriteLine("DERIVED: Public Overridable Sub f(x As Integer, Optional y As String = """")")
    End Sub
End Class

Module Program
    Sub Main(args As String())
        Test(New base)
        Test(New derived)
    End Sub
    Sub Test(b As base)
        b.f(1)
        b.f(1, 2)
        b.f(1, "")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
BASE: Public Overridable Sub f(x As Integer)
BASE: Public Overridable Sub f(x As Integer, Optional y As Integer = 0)
BASE: Public Overridable Sub f(x As Integer, Optional y As String = "")
DERIVED: Public Overridable Sub f(x As Integer)
DERIVED: Public Overridable Sub f(x As Integer, Optional y As Integer = 0)
DERIVED: Public Overridable Sub f(x As Integer, Optional y As String = "")
]]>)
        End Sub

        <Fact>
        Public Sub OverridingFunctionsOverloadedOnOptionalParameters2()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class base1
    Public Overridable Sub f(x As Integer)
        Console.WriteLine("BASE1: f(x As Integer)")
    End Sub
    Public Overridable Sub f(x As Integer, Optional y As String = "")
        Console.WriteLine("BASE1: f(x As Integer, Optional y As String = """")")
    End Sub
End Class

Class base2
    Inherits base1
    Public Overridable Overloads Sub f(x As Integer, Optional y As Integer = 0)
        Console.WriteLine("BASE2: f(x As Integer, Optional y As Integer = 0)")
    End Sub
End Class

Class derived
    Inherits base2
    Public Overrides Sub f(x As Integer)
        Console.WriteLine("DERIVED: f(x As Integer)")
    End Sub
    Public Overrides Sub f(x As Integer, Optional y As Integer = 0)
        Console.WriteLine("DERIVED: f(x As Integer, Optional y As Integer = 0)")
    End Sub
    Public Overrides Sub f(x As Integer, Optional y As String = "")
        Console.WriteLine("DERIVED: f(x As Integer, Optional y As String = """")")
    End Sub
End Class

Module Program
    Sub Main(args As String())
        Test(New base2)
        Test(New derived)
    End Sub
    Sub Test(b As base2)
        b.f(1)
        b.f(1, 2)
        b.f(1, "")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
BASE2: f(x As Integer, Optional y As Integer = 0)
BASE2: f(x As Integer, Optional y As Integer = 0)
BASE1: f(x As Integer, Optional y As String = "")
DERIVED: f(x As Integer, Optional y As Integer = 0)
DERIVED: f(x As Integer, Optional y As Integer = 0)
DERIVED: f(x As Integer, Optional y As String = "")
]]>)
        End Sub
        <WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        <Fact>
        Public Sub OverloadsWithOnlyOptionalParameters()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;

public class OverloadsUsingOptional
{
    public static void M(int k, int l = 0)
    {
        Console.Write("M(int k, int l = 0);");
    }
    public static void M(int k, int l = 0, int m = 0)
    {
        Console.Write("M(int k, int l = 0, int m = 0);");
    }
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Class S
    Shared Sub Main(args() As String)
        OverloadsUsingOptional.M(1, 2)
        OverloadsUsingOptional.M(1, 2, 3)
    End Sub
End Class
]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation})
            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="M(int k, int l = 0);M(int k, int l = 0, int m = 0);")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <WorkItem(543751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543751")>
        <Fact>
        Public Sub OverloadsWithOnlyOptionalParameters2()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;

public class OverloadsUsingOptional
{
    public static void M(int k, int l = 0)
    {
        Console.Write("M(int k, int l = 0);");
    }
    public static void M(int k, int l = 0, int m = 0)
    {
        Console.Write("M(int k, int l = 0, int m = 0);");
    }
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class S
    Shared Sub Main(args() As String)
        OverloadsUsingOptional.M(1)
    End Sub
End Class
]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation})

            CompilationUtils.AssertTheseDiagnostics(vbCompilation,
<errors>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Overloads Sub M(k As Integer, [l As Integer = 0])': Not most specific.
    'Public Shared Overloads Sub M(k As Integer, [l As Integer = 0], [m As Integer = 0])': Not most specific.
        OverloadsUsingOptional.M(1)
                               ~
</errors>)
        End Sub

        <Fact>
        Public Sub StructureConstructorsWithOptionalAndParamArrayParameters()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Namespace N
    Structure A
        Public Sub New(Optional x As Integer = 0)
            Console.Write("New(Optional x As Integer = 0);")
        End Sub
        Public Sub New(ParamArray x As Integer())
            Console.Write("New(ParamArray x As Integer());")
        End Sub
        Public Shared Sub Main()
            Dim a1 As New A
            Dim a2 As New A(1)
            Dim a3 As New A(1, 2)
        End Sub
    End Structure
End Namespace
    </file>
</compilation>, expectedOutput:="New(Optional x As Integer = 0);New(ParamArray x As Integer());")

        End Sub

        <Fact>
        Public Sub HidingBySignatureWithOptionalParameters()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
MustInherit Class A
    Public MustOverride Sub f(Optional x As String = "")
End Class

MustInherit Class B
    Inherits A
    Public MustOverride Overloads Sub f()
End Class

Class C
    Inherits B
    Public Overloads Overrides Sub f(Optional x As String = "")
        Console.Write("f(Optional x As String = "");")
    End Sub
    Public Overloads Overrides Sub f()
        Console.Write("f();")
    End Sub
    Public Shared Sub Main()
        Dim c As New C
        c.f()
        c.f("")
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="f();f(Optional x As String = "");")

        End Sub

        <Fact>
        Public Sub HidingBySignatureWithOptionalParameters2()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class A
    Public Overridable Sub f(Optional x As String = "")
    End Sub
End Class

Class B
    Inherits A
    Public Overridable Overloads Sub f()
    End Sub
End Class

Class BB
    Inherits B
    Private Overloads Sub f()
    End Sub
    Private Overloads Sub f(Optional x As String = "")
    End Sub
End Class

Class C
    Inherits BB
    Public Overloads Overrides Sub f(Optional x As String = "")
        Console.Write("f(Optional x As String = "");")
    End Sub
    Public Overloads Overrides Sub f()
        Console.Write("f();")
    End Sub
    Public Shared Sub Main()
        Dim c As New C
        c.f()
        c.f("")
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="f();f(Optional x As String = "");")

        End Sub

        <Fact>
        Public Sub ImplicitStructParameterlessConstructorCallWithMe()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure S1
    Public Sub New(x As Integer)
    End Sub
    Shared Sub Main()
    End Sub
End Structure
    </file>
</compilation>).
            VerifyIL("S1..ctor(Integer)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ExplicitStructParameterlessConstructorCallWithMe()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure S1
    Public Sub New(x As Integer)
        Me.New()
    End Sub
    Shared Sub Main()
    End Sub
End Structure
    </file>
</compilation>).
            VerifyIL("S1..ctor(Integer)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(7926, "DevDiv_Projects/Roslyn")>
        <WorkItem(541123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541123")>
        <Fact>
        Public Sub StructNonDefaultConstructorInitializer()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Structure S
    Public Sub New(x As Integer)
    End Sub

    Public Sub New(x As Integer, y as String)
        Me.New(x)
    End Sub
End Structure
    </file>
</compilation>).
            VerifyIL("S..ctor(Integer, String)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Sub S..ctor(Integer)"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicConstructor_Fields()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public Sub New(i As Integer)
    End Sub
End Structure

Class C
    Dim Fld As S
    Dim FldArr(4) As S

    Sub M()
        Me.Fld = New S(1)
        Me.FldArr(1) = New S(1)

        Try
            Me.Fld = New S(1)
            Me.FldArr(1) = New S(1)
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(vbSource).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  3
  .locals init (System.Exception V_0) //ex
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     "Sub S..ctor(Integer)"
  IL_0007:  stfld      "C.Fld As S"
  IL_000c:  ldarg.0
  IL_000d:  ldfld      "C.FldArr As S()"
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.1
  IL_0014:  newobj     "Sub S..ctor(Integer)"
  IL_0019:  stelem     "S"
  .try
{
  IL_001e:  ldarg.0
  IL_001f:  ldc.i4.1
  IL_0020:  newobj     "Sub S..ctor(Integer)"
  IL_0025:  stfld      "C.Fld As S"
  IL_002a:  ldarg.0
  IL_002b:  ldfld      "C.FldArr As S()"
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.1
  IL_0032:  newobj     "Sub S..ctor(Integer)"
  IL_0037:  stelem     "S"
  IL_003c:  leave.s    IL_004c
}
  catch System.Exception
{
  IL_003e:  dup
  IL_003f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0044:  stloc.0
  IL_0045:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_004a:  leave.s    IL_004c
}
  IL_004c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicConstructor_Locals()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public Sub New(i As Integer)
    End Sub
End Structure

Class C
    Sub M()
        Dim loc As S
        Dim locArr(4) As S

        loc = New S(1)
        locArr(1) = New S(1)

        Try
            loc = New S(1)
            locArr(1) = New S(1)
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(vbSource).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       64 (0x40)
  .maxstack  3
  .locals init (S() V_0, //locArr
  System.Exception V_1) //ex
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "S"
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newobj     "Sub S..ctor(Integer)"
  IL_000d:  pop
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  ldc.i4.1
  IL_0011:  newobj     "Sub S..ctor(Integer)"
  IL_0016:  stelem     "S"
  .try
{
  IL_001b:  ldc.i4.1
  IL_001c:  newobj     "Sub S..ctor(Integer)"
  IL_0021:  pop
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.1
  IL_0025:  newobj     "Sub S..ctor(Integer)"
  IL_002a:  stelem     "S"
  IL_002f:  leave.s    IL_003f
}
  catch System.Exception
{
  IL_0031:  dup
  IL_0032:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0037:  stloc.1
  IL_0038:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_003d:  leave.s    IL_003f
}
  IL_003f:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicConstructor_LocalInLoop()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public x As Integer
    Public y As Integer
    Public Sub New(_x As Integer)
        Me.x = _x
        Throw New System.Exception()
    End Sub
    Public Sub New(_x As Integer, _y As Integer)
        Me.x = _x
        Me.y = _y
    End Sub
End Structure

Class C
    Shared Sub Main()
        Dim i As Integer = 0
        While i &lt; 2
            Try
                Dim a As S
                Console.WriteLine("x={0}, y={1}", a.x, a.y)
                a = New S(i+1, 2)
                a = New S(10)
            Catch ex As Exception
            End Try
            i = i + 1
        End While
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
x=0, y=0
x=1, y=2
]]>).
VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (Integer V_0, //i
  S V_1, //a
  System.Exception V_2) //ex
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_004b
  IL_0004:  nop
  .try
{
  IL_0005:  ldstr      "x={0}, y={1}"
  IL_000a:  ldloc.1
  IL_000b:  ldfld      "S.x As Integer"
  IL_0010:  box        "Integer"
  IL_0015:  ldloc.1
  IL_0016:  ldfld      "S.y As Integer"
  IL_001b:  box        "Integer"
  IL_0020:  call       "Sub System.Console.WriteLine(String, Object, Object)"
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  add.ovf
  IL_0028:  ldc.i4.2
  IL_0029:  newobj     "Sub S..ctor(Integer, Integer)"
  IL_002e:  stloc.1
  IL_002f:  ldc.i4.s   10
  IL_0031:  newobj     "Sub S..ctor(Integer)"
  IL_0036:  stloc.1
  IL_0037:  leave.s    IL_0047
}
  catch System.Exception
{
  IL_0039:  dup
  IL_003a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_003f:  stloc.2
  IL_0040:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0045:  leave.s    IL_0047
}
  IL_0047:  ldloc.0
  IL_0048:  ldc.i4.1
  IL_0049:  add.ovf
  IL_004a:  stloc.0
  IL_004b:  ldloc.0
  IL_004c:  ldc.i4.2
  IL_004d:  blt.s      IL_0004
  IL_004f:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicConstructor_LocalDeclaration()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public Sub New(i As Integer)
    End Sub
End Structure

Class C
    Sub M()
        Try
            Dim loc1 As New S(1) ' cannot escape because cannot be used before declaration
            Dim loc2 As S = New S(1) ' cannot escape because cannot be used before declaration
            loc1 = New S(1) ' can escape
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(vbSource).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  .try
  {
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     "Sub S..ctor(Integer)"
    IL_0006:  pop
    IL_0007:  ldc.i4.1
    IL_0008:  newobj     "Sub S..ctor(Integer)"
    IL_000d:  pop
    IL_000e:  ldc.i4.1
    IL_000f:  newobj     "Sub S..ctor(Integer)"
    IL_0014:  pop
    IL_0015:  leave.s    IL_0025
  }
  catch System.Exception
  {
    IL_0017:  dup
    IL_0018:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001d:  stloc.0
    IL_001e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0023:  leave.s    IL_0025
  }
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicConstructor_LocalDeclaration_SideEffects()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public Sub New(ByRef i As Integer)
    End Sub
End Structure

Class C
    Property AutoProp As Integer

    Sub M()
        Dim loc0 As New S(AutoProp) ' cannot escape
        loc0 = New S(AutoProp) ' cannot escape
        Try
            Dim loc1 As New S(AutoProp) ' cannot escape because cannot be used before declaration
            loc1 = New S(AutoProp) ' can escape
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            ' NOTE: Current implementation does not support optimization for constructor calls with side effects
            CompileAndVerify(vbSource).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size      105 (0x69)
  .maxstack  3
  .locals init (Integer V_0,
  System.Exception V_1) //ex
  IL_0000:  ldarg.0
  IL_0001:  call       "Function C.get_AutoProp() As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  newobj     "Sub S..ctor(ByRef Integer)"
  IL_000e:  ldarg.0
  IL_000f:  ldloc.0
  IL_0010:  call       "Sub C.set_AutoProp(Integer)"
  IL_0015:  pop
  IL_0016:  ldarg.0
  IL_0017:  call       "Function C.get_AutoProp() As Integer"
  IL_001c:  stloc.0
  IL_001d:  ldloca.s   V_0
  IL_001f:  newobj     "Sub S..ctor(ByRef Integer)"
  IL_0024:  ldarg.0
  IL_0025:  ldloc.0
  IL_0026:  call       "Sub C.set_AutoProp(Integer)"
  IL_002b:  pop
  .try
{
  IL_002c:  ldarg.0
  IL_002d:  call       "Function C.get_AutoProp() As Integer"
  IL_0032:  stloc.0
  IL_0033:  ldloca.s   V_0
  IL_0035:  newobj     "Sub S..ctor(ByRef Integer)"
  IL_003a:  ldarg.0
  IL_003b:  ldloc.0
  IL_003c:  call       "Sub C.set_AutoProp(Integer)"
  IL_0041:  pop
  IL_0042:  ldarg.0
  IL_0043:  call       "Function C.get_AutoProp() As Integer"
  IL_0048:  stloc.0
  IL_0049:  ldloca.s   V_0
  IL_004b:  newobj     "Sub S..ctor(ByRef Integer)"
  IL_0050:  ldarg.0
  IL_0051:  ldloc.0
  IL_0052:  call       "Sub C.set_AutoProp(Integer)"
  IL_0057:  pop
  IL_0058:  leave.s    IL_0068
}
  catch System.Exception
{
  IL_005a:  dup
  IL_005b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0060:  stloc.1
  IL_0061:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0066:  leave.s    IL_0068
}
  IL_0068:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicConstructor_Params()
            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System

Structure S
    Public Sub New(i As Integer)
    End Sub
End Structure

Class C
    Sub M(paramByVal As S, ByRef paramByRef As S)
        paramByVal = New S(1)
        paramByRef = New S(1)
        Try
            paramByVal = New S(1)
            paramByRef = New S(1)
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(vbSource).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  IL_0000:  ldarga.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub S..ctor(Integer)"
  IL_0008:  ldarg.2
  IL_0009:  ldc.i4.1
  IL_000a:  newobj     "Sub S..ctor(Integer)"
  IL_000f:  stobj      "S"
  .try
{
  IL_0014:  ldc.i4.1
  IL_0015:  newobj     "Sub S..ctor(Integer)"
  IL_001a:  starg.s    V_1
  IL_001c:  ldarg.2
  IL_001d:  ldc.i4.1
  IL_001e:  newobj     "Sub S..ctor(Integer)"
  IL_0023:  stobj      "S"
  IL_0028:  leave.s    IL_0038
}
  catch System.Exception
{
  IL_002a:  dup
  IL_002b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0030:  stloc.0
  IL_0031:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0036:  leave.s    IL_0038
}
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicParameterlessConstructor_Fields()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Dim Fld As S
    Dim FldArr(4) As S

    Sub M()
        Me.Fld = New S()
        Me.FldArr(1) = New S()

        Try
            Me.Fld = New S()
            Me.FldArr(1) = New S()
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (System.Exception V_0) //ex
  IL_0000:  ldarg.0
  IL_0001:  newobj     "Sub S..ctor()"
  IL_0006:  stfld      "C.Fld As S"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "C.FldArr As S()"
  IL_0011:  ldc.i4.1
  IL_0012:  newobj     "Sub S..ctor()"
  IL_0017:  stelem     "S"
  .try
{
  IL_001c:  ldarg.0
  IL_001d:  newobj     "Sub S..ctor()"
  IL_0022:  stfld      "C.Fld As S"
  IL_0027:  ldarg.0
  IL_0028:  ldfld      "C.FldArr As S()"
  IL_002d:  ldc.i4.1
  IL_002e:  newobj     "Sub S..ctor()"
  IL_0033:  stelem     "S"
  IL_0038:  leave.s    IL_0048
}
  catch System.Exception
{
  IL_003a:  dup
  IL_003b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0040:  stloc.0
  IL_0041:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0046:  leave.s    IL_0048
}
  IL_0048:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicParameterlessConstructor_Locals()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()
        Dim loc As S
        Dim locArr(4) As S

        loc = New S()
        locArr(1) = New S()

        Try
            loc = New S()
            locArr(1) = New S()
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (S() V_0, //locArr
  System.Exception V_1) //ex
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "S"
  IL_0006:  stloc.0
  IL_0007:  newobj     "Sub S..ctor()"
  IL_000c:  pop
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.1
  IL_000f:  newobj     "Sub S..ctor()"
  IL_0014:  stelem     "S"
  .try
{
  IL_0019:  newobj     "Sub S..ctor()"
  IL_001e:  pop
  IL_001f:  ldloc.0
  IL_0020:  ldc.i4.1
  IL_0021:  newobj     "Sub S..ctor()"
  IL_0026:  stelem     "S"
  IL_002b:  leave.s    IL_003b
}
  catch System.Exception
{
  IL_002d:  dup
  IL_002e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0033:  stloc.1
  IL_0034:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0039:  leave.s    IL_003b
}
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_PublicParameterlessConstructor_Params()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M(paramByVal As S, ByRef paramByRef As S)
        paramByVal = New S()
        paramByRef = New S()
        Try
            paramByVal = New S()
            paramByRef = New S()
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            ' TODO (tomat): verification fails
            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  IL_0000:  ldarga.s   V_1
  IL_0002:  call       "Sub S..ctor()"
  IL_0007:  ldarg.2
  IL_0008:  newobj     "Sub S..ctor()"
  IL_000d:  stobj      "S"
  .try
{
  IL_0012:  newobj     "Sub S..ctor()"
  IL_0017:  starg.s    V_1
  IL_0019:  ldarg.2
  IL_001a:  newobj     "Sub S..ctor()"
  IL_001f:  stobj      "S"
  IL_0024:  leave.s    IL_0034
}
  catch System.Exception
{
  IL_0026:  dup
  IL_0027:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_002c:  stloc.0
  IL_002d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0032:  leave.s    IL_0034
}
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_NoParameterlessConstructor_Fields()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Dim Fld As S
    Dim FldArr(4) As S

    Sub M()
        Me.Fld = New S()
        Me.FldArr(1) = New S()

        Try
            Me.Fld = New S()
            Me.FldArr(1) = New S()
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C.Fld As S"
  IL_0006:  initobj    "S"
  IL_000c:  ldarg.0
  IL_000d:  ldfld      "C.FldArr As S()"
  IL_0012:  ldc.i4.1
  IL_0013:  ldelema    "S"
  IL_0018:  initobj    "S"
  .try
  {
    IL_001e:  ldarg.0
    IL_001f:  ldflda     "C.Fld As S"
    IL_0024:  initobj    "S"
    IL_002a:  ldarg.0
    IL_002b:  ldfld      "C.FldArr As S()"
    IL_0030:  ldc.i4.1
    IL_0031:  ldelema    "S"
    IL_0036:  initobj    "S"
    IL_003c:  leave.s    IL_004c
  }
  catch System.Exception
  {
    IL_003e:  dup
    IL_003f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0044:  stloc.0
    IL_0045:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_004a:  leave.s    IL_004c
  }
  IL_004c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_NoParameterlessConstructor_Locals()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()
        Dim loc As S
        Dim locArr(4) As S

        loc = New S()
        locArr(1) = New S()

        Try
            loc = New S()
            locArr(1) = New S()
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (S() V_0, //locArr
  System.Exception V_1) //ex
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "S"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  ldelema    "S"
  IL_000e:  initobj    "S"
  .try
{
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  ldelema    "S"
  IL_001b:  initobj    "S"
  IL_0021:  leave.s    IL_0031
}
  catch System.Exception
{
  IL_0023:  dup
  IL_0024:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0029:  stloc.1
  IL_002a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_002f:  leave.s    IL_0031
}
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PartiallyInitializedValue_NoParameterlessConstructor_Params()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M(paramByVal As S, ByRef paramByRef As S)
        paramByVal = New S()
        paramByRef = New S()
        Try
            paramByVal = New S()
            paramByRef = New S()
        Catch ex As Exception
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.M",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  IL_0000:  ldarga.s   V_1
  IL_0002:  initobj    "S"
  IL_0008:  ldarg.2
  IL_0009:  initobj    "S"
  .try
  {
    IL_000f:  ldarga.s   V_1
    IL_0011:  initobj    "S"
    IL_0017:  ldarg.2
    IL_0018:  initobj    "S"
    IL_001e:  leave.s    IL_002e
  }
  catch System.Exception
  {
    IL_0020:  dup
    IL_0021:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0026:  stloc.0
    IL_0027:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_002c:  leave.s    IL_002e
  }
  IL_002e:  ret
}
]]>)
        End Sub

        <WorkItem(541123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541123")>
        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub PublicParameterlessConstructorInMetadata_Public()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim s As S = New S()
        s = Nothing
        s = New S()
        SS(Nothing)
        SS(New S())
        Dim a = (New S()).ToString()
        s = DirectCast(Nothing, S)
    End Sub
    Shared Sub SS(s As S)
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       64 (0x40)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  newobj     "Sub S..ctor()"
  IL_0005:  pop
  IL_0006:  newobj     "Sub S..ctor()"
  IL_000b:  pop
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    "S"
  IL_0014:  ldloc.0
  IL_0015:  call       "Sub C.SS(S)"
  IL_001a:  newobj     "Sub S..ctor()"
  IL_001f:  call       "Sub C.SS(S)"
  IL_0024:  newobj     "Sub S..ctor()"
  IL_0029:  stloc.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  constrained. "S"
  IL_0032:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0037:  pop
  IL_0038:  ldnull
  IL_0039:  unbox.any  "S"
  IL_003e:  pop
  IL_003f:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub PublicParameterlessConstructorInMetadata_Protected()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method family hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim s As S = New S()
        s = Nothing
        s = New S()
        SS(Nothing)
        SS(New S())
        Dim a = (New S()).ToString()
        s = DirectCast(Nothing, S)
    End Sub
    Shared Sub SS(s As S)
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub C.SS(S)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  initobj    "S"
  IL_0016:  ldloc.0
  IL_0017:  call       "Sub C.SS(S)"
  IL_001c:  ldloca.s   V_0
  IL_001e:  initobj    "S"
  IL_0024:  ldloc.0
  IL_0025:  stloc.0
  IL_0026:  ldloca.s   V_0
  IL_0028:  constrained. "S"
  IL_002e:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0033:  pop
  IL_0034:  ldnull
  IL_0035:  unbox.any  "S"
  IL_003a:  pop
  IL_003b:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub PublicParameterlessConstructorInMetadata_Private()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method private hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim s As S = New S()
        s = Nothing
        s = New S()
        SS(Nothing)
        SS(New S())
        Dim a = (New S()).ToString()
        s = DirectCast(Nothing, S)
    End Sub
    Shared Sub SS(s As S)
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub C.SS(S)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  initobj    "S"
  IL_0016:  ldloc.0
  IL_0017:  call       "Sub C.SS(S)"
  IL_001c:  ldloca.s   V_0
  IL_001e:  initobj    "S"
  IL_0024:  ldloc.0
  IL_0025:  stloc.0
  IL_0026:  ldloca.s   V_0
  IL_0028:  constrained. "S"
  IL_002e:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0033:  pop
  IL_0034:  ldnull
  IL_0035:  unbox.any  "S"
  IL_003a:  pop
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PublicParameterlessConstructorInMetadata_Private_D()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method private hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim s As S = New S()
        s = Nothing
        s = New S()
        SS(Nothing)
        SS(New S())
        Dim a = (New S()).ToString()
        s = DirectCast(Nothing, S)
    End Sub
    Shared Sub SS(s As S)
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDebugDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       84 (0x54)
  .maxstack  1
  .locals init (S V_0, //s
                Object V_1, //a
                S V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloca.s   V_0
  IL_000a:  initobj    "S"
  IL_0010:  ldloca.s   V_0
  IL_0012:  initobj    "S"
  IL_0018:  ldloca.s   V_2
  IL_001a:  initobj    "S"
  IL_0020:  ldloc.2
  IL_0021:  call       "Sub C.SS(S)"
  IL_0026:  ldloca.s   V_2
  IL_0028:  initobj    "S"
  IL_002e:  ldloc.2
  IL_002f:  call       "Sub C.SS(S)"
  IL_0034:  ldloca.s   V_2
  IL_0036:  initobj    "S"
  IL_003c:  ldloc.2
  IL_003d:  stloc.2
  IL_003e:  ldloca.s   V_2
  IL_0040:  constrained. "S"
  IL_0046:  callvirt   "Function System.ValueType.ToString() As String"
  IL_004b:  stloc.1
  IL_004c:  ldnull
  IL_004d:  unbox.any  "S"
  IL_0052:  stloc.0
  IL_0053:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub PublicParameterlessConstructorInMetadata_OptionalParameter()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method private hidebysig specialname rtspecialname
        instance void  .ctor([opt] int32 a) cil managed
  {
    .param [2] = int32(0x0000007B)
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim s As S = New S()
        s = Nothing
        s = New S()
        SS(Nothing)
        SS(New S())
        Dim a = (New S()).ToString()
        s = DirectCast(Nothing, S)
    End Sub
    Shared Sub SS(s As S)
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub C.SS(S)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  initobj    "S"
  IL_0016:  ldloc.0
  IL_0017:  call       "Sub C.SS(S)"
  IL_001c:  ldloca.s   V_0
  IL_001e:  initobj    "S"
  IL_0024:  ldloc.0
  IL_0025:  stloc.0
  IL_0026:  ldloca.s   V_0
  IL_0028:  constrained. "S"
  IL_002e:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0033:  pop
  IL_0034:  ldnull
  IL_0035:  unbox.any  "S"
  IL_003a:  pop
  IL_003b:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub SimpleStructInstantiationAndAssigningNothing()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim s As S = New S()
        s = Nothing
        s = New S()
        SS(Nothing)
        SS(New S())
        Dim a = (New S()).ToString()
        s = DirectCast(Nothing, S)
    End Sub
    Shared Sub SS(s As S)
    End Sub
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "S"
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub C.SS(S)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  initobj    "S"
  IL_0016:  ldloc.0
  IL_0017:  call       "Sub C.SS(S)"
  IL_001c:  ldloca.s   V_0
  IL_001e:  initobj    "S"
  IL_0024:  ldloc.0
  IL_0025:  stloc.0
  IL_0026:  ldloca.s   V_0
  IL_0028:  constrained. "S"
  IL_002e:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0033:  pop
  IL_0034:  ldnull
  IL_0035:  unbox.any  "S"
  IL_003a:  pop
  IL_003b:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub TypeParameterInitializationWithNothing()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class CT(Of X)
    Public Sub T()
        Dim a As X = Nothing
    End Sub
End Class
Module EmitTest
    Sub Main()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="").
            VerifyIL("CT(Of X).T()",
            <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact()>
        Public Sub TypeParameterInitializationWithNothing_StructConstraint()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class CT(Of X As Structure)
    Public Sub T()
        Dim a As X = New X()
        a = Nothing
        a = New X()
    End Sub
End Class
Module EmitTest
    Sub Main()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="").
            VerifyIL("CT(Of X).T()",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  call       "Function System.Activator.CreateInstance(Of X)() As X"
  IL_0005:  pop
  IL_0006:  call       "Function System.Activator.CreateInstance(Of X)() As X"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact()>
        Public Sub TypeParameterInitializationWithNothing_NewConstraint()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class CT(Of X As New)
    Public Sub T()
        Dim a As X = New X()
        a = Nothing
        a = New X()
    End Sub
End Class
Module EmitTest
    Sub Main()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="").
            VerifyIL("CT(Of X).T()",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  call       "Function System.Activator.CreateInstance(Of X)() As X"
  IL_0005:  pop
  IL_0006:  call       "Function System.Activator.CreateInstance(Of X)() As X"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)
        End Sub

        <WorkItem(541308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541308")>
        <Fact>
        Public Sub StructInstantiationWithParameters()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Structure S
    Public Sub New(i As Integer)
    End Sub
End Structure
Module EmitTest
    Public Sub Main()
        Dim s As S = New S(1)
        s = New S(1)
        Dim a = (New S(1)).ToString()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     "Sub S..ctor(Integer)"
  IL_0006:  pop
  IL_0007:  ldc.i4.1
  IL_0008:  newobj     "Sub S..ctor(Integer)"
  IL_000d:  pop
  IL_000e:  ldc.i4.1
  IL_000f:  newobj     "Sub S..ctor(Integer)"
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  constrained. "S"
  IL_001d:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0022:  pop
  IL_0023:  ret
}
]]>)
        End Sub

        <WorkItem(541123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541123")>
        <WorkItem(541309, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541309")>
        <Fact>
        Public Sub PrivateParameterlessConstructorInMetadata()
            Dim ilSource = <![CDATA[
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method private hidebysig specialname rtspecialname
        instance void  .ctor() cil managed
  {
    ret
  }
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim s as S = New S()
        s = Nothing
    End Sub
End Class
    </file>
</compilation>

            ' CONSIDER: This is the dev10 behavior.
            ' Shouldn't there be an error for trying to call an inaccessible ctor?
            ' NOTE: Current behavior is to skip private constructor and use 'initobj'
            CompileWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll).
                VerifyIL("C.Main",
            <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestAsNewWithMultipleLocals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module EmitTest

    Class C
        private shared _sharedState as integer = 0
        public _p as integer = 0

        Public Sub New()
            _sharedState = _sharedState + 1
            _p = _sharedState
        End Sub

        Public ReadOnly Property P as integer
            Get
                return _p
            End Get
        End Property
    End Class

    Sub Main()
        Dim x, y as New C()
        Console.WriteLine(x.P)
        Console.WriteLine(y.P)
    End Sub

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
1
2
]]>).
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (EmitTest.C V_0) //x
  IL_0000:  newobj     "Sub EmitTest.C..ctor()"
  IL_0005:  stloc.0
  IL_0006:  newobj     "Sub EmitTest.C..ctor()"
  IL_000b:  ldloc.0
  IL_000c:  callvirt   "Function EmitTest.C.get_P() As Integer"
  IL_0011:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0016:  callvirt   "Function EmitTest.C.get_P() As Integer"
  IL_001b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0020:  ret
}
]]>)
        End Sub

        <WorkItem(540533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540533")>
        <Fact()>
        Public Sub Bug6817()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub goo()
        apCompare(1.CompareTo(2))
        Exit Sub
    End Sub
    Sub Main()
        goo
    End Sub
End Module
Public Module M2
    Public Sub apCompare(ActualValue As Object)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyIL("M1.goo",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.2
  IL_0005:  call       "Function Integer.CompareTo(Integer) As Integer"
  IL_000a:  box        "Integer"
  IL_000f:  call       "Sub M2.apCompare(Object)"
  IL_0014:  ret
}
]]>)
        End Sub

        <WorkItem(8597, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub OpenGenericWithAliasQualifierInGetType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports OuterOfString = Outer(Of String)
Imports OuterOfInt = Outer(Of Integer)

Public Class Outer(Of T)
    Public Class Inner(Of U)
    End Class
End Class

Public Module Module1
    Public Sub Main()
        Console.WriteLine(GetType(OuterOfString.Inner(Of )) Is GetType(OuterOfInt.Inner(Of )))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="True")
        End Sub

        <Fact>
        Public Sub ArrayLongLength()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        Dim arr As Integer() = New Integer(4) {}
        System.Console.Write(arr.LongLength + 1)
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="6").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     "Integer"
  IL_0006:  ldlen
  IL_0007:  conv.i8
  IL_0008:  ldc.i4.1
  IL_0009:  conv.i8
  IL_000a:  add.ovf
  IL_000b:  call       "Sub System.Console.Write(Long)"
  IL_0010:  ret
}
]]>)
        End Sub

        <WorkItem(528679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528679")>
        <Fact()>
        Public Sub FunctionCallWhileOptionInferOn()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Const global_y As Long = 20
    Public Shared Sub Main()
    End Sub
    Function goo(ByRef x As Integer) As Integer
        x = x + 10
        Return x + 10
    End Function
    Sub goo1()
        Dim global_y As Integer = goo(global_y)
    End Sub
End Class
                </file>
            </compilation>,
            expectedOutput:="").
                        VerifyIL("MyClass1.goo1",
            <![CDATA[
{
    // Code size       10 (0xa)
    .maxstack  2
    .locals init (Integer V_0) //global_y
    IL_0000:  ldarg.0
    IL_0001:  ldloca.s   V_0
    IL_0003:  call       "Function MyClass1.goo(ByRef Integer) As Integer"
    IL_0008:  stloc.0
    IL_0009:  ret
}]]>)
        End Sub

        ' Verify that the metadata for an attribute with a serialized an enum type with generics and nested class is correctly written by compiler and read by reflection.
        <WorkItem(541278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541278")>
        <Fact>
        Public Sub EmittingAttributesWithGenericsAndNestedClasses()
            CompileAndVerify(
            <compilation>
                <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

<AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
Class A
    Inherits Attribute
    Public Sub New(x As Object)
        Y = x
    End Sub

    Public Property Y As Object
End Class

Class B(Of T1)
    Class D(Of T2)
        Enum ED
            AED = &HFEDCBA
        End Enum
    End Class
    Class C
        Enum EC
            AEC = 1
        End Enum
    End Class
    Enum EB
        AEB = &HABCDEF
    End Enum
End Class

<A(B(Of Integer).EB.AEB)>
<A(B(Of Integer).D(Of Byte).ED.AED)>
<A(B(Of Integer).C.EC.AEC)>
Class C
End Class

Module m1
    Sub Main()
        Dim c1 As New C()
        For Each a As A In c1.GetType().GetCustomAttributes(False)
            Console.WriteLine(a.Y)
        Next
    End Sub
End Module
]]></file>
            </compilation>,
            expectedOutput:=<![CDATA[
AEB
AED
AEC
]]>).
                        VerifyIL("m1.Main",
            <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (Object() V_0,
  Integer V_1)
  IL_0000:  newobj     "Sub C..ctor()"
  IL_0005:  callvirt   "Function Object.GetType() As System.Type"
  IL_000a:  ldc.i4.0
  IL_000b:  callvirt   "Function System.Reflection.MemberInfo.GetCustomAttributes(Boolean) As Object()"
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0030
  IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldelem.ref
  IL_0018:  castclass  "A"
  IL_001d:  callvirt   "Function A.get_Y() As Object"
  IL_0022:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0027:  call       "Sub System.Console.WriteLine(Object)"
  IL_002c:  ldloc.1
  IL_002d:  ldc.i4.1
  IL_002e:  add.ovf
  IL_002f:  stloc.1
  IL_0030:  ldloc.1
  IL_0031:  ldloc.0
  IL_0032:  ldlen
  IL_0033:  conv.i4
  IL_0034:  blt.s      IL_0015
  IL_0036:  ret
}
    ]]>)
        End Sub

        <Fact>
        Public Sub NewEnum001()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Imports System
Class A
    Enum E1
        AAA
    End Enum

    Shared Sub Main()
        Dim e as DayOfWeek = New DayOfWeek()
        Console.Write(e.ToString)
        Dim e1 as E1 = New E1()
        Console.Writeline(e1.ToString)
    End Sub
End Class


    </file>
            </compilation>,
            expectedOutput:="SundayAAA").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (System.DayOfWeek V_0, //e
  A.E1 V_1) //e1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.DayOfWeek"
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. "System.DayOfWeek"
  IL_0010:  callvirt   "Function System.Enum.ToString() As String"
  IL_0015:  call       "Sub System.Console.Write(String)"
  IL_001a:  ldloca.s   V_1
  IL_001c:  initobj    "A.E1"
  IL_0022:  ldloca.s   V_1
  IL_0024:  constrained. "A.E1"
  IL_002a:  callvirt   "Function System.Enum.ToString() As String"
  IL_002f:  call       "Sub System.Console.WriteLine(String)"
  IL_0034:  ret
}
]]>)
        End Sub

        <WorkItem(542593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542593")>
        <Fact>
        Public Sub InheritClassFromRetargetedAssemblyReference()
            Dim ref1 = New VisualBasicCompilationReference(CompilationUtils.CreateEmptyCompilationWithReferences(
                <compilation>
                    <file name="a.vb">
Imports System.Collections.Generic
Imports System.Collections

Public Class C1(Of T)
    Implements IEnumerable(Of T)

    Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return Nothing
    End Function

    Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class
                    </file>
                </compilation>, references:={MetadataReference.CreateFromImage(Net40.Resources.mscorlib.AsImmutableOrNull())}))

            Dim comp = CompilationUtils.CreateEmptyCompilationWithReferences(
                <compilation>
                    <file name="b.vb">
Imports System.Collections.Generic
Imports System.Collections

Public Class C2(Of U)
    Inherits C1(Of U)
    Implements IEnumerable(Of U)

    Public Function GetEnumerator2() As IEnumerator(Of U) Implements IEnumerable(Of U).GetEnumerator
        Return GetEnumerator1()
    End Function
End Class
                    </file>
                </compilation>, references:={MetadataReference.CreateFromImage(Net40.Resources.mscorlib.AsImmutableOrNull()), ref1})

            CompileAndVerify(comp)

            Dim classC1 = DirectCast(comp.GlobalNamespace.GetMembers("C1").First(), NamedTypeSymbol)
            Dim c1GetEnumerator = DirectCast(classC1.GetMembers("GetEnumerator").First(), MethodSymbol)
            Assert.IsType(Of Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting.RetargetingMethodSymbol)(c1GetEnumerator)

            Dim classC2 = DirectCast(comp.GlobalNamespace.GetMembers("C2").First(), NamedTypeSymbol)
            Dim c2GetEnumerator2 = DirectCast(classC2.GetMembers("GetEnumerator2").First(), MethodSymbol)

            Assert.Equal(c1GetEnumerator.ExplicitInterfaceImplementations(0).OriginalDefinition,
                         c2GetEnumerator2.ExplicitInterfaceImplementations(0).OriginalDefinition)
        End Sub

        <WorkItem(542593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542593")>
        <Fact>
        Public Sub InheritClassFromRetargetedAssemblyReferenceProperty()
            Dim ref1 = New VisualBasicCompilationReference(CompilationUtils.CreateEmptyCompilationWithReferences(
                <compilation>
                    <file name="a.vb">
Imports System.Collections.Generic
Imports System.Collections

Public Class C1
    Implements IEnumerator

    Public ReadOnly Property Current1 As Object Implements System.Collections.IEnumerator.Current
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext1() As Boolean Implements System.Collections.IEnumerator.MoveNext
        Return False
    End Function

    Public Sub Reset1() Implements System.Collections.IEnumerator.Reset
    End Sub
End Class
                    </file>
                </compilation>, references:={MetadataReference.CreateFromImage(Net40.Resources.mscorlib.AsImmutableOrNull())}))

            Dim comp = CompilationUtils.CreateEmptyCompilationWithReferences(
                <compilation>
                    <file name="b.vb">
Imports System.Collections.Generic
Imports System.Collections

Public Class C2
    Inherits C1
    Implements IEnumerator

    Public ReadOnly Property Current2 As Object Implements System.Collections.IEnumerator.Current
        Get
            Return Current1
        End Get
    End Property

    Public Function MoveNext2() As Boolean Implements System.Collections.IEnumerator.MoveNext
        Return MoveNext1()
    End Function

    Public Sub Reset2() Implements System.Collections.IEnumerator.Reset
        Reset1()
    End Sub
End Class
                    </file>
                </compilation>, references:={MetadataReference.CreateFromImage(Net40.Resources.mscorlib.AsImmutableOrNull()), ref1})

            Dim compilationVerifier = CompileAndVerify(comp)

            Dim classC1 = DirectCast(comp.GlobalNamespace.GetMembers("C1").First(), NamedTypeSymbol)
            Dim c1Current1 = DirectCast(classC1.GetMembers("Current1").First(), PropertySymbol)
            Assert.IsType(Of Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting.RetargetingPropertySymbol)(c1Current1)

            Dim classC2 = DirectCast(comp.GlobalNamespace.GetMembers("C2").First(), NamedTypeSymbol)
            Dim c2Current2 = DirectCast(classC2.GetMembers("Current2").First(), PropertySymbol)

            Assert.Equal(c1Current1.ExplicitInterfaceImplementations(0),
                         c2Current2.ExplicitInterfaceImplementations(0))
        End Sub

        <WorkItem(9850, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NullableConversion()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Module M
    Function F() As Object
        Dim o As Integer? = 0
        Return o
    End Function
End Module
    </file>
            </compilation>)
        End Sub

        <WorkItem(9852, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub NullableIsNothing()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Module M
    Function F1(o As Integer?) As Boolean
        Return o Is Nothing
    End Function
    Function F2(o As Integer?) As Boolean
        Return o IsNot Nothing
    End Function
End Module
    </file>
            </compilation>)
        End Sub

        <Fact>
        Public Sub MetadataTypeName()
            Dim source =
<compilation>
    <file name="a.vb">
Namespace Goo
    Class B
    End Class
End Namespace
Namespace GOO
    Class C
    End Class
End Namespace

Namespace GoO.Bar
    Partial Class D
    End Class
End Namespace

Namespace goo.Baz
    Class E
    End Class
End Namespace

Namespace goO
    Namespace Bar
        Class F
        End Class
    End Namespace
End Namespace

Namespace GOo.Bar
    Partial Class D
    End Class
End Namespace

Namespace Global.PROJEcT.Quuz
    Class A
    End Class
End Namespace

Namespace Global
    Namespace SYStem
        Class G
        End Class
    End Namespace
End Namespace
    </file>
</compilation>

            Dim comp = CompileAndVerify(source, options:=TestOptions.ReleaseDll.WithRootNamespace("Project"), validator:=
                Sub(a)
                    AssertEx.SetEqual({"<Module>",
                                       "PROJEcT.Quuz.A",
                                       "Project.GOO.C",
                                       "Project.GoO.Bar.D",
                                       "Project.Goo.B",
                                       "Project.goO.Bar.F",
                                       "Project.goo.Baz.E",
                                       "SYStem.G"}, MetadataValidation.GetFullTypeNames(a.GetMetadataReader()))
                End Sub)
        End Sub

        <Fact, WorkItem(542974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542974")>
        Public Sub LogicalOrWithBinaryExpressionOperands()
            Dim comp = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim a = "1"
        Dim b = 0.0
        If a &lt;&gt; "1" Or b > 0.1 Then
            System.Console.WriteLine("Fail")
        Else
            System.Console.WriteLine("Pass")
        End If
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.DebugDll)

            comp.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  3
  .locals init (String V_0, //a
                Double V_1, //b
                Boolean V_2)
  IL_0000:  nop
  IL_0001:  ldstr      "1"
  IL_0006:  stloc.0
  IL_0007:  ldc.r8     0
  IL_0010:  stloc.1
  IL_0011:  ldloc.0
  IL_0012:  ldstr      "1"
  IL_0017:  ldc.i4.0
  IL_0018:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_001d:  ldc.i4.0
  IL_001e:  cgt.un
  IL_0020:  ldloc.1
  IL_0021:  ldc.r8     0.1
  IL_002a:  cgt
  IL_002c:  or
  IL_002d:  stloc.2
  IL_002e:  ldloc.2
  IL_002f:  brfalse.s  IL_003f
  IL_0031:  ldstr      "Fail"
  IL_0036:  call       "Sub System.Console.WriteLine(String)"
  IL_003b:  nop
  IL_003c:  nop
  IL_003d:  br.s       IL_004c
  IL_003f:  nop
  IL_0040:  ldstr      "Pass"
  IL_0045:  call       "Sub System.Console.WriteLine(String)"
  IL_004a:  nop
  IL_004b:  nop
  IL_004c:  ret
}
]]>)
        End Sub

        <WorkItem(543243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543243")>
        <Fact()>
        Public Sub TestAutoProperty()
            Dim vbCompilation = CreateVisualBasicCompilation("TestAutoProperty",
            <![CDATA[Public Module Program
    Property P As Integer
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim vbVerifier = CompileAndVerify(vbCompilation, expectedSignatures:=
            {
                Signature("Program", "P", ".property readwrite static System.Int32 P"),
                Signature("Program", "get_P", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public specialname static System.Int32 get_P() cil managed"),
                Signature("Program", "set_P", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public specialname static System.Void set_P(System.Int32 AutoPropertyValue) cil managed")
            })

            vbVerifier.VerifyDiagnostics()
        End Sub

        <WorkItem(543243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543243")>
        <Fact()>
        Public Sub TestOrInDebug()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim a = "1"
        Dim b = 0.0
        If a &lt; &gt; "1" Or b &gt; 0.1 Then
            System.Console.WriteLine("Fail")
        Else
            System.Console.WriteLine("Pass")
        End If
    End Sub
End Module
    </file>
</compilation>,
            options:=TestOptions.DebugExe,
            expectedOutput:="Pass")

            c.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  3
  .locals init (String V_0, //a
                Double V_1, //b
                Boolean V_2)
  IL_0000:  nop
  IL_0001:  ldstr      "1"
  IL_0006:  stloc.0
  IL_0007:  ldc.r8     0
  IL_0010:  stloc.1
  IL_0011:  ldloc.0
  IL_0012:  ldstr      "1"
  IL_0017:  ldc.i4.0
  IL_0018:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
  IL_001d:  ldc.i4.0
  IL_001e:  cgt.un
  IL_0020:  ldloc.1
  IL_0021:  ldc.r8     0.1
  IL_002a:  cgt
  IL_002c:  or
  IL_002d:  stloc.2
  IL_002e:  ldloc.2
  IL_002f:  brfalse.s  IL_003f
  IL_0031:  ldstr      "Fail"
  IL_0036:  call       "Sub System.Console.WriteLine(String)"
  IL_003b:  nop
  IL_003c:  nop
  IL_003d:  br.s       IL_004c
  IL_003f:  nop
  IL_0040:  ldstr      "Pass"
  IL_0045:  call       "Sub System.Console.WriteLine(String)"
  IL_004a:  nop
  IL_004b:  nop
  IL_004c:  ret
}
]]>)
        End Sub

        <WorkItem(539392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539392")>
        <Fact()>
        Public Sub DecimalBinaryOp_01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Console.WriteLine(Decimal.MaxValue + 0)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="79228162514264337593543950335").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  5
  IL_0000:  ldc.i4.m1
  IL_0001:  ldc.i4.m1
  IL_0002:  ldc.i4.m1
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  newobj     "Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)"
  IL_000a:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_000f:  ret
}
]]>)
        End Sub

        <WorkItem(543611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543611")>
        <Fact()>
        Public Sub CompareToOnDecimalLiteral()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Console.WriteLine(1D.CompareTo(2D))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="-1").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Decimal V_0)
  IL_0000:  ldsfld     "Decimal.One As Decimal"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.2
  IL_0009:  conv.i8
  IL_000a:  newobj     "Sub Decimal..ctor(Long)"
  IL_000f:  call       "Function Decimal.CompareTo(Decimal) As Integer"
  IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0019:  ret
}
]]>)
        End Sub

        <WorkItem(543611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543611")>
        <Fact()>
        Public Sub CallOnReadonlyValField()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Test

    Structure C1

        Public x As Decimal

    End Structure

    Shared Function Goo() As C1
        Return New C1()
    End Function

    Shared Sub Main()
        Console.Write(Goo().x.CompareTo(Decimal.One))
    End Sub

End Class
    </file>
</compilation>, expectedOutput:="-1").
            VerifyIL("Test.Main",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Test.C1 V_0)
  IL_0000:  call       "Function Test.Goo() As Test.C1"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldflda     "Test.C1.x As Decimal"
  IL_000d:  ldsfld     "Decimal.One As Decimal"
  IL_0012:  call       "Function Decimal.CompareTo(Decimal) As Integer"
  IL_0017:  call       "Sub System.Console.Write(Integer)"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CallOnReadonlyValFieldNested()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Program

    Public Shared Sub Main()
        Dim c = New cls1()
        c.y.mutate(123)
        c.y.n.n.mutate(456)
        Console.WriteLine(c.y.n.n.num)
    End Sub

End Class

Class cls1

    Public ReadOnly y As MyManagedStruct = New MyManagedStruct(42)

End Class

Structure MyManagedStruct

    Public Structure Nested

        Public n As Nested1

        Public Structure Nested1

            Public num As Integer

            Public Sub mutate(x As Integer)
                num = x
            End Sub

        End Structure

    End Structure

    Public n As Nested

    Public Sub mutate(x As Integer)
        n.n.num = x
    End Sub

    Public Sub New(x As Integer)
        n.n.num = x
    End Sub

End Structure


    </file>
</compilation>, expectedOutput:="42").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  3
  .locals init (MyManagedStruct V_0)
  IL_0000:  newobj     "Sub cls1..ctor()"
  IL_0005:  dup
  IL_0006:  ldfld      "cls1.y As MyManagedStruct"
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.s   123
  IL_0010:  call       "Sub MyManagedStruct.mutate(Integer)"
  IL_0015:  dup
  IL_0016:  ldfld      "cls1.y As MyManagedStruct"
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldflda     "MyManagedStruct.n As MyManagedStruct.Nested"
  IL_0023:  ldflda     "MyManagedStruct.Nested.n As MyManagedStruct.Nested.Nested1"
  IL_0028:  ldc.i4     0x1c8
  IL_002d:  call       "Sub MyManagedStruct.Nested.Nested1.mutate(Integer)"
  IL_0032:  ldfld      "cls1.y As MyManagedStruct"
  IL_0037:  ldfld      "MyManagedStruct.n As MyManagedStruct.Nested"
  IL_003c:  ldfld      "MyManagedStruct.Nested.n As MyManagedStruct.Nested.Nested1"
  IL_0041:  ldfld      "MyManagedStruct.Nested.Nested1.num As Integer"
  IL_0046:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004b:  ret
}
]]>)
        End Sub

        <WorkItem(543611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543611")>
        <Fact()>
        Public Sub MultipleconstsByRef()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main()

        ' Expecting to have exactly 3 decimal temps here
        ' more than 3 is redundant
        ' less than 3 is not possible

        moo(1D, 1D, 1D)
        moo(1D, 1D, 1D)
        moo(1D, 1D, 1D)
    End Sub

    Sub moo(ByRef x As Decimal, ByRef y As Decimal, ByRef z As Decimal)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (Decimal V_0,
  Decimal V_1,
  Decimal V_2)
  IL_0000:  ldsfld     "Decimal.One As Decimal"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldsfld     "Decimal.One As Decimal"
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldsfld     "Decimal.One As Decimal"
  IL_0015:  stloc.2
  IL_0016:  ldloca.s   V_2
  IL_0018:  call       "Sub Program.moo(ByRef Decimal, ByRef Decimal, ByRef Decimal)"
  IL_001d:  ldsfld     "Decimal.One As Decimal"
  IL_0022:  stloc.2
  IL_0023:  ldloca.s   V_2
  IL_0025:  ldsfld     "Decimal.One As Decimal"
  IL_002a:  stloc.1
  IL_002b:  ldloca.s   V_1
  IL_002d:  ldsfld     "Decimal.One As Decimal"
  IL_0032:  stloc.0
  IL_0033:  ldloca.s   V_0
  IL_0035:  call       "Sub Program.moo(ByRef Decimal, ByRef Decimal, ByRef Decimal)"
  IL_003a:  ldsfld     "Decimal.One As Decimal"
  IL_003f:  stloc.0
  IL_0040:  ldloca.s   V_0
  IL_0042:  ldsfld     "Decimal.One As Decimal"
  IL_0047:  stloc.1
  IL_0048:  ldloca.s   V_1
  IL_004a:  ldsfld     "Decimal.One As Decimal"
  IL_004f:  stloc.2
  IL_0050:  ldloca.s   V_2
  IL_0052:  call       "Sub Program.moo(ByRef Decimal, ByRef Decimal, ByRef Decimal)"
  IL_0057:  ret
}
]]>)
        End Sub

        <WorkItem(638119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638119")>
        <Fact()>
        Public Sub ArrayInitZero()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Test()
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Sub Test()
            ' no element inits
            Dim arrB1 = new boolean() {false, false, false}
            System.Console.WriteLine(arrB1(0))

            ' no element inits
            Dim arrE1 = new Exception() {Nothing, Nothing, Nothing}
            System.Console.WriteLine(arrE1(0))

            ' 1 element init
            Dim arrB2 = new boolean() {false, true, false}
            System.Console.WriteLine(arrB2(1))

            ' 1 element init
            Dim arrE2 = new Exception() {Nothing, new Exception(), Nothing, Nothing, Nothing, Nothing, Nothing}
            System.Console.WriteLine(arrE2(1))

            ' blob init
            Dim arrB3 = new boolean() {true, false, true, true}
            System.Console.WriteLine(arrB3(2))

    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput:=<![CDATA[False

True
System.Exception: Exception of type 'System.Exception' was thrown.
True
]]>).
            VerifyIL("Program.Test",
            <![CDATA[
{
  // Code size       89 (0x59)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Boolean"
  IL_0006:  ldc.i4.0
  IL_0007:  ldelem.u1
  IL_0008:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_000d:  ldc.i4.3
  IL_000e:  newarr     "System.Exception"
  IL_0013:  ldc.i4.0
  IL_0014:  ldelem.ref
  IL_0015:  call       "Sub System.Console.WriteLine(Object)"
  IL_001a:  ldc.i4.3
  IL_001b:  newarr     "Boolean"
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldc.i4.1
  IL_0023:  stelem.i1
  IL_0024:  ldc.i4.1
  IL_0025:  ldelem.u1
  IL_0026:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_002b:  ldc.i4.7
  IL_002c:  newarr     "System.Exception"
  IL_0031:  dup
  IL_0032:  ldc.i4.1
  IL_0033:  newobj     "Sub System.Exception..ctor()"
  IL_0038:  stelem.ref
  IL_0039:  ldc.i4.1
  IL_003a:  ldelem.ref
  IL_003b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0040:  ldc.i4.4
  IL_0041:  newarr     "Boolean"
  IL_0046:  dup
  IL_0047:  ldtoken    "Integer <PrivateImplementationDetails>.52A5C4A10657220CAC05C63ADFA923C7771C55D868A58EE360EB3D1511985C3E"
  IL_004c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0051:  ldc.i4.2
  IL_0052:  ldelem.u1
  IL_0053:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0058:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ArrayInitZero_D()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Test()
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Sub Test()
            ' no element inits
            Dim arrB1 = new boolean() {false, false, false}
            System.Console.WriteLine(arrB1(0))

            ' no element inits
            Dim arrE1 = new Exception() {Nothing, Nothing, Nothing}
            System.Console.WriteLine(arrE1(0))

            ' 1 element init
            Dim arrB2 = new boolean() {false, true, false}
            System.Console.WriteLine(arrB2(1))

            ' 1 element init
            Dim arrE2 = new Exception() {Nothing, new Exception(), Nothing, Nothing, Nothing, Nothing, Nothing}
            System.Console.WriteLine(arrE2(1))

            ' blob init
            Dim arrB3 = new boolean() {true, false, true, true}
            System.Console.WriteLine(arrB3(2))

    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseDebugExe.WithModuleName("MODULE"), expectedOutput:=<![CDATA[False

True
System.Exception: Exception of type 'System.Exception' was thrown.
True
]]>).
            VerifyIL("Program.Test",
            <![CDATA[
{
  // Code size      101 (0x65)
  .maxstack  4
  .locals init (Boolean() V_0, //arrB1
                System.Exception() V_1, //arrE1
                Boolean() V_2, //arrB2
                System.Exception() V_3, //arrE2
                Boolean() V_4) //arrB3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Boolean"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldelem.u1
  IL_000a:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_000f:  ldc.i4.3
  IL_0010:  newarr     "System.Exception"
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.0
  IL_0018:  ldelem.ref
  IL_0019:  call       "Sub System.Console.WriteLine(Object)"
  IL_001e:  ldc.i4.3
  IL_001f:  newarr     "Boolean"
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.1
  IL_0027:  stelem.i1
  IL_0028:  stloc.2
  IL_0029:  ldloc.2
  IL_002a:  ldc.i4.1
  IL_002b:  ldelem.u1
  IL_002c:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0031:  ldc.i4.7
  IL_0032:  newarr     "System.Exception"
  IL_0037:  dup
  IL_0038:  ldc.i4.1
  IL_0039:  newobj     "Sub System.Exception..ctor()"
  IL_003e:  stelem.ref
  IL_003f:  stloc.3
  IL_0040:  ldloc.3
  IL_0041:  ldc.i4.1
  IL_0042:  ldelem.ref
  IL_0043:  call       "Sub System.Console.WriteLine(Object)"
  IL_0048:  ldc.i4.4
  IL_0049:  newarr     "Boolean"
  IL_004e:  dup
  IL_004f:  ldtoken    "Integer <PrivateImplementationDetails>.52A5C4A10657220CAC05C63ADFA923C7771C55D868A58EE360EB3D1511985C3E"
  IL_0054:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0059:  stloc.s    V_4
  IL_005b:  ldloc.s    V_4
  IL_005d:  ldc.i4.2
  IL_005e:  ldelem.u1
  IL_005f:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0064:  ret
}
]]>)
        End Sub

        <WorkItem(529162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529162")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub TestMSVBTypeNameAPI()
            Dim vbCompilation = CreateVisualBasicCompilation("TestMSVBTypeNameAPI",
            <![CDATA[Public Module Program
    Sub Main()
        Dim x = 0UI
        System.Console.WriteLine(Microsoft.VisualBasic.Information.TypeName(x))
    End Sub
End Module]]>,
                compilationOptions:=TestOptions.ReleaseExe)

            Dim vbVerifier = CompileAndVerify(vbCompilation, expectedOutput:="UInteger")

            vbVerifier.VerifyDiagnostics()
            vbVerifier.VerifyIL("Program.Main", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  box        "UInteger"
  IL_0006:  call       "Function Microsoft.VisualBasic.CompilerServices.Versioned.TypeName(Object) As String"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
}]]>)
        End Sub

        <WorkItem(11640, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestDecimalConversionCDec01()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports Microsoft.VisualBasic
Imports System.Math

Class Test
    Public Shared Sub Main()
        If CDec(36%) <> 36@ Then
            System.Console.WriteLine("FAIL")
        Else
            System.Console.WriteLine("PASS")
        End If
    End Sub
End Class]]>
    </file>
</compilation>, expectedOutput:="PASS").
                VerifyIL("Test.Main", <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "PASS"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  ret
}
]]>)
        End Sub

        <WorkItem(543757, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543757")>
        <Fact()>
        Public Sub TestNotXor()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Dim f As Boolean = False
    Dim t As Boolean = True

    Sub Main(args As String())
        Dim q As Boolean = Not (f Xor t)
        Console.Write(q)

        q = Not(Not (f Xor t))
        Console.Write(q)

    End Sub
End Module
]]>
    </file>
</compilation>, expectedOutput:="FalseTrue").
                VerifyIL("Program.Main", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     "Program.f As Boolean"
  IL_0005:  ldsfld     "Program.t As Boolean"
  IL_000a:  ceq
  IL_000c:  call       "Sub System.Console.Write(Boolean)"
  IL_0011:  ldsfld     "Program.f As Boolean"
  IL_0016:  ldsfld     "Program.t As Boolean"
  IL_001b:  xor
  IL_001c:  call       "Sub System.Console.Write(Boolean)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub NotEqualIntegralAndFloat()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Class Class1
    Shared Sub Main()
        Dim i As Integer = -1
        Dim result As Boolean = i <> 0

        If (result) Then
            System.Console.Write("notequal1")
        End If

        result = i = 0
        If (result) Then
            System.Console.Write("equal1")
        End If

        Dim d As Double = -1
        result = d <> 0

        If (result) Then
            System.Console.Write("notequal2")
        End If

        result = d = 0
        If (result) Then
            System.Console.Write("equal2")
        End If
    End Sub
End Class
]]>
    </file>
</compilation>, expectedOutput:="notequal1notequal2").
                VerifyIL("Class1.Main", <![CDATA[
{
  // Code size       92 (0x5c)
  .maxstack  3
  IL_0000:  ldc.i4.m1
  IL_0001:  dup
  IL_0002:  ldc.i4.0
  IL_0003:  cgt.un
  IL_0005:  brfalse.s  IL_0011
  IL_0007:  ldstr      "notequal1"
  IL_000c:  call       "Sub System.Console.Write(String)"
  IL_0011:  ldc.i4.0
  IL_0012:  ceq
  IL_0014:  brfalse.s  IL_0020
  IL_0016:  ldstr      "equal1"
  IL_001b:  call       "Sub System.Console.Write(String)"
  IL_0020:  ldc.r8     -1
  IL_0029:  dup
  IL_002a:  ldc.r8     0
  IL_0033:  ceq
  IL_0035:  ldc.i4.0
  IL_0036:  ceq
  IL_0038:  brfalse.s  IL_0044
  IL_003a:  ldstr      "notequal2"
  IL_003f:  call       "Sub System.Console.Write(String)"
  IL_0044:  ldc.r8     0
  IL_004d:  ceq
  IL_004f:  brfalse.s  IL_005b
  IL_0051:  ldstr      "equal2"
  IL_0056:  call       "Sub System.Console.Write(String)"
  IL_005b:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(544128, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544128")>
        Public Sub CodeGenLambdaNarrowingRelaxation()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class HasAutoProps
    Property Scen11() As Func(Of String, Integer) = Function(y As Integer) y.ToString()
End Class
]]>
    </file>
</compilation>)
        End Sub

        <Fact(), WorkItem(544182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544182")>
        Public Sub OverrideImplementMembersWithOptArguments()
            Dim optParameterSource = <![CDATA[
.class interface public abstract auto ansi IAnimal
{
  .method public newslot abstract strict virtual
          instance void  MakeNoise([opt] int32 pitch) cil managed
  {
  } // end of method IAnimal::MakeNoise
  .method public newslot specialname abstract strict virtual
          instance class IAnimal  get_Descendants([opt] int32 generation) cil managed
  {
  } // end of method IAnimal::get_Descendants
  .property instance class IAnimal Descendants(int32)
  {
    .get instance class IAnimal IAnimal::get_Descendants(int32)
  } // end of property IAnimal::Descendants
} // end of class IAnimal

.class public abstract auto ansi AbstractAnimal
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method AbstractAnimal::.ctor
  .method public newslot abstract strict virtual
          instance void  MakeNoise([opt] int32 pitch) cil managed
  {
  } // end of method AbstractAnimal::MakeNoise
  .method public newslot specialname abstract strict virtual
          instance class AbstractAnimal  get_Descendants([opt] int32 generation) cil managed
  {
  } // end of method AbstractAnimal::get_Descendants
  .property instance class AbstractAnimal
          Descendants(int32)
  {
    .get instance class AbstractAnimal AbstractAnimal::get_Descendants(int32)
  } // end of property AbstractAnimal::Descendants
} // end of class AbstractAnimal
]]>.Value
            Dim vbSource =
<compilation>
    <file name="a.vb">
Class Bandicoot : Implements IAnimal
    Sub MakeNoise(Optional pitch As Integer = 20000) Implements IAnimal.MakeNoise
    End Sub
    ReadOnly Property Descendants(Optional generation As Integer = 0) As IAnimal Implements IAnimal.Descendants
        Get
            Return Nothing
        End Get
    End Property
End Class

Class Platypus : Inherits AbstractAnimal
    Public Overrides Sub MakeNoise(Optional pitch As Integer = 2500)
    End Sub
    Public Overrides ReadOnly Property Descendants(Optional generation As Integer = 6) As AbstractAnimal
        Get
            Return Nothing
        End Get
    End Property
End Class
    </file>
</compilation>

            CompileWithCustomILSource(vbSource, optParameterSource)
        End Sub

        <Fact()>
        Public Sub MyClassAssign()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub Main()
    End Sub
End Module

Structure S
    Private F As Object
    Sub M()
        MyClass.F = Nothing
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:="").
            VerifyIL("S.M",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "S.F As Object"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestHostProtectionAttributeWithoutSecurityAction()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Sub Main()
    End Sub
End Module

&lt;System.Security.Permissions.HostProtectionAttribute(UI := true)&gt;
Class C1
End Class
    </file>
</compilation>,
expectedOutput:="")

        End Sub

        <WorkItem(545201, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545201")>
        <Fact()>
        Public Sub TestConversionMultiDimArrayToIList()
            Dim vbCompilation = CreateVisualBasicCompilation("TestConversionMultiDimArrayToIList",
            <![CDATA[Imports System
Imports System.Collections
Module Program
    Sub Main(args As String())
        Test.Scen1(Of Exception, Integer).Scen1()
        Test.Scen1(Of Exception, Integer).Scen1({1, 2, 3, 4})
    End Sub
End Module

Module Test
    Class Scen1(Of T, U)
        Public Shared Sub Scen1()
            Try
                Dim a As T, b As T
                'T(,,)->IList
                Dim l4 As IList = {{{a, b}, {a, b}}}
                Console.WriteLine("PASS")
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
            End Try
        End Sub

        Public Shared Sub Scen1(d() as U)
            Try
               'U(,,)->IList
                Dim l4 As IList = {{{d(0), d(1)}, {d(2), d(3)}}}
                Console.WriteLine("{0}", l4.GetType())
                dim a4 as integer(,,) = l4
                for i = 0 to a4.GetLength(0) - 1
                    for j = 0 to a4.GetLength(1) - 1
                        for k = 0 to a4.GetLength(2) - 1
                            Console.WriteLine("{0}", a4(i, j, k))
                        next
                    next
                next
                Console.WriteLine("PASS")
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
            End Try
        End Sub
    End Class
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[
PASS
System.Int32[,,]
1
2
3
4
PASS
]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <WorkItem(545349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545349")>
        <Fact()>
        Public Sub CompoundPropGeneric()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Class Test
    Public Shared Sub Main()
        Dim s As New S1
        TestINop(s)
    End Sub

    Public Shared Sub TestINop(Of T As I)(a As T)
        ' should call Getter and Setter on same instance
        Nop(a).IntPropI += 1
    End Sub

    Public Shared Function Nop(Of T)(a As T) As T
        Return a
    End Function
End Class


Public Interface I
    Property IntPropI As Integer
End Interface

Public Structure S1
    Implements I

    Private x As Integer

    Public Property IntPropI As Integer Implements I.IntPropI
        Get
            x += 1
            Return x
        End Get
        Set(value As Integer)
            x += 1
            Console.WriteLine(x)
        End Set
    End Property
End Structure

    </file>
</compilation>,
expectedOutput:="2").
            VerifyIL("Test.TestINop(Of T)(T)",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (T V_0,
            T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Test.Nop(Of T)(T) As T"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloca.s   V_1
  IL_000b:  initobj    "T"
  IL_0011:  ldloc.1
  IL_0012:  box        "T"
  IL_0017:  brtrue.s   IL_0021
  IL_0019:  ldobj      "T"
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldloca.s   V_0
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function I.get_IntPropI() As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub I.set_IntPropI(Integer)"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub IsComImport()
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           Dim globalNamespace = [module].GlobalNamespace
                                                           Dim type = globalNamespace.GetMember(Of NamedTypeSymbol)("A")
                                                           Assert.True(type.IsComImport())
                                                           type = globalNamespace.GetMember(Of NamedTypeSymbol)("B")
                                                           Assert.True(type.IsComImport())
                                                           type = globalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                           Assert.False(type.IsComImport())
                                                       End Sub
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Runtime.InteropServices
<ComImport()>
<Guid("48C6BFCA-35EB-4537-8A13-DB15C86B61D4")>
Class A
End Class
<ComImport()>
Class B
    Public Sub New()
        MyBase.New()
    End Sub
End Class
Class C
End Class
    ]]></file>
</compilation>)
            CompileAndVerify(compilation, sourceSymbolValidator:=validator, symbolValidator:=validator, verify:=Verification.Passes)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub ComImportMethods()
            Dim sourceValidator As Action(Of ModuleSymbol) = Sub([module])
                                                                 Dim expectedMethodImplAttributes As System.Reflection.MethodImplAttributes = MethodImplAttributes.Managed Or
                                                                     MethodImplAttributes.Runtime Or MethodImplAttributes.InternalCall

                                                                 Dim globalNamespace = [module].GlobalNamespace
                                                                 Dim typeA = globalNamespace.GetMember(Of NamedTypeSymbol)("A")
                                                                 Assert.True(typeA.IsComImport())
                                                                 Assert.Equal(1, typeA.GetAttributes().Length)
                                                                 Dim ctorA = typeA.InstanceConstructors.First()
                                                                 Assert.Equal(expectedMethodImplAttributes, ctorA.ImplementationAttributes)
                                                                 Dim methodGoo = DirectCast(typeA.GetMembers("Goo").First(), MethodSymbol)
                                                                 Assert.Equal(expectedMethodImplAttributes, methodGoo.ImplementationAttributes)

                                                                 Dim typeB = globalNamespace.GetMember(Of NamedTypeSymbol)("B")
                                                                 Assert.True(typeB.IsComImport())
                                                                 Assert.Equal(1, typeB.GetAttributes().Length)
                                                                 Dim ctorB = typeB.InstanceConstructors.First()
                                                                 Assert.True(DirectCast(ctorB.GetCciAdapter(), Cci.IMethodDefinition).IsExternal)
                                                                 Assert.Equal(expectedMethodImplAttributes, ctorB.ImplementationAttributes)
                                                             End Sub

            Dim metadataValidator As Action(Of ModuleSymbol) = Sub([module])
                                                                   Dim globalNamespace = [module].GlobalNamespace

                                                                   ' ComImportAttribute: Pseudo custom attribute shouldn't have been emitted
                                                                   Dim typeA = globalNamespace.GetMember(Of NamedTypeSymbol)("A")
                                                                   Assert.True(typeA.IsComImport())
                                                                   Assert.Equal(0, typeA.GetAttributes().Length)

                                                                   Dim typeB = globalNamespace.GetMember(Of NamedTypeSymbol)("B")
                                                                   Assert.True(typeB.IsComImport())
                                                                   Assert.Equal(0, typeB.GetAttributes().Length)

                                                                   Dim ctorB As MethodSymbol = typeB.InstanceConstructors.First()
                                                                   Assert.True(ctorB.IsExternalMethod)
                                                               End Sub

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<ComImport()>
Class A
    Public Sub New()
        Console.WriteLine("FAIL")
    End Sub

    Public Shared Function Goo() As Integer
        Console.WriteLine("FAIL")
        Return 0
    End Function
End Class

<ComImport()>
Class B
End Class

Module M1
    Sub Main()
        Try
            Dim a = new A()
        Catch ex As Exception
            Console.Write("PASS1, ")
        End Try

        Try
            A.Goo()
        Catch ex As Exception
            Console.WriteLine("PASS2")
        End Try
    End Sub
End Module
    ]]></file>
</compilation>, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, sourceSymbolValidator:=sourceValidator, expectedOutput:="PASS1, PASS2")
        End Sub

        <Fact()>
        Public Sub ExitPropertyDefaultReturnValue()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class Program
    Property P As Integer
        Get
            Exit Property
        End Get
        Set(ByVal value As Integer)
            Exit Property
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            ' NOTE: warning, not error for missing return value.
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_DefAsgNoRetValPropVal1, "End Get").WithArguments("P"))

            Dim verifier = CompileAndVerify(compilation)
            verifier.VerifyIL("Program.get_P", <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0) //P
  IL_0000:  ldloc.0
  IL_0001:  ret
}
]]>)
            verifier.VerifyIL("Program.set_P", <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ExitPropertyGetterSetReturnValue()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class Program
    Property P As Integer
        Get
            P = 1
            Exit Property
        End Get
        Set(ByVal value As Integer)
            Exit Property
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            compilation.VerifyDiagnostics()

            Dim verifier = CompileAndVerify(compilation)
            verifier.VerifyIL("Program.get_P", <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ExitPropertySetterSetReturnValue()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Class Program
    Property P As Integer
        Get
            Exit Property
        End Get
        Set(ByVal value As Integer)
            P = 1
            Exit Property
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC42355: Property 'P' doesn't return a value on all code paths. Are you missing a 'Return' statement?
        End Get
        ~~~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P As Integer'.
            P = 1
            ~
</expected>)

            ' NOTE: call setter - doesn't set return value.
            Dim verifier = CompileAndVerify(compilation)
            verifier.VerifyIL("Program.set_P", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       "Sub Program.set_P(Integer)"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ExitPropertyOutput()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System

Class Program
    Shared Sub Main()
        Console.WriteLine(Prop2)
        Prop2 += 1
        Console.WriteLine(Prop2)
    End Sub

    Shared _prop2 As Integer = 1

    Shared Property Prop2 As Integer
        Get
            Console.WriteLine("In get")
            Prop2 = _prop2
            Exit Property
        End Get
        Set(ByVal value As Integer)
            Console.WriteLine("In set")
            _prop2 = value
            Exit Property
        End Set
    End Property
End Class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=<![CDATA[
In get
1
In get
In set
In get
2
]]>)
        End Sub

        <WorkItem(545716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545716")>
        <Fact()>
        Public Sub Regress14344()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class EdmFunction
    Private Shared Sub SetFunctionAttribute(ByRef field As Byte, attribute As byte)
        field += field And attribute
    End Sub
End Class
    </file>
</compilation>).
            VerifyIL("EdmFunction.SetFunctionAttribute",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldind.u1
  IL_0003:  ldarg.0
  IL_0004:  ldind.u1
  IL_0005:  ldarg.1
  IL_0006:  and
  IL_0007:  add
  IL_0008:  conv.ovf.u1.un
  IL_0009:  stind.i1
  IL_000a:  ret
}
]]>)
        End Sub

        <WorkItem(546189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546189")>
        <Fact()>
        Public Sub Regress15299()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Dim x As Long = -3

        Try
            Dim y As ULong = x
            Console.WriteLine(y)
        Catch ex As OverflowException
            System.Console.WriteLine("pass")
        End Try
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="pass").
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (Long V_0, //x
  System.OverflowException V_1) //ex
  IL_0000:  ldc.i4.s   -3
  IL_0002:  conv.i8
  IL_0003:  stloc.0
  .try
{
  IL_0004:  ldloc.0
  IL_0005:  conv.ovf.u8
  IL_0006:  call       "Sub System.Console.WriteLine(ULong)"
  IL_000b:  leave.s    IL_0025
}
  catch System.OverflowException
{
  IL_000d:  dup
  IL_000e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0013:  stloc.1
  IL_0014:  ldstr      "pass"
  IL_0019:  call       "Sub System.Console.WriteLine(String)"
  IL_001e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0023:  leave.s    IL_0025
}
  IL_0025:  ret
}
]]>)
        End Sub

        <WorkItem(546422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546422")>
        <Fact()>
        Public Sub LateBindingToSystemArrayIndex01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
option strict off

Imports System

Module Module1
    Sub Main()
        Console.WriteLine(getTypes().Length)
    End Sub

    Function getTypes() As Array
        Dim types As Array = {1, 2, 3, 4}
        Dim s(types.Length - 1) As Object
        Dim arr As Array
        arr = Array.CreateInstance(New Integer.GetType, 12)
        Dim i As Integer
        For i = 0 To types.Length - 1
            arr(i) = types.GetValue(i)
        Next
        Return arr
    End Function

End Module

    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput:="12").
            VerifyIL("Module1.getTypes",
            <![CDATA[
{
  // Code size      116 (0x74)
  .maxstack  6
  .locals init (System.Array V_0, //types
                System.Array V_1, //arr
                Integer V_2, //i
                Integer V_3)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_0018:  ldc.i4.1
  IL_0019:  sub.ovf
  IL_001a:  ldc.i4.1
  IL_001b:  add.ovf
  IL_001c:  newarr     "Object"
  IL_0021:  pop
  IL_0022:  ldloca.s   V_3
  IL_0024:  initobj    "Integer"
  IL_002a:  ldloc.3
  IL_002b:  box        "Integer"
  IL_0030:  call       "Function Object.GetType() As System.Type"
  IL_0035:  ldc.i4.s   12
  IL_0037:  call       "Function System.Array.CreateInstance(System.Type, Integer) As System.Array"
  IL_003c:  stloc.1
  IL_003d:  ldloc.0
  IL_003e:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_0043:  ldc.i4.1
  IL_0044:  sub.ovf
  IL_0045:  stloc.3
  IL_0046:  ldc.i4.0
  IL_0047:  stloc.2
  IL_0048:  br.s       IL_006e
  IL_004a:  ldloc.1
  IL_004b:  ldc.i4.2
  IL_004c:  newarr     "Object"
  IL_0051:  dup
  IL_0052:  ldc.i4.0
  IL_0053:  ldloc.2
  IL_0054:  box        "Integer"
  IL_0059:  stelem.ref
  IL_005a:  dup
  IL_005b:  ldc.i4.1
  IL_005c:  ldloc.0
  IL_005d:  ldloc.2
  IL_005e:  callvirt   "Function System.Array.GetValue(Integer) As Object"
  IL_0063:  stelem.ref
  IL_0064:  ldnull
  IL_0065:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexSet(Object, Object(), String())"
  IL_006a:  ldloc.2
  IL_006b:  ldc.i4.1
  IL_006c:  add.ovf
  IL_006d:  stloc.2
  IL_006e:  ldloc.2
  IL_006f:  ldloc.3
  IL_0070:  ble.s      IL_004a
  IL_0072:  ldloc.1
  IL_0073:  ret
}
]]>)
        End Sub

        <WorkItem(546422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546422")>
        <Fact()>
        Public Sub LateBindingToSystemArrayIndex01_D()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
option strict off

Imports System

Module Module1
    Sub Main()
        Console.WriteLine(getTypes().Length)
    End Sub

    Function getTypes() As Array
        Dim types As Array = {1, 2, 3, 4}
        Dim s(types.Length - 1) As Object
        Dim arr As Array
        arr = Array.CreateInstance(New Integer.GetType, 12)
        Dim i As Integer
        For i = 0 To types.Length - 1
            arr(i) = types.GetValue(i)
        Next
        Return arr
    End Function

End Module

    </file>
</compilation>, options:=TestOptions.ReleaseDebugExe.WithModuleName("MODULE"), expectedOutput:="12").
            VerifyIL("Module1.getTypes",
            <![CDATA[
{
  // Code size      127 (0x7f)
  .maxstack  6
  .locals init (System.Array V_0, //getTypes
                System.Array V_1, //types
                Object() V_2, //s
                System.Array V_3, //arr
                Integer V_4, //i
                Integer V_5,
                Integer V_6)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.CF97ADEEDB59E05BFD73A2B4C2A8885708C4F4F70C84C64B27120E72AB733B72"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_0018:  ldc.i4.1
  IL_0019:  sub.ovf
  IL_001a:  ldc.i4.1
  IL_001b:  add.ovf
  IL_001c:  newarr     "Object"
  IL_0021:  stloc.2
  IL_0022:  ldloca.s   V_5
  IL_0024:  initobj    "Integer"
  IL_002a:  ldloc.s    V_5
  IL_002c:  box        "Integer"
  IL_0031:  call       "Function Object.GetType() As System.Type"
  IL_0036:  ldc.i4.s   12
  IL_0038:  call       "Function System.Array.CreateInstance(System.Type, Integer) As System.Array"
  IL_003d:  stloc.3
  IL_003e:  ldloc.1
  IL_003f:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_0044:  ldc.i4.1
  IL_0045:  sub.ovf
  IL_0046:  stloc.s    V_6
  IL_0048:  ldc.i4.0
  IL_0049:  stloc.s    V_4
  IL_004b:  br.s       IL_0075
  IL_004d:  ldloc.3
  IL_004e:  ldc.i4.2
  IL_004f:  newarr     "Object"
  IL_0054:  dup
  IL_0055:  ldc.i4.0
  IL_0056:  ldloc.s    V_4
  IL_0058:  box        "Integer"
  IL_005d:  stelem.ref
  IL_005e:  dup
  IL_005f:  ldc.i4.1
  IL_0060:  ldloc.1
  IL_0061:  ldloc.s    V_4
  IL_0063:  callvirt   "Function System.Array.GetValue(Integer) As Object"
  IL_0068:  stelem.ref
  IL_0069:  ldnull
  IL_006a:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexSet(Object, Object(), String())"
  IL_006f:  ldloc.s    V_4
  IL_0071:  ldc.i4.1
  IL_0072:  add.ovf
  IL_0073:  stloc.s    V_4
  IL_0075:  ldloc.s    V_4
  IL_0077:  ldloc.s    V_6
  IL_0079:  ble.s      IL_004d
  IL_007b:  ldloc.3
  IL_007c:  stloc.0
  IL_007d:  ldloc.0
  IL_007e:  ret
}
]]>)
        End Sub

        <WorkItem(575547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575547")>
        <Fact()>
        Public Sub LateBindingToSystemArrayIndex02()
            ' Option Strict On
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Public Function F() As System.Array
        Return Nothing
    End Function
End Class
Module M
    Sub M(o As C)
        Dim value As Object
        value = o.F(1)
        o.F(2) = value
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(source1)
            compilation1.AssertTheseDiagnostics(
<expected>
BC30574: Option Strict On disallows late binding.
        value = o.F(1)
                  ~
BC30574: Option Strict On disallows late binding.
        o.F(2) = value
          ~
</expected>)
            ' Option Strict Off
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict Off
Class C
    Public Function F() As System.Array
        Return Nothing
    End Function
End Class
Module M
    Sub M(o As C)
        Dim value As Object
        value = o.F(1)
        o.F(2) = value
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2)
            compilation2.AssertNoErrors()
        End Sub

        <Fact(), WorkItem(546860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546860")>
        Public Sub Bug17007()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
	System.Console.WriteLine(Test1(100))
    End Sub

    Function Test1(x as Integer) As Integer
	return Test2(x,-x)
    End Function

    Function Test2(x as Integer, y as Integer) As Integer
	return y
    End Function
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe,
expectedOutput:=
            <![CDATA[
-100
]]>)
        End Sub

        <Fact>
        Public Sub InitializeComponentTest()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    Public Sub Main()
    End Sub
End Module

<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Class FromDesigner1

    Sub InitializeComponenT()
    End Sub

    Sub New(a As Integer)
        MyBase.New()
    End Sub

    Sub New(a As Byte)
        Dim x = Sub()
                    InitializeComponenT()
                End Sub
    End Sub

    Sub New(a As Short)
        If False Then
            InitializeComponenT()
        End If
    End Sub

    Sub New(a As SByte)
        Me.New(CShort(a))
    End Sub

    Shared Sub New()
    End Sub

    Sub New(a As Long)
        M1()
    End Sub

    Sub M1()
        M2()
    End Sub

    Sub M2()
        M1()
        M3()
    End Sub

    Sub M3()
        M1()
        M2()
        M3()
        InitializeComponent()
    End Sub
End Class

<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Class FromDesigner2

    Shared Sub InitializeComponent()
    End Sub

    Sub New(b As Integer)
        MyBase.New()
    End Sub

    Sub New(b As Byte)
    End Sub

End Class

<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Class FromDesigner3

    Private field As Integer = 1
    Private Shared sharedField As Integer = 2

    Sub InitializeComponent()
    End Sub

End Class

<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Class FromDesigner4

    Private field As Integer = 1

    Sub InitializeComponent(Of T)()
    End Sub

End Class

<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Class FromDesigner5

    Sub InitializeComponent()
    End Sub

    Sub New(c As Integer)
    End Sub
End Class

<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Class FromDesigner6

    Sub InitializeComponent()
    End Sub

    Sub New(c As Integer)
        MyClass.New()
    End Sub

    Sub New()
        InitializeComponent()
    End Sub
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            AssertTheseEmitDiagnostics(compilation,
<expected>
BC40054: 'Public Sub New(a As Integer)' in designer-generated type 'FromDesigner1' should call InitializeComponent method.
    Sub New(a As Integer)
        ~~~
BC40054: 'Public Sub New(c As Integer)' in designer-generated type 'FromDesigner5' should call InitializeComponent method.
    Sub New(c As Integer)
        ~~~
</expected>)

            Dim compilationVerifier = CompileAndVerify(compilation)

            compilationVerifier.VerifyIL("FromDesigner3..ctor",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      "FromDesigner3.field As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Sub FromDesigner3.InitializeComponent()"
  IL_0013:  ret
}
]]>)

            compilationVerifier.VerifyIL("FromDesigner3..cctor",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  stsfld     "FromDesigner3.sharedField As Integer"
  IL_0006:  ret
}
]]>)

            compilationVerifier.VerifyIL("FromDesigner4..ctor",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      "FromDesigner4.field As Integer"
  IL_000d:  ret
}
]]>)

            compilationVerifier.VerifyIL("FromDesigner5..ctor",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ret
}
]]>)
        End Sub

        <WorkItem(530067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530067")>
        <Fact>
        Public Sub NopAfterCall()
            ' For a nop to be inserted after a call, two conditions must be met:
            '   1) sub (vs function)
            '   2) debug build

            Dim source =
                <compilation>
                    <file name="a.vb">
Module C
    Sub Main()
	    S()
        F()
    End Sub

    Sub S()
    End Sub

    Function F() As Integer
        Return 1
    End Function
End Module
    </file>
                </compilation>

            Dim compRelease = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe)
            Dim compDebug = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.DebugExe)

            ' (2) is not met.
            CompileAndVerify(compRelease).VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  call       "Sub C.S()"
  IL_0005:  call       "Function C.F() As Integer"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            ' S meets (1), but F does not (it doesn't need a nop since it has a pop).
            CompileAndVerify(compDebug).VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       "Sub C.S()"
  IL_0006:  nop
  IL_0007:  call       "Function C.F() As Integer"
  IL_000c:  pop
  IL_000d:  ret
}
]]>)
        End Sub

        <WorkItem(529162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529162")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Bug529162()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq.Expressions

Module Module1

    Sub Main()

        Dim TestCallByName As Expression(Of Action) = Sub() Microsoft.VisualBasic.Interaction.CallByName(Nothing, Nothing, Nothing)
        Print(TestCallByName)
        Dim TestIsNumeric As Expression(Of Action) = Sub() Microsoft.VisualBasic.Information.IsNumeric(Nothing)
        Print(TestIsNumeric)
        Dim TestTypeName As Expression(Of Action) = Sub() Microsoft.VisualBasic.Information.TypeName(Nothing)
        Print(TestTypeName)
        Dim TestSystemTypeName As Expression(Of Action) = Sub() Microsoft.VisualBasic.Information.SystemTypeName(Nothing)
        Print(TestSystemTypeName)
        Dim TestVbTypeName As Expression(Of Action) = Sub() Microsoft.VisualBasic.Information.VbTypeName(Nothing)
        Print(TestVbTypeName)
    End Sub

    Sub Print(expr As Expression(Of Action))
        Dim [call] = DirectCast(expr.Body, MethodCallExpression)
        System.Console.WriteLine("{0} - {1}", [call].Method.DeclaringType, [call].Method)
    End Sub

    Sub TestCallByName()
        Microsoft.VisualBasic.Interaction.CallByName(Nothing, Nothing, Nothing)
    End Sub

    Sub TestIsNumeric()
        Microsoft.VisualBasic.Information.IsNumeric(Nothing)
    End Sub

    Sub TestTypeName()
        Microsoft.VisualBasic.Information.TypeName(Nothing)
    End Sub

    Sub TestSystemTypeName()
        Microsoft.VisualBasic.Information.SystemTypeName(Nothing)
    End Sub

    Sub TestVbTypeName()
        Microsoft.VisualBasic.Information.VbTypeName(Nothing)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Microsoft.VisualBasic.CompilerServices.Versioned - System.Object CallByName(System.Object, System.String, Microsoft.VisualBasic.CallType, System.Object[])
Microsoft.VisualBasic.CompilerServices.Versioned - Boolean IsNumeric(System.Object)
Microsoft.VisualBasic.CompilerServices.Versioned - System.String TypeName(System.Object)
Microsoft.VisualBasic.CompilerServices.Versioned - System.String SystemTypeName(System.String)
Microsoft.VisualBasic.CompilerServices.Versioned - System.String VbTypeName(System.String)
]]>)

            verifier.VerifyIL("Module1.TestCallByName",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldnull
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  newarr     "Object"
  IL_0009:  call       "Function Microsoft.VisualBasic.CompilerServices.Versioned.CallByName(Object, String, Microsoft.VisualBasic.CallType, ParamArray Object()) As Object"
  IL_000e:  pop
  IL_000f:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestIsNumeric",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Versioned.IsNumeric(Object) As Boolean"
  IL_0006:  pop
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestTypeName",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Versioned.TypeName(Object) As String"
  IL_0006:  pop
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestSystemTypeName",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Versioned.SystemTypeName(String) As String"
  IL_0006:  pop
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestVbTypeName",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Versioned.VbTypeName(String) As String"
  IL_0006:  pop
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(653588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653588")>
        <Fact()>
        Public Sub UnusedStructFieldLoad()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Structure S1
    Public field As Integer

    Public Sub New(v As Integer)
        Me.field = v
    End Sub
End Structure

Class A
    Shared Sub Main()
        Dim x = (New S1()).field
        Dim y = (New S1(42)).field
    End Sub
End Class

    </file>
</compilation>, expectedOutput:="").
            VerifyIL("A.Main",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     "Sub S1..ctor(Integer)"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)
        End Sub

        <WorkItem(531166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531166")>
        <Fact()>
        Public Sub LoadingEnumValue__()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Function Goo() As AttributeTargets
        Return AttributeTargets.All
    End Function
    Sub Main(args As String())
        Console.Write(Goo().value__)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="32767").
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (System.AttributeTargets V_0)
  IL_0000:  call       "Function Program.Goo() As System.AttributeTargets"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldfld      "System.AttributeTargets.value__ As Integer"
  IL_000d:  call       "Sub System.Console.Write(Integer)"
  IL_0012:  ret
}
]]>)
        End Sub

        <WorkItem(665317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665317")>
        <Fact()>
        Public Sub InitGenericElement()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class A

End Class
Class B : Inherits A

End Class

Class Program
    Shared Sub Goo(Of T As Class)(array As T())
        array(0) = Nothing
    End Sub

    Shared Sub Bar(Of T)(array As T())
        array(0) = Nothing
    End Sub

    Shared Sub Baz(Of T)(array As T()())
        array(0) = Nothing
    End Sub

    Shared Sub Main()
        Dim array = New B(5) {}
        Goo(Of A)(array)
        Bar(Of A)(array)

        Dim array1 = New B(5)() {}
        Baz(Of A)(array1)
    End Sub
End Class

    </file>
</compilation>, expectedOutput:="").
            VerifyIL("Program.Goo(Of T)(T())",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.0
  IL_000b:  stelem     "T"
  IL_0010:  ret
}
]]>).
            VerifyIL("Program.Bar(Of T)(T())",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  initobj    "T"
  IL_000a:  ldloc.0
  IL_000b:  stelem     "T"
  IL_0010:  ret
}
]]>).
            VerifyIL("Program.Baz(Of T)(T()())",
            <![CDATA[
{
  // Code size        5 (0x5)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldnull
  IL_0003:  stelem.ref
  IL_0004:  ret
}
]]>)
        End Sub

        <WorkItem(718502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718502")>
        <Fact()>
        Public Sub UnaryMinusInCondition()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
imports system

Class A
    private shared x as integer = 42

    Shared Sub Main()
        if --x = 0
            Console.Write(0)
        end if
        if ++x = 0
            Console.Write(0)
        end if
        if --x &lt;> 0
            Console.Write(1)
        end if
        if ++x &lt;> 0
            Console.Write(1)
        end if
    End Sub
End Class

    </file>
</compilation>, expectedOutput:="11").
            VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldsfld     "A.x As Integer"
  IL_0007:  sub.ovf
  IL_0008:  sub.ovf
  IL_0009:  brtrue.s   IL_0011
  IL_000b:  ldc.i4.0
  IL_000c:  call       "Sub System.Console.Write(Integer)"
  IL_0011:  ldsfld     "A.x As Integer"
  IL_0016:  brtrue.s   IL_001e
  IL_0018:  ldc.i4.0
  IL_0019:  call       "Sub System.Console.Write(Integer)"
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  ldsfld     "A.x As Integer"
  IL_0025:  sub.ovf
  IL_0026:  sub.ovf
  IL_0027:  brfalse.s  IL_002f
  IL_0029:  ldc.i4.1
  IL_002a:  call       "Sub System.Console.Write(Integer)"
  IL_002f:  ldsfld     "A.x As Integer"
  IL_0034:  brfalse.s  IL_003c
  IL_0036:  ldc.i4.1
  IL_0037:  call       "Sub System.Console.Write(Integer)"
  IL_003c:  ret
}
]]>)
        End Sub

        <WorkItem(745103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/745103")>
        <Fact()>
        Public Sub TestCompoundOnAFieldOfGeneric()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class Module1
    Shared Sub Main()
        Dim x = New c0()
        test(Of c0).Repro1(x)
        System.Console.Write(x.x)

        test(Of c0).Repro2(x)
        System.Console.Write(x.x)
    End Sub
End Class

Class c0
    Public x As Integer

    Public Property P1 As Integer
        Get
            Return x
        End Get
        Set(value As Integer)
            x = value
        End Set
    End Property

    Default Public Property Item(i As Integer) As Integer
        Get
            Return x
        End Get
        Set(value As Integer)
            x = value
        End Set
    End Property

    Public Shared Function Goo(arg As c0) As Integer
        Return 1
    End Function

    Public Function Goo() As Integer
        Return 1
    End Function
End Class

Class test(Of T As c0)
    Public Shared Sub Repro1(arg As T)
        arg.x += 1
        arg.P1 += 1
        arg(1) += 1
    End Sub

    Public Shared Sub Repro2(arg As T)
        arg.x = c0.Goo(arg)
        arg.x = arg.Goo()
    End Sub
End class

    </file>
</compilation>, expectedOutput:="31").
            VerifyIL("test(Of T).Repro1(T)",
            <![CDATA[
{
      // Code size       77 (0x4d)
  .maxstack  4
  .locals init (Integer& V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  ldflda     "c0.x As Integer"
  IL_000b:  dup
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldind.i4
  IL_000f:  ldc.i4.1
  IL_0010:  add.ovf
  IL_0011:  stind.i4
  IL_0012:  ldarg.0
  IL_0013:  dup
  IL_0014:  stloc.1
  IL_0015:  box        "T"
  IL_001a:  ldloca.s   V_1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function c0.get_P1() As Integer"
  IL_0027:  ldc.i4.1
  IL_0028:  add.ovf
  IL_0029:  callvirt   "Sub c0.set_P1(Integer)"
  IL_002e:  ldarg.0
  IL_002f:  dup
  IL_0030:  stloc.1
  IL_0031:  box        "T"
  IL_0036:  ldc.i4.1
  IL_0037:  ldloca.s   V_1
  IL_0039:  ldc.i4.1
  IL_003a:  constrained. "T"
  IL_0040:  callvirt   "Function c0.get_Item(Integer) As Integer"
  IL_0045:  ldc.i4.1
  IL_0046:  add.ovf
  IL_0047:  callvirt   "Sub c0.set_Item(Integer, Integer)"
  IL_004c:  ret
}
]]>).
            VerifyIL("test(Of T).Repro2(T)",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  ldarg.0
  IL_0007:  box        "T"
  IL_000c:  castclass  "c0"
  IL_0011:  call       "Function c0.Goo(c0) As Integer"
  IL_0016:  stfld      "c0.x As Integer"
  IL_001b:  ldarg.0
  IL_001c:  box        "T"
  IL_0021:  ldarga.s   V_0
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function c0.Goo() As Integer"
  IL_002e:  stfld      "c0.x As Integer"
  IL_0033:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CallFinalMethodOnTypeParam()
            CompileAndVerify(
<compilation>
    <file name="a.vb">


Module Module1
    Sub Main()
        Test1(New cls1())
        Test1(New cls2())

        Test2(New cls2())
    End Sub

    Sub Test1(Of T As cls1)(arg As T)
        arg.Goo()
    End Sub

    Sub Test2(Of T As cls2)(arg As T)
        arg.Goo()
    End Sub
End Module

Class cls0
    Overridable Sub Goo()

    End Sub
End Class

Class cls1 : Inherits cls0
    Public NotOverridable Overrides Sub Goo()
        System.Console.Write("Goo")
    End Sub
End Class

Class cls2 : Inherits cls1
End Class


    </file>
</compilation>, expectedOutput:="GooGooGoo").
            VerifyIL("Module1.Test1(Of T)(T)",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Sub cls1.Goo()"
  IL_000d:  ret
}

]]>).
            VerifyIL("Module1.Test2(Of T)(T)",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Sub cls1.Goo()"
  IL_000d:  ret
}

]]>)
        End Sub

        <WorkItem(770557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770557")>
        <Fact()>
        Public Sub BoolConditionDebug001()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[

Imports System
Imports System.Runtime.InteropServices
<StructLayout(LayoutKind.Explicit)>
Public Structure BoolBreaker
    <FieldOffset(0)> Public i As Int32
    <FieldOffset(0)> Public bool As Boolean
End Structure

Friend Module Module1
    Sub Main()
        Dim x As BoolBreaker
        x.i = 2
        If x.bool <> True Then
            Console.Write("i=2 -> x.bool <> True ") ' Roslyn
        Else
            Console.Write("i=2 -> x.bool = True ") ' Native
        End If
        x.i = 2147483647
        If x.bool <> True Then
            Console.Write("i=2147483647 -> x.bool <> True ")
        Else
            Console.Write("i=21474836472 -> x.bool = True ")
        End If
    End Sub
End Module

]]>
    </file>
</compilation>, expectedOutput:="i=2 -> x.bool = True i=21474836472 -> x.bool = True").
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (BoolBreaker V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.2
  IL_0003:  stfld      "BoolBreaker.i As Integer"
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "BoolBreaker.bool As Boolean"
  IL_000e:  brtrue.s   IL_001c
  IL_0010:  ldstr      "i=2 -> x.bool <> True "
  IL_0015:  call       "Sub System.Console.Write(String)"
  IL_001a:  br.s       IL_0026
  IL_001c:  ldstr      "i=2 -> x.bool = True "
  IL_0021:  call       "Sub System.Console.Write(String)"
  IL_0026:  ldloca.s   V_0
  IL_0028:  ldc.i4     0x7fffffff
  IL_002d:  stfld      "BoolBreaker.i As Integer"
  IL_0032:  ldloc.0
  IL_0033:  ldfld      "BoolBreaker.bool As Boolean"
  IL_0038:  brtrue.s   IL_0045
  IL_003a:  ldstr      "i=2147483647 -> x.bool <> True "
  IL_003f:  call       "Sub System.Console.Write(String)"
  IL_0044:  ret
  IL_0045:  ldstr      "i=21474836472 -> x.bool = True "
  IL_004a:  call       "Sub System.Console.Write(String)"
  IL_004f:  ret
}
]]>)
        End Sub

        <WorkItem(770557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770557")>
        <Fact()>
        Public Sub BoolConditionDebug002()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[

Imports System
Imports System.Runtime.InteropServices
<StructLayout(LayoutKind.Explicit)>
Public Structure BoolBreaker
    <FieldOffset(0)> Public i As Int32
    <FieldOffset(0)> Public bool As Boolean
End Structure

Friend Module Module1
    Sub Main()
        Dim x As BoolBreaker
        x.i = 2
        If x.bool <> True Then
            Console.Write("i=2 -> x.bool <> True ") ' Roslyn
        Else
            Console.Write("i=2 -> x.bool = True ") ' Native
        End If
        x.i = 2147483647
        If x.bool <> True Then
            Console.Write("i=2147483647 -> x.bool <> True ")
        Else
            Console.Write("i=21474836472 -> x.bool = True ")
        End If
    End Sub
End Module

]]>
    </file>
</compilation>, options:=TestOptions.DebugExe,
                expectedOutput:="i=2 -> x.bool = True i=21474836472 -> x.bool = True")

            c.VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size      102 (0x66)
  .maxstack  2
  .locals init (BoolBreaker V_0, //x
                Boolean V_1,
                Boolean V_2)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.2
  IL_0004:  stfld      "BoolBreaker.i As Integer"
  IL_0009:  ldloc.0
  IL_000a:  ldfld      "BoolBreaker.bool As Boolean"
  IL_000f:  ldc.i4.0
  IL_0010:  ceq
  IL_0012:  stloc.1
  IL_0013:  ldloc.1
  IL_0014:  brfalse.s  IL_0024
  IL_0016:  ldstr      "i=2 -> x.bool <> True "
  IL_001b:  call       "Sub System.Console.Write(String)"
  IL_0020:  nop
  IL_0021:  nop
  IL_0022:  br.s       IL_0031
  IL_0024:  nop
  IL_0025:  ldstr      "i=2 -> x.bool = True "
  IL_002a:  call       "Sub System.Console.Write(String)"
  IL_002f:  nop
  IL_0030:  nop
  IL_0031:  ldloca.s   V_0
  IL_0033:  ldc.i4     0x7fffffff
  IL_0038:  stfld      "BoolBreaker.i As Integer"
  IL_003d:  ldloc.0
  IL_003e:  ldfld      "BoolBreaker.bool As Boolean"
  IL_0043:  ldc.i4.0
  IL_0044:  ceq
  IL_0046:  stloc.2
  IL_0047:  ldloc.2
  IL_0048:  brfalse.s  IL_0058
  IL_004a:  ldstr      "i=2147483647 -> x.bool <> True "
  IL_004f:  call       "Sub System.Console.Write(String)"
  IL_0054:  nop
  IL_0055:  nop
  IL_0056:  br.s       IL_0065
  IL_0058:  nop
  IL_0059:  ldstr      "i=21474836472 -> x.bool = True "
  IL_005e:  call       "Sub System.Console.Write(String)"
  IL_0063:  nop
  IL_0064:  nop
  IL_0065:  ret
}
]]>)
        End Sub

        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact()>
        Public Sub MissingMember_Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure Int32
    End Structure
    Public Structure UInt32
    End Structure
    Public Structure Nullable(Of T)
    End Structure
    Public Class [String]
    End Class
End Namespace
Class C
    Shared Sub M(s As String)
        Select Case s
        Case "A"
        Case "B"
        End Select
    End Sub
End Class
]]></file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.CompareString' is not defined.
        Select Case s
                    ~
BC35000: Requested operation is not available because the runtime library function 'System.String.get_Chars' is not defined.
        Select Case s
                    ~
</errors>)
        End Sub

        ' As above with Microsoft.VisualBasic.CompilerServices.EmbeddedOperators defined.
        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact()>
        Public Sub MissingMember_Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure Int32
    End Structure
    Public Structure UInt32
    End Structure
    Public Structure Nullable(Of T)
    End Structure
    Public Class [String]
    End Class
End Namespace
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class EmbeddedOperators
    End Class
End Namespace
Class C
    Shared Sub M(s As String)
        Select Case s
        Case "A"
        Case "B"
        End Select
    End Sub
End Class
]]></file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.EmbeddedOperators.CompareString' is not defined.
        Select Case s
                    ~
BC35000: Requested operation is not available because the runtime library function 'System.String.get_Chars' is not defined.
        Select Case s
                    ~
</errors>)
        End Sub

        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact()>
        Public Sub MissingMember_System_Type__GetTypeFromHandle()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure Int32
    End Structure
    Public Structure Nullable(Of T)
    End Structure
    Public Class [Type]
    End Class
End Namespace
Class C
    Shared F As Object = GetType(C)
End Class
]]></file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Type.GetTypeFromHandle' is not defined.
    Shared F As Object = GetType(C)
                         ~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact()>
        Public Sub MissingMember_Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Class Exception
    End Class
End Namespace
Class C
    Shared Sub M()
        Try
            Dim o = Nothing
        Catch e As Exception
        End Try
    End Sub
End Class
]]></file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError' is not defined.
        Catch e As Exception
        ~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError' is not defined.
        Catch e As Exception
        ~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(765569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/765569")>
        <Fact()>
        Public Sub ConstMatchesType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Enum LineStyle
  Dot
  Dash
End Enum

Friend Class A
 Public shared Sub Main()
   Const linestyle As LineStyle = LineStyle.Dot
   Console.WriteLine(linestyle)
 End Sub
End CLass

    </file>
</compilation>, expectedOutput:="0").
            VerifyIL("A.Main",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0006:  ret
}
]]>)
        End Sub

        <WorkItem(824308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824308")>
        <Fact()>
        Public Sub ConstCircular001()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum LineStyle
  Dot
  Dash
End Enum

Friend Class A
 Public shared Sub Main()
   Const blah As LineStyle = blah Or LineStyle.Dot
   Console.WriteLine(blah)
 End Sub
End CLass

]]></file>
</compilation>)
            comp.AssertTheseDiagnostics(
<errors>
BC30500: Constant 'blah' cannot depend on its own value.
   Const blah As LineStyle = blah Or LineStyle.Dot
         ~~~~
</errors>)

        End Sub

        <WorkItem(824308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824308")>
        <Fact()>
        Public Sub ConstCircular002()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum LineStyle
  Dot
  Dash
End Enum

Friend Class A
 Public shared Sub Main()
   Const blah = blah Or LineStyle.Dot
   Console.WriteLine(blah)
 End Sub
End CLass

]]></file>
</compilation>)
            comp.AssertTheseDiagnostics(
<errors>
BC30500: Constant 'blah' cannot depend on its own value.
   Const blah = blah Or LineStyle.Dot
                ~~~~
BC42104: Variable 'blah' is used before it has been assigned a value. A null reference exception could result at runtime.
   Const blah = blah Or LineStyle.Dot
                ~~~~
</errors>)

        End Sub

        <WorkItem(824308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824308")>
        <Fact()>
        Public Sub ConstCircular003()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum LineStyle
  Dot
  Dash
End Enum

Friend Class A
 Public shared Sub Main()
   Const blah As Object = blah Or LineStyle.Dot
   Const blah1 As Object = blah1
   Console.WriteLine(blah)
 End Sub
End CLass

]]></file>
</compilation>)
            comp.AssertTheseDiagnostics(
<errors>
BC30500: Constant 'blah' cannot depend on its own value.
   Const blah As Object = blah Or LineStyle.Dot
                          ~~~~
BC42104: Variable 'blah' is used before it has been assigned a value. A null reference exception could result at runtime.
   Const blah As Object = blah Or LineStyle.Dot
                          ~~~~
BC30500: Constant 'blah1' cannot depend on its own value.
   Const blah1 As Object = blah1
                           ~~~~~
BC42104: Variable 'blah1' is used before it has been assigned a value. A null reference exception could result at runtime.
   Const blah1 As Object = blah1
                           ~~~~~

</errors>)

        End Sub

        <Fact>
        <WorkItem(4196, "https://github.com/dotnet/roslyn/issues/4196")>
        Public Sub BadDefaultParameterValue()
            Dim source =
<compilation>
    <file name="a.vb">
Imports BadDefaultParameterValue
Module C
    Sub Main
        Util.M("test")
    End Sub
End Module
    </file>
</compilation>

            Dim testReference = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.Repros.BadDefaultParameterValue).GetReference()
            Dim compilation = CompileAndVerify(source, references:=New MetadataReference() {testReference})
            compilation.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldstr      "test"
  IL_0005:  ldnull
  IL_0006:  call       "Sub BadDefaultParameterValue.Util.M(String, String)"
  IL_000b:  ret
}]]>)
        End Sub

        <ConditionalFact(GetType(NoIOperationValidation))>
        <WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")>
        Public Sub EmitSequenceOfBinaryExpressions_01()
            Dim source =
$"
Class Test
    Shared Sub Main()
        Dim f = new Long(4096-1) {{}}
        for i As Integer = 0 To 4095
            f(i) = 4096 - i
        Next

        System.Console.WriteLine(If(Calculate1(f) = Calculate2(f), ""True"", ""False""))
    End Sub

    Shared Function Calculate1(f As Long()) As Long
        Return {BuildSequenceOfBinaryExpressions_01()}
    End Function

    Shared Function Calculate2(f As Long()) As Long
        Dim result as Long = 0
        Dim i as Integer
        For i = 0 To f.Length - 1
            result+=(i + 1)*f(i)
        Next

        return result + (i + 1)
    End Function
End Class
"
            Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="True")
        End Sub

        Private Shared Function BuildSequenceOfBinaryExpressions_01(Optional count As Integer = 4096) As String
            Dim builder = New System.Text.StringBuilder()
            Dim i As Integer

            For i = 0 To count - 1
                builder.Append(i + 1)
                builder.Append(" * ")
                builder.Append("f(")
                builder.Append(i)
                builder.Append(") + ")
            Next

            builder.Append(i + 1)

            Return builder.ToString()
        End Function

        <ConditionalFact(GetType(NoIOperationValidation))>
        <WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")>
        Public Sub EmitSequenceOfBinaryExpressions_02()
            Dim source =
$"
Class Test
    Shared Sub Main()
        Dim f = new Long(4096-1) {{}}
        for i As Integer = 0 To 4095
            f(i) = 4096 - i
        Next

        System.Console.WriteLine(Calculate(f))
    End Sub

    Shared Function Calculate(f As Long()) As Double
        Return {BuildSequenceOfBinaryExpressions_01()}
    End Function
End Class
"
            Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))

            CompileAndVerify(compilation, expectedOutput:="11461640193")
        End Sub

        <ConditionalFact(GetType(NoIOperationValidation))>
        <WorkItem(6077, "https://github.com/dotnet/roslyn/issues/6077")>
        <WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")>
        Public Sub EmitSequenceOfBinaryExpressions_03()

            Dim diagnostics = ImmutableArray(Of Diagnostic).Empty

            Const start = 8192
            Const [step] = 4096
            Const limit = start * 4

            For count As Integer = start To limit Step [step]
                Dim source =
$"
Class Test
    Shared Sub Main()
    End Sub

    Shared Function Calculate(a As Boolean(), f As Boolean()) As Boolean
        Return {BuildSequenceOfBinaryExpressions_03(count)}
    End Function
End Class
"
                Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
                diagnostics = compilation.GetEmitDiagnostics()

                If Not diagnostics.IsEmpty Then
                    Exit For
                End If
            Next

            diagnostics.Verify(
    Diagnostic(ERRID.ERR_TooLongOrComplexExpression, "a").WithLocation(7, 16)
                )
        End Sub

        Private Shared Function BuildSequenceOfBinaryExpressions_03(Optional count As Integer = 8192) As String
            Dim builder = New System.Text.StringBuilder()
            Dim i As Integer

            For i = 0 To count - 1
                builder.Append("a(")
                builder.Append(i)
                builder.Append(")")
                builder.Append(" AndAlso ")
                builder.Append("f(")
                builder.Append(i)
                builder.Append(") OrElse ")
            Next

            builder.Append("a(")
            builder.Append(i)
            builder.Append(")")

            Return builder.ToString()
        End Function

        <ConditionalFact(GetType(NoIOperationValidation))>
        <WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")>
        Public Sub EmitSequenceOfBinaryExpressions_04()
            Dim size = 8192
            Dim source =
$"
Class Test
    Shared Sub Main()
        Dim f = new Single?({size}-1) {{}}
        for i As Integer = 0 To ({size} - 1)
            f(i) = 4096 - i
        Next

        System.Console.WriteLine(Calculate(f))
    End Sub

    Shared Function Calculate(f As Single?()) As Double?
        Return {BuildSequenceOfBinaryExpressions_01(size)}
    End Function
End Class
"
            Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseExe)

            compilation.VerifyEmitDiagnostics(
    Diagnostic(ERRID.ERR_TooLongOrComplexExpression, "1").WithLocation(13, 16)
                )
        End Sub

        <ConditionalFact(GetType(NoIOperationValidation))>
        <WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")>
        Public Sub EmitSequenceOfBinaryExpressions_05()
            Dim count As Integer = 50
            Dim source =
$"
Class Test
    Shared Sub Main()
        Test1()
        Test2()
    End Sub

    Shared Sub Test1()
        Dim f = new Double({count}-1) {{}}
        for i As Integer = 0 To {count}-1
            f(i) = 4096 - i
        Next

        System.Console.WriteLine(Calculate(f))
    End Sub

    Shared Function Calculate(f As Double()) As Double
        Return {BuildSequenceOfBinaryExpressions_01(count)}
    End Function

    Shared Sub Test2()
        Dim f = new Double?({count}-1) {{}}
        for i As Integer = 0 To {count}-1
            f(i) = 4096 - i
        Next

        System.Console.WriteLine(Calculate(f))
    End Sub

    Shared Function Calculate(f As Double?()) As Double?
        Return {BuildSequenceOfBinaryExpressions_01(count)}
    End Function

End Class
"
            Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="5180801
5180801")
        End Sub

        ' Temporarily disabling in release builds due to the following item:
        ' https://github.com/dotnet/roslyn/issues/60472
#If DEBUG Then
        ' Restricting to English as there are different tolerance limits on non-English cultures. The test
        ' is to prevent regressions and single language should be sufficient here
        <ConditionalFact(GetType(NoIOperationValidation), GetType(WindowsOnly), GetType(IsEnglishLocal))>
        <WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")>
        Public Sub EmitSequenceOfBinaryExpressions_06()
            Dim source =
$"
Class Test
    Shared Sub Main()
    End Sub

    Shared Function Calculate(a As S1(), f As S1()) As Boolean
        Return {BuildSequenceOfBinaryExpressions_03()}
    End Function
End Class

Structure S1
    Public Shared Operator And(x As S1, y As S1) As S1
        Return New S1()
    End Operator

    Public Shared Operator Or(x As S1, y As S1) As S1
        Return New S1()
    End Operator

    Public Shared Operator IsTrue(x As S1) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As S1) As Boolean
        Return True
    End Operator

    Public Shared Widening Operator CType(x As S1) As Boolean
        Return True
    End Operator
End Structure
"
            Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))

            compilation.VerifyEmitDiagnostics(
    Diagnostic(ERRID.ERR_TooLongOrComplexExpression, "a").WithLocation(7, 16),
    Diagnostic(ERRID.ERR_TooLongOrComplexExpression, "a").WithLocation(7, 16),
    Diagnostic(ERRID.ERR_TooLongOrComplexExpression, "a").WithLocation(7, 16)
                )
        End Sub
#End If

        <Fact()>
        Public Sub InplaceCtorUsesLocal()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[

Module Module1
    Private arr As S1() = New S1(1) {}

    Structure S1
        Public a, b As Integer

        Public Sub New(a As Integer, b As Integer)
            Me.a = a
            Me.b = b
        End Sub

        Public Sub New(a As Integer)
            Me.a = a
        End Sub

    End Structure

    Sub Main()
        Dim arg = System.Math.Max(1, 2)
        Dim val = New S1(arg, arg)
        arr(0) = val
        System.Console.WriteLine(arr(0).a)
    End Sub
End Module

]]>
    </file>
</compilation>, options:=TestOptions.ReleaseExe,
                expectedOutput:="2")

            c.VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (Integer V_0, //arg
                Module1.S1 V_1) //val
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  call       "Function System.Math.Max(Integer, Integer) As Integer"
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldloc.0
  IL_000b:  ldloc.0
  IL_000c:  call       "Sub Module1.S1..ctor(Integer, Integer)"
  IL_0011:  ldsfld     "Module1.arr As Module1.S1()"
  IL_0016:  ldc.i4.0
  IL_0017:  ldloc.1
  IL_0018:  stelem     "Module1.S1"
  IL_001d:  ldsfld     "Module1.arr As Module1.S1()"
  IL_0022:  ldc.i4.0
  IL_0023:  ldelema    "Module1.S1"
  IL_0028:  ldfld      "Module1.S1.a As Integer"
  IL_002d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0032:  ret
}
]]>)
        End Sub

        <Fact()>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        <WorkItem(7148, "https://github.com/dotnet/roslyn/issues/7148")>
        Public Sub Issue7148_1()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Globalization
Public Class TestClass
    Private _rotation As Decimal
    Private Sub CalculateDimensions()
        _rotation *= 180 / System.Math.PI 'This line causes '"vbc.exe" exited with code -2146232797'
    End Sub

    Shared Sub Main()
        Dim x as New TestClass()
        x._rotation = 1
        x.CalculateDimensions()
        System.Console.WriteLine(x._rotation.ToString(CultureInfo.InvariantCulture))
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe,
                expectedOutput:="57.2957795130823")

            c.VerifyIL("TestClass.CalculateDimensions",
            <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  3
  .locals init (Decimal& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "TestClass._rotation As Decimal"
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldobj      "Decimal"
  IL_000e:  call       "Function System.Convert.ToDouble(Decimal) As Double"
  IL_0013:  ldc.r8     57.2957795130823
  IL_001c:  mul
  IL_001d:  newobj     "Sub Decimal..ctor(Double)"
  IL_0022:  stobj      "Decimal"
  IL_0027:  ret
}
]]>)
        End Sub

        <Fact()>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        <WorkItem(7148, "https://github.com/dotnet/roslyn/issues/7148")>
        Public Sub Issue7148_2()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Globalization
Public Class TestClass
    Private Shared Sub CalculateDimensions(_rotation As Decimal())
        _rotation(GetIndex()) *= 180 / System.Math.PI 'This line causes '"vbc.exe" exited with code -2146232797'
    End Sub

    Private Shared Function GetIndex() As Integer
        Return 0
    End Function

    Shared Sub Main()
        Dim _rotation(0) as Decimal
        _rotation(0) = 1
        CalculateDimensions(_rotation)
        System.Console.WriteLine(_rotation(0).ToString(CultureInfo.InvariantCulture))
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe,
                expectedOutput:="57.2957795130823")

            c.VerifyIL("TestClass.CalculateDimensions",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (Decimal& V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function TestClass.GetIndex() As Integer"
  IL_0006:  ldelema    "Decimal"
  IL_000b:  dup
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldobj      "Decimal"
  IL_0013:  call       "Function System.Convert.ToDouble(Decimal) As Double"
  IL_0018:  ldc.r8     57.2957795130823
  IL_0021:  mul
  IL_0022:  newobj     "Sub Decimal..ctor(Double)"
  IL_0027:  stobj      "Decimal"
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(9703, "https://github.com/dotnet/roslyn/issues/9703")>
        Public Sub IgnoredConversion()
            CompileAndVerify(
                <compilation>
                    <file name="ignoreNullableValue.vb">
Module MainModule
    Public Class Form1
        Public Class BadCompiler
            Public Property Value As Date?
        End Class

        Private TestObj As BadCompiler = New BadCompiler()

        Public Sub IPE()
            Dim o as Object
            o = TestObj.Value
        End Sub
    End Class

    Public Sub Main()
        Dim f = new Form1
        f.IPE()
    End Sub
End Module
                    </file>
                </compilation>).
                            VerifyIL("MainModule.Form1.IPE",
            <![CDATA[
{
// Code size       13 (0xd)
.maxstack  1
IL_0000:  ldarg.0
IL_0001:  ldfld      "MainModule.Form1.TestObj As MainModule.Form1.BadCompiler"
IL_0006:  callvirt   "Function MainModule.Form1.BadCompiler.get_Value() As Date?"
IL_000b:  pop
IL_000c:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(15672, "https://github.com/dotnet/roslyn/pull/15672")>
        Public Sub ConditionalAccessOffOfUnconstrainedDefault1()
            Dim c = CompileAndVerify(
                <compilation>
                    <file name="ignoreNullableValue.vb">
Module Module1
    Public Sub Main()
        Test(42)
        Test("")
    End Sub

    Public Sub Test(of T)(arg as T)
        System.Console.WriteLine(DirectCast(Nothing, T)?.ToString())
    End Sub
End Module
                    </file>
                </compilation>, options:=TestOptions.ReleaseExe,
                expectedOutput:="0")

            c.VerifyIL("Module1.Test",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  dup
  IL_000d:  ldobj      "T"
  IL_0012:  box        "T"
  IL_0017:  brtrue.s   IL_001d
  IL_0019:  pop
  IL_001a:  ldnull
  IL_001b:  br.s       IL_0028
  IL_001d:  constrained. "T"
  IL_0023:  callvirt   "Function Object.ToString() As String"
  IL_0028:  call       "Sub System.Console.WriteLine(String)"
  IL_002d:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(22533, "https://github.com/dotnet/roslyn/issues/22533")>
        Public Sub TestExplicitDoubleConversionEmitted()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Function M() As Boolean
        Dim dValue As Double = 600.1
        Dim mbytDeciWgt As Byte = 1

        Return CDbl(dValue) > CDbl(dValue + CDbl(10 ^ -mbytDeciWgt))
    End Function
End Module
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Double V_0, //dValue
                Byte V_1) //mbytDeciWgt
  IL_0000:  ldc.r8     600.1
  IL_0009:  stloc.0
  IL_000a:  ldc.i4.1
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  conv.r8
  IL_000e:  ldloc.0
  IL_000f:  ldc.r8     10
  IL_0018:  ldloc.1
  IL_0019:  neg
  IL_001a:  conv.ovf.i2
  IL_001b:  conv.r8
  IL_001c:  call       "Function System.Math.Pow(Double, Double) As Double"
  IL_0021:  conv.r8
  IL_0022:  add
  IL_0023:  conv.r8
  IL_0024:  cgt
  IL_0026:  ret
}
]]>)

        End Sub

        <Fact, WorkItem(22533, "https://github.com/dotnet/roslyn/issues/22533")>
        Public Sub TestImplicitDoubleConversionEmitted()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Function M() As Boolean
        Dim dValue As Double = 600.1
        Dim mbytDeciWgt As Byte = 1

        Return dValue > dValue + (10 ^ -mbytDeciWgt)
    End Function
End Module
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  .locals init (Byte V_0) //mbytDeciWgt
  IL_0000:  ldc.r8     600.1
  IL_0009:  ldc.i4.1
  IL_000a:  stloc.0
  IL_000b:  dup
  IL_000c:  ldc.r8     10
  IL_0015:  ldloc.0
  IL_0016:  neg
  IL_0017:  conv.ovf.i2
  IL_0018:  conv.r8
  IL_0019:  call       "Function System.Math.Pow(Double, Double) As Double"
  IL_001e:  add
  IL_001f:  cgt
  IL_0021:  ret
}
]]>)

        End Sub

        <Fact, WorkItem(22533, "https://github.com/dotnet/roslyn/issues/22533")>
        Public Sub TestExplicitSingleConversionEmitted()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Function M() As Boolean
        Dim dValue As Single = 600.1
        Dim mbytDeciWgt As Byte = 1

        Return CSng(dValue) > CSng(dValue + CSng(10 ^ -mbytDeciWgt))
    End Function
End Module
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (Single V_0, //dValue
                Byte V_1) //mbytDeciWgt
  IL_0000:  ldc.r4     600.1
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  conv.r4
  IL_000a:  ldloc.0
  IL_000b:  ldc.r8     10
  IL_0014:  ldloc.1
  IL_0015:  neg
  IL_0016:  conv.ovf.i2
  IL_0017:  conv.r8
  IL_0018:  call       "Function System.Math.Pow(Double, Double) As Double"
  IL_001d:  conv.r4
  IL_001e:  add
  IL_001f:  conv.r4
  IL_0020:  cgt
  IL_0022:  ret
}
]]>)

        End Sub

        <Fact, WorkItem(22533, "https://github.com/dotnet/roslyn/issues/22533")>
        Public Sub TestExplicitSingleConversionNotEmittedOnConstantValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Function M() As Boolean
        Dim dValue As Single = 600.1
        Dim mbytDeciWgt As Byte = 1

        Return CSng(dValue) > CSng(CSng(600) + CSng(0.1))
    End Function
End Module
    </file>
</compilation>).
            VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldc.r4     600.1
  IL_0005:  conv.r4
  IL_0006:  ldc.r4     600.1
  IL_000b:  cgt
  IL_000d:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ArrayElementByReference_Invariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New String() {"b"})
    End Sub
    Sub F(a() As String)
        G(a)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As String)
        H(a(0))
    End Sub
    Sub H(ByRef s As String)
        s = s.ToUpper()
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="B").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    "String"
  IL_0007:  call       "Sub M.H(ByRef String)"
  IL_000c:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")>
        Public Sub ArrayElementByReference_Covariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Object() {"a"})
        F(New String() {"b"})
    End Sub
    Sub F(a() As Object)
        G(a)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As Object)
        H(a(0))
    End Sub
    Sub H(ByRef s As String)
        s = s.ToUpper()
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="AB").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.0
  IL_0003:  ldelem.ref
  IL_0004:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Sub M.H(ByRef String)"
  IL_0011:  ldc.i4.0
  IL_0012:  ldloc.0
  IL_0013:  stelem.ref
  IL_0014:  ret
}
]]>)
        End Sub

        ' Generated code results in ArrayTypeMismatchException,
        ' matching native compiler.
        <Fact, WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")>
        Public Sub ArrayElementByReferenceBase_Covariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Object() {"a"})
        F(New String() {"b"})
    End Sub
    Sub F(a() As Object)
        Try
            G(a)
            System.Console.Write(a(0))
        Catch e As System.Exception
            System.Console.Write(e.GetType().Name)
        End Try
    End Sub
    Sub G(a() As Object)
        H(a(0))
    End Sub
    Sub H(ByRef s As Object)
        s = s.ToString().ToUpper()
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="AArrayTypeMismatchException").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    "Object"
  IL_0007:  call       "Sub M.H(ByRef Object)"
  IL_000c:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")>
        Public Sub ArrayElementByReference_TypeParameter()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        Dim a = New String() { "b" }
        Dim b = New B()
        b.F(a)
        System.Console.Write(a(0))
    End Sub
End Module
MustInherit Class A(Of T)
    Friend MustOverride Sub F(Of U As T)(a() As U)
End Class
Class B
    Inherits A(Of String)
    Friend Overrides Sub F(Of U As String)(a() As U)
        G(a(0))
    End Sub
    Sub G(ByRef s As String)
        s = s.ToUpper()
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="B").
            VerifyIL("B.F",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (U() V_0,
                String V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldelem     "U"
  IL_000a:  box        "U"
  IL_000f:  castclass  "String"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Sub B.G(ByRef String)"
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  ldloc.1
  IL_001f:  unbox.any  "U"
  IL_0024:  stelem     "U"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")>
        Public Sub ArrayElementByReference_StructConstraint()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        Dim a = New Integer() { 1 }
        Dim b = New B()
        b.F(a)
        System.Console.Write(a(0))
    End Sub
End Module
MustInherit Class A(Of T)
    Friend MustOverride Sub F(Of U As {T, Structure})(a() As U)
End Class
Class B
    Inherits A(Of Integer)
    Friend Overrides Sub F(Of U As {Integer, Structure})(a() As U)
        G(a(0))
    End Sub
    Sub G(ByRef i As Integer)
        i += 1
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="2").
            VerifyIL("B.F",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (U() V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldelem     "U"
  IL_000a:  box        "U"
  IL_000f:  unbox.any  "Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Sub B.G(ByRef Integer)"
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  ldloc.1
  IL_001f:  box        "Integer"
  IL_0024:  unbox.any  "U"
  IL_0029:  stelem     "U"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayElementByReference_ValueType()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Integer() {1})
    End Sub
    Sub F(a() As Integer)
        G(a)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As Integer)
        H(a(0))
    End Sub
    Sub H(ByRef i As Integer)
        i = 2
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="2").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    "Integer"
  IL_0007:  call       "Sub M.H(ByRef Integer)"
  IL_000c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayElementCompoundAssignment_Invariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New String() {""}, "B")
    End Sub
    Sub F(a() As String, s As String)
        G(a, s)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As String, s As String)
        a(0) += s
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="B").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (String& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    "String"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldind.ref
  IL_000b:  ldarg.1
  IL_000c:  call       "Function String.Concat(String, String) As String"
  IL_0011:  stind.ref
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")>
        Public Sub ArrayElementCompoundAssignment_Covariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Object() {""}, "A")
        F(New String() {""}, "B")
    End Sub
    Sub F(a() As Object, s As String)
        G(a, s)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As Object, s As String)
        a(0) += s
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="AB").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  4
  .locals init (Object() V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.0
  IL_0006:  ldelem.ref
  IL_0007:  ldarg.1
  IL_0008:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_000d:  stelem.ref
  IL_000e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayElementCompoundAssignment_ValueType()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Integer() {1}, 2)
    End Sub
    Sub F(a() As Integer, i As Integer)
        G(a, i)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As Integer, i As Integer)
        a(0) += i
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="3").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  3
  .locals init (Integer& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    "Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldind.i4
  IL_000b:  ldarg.1
  IL_000c:  add.ovf
  IL_000d:  stind.i4
  IL_000e:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(547533, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547533")>
        Public Sub ArrayElementCompoundAssignment_Covariant_NonConstantIndex()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Object() {""}, "A")
        F(New String() {""}, "B")
    End Sub
    Sub F(a() As Object, s As String)
        G(a, s)
        System.Console.Write(a(0))
    End Sub
    Sub G(a() As Object, s As String)
        a(Index(a)) += s
    End Sub
    Function Index(arg As Object) As Integer
        System.Console.Write(arg.GetType().Name)
        Return 0
    End Function
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="Object[]AString[]B").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  4
  .locals init (Object() V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  call       "Function M.Index(Object) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.1
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  ldelem.ref
  IL_000e:  ldarg.1
  IL_000f:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_0014:  stelem.ref
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayElementWithBlock_Invariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New String() {"B"})
    End Sub
    Sub F(a() As String)
        System.Console.Write(G(a))
    End Sub
    Function G(a() As String) As String
        With a(0)
            Return .ToString() + .ToLower()
        End With
    End Function
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="Bb").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (String V_0) //$W0
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  callvirt   "Function String.ToString() As String"
  IL_000a:  ldloc.0
  IL_000b:  callvirt   "Function String.ToLower() As String"
  IL_0010:  call       "Function String.Concat(String, String) As String"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayElementWithBlock_Covariant()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Object() {"A"})
        F(New String() {"B"})
    End Sub
    Sub F(a() As Object)
        System.Console.Write(G(a))
    End Sub
    Function G(a() As Object) As String
        With a(0)
            Return .ToString() + .ToLower()
        End With
    End Function
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="AaBb").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  8
  .locals init (Object V_0) //$W0
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.ref
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  callvirt   "Function Object.ToString() As String"
  IL_000a:  ldloc.0
  IL_000b:  ldnull
  IL_000c:  ldstr      "ToLower"
  IL_0011:  ldc.i4.0
  IL_0012:  newarr     "Object"
  IL_0017:  ldnull
  IL_0018:  ldnull
  IL_0019:  ldnull
  IL_001a:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_0024:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ArrayElementWithBlock_ValueType()
            Dim comp =
<compilation>
    <file>
Option Strict Off
Module M
    Sub Main()
        F(New Integer() {1})
    End Sub
    Sub F(a() As Integer)
        System.Console.Write(G(a))
    End Sub
    Function G(a() As Integer) As String
        With a(0)
            Return .ToString() + .ToString()
        End With
    End Function
End Module
    </file>
</compilation>
            CompileAndVerify(comp, expectedOutput:="11").
            VerifyIL("M.G",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Integer& V_0) //$W0
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    "Integer"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       "Function Integer.ToString() As String"
  IL_000e:  ldloc.0
  IL_000f:  call       "Function Integer.ToString() As String"
  IL_0014:  call       "Function String.Concat(String, String) As String"
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub NormalizedNaN()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class Program
    Shared Sub Main()
        CheckNaN(Double.NaN)
        CheckNaN(Single.NaN)
        CheckNaN(0.0 / 0.0)
        CheckNaN(0.0 / -0.0)
        Dim inf As Double = 1.0 / 0.0
        CheckNaN(inf + Double.NaN)
        CheckNaN(inf - Double.NaN)
        CheckNaN(-Double.NaN)
    End Sub

    Shared Sub CheckNaN(nan As Double)
        Dim expected As Long = &amp;HFFF8000000000000
        Dim actual As Long = System.BitConverter.DoubleToInt64Bits(nan)
        If expected &lt;> actual Then
            Throw New System.Exception($"expected=0X{expected: X} actual=0X{actual:X}")
        End If
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[
]]>)
        End Sub

    End Class
End Namespace

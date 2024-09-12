' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenRefReturnTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub LocalType()
            Dim comp1 = CreateCSharpCompilation(
"public class A<T>
{
#pragma warning disable 0649
    private static T _f;
    public static ref T F()
    {
        return ref _f;
    }
}
public class B
{
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = A(Of B).F()
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            comp2.VerifyEmitDiagnostics()
            Dim tree = comp2.SyntaxTrees(0)
            Dim model = comp2.GetSemanticModel(tree)
            Dim syntax = tree.GetRoot().DescendantNodes().OfType(Of Syntax.VariableDeclaratorSyntax).Single().Names(0)
            Dim symbol = DirectCast(model.GetDeclaredSymbol(syntax), LocalSymbol)
            Assert.Equal("o As B", symbol.ToTestDisplayString())
            Assert.False(symbol.IsByRef)
        End Sub

        <Fact()>
        Public Sub ArrayAccess()
            Dim comp1 = CreateCSharpCompilation(
"public class C
{
    public static ref T F<T>(T[] t, int i)
    {
        return ref t[i];
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim a = {1, 2, 3}
        C.F(a, 2) *= 2
        For Each o in a
            System.Console.Write(""{0} "", o)
        Next
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="1 2 6")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (Integer() V_0, //a
                Integer& V_1,
                Integer() V_2,
                Integer V_3,
                Integer V_4, //o
                Boolean V_5)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     "Integer"
  IL_0007:  dup
  IL_0008:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D"
  IL_000d:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.2
  IL_0015:  call       "ByRef Function C.F(Of Integer)(Integer(), Integer) As Integer"
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  ldind.i4
  IL_001e:  ldc.i4.2
  IL_001f:  mul.ovf
  IL_0020:  stind.i4
  IL_0021:  ldloc.0
  IL_0022:  stloc.2
  IL_0023:  ldc.i4.0
  IL_0024:  stloc.3
  IL_0025:  br.s       IL_0043
  IL_0027:  ldloc.2
  IL_0028:  ldloc.3
  IL_0029:  ldelem.i4
  IL_002a:  stloc.s    V_4
  IL_002c:  ldstr      "{0} "
  IL_0031:  ldloc.s    V_4
  IL_0033:  box        "Integer"
  IL_0038:  call       "Sub System.Console.Write(String, Object)"
  IL_003d:  nop
  IL_003e:  nop
  IL_003f:  ldloc.3
  IL_0040:  ldc.i4.1
  IL_0041:  add.ovf
  IL_0042:  stloc.3
  IL_0043:  ldloc.3
  IL_0044:  ldloc.2
  IL_0045:  ldlen
  IL_0046:  conv.i4
  IL_0047:  clt
  IL_0049:  stloc.s    V_5
  IL_004b:  ldloc.s    V_5
  IL_004d:  brtrue.s   IL_0027
  IL_004f:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub [Delegate]()
            Dim comp1 = CreateCSharpCompilation(
"public delegate ref T D<T>();
public class C<T>
{
    public T F;
    public ref T G()
    {
        return ref F;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        Dim d As D(Of Integer) = AddressOf o.G
        d() = 2
        System.Console.Write(o.F)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="2")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (C(Of Integer) V_0, //o
                D(Of Integer) V_1) //d
  IL_0000:  nop
  IL_0001:  newobj     "Sub C(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      "ByRef Function C(Of Integer).G() As Integer"
  IL_000e:  newobj     "Sub D(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  callvirt   "ByRef Function D(Of Integer).Invoke() As Integer"
  IL_001a:  ldc.i4.2
  IL_001b:  stind.i4
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "C(Of Integer).F As Integer"
  IL_0022:  call       "Sub System.Console.Write(Integer)"
  IL_0027:  nop
  IL_0028:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' Locals should be values not references.
        ''' </summary>
        <Fact()>
        Public Sub Local()
            Dim comp1 = CreateCSharpCompilation(
"public class C
{
    public static ref T F<T>(ref T t)
    {
        return ref t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim x = 2
        Dim y = C.F(x)
        y = 3
        System.Console.Write(""{0} {1}"", x, y)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="2 3")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (Integer V_0, //x
                Integer V_1) //y
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "ByRef Function C.F(Of Integer)(ByRef Integer) As Integer"
  IL_000a:  ldind.i4
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.3
  IL_000d:  stloc.1
  IL_000e:  ldstr      "{0} {1}"
  IL_0013:  ldloc.0
  IL_0014:  box        "Integer"
  IL_0019:  ldloc.1
  IL_001a:  box        "Integer"
  IL_001f:  call       "Sub System.Console.Write(String, Object, Object)"
  IL_0024:  nop
  IL_0025:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub SharedPropertyAssignment()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private static T _p;
    public static ref T P
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim d = 1.5  ' must not be stack local
        C(Of Double).P = d
        C(Of Double).P = d  ' assign second time, should not be on stack
        C(Of Double).P += 2.0
        System.Console.Write(C(Of Double).P)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="3.5")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (Double V_0) //d
  IL_0000:  ldc.r8     1.5
  IL_0009:  stloc.0
  IL_000a:  call       "ByRef Function C(Of Double).get_P() As Double"
  IL_000f:  ldloc.0
  IL_0010:  stind.r8
  IL_0011:  call       "ByRef Function C(Of Double).get_P() As Double"
  IL_0016:  ldloc.0
  IL_0017:  stind.r8
  IL_0018:  call       "ByRef Function C(Of Double).get_P() As Double"
  IL_001d:  call       "ByRef Function C(Of Double).get_P() As Double"
  IL_0022:  ldind.r8
  IL_0023:  ldc.r8     2
  IL_002c:  add
  IL_002d:  stind.r8
  IL_002e:  call       "ByRef Function C(Of Double).get_P() As Double"
  IL_0033:  ldind.r8
  IL_0034:  call       "Sub System.Console.Write(Double)"
  IL_0039:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub PropertyAssignment()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T P
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        o.P = 1
        o.P += 2
        System.Console.Write(o.P)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="3")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (C(Of Integer) V_0, //o
                C(Of Integer) V_1)
  IL_0000:  nop
  IL_0001:  newobj     "Sub C(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_000d:  ldc.i4.1
  IL_000e:  stind.i4
  IL_000f:  ldloc.0
  IL_0010:  dup
  IL_0011:  stloc.1
  IL_0012:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_0017:  ldloc.1
  IL_0018:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_001d:  ldind.i4
  IL_001e:  ldc.i4.2
  IL_001f:  add.ovf
  IL_0020:  stind.i4
  IL_0021:  ldloc.0
  IL_0022:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_0027:  ldind.i4
  IL_0028:  call       "Sub System.Console.Write(Integer)"
  IL_002d:  nop
  IL_002e:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub DefaultPropertyAssignment()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T[] _p = new T[10];
    public ref T this[int index]
    {
        get { return ref _p[index]; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        o(2) = 1
        o(2) += 2
        System.Console.Write(o(2))
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="3")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (C(Of Integer) V_0, //o
                C(Of Integer) V_1)
  IL_0000:  nop
  IL_0001:  newobj     "Sub C(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.2
  IL_0009:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_000e:  ldc.i4.1
  IL_000f:  stind.i4
  IL_0010:  ldloc.0
  IL_0011:  dup
  IL_0012:  stloc.1
  IL_0013:  ldc.i4.2
  IL_0014:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_0019:  ldloc.1
  IL_001a:  ldc.i4.2
  IL_001b:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_0020:  ldind.i4
  IL_0021:  ldc.i4.2
  IL_0022:  add.ovf
  IL_0023:  stind.i4
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.2
  IL_0026:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_002b:  ldind.i4
  IL_002c:  call       "Sub System.Console.Write(Integer)"
  IL_0031:  nop
  IL_0032:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub PropertyArgument()
            Dim comp1 = CreateCSharpCompilation(
"public class A<T>
{
    public A(T p)
    {
        _p = p;
    }
#pragma warning disable 0649
    private T _p;
    public ref T P
    {
        get { return ref _p; }
    }
}
public class B
{
    public static void F(ref int i)
    {
        i *= 2;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim x = New A(Of Integer)(1)
        Dim y = New A(Of Byte)(2)
        B.F(x.P) ' No conversion, passed by ref
        B.F(y.P) ' Widening conversion, passed by value with copy-back
        System.Console.Write(""{0} {1}"", x.P, y.P)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="2 4")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (A(Of Integer) V_0, //x
                A(Of Byte) V_1, //y
                A(Of Byte) V_2,
                Integer V_3)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     "Sub A(Of Integer)..ctor(Integer)"
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     "Sub A(Of Byte)..ctor(Byte)"
  IL_000e:  stloc.1
  IL_000f:  ldloc.0
  IL_0010:  callvirt   "ByRef Function A(Of Integer).get_P() As Integer"
  IL_0015:  call       "Sub B.F(ByRef Integer)"
  IL_001a:  nop
  IL_001b:  ldloc.1
  IL_001c:  dup
  IL_001d:  stloc.2
  IL_001e:  callvirt   "ByRef Function A(Of Byte).get_P() As Byte"
  IL_0023:  ldind.u1
  IL_0024:  stloc.3
  IL_0025:  ldloca.s   V_3
  IL_0027:  call       "Sub B.F(ByRef Integer)"
  IL_002c:  nop
  IL_002d:  ldloc.2
  IL_002e:  callvirt   "ByRef Function A(Of Byte).get_P() As Byte"
  IL_0033:  ldloc.3
  IL_0034:  conv.ovf.u1
  IL_0035:  stind.i1
  IL_0036:  ldstr      "{0} {1}"
  IL_003b:  ldloc.0
  IL_003c:  callvirt   "ByRef Function A(Of Integer).get_P() As Integer"
  IL_0041:  ldind.i4
  IL_0042:  box        "Integer"
  IL_0047:  ldloc.1
  IL_0048:  callvirt   "ByRef Function A(Of Byte).get_P() As Byte"
  IL_004d:  ldind.u1
  IL_004e:  box        "Byte"
  IL_0053:  call       "Sub System.Console.Write(String, Object, Object)"
  IL_0058:  nop
  IL_0059:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' Setter of read/write property should be ignored.
        ''' </summary>
        <Fact()>
        Public Sub ReadWriteProperty()
            Dim ilSource = <![CDATA[
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .field private int32 _f
  .method public instance int32& F()
  {
    ldarg.0
    ldflda int32 C::_f
    ret
  }
  .field private int32 _p
  .method public instance int32& get_P()
  {
    ldarg.0
    ldflda int32 C::_p
    ret
  }
  .method public instance void set_P(int32& val)
  {
    ldnull
    throw
  }
  .property instance int32& P()
  {
    .get instance int32& C::get_P()
    .set instance void C::set_P(int32& val)
  }
}]]>.Value
            Dim ref1 = CompileIL(ilSource)
            Dim comp = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C()
        o.F() = 1
        o.P = o.F()
        o.P += 2
        System.Console.Write(""{0}, {1}"", o.F(), o.P)
    End Sub
End Module",
                referencedAssemblies:={MscorlibRef, SystemRef, MsvbRef, ref1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp, expectedOutput:="1, 3")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (C V_0, //o
                C V_1)
  IL_0000:  nop
  IL_0001:  newobj     "Sub C..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   "ByRef Function C.F() As Integer"
  IL_000d:  ldc.i4.1
  IL_000e:  stind.i4
  IL_000f:  ldloc.0
  IL_0010:  callvirt   "ByRef Function C.get_P() As Integer"
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "ByRef Function C.F() As Integer"
  IL_001b:  ldind.i4
  IL_001c:  stind.i4
  IL_001d:  ldloc.0
  IL_001e:  dup
  IL_001f:  stloc.1
  IL_0020:  callvirt   "ByRef Function C.get_P() As Integer"
  IL_0025:  ldloc.1
  IL_0026:  callvirt   "ByRef Function C.get_P() As Integer"
  IL_002b:  ldind.i4
  IL_002c:  ldc.i4.2
  IL_002d:  add.ovf
  IL_002e:  stind.i4
  IL_002f:  ldstr      "{0}, {1}"
  IL_0034:  ldloc.0
  IL_0035:  callvirt   "ByRef Function C.F() As Integer"
  IL_003a:  ldind.i4
  IL_003b:  box        "Integer"
  IL_0040:  ldloc.0
  IL_0041:  callvirt   "ByRef Function C.get_P() As Integer"
  IL_0046:  ldind.i4
  IL_0047:  box        "Integer"
  IL_004c:  call       "Sub System.Console.Write(String, Object, Object)"
  IL_0051:  nop
  IL_0052:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' Setter of read/write property should be ignored,
        ''' even if mismatched signature.
        ''' </summary>
        <Fact()>
        Public Sub ReadWriteProperty_DifferentSignatures()
            Dim ilSource = <![CDATA[
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .field private object _p
  .method public instance object& get_P()
  {
    ldarg.0
    ldflda object C::_p
    ret
  }
  .method public instance void set_P(object v)
  {
    ldnull
    throw
  }
  .property instance object& P()
  {
    .get instance object& C::get_P()
    .set instance void C::set_P(object)
  }
  .field private object _q
  .method public instance object& get_Q(object i)
  {
    ldarg.0
    ldflda object C::_q
    ret
  }
  .method public instance void set_Q(object i, object v)
  {
    ldnull
    throw
  }
  .property instance object& Q(object)
  {
    .get instance object& C::get_Q(object)
    .set instance void C::set_Q(object, object)
  }
}]]>.Value
            Dim ref1 = CompileIL(ilSource)
            Dim comp = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C()
        o.P = 1
        o.Q(1) = 2
        System.Console.Write(""{0}, {1}"", o.P, o.Q(1))
    End Sub
End Module",
                referencedAssemblies:={MscorlibRef, SystemRef, MsvbRef, ref1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp, expectedOutput:="1, 2")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       81 (0x51)
  .maxstack  4
  .locals init (C V_0) //o
  IL_0000:  nop
  IL_0001:  newobj     "Sub C..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   "ByRef Function C.get_P() As Object"
  IL_000d:  ldc.i4.1
  IL_000e:  box        "Integer"
  IL_0013:  stind.ref
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  box        "Integer"
  IL_001b:  callvirt   "ByRef Function C.get_Q(Object) As Object"
  IL_0020:  ldc.i4.2
  IL_0021:  box        "Integer"
  IL_0026:  stind.ref
  IL_0027:  ldstr      "{0}, {1}"
  IL_002c:  ldloc.0
  IL_002d:  callvirt   "ByRef Function C.get_P() As Object"
  IL_0032:  ldind.ref
  IL_0033:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.1
  IL_003a:  box        "Integer"
  IL_003f:  callvirt   "ByRef Function C.get_Q(Object) As Object"
  IL_0044:  ldind.ref
  IL_0045:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004a:  call       "Sub System.Console.Write(String, Object, Object)"
  IL_004f:  nop
  IL_0050:  ret
}
]]>)
            verifier.VerifyDiagnostics()
            Dim p = comp.GetMember(Of PropertySymbol)("C.P")
            Assert.True(p.ReturnsByRef)
            Assert.Equal("ByRef Property C.P As System.Object", p.ToTestDisplayString())
            Dim q = comp.GetMember(Of PropertySymbol)("C.Q")
            Assert.True(q.ReturnsByRef)
            Assert.Equal("ByRef Property C.Q(i As System.Object) As System.Object", q.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub Implement()
            Dim comp1 = CreateCSharpCompilation(
"public interface I
{
    ref object F();
    ref object P { get; }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Class C
    Implements I
    Public Function F() As Object Implements I.F
        Return Nothing
    End Function
    Public ReadOnly Property P As Object Implements I.P
        Get
            Return Nothing
        End Get
    End Property
End Class",
                referencedCompilations:={comp1})
            comp2.AssertTheseDiagnostics(
<error><![CDATA[
BC30149: Class 'C' must implement 'ByRef Function F() As Object' for interface 'I'.
    Implements I
               ~
BC30149: Class 'C' must implement 'ReadOnly ByRef Property P As Object' for interface 'I'.
    Implements I
               ~
BC30401: 'F' cannot implement 'F' because there is no matching function on interface 'I'.
    Public Function F() As Object Implements I.F
                                             ~~~
BC30401: 'P' cannot implement 'P' because there is no matching property on interface 'I'.
    Public ReadOnly Property P As Object Implements I.P
                                                    ~~~
]]></error>)
        End Sub

        <Fact()>
        Public Sub Override()
            Dim comp1 = CreateCSharpCompilation(
"public abstract class A
{
    public abstract ref object F();
    public abstract ref object P { get; }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"MustInherit Class B
    Inherits A
    Public Overrides Function F() As Object
        Return Nothing
    End Function
    Public Overrides ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property
End Class",
                referencedCompilations:={comp1})
            comp2.AssertTheseDiagnostics(
<error><![CDATA[
BC30437: 'Public Overrides Function F() As Object' cannot override 'Public MustOverride Overloads ByRef Function F() As Object' because they differ by their return types.
    Public Overrides Function F() As Object
                              ~
BC30437: 'Public Overrides ReadOnly Property P As Object' cannot override 'Public MustOverride Overloads ReadOnly ByRef Property P As Object' because they differ by their return types.
    Public Overrides ReadOnly Property P As Object
                                       ~
]]></error>)
        End Sub

        <Fact()>
        Public Sub Override_Metadata()
            Dim ilSource = <![CDATA[
.class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance object F() { ldnull throw }
  .method public virtual instance object& get_P() { ldnull throw }
  .property instance object& P()
  {
    .get instance object& A::get_P()
  }
}
.class public abstract B1 extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance object F() { ldnull throw }
  .method public virtual instance object get_P() { ldnull throw }
  .property instance object P()
  {
    .get instance object B1::get_P()
  }
}
.class public abstract B2 extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance object& F() { ldnull throw }
  .method public virtual instance object& get_P() { ldnull throw }
  .property instance object& P()
  {
    .get instance object& B2::get_P()
  }
}]]>.Value
            Dim ref1 = CompileIL(ilSource)
            Dim comp = CreateVisualBasicCompilation(
                Nothing,
                "",
                referencedAssemblies:={MscorlibRef, SystemRef, MsvbRef, ref1},
                compilationOptions:=TestOptions.DebugDll)

            Dim method = comp.GetMember(Of MethodSymbol)("B1.F")
            Assert.Equal("Function B1.F() As System.Object", method.ToTestDisplayString())
            Assert.Equal("Function A.F() As System.Object", method.OverriddenMethod.ToTestDisplayString())

            Dim [property] = comp.GetMember(Of PropertySymbol)("B1.P")
            Assert.Equal("ReadOnly Property B1.P As System.Object", [property].ToTestDisplayString())
            Assert.Null([property].OverriddenProperty)

            method = comp.GetMember(Of MethodSymbol)("B2.F")
            Assert.Equal("ByRef Function B2.F() As System.Object", method.ToTestDisplayString())
            Assert.Null(method.OverriddenMethod)

            [property] = comp.GetMember(Of PropertySymbol)("B2.P")
            Assert.Equal("ReadOnly ByRef Property B2.P As System.Object", [property].ToTestDisplayString())
            Assert.Equal("ReadOnly ByRef Property A.P As System.Object", [property].OverriddenProperty.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub ExpressionLambdas_01()
            Dim comp1 = CreateCSharpCompilation(
"public class A<T>
{
#pragma warning disable 0649
    private static T _f;
    public static ref T F()
    {
        return ref _f;
    }
    private T _p;
    public ref T P
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Imports System
Imports System.Linq.Expressions
Module M
    Sub Main()
        Dim e As Expression(Of Action) = Sub() M(A(Of Integer).F())
        Dim f As Expression(Of Action) = Sub() M(New A(Of Integer)().P)
    End Sub
    Sub M(i As Integer)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            comp2.AssertTheseEmitDiagnostics(
<error><![CDATA[
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        Dim e As Expression(Of Action) = Sub() M(A(Of Integer).F())
                                                 ~~~~~~~~~~~~~~~~~
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        Dim f As Expression(Of Action) = Sub() M(New A(Of Integer)().P)
                                                 ~~~~~~~~~~~~~~~~~~~~~
]]></error>)
        End Sub

        <Fact()>
        <WorkItem(49617, "https://github.com/dotnet/roslyn/issues/49617")>
        Public Sub ExpressionLambdas_02()
            Dim comp1 = CreateCSharpCompilation(
"
public class Model
{
    int value;
    public ref int Value => ref value;
}
")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
Imports System
Imports System.Linq.Expressions
Module M
    Sub Main()
        TestExpression(Function(m) m.Value = 1)
    End Sub

    Sub TestExpression(expression As Expression(Of Action(Of Model)))
    End Sub
End Module
",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            comp2.AssertTheseEmitDiagnostics(
<error><![CDATA[
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        TestExpression(Function(m) m.Value = 1)
                                   ~~~~~~~
]]></error>)
        End Sub

        <Fact()>
        <WorkItem(49617, "https://github.com/dotnet/roslyn/issues/49617")>
        Public Sub ExpressionLambdas_03()
            Dim comp1 = CreateCSharpCompilation(
"
public class Model
{
    int value;
    public ref int Value => ref value;
}
")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
Imports System
Imports System.Linq.Expressions
Module M
    Sub Main()
        TestExpression(Function() new Model With { .Value = 1 })
    End Sub

    Sub TestExpression(expression As Expression(Of Func(Of Model)))
    End Sub
End Module
",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            comp2.AssertTheseEmitDiagnostics(
<error><![CDATA[
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        TestExpression(Function() new Model With { .Value = 1 })
                                                    ~~~~~
]]></error>)
        End Sub

        <Fact()>
        <WorkItem(49617, "https://github.com/dotnet/roslyn/issues/49617")>
        Public Sub ExpressionLambdas_04()
            Dim comp1 = CreateCSharpCompilation(
"
public class Model : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => throw null;
    public ref bool Add(int x) => throw null;
}
")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
Imports System
Imports System.Linq.Expressions
Module M
    Sub Main()
        TestExpression(Function() new Model From { 1, 2, 3 })
    End Sub

    Sub TestExpression(expression As Expression(Of Func(Of Model)))
    End Sub
End Module
",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            comp2.AssertTheseEmitDiagnostics(
<error><![CDATA[
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        TestExpression(Function() new Model From { 1, 2, 3 })
                                                   ~
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        TestExpression(Function() new Model From { 1, 2, 3 })
                                                      ~
BC37263: An expression tree may not contain a call to a method or property that returns by reference.
        TestExpression(Function() new Model From { 1, 2, 3 })
                                                         ~
]]></error>)
        End Sub

        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub MidAssignment()
            Dim comp1 = CreateCSharpCompilation(
"public class C
{
#pragma warning disable 0649
    private string _p = ""abcd"";
    public ref string P
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C()
        Mid(o.P, 2, 2) = ""efg""
        System.Console.Write(o.P)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="aefd")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (C V_0) //o
  IL_0000:  nop
  IL_0001:  newobj     "Sub C..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   "ByRef Function C.get_P() As String"
  IL_000d:  ldc.i4.2
  IL_000e:  ldc.i4.2
  IL_000f:  ldstr      "efg"
  IL_0014:  call       "Sub Microsoft.VisualBasic.CompilerServices.StringType.MidStmtStr(ByRef String, Integer, Integer, String)"
  IL_0019:  nop
  IL_001a:  ldloc.0
  IL_001b:  callvirt   "ByRef Function C.get_P() As String"
  IL_0020:  ldind.ref
  IL_0021:  call       "Sub System.Console.Write(String)"
  IL_0026:  nop
  IL_0027:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' Early-bound calls to ByRef-returning methods
        ''' supported as arguments to late-bound methods.
        ''' </summary>
        <Fact()>
        Public Sub RefReturnArgumentToLateBoundCall()
            Dim comp1 = CreateCSharpCompilation(
"public class A
{
#pragma warning disable 0649
    private string _f;
    public ref string F()
    {
        return ref _f;
    }
    private string _g;
    public ref string G()
    {
        return ref _g;
    }
}
public class B
{
    public void F(string a, ref string b)
    {
        a = a.ToLower();
        b = b.ToLower();
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim a = New A()
        a.F() = ""ABC""
        a.G() = ""DEF""
        F(New B(), a)
        System.Console.Write(a.F() + a.G())
    End Sub
    Sub F(b As Object, a As A)
        b.F(a.F(), a.G())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="ABCdef")
            verifier.VerifyIL("M.F",
            <![CDATA[
{
  // Code size      140 (0x8c)
  .maxstack  10
  .locals init (String& V_0,
                String& V_1,
                Object() V_2,
                Boolean() V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  ldstr      "F"
  IL_0008:  ldc.i4.2
  IL_0009:  newarr     "Object"
  IL_000e:  dup
  IL_000f:  ldc.i4.0
  IL_0010:  ldarg.1
  IL_0011:  callvirt   "ByRef Function A.F() As String"
  IL_0016:  dup
  IL_0017:  stloc.0
  IL_0018:  ldind.ref
  IL_0019:  stelem.ref
  IL_001a:  dup
  IL_001b:  ldc.i4.1
  IL_001c:  ldarg.1
  IL_001d:  callvirt   "ByRef Function A.G() As String"
  IL_0022:  dup
  IL_0023:  stloc.1
  IL_0024:  ldind.ref
  IL_0025:  stelem.ref
  IL_0026:  dup
  IL_0027:  stloc.2
  IL_0028:  ldnull
  IL_0029:  ldnull
  IL_002a:  ldc.i4.2
  IL_002b:  newarr     "Boolean"
  IL_0030:  dup
  IL_0031:  ldc.i4.0
  IL_0032:  ldc.i4.1
  IL_0033:  stelem.i1
  IL_0034:  dup
  IL_0035:  ldc.i4.1
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  dup
  IL_0039:  stloc.3
  IL_003a:  ldc.i4.1
  IL_003b:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0040:  pop
  IL_0041:  ldloc.3
  IL_0042:  ldc.i4.0
  IL_0043:  ldelem.u1
  IL_0044:  brtrue.s   IL_0048
  IL_0046:  br.s       IL_0066
  IL_0048:  ldloc.0
  IL_0049:  ldloc.2
  IL_004a:  ldc.i4.0
  IL_004b:  ldelem.ref
  IL_004c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0051:  ldtoken    "String"
  IL_0056:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_005b:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_0060:  castclass  "String"
  IL_0065:  stind.ref
  IL_0066:  ldloc.3
  IL_0067:  ldc.i4.1
  IL_0068:  ldelem.u1
  IL_0069:  brtrue.s   IL_006d
  IL_006b:  br.s       IL_008b
  IL_006d:  ldloc.1
  IL_006e:  ldloc.2
  IL_006f:  ldc.i4.1
  IL_0070:  ldelem.ref
  IL_0071:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0076:  ldtoken    "String"
  IL_007b:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0080:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_0085:  castclass  "String"
  IL_008a:  stind.ref
  IL_008b:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' Late-bound calls with ByRef return values not supported.
        ''' </summary>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Sub RefReturnLateBoundCall()
            Dim comp1 = CreateCSharpCompilation(
"public class A
{
#pragma warning disable 0649
    private string _f = ""ABC"";
    public string F()
    {
        return _f;
    }
    private string _g = ""DEF"";
    public ref string G()
    {
        return ref _g;
    }
}
public class B
{
    public void F(string a, ref string b)
    {
        a = a.ToLower();
        b = b.ToLower();
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim a = New A()
        Dim saveCulture = System.Threading.Thread.CurrentThread.CurrentCulture
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            F(New B(), a)
        Catch e As System.Exception
            System.Console.Write(e.Message)
        Finally
            System.Threading.Thread.CurrentThread.CurrentCulture = saveCulture
        End Try
    End Sub
    Sub F(b As B, a As Object)
        b.F(a.F(), a.G())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(comp2, expectedOutput:="Public member 'G' on type 'A' not found.")
            verifier.VerifyIL("M.F",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  9
  .locals init (Object V_0,
                String V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldnull
  IL_0004:  ldstr      "F"
  IL_0009:  ldc.i4.0
  IL_000a:  newarr     "Object"
  IL_000f:  ldnull
  IL_0010:  ldnull
  IL_0011:  ldnull
  IL_0012:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_001c:  ldarg.1
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  ldnull
  IL_0020:  ldstr      "G"
  IL_0025:  ldc.i4.0
  IL_0026:  newarr     "Object"
  IL_002b:  ldnull
  IL_002c:  ldnull
  IL_002d:  ldnull
  IL_002e:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0033:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0038:  stloc.1
  IL_0039:  ldloca.s   V_1
  IL_003b:  callvirt   "Sub B.F(String, ByRef String)"
  IL_0040:  nop
  IL_0041:  ldloc.0
  IL_0042:  ldnull
  IL_0043:  ldstr      "G"
  IL_0048:  ldc.i4.1
  IL_0049:  newarr     "Object"
  IL_004e:  dup
  IL_004f:  ldc.i4.0
  IL_0050:  ldloc.1
  IL_0051:  stelem.ref
  IL_0052:  ldnull
  IL_0053:  ldnull
  IL_0054:  ldc.i4.1
  IL_0055:  ldc.i4.0
  IL_0056:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSetComplex(Object, System.Type, String, Object(), String(), System.Type(), Boolean, Boolean)"
  IL_005b:  nop
  IL_005c:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub InLambda()
            Dim comp1 = CreateCSharpCompilation(
"public class C
{
    public static ref T F<T>(ref T t)
    {
        return ref t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim f = Sub(ByRef x As Integer, y As Integer) C.F(x) = y
        Dim o = 2
        f(o, 3)
        System.Console.Write(o)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="3")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer, Integer) V_0, //f
                Integer V_1) //o
  IL_0000:  nop
  IL_0001:  ldsfld     "M._Closure$__.$I0-0 As <generated method>"
  IL_0006:  brfalse.s  IL_000f
  IL_0008:  ldsfld     "M._Closure$__.$I0-0 As <generated method>"
  IL_000d:  br.s       IL_0025
  IL_000f:  ldsfld     "M._Closure$__.$I As M._Closure$__"
  IL_0014:  ldftn      "Sub M._Closure$__._Lambda$__0-0(ByRef Integer, Integer)"
  IL_001a:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_001f:  dup
  IL_0020:  stsfld     "M._Closure$__.$I0-0 As <generated method>"
  IL_0025:  stloc.0
  IL_0026:  ldc.i4.2
  IL_0027:  stloc.1
  IL_0028:  ldloc.0
  IL_0029:  ldloca.s   V_1
  IL_002b:  ldc.i4.3
  IL_002c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer, Integer).Invoke(ByRef Integer, Integer)"
  IL_0031:  nop
  IL_0032:  ldloc.1
  IL_0033:  call       "Sub System.Console.Write(Integer)"
  IL_0038:  nop
  IL_0039:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub LambdaToByRefDelegate()
            Dim comp1 = CreateCSharpCompilation(
"public delegate ref T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)()
        B.F(Function() o.F(), 2)

        Dim d = Function() o.F()
        B.F(d, 2)
        System.Console.Write(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36532: Nested function does not have the same signature as delegate 'D(Of Integer)'.
        B.F(Function() o.F(), 2)
            ~~~~~~~~~~~~~~~~
BC30311: Value of type 'Function <generated method>() As Integer' cannot be converted to 'D(Of Integer)'.
        B.F(d, 2)
            ~
]]></expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub LambdaCallingByRefFunction()
            Dim comp1 = CreateCSharpCompilation(
"public delegate T D<T>();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d)
    {
        System.Console.WriteLine(d());
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of String)(""13206"")
        B.F(Function() o.F())

        Dim d = Function() o.F()
        B.F(d)
        System.Console.WriteLine(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"13206
13206
13206")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub LambdaCallingByRefFunctionDifferentType()
            Dim comp1 = CreateCSharpCompilation(
"public delegate string D();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F(D d)
    {
        System.Console.WriteLine(d());
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)(13206)
        B.F(Function() o.F())

        Dim d = Function() o.F()
        B.F(d)
        System.Console.WriteLine(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"13206
13206
13206")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub LambdaCallingByRefFunctionDropReturn()
            Dim comp1 = CreateCSharpCompilation(
"public delegate void D();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }
    public ref T F()
    {
        System.Console.WriteLine(""A.F"");
        return ref _t;
    }
}
public class B
{
    public static void F(D d)
    {
        System.Console.WriteLine(""B.F"");
        d();
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)(13206)
        B.F(Function() o.F())

        Dim d = Function() o.F()
        B.F(d)
        System.Console.WriteLine(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"B.F
A.F
B.F
A.F
A.F
13206")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub LambdaCallingByRefFunctionKeepingVsDroppingByRef()
            Dim comp1 = CreateCSharpCompilation(
"
public delegate T D1<T>();
public delegate ref T D2<T>();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D1<T> d, T t)
    {
        System.Console.WriteLine(""D1"");
        d();
    }
    public static void F<T>(D2<T> d, T t)
    {
        System.Console.WriteLine(""D2"");
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)(13206)
        B.F(Function() o.F, 1)
        Dim d = Function() o.F
        B.F(d, 2)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"D1
D1")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunction()
            Dim comp1 = CreateCSharpCompilation(
"public delegate ref T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)()
        B.F(AddressOf o.F, 2)
        System.Console.Write(o.F())
        B.F(New D(Of Integer)(AddressOf o.F), 3)
        System.Console.Write(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="23")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionDropArguments()
            Dim comp1 = CreateCSharpCompilation(
"public delegate ref T D<T>(int x);
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d(1) = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)()
        B.F(AddressOf o.F, 2)
        System.Console.Write(o.F())
        B.F(New D(Of Integer)(AddressOf o.F), 3)
        System.Console.Write(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of Integer)(x As Integer) As Integer'.
        B.F(AddressOf o.F, 2)
                      ~~~
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of Integer)(x As Integer) As Integer'.
        B.F(New D(Of Integer)(AddressOf o.F), 3)
                                        ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionDropArgumentsAndByRef()
            Dim comp1 = CreateCSharpCompilation(
"public delegate T D<T>(int x);
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        System.Console.WriteLine(""A.F"");
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        System.Console.WriteLine(""B.F"");
        d(1);
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)()
        B.F(AddressOf o.F, 2)
        B.F(New D(Of Integer)(AddressOf o.F), 3)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate Function D(Of Integer)(x As Integer) As Integer'.
        B.F(AddressOf o.F, 2)
                      ~~~
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate Function D(Of Integer)(x As Integer) As Integer'.
        B.F(New D(Of Integer)(AddressOf o.F), 3)
                                        ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionDropByRef()
            Dim comp1 = CreateCSharpCompilation(
"public delegate T D<T>();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d)
    {
        System.Console.WriteLine(d());
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of String)(13206)
        B.F(AddressOf o.F)
        System.Console.WriteLine(o.F())
        o = New A(Of String)(13207)
        B.F(New D(Of Integer)(AddressOf o.F))
        System.Console.WriteLine(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As String' does not have a signature compatible with delegate 'Delegate Function D(Of String)() As String'.
        B.F(AddressOf o.F)
                      ~~~
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub F(Of T)(d As D(Of T))' cannot be inferred.
        B.F(New D(Of Integer)(AddressOf o.F))
          ~
BC31143: Method 'Public Overloads ByRef Function F() As String' does not have a signature compatible with delegate 'Delegate Function D(Of Integer)() As Integer'.
        B.F(New D(Of Integer)(AddressOf o.F))
                                        ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionDropReturn()
            Dim comp1 = CreateCSharpCompilation(
"public delegate void D();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public ref T F()
    {
        System.Console.WriteLine(""A.F"");
        return ref _t;
    }
}
public class B
{
    public static void F(D d)
    {
        System.Console.WriteLine(""B.F"");
        d();
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of String)(13206)
        B.F(AddressOf o.F)
        B.F(New D(AddressOf o.F))
        System.Console.Write(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As String' does not have a signature compatible with delegate 'Delegate Sub D()'.
        B.F(AddressOf o.F)
                      ~~~
BC31143: Method 'Public Overloads ByRef Function F() As String' does not have a signature compatible with delegate 'Delegate Sub D()'.
        B.F(New D(AddressOf o.F))
                            ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionDropByRefDifferentType()
            Dim comp1 = CreateCSharpCompilation(
"public delegate string D();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F(D d)
    {
        System.Console.WriteLine(d());
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)(13206)
        B.F(AddressOf o.F)
        System.Console.WriteLine(o.F())
        o = New A(Of Integer)(13207)
        B.F(New D(AddressOf o.F))
        System.Console.WriteLine(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate Function D() As String'.
        B.F(AddressOf o.F)
                      ~~~
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate Function D() As String'.
        B.F(New D(AddressOf o.F))
                            ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateAddByRef()
            Dim comp1 = CreateCSharpCompilation(
"public delegate ref T D<T>();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public T F()
    {
        return _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of String)(13206)
        B.F(AddressOf o.F, ""1"")
        System.Console.Write(o.F())
        o = New A(Of String)(13207)
        B.F(New D(Of Integer)(AddressOf o.F), ""2"")
        System.Console.Write(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads Function F() As String' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of String)() As String'.
        B.F(AddressOf o.F, "1")
                      ~~~
BC31143: Method 'Public Overloads Function F() As String' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of Integer)() As Integer'.
        B.F(New D(Of Integer)(AddressOf o.F), "2")
                                        ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionWithDifferentType()
            Dim comp1 = CreateCSharpCompilation(
"public delegate ref string D();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D d, T t)
    {
        d() = t.ToString();
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)()
        B.F(AddressOf o.F, 2)
        System.Console.Write(o.F())
        B.F(New D(AddressOf o.F), 3)
        System.Console.Write(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate ByRef Function D() As String'.
        B.F(AddressOf o.F, 2)
                      ~~~
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate ByRef Function D() As String'.
        B.F(New D(AddressOf o.F), 3)
                            ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub DelegateToByRefFunctionWithDerivedType()
            Dim comp1 = CreateCSharpCompilation(
"
public delegate ref T D<T>();

public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of String)()

        B.F(Of Object)(AddressOf o.F, new Object)
        System.Console.Write(o.F())

        B.F(Of Object)(New D(of Object)(AddressOf o.F), Nothing)
        System.Console.Write(o.F())

    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
    BC31143: Method 'Public Overloads ByRef Function F() As String' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of Object)() As Object'.
        B.F(Of Object)(AddressOf o.F, new Object)
                                 ~~~
BC31143: Method 'Public Overloads ByRef Function F() As String' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of Object)() As Object'.
        B.F(Of Object)(New D(of Object)(AddressOf o.F), Nothing)
                                                  ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub RefMethodGroupConversionError_WithResolution()
            Dim comp1 = CreateCSharpCompilation(
"
public class Base
{
    public static Base Instance = new Base();
}

public class Derived1 : Base
{
    public static new Derived1 Instance = new Derived1();
}

public class Derived2 : Derived1
{
}

public delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

public class Methods
{
    public static ref Base M1(Base arg) => ref Base.Instance;
    public static ref Derived1 M1(Derived1 arg) => ref Derived1.Instance;
}
")
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim f as RefFunc1(OF Derived2, Base) = AddressOf Methods.M1
        System.Console.WriteLine(f(Nothing))
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
    BC31143: Method 'Public Shared Overloads ByRef Function M1(arg As Derived1) As Derived1' does not have a signature compatible with delegate 'Delegate ByRef Function RefFunc1(Of Derived2, Base)(arg As Derived2) As Base'.
        Dim f as RefFunc1(OF Derived2, Base) = AddressOf Methods.M1
                                                         ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub RefMethodGroupConversionNoError_WithResolution()
            Dim comp1 = CreateCSharpCompilation(
"
public class Base
{
    public static Base Instance = new Base();
}

public class Derived1 : Base
{
    public static new Derived1 Instance = new Derived1();
}

public class Derived2 : Derived1
{
}

public delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

public class Methods
{
    public static ref Base M1(Base arg) => throw null;
    public static ref Base M1(Derived1 arg) => ref Base.Instance;
}
")
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim f as RefFunc1(of Derived2, Base) = AddressOf Methods.M1
        System.Console.WriteLine(f(Nothing))
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(comp2, expectedOutput:="Base")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub RefMethodGroupConversionNoError_WithResolution1()
            Dim comp1 = CreateCSharpCompilation(
"
public class Base
{
    public static Base Instance = new Base();
}

public class Derived1 : Base
{
    public static new Derived1 Instance = new Derived1();
}

public class Derived2 : Derived1
{
}

public delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

public class Methods
{
    public static ref Base M1(Derived1 arg) => ref Base.Instance;
    public static ref Base M3(Derived2 arg) => ref Base.Instance;

    public static void Test(RefFunc1<Derived2, Base> arg) => System.Console.WriteLine(arg);
    public static void Test(RefFunc1<Derived2, Derived1> arg) => System.Console.WriteLine(arg);}
")
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Methods.Test(AddressOf Methods.M1)
        Methods.Test(AddressOf Methods.M3)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(comp2, expectedOutput:="RefFunc1`2[Derived2,Base]
RefFunc1`2[Derived2,Base]")
            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub RefMethodGroupOverloadResolution()
            Dim comp1 = CreateCSharpCompilation(
"
public class Base
{
    public static Base Instance = new Base();
}

public class Derived1 : Base
{
    public static new Derived1 Instance = new Derived1();
}

public class Derived2 : Derived1
{
}

public delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

public class Methods
{
    public static ref Derived1 M2(Base arg) => ref Derived1.Instance;

    public static void Test(RefFunc1<Derived2, Base> arg) => System.Console.WriteLine(arg);
    public static void Test(RefFunc1<Derived2, Derived1> arg) => System.Console.WriteLine(arg);
}
")
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Methods.Test(AddressOf Methods.M2)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(comp2, expectedOutput:="RefFunc1`2[Derived2,Derived1]")
            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub RefLambdaOverloadResolution()
            Dim comp1 = CreateCSharpCompilation(
"
public class Base
{
    public static Base Instance = new Base();
}

public class Derived1 : Base
{
    public static new Derived1 Instance = new Derived1();
}

public class Derived2 : Derived1
{
}

public delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

public class Methods
{
    public static ref Derived1 M2(Base arg) => ref Derived1.Instance;

    public static void Test(RefFunc1<Derived1, Base> arg) => System.Console.WriteLine(arg);
    public static void Test(System.Func<Derived1, Base> arg) => System.Console.WriteLine(arg);
}
")
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Methods.Test(Function(t)Base.Instance)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(comp2, expectedOutput:="System.Func`2[Derived1,Base]")
            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        <WorkItem(17140, "https://github.com/dotnet/roslyn/issues/17140")>
        Public Sub DelegateToByRefFunctionWithBaseType()
            Dim comp1 = CreateCSharpCompilation(
"
public delegate ref T D<T>();

public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Object)()

        B.F(Of String)(AddressOf o.F, String.Empty)
        System.Console.Write(o.F())

        B.F(Of String)(New D(of String)(AddressOf o.F), Nothing)
        System.Console.Write(o.F())

    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
    BC31143: Method 'Public Overloads ByRef Function F() As Object' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of String)() As String'.
        B.F(Of String)(AddressOf o.F, String.Empty)
                                 ~~~
BC31143: Method 'Public Overloads ByRef Function F() As Object' does not have a signature compatible with delegate 'Delegate ByRef Function D(Of String)() As String'.
        B.F(Of String)(New D(of String)(AddressOf o.F), Nothing)
                                                  ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionKeepingVsDroppingByRef()
            Dim comp1 = CreateCSharpCompilation(
"
public delegate T D1<T>();
public delegate ref T D2<T>();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D1<T> d, T t)
    {
        System.Console.WriteLine(""D1"");
        d();
    }
    public static void F<T>(D2<T> d, T t)
    {
        System.Console.WriteLine(""D2"");
        d() = t;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)(13206)
        B.F(AddressOf o.F, 1)
        System.Console.WriteLine(o.F())
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"D2
1")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")>
        Public Sub DelegateToByRefFunctionDroppingByRefVsDroppingReturn()
            Dim comp1 = CreateCSharpCompilation(
"
public delegate T D1<T>();
public delegate void D2<T>();
public class A<T>
{
    private T _t;

    public A(T t)
    {
        _t = t;
    }

    public ref T F()
    {
        System.Console.WriteLine(""A.F"");
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D1<T> d)
    {
        System.Console.WriteLine(""D1"");
        d();
    }
    public static void F<T>(D2<T> d)
    {
        System.Console.WriteLine(""D2"");
        d();
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o As New A(Of Integer)(13206)
        B.F(AddressOf o.F)
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC31143: Method 'Public Overloads ByRef Function F() As Integer' does not have a signature compatible with delegate 'Delegate Function D1(Of Integer)() As Integer'.
        B.F(AddressOf o.F)
                      ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(17706, "https://github.com/dotnet/roslyn/issues/17706")>
        Public Sub SpillingByRefCall_NoSpilling()
            Dim comp1 = CreateCSharpCompilation(
"
using System;

public class TestClass
{
    int x = 0;

    public ref int Save(int y)
    {
        x = y;
        return ref x;
    }

    public void Write(ref int y)
    {
        Console.WriteLine(y);
    }

    public void Write(ref int y, int z)
    {
        Console.WriteLine(y);
    }
}")
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        TestMethod().Wait()
    End Sub

    Async Function TestMethod() As Task
        Dim inst = New TestClass

        ' this is OK. `ref` call is not spilled.
        ' prints: 10    (last value)
        inst.Write(inst.Save(Await Task.FromResult(10)))


        ' this is OK. `ref` call is not spilled.
        ' prints: 22    (last value)
        inst.Write(inst.Save(Await Task.FromResult(20)), inst.Save(22))
    End Function

End Module
",
                referencedCompilations:={comp1},
                referencedAssemblies:=LatestVbReferences,
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"
10
22
")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/24275")>
        <WorkItem(24275, "https://github.com/dotnet/roslyn/issues/24275")>
        Public Sub SpillingByRefCall_Spilling()
            Dim comp1 = CreateCSharpCompilation(
"
using System;

public class TestClass
{
    int x = 0;

    public ref int Save(int y)
    {
        x = y;
        return ref x;
    }

    public void Write(ref int y)
    {
        Console.WriteLine(y);
    }

    public void Write(ref int y, int z)
    {
        Console.WriteLine(y);
    }
}")
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        TestMethod().Wait()
    End Sub

    Async Function TestMethod() As Task
        Dim inst = New TestClass

        ' ERROR?
        ' currently `ref` is spilled 'by-value' and assert fires.
        inst.Write(inst.Save(Await Task.FromResult(30)), inst.Save(Await Task.FromResult(33)))
    End Function

End Module
",
                referencedCompilations:={comp1},
                referencedAssemblies:=LatestVbReferences,
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics()
            Dim verifier = CompileAndVerify(comp2, expectedOutput:=
"
??
")
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Method_01()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T GetP()
    {
        return ref _p;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        o.GetP() += 1
        With o.GetP()
            System.Console.Write(.ToString())
            o.GetP() = 2
            System.Console.Write(.ToString())
        End With
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="12")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (C(Of Integer) V_0, //o
                Integer& V_1,
                Integer& V_2) //$W0
  IL_0000:  nop
  IL_0001:  newobj     "Sub C(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   "ByRef Function C(Of Integer).GetP() As Integer"
  IL_000d:  dup
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  ldind.i4
  IL_0011:  ldc.i4.1
  IL_0012:  add.ovf
  IL_0013:  stind.i4
  IL_0014:  nop
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "ByRef Function C(Of Integer).GetP() As Integer"
  IL_001b:  stloc.2
  IL_001c:  ldloc.2
  IL_001d:  call       "Function Integer.ToString() As String"
  IL_0022:  call       "Sub System.Console.Write(String)"
  IL_0027:  nop
  IL_0028:  ldloc.0
  IL_0029:  callvirt   "ByRef Function C(Of Integer).GetP() As Integer"
  IL_002e:  ldc.i4.2
  IL_002f:  stind.i4
  IL_0030:  ldloc.2
  IL_0031:  call       "Function Integer.ToString() As String"
  IL_0036:  call       "Sub System.Console.Write(String)"
  IL_003b:  nop
  IL_003c:  nop
  IL_003d:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Method_02()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T GetP()
    {
        return ref _p;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
#disable warning BC42356

Module M
    async Sub Main()
        Dim o = New C(Of Integer)()
        With o.GetP()
        End With
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugDll)
            comp2.AssertTheseDiagnostics(
<expected>
BC37326: A call to a method or property that returns by reference may not be used as 'With' statement expression in an async or iterator method, or if referenced implicitly in a lambda.
        With o.GetP()
             ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Method_03()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T GetP()
    {
        return ref _p;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    iterator Function Main() As System.Collections.IEnumerable
        Dim o = New C(Of Integer)()
        With o.GetP()
        End With
    End Function
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugDll)
            comp2.AssertTheseDiagnostics(
<expected>
BC37326: A call to a method or property that returns by reference may not be used as 'With' statement expression in an async or iterator method, or if referenced implicitly in a lambda.
        With o.GetP()
             ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Method_04()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T GetP()
    {
        return ref _p;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"
#disable warning BC42356

Module M
    Sub Main()
        Dim f = async Sub()
                    Dim o = New C(Of Integer)()
                    With o.GetP()
                    End With
                End Sub
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugDll)
            comp2.AssertTheseDiagnostics(
<expected>
BC37326: A call to a method or property that returns by reference may not be used as 'With' statement expression in an async or iterator method, or if referenced implicitly in a lambda.
                    With o.GetP()
                         ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Method_05()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T GetP()
    {
        return ref _p;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim f = iterator Function() As System.Collections.IEnumerable
                    Dim o = New C(Of Integer)()
                    With o.GetP()
                    End With
                End Function
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugDll)
            comp2.AssertTheseDiagnostics(
<expected>
BC37326: A call to a method or property that returns by reference may not be used as 'With' statement expression in an async or iterator method, or if referenced implicitly in a lambda.
                    With o.GetP()
                         ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Method_06()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T GetP()
    {
        return ref _p;
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        With o.GetP()
            Dim f = Sub() System.Console.Write(.ToString())
        End With
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugDll)

            comp2.AssertTheseDiagnostics(
<expected>
BC37326: A call to a method or property that returns by reference may not be used as 'With' statement expression in an async or iterator method, or if referenced implicitly in a lambda.
        With o.GetP()
             ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Property_01()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T P
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        o.P += 1
        With o.P
            System.Console.Write(.ToString())
            o.P = 2
            System.Console.Write(.ToString())
        End With
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="12")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (C(Of Integer) V_0, //o
                C(Of Integer) V_1,
                Integer& V_2) //$W0
  IL_0000:  nop
  IL_0001:  newobj     "Sub C(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_000f:  ldloc.1
  IL_0010:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_0015:  ldind.i4
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  stind.i4
  IL_0019:  nop
  IL_001a:  ldloc.0
  IL_001b:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_0020:  stloc.2
  IL_0021:  ldloc.2
  IL_0022:  call       "Function Integer.ToString() As String"
  IL_0027:  call       "Sub System.Console.Write(String)"
  IL_002c:  nop
  IL_002d:  ldloc.0
  IL_002e:  callvirt   "ByRef Function C(Of Integer).get_P() As Integer"
  IL_0033:  ldc.i4.2
  IL_0034:  stind.i4
  IL_0035:  ldloc.2
  IL_0036:  call       "Function Integer.ToString() As String"
  IL_003b:  call       "Sub System.Console.Write(String)"
  IL_0040:  nop
  IL_0041:  nop
  IL_0042:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Property_02()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T P
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        With o.P
            Dim f = Sub() System.Console.Write(.ToString())
        End With
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC37326: A call to a method or property that returns by reference may not be used as 'With' statement expression in an async or iterator method, or if referenced implicitly in a lambda.
        With o.P
             ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/68194")>
        Public Sub With_Indexer()
            Dim comp1 = CreateCSharpCompilation(
"public class C<T>
{
#pragma warning disable 0649
    private T _p;
    public ref T this[int i]
    {
        get { return ref _p; }
    }
}")
            comp1.VerifyDiagnostics()
            Dim comp2 = CreateVisualBasicCompilation(
                Nothing,
"Module M
    Sub Main()
        Dim o = New C(Of Integer)()
        o(0) += 1
        With o(0)
            System.Console.Write(.ToString())
            o(0) = 2
            System.Console.Write(.ToString())
        End With
    End Sub
End Module",
                referencedCompilations:={comp1},
                compilationOptions:=TestOptions.DebugExe)
            Dim verifier = CompileAndVerify(comp2, expectedOutput:="12")
            verifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  3
  .locals init (C(Of Integer) V_0, //o
            C(Of Integer) V_1,
            Integer& V_2) //$W0
  IL_0000:  nop
  IL_0001:  newobj     "Sub C(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  ldc.i4.0
  IL_000b:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.0
  IL_0012:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_0017:  ldind.i4
  IL_0018:  ldc.i4.1
  IL_0019:  add.ovf
  IL_001a:  stind.i4
  IL_001b:  nop
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_0023:  stloc.2
  IL_0024:  ldloc.2
  IL_0025:  call       "Function Integer.ToString() As String"
  IL_002a:  call       "Sub System.Console.Write(String)"
  IL_002f:  nop
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.0
  IL_0032:  callvirt   "ByRef Function C(Of Integer).get_Item(Integer) As Integer"
  IL_0037:  ldc.i4.2
  IL_0038:  stind.i4
  IL_0039:  ldloc.2
  IL_003a:  call       "Function Integer.ToString() As String"
  IL_003f:  call       "Sub System.Console.Write(String)"
  IL_0044:  nop
  IL_0045:  nop
  IL_0046:  ret
}
]]>)
            verifier.VerifyDiagnostics()
        End Sub

    End Class

End Namespace

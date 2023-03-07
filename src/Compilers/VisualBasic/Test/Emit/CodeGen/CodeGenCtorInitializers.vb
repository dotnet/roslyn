' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenStructCtor
        Inherits BasicTestBase

        <Fact()>
        Public Sub ParameterlessCtor003()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        

Structure S1
    Public x as integer
    Public y as integer
    
    public Sub New(x as integer)
        Me.New       
        me.x = x
        me.y = me.x + 1
    end sub

end structure 

Module M1
    Sub Main()
        dim s as new S1()
        Console.WriteLine(s.x)

        s.y = 333
        s = new S1()
        Console.WriteLine(s.y)

        s = new S1(3)
        Console.WriteLine(s.x)
        Console.WriteLine(s.y)

    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[
0
0
3
4
]]>).VerifyIL("S1..ctor(Integer)",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  initobj    "S1"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      "S1.x As Integer"
  IL_000e:  ldarg.0
  IL_000f:  ldarg.0
  IL_0010:  ldfld      "S1.x As Integer"
  IL_0015:  ldc.i4.1
  IL_0016:  add.ovf
  IL_0017:  stfld      "S1.y As Integer"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor001()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Class c1
        Public readonly Property p1 As Integer
        Public readonly Property p2 As Integer
        Public Sub New()
            p1 = 42
            p2 = p1
        End Sub
    End Class

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c.p2)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor()",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.s   42
  IL_0009:  stfld      "Program.c1._p1 As Integer"
  IL_000e:  ldarg.0
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.c1.get_p1() As Integer"
  IL_0015:  stfld      "Program.c1._p2 As Integer"
  IL_001a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor002()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Class c1
        Public shared readonly Property p1 As Integer
        Public shared readonly Property p2 As Integer
        Shared Sub New()
            p1 = 42
            p2 = p1
        End Sub
    End Class

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c1.p2)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..cctor()",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  stsfld     "Program.c1._p1 As Integer"
  IL_0007:  call       "Function Program.c1.get_p1() As Integer"
  IL_000c:  stsfld     "Program.c1._p2 As Integer"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor001s()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Structure c1
        Public readonly Property p1 As Integer
        Public readonly Property p2 As Integer
        Public Sub New(dummy as integer)
            p1 = 42
            p2 = p1
        End Sub
    End Structure

    Sub Main(args As String())
        Dim c As New c1(1)
        System.Console.WriteLine(c.p2)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor(Integer)",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    "Program.c1"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      "Program.c1._p1 As Integer"
  IL_000f:  ldarg.0
  IL_0010:  ldarg.0
  IL_0011:  call       "Function Program.c1.get_p1() As Integer"
  IL_0016:  stfld      "Program.c1._p2 As Integer"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor002s()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Structure c1
        Public shared readonly Property p1 As Integer
        Public shared readonly Property p2 As Integer
        Shared Sub New()
            p1 = 42
            p2 = p1
        End Sub
    End Structure

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c1.p2)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..cctor()",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  stsfld     "Program.c1._p1 As Integer"
  IL_0007:  call       "Function Program.c1.get_p1() As Integer"
  IL_000c:  stsfld     "Program.c1._p2 As Integer"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor003()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Class c1
        Public readonly Property p1 As Integer
        Public Sub New()
            p1 += 42
        End Sub
    End Class

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor()",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Program.c1.get_p1() As Integer"
  IL_000d:  ldc.i4.s   42
  IL_000f:  add.ovf
  IL_0010:  stfld      "Program.c1._p1 As Integer"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor003s()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Structure c1
        Public readonly Property p1 As Integer
        Public Sub New(dummy as integer)
            p1 += 42
        End Sub
    End Structure

    Sub Main(args As String())
        Dim c As New c1(1)
        System.Console.WriteLine(c.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor(Integer)",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  initobj    "Program.c1"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.0
  IL_0009:  call       "Function Program.c1.get_p1() As Integer"
  IL_000e:  ldc.i4.s   42
  IL_0010:  add.ovf
  IL_0011:  stfld      "Program.c1._p1 As Integer"
  IL_0016:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor004()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Class c1
        Public shared readonly Property p1 As Integer
        Shared Sub New()
            p1 += Goo
        End Sub
    End Class

    function Goo() as integer
        System.Console.Write("hello")
        return 42
    end function

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[hello42]]>
            ).VerifyIL("Program.c1..cctor()",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       "Function Program.c1.get_p1() As Integer"
  IL_0005:  call       "Function Program.Goo() As Integer"
  IL_000a:  add.ovf
  IL_000b:  stsfld     "Program.c1._p1 As Integer"
  IL_0010:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor005()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Class c1
        Public readonly Property p1 As Integer
        Public Sub New()
            goo(p1)
        End Sub
    End Class

    sub goo(byref x as integer)
        x = 42
    end sub

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor()",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  call       "Function Program.c1.get_p1() As Integer"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub Program.goo(ByRef Integer)"
  IL_0014:  ldarg.0
  IL_0015:  ldloc.0
  IL_0016:  stfld      "Program.c1._p1 As Integer"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor005i()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    class c1
        Public readonly Property p1 As Integer
        Public readonly Property p2 As Integer = goo(p1)
    End class

    function goo(byref x as integer)
        x = 42
        return 1
    end function

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor()",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Program.c1.get_p1() As Integer"
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       "Function Program.goo(ByRef Integer) As Object"
  IL_0015:  ldarg.0
  IL_0016:  ldloc.0
  IL_0017:  stfld      "Program.c1._p1 As Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0021:  stfld      "Program.c1._p2 As Integer"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor005s()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    class c1
        Public shared readonly Property p1 As Integer = 3
        Public shared readonly p2 As Integer = goo(p1)
    End class

    function goo(byref x as integer)
        x = 42
        return 1
    end function

    Sub Main(args As String())
        System.Console.WriteLine(c1.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..cctor()",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  stsfld     "Program.c1._p1 As Integer"
  IL_0006:  call       "Function Program.c1.get_p1() As Integer"
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Function Program.goo(ByRef Integer) As Object"
  IL_0013:  ldloc.0
  IL_0014:  stsfld     "Program.c1._p1 As Integer"
  IL_0019:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_001e:  stsfld     "Program.c1.p2 As Integer"
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor005si()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    class c1
        Public shared readonly Property p1 As Integer = 3
        Public readonly property p2 As Integer = goo(p1)
    End class

    function goo(byref x as integer)
        x = 42
        return 1
    end function

    Sub Main(args As String())
        dim c as new c1
        System.Console.WriteLine(c1.p1)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[3]]>
            ).VerifyIL("Program.c1..ctor()",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  call       "Function Program.c1.get_p1() As Integer"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Program.goo(ByRef Integer) As Object"
  IL_0014:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0019:  stfld      "Program.c1._p2 As Integer"
  IL_001e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ReadOnlyAutopropInCtor006()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
option strict off

Module Program
    Class c1
        Public readonly Property p1 As Integer
        Public Sub New()
            Dim o as object = new Late
            o.goo(p1)
        End Sub
    End Class

    Sub Main(args As String())
        Dim c As New c1
        System.Console.WriteLine(c.p1)
    End Sub

    class Late
        sub goo(byref x as integer)
            x = 42
        end sub
    end class

End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>
            ).VerifyIL("Program.c1..ctor()",
            <![CDATA[
{
  // Code size      100 (0x64)
  .maxstack  10
  .locals init (Object() V_0,
                Boolean() V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  newobj     "Sub Program.Late..ctor()"
  IL_000b:  ldnull
  IL_000c:  ldstr      "goo"
  IL_0011:  ldc.i4.1
  IL_0012:  newarr     "Object"
  IL_0017:  dup
  IL_0018:  ldc.i4.0
  IL_0019:  ldarg.0
  IL_001a:  call       "Function Program.c1.get_p1() As Integer"
  IL_001f:  box        "Integer"
  IL_0024:  stelem.ref
  IL_0025:  dup
  IL_0026:  stloc.0
  IL_0027:  ldnull
  IL_0028:  ldnull
  IL_0029:  ldc.i4.1
  IL_002a:  newarr     "Boolean"
  IL_002f:  dup
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i1
  IL_0033:  dup
  IL_0034:  stloc.1
  IL_0035:  ldc.i4.1
  IL_0036:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_003b:  pop
  IL_003c:  ldloc.1
  IL_003d:  ldc.i4.0
  IL_003e:  ldelem.u1
  IL_003f:  brfalse.s  IL_0063
  IL_0041:  ldarg.0
  IL_0042:  ldloc.0
  IL_0043:  ldc.i4.0
  IL_0044:  ldelem.ref
  IL_0045:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004a:  ldtoken    "Integer"
  IL_004f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0054:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_0059:  unbox.any  "Integer"
  IL_005e:  stfld      "Program.c1._p1 As Integer"
  IL_0063:  ret
}
]]>)
        End Sub

        <WorkItem(4383, "https://github.com/dotnet/roslyn/issues/4383")>
        <Fact()>
        Public Sub DecimalConstInit001()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Console.WriteLine(ClassWithStaticField.Dictionary("String3"))
    End Sub
End Module

    Public Class ClassWithStaticField

    Public Const DecimalConstant As Decimal = 375D

    Private Shared ReadOnly DictionaryField As Dictionary(Of String, Single) = New Dictionary(Of String, Single) From {
        {"String1", 1.0F},
        {"String2", 2.0F},
        {"String3", 3.0F}
    }

    Public Shared ReadOnly Property Dictionary As Dictionary(Of String, Single) 
        Get
            Return DictionaryField
        End Get
    End Property

End Class

    </file>
</compilation>,
expectedOutput:=<![CDATA[3]]>
            ).VerifyIL("ClassWithStaticField..cctor()",
            <![CDATA[
{
  // Code size       75 (0x4b)
  .maxstack  4
  IL_0000:  ldc.i4     0x177
  IL_0005:  conv.i8
  IL_0006:  newobj     "Sub Decimal..ctor(Long)"
  IL_000b:  stsfld     "ClassWithStaticField.DecimalConstant As Decimal"
  IL_0010:  newobj     "Sub System.Collections.Generic.Dictionary(Of String, Single)..ctor()"
  IL_0015:  dup
  IL_0016:  ldstr      "String1"
  IL_001b:  ldc.r4     1
  IL_0020:  callvirt   "Sub System.Collections.Generic.Dictionary(Of String, Single).Add(String, Single)"
  IL_0025:  dup
  IL_0026:  ldstr      "String2"
  IL_002b:  ldc.r4     2
  IL_0030:  callvirt   "Sub System.Collections.Generic.Dictionary(Of String, Single).Add(String, Single)"
  IL_0035:  dup
  IL_0036:  ldstr      "String3"
  IL_003b:  ldc.r4     3
  IL_0040:  callvirt   "Sub System.Collections.Generic.Dictionary(Of String, Single).Add(String, Single)"
  IL_0045:  stsfld     "ClassWithStaticField.DictionaryField As System.Collections.Generic.Dictionary(Of String, Single)"
  IL_004a:  ret
}
]]>)
        End Sub

    End Class
End Namespace

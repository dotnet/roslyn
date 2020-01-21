' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenWithBlock
        Inherits BasicTestBase

        <Fact()>
        Public Sub WithTestModuleField()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module ModuleWithField
    Public field1 As String = "a"
End Module
Module WithTestModuleField
    Sub Main()
        With field1
            Dim l = .Length
            System.Console.WriteLine(.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="a")
        End Sub

        <Fact()>
        Public Sub WithTestLineContinuation()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class Class1
    Public Property Property1 As Class1
    Public Property Property2 As String
End Class
Module WithTestLineContinuation
    Sub Main()
        With New Class1()
            .Property1 = New Class1()
            .Property1.
                Property1 = New Class1()
            .Property1 _
                .
                Property1 _
                    .Property2 = "a"
            System.Console.WriteLine(.Property1 _
                              .Property1.
                              Property2)
        End With
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="a")
        End Sub

        <Fact()>
        Public Sub WithTestNested()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class Class2
    Default Public ReadOnly Property Item(x As Integer) As Class2
        Get
            Return New Class2
        End Get
    End Property
    Public Property Property2 As String
End Class
Module WithTestNested
    Sub Main()
        Dim c2 As New Class2()
        With c2(3)
            .Property2 = "b"
            With .Item(4)
                .Property2 = "a"
                System.Console.Write(.Property2)
            End With
            System.Console.Write(.Property2)
        End With
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="ab")
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithNothingLiteral()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        With Nothing
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  pop
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub WithUnused()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x(10) As S
        x(0).s = "hello"

        Dim s As String
        Dim dummy As Integer

        For i As Integer = 0 To 5
            With x(i)
                s = .s
                dummy += .i
            End With
        Next
    End Sub

    Structure S
        Public s As String
        Public i As Integer
    End Structure
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyDiagnostics().
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (Module1.S() V_0, //x
                Integer V_1, //dummy
                Integer V_2, //i
                Module1.S& V_3) //$W0
  IL_0000:  ldc.i4.s   11
  IL_0002:  newarr     "Module1.S"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldelema    "Module1.S"
  IL_000f:  ldstr      "hello"
  IL_0014:  stfld      "Module1.S.s As String"
  IL_0019:  ldc.i4.0
  IL_001a:  stloc.2
  IL_001b:  ldloc.0
  IL_001c:  ldloc.2
  IL_001d:  ldelema    "Module1.S"
  IL_0022:  stloc.3
  IL_0023:  ldloc.1
  IL_0024:  ldloc.3
  IL_0025:  ldfld      "Module1.S.i As Integer"
  IL_002a:  add.ovf
  IL_002b:  stloc.1
  IL_002c:  ldloc.2
  IL_002d:  ldc.i4.1
  IL_002e:  add.ovf
  IL_002f:  stloc.2
  IL_0030:  ldloc.2
  IL_0031:  ldc.i4.5
  IL_0032:  ble.s      IL_001b
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithNothingLiteralAndExtensionMethod()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Sub Main(args As String())
        With Nothing
            Try
                .ExtMethod()
            Catch ex As NullReferenceException
                Console.WriteLine("Success")
            End Try
        End With
    End Sub
End Module

Module Ext
    &lt;ExtensionAttribute&gt;
    Sub ExtMeth(this As Object)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="Success").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithExtensionMethod()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Class C1
End Class

Module Program
    Sub Main(args As String())
        With New C1()
            .Goo()
        End With
    End Sub

    &lt;Extension()&gt;
    Public Sub Goo(ByRef x As C1)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (C1 V_0)
  IL_0000:  newobj     "Sub C1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Sub Program.Goo(ByRef C1)"
  IL_000d:  ldnull
  IL_000e:  pop
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStringLiteral()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With "abc"
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.String").VerifyDiagnostics()

        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStringExpression()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With "abc|" + "cba".ToString()
            Console.WriteLine(.GetType().ToString() + ": " + .ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.String: abc|cba")

            c.VerifyDiagnostics()

            c.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (String V_0) //$W0
  IL_0000:  ldstr      "abc|"
  IL_0005:  ldstr      "cba"
  IL_000a:  callvirt   "Function String.ToString() As String"
  IL_000f:  call       "Function String.Concat(String, String) As String"
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "Function Object.GetType() As System.Type"
  IL_001b:  callvirt   "Function System.Type.ToString() As String"
  IL_0020:  ldstr      ": "
  IL_0025:  ldloc.0
  IL_0026:  callvirt   "Function String.ToString() As String"
  IL_002b:  call       "Function String.Concat(String, String, String) As String"
  IL_0030:  call       "Sub System.Console.WriteLine(String)"
  IL_0035:  ldnull
  IL_0036:  stloc.0
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStringArrayLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim sss(10) As String
        sss(3) = "#3"
        With sss("123".Length)
            Console.WriteLine(.GetType().ToString() + ": " + .ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.String: #3").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (String V_0) //$W0
  IL_0000:  ldc.i4.s   11
  IL_0002:  newarr     "String"
  IL_0007:  dup
  IL_0008:  ldc.i4.3
  IL_0009:  ldstr      "#3"
  IL_000e:  stelem.ref
  IL_000f:  ldstr      "123"
  IL_0014:  call       "Function String.get_Length() As Integer"
  IL_0019:  ldelem.ref
  IL_001a:  stloc.0
  IL_001b:  ldloc.0
  IL_001c:  callvirt   "Function Object.GetType() As System.Type"
  IL_0021:  callvirt   "Function System.Type.ToString() As String"
  IL_0026:  ldstr      ": "
  IL_002b:  ldloc.0
  IL_002c:  callvirt   "Function String.ToString() As String"
  IL_0031:  call       "Function String.Concat(String, String, String) As String"
  IL_0036:  call       "Sub System.Console.WriteLine(String)"
  IL_003b:  ldnull
  IL_003c:  stloc.0
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithNumericLiteral()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With 1
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.Int32")

            c.VerifyDiagnostics()

            c.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (Integer V_0) //$W0
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        "Integer"
  IL_0008:  call       "Function Object.GetType() As System.Type"
  IL_000d:  callvirt   "Function System.Type.ToString() As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithNumericRValue()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With IntProp
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
    Public Property IntProp As Integer
End Module
    </file>
</compilation>,
expectedOutput:="System.Int32")

            c.VerifyDiagnostics()

            c.VerifyIL("Program.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (Integer V_0) //$W0
  IL_0000:  call       "Function Program.get_IntProp() As Integer"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  box        "Integer"
  IL_000c:  call       "Function Object.GetType() As System.Type"
  IL_0011:  callvirt   "Function System.Type.ToString() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithNumericLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim i As Integer = 1
        With i
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.Int32").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        "Integer"
  IL_0008:  call       "Function Object.GetType() As System.Type"
  IL_000d:  callvirt   "Function System.Type.ToString() As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithNumericLValue2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Public i As Integer = 1
    Sub Main(args As String())
        With i
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.Int32").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  ldsflda    "Program.i As Integer"
  IL_0005:  ldind.i4
  IL_0006:  box        "Integer"
  IL_000b:  call       "Function Object.GetType() As System.Type"
  IL_0010:  callvirt   "Function System.Type.ToString() As String"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructureLValueAndExtensionMethod()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Structure C1
    Public field As Integer
    Public Property GooProp As Integer
End Structure

Class C2
    Public Shared Sub Main()
        Dim x = New C1()
        With x
            .field = 123 
            .ExtMeth
        End With
    End Sub
End Class

Module program
    &lt;ExtensionAttribute&gt;
    Sub ExtMeth(this As C1)
        Console.Write(this.field)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="123").
            VerifyDiagnostics().
            VerifyIL("C2.Main",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (C1 V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "C1"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   123
  IL_000c:  stfld      "C1.field As Integer"
  IL_0011:  ldloc.0
  IL_0012:  call       "Sub program.ExtMeth(C1)"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructureLValueAndExtensionMethod2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Structure C1
    Public field As Integer
    Public Property GooProp As Integer
End Structure

Class C2
    Public X As New C1()
    Public Sub Main2()
        With X
            .field = 123 
            .ExtMeth
        End With
    End Sub
    Public Shared Sub Main()
        Call New C2().Main2()
    End Sub
End Class

Module program
    &lt;ExtensionAttribute&gt;
    Sub ExtMeth(this As C1)
        Console.Write(this.field)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="123").
            VerifyDiagnostics().
            VerifyIL("C2.Main2",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C2.X As C1"
  IL_0006:  dup
  IL_0007:  ldc.i4.s   123
  IL_0009:  stfld      "C1.field As Integer"
  IL_000e:  ldobj      "C1"
  IL_0013:  call       "Sub program.ExtMeth(C1)"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructureLValueAndExtensionMethod3()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Structure C1
    Public field As Integer
    Public Property GooProp As Integer
End Structure

Class C2
    Public X As New C1()
    Public Sub Main2()
        With X
            Dim val = 123
            Dim _sub As Action = Sub() 
                                    .field = val
                                    .ExtMeth
                                 End Sub
            _sub()
        End With
    End Sub
    Public Shared Sub Main()
        Call New C2().Main2()
    End Sub
End Class

Module program
    &lt;ExtensionAttribute&gt;
    Sub ExtMeth(this As C1)
        Console.Write(this.field)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:="123")
            c.VerifyDiagnostics()
            c.VerifyIL("C2.Main2",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  IL_0000:  newobj     "Sub C2._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_000c:  dup
  IL_000d:  ldc.i4.s   123
  IL_000f:  stfld      "C2._Closure$__2-0.$VB$Local_val As Integer"
  IL_0014:  ldftn      "Sub C2._Closure$__2-0._Lambda$__0()"
  IL_001a:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001f:  callvirt   "Sub System.Action.Invoke()"
  IL_0024:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_0006:  ldflda     "C2.X As C1"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "C2._Closure$__2-0.$VB$Local_val As Integer"
  IL_0011:  stfld      "C1.field As Integer"
  IL_0016:  ldarg.0
  IL_0017:  ldfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_001c:  ldfld      "C2.X As C1"
  IL_0021:  call       "Sub program.ExtMeth(C1)"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructRValue()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure SSS
        Public Sub New(i As Integer)
        End Sub
        Public Function M() As String
            Return Me.GetType().ToString()
        End Function
    End Structure
    Sub Main(args As String())
        With New SSS(1)
            Dim a = .M()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Program+SSS")
            c.VerifyDiagnostics()
            c.VerifyIL("Program.Main", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Program.SSS V_0) //$W0
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Program.SSS..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Program.SSS.M() As String"
  IL_000f:  callvirt   "Function String.ToString() As String"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructRValueAndCleanup()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure SSSS
        Private S As String
    End Structure
    Structure SSS
        Public Sub New(i As Integer)
        End Sub
        Private A As SSSS 
        Public Function M() As String
            Return Me.GetType().ToString()
        End Function
    End Structure
    Sub Main(args As String())
        With New SSS(1)
            Dim a = .M()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Program+SSS").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Program.SSS V_0) //$W0
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Program.SSS..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Program.SSS.M() As String"
  IL_000f:  callvirt   "Function String.ToString() As String"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ldloca.s   V_0
  IL_001b:  initobj    "Program.SSS"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithMutatingStructRValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Structure SSS
        Public F As Integer

        Public Sub SetF(f As Integer)
            Me.F = f
        End Sub
    End Structure

    Public Shared Sub Main(args() As String)
        With New SSS
            .SetF(1)
            Console.Write(.F)
            .SetF(2)
            Console.Write(.F)
        End With
    End Sub
End Class

    </file>
</compilation>,
expectedOutput:="12").
            VerifyDiagnostics().
            VerifyIL("C2.Main",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (C2.SSS V_0) //$W0
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "C2.SSS"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Sub C2.SSS.SetF(Integer)"
  IL_0010:  ldloc.0
  IL_0011:  ldfld      "C2.SSS.F As Integer"
  IL_0016:  call       "Sub System.Console.Write(Integer)"
  IL_001b:  ldloca.s   V_0
  IL_001d:  ldc.i4.2
  IL_001e:  call       "Sub C2.SSS.SetF(Integer)"
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "C2.SSS.F As Integer"
  IL_0029:  call       "Sub System.Console.Write(Integer)"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWithInsideUsingOfStructureValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure STRUCT
    Implements IDisposable
    Public C As String
    Public D As Integer
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class Clazz
    Public Shared Sub Main(args() As String)
        Using s = New STRUCT()
            With s
                Goo(.D)
            End With
            Console.Write(s.D)
        End Using
    End Sub
    Public Shared Sub Goo(ByRef x As Integer)
        x = 123
    End Sub
End Class

    </file>
</compilation>,
expectedOutput:="0").
            VerifyDiagnostics(Diagnostic(ERRID.WRN_MutableStructureInUsing, "s = New STRUCT()").WithArguments("s")).
            VerifyIL("Clazz.Main",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  1
  .locals init (STRUCT V_0, //s
  Integer V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "STRUCT"
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "STRUCT.D As Integer"
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Sub Clazz.Goo(ByRef Integer)"
  IL_0016:  ldloc.0
  IL_0017:  ldfld      "STRUCT.D As Integer"
  IL_001c:  call       "Sub System.Console.Write(Integer)"
  IL_0021:  leave.s    IL_0031
}
  finally
{
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. "STRUCT"
  IL_002b:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0030:  endfinally
}
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure SSS
        Public Sub New(i As Integer)
        End Sub
        Public Function M() As String
            Return Me.GetType().ToString()
        End Function
    End Structure
    Sub Main(args As String())
        Dim s1 As New SSS(1)
        With s1
            Dim a = .M()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Program+SSS").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Program.SSS V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Program.SSS..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Program.SSS.M() As String"
  IL_000f:  callvirt   "Function String.ToString() As String"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructLValue2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure SSS
        Public Sub New(i As Integer)
        End Sub
        Public Function M() As String
            Return Me.GetType().ToString()
        End Function
    End Structure
    Sub Main(args As String())
        Test(New SSS(1))
    End Sub
    Sub Test(s1 As SSS)
        With s1
            Dim a = .M()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Program+SSS").
            VerifyDiagnostics().
            VerifyIL("Program.Test",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "Function Program.SSS.M() As String"
  IL_0007:  callvirt   "Function String.ToString() As String"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructLValue3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure SSS
        Public Sub New(i As Integer)
        End Sub
        Public Function M() As String
            Return Me.GetType().ToString()
        End Function
    End Structure

    Public s1 As New SSS(1)

    Sub Main(args As String())
        With s1
            Dim a = .M()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Program+SSS").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldsflda    "Program.s1 As Program.SSS"
  IL_0005:  call       "Function Program.SSS.M() As String"
  IL_000a:  callvirt   "Function String.ToString() As String"
  IL_000f:  call       "Sub System.Console.WriteLine(String)"
  IL_0014:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithMutatingStructLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Structure SSS
        Public F As Integer

        Public Sub SetF(f As Integer)
            Me.F = f
        End Sub
    End Structure

    Public Shared Sub Main(args() As String)
        Dim sss As New SSS
        With sss 
            .SetF(1)
            Console.Write(.F)
            .SetF(2)
            Console.Write(.F)
        End With
    End Sub
End Class

    </file>
</compilation>,
expectedOutput:="12").
            VerifyDiagnostics().
            VerifyIL("C2.Main",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (C2.SSS V_0) //sss
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "C2.SSS"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Sub C2.SSS.SetF(Integer)"
  IL_0010:  ldloc.0
  IL_0011:  ldfld      "C2.SSS.F As Integer"
  IL_0016:  call       "Sub System.Console.Write(Integer)"
  IL_001b:  ldloca.s   V_0
  IL_001d:  ldc.i4.2
  IL_001e:  call       "Sub C2.SSS.SetF(Integer)"
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "C2.SSS.F As Integer"
  IL_0029:  call       "Sub System.Console.Write(Integer)"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithMutatingStructLValue2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Structure SSS
        Public F As Integer

        Public Sub SetF(f As Integer)
            Me.F = f
        End Sub
    End Structure

    Public Shared _sss As New SSS

    Public Shared Sub Main(args() As String)
        With _sss 
            .SetF(1)
            Console.Write(.F)
            .SetF(2)
            Console.Write(.F)
        End With
    End Sub
End Class

    </file>
</compilation>,
expectedOutput:="12").
            VerifyDiagnostics().
            VerifyIL("C2.Main",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  IL_0000:  ldsflda    "C2._sss As C2.SSS"
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  call       "Sub C2.SSS.SetF(Integer)"
  IL_000c:  dup
  IL_000d:  ldfld      "C2.SSS.F As Integer"
  IL_0012:  call       "Sub System.Console.Write(Integer)"
  IL_0017:  dup
  IL_0018:  ldc.i4.2
  IL_0019:  call       "Sub C2.SSS.SetF(Integer)"
  IL_001e:  ldfld      "C2.SSS.F As Integer"
  IL_0023:  call       "Sub System.Console.Write(Integer)"
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithStructArrayLValue()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure SSS
        Public Sub New(i As Integer)
        End Sub
        Public Function M() As String
            Return Me.GetType().ToString()
        End Function
    End Structure
    Sub Main(args As String())
        Dim s1(100) As SSS
        With s1("123".Length)
            Dim a = .M()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Program+SSS")
            c.VerifyDiagnostics()
            c.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldc.i4.s   101
  IL_0002:  newarr     "Program.SSS"
  IL_0007:  ldstr      "123"
  IL_000c:  call       "Function String.get_Length() As Integer"
  IL_0011:  ldelema    "Program.SSS"
  IL_0016:  call       "Function Program.SSS.M() As String"
  IL_001b:  callvirt   "Function String.ToString() As String"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithTypeParameterRValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C(Of T)
    Public Shared Sub M()
        With TProp
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
    Shared Public Property TProp As T
End Class
Module Program
    Sub Main(args As String())
        C(Of Date).M()
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.DateTime").
            VerifyDiagnostics().
            VerifyIL("C(Of T).M",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (T V_0) //$W0
  IL_0000:  call       "Function C(Of T).get_TProp() As T"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  constrained. "T"
  IL_000e:  callvirt   "Function Object.GetType() As System.Type"
  IL_0013:  callvirt   "Function System.Type.ToString() As String"
  IL_0018:  call       "Sub System.Console.WriteLine(String)"
  IL_001d:  ldloca.s   V_0
  IL_001f:  initobj    "T"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithTypeParameterRValue2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C(Of T As New)
    Public Shared Sub M()
        With New T
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Class
Module Program
    Sub Main(args As String())
        C(Of Date).M()
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.DateTime").
            VerifyDiagnostics().
            VerifyIL("C(Of T).M",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (T V_0) //$W0
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  constrained. "T"
  IL_000e:  callvirt   "Function Object.GetType() As System.Type"
  IL_0013:  callvirt   "Function System.Type.ToString() As String"
  IL_0018:  call       "Sub System.Console.WriteLine(String)"
  IL_001d:  ldloca.s   V_0
  IL_001f:  initobj    "T"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithTypeParameterLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C(Of T As New)
    Public Shared Sub M()
        Dim t As New T
        With t
            Dim a = .GetType()
            Console.WriteLine(a.ToString())
        End With
    End Sub
End Class
Module Program
    Sub Main(args As String())
        C(Of Date).M()
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.DateTime").
            VerifyDiagnostics().
            VerifyIL("C(Of T).M",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (T V_0) //t
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  constrained. "T"
  IL_000e:  callvirt   "Function Object.GetType() As System.Type"
  IL_0013:  callvirt   "Function System.Type.ToString() As String"
  IL_0018:  call       "Sub System.Console.WriteLine(String)"
  IL_001d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithTypeParameterArrayLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C(Of T As New)
    Public Shared Sub M()
        Dim t(10) As T
        With t("123".Length)
            Dim a = .GetType()
            Console.Write(a.ToString())
            If a IsNot Nothing Then
                Console.WriteLine(.GetHashCode())
            End If
        End With
    End Sub
End Class
Module Program
    Sub Main(args As String())
        C(Of Date).M()
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="System.DateTime0").
            VerifyDiagnostics().
            VerifyIL("C(Of T).M",
            <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (T() V_0, //$W0
                Integer V_1) //$W1
  IL_0000:  ldc.i4.s   11
  IL_0002:  newarr     "T"
  IL_0007:  stloc.0
  IL_0008:  ldstr      "123"
  IL_000d:  call       "Function String.get_Length() As Integer"
  IL_0012:  stloc.1
  IL_0013:  ldloc.0
  IL_0014:  ldloc.1
  IL_0015:  readonly.
  IL_0017:  ldelema    "T"
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function Object.GetType() As System.Type"
  IL_0027:  dup
  IL_0028:  callvirt   "Function System.Type.ToString() As String"
  IL_002d:  call       "Sub System.Console.Write(String)"
  IL_0032:  brfalse.s  IL_004d
  IL_0034:  ldloc.0
  IL_0035:  ldloc.1
  IL_0036:  readonly.
  IL_0038:  ldelema    "T"
  IL_003d:  constrained. "T"
  IL_0043:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_0048:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004d:  ldnull
  IL_004e:  stloc.0
  IL_004f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithAnonymousType()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim a = New With {.a = 1, .b="text"}
        With a 
            .a = 123
            Console.WriteLine(.a &amp; .b)
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="123text")

            c.VerifyDiagnostics()

            c.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (VB$AnonymousType_0(Of Integer, String) V_0) //$W0
  IL_0000:  ldc.i4.1
  IL_0001:  ldstr      "text"
  IL_0006:  newobj     "Sub VB$AnonymousType_0(Of Integer, String)..ctor(Integer, String)"
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.s   123
  IL_000f:  callvirt   "Sub VB$AnonymousType_0(Of Integer, String).set_a(Integer)"
  IL_0014:  ldloc.0
  IL_0015:  callvirt   "Function VB$AnonymousType_0(Of Integer, String).get_a() As Integer"
  IL_001a:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_001f:  ldloc.0
  IL_0020:  callvirt   "Function VB$AnonymousType_0(Of Integer, String).get_b() As String"
  IL_0025:  call       "Function String.Concat(String, String) As String"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  ldnull
  IL_0030:  stloc.0
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestNestedWithWithAnonymousType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        With New With {.a = 1, .b= New With { Key .x = "---", .y="text"} } 
            Console.Write(.ToString())
            Console.Write("|")
            With .b
                Console.Write(.ToString())
            End With
        End With
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="{ a = 1, b = { x = ---, y = text } }|{ x = ---, y = text }").
            VerifyDiagnostics().
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  3
  IL_0000:  ldc.i4.1
  IL_0001:  ldstr      "---"
  IL_0006:  ldstr      "text"
  IL_000b:  newobj     "Sub VB$AnonymousType_1(Of String, String)..ctor(String, String)"
  IL_0010:  newobj     "Sub VB$AnonymousType_0(Of Integer, <anonymous type: Key x As String, y As String>)..ctor(Integer, <anonymous type: Key x As String, y As String>)"
  IL_0015:  dup
  IL_0016:  callvirt   "Function VB$AnonymousType_0(Of Integer, <anonymous type: Key x As String, y As String>).ToString() As String"
  IL_001b:  call       "Sub System.Console.Write(String)"
  IL_0020:  ldstr      "|"
  IL_0025:  call       "Sub System.Console.Write(String)"
  IL_002a:  callvirt   "Function VB$AnonymousType_0(Of Integer, <anonymous type: Key x As String, y As String>).get_b() As <anonymous type: Key x As String, y As String>"
  IL_002f:  callvirt   "Function VB$AnonymousType_1(Of String, String).ToString() As String"
  IL_0034:  call       "Sub System.Console.Write(String)"
  IL_0039:  ldnull
  IL_003a:  pop
  IL_003b:  ldnull
  IL_003c:  pop
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWithStatement()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Structure S
        Public S() As SS
    End Structure

    Structure SS
        Public s As SSS
    End Structure

    Structure SSS
        Public F As Integer
        Public Overrides Function ToString() As String
            Return "Hello, " &amp; Me.F
        End Function
    End Structure

    Public Shared Sub Main(args() As String)
        Dim s(10) As S
        With s("1".Length)
            .S = New SS(2) {}
        End With
        With s("1".Length).S(1).s
            .F = 123
        End With
        Console.Write(s(1).S(1).s)
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Hello, 123").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWithStatement_MyClass()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure Clazz
    Structure SS
        Public FLD As String
    End Structure

    Public FLD As SS

    Sub TEST()
        With MyClass.FLD
            Console.Write(.GetType().ToString())
        End With
    End Sub
    Public Shared Sub Main(args() As String)
        Call New Clazz().TEST()
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:="Clazz+SS").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWithStatement_MyBase()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Clazz
    Public Structure SS
        Public FLD As String
    End Structure

    Public FLD As SS
End Class

Class Derived
    Inherits Clazz

    Sub TEST()
        With MyBase.FLD
            Console.Write(.GetType().ToString())
        End With
    End Sub
    Public Shared Sub Main(args() As String)
        Call New Derived().TEST()
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="Clazz+SS").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithMeReference_Class()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Public A As Integer
    Public B As Date
    Public C As String

    Public Sub New()
        With Me
            .A = 1
            .B = #1/2/2003#
            .C = "!"
        End With
    End Sub

    Public Overrides Function ToString() As String
        Return ".A = " &amp; Me.A &amp; "; .B = " &amp; Me.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; .C = " &amp; Me.C
    End Function

    Public Shared Sub Main(args() As String)
        Console.Write(New C2().ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=".A = 1; .B = 1/2/2003; .C = !").
            VerifyDiagnostics().
            VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      "C2.A As Integer"
  IL_000d:  ldarg.0
  IL_000e:  ldc.i8     0x8c48009070c0000
  IL_0017:  newobj     "Sub Date..ctor(Long)"
  IL_001c:  stfld      "C2.B As Date"
  IL_0021:  ldarg.0
  IL_0022:  ldstr      "!"
  IL_0027:  stfld      "C2.C As String"
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithMeReference_Struct()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public A As Integer
    Public B As Date
    Public C As String

    Public Sub New(i As Integer)
        With Me
            .A = 1
            .B = #1/2/2003#
            .C = "!"
        End With
    End Sub

    Public Overrides Function ToString() As String
        Return ".A = " &amp; Me.A &amp; "; .B = " &amp; Me.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; .C = " &amp; Me.C
    End Function

    Public Shared Sub Main(args() As String)
        Console.Write(New C2(1).ToString())
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:=".A = 1; .B = 1/2/2003; .C = !").
            VerifyDiagnostics().
            VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    "C2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      "C2.A As Integer"
  IL_000e:  ldarg.0
  IL_000f:  ldc.i8     0x8c48009070c0000
  IL_0018:  newobj     "Sub Date..ctor(Long)"
  IL_001d:  stfld      "C2.B As Date"
  IL_0022:  ldarg.0
  IL_0023:  ldstr      "!"
  IL_0028:  stfld      "C2.C As String"
  IL_002d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWithWithMeReference_Class_Capture()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Public A As Integer
    Public B As Date
    Public C As String

    Public Sub New()
        With Me
            .A = 1
            Dim a As Action = Sub()
                                  .B = #1/2/2003#
                                  .C = "!"
                              End Sub
            a()
        End With
    End Sub

    Public Overrides Function ToString() As String
        Return ".A = " &amp; Me.A &amp; "; .B = " &amp; Me.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; .C = " &amp; Me.C
    End Function

    Public Shared Sub Main(args() As String)
        Console.Write(New C2().ToString())
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=".A = 1; .B = 1/2/2003; .C = !")
            c.VerifyDiagnostics()
            c.VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.1
  IL_0008:  stfld      "C2.A As Integer"
  IL_000d:  ldarg.0
  IL_000e:  ldftn      "Sub C2._Lambda$__3-0()"
  IL_0014:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0019:  callvirt   "Sub System.Action.Invoke()"
  IL_001e:  ret
}
]]>)
            c.VerifyIL("C2._Lambda$__3-0",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i8     0x8c48009070c0000
  IL_000a:  newobj     "Sub Date..ctor(Long)"
  IL_000f:  stfld      "C2.B As Date"
  IL_0014:  ldarg.0
  IL_0015:  ldstr      "!"
  IL_001a:  stfld      "C2.C As String"
  IL_001f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_LiftedStructLValue()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public _abc As ABC

    Structure ABC
        Public A As Integer
        Public B As Date
        Public C As String
    End Structure

    Public Sub New(i As ABC)
        With i
            .A = 1
            Dim a As Action = Sub()
                                  .B = #1/2/2003#
                                  .C = "!"
                              End Sub
            a()
        End With
        Me._abc = i
    End Sub

    Public Overrides Function ToString() As String
        Return ".A = " &amp; Me._abc.A &amp; "; .B = " &amp; Me._abc.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; .C = " &amp; Me._abc.C
    End Function

    Public Shared Sub Main(args() As String)
        Console.Write(New C2(Nothing).ToString())
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:=".A = 1; .B = 1/2/2003; .C = !")
            c.VerifyDiagnostics()
            c.VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  2
  .locals init (C2._Closure$__3-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub C2._Closure$__3-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      "C2._Closure$__3-0.$VB$Local_i As C2.ABC"
  IL_000d:  ldarg.0
  IL_000e:  initobj    "C2"
  IL_0014:  ldloc.0
  IL_0015:  ldflda     "C2._Closure$__3-0.$VB$Local_i As C2.ABC"
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      "C2.ABC.A As Integer"
  IL_0020:  ldloc.0
  IL_0021:  ldftn      "Sub C2._Closure$__3-0._Lambda$__0()"
  IL_0027:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_002c:  callvirt   "Sub System.Action.Invoke()"
  IL_0031:  ldarg.0
  IL_0032:  ldloc.0
  IL_0033:  ldfld      "C2._Closure$__3-0.$VB$Local_i As C2.ABC"
  IL_0038:  stfld      "C2._abc As C2.ABC"
  IL_003d:  ret
}

]]>)
            c.VerifyIL("C2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C2._Closure$__3-0.$VB$Local_i As C2.ABC"
  IL_0006:  ldc.i8     0x8c48009070c0000
  IL_000f:  newobj     "Sub Date..ctor(Long)"
  IL_0014:  stfld      "C2.ABC.B As Date"
  IL_0019:  ldarg.0
  IL_001a:  ldflda     "C2._Closure$__3-0.$VB$Local_i As C2.ABC"
  IL_001f:  ldstr      "!"
  IL_0024:  stfld      "C2.ABC.C As String"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_LiftedStructLValue_Nested()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public _abc As ABC

    Structure ABC
        Public A As Integer
        Public B As Date
        Public C As String
    End Structure

    Public Sub New(i As ABC)
        Dim b = Function() As ABC
                    Dim x As New ABC
                    With x
                        .A = 1
                        Dim a As Action = Sub()
                                              .B = #1/2/2003#
                                              .C = "!"
                                          End Sub
                        a()
                    End With
                    Return x
                End Function
        Me._abc = b()
    End Sub

    Public Overrides Function ToString() As String
        Return ".A = " &amp; Me._abc.A &amp; "; .B = " &amp; Me._abc.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; .C = " &amp; Me._abc.C
    End Function

    Public Shared Sub Main(args() As String)
        Console.Write(New C2(Nothing).ToString())
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:=".A = 1; .B = 1/2/2003; .C = !")

            c.VerifyDiagnostics()
            c.VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (VB$AnonymousDelegate_0(Of C2.ABC) V_0) //b
  IL_0000:  ldarg.0
  IL_0001:  initobj    "C2"
  IL_0007:  ldsfld     "C2._Closure$__.$I3-0 As <generated method>"
  IL_000c:  brfalse.s  IL_0015
  IL_000e:  ldsfld     "C2._Closure$__.$I3-0 As <generated method>"
  IL_0013:  br.s       IL_002b
  IL_0015:  ldsfld     "C2._Closure$__.$I As C2._Closure$__"
  IL_001a:  ldftn      "Function C2._Closure$__._Lambda$__3-0() As C2.ABC"
  IL_0020:  newobj     "Sub VB$AnonymousDelegate_0(Of C2.ABC)..ctor(Object, System.IntPtr)"
  IL_0025:  dup
  IL_0026:  stsfld     "C2._Closure$__.$I3-0 As <generated method>"
  IL_002b:  stloc.0
  IL_002c:  ldarg.0
  IL_002d:  ldloc.0
  IL_002e:  callvirt   "Function VB$AnonymousDelegate_0(Of C2.ABC).Invoke() As C2.ABC"
  IL_0033:  stfld      "C2._abc As C2.ABC"
  IL_0038:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__._Lambda$__3-0",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  3
  IL_0000:  newobj     "Sub C2._Closure$__3-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldflda     "C2._Closure$__3-0.$VB$Local_x As C2.ABC"
  IL_000b:  initobj    "C2.ABC"
  IL_0011:  dup
  IL_0012:  ldflda     "C2._Closure$__3-0.$VB$Local_x As C2.ABC"
  IL_0017:  ldc.i4.1
  IL_0018:  stfld      "C2.ABC.A As Integer"
  IL_001d:  dup
  IL_001e:  ldftn      "Sub C2._Closure$__3-0._Lambda$__1()"
  IL_0024:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0029:  callvirt   "Sub System.Action.Invoke()"
  IL_002e:  ldfld      "C2._Closure$__3-0.$VB$Local_x As C2.ABC"
  IL_0033:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__3-0._Lambda$__1",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C2._Closure$__3-0.$VB$Local_x As C2.ABC"
  IL_0006:  ldc.i8     0x8c48009070c0000
  IL_000f:  newobj     "Sub Date..ctor(Long)"
  IL_0014:  stfld      "C2.ABC.B As Date"
  IL_0019:  ldarg.0
  IL_001a:  ldflda     "C2._Closure$__3-0.$VB$Local_x As C2.ABC"
  IL_001f:  ldstr      "!"
  IL_0024:  stfld      "C2.ABC.C As String"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_LiftedStructLValueArrayElement()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Public _abc As ABC

    Structure ABC
        Public A As Integer
        Public B As Date
        Public C As String
    End Structure

    Private ARR() As ABC

    Public Sub New(i As ABC)
        ARR = New ABC(2) {}
        ARR(0).A = 1
        With ARR(ARR(0).A)
            Dim b = Sub()
                        .A = 2
                        Dim a As Action = Sub()
                                              .B = #6/6/2006#
                                              .C = "?"
                                          End Sub
                        a()
                    End Sub
            b()
        End With

        Print(ARR(1))
    End Sub

    Public Sub Print(a As ABC)
        Console.Write("A = " &amp; a.A &amp; "; B = " &amp; a.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; C = " &amp; a.C)
    End Sub

    Public Shared Sub Main(args() As String)
        Dim a As New C2(Nothing)
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:="A = 2; B = 6/6/2006; C = ?")
            c.VerifyDiagnostics()
            c.VerifyIL("C2..ctor", <![CDATA[
{
  // Code size      112 (0x70)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  initobj    "C2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.3
  IL_0009:  newarr     "C2.ABC"
  IL_000e:  stfld      "C2.ARR As C2.ABC()"
  IL_0013:  ldarg.0
  IL_0014:  ldfld      "C2.ARR As C2.ABC()"
  IL_0019:  ldc.i4.0
  IL_001a:  ldelema    "C2.ABC"
  IL_001f:  ldc.i4.1
  IL_0020:  stfld      "C2.ABC.A As Integer"
  IL_0025:  newobj     "Sub C2._Closure$__4-0..ctor()"
  IL_002a:  dup
  IL_002b:  ldarg.0
  IL_002c:  ldfld      "C2.ARR As C2.ABC()"
  IL_0031:  stfld      "C2._Closure$__4-0.$W2 As C2.ABC()"
  IL_0036:  dup
  IL_0037:  ldarg.0
  IL_0038:  ldfld      "C2.ARR As C2.ABC()"
  IL_003d:  ldc.i4.0
  IL_003e:  ldelema    "C2.ABC"
  IL_0043:  ldfld      "C2.ABC.A As Integer"
  IL_0048:  stfld      "C2._Closure$__4-0.$W3 As Integer"
  IL_004d:  ldftn      "Sub C2._Closure$__4-0._Lambda$__0()"
  IL_0053:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_0058:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_005d:  ldarg.0
  IL_005e:  ldarg.0
  IL_005f:  ldfld      "C2.ARR As C2.ABC()"
  IL_0064:  ldc.i4.1
  IL_0065:  ldelem     "C2.ABC"
  IL_006a:  call       "Sub C2.Print(C2.ABC)"
  IL_006f:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__4-0._Lambda$__0", <![CDATA[
{
  // Code size       66 (0x42)
  .maxstack  3
  .locals init (System.Action V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C2._Closure$__4-0.$W2 As C2.ABC()"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "C2._Closure$__4-0.$W3 As Integer"
  IL_000c:  ldelema    "C2.ABC"
  IL_0011:  ldc.i4.2
  IL_0012:  stfld      "C2.ABC.A As Integer"
  IL_0017:  ldarg.0
  IL_0018:  ldfld      "C2._Closure$__4-0.$I1 As System.Action"
  IL_001d:  brfalse.s  IL_0027
  IL_001f:  ldarg.0
  IL_0020:  ldfld      "C2._Closure$__4-0.$I1 As System.Action"
  IL_0025:  br.s       IL_003c
  IL_0027:  ldarg.0
  IL_0028:  ldarg.0
  IL_0029:  ldftn      "Sub C2._Closure$__4-0._Lambda$__1()"
  IL_002f:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0034:  dup
  IL_0035:  stloc.0
  IL_0036:  stfld      "C2._Closure$__4-0.$I1 As System.Action"
  IL_003b:  ldloc.0
  IL_003c:  callvirt   "Sub System.Action.Invoke()"
  IL_0041:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__4-0._Lambda$__1", <![CDATA[
{
  // Code size       64 (0x40)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C2._Closure$__4-0.$W2 As C2.ABC()"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "C2._Closure$__4-0.$W3 As Integer"
  IL_000c:  ldelema    "C2.ABC"
  IL_0011:  ldc.i8     0x8c8571349d14000
  IL_001a:  newobj     "Sub Date..ctor(Long)"
  IL_001f:  stfld      "C2.ABC.B As Date"
  IL_0024:  ldarg.0
  IL_0025:  ldfld      "C2._Closure$__4-0.$W2 As C2.ABC()"
  IL_002a:  ldarg.0
  IL_002b:  ldfld      "C2._Closure$__4-0.$W3 As Integer"
  IL_0030:  ldelema    "C2.ABC"
  IL_0035:  ldstr      "?"
  IL_003a:  stfld      "C2.ABC.C As String"
  IL_003f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_LiftedStructLValueArrayElement2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Public _abc As ABC

    Structure ABC
        Public A As Integer
        Public B As Date
        Public C As String
    End Structure

    Private ARR() As ABC

    Public Sub S_TEST()
        ARR = New ABC(2) {}
        ARR(0).A = 1

        Dim outer As Action = Sub()
            With ARR(ARR(0).A)
                Dim b = Sub()
                            .A = 2
                            Dim a As Action = Sub()
                                                  .B = #6/6/2006#
                                                  .C = "?"
                                              End Sub
                            a()
                        End Sub
                b()
            End With
        End Sub
        outer()

        Print(ARR(1))
    End Sub

    Public Sub Print(a As ABC)
        Console.Write("A = " &amp; a.A &amp; "; B = " &amp; a.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; C = " &amp; a.C)
    End Sub

    Public Shared Sub Main(args() As String)
        Dim a As New C2()
        a.S_TEST()
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="A = 2; B = 6/6/2006; C = ?").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_LiftedStructLValueFieldAccess()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C2
    Structure ABC
        Public A As Integer
        Public B As Date
        Public C As String
    End Structure

    Private ARR As ABC

    Public Sub New()
        Dim sss As String = "?"
        With Me.ARR
            Dim b = Sub()
                        .A = 2
                        Dim a As Action = Sub()
                                              .B = #6/6/2006#
                                              .C = sss
                                          End Sub
                        a()
                    End Sub
            b()
        End With

        Print(Me.ARR)
    End Sub

    Public Sub Print(a As ABC)
        Console.Write("A = " &amp; a.A &amp; "; B = " &amp; a.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; C = " &amp; a.C)
    End Sub

    Public Shared Sub Main(args() As String)
        Dim a As New C2()
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="A = 2; B = 6/6/2006; C = ?")
            c.VerifyDiagnostics()
            c.VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  newobj     "Sub C2._Closure$__2-0..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "Sub Object..ctor()"
  IL_000b:  dup
  IL_000c:  ldarg.0
  IL_000d:  stfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_0012:  dup
  IL_0013:  ldstr      "?"
  IL_0018:  stfld      "C2._Closure$__2-0.$VB$Local_sss As String"
  IL_001d:  ldftn      "Sub C2._Closure$__2-0._Lambda$__0()"
  IL_0023:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_0028:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_002d:  ldarg.0
  IL_002e:  ldarg.0
  IL_002f:  ldfld      "C2.ARR As C2.ABC"
  IL_0034:  call       "Sub C2.Print(C2.ABC)"
  IL_0039:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (System.Action V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_0006:  ldflda     "C2.ARR As C2.ABC"
  IL_000b:  ldc.i4.2
  IL_000c:  stfld      "C2.ABC.A As Integer"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "C2._Closure$__2-0.$I1 As System.Action"
  IL_0017:  brfalse.s  IL_0021
  IL_0019:  ldarg.0
  IL_001a:  ldfld      "C2._Closure$__2-0.$I1 As System.Action"
  IL_001f:  br.s       IL_0036
  IL_0021:  ldarg.0
  IL_0022:  ldarg.0
  IL_0023:  ldftn      "Sub C2._Closure$__2-0._Lambda$__1()"
  IL_0029:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_002e:  dup
  IL_002f:  stloc.0
  IL_0030:  stfld      "C2._Closure$__2-0.$I1 As System.Action"
  IL_0035:  ldloc.0
  IL_0036:  callvirt   "Sub System.Action.Invoke()"
  IL_003b:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__2-0._Lambda$__1",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_0006:  ldflda     "C2.ARR As C2.ABC"
  IL_000b:  ldc.i8     0x8c8571349d14000
  IL_0014:  newobj     "Sub Date..ctor(Long)"
  IL_0019:  stfld      "C2.ABC.B As Date"
  IL_001e:  ldarg.0
  IL_001f:  ldfld      "C2._Closure$__2-0.$VB$Me As C2"
  IL_0024:  ldflda     "C2.ARR As C2.ABC"
  IL_0029:  ldarg.0
  IL_002a:  ldfld      "C2._Closure$__2-0.$VB$Local_sss As String"
  IL_002f:  stfld      "C2.ABC.C As String"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_LiftedStructLValueFieldAccess_Shared()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Structure ABC
        Public A As Integer
        Public B As Date
        Public C As String
    End Structure

    Private Shared ARR As ABC

    Public Sub New(i As Integer)
        With Me.ARR
            Dim b = Sub()
                        Dim x = "?"
                        .A = 2
                        Dim a As Action = Sub()
                                              .B = #6/6/2006#
                                              .C = x
                                          End Sub
                        a()
                    End Sub
            b()
        End With

        Print(C2.ARR)
    End Sub

    Public Sub Print(a As ABC)
        Console.Write("A = " &amp; a.A &amp; "; B = " &amp; a.B.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture) &amp; "; C = " &amp; a.C)
    End Sub

    Public Shared Sub Main(args() As String)
        Dim a As New C2(0)
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:="A = 2; B = 6/6/2006; C = ?")
            c.VerifyDiagnostics(Diagnostic(ERRID.WRN_SharedMemberThroughInstance, "Me.ARR"))
            c.VerifyIL("C2..ctor",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    "C2"
  IL_0007:  ldsfld     "C2._Closure$__.$I3-0 As <generated method>"
  IL_000c:  brfalse.s  IL_0015
  IL_000e:  ldsfld     "C2._Closure$__.$I3-0 As <generated method>"
  IL_0013:  br.s       IL_002b
  IL_0015:  ldsfld     "C2._Closure$__.$I As C2._Closure$__"
  IL_001a:  ldftn      "Sub C2._Closure$__._Lambda$__3-0()"
  IL_0020:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_0025:  dup
  IL_0026:  stsfld     "C2._Closure$__.$I3-0 As <generated method>"
  IL_002b:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0030:  ldarg.0
  IL_0031:  ldsfld     "C2.ARR As C2.ABC"
  IL_0036:  call       "Sub C2.Print(C2.ABC)"
  IL_003b:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__._Lambda$__3-0",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  IL_0000:  newobj     "Sub C2._Closure$__3-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "?"
  IL_000b:  stfld      "C2._Closure$__3-0.$VB$Local_x As String"
  IL_0010:  ldsflda    "C2.ARR As C2.ABC"
  IL_0015:  ldc.i4.2
  IL_0016:  stfld      "C2.ABC.A As Integer"
  IL_001b:  ldftn      "Sub C2._Closure$__3-0._Lambda$__1()"
  IL_0021:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0026:  callvirt   "Sub System.Action.Invoke()"
  IL_002b:  ret
}
]]>)
            c.VerifyIL("C2._Closure$__3-0._Lambda$__1",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldsflda    "C2.ARR As C2.ABC"
  IL_0005:  ldc.i8     0x8c8571349d14000
  IL_000e:  newobj     "Sub Date..ctor(Long)"
  IL_0013:  stfld      "C2.ABC.B As Date"
  IL_0018:  ldsflda    "C2.ARR As C2.ABC"
  IL_001d:  ldarg.0
  IL_001e:  ldfld      "C2._Closure$__3-0.$VB$Local_x As String"
  IL_0023:  stfld      "C2.ABC.C As String"
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWithAndReadOnlyValueTypedFields()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure STRUCT
    Public C As String
    Public D As Integer
End Structure

Class Clazz
    Public Shared ReadOnly FLD1 As New STRUCT
    Public Shared FLD2 As New STRUCT

    Shared Sub New()
        Console.Write(FLD1.D)
        Console.Write(" ")
        Console.Write(FLD2.D)
        Console.Write(" ")
        With FLD1
            Goo(.D, 1)
        End With
        With FLD2
            Goo(.D, 1)
        End With
        Console.Write(FLD1.D)
        Console.Write(" ")
        Console.Write(FLD2.D)
        Console.Write(" ")
    End Sub

    Public Shared Sub Main(args() As String)
        With FLD1
            Goo(.D, 2)
        End With
        With FLD2
            Goo(.D, 2)
        End With
        Console.Write(FLD1.D)
        Console.Write(" ")
        Console.Write(FLD2.D)
    End Sub
    Public Shared Sub Goo(ByRef x As Integer, val As Integer)
        x = val
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="0 0 1 1 1 2")
        End Sub

        <Fact()>
        Public Sub TestSimpleWith_ValueTypeLValueInParentheses()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure C2
    Structure SSS
        Public A As Integer
        Public B As Date
        Public Sub SetA(_a As Integer)
            Me.A = _a
        End Sub
    End Structure

    Public Sub New(i As Integer)
        Dim a As New SSS
        With (a)
            .SetA(222)
        End With
        Console.Write(a.A)
        Console.Write(" ")
        With a
            .SetA(222)
        End With
        Console.Write(a.A)
    End Sub

    Public Shared Sub main(args() As String)
        Dim a As New C2(1)
    End Sub
End Structure
    </file>
</compilation>,
expectedOutput:="0 222").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWith_WithDisposableStruct_RValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure Struct
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Structure Clazz
    Sub S()
        With New Struct
            .Dispose()
        End With
    End Sub
End Structure    </file>
</compilation>).
            VerifyDiagnostics().
            VerifyIL("Clazz.S",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (Struct V_0) //$W0
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Struct"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Sub Struct.Dispose()"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_WithDisposableStruct_LValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure Struct
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Structure Clazz
    Sub S()
        Dim s As New Struct
        With s
            .Dispose()
        End With
    End Sub
End Structure    </file>
</compilation>).
            VerifyDiagnostics().
            VerifyIL("Clazz.S",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (Struct V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Struct"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Sub Struct.Dispose()"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_Arrays()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Structure Clazz
    Shared Sub Main()
        With New Integer() {}
            Dim cnt = .Count
            Console.Write(cnt)
        End With
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:="0").
            VerifyDiagnostics().
            VerifyIL("Clazz.Main",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     "Integer"
  IL_0006:  call       "Function System.Linq.Enumerable.Count(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer)) As Integer"
  IL_000b:  call       "Sub System.Console.Write(Integer)"
  IL_0010:  ldnull
  IL_0011:  pop
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithInferredVarType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Structure Struct
    Public A As String
    Public B As String
End Structure

Structure Clazz

    Public S As Struct

    Shared Sub TEST()
        Dim c = New Clazz

        With c
            .S = New Struct()
            .S.A = ""
            .S.B = .S.A
            With .S
                Dim a = .A
                Dim b = .B

                Console.Write(a.GetType())
                Console.Write("|")
                Console.Write(b.GetType())
            End With
        End With
    End Sub
    Shared Sub Main(args() As String)
        TEST()
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:="System.String|System.String").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithInferredVarType2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Structure Struct
    Public A As String
    Public B As String
End Structure

Structure Clazz

    Public S As Struct

    Shared Sub TEST()
        Dim c = New Clazz

        With c
            .S = New Struct()
            .S.A = ""
            .S.B = .S.A

            Dim a = New With {.y = New Struct(), .x = Sub()
                                                          With .y
                                                              Dim a = .A
                                                              Dim b = .B
                                                          End With
                                                      End Sub}
        End With
    End Sub

    Shared Sub Main(args() As String)
        TEST()
    End Sub
End Structure
    </file>
</compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_CannotLiftAnonymousType1, ".y").WithArguments("y"),
                    Diagnostic(ERRID.ERR_BlockLocalShadowing1, "a").WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithInferredVarType3()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Structure Struct
    Public A As String
    Public B As String
End Structure

Structure Struct2
    Public X As Struct
    Public Y As String
End Structure

Structure Clazz
    Shared Sub TEST()
        Dim c As Struct2
        With c
            .Y = ""
            With .X
                .A = ""
            End With
        End With
        Console.Write(c)
        With c
            With .X
                .B = ""
            End With
        End With
        Console.WriteLine(c)
    End Sub
End Structure
    </file>
</compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "c").WithArguments("c"))
        End Sub

        <WorkItem(545120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545120")>
        <Fact()>
        Public Sub TestWith_NestedWithWithLambdasAndObjectInitializers()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Sub Main(args() As String)
        Dim t As New Clazz(1)
    End Sub
    Public F As SS2
    Sub New(i As Integer)
        F = New SS2()
        With F
            Dim a As New SS2() With {.X = Function() As SS1
                                                 With .Y
                                                     .A = "xyz"
                                                 End With
                                                 Return .Y
                                             End Function.Invoke()}
        End With
    End Sub
End Structure
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithQuery()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Structure Struct
    Public A As String
    Public B As String
End Structure

Class Clazz
    Structure SS
        Public FLD As Struct
    End Structure

    Public FLD As SS

    Sub TEST()
        With MyClass.FLD
            .FLD.A = "Success"
            Dim q = From x In "a" Select .FLD.A &amp; "=" &amp; x
            Console.Write(q.First())
        End With
    End Sub

    Shared Sub Main(args() As String)
        Call New Clazz().TEST()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="Success=a").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(DesktopOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsDesktopTypes)>
        Public Sub TestWith_MyBase()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Class Base
    Public Structure SS
        Public FLD As String
    End Structure
    Public FLD As SS
End Class

Class Clazz
    Inherits Base

    Public Shadows FLD As Integer

    Sub TEST()
        With MyBase.FLD
            Dim q = From x In "" Select .FLD
            Console.Write(q.GetType().ToString())
        End With
    End Sub

    Shared Sub Main(args() As String)
        Call New Clazz().TEST()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="System.Linq.Enumerable+WhereSelectEnumerableIterator`2[System.Char,System.String]")
            c.VerifyDiagnostics()
            c.VerifyIL("Clazz.TEST",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  ldstr      ""
  IL_0005:  ldarg.0
  IL_0006:  ldftn      "Function Clazz._Lambda$__2-0(Char) As String"
  IL_000c:  newobj     "Sub System.Func(Of Char, String)..ctor(Object, System.IntPtr)"
  IL_0011:  call       "Function System.Linq.Enumerable.Select(Of Char, String)(System.Collections.Generic.IEnumerable(Of Char), System.Func(Of Char, String)) As System.Collections.Generic.IEnumerable(Of String)"
  IL_0016:  callvirt   "Function Object.GetType() As System.Type"
  IL_001b:  callvirt   "Function System.Type.ToString() As String"
  IL_0020:  call       "Sub System.Console.Write(String)"
  IL_0025:  ret
}
]]>)
            c.VerifyIL("Clazz._Lambda$__2-0",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "Base.FLD As Base.SS"
  IL_0006:  ldfld      "Base.SS.FLD As String"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithQuery2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Structure Struct
    Public A As String
    Public B As String
End Structure

Class Clazz_Base
    Public Structure SS
        Public FLD As Struct
    End Structure

    Public FLD As SS
End Class

Class Clazz
    Inherits Clazz_Base

    Sub TEST()
        With MyBase.FLD
            .FLD.A = "Success"
            Dim q = From x In "a" Select .FLD.A &amp; "=" &amp; x
            Console.Write(q.First())
        End With
    End Sub

    Shared Sub Main(args() As String)
        Call New Clazz().TEST()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="Success=a").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithQuery3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Structure Struct
    Public A As String
    Public B As String
End Structure

Class Clazz_Base
    Public Structure SS
        Public FLD As Struct
    End Structure

    Public FLD As SS
End Class

Class Clazz
    Inherits Clazz_Base

    Sub TEST()
        With MyBase.FLD
            Dim _sub As Action = Sub()
                                    .FLD.A = "Success"
                                 End Sub
            _sub()
            Dim q = From x In "a" Select .FLD.A &amp; "=" &amp; x
            Console.Write(q.First())
        End With
    End Sub

    Shared Sub Main(args() As String)
        Call New Clazz().TEST()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="Success=a").VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestWith_NestedWithWithQuery4()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Structure STRUCT
    Public C As String
    Public D As Integer
End Structure

Class Clazz
    Public Shared Sub Main(args() As String)
        Dim source(10) As STRUCT

        Dim result = From x In source Select DirectCast(Function()
                                                            With x
                                                                Goo(.D)
                                                            End With
                                                            Return x
                                                        End Function, Func(Of STRUCT))()

        Console.Write(result.FirstOrDefault.D)
    End Sub
    Public Shared Sub Goo(ByRef x As Integer)
        x = 123
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="0")
            c.VerifyDiagnostics()
            c.VerifyIL("Clazz._Closure$__1-0._Lambda$__1",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Clazz._Closure$__1-0.$VB$Local_x As STRUCT"
  IL_0006:  ldfld      "STRUCT.D As Integer"
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Sub Clazz.Goo(ByRef Integer)"
  IL_0013:  ldarg.0
  IL_0014:  ldfld      "Clazz._Closure$__1-0.$VB$Local_x As STRUCT"
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_PropertyAccess_Simple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface IBar
    Property F As Integer
End Interface

Structure Clazz
    Implements IBar

    Public Property F As Integer Implements IBar.F

    Sub S()
        With Me
            .F += 1
        End With
    End Sub

End Structure
    </file>
</compilation>).
            VerifyDiagnostics().
            VerifyIL("Clazz.S",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Clazz.get_F() As Integer"
  IL_0007:  ldc.i4.1
  IL_0008:  add.ovf
  IL_0009:  call       "Sub Clazz.set_F(Integer)"
  IL_000e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_DictionaryAccess_Simple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure Clazz
    Sub TEST()
        Dim x As New Clazz
        With x
            Console.Write(!Success)
            !A = "aaa"
        End With
    End Sub

    Default Public Property F(s As String) As String
        Get
            Return s
        End Get
        Set(value As String)
        End Set
    End Property

    Shared Sub Main(args() As String)
        Call New Clazz().TEST()
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:="Success").
            VerifyDiagnostics().
            VerifyIL("Clazz.TEST",
            <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (Clazz V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Clazz"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldstr      "Success"
  IL_000f:  call       "Function Clazz.get_F(String) As String"
  IL_0014:  call       "Sub System.Console.Write(String)"
  IL_0019:  ldloca.s   V_0
  IL_001b:  ldstr      "A"
  IL_0020:  ldstr      "aaa"
  IL_0025:  call       "Sub Clazz.set_F(String, String)"
  IL_002a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_DictionaryAccess_ArrayAccess_Lambda()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure Clazz
    Sub TEST(x() As Clazz, i As Integer)
        With x(i - 5)
            Console.Write(!Success)
            Console.Write(" ")
            Dim _sub As Action = Sub()
                                     Console.Write(!Lambda)
                                 End Sub
            _sub()
        End With
    End Sub

    Default Public Property F(s As String) As String
        Get
            Return s
        End Get
        Set(value As String)
        End Set
    End Property

    Shared Sub Main(args() As String)
        Call New Clazz().TEST(New Clazz() {New Clazz}, 5)
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:="Success Lambda")

            c.VerifyDiagnostics()

            c.VerifyIL("Clazz.TEST", <![CDATA[
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (Clazz._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub Clazz._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      "Clazz._Closure$__1-0.$W2 As Clazz()"
  IL_000d:  ldloc.0
  IL_000e:  ldarg.2
  IL_000f:  ldc.i4.5
  IL_0010:  sub.ovf
  IL_0011:  stfld      "Clazz._Closure$__1-0.$W3 As Integer"
  IL_0016:  ldloc.0
  IL_0017:  ldfld      "Clazz._Closure$__1-0.$W2 As Clazz()"
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "Clazz._Closure$__1-0.$W3 As Integer"
  IL_0022:  ldelema    "Clazz"
  IL_0027:  ldstr      "Success"
  IL_002c:  call       "Function Clazz.get_F(String) As String"
  IL_0031:  call       "Sub System.Console.Write(String)"
  IL_0036:  ldstr      " "
  IL_003b:  call       "Sub System.Console.Write(String)"
  IL_0040:  ldloc.0
  IL_0041:  ldftn      "Sub Clazz._Closure$__1-0._Lambda$__0()"
  IL_0047:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_004c:  callvirt   "Sub System.Action.Invoke()"
  IL_0051:  ret
}
]]>)
            c.VerifyIL("Clazz._Closure$__1-0._Lambda$__0", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Clazz._Closure$__1-0.$W2 As Clazz()"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "Clazz._Closure$__1-0.$W3 As Integer"
  IL_000c:  ldelema    "Clazz"
  IL_0011:  ldstr      "Lambda"
  IL_0016:  call       "Function Clazz.get_F(String) As String"
  IL_001b:  call       "Sub System.Console.Write(String)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_MemberAccess_ArrayAccess_Lambda()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Structure Clazz
    Sub TEST(x() As Clazz, i As Integer)
        With x(i - 5)
            Console.Write(.F("Success"))
            Console.Write(" ")
            Dim _sub As Action = Sub()
                                     Console.Write(.F("Lambda"))
                                 End Sub
            _sub()
        End With
    End Sub

    Default Public Property F(s As String) As String
        Get
            Return s
        End Get
        Set(value As String)
        End Set
    End Property

    Shared Sub Main(args() As String)
        Call New Clazz().TEST(New Clazz() {New Clazz}, 5)
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:="Success Lambda")

            c.VerifyDiagnostics()

            c.VerifyIL("Clazz.TEST", <![CDATA[
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (Clazz._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub Clazz._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      "Clazz._Closure$__1-0.$W2 As Clazz()"
  IL_000d:  ldloc.0
  IL_000e:  ldarg.2
  IL_000f:  ldc.i4.5
  IL_0010:  sub.ovf
  IL_0011:  stfld      "Clazz._Closure$__1-0.$W3 As Integer"
  IL_0016:  ldloc.0
  IL_0017:  ldfld      "Clazz._Closure$__1-0.$W2 As Clazz()"
  IL_001c:  ldloc.0
  IL_001d:  ldfld      "Clazz._Closure$__1-0.$W3 As Integer"
  IL_0022:  ldelema    "Clazz"
  IL_0027:  ldstr      "Success"
  IL_002c:  call       "Function Clazz.get_F(String) As String"
  IL_0031:  call       "Sub System.Console.Write(String)"
  IL_0036:  ldstr      " "
  IL_003b:  call       "Sub System.Console.Write(String)"
  IL_0040:  ldloc.0
  IL_0041:  ldftn      "Sub Clazz._Closure$__1-0._Lambda$__0()"
  IL_0047:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_004c:  callvirt   "Sub System.Action.Invoke()"
  IL_0051:  ret
}
]]>)
            c.VerifyIL("Clazz._Closure$__1-0._Lambda$__0", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Clazz._Closure$__1-0.$W2 As Clazz()"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "Clazz._Closure$__1-0.$W3 As Integer"
  IL_000c:  ldelema    "Clazz"
  IL_0011:  ldstr      "Lambda"
  IL_0016:  call       "Function Clazz.get_F(String) As String"
  IL_001b:  call       "Sub System.Console.Write(String)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_DictionaryAccess_NestedWithWithQuery()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Structure Clazz
    Public Str As String
    Public Sub New(_str As String)
        Me.Str = _str
    End Sub

    Sub TEST(x() As Clazz, i As Integer)
        With x(i - 5)
            With !One
                With !Two
                    With !Three
                        Console.Write(!Success.Str)
                        Console.Write(" || ")
                        Dim q = From z In "*" Select U = !Query.Str
                        Console.Write(q.FirstOrDefault)
                    End With
                End With
            End With
        End With
    End Sub

    Default Public Property F(s As String) As Clazz
        Get
            Return New Clazz(Me.Str &amp; " > " &amp; s)
        End Get
        Set(value As Clazz)
        End Set
    End Property

    Shared Sub Main(args() As String)
        Dim p = New Clazz(0) {}
        p(0) = New Clazz("##")
        Call New Clazz().TEST(p, 5)
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:="## > One > Two > Three > Success || ## > One > Two > Three > Query")
            c.VerifyDiagnostics()
            c.VerifyIL("Clazz.TEST", <![CDATA[
{
  // Code size      142 (0x8e)
  .maxstack  3
  .locals init (Clazz V_0, //$W0
                Clazz V_1, //$W1
                Clazz._Closure$__3-0 V_2) //$VB$Closure_2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  ldc.i4.5
  IL_0003:  sub.ovf
  IL_0004:  ldelema    "Clazz"
  IL_0009:  ldstr      "One"
  IL_000e:  call       "Function Clazz.get_F(String) As Clazz"
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldstr      "Two"
  IL_001b:  call       "Function Clazz.get_F(String) As Clazz"
  IL_0020:  stloc.1
  IL_0021:  newobj     "Sub Clazz._Closure$__3-0..ctor()"
  IL_0026:  stloc.2
  IL_0027:  ldloc.2
  IL_0028:  ldloca.s   V_1
  IL_002a:  ldstr      "Three"
  IL_002f:  call       "Function Clazz.get_F(String) As Clazz"
  IL_0034:  stfld      "Clazz._Closure$__3-0.$W2 As Clazz"
  IL_0039:  ldloc.2
  IL_003a:  ldflda     "Clazz._Closure$__3-0.$W2 As Clazz"
  IL_003f:  ldstr      "Success"
  IL_0044:  call       "Function Clazz.get_F(String) As Clazz"
  IL_0049:  ldfld      "Clazz.Str As String"
  IL_004e:  call       "Sub System.Console.Write(String)"
  IL_0053:  ldstr      " || "
  IL_0058:  call       "Sub System.Console.Write(String)"
  IL_005d:  ldstr      "*"
  IL_0062:  ldloc.2
  IL_0063:  ldftn      "Function Clazz._Closure$__3-0._Lambda$__0(Char) As String"
  IL_0069:  newobj     "Sub System.Func(Of Char, String)..ctor(Object, System.IntPtr)"
  IL_006e:  call       "Function System.Linq.Enumerable.Select(Of Char, String)(System.Collections.Generic.IEnumerable(Of Char), System.Func(Of Char, String)) As System.Collections.Generic.IEnumerable(Of String)"
  IL_0073:  call       "Function System.Linq.Enumerable.FirstOrDefault(Of String)(System.Collections.Generic.IEnumerable(Of String)) As String"
  IL_0078:  call       "Sub System.Console.Write(String)"
  IL_007d:  ldloca.s   V_1
  IL_007f:  initobj    "Clazz"
  IL_0085:  ldloca.s   V_0
  IL_0087:  initobj    "Clazz"
  IL_008d:  ret
}
]]>)
            c.VerifyIL("Clazz._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "Clazz._Closure$__3-0.$W2 As Clazz"
  IL_0006:  ldstr      "Query"
  IL_000b:  call       "Function Clazz.get_F(String) As Clazz"
  IL_0010:  ldfld      "Clazz.Str As String"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestWith_ByRefExtensionMethodOfLValuePlaceholder()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Class C1
    Public Field As Integer

    Public Sub New(p As Integer)
        Field = p
    End Sub

End Class

Module Program
    Sub Main(args As String())
        With New C1(23)
            Console.WriteLine(.Field)
            .Goo()
            Console.WriteLine(.Field)
        End With
    End Sub

    &lt;Extension()&gt;
    Public Sub Goo(ByRef x As C1)
        x = New C1(42)
        Console.WriteLine(x.Field)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>, expectedOutput:=<![CDATA[
23
42
23
]]>).
            VerifyDiagnostics()
        End Sub

        <Fact(), WorkItem(2640, "https://github.com/dotnet/roslyn/issues/2640")>
        Public Sub WithUnusedArrayElement()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
  Private Structure MyStructure
    Public x As Single
  End Structure
  Sub Main()
    Dim unusedArrayInWith(0) As MyStructure
    With unusedArrayInWith(GetIndex())
      System.Console.WriteLine("Hello, World")
    End With
  End Sub

  Function GetIndex() as Integer
    System.Console.WriteLine("GetIndex")
    Return 0
  End Function
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe, expectedOutput:=<![CDATA[
GetIndex
Hello, World
]]>)
        End Sub

        <Fact()>
        <WorkItem(16968, "https://github.com/dotnet/roslyn/issues/16968")>
        Public Sub WithExpressionIsAccessedFromLambdaExecutedAfterTheBlock()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1

    Private f As EventOwner

    Sub Main()

        f = New EventOwner()
        With f
            AddHandler .Baz, Sub()
                                 .Bar = "called"
                             End Sub
        End With

        f.RaiseBaz()
        System.Console.WriteLine(f.Bar)
    End Sub

End Module

Class EventOwner

    Public Property Bar As String

    Public Event Baz As System.Action

    Public Sub RaiseBaz()
        RaiseEvent Baz()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="called")
        End Sub

    End Class

End Namespace

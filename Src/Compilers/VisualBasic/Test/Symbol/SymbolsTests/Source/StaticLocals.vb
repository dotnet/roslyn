' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class StaticLocals
        Inherits BasicTestBase

        <Fact>
        Public Sub FromSpec()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Test
    Sub F()
        Static x As Integer = 5

        Console.WriteLine("Static variable x = " & x)
        x += 1
    End Sub

    Sub Main()
        Dim i As Integer

        For i = 1 to 3
            F()
        Next i

        i = 3
label:
        Dim y As Integer = 8

        If i > 0 Then
            Console.WriteLine("Local variable y = " & y)
            y -= 1
            i -= 1
            Goto label
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
Static variable x = 5
Static variable x = 6
Static variable x = 7
Local variable y = 8
Local variable y = 8
Local variable y = 8
]]>)
        End Sub

        <Fact>
        Public Sub ArrayWithSizes()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
        TestProc()
        TestProc()
    End Sub

    Shared Sub TestProc()
        Static x(9) As Integer
        Static y(8), z(7) As Integer

        System.Console.WriteLine(x.Length)
        System.Console.WriteLine(y.Length)
        System.Console.WriteLine(z.Length)

        x = New Integer(7) {}
        y = New Integer(8) {}
        z = New Integer(9) {}
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
10
9
8
8
9
10
]]>)
        End Sub

        <Fact>
        Public Sub AsNew1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
        TestProc()
        TestProc()
    End Sub

    Shared Sub TestProc()
        Static x, y As New Object()

        System.Console.WriteLine(x Is Nothing)
        System.Console.WriteLine(y Is Nothing)
        System.Console.WriteLine(x Is y)

        x = Nothing
        y = Nothing
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
False
False
False
True
True
True
]]>)
        End Sub

        <Fact>
        Public Sub AsNew2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Structure Value
        Public z As Integer
    End Structure

    Shared Sub Main()
        TestProc()
        TestProc()
    End Sub

    Shared Sub TestProc()
        Static x, y As New Value() With {.z = 1}

        System.Console.WriteLine(x.z)
        System.Console.WriteLine(y.z)

        x = New Value() With {.z = 2}
        y = x
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
1
1
2
2
]]>)
        End Sub

        <Fact>
        Public Sub UseSiteErrors()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
    End Sub

    Shared Sub TestProc1()
        Static x(9) As Integer
        Static y1, y2 As New Integer()
        Static z As Integer = 1
    End Sub

    Shared Sub TestProc2()
        Static x() As Integer
        Static y As Integer

        x = nothing
        y = 1
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(compilationDef, options:=Options.OptionsExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor' is not defined.
        Static x(9) As Integer
               ~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor' is not defined.
        Static x(9) As Integer
               ~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State' is not defined.
        Static x(9) As Integer
               ~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor' is not defined.
        Static y1, y2 As New Integer()
                         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor' is not defined.
        Static y1, y2 As New Integer()
                         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor' is not defined.
        Static y1, y2 As New Integer()
                         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor' is not defined.
        Static y1, y2 As New Integer()
                         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State' is not defined.
        Static y1, y2 As New Integer()
                         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State' is not defined.
        Static y1, y2 As New Integer()
                         ~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor' is not defined.
        Static z As Integer = 1
                              ~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor' is not defined.
        Static z As Integer = 1
                              ~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State' is not defined.
        Static z As Integer = 1
                              ~
</expected>)
        End Sub

        <Fact>
        Public Sub DimWithStatic()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
        TestProc()
        TestProc()
    End Sub

    Shared Sub TestProc()
        Static Dim x As Integer = 1
        Dim Static y As Integer = 2

        System.Console.WriteLine(x)
        System.Console.WriteLine(y)

        x+=2
        y+=3
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
1
2
3
5
]]>)
        End Sub

        <Fact>
        Public Sub ModifierErrors()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
    End Sub

    Shared Sub TestProc()
        Static Dim Dim x As Integer = 1
        Dim Static Static y As Integer = 2
        Static Dim Static z As Integer = 1
        Dim Static Dim u As Integer = 2
        Static Const v As Integer = 0 
        Const Static w As Integer = 0 
        Dim Static Const a As Integer = 0 
        Dim Const Static b As Integer = 0 
        Const Dim Static c As Integer = 0 
        Const Static Static d As Integer = 0 
        Static Static e As Integer = 0 
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30178: Specifier is duplicated.
        Static Dim Dim x As Integer = 1
                   ~~~
BC30178: Specifier is duplicated.
        Dim Static Static y As Integer = 2
                   ~~~~~~
BC30178: Specifier is duplicated.
        Static Dim Static z As Integer = 1
                   ~~~~~~
BC30178: Specifier is duplicated.
        Dim Static Dim u As Integer = 2
                   ~~~
BC30246: 'Static' is not valid on a local constant declaration.
        Static Const v As Integer = 0 
        ~~~~~~
BC42099: Unused local constant: 'v'.
        Static Const v As Integer = 0 
                     ~
BC30246: 'Static' is not valid on a local constant declaration.
        Const Static w As Integer = 0 
              ~~~~~~
BC42099: Unused local constant: 'w'.
        Const Static w As Integer = 0 
                     ~
BC30246: 'Dim' is not valid on a local constant declaration.
        Dim Static Const a As Integer = 0 
        ~~~
BC42099: Unused local constant: 'a'.
        Dim Static Const a As Integer = 0 
                         ~
BC30246: 'Dim' is not valid on a local constant declaration.
        Dim Const Static b As Integer = 0 
        ~~~
BC42099: Unused local constant: 'b'.
        Dim Const Static b As Integer = 0 
                         ~
BC30246: 'Dim' is not valid on a local constant declaration.
        Const Dim Static c As Integer = 0 
              ~~~
BC42099: Unused local constant: 'c'.
        Const Dim Static c As Integer = 0 
                         ~
BC30246: 'Static' is not valid on a local constant declaration.
        Const Static Static d As Integer = 0 
              ~~~~~~
BC30178: Specifier is duplicated.
        Const Static Static d As Integer = 0 
                     ~~~~~~
BC42099: Unused local constant: 'd'.
        Const Static Static d As Integer = 0 
                            ~
BC30178: Specifier is duplicated.
        Static Static e As Integer = 0 
               ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub FieldMetadata()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq

Class Test

    Shared Sub Main()
        Dim type = GetType(Test)

        For Each fld In type.GetFields(Reflection.BindingFlags.DeclaredOnly Or
                                        Reflection.BindingFlags.NonPublic Or
                                        Reflection.BindingFlags.Public Or
                                        Reflection.BindingFlags.Instance Or
                                        Reflection.BindingFlags.Static).OrderBy(Function(f) f.Name)
            System.Console.WriteLine(fld.Name)
            System.Console.WriteLine(fld.Attributes)
            System.Console.WriteLine(fld.GetCustomAttributes(True).Length)
        Next
    End Sub

    Shared Sub TestProc1()
        Static x As Integer
        x = 0
    End Sub

    Sub TestProc2()
        Static x As Integer
        x = 0
    End Sub

    Shared Sub TestProc3()
        Static x As Integer = 0
        x = 0
    End Sub

    Sub TestProc4()
        Static x As Integer = 0
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
$STATIC$TestProc1$001$x
529
0
$STATIC$TestProc2$2001$x
513
0
$STATIC$TestProc3$001$x
529
0
$STATIC$TestProc3$001$x$Init
529
0
$STATIC$TestProc4$2001$x
513
0
$STATIC$TestProc4$2001$x$Init
513
0
_ClosureCache$__2
17
1
]]>)
        End Sub

        <Fact>
        Public Sub ILTest1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
    End Sub

    Shared Sub TestProc1(a As TestStruct1)
        Static x As Integer = 0
    End Sub

    Shared Sub TestProc2(a As TestStruct2)
        Static x As Integer
        x = 0
    End Sub

    Structure TestStruct1
    End Structure

    Structure TestStruct2
    End Structure

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe.WithDebugInformationKind(DebugInformationKind.Full))

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Test.TestProc1",
            <![CDATA[
{
  // Code size      116 (0x74)
  .maxstack  3
  .locals init (Boolean V_0)
  IL_0000:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldsflda    "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000c:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0011:  ldnull
  IL_0012:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  .try
{
  IL_001a:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0026:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_0045
  IL_0032:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0037:  ldc.i4.2
  IL_0038:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003d:  ldc.i4.0
  IL_003e:  stsfld     "Test.x As Integer"
  IL_0043:  leave.s    IL_0073
  IL_0045:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004a:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_004f:  ldc.i4.2
  IL_0050:  bne.un.s   IL_0058
  IL_0052:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_0057:  throw
  IL_0058:  leave.s    IL_0073
}
  finally
{
  IL_005a:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_005f:  ldc.i4.1
  IL_0060:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0065:  ldloc.0
  IL_0066:  brfalse.s  IL_0072
  IL_0068:  ldsfld     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_006d:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0072:  endfinally
}
  IL_0073:  ret
}
]]>)

            verifier.VerifyIL("Test.TestProc2",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  stsfld     "Test.x As Integer"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ILTest2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
    End Sub

    Sub TestProc1(a As TestStruct1)
        Static x As Integer = 0
    End Sub

    Sub TestProc2(a As TestStruct2)
        Static x As Integer
        x = 0
    End Sub

    Structure TestStruct1
    End Structure

    Structure TestStruct2
    End Structure

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithReferences(
                compilationDef,
                {MsvbRef, TestReferences.NetFx.v2_0_50727.mscorlib},
                OptionsExe.WithDebugInformationKind(DebugInformationKind.Full).
                           WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default))

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Test.TestProc1",
            <![CDATA[
{
  // Code size      118 (0x76)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0006:  brtrue.s   IL_001a
  IL_0008:  ldarg.0
  IL_0009:  ldflda     "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000e:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0013:  ldnull
  IL_0014:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0019:  pop
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0020:  call       "Sub System.Threading.Monitor.Enter(Object)"
  .try
{
  IL_0025:  ldarg.0
  IL_0026:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_0047
  IL_0032:  ldarg.0
  IL_0033:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0038:  ldc.i4.2
  IL_0039:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003e:  ldarg.0
  IL_003f:  ldc.i4.0
  IL_0040:  stfld      "Test.x As Integer"
  IL_0045:  leave.s    IL_0075
  IL_0047:  ldarg.0
  IL_0048:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004d:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0052:  ldc.i4.2
  IL_0053:  bne.un.s   IL_005b
  IL_0055:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_005a:  throw
  IL_005b:  leave.s    IL_0075
}
  finally
{
  IL_005d:  ldarg.0
  IL_005e:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0063:  ldc.i4.1
  IL_0064:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0069:  ldarg.0
  IL_006a:  ldfld      "Test.x$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_006f:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0074:  endfinally
}
  IL_0075:  ret
}
]]>)

            verifier.VerifyIL("Test.TestProc2",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      "Test.x As Integer"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub IncompleteInitialization()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Class Test

    Shared Sub Main()
        Try
            TestProc1()
        Catch ex As Exception
            System.Console.WriteLine(ex.GetType())
        End Try

        TestProc1()

        Dim t As New Test()

        Try
            t.TestProc2()
        Catch ex As Exception
            System.Console.WriteLine(ex.GetType())
        End Try

        t.TestProc2()
    End Sub

    Shared Function TestProc1() As Integer
        System.Console.WriteLine("TestProc1")
        Static x As Integer = TestProc1()
        System.Console.WriteLine(x)
        Return 123
    End Function

    Function TestProc2() As Integer
        System.Console.WriteLine("TestProc2")
        Static x As Integer = TestProc2()
        System.Console.WriteLine(x)
        Return 456
    End Function

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            CompileAndVerify(compilation,
            <![CDATA[
TestProc1
TestProc1
Microsoft.VisualBasic.CompilerServices.IncompleteInitialization
TestProc1
0
TestProc2
TestProc2
Microsoft.VisualBasic.CompilerServices.IncompleteInitialization
TestProc2
0
]]>)
        End Sub

        <Fact>
        Public Sub NameConflicts()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class Test

    Shared Sub Main()
        Dim x As Integer = 1

        Static y As Integer = 2

        If x > 0 Then
            Static x As Integer = 3
            Static y As Integer = 4
            Static z As Integer = 5
        Else
            Static z As Integer = 6
        End If
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 'x' hides a variable in an enclosing block.
            Static x As Integer = 3
                   ~
BC30616: Variable 'y' hides a variable in an enclosing block.
            Static y As Integer = 4
                   ~
BC31401: Static local variable 'y' is already declared.
            Static y As Integer = 4
                   ~
BC31401: Static local variable 'z' is already declared.
            Static z As Integer = 6
                   ~
</expected>)
        End Sub

        <Fact>
        Public Sub RestrictedTypes()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Class Test

    Shared Sub Main()
        Static x1 As ArgIterator
        Static x2 As ArgIterator()
        Static x3 As New ArgIterator
        Static x4, x5 As New ArgIterator
        Static x6() As ArgIterator
        Static x7(1) As ArgIterator
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC42024: Unused local variable: 'x1'.
        Static x1 As ArgIterator
               ~~
BC31396: 'System.ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Static x1 As ArgIterator
                     ~~~~~~~~~~~
BC42024: Unused local variable: 'x2'.
        Static x2 As ArgIterator()
               ~~
BC31396: 'System.ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Static x2 As ArgIterator()
                     ~~~~~~~~~~~~~
BC31396: 'System.ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Static x3 As New ArgIterator
                         ~~~~~~~~~~~
BC31396: 'System.ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Static x4, x5 As New ArgIterator
                             ~~~~~~~~~~~
BC42024: Unused local variable: 'x6'.
        Static x6() As ArgIterator
               ~~
BC31396: 'System.ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Static x6() As ArgIterator
                       ~~~~~~~~~~~
BC31396: 'System.ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Static x7(1) As ArgIterator
                        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub StaticInBetween()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Class Test

    Shared Sub Main()
        TestProc()
        TestProc()
    End Sub

    Shared Sub TestProc()
        Dim x As Integer
        Static y As Integer
        Dim z As Integer

        x = 0
        z = -1
        y += 1
        System.Console.WriteLine(x)
        System.Console.WriteLine(y)
        System.Console.WriteLine(z)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe.WithDebugInformationKind(DebugInformationKind.Full))

            CompileAndVerify(compilation,
            <![CDATA[
0
1
-1
0
2
-1
]]>).VerifyIL("Test.TestProc",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.m1
  IL_0003:  ldsfld     "Test.y As Integer"
  IL_0008:  ldc.i4.1
  IL_0009:  add.ovf
  IL_000a:  stsfld     "Test.y As Integer"
  IL_000f:  ldloc.0
  IL_0010:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0015:  ldsfld     "Test.y As Integer"
  IL_001a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0024:  ret
}
]]>)

            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(530442, "DevDiv")>
        <Fact()>
        Public Sub Bug16169()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Public Module Module1
    Public Sub Main()
        MaximumLengthIdentifierIn2012()
        MaximumLengthIdentifierIn2012()
        MaximumLengthIdentifierIn2012()
    End Sub
    Sub MaximumLengthIdentifierIn2012()
        Static abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz As Integer = 1
        Console.WriteLine(abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz.ToString)
        abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz += 1
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, Options.OptionsExe)

            AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation)
        End Sub

    End Class
End Namespace

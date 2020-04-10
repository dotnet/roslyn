' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class IsIsNot
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()

            Dim compilationDef =
<compilation name="IsIsNot1">
    <file name="a.vb">
Option Strict On

Imports System.Console

Module Module1
    Sub Main()
        Dim St As String
        Dim Ob As Object

        St = "c"
        Ob = "c"

        WriteLine("---01---")
        TestBothNothing()

        WriteLine("---02---")
        TestLeftNothing(Nothing)

        WriteLine("---03---")
        TestLeftNothing("a")

        WriteLine("---04---")
        TestRightNothing(Nothing)

        WriteLine("---05---")
        TestRightNothing("b")

        WriteLine("---06---")
        Test(Nothing, Nothing)

        WriteLine("---07---")
        Test(St, Nothing)

        WriteLine("---08---")
        Test(Nothing, St)

        WriteLine("---09---")
        Test(St, St)

        WriteLine("---10---")
        Test(St, Ob)
    End Sub

    Sub Test(left As String, right As Object)
        WriteLine(left Is right)
        WriteLine(left IsNot right)

        If (left Is right) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If

        If (left IsNot right) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If
    End Sub

    Sub TestLeftNothing(Ob As Object)
        WriteLine(Ob Is Nothing)
        WriteLine(Ob IsNot Nothing)

        If (Ob Is Nothing) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If

        If (Ob IsNot Nothing) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If
    End Sub

    Sub TestRightNothing(Ob As Object)
        WriteLine(Nothing Is Ob)
        WriteLine(Nothing IsNot Ob)

        If (Nothing Is Ob) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If

        If (Nothing IsNot Ob) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If
    End Sub

    Sub TestBothNothing()
        WriteLine(Nothing Is Nothing)
        WriteLine(Nothing IsNot Nothing)

        If (Nothing Is Nothing) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If

        If (Nothing IsNot Nothing) AndAlso True Then
            WriteLine(True)
        Else
            WriteLine(False)
        End If
    End Sub

    Sub TestGeneric1(Of T As Class)(x As T, y As T)
        Dim z As Boolean
        z = x Is Nothing
        z = Nothing Is x
        z = x Is y
    End Sub

    Sub TestGeneric2(Of T)(x As T, y As T)
        Dim z As Boolean
        z = x Is Nothing
        z = Nothing Is x
        'z = x Is y
    End Sub

End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=<![CDATA[
---01---
True
False
True
False
---02---
True
False
True
False
---03---
False
True
False
True
---04---
True
False
True
False
---05---
False
True
False
True
---06---
True
False
True
False
---07---
False
True
False
True
---08---
False
True
False
True
---09---
True
False
True
False
---10---
True
False
True
False
]]>).
                VerifyIL("Module1.Test",
            <![CDATA[
{
// Code size       57 (0x39)
.maxstack  2
IL_0000:  ldarg.0
IL_0001:  ldarg.1
IL_0002:  ceq
IL_0004:  call       "Sub System.Console.WriteLine(Boolean)"
IL_0009:  ldarg.0
IL_000a:  ldarg.1
IL_000b:  ceq
IL_000d:  ldc.i4.0
IL_000e:  ceq
IL_0010:  call       "Sub System.Console.WriteLine(Boolean)"
IL_0015:  ldarg.0
IL_0016:  ldarg.1
IL_0017:  bne.un.s   IL_0021
IL_0019:  ldc.i4.1
IL_001a:  call       "Sub System.Console.WriteLine(Boolean)"
IL_001f:  br.s       IL_0027
IL_0021:  ldc.i4.0
IL_0022:  call       "Sub System.Console.WriteLine(Boolean)"
IL_0027:  ldarg.0
IL_0028:  ldarg.1
IL_0029:  beq.s      IL_0032
IL_002b:  ldc.i4.1
IL_002c:  call       "Sub System.Console.WriteLine(Boolean)"
IL_0031:  ret
IL_0032:  ldc.i4.0
IL_0033:  call       "Sub System.Console.WriteLine(Boolean)"
IL_0038:  ret
}
]]>).
                VerifyIL("Module1.TestLeftNothing",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  ceq
  IL_0004:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0009:  ldarg.0
  IL_000a:  ldnull
  IL_000b:  cgt.un
  IL_000d:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0012:  ldarg.0
  IL_0013:  brtrue.s   IL_001d
  IL_0015:  ldc.i4.1
  IL_0016:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001b:  br.s       IL_0023
  IL_001d:  ldc.i4.0
  IL_001e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0023:  ldarg.0
  IL_0024:  brfalse.s  IL_002d
  IL_0026:  ldc.i4.1
  IL_0027:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0033:  ret
}
]]>).
                VerifyIL("Module1.TestRightNothing",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  ceq
  IL_0004:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0009:  ldarg.0
  IL_000a:  ldnull
  IL_000b:  cgt.un
  IL_000d:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0012:  ldarg.0
  IL_0013:  brtrue.s   IL_001d
  IL_0015:  ldc.i4.1
  IL_0016:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001b:  br.s       IL_0023
  IL_001d:  ldc.i4.0
  IL_001e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0023:  ldarg.0
  IL_0024:  brfalse.s  IL_002d
  IL_0026:  ldc.i4.1
  IL_0027:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0033:  ret
}
]]>).
                VerifyIL("Module1.TestBothNothing",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldnull
  IL_0002:  ceq
  IL_0004:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0009:  ldnull
  IL_000a:  ldnull
  IL_000b:  cgt.un
  IL_000d:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0012:  ldc.i4.1
  IL_0013:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0018:  ldc.i4.0
  IL_0019:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001e:  ret
}
]]>).
                VerifyIL("Module1.TestGeneric1",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  box        "T"
  IL_000d:  pop
  IL_000e:  ldarg.0
  IL_000f:  box        "T"
  IL_0014:  pop
  IL_0015:  ldarg.1
  IL_0016:  box        "T"
  IL_001b:  pop
  IL_001c:  ret
}
]]>).
                VerifyIL("Module1.TestGeneric2",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  box        "T"
  IL_000d:  pop
  IL_000e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub Test2()
            Dim compilationDef =
<compilation name="IsIsNot2">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Console

Module Module1
    Sub Main()
        Dim Ob As Object = Nothing
        Dim result As Boolean
        Dim n As Nullable(Of Integer) = Nothing
        Dim De As Decimal = Nothing

        result = (WriteLine() Is doesntexist)
        result = (doesntexist IsNot WriteLine())

        result = (n Is Nothing)
        result = (n IsNot Nothing)
        result = (Nothing Is n)
        result = (Nothing IsNot n)

        result = (n Is Ob)
        result = (n IsNot Ob)
        result = (Ob Is n)
        result = (Ob IsNot n)

        result = (De Is Nothing)
        result = (De IsNot Nothing)
        result = (Nothing Is De)
        result = (Nothing IsNot De)
        result = (De Is De)
        result = (De IsNot De)

    End Sub

    Sub TestGeneric1(Of T)(x As T, y As T)
        Dim z As Boolean
        z = x Is y
        z = x IsNot y

        Dim Ob As Object = Nothing

        z = Ob Is y
        z = Ob IsNot y
    End Sub

    Sub TestGeneric2(Of T As Structure)(x As T, y As T)
        Dim z As Boolean
        z = x Is Nothing
        z = Nothing Is x
        z = x Is y
        z = x IsNot Nothing
        z = Nothing IsNot x
        z = x IsNot y
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            Dim module1 = compilation.GlobalNamespace.GetTypeMembers("Module1").Single()
            Dim TestGeneric2 = module1.GetMembers("TestGeneric2").OfType(Of MethodSymbol)().Single()

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "WriteLine()"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "doesntexist").WithArguments("doesntexist"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "doesntexist").WithArguments("doesntexist"),
                Diagnostic(ERRID.ERR_VoidValue, "WriteLine()"),
                Diagnostic(ERRID.ERR_IsOperatorNullable1, "n").WithArguments("Integer?"),
                Diagnostic(ERRID.ERR_IsNotOperatorNullable1, "n").WithArguments("Integer?"),
                Diagnostic(ERRID.ERR_IsOperatorNullable1, "n").WithArguments("Integer?"),
                Diagnostic(ERRID.ERR_IsNotOperatorNullable1, "n").WithArguments("Integer?"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "De").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_IsOperatorGenericParam1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsOperatorGenericParam1, "y").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOperatorGenericParam1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOperatorGenericParam1, "y").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsOperatorGenericParam1, "y").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOperatorGenericParam1, "y").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsOperatorRequiresReferenceTypes1, "y").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "x").WithArguments("T"),
                Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "y").WithArguments("T"))
        End Sub

    End Class

End Namespace


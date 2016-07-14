' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenTuples
        Inherits BasicTestBase

        <Fact()>
        Public Sub TupleTypeBinding()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t as (Integer, Integer)
        console.writeline(t)            
    End Sub
End Module

Namespace System
    Structure ValueTuple(Of T1, T2)
        Public Overrides Function ToString() As String
            Return "hello"
        End Function
    End Structure
End Namespace

    </file>
</compilation>, expectedOutput:=<![CDATA[
hello
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //t
  IL_0000:  ldloc.0
  IL_0001:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_0006:  call       "Sub System.Console.WriteLine(Object)"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleFieldBinding()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t as (Integer, Integer)

        t.Item1 = 42
        t.Item2 = t.Item1
        console.writeline(t.Item2)            
    End Sub
End Module

Namespace System
    Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
    End Structure
End Namespace

    </file>
</compilation>, expectedOutput:=<![CDATA[
42
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.0
  IL_000c:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0011:  stfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0016:  ldloc.0
  IL_0017:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_001c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleFieldBindingLong()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t as (Integer, Integer, Integer, integer, integer, integer, integer, integer, integer, Integer, Integer, Integer, integer, integer, integer, integer, integer, integer)

        t.Item17 = 42
        t.Item12 = t.Item17
        console.writeline(t.Item12)            
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
42
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_0007:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer)"
  IL_000c:  ldc.i4.s   42
  IL_000e:  stfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer).Item3 As Integer"
  IL_0013:  ldloca.s   V_0
  IL_0015:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_001a:  ldloc.0
  IL_001b:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_0020:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer)"
  IL_0025:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer).Item3 As Integer"
  IL_002a:  stfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Item5 As Integer"
  IL_002f:  ldloc.0
  IL_0030:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_0035:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Item5 As Integer"
  IL_003a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003f:  ret
}
]]>)
        End Sub


        <Fact()>
        Public Sub TupleNamedFieldBinding()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t As (a As Integer, b As Integer)

        t.a = 42
        t.b = t.a

        Console.WriteLine(t.b)
    End Sub
End Module

Namespace System
    Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
    End Structure
End Namespace

    </file>
</compilation>, expectedOutput:=<![CDATA[
42
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.0
  IL_000c:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0011:  stfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0016:  ldloc.0
  IL_0017:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_001c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleNamedFieldBindingLong()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t as (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer, 
                    a5 as integer, a6 as integer, a7 as integer, a8 as integer, 
                    a9 as integer, a10 as Integer, a11 as Integer, a12 as Integer, 
                    a13 as integer, a14 as integer, a15 as integer, a16 as integer, 
                    a17 as integer, a18 as integer)

        t.a17 = 42
        t.a12 = t.a17
        console.writeline(t.a12)            
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
42
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_0007:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer)"
  IL_000c:  ldc.i4.s   42
  IL_000e:  stfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer).Item3 As Integer"
  IL_0013:  ldloca.s   V_0
  IL_0015:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_001a:  ldloc.0
  IL_001b:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_0020:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer)"
  IL_0025:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer).Item3 As Integer"
  IL_002a:  stfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Item5 As Integer"
  IL_002f:  ldloc.0
  IL_0030:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)).Rest As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)"
  IL_0035:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer)).Item5 As Integer"
  IL_003a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003f:  ret
}
]]>)
        End Sub

    End Class

End Namespace


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
        Dim t as (Integer, Integer, Integer, integer, integer, integer, integer, integer, integer, Integer, Integer, String, integer, integer, integer, integer, String, integer)

        t.Item17 = "hello"
        t.Item12 = t.Item17
        console.writeline(t.Item12)            
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
hello
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)).Rest As (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)"
  IL_0007:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, String, Integer, Integer, (Integer, Integer, String, Integer)).Rest As (Integer, Integer, String, Integer)"
  IL_000c:  ldstr      "hello"
  IL_0011:  stfld      "System.ValueTuple(Of Integer, Integer, String, Integer).Item3 As String"
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldflda     "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)).Rest As (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)"
  IL_001d:  ldloc.0
  IL_001e:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)).Rest As (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)"
  IL_0023:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, String, Integer, Integer, (Integer, Integer, String, Integer)).Rest As (Integer, Integer, String, Integer)"
  IL_0028:  ldfld      "System.ValueTuple(Of Integer, Integer, String, Integer).Item3 As String"
  IL_002d:  stfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, String, Integer, Integer, (Integer, Integer, String, Integer)).Item5 As String"
  IL_0032:  ldloc.0
  IL_0033:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)).Rest As (Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer, String, Integer)"
  IL_0038:  ldfld      "System.ValueTuple(Of Integer, Integer, Integer, Integer, String, Integer, Integer, (Integer, Integer, String, Integer)).Item5 As String"
  IL_003d:  call       "Sub System.Console.WriteLine(String)"
  IL_0042:  ret
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
        Public Sub TupleDefaultFieldBinding()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim t As (Integer, Integer) = nothing

        t.Item1 = 42
        t.Item2 = t.Item1

        Console.WriteLine(t.Item2)

        Dim t1 = (A:=1, B:=123)
        Console.WriteLine(t1.B)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
42
123
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.ValueTuple(Of Integer, Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.s   42
  IL_000c:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldloc.0
  IL_0014:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0019:  stfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_001e:  ldloc.0
  IL_001f:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0024:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0029:  ldc.i4.1
  IL_002a:  ldc.i4.s   123
  IL_002c:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0031:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0036:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003b:  ret
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

        <Fact()>
        Public Sub TupleLiteralBinding()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t as (Integer, Integer) = (1, 2)
        console.writeline(t)            
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
(1, 2)
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0007:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_000c:  call       "Sub System.Console.WriteLine(Object)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleLiteralBindingNamed()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t = (A := 1, B := "hello")
        console.writeline(t.B)            
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
hello
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldstr      "hello"
  IL_0006:  newobj     "Sub System.ValueTuple(Of Integer, String)..ctor(Integer, String)"
  IL_000b:  ldfld      "System.ValueTuple(Of Integer, String).Item2 As String"
  IL_0010:  call       "Sub System.Console.WriteLine(String)"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleLiteralSample()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

Module Module1
    Sub Main()

        Dim t As (Integer, Integer) = Nothing
        t.Item1 = 42
        t.Item2 = t.Item1
        Console.WriteLine(t.Item2)

        Dim t1 = (A:=1, B:=123)
        Console.WriteLine(t1.B)

        Dim numbers = {1, 2, 3, 4}

        Dim t2 = Tally(numbers).Result
        System.Console.WriteLine($"Sum: {t2.Sum}, Count: {t2.Count}")

    End Sub

    Public Async Function Tally(values As IEnumerable(Of Integer)) As Task(Of (Sum As Integer, Count As Integer))
        Dim s = 0, c = 0

        For Each n In values
            s += n
            c += 1
        Next

        'Await Task.Yield()

        Return (Sum:=s, Count:=c)
    End Function
End Module


Namespace System
    Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2

        Sub New(item1 as T1, item2 as T2)
            Me.Item1 = item1
            Me.Item2 = item2
        End Sub
    End Structure
End Namespace

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42
123
Sum: 10, Count: 4")

        End Sub

        <Fact()>
        Public Sub Overloading001()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Module m1
    Sub Test(x as (a as integer, b as Integer))
    End Sub

    Sub Test(x as (c as integer, d as Integer))
    End Sub
End module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
    BC30269: 'Public Sub Test(x As (a As Integer, b As Integer))' has multiple definitions with identical signatures.
    Sub Test(x as (a as integer, b as Integer))
        ~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub Overloading002()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Module m1
    Sub Test(x as (integer,Integer))
    End Sub

    Sub Test(x as (a as integer, b as Integer))
    End Sub
End module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC30269: 'Public Sub Test(x As (Integer, Integer))' has multiple definitions with identical signatures.
    Sub Test(x as (integer,Integer))
        ~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim x as (String, String) = (Nothing, Nothing)
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, )
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.ValueTuple(Of String, String) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldnull
  IL_0003:  ldnull
  IL_0004:  call       "Sub System.ValueTuple(Of String, String)..ctor(String, String)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. "System.ValueTuple(Of String, String)"
  IL_0011:  callvirt   "Function Object.ToString() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped002()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim x as (Func(Of integer), Func(of String)) = (Function() 42, Function() "hi")
        System.Console.WriteLine((x.Item1(), x.Item2()).ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(42, hi)
            ]]>)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim x = CType((String, String),(Nothing, Nothing))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, )
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.ValueTuple(Of String, String) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldnull
  IL_0003:  ldnull
  IL_0004:  call       "Sub System.ValueTuple(Of String, String)..ctor(String, String)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. "System.ValueTuple(Of String, String)"
  IL_0011:  callvirt   "Function Object.ToString() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
        End Sub

    End Class

End Namespace


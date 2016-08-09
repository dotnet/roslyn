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
    <CompilerTrait(CompilerFeature.Tuples)>
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
        Public Sub TupleTypeBindingNoTuple()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[

Imports System
Module C

    Sub Main()
        Dim t as (Integer, Integer)
        console.writeline(t)            
        console.writeline(t.Item1)            

        Dim t1 as (A As Integer, B As Integer)
        console.writeline(t1)            
        console.writeline(t1.Item1)            
        console.writeline(t1.A)            
    End Sub
End Module

]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC31091: Import of type 'ValueTuple(Of ,)' from assembly or module 'NoTuples.dll' failed.
        Dim t as (Integer, Integer)
                 ~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'ValueTuple(Of ,)' from assembly or module 'NoTuples.dll' failed.
        Dim t as (Integer, Integer)
                 ~~~~~~~~~~~~~~~~~~
BC30389: '(Integer, Integer).Item1' is not accessible in this context because it is 'Private'.
        console.writeline(t.Item1)            
                          ~~~~~~~
BC31091: Import of type 'ValueTuple(Of ,)' from assembly or module 'NoTuples.dll' failed.
        Dim t1 as (A As Integer, B As Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'ValueTuple(Of ,)' from assembly or module 'NoTuples.dll' failed.
        Dim t1 as (A As Integer, B As Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30389: '(A As Integer, B As Integer).Item1' is not accessible in this context because it is 'Private'.
        console.writeline(t1.Item1)            
                          ~~~~~~~~
BC30389: '(A As Integer, B As Integer).A' is not accessible in this context because it is 'Private'.
        console.writeline(t1.A)            
                          ~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub DataFlow()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Module C

    Sub Main()
        dim initialized as Object = Nothing
        dim baseline_literal = (initialized, initialized)


        dim uninitialized as Object
        dim literal = (uninitialized, uninitialized)

        dim uninitialized1 as Exception
        dim identity_literal as (Alice As Exception, Bob As Exception)  = (uninitialized1, uninitialized1)                
        
        dim uninitialized2 as Exception
        dim converted_literal as (Object, Object)  = (uninitialized2, uninitialized2)                
    End Sub
End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC42104: Variable 'uninitialized' is used before it has been assigned a value. A null reference exception could result at runtime.
        dim literal = (uninitialized, uninitialized)
                       ~~~~~~~~~~~~~
BC42104: Variable 'uninitialized1' is used before it has been assigned a value. A null reference exception could result at runtime.
        dim identity_literal as (Alice As Exception, Bob As Exception)  = (uninitialized1, uninitialized1)                
                                                                           ~~~~~~~~~~~~~~
BC42104: Variable 'uninitialized2' is used before it has been assigned a value. A null reference exception could result at runtime.
        dim converted_literal as (Object, Object)  = (uninitialized2, uninitialized2)                
                                                      ~~~~~~~~~~~~~~

</errors>)

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
        Public Sub TupleFieldBinding01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim vt as ValueTuple(Of Integer, Integer) = M1(2,3)
        console.writeline(vt.Item2)            
    End Sub
    
    Function M1(x As Integer, y As Integer) As ValueTuple(Of Integer, Integer)
        Return New ValueTuple(Of Integer, Integer)(x, y) 
    End Function
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
3
            ]]>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            verifier.VerifyIL("C.M1", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0007:  ret
}
]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  call       "Function C.M1(Integer, Integer) As (Integer, Integer)"
  IL_0007:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_000c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0011:  ret
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

            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(Nothing, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(System.String, System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped001Err()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Module C

    Sub Main()
        Dim x as (A as String, B as String) = (C:=Nothing, D:=Nothing, E:=Nothing)
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC30491: Expression does not produce a value.
        Dim x as (A as String, B as String) = (C:=Nothing, D:=Nothing, E:=Nothing)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)


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
        Public Sub SimpleTupleTargetTyped002a()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim x as (Func(Of integer), Func(of String)) = (Function() 42, Function() Nothing)
        System.Console.WriteLine((x.Item1(), x.Item2()).ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(42, )
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
        Dim x = CType((Nothing, 1),(String, Byte))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, 1)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.ValueTuple(Of String, Byte) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldnull
  IL_0003:  ldc.i4.1
  IL_0004:  call       "Sub System.ValueTuple(Of String, Byte)..ctor(String, Byte)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. "System.ValueTuple(Of String, Byte)"
  IL_0011:  callvirt   "Function Object.ToString() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped004()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim x = DirectCast((Nothing, 1),(String, String))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, 1)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (System.ValueTuple(Of String, String) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldnull
  IL_0003:  ldc.i4.1
  IL_0004:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_0009:  call       "Sub System.ValueTuple(Of String, String)..ctor(String, String)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  constrained. "System.ValueTuple(Of String, String)"
  IL_0016:  callvirt   "Function Object.ToString() As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped005()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as integer = 100
        Dim x = CType((Nothing, i),(String, byte))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (Integer V_0, //i
                System.ValueTuple(Of String, Byte) V_1) //x
  IL_0000:  ldc.i4.s   100
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_1
  IL_0005:  ldnull
  IL_0006:  ldloc.0
  IL_0007:  conv.ovf.u1
  IL_0008:  call       "Sub System.ValueTuple(Of String, Byte)..ctor(String, Byte)"
  IL_000d:  ldloca.s   V_1
  IL_000f:  constrained. "System.ValueTuple(Of String, Byte)"
  IL_0015:  callvirt   "Function Object.ToString() As String"
  IL_001a:  call       "Sub System.Console.WriteLine(String)"
  IL_001f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped006()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as integer = 100
        Dim x = DirectCast((Nothing, i),(String, byte))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (Integer V_0, //i
                System.ValueTuple(Of String, Byte) V_1) //x
  IL_0000:  ldc.i4.s   100
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_1
  IL_0005:  ldnull
  IL_0006:  ldloc.0
  IL_0007:  conv.ovf.u1
  IL_0008:  call       "Sub System.ValueTuple(Of String, Byte)..ctor(String, Byte)"
  IL_000d:  ldloca.s   V_1
  IL_000f:  constrained. "System.ValueTuple(Of String, Byte)"
  IL_0015:  callvirt   "Function Object.ToString() As String"
  IL_001a:  call       "Sub System.Console.WriteLine(String)"
  IL_001f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleTupleTargetTyped007()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as integer = 100
        Dim x = TryCast((Nothing, i),(String, byte))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (Integer V_0, //i
                System.ValueTuple(Of String, Byte) V_1) //x
  IL_0000:  ldc.i4.s   100
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_1
  IL_0005:  ldnull
  IL_0006:  ldloc.0
  IL_0007:  conv.ovf.u1
  IL_0008:  call       "Sub System.ValueTuple(Of String, Byte)..ctor(String, Byte)"
  IL_000d:  ldloca.s   V_1
  IL_000f:  constrained. "System.ValueTuple(Of String, Byte)"
  IL_0015:  callvirt   "Function Object.ToString() As String"
  IL_001a:  call       "Sub System.Console.WriteLine(String)"
  IL_001f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionWidening()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (x as byte, y as byte) = (a:=100, b:=100)
        Dim x as (integer, double) = i
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Double) V_0, //x
                System.ValueTuple(Of Byte, Byte) V_1)
  IL_0000:  ldc.i4.s   100
  IL_0002:  ldc.i4.s   100
  IL_0004:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  IL_000b:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0010:  ldloc.1
  IL_0011:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0016:  conv.r8
  IL_0017:  newobj     "Sub System.ValueTuple(Of Integer, Double)..ctor(Integer, Double)"
  IL_001c:  stloc.0
  IL_001d:  ldloca.s   V_0
  IL_001f:  constrained. "System.ValueTuple(Of Integer, Double)"
  IL_0025:  callvirt   "Function Object.ToString() As String"
  IL_002a:  call       "Sub System.Console.WriteLine(String)"
  IL_002f:  ret
}
]]>)

            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(a:=100, b:=100)", node.ToString())
            Assert.Equal("(a As Integer, b As Integer)", model.GetTypeInfo(node).Type.ToDisplayString())
            Assert.Equal("(x As System.Byte, y As System.Byte)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)
        End Sub

        <Fact()>
        Public Sub TupleConversionNarrowing()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (Integer, String) = (100, 100)
        Dim x as (Byte, Byte) = i
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (System.ValueTuple(Of Byte, Byte) V_0, //x
                System.ValueTuple(Of Integer, String) V_1)
  IL_0000:  ldc.i4.s   100
  IL_0002:  ldc.i4.s   100
  IL_0004:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_0009:  newobj     "Sub System.ValueTuple(Of Integer, String)..ctor(Integer, String)"
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  ldfld      "System.ValueTuple(Of Integer, String).Item1 As Integer"
  IL_0015:  conv.ovf.u1
  IL_0016:  ldloc.1
  IL_0017:  ldfld      "System.ValueTuple(Of Integer, String).Item2 As String"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToByte(String) As Byte"
  IL_0021:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_0026:  stloc.0
  IL_0027:  ldloca.s   V_0
  IL_0029:  constrained. "System.ValueTuple(Of Byte, Byte)"
  IL_002f:  callvirt   "Function Object.ToString() As String"
  IL_0034:  call       "Sub System.Console.WriteLine(String)"
  IL_0039:  ret
}
]]>)

            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(100, 100)", node.ToString())
            Assert.Equal("(Integer, Integer)", model.GetTypeInfo(node).Type.ToDisplayString())
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.NarrowingTuple, model.GetConversion(node).Kind)
        End Sub

        <Fact()>
        Public Sub TupleConversionNarrowingUnchecked()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (Integer, String) = (100, 100)
        Dim x as (Byte, Byte) = i
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, options:=TestOptions.ReleaseExe.WithOverflowChecks(False), expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (System.ValueTuple(Of Byte, Byte) V_0, //x
                System.ValueTuple(Of Integer, String) V_1)
  IL_0000:  ldc.i4.s   100
  IL_0002:  ldc.i4.s   100
  IL_0004:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_0009:  newobj     "Sub System.ValueTuple(Of Integer, String)..ctor(Integer, String)"
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  ldfld      "System.ValueTuple(Of Integer, String).Item1 As Integer"
  IL_0015:  conv.u1
  IL_0016:  ldloc.1
  IL_0017:  ldfld      "System.ValueTuple(Of Integer, String).Item2 As String"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToByte(String) As Byte"
  IL_0021:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_0026:  stloc.0
  IL_0027:  ldloca.s   V_0
  IL_0029:  constrained. "System.ValueTuple(Of Byte, Byte)"
  IL_002f:  callvirt   "Function Object.ToString() As String"
  IL_0034:  call       "Sub System.Console.WriteLine(String)"
  IL_0039:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionObject()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (object, object) = (1, (2,3))
        Dim x as (integer, (integer, integer)) = ctype(i, (integer, (integer, integer)))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(1, (2, 3))
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       86 (0x56)
  .maxstack  3
  .locals init (System.ValueTuple(Of Integer, (Integer, Integer)) V_0, //x
                System.ValueTuple(Of Object, Object) V_1,
                System.ValueTuple(Of Integer, Integer) V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  ldc.i4.2
  IL_0007:  ldc.i4.3
  IL_0008:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_000d:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_0012:  newobj     "Sub System.ValueTuple(Of Object, Object)..ctor(Object, Object)"
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ldfld      "System.ValueTuple(Of Object, Object).Item1 As Object"
  IL_001e:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0023:  ldloc.1
  IL_0024:  ldfld      "System.ValueTuple(Of Object, Object).Item2 As Object"
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0038
  IL_002c:  pop
  IL_002d:  ldloca.s   V_2
  IL_002f:  initobj    "System.ValueTuple(Of Integer, Integer)"
  IL_0035:  ldloc.2
  IL_0036:  br.s       IL_003d
  IL_0038:  unbox.any  "System.ValueTuple(Of Integer, Integer)"
  IL_003d:  newobj     "Sub System.ValueTuple(Of Integer, (Integer, Integer))..ctor(Integer, (Integer, Integer))"
  IL_0042:  stloc.0
  IL_0043:  ldloca.s   V_0
  IL_0045:  constrained. "System.ValueTuple(Of Integer, (Integer, Integer))"
  IL_004b:  callvirt   "Function Object.ToString() As String"
  IL_0050:  call       "Sub System.Console.WriteLine(String)"
  IL_0055:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionOverloadResolution()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim b as (byte, byte) = (100, 100)
        Test(b)
        Dim i as (integer, integer) = b
        Test(i)
        Dim l as (Long, integer) = b
        Test(l)
    End Sub

    Sub Test(x as (integer, integer))
        System.Console.Writeline("integer")
    End SUb

    Sub Test(x as (Long, Long))
        System.Console.Writeline("long")
    End SUb

End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
integer
integer
long            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      101 (0x65)
  .maxstack  3
  .locals init (System.ValueTuple(Of Byte, Byte) V_0,
                System.ValueTuple(Of Long, Integer) V_1)
  IL_0000:  ldc.i4.s   100
  IL_0002:  ldc.i4.s   100
  IL_0004:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0011:  ldloc.0
  IL_0012:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0017:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_001c:  call       "Sub C.Test((Integer, Integer))"
  IL_0021:  dup
  IL_0022:  stloc.0
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0029:  ldloc.0
  IL_002a:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_002f:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0034:  call       "Sub C.Test((Integer, Integer))"
  IL_0039:  stloc.0
  IL_003a:  ldloc.0
  IL_003b:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0040:  conv.u8
  IL_0041:  ldloc.0
  IL_0042:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0047:  newobj     "Sub System.ValueTuple(Of Long, Integer)..ctor(Long, Integer)"
  IL_004c:  stloc.1
  IL_004d:  ldloc.1
  IL_004e:  ldfld      "System.ValueTuple(Of Long, Integer).Item1 As Long"
  IL_0053:  ldloc.1
  IL_0054:  ldfld      "System.ValueTuple(Of Long, Integer).Item2 As Integer"
  IL_0059:  conv.i8
  IL_005a:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_005f:  call       "Sub C.Test((Long, Long))"
  IL_0064:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionNullable001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (x as byte, y as byte)? = (a:=100, b:=100)
        Dim x as (integer, double) = CType(i, (integer, double))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init ((x As Byte, y As Byte)? V_0, //i
                System.ValueTuple(Of Integer, Double) V_1, //x
                System.ValueTuple(Of Byte, Byte) V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   100
  IL_0004:  ldc.i4.s   100
  IL_0006:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_000b:  call       "Sub (x As Byte, y As Byte)?..ctor((x As Byte, y As Byte))"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function (x As Byte, y As Byte)?.get_Value() As (x As Byte, y As Byte)"
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_001e:  ldloc.2
  IL_001f:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0024:  conv.r8
  IL_0025:  newobj     "Sub System.ValueTuple(Of Integer, Double)..ctor(Integer, Double)"
  IL_002a:  stloc.1
  IL_002b:  ldloca.s   V_1
  IL_002d:  constrained. "System.ValueTuple(Of Integer, Double)"
  IL_0033:  callvirt   "Function Object.ToString() As String"
  IL_0038:  call       "Sub System.Console.WriteLine(String)"
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionNullable002()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (x as byte, y as byte) = (a:=100, b:=100)
        Dim x as (integer, double)? = CType(i, (integer, double))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (System.ValueTuple(Of Byte, Byte) V_0, //i
                (Integer, Double)? V_1, //x
                System.ValueTuple(Of Byte, Byte) V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   100
  IL_0004:  ldc.i4.s   100
  IL_0006:  call       "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldloc.0
  IL_000e:  stloc.2
  IL_000f:  ldloc.2
  IL_0010:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0015:  ldloc.2
  IL_0016:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_001b:  conv.r8
  IL_001c:  newobj     "Sub System.ValueTuple(Of Integer, Double)..ctor(Integer, Double)"
  IL_0021:  call       "Sub (Integer, Double)?..ctor((Integer, Double))"
  IL_0026:  ldloca.s   V_1
  IL_0028:  constrained. "(Integer, Double)?"
  IL_002e:  callvirt   "Function Object.ToString() As String"
  IL_0033:  call       "Sub System.Console.WriteLine(String)"
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionNullable003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as (x as byte, y as byte)? = (a:=100, b:=100)
        Dim x = CType(i, (integer, double)?)
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init ((x As Byte, y As Byte)? V_0, //i
                (Integer, Double)? V_1, //x
                (Integer, Double)? V_2,
                System.ValueTuple(Of Byte, Byte) V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   100
  IL_0004:  ldc.i4.s   100
  IL_0006:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_000b:  call       "Sub (x As Byte, y As Byte)?..ctor((x As Byte, y As Byte))"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function (x As Byte, y As Byte)?.get_HasValue() As Boolean"
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_2
  IL_001b:  initobj    "(Integer, Double)?"
  IL_0021:  ldloc.2
  IL_0022:  br.s       IL_0043
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       "Function (x As Byte, y As Byte)?.GetValueOrDefault() As (x As Byte, y As Byte)"
  IL_002b:  stloc.3
  IL_002c:  ldloc.3
  IL_002d:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0032:  ldloc.3
  IL_0033:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0038:  conv.r8
  IL_0039:  newobj     "Sub System.ValueTuple(Of Integer, Double)..ctor(Integer, Double)"
  IL_003e:  newobj     "Sub (Integer, Double)?..ctor((Integer, Double))"
  IL_0043:  stloc.1
  IL_0044:  ldloca.s   V_1
  IL_0046:  constrained. "(Integer, Double)?"
  IL_004c:  callvirt   "Function Object.ToString() As String"
  IL_0051:  call       "Sub System.Console.WriteLine(String)"
  IL_0056:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleConversionNullable004()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim i as ValueTuple(of byte, byte)? = (a:=100, b:=100)
        Dim x = CType(i, ValueTuple(of integer, double)?)
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(100, 100)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init ((Byte, Byte)? V_0, //i
                (Integer, Double)? V_1, //x
                (Integer, Double)? V_2,
                System.ValueTuple(Of Byte, Byte) V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   100
  IL_0004:  ldc.i4.s   100
  IL_0006:  newobj     "Sub System.ValueTuple(Of Byte, Byte)..ctor(Byte, Byte)"
  IL_000b:  call       "Sub (Byte, Byte)?..ctor((Byte, Byte))"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function (Byte, Byte)?.get_HasValue() As Boolean"
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_2
  IL_001b:  initobj    "(Integer, Double)?"
  IL_0021:  ldloc.2
  IL_0022:  br.s       IL_0043
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       "Function (Byte, Byte)?.GetValueOrDefault() As (Byte, Byte)"
  IL_002b:  stloc.3
  IL_002c:  ldloc.3
  IL_002d:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0032:  ldloc.3
  IL_0033:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0038:  conv.r8
  IL_0039:  newobj     "Sub System.ValueTuple(Of Integer, Double)..ctor(Integer, Double)"
  IL_003e:  newobj     "Sub (Integer, Double)?..ctor((Integer, Double))"
  IL_0043:  stloc.1
  IL_0044:  ldloca.s   V_1
  IL_0046:  constrained. "(Integer, Double)?"
  IL_004c:  callvirt   "Function Object.ToString() As String"
  IL_0051:  call       "Sub System.Console.WriteLine(String)"
  IL_0056:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitConversions02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim x = (a:=1, b:=1)
        Dim y As C1 = x
        x = y
        System.Console.WriteLine(x)

        x = CType(CType(x, C1), (integer, integer))
        System.Console.WriteLine(x)

    End Sub
End Module

Class C1
    Public Shared Widening Operator CType(arg as (long, long)) as C1
        return new C1()
    End Operator

    Public Shared Widening Operator CType(arg as C1) as (c As Byte, d as Byte)
        return (2, 2)
    End Operator
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(2, 2)
(2, 2)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      125 (0x7d)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0,
                System.ValueTuple(Of Byte, Byte) V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_000e:  conv.i8
  IL_000f:  ldloc.0
  IL_0010:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0015:  conv.i8
  IL_0016:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_001b:  call       "Function C1.op_Implicit((Long, Long)) As C1"
  IL_0020:  call       "Function C1.op_Implicit(C1) As (c As Byte, d As Byte)"
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_002c:  ldloc.1
  IL_002d:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0032:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0037:  dup
  IL_0038:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_003d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0049:  conv.i8
  IL_004a:  ldloc.0
  IL_004b:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0050:  conv.i8
  IL_0051:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_0056:  call       "Function C1.op_Implicit((Long, Long)) As C1"
  IL_005b:  call       "Function C1.op_Implicit(C1) As (c As Byte, d As Byte)"
  IL_0060:  stloc.1
  IL_0061:  ldloc.1
  IL_0062:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0067:  ldloc.1
  IL_0068:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_006d:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0072:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_0077:  call       "Sub System.Console.WriteLine(Object)"
  IL_007c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ExplicitConversions02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim x = (a:=1, b:=1)
        Dim y As C1 = CType(x, C1)
        x = CTYpe(y, (integer, integer))
        System.Console.WriteLine(x)

        x = CType(CType(x, C1), (integer, integer))
        System.Console.WriteLine(x)

    End Sub
End Module

Class C1
    Public Shared Narrowing Operator CType(arg as (long, long)) as C1
        return new C1()
    End Operator

    Public Shared Narrowing Operator CType(arg as C1) as (c As Byte, d as Byte)
        return (2, 2)
    End Operator
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(2, 2)
(2, 2)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      125 (0x7d)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0,
                System.ValueTuple(Of Byte, Byte) V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_000e:  conv.i8
  IL_000f:  ldloc.0
  IL_0010:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0015:  conv.i8
  IL_0016:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_001b:  call       "Function C1.op_Explicit((Long, Long)) As C1"
  IL_0020:  call       "Function C1.op_Explicit(C1) As (c As Byte, d As Byte)"
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_002c:  ldloc.1
  IL_002d:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0032:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0037:  dup
  IL_0038:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_003d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0049:  conv.i8
  IL_004a:  ldloc.0
  IL_004b:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0050:  conv.i8
  IL_0051:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_0056:  call       "Function C1.op_Explicit((Long, Long)) As C1"
  IL_005b:  call       "Function C1.op_Explicit(C1) As (c As Byte, d As Byte)"
  IL_0060:  stloc.1
  IL_0061:  ldloc.1
  IL_0062:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0067:  ldloc.1
  IL_0068:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_006d:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0072:  box        "System.ValueTuple(Of Integer, Integer)"
  IL_0077:  call       "Sub System.Console.WriteLine(Object)"
  IL_007c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitConversions03()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim x = (a:=1, b:=1)
        Dim y As C1 = x
        Dim x1 as (integer, integer)? = y
        System.Console.WriteLine(x1)

        x1 = CType(CType(x, C1), (integer, integer)?)
        System.Console.WriteLine(x1)

    End Sub
End Module

Class C1
    Public Shared Widening Operator CType(arg as (long, long)) as C1
        return new C1()
    End Operator

    Public Shared Widening Operator CType(arg as C1) as (c As Byte, d as Byte)
        return (2, 2)
    End Operator
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(2, 2)
(2, 2)
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      140 (0x8c)
  .maxstack  3
  .locals init (System.ValueTuple(Of Integer, Integer) V_0, //x
                C1 V_1, //y
                System.ValueTuple(Of Integer, Integer) V_2,
                System.ValueTuple(Of Byte, Byte) V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.1
  IL_0004:  call       "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0009:  ldloc.0
  IL_000a:  stloc.2
  IL_000b:  ldloc.2
  IL_000c:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0011:  conv.i8
  IL_0012:  ldloc.2
  IL_0013:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0018:  conv.i8
  IL_0019:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_001e:  call       "Function C1.op_Implicit((Long, Long)) As C1"
  IL_0023:  stloc.1
  IL_0024:  ldloc.1
  IL_0025:  call       "Function C1.op_Implicit(C1) As (c As Byte, d As Byte)"
  IL_002a:  stloc.3
  IL_002b:  ldloc.3
  IL_002c:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0031:  ldloc.3
  IL_0032:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0037:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_003c:  newobj     "Sub (Integer, Integer)?..ctor((Integer, Integer))"
  IL_0041:  box        "(Integer, Integer)?"
  IL_0046:  call       "Sub System.Console.WriteLine(Object)"
  IL_004b:  ldloc.0
  IL_004c:  stloc.2
  IL_004d:  ldloc.2
  IL_004e:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0053:  conv.i8
  IL_0054:  ldloc.2
  IL_0055:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_005a:  conv.i8
  IL_005b:  newobj     "Sub System.ValueTuple(Of Long, Long)..ctor(Long, Long)"
  IL_0060:  call       "Function C1.op_Implicit((Long, Long)) As C1"
  IL_0065:  call       "Function C1.op_Implicit(C1) As (c As Byte, d As Byte)"
  IL_006a:  stloc.3
  IL_006b:  ldloc.3
  IL_006c:  ldfld      "System.ValueTuple(Of Byte, Byte).Item1 As Byte"
  IL_0071:  ldloc.3
  IL_0072:  ldfld      "System.ValueTuple(Of Byte, Byte).Item2 As Byte"
  IL_0077:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_007c:  newobj     "Sub (Integer, Integer)?..ctor((Integer, Integer))"
  IL_0081:  box        "(Integer, Integer)?"
  IL_0086:  call       "Sub System.Console.WriteLine(Object)"
  IL_008b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ImplicitConversions04()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim x = (1, (1, (1, (1, (1, (1, 1))))))
        Dim y as C1 = x   

        Dim x2 as (integer, integer) = y
        System.Console.WriteLine(x2)

        Dim x3 as (integer, (integer, integer)) = y
        System.Console.WriteLine(x3)
 
        Dim x12 as (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, Integer))))))))))) = y
        System.Console.WriteLine(x12)

    End Sub
End Module

Class C1
    Private x as Byte

    Public Shared Widening Operator CType(arg as (long, C1)) as C1
        Dim result = new C1()
        result.x = arg.Item2.x
        return result
    End Operator

    Public Shared Widening Operator CType(arg as (long, long)) as C1
        Dim result = new C1()
        result.x = CByte(arg.Item2)
        return result
    End Operator

    Public Shared Widening Operator CType(arg as C1) as (c As Byte, d as C1)
        Dim t = arg.x
        arg.x += 1
        return (CByte(t), arg)
    End Operator

    Public Shared Widening Operator CType(arg as C1) as (c As Byte, d as Byte)
        Dim t1 = arg.x
        arg.x += 1
        Dim t2 = arg.x
        arg.x += 1
        return (CByte(t1), CByte(t2))
    End Operator
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(1, 2)
(3, (4, 5))
(6, (7, (8, (9, (10, (11, (12, (13, (14, (15, (16, 17)))))))))))
            ]]>)

        End Sub

        <Fact()>
        Public Sub ExplicitConversions04()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim x = (1, (1, (1, (1, (1, (1, 1))))))
        Dim y as C1 = x   

        Dim x2 as (integer, integer) = y
        System.Console.WriteLine(x2)

        Dim x3 as (integer, (integer, integer)) = y
        System.Console.WriteLine(x3)
 
        Dim x12 as (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, (Integer, Integer))))))))))) = y
        System.Console.WriteLine(x12)

    End Sub
End Module

Class C1
    Private x as Byte

    Public Shared Narrowing Operator CType(arg as (long, C1)) as C1
        Dim result = new C1()
        result.x = arg.Item2.x
        return result
    End Operator

    Public Shared Narrowing Operator CType(arg as (long, long)) as C1
        Dim result = new C1()
        result.x = CByte(arg.Item2)
        return result
    End Operator

    Public Shared Narrowing Operator CType(arg as C1) as (c As Byte, d as C1)
        Dim t = arg.x
        arg.x += 1
        return (CByte(t), arg)
    End Operator

    Public Shared Narrowing Operator CType(arg as C1) as (c As Byte, d as Byte)
        Dim t1 = arg.x
        arg.x += 1
        Dim t2 = arg.x
        arg.x += 1
        return (CByte(t1), CByte(t2))
    End Operator
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(1, 2)
(3, (4, 5))
(6, (7, (8, (9, (10, (11, (12, (13, (14, (15, (16, 17)))))))))))
            ]]>)
        End Sub

        <Fact()>
        Public Sub MethodTypeInference001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        System.Console.WriteLine(Test((1,"q")))
    End Sub

    Function Test(of T1, T2)(x as (T1, T2)) as (T1, T2)
        Console.WriteLine(Gettype(T1))
        Console.WriteLine(Gettype(T2))
        
        return x
    End Function

End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Int32
System.String
(1, q)
            ]]>)

        End Sub

        <Fact()>
        Public Sub MethodTypeInference002()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim v = (new Object(),"q")
        
        Test(v)
        System.Console.WriteLine(v)
    End Sub

    Function Test(of T)(x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
        
        return x
    End Function
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Object
(System.Object, q)

            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (System.ValueTuple(Of Object, String) V_0)
  IL_0000:  newobj     "Sub Object..ctor()"
  IL_0005:  ldstr      "q"
  IL_000a:  newobj     "Sub System.ValueTuple(Of Object, String)..ctor(Object, String)"
  IL_000f:  dup
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldfld      "System.ValueTuple(Of Object, String).Item1 As Object"
  IL_0017:  ldloc.0
  IL_0018:  ldfld      "System.ValueTuple(Of Object, String).Item2 As String"
  IL_001d:  newobj     "Sub System.ValueTuple(Of Object, Object)..ctor(Object, Object)"
  IL_0022:  call       "Function C.Test(Of Object)((Object, Object)) As (Object, Object)"
  IL_0027:  pop
  IL_0028:  box        "System.ValueTuple(Of Object, String)"
  IL_002d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0032:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub MethodTypeInference002Err()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Module C
    Sub Main()
        Dim v = (new Object(),"q")
        
        Test(v)
        System.Console.WriteLine(v)

        TestRef(v)
        System.Console.WriteLine(v)
    End Sub

    Function Test(of T)(x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
        
        return x
    End Function

    Function TestRef(of T)(ByRef x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
    
        x.Item1 = x.Item2    

        return x
    End Function

End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36651: Data type(s) of the type parameter(s) in method 'Public Function TestRef(Of T)(ByRef x As (T, T)) As (T, T)' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        TestRef(v)
        ~~~~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub MethodTypeInference003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        System.Console.WriteLine(Test((Nothing,"q")))
    End Sub

    Function Test(of T)(x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
        
        return x
    End Function
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.String
(, q)
            ]]>)

        End Sub

        <Fact()>
        Public Sub MethodTypeInference004()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        System.Console.WriteLine(Test((new Object(),"q")))
        System.Console.WriteLine(Test1((new Object(),"q")))
    End Sub

    Function Test(of T)(x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
        
        return x
    End Function

    Function Test1(of T)(x as T) as T
        Console.WriteLine(Gettype(T))
        
        return x
    End Function
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Object
(System.Object, q)
System.ValueTuple`2[System.Object,System.String]
(System.Object, q)
            ]]>)

        End Sub

        <Fact()>
        Public Sub MethodTypeInference004Err()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Module C
    Sub Main()
        Dim q = "q"
        Dim a as object = "a"

        System.Console.WriteLine(Test((q, a)))
      
        System.Console.WriteLine(q)
        System.Console.WriteLine(a)
    End Sub

    Function Test(of T)(byref x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
    
        x.Item1 = x.Item2

        return x
    End Function
End Module
]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36651: Data type(s) of the type parameter(s) in method 'Public Function Test(Of T)(ByRef x As (T, T)) As (T, T)' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        System.Console.WriteLine(Test((q, a)))
                                 ~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub MethodTypeInference005()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim ie As IEnumerable(Of String) = {1, 2}
        Dim t = (ie, ie)
        Test(t, New Object)
        Test((ie, ie), New Object)
    End Sub

    Sub Test(Of T)(a1 As (IEnumerable(Of T), IEnumerable(Of T)), a2 As T)
        System.Console.WriteLine(GetType(T))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Object
System.Object
            ]]>)
        End Sub

        <Fact()>
        Public Sub MethodTypeInference006()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim ie As IEnumerable(Of Integer) = {1, 2}
        Dim t = (ie, ie)
        Test(t)
        Test((1, 1))
    End Sub

    Sub Test(Of T)(f1 As IComparable(Of (T, T)))
        System.Console.WriteLine(GetType(T))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Collections.Generic.IEnumerable`1[System.Int32]
System.Int32
            ]]>)
        End Sub

        <Fact()>
        Public Sub MethodTypeInference007()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim ie As IEnumerable(Of Integer) = {1, 2}
        Dim t = (ie, ie)
        Test(t)
        Test((1, 1))
    End Sub

    Sub Test(Of T)(f1 As IComparable(Of (T, T)))
        System.Console.WriteLine(GetType(T))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Collections.Generic.IEnumerable`1[System.Int32]
System.Int32
            ]]>)
        End Sub

        <Fact()>
        Public Sub MethodTypeInference008()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim t = (1, 1L)

        ' these are valid
        Test1(t)
        Test1((1, 1L))
    End Sub

    Sub Test1(Of T)(f1 As (T, T))
        System.Console.WriteLine(GetType(T))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Int64
System.Int64
            ]]>)
        End Sub

        <Fact()>
        Public Sub MethodTypeInference008Err()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim t = (1, 1L)

        ' these are valid
        Test1(t)
        Test1((1, 1L))

        ' these are not
        Test2(t)
        Test2((1, 1L))
    End Sub

    Sub Test1(Of T)(f1 As (T, T))
        System.Console.WriteLine(GetType(T))
    End Sub

    Sub Test2(Of T)(f1 As IComparable(Of (T, T)))
        System.Console.WriteLine(GetType(T))
    End Sub
End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36657: Data type(s) of the type parameter(s) in method 'Public Sub Test2(Of T)(f1 As IComparable(Of (T, T)))' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        Test2(t)
        ~~~~~
BC36657: Data type(s) of the type parameter(s) in method 'Public Sub Test2(Of T)(f1 As IComparable(Of (T, T)))' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        Test2((1, 1L))
        ~~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub Inference04()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Test( Function(x)x.y )
        Test( Function(x)x.bob )
    End Sub

    Sub Test(of T)(x as Func(of (x as Byte, y As Byte), T))
        System.Console.WriteLine("first")
        System.Console.WriteLine(x((2,3)).ToString())
    End Sub

    Sub Test(of T)(x as Func(of (alice as integer, bob as integer), T))
        System.Console.WriteLine("second")
        System.Console.WriteLine(x((4,5)).ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
first
3
second
5
            ]]>)
        End Sub

        <Fact()>
        Public Sub Inference07()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Test(Function(x)(x, x), Function(t)1)
        Test1(Function(x)(x, x), Function(t)1)
        Test2((a:= 1, b:= 2), Function(t)(t.a, t.b))
    End Sub

    Sub Test(Of U)(f1 as Func(of Integer, ValueTuple(Of U, U)), f2 as Func(Of ValueTuple(Of U, U), Integer))
        System.Console.WriteLine(f2(f1(1)))
    End Sub

    Sub Test1(of U)(f1 As Func(of integer, (U, U)), f2 as Func(Of (U, U), integer))
        System.Console.WriteLine(f2(f1(1)))
    End Sub 

    Sub Test2(of U, T)(f1 as U , f2 As Func(Of U, (x as T, y As T)))
        System.Console.WriteLine(f2(f1).y)
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
1
1
2
            ]]>)
        End Sub

        <Fact()>
        Public Sub InferenceChain001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Test(Function(x As (Integer, Integer)) (x, x), Function(t) (t, t))
    End Sub

    Sub Test(Of T, U, V)(f1 As Func(Of (T,T), (U,U)), f2 As Func(Of (U,U), (V,V)))
        System.Console.WriteLine(f2(f1(Nothing)))

        System.Console.WriteLine(GetType(T))
        System.Console.WriteLine(GetType(U))
        System.Console.WriteLine(GetType(V))
    End Sub


End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(((0, 0), (0, 0)), ((0, 0), (0, 0)))
System.Int32
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.ValueTuple`2[System.Int32,System.Int32],System.ValueTuple`2[System.Int32,System.Int32]]

            ]]>)
        End Sub

        <Fact()>
        Public Sub InferenceChain002()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System
Module Module1
    Sub Main()
        Test(Function(x As (Integer, Object)) (x, x), Function(t) (t, t))
    End Sub

    Sub Test(Of T, U, V)(ByRef f1 As Func(Of (T, T), (U, U)), ByRef f2 As Func(Of (U, U), (V, V)))
        System.Console.WriteLine(f2(f1(Nothing)))

        System.Console.WriteLine(GetType(T))
        System.Console.WriteLine(GetType(U))
        System.Console.WriteLine(GetType(V))
    End Sub


End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(((0, ), (0, )), ((0, ), (0, )))
System.Object
System.ValueTuple`2[System.Int32,System.Object]
System.ValueTuple`2[System.ValueTuple`2[System.Int32,System.Object],System.ValueTuple`2[System.Int32,System.Object]]

            ]]>)
        End Sub
    End Class

End Namespace


' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    <CompilerTrait(CompilerFeature.Tuples)>
    Public Class CodeGenTuples
        Inherits BasicTestBase

        ReadOnly s_valueTupleRefs As MetadataReference() = New MetadataReference() {ValueTupleRef, SystemRuntimeFacadeRef}
        ReadOnly s_valueTupleRefsAndDefault As MetadataReference() = New MetadataReference() {ValueTupleRef,
                                                                                                SystemRuntimeFacadeRef,
                                                                                                MscorlibRef,
                                                                                                SystemRef,
                                                                                                SystemCoreRef,
                                                                                                MsvbRef}


        ReadOnly s_trivial2uple As String = "
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return ""{"" + Item1?.ToString() + "", "" + Item2?.ToString() + ""}""
        End Function
    End Structure
End Namespace

namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter Or AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Class Or AttributeTargets.Struct )>
    public class TupleElementNamesAttribute : Inherits Attribute
        public Sub New(transformNames As String())
	    End Sub
    End Class
End Namespace

"
        ReadOnly s_trivial3uple As String = "
Namespace System
    Public Structure ValueTuple(Of T1, T2, T3)
        Public Dim Item1 As T1
        Public Dim Item2 As T2
        Public Dim Item3 As T3

        Public Sub New(item1 As T1, item2 As T2, item3 As T3)
            me.Item1 = item1
            me.Item2 = item2
            me.Item3 = item3
        End Sub
    End Structure
End Namespace

"
        ReadOnly s_trivialRemainingTuples As String = "
Namespace System
    Public Structure ValueTuple(Of T1, T2, T3, T4)
        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4)
        End Sub
    End Structure

    Public Structure ValueTuple(Of T1, T2, T3, T4, T5)
        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5)
        End Sub
    End Structure

    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6)
        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6)
        End Sub
    End Structure

    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6, T7)
        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7)
        End Sub
    End Structure

    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)
        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7, rest As TRest)
        End Sub
    End Structure
End Namespace
"

        <Fact>
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

        <Fact>
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
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim t as (Integer, Integer)
                 ~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim t as (Integer, Integer)
                 ~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim t1 as (A As Integer, B As Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim t1 as (A As Integer, B As Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
        Dim t1 as (A As Integer, B As Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleDefaultType001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t = (Nothing, Nothing)
        console.writeline(t)            
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
(, )
            ]]>)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldnull
  IL_0002:  newobj     "Sub System.ValueTuple(Of Object, Object)..ctor(Object, Object)"
  IL_0007:  box        "System.ValueTuple(Of Object, Object)"
  IL_000c:  call       "Sub System.Console.WriteLine(Object)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType001err()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[

Imports System
Module C

    Sub Main()
        Dim t = (Nothing, Nothing)
        console.writeline(t)            
    End Sub
End Module

]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim t = (Nothing, Nothing)
                ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType002()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t1 = ({Nothing}, {Nothing})
        console.writeline(t1.GetType())            

        Dim t2 = {(Nothing, Nothing)}
        console.writeline(t2.GetType())            
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.ValueTuple`2[System.Object[],System.Object[]]
System.ValueTuple`2[System.Object,System.Object][]
            ]]>)

        End Sub

        <Fact()>
        Public Sub TupleDefaultType003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim t3 = Function(){(Nothing, Nothing)}
        console.writeline(t3.GetType())            

        Dim t4 = {Function()(Nothing, Nothing)}
        console.writeline(t4.GetType())            

    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
VB$AnonymousDelegate_0`1[System.ValueTuple`2[System.Object,System.Object][]]
VB$AnonymousDelegate_0`1[System.ValueTuple`2[System.Object,System.Object]][]
            ]]>)

        End Sub

        <Fact()>
        Public Sub TupleDefaultType004()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Test(({Nothing}, {{Nothing}}))
        Test((Function(x as integer)x, Function(x as Long)x))
    End Sub

    function Test(of T, U)(x as (T, U)) as (U, T)
        System.Console.WriteLine(GetType(T))
        System.Console.WriteLine(GetType(U))
        System.Console.WriteLine()

        return (x.Item2, x.Item1)
    End Function
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Object[]
System.Object[,]

VB$AnonymousDelegate_0`2[System.Int32,System.Int32]
VB$AnonymousDelegate_0`2[System.Int64,System.Int64]
            ]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType005()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Test((Function(x as integer)Function()x, Function(x as Long){({Nothing}, {Nothing})}))
    End Sub

    function Test(of T, U)(x as (T, U)) as (U, T)
        System.Console.WriteLine(GetType(T))
        System.Console.WriteLine(GetType(U))
        System.Console.WriteLine()

        return (x.Item2, x.Item1)
    End Function
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
VB$AnonymousDelegate_0`2[System.Int32,VB$AnonymousDelegate_1`1[System.Int32]]
VB$AnonymousDelegate_0`2[System.Int64,System.ValueTuple`2[System.Object[],System.Object[]][]]
            ]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType006()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Test((Nothing, Nothing), "q")
        Test((Nothing, "q"), Nothing)

        Test1("q", (Nothing, Nothing))
        Test1(Nothing, ("q", Nothing))

    End Sub

    function Test(of T)(x as (T, T), y as T) as T
        System.Console.WriteLine(GetType(T))

        return y
    End Function

    function Test1(of T)(x as T, y as (T, T)) as T
        System.Console.WriteLine(GetType(T))

        return x
    End Function

End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.String
System.String
System.String
System.String
            ]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType006err()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Module C

    Sub Main()
        Test1((Nothing, Nothing), Nothing)
        Test2(Nothing, (Nothing, Nothing))
    End Sub

    function Test1(of T)(x as (T, T), y as T) as T
        System.Console.WriteLine(GetType(T))
        return y
    End Function

    function Test2(of T)(x as T, y as (T, T)) as T
        System.Console.WriteLine(GetType(T))
        return x
    End Function
End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36645: Data type(s) of the type parameter(s) in method 'Public Function Test1(Of T)(x As (T, T), y As T) As T' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test1((Nothing, Nothing), Nothing)
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Function Test2(Of T)(x As T, y As (T, T)) As T' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2(Nothing, (Nothing, Nothing))
        ~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType007()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim valid As (A as Action, B as Action) = (AddressOf Main, AddressOf Main)
        Test2(valid)

        Test2((AddressOf Main, Sub() Main))
    End Sub

    function Test2(of T)(x as (T, T)) as (T, T)
        System.Console.WriteLine(GetType(T))
        return x
    End Function
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.Action
VB$AnonymousDelegate_0
            ]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType007err()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Module C

    Sub Main()
        Dim x = (AddressOf Main, AddressOf Main)
        Dim x1 = (Function() Main, Function() Main)
        Dim x2 = (AddressOf Mai, Function() Mai)
        Dim x3 = (A := AddressOf Main, B := (D := AddressOf Main, C := AddressOf Main))
        
        Test1((AddressOf Main, Sub() Main))
        Test1((AddressOf Main, Function() Main))
        Test2((AddressOf Main, Function() Main))
    End Sub

    function Test1(of T)(x as T) as T
        System.Console.WriteLine(GetType(T))
        return x
    End Function

    function Test2(of T)(x as (T, T)) as (T, T)
        System.Console.WriteLine(GetType(T))
        return x
    End Function
End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC30491: Expression does not produce a value.
        Dim x = (AddressOf Main, AddressOf Main)
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30491: Expression does not produce a value.
        Dim x1 = (Function() Main, Function() Main)
                             ~~~~
BC30491: Expression does not produce a value.
        Dim x1 = (Function() Main, Function() Main)
                                              ~~~~
BC30451: 'Mai' is not declared. It may be inaccessible due to its protection level.
        Dim x2 = (AddressOf Mai, Function() Mai)
                            ~~~
BC30491: Expression does not produce a value.
        Dim x3 = (A := AddressOf Main, B := (D := AddressOf Main, C := AddressOf Main))
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Function Test1(Of T)(x As T) As T' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test1((AddressOf Main, Sub() Main))
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Function Test1(Of T)(x As T) As T' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test1((AddressOf Main, Function() Main))
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Function Test2(Of T)(x As (T, T)) As (T, T)' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2((AddressOf Main, Function() Main))
        ~~~~~
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
</compilation>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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

        <Fact>
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
            ]]>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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
            ]]>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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

namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter Or AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Class Or AttributeTargets.Struct )>
    public class TupleElementNamesAttribute : Inherits Attribute
        public Sub New(transformNames As String())
	    End Sub
    End Class
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


        <Fact>
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
            ]]>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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
            ]]>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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
            ]]>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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
            ]]>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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

namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter Or AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Class Or AttributeTargets.Struct )>
    public class TupleElementNamesAttribute : Inherits Attribute
        public Sub New(transformNames As String())
	    End Sub
    End Class
End Namespace

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42
123
Sum: 10, Count: 4")

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
    BC30269: 'Public Sub Test(x As (a As Integer, b As Integer))' has multiple definitions with identical signatures.
    Sub Test(x as (a as integer, b as Integer))
        ~~~~
</errors>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30269: 'Public Sub Test(x As (Integer, Integer))' has multiple definitions with identical signatures.
    Sub Test(x as (integer,Integer))
        ~~~~
</errors>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(C As Object, D As Object, E As Object)' cannot be converted to '(A As String, B As String)'.
        Dim x as (A as String, B as String) = (C:=Nothing, D:=Nothing, E:=Nothing)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(42, hi)
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(42, )
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.ReleaseExe.WithOverflowChecks(False), expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(1, 2)
(3, (4, 5))
(6, (7, (8, (9, (10, (11, (12, (13, (14, (15, (16, 17)))))))))))
            ]]>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(1, 2)
(3, (4, 5))
(6, (7, (8, (9, (10, (11, (12, (13, (14, (15, (16, 17)))))))))))
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Int32
System.String
(1, q)
            ]]>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC36651: Data type(s) of the type parameter(s) in method 'Public Function TestRef(Of T)(ByRef x As (T, T)) As (T, T)' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        TestRef(v)
        ~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub MethodTypeInference003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        System.Console.WriteLine(Test((Nothing,"q")))
        System.Console.WriteLine(Test(("q", Nothing)))

        System.Console.WriteLine(Test1((Nothing, Nothing), (Nothing,"q")))
        System.Console.WriteLine(Test1(("q", Nothing), (Nothing, Nothing)))
    End Sub

    Function Test(of T)(x as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))

        return x
    End Function

        Function Test1(of T)(x as (T, T), y as (T, T)) as (T, T)
        Console.WriteLine(Gettype(T))
        
        return x
    End Function
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.String
(, q)
System.String
(q, )
System.String
(, )
System.String
(q, )
            ]]>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Object
(System.Object, q)
System.ValueTuple`2[System.Object,System.String]
(System.Object, q)
            ]]>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC36651: Data type(s) of the type parameter(s) in method 'Public Function Test(Of T)(ByRef x As (T, T)) As (T, T)' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        System.Console.WriteLine(Test((q, a)))
                                 ~~~~
</errors>)

        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Object
System.Object
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Collections.Generic.IEnumerable`1[System.Int32]
System.Int32
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Collections.Generic.IEnumerable`1[System.Int32]
System.Int32
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Int64
System.Int64
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

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

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
first
3
second
5
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
1
1
2
            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(((0, 0), (0, 0)), ((0, 0), (0, 0)))
System.Int32
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.ValueTuple`2[System.Int32,System.Int32],System.ValueTuple`2[System.Int32,System.Int32]]

            ]]>)
        End Sub

        <Fact>
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
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(((0, ), (0, )), ((0, ), (0, )))
System.Object
System.ValueTuple`2[System.Int32,System.Object]
System.ValueTuple`2[System.ValueTuple`2[System.Int32,System.Object],System.ValueTuple`2[System.Int32,System.Object]]

            ]]>)
        End Sub

        <Fact>
        Public Sub SimpleTupleNested()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = (1, (2, (3, 4)).ToString())
        System.Console.Write(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(1, (2, (3, 4)))]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  5
  .locals init (System.ValueTuple(Of Integer, String) V_0, //x
                System.ValueTuple(Of Integer, (Integer, Integer)) V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_000b:  newobj     "Sub System.ValueTuple(Of Integer, (Integer, Integer))..ctor(Integer, (Integer, Integer))"
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  constrained. "System.ValueTuple(Of Integer, (Integer, Integer))"
  IL_0019:  callvirt   "Function Object.ToString() As String"
  IL_001e:  call       "Sub System.ValueTuple(Of Integer, String)..ctor(Integer, String)"
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. "System.ValueTuple(Of Integer, String)"
  IL_002b:  callvirt   "Function Object.ToString() As String"
  IL_0030:  call       "Sub System.Console.Write(String)"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleUnderlyingItemAccess()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = (1, 2)
        System.Console.WriteLine(x.Item2.ToString())
        x.Item1 = 40
        System.Console.WriteLine(x.Item1 + x.Item2)
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0010:  call       "Function Integer.ToString() As String"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0029:  ldloc.0
  IL_002a:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_002f:  add.ovf
  IL_0030:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleUnderlyingItemAccess01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = (A:=1, B:=2)
        System.Console.WriteLine(x.Item2.ToString())
        x.Item1 = 40
        System.Console.WriteLine(x.Item1 + x.Item2)
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0010:  call       "Function Integer.ToString() As String"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0029:  ldloc.0
  IL_002a:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_002f:  add.ovf
  IL_0030:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleItemAccess()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = (A:=1, B:=2)
        System.Console.WriteLine(x.Item2.ToString())
        x.A = 40
        System.Console.WriteLine(x.A + x.B)
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0010:  call       "Function Integer.ToString() As String"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0029:  ldloc.0
  IL_002a:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_002f:  add.ovf
  IL_0030:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleItemAccess01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = (A:=1, B:=(C:=2, D:= 3))
        System.Console.WriteLine(x.B.C.ToString())
        x.B.D = 39
        System.Console.WriteLine(x.A + x.B.C + x.B.D)
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       87 (0x57)
  .maxstack  4
  .locals init (System.ValueTuple(Of Integer, (C As Integer, D As Integer)) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_000a:  call       "Sub System.ValueTuple(Of Integer, (C As Integer, D As Integer))..ctor(Integer, (C As Integer, D As Integer))"
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldflda     "System.ValueTuple(Of Integer, (C As Integer, D As Integer)).Item2 As (C As Integer, D As Integer)"
  IL_0016:  ldflda     "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_001b:  call       "Function Integer.ToString() As String"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldflda     "System.ValueTuple(Of Integer, (C As Integer, D As Integer)).Item2 As (C As Integer, D As Integer)"
  IL_002c:  ldc.i4.s   39
  IL_002e:  stfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0033:  ldloc.0
  IL_0034:  ldfld      "System.ValueTuple(Of Integer, (C As Integer, D As Integer)).Item1 As Integer"
  IL_0039:  ldloc.0
  IL_003a:  ldfld      "System.ValueTuple(Of Integer, (C As Integer, D As Integer)).Item2 As (C As Integer, D As Integer)"
  IL_003f:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0044:  add.ovf
  IL_0045:  ldloc.0
  IL_0046:  ldfld      "System.ValueTuple(Of Integer, (C As Integer, D As Integer)).Item2 As (C As Integer, D As Integer)"
  IL_004b:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_0050:  add.ovf
  IL_0051:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0056:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleTypeDeclaration()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As (Integer, String, Integer) = (1, "hello", 2)
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(1, hello, 2)]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  4
  .locals init (System.ValueTuple(Of Integer, String, Integer) V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldstr      "hello"
  IL_0008:  ldc.i4.2
  IL_0009:  call       "Sub System.ValueTuple(Of Integer, String, Integer)..ctor(Integer, String, Integer)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  constrained. "System.ValueTuple(Of Integer, String, Integer)"
  IL_0016:  callvirt   "Function Object.ToString() As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleTypeMismatch_01()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Module C
    Sub Main()
             Dim x As (Integer, String) = (1, "hello", 2)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Integer, String, Integer)' cannot be converted to '(Integer, String)'.
             Dim x As (Integer, String) = (1, "hello", 2)
                                          ~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleTypeMismatch_02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Module C
    Sub Main()
        Dim x As (Integer, String) = (1, Nothing, 2)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Integer, Object, Integer)' cannot be converted to '(Integer, String)'.
        Dim x As (Integer, String) = (1, Nothing, 2)
                                     ~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub LongTupleTypeMismatch()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Module C
    Sub Main()
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = ("Alice", 2, 3, 4, 5, 6, 7, 8)
        Dim y As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8)
    End Sub
End Module
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC37267: Predefined type 'ValueTuple(Of )' is not defined or imported.
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = ("Alice", 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = ("Alice", 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = ("Alice", 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of )' is not defined or imported.
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = ("Alice", 2, 3, 4, 5, 6, 7, 8)
                                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = ("Alice", 2, 3, 4, 5, 6, 7, 8)
                                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of )' is not defined or imported.
        Dim y As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
        Dim y As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
        Dim y As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of )' is not defined or imported.
        Dim y As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8)
                                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
        Dim y As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8)
                                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleTypeWithLateDiscoveredName()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb"><![CDATA[
Imports System
Module C
    Sub Main()
        Dim x As (Integer, A As String) = (1, "hello", C:=2)
    End Sub
End Module

]]></file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Integer, String, C As Integer)' cannot be converted to '(Integer, A As String)'.
        Dim x As (Integer, A As String) = (1, "hello", C:=2)
                                          ~~~~~~~~~~~~~~~~~~
</errors>)


            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(1, ""hello"", C:=2)", node.ToString())
            Assert.Equal("(System.Int32, System.String, C As System.Int32)", model.GetTypeInfo(node).Type.ToTestDisplayString())

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Dim xSymbol = DirectCast(model.GetDeclaredSymbol(x), LocalSymbol).Type
            Assert.Equal("(System.Int32, A As System.String)", xSymbol.ToTestDisplayString())
            Assert.True(xSymbol.IsTupleType)

            Assert.Equal({"System.Int32", "System.String"}, xSymbol.TupleElementTypes.SelectAsArray(Function(t) t.ToTestDisplayString()))
            Assert.Equal({Nothing, "A"}, xSymbol.TupleElementNames)
        End Sub

        <Fact>
        Public Sub TupleTypeDeclarationWithNames()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As (A As Integer, B As String) = (1, "hello")
        System.Console.WriteLine(x.A.ToString())
        System.Console.WriteLine(x.B.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[1
hello]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleDictionary01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic

Class C
    Shared Sub Main()
        Dim k = (1, 2)
        Dim v = (A:=1, B:=(C:=2, D:=(E:=3, F:=4)))

        Dim d = Test(k, v)
        System.Console.Write(d((1, 2)).B.D.Item2)
    End Sub

    Shared Function Test(Of K, V)(key As K, value As V) As Dictionary(Of K, V)
        Dim d = new Dictionary(Of K, V)()
        d(key) = value
        return d
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[4]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  6
  .locals init (System.ValueTuple(Of Integer, (C As Integer, D As (E As Integer, F As Integer))) V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  ldc.i4.3
  IL_000c:  ldc.i4.4
  IL_000d:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0012:  newobj     "Sub System.ValueTuple(Of Integer, (E As Integer, F As Integer))..ctor(Integer, (E As Integer, F As Integer))"
  IL_0017:  call       "Sub System.ValueTuple(Of Integer, (C As Integer, D As (E As Integer, F As Integer)))..ctor(Integer, (C As Integer, D As (E As Integer, F As Integer)))"
  IL_001c:  ldloc.0
  IL_001d:  call       "Function C.Test(Of (Integer, Integer), (A As Integer, B As (C As Integer, D As (E As Integer, F As Integer))))((Integer, Integer), (A As Integer, B As (C As Integer, D As (E As Integer, F As Integer)))) As System.Collections.Generic.Dictionary(Of (Integer, Integer), (A As Integer, B As (C As Integer, D As (E As Integer, F As Integer))))"
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  newobj     "Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)"
  IL_0029:  callvirt   "Function System.Collections.Generic.Dictionary(Of (Integer, Integer), (A As Integer, B As (C As Integer, D As (E As Integer, F As Integer)))).get_Item((Integer, Integer)) As (A As Integer, B As (C As Integer, D As (E As Integer, F As Integer)))"
  IL_002e:  ldfld      "System.ValueTuple(Of Integer, (C As Integer, D As (E As Integer, F As Integer))).Item2 As (C As Integer, D As (E As Integer, F As Integer))"
  IL_0033:  ldfld      "System.ValueTuple(Of Integer, (E As Integer, F As Integer)).Item2 As (E As Integer, F As Integer)"
  IL_0038:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_003d:  call       "Sub System.Console.Write(Integer)"
  IL_0042:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub TupleLambdaCapture01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        System.Console.Write(Test(42))
    End Sub

    Public Shared Function Test(Of T)(a As T) As T
        Dim x = (f1:=a, f2:=a)
        Dim f As System.Func(Of T) = Function() x.f2
        return f()
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C._Closure$__2-0(Of $CLS0)._Lambda$__0()", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C._Closure$__2-0(Of $CLS0).$VB$Local_x As (f1 As $CLS0, f2 As $CLS0)"
  IL_0006:  ldfld      "System.ValueTuple(Of $CLS0, $CLS0).Item2 As $CLS0"
  IL_000b:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub TupleLambdaCapture02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        System.Console.Write(Test(42))
    End Sub
    Shared Function Test(Of T)(a As T) As String
        Dim x = (f1:=a, f2:=a)
        Dim f As System.Func(Of String) = Function() x.ToString()
        Return f()
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(42, 42)]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C._Closure$__2-0(Of $CLS0)._Lambda$__0()", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C._Closure$__2-0(Of $CLS0).$VB$Local_x As (f1 As $CLS0, f2 As $CLS0)"
  IL_0006:  constrained. "System.ValueTuple(Of $CLS0, $CLS0)"
  IL_000c:  callvirt   "Function Object.ToString() As String"
  IL_0011:  ret
}
]]>)

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/13298")>
        Public Sub TupleLambdaCapture03()

            ' This test crashes in TypeSubstitution
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        System.Console.Write(Test(42))
    End Sub
    Shared Function Test(Of T)(a As T) As T
        Dim x = (f1:=a, f2:=b)
        Dim f As System.Func(Of T) = Function() x.Test(a)
        Return f()
    End Function
End Class
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub
        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function
        Public Function Test(Of U)(val As U) As U
            Return val
        End Function
    End Structure
End Namespace

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[

]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/pull/13209")>
        Public Sub TupleLambdaCapture04()

            ' this test crashes in TypeSubstitution
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        System.Console.Write(Test(42))
    End Sub
    Shared Function Test(Of T)(a As T) As T
        Dim x = (f1:=1, f2:=2)
        Dim f As System.Func(Of T) = Function() x.Test(a)
        Return f()
    End Function
End Class
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub
        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function
        Public Function Test(Of U)(val As U) As U
            Return val
        End Function
    End Structure
End Namespace

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[

]]>)

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/pull/13209")>
        Public Sub TupleLambdaCapture05()

            ' this test crashes in TypeSubstitution
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        System.Console.Write(Test(42))
    End Sub
    Shared Function Test(Of T)(a As T) As T
        Dim x = (f1:=a, f2:=a)
        Dim f As System.Func(Of T) = Function() x.P1
        Return f()
    End Function
End Class
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub
        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function
        Public ReadOnly Property P1 As T1
            Get
                Return Item1
            End Get
        End Property
    End Structure
End Namespace

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("Module1.Main", <![CDATA[

]]>)

        End Sub

        <Fact>
        Public Sub TupleAsyncCapture01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub Main()
        Console.Write(Test(42).Result)
    End Sub
    Shared Async Function Test(Of T)(a As T) As Task(Of T)
        Dim x = (f1:=a, f2:=a)
        Await Task.Yield()
        Return x.f1
    End Function
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46}, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C.VB$StateMachine_2_Test(Of SM$T).MoveNext()", <![CDATA[
{
  // Code size      204 (0xcc)
  .maxstack  3
  .locals init (SM$T V_0,
                Integer V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$VB$Local_a As SM$T"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$VB$Local_a As SM$T"
    IL_0017:  newobj     "Sub System.ValueTuple(Of SM$T, SM$T)..ctor(SM$T, SM$T)"
    IL_001c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$VB$ResumableLocal_x$0 As (f1 As SM$T, f2 As SM$T)"
    IL_0021:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.1
    IL_003c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
    IL_0041:  ldarg.0
    IL_0042:  ldloc.2
    IL_0043:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0048:  ldarg.0
    IL_0049:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of SM$T)"
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldarg.0
    IL_0051:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of SM$T).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.VB$StateMachine_2_Test(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef C.VB$StateMachine_2_Test(Of SM$T))"
    IL_0056:  leave.s    IL_00cb
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.m1
    IL_005a:  dup
    IL_005b:  stloc.1
    IL_005c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
    IL_0061:  ldarg.0
    IL_0062:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  stloc.2
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_006e:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_007b:  ldloca.s   V_2
    IL_007d:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0083:  ldarg.0
    IL_0084:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$VB$ResumableLocal_x$0 As (f1 As SM$T, f2 As SM$T)"
    IL_0089:  ldfld      "System.ValueTuple(Of SM$T, SM$T).Item1 As SM$T"
    IL_008e:  stloc.0
    IL_008f:  leave.s    IL_00b5
  }
  catch System.Exception
  {
    IL_0091:  dup
    IL_0092:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0097:  stloc.s    V_4
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -2
    IL_009c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of SM$T)"
    IL_00a7:  ldloc.s    V_4
    IL_00a9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of SM$T).SetException(System.Exception)"
    IL_00ae:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00b3:  leave.s    IL_00cb
  }
  IL_00b5:  ldarg.0
  IL_00b6:  ldc.i4.s   -2
  IL_00b8:  dup
  IL_00b9:  stloc.1
  IL_00ba:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
  IL_00bf:  ldarg.0
  IL_00c0:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of SM$T)"
  IL_00c5:  ldloc.0
  IL_00c6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of SM$T).SetResult(SM$T)"
  IL_00cb:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub TupleAsyncCapture02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub Main()
        Console.Write(Test(42).Result)
    End Sub
    Shared Async Function Test(Of T)(a As T) As Task(Of String)
        Dim x = (f1:=a, f2:=a)
        Await Task.Yield()
        Return x.ToString()
    End Function
End Class
    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46}, expectedOutput:=<![CDATA[(42, 42)]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C.VB$StateMachine_2_Test(Of SM$T).MoveNext()", <![CDATA[
{
  // Code size      210 (0xd2)
  .maxstack  3
  .locals init (String V_0,
                Integer V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$VB$Local_a As SM$T"
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$VB$Local_a As SM$T"
    IL_0017:  newobj     "Sub System.ValueTuple(Of SM$T, SM$T)..ctor(SM$T, SM$T)"
    IL_001c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$VB$ResumableLocal_x$0 As (f1 As SM$T, f2 As SM$T)"
    IL_0021:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.1
    IL_003c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
    IL_0041:  ldarg.0
    IL_0042:  ldloc.2
    IL_0043:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0048:  ldarg.0
    IL_0049:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldarg.0
    IL_0051:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.VB$StateMachine_2_Test(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef C.VB$StateMachine_2_Test(Of SM$T))"
    IL_0056:  leave.s    IL_00d1
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.m1
    IL_005a:  dup
    IL_005b:  stloc.1
    IL_005c:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
    IL_0061:  ldarg.0
    IL_0062:  ldfld      "C.VB$StateMachine_2_Test(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  stloc.2
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_006e:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_007b:  ldloca.s   V_2
    IL_007d:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0083:  ldarg.0
    IL_0084:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$VB$ResumableLocal_x$0 As (f1 As SM$T, f2 As SM$T)"
    IL_0089:  constrained. "System.ValueTuple(Of SM$T, SM$T)"
    IL_008f:  callvirt   "Function Object.ToString() As String"
    IL_0094:  stloc.0
    IL_0095:  leave.s    IL_00bb
  }
  catch System.Exception
  {
    IL_0097:  dup
    IL_0098:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_009d:  stloc.s    V_4
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
    IL_00ad:  ldloc.s    V_4
    IL_00af:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).SetException(System.Exception)"
    IL_00b4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00b9:  leave.s    IL_00d1
  }
  IL_00bb:  ldarg.0
  IL_00bc:  ldc.i4.s   -2
  IL_00be:  dup
  IL_00bf:  stloc.1
  IL_00c0:  stfld      "C.VB$StateMachine_2_Test(Of SM$T).$State As Integer"
  IL_00c5:  ldarg.0
  IL_00c6:  ldflda     "C.VB$StateMachine_2_Test(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
  IL_00cb:  ldloc.0
  IL_00cc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).SetResult(String)"
  IL_00d1:  ret
}
]]>)

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/pull/13209")>
        Public Sub TupleAsyncCapture03()

            ' this test crashes in TypeSubstitution
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub Main()
        Console.Write(Test(42).Result)
    End Sub
    Shared Async Function Test(Of T)(a As T) As Task(Of String)
        Dim x = (f1:=a, f2:=a)
        Await Task.Yield()
        Return x.Test(a)
    End Function
End Class

Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub
        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function
        Public Function Test(Of U)(val As U) As U
            Return val
        End Function
    End Structure
End Namespace
    </file>
</compilation>, additionalRefs:={MscorlibRef_v46}, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C.VB$StateMachine_2_Test(Of SM$T).MoveNext()", <![CDATA[
]]>)

        End Sub

        <Fact>
        Public Sub LongTupleWithSubstitution()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub Main()
        Console.Write(Test(42).Result)
    End Sub
    Shared Async Function Test(Of T)(a As T) As Task(Of T)
        Dim x = (f1:=1, f2:=2, f3:=3, f4:=4, f5:=5, f6:=6, f7:=7, f8:=a)
        Await Task.Yield()
        Return x.f8
    End Function
End Class
    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46}, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleUsageWithoutTupleLibrary()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (Integer, String) = (1, "hello")
    End Sub
End Module

]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim x As (Integer, String) = (1, "hello")
                 ~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim x As (Integer, String) = (1, "hello")
                 ~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        Dim x As (Integer, String) = (1, "hello")
                                     ~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleUsageWithMissingTupleMembers()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (Integer, String) = (1, 2)
    End Sub
End Module

Namespace System
    Public Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace
]]></file>
</compilation>)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC35000: Requested operation is not available because the runtime library function 'ValueTuple..ctor' is not defined.
        Dim x As (Integer, String) = (1, 2)
                                     ~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleWithDuplicateNames()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (a As Integer, a As String) = (b:=1, b:="hello", b:=2)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37262: Tuple element names must be unique.
        Dim x As (a As Integer, a As String) = (b:=1, b:="hello", b:=2)
                                ~
BC37262: Tuple element names must be unique.
        Dim x As (a As Integer, a As String) = (b:=1, b:="hello", b:=2)
                                                      ~
BC37262: Tuple element names must be unique.
        Dim x As (a As Integer, a As String) = (b:=1, b:="hello", b:=2)
                                                                  ~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleWithDuplicateReservedNames()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (Item1 As Integer, Item1 As String) = (Item1:=1, Item1:="hello")
        Dim y As (Item2 As Integer, Item2 As String) = (Item2:=1, Item2:="hello")
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37261: Tuple element name 'Item1' is only allowed at position 1.
        Dim x As (Item1 As Integer, Item1 As String) = (Item1:=1, Item1:="hello")
                                    ~~~~~
BC37261: Tuple element name 'Item1' is only allowed at position 1.
        Dim x As (Item1 As Integer, Item1 As String) = (Item1:=1, Item1:="hello")
                                                                  ~~~~~
BC37261: Tuple element name 'Item2' is only allowed at position 2.
        Dim y As (Item2 As Integer, Item2 As String) = (Item2:=1, Item2:="hello")
                  ~~~~~
BC37261: Tuple element name 'Item2' is only allowed at position 2.
        Dim y As (Item2 As Integer, Item2 As String) = (Item2:=1, Item2:="hello")
                                                        ~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleWithNonReservedNames()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (Item1 As Integer, Item01 As Integer, Item10 As Integer) = (Item01:=1, Item1:=2, Item10:=3)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37261: Tuple element name 'Item10' is only allowed at position 10.
        Dim x As (Item1 As Integer, Item01 As Integer, Item10 As Integer) = (Item01:=1, Item1:=2, Item10:=3)
                                                       ~~~~~~
BC37261: Tuple element name 'Item1' is only allowed at position 1.
        Dim x As (Item1 As Integer, Item01 As Integer, Item10 As Integer) = (Item01:=1, Item1:=2, Item10:=3)
                                                                                        ~~~~~
BC37261: Tuple element name 'Item10' is only allowed at position 10.
        Dim x As (Item1 As Integer, Item01 As Integer, Item10 As Integer) = (Item01:=1, Item1:=2, Item10:=3)
                                                                                                  ~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub DefaultValueForTuple()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As (a As Integer, b As String) = (1, "hello")
        x = Nothing
        System.Console.WriteLine(x.a)
        System.Console.WriteLine(If(x.b, "null"))
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[0
null]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleWithDuplicateMemberNames()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (a As Integer, a As String) = (b:=1, c:="hello", b:=2)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37262: Tuple element names must be unique.
        Dim x As (a As Integer, a As String) = (b:=1, c:="hello", b:=2)
                                ~
BC37262: Tuple element names must be unique.
        Dim x As (a As Integer, a As String) = (b:=1, c:="hello", b:=2)
                                                                  ~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleWithReservedMemberNames()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x As (Item1 As Integer, Item3 As String, Item2 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Rest As Integer) =
            (Item2:="bad", Item4:="bad", Item3:=3, Item4:=4, Item5:=5, Item6:=6, Item7:=7, Rest:="bad")
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37261: Tuple element name 'Item3' is only allowed at position 3.
        Dim x As (Item1 As Integer, Item3 As String, Item2 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Rest As Integer) =
                                    ~~~~~
BC37261: Tuple element name 'Item2' is only allowed at position 2.
        Dim x As (Item1 As Integer, Item3 As String, Item2 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Rest As Integer) =
                                                     ~~~~~
BC37260: Tuple element name 'Rest' is disallowed at any position.
        Dim x As (Item1 As Integer, Item3 As String, Item2 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Rest As Integer) =
                                                                                                                                               ~~~~
BC37261: Tuple element name 'Item2' is only allowed at position 2.
            (Item2:="bad", Item4:="bad", Item3:=3, Item4:=4, Item5:=5, Item6:=6, Item7:=7, Rest:="bad")
             ~~~~~
BC37261: Tuple element name 'Item4' is only allowed at position 4.
            (Item2:="bad", Item4:="bad", Item3:=3, Item4:=4, Item5:=5, Item6:=6, Item7:=7, Rest:="bad")
                           ~~~~~
BC37260: Tuple element name 'Rest' is disallowed at any position.
            (Item2:="bad", Item4:="bad", Item3:=3, Item4:=4, Item5:=5, Item6:=6, Item7:=7, Rest:="bad")
                                                                                           ~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleWithExistingUnderlyingMemberNames()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System

Module C
    Sub Main()
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37260: Tuple element name 'CompareTo' is disallowed at any position.
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
                 ~~~~~~~~~
BC37260: Tuple element name 'Deconstruct' is disallowed at any position.
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
                                          ~~~~~~~~~~~
BC37260: Tuple element name 'Equals' is disallowed at any position.
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
                                                          ~~~~~~
BC37260: Tuple element name 'GetHashCode' is disallowed at any position.
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
                                                                     ~~~~~~~~~~~
BC37260: Tuple element name 'Rest' is disallowed at any position.
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
                                                                                     ~~~~
BC37260: Tuple element name 'ToString' is disallowed at any position.
        Dim x = (CompareTo:=2, Create:=3, Deconstruct:=4, Equals:=5, GetHashCode:=6, Rest:=8, ToString:=10)
                                                                                              ~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub LongTupleDeclaration()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, String, Integer, Integer, Integer, Integer) =
            (1, 2, 3, 4, 5, 6, 7, "Alice", 2, 3, 4, 5)
        System.Console.Write($"{x.Item1} {x.Item2} {x.Item3} {x.Item4} {x.Item5} {x.Item6} {x.Item7} {x.Item8} {x.Item9} {x.Item10} {x.Item11} {x.Item12}")
    End Sub
End Class
    </file>
</compilation>,
                additionalRefs:=s_valueTupleRefs,
                expectedOutput:=<![CDATA[1 2 3 4 5 6 7 Alice 2 3 4 5]]>,
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().Single().Names(0)

                        Assert.Equal("x As (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, " _
                            + "System.String, System.Int32, System.Int32, System.Int32, System.Int32)",
                            model.GetDeclaredSymbol(x).ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub LongTupleDeclarationWithNames()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As (a As Integer, b As Integer, c As Integer, d As Integer, e As Integer, f As Integer, g As Integer, _
            h As String, i As Integer, j As Integer, k As Integer, l As Integer) =
            (1, 2, 3, 4, 5, 6, 7, "Alice", 2, 3, 4, 5)
        System.Console.Write($"{x.a} {x.b} {x.c} {x.d} {x.e} {x.f} {x.g} {x.h} {x.i} {x.j} {x.k} {x.l}")
    End Sub
End Class
    </file>
</compilation>,
                additionalRefs:=s_valueTupleRefs,
                expectedOutput:=<![CDATA[1 2 3 4 5 6 7 Alice 2 3 4 5]]>,
                sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                           Dim compilation = m.DeclaringCompilation
                                           Dim tree = compilation.SyntaxTrees.First()
                                           Dim model = compilation.GetSemanticModel(tree)
                                           Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                                           Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().Single().Names(0)

                                           Assert.Equal("x As (a As System.Int32, b As System.Int32, c As System.Int32, d As System.Int32, " _
                                                        + "e As System.Int32, f As System.Int32, g As System.Int32, h As System.String, " _
                                                        + "i As System.Int32, j As System.Int32, k As System.Int32, l As System.Int32)",
                                               model.GetDeclaredSymbol(x).ToTestDisplayString())
                                       End Sub)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub HugeTupleCreationParses()

            Dim b = New StringBuilder()
            b.Append("(")
            For i As Integer = 0 To 3000
                b.Append("1, ")
            Next
            b.Append("1)")

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x = <%= b.ToString() %>
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertNoDiagnostics()

        End Sub

        <Fact>
        Public Sub HugeTupleDeclarationParses()

            Dim b = New StringBuilder()
            b.Append("(")
            For i As Integer = 0 To 3000
                b.Append("Integer, ")
            Next
            b.Append("Integer)")

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As <%= b.ToString() %>;
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)

        End Sub

        <Fact>
        <WorkItem(13302, "https://github.com/dotnet/roslyn/issues/13302")>
        Public Sub GenericTupleWithoutTupleLibrary_01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Class C
    Shared Sub Main()
        Dim x = M(Of Integer, Boolean)()
        System.Console.Write($"{x.first} {x.second}")
    End Sub
    Public Shared Function M(Of T1, T2)() As (first As T1, second As T2)
        return (Nothing, Nothing)
    End Function
End Class
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
    Public Shared Function M(Of T1, T2)() As (first As T1, second As T2)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
    Public Shared Function M(Of T1, T2)() As (first As T1, second As T2)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function M(Of T1, T2)() As (first As T1, second As T2)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        return (Nothing, Nothing)
               ~~~~~~~~~~~~~~~~~~
</errors>)

            Dim mTuple = DirectCast(comp.GetMember(Of MethodSymbol)("C.M").ReturnType, NamedTypeSymbol)

            Assert.True(mTuple.IsTupleType)
            Assert.Equal(TypeKind.Error, mTuple.TupleUnderlyingType.TypeKind)
            Assert.Equal(SymbolKind.ErrorType, mTuple.TupleUnderlyingType.Kind)
            Assert.IsAssignableFrom(Of ErrorTypeSymbol)(mTuple.TupleUnderlyingType)
            Assert.Equal(TypeKind.Struct, mTuple.TypeKind)
            'AssertTupleTypeEquality(mTuple)
            Assert.False(mTuple.IsImplicitlyDeclared)
            'Assert.Equal("Predefined type 'System.ValueTuple`2' is not defined or imported", mTuple.GetUseSiteDiagnostic().GetMessage(CultureInfo.InvariantCulture))
            Assert.Null(mTuple.BaseType)
            Assert.False(DirectCast(mTuple, TupleTypeSymbol).UnderlyingDefinitionToMemberMap.Any())

            Dim mFirst = DirectCast(mTuple.GetMembers("first").Single(), FieldSymbol)

            Assert.IsType(Of TupleErrorFieldSymbol)(mFirst)

            Assert.True(mFirst.IsTupleField)
            Assert.Equal("first", mFirst.Name)
            Assert.Same(mFirst, mFirst.OriginalDefinition)
            Assert.True(mFirst.Equals(mFirst))
            Assert.Null(mFirst.TupleUnderlyingField)
            Assert.Null(mFirst.AssociatedSymbol)
            Assert.Same(mTuple, mFirst.ContainingSymbol)
            Assert.True(mFirst.CustomModifiers.IsEmpty)
            Assert.True(mFirst.GetAttributes().IsEmpty)
            'Assert.Null(mFirst.GetUseSiteDiagnostic())
            Assert.False(mFirst.Locations.IsEmpty)
            Assert.Equal("first", mFirst.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.False(mFirst.IsImplicitlyDeclared)
            Assert.Null(mFirst.TypeLayoutOffset)

            Dim mItem1 = DirectCast(mTuple.GetMembers("Item1").Single(), FieldSymbol)

            Assert.IsType(Of TupleErrorFieldSymbol)(mItem1)

            Assert.True(mItem1.IsTupleField)
            Assert.Equal("Item1", mItem1.Name)
            Assert.Same(mItem1, mItem1.OriginalDefinition)
            Assert.True(mItem1.Equals(mItem1))
            Assert.Null(mItem1.TupleUnderlyingField)
            Assert.Null(mItem1.AssociatedSymbol)
            Assert.Same(mTuple, mItem1.ContainingSymbol)
            Assert.True(mItem1.CustomModifiers.IsEmpty)
            Assert.True(mItem1.GetAttributes().IsEmpty)
            'Assert.Null(mItem1.GetUseSiteDiagnostic())
            Assert.True(mItem1.Locations.IsEmpty)
            Assert.True(mItem1.IsImplicitlyDeclared)
            Assert.Null(mItem1.TypeLayoutOffset)

        End Sub

        <Fact>
        <WorkItem(13300, "https://github.com/dotnet/roslyn/issues/13300")>
        Public Sub GenericTupleWithoutTupleLibrary_02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Class C
    Function M(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)() As (T1, T2, T3, T4, T5, T6, T7, T8, T9)
        Throw New System.NotSupportedException()
    End Function
End Class
Namespace System
    Public Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
    Function M(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)() As (T1, T2, T3, T4, T5, T6, T7, T8, T9)
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,,,,,,)' is not defined or imported.
    Function M(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)() As (T1, T2, T3, T4, T5, T6, T7, T8, T9)
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub GenericTuple()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x = M(Of Integer, Boolean)()
        System.Console.Write($"{x.first} {x.second}")
    End Sub
    Shared Function M(Of T1, T2)() As (first As T1, second As T2)
        Return (Nothing, Nothing)
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[0 False]]>)

        End Sub

        <Fact>
        Public Sub LongTupleCreation()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x = (1, 2, 3, 4, 5, 6, 7, "Alice", 2, 3, 4, 5, 6, 7, "Bob", 2, 3)
        System.Console.Write($"{x.Item1} {x.Item2} {x.Item3} {x.Item4} {x.Item5} {x.Item6} {x.Item7} {x.Item8} " _
            + $"{x.Item9} {x.Item10} {x.Item11} {x.Item12} {x.Item13} {x.Item14} {x.Item15} {x.Item16} {x.Item17}")
    End Sub
End Class
    </file>
</compilation>,
                additionalRefs:=s_valueTupleRefs,
                expectedOutput:=<![CDATA[1 2 3 4 5 6 7 Alice 2 3 4 5 6 7 Bob 2 3]]>,
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim x = nodes.OfType(Of TupleExpressionSyntax)().Single()

                        Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, " _
                            + "System.String, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, " _
                            + "System.String, System.Int32, System.Int32)",
                            model.GetTypeInfo(x).Type.ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleInLambda()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim f As System.Action(Of (Integer, String)) = Sub(x As (Integer, String)) System.Console.Write($"{x.Item1} {x.Item2}")
        f((42, "Alice"))
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleWithNamesInLambda()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim f As System.Action(Of (Integer, String)) = Sub(x As (a As Integer, b As String)) System.Console.Write($"{x.Item1} {x.Item2}")
        f((c:=42, d:="Alice"))
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

        End Sub

        <Fact>
        Public Sub TupleInProperty()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Property P As (a As Integer, b As String)

    Shared Sub Main()
        P = (42, "Alice")
        System.Console.Write($"{P.a} {P.b}")
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub ExtensionMethodOnTuple()

            Dim comp = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module M
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Sub Extension(x As (a As Integer, b As String))
        System.Console.Write($"{x.a} {x.b}")
    End Sub
End Module
Class C
    Shared Sub Main()
        Call (42, "Alice").Extension()
    End Sub
End Class
    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:="42 Alice")

        End Sub

        <Fact>
        Public Sub TupleInOptionalParam()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Sub M(x As Integer, Optional y As (a As Integer, b As String) = (42, "Alice"))
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30059: Constant expression is required.
    Sub M(x As Integer, Optional y As (a As Integer, b As String) = (42, "Alice"))
                                                                    ~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub TupleDefaultInOptionalParam()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        M()
    End Sub
    Shared Sub M(Optional x As (a As Integer, b As String) = Nothing)
        System.Console.Write($"{x.a} {x.b}")
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[0 ]]>)

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleAsNamedParam()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        M(y:=(42, "Alice"), x:=1)
    End Sub
    Shared Sub M(x As Integer, y As (a As Integer, b As String))
        System.Console.Write($"{y.a} {y.Item2}")
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub LongTupleCreationWithNames()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x =
            (a:=1, b:=2, c:=3, d:=4, e:=5, f:=6, g:=7, h:="Alice", i:=2, j:=3, k:=4, l:=5, m:=6, n:=7, o:="Bob", p:=2, q:=3)
        System.Console.Write($"{x.a} {x.b} {x.c} {x.d} {x.e} {x.f} {x.g} {x.h} {x.i} {x.j} {x.k} {x.l} {x.m} {x.n} {x.o} {x.p} {x.q}")
    End Sub
End Class
    </file>
</compilation>,
                additionalRefs:=s_valueTupleRefs,
                expectedOutput:=<![CDATA[1 2 3 4 5 6 7 Alice 2 3 4 5 6 7 Bob 2 3]]>,
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim x = nodes.OfType(Of TupleExpressionSyntax)().Single()

                        Assert.Equal("(a As System.Int32, b As System.Int32, c As System.Int32, d As System.Int32, e As System.Int32, f As System.Int32, g As System.Int32, " _
                            + "h As System.String, i As System.Int32, j As System.Int32, k As System.Int32, l As System.Int32, m As System.Int32, n As System.Int32, " _
                            + "o As System.String, p As System.Int32, q As System.Int32)",
                            model.GetTypeInfo(x).Type.ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub LongTupleWithArgumentEvaluation()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x = (a:=PrintAndReturn(1), b:=2, c:=3, d:=PrintAndReturn(4), e:=5, f:=6, g:=PrintAndReturn(7), h:=PrintAndReturn("Alice"), i:=2, j:=3, k:=4, l:=5, m:=6, n:=PrintAndReturn(7), o:=PrintAndReturn("Bob"), p:=2, q:=PrintAndReturn(3))
    End Sub
    Shared Function PrintAndReturn(Of T)(i As T)
        System.Console.Write($"{i} ")
        Return i
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 4 7 Alice 7 Bob 3]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DuplicateTupleMethodsNotAllowed()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Imports System
Class C
    Function M(a As (String, String)) As (Integer, Integer)
        Return new System.ValueTuple(Of Integer, Integer)(a.Item1.Length, a.Item2.Length)
    End Function
    Function M(a As System.ValueTuple(Of String, String)) As System.ValueTuple(Of Integer, Integer)
        Return (a.Item1.Length, a.Item2.Length)
    End Function
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30269: 'Public Function M(a As (String, String)) As (Integer, Integer)' has multiple definitions with identical signatures.
    Function M(a As (String, String)) As (Integer, Integer)
             ~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleArrays()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface I
    Function M(a As (Integer, Integer)()) As System.ValueTuple(Of Integer, Integer)()
End Interface

Class C
    Implements I

    Shared Sub Main()
        Dim i As I = new C()
        Dim r = i.M(new System.ValueTuple(Of Integer, Integer)() { new System.ValueTuple(Of Integer, Integer)(1, 2) })
        System.Console.Write($"{r(0).Item1} {r(0).Item2}")
    End Sub

    Public Function M(a As (Integer, Integer)()) As System.ValueTuple(Of Integer, Integer)() Implements I.M
        Return New System.ValueTuple(Of Integer, Integer)() { (a(0).Item1, a(0).Item2) }
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 2]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleRef()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim r = (1, 2)
        M(r)
        System.Console.Write($"{r.Item1} {r.Item2}")
    End Sub
    Shared Sub M(ByRef a As (Integer, Integer))
        System.Console.WriteLine($"{a.Item1} {a.Item2}")
        a = (3, 4)
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 2
3 4]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleOut()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim r As (Integer, Integer)
        M(r)
        System.Console.Write($"{r.Item1} {r.Item2}")
    End Sub
    Shared Sub M(ByRef a As (Integer, Integer))
        System.Console.WriteLine($"{a.Item1} {a.Item2}")
        a = (1, 2)
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[0 0
1 2]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleTypeArgs()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim a = (1, "Alice")
        Dim r = M(Of Integer, String)(a)
        System.Console.Write($"{r.Item1} {r.Item2}")
    End Sub
    Shared Function M(Of T1, T2)(a As (T1, T2)) As (T1, T2)
        Return a
    End Function
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 Alice]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub NullableTuple()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        M((1, "Alice"))
    End Sub
    Shared Sub M(a As (Integer, String)?)
        System.Console.Write($"{a.HasValue} {a.Value.Item1} {a.Value.Item2}")
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[True 1 Alice]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleUnsupportedInUsingStatment()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Imports VT2 = (Integer, Integer)
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC30203: Identifier expected.
Imports VT2 = (Integer, Integer)
              ~
BC40056: Namespace or type specified in the Imports '' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports VT2 = (Integer, Integer)
              ~~~~~~~~~~~~~~~~~~
BC32093: 'Of' required when specifying type arguments for a generic type or method.
Imports VT2 = (Integer, Integer)
               ~
</errors>)

        End Sub

        <Fact>
        Public Sub MissingTypeInAlias()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Imports System
Imports VT2 = System.ValueTuple(Of Integer, Integer) ' ValueTuple is referenced but does not exist
Namespace System
    Public Class Bogus
    End Class
End Namespace
Namespace TuplesCrash2
    Class C
        Shared Sub Main()

        End Sub
    End Class
End Namespace
]]></file>
</compilation>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = model.LookupStaticMembers(234)

            For i As Integer = 0 To tree.GetText().Length
                model.LookupStaticMembers(i)
            Next
            ' Didn't crash

        End Sub

        <Fact>
        Public Sub MultipleDefinitionsOfValueTuple()

            Dim source1 =
<compilation name="comp1">
    <file name="a.vb">
Public Module M1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Sub Extension(x As Integer, y As (Integer, Integer))
        System.Console.Write("M1.Extension")
    End Sub
End Module
<%= s_trivial2uple %></file>
</compilation>

            Dim source2 =
<compilation name="comp2">
    <file name="a.vb">
Public Module M2
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Sub Extension(x As Integer, y As (Integer, Integer))
        System.Console.Write("M2.Extension")
    End Sub
End Module
<%= s_trivial2uple %></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlibAndVBRuntime(source1, additionalRefs:={MscorlibRef_v46})
            comp1.AssertNoDiagnostics()
            Dim comp2 = CreateCompilationWithMscorlibAndVBRuntime(source2, additionalRefs:={MscorlibRef_v46})
            comp2.AssertNoDiagnostics()

            Dim source =
<compilation name="comp">
    <file name="a.vb">
Imports System
Imports M1
Imports M2
Class C
    Public Shared Sub Main()
        Dim x As Integer = 0
        x.Extension((1, 1))
    End Sub
End Class
</file>
</compilation>

            Dim comp3 = CreateCompilationWithMscorlibAndVBRuntime(source, additionalRefs:={comp1.ToMetadataReference(), comp2.ToMetadataReference()})
            comp3.AssertTheseDiagnostics(
<errors>
BC30521: Overload resolution failed because no accessible 'Extension' is most specific for these arguments:
    Extension method 'Public Sub Extension(y As (Integer, Integer))' defined in 'M1': Not most specific.
    Extension method 'Public Sub Extension(y As (Integer, Integer))' defined in 'M2': Not most specific.
        x.Extension((1, 1))
          ~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
        x.Extension((1, 1))
                    ~~~~~~
</errors>)

            Dim comp4 = CreateCompilationWithMscorlibAndVBRuntime(source,
                            additionalRefs:={comp1.ToMetadataReference()},
                            options:=TestOptions.DebugExe)

            comp4.AssertTheseDiagnostics(
<errors>
BC40056: Namespace or type specified in the Imports 'M2' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports M2
        ~~
</errors>)
            CompileAndVerify(comp4, expectedOutput:=<![CDATA[M1.Extension]]>)

        End Sub

        <Fact>
        Public Sub Tuple2To8Members()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console
Class C
    Shared Sub Main()
        Write((1, 2).Item1)
        Write((1, 2).Item2)
        WriteLine()
        Write((1, 2, 3).Item1)
        Write((1, 2, 3).Item2)
        Write((1, 2, 3).Item3)
        WriteLine()
        Write((1, 2, 3, 4).Item1)
        Write((1, 2, 3, 4).Item2)
        Write((1, 2, 3, 4).Item3)
        Write((1, 2, 3, 4).Item4)
        WriteLine()
        Write((1, 2, 3, 4, 5).Item1)
        Write((1, 2, 3, 4, 5).Item2)
        Write((1, 2, 3, 4, 5).Item3)
        Write((1, 2, 3, 4, 5).Item4)
        Write((1, 2, 3, 4, 5).Item5)
        WriteLine()
        Write((1, 2, 3, 4, 5, 6).Item1)
        Write((1, 2, 3, 4, 5, 6).Item2)
        Write((1, 2, 3, 4, 5, 6).Item3)
        Write((1, 2, 3, 4, 5, 6).Item4)
        Write((1, 2, 3, 4, 5, 6).Item5)
        Write((1, 2, 3, 4, 5, 6).Item6)
        WriteLine()
        Write((1, 2, 3, 4, 5, 6, 7).Item1)
        Write((1, 2, 3, 4, 5, 6, 7).Item2)
        Write((1, 2, 3, 4, 5, 6, 7).Item3)
        Write((1, 2, 3, 4, 5, 6, 7).Item4)
        Write((1, 2, 3, 4, 5, 6, 7).Item5)
        Write((1, 2, 3, 4, 5, 6, 7).Item6)
        Write((1, 2, 3, 4, 5, 6, 7).Item7)
        WriteLine()
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item1)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item2)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item3)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item4)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item5)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item6)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item7)
        Write((1, 2, 3, 4, 5, 6, 7, 8).Item8)
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[12
123
1234
12345
123456
1234567
12345678]]>)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_BadArguments()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)

            Assert.Throws(Of ArgumentNullException)(Sub() comp.CreateTupleTypeSymbol(underlyingType:=Nothing, elementNames:=Nothing))
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType)
            Try
                comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Item1"))
                Assert.True(False)
            Catch ex As ArgumentException
                Assert.Contains(CodeAnalysisResources.TupleElementNameCountMismatch, ex.Message)
            End Try

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithValueTuple()

            Dim tupleComp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create(Of String)(Nothing, Nothing))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_NoNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, Nothing)

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.ErrorType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Alice", "Bob"))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.ErrorType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(Alice As System.Int32, Bob As System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.Equal(New String() {"Alice", "Bob"}, tupleWithoutNames.TupleElementNames)
            Assert.Equal(New String() {"System.Int32", "System.String"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithSomeNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim vt3 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T3).Construct(intType, stringType, intType)

            Dim tupleWithSomeNames = comp.CreateTupleTypeSymbol(vt3, ImmutableArray.Create(Nothing, "Item2", "Charlie"))

            Assert.True(tupleWithSomeNames.IsTupleType)
            Assert.Equal(SymbolKind.ErrorType, tupleWithSomeNames.TupleUnderlyingType.Kind)
            Assert.Equal("(System.Int32, Item2 As System.String, Charlie As System.Int32)", tupleWithSomeNames.ToTestDisplayString())
            Assert.Equal(New String() {Nothing, "Item2", "Charlie"}, tupleWithSomeNames.TupleElementNames)
            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32"},
                         tupleWithSomeNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

            Assert.Equal(SymbolKind.NamedType, tupleWithSomeNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithBadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Item2", "Item1"))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal("(Item2 As System.Int32, Item1 As System.Int32)", tupleWithoutNames.ToTestDisplayString())
            Assert.Equal(New String() {"Item2", "Item1"}, tupleWithoutNames.TupleElementNames)
            Assert.Equal(New String() {"System.Int32", "System.Int32"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_Tuple8NoNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim vt8 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).
                Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                       comp.GetWellKnownType(WellKnownType.System_ValueTuple_T1).Construct(stringType))

            Dim tuple8WithoutNames = comp.CreateTupleTypeSymbol(vt8, Nothing)

            Assert.True(tuple8WithoutNames.IsTupleType)
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String)",
                         tuple8WithoutNames.ToTestDisplayString())

            Assert.True(tuple8WithoutNames.TupleElementNames.IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         tuple8WithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_Tuple8WithNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim vt8 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).
                Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                       comp.GetWellKnownType(WellKnownType.System_ValueTuple_T1).Construct(stringType))

            Dim tuple8WithNames = comp.CreateTupleTypeSymbol(vt8, ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"))

            Assert.True(tuple8WithNames.IsTupleType)
            Assert.Equal("(Alice1 As System.Int32, Alice2 As System.String, Alice3 As System.Int32, Alice4 As System.String, Alice5 As System.Int32, Alice6 As System.String, Alice7 As System.Int32, Alice8 As System.String)",
                         tuple8WithNames.ToTestDisplayString())

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"}, tuple8WithNames.TupleElementNames)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         tuple8WithNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_Tuple9NoNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim vt9 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).
                Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                       comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, intType))

            Dim tuple9WithoutNames = comp.CreateTupleTypeSymbol(vt9, Nothing)

            Assert.True(tuple9WithoutNames.IsTupleType)
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32)",
                         tuple9WithoutNames.ToTestDisplayString())

            Assert.True(tuple9WithoutNames.TupleElementNames.IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         tuple9WithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_Tuple9WithNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim vt9 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).
                Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                       comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, intType))

            Dim tuple9WithNames = comp.CreateTupleTypeSymbol(vt9, ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"))

            Assert.True(tuple9WithNames.IsTupleType)
            Assert.Equal("(Alice1 As System.Int32, Alice2 As System.String, Alice3 As System.Int32, Alice4 As System.String, Alice5 As System.Int32, Alice6 As System.String, Alice7 As System.Int32, Alice8 As System.String, Alice9 As System.Int32)",
                         tuple9WithNames.ToTestDisplayString())

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"}, tuple9WithNames.TupleElementNames)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         tuple9WithNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_Tuple9WithDefaultNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim vt9 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).
                Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                       comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, intType))

            Dim tuple9WithNames = comp.CreateTupleTypeSymbol(vt9, ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9"))

            Assert.True(tuple9WithNames.IsTupleType)
            Assert.Equal("(Item1 As System.Int32, Item2 As System.String, Item3 As System.Int32, Item4 As System.String, Item5 As System.Int32, Item6 As System.String, Item7 As System.Int32, Item8 As System.String, Item9 As System.Int32)",
                         tuple9WithNames.ToTestDisplayString())

            Assert.Equal(New String() {"Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9"}, tuple9WithNames.TupleElementNames)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         tuple9WithNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_ElementTypeIsError()

            Dim tupleComp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, ErrorTypeSymbol.UnknownResultType)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, Nothing)

            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

            Dim types = tupleWithoutNames.TupleElementTypes
            Assert.Equal(2, types.Length)
            Assert.Equal(SymbolKind.NamedType, types(0).Kind)
            Assert.Equal(SymbolKind.ErrorType, types(1).Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_BadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})

            Dim intType As NamedTypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType)

            ' Illegal VB identifier and blank
            Dim tuple2 = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("123", ""))
            Assert.Equal({"123", ""}, tuple2.TupleElementNames)

            ' Reserved keywords
            Dim tuple3 = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("return", "class"))
            Assert.Equal({"return", "class"}, tuple3.TupleElementNames)

            Try
                comp.CreateTupleTypeSymbol(underlyingType:=intType)
                Assert.True(False)
            Catch ex As ArgumentException
                Assert.Contains(CodeAnalysisResources.TupleUnderlyingTypeMustBeTupleCompatible, ex.Message)
            End Try

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_CSharpElements()

            Dim csSource = "public class C { }"
            Dim csComp = CreateCSharpCompilation("CSharp", csSource,
                                                 compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csComp.VerifyDiagnostics()
            Dim csType = DirectCast(csComp.GlobalNamespace.GetMembers("C").Single(), INamedTypeSymbol)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})
            Try
                comp.CreateTupleTypeSymbol(csType, Nothing)
                Assert.True(False)
            Catch ex As ArgumentException
                Assert.Contains(VBResources.NotAVbSymbol, ex.Message)
            End Try

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_BadArguments()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)

            Assert.Throws(Of ArgumentNullException)(Sub() comp.CreateTupleTypeSymbol(elementTypes:=Nothing, elementNames:=Nothing))

            ' 0-tuple and 1-tuple are not supported at this point
            Assert.Throws(Of ArgumentException)(Sub() comp.CreateTupleTypeSymbol(elementTypes:=ImmutableArray(Of ITypeSymbol).Empty, elementNames:=Nothing))
            Assert.Throws(Of ArgumentException)(Sub() comp.CreateTupleTypeSymbol(elementTypes:=ImmutableArray.Create(intType), elementNames:=Nothing))

            ' If names are provided, you need as many as element types
            Assert.Throws(Of ArgumentException)(Sub() comp.CreateTupleTypeSymbol(elementTypes:=ImmutableArray.Create(intType, intType), elementNames:=ImmutableArray.Create("Item1")))

            ' null types aren't allowed
            Assert.Throws(Of ArgumentNullException)(Sub() comp.CreateTupleTypeSymbol(elementTypes:=ImmutableArray.Create(intType, Nothing), elementNames:=Nothing))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_WithValueTuple()

            Dim tupleComp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), ImmutableArray.Create(Of String)(Nothing, Nothing))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_NoNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), Nothing)

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.ErrorType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_WithNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), ImmutableArray.Create("Alice", "Bob"))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.ErrorType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(Alice As System.Int32, Bob As System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.Equal(New String() {"Alice", "Bob"}, tupleWithoutNames.TupleElementNames)
            Assert.Equal(New String() {"System.Int32", "System.String"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_WithBadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("Item2", "Item1"))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal("(Item2 As System.Int32, Item1 As System.Int32)", tupleWithoutNames.ToTestDisplayString())
            Assert.Equal(New String() {"Item2", "Item1"}, tupleWithoutNames.TupleElementNames)
            Assert.Equal(New String() {"System.Int32", "System.Int32"}, tupleWithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_Tuple8NoNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim tuple8WithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType),
                                                                Nothing)

            Assert.True(tuple8WithoutNames.IsTupleType)
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String)",
                         tuple8WithoutNames.ToTestDisplayString())

            Assert.True(tuple8WithoutNames.TupleElementNames.IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         tuple8WithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_Tuple8WithNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim tuple8WithNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType),
                                                            ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"))

            Assert.True(tuple8WithNames.IsTupleType)
            Assert.Equal("(Alice1 As System.Int32, Alice2 As System.String, Alice3 As System.Int32, Alice4 As System.String, Alice5 As System.Int32, Alice6 As System.String, Alice7 As System.Int32, Alice8 As System.String)",
                         tuple8WithNames.ToTestDisplayString())

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"}, tuple8WithNames.TupleElementNames)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         tuple8WithNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_Tuple9NoNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim tuple9WithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType, intType),
                                                                Nothing)

            Assert.True(tuple9WithoutNames.IsTupleType)
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32)",
                         tuple9WithoutNames.ToTestDisplayString())

            Assert.True(tuple9WithoutNames.TupleElementNames.IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         tuple9WithoutNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_Tuple9WithNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Dim tuple9WithNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType, intType),
                                                             ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"))

            Assert.True(tuple9WithNames.IsTupleType)
            Assert.Equal("(Alice1 As System.Int32, Alice2 As System.String, Alice3 As System.Int32, Alice4 As System.String, Alice5 As System.Int32, Alice6 As System.String, Alice7 As System.Int32, Alice8 As System.String, Alice9 As System.Int32)",
                         tuple9WithNames.ToTestDisplayString())

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"}, tuple9WithNames.TupleElementNames)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         tuple9WithNames.TupleElementTypes.Select(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_ElementTypeIsError()

            Dim tupleComp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, ErrorTypeSymbol.UnknownResultType), Nothing)

            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

            Dim types = tupleWithoutNames.TupleElementTypes
            Assert.Equal(2, types.Length)
            Assert.Equal(SymbolKind.NamedType, types(0).Kind)
            Assert.Equal(SymbolKind.ErrorType, types(1).Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_BadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)

            ' Illegal VB identifier and blank
            Dim tuple2 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("123", ""))
            Assert.Equal({"123", ""}, tuple2.TupleElementNames)

            ' Reserved keywords
            Dim tuple3 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("return", "class"))
            Assert.Equal({"return", "class"}, tuple3.TupleElementNames)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_CSharpElements()

            Dim csSource = "public class C { }"
            Dim csComp = CreateCSharpCompilation("CSharp", csSource,
                                                 compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csComp.VerifyDiagnostics()
            Dim csType = DirectCast(csComp.GlobalNamespace.GetMembers("C").Single(), INamedTypeSymbol)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Assert.Throws(Of ArgumentException)(Sub() comp.CreateTupleTypeSymbol(ImmutableArray.Create(stringType, csType), Nothing))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_ComparingSymbols()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb">
Class C
    Dim F As System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (a As String, b As String))
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            Dim tuple1 = comp.GlobalNamespace.GetMember(Of SourceMemberFieldSymbol)("C.F").Type

            Dim intType = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType = comp.GetSpecialType(SpecialType.System_String)

            Dim twoStrings = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, stringType)
            Dim twoStringsWithNames = DirectCast(comp.CreateTupleTypeSymbol(twoStrings, ImmutableArray.Create("a", "b")), TypeSymbol)
            Dim tuple2Underlying = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).Construct(intType, intType, intType, intType, intType, intType, intType, twoStringsWithNames)
            Dim tuple2 = DirectCast(comp.CreateTupleTypeSymbol(tuple2Underlying), TypeSymbol)

            Dim tuple3 = DirectCast(comp.CreateTupleTypeSymbol(ImmutableArray.Create(Of ITypeSymbol)(intType, intType, intType, intType, intType, intType, intType, stringType, stringType),
                                      ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "a", "b")), TypeSymbol)

            Dim tuple4 = DirectCast(comp.CreateTupleTypeSymbol(CType(tuple1.TupleUnderlyingType, INamedTypeSymbol),
                                      ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "a", "b")), TypeSymbol)

            Assert.True(tuple1.Equals(tuple2))
            'Assert.True(tuple1.Equals(tuple2, TypeCompareKind.IgnoreDynamicAndTupleNames))
            Assert.False(tuple1.Equals(tuple3))
            'Assert.True(tuple1.Equals(tuple3, TypeCompareKind.IgnoreDynamicAndTupleNames))
            Assert.False(tuple1.Equals(tuple4))
            'Assert.True(tuple1.Equals(tuple4, TypeCompareKind.IgnoreDynamicAndTupleNames))

        End Sub

        <Fact>
        Public Sub TupleMethodsOnNonTupleType()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})
            Dim intType As INamedTypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Assert.False(intType.IsTupleType)
            Assert.True(intType.TupleElementNames.IsDefault)
            Assert.True(intType.TupleElementTypes.IsDefault)

        End Sub

        <Fact>
        Public Sub TupleTargetTypeAndConvert01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
       ' This works
       Dim x1 As (Short, String) = (1, "hello")

       Dim x2 As (Short, String) = DirectCast((1, "hello"), (Long, String))

       Dim x3 As (a As Short, b As String) = DirectCast((1, "hello"), (c As Long, d As String))
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30512: Option Strict On disallows implicit conversions from '(Long, String)' to '(Short, String)'.
       Dim x2 As (Short, String) = DirectCast((1, "hello"), (Long, String))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from '(c As Long, d As String)' to '(a As Short, b As String)'.
       Dim x3 As (a As Short, b As String) = DirectCast((1, "hello"), (c As Long, d As String))
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleTargetTypeAndConvert02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x2 As (Short, String) = DirectCast((1, "hello"), (Byte, String))
        System.Console.WriteLine(x2)
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(1, hello)]]>)

            verifier.VerifyDiagnostics()
            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (System.ValueTuple(Of Byte, String) V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldstr      "hello"
  IL_0008:  call       "Sub System.ValueTuple(Of Byte, String)..ctor(Byte, String)"
  IL_000d:  ldloc.0
  IL_000e:  ldfld      "System.ValueTuple(Of Byte, String).Item1 As Byte"
  IL_0013:  ldloc.0
  IL_0014:  ldfld      "System.ValueTuple(Of Byte, String).Item2 As String"
  IL_0019:  newobj     "Sub System.ValueTuple(Of Short, String)..ctor(Short, String)"
  IL_001e:  box        "System.ValueTuple(Of Short, String)"
  IL_0023:  call       "Sub System.Console.WriteLine(Object)"
  IL_0028:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim x As (Integer, Integer)

        x = (Nothing, Nothing, Nothing)
        x = (1, 2, 3)
        x = (1, "string")
        x = (1, 1, garbage)
        x = (1, 1, )
        x = (Nothing, Nothing) ' ok
        x = (1, Nothing) ' ok
        x = (1, Function(t) t)
        x = Nothing ' ok
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Object, Object, Object)' cannot be converted to '(Integer, Integer)'.
        x = (Nothing, Nothing, Nothing)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, Integer)' cannot be converted to '(Integer, Integer)'.
        x = (1, 2, 3)
            ~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
        x = (1, "string")
                ~~~~~~~~
BC30451: 'garbage' is not declared. It may be inaccessible due to its protection level.
        x = (1, 1, garbage)
                   ~~~~~~~
BC30201: Expression expected.
        x = (1, 1, )
                   ~
BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        x = (1, Function(t) t)
                ~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleExplicitConversionFail01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim x As (Integer, Integer)

        x = DirectCast((Nothing, Nothing, Nothing), (Integer, Integer))
        x = DirectCast((1, 2, 3), (Integer, Integer))
        x = DirectCast((1, "string"), (Integer, Integer)) ' ok
        x = DirectCast((1, 1, garbage), (Integer, Integer))
        x = DirectCast((1, 1, ), (Integer, Integer))
        x = DirectCast((Nothing, Nothing), (Integer, Integer)) ' ok
        x = DirectCast((1, Nothing), (Integer, Integer)) ' ok
        x = DirectCast((1, Function(t) t), (Integer, Integer))
        x = DirectCast(Nothing, (Integer, Integer)) ' ok
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Object, Object, Object)' cannot be converted to '(Integer, Integer)'.
        x = DirectCast((Nothing, Nothing, Nothing), (Integer, Integer))
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, Integer)' cannot be converted to '(Integer, Integer)'.
        x = DirectCast((1, 2, 3), (Integer, Integer))
                       ~~~~~~~~~
BC30451: 'garbage' is not declared. It may be inaccessible due to its protection level.
        x = DirectCast((1, 1, garbage), (Integer, Integer))
                              ~~~~~~~
BC30201: Expression expected.
        x = DirectCast((1, 1, ), (Integer, Integer))
                              ~
BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        x = DirectCast((1, Function(t) t), (Integer, Integer))
                           ~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim x As System.ValueTuple(Of Integer, Integer)

        x = (Nothing, Nothing, Nothing)
        x = (1, 2, 3)
        x = (1, "string")
        x = (1, 1, garbage)
        x = (1, 1, )
        x = (Nothing, Nothing) ' ok
        x = (1, Nothing) ' ok
        x = (1, Function(t) t)
        x = Nothing ' ok
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Object, Object, Object)' cannot be converted to '(Integer, Integer)'.
        x = (Nothing, Nothing, Nothing)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, Integer)' cannot be converted to '(Integer, Integer)'.
        x = (1, 2, 3)
            ~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
        x = (1, "string")
                ~~~~~~~~
BC30451: 'garbage' is not declared. It may be inaccessible due to its protection level.
        x = (1, 1, garbage)
                   ~~~~~~~
BC30201: Expression expected.
        x = (1, 1, )
                   ~
BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        x = (1, Function(t) t)
                ~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleInferredLambdStrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim valid = (1, Function() Nothing)
        Dim x = (Nothing, Function(t) t)
        Dim y = (1, Function(t) t)
        Dim z = (Function(t) t, Function(t) t)
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        Dim x = (Nothing, Function(t) t)
                                   ~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        Dim y = (1, Function(t) t)
                             ~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        Dim z = (Function(t) t, Function(t) t)
                          ~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        Dim z = (Function(t) t, Function(t) t)
                                         ~
</errors>)

        End Sub

        <Fact()>
        Public Sub TupleInferredLambdStrictOff()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Class C
    Shared Sub Main()
        Dim valid = (1, Function() Nothing)
        Test(valid)

        Dim x = (Nothing, Function(t) t)
        Test(x)

        Dim y = (1, Function(t) t)
        Test(y)
    End Sub

    shared function Test(of T)(x as T) as T
        System.Console.WriteLine(GetType(T))

        return x
    End Function
End Class

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:=<![CDATA[
System.ValueTuple`2[System.Int32,VB$AnonymousDelegate_0`1[System.Object]]
System.ValueTuple`2[System.Object,VB$AnonymousDelegate_1`2[System.Object,System.Object]]
System.ValueTuple`2[System.Int32,VB$AnonymousDelegate_1`2[System.Object,System.Object]]
            ]]>)
        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail03()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim x As (String, String)

        x = (Nothing, Nothing, Nothing)
        x = (1, 2, 3)
        x = (1, "string")
        x = (1, 1, garbage)
        x = (1, 1, )
        x = (Nothing, Nothing) ' ok
        x = (1, Nothing) ' ok
        x = (1, Function(t) t)
        x = Nothing ' ok
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Object, Object, Object)' cannot be converted to '(String, String)'.
        x = (Nothing, Nothing, Nothing)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, Integer)' cannot be converted to '(String, String)'.
        x = (1, 2, 3)
            ~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        x = (1, "string")
             ~
BC30451: 'garbage' is not declared. It may be inaccessible due to its protection level.
        x = (1, 1, garbage)
                   ~~~~~~~
BC30201: Expression expected.
        x = (1, 1, )
                   ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        x = (1, Nothing) ' ok
             ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        x = (1, Function(t) t)
             ~
BC36625: Lambda expression cannot be converted to 'String' because 'String' is not a delegate type.
        x = (1, Function(t) t)
                ~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail04()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim x As ((Integer, Integer), Integer)

        x = ((Nothing, Nothing, Nothing), 1)
        x = ((1, 2, 3), 1)
        x = ((1, "string"), 1)
        x = ((1, 1, garbage), 1)
        x = ((1, 1, ), 1)
        x = ((Nothing, Nothing), 1) ' ok
        x = ((1, Nothing), 1) ' ok
        x = ((1, Function(t) t), 1)
        x = (Nothing, 1) ' ok
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Object, Object, Object)' cannot be converted to '(Integer, Integer)'.
        x = ((Nothing, Nothing, Nothing), 1)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, Integer)' cannot be converted to '(Integer, Integer)'.
        x = ((1, 2, 3), 1)
             ~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
        x = ((1, "string"), 1)
                 ~~~~~~~~
BC30451: 'garbage' is not declared. It may be inaccessible due to its protection level.
        x = ((1, 1, garbage), 1)
                    ~~~~~~~
BC30201: Expression expected.
        x = ((1, 1, ), 1)
                    ~
BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        x = ((1, Function(t) t), 1)
                 ~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail05()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Class C
    Shared Sub Main()
        Dim x As (x0 As System.ValueTuple(Of Integer, Integer), x1 As Integer, x2 As Integer, x3 As Integer, x4 As Integer, x5 As Integer, x6 As Integer, x7 As Integer, x8 As Integer, x9 As Integer, x10 As Integer)

        x = (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
        x = ((0, 0.0), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8 )
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9.1, 10)
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8,
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9
        x = ((0, 0), 1, 2, 3, 4, oops, 6, 7, oopsss, 9, 10)

        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9)
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, (1, 1, 1), 10)
    End Sub
End Class
]]><%= s_trivial2uple %><%= s_trivialRemainingTuples %></file>
</compilation>)
            ' Intentionally not including 3-tuple for use-site errors

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type 'Integer' cannot be converted to '(Integer, Integer)'.
        x = (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
             ~
BC30311: Value of type '((Integer, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)' cannot be converted to '(x0 As (Integer, Integer), x1 As Integer, x2 As Integer, x3 As Integer, x4 As Integer, x5 As Integer, x6 As Integer, x7 As Integer, x8 As Integer, x9 As Integer, x10 As Integer)'.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '((Integer, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)' cannot be converted to '(x0 As (Integer, Integer), x1 As Integer, x2 As Integer, x3 As Integer, x4 As Integer, x5 As Integer, x6 As Integer, x7 As Integer, x8 As Integer, x9 As Integer, x10 As Integer)'.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8 )
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,)' is not defined or imported.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8,
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30452: Operator '=' is not defined for types '(x0 As (Integer, Integer), x1 As Integer, x2 As Integer, x3 As Integer, x4 As Integer, x5 As Integer, x6 As Integer, x7 As Integer, x8 As Integer, x9 As Integer, x10 As Integer)' and '((Integer, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,)' is not defined or imported.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30198: ')' expected.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9
                                              ~
BC30198: ')' expected.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9
                                              ~
BC30451: 'oops' is not declared. It may be inaccessible due to its protection level.
        x = ((0, 0), 1, 2, 3, 4, oops, 6, 7, oopsss, 9, 10)
                                 ~~~~
BC30451: 'oopsss' is not declared. It may be inaccessible due to its protection level.
        x = ((0, 0), 1, 2, 3, 4, oops, 6, 7, oopsss, 9, 10)
                                             ~~~~~~
BC30311: Value of type '((Integer, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)' cannot be converted to '(x0 As (Integer, Integer), x1 As Integer, x2 As Integer, x3 As Integer, x4 As Integer, x5 As Integer, x6 As Integer, x7 As Integer, x8 As Integer, x9 As Integer, x10 As Integer)'.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,)' is not defined or imported.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, 9)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, Integer)' cannot be converted to 'Integer'.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, (1, 1, 1), 10)
                                             ~~~~~~~~~
BC37267: Predefined type 'ValueTuple(Of ,,)' is not defined or imported.
        x = ((0, 0), 1, 2, 3, 4, 5, 6, 7, 8, (1, 1, 1), 10)
                                             ~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail06()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Class C
    Shared Sub Main()
        Dim l As Func(Of String) = Function() 1
        Dim x As (String, Func(Of String)) = (Nothing, Function() 1)
        Dim l1 As Func(Of (String, String)) = Function() (Nothing, 1.1)
        Dim x1 As (String, Func(Of (String, String))) = (Nothing, Function() (Nothing, 1.1))
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim l As Func(Of String) = Function() 1
                                              ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim x As (String, Func(Of String)) = (Nothing, Function() 1)
                                                                  ~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'String'.
        Dim l1 As Func(Of (String, String)) = Function() (Nothing, 1.1)
                                                                   ~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'String'.
        Dim x1 As (String, Func(Of (String, String))) = (Nothing, Function() (Nothing, 1.1))
                                                                                       ~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleExplicitConversionFail06()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="comp">
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Class C
    Shared Sub Main()
        Dim l As Func(Of String) = Function() 1
        Dim x As (String, Func(Of String)) = DirectCast((Nothing, Function() 1), (String, Func(Of String)))
        Dim l1 As Func(Of (String, String)) = DirectCast(Function() (Nothing, 1.1), Func(Of (String, String)))
        Dim x1 As (String, Func(Of (String, String))) = DirectCast((Nothing, Function() (Nothing, 1.1)), (String, Func(Of (String, String))))
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim l As Func(Of String) = Function() 1
                                              ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim x As (String, Func(Of String)) = DirectCast((Nothing, Function() 1), (String, Func(Of String)))
                                                                             ~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'String'.
        Dim l1 As Func(Of (String, String)) = DirectCast(Function() (Nothing, 1.1), Func(Of (String, String)))
                                                                              ~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'String'.
        Dim x1 As (String, Func(Of (String, String))) = DirectCast((Nothing, Function() (Nothing, 1.1)), (String, Func(Of (String, String))))
                                                                                                  ~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleTargetTypeLambda()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Test(d As Func(Of Func(Of (Short, Short))))
        Console.WriteLine("short")
    End Sub
    Shared Sub Test(d As Func(Of Func(Of (Byte, Byte))))
        Console.WriteLine("byte")
    End Sub
    Shared Sub Main()
        Test(Function() Function() DirectCast((1, 1), (Byte, Byte)))
        Test(Function() Function() (1, 1))
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30521: Overload resolution failed because no accessible 'Test' is most specific for these arguments:
    'Public Shared Sub Test(d As Func(Of Func(Of (Short, Short))))': Not most specific.
    'Public Shared Sub Test(d As Func(Of Func(Of (Byte, Byte))))': Not most specific.
        Test(Function() Function() DirectCast((1, 1), (Byte, Byte)))
        ~~~~
BC30521: Overload resolution failed because no accessible 'Test' is most specific for these arguments:
    'Public Shared Sub Test(d As Func(Of Func(Of (Short, Short))))': Not most specific.
    'Public Shared Sub Test(d As Func(Of Func(Of (Byte, Byte))))': Not most specific.
        Test(Function() Function() (1, 1))
        ~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleTargetTypeLambda1()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Test(d As Func(Of (Func(Of Short), Integer)))
        Console.WriteLine("short")
    End Sub
    Shared Sub Test(d As Func(Of (Func(Of Byte), Integer)))
        Console.WriteLine("byte")
    End Sub
    Shared Sub Main()
        Test(Function() (Function() CType(1, Byte), 1))
        Test(Function() (Function() 1, 1))
    End Sub
End Class

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30521: Overload resolution failed because no accessible 'Test' is most specific for these arguments:
    'Public Shared Sub Test(d As Func(Of (Func(Of Short), Integer)))': Not most specific.
    'Public Shared Sub Test(d As Func(Of (Func(Of Byte), Integer)))': Not most specific.
        Test(Function() (Function() CType(1, Byte), 1))
        ~~~~
BC30521: Overload resolution failed because no accessible 'Test' is most specific for these arguments:
    'Public Shared Sub Test(d As Func(Of (Func(Of Short), Integer)))': Not most specific.
    'Public Shared Sub Test(d As Func(Of (Func(Of Byte), Integer)))': Not most specific.
        Test(Function() (Function() 1, 1))
        ~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TargetTypingOverload01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Test((Nothing, Nothing))
        Test((1, 1))
        Test((Function() 7, Function() 8), 2)
    End Sub
    Shared Sub Test(Of T)(x As (T, T))
        Console.WriteLine("first")
    End Sub
    Shared Sub Test(x As (Object, Object))
        Console.WriteLine("second")
    End Sub
    Shared Sub Test(Of T)(x As (Func(Of T), Func(Of T)), y As T)
        Console.WriteLine("third")
        Console.WriteLine(x.Item1().ToString())
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[second
first
third
7]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TargetTypingOverload02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Main()
        Test((Function() 7, Function() 8))
    End Sub
    Shared Sub Test(Of T)(x As (T, T))
        Console.WriteLine("first")
    End Sub
    Shared Sub Test(x As (Object, Object))
        Console.WriteLine("second")
    End Sub
    Shared Sub Test(Of T)(x As (Func(Of T), Func(Of T)))
        Console.WriteLine("third")
        Console.WriteLine(x.Item1().ToString())
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[second]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TargetTypingNullable01()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim x = M1()
        Test(x)
    End Sub
    Shared Function M1() As (a As Integer, b As Double)?
        Return (1, 2)
    End Function
    Shared Sub Test(Of T)(arg As T)
        Console.WriteLine(GetType(T))
        Console.WriteLine(arg)
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[System.Nullable`1[System.ValueTuple`2[System.Int32,System.Double]]
(1, 2)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TargetTypingOverload01Long()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Test((Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing))
        Test((1, 2, 3, 4, 5, 6, 7, 8, 9, 10))
        Test((Function() 11, Function() 12, Function() 13, Function() 14, Function() 15, Function() 16, Function() 17, Function() 18, Function() 19, Function() 20))
    End Sub
    Shared Sub Test(Of T)(x As (T, T, T, T, T, T, T, T, T, T))
        Console.WriteLine("first")
    End Sub
    Shared Sub Test(x As (Object, Object, Object, Object, Object, Object, Object, Object, Object, Object))
        Console.WriteLine("second")
    End Sub
    Shared Sub Test(Of T)(x As (Func(Of T), Func(Of T), Func(Of T), Func(Of T), Func(Of T), Func(Of T), Func(Of T), Func(Of T), Func(Of T), Func(Of T)), y As T)
        Console.WriteLine("third")
        Console.WriteLine(x.Item1().ToString())
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[second
first
second]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/12961")>
        Public Sub TargetTypingNullable02()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim x = M1()
        Test(x)
    End Sub
    Shared Function M1() As (a As Integer, b As String)?
        Return (1, Nothing)
    End Function
    Shared Sub Test(Of T)(arg As T)
        Console.WriteLine(GetType(T))
        Console.WriteLine(arg)
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[System.Nullable`1[System.ValueTuple`2[System.Int32,System.String]]
(1, )]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/12961")>
        Public Sub TargetTypingNullable02Long()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim x = M1()
        Console.WriteLine(x?.a)
        Console.WriteLine(x?.a8)
        Test(x)
    End Sub
    Shared Function M1() As (a As Integer, b As String, a1 As Integer, a2 As Integer, a3 As Integer, a4 As Integer, a5 As Integer, a6 As Integer, a7 As Integer, a8 As Integer)?
        Return (1, Nothing, 1, 2, 3, 4, 5, 6, 7, 8)
    End Function
    Shared Sub Test(Of T)(arg As T)
        Console.WriteLine(arg)
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[System.Nullable`1[System.ValueTuple`2[System.Int32,System.String]]
(1, )]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/12961")>
        Public Sub TargetTypingNullableOverload()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Test((Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)) ' Overload resolution fails
        Test(("a", "a", "a", "a", "a", "a", "a", "a", "a", "a"))
        Test((1, 1, 1, 1, 1, 1, 1, 1, 1, 1))
    End Sub
    Shared Sub Test(x As (String, String, String, String, String, String, String, String, String, String))
        Console.WriteLine("first")
    End Sub
    Shared Sub Test(x As (String, String, String, String, String, String, String, String, String, String)?)
        Console.WriteLine("second")
    End Sub
    Shared Sub Test(x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)?)
        Console.WriteLine("third")
    End Sub
    Shared Sub Test(x As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer))
        Console.WriteLine("fourth")
    End Sub
End Class
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[first
fourth]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/13277")>
        <WorkItem(13277, "https://github.com/dotnet/roslyn/issues/13277")>
        Public Sub CreateTupleTypeSymbol_UnderlyingTypeIsError()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})

            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.CreateErrorTypeSymbol(Nothing, "ValueTuple", 2).Construct(intType, intType)

            Dim tuple = comp.CreateTupleTypeSymbol(vt2, Nothing)
            ' Crashes in IsTupleCompatible

        End Sub

        <Fact>
        <WorkItem(13042, "https://github.com/dotnet/roslyn/issues/13042")>
        Public Sub GetSymbolInfoOnTupleType()
            Dim verifier = CompileAndVerify(
 <compilation>
     <file name="a.vb">
Module C
     Function M() As (System.Int32, String)
        throw new System.Exception()
     End Function
End Module

    </file>
 </compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim type = nodes.OfType(Of QualifiedNameSyntax)().First()
            Assert.Equal("System.Int32", type.ToString())
            Assert.NotNull(model.GetSymbolInfo(type).Symbol)
            Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub RetargetTupleErrorType()
            Dim libComp = CreateCompilationWithMscorlibAndVBRuntime(
 <compilation>
     <file name="a.vb">
Public Class A
     Public Shared Function M() As (Integer, Integer)
        Return (1, 2)
     End Function
End Class
     </file>
 </compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})
            libComp.AssertNoDiagnostics()


            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
 <compilation>
     <file name="a.vb">
Public Class B
     Public Sub M2()
        A.M()
     End Sub
End Class
     </file>
 </compilation>, additionalRefs:={libComp.ToMetadataReference()})

            comp.AssertTheseDiagnostics(
<errors>
BC30652: Reference required to assembly 'System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' containing the type 'ValueTuple(Of ,)'. Add one to your project.
        A.M()
        ~~~~~
</errors>)

            Dim methodM = comp.GetMember(Of MethodSymbol)("A.M")
            Assert.Equal("(System.Int32, System.Int32)", methodM.ReturnType.ToTestDisplayString())
            Assert.True(methodM.ReturnType.IsTupleType)
            Assert.False(methodM.ReturnType.IsErrorType())
            Assert.True(methodM.ReturnType.TupleUnderlyingType.IsErrorType())

        End Sub

        <Fact>
        Public Sub CaseSensitivity001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x2 = (A:=10, B:=20)
        System.Console.Write(x2.a)
        System.Console.Write(x2.item2)
        
        Dim x3 = (item1 := 1, item2 := 2)
        System.Console.Write(x3.Item1)
        System.Console.WriteLine(x3.Item2)

    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[102012]]>)

        End Sub

        <Fact>
        Public Sub CaseSensitivity002()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[

Module Module1
    Sub Main()
        Dim x1 = (A:=10, a:=20)
        System.Console.Write(x1.a)
        System.Console.Write(x1.A)

        Dim x2 as (A as Integer, a As Integer) = (10, 20)
        System.Console.Write(x1.a)
        System.Console.Write(x1.A)

        Dim x3 = (I1:=10, item1:=20)
        Dim x4 = (Item1:=10, item1:=20)
        Dim x5 = (item1:=10, item1:=20)
        Dim x6 = (tostring:=10, item1:=20)
    End Sub
End Module
        ]]>
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37262: Tuple element names must be unique.
        Dim x1 = (A:=10, a:=20)
                         ~
BC31429: 'A' is ambiguous because multiple kinds of members with this name exist in structure '(A As Integer, a As Integer)'.
        System.Console.Write(x1.a)
                             ~~~~
BC31429: 'A' is ambiguous because multiple kinds of members with this name exist in structure '(A As Integer, a As Integer)'.
        System.Console.Write(x1.A)
                             ~~~~
BC37262: Tuple element names must be unique.
        Dim x2 as (A as Integer, a As Integer) = (10, 20)
                                 ~
BC31429: 'A' is ambiguous because multiple kinds of members with this name exist in structure '(A As Integer, a As Integer)'.
        System.Console.Write(x1.a)
                             ~~~~
BC31429: 'A' is ambiguous because multiple kinds of members with this name exist in structure '(A As Integer, a As Integer)'.
        System.Console.Write(x1.A)
                             ~~~~
BC37261: Tuple element name 'item1' is only allowed at position 1.
        Dim x3 = (I1:=10, item1:=20)
                          ~~~~~
BC37261: Tuple element name 'item1' is only allowed at position 1.
        Dim x4 = (Item1:=10, item1:=20)
                             ~~~~~
BC37261: Tuple element name 'item1' is only allowed at position 1.
        Dim x5 = (item1:=10, item1:=20)
                             ~~~~~
BC37260: Tuple element name 'tostring' is disallowed at any position.
        Dim x6 = (tostring:=10, item1:=20)
                  ~~~~~~~~
BC37261: Tuple element name 'item1' is only allowed at position 1.
        Dim x6 = (tostring:=10, item1:=20)
                                ~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub CaseSensitivity003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim x as (Item1 as String, itEm2 as String, Bob as string) = (Nothing, Nothing, Nothing)
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(, , )
            ]]>)


            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(Nothing, Nothing, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)

            Dim fields = From m In model.GetTypeInfo(node).ConvertedType.GetMembers()
                         Where m.Kind = SymbolKind.Field
                         Order By m.Name
                         Select m.Name

            ' check no duplication of original/default ItemX fields
            Assert.Equal("Bob#Item1#Item2#Item3", fields.Join("#"))
        End Sub

        <Fact>
        Public Sub CaseSensitivity004()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C

    Sub Main()
        Dim x = (
                    I1 := 1,
                    I2 := 2,
                    I3 := 3,
                    ITeM4 := 4,
                    I5 := 5,
                    I6 := 6,
                    I7 := 7,
                    ITeM8 := 8,
                    ItEM9 := 9
                )

        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(1, 2, 3, 4, 5, 6, 7, 8, 9)
            ]]>)


            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Dim fields = From m In model.GetTypeInfo(node).Type.GetMembers()
                         Where m.Kind = SymbolKind.Field
                         Order By m.Name
                         Select m.Name

            ' check no duplication of original/default ItemX fields
            Assert.Equal("I1#I2#I3#I5#I6#I7#Item1#Item2#Item3#Item4#Item5#Item6#Item7#Item8#Item9#Rest", fields.Join("#"))
        End Sub

        <Fact>
        Public Sub TupleNamesFromCS001()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Class1
    {
        public (int Alice, int Bob) foo = (2, 3);

        public (int Alice, int Bob) Bar() => (4, 5);

        public (int Alice, int Bob) Baz => (6, 7);

    }

    public class Class2
    {
        public (int Alice, int q, int w, int e, int f, int g, int h, int j, int Bob) foo = SetBob(11);

        public (int Alice, int q, int w, int e, int f, int g, int h, int j, int Bob) Bar() => SetBob(12);

        public (int Alice, int q, int w, int e, int f, int g, int h, int j, int Bob) Baz => SetBob(13);

        private static (int Alice, int q, int w, int e, int f, int g, int h, int j, int Bob) SetBob(int x)
        {
            var result = default((int Alice, int q, int w, int e, int f, int g, int h, int j, int Bob));
            result.Bob = x;
            return result;
        }
    }

    public class class3: IEnumerable<(int Alice, int Bob)>
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<(Int32 Alice, Int32 Bob)> IEnumerable<(Int32 Alice, Int32 Bob)>.GetEnumerator()
        {
            yield return (1, 2);
            yield return (3, 4);
        }
    }
}

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim x As New ClassLibrary1.Class1
        System.Console.WriteLine(x.foo.Alice)
        System.Console.WriteLine(x.foo.Bob)
        System.Console.WriteLine(x.Bar.Alice)
        System.Console.WriteLine(x.Bar.Bob)
        System.Console.WriteLine(x.Baz.Alice)
        System.Console.WriteLine(x.Baz.Bob)

        Dim y As New ClassLibrary1.Class2
        System.Console.WriteLine(y.foo.Alice)
        System.Console.WriteLine(y.foo.Bob)
        System.Console.WriteLine(y.Bar.Alice)
        System.Console.WriteLine(y.Bar.Bob)
        System.Console.WriteLine(y.Baz.Alice)
        System.Console.WriteLine(y.Baz.Bob)

        Dim z As New ClassLibrary1.class3
        For Each item In z
            System.Console.WriteLine(item.Alice)
            System.Console.WriteLine(item.Bob)
        Next

    End Sub
End Module

]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="
2
3
4
5
6
7
0
11
0
12
0
13
1
2
3
4")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleNamesFromVB001()

            Dim classLib = CreateVisualBasicCompilation("VBClass",
            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

Namespace ClassLibrary1
    Public Class Class1
        Public foo As (Alice As Integer, Bob As Integer) = (2, 3)

        Public Function Bar() As (Alice As Integer, Bob As Integer)
            Return (4, 5)
        End Function

        Public ReadOnly Property Baz As (Alice As Integer, Bob As Integer)
            Get
                Return (6, 7)
            End Get
        End Property
    End Class

    Public Class Class2
        Public foo As (Alice As Integer, q As Integer, w As Integer, e As Integer, f As Integer, g As Integer, h As Integer, j As Integer, Bob As Integer) = SetBob(11)

        Public Function Bar() As (Alice As Integer, q As Integer, w As Integer, e As Integer, f As Integer, g As Integer, h As Integer, j As Integer, Bob As Integer)
            Return SetBob(12)
        End Function

        Public ReadOnly Property Baz As (Alice As Integer, q As Integer, w As Integer, e As Integer, f As Integer, g As Integer, h As Integer, j As Integer, Bob As Integer)
            Get
                Return SetBob(13)
            End Get
        End Property

        Private Shared Function SetBob(x As Integer) As (Alice As Integer, q As Integer, w As Integer, e As Integer, f As Integer, g As Integer, h As Integer, j As Integer, Bob As Integer)
            Dim result As (Alice As Integer, q As Integer, w As Integer, e As Integer, f As Integer, g As Integer, h As Integer, j As Integer, Bob As Integer) = Nothing
            result.Bob = x
            Return result
        End Function
    End Class

    Public Class class3
        Implements IEnumerable(Of (Alice As Integer, Bob As Integer))

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function

        Public Iterator Function GetEnumerator() As IEnumerator(Of (Alice As Integer, Bob As Integer)) Implements IEnumerable(Of (Alice As Integer, Bob As Integer)).GetEnumerator
            Yield (1, 2)
            Yield (3, 4)
        End Function
    End Class
End Namespace

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim x As New ClassLibrary1.Class1
        System.Console.WriteLine(x.foo.Alice)
        System.Console.WriteLine(x.foo.Bob)
        System.Console.WriteLine(x.Bar.Alice)
        System.Console.WriteLine(x.Bar.Bob)
        System.Console.WriteLine(x.Baz.Alice)
        System.Console.WriteLine(x.Baz.Bob)

        Dim y As New ClassLibrary1.Class2
        System.Console.WriteLine(y.foo.Alice)
        System.Console.WriteLine(y.foo.Bob)
        System.Console.WriteLine(y.Bar.Alice)
        System.Console.WriteLine(y.Bar.Bob)
        System.Console.WriteLine(y.Baz.Alice)
        System.Console.WriteLine(y.Baz.Bob)

        Dim z As New ClassLibrary1.class3
        For Each item In z
            System.Console.WriteLine(item.Alice)
            System.Console.WriteLine(item.Bob)
        Next

    End Sub
End Module

]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={classLib},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="
2
3
4
5
6
7
0
11
0
12
0
13
1
2
3
4")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleNamesFromVB001_InterfaceImpl()

            Dim classLib = CreateVisualBasicCompilation("VBClass",
            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

Namespace ClassLibrary1
    Public Class class3
        Implements IEnumerable(Of (Alice As Integer, Bob As Integer))

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Iterator Function GetEnumerator() As IEnumerator(Of (Alice As Integer, Bob As Integer)) Implements IEnumerable(Of (Alice As Integer, Bob As Integer)).GetEnumerator
            Yield (1, 2)
            Yield (3, 4)
        End Function
    End Class
End Namespace

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim z As New ClassLibrary1.class3
        For Each item In z
            System.Console.WriteLine(item.Alice)
            System.Console.WriteLine(item.Bob)
        Next

    End Sub
End Module

]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={classLib},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="
1
2
3
4")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleNamesFromCS002()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Class1
    {
        public (int Alice, (int Alice, int Bob) Bob) foo = (2, (2, 3));

        public ((int Alice, int Bob)[] Alice, int Bob) Bar() => (new(int, int)[] { (4, 5) }, 5);

        public (int Alice, List<(int Alice, int Bob)?> Bob) Baz => (6, new List<(int Alice, int Bob)?>() { (8, 9) });

        public static event Action<(int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7, (int Alice, int Bob) Bob)> foo1;

        public static void raise()
        {
            foo1((0, 1, 2, 3, 4, 5, 6, 7, (8, 42)));
        }
    }
}

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim x As New ClassLibrary1.Class1
        System.Console.WriteLine(x.foo.Bob.Bob)
        System.Console.WriteLine(x.foo.Item2.Item2)
        System.Console.WriteLine(x.Bar.Alice(0).Bob)
        System.Console.WriteLine(x.Bar.Item1(0).Item2)
        System.Console.WriteLine(x.Baz.Bob(0).Value)
        System.Console.WriteLine(x.Baz.Item2(0).Value)

        AddHandler ClassLibrary1.Class1.foo1, Sub(p)
                                                  System.Console.WriteLine(p.Bob.Bob)
                                                  System.Console.WriteLine(p.Rest.Item2.Bob)
                                              End Sub

        ClassLibrary1.Class1.raise()

    End Sub

End Module


]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="
3
3
5
5
(8, 9)
(8, 9)
42
42")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleNamesFromVB002()

            Dim classLib = CreateVisualBasicCompilation("VBClass",
            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

Namespace ClassLibrary1
    Public Class Class1
        Public foo As (Alice As Integer, Bob As (Alice As Integer, Bob As Integer)) = (2, (2, 3))

        Public Function Bar() As (Alice As (Alice As Integer, Bob As Integer)(), Bob As Integer)
            Return (New(Integer, Integer)() {(4, 5)}, 5)
        End Function

        Public ReadOnly Property Baz As (Alice As Integer, Bob As List(Of (Alice As Integer, Bob As Integer) ?))
            Get
                Return (6, New List(Of (Alice As Integer, Bob As Integer) ?)() From {(8, 9)})
            End Get
        End Property

        Public Shared Event foo1 As Action(Of (i0 As Integer, i1 As Integer, i2 As Integer, i3 As Integer, i4 As Integer, i5 As Integer, i6 As Integer, i7 As Integer, Bob As (Alice As Integer, Bob As Integer)))

        Public Shared Sub raise()
            RaiseEvent foo1((0, 1, 2, 3, 4, 5, 6, 7, (8, 42)))
        End Sub
    End Class
End Namespace

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim x As New ClassLibrary1.Class1
        System.Console.WriteLine(x.foo.Bob.Bob)
        System.Console.WriteLine(x.foo.Item2.Item2)
        System.Console.WriteLine(x.Bar.Alice(0).Bob)
        System.Console.WriteLine(x.Bar.Item1(0).Item2)
        System.Console.WriteLine(x.Baz.Bob(0).Value)
        System.Console.WriteLine(x.Baz.Item2(0).Value)

        AddHandler ClassLibrary1.Class1.foo1, Sub(p)
                                                  System.Console.WriteLine(p.Bob.Bob)
                                                  System.Console.WriteLine(p.Rest.Item2.Bob)
                                              End Sub

        ClassLibrary1.Class1.raise()

    End Sub

End Module


]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={classLib},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbexeVerifier = CompileAndVerify(vbCompilation,
                                                 expectedOutput:="
3
3
5
5
(8, 9)
(8, 9)
42
42")

            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleNamesFromCS003()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Class1
    {
        public (int Alice, int alice) foo = (2, 3);
    }
}

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim x As New ClassLibrary1.Class1
        System.Console.WriteLine(x.foo.Item1)
        System.Console.WriteLine(x.foo.Item2)
        System.Console.WriteLine(x.foo.Alice)
        System.Console.WriteLine(x.foo.alice)

        Dim f = x.foo
        System.Console.WriteLine(f.Item1)
        System.Console.WriteLine(f.Item2)
        System.Console.WriteLine(f.Alice)
        System.Console.WriteLine(f.alice)

    End Sub

End Module


]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            vbCompilation.VerifyDiagnostics(
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.foo.Alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer)").WithLocation(8, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.foo.alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer)").WithLocation(9, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "f.Alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer)").WithLocation(14, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "f.alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer)").WithLocation(15, 34)
)
        End Sub

        <Fact>
        Public Sub TupleNamesFromCS004()

            Dim csCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Class1
    {
        public (int Alice, int alice, int) foo = (2, 3, 4);
    }
}

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                        referencedAssemblies:=s_valueTupleRefsAndDefault)

            Dim vbCompilation = CreateVisualBasicCompilation("VBDll",
            <![CDATA[
Module Module1

    Sub Main()
        Dim x As New ClassLibrary1.Class1
        System.Console.WriteLine(x.foo.Item1)
        System.Console.WriteLine(x.foo.Item2)
        System.Console.WriteLine(x.foo.Item3)
        System.Console.WriteLine(x.foo.Alice)
        System.Console.WriteLine(x.foo.alice)

        Dim f = x.foo
        System.Console.WriteLine(f.Item1)
        System.Console.WriteLine(f.Item2)
        System.Console.WriteLine(f.Item3)
        System.Console.WriteLine(f.Alice)
        System.Console.WriteLine(f.alice)

    End Sub

End Module


]]>,
                            compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                            referencedCompilations:={csCompilation},
                            referencedAssemblies:=s_valueTupleRefsAndDefault)

            vbCompilation.VerifyDiagnostics(
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.foo.Alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer, Integer)").WithLocation(9, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.foo.alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer, Integer)").WithLocation(10, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "f.Alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer, Integer)").WithLocation(16, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "f.alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer, Integer)").WithLocation(17, 34)
                )
        End Sub

        <Fact>
        Public Sub BadTupleNameMetadata()
            Dim comp = CreateCompilationWithCustomILSource(<compilation>
                                                               <file name="a.vb">
                                                               </file>
                                                           </compilation>,
"
.assembly extern mscorlib { }
.assembly extern System.ValueTuple
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )
  .ver 4:0:1:0
}

.class public auto ansi C
       extends [mscorlib]System.Object
{
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> ValidField

    .field public int32 ValidFieldWithAttribute
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[1](""name1"")}
        = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )

    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> TooFewNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[1](""name1"")}
        = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )

    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> TooManyNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[3](""e1"", ""e2"", ""e3"")}
        = ( 01 00 03 00 00 00 02 65 31 02 65 32 02 65 33 )

    .method public hidebysig instance class [System.ValueTuple]System.ValueTuple`2<int32,int32> 
            TooFewNamesMethod() cil managed
    {
      .param [0]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
          // = {string[1](""name1"")}
          = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.0
      IL_0002:  newobj     instance void class [System.ValueTuple]System.ValueTuple`2<int32,int32>::.ctor(!0,
                                                                                                          !1)
      IL_0007:  ret
    } // end of method C::TooFewNamesMethod

    .method public hidebysig instance class [System.ValueTuple]System.ValueTuple`2<int32,int32> 
            TooManyNamesMethod() cil managed
    {
      .param [0]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
           // = {string[3](""e1"", ""e2"", ""e3"")}
           = ( 01 00 03 00 00 00 02 65 31 02 65 32 02 65 33 )
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.0
      IL_0002:  newobj     instance void class [System.ValueTuple]System.ValueTuple`2<int32,int32>::.ctor(!0,
                                                                                                          !1)
      IL_0007:  ret
    } // end of method C::TooManyNamesMethod
} // end of class C
",
additionalReferences:=s_valueTupleRefs)

            Dim c = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")

            Dim validField = c.GetMember(Of FieldSymbol)("ValidField")
            Assert.False(validField.Type.IsErrorType())
            Assert.True(validField.Type.IsTupleType)
            Assert.True(validField.Type.TupleElementNames.IsDefault)

            Dim validFieldWithAttribute = c.GetMember(Of FieldSymbol)("ValidFieldWithAttribute")
            Assert.True(validFieldWithAttribute.Type.IsErrorType())
            Assert.False(validFieldWithAttribute.Type.IsTupleType)
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(validFieldWithAttribute.Type)

            Dim tooFewNames = c.GetMember(Of FieldSymbol)("TooFewNames")
            Assert.True(tooFewNames.Type.IsErrorType())
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(tooFewNames.Type)

            Dim tooManyNames = c.GetMember(Of FieldSymbol)("TooManyNames")
            Assert.True(tooManyNames.Type.IsErrorType())
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(tooManyNames.Type)

            Dim tooFewNamesMethod = c.GetMember(Of MethodSymbol)("TooFewNamesMethod")
            Assert.True(tooFewNamesMethod.ReturnType.IsErrorType())
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(tooFewNamesMethod.ReturnType)

            Dim tooManyNamesMethod = c.GetMember(Of MethodSymbol)("TooManyNamesMethod")
            Assert.True(tooManyNamesMethod.ReturnType.IsErrorType())
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(tooManyNamesMethod.ReturnType)
        End Sub

        <Fact>
        Public Sub MetadataForPartiallyNamedTuples()
            Dim comp = CreateCompilationWithCustomILSource(<compilation>
                                                               <file name="a.vb">
                                                               </file>
                                                           </compilation>,
"
.assembly extern mscorlib { }
.assembly extern System.ValueTuple
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )
  .ver 4:0:1:0
}

.class public auto ansi C
       extends [mscorlib]System.Object
{
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> ValidField

    .field public int32 ValidFieldWithAttribute
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[1](""name1"")}
        = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )

    // In source, all or no names must be specified for a tuple
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> PartialNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[2](""e1"", null)}
        = ( 01 00 02 00 00 00 02 65 31 FF )

    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> AllNullNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[2](null, null)}
        = ( 01 00 02 00 00 00 ff ff 00 00 )

    .method public hidebysig instance void PartialNamesMethod(
        class [System.ValueTuple]System.ValueTuple`1<class [System.ValueTuple]System.ValueTuple`2<int32,int32>> c) cil managed
    {
      .param [1]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // First null is fine (unnamed tuple) but the second is half-named
        // = {string[3](null, ""e1"", null)}
        = ( 01 00 03 00 00 00 FF 02 65 31 FF )
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method C::PartialNamesMethod

    .method public hidebysig instance void AllNullNamesMethod(
        class [System.ValueTuple]System.ValueTuple`1<class [System.ValueTuple]System.ValueTuple`2<int32,int32>> c) cil managed
    {
      .param [1]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // First null is fine (unnamed tuple) but the second is half-named
        // = {string[3](null, null, null)}
        = ( 01 00 03 00 00 00 ff ff ff 00 00 )
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method C::AllNullNamesMethod
} // end of class C
",
additionalReferences:=s_valueTupleRefs)

            Dim c = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")

            Dim validField = c.GetMember(Of FieldSymbol)("ValidField")
            Assert.False(validField.Type.IsErrorType())
            Assert.True(validField.Type.IsTupleType)
            Assert.True(validField.Type.TupleElementNames.IsDefault)

            Dim validFieldWithAttribute = c.GetMember(Of FieldSymbol)("ValidFieldWithAttribute")
            Assert.True(validFieldWithAttribute.Type.IsErrorType())
            Assert.False(validFieldWithAttribute.Type.IsTupleType)
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(validFieldWithAttribute.Type)

            Dim partialNames = c.GetMember(Of FieldSymbol)("PartialNames")
            Assert.False(partialNames.Type.IsErrorType())
            Assert.True(partialNames.Type.IsTupleType)
            Assert.Equal("(e1 As System.Int32, System.Int32)", partialNames.Type.ToTestDisplayString())

            Dim allNullNames = c.GetMember(Of FieldSymbol)("AllNullNames")
            Assert.False(allNullNames.Type.IsErrorType())
            Assert.True(allNullNames.Type.IsTupleType)
            Assert.Equal("(System.Int32, System.Int32)", allNullNames.Type.ToTestDisplayString())

            Dim partialNamesMethod = c.GetMember(Of MethodSymbol)("PartialNamesMethod")
            Dim partialParamType = partialNamesMethod.Parameters.Single().Type
            Assert.False(partialParamType.IsErrorType())
            Assert.True(partialParamType.IsTupleType)
            Assert.Equal("((e1 As System.Int32, System.Int32))", partialParamType.ToTestDisplayString())

            Dim allNullNamesMethod = c.GetMember(Of MethodSymbol)("AllNullNamesMethod")
            Dim allNullParamType = allNullNamesMethod.Parameters.Single().Type
            Assert.False(allNullParamType.IsErrorType())
            Assert.True(allNullParamType.IsTupleType)
            Assert.Equal("((System.Int32, System.Int32))", allNullParamType.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub NestedTuplesNoAttribute()
            Dim comp = CreateCompilationWithCustomILSource(<compilation>
                                                               <file name="a.vb">
                                                               </file>
                                                           </compilation>,
"
.assembly extern mscorlib { }
.assembly extern System.ValueTuple
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )
  .ver 4:0:1:0
}

.class public auto ansi beforefieldinit Base`1<T>
       extends [mscorlib]System.Object
{
}

.class public auto ansi C
       extends [mscorlib]System.Object
{
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> Field1

    .field public class Base`1<class [System.ValueTuple]System.ValueTuple`1<
                                                                               class [System.ValueTuple]System.ValueTuple`2<int32, int32>>>  Field2;

} // end of class C
",
additionalReferences:=s_valueTupleRefs)

            Dim c = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")

            Dim base1 = comp.GlobalNamespace.GetTypeMember("Base")
            Assert.NotNull(base1)

            Dim field1 = c.GetMember(Of FieldSymbol)("Field1")
            Assert.False(field1.Type.IsErrorType())
            Assert.True(field1.Type.IsTupleType)
            Assert.True(field1.Type.TupleElementNames.IsDefault)

            Dim field2Type = DirectCast(c.GetMember(Of FieldSymbol)("Field2").Type, NamedTypeSymbol)
            Assert.Equal(base1, field2Type.OriginalDefinition)
            Assert.True(field2Type.IsGenericType)

            Dim first = field2Type.TypeArguments(0)
            Assert.True(first.IsTupleType)
            Assert.Equal(1, first.TupleElementTypes.Length)
            Assert.True(first.TupleElementNames.IsDefault)

            Dim second = first.TupleElementTypes(0)
            Assert.True(second.IsTupleType)
            Assert.True(second.TupleElementNames.IsDefault)
            Assert.Equal(2, second.TupleElementTypes.Length)
            Assert.All(second.TupleElementTypes,
                       Sub(t) Assert.Equal(SpecialType.System_Int32, t.SpecialType))
        End Sub

        <Fact>
        <WorkItem(258853, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/258853")>
        Public Sub BadOverloadWithTupleLiteralWithNaturalType()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Module C
    Sub M(x As Integer)
    End Sub
    Sub M(x As String)
    End Sub

    Sub Main()
        M((1, 2))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)


            compilation.AssertTheseDiagnostics(
<errors>
    BC30518: Overload resolution failed because no accessible 'M' can be called with these arguments:
    'Public Sub M(x As Integer)': Value of type '(Integer, Integer)' cannot be converted to 'Integer'.
    'Public Sub M(x As String)': Value of type '(Integer, Integer)' cannot be converted to 'String'.
        M((1, 2))
        ~
</errors>)
        End Sub

        <Fact>
        <WorkItem(258853, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/258853")>
        Public Sub BadOverloadWithTupleLiteralWithNothing()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Module C
    Sub M(x As Integer)
    End Sub
    Sub M(x As String)
    End Sub

    Sub Main()
        M((1, Nothing))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)


            compilation.AssertTheseDiagnostics(
<errors>
    BC30518: Overload resolution failed because no accessible 'M' can be called with these arguments:
    'Public Sub M(x As Integer)': Value of type '(Integer, Object)' cannot be converted to 'Integer'.
    'Public Sub M(x As String)': Value of type '(Integer, Object)' cannot be converted to 'String'.
        M((1, Nothing))
        ~
</errors>)
        End Sub

        <Fact>
        <WorkItem(258853, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/258853")>
        Public Sub BadOverloadWithTupleLiteralWithAddressOf()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Module C
    Sub M(x As Integer)
    End Sub
    Sub M(x As String)
    End Sub

    Sub Main()
        M((1, AddressOf Main))
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)


            compilation.AssertTheseDiagnostics(
<errors>
    BC30518: Overload resolution failed because no accessible 'M' can be called with these arguments:
    'Public Sub M(x As Integer)': Expression does not produce a value.
    'Public Sub M(x As String)': Expression does not produce a value.
        M((1, AddressOf Main))
        ~
</errors>)
        End Sub

        <Fact>
        Public Sub TupleLiteralWithOnlySomeNames()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Dim t As (Integer, String, Integer) = (1, b:="hello", Item3:=3)
        console.write($"{t.Item1} {t.Item2} {t.Item3}")
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef}, expectedOutput:="1 hello 3")

        End Sub

        <Fact()>
        <WorkItem(13705, "https://github.com/dotnet/roslyn/issues/13705")>
        Public Sub TupleCoVariance()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb"><![CDATA[
Interface I(Of Out T)
    Function M() As System.ValueTuple(Of Integer, T)
End Interface
]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36726: Type 'T' cannot be used for the 'T2' in 'System.ValueTuple(Of T1, T2)' in this context because 'T' is an 'Out' type parameter.
    Function M() As System.ValueTuple(Of Integer, T)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        <WorkItem(13705, "https://github.com/dotnet/roslyn/issues/13705")>
        Public Sub TupleCoVariance2()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb"><![CDATA[
Interface I(Of Out T)
    Function M() As (Integer, T)
End Interface
]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36726: Type 'T' cannot be used for the 'T2' in 'System.ValueTuple(Of T1, T2)' in this context because 'T' is an 'Out' type parameter.
    Function M() As (Integer, T)
                    ~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        <WorkItem(13705, "https://github.com/dotnet/roslyn/issues/13705")>
        Public Sub TupleContraVariance()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb"><![CDATA[
Interface I(Of In T)
    Sub M(x As (Boolean, T))
End Interface
]]></file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

            comp.AssertTheseDiagnostics(
<errors>
BC36727: Type 'T' cannot be used for the 'T2' in 'System.ValueTuple(Of T1, T2)' in this context because 'T' is an 'In' type parameter.
    Sub M(x As (Boolean, T))
               ~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub DefiniteAssignment001()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim  ss as (A as string, B as string)

        ss.A = "q"
        ss.Item2 = "w"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, w)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment001Err()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim  ss as (A as string, B as string)

        ss.A = "q"
        'ss.Item2 = "w"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, )]]>)

            verifier.VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss").WithArguments("ss").WithLocation(8, 34)
            )
        End Sub

        <Fact>
        Public Sub DefiniteAssignment002()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim  ss as (A as string, B as string)

        ss.A = "q"
        ss.B = "q"
        ss.Item1 = "w"
        ss.Item2 = "w"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(w, w)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment003()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (A as string, D as (B as string, C as string ))

        ss.A = "q"
        ss.D.B = "w"
        ss.D.C = "e"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, (w, e))]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment004()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as  (I1 as string , 
            I2  As string, 
            I3  As string, 
            I4  As string, 
            I5  As string, 
            I6  As string, 
            I7  As string, 
            I8  As string, 
            I9  As string, 
            I10 As string)

        ss.I1 = "q"
        ss.I2 = "q"
        ss.I3 = "q"
        ss.I4 = "q"
        ss.I5 = "q"
        ss.I6 = "q"
        ss.I7 = "q"
        ss.I8 = "q"
        ss.I9 = "q"
        ss.I10 = "q"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, q, q, q)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment005()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
            I2  As String, 
            I3  As String, 
            I4  As String, 
            I5  As String, 
            I6  As String, 
            I7  As String, 
            I8  As String, 
            I9  As String, 
            I10 As String)

        ss.Item1 = "q"
        ss.Item2 = "q"
        ss.Item3 = "q"
        ss.Item4 = "q"
        ss.Item5 = "q"
        ss.Item6 = "q"
        ss.Item7 = "q"
        ss.Item8 = "q"
        ss.Item9 = "q"
        ss.Item10 = "q"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, q, q, q)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment006()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
                    I2  As String, 
                    I3  As String, 
                    I4  As String, 
                    I5  As String, 
                    I6  As String, 
                    I7  As String, 
                    I8  As String, 
                    I9  As String, 
                    I10 As String)

        ss.Item1 = "q"
        ss.I2 = "q"
        ss.Item3 = "q"
        ss.I4 = "q"
        ss.Item5 = "q"
        ss.I6 = "q"
        ss.Item7 = "q"
        ss.I8 = "q"
        ss.Item9 = "q"
        ss.I10 = "q"

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, q, q, q)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment007()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
                    I2  As String, 
                    I3  As String, 
                    I4  As String, 
                    I5  As String, 
                    I6  As String, 
                    I7  As String, 
                    I8  As String, 
                    I9  As String, 
                    I10 As String)

        ss.Item1 = "q"
        ss.I2 = "q"
        ss.Item3 = "q"
        ss.I4 = "q"
        ss.Item5 = "q"
        ss.I6 = "q"
        ss.Item7 = "q"
        ss.I8 = "q"
        ss.Item9 = "q"
        ss.I10 = "q"

        System.Console.WriteLine(ss.Rest)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment008()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
                    I2  As String, 
                    I3  As String, 
                    I4  As String, 
                    I5  As String, 
                    I6  As String, 
                    I7  As String, 
                    I8  As String, 
                    I9  As String, 
                    I10 As String)

        ss.I8 = "q"
        ss.Item9 = "q"
        ss.I10 = "q"

        System.Console.WriteLine(ss.Rest)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q)]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment008long()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
                    I2  As String, 
                    I3  As String, 
                    I4  As String, 
                    I5  As String, 
                    I6  As String, 
                    I7  As String, 
                    I8  As String, 
                    I9  As String, 
                    I10 As String,
                    I11 as string, 
                    I12  As String, 
                    I13  As String, 
                    I14  As String, 
                    I15  As String, 
                    I16  As String, 
                    I17  As String, 
                    I18  As String, 
                    I19  As String,
                    I20 As String,
                    I21 as string, 
                    I22  As String, 
                    I23  As String, 
                    I24  As String, 
                    I25  As String, 
                    I26  As String, 
                    I27  As String, 
                    I28  As String, 
                    I29  As String,
                    I30  As String,
                    I31  As String)

        'warn 
        System.Console.WriteLine(ss.Rest.Rest.Rest)
        'warn 
        System.Console.WriteLine(ss.I31)

        ss.I29 = "q"
        ss.Item30 = "q"
        ss.I31 = "q"

        System.Console.WriteLine(ss.I29)
        System.Console.WriteLine(ss.Rest.Rest.Rest)
        System.Console.WriteLine(ss.I31)

        ' warn
        System.Console.WriteLine(ss.Rest.Rest)

        ' warn
        System.Console.WriteLine(ss.Rest)

        ' warn
        System.Console.WriteLine(ss)

        ' warn
        System.Console.WriteLine(ss.I2)

    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(, , , , , , , , , )

q
(, , , , , , , q, q, q)
q
(, , , , , , , , , , , , , , q, q, q)
(, , , , , , , , , , , , , , , , , , , , , q, q, q)
(, , , , , , , , , , , , , , , , , , , , , , , , , , , , q, q, q)]]>)

            verifier.VerifyDiagnostics(
    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss.Rest.Rest.Rest").WithArguments("Rest").WithLocation(36, 34),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "ss.I31").WithArguments("I31").WithLocation(38, 34),
    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss.Rest.Rest").WithArguments("Rest").WithLocation(49, 34),
    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss.Rest").WithArguments("Rest").WithLocation(52, 34),
    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss").WithArguments("ss").WithLocation(55, 34),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "ss.I2").WithArguments("I2").WithLocation(58, 34))

        End Sub

        <Fact>
        Public Sub DefiniteAssignment009()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
                    I2  As String, 
                    I3  As String, 
                    I4  As String, 
                    I5  As String, 
                    I6  As String, 
                    I7  As String, 
                    I8  As String, 
                    I9  As String, 
                    I10 As String)

        ss.I1 = "q"
        ss.I2 = "q"
        ss.I3 = "q"
        ss.I4 = "q"
        ss.I5 = "q"
        ss.I6 = "q"
        ss.I7 = "q"
        ss.Rest = Nothing

        System.Console.WriteLine(ss)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, , , )]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment010()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim ss as (I1 as string, 
                    I2  As String, 
                    I3  As String, 
                    I4  As String, 
                    I5  As String, 
                    I6  As String, 
                    I7  As String, 
                    I8  As String, 
                    I9  As String, 
                    I10 As String)

        ss.Rest = ("q", "w", "e")

        System.Console.WriteLine(ss.I9)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[w]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DefiniteAssignment011()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        if (1.ToString() = 2.ToString())
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ss.I7 = "q"
            ss.I8 = "q"

            System.Console.WriteLine(ss)

        elseif (1.ToString() = 3.ToString())
    
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ss.I7 = "q"
            ' ss.I8 = "q"

            System.Console.WriteLine(ss)

        else 
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ' ss.I7 = "q"
            ss.I8 = "q"

            System.Console.WriteLine(ss) ' should fail
        end if
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, , q)]]>)

            verifier.VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss").WithArguments("ss").WithLocation(44, 38),
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss").WithArguments("ss").WithLocation(65, 38)
            )

        End Sub

        <Fact>
        Public Sub DefiniteAssignment012()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
       if (1.ToString() = 2.ToString())
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ss.I7 = "q"
            ss.I8 = "q"

            System.Console.WriteLine(ss)

        else if (1.ToString() = 3.ToString())
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ss.I7 = "q"
            ' ss.I8 = "q"

            System.Console.WriteLine(ss) ' should fail1

        else 
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ' ss.I7 = "q"
            ss.I8 = "q"

            System.Console.WriteLine(ss) ' should fail2
        end if
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, , q)]]>)

            verifier.VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss").WithArguments("ss").WithLocation(43, 38),
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss").WithArguments("ss").WithLocation(64, 38)
                )

        End Sub

        <Fact>
        Public Sub DefiniteAssignment013()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.I2 = "q"
            ss.I3 = "q"
            ss.I4 = "q"
            ss.I5 = "q"
            ss.I6 = "q"
            ss.I7 = "q"

            ss.Item1 = "q"
            ss.Item2 = "q"
            ss.Item3 = "q"
            ss.Item4 = "q"
            ss.Item5 = "q"
            ss.Item6 = "q"
            ss.Item7 = "q"

            System.Console.WriteLine(ss.Rest)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[()]]>)

            verifier.VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "ss.Rest").WithArguments("Rest").WithLocation(28, 38)
            )

        End Sub

        <Fact>
        Public Sub DefiniteAssignment014()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
            Dim ss as (I1 as string, 
                        I2  As String, 
                        I3  As String, 
                        I4  As String, 
                        I5  As String, 
                        I6  As String, 
                        I7  As String, 
                        I8  As String)

            ss.I1 = "q"
            ss.Item2 = "aa"

            System.Console.WriteLine(ss.Item1)
            System.Console.WriteLine(ss.I2)

            System.Console.WriteLine(ss.I3)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, expectedOutput:=<![CDATA[q
aa]]>)

            verifier.VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "ss.I3").WithArguments("I3").WithLocation(18, 38)
            )

        End Sub

        <Fact>
        Public Sub DefiniteAssignment015()

            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
        Sub Main()
            Dim v = Test().Result
        end sub

        async Function Test() as Task(of long)
            Dim v1 as (a as Integer, b as Integer)
            Dim v2 as (x as Byte, y as Integer)

            v1.a = 5

            v2.x = 5   ' no need to persist across await since it is unused after it.
            System.Console.WriteLine(v2.Item1)

            await Task.Yield()

            ' this is assigned and persisted across await
            return v1.Item1            
        end Function
End Module

            </file>
    </compilation>, useLatestFramework:=True, additionalRefs:=s_valueTupleRefs, expectedOutput:="5")

            ' NOTE: !!! There should be NO IL local for  " v1 as (Long, Integer)" , it should be captured instead
            ' NOTE: !!! There should be an IL local for  " v2 as (Byte, Integer)" , it should not be captured 
            verifier.VerifyIL("Module1.VB$StateMachine_1_Test.MoveNext()", <![CDATA[
{
  // Code size      214 (0xd6)
  .maxstack  3
  .locals init (Long V_0,
                Integer V_1,
                System.ValueTuple(Of Byte, Integer) V_2, //v2
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_3,
                System.Runtime.CompilerServices.YieldAwaitable V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Test.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Module1.VB$StateMachine_1_Test.$VB$ResumableLocal_v1$0 As (a As Integer, b As Integer)"
    IL_0010:  ldc.i4.5
    IL_0011:  stfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
    IL_0016:  ldloca.s   V_2
    IL_0018:  ldc.i4.5
    IL_0019:  stfld      "System.ValueTuple(Of Byte, Integer).Item1 As Byte"
    IL_001e:  ldloc.2
    IL_001f:  ldfld      "System.ValueTuple(Of Byte, Integer).Item1 As Byte"
    IL_0024:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0029:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_002e:  stloc.s    V_4
    IL_0030:  ldloca.s   V_4
    IL_0032:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0037:  stloc.3
    IL_0038:  ldloca.s   V_3
    IL_003a:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.1
    IL_0045:  stfld      "Module1.VB$StateMachine_1_Test.$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.3
    IL_004c:  stfld      "Module1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Module1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Long)"
    IL_0057:  ldloca.s   V_3
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Long).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Module1.VB$StateMachine_1_Test)(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Module1.VB$StateMachine_1_Test)"
    IL_005f:  leave.s    IL_00d5
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.1
    IL_0065:  stfld      "Module1.VB$StateMachine_1_Test.$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Module1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0070:  stloc.3
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Module1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0077:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_007d:  ldloca.s   V_3
    IL_007f:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_0084:  ldloca.s   V_3
    IL_0086:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_008c:  ldarg.0
    IL_008d:  ldflda     "Module1.VB$StateMachine_1_Test.$VB$ResumableLocal_v1$0 As (a As Integer, b As Integer)"
    IL_0092:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
    IL_0097:  conv.i8
    IL_0098:  stloc.0
    IL_0099:  leave.s    IL_00bf
  }
  catch System.Exception
  {
    IL_009b:  dup
    IL_009c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a1:  stloc.s    V_5
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      "Module1.VB$StateMachine_1_Test.$State As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     "Module1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Long)"
    IL_00b1:  ldloc.s    V_5
    IL_00b3:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Long).SetException(System.Exception)"
    IL_00b8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00bd:  leave.s    IL_00d5
  }
  IL_00bf:  ldarg.0
  IL_00c0:  ldc.i4.s   -2
  IL_00c2:  dup
  IL_00c3:  stloc.1
  IL_00c4:  stfld      "Module1.VB$StateMachine_1_Test.$State As Integer"
  IL_00c9:  ldarg.0
  IL_00ca:  ldflda     "Module1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Long)"
  IL_00cf:  ldloc.0
  IL_00d0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Long).SetResult(Long)"
  IL_00d5:  ret
}
]]>)

        End Sub

        <Fact>
        <WorkItem(13661, "https://github.com/dotnet/roslyn/issues/13661")>
        Public Sub LongTupleWithPartialNames_Bug13661()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module C
    Sub Main()
        Dim t = (A:=1, 2, C:=3, D:=4, E:=5, F:=6, G:=7, 8, I:=9)
        System.Console.Write($"{t.I}")
    End Sub
End Module

    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef},
                options:=TestOptions.DebugExe, expectedOutput:="9",
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim t = nodes.OfType(Of VariableDeclaratorSyntax)().Single().Names(0)
                        Dim xSymbol = DirectCast(model.GetDeclaredSymbol(t), LocalSymbol).Type

                        AssertEx.SetEqual(xSymbol.GetMembers().OfType(Of FieldSymbol)().Select(Function(f) f.Name),
                            "A", "C", "D", "E", "F", "G", "I", "Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9", "Rest")
                    End Sub)
            ' No assert hit

        End Sub

        <Fact>
        Public Sub UnifyUnderlyingWithTuple_08()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1

    Sub Main()

        Dim x = (1, 3)
        Dim s As String = x
        System.Console.WriteLine(s)
        System.Console.WriteLine(CType(x, Long))

        Dim y As (Integer, String) = New KeyValuePair(Of Integer, String)(2, "4")
        System.Console.WriteLine(y)
        System.Console.WriteLine(CType("5", ValueTuple(Of String, String)))

        System.Console.WriteLine(+x)
        System.Console.WriteLine(-x)
        System.Console.WriteLine(Not x)
        System.Console.WriteLine(If(x, True, False))
        System.Console.WriteLine(If(Not x, True, False))

        System.Console.WriteLine(x + 1)
        System.Console.WriteLine(x - 1)
        System.Console.WriteLine(x * 3)
        System.Console.WriteLine(x / 2)
        System.Console.WriteLine(x \ 2)
        System.Console.WriteLine(x Mod 3)
        System.Console.WriteLine(x & 3)
        System.Console.WriteLine(x And 3)
        System.Console.WriteLine(x Or 15)
        System.Console.WriteLine(x Xor 3)
        System.Console.WriteLine(x Like 15)
        System.Console.WriteLine(x ^ 4)
        System.Console.WriteLine(x << 1)
        System.Console.WriteLine(x >> 1)
        System.Console.WriteLine(x = 1)
        System.Console.WriteLine(x <> 1)
        System.Console.WriteLine(x > 1)
        System.Console.WriteLine(x < 1)
        System.Console.WriteLine(x >= 1)
        System.Console.WriteLine(x <= 1)
    End Sub
End Module
]]></file>
</compilation>

            Dim tuple =
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System

    Public Structure ValueTuple(Of T1, T2)

        Public Item1 As T1
        Public Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            Me.Item1 = item1
            Me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function

        Public Shared Widening Operator CType(arg As ValueTuple(Of T1, T2)) As String
            Return arg.ToString()
        End Operator

        Public Shared Narrowing Operator CType(arg As ValueTuple(Of T1, T2)) As Long
            Return CLng(CObj(arg.Item1) + CObj(arg.Item2))
        End Operator

        Public Shared Widening Operator CType(arg As System.Collections.Generic.KeyValuePair(Of T1, T2)) As ValueTuple(Of T1, T2)
            Return New ValueTuple(Of T1, T2)(arg.Key, arg.Value)
        End Operator

        Public Shared Narrowing Operator CType(arg As String) As ValueTuple(Of T1, T2)
            Return New ValueTuple(Of T1, T2)(CType(CObj(arg), T1), CType(CObj(arg), T2))
        End Operator

        Public Shared Operator +(arg As ValueTuple(Of T1, T2)) As ValueTuple(Of T1, T2)
            Return arg
        End Operator

        Public Shared Operator -(arg As ValueTuple(Of T1, T2)) As Long
            Return -CType(arg, Long)
        End Operator

        Public Shared Operator Not(arg As ValueTuple(Of T1, T2)) As Boolean
            Return CType(arg, Long) = 0
        End Operator

        Public Shared Operator IsTrue(arg As ValueTuple(Of T1, T2)) As Boolean
            Return CType(arg, Long) <> 0
        End Operator

        Public Shared Operator IsFalse(arg As ValueTuple(Of T1, T2)) As Boolean
            Return CType(arg, Long) = 0
        End Operator

        Public Shared Operator +(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) + arg2
        End Operator

        Public Shared Operator -(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) - arg2
        End Operator

        Public Shared Operator *(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) * arg2
        End Operator

        Public Shared Operator /(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) / arg2
        End Operator

        Public Shared Operator \(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) \ arg2
        End Operator

        Public Shared Operator Mod(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) Mod arg2
        End Operator

        Public Shared Operator &(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) & arg2
        End Operator

        Public Shared Operator And(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) And arg2
        End Operator

        Public Shared Operator Or(arg1 As ValueTuple(Of T1, T2), arg2 As Long) As Long
            Return CType(arg1, Long) Or arg2
        End Operator

        Public Shared Operator Xor(arg1 As ValueTuple(Of T1, T2), arg2 As Long) As Long
            Return CType(arg1, Long) Xor arg2
        End Operator

        Public Shared Operator Like(arg1 As ValueTuple(Of T1, T2), arg2 As Long) As Long
            Return CType(arg1, Long) Or arg2
        End Operator

        Public Shared Operator ^(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) ^ arg2
        End Operator

        Public Shared Operator <<(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) << arg2
        End Operator

        Public Shared Operator >>(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Long
            Return CType(arg1, Long) >> arg2
        End Operator

        Public Shared Operator =(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Boolean
            Return CType(arg1, Long) = arg2
        End Operator

        Public Shared Operator <>(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Boolean
            Return CType(arg1, Long) <> arg2
        End Operator

        Public Shared Operator >(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Boolean
            Return CType(arg1, Long) > arg2
        End Operator

        Public Shared Operator <(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Boolean
            Return CType(arg1, Long) < arg2
        End Operator

        Public Shared Operator >=(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Boolean
            Return CType(arg1, Long) >= arg2
        End Operator

        Public Shared Operator <=(arg1 As ValueTuple(Of T1, T2), arg2 As Integer) As Boolean
            Return CType(arg1, Long) <= arg2
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return 0
        End Function
    End Structure
End Namespace
]]></file>
</compilation>

            Dim expectedOutput =
"{1, 3}
4
{2, 4}
{5, 5}
{1, 3}
-4
False
True
False
5
3
12
2
2
1
43
0
15
7
15
256
8
2
False
True
True
False
True
False
"
            Dim [lib] = CreateCompilationWithMscorlibAndVBRuntime(tuple, options:=TestOptions.ReleaseDll)
            [lib].VerifyEmitDiagnostics()

            Dim consumer1 = CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].ToMetadataReference()})
            CompileAndVerify(consumer1, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Dim consumer2 = CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].EmitToImageReference()})
            CompileAndVerify(consumer2, expectedOutput:=expectedOutput).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub UnifyUnderlyingWithTuple_12()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1

    Sub Main()

        Dim x? = (1, 3)
        Dim s As String = x
        System.Console.WriteLine(s)
        System.Console.WriteLine(CType(x, Long))

        Dim y As (Integer, String)? = New KeyValuePair(Of Integer, String)(2, "4")
        System.Console.WriteLine(y)
        System.Console.WriteLine(CType("5", ValueTuple(Of String, String)))

        System.Console.WriteLine(+x)
        System.Console.WriteLine(-x)
        System.Console.WriteLine(Not x)
        System.Console.WriteLine(If(x, True, False))
        System.Console.WriteLine(If(Not x, True, False))

        System.Console.WriteLine(x + 1)
        System.Console.WriteLine(x - 1)
        System.Console.WriteLine(x * 3)
        System.Console.WriteLine(x / 2)
        System.Console.WriteLine(x \ 2)
        System.Console.WriteLine(x Mod 3)
        System.Console.WriteLine(x & 3)
        System.Console.WriteLine(x And 3)
        System.Console.WriteLine(x Or 15)
        System.Console.WriteLine(x Xor 3)
        System.Console.WriteLine(x Like 15)
        System.Console.WriteLine(x ^ 4)
        System.Console.WriteLine(x << 1)
        System.Console.WriteLine(x >> 1)
        System.Console.WriteLine(x = 1)
        System.Console.WriteLine(x <> 1)
        System.Console.WriteLine(x > 1)
        System.Console.WriteLine(x < 1)
        System.Console.WriteLine(x >= 1)
        System.Console.WriteLine(x <= 1)
    End Sub
End Module
]]></file>
</compilation>

            Dim tuple =
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System

    Public Structure ValueTuple(Of T1, T2)

        Public Item1 As T1
        Public Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            Me.Item1 = item1
            Me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function

        Public Shared Widening Operator CType(arg As ValueTuple(Of T1, T2)?) As String
            Return arg.ToString()
        End Operator

        Public Shared Narrowing Operator CType(arg As ValueTuple(Of T1, T2)?) As Long
            Return CLng(CObj(arg.Value.Item1) + CObj(arg.Value.Item2))
        End Operator

        Public Shared Widening Operator CType(arg As System.Collections.Generic.KeyValuePair(Of T1, T2)) As ValueTuple(Of T1, T2)?
            Return New ValueTuple(Of T1, T2)(arg.Key, arg.Value)
        End Operator

        Public Shared Narrowing Operator CType(arg As String) As ValueTuple(Of T1, T2)?
            Return New ValueTuple(Of T1, T2)(CType(CObj(arg), T1), CType(CObj(arg), T2))
        End Operator

        Public Shared Operator +(arg As ValueTuple(Of T1, T2)?) As ValueTuple(Of T1, T2)?
            Return arg
        End Operator

        Public Shared Operator -(arg As ValueTuple(Of T1, T2)?) As Long
            Return -CType(arg, Long)
        End Operator

        Public Shared Operator Not(arg As ValueTuple(Of T1, T2)?) As Boolean
            Return CType(arg, Long) = 0
        End Operator

        Public Shared Operator IsTrue(arg As ValueTuple(Of T1, T2)?) As Boolean
            Return CType(arg, Long) <> 0
        End Operator

        Public Shared Operator IsFalse(arg As ValueTuple(Of T1, T2)?) As Boolean
            Return CType(arg, Long) = 0
        End Operator

        Public Shared Operator +(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) + arg2
        End Operator

        Public Shared Operator -(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) - arg2
        End Operator

        Public Shared Operator *(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) * arg2
        End Operator

        Public Shared Operator /(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) / arg2
        End Operator

        Public Shared Operator \(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) \ arg2
        End Operator

        Public Shared Operator Mod(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) Mod arg2
        End Operator

        Public Shared Operator &(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) & arg2
        End Operator

        Public Shared Operator And(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) And arg2
        End Operator

        Public Shared Operator Or(arg1 As ValueTuple(Of T1, T2)?, arg2 As Long) As Long
            Return CType(arg1, Long) Or arg2
        End Operator

        Public Shared Operator Xor(arg1 As ValueTuple(Of T1, T2)?, arg2 As Long) As Long
            Return CType(arg1, Long) Xor arg2
        End Operator

        Public Shared Operator Like(arg1 As ValueTuple(Of T1, T2)?, arg2 As Long) As Long
            Return CType(arg1, Long) Or arg2
        End Operator

        Public Shared Operator ^(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) ^ arg2
        End Operator

        Public Shared Operator <<(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) << arg2
        End Operator

        Public Shared Operator >>(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Long
            Return CType(arg1, Long) >> arg2
        End Operator

        Public Shared Operator =(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Boolean
            Return CType(arg1, Long) = arg2
        End Operator

        Public Shared Operator <>(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Boolean
            Return CType(arg1, Long) <> arg2
        End Operator

        Public Shared Operator >(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Boolean
            Return CType(arg1, Long) > arg2
        End Operator

        Public Shared Operator <(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Boolean
            Return CType(arg1, Long) < arg2
        End Operator

        Public Shared Operator >=(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Boolean
            Return CType(arg1, Long) >= arg2
        End Operator

        Public Shared Operator <=(arg1 As ValueTuple(Of T1, T2)?, arg2 As Integer) As Boolean
            Return CType(arg1, Long) <= arg2
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return 0
        End Function
    End Structure
End Namespace
]]></file>
</compilation>

            Dim expectedOutput =
"{1, 3}
4
{2, 4}
{5, 5}
{1, 3}
-4
False
True
False
5
3
12
2
2
1
43
0
15
7
15
256
8
2
False
True
True
False
True
False
"
            Dim [lib] = CreateCompilationWithMscorlibAndVBRuntime(tuple, options:=TestOptions.ReleaseDll)
            [lib].VerifyEmitDiagnostics()

            Dim consumer1 = CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].ToMetadataReference()})
            CompileAndVerify(consumer1, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Dim consumer2 = CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].EmitToImageReference()})
            CompileAndVerify(consumer2, expectedOutput:=expectedOutput).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleConversion01()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb"><![CDATA[

Module C
    Sub Main()
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
    End Sub
End Module

]]></file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                                   ~~~~
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                         ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                               ~~~~
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                                   ~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleConversion01_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb"><![CDATA[
Option Strict On
Module C
    Sub Main()
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
    End Sub
End Module

]]></file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30512: Option Strict On disallows implicit conversions from '(c As Long, d As Long)' to '(a As Integer, b As Integer)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                                   ~~~~
BC30512: Option Strict On disallows implicit conversions from '(c As Integer, d As Integer)' to '(a As Short, b As Short)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                         ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                               ~~~~
BC30512: Option Strict On disallows implicit conversions from '(c As Long, d As Long)' to '(a As Integer, b As Integer)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                                   ~~~~~~~
</errors>)

        End Sub

        <Fact>
        <WorkItem(11288, "https://github.com/dotnet/roslyn/issues/11288")>
        Public Sub TupleConversion02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x4 As (a As Integer, b As Integer) = DirectCast((1, Nothing, 2), (c As Long, d As Long))
    End Sub
End Module
        <%= s_trivial2uple %><%= s_trivial3uple %>
    </file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC30311: Value of type '(Integer, Object, Integer)' cannot be converted to '(c As Long, d As Long)'.
        Dim x4 As (a As Integer, b As Integer) = DirectCast((1, Nothing, 2), (c As Long, d As Long))
                                                            ~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleConvertedType01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = (e:=1, f:="hello")
    End Sub
End Module

        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType01_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Option Strict On
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = (e:=1, f:="hello")
    End Sub
End Module

        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType01insource()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = DirectCast((e:=1, f:="hello"), (c As Short, d As String)?)
        Dim y As Short? = DirectCast(11, Short?)
    End Sub
End Module

        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim l11 = nodes.OfType(Of LiteralExpressionSyntax)().ElementAt(2)

            Assert.Equal("11", l11.ToString())
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).Type.ToTestDisplayString())
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(l11).Kind)

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (c As System.Int16, d As System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Assert.Equal("System.Nullable(Of (c As System.Int16, d As System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (c As System.Int16, d As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType01insourceImplicit()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = (1, "hello")
    End Sub
End Module

        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(1, ""hello"")", node.ToString())
            Dim typeInfo As TypeInfo = model.GetTypeInfo(node)
            Assert.Equal("(System.Int32, System.String)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            CompileAndVerify(comp)

        End Sub

        <Fact>
        Public Sub TupleConvertedType02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = (e:=1, f:="hello")
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Dim typeInfo As TypeInfo = model.GetTypeInfo(node)
            Assert.Equal("(e As System.Int32, f As System.String)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource00()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = DirectCast((e:=1, f:="hello"), (c As Short, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource00_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Option Strict On
Module C
    Sub Main()
        Dim x As (a As Short, b As String)? = DirectCast((e:=1, f:="hello"), (c As Short, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x = (e:=1, f:="hello")
        Dim x1 As (a As Object, b As String) = DirectCast((x), (c As Long, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single().Parent

            Assert.Equal("DirectCast((x), (c As Long, d As String))", node.ToString())
            Assert.Equal("(c As System.Int64, d As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(a As System.Object, b As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single()
            Assert.Equal("(x)", x.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).Type.ToTestDisplayString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(x).Kind)

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource01_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Option Strict On
Module C
    Sub Main()
        Dim x = (e:=1, f:="hello")
        Dim x1 As (a As Object, b As String) = DirectCast((x), (c As Long, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single().Parent

            Assert.Equal("DirectCast((x), (c As Long, d As String))", node.ToString())
            Assert.Equal("(c As System.Int64, d As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(a As System.Object, b As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single()
            Assert.Equal("(x)", x.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).Type.ToTestDisplayString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(x).Kind)

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x = (e:=1, f:="hello")
        Dim x1 As (a As Object, b As String)? = DirectCast((x), (c As Long, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single().Parent

            Assert.Equal("DirectCast((x), (c As Long, d As String))", node.ToString())
            Assert.Equal("(c As System.Int64, d As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Object, b As System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single()
            Assert.Equal("(x)", x.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).Type.ToTestDisplayString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(x).Kind)

        End Sub

        <Fact>
        Public Sub TupleConvertedType03()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Integer, b As String)? = (e:=1, f:="hello")
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int32, b As System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int32, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType03insource()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Integer, b As String)? = DirectCast((e:=1, f:="hello"), (c As Integer, d As String)?)
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (c As System.Int32, d As System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node).Kind)

            Assert.Equal("DirectCast((e:=1, f:=""hello""), (c As Integer, d As String)?)", node.Parent.ToString())
            Assert.Equal("System.Nullable(Of (c As System.Int32, d As System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (c As System.Int32, d As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int32, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType04()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Integer, b As String)? = DirectCast((e:=1, f:="hello"), (c As Integer, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int32, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int32, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int32, b As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullable, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int32, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType05()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Integer, b As String) = (e:=1, f:="hello")
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(a As System.Int32, b As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int32, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType05insource()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Integer, b As String) = DirectCast((e:=1, f:="hello"), (c As Integer, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int32, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int32, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType05insource_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Option Strict On
Module C
    Sub Main()
        Dim x As (a As Integer, b As String) = DirectCast((e:=1, f:="hello"), (c As Integer, d As String))
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int32, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int32, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType06()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String) = (e:=1, f:="hello")
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(a As System.Int16, b As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType06insource()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String) = DirectCast((e:=1, f:="hello"), (c As Short, d As String))
        Dim y As Short = DirectCast(11, short)
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim l11 = nodes.OfType(Of LiteralExpressionSyntax)().ElementAt(2)

            Assert.Equal("11", l11.ToString())
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).Type.ToTestDisplayString())
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(l11).Kind)

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=""hello"")", node.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeNull01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String) = (e:=1, f:=Nothing)
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(a As System.Int16, b As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeNull01insource()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Module C
    Sub Main()
        Dim x As (a As Short, b As String) = DirectCast((e:=1, f:=Nothing), (c As Short, d As String))
        Dim y As String = DirectCast(Nothing, String)
    End Sub
End Module
        <%= s_trivial2uple %>
    </file>
</compilation>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim lnothing = nodes.OfType(Of LiteralExpressionSyntax)().ElementAt(2)

            Assert.Equal("Nothing", lnothing.ToString())
            Assert.Null(model.GetTypeInfo(lnothing).Type)
            Assert.Equal("System.Object", model.GetTypeInfo(lnothing).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNothingLiteral, model.GetConversion(lnothing).Kind)

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC01()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
        System.Console.Write(x.ToString())
    End Sub

    Class C1
        Public Dim s As String
        Public Sub New(ByVal arg As String)
            s = arg + "1"
        End Sub
        Public Shared Narrowing Operator CType(ByVal arg As C1) As String
            Return arg.s
        End Operator
    End Class
End Class
        <%= s_trivial2uple %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=New C1(""qq""))", node.ToString())
            Assert.Equal("(e As System.Int32, f As C.C1)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(a As System.Int16, b As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.NarrowingTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

            CompileAndVerify(comp, expectedOutput:="{1, qq1}")

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC01_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Option Strict On
Class C
    Shared Sub Main()
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
        System.Console.Write(x.ToString())
    End Sub

    Class C1
        Public Dim s As String
        Public Sub New(ByVal arg As String)
            s = arg + "1"
        End Sub
        Public Shared Narrowing Operator CType(ByVal arg As C1) As String
            Return arg.s
        End Operator
    End Class
End Class
        <%= s_trivial2uple %>
    </file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'e' is ignored because a different name is specified by the target type '(a As Short, b As String)'.
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
                                              ~~~~
BC41009: The tuple element name 'f' is ignored because a different name is specified by the target type '(a As Short, b As String)'.
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
                                                    ~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'C.C1' to 'String'.
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
                                                       ~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC01insource()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As (a As Short, b As String) = DirectCast((e:=1, f:=New C1("qq")), (c As Short, d As String))
        System.Console.Write(x.ToString())
    End Sub

    Class C1
        Public Dim s As String
        Public Sub New(ByVal arg As String)
            s = arg + "1"
        End Sub
        Public Shared Narrowing Operator CType(ByVal arg As C1) As String
            Return arg.s
        End Operator
    End Class
End Class
        <%= s_trivial2uple %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e:=1, f:=New C1(""qq""))", node.ToString())
            Assert.Equal("(e As System.Int32, f As C.C1)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.NarrowingTuple, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

            CompileAndVerify(comp, expectedOutput:="{1, qq1}")

        End Sub

        <Fact>
        <WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")>
        Public Sub TupleConvertedTypeUDC02()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As C1 = (1, "qq")
        System.Console.Write(x.ToString())
    End Sub

    Class C1
        Public Dim val As (Byte, String)
        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub
        Public Shared Narrowing Operator CType(ByVal arg As (Byte, String)) As C1
            Return New C1(arg)
        End Operator
        Public Overrides Function ToString() As String
            Return val.ToString()
        End Function
    End Class
End Class
        <%= s_trivial2uple %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(1, ""qq"")", node.ToString())
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("C.C1", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(node).Kind)

            CompileAndVerify(comp, expectedOutput:="{1, qq}")

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC03()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim x As C1 = ("1", "qq")
        System.Console.Write(x.ToString())
    End Sub

    Class C1
        Public Dim val As (Byte, String)
        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub
        Public Shared Narrowing Operator CType(ByVal arg As (Byte, String)) As C1
            Return New C1(arg)
        End Operator
        Public Overrides Function ToString() As String
            Return val.ToString()
        End Function
    End Class
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
</errors>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(""1"", ""qq"")", node.ToString())
            Assert.Equal("(System.String, System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("C.C1", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(node).Kind)

            CompileAndVerify(comp, expectedOutput:="(1, qq)")

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC03_StrictOn()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Option Strict On
Class C
    Shared Sub Main()
        Dim x As C1 = ("1", "qq")
        System.Console.Write(x.ToString())
    End Sub

    Class C1
        Public Dim val As (Byte, String)
        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub
        Public Shared Narrowing Operator CType(ByVal arg As (Byte, String)) As C1
            Return New C1(arg)
        End Operator
        Public Overrides Function ToString() As String
            Return val.ToString()
        End Function
    End Class
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30512: Option Strict On disallows implicit conversions from '(String, String)' to 'C.C1'.
        Dim x As C1 = ("1", "qq")
                      ~~~~~~~~~~~
</errors>)

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(""1"", ""qq"")", node.ToString())
            Assert.Equal("(System.String, System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("C.C1", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(node).Kind)

        End Sub

        <Fact>
        <WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")>
        Public Sub TupleConvertedTypeUDC04()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim x As C1 = (1, "qq")
        System.Console.Write(x.ToString())
    End Sub

    Public Class C1
        Public Dim val As (Byte, String)

        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub

        Public Overrides Function ToString() As String
            Return val.ToString()
        End Function
    End Class
End Class

Namespace System
   Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function

        Public Shared Narrowing Operator CType(ByVal arg As (T1, T2)) As C.C1
            Return New C.C1((CType(DirectCast(DirectCast(arg.Item1, Object), Integer), Byte),
                DirectCast(DirectCast(arg.Item2, Object), String)))
        End Operator
    End Structure
End Namespace
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics()

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().First()

            Assert.Equal("(1, ""qq"")", node.ToString())
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("C.C1", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(node).Kind)

            CompileAndVerify(comp, expectedOutput:="{1, qq}")

        End Sub

        <Fact>
        <WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")>
        Public Sub TupleConvertedTypeUDC05()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim x As C1 = (1, "qq")
        System.Console.Write(x.ToString())
    End Sub

    Public Class C1
        Public Dim val As (Byte, String)

        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub

        Public Shared Narrowing Operator CType(ByVal arg As (Byte, String)) As C1
            System.Console.Write("C1")
            Return New C1(arg)
        End Operator

        Public Overrides Function ToString() As String
            Return val.ToString()
        End Function
    End Class
End Class

Namespace System
   Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function

        Public Shared Narrowing Operator CType(ByVal arg As (T1, T2)) As C.C1
            System.Console.Write("VT ")
            Return New C.C1((CType(DirectCast(DirectCast(arg.Item1, Object), Integer), Byte),
                DirectCast(DirectCast(arg.Item2, Object), String)))
        End Operator
    End Structure
End Namespace
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics()

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().First()

            Assert.Equal("(1, ""qq"")", node.ToString())
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("C.C1", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(node).Kind)

            CompileAndVerify(comp, expectedOutput:="VT {1, qq}")

        End Sub

        <Fact>
        <WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")>
        Public Sub TupleConvertedTypeUDC06()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim x As C1 = (1, Nothing)
        System.Console.Write(x.ToString())
    End Sub

    Public Class C1
        Public Dim val As (Byte, String)

        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub

        Public Shared Narrowing Operator CType(ByVal arg As (Byte, String)) As C1
            System.Console.Write("C1")
            Return New C1(arg)
        End Operator

        Public Overrides Function ToString() As String
            Return val.ToString()
        End Function
    End Class
End Class

Namespace System
   Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return "{" + Item1?.ToString() + ", " + Item2?.ToString() + "}"
        End Function

        Public Shared Narrowing Operator CType(ByVal arg As (T1, T2)) As C.C1
            System.Console.Write("VT ")
            Return New C.C1((CType(DirectCast(DirectCast(arg.Item1, Object), Integer), Byte),
                DirectCast(DirectCast(arg.Item2, Object), String)))
        End Operator
    End Structure
End Namespace
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics()

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().First()

            Assert.Equal("(1, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("C.C1", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(node).Kind)

            CompileAndVerify(comp, expectedOutput:="VT {1, }")

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC07_StrictOff_Narrowing()

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim x As C1 = M1()
        System.Console.Write(x.ToString())
    End Sub

    Shared Function M1() As (Integer, String)
        Return (1, "qq")
    End Function

    Public Class C1
        Public Dim val As (Byte, String)

        Public Sub New(ByVal arg As (Byte, String))
            val = arg
        End Sub

        Public Shared Narrowing Operator CType(ByVal arg As (Byte, String)) As C1
            System.Console.Write("C1 ")
            Return New C1(arg)
        End Operator
    End Class
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            CompileAndVerify(comp, expectedOutput:="C1 C+C1")

        End Sub

    End Class

End Namespace


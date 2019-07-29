' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

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

        ReadOnly s_tupleattributes As String = "
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
        Public Sub TupleNamesInArrayInAttribute()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
<My(New (String, bob As String)() { })>
Public Class MyAttribute
    Inherits System.Attribute

    Public Sub New(x As (alice As String, String)())
    End Sub
End Class
    ]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30045: Attribute constructor has a parameter of type '(alice As String, String)()', which is not an integral, floating-point or Enum type or one of Object, Char, String, Boolean, System.Type or 1-dimensional array of these types.
<My(New (String, bob As String)() { })>
 ~~
                                        ]]></errors>)

        End Sub

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
        Public Sub TupleTypeBindingTypeChar()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
option strict on

Imports System
Module C

    Sub Main()
        Dim t as (A%, B$) = Nothing
        console.writeline(t.GetType())
    End Sub
End Module

    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.ValueTuple`2[System.Int32,System.String]
            ]]>)

            verifier.VerifyDiagnostics()

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (System.ValueTuple(Of Integer, String) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.ValueTuple(Of Integer, String)"
  IL_0008:  ldloc.0
  IL_0009:  box        "System.ValueTuple(Of Integer, String)"
  IL_000e:  call       "Function Object.GetType() As System.Type"
  IL_0013:  call       "Sub System.Console.WriteLine(Object)"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleTypeBindingTypeChar1()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
option strict off

Imports System
Module C

    Sub Main()
        Dim t as (A%, B$) = Nothing
        console.writeline(t.GetType())
    End Sub
End Module

    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.ValueTuple`2[System.Int32,System.String]
            ]]>)

            verifier.VerifyDiagnostics()

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (System.ValueTuple(Of Integer, String) V_0) //t
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "System.ValueTuple(Of Integer, String)"
  IL_0008:  ldloc.0
  IL_0009:  box        "System.ValueTuple(Of Integer, String)"
  IL_000e:  call       "Function Object.GetType() As System.Type"
  IL_0013:  call       "Sub System.Console.WriteLine(Object)"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleTypeBindingTypeCharErr()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module C

    Sub Main()
        Dim t as (A% As String, B$ As String, C As String$) = nothing
        console.writeline(t.A.Length)  'A should not take the type from % in this case

        Dim t1 as (String$, String%) = nothing
        console.writeline(t1.GetType())

        Dim B = 1
        
        Dim t2 = (A% := "qq", B$) 
        console.writeline(t2.A.Length) 'A should not take the type from % in this case

        Dim t3 As (V1(), V2%()) = Nothing
        console.writeline(t3.Item1.Length) 
    End Sub

   Async Sub T()
        Dim t4 as (Integer% As String, Await As String, Function$) = nothing
        console.writeline(t4.Integer.Length)  
        console.writeline(t4.Await.Length)  
        console.writeline(t4.Function.Length)  

        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
        console.writeline(t4.Function.Length)  

    End Sub

    class V2
    end class
End Module

]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
        Dim t as (A% As String, B$ As String, C As String$) = nothing
                  ~~
BC30302: Type character '$' cannot be used in a declaration with an explicit type.
        Dim t as (A% As String, B$ As String, C As String$) = nothing
                                ~~
BC30468: Type declaration characters are not valid in this context.
        Dim t as (A% As String, B$ As String, C As String$) = nothing
                                                   ~~~~~~~
BC37262: Tuple element names must be unique.
        Dim t1 as (String$, String%) = nothing
                            ~~~~~~~
BC37270: Type characters cannot be used in tuple literals.
        Dim t2 = (A% := "qq", B$) 
                  ~~
BC30277: Type character '$' does not match declared data type 'Integer'.
        Dim t2 = (A% := "qq", B$) 
                              ~~
BC30002: Type 'V1' is not defined.
        Dim t3 As (V1(), V2%()) = Nothing
                   ~~
BC32017: Comma, ')', or a valid expression continuation expected.
        Dim t3 As (V1(), V2%()) = Nothing
                            ~
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
   Async Sub T()
             ~
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
        Dim t4 as (Integer% As String, Await As String, Function$) = nothing
                   ~~~~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim t4 as (Integer% As String, Await As String, Function$) = nothing
                                       ~~~~~
BC30180: Keyword does not name a type.
        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
                   ~
BC30180: Keyword does not name a type.
        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
                                       ~
BC30002: Type 'Junk2' is not defined.
        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
                                                     ~~~~~
BC32017: Comma, ')', or a valid expression continuation expected.
        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
                                                           ~~~~~~~~
BC30002: Type 'Junk4' is not defined.
        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
                                                                     ~~~~~
BC32017: Comma, ')', or a valid expression continuation expected.
        Dim t5 as (Function As String, Sub, Junk1 As Junk2 Recovery, Junk4 Junk5) = nothing
                                                                           ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub TupleTypeBindingNoTuple()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.String
System.String
System.String
System.String
            ]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType006err()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Action
VB$AnonymousDelegate_0
            ]]>)
        End Sub

        <Fact()>
        Public Sub TupleDefaultType007err()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, additionalRefs:=s_valueTupleRefs)

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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
            ]]>, references:=s_valueTupleRefs)

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
            ]]>, references:=s_valueTupleRefs)

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
            ]]>, references:=s_valueTupleRefs)

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

        TestArray()
        TestNullable()
    End Sub

    Sub TestArray()
        Dim t = New (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer,
                    a5 as integer, a6 as integer, a7 as integer, a8 as integer,
                    a9 as integer, a10 as Integer, a11 as Integer, a12 as Integer,
                    a13 as integer, a14 as integer, a15 as integer, a16 as integer,
                    a17 as integer, a18 as integer)() {Nothing}

        t(0).a17 = 42
        t(0).a12 = t(0).a17
        console.writeline(t(0).a12)
    End Sub

        Sub TestNullable()
        Dim t as New (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer,
                    a5 as integer, a6 as integer, a7 as integer, a8 as integer,
                    a9 as integer, a10 as Integer, a11 as Integer, a12 as Integer,
                    a13 as integer, a14 as integer, a15 as integer, a16 as integer,
                    a17 as integer, a18 as integer)?

        console.writeline(t.HasValue)
    End Sub

End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[
42
42
False
            ]]>, references:=s_valueTupleRefs)

            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size       74 (0x4a)
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
  IL_003f:  call       "Sub C.TestArray()"
  IL_0044:  call       "Sub C.TestNullable()"
  IL_0049:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TupleNewLongErr()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Module C

    Sub Main()
        Dim t = New (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer,
                    a5 as integer, a6 as integer, a7 as integer, a8 as integer,
                    a9 as integer, a10 as Integer, a11 as Integer, a12 as String,
                    a13 as integer, a14 as integer, a15 as integer, a16 as integer,
                    a17 as integer, a18 as integer)

        console.writeline(t.a12.Length)

        Dim t1 As New (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer,
            a5 as integer, a6 as integer, a7 as integer, a8 as integer,
            a9 as integer, a10 as Integer, a11 as Integer, a12 as String,
            a13 as integer, a14 as integer, a15 as integer, a16 as integer,
            a17 as integer, a18 as integer)

        console.writeline(t1.a12.Length)

    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            ' should not complain about missing constructor
            comp.AssertTheseDiagnostics(
<errors>
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim t = New (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer,
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim t1 As New (a1 as Integer, a2 as Integer, a3 as Integer, a4 as integer,
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub TupleDisallowedWithNew()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Class C

    Dim t1 = New (a1 as Integer, a2 as Integer)()
    Dim t2 As New (a1 as Integer, a2 as Integer)

    Sub M()
        Dim t1 = New (a1 as Integer, a2 as Integer)()
        Dim t2 As New (a1 as Integer, a2 as Integer)
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
    Dim t1 = New (a1 as Integer, a2 as Integer)()
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
    Dim t2 As New (a1 as Integer, a2 as Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim t1 = New (a1 as Integer, a2 as Integer)()
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim t2 As New (a1 as Integer, a2 as Integer)
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub ParseNewTuple()
            Dim comp1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Sub Main()
        Dim x = New (A, A)
        Dim y = New (A, A)()
        Dim z = New (x As Integer, A)
    End Sub
End Class
    ]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)
            comp1.AssertTheseDiagnostics(<errors>
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim x = New (A, A)
                    ~~~~~~
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim y = New (A, A)()
                    ~~~~~~
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim z = New (x As Integer, A)
                    ~~~~~~~~~~~~~~~~~
                                         </errors>)

            Dim comp2 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Public Function Bar() As (Alice As (Alice As Integer, Bob As Integer)(), Bob As Integer)
        ' this is actually ok, since it is an array
        Return (New(Integer, Integer)() {(4, 5)}, 5)
    End Function
End Module
    ]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)
            comp2.AssertNoDiagnostics()

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
            ]]>, references:=s_valueTupleRefs)

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
            ]]>, references:=s_valueTupleRefs)

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
        <WorkItem(18762, "https://github.com/dotnet/roslyn/issues/18762")>
        Public Sub UnnamedTempShouldNotCrashPdbEncoding()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
    Private Async Function DoAllWorkAsync() As Task(Of (FirstValue As String, SecondValue As String))
        Return (Nothing, Nothing)
    End Function
End Module
    </file>
</compilation>,
references:={ValueTupleRef, SystemRuntimeFacadeRef}, useLatestFramework:=True, options:=TestOptions.DebugDll)

        End Sub

        <Fact>
        Public Sub Overloading001()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
BC37271: 'Public Sub Test(x As (a As Integer, b As Integer))' has multiple definitions with identical signatures with different tuple element names, including 'Public Sub Test(x As (c As Integer, d As Integer))'.
    Sub Test(x as (a as integer, b as Integer))
        ~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub Overloading002()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
BC37271: 'Public Sub Test(x As (Integer, Integer))' has multiple definitions with identical signatures with different tuple element names, including 'Public Sub Test(x As (a As Integer, b As Integer))'.
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(42, )
            ]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
            Dim compilation = verifier.Compilation

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of CTypeExpressionSyntax)().Single()

            Assert.Equal("CType((Nothing, 1),(String, Byte))", node.ToString())

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.String, System.Byte)) (Syntax: 'CType((Noth ... ing, Byte))')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String, System.Byte)) (Syntax: '(Nothing, 1)')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value)
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        <CompilerTrait(CompilerFeature.IOperation)>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

            Dim compilation = verifier.Compilation

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of DirectCastExpressionSyntax)().Single()

            Assert.Equal("DirectCast((Nothing, i),(String, byte))", node.ToString())

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.String, System.Byte)) (Syntax: 'DirectCast( ... ing, byte))')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String, i As System.Byte)) (Syntax: '(Nothing, i)')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 'i')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
            Dim compilation = verifier.Compilation

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TryCastExpressionSyntax)().Single()

            Assert.Equal("TryCast((Nothing, i),(String, byte))", node.ToString())

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.String, System.Byte)) (Syntax: 'TryCast((No ... ing, byte))')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String, i As System.Byte)) (Syntax: '(Nothing, i)')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 'i')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
]]>.Value)
            Dim model = compilation.GetSemanticModel(tree)
            Dim typeInfo = model.GetTypeInfo(node.Expression)
            Assert.Null(typeInfo.Type)
            Assert.Equal("(System.String, i As System.Byte)", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Expression).Kind)
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, options:=TestOptions.ReleaseExe.WithOverflowChecks(False), expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
(1, 2)
(3, (4, 5))
(6, (7, (8, (9, (10, (11, (12, (13, (14, (15, (16, 17)))))))))))
            ]]>)
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C
    Shared Sub Main()
        Dim x1 As Byte = 300
        Dim x2 As Byte() = { 300 }
        Dim x3 As (Byte, Byte) = (300, 300)

        Dim x4 As Byte? = 300
        Dim x5 As Byte?() = { 300 }
        Dim x6 As (Byte?, Byte?) = (300, 300)
        Dim x7 As (Byte, Byte)? = (300, 300)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C

    Shared Sub M1 (x as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M1 (x as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M2 (x as Byte(), y as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M2 (x as Byte(), y as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M3 (x as (Byte, Byte))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M3 (x as (Short, Short))
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M4 (x as (Byte, Byte), y as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M4 (x as (Byte, Byte), y as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M5 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M5 (x as Short?)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M6 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M6 (x as (Short?, Short?))
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M7 (x as (Byte, Byte)?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M7 (x as (Short, Short)?)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M8 (x as (Byte, Byte)?, y as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M8 (x as (Byte, Byte)?, y as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub Main()
        M1(70000)
        M2({ 300 }, 70000)
        M3((70000, 70000))
        M4((300, 300), 70000)

        M5(70000)
        M6((70000, 70000))
        M7((70000, 70000))
        M8((300, 300), 70000)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Byte
Byte
Byte
Byte
Byte
Byte
Byte
Byte")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub M1 (x as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M1 (x as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M2 (x as Byte(), y as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M2 (x as Byte(), y as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M3 (x as (Byte, Byte))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M3 (x as (Short, Short))
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M4 (x as (Byte, Byte), y as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M4 (x as (Byte, Byte), y as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M5 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M5 (x as Short?)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M6 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M6 (x as (Short?, Short?))
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M7 (x as (Byte, Byte)?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M7 (x as (Short, Short)?)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub M8 (x as (Byte, Byte)?, y as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M8 (x as (Byte, Byte)?, y as Short)
        System.Console.WriteLine("Short")
    End Sub

    Shared Sub Main()
        M1(70000)
        M2({ 300 }, 70000)
        M3((70000, 70000))
        M4((300, 300), 70000)

        M5(70000)
        M6((70000, 70000))
        M7((70000, 70000))
        M8((300, 300), 70000)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Byte
Byte
Byte
Byte
Byte
Byte
Byte
Byte")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_04()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C

    Shared Sub M1 (x as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M1 (x as Integer)
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M2 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M2 (x as Integer?)
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M3 (x as Byte())
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M3 (x as Integer())
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M4 (x as (Byte, Byte))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M4 (x as (Integer, Integer))
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M5 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M5 (x as (Integer?, Integer?))
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M6 (x as (Byte, Byte)?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M6 (x as (Integer, Integer)?)
        System.Console.WriteLine("Integer")
    End Sub
    
    Shared Sub M7 (x as (Byte, Byte)())
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M7 (x as (Integer, Integer)())
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub Main()
        M1(1)
        M2(1)
        M3({1})
        M4((1, 1))
        M5((1, 1))
        M6((1, 1))
        M7({(1, 1)})
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(True), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Integer
Integer
Integer
Integer
Integer
Integer
Integer")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_05()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C

    Shared Sub M1 (x as Byte)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M1 (x as Long)
        System.Console.WriteLine("Long")
    End Sub

    Shared Sub M2 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M2 (x as Long?)
        System.Console.WriteLine("Long")
    End Sub

    Shared Sub M3 (x as Byte())
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M3 (x as Long())
        System.Console.WriteLine("Long")
    End Sub

    Shared Sub M4 (x as (Byte, Byte))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M4 (x as (Long, Long))
        System.Console.WriteLine("Long")
    End Sub

    Shared Sub M5 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M5 (x as (Long?, Long?))
        System.Console.WriteLine("Long")
    End Sub

    Shared Sub M6 (x as (Byte, Byte)?)
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M6 (x as (Long, Long)?)
        System.Console.WriteLine("Long")
    End Sub
    
    Shared Sub M7 (x as (Byte, Byte)())
        System.Console.WriteLine("Byte")
    End Sub
    Shared Sub M7 (x as (Long, Long)())
        System.Console.WriteLine("Long")
    End Sub

    Shared Sub Main()
        M1(1)
        M2(1)
        M3({1})
        M4((1, 1))
        M5((1, 1))
        M6((1, 1))
        M7({(1, 1)})
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(True), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Long
Long
Long
Long
Long
Long
Long")
        End Sub

        <Fact>
        <WorkItem(14473, "https://github.com/dotnet/roslyn/issues/14473")>
        Public Sub NarrowingFromNumericConstant_06()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub M2 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M3 (x as Byte())
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M5 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M6 (x as Byte?())
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M7 (x as (Byte, Byte)())
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub Main()
        Dim a as Byte? = 1
        Dim b as Byte() = {1}
        Dim c as (Byte?, Byte?) = (1, 1)
        Dim d as Byte?() = {1}
        Dim e as (Byte, Byte)() = {(1, 1)}
        M2(1)
        M3({1})
        M5((1, 1))
        M6({1})
        M7({(1, 1)})
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(True), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Byte
Byte
Byte
Byte
Byte")
        End Sub

        <Fact>
        <WorkItem(14473, "https://github.com/dotnet/roslyn/issues/14473")>
        Public Sub NarrowingFromNumericConstant_07()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub M1 (x as Byte)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M2 (x as Byte(), y as Byte)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M3 (x as (Byte, Byte))
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M4 (x as (Byte, Byte), y as Byte)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M5 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M6 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M7 (x as (Byte, Byte)?)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M8 (x as (Byte, Byte)?, y as Byte)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub Main()
        M1(300)
        M2({ 300 }, 300)
        M3((300, 300))
        M4((300, 300), 300)

        M5(70000)
        M6((70000, 70000))
        M7((70000, 70000))
        M8((300, 300), 300)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Byte
Byte
Byte
Byte
Byte
Byte
Byte
Byte")
        End Sub

        <Fact>
        Public Sub NarrowingFromNumericConstant_08()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C

    Shared Sub Main()
        Dim x00 As (Integer, Integer) = (1, 1)
        Dim x01 As (Byte, Integer) = (1, 1)
        Dim x02 As (Integer, Byte) = (1, 1)
        Dim x03 As (Byte, Long) = (1, 1)
        Dim x04 As (Long, Byte) = (1, 1)
        Dim x05 As (Byte, Integer) = (300, 1)
        Dim x06 As (Integer, Byte) = (1, 300)
        Dim x07 As (Byte, Long) = (300, 1)
        Dim x08 As (Long, Byte) = (1, 300)
        Dim x09 As (Long, Long) = (1, 300)
        Dim x10 As (Byte, Byte) = (1, 300)
        Dim x11 As (Byte, Byte) = (300, 1)
        Dim one As Integer = 1
        Dim x12 As (Byte, Byte, Byte) = (one, 300, 1)
        Dim x13 As (Byte, Byte, Byte) = (300, one, 1)
        Dim x14 As (Byte, Byte, Byte) = (300, 1, one)
        Dim x15 As (Byte, Byte) = (one, one)
        Dim x16 As (Integer, (Byte, Integer)) = (1, (1, 1))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ToArray()

            AssertConversions(model, nodes(0), ConversionKind.Identity, ConversionKind.Identity, ConversionKind.Identity)
            AssertConversions(model, nodes(1), ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.Identity)
            AssertConversions(model, nodes(2), ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.Identity,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(3), ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric)
            AssertConversions(model, nodes(4), ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(5), ConversionKind.NarrowingTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.Identity)
            AssertConversions(model, nodes(6), ConversionKind.NarrowingTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.Identity,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(7), ConversionKind.NarrowingTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric)
            AssertConversions(model, nodes(8), ConversionKind.NarrowingTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(9), ConversionKind.WideningTuple,
                              ConversionKind.WideningNumeric,
                              ConversionKind.WideningNumeric)
            AssertConversions(model, nodes(10), ConversionKind.NarrowingTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(11), ConversionKind.NarrowingTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(12), ConversionKind.NarrowingTuple,
                              ConversionKind.NarrowingNumeric,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(13), ConversionKind.NarrowingTuple,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.NarrowingNumeric,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant)
            AssertConversions(model, nodes(14), ConversionKind.NarrowingTuple,
                              ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.NarrowingNumeric)
            AssertConversions(model, nodes(15), ConversionKind.NarrowingTuple,
                              ConversionKind.NarrowingNumeric,
                              ConversionKind.NarrowingNumeric)
            AssertConversions(model, nodes(16), ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant,
                              ConversionKind.Identity,
                              ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant)
        End Sub

        Private Shared Sub AssertConversions(model As SemanticModel, literal As TupleExpressionSyntax, aggregate As ConversionKind, ParamArray parts As ConversionKind())
            If parts.Length > 0 Then
                Assert.Equal(literal.Arguments.Count, parts.Length)

                For i As Integer = 0 To parts.Length - 1
                    Assert.Equal(parts(i), model.GetConversion(literal.Arguments(i).Expression).Kind)
                Next
            End If

            Assert.Equal(aggregate, model.GetConversion(literal).Kind)
        End Sub

        <Fact>
        Public Sub Narrowing_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub M2 (x as Byte?)
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub M5 (x as (Byte?, Byte?))
        System.Console.WriteLine("Byte")
    End Sub

    Shared Sub Main()
        Dim x as integer = 1
        Dim a as Byte? = x
        Dim b as (Byte?, Byte?) = (x, x)
        M2(x)
        M5((x, x))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(True), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        Dim a as Byte? = x
                         ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        Dim b as (Byte?, Byte?) = (x, x)
                                   ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        Dim b as (Byte?, Byte?) = (x, x)
                                      ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        M2(x)
           ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        M5((x, x))
            ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        M5((x, x))
               ~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub Main()
        Dim x as integer = 1
        Dim x1 = CType(x, Byte)
        Dim x2 = CType(x, Byte?)
        Dim x3 = CType((x, x), (Byte, Integer))
        Dim x4 = CType((x, x), (Byte?, Integer?))
        Dim x5 = CType((x, x), (Byte, Integer)?)
        Dim x6 = CType((x, x), (Byte?, Integer?)?)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub Narrowing_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub Main()
        Dim x as (Integer, Integer) = (1, 1)
        Dim x3 = CType(x, (Byte, Integer))
        Dim x4 = CType(x, (Byte?, Integer?))
        Dim x5 = CType(x, (Byte, Integer)?)
        Dim x6 = CType(x, (Byte?, Integer?)?)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub Narrowing_04()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub Main()
        Dim x as (Integer, Integer) = (1, 1)
        Dim x3 as (Byte, Integer) = x
        Dim x4 as (Byte?, Integer?) = x
        Dim x5 as (Byte, Integer)? = x
        Dim x6 as (Byte?, Integer?)?= x
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp,
<expected>
BC30512: Option Strict On disallows implicit conversions from '(Integer, Integer)' to '(Byte, Integer)'.
        Dim x3 as (Byte, Integer) = x
                                    ~
BC30512: Option Strict On disallows implicit conversions from '(Integer, Integer)' to '(Byte?, Integer?)'.
        Dim x4 as (Byte?, Integer?) = x
                                      ~
BC30512: Option Strict On disallows implicit conversions from '(Integer, Integer)' to '(Byte, Integer)?'.
        Dim x5 as (Byte, Integer)? = x
                                     ~
BC30512: Option Strict On disallows implicit conversions from '(Integer, Integer)' to '(Byte?, Integer?)?'.
        Dim x6 as (Byte?, Integer?)?= x
                                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub OverloadResolution_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Public Class C

    Shared Sub M1 (x as Short)
        System.Console.WriteLine("Short")
    End Sub
    Shared Sub M1 (x as Integer)
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M2 (x as Short?)
        System.Console.WriteLine("Short")
    End Sub
    Shared Sub M2 (x as Integer?)
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M3 (x as (Short, Short))
        System.Console.WriteLine("Short")
    End Sub
    Shared Sub M3(x as (Integer, Integer))
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M4 (x as (Short?, Short?))
        System.Console.WriteLine("Short")
    End Sub
    Shared Sub M4(x as (Integer?, Integer?))
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub M5 (x as (Short, Short)?)
        System.Console.WriteLine("Short")
    End Sub
    Shared Sub M5(x as (Integer, Integer)?)
        System.Console.WriteLine("Integer")
    End Sub

    Shared Sub Main()
        Dim x as Byte = 1
        M1(x)
        M2(x)
        M3((x, x))
        M4((x, x))
        M5((x, x))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"Short
Short
Short
Short
Short")
        End Sub

        <Fact>
        Public Sub FailedDueToNumericOverflow_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C

    Shared Sub Main()
        Dim x1 As Byte = 300
        Dim x2 as (Integer, Byte) = (300, 300)
        Dim x3 As Byte? = 300
        Dim x4 as (Integer?, Byte?) = (300, 300)
        Dim x5 as (Integer, Byte)? = (300, 300)
        Dim x6 as (Integer?, Byte?)? = (300, 300)

        System.Console.WriteLine(x1)
        System.Console.WriteLine(x2)
        System.Console.WriteLine(x3)
        System.Console.WriteLine(x4)
        System.Console.WriteLine(x5)
        System.Console.WriteLine(x6)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp, <expected></expected>)

            CompileAndVerify(comp, expectedOutput:=
"44
(300, 44)
44
(300, 44)
(300, 44)
(300, 44)")

            comp = comp.WithOptions(comp.Options.WithOverflowChecks(True))

            AssertTheseDiagnostics(comp,
<expected>
BC30439: Constant expression not representable in type 'Byte'.
        Dim x1 As Byte = 300
                         ~~~
BC30439: Constant expression not representable in type 'Byte'.
        Dim x2 as (Integer, Byte) = (300, 300)
                                          ~~~
BC30439: Constant expression not representable in type 'Byte?'.
        Dim x3 As Byte? = 300
                          ~~~
BC30439: Constant expression not representable in type 'Byte?'.
        Dim x4 as (Integer?, Byte?) = (300, 300)
                                            ~~~
BC30439: Constant expression not representable in type 'Byte'.
        Dim x5 as (Integer, Byte)? = (300, 300)
                                           ~~~
BC30439: Constant expression not representable in type 'Byte?'.
        Dim x6 as (Integer?, Byte?)? = (300, 300)
                                             ~~~
</expected>)
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
                Dim q = "q"
                Dim a As Object = "a"

                System.Console.WriteLine(Test((q, a)))

                System.Console.WriteLine(q)
                System.Console.WriteLine(a)

                System.Console.WriteLine(Test((Ps, Po)))

                System.Console.WriteLine(q)
                System.Console.WriteLine(a)
            End Sub

            Function Test(Of T)(ByRef x As (T, T)) As (T, T)
                Console.WriteLine(GetType(T))

                x.Item1 = x.Item2

                Return x
            End Function

            Public Property Ps As String
                Get
                    Return "q"
                End Get
                Set(value As String)
                    System.Console.WriteLine("written1 !!!")
                End Set
            End Property

            Public Property Po As Object
                Get
                    Return "a"
                End Get
                Set(value As Object)
                    System.Console.WriteLine("written2 !!!")
                End Set
            End Property
        End Module

    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Object
(a, a)
q
a
System.Object
(a, a)
q
a
            ]]>)
        End Sub

        <Fact>
        Public Sub MethodTypeInference004a()

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Object
(System.Object, q)
System.ValueTuple`2[System.Object,System.String]
(System.Object, q)
            ]]>)

        End Sub

        <Fact>
        Public Sub MethodTypeInference004Err()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[

Imports System
Module C
    Sub Main()
        Dim q = "q"
        Dim a as object = "a"

        Dim t = (q, a)

        System.Console.WriteLine(Test(t))

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
        System.Console.WriteLine(Test(t))
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.Int64
System.Int64
            ]]>)
        End Sub

        <Fact>
        Public Sub MethodTypeInference008Err()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(1, (2, (3, 4)))]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[2
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(1, hello, 2)]]>)

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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub LongTupleTypeMismatch()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.False(DirectCast(xSymbol, INamedTypeSymbol).IsSerializable)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[1
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[4]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(42, 42)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42]]>)

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
</compilation>, references:={ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46}, expectedOutput:=<![CDATA[42]]>)

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
</compilation>, references:={ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46}, expectedOutput:=<![CDATA[(42, 42)]]>)

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
</compilation>, references:={MscorlibRef_v46}, expectedOutput:=<![CDATA[42]]>)

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
</compilation>, references:={ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46}, expectedOutput:=<![CDATA[42]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleUsageWithoutTupleLibrary()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
                                     ~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleUsageWithMissingTupleMembers()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[0
null]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleWithDuplicateMemberNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
                references:=s_valueTupleRefs,
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
                references:=s_valueTupleRefs,
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

        <ConditionalFact(GetType(NoIOperationValidation))>
        Public Sub HugeTupleCreationParses()

            Dim b = New StringBuilder()
            b.Append("(")
            For i As Integer = 0 To 2000
                b.Append("1, ")
            Next
            b.Append("1)")

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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

        <ConditionalFact(GetType(NoIOperationValidation))>
        Public Sub HugeTupleDeclarationParses()

            Dim b = New StringBuilder()
            b.Append("(")
            For i As Integer = 0 To 3000
                b.Append("Integer, ")
            Next
            b.Append("Integer)")

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal("first As T1", mFirst.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[0 False]]>)

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
                references:=s_valueTupleRefs,
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:="42 Alice")

        End Sub

        <Fact>
        Public Sub TupleInOptionalParam()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[0 ]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[42 Alice]]>)

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
                references:=s_valueTupleRefs,
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
        Public Sub TupleCreationWithInferredNamesWithVB15()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Dim e As Integer = 5
    Dim f As Integer = 6
    Dim instance As C = Nothing
    Sub M()
        Dim a As Integer = 1
        Dim b As Integer = 3
        Dim Item4 As Integer = 4
        Dim g As Integer = 7
        Dim Rest As Integer = 9
        Dim y As (x As Integer, Integer, b As Integer, Integer, Integer, Integer, f As Integer, Integer, Integer, Integer) =
            (a, (a), b:=2, b, Item4, instance.e, Me.f, g, g, Rest)
        Dim z = (x:=b, b)
        System.Console.Write(y)
        System.Console.Write(z)
    End Sub
End Class
    </file>
</compilation>,
                references:=s_valueTupleRefs,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15),
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim yTuple = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(0)
                        Assert.Equal("(a As System.Int32, System.Int32, b As System.Int32, System.Int32, System.Int32, e As System.Int32, f As System.Int32, System.Int32, System.Int32, System.Int32)",
                            model.GetTypeInfo(yTuple).Type.ToTestDisplayString())

                        Dim zTuple = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(1)
                        Assert.Equal("(x As System.Int32, b As System.Int32)", model.GetTypeInfo(zTuple).Type.ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleCreationWithInferredNames()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Dim e As Integer = 5
    Dim f As Integer = 6
    Dim instance As C = Nothing
    Sub M()
        Dim a As Integer = 1
        Dim b As Integer = 3
        Dim Item4 As Integer = 4
        Dim g As Integer = 7
        Dim Rest As Integer = 9
        Dim y As (x As Integer, Integer, b As Integer, Integer, Integer, Integer, f As Integer, Integer, Integer, Integer) =
            (a, (a), b:=2, b, Item4, instance.e, Me.f, g, g, Rest)
        Dim z = (x:=b, b)
        System.Console.Write(y)
        System.Console.Write(z)
    End Sub
End Class
    </file>
</compilation>,
                references:=s_valueTupleRefs,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3),
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim yTuple = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(0)
                        Assert.Equal("(a As System.Int32, System.Int32, b As System.Int32, System.Int32, System.Int32, e As System.Int32, f As System.Int32, System.Int32, System.Int32, System.Int32)",
                            model.GetTypeInfo(yTuple).Type.ToTestDisplayString())

                        Dim zTuple = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(1)
                        Assert.Equal("(x As System.Int32, b As System.Int32)", model.GetTypeInfo(zTuple).Type.ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleCreationWithInferredNames2()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Dim e As Integer = 5
    Dim instance As C = Nothing
    Function M() As Integer
        Dim y As (Integer?, object) = (instance?.e, (e, instance.M()))
        System.Console.Write(y)
        Return 42
    End Function
End Class
    </file>
</compilation>,
                references:=s_valueTupleRefs,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3),
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim yTuple = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(0)
                        Assert.Equal("(e As System.Nullable(Of System.Int32), (e As System.Int32, M As System.Int32))",
                            model.GetTypeInfo(yTuple).Type.ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MissingMemberAccessWithVB15()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, 2)
        System.Console.Write(t.A)
        System.Console.Write(GetTuple().a)
    End Sub
    Function GetTuple() As (Integer, Integer)
        Return (1, 2)
    End Function
End Class
    </file>
</compilation>,
                additionalRefs:=s_valueTupleRefs,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            comp.AssertTheseDiagnostics(<errors>
BC37289: Tuple element name 'a' is inferred. Please use language version 15.3 or greater to access an element by its inferred name.
        System.Console.Write(t.A)
                             ~~~
BC30456: 'a' is not a member of '(Integer, Integer)'.
        System.Console.Write(GetTuple().a)
                             ~~~~~~~~~~~~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub UseSiteDiagnosticOnTupleField()
            Dim missingComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UseSiteDiagnosticOnTupleField_missingComp">
    <file name="missing.vb">
Public Class Missing
End Class
    </file>
</compilation>)
            missingComp.VerifyDiagnostics()

            Dim libComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="lib.vb">
Public Class C
    Public Shared Function GetTuple() As (Missing, Integer)
        Throw New System.Exception()
    End Function
End Class
    </file>
</compilation>, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, missingComp.ToMetadataReference()})
            libComp.VerifyDiagnostics()

            Dim source =
                <compilation>
                    <file name="a.vb">
Class D
    Sub M()
        System.Console.Write(C.GetTuple().Item1)
    End Sub
End Class
    </file>
                </compilation>

            Dim comp15 = CreateCompilationWithMscorlib40AndVBRuntime(source,
                additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, libComp.ToMetadataReference()},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            comp15.AssertTheseDiagnostics(<errors>
BC30652: Reference required to assembly 'UseSiteDiagnosticOnTupleField_missingComp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project.
        System.Console.Write(C.GetTuple().Item1)
                             ~~~~~~~~~~~~
                                          </errors>)

            Dim comp15_3 = CreateCompilationWithMscorlib40AndVBRuntime(source,
                additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, libComp.ToMetadataReference()},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

            comp15_3.AssertTheseDiagnostics(<errors>
BC30652: Reference required to assembly 'UseSiteDiagnosticOnTupleField_missingComp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project.
        System.Console.Write(C.GetTuple().Item1)
                             ~~~~~~~~~~~~
                                          </errors>)
        End Sub

        <Fact>
        Public Sub UseSiteDiagnosticOnTupleField2()
            Dim source =
                <compilation>
                    <file name="a.vb">
Class C
    Sub M()
        Dim a = 1
        Dim t = (a, 2)
        System.Console.Write(t.a)
    End Sub
End Class
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Sub New(item1 As T1, item2 As T2)
        End Sub
    End Structure
End Namespace
                    </file>
                </compilation>

            Dim comp15 = CreateCompilationWithMscorlib40AndVBRuntime(source,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            comp15.AssertTheseDiagnostics(<errors>
BC35000: Requested operation is not available because the runtime library function 'ValueTuple.Item1' is not defined.
        System.Console.Write(t.a)
                             ~~~
                                          </errors>)

            Dim comp15_3 = CreateCompilationWithMscorlib40AndVBRuntime(source,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

            comp15_3.AssertTheseDiagnostics(<errors>
BC35000: Requested operation is not available because the runtime library function 'ValueTuple.Item1' is not defined.
        System.Console.Write(t.a)
                             ~~~
                                            </errors>)
        End Sub

        <Fact>
        Public Sub MissingMemberAccessWithExtensionWithVB15()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, 2)
        System.Console.Write(t.A)
        System.Console.Write(GetTuple().a)
    End Sub
    Function GetTuple() As (Integer, Integer)
        Return (1, 2)
    End Function
End Class
Module Extensions
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function A(self As (Integer, Action)) As String
        Return Nothing
    End Function
End Module
    </file>
</compilation>,
                additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            comp.AssertTheseDiagnostics(<errors>
BC37289: Tuple element name 'a' is inferred. Please use language version 15.3 or greater to access an element by its inferred name.
        System.Console.Write(t.A)
                             ~~~
BC30456: 'a' is not a member of '(Integer, Integer)'.
        System.Console.Write(GetTuple().a)
                             ~~~~~~~~~~~~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub MissingMemberAccessWithVB15_3()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a, 2)
        System.Console.Write(t.b)
    End Sub
End Class
    </file>
</compilation>,
                additionalRefs:=s_valueTupleRefs,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

            comp.AssertTheseDiagnostics(<errors>
BC30456: 'b' is not a member of '(a As Integer, Integer)'.
        System.Console.Write(t.b)
                             ~~~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub InferredNamesInLinq()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Imports System.Linq
Class C
    Dim f1 As Integer = 0
    Dim f2 As Integer = 1
    Shared Sub Main(list As IEnumerable(Of C))
        Dim result = list.Select(Function(c) (c.f1, c.f2)).Where(Function(t) t.f2 = 1) ' t and result have names f1 and f2
        System.Console.Write(result.Count())
    End Sub
End Class
    </file>
</compilation>,
                references:={ValueTupleRef, SystemRuntimeFacadeRef, LinqAssemblyRef},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3),
                sourceSymbolValidator:=
                    Sub(m As ModuleSymbol)
                        Dim compilation = m.DeclaringCompilation
                        Dim tree = compilation.SyntaxTrees.First()
                        Dim model = compilation.GetSemanticModel(tree)
                        Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

                        Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

                        Dim result = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().ElementAt(2).Names(0)
                        Dim resultSymbol = model.GetDeclaredSymbol(result)
                        Assert.Equal("result As System.Collections.Generic.IEnumerable(Of (f1 As System.Int32, f2 As System.Int32))", resultSymbol.ToTestDisplayString())
                    End Sub)

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub InferredNamesInTernary()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim i = 1
        Dim flag = False
        Dim t = If(flag, (i, 2), (i, 3))
        System.Console.Write(t.i)
    End Sub
End Class
    </file>
</compilation>,
                references:={ValueTupleRef, SystemRuntimeFacadeRef, LinqAssemblyRef},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3),
                expectedOutput:="1")

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub InferredNames_ExtensionNowFailsInVB15ButNotVB15_3()
            Dim source = <compilation>
                             <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim M As Action = Sub() Console.Write("lambda")
        Dim t = (1, M)
        t.M()
    End Sub
End Class
Module Extensions
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Sub M(self As (Integer, Action))
        Console.Write("extension")
    End Sub
End Module
    </file>
                         </compilation>
            ' When VB 15 shipped, no tuple element would be found/inferred, so the extension method was called.
            ' The VB 15.3 compiler disallows that, even when LanguageVersion is 15.
            Dim comp15 = CreateCompilationWithMscorlib40AndVBRuntime(source,
                additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            comp15.AssertTheseDiagnostics(<errors>
BC37289: Tuple element name 'M' is inferred. Please use language version 15.3 or greater to access an element by its inferred name.
        t.M()
        ~~~
                                          </errors>)

            Dim verifier15_3 = CompileAndVerify(source,
                references:={ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3),
                expectedOutput:="lambda")
            verifier15_3.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub InferredName_Conversion()
            Dim source = <compilation>
                             <file>
Class C
    Shared Sub F(t As (Object, Object))
    End Sub
    Shared Sub G(o As Object)
        Dim t = (1, o)
        F(t)
    End Sub
End Class
    </file>
                         </compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source,
                additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef},
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            comp.AssertTheseEmitDiagnostics(<errors/>)
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 4 7 Alice 7 Bob 3]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub DuplicateTupleMethodsNotAllowed()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 2]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 2
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[0 0
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[1 Alice]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[True 1 Alice]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub TupleUnsupportedInUsingStatement()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
<compilation>
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
<compilation>
    <file name="a.vb">
Public Module M2
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Sub Extension(x As Integer, y As (Integer, Integer))
        System.Console.Write("M2.Extension")
    End Sub
End Module
<%= s_trivial2uple %></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40AndVBRuntime(source1, additionalRefs:={MscorlibRef_v46}, assemblyName:="comp1")
            comp1.AssertNoDiagnostics()
            Dim comp2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, additionalRefs:={MscorlibRef_v46}, assemblyName:="comp2")
            comp2.AssertNoDiagnostics()

            Dim source =
<compilation>
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

            Dim comp3 = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={comp1.ToMetadataReference(), comp2.ToMetadataReference()})
            comp3.AssertTheseDiagnostics(
<errors>
BC30521: Overload resolution failed because no accessible 'Extension' is most specific for these arguments:
    Extension method 'Public Sub Extension(y As (Integer, Integer))' defined in 'M1': Not most specific.
    Extension method 'Public Sub Extension(y As (Integer, Integer))' defined in 'M2': Not most specific.
        x.Extension((1, 1))
          ~~~~~~~~~
BC37305: Predefined type 'ValueTuple(Of ,)' is declared in multiple referenced assemblies: 'comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'comp2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
        x.Extension((1, 1))
                    ~~~~~~
</errors>)

            Dim comp4 = CreateCompilationWithMscorlib40AndVBRuntime(source,
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[12
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
            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Item1")))
            Assert.Contains(CodeAnalysisResources.TupleElementNameCountMismatch, ex.Message)

            Dim tree = VisualBasicSyntaxTree.ParseText("Class C")
            Dim loc1 = Location.Create(tree, New TextSpan(0, 1))
            ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(vt2, elementLocations:=ImmutableArray.Create(loc1)))
            Assert.Contains(CodeAnalysisResources.TupleElementLocationCountMismatch, ex.Message)
        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithValueTuple()

            Dim tupleComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.True(GetTupleElementNames(tupleWithoutNames).IsDefault)
            Assert.Equal((New String() {"System.Int32", "System.String"}), ElementTypeNames(tupleWithoutNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)
            Assert.All(tupleWithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
        End Sub

        Private Shared Function ElementTypeNames(tuple As INamedTypeSymbol) As IEnumerable(Of String)
            Return tuple.TupleElements.Select(Function(t) t.Type.ToTestDisplayString())
        End Function

        <Fact>
        Public Sub CreateTupleTypeSymbol_Locations()

            Dim tupleComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType)

            Dim tree = VisualBasicSyntaxTree.ParseText("Class C")
            Dim loc1 = Location.Create(tree, New TextSpan(0, 1))
            Dim loc2 = Location.Create(tree, New TextSpan(1, 1))
            Dim tuple = comp.CreateTupleTypeSymbol(
                vt2, ImmutableArray.Create("i1", "i2"), ImmutableArray.Create(loc1, loc2))

            Assert.True(tuple.IsTupleType)
            Assert.Equal(SymbolKind.NamedType, tuple.TupleUnderlyingType.Kind)
            Assert.Equal("(i1 As System.Int32, i2 As System.String)", tuple.ToTestDisplayString())
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tuple))
            Assert.Equal(SymbolKind.NamedType, tuple.Kind)
            Assert.Equal(loc1, tuple.GetMembers("i1").Single.Locations.Single())
            Assert.Equal(loc2, tuple.GetMembers("i2").Single.Locations.Single())
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
            Assert.True(GetTupleElementNames(tupleWithoutNames).IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tupleWithoutNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)
            Assert.All(tupleWithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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
            Assert.Equal(New String() {"Alice", "Bob"}, GetTupleElementNames(tupleWithoutNames))
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tupleWithoutNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)
            Assert.All(tupleWithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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
            Assert.Equal(New String() {Nothing, "Item2", "Charlie"}, GetTupleElementNames(tupleWithSomeNames))
            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32"}, ElementTypeNames(tupleWithSomeNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithSomeNames.Kind)
            Assert.All(tupleWithSomeNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithBadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Item2", "Item1"))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal("(Item2 As System.Int32, Item1 As System.Int32)", tupleWithoutNames.ToTestDisplayString())
            Assert.Equal(New String() {"Item2", "Item1"}, GetTupleElementNames(tupleWithoutNames))
            Assert.Equal(New String() {"System.Int32", "System.Int32"}, ElementTypeNames(tupleWithoutNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)
            Assert.All(tupleWithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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

            Assert.True(GetTupleElementNames(tuple8WithoutNames).IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         ElementTypeNames(tuple8WithoutNames))
            Assert.All(tuple8WithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"}, GetTupleElementNames(tuple8WithNames))

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         ElementTypeNames(tuple8WithNames))
            Assert.All(tuple8WithNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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

            Assert.True(GetTupleElementNames(tuple9WithoutNames).IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         ElementTypeNames(tuple9WithoutNames))
            Assert.All(tuple9WithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"}, GetTupleElementNames(tuple9WithNames))

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         ElementTypeNames(tuple9WithNames))
            Assert.All(tuple9WithNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
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

            Assert.Equal(New String() {"Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9"}, GetTupleElementNames(tuple9WithNames))

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         ElementTypeNames(tuple9WithNames))
            Assert.All(tuple9WithNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_ElementTypeIsError()

            Dim tupleComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, ErrorTypeSymbol.UnknownResultType)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, Nothing)

            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

            Dim types = tupleWithoutNames.TupleElements.SelectAsArray(Function(e) e.Type)
            Assert.Equal(2, types.Length)
            Assert.Equal(SymbolKind.NamedType, types(0).Kind)
            Assert.Equal(SymbolKind.ErrorType, types(1).Kind)
            Assert.All(tupleWithoutNames.GetMembers().OfType(Of IFieldSymbol)().Select(Function(f) f.Locations.FirstOrDefault()),
                Sub(Loc) Assert.Equal(Loc, Nothing))
        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_BadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})

            Dim intType As NamedTypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType)
            Dim vt3 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T3).Construct(intType, intType, intType)

            ' Illegal VB identifier, space and null
            Dim tuple2 = comp.CreateTupleTypeSymbol(vt3, ImmutableArray.Create("123", " ", Nothing))
            Assert.Equal({"123", " ", Nothing}, GetTupleElementNames(tuple2))

            ' Reserved keywords
            Dim tuple3 = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("return", "class"))
            Assert.Equal({"return", "class"}, GetTupleElementNames(tuple3))

            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(underlyingType:=intType))
            Assert.Contains(CodeAnalysisResources.TupleUnderlyingTypeMustBeTupleCompatible, ex.Message)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_EmptyNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})

            Dim intType As NamedTypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType)

            ' Illegal VB identifier and empty
            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("123", "")))
            Assert.Contains(CodeAnalysisResources.TupleElementNameEmpty, ex.Message)
            Assert.Contains("1", ex.Message)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_CSharpElements()

            Dim csSource = "public class C { }"
            Dim csComp = CreateCSharpCompilation("CSharp", csSource,
                                                 compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csComp.VerifyDiagnostics()
            Dim csType = DirectCast(csComp.GlobalNamespace.GetMembers("C").Single(), INamedTypeSymbol)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})
            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(csType, Nothing))
            Assert.Contains(VBResources.NotAVbSymbol, ex.Message)
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

            Dim tupleComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), ImmutableArray.Create(Of String)(Nothing, Nothing))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.TupleUnderlyingType.Kind)
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.True(GetTupleElementNames(tupleWithoutNames).IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tupleWithoutNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_Locations()

            Dim tupleComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim tree = VisualBasicSyntaxTree.ParseText("Class C")
            Dim loc1 = Location.Create(tree, New TextSpan(0, 1))
            Dim loc2 = Location.Create(tree, New TextSpan(1, 1))
            Dim tuple = comp.CreateTupleTypeSymbol(
                ImmutableArray.Create(intType, stringType),
                ImmutableArray.Create("i1", "i2"),
                ImmutableArray.Create(loc1, loc2))

            Assert.True(tuple.IsTupleType)
            Assert.Equal(SymbolKind.NamedType, tuple.TupleUnderlyingType.Kind)
            Assert.Equal("(i1 As System.Int32, i2 As System.String)", tuple.ToTestDisplayString())
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tuple))
            Assert.Equal(SymbolKind.NamedType, tuple.Kind)
            Assert.Equal(loc1, tuple.GetMembers("i1").Single().Locations.Single())
            Assert.Equal(loc2, tuple.GetMembers("i2").Single().Locations.Single())
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
            Assert.True(GetTupleElementNames(tupleWithoutNames).IsDefault)
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tupleWithoutNames))
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
            Assert.Equal(New String() {"Alice", "Bob"}, GetTupleElementNames(tupleWithoutNames))
            Assert.Equal(New String() {"System.Int32", "System.String"}, ElementTypeNames(tupleWithoutNames))
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_WithBadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("Item2", "Item1"))

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal("(Item2 As System.Int32, Item1 As System.Int32)", tupleWithoutNames.ToTestDisplayString())
            Assert.Equal(New String() {"Item2", "Item1"}, GetTupleElementNames(tupleWithoutNames))
            Assert.Equal(New String() {"System.Int32", "System.Int32"}, ElementTypeNames(tupleWithoutNames))
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

            Assert.True(GetTupleElementNames(tuple8WithoutNames).IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         ElementTypeNames(tuple8WithoutNames))

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

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"}, GetTupleElementNames(tuple8WithNames))

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String"},
                         ElementTypeNames(tuple8WithNames))

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

            Assert.True(GetTupleElementNames(tuple9WithoutNames).IsDefault)

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         ElementTypeNames(tuple9WithoutNames))

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

            Assert.Equal(New String() {"Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"}, GetTupleElementNames(tuple9WithNames))

            Assert.Equal(New String() {"System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32"},
                         ElementTypeNames(tuple9WithNames))

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_ElementTypeIsError()

            Dim tupleComp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><%= s_trivial2uple %></file>
</compilation>)

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, tupleComp.ToMetadataReference()})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, ErrorTypeSymbol.UnknownResultType), Nothing)

            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind)

            Dim types = tupleWithoutNames.TupleElements.SelectAsArray(Function(e) e.Type)
            Assert.Equal(2, types.Length)
            Assert.Equal(SymbolKind.NamedType, types(0).Kind)
            Assert.Equal(SymbolKind.ErrorType, types(1).Kind)

        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol2_BadNames()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef})

            Dim intType As ITypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)

            ' Illegal VB identifier and blank
            Dim tuple2 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("123", " "))
            Assert.Equal({"123", " "}, GetTupleElementNames(tuple2))

            ' Reserved keywords
            Dim tuple3 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("return", "class"))
            Assert.Equal({"return", "class"}, GetTupleElementNames(tuple3))

        End Sub

        Private Shared Function GetTupleElementNames(tuple As INamedTypeSymbol) As ImmutableArray(Of String)
            Dim elements = tuple.TupleElements

            If elements.All(Function(e) e.IsImplicitlyDeclared) Then
                Return Nothing
            End If

            Return elements.SelectAsArray(Function(e) e.ProvidedTupleElementNameOrNull)
        End Function

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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Dim intType As NamedTypeSymbol = comp.GetSpecialType(SpecialType.System_String)

            Assert.False(intType.IsTupleType)
            Assert.True(intType.TupleElementNames.IsDefault)
            Assert.True(intType.TupleElementTypes.IsDefault)

        End Sub

        <Fact>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateTupleTypeSymbol_UnderlyingType_DefaultArgs()
            Dim comp = CreateCompilation(
"Module Program
    Private F As (Integer, String)
End Module")
            Dim tuple1 = DirectCast(DirectCast(comp.GetMember("Program.F"), IFieldSymbol).Type, INamedTypeSymbol)
            Dim underlyingType = tuple1.TupleUnderlyingType

            Dim tuple2 = comp.CreateTupleTypeSymbol(underlyingType)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, Nothing, Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, Nothing, Nothing, Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementNames:=Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementLocations:=Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=Nothing)
            Assert.True(tuple1.Equals(tuple2))
        End Sub

        <Fact>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateTupleTypeSymbol_ElementTypes_DefaultArgs()
            Dim comp = CreateCompilation(
"Module Program
    Private F As (Integer, String)
End Module")
            Dim tuple1 = DirectCast(DirectCast(comp.GetMember("Program.F"), IFieldSymbol).Type, INamedTypeSymbol)
            Dim elementTypes = tuple1.TupleElements.SelectAsArray(Function(e) e.Type)

            Dim tuple2 = comp.CreateTupleTypeSymbol(elementTypes)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, Nothing, Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, Nothing, Nothing, Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementNames:=Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementLocations:=Nothing)
            Assert.True(tuple1.Equals(tuple2))

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=Nothing)
            Assert.True(tuple1.Equals(tuple2))
        End Sub

        <Fact>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateTupleTypeSymbol_UnderlyingType_WithNullableAnnotations_01()
            Dim comp = CreateCompilation(
"Module Program
    Private F As (Integer, String)
End Module")
            Dim tuple1 = DirectCast(DirectCast(comp.GetMember("Program.F"), IFieldSymbol).Type, INamedTypeSymbol)
            Dim underlyingType = tuple1.TupleUnderlyingType

            Dim tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=Nothing)
            Assert.True(tuple1.Equals(tuple2))

            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=ImmutableArray(Of NullableAnnotation).Empty))
            Assert.Contains(CodeAnalysisResources.TupleElementNullableAnnotationCountMismatch, ex.Message)

            tuple2 = comp.CreateTupleTypeSymbol(
                underlyingType,
                elementNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None))
            Assert.True(tuple1.Equals(tuple2))
            Assert.Equal("(System.Int32, System.String)", tuple2.ToTestDisplayString())

            tuple2 = comp.CreateTupleTypeSymbol(
                underlyingType,
                elementNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.NotAnnotated, CodeAnalysis.NullableAnnotation.Annotated))
            Assert.True(tuple1.Equals(tuple2))
            Assert.Equal("(System.Int32, System.String)", tuple2.ToTestDisplayString())

            tuple2 = comp.CreateTupleTypeSymbol(
                underlyingType,
                elementNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.None))
            Assert.True(tuple1.Equals(tuple2))
            Assert.Equal("(System.Int32, System.String)", tuple2.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateTupleTypeSymbol_UnderlyingType_WithNullableAnnotations_02()
            Dim comp = CreateCompilation(
"Module Program
    Private F As (_1 As Object, _2 As Object, _3 As Object, _4 As Object, _5 As Object, _6 As Object, _7 As Object, _8 As Object, _9 As Object)
End Module")
            Dim tuple1 = DirectCast(DirectCast(comp.GetMember("Program.F"), IFieldSymbol).Type, INamedTypeSymbol)
            Dim underlyingType = tuple1.TupleUnderlyingType

            Dim tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=Nothing)
            Assert.True(TypeEquals(tuple1, tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.Equal("(System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object)", tuple2.ToTestDisplayString())

            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=CreateAnnotations(CodeAnalysis.NullableAnnotation.NotAnnotated, 8)))
            Assert.Contains(CodeAnalysisResources.TupleElementNullableAnnotationCountMismatch, ex.Message)

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=CreateAnnotations(CodeAnalysis.NullableAnnotation.None, 9))
            Assert.True(TypeEquals(tuple1, tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.Equal("(System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object)", tuple2.ToTestDisplayString())

            tuple2 = comp.CreateTupleTypeSymbol(underlyingType, elementNullableAnnotations:=CreateAnnotations(CodeAnalysis.NullableAnnotation.Annotated, 9))
            Assert.True(TypeEquals(tuple1, tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.Equal("(System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object)", tuple2.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateTupleTypeSymbol_ElementTypes_WithNullableAnnotations_01()
            Dim comp = CreateCompilation(
"Module Program
    Private F As (Integer, String)
End Module")
            Dim tuple1 = DirectCast(DirectCast(comp.GetMember("Program.F"), IFieldSymbol).Type, INamedTypeSymbol)
            Dim elementTypes = tuple1.TupleElements.SelectAsArray(Function(e) e.Type)

            Dim tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=Nothing)
            Assert.True(tuple1.Equals(tuple2))

            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=ImmutableArray(Of NullableAnnotation).Empty))
            Assert.Contains(CodeAnalysisResources.TupleElementNullableAnnotationCountMismatch, ex.Message)

            tuple2 = comp.CreateTupleTypeSymbol(
                elementTypes,
                elementNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None))
            Assert.True(tuple1.Equals(tuple2))
            Assert.Equal("(System.Int32, System.String)", tuple2.ToTestDisplayString())

            tuple2 = comp.CreateTupleTypeSymbol(
                elementTypes,
                elementNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.NotAnnotated, CodeAnalysis.NullableAnnotation.Annotated))
            Assert.True(tuple1.Equals(tuple2))
            Assert.Equal("(System.Int32, System.String)", tuple2.ToTestDisplayString())

            tuple2 = comp.CreateTupleTypeSymbol(
                elementTypes,
                elementNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.None))
            Assert.True(tuple1.Equals(tuple2))
            Assert.Equal("(System.Int32, System.String)", tuple2.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateTupleTypeSymbol_ElementTypes_WithNullableAnnotations_02()
            Dim comp = CreateCompilation(
"Module Program
    Private F As (_1 As Object, _2 As Object, _3 As Object, _4 As Object, _5 As Object, _6 As Object, _7 As Object, _8 As Object, _9 As Object)
End Module")
            Dim tuple1 = DirectCast(DirectCast(comp.GetMember("Program.F"), IFieldSymbol).Type, INamedTypeSymbol)
            Dim elementTypes = tuple1.TupleElements.SelectAsArray(Function(e) e.Type)

            Dim tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=Nothing)
            Assert.True(TypeEquals(tuple1, tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.Equal("(System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object)", tuple2.ToTestDisplayString())

            Dim ex = Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=CreateAnnotations(CodeAnalysis.NullableAnnotation.NotAnnotated, 8)))
            Assert.Contains(CodeAnalysisResources.TupleElementNullableAnnotationCountMismatch, ex.Message)

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=CreateAnnotations(CodeAnalysis.NullableAnnotation.None, 9))
            Assert.True(TypeEquals(tuple1, tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.Equal("(System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object)", tuple2.ToTestDisplayString())

            tuple2 = comp.CreateTupleTypeSymbol(elementTypes, elementNullableAnnotations:=CreateAnnotations(CodeAnalysis.NullableAnnotation.Annotated, 9))
            Assert.True(TypeEquals(tuple1, tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.Equal("(System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object, System.Object)", tuple2.ToTestDisplayString())
        End Sub

        Private Shared Function CreateAnnotations(annotation As CodeAnalysis.NullableAnnotation, n As Integer) As ImmutableArray(Of CodeAnalysis.NullableAnnotation)
            Return ImmutableArray.CreateRange(Enumerable.Range(0, n).Select(Function(i) annotation))
        End Function

        Private Shared Function TypeEquals(a As ITypeSymbol, b As ITypeSymbol, compareKind As TypeCompareKind) As Boolean
            Return TypeSymbol.Equals(DirectCast(a, TypeSymbol), DirectCast(b, TypeSymbol), compareKind)
        End Function

        <Fact>
        Public Sub TupleTargetTypeAndConvert01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(1, hello)]]>)

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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
        Public Sub TupleInferredLambdaStrictOn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
        Public Sub TupleInferredLambdaStrictOff()

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
System.ValueTuple`2[System.Int32,VB$AnonymousDelegate_0`1[System.Object]]
System.ValueTuple`2[System.Object,VB$AnonymousDelegate_1`2[System.Object,System.Object]]
System.ValueTuple`2[System.Int32,VB$AnonymousDelegate_1`2[System.Object,System.Object]]
            ]]>)
        End Sub

        <Fact>
        Public Sub TupleImplicitConversionFail03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TupleCTypeNullableConversionWithTypelessTuple()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Class C
    Shared Sub Main()
        Dim x As (Integer, String)? = CType((1, Nothing), (Integer, String)?)
        Console.Write(x)
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertNoDiagnostics()
            CompileAndVerify(comp, expectedOutput:="(1, )")

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().Single()
            Assert.Equal("(1, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(node).Kind)

            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())

            Dim [ctype] = tree.GetRoot().DescendantNodes().OfType(Of CTypeExpressionSyntax)().Single()

            Assert.Equal("CType((1, Nothing), (Integer, String)?)", [ctype].ToString())

            comp.VerifyOperationTree([ctype], expectedOperationTree:=
            <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of (System.Int32, System.String))) (Syntax: 'CType((1, N ... , String)?)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.String)) (Syntax: '(1, Nothing)')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value)
        End Sub

        <Fact>
        Public Sub TupleDirectCastNullableConversionWithTypelessTuple()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Class C
    Shared Sub Main()
        Dim x As (Integer, String)? = DirectCast((1, Nothing), (Integer, String)?)
        Console.Write(x)
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertNoDiagnostics()
            CompileAndVerify(comp, expectedOutput:="(1, )")

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().Single()
            Assert.Equal("(1, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(node).Kind)

            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleTryCastNullableConversionWithTypelessTuple()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Class C
    Shared Sub Main()
        Dim x As (Integer, String)? = TryCast((1, Nothing), (Integer, String)?)
        Console.Write(x)
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics(<errors>
BC30792: 'TryCast' operand must be reference type, but '(Integer, String)?' is a value type.
        Dim x As (Integer, String)? = TryCast((1, Nothing), (Integer, String)?)
                                                            ~~~~~~~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().Single()
            Assert.Equal("(1, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleTryCastNullableConversionWithTypelessTuple2()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Class C
    Shared Sub M(Of T)()
        Dim x = TryCast((0, Nothing), C(Of Integer, T))
        Console.Write(x)
    End Sub
End Class
Class C(Of T, U)
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(<errors>
BC30311: Value of type '(Integer, Object)' cannot be converted to 'C(Of Integer, T)'.
        Dim x = TryCast((0, Nothing), C(Of Integer, T))
                        ~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().Single()
            Assert.Equal("(0, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(node).Kind)

            Assert.Equal("C(Of System.Int32, T)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("C(Of System.Int32, T)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleImplicitNullableConversionWithTypelessTuple()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Class C
    Shared Sub Main()
        Dim x As (Integer, String)? = (1, Nothing)
        System.Console.Write(x)
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertNoDiagnostics()
            CompileAndVerify(comp, expectedOutput:="(1, )")

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().Single()
            Assert.Equal("(1, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("System.Nullable(Of (System.Int32, System.String))", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(node).Kind)

        End Sub

        <Fact>
        Public Sub ImplicitConversionOnTypelessTupleWithUserConversion()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Structure C
    Shared Sub Main()
        Dim x As C = (1, Nothing)
        Dim y As C? = (2, Nothing)
    End Sub
    Public Shared Widening Operator CType(ByVal d As (Integer, String)) As C
          Return New C()
    End Operator
End Structure
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics(<errors>
BC30311: Value of type '(Integer, Object)' cannot be converted to 'C?'.
        Dim y As C? = (2, Nothing)
                      ~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim firstTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(0)
            Assert.Equal("(1, Nothing)", firstTuple.ToString())
            Assert.Null(model.GetTypeInfo(firstTuple).Type)
            Assert.Equal("C", model.GetTypeInfo(firstTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Narrowing Or ConversionKind.UserDefined, model.GetConversion(firstTuple).Kind)

            Dim secondTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(1)
            Assert.Equal("(2, Nothing)", secondTuple.ToString())
            Assert.Null(model.GetTypeInfo(secondTuple).Type)
            Assert.Equal("System.Nullable(Of C)", model.GetTypeInfo(secondTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, model.GetConversion(secondTuple).Kind)

        End Sub

        <Fact>
        Public Sub DirectCastOnTypelessTupleWithUserConversion()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Structure C
    Shared Sub Main()
        Dim x = DirectCast((1, Nothing), C)
        Dim y = DirectCast((2, Nothing), C?)
    End Sub
    Public Shared Widening Operator CType(ByVal d As (Integer, String)) As C
          Return New C()
    End Operator
End Structure
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics(<errors>
BC30311: Value of type '(Integer, Object)' cannot be converted to 'C'.
        Dim x = DirectCast((1, Nothing), C)
                           ~~~~~~~~~~~~
BC30311: Value of type '(Integer, Object)' cannot be converted to 'C?'.
        Dim y = DirectCast((2, Nothing), C?)
                           ~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim firstTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(0)
            Assert.Equal("(1, Nothing)", firstTuple.ToString())
            Assert.Null(model.GetTypeInfo(firstTuple).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(firstTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(firstTuple).Kind)

            Dim secondTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(1)
            Assert.Equal("(2, Nothing)", secondTuple.ToString())
            Assert.Null(model.GetTypeInfo(secondTuple).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(secondTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(secondTuple).Kind)

        End Sub

        <Fact>
        Public Sub TryCastOnTypelessTupleWithUserConversion()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Structure C
    Shared Sub Main()
        Dim x = TryCast((1, Nothing), C)
        Dim y = TryCast((2, Nothing), C?)
    End Sub
    Public Shared Widening Operator CType(ByVal d As (Integer, String)) As C
          Return New C()
    End Operator
End Structure
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics(<errors>
BC30792: 'TryCast' operand must be reference type, but 'C' is a value type.
        Dim x = TryCast((1, Nothing), C)
                                      ~
BC30792: 'TryCast' operand must be reference type, but 'C?' is a value type.
        Dim y = TryCast((2, Nothing), C?)
                                      ~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim firstTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(0)
            Assert.Equal("(1, Nothing)", firstTuple.ToString())
            Assert.Null(model.GetTypeInfo(firstTuple).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(firstTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(firstTuple).Kind)

            Dim secondTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(1)
            Assert.Equal("(2, Nothing)", secondTuple.ToString())
            Assert.Null(model.GetTypeInfo(secondTuple).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(secondTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(secondTuple).Kind)

        End Sub

        <Fact>
        Public Sub CTypeOnTypelessTupleWithUserConversion()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Structure C
    Shared Sub Main()
        Dim x = CType((1, Nothing), C)
        Dim y = CType((2, Nothing), C?)
    End Sub
    Public Shared Widening Operator CType(ByVal d As (Integer, String)) As C
          Return New C()
    End Operator
End Structure
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics(<errors>
BC30311: Value of type '(Integer, Object)' cannot be converted to 'C?'.
        Dim y = CType((2, Nothing), C?)
                      ~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim firstTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(0)
            Assert.Equal("(1, Nothing)", firstTuple.ToString())
            Assert.Null(model.GetTypeInfo(firstTuple).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(firstTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(firstTuple).Kind)

            Dim secondTuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ElementAt(1)
            Assert.Equal("(2, Nothing)", secondTuple.ToString())
            Assert.Null(model.GetTypeInfo(secondTuple).Type)
            Assert.Equal("(System.Int32, System.Object)", model.GetTypeInfo(secondTuple).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(secondTuple).Kind)

        End Sub

        <Fact>
        Public Sub TupleTargetTypeLambda()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[second
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
        Test1((Function() 7, Function() 8))
        Test2(Function() 7, Function() 8)
    End Sub
    Shared Sub Test1(Of T)(x As (T, T))
        Console.WriteLine("first")
    End Sub
    Shared Sub Test1(x As (Object, Object))
        Console.WriteLine("second")
    End Sub
    Shared Sub Test1(Of T)(x As (Func(Of T), Func(Of T)))
        Console.WriteLine("third")
        Console.WriteLine(x.Item1().ToString())
    End Sub

    Shared Sub Test2(Of T)(x As T, y as T)
        Console.WriteLine("first")
    End Sub
    Shared Sub Test2(x As Object, y as Object)
        Console.WriteLine("second")
    End Sub
    Shared Sub Test2(Of T)(x As Func(Of T), y as Func(Of T))
        Console.WriteLine("third")
    End Sub
End Class
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=
"first
first")

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[System.Nullable`1[System.ValueTuple`2[System.Int32,System.Double]]
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[second
first
first]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[System.Nullable`1[System.ValueTuple`2[System.Int32,System.String]]
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[System.Nullable`1[System.ValueTuple`2[System.Int32,System.String]]
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[first
fourth]]>)

            verifier.VerifyDiagnostics()

        End Sub

        <Fact()>
        <WorkItem(13277, "https://github.com/dotnet/roslyn/issues/13277")>
        <WorkItem(14365, "https://github.com/dotnet/roslyn/issues/14365")>
        Public Sub CreateTupleTypeSymbol_UnderlyingTypeIsError()

            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef, TestReferences.SymbolsTests.netModule.netModule1})

            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim vt2 = comp.CreateErrorTypeSymbol(Nothing, "ValueTuple", 2).Construct(intType, intType)

            Assert.Throws(Of ArgumentException)(Function() comp.CreateTupleTypeSymbol(underlyingType:=vt2))

            Dim csComp = CreateCSharpCompilation("")
            Assert.Throws(Of ArgumentNullException)(Sub() comp.CreateErrorTypeSymbol(Nothing, Nothing, 2))
            Assert.Throws(Of ArgumentException)(Sub() comp.CreateErrorTypeSymbol(Nothing, "a", -1))
            Assert.Throws(Of ArgumentException)(Sub() comp.CreateErrorTypeSymbol(csComp.GlobalNamespace, "a", 1))

            Assert.Throws(Of ArgumentNullException)(Sub() comp.CreateErrorNamespaceSymbol(Nothing, "a"))
            Assert.Throws(Of ArgumentNullException)(Sub() comp.CreateErrorNamespaceSymbol(csComp.GlobalNamespace, Nothing))
            Assert.Throws(Of ArgumentException)(Sub() comp.CreateErrorNamespaceSymbol(csComp.GlobalNamespace, "a"))

            Dim ns = comp.CreateErrorNamespaceSymbol(comp.GlobalNamespace, "a")
            Assert.Equal("a", ns.ToTestDisplayString())
            Assert.False(ns.IsGlobalNamespace)
            Assert.Equal(NamespaceKind.Compilation, ns.NamespaceKind)
            Assert.Same(comp.GlobalNamespace, ns.ContainingSymbol)
            Assert.Same(comp.GlobalNamespace.ContainingAssembly, ns.ContainingAssembly)
            Assert.Same(comp.GlobalNamespace.ContainingModule, ns.ContainingModule)

            ns = comp.CreateErrorNamespaceSymbol(comp.Assembly.GlobalNamespace, "a")
            Assert.Equal("a", ns.ToTestDisplayString())
            Assert.False(ns.IsGlobalNamespace)
            Assert.Equal(NamespaceKind.Assembly, ns.NamespaceKind)
            Assert.Same(comp.Assembly.GlobalNamespace, ns.ContainingSymbol)
            Assert.Same(comp.Assembly.GlobalNamespace.ContainingAssembly, ns.ContainingAssembly)
            Assert.Same(comp.Assembly.GlobalNamespace.ContainingModule, ns.ContainingModule)

            ns = comp.CreateErrorNamespaceSymbol(comp.SourceModule.GlobalNamespace, "a")
            Assert.Equal("a", ns.ToTestDisplayString())
            Assert.False(ns.IsGlobalNamespace)
            Assert.Equal(NamespaceKind.Module, ns.NamespaceKind)
            Assert.Same(comp.SourceModule.GlobalNamespace, ns.ContainingSymbol)
            Assert.Same(comp.SourceModule.GlobalNamespace.ContainingAssembly, ns.ContainingAssembly)
            Assert.Same(comp.SourceModule.GlobalNamespace.ContainingModule, ns.ContainingModule)

            ns = comp.CreateErrorNamespaceSymbol(comp.CreateErrorNamespaceSymbol(comp.GlobalNamespace, "a"), "b")
            Assert.Equal("a.b", ns.ToTestDisplayString())

            ns = comp.CreateErrorNamespaceSymbol(comp.GlobalNamespace, "")
            Assert.Equal("", ns.ToTestDisplayString())
            Assert.False(ns.IsGlobalNamespace)

            vt2 = comp.CreateErrorTypeSymbol(comp.CreateErrorNamespaceSymbol(comp.GlobalNamespace, "System"), "ValueTuple", 2).Construct(intType, intType)
            Assert.Equal("(System.Int32, System.Int32)", comp.CreateTupleTypeSymbol(underlyingType:=vt2).ToTestDisplayString())

            vt2 = comp.CreateErrorTypeSymbol(comp.CreateErrorNamespaceSymbol(comp.Assembly.GlobalNamespace, "System"), "ValueTuple", 2).Construct(intType, intType)
            Assert.Equal("(System.Int32, System.Int32)", comp.CreateTupleTypeSymbol(underlyingType:=vt2).ToTestDisplayString())

            vt2 = comp.CreateErrorTypeSymbol(comp.CreateErrorNamespaceSymbol(comp.SourceModule.GlobalNamespace, "System"), "ValueTuple", 2).Construct(intType, intType)
            Assert.Equal("(System.Int32, System.Int32)", comp.CreateTupleTypeSymbol(underlyingType:=vt2).ToTestDisplayString())
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
 </compilation>, references:=s_valueTupleRefs)

            Dim comp = verifier.Compilation
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim type = nodes.OfType(Of QualifiedNameSyntax)().First()
            Assert.Equal("System.Int32", type.ToString())
            Assert.NotNull(model.GetSymbolInfo(type).Symbol)
            Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString())

        End Sub


        <Fact(Skip:="See bug 16697")>
        <WorkItem(16697, "https://github.com/dotnet/roslyn/issues/16697")>
        Public Sub GetSymbolInfo_01()
            Dim source = "
 Class C
    Shared Sub Main()
         Dim x1 = (Alice:=1, ""hello"")

         Dim Alice = x1.Alice
    End Sub
End Class
 "

            Dim tree = Parse(source, options:=TestOptions.Regular)
            Dim comp = CreateCompilationWithMscorlib40(tree)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim nc = nodes.OfType(Of NameColonEqualsSyntax)().ElementAt(0)

            Dim sym = model.GetSymbolInfo(nc.Name)

            Assert.Equal("Alice", sym.Symbol.Name)
            Assert.Equal(SymbolKind.Field, sym.Symbol.Kind) ' Incorrectly returns Local
            Assert.Equal(nc.Name.GetLocation(), sym.Symbol.Locations(0)) ' Incorrect location
        End Sub

        <Fact>
        <WorkItem(23651, "https://github.com/dotnet/roslyn/issues/23651")>
        Public Sub GetSymbolInfo_WithDuplicateInferredNames()
            Dim source = "
 Class C
    Shared Sub M(Bob As String)
         Dim x1 = (Bob, Bob)
    End Sub
End Class
 "

            Dim tree = Parse(source, options:=TestOptions.Regular)
            Dim comp = CreateCompilationWithMscorlib40(tree)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim tuple = nodes.OfType(Of TupleExpressionSyntax)().Single()
            Dim type = DirectCast(model.GetTypeInfo(tuple).Type, TypeSymbol)
            Assert.True(type.TupleElementNames.IsDefault)
        End Sub

        <Fact>
        Public Sub RetargetTupleErrorType()
            Dim libComp = CreateCompilationWithMscorlib40AndVBRuntime(
 <compilation>
     <file name="a.vb">
Public Class A
     Public Shared Function M() As (Integer, Integer)
        Return (1, 2)
     End Function
End Class
     </file>
 </compilation>, additionalRefs:=s_valueTupleRefs)
            libComp.AssertNoDiagnostics()


            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[102012]]>)

        End Sub

        <Fact>
        Public Sub CaseSensitivity002()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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

        ' The NonNullTypes context for nested tuple types is using a dummy rather than actual context from surrounding code.
        ' This does not affect `IsNullable`, but it affects `IsAnnotatedWithNonNullTypesContext`, which is used in comparisons.
        ' So when we copy modifiers (re-applying nullability information, including actual NonNullTypes context), we make the comparison fail.
        ' I think the solution is to never use a dummy context, even for value types.
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
        public (int Alice, int Bob) goo = (2, 3);

        public (int Alice, int Bob) Bar() => (4, 5);

        public (int Alice, int Bob) Baz => (6, 7);

    }

    public class Class2
    {
        public (int Alice, int q, int w, int e, int f, int g, int h, int j, int Bob) goo = SetBob(11);

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
        System.Console.WriteLine(x.goo.Alice)
        System.Console.WriteLine(x.goo.Bob)
        System.Console.WriteLine(x.Bar.Alice)
        System.Console.WriteLine(x.Bar.Bob)
        System.Console.WriteLine(x.Baz.Alice)
        System.Console.WriteLine(x.Baz.Bob)

        Dim y As New ClassLibrary1.Class2
        System.Console.WriteLine(y.goo.Alice)
        System.Console.WriteLine(y.goo.Bob)
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
        Public goo As (Alice As Integer, Bob As Integer) = (2, 3)

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
        Public goo As (Alice As Integer, q As Integer, w As Integer, e As Integer, f As Integer, g As Integer, h As Integer, j As Integer, Bob As Integer) = SetBob(11)

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
        System.Console.WriteLine(x.goo.Alice)
        System.Console.WriteLine(x.goo.Bob)
        System.Console.WriteLine(x.Bar.Alice)
        System.Console.WriteLine(x.Bar.Bob)
        System.Console.WriteLine(x.Baz.Alice)
        System.Console.WriteLine(x.Baz.Bob)

        Dim y As New ClassLibrary1.Class2
        System.Console.WriteLine(y.goo.Alice)
        System.Console.WriteLine(y.goo.Bob)
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
        public (int Alice, (int Alice, int Bob) Bob) goo = (2, (2, 3));

        public ((int Alice, int Bob)[] Alice, int Bob) Bar() => (new(int, int)[] { (4, 5) }, 5);

        public (int Alice, List<(int Alice, int Bob)?> Bob) Baz => (6, new List<(int Alice, int Bob)?>() { (8, 9) });

        public static event Action<(int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7, (int Alice, int Bob) Bob)> goo1;

        public static void raise()
        {
            goo1((0, 1, 2, 3, 4, 5, 6, 7, (8, 42)));
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
        System.Console.WriteLine(x.goo.Bob.Bob)
        System.Console.WriteLine(x.goo.Item2.Item2)
        System.Console.WriteLine(x.Bar.Alice(0).Bob)
        System.Console.WriteLine(x.Bar.Item1(0).Item2)
        System.Console.WriteLine(x.Baz.Bob(0).Value)
        System.Console.WriteLine(x.Baz.Item2(0).Value)

        AddHandler ClassLibrary1.Class1.goo1, Sub(p)
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
        Public goo As (Alice As Integer, Bob As (Alice As Integer, Bob As Integer)) = (2, (2, 3))

        Public Function Bar() As (Alice As (Alice As Integer, Bob As Integer)(), Bob As Integer)
            Return (New(Integer, Integer)() {(4, 5)}, 5)
        End Function

        Public ReadOnly Property Baz As (Alice As Integer, Bob As List(Of (Alice As Integer, Bob As Integer) ?))
            Get
                Return (6, New List(Of (Alice As Integer, Bob As Integer) ?)() From {(8, 9)})
            End Get
        End Property

        Public Shared Event goo1 As Action(Of (i0 As Integer, i1 As Integer, i2 As Integer, i3 As Integer, i4 As Integer, i5 As Integer, i6 As Integer, i7 As Integer, Bob As (Alice As Integer, Bob As Integer)))

        Public Shared Sub raise()
            RaiseEvent goo1((0, 1, 2, 3, 4, 5, 6, 7, (8, 42)))
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
        System.Console.WriteLine(x.goo.Bob.Bob)
        System.Console.WriteLine(x.goo.Item2.Item2)
        System.Console.WriteLine(x.Bar.Alice(0).Bob)
        System.Console.WriteLine(x.Bar.Item1(0).Item2)
        System.Console.WriteLine(x.Baz.Bob(0).Value)
        System.Console.WriteLine(x.Baz.Item2(0).Value)

        AddHandler ClassLibrary1.Class1.goo1, Sub(p)
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
        public (int Alice, int alice) goo = (2, 3);
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
        System.Console.WriteLine(x.goo.Item1)
        System.Console.WriteLine(x.goo.Item2)
        System.Console.WriteLine(x.goo.Alice)
        System.Console.WriteLine(x.goo.alice)

        Dim f = x.goo
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
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.goo.Alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer)").WithLocation(8, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.goo.alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer)").WithLocation(9, 34),
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
        public (int Alice, int alice, int) goo = (2, 3, 4);
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
        System.Console.WriteLine(x.goo.Item1)
        System.Console.WriteLine(x.goo.Item2)
        System.Console.WriteLine(x.goo.Item3)
        System.Console.WriteLine(x.goo.Alice)
        System.Console.WriteLine(x.goo.alice)

        Dim f = x.goo
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
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.goo.Alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer, Integer)").WithLocation(9, 34),
    Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "x.goo.alice").WithArguments("Alice", "structure", "(Alice As Integer, alice As Integer, Integer)").WithLocation(10, 34),
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
            Assert.False(DirectCast(validFieldWithAttribute.Type, INamedTypeSymbol).IsSerializable)

            Dim tooFewNames = c.GetMember(Of FieldSymbol)("TooFewNames")
            Assert.True(tooFewNames.Type.IsErrorType())
            Assert.IsType(Of UnsupportedMetadataTypeSymbol)(tooFewNames.Type)
            Assert.False(DirectCast(tooFewNames.Type, INamedTypeSymbol).IsSerializable)

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
            Assert.Equal("ValueTuple(Of (e1 As System.Int32, System.Int32))", partialParamType.ToTestDisplayString())

            Dim allNullNamesMethod = c.GetMember(Of MethodSymbol)("AllNullNamesMethod")
            Dim allNullParamType = allNullNamesMethod.Parameters.Single().Type
            Assert.False(allNullParamType.IsErrorType())
            Assert.True(allNullParamType.IsTupleType)
            Assert.Equal("ValueTuple(Of (System.Int32, System.Int32))", allNullParamType.ToTestDisplayString())
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
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:="1 hello 3")

        End Sub

        <Fact()>
        <WorkItem(13705, "https://github.com/dotnet/roslyn/issues/13705")>
        Public Sub TupleCoVariance()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I(Of Out T)
    Function M() As System.ValueTuple(Of Integer, T)
End Interface
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I(Of Out T)
    Function M() As (Integer, T)
End Interface
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I(Of In T)
    Sub M(x As (Boolean, T))
End Interface
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, w)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, )]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(w, w)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, (w, e))]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, q, q, q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, q, q, q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, q, q, q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, q, , , )]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[w]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, , q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(q, q, q, q, q, q, , q)]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[()]]>)

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
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[q
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
    </compilation>, useLatestFramework:=True, references:=s_valueTupleRefs, expectedOutput:="5")

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
</compilation>, references:=s_valueTupleRefs,
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
            Dim [lib] = CreateCompilationWithMscorlib40AndVBRuntime(tuple, options:=TestOptions.ReleaseDll)
            [lib].VerifyEmitDiagnostics()

            Dim consumer1 = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].ToMetadataReference()})
            CompileAndVerify(consumer1, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Dim consumer2 = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].EmitToImageReference()})
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
            Dim [lib] = CreateCompilationWithMscorlib40AndVBRuntime(tuple, options:=TestOptions.ReleaseDll)
            [lib].VerifyEmitDiagnostics()

            Dim consumer1 = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].ToMetadataReference()})
            CompileAndVerify(consumer1, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Dim consumer2 = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseExe, additionalRefs:={[lib].EmitToImageReference()})
            CompileAndVerify(consumer2, expectedOutput:=expectedOutput).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TupleConversion01()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                                   ~~~~
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                         ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                               ~~~~
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                                   ~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleConversion01_StrictOn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x1 As (a As Integer, b As Integer) = DirectCast((e:=1, f:=2), (c As Long, d As Long))
                                                                   ~~~~
BC30512: Option Strict On disallows implicit conversions from '(c As Integer, d As Integer)' to '(a As Short, b As Short)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                         ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(c As Integer, d As Integer)'.
        Dim x2 As (a As Short, b As Short) = DirectCast((e:=1, f:=2), (c As Integer, d As Integer))
                                                               ~~~~
BC30512: Option Strict On disallows implicit conversions from '(c As Long, d As Long)' to '(a As Integer, b As Integer)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                             ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(c As Long, d As Long)'.
        Dim x3 As (a As Integer, b As Integer) = DirectCast((e:=1, f:="qq"), (c As Long, d As Long))
                                                                   ~~~~~~~
</errors>)

        End Sub

        <Fact>
        <WorkItem(11288, "https://github.com/dotnet/roslyn/issues/11288")>
        Public Sub TupleConversion02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim e = node.Arguments(0).Expression
            Assert.Equal("1", e.ToString())
            typeInfo = model.GetTypeInfo(e)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int16", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(e).Kind)

            Dim f = node.Arguments(1).Expression
            Assert.Equal("""hello""", f.ToString())
            typeInfo = model.GetTypeInfo(f)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(f).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType01_StrictOn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType01insource()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Assert.Equal("System.Nullable(Of (c As System.Int16, d As System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (c As System.Int16, d As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType01insourceImplicit()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            CompileAndVerify(comp)

        End Sub

        <Fact>
        Public Sub TupleConvertedType02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim e = node.Arguments(0).Expression
            Assert.Equal("1", e.ToString())
            typeInfo = model.GetTypeInfo(e)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int16", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(e).Kind)

            Dim f = node.Arguments(1).Expression
            Assert.Equal("""hello""", f.ToString())
            typeInfo = model.GetTypeInfo(f)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(f).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource00()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Assert.Equal("DirectCast((e:=1, f:=""hello""), (c As Short, d As String))", node.Parent.ToString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource00_StrictOn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (a As System.Int16, b As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int16, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType02insource01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of ParenthesizedExpressionSyntax)().Single()
            Assert.Equal("(x)", x.ToString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).Type.ToTestDisplayString())
            Assert.Equal("(e As System.Int32, f As System.String)", model.GetTypeInfo(x).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(x).Kind)

        End Sub

        <Fact>
        Public Sub TupleConvertedType03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int32, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType03insource()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(node).Kind)

            Assert.Equal("DirectCast((e:=1, f:=""hello""), (c As Integer, d As String)?)", node.Parent.ToString())
            Assert.Equal("System.Nullable(Of (c As System.Int32, d As System.String))", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of (c As System.Int32, d As System.String))", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As System.Nullable(Of (a As System.Int32, b As System.String))", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType04()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim e = node.Arguments(0).Expression
            Assert.Equal("1", e.ToString())
            Dim typeInfo = model.GetTypeInfo(e)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int16", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(e).Kind)

            Dim f = node.Arguments(1).Expression
            Assert.Equal("""hello""", f.ToString())
            typeInfo = model.GetTypeInfo(f)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(f).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedType06insource()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeNull01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeNull01insource()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.InvolvesNarrowingFromNumericConstant, model.GetConversion(node).Kind)

            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString())
            Assert.Equal("(c As System.Int16, d As System.String)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node.Parent).Kind)

            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x As (a As System.Int16, b As System.String)", model.GetDeclaredSymbol(x).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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
BC41009: The tuple element name 'e' is ignored because a different name or no name is specified by the target type '(a As Short, b As String)'.
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
                                              ~~~~
BC41009: The tuple element name 'f' is ignored because a different name or no name is specified by the target type '(a As Short, b As String)'.
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
                                                    ~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'C.C1' to 'String'.
        Dim x As (a As Short, b As String) = (e:=1, f:=New C1("qq"))
                                                       ~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleConvertedTypeUDC01insource()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
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

        <Fact>
        Public Sub TupleConvertedTypeUDC07()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On
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

        Public Shared Widening Operator CType(ByVal arg As (Byte, String)) As C1
            System.Console.Write("C1 ")
            Return New C1(arg)
        End Operator
    End Class
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30512: Option Strict On disallows implicit conversions from '(Integer, String)' to 'C.C1'.
        Dim x As C1 = M1()
                      ~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub Inference01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test((Nothing, Nothing))
        Test((1, 1))
        Test((Function() 7, Function() 8), 2)
    End Sub

    Shared Sub Test(Of T)(x As (T, T))
        System.Console.WriteLine("first")
    End Sub

    Shared Sub Test(x As (Object, Object))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test(Of T)(x As (System.Func(Of T), System.Func(Of T)), y As T)
        System.Console.WriteLine("third")
        System.Console.WriteLine(x.Item1().ToString())
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
second
first
third
7
")

        End Sub

        <Fact>
        Public Sub Inference02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test1((Function() 7, Function() 8))
        Test2(Function() 7, Function() 8)
        Test3((Function() 7, Function() 8))
    End Sub

    Shared Sub Test1(Of T)(x As (T, T))
        System.Console.WriteLine("first")
    End Sub

    Shared Sub Test1(x As (Object, Object))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test1(Of T)(x As (System.Func(Of T), System.Func(Of T)))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test2(Of T)(x As T, y As T)
        System.Console.WriteLine("first")
    End Sub

    Shared Sub Test2(x As Object, y As Object)
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test2(Of T)(x As System.Func(Of T), y As System.Func(Of T))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test3(Of T)(x As (T, T)?)
        System.Console.WriteLine("first")
    End Sub

    Shared Sub Test3(x As (Object, Object)?)
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test3(Of T)(x As (System.Func(Of T), System.Func(Of T))?)
        System.Console.WriteLine("third")
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
first
first
first")
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test1((Function() 7, Function() 8))
        Test2(Function() 7, Function() 8)
        Test3((Function() 7, Function() 8))
    End Sub

    Shared Sub Test1(x As (System.Func(Of Integer), System.Func(Of Integer)))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test1(x As (System.Func(Of Integer, Integer), System.Func(Of Integer, Integer)))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test2(x As System.Func(Of Integer), y As System.Func(Of Integer))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test2(x As System.Func(Of Integer, Integer), y As System.Func(Of Integer, Integer))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test3(x As (System.Func(Of Integer), System.Func(Of Integer))?)
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test3(x As (System.Func(Of Integer, Integer), System.Func(Of Integer, Integer))?)
        System.Console.WriteLine("third")
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
second
second
second")
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim int = 1
        Dim a = Function(x as Integer) x
        Test1(a, int)
        Test2((a, int))
        Test3((a, int))

        Dim b = (a, int)
        Test2(b)
        Test3(b)
    End Sub

    Shared Sub Test1(x As System.Action(Of Integer), y As Integer)
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test1(x As System.Func(Of Integer, Integer), y As Integer)
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test2(x As (System.Action(Of Integer), Integer))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test2(x As (System.Func(Of Integer, Integer), Integer))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test3(x As (System.Action(Of Integer), Integer)?)
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test3(x As (System.Func(Of Integer, Integer), Integer)?)
        System.Console.WriteLine("third")
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
third
third
third
third
third")
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim x00 As (Integer, Func(Of Integer)) = (int, Function() int)
        Dim x01 As (Integer, Func(Of Long)) = (int, Function() int)
        Dim x02 As (Integer, Action) = (int, Function() int)
        Dim x03 As (Integer, Object) = (int, Function() int)
        Dim x04 As (Integer, Func(Of Short)) = (int, Function() int)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ToArray()

            AssertConversions(model, nodes(0), ConversionKind.WideningTuple, ConversionKind.Identity, ConversionKind.Widening Or ConversionKind.Lambda)
            AssertConversions(model, nodes(1), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWidening,
                              ConversionKind.Identity,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWidening)
            AssertConversions(model, nodes(2), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs,
                              ConversionKind.Identity,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs)
            AssertConversions(model, nodes(3), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda,
                              ConversionKind.Identity,
                              ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda)
            AssertConversions(model, nodes(4), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Identity,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelNarrowing)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_04()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim x00 As (Func(Of Integer), Integer) = (Function() int, int)
        Dim x01 As (Func(Of Long), Integer) = (Function() int, int)
        Dim x02 As (Action, Integer) = (Function() int, int)
        Dim x03 As (Object, Integer) = (Function() int, int)
        Dim x04 As (Func(Of Short), Integer) = (Function() int, int)
        Dim x05 As (Func(Of Short), Func(Of Long)) = (Function() int, Function() int)
        Dim x06 As (Func(Of Long), Func(Of Short)) = (Function() int, Function() int)
        Dim x07 As (Short, (Func(Of Long), Func(Of Short))) = (int, (Function() int, Function() int))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ToArray()

            AssertConversions(model, nodes(0), ConversionKind.WideningTuple, ConversionKind.Widening Or ConversionKind.Lambda, ConversionKind.Identity)
            AssertConversions(model, nodes(1), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWidening,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWidening,
                              ConversionKind.Identity)
            AssertConversions(model, nodes(2), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs,
                              ConversionKind.Identity)
            AssertConversions(model, nodes(3), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda,
                              ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda,
                              ConversionKind.Identity)
            AssertConversions(model, nodes(4), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Identity)
            AssertConversions(model, nodes(5), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWidening)
            AssertConversions(model, nodes(6), ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWidening,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelNarrowing)
            AssertConversions(model, nodes(7), ConversionKind.NarrowingTuple Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.NarrowingNumeric,
                              ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelNarrowing)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_05()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim x00 As (Integer, Func(Of Integer))? = (int, Function() int)
        Dim x01 As (Short, Func(Of Long))? = (int, Function() int)
        Dim x02 As (Integer, Action)? = (int, Function() int)
        Dim x03 As (Integer, Object)? = (int, Function() int)
        Dim x04 As (Integer, Func(Of Short))? = (int, Function() int)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().ToArray()

            AssertConversions(model, nodes(0), ConversionKind.WideningNullableTuple, ConversionKind.Identity, ConversionKind.Widening Or ConversionKind.Lambda)
            AssertConversions(model, nodes(1), ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelWidening,
                              ConversionKind.NarrowingNumeric,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWidening)
            AssertConversions(model, nodes(2), ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs,
                              ConversionKind.Identity,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs)
            AssertConversions(model, nodes(3), ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda,
                              ConversionKind.Identity,
                              ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda)
            AssertConversions(model, nodes(4), ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelNarrowing,
                              ConversionKind.Identity,
                              ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelNarrowing)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_06()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim t = (int, Function() int)
        Dim x00 As (Integer, Func(Of Integer)) = t
        Dim x01 As (Integer, Func(Of Long)) = t
        Dim x02 As (Integer, Action) = t
        Dim x03 As (Integer, Object) = t
        Dim x04 As (Integer, Func(Of Short)) = t
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "t").ToArray()

            Assert.Equal(ConversionKind.WideningTuple, model.GetConversion(nodes(0)).Kind)
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWidening, model.GetConversion(nodes(1)).Kind)
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs, model.GetConversion(nodes(2)).Kind)
            Assert.Equal(ConversionKind.WideningTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, model.GetConversion(nodes(3)).Kind)
            Assert.Equal(ConversionKind.NarrowingTuple Or ConversionKind.DelegateRelaxationLevelNarrowing, model.GetConversion(nodes(4)).Kind)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_07()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim t = (int, Function() int)
        Dim x00 As (Integer, Func(Of Integer))? = t
        Dim x01 As (Integer, Func(Of Long))? = t
        Dim x02 As (Integer, Action)? = t
        Dim x03 As (Integer, Object)? = t
        Dim x04 As (Integer, Func(Of Short))? = t
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "t").ToArray()

            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(nodes(0)).Kind)
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWidening, model.GetConversion(nodes(1)).Kind)
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs, model.GetConversion(nodes(2)).Kind)
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, model.GetConversion(nodes(3)).Kind)
            Assert.Equal(ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelNarrowing, model.GetConversion(nodes(4)).Kind)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_08()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim t? = (int, Function() int)
        Dim x00 As (Integer, Func(Of Integer))? = t
        Dim x01 As (Integer, Func(Of Long))? = t
        Dim x02 As (Integer, Action)? = t
        Dim x03 As (Integer, Object)? = t
        Dim x04 As (Integer, Func(Of Short))? = t
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "t").ToArray()

            Assert.Equal(ConversionKind.WideningNullableTuple, model.GetConversion(nodes(0)).Kind)
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWidening, model.GetConversion(nodes(1)).Kind)
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs, model.GetConversion(nodes(2)).Kind)
            Assert.Equal(ConversionKind.WideningNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, model.GetConversion(nodes(3)).Kind)
            Assert.Equal(ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelNarrowing, model.GetConversion(nodes(4)).Kind)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationLevel_09()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Public Class C

    Shared Sub Main()
        Dim int as integer = 1
        Dim t? = (int, Function() int)
        Dim x00 As (Integer, Func(Of Integer)) = t
        Dim x01 As (Integer, Func(Of Long)) = t
        Dim x02 As (Integer, Action) = t
        Dim x03 As (Integer, Object) = t
        Dim x04 As (Integer, Func(Of Short)) = t
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe.WithOverflowChecks(False), additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "t").ToArray()

            Assert.Equal(ConversionKind.NarrowingNullableTuple, model.GetConversion(nodes(0)).Kind)
            Assert.Equal(ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelWidening, model.GetConversion(nodes(1)).Kind)
            Assert.Equal(ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs, model.GetConversion(nodes(2)).Kind)
            Assert.Equal(ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, model.GetConversion(nodes(3)).Kind)
            Assert.Equal(ConversionKind.NarrowingNullableTuple Or ConversionKind.DelegateRelaxationLevelNarrowing, model.GetConversion(nodes(4)).Kind)
        End Sub

        <Fact>
        Public Sub AnonymousDelegate_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim int = 1
        Dim a = Function(x as Integer) x
        Test1(a)
        Test2((a, int))
        Test3((a, int))

        Dim b = (a, int)
        Test2(b)
        Test3(b)
    End Sub

    Shared Sub Test1(x As Object)
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test2(x As (Object, Integer))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test3(x As (Object, Integer)?)
        System.Console.WriteLine("second")
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
second
second
second
second
second")
        End Sub

        <Fact>
        <WorkItem(14529, "https://github.com/dotnet/roslyn/issues/14529")>
        <WorkItem(14530, "https://github.com/dotnet/roslyn/issues/14530")>
        Public Sub AnonymousDelegate_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim int = 1
        Dim a = Function(x as Integer) x
        Test1(a)
        Test2((a, int))
        Test3((a, int)) 

        Dim b = (a, int)
        Test2(b)
        Test3(b) 

        Test4({a})
    End Sub

    Shared Sub Test1(Of T)(x As System.Func(Of T, T))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test2(Of T)(x As (System.Func(Of T, T), Integer))
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test3(Of T)(x As (System.Func(Of T, T), Integer)?)
        System.Console.WriteLine("third")
    End Sub

    Shared Sub Test4(Of T)(x As System.Func(Of T, T)())
        System.Console.WriteLine("third")
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
third
third
third
third
third
third")
        End Sub

        <Fact>
        Public Sub UserDefinedConversions_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C
    Shared Sub Main()
        Dim int = 1
        Dim tuple = (int, int)
        Dim a as (A, Integer) = tuple
        Dim b as (A, Integer) = (int, int)
        Dim c as (A, Integer)? = tuple
        Dim d as (A, Integer)? = (int, int)

        System.Console.WriteLine(a)
        System.Console.WriteLine(b)
        System.Console.WriteLine(c)
        System.Console.WriteLine(d)
    End Sub
End Class

Class A
    Public Shared Widening Operator CType(val As String) As A
        System.Console.WriteLine(val)
        Return New A()
    End Operator
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
1
1
1
1
(A, 1)
(A, 1)
(A, 1)
(A, 1)")
        End Sub

        <Fact>
        Public Sub UserDefinedConversions_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C
    Shared Sub Main()
        Dim int = 1
        Dim val as new B()
        Dim tuple = (val, int)
        Dim a as (Integer, Integer) = tuple
        Dim b as (Integer, Integer) = (val, int)
        Dim c as (Integer, Integer)? = tuple
        Dim d as (Integer, Integer)? = (val, int)

        System.Console.WriteLine(a)
        System.Console.WriteLine(b)
        System.Console.WriteLine(c)
        System.Console.WriteLine(d)
    End Sub
End Class

Class B
    Public Shared Widening Operator CType(val As B) As String
        System.Console.WriteLine(val Is Nothing)
        Return "2"
    End Operator
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
False
False
False
False
(2, 1)
(2, 1)
(2, 1)
(2, 1)")
        End Sub

        <Fact>
        <WorkItem(14530, "https://github.com/dotnet/roslyn/issues/14530")>
        Public Sub UserDefinedConversions_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class C
    Shared Sub Main()
        Dim int = 1
        Dim ad = Function() 2
        Dim tuple = (ad, int)
        Dim a as (A, Integer) = tuple
        Dim b as (A, Integer) = (ad, int)
        Dim c as (A, Integer)? = tuple
        Dim d as (A, Integer)? = (ad, int)

        System.Console.WriteLine(a)
        System.Console.WriteLine(b)
        System.Console.WriteLine(c)
        System.Console.WriteLine(d)
    End Sub
End Class

Class A
    Public Shared Widening Operator CType(val As System.Func(Of Integer, Integer)) As A
        System.Console.WriteLine(val)
        Return New A()
    End Operator
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
System.Func`2[System.Int32,System.Int32]
System.Func`2[System.Int32,System.Int32]
System.Func`2[System.Int32,System.Int32]
System.Func`2[System.Int32,System.Int32]
(A, 1)
(A, 1)
(A, 1)
(A, 1)")
        End Sub

        <Fact>
        Public Sub Inference02_Addressof()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Function M() As Integer
        Return 7
    End Function

    Shared Sub Main()
        Test((AddressOf M, AddressOf M))
    End Sub

    Shared Sub Test(Of T)(x As (T, T))
        System.Console.WriteLine("first")
    End Sub

    Shared Sub Test(x As (Object, Object))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test(Of T)(x As (System.Func(Of T), System.Func(Of T)))
        System.Console.WriteLine("third")
        System.Console.WriteLine(x.Item1().ToString())
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
third
7
")

        End Sub

        <Fact>
        Public Sub Inference03_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test((Function(x) x, Function(x) x))
    End Sub

    Shared Sub Test(Of T)(x As (T, T))
    End Sub

    Shared Sub Test(x As (Object, Object))
    End Sub

    Shared Sub Test(Of T)(x As (System.Func(Of Integer, T), System.Func(Of T, T)))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'Test' is most specific for these arguments:
    'Public Shared Sub Test(Of <generated method>)(x As (<generated method>, <generated method>))': Not most specific.
    'Public Shared Sub Test(Of Integer)(x As (Func(Of Integer, Integer), Func(Of Integer, Integer)))': Not most specific.
        Test((Function(x) x, Function(x) x))
        ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Inference03_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test((Function(x) x, Function(x) x))
    End Sub

    Shared Sub Test(x As (Object, Object))
        System.Console.WriteLine("second")
    End Sub

    Shared Sub Test(Of T)(x As (System.Func(Of Integer, T), System.Func(Of T, T)))
        System.Console.WriteLine("third")
        System.Console.WriteLine(x.Item1(5).ToString())
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
third
5
")

        End Sub

        <Fact>
        Public Sub Inference03_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test((Function(x) x, Function(x) x))
    End Sub

    Shared Sub Test(Of T)(x As T, y As T)
        System.Console.WriteLine("first")
    End Sub

    Shared Sub Test(x As (Object, Object))
        System.Console.WriteLine("second")
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
second
")

        End Sub

        <Fact>
        Public Sub Inference05()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test((Function(x) x.x, Function(x) x.Item2))
        Test((Function(x) x.bob, Function(x) x.Item1))
    End Sub

    Shared Sub Test(Of T)(x As (f1 As Func(Of (x As Byte, y As Byte), T), f2 As Func(Of (Integer, Integer), T)))
        Console.WriteLine("first")
        Console.WriteLine(x.f1((2, 3)).ToString())
        Console.WriteLine(x.f2((2, 3)).ToString())
    End Sub

    Shared Sub Test(Of T)(x As (f1 As Func(Of (alice As Integer, bob As Integer), T), f2 As Func(Of (Integer, Integer), T)))
        Console.WriteLine("second")
        Console.WriteLine(x.f1((4, 5)).ToString())
        Console.WriteLine(x.f2((4, 5)).ToString())
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="first
2
3
second
5
4
")

        End Sub

        <Fact>
        Public Sub Inference08()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test1((a:=1, b:=2), (c:=3, d:=4))
        Test2((a:=1, b:=2), (c:=3, d:=4), Function(t) t.Item2)
        Test2((a:=1, b:=2), (a:=3, b:=4), Function(t) t.a)
        Test2((a:=1, b:=2), (c:=3, d:=4), Function(t) t.a)
    End Sub

    Shared Sub Test1(Of T)(x As T, y As T)
        Console.WriteLine("test1")
        Console.WriteLine(x)
    End Sub

    Shared Sub Test2(Of T)(x As T, y As T, f As Func(Of T, Integer))
        Console.WriteLine("test2_1")
        Console.WriteLine(f(x))
    End Sub

    Shared Sub Test2(Of T)(x As T, y As Object, f As Func(Of T, Integer))
        Console.WriteLine("test2_2")
        Console.WriteLine(f(x))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
test1
(1, 2)
test2_1
2
test2_1
1
test2_2
1
")

        End Sub

        <Fact>
        Public Sub Inference08t()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim ab = (a:=1, b:=2)
        Dim cd = (c:=3, d:=4)

        Test1(ab, cd)
        Test2(ab, cd, Function(t) t.Item2)
        Test2(ab, ab, Function(t) t.a)
        Test2(ab, cd, Function(t) t.a)
    End Sub

    Shared Sub Test1(Of T)(x As T, y As T)
        Console.WriteLine("test1")
        Console.WriteLine(x)
    End Sub

    Shared Sub Test2(Of T)(x As T, y As T, f As Func(Of T, Integer))
        Console.WriteLine("test2_1")
        Console.WriteLine(f(x))
    End Sub

    Shared Sub Test2(Of T)(x As T, y As Object, f As Func(Of T, Integer))
        Console.WriteLine("test2_2")
        Console.WriteLine(f(x))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
test1
(1, 2)
test2_1
2
test2_1
1
test2_2
1
")

        End Sub

        <Fact>
        Public Sub Inference09()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test1((a:=1, b:=2), DirectCast(1, ValueType))
    End Sub

    Shared Sub Test1(Of T)(x As T, y As T)
        Console.Write(GetType(T))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="System.ValueType")

        End Sub

        <Fact>
        Public Sub Inference10()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim t = (a:=1, b:=2)
        Test1(t, DirectCast(1, ValueType))
    End Sub

    Shared Sub Test1(Of T)(ByRef x As T, y As T)
        Console.Write(GetType(T))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC36651: Data type(s) of the type parameter(s) in method 'Public Shared Sub Test1(Of T)(ByRef x As T, y As T)' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        Test1(t, DirectCast(1, ValueType))
        ~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub Inference11()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim ab = (a:=1, b:=2)
        Dim cd = (c:=1, d:=2)

        Test3(ab, cd)

        Test1(ab, cd)
        Test2(ab, cd)
    End Sub

    Shared Sub Test1(Of T)(ByRef x As T, y As T)
        Console.Write(GetType(T))
    End Sub

    Shared Sub Test2(Of T)(x As T, ByRef y As T)
        Console.Write(GetType(T))
    End Sub

    Shared Sub Test3(Of T)(ByRef x As T, ByRef y As T)
        Console.Write(GetType(T))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
</errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim test3 = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().First()

            Assert.Equal("Sub C.Test3(Of (System.Int32, System.Int32))(ByRef x As (System.Int32, System.Int32), ByRef y As (System.Int32, System.Int32))",
                         model.GetSymbolInfo(test3).Symbol.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub Inference12()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=DirectCast(1, Object)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(c:=1, d:=2)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(1, 2)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(a:=1, b:=2)))
    End Sub

    Shared Sub Test1(Of T, U)(x As (T, U), y As (T, U))
        Console.WriteLine(GetType(U))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
System.Object
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
")

        End Sub

        <Fact>
        <WorkItem(14152, "https://github.com/dotnet/roslyn/issues/14152")>
        Public Sub Inference13()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=DirectCast(1, Object)))
        Test1(Nullable((a:=1, b:=(a:=1, b:=2))), (a:=1, b:=DirectCast(1, Object)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(c:=1, d:=2)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(1, 2)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(a:=1, b:=2)))
    End Sub

    Shared Function Nullable(Of T as structure)(x as T) as T?
        return x
    End Function

    Shared Sub Test1(Of T, U)(x As (T, U)?, y As (T, U))
        Console.WriteLine(GetType(U))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
System.Object
System.Object
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
")
        End Sub

        <Fact>
        <WorkItem(22329, "https://github.com/dotnet/roslyn/issues/22329")>
        <WorkItem(14152, "https://github.com/dotnet/roslyn/issues/14152")>
        Public Sub Inference13a()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test2(Nullable((a:=1, b:=(a:=1, b:=2))), (a:=1, b:=DirectCast(1, Object)))
        Test2((a:=1, b:=(a:=1, b:=2)), Nullable((a:=1, b:=DirectCast((a:=1, b:=2), Object))))
    End Sub

    Shared Function Nullable(Of T as structure)(x as T) as T?
        return x
    End Function

    Shared Sub Test2(Of T, U)(x As (T, U), y As (T, U))
        Console.WriteLine(GetType(U))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            AssertTheseDiagnostics(comp,
<expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Sub Test2(Of T, U)(x As (T, U), y As (T, U))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2(Nullable((a:=1, b:=(a:=1, b:=2))), (a:=1, b:=DirectCast(1, Object)))
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Sub Test2(Of T, U)(x As (T, U), y As (T, U))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2((a:=1, b:=(a:=1, b:=2)), Nullable((a:=1, b:=DirectCast((a:=1, b:=2), Object))))
        ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Inference14()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(c:=1, d:=2)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(1, 2)))
        Test1((a:=1, b:=(a:=1, b:=2)), (a:=1, b:=(a:=1, b:=2)))
    End Sub

    Shared Sub Test1(Of T, U As Structure)(x As (T, U)?, y As (T, U)?)
        Console.WriteLine(GetType(U))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
")
            ' In C#, there are errors because names matter during best type inference
            ' This should get fixed after issue https://github.com/dotnet/roslyn/issues/13938 is fixed

        End Sub

        <Fact>
        Public Sub Inference15()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Test1((a:="1", b:=Nothing), (a:=Nothing, b:="w"), Function(x) x.z)
    End Sub

    Shared Sub Test1(Of T, U)(x As (T, U), y As (T, U), f As Func(Of (x As T, z As U), T))
        Console.WriteLine(GetType(U))
        Console.WriteLine(f(y))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
System.String
w
")

        End Sub

        <Fact>
        Public Sub Inference16()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim x = (1, 2, 3)
        Test(x)

        Dim x1 = (1, 2, CType(3, Long))
        Test(x1)

        Dim x2 = (1, DirectCast(2, Object), CType(3, Long))
        Test(x2)
    End Sub

    Shared Sub Test(Of T)(x As (T, T, T))
        Console.WriteLine(GetType(T))
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
System.Int32
System.Int64
System.Object
")

        End Sub

        <Fact()>
        Public Sub Constraints_01()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System
    Public Structure ValueTuple(Of T1 As Class, T2)   
        Sub New(_1 As T1, _2 As T2)       
        End Sub
    End Structure
End Namespace

Class C
    Sub M(p As (Integer, Integer))
        Dim t0 = (1, 2)
        Dim t1 As (Integer, Integer) = t0
    End Sub
End Class
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T1'.
    Sub M(p As (Integer, Integer))
          ~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T1'.
        Dim t0 = (1, 2)
                  ~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T1'.
        Dim t1 As (Integer, Integer) = t0
                   ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub Constraints_02()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class C
    Sub M(p As (Integer, ArgIterator), q As ValueTuple(Of Integer, ArgIterator))
        Dim t0 As (Integer, ArgIterator) = p
        Dim t1 = (1, New ArgIterator())
        Dim t2 = New ValueTuple(Of Integer, ArgIterator)(1, Nothing)
        Dim t3 As ValueTuple(Of Integer, ArgIterator) = t2
    End Sub
End Class
]]></file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Sub M(p As (Integer, ArgIterator), q As ValueTuple(Of Integer, ArgIterator))
          ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Sub M(p As (Integer, ArgIterator), q As ValueTuple(Of Integer, ArgIterator))
                                       ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t0 As (Integer, ArgIterator) = p
                            ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t1 = (1, New ArgIterator())
                     ~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t2 = New ValueTuple(Of Integer, ArgIterator)(1, Nothing)
                                            ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t3 As ValueTuple(Of Integer, ArgIterator) = t2
                                         ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub Constraints_03()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Namespace System
    Public Structure ValueTuple(Of T1, T2 As Class)   
        Sub New(_1 As T1, _2 As T2)       
        End Sub
    End Structure
End Namespace
Class C(Of T)
    Dim field As List(Of (T, T))
    Function M(Of U)(x As U) As (U, U)
        Dim t0 = New C(Of Integer)()
        Dim t1 = M(1)
        Return (Nothing, Nothing)
    End Function
End Class
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC32106: Type argument 'T' does not satisfy the 'Class' constraint for type parameter 'T2'.
    Dim field As List(Of (T, T))
                             ~
BC32106: Type argument 'U' does not satisfy the 'Class' constraint for type parameter 'T2'.
    Function M(Of U)(x As U) As (U, U)
                                ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub Constraints_04()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Namespace System
    Public Structure ValueTuple(Of T1, T2 As Class)   
        Sub New(_1 As T1, _2 As T2)       
        End Sub
    End Structure
End Namespace
Class C(Of T As Class)
    Dim field As List(Of (T, T))
    Function M(Of U As Class)(x As U) As (U, U)
        Dim t0 = New C(Of Integer)()
        Dim t1 = M(1)
        Return (Nothing, Nothing)
    End Function
End Class
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim t0 = New C(Of Integer)()
                          ~~~~~~~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'U'.
        Dim t1 = M(1)
                 ~
</errors>)
        End Sub

        <Fact()>
        Public Sub Constraints_05()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Namespace System
    Public Structure ValueTuple(Of T1, T2 As Structure)   
        Sub New(_1 As T1, _2 As T2)       
        End Sub
    End Structure
End Namespace
Class C(Of T As Class)
    Dim field As List(Of (T, T))
    Function M(Of U As Class)(x As (U, U)) As (U, U)
        Dim t0 = New C(Of Integer)()
        Dim t1 = M((1, 2))
        Return (Nothing, Nothing)
    End Function
End Class
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T2'.
    Dim field As List(Of (T, T))
                             ~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T2'.
    Function M(Of U As Class)(x As (U, U)) As (U, U)
                              ~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T2'.
    Function M(Of U As Class)(x As (U, U)) As (U, U)
                                              ~~~~~~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T'.
        Dim t0 = New C(Of Integer)()
                          ~~~~~~~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'U'.
        Dim t1 = M((1, 2))
                 ~
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T2'.
        Return (Nothing, Nothing)
                         ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub Constraints_06()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace System
    Public Structure ValueTuple(Of T1 As Class)
        Public Sub New(item1 As T1)
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest As Class)
        Public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7, rest As TRest)
        End Sub
    End Structure
End Namespace
Class C
    Sub M(p As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer))
        Dim t0 = (1, 2, 3, 4, 5, 6, 7, 8)
        Dim t1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = t0
    End Sub
End Class
]]></file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T1'.
    Sub M(p As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer))
          ~
BC32106: Type argument 'ValueTuple(Of Integer)' does not satisfy the 'Class' constraint for type parameter 'TRest'.
    Sub M(p As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer))
          ~
BC32106: Type argument 'ValueTuple(Of Integer)' does not satisfy the 'Class' constraint for type parameter 'TRest'.
        Dim t0 = (1, 2, 3, 4, 5, 6, 7, 8)
                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T1'.
        Dim t0 = (1, 2, 3, 4, 5, 6, 7, 8)
                                       ~
BC32106: Type argument 'ValueTuple(Of Integer)' does not satisfy the 'Class' constraint for type parameter 'TRest'.
        Dim t1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = t0
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T1'.
        Dim t1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = t0
                                                                                  ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub LongTupleConstraints()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class C
    Sub M0(p As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator))
        Dim t1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator) = p
        Dim t2 = (1, 2, 3, 4, 5, 6, 7, New ArgIterator())
        Dim t3 = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer, ArgIterator))()
        Dim t4 = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, ArgIterator))()
        Dim t5 As ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer, ArgIterator)) = t3
        Dim t6 As ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, ArgIterator)) = t4
    End Sub

    Sub M1(q As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator))
        Dim v1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator) = q
        Dim v2 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, New ArgIterator())
    End Sub
End Class]]></file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Sub M0(p As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator))
           ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator) = p
                                                                                  ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t2 = (1, 2, 3, 4, 5, 6, 7, New ArgIterator())
                                       ~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t3 = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer, ArgIterator))()
                                                                                                                         ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t4 = New ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, ArgIterator))()
                                                                                                            ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t5 As ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, ValueTuple(Of Integer, ArgIterator)) = t3
                                                                                                                      ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim t6 As ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, ArgIterator)) = t4
                                                                                                         ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Sub M1(q As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator))
           ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v1 As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, ArgIterator) = q
                                                                                                                                                          ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v2 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, New ArgIterator())
                                                                     ~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub RestrictedTypes1()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim x = (1, 2, New ArgIterator())
        Dim y As (x As Integer, y As Object) = (1, 2, New ArgIterator())
        Dim z As (x As Integer, y As ArgIterator) = (1, 2, New ArgIterator())
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x = (1, 2, New ArgIterator())
                       ~~~~~~~~~~~~~~~~~
BC30311: Value of type '(Integer, Integer, ArgIterator)' cannot be converted to '(x As Integer, y As Object)'.
        Dim y As (x As Integer, y As Object) = (1, 2, New ArgIterator())
                                               ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim y As (x As Integer, y As Object) = (1, 2, New ArgIterator())
                                                      ~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z As (x As Integer, y As ArgIterator) = (1, 2, New ArgIterator())
                                ~
BC30311: Value of type '(Integer, Integer, ArgIterator)' cannot be converted to '(x As Integer, y As ArgIterator)'.
        Dim z As (x As Integer, y As ArgIterator) = (1, 2, New ArgIterator())
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z As (x As Integer, y As ArgIterator) = (1, 2, New ArgIterator())
                                                           ~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub RestrictedTypes2()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim y As (x As Integer, y As ArgIterator)
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC42024: Unused local variable: 'y'.
        Dim y As (x As Integer, y As ArgIterator)
            ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim y As (x As Integer, y As ArgIterator)
                                ~
</errors>)

        End Sub

        <Fact>
        Public Sub ImplementInterface()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Interface I
    Function M(value As (x As Integer, y As String)) As (Alice As Integer, Bob As String)
    ReadOnly Property P1 As (Alice As Integer, Bob As String)
End Interface
Public Class C
    Implements I

    Shared Sub Main()
        Dim c = New C()
        Dim x = c.M(c.P1)
        Console.Write(x)
    End Sub

    Public Function M(value As (x As Integer, y As String)) As (Alice As Integer, Bob As String) Implements I.M
        Return value
    End Function
    ReadOnly Property P1 As (Alice As Integer, Bob As String) Implements I.P1
        Get
            Return (r:=1, s:="hello")
        End Get
    End Property
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(1, hello)")

        End Sub

        <Fact>
        Public Sub TupleTypeArguments()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Interface I(Of TA, TB As TA)
   Function M(a As TA, b As TB) As (TA, TB)
End Interface

Public Class C
    Implements I(Of (Integer, String), (Alice As Integer, Bob As String))

    Shared Sub Main()
        Dim c = New C()
        Dim x = c.M((1, "Australia"), (2, "Brazil"))
        Console.Write(x)
    End Sub

    Public Function M(x As (Integer, String), y As (Alice As Integer, Bob As String)) As ((Integer, String), (Alice As Integer, Bob As String)) Implements I(Of (Integer, String), (Alice As Integer, Bob As String)).M
        Return (x, y)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="((1, Australia), (2, Brazil))")

        End Sub

        <Fact>
        Public Sub OverrideGenericInterfaceWithDifferentNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Interface I(Of TA, TB As TA)
   Function M(paramA As TA, paramB As TB) As (returnA As TA, returnB As TB)
End Interface

Public Class C
    Implements I(Of (a As Integer, b As String), (Integer, String))

    Public Overridable Function M(x As ((Integer, Integer), (Integer, Integer))) As (x As (Integer, Integer), y As (Integer, Integer)) Implements I(Of (b As Integer, a As Integer), (a As Integer, b As Integer)).M
        Throw New Exception()
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugDll, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Function M(paramA As (a As Integer, b As String), paramB As (Integer, String)) As (returnA As (a As Integer, b As String), returnB As (Integer, String))' for interface 'I(Of (a As Integer, b As String), (Integer, String))'.
    Implements I(Of (a As Integer, b As String), (Integer, String))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31035: Interface 'I(Of (b As Integer, a As Integer), (a As Integer, b As Integer))' is not implemented by this class.
    Public Overridable Function M(x As ((Integer, Integer), (Integer, Integer))) As (x As (Integer, Integer), y As (Integer, Integer)) Implements I(Of (b As Integer, a As Integer), (a As Integer, b As Integer)).M
                                                                                                                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TupleWithoutFeatureFlag()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim x As (Integer, Integer) = (1, 1)
        Else
    End Sub
End Class
    </file>
</compilation>,
options:=TestOptions.DebugDll, additionalRefs:=s_valueTupleRefs,
parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic14))

            comp.AssertTheseDiagnostics(
<errors>
BC36716: Visual Basic 14.0 does not support tuples.
        Dim x As (Integer, Integer) = (1, 1)
                 ~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 14.0 does not support tuples.
        Dim x As (Integer, Integer) = (1, 1)
                                      ~~~~~~
BC30086: 'Else' must be preceded by a matching 'If' or 'ElseIf'.
        Else
        ~~~~
</errors>)
            Dim x = comp.GetDiagnostics()
            Assert.Equal("15", Compilation.GetRequiredLanguageVersion(comp.GetDiagnostics()(0)))
            Assert.Null(Compilation.GetRequiredLanguageVersion(comp.GetDiagnostics()(2)))
            Assert.Throws(Of ArgumentNullException)(Sub() Compilation.GetRequiredLanguageVersion(Nothing))
        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim v1 = M1()
        Console.WriteLine($"{v1.Item1} {v1.Item2}")

        Dim v2 = M2()
        Console.WriteLine($"{v2.Item1} {v2.Item2} {v2.a2} {v2.b2}")

        Dim v6 = M6()
        Console.WriteLine($"{v6.Item1} {v6.Item2} {v6.item1} {v6.item2}")

        Console.WriteLine(v1.ToString())
        Console.WriteLine(v2.ToString())
        Console.WriteLine(v6.ToString())
    End Sub

    Shared Function M1() As (Integer, Integer)
        Return (1, 11)
    End Function
    Shared Function M2() As (a2 As Integer, b2 As Integer)
        Return (2, 22)
    End Function
    Shared Function M6() As (item1 As Integer, item2 As Integer)
        Return (6, 66)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
1 11
2 22 2 22
6 66 6 66
(1, 11)
(2, 22)
(6, 66)
")

            Dim c = comp.GetTypeByMetadataName("C")

            Dim m1Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M1").ReturnType, NamedTypeSymbol)
            Dim m2Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M2").ReturnType, NamedTypeSymbol)
            Dim m6Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M6").ReturnType, NamedTypeSymbol)

            AssertTestDisplayString(m1Tuple.GetMembers(),
                "(System.Int32, System.Int32).Item1 As System.Int32",
                "(System.Int32, System.Int32).Item2 As System.Int32",
                "Sub (System.Int32, System.Int32)..ctor()",
                "Sub (System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
                "Function (System.Int32, System.Int32).Equals(obj As System.Object) As System.Boolean",
                "Function (System.Int32, System.Int32).Equals(other As (System.Int32, System.Int32)) As System.Boolean",
                "Function (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
                "Function (System.Int32, System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
                "Function (System.Int32, System.Int32).CompareTo(other As (System.Int32, System.Int32)) As System.Int32",
                "Function (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
                "Function (System.Int32, System.Int32).GetHashCode() As System.Int32",
                "Function (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (System.Int32, System.Int32).ToString() As System.String",
                "Function (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
                "Function (System.Int32, System.Int32).System.ITupleInternal.get_Size() As System.Int32",
                "ReadOnly Property (System.Int32, System.Int32).System.ITupleInternal.Size As System.Int32"
                )

            Assert.Equal({
                ".ctor",
                ".ctor",
                "CompareTo",
                "Equals",
                "Equals",
                "GetHashCode",
                "Item1",
                "Item2",
                "System.Collections.IStructuralComparable.CompareTo",
                "System.Collections.IStructuralEquatable.Equals",
                "System.Collections.IStructuralEquatable.GetHashCode",
                "System.IComparable.CompareTo",
                "System.ITupleInternal.get_Size",
                "System.ITupleInternal.GetHashCode",
                "System.ITupleInternal.Size",
                "System.ITupleInternal.ToStringEnd",
                "ToString"},
                DirectCast(m1Tuple, TupleTypeSymbol).UnderlyingDefinitionToMemberMap.Values.Select(Function(s) s.Name).OrderBy(Function(s) s).ToArray()
                )

            AssertTestDisplayString(m2Tuple.GetMembers(),
                "(a2 As System.Int32, b2 As System.Int32).Item1 As System.Int32",
                "(a2 As System.Int32, b2 As System.Int32).a2 As System.Int32",
                "(a2 As System.Int32, b2 As System.Int32).Item2 As System.Int32",
                "(a2 As System.Int32, b2 As System.Int32).b2 As System.Int32",
                "Sub (a2 As System.Int32, b2 As System.Int32)..ctor()",
                "Sub (a2 As System.Int32, b2 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
                "Function (a2 As System.Int32, b2 As System.Int32).Equals(obj As System.Object) As System.Boolean",
                "Function (a2 As System.Int32, b2 As System.Int32).Equals(other As (System.Int32, System.Int32)) As System.Boolean",
                "Function (a2 As System.Int32, b2 As System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
                "Function (a2 As System.Int32, b2 As System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
                "Function (a2 As System.Int32, b2 As System.Int32).CompareTo(other As (System.Int32, System.Int32)) As System.Int32",
                "Function (a2 As System.Int32, b2 As System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
                "Function (a2 As System.Int32, b2 As System.Int32).GetHashCode() As System.Int32",
                "Function (a2 As System.Int32, b2 As System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (a2 As System.Int32, b2 As System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (a2 As System.Int32, b2 As System.Int32).ToString() As System.String",
                "Function (a2 As System.Int32, b2 As System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
                "Function (a2 As System.Int32, b2 As System.Int32).System.ITupleInternal.get_Size() As System.Int32",
                "ReadOnly Property (a2 As System.Int32, b2 As System.Int32).System.ITupleInternal.Size As System.Int32"
                )

            Assert.Equal({
                ".ctor",
                ".ctor",
                "CompareTo",
                "Equals",
                "Equals",
                "GetHashCode",
                "Item1",
                "Item2",
                "System.Collections.IStructuralComparable.CompareTo",
                "System.Collections.IStructuralEquatable.Equals",
                "System.Collections.IStructuralEquatable.GetHashCode",
                "System.IComparable.CompareTo",
                "System.ITupleInternal.get_Size",
                "System.ITupleInternal.GetHashCode",
                "System.ITupleInternal.Size",
                "System.ITupleInternal.ToStringEnd",
                "ToString"},
                DirectCast(m2Tuple, TupleTypeSymbol).UnderlyingDefinitionToMemberMap.Values.Select(Function(s) s.Name).OrderBy(Function(s) s).ToArray()
                )

            AssertTestDisplayString(m6Tuple.GetMembers(),
                "(Item1 As System.Int32, Item2 As System.Int32).Item1 As System.Int32",
                "(Item1 As System.Int32, Item2 As System.Int32).Item2 As System.Int32",
                "Sub (Item1 As System.Int32, Item2 As System.Int32)..ctor()",
                "Sub (Item1 As System.Int32, Item2 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
                "Function (Item1 As System.Int32, Item2 As System.Int32).Equals(obj As System.Object) As System.Boolean",
                "Function (Item1 As System.Int32, Item2 As System.Int32).Equals(other As (System.Int32, System.Int32)) As System.Boolean",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
                "Function (Item1 As System.Int32, Item2 As System.Int32).CompareTo(other As (System.Int32, System.Int32)) As System.Int32",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
                "Function (Item1 As System.Int32, Item2 As System.Int32).GetHashCode() As System.Int32",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (Item1 As System.Int32, Item2 As System.Int32).ToString() As System.String",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
                "Function (Item1 As System.Int32, Item2 As System.Int32).System.ITupleInternal.get_Size() As System.Int32",
                "ReadOnly Property (Item1 As System.Int32, Item2 As System.Int32).System.ITupleInternal.Size As System.Int32"
                )

            Assert.Equal({
                ".ctor",
                ".ctor",
                "CompareTo",
                "Equals",
                "Equals",
                "GetHashCode",
                "Item1",
                "Item2",
                "System.Collections.IStructuralComparable.CompareTo",
                "System.Collections.IStructuralEquatable.Equals",
                "System.Collections.IStructuralEquatable.GetHashCode",
                "System.IComparable.CompareTo",
                "System.ITupleInternal.get_Size",
                "System.ITupleInternal.GetHashCode",
                "System.ITupleInternal.Size",
                "System.ITupleInternal.ToStringEnd",
                "ToString"},
                DirectCast(m6Tuple, TupleTypeSymbol).UnderlyingDefinitionToMemberMap.Values.Select(Function(s) s.Name).OrderBy(Function(s) s).ToArray()
                )

            Assert.Equal("", m1Tuple.Name)
            Assert.Equal(SymbolKind.NamedType, m1Tuple.Kind)
            Assert.Equal(TypeKind.Struct, m1Tuple.TypeKind)
            Assert.False(m1Tuple.IsImplicitlyDeclared)
            Assert.True(m1Tuple.IsTupleType)
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32)", m1Tuple.TupleUnderlyingType.ToTestDisplayString())
            Assert.Same(m1Tuple, m1Tuple.ConstructedFrom)
            Assert.Same(m1Tuple, m1Tuple.OriginalDefinition)
            AssertTupleTypeEquality(m1Tuple)
            Assert.Same(m1Tuple.TupleUnderlyingType.ContainingSymbol, m1Tuple.ContainingSymbol)
            Assert.Null(m1Tuple.EnumUnderlyingType)

            Assert.Equal({
                "Item1",
                "Item2",
                ".ctor",
                "Equals",
                "System.Collections.IStructuralEquatable.Equals",
                "System.IComparable.CompareTo",
                "CompareTo",
                "System.Collections.IStructuralComparable.CompareTo",
                "GetHashCode",
                "System.Collections.IStructuralEquatable.GetHashCode",
                "System.ITupleInternal.GetHashCode",
                "ToString",
                "System.ITupleInternal.ToStringEnd",
                "System.ITupleInternal.get_Size",
                "System.ITupleInternal.Size"},
                m1Tuple.MemberNames.ToArray())

            Assert.Equal({
                "Item1",
                "a2",
                "Item2",
                "b2",
                ".ctor",
                "Equals",
                "System.Collections.IStructuralEquatable.Equals",
                "System.IComparable.CompareTo",
                "CompareTo",
                "System.Collections.IStructuralComparable.CompareTo",
                "GetHashCode",
                "System.Collections.IStructuralEquatable.GetHashCode",
                "System.ITupleInternal.GetHashCode",
                "ToString",
                "System.ITupleInternal.ToStringEnd",
                "System.ITupleInternal.get_Size",
                "System.ITupleInternal.Size"},
                m2Tuple.MemberNames.ToArray())

            Assert.Equal(0, m1Tuple.Arity)
            Assert.True(m1Tuple.TypeParameters.IsEmpty)
            Assert.Equal("System.ValueType", m1Tuple.BaseType.ToTestDisplayString())
            Assert.False(m1Tuple.HasTypeArgumentsCustomModifiers)
            Assert.False(m1Tuple.IsComImport)
            Assert.True(m1Tuple.TypeArgumentsNoUseSiteDiagnostics.IsEmpty)
            Assert.True(m1Tuple.GetAttributes().IsEmpty)
            Assert.Equal("(a2 As System.Int32, b2 As System.Int32).Item1 As System.Int32", m2Tuple.GetMembers("Item1").Single().ToTestDisplayString())
            Assert.Equal("(a2 As System.Int32, b2 As System.Int32).a2 As System.Int32", m2Tuple.GetMembers("a2").Single().ToTestDisplayString())
            Assert.True(m1Tuple.GetTypeMembers().IsEmpty)
            Assert.True(m1Tuple.GetTypeMembers("C9").IsEmpty)
            Assert.True(m1Tuple.GetTypeMembers("C9", 0).IsEmpty)
            Assert.Equal(6, m1Tuple.Interfaces.Length)

            Assert.True(m1Tuple.GetTypeMembersUnordered().IsEmpty)
            Assert.Equal(1, m1Tuple.Locations.Length)
            Assert.Equal("(Integer, Integer)", m1Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.Equal("(a2 As Integer, b2 As Integer)", m2Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString())

            AssertTupleTypeEquality(m2Tuple)
            AssertTupleTypeEquality(m6Tuple)

            Assert.False(m1Tuple.Equals(m2Tuple))
            Assert.False(m1Tuple.Equals(m6Tuple))
            Assert.False(m6Tuple.Equals(m2Tuple))
            AssertTupleTypeMembersEquality(m1Tuple, m2Tuple)
            AssertTupleTypeMembersEquality(m1Tuple, m6Tuple)
            AssertTupleTypeMembersEquality(m2Tuple, m6Tuple)

            Dim m1Item1 = DirectCast(m1Tuple.GetMembers()(0), FieldSymbol)
            Dim m2Item1 = DirectCast(m2Tuple.GetMembers()(0), FieldSymbol)
            Dim m2a2 = DirectCast(m2Tuple.GetMembers()(1), FieldSymbol)

            AssertNonvirtualTupleElementField(m1Item1)
            AssertNonvirtualTupleElementField(m2Item1)
            AssertVirtualTupleElementField(m2a2)

            Assert.True(m1Item1.IsTupleField)
            Assert.Same(m1Item1, m1Item1.OriginalDefinition)
            Assert.True(m1Item1.Equals(m1Item1))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m1Item1.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m1Item1.AssociatedSymbol)
            Assert.Same(m1Tuple, m1Item1.ContainingSymbol)
            Assert.Same(m1Tuple.TupleUnderlyingType, m1Item1.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m1Item1.CustomModifiers.IsEmpty)
            Assert.True(m1Item1.GetAttributes().IsEmpty)
            Assert.Null(m1Item1.GetUseSiteErrorInfo())
            Assert.False(m1Item1.Locations.IsEmpty)
            Assert.True(m1Item1.DeclaringSyntaxReferences.IsEmpty)
            Assert.Equal("Item1", m1Item1.TupleUnderlyingField.Name)
            Assert.True(m1Item1.IsImplicitlyDeclared)
            Assert.Null(m1Item1.TypeLayoutOffset)

            Assert.True(m2Item1.IsTupleField)
            Assert.Same(m2Item1, m2Item1.OriginalDefinition)
            Assert.True(m2Item1.Equals(m2Item1))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m2Item1.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m2Item1.AssociatedSymbol)
            Assert.Same(m2Tuple, m2Item1.ContainingSymbol)
            Assert.Same(m2Tuple.TupleUnderlyingType, m2Item1.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m2Item1.CustomModifiers.IsEmpty)
            Assert.True(m2Item1.GetAttributes().IsEmpty)
            Assert.Null(m2Item1.GetUseSiteErrorInfo())
            Assert.False(m2Item1.Locations.IsEmpty)
            Assert.Equal("Item1", m2Item1.Name)
            Assert.Equal("Item1", m2Item1.TupleUnderlyingField.Name)
            Assert.NotEqual(m2Item1.Locations.Single(), m2Item1.TupleUnderlyingField.Locations.Single())
            Assert.Equal("MetadataFile(System.ValueTuple.dll)", m2Item1.TupleUnderlyingField.Locations.Single().ToString())
            Assert.Equal("SourceFile(a.vb[589..591))", m2Item1.Locations.Single().ToString())
            Assert.True(m2Item1.IsImplicitlyDeclared)
            Assert.Null(m2Item1.TypeLayoutOffset)

            Assert.True(m2a2.IsTupleField)
            Assert.Same(m2a2, m2a2.OriginalDefinition)
            Assert.True(m2a2.Equals(m2a2))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m2a2.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m2a2.AssociatedSymbol)
            Assert.Same(m2Tuple, m2a2.ContainingSymbol)
            Assert.Same(m2Tuple.TupleUnderlyingType, m2a2.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m2a2.CustomModifiers.IsEmpty)
            Assert.True(m2a2.GetAttributes().IsEmpty)
            Assert.Null(m2a2.GetUseSiteErrorInfo())
            Assert.False(m2a2.Locations.IsEmpty)
            Assert.Equal("a2 As Integer", m2a2.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.Equal("Item1", m2a2.TupleUnderlyingField.Name)
            Assert.False(m2a2.IsImplicitlyDeclared)
            Assert.Null(m2a2.TypeLayoutOffset)
        End Sub

        Private Sub AssertTupleTypeEquality(tuple As NamedTypeSymbol)
            Assert.True(tuple.Equals(tuple))

            Dim members = tuple.GetMembers()

            For i = 0 To members.Length - 1
                For j = 0 To members.Length - 1
                    If i <> j Then
                        Assert.NotSame(members(i), members(j))
                        Assert.False(members(i).Equals(members(j)))
                        Assert.False(members(j).Equals(members(i)))
                    End If
                Next
            Next

            Dim underlyingMembers = tuple.TupleUnderlyingType.GetMembers()

            For Each m In members
                Assert.False(underlyingMembers.Any(Function(u) u.Equals(m)))
                Assert.False(underlyingMembers.Any(Function(u) m.Equals(u)))
            Next

        End Sub

        Private Sub AssertTupleTypeMembersEquality(tuple1 As NamedTypeSymbol, tuple2 As NamedTypeSymbol)
            Assert.NotSame(tuple1, tuple2)

            If tuple1.Equals(tuple2) Then
                Assert.True(tuple2.Equals(tuple1))
                Dim members1 = tuple1.GetMembers()
                Dim members2 = tuple2.GetMembers()
                Assert.Equal(members1.Length, members2.Length)

                For i = 0 To members1.Length - 1
                    Assert.NotSame(members1(i), members2(i))
                    Assert.True(members1(i).Equals(members2(i)))
                    Assert.True(members2(i).Equals(members1(i)))
                    Assert.Equal(members2(i).GetHashCode(), members1(i).GetHashCode())

                    If members1(i).Kind = SymbolKind.Method Then
                        Dim parameters1 = DirectCast(members1(i), MethodSymbol).Parameters
                        Dim parameters2 = DirectCast(members2(i), MethodSymbol).Parameters
                        AssertTupleMembersParametersEquality(parameters1, parameters2)

                        Dim typeParameters1 = DirectCast(members1(i), MethodSymbol).TypeParameters
                        Dim typeParameters2 = DirectCast(members2(i), MethodSymbol).TypeParameters
                        Assert.Equal(typeParameters1.Length, typeParameters2.Length)
                        For j = 0 To typeParameters1.Length - 1
                            Assert.NotSame(typeParameters1(j), typeParameters2(j))
                            Assert.True(typeParameters1(j).Equals(typeParameters2(j)))
                            Assert.True(typeParameters2(j).Equals(typeParameters1(j)))
                            Assert.Equal(typeParameters2(j).GetHashCode(), typeParameters1(j).GetHashCode())
                        Next
                    ElseIf members1(i).Kind = SymbolKind.Property Then
                        Dim parameters1 = DirectCast(members1(i), PropertySymbol).Parameters
                        Dim parameters2 = DirectCast(members2(i), PropertySymbol).Parameters
                        AssertTupleMembersParametersEquality(parameters1, parameters2)
                    End If
                Next

                For i = 0 To members1.Length - 1
                    For j = 0 To members2.Length - 1
                        If i <> j Then
                            Assert.NotSame(members1(i), members2(j))
                            Assert.False(members1(i).Equals(members2(j)))
                        End If
                    Next
                Next
            Else
                Assert.False(tuple2.Equals(tuple1))
                Dim members1 = tuple1.GetMembers()
                Dim members2 = tuple2.GetMembers()
                For Each m In members1
                    Assert.False(members2.Any(Function(u) u.Equals(m)))
                    Assert.False(members2.Any(Function(u) m.Equals(u)))
                Next
            End If
        End Sub

        Private Sub AssertTupleMembersParametersEquality(parameters1 As ImmutableArray(Of ParameterSymbol), parameters2 As ImmutableArray(Of ParameterSymbol))
            Assert.Equal(parameters1.Length, parameters2.Length)
            For j = 0 To parameters1.Length - 1
                Assert.NotSame(parameters1(j), parameters2(j))
                Assert.True(parameters1(j).Equals(parameters2(j)))
                Assert.True(parameters2(j).Equals(parameters1(j)))
                Assert.Equal(parameters2(j).GetHashCode(), parameters1(j).GetHashCode())
            Next
        End Sub

        Private Sub AssertVirtualTupleElementField(sym As FieldSymbol)
            Assert.True(sym.IsTupleField)
            Assert.True(sym.IsVirtualTupleField)

            ' it is an element so must have nonnegative index
            Assert.True(sym.TupleElementIndex >= 0)
        End Sub

        Private Sub AssertNonvirtualTupleElementField(sym As FieldSymbol)
            Assert.True(sym.IsTupleField)
            Assert.False(sym.IsVirtualTupleField)

            ' it is an element so must have nonnegative index
            Assert.True(sym.TupleElementIndex >= 0)

            ' if it was 8th or after, it would be virtual
            Assert.True(sym.TupleElementIndex < TupleTypeSymbol.RestPosition - 1)
        End Sub

        Private Shared Sub AssertTestDisplayString(symbols As ImmutableArray(Of Symbol), ParamArray baseLine As String())
            ' Re-ordering arguments because expected is usually first.
            AssertEx.Equal(baseLine, symbols.Select(Function(s) s.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim v3 = M3()
        Console.WriteLine(v3.Item1)
        Console.WriteLine(v3.Item2)
        Console.WriteLine(v3.Item3)
        Console.WriteLine(v3.Item4)
        Console.WriteLine(v3.Item5)
        Console.WriteLine(v3.Item6)
        Console.WriteLine(v3.Item7)
        Console.WriteLine(v3.Item8)
        Console.WriteLine(v3.Item9)
        Console.WriteLine(v3.Rest.Item1)
        Console.WriteLine(v3.Rest.Item2)

        Console.WriteLine(v3.ToString())
    End Sub

    Shared Function M3() As (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)
        Return (31, 32, 33, 34, 35, 36, 37, 38, 39)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
31
32
33
34
35
36
37
38
39
38
39
(31, 32, 33, 34, 35, 36, 37, 38, 39)
")

            Dim c = comp.GetTypeByMetadataName("C")

            Dim m3Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M3").ReturnType, NamedTypeSymbol)

            AssertTestDisplayString(m3Tuple.GetMembers(),
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item1 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item2 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item3 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item4 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item5 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item6 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item7 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Rest As (System.Int32, System.Int32)",
"Sub (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor()",
"Sub (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As (System.Int32, System.Int32))",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).CompareTo(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).GetHashCode() As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).ToString() As System.String",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item8 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item9 As System.Int32"
                )

            Dim m3Item8 = DirectCast(m3Tuple.GetMembers("Item8").Single(), FieldSymbol)

            AssertVirtualTupleElementField(m3Item8)

            Assert.True(m3Item8.IsTupleField)
            Assert.Same(m3Item8, m3Item8.OriginalDefinition)
            Assert.True(m3Item8.Equals(m3Item8))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m3Item8.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m3Item8.AssociatedSymbol)
            Assert.Same(m3Tuple, m3Item8.ContainingSymbol)
            Assert.NotEqual(m3Tuple.TupleUnderlyingType, m3Item8.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m3Item8.CustomModifiers.IsEmpty)
            Assert.True(m3Item8.GetAttributes().IsEmpty)
            Assert.Null(m3Item8.GetUseSiteErrorInfo())
            Assert.False(m3Item8.Locations.IsEmpty)
            Assert.True(m3Item8.DeclaringSyntaxReferences.IsEmpty)
            Assert.Equal("Item1", m3Item8.TupleUnderlyingField.Name)
            Assert.True(m3Item8.IsImplicitlyDeclared)
            Assert.Null(m3Item8.TypeLayoutOffset)

            Dim m3TupleRestTuple = DirectCast(DirectCast(m3Tuple.GetMembers("Rest").Single(), FieldSymbol).Type, NamedTypeSymbol)
            AssertTestDisplayString(m3TupleRestTuple.GetMembers(),
                "(System.Int32, System.Int32).Item1 As System.Int32",
                "(System.Int32, System.Int32).Item2 As System.Int32",
                "Sub (System.Int32, System.Int32)..ctor()",
                "Sub (System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
                "Function (System.Int32, System.Int32).Equals(obj As System.Object) As System.Boolean",
                "Function (System.Int32, System.Int32).Equals(other As (System.Int32, System.Int32)) As System.Boolean",
                "Function (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
                "Function (System.Int32, System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
                "Function (System.Int32, System.Int32).CompareTo(other As (System.Int32, System.Int32)) As System.Int32",
                "Function (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
                "Function (System.Int32, System.Int32).GetHashCode() As System.Int32",
                "Function (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
                "Function (System.Int32, System.Int32).ToString() As System.String",
                "Function (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
                "Function (System.Int32, System.Int32).System.ITupleInternal.get_Size() As System.Int32",
                "ReadOnly Property (System.Int32, System.Int32).System.ITupleInternal.Size As System.Int32"
                )

            Assert.True(m3TupleRestTuple.IsTupleType)
            AssertTupleTypeEquality(m3TupleRestTuple)
            Assert.True(m3TupleRestTuple.Locations.IsEmpty)
            Assert.True(m3TupleRestTuple.DeclaringSyntaxReferences.IsEmpty)

            For Each m In m3TupleRestTuple.GetMembers().OfType(Of FieldSymbol)()
                Assert.True(m.Locations.IsEmpty)
            Next

        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim v4 = M4()
        Console.WriteLine(v4.Item1)
        Console.WriteLine(v4.Item2)
        Console.WriteLine(v4.Item3)
        Console.WriteLine(v4.Item4)
        Console.WriteLine(v4.Item5)
        Console.WriteLine(v4.Item6)
        Console.WriteLine(v4.Item7)
        Console.WriteLine(v4.Item8)
        Console.WriteLine(v4.Item9)
        Console.WriteLine(v4.Rest.Item1)
        Console.WriteLine(v4.Rest.Item2)

        Console.WriteLine(v4.a4)
        Console.WriteLine(v4.b4)
        Console.WriteLine(v4.c4)
        Console.WriteLine(v4.d4)
        Console.WriteLine(v4.e4)
        Console.WriteLine(v4.f4)
        Console.WriteLine(v4.g4)
        Console.WriteLine(v4.h4)
        Console.WriteLine(v4.i4)

        Console.WriteLine(v4.ToString())
    End Sub

    Shared Function M4() As (a4 As Integer, b4 As Integer, c4 As Integer, d4 As Integer, e4 As Integer, f4 As Integer, g4 As Integer, h4 As Integer, i4 As Integer)
        Return (41, 42, 43, 44, 45, 46, 47, 48, 49)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
41
42
43
44
45
46
47
48
49
48
49
41
42
43
44
45
46
47
48
49
(41, 42, 43, 44, 45, 46, 47, 48, 49)
")

            Dim c = comp.GetTypeByMetadataName("C")

            Dim m4Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M4").ReturnType, NamedTypeSymbol)
            AssertTupleTypeEquality(m4Tuple)

            AssertTestDisplayString(m4Tuple.GetMembers(),
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item1 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).a4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item2 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).b4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item3 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).c4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).d4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item5 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).e4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item6 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).f4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item7 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).g4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Rest As (System.Int32, System.Int32)",
"Sub (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32)..ctor()",
"Sub (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As (System.Int32, System.Int32))",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Equals(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Boolean",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).CompareTo(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Int32",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).GetHashCode() As System.Int32",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).ToString() As System.String",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).System.ITupleInternal.Size As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item8 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).h4 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).Item9 As System.Int32",
"(a4 As System.Int32, b4 As System.Int32, c4 As System.Int32, d4 As System.Int32, e4 As System.Int32, f4 As System.Int32, g4 As System.Int32, h4 As System.Int32, i4 As System.Int32).i4 As System.Int32"
)

            Dim m4Item8 = DirectCast(m4Tuple.GetMembers("Item8").Single(), FieldSymbol)

            AssertVirtualTupleElementField(m4Item8)

            Assert.True(m4Item8.IsTupleField)
            Assert.Same(m4Item8, m4Item8.OriginalDefinition)
            Assert.True(m4Item8.Equals(m4Item8))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m4Item8.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m4Item8.AssociatedSymbol)
            Assert.Same(m4Tuple, m4Item8.ContainingSymbol)
            Assert.NotEqual(m4Tuple.TupleUnderlyingType, m4Item8.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m4Item8.CustomModifiers.IsEmpty)
            Assert.True(m4Item8.GetAttributes().IsEmpty)
            Assert.Null(m4Item8.GetUseSiteErrorInfo())
            Assert.False(m4Item8.Locations.IsEmpty)
            Assert.Equal("Item1", m4Item8.TupleUnderlyingField.Name)
            Assert.True(m4Item8.IsImplicitlyDeclared)
            Assert.Null(m4Item8.TypeLayoutOffset)

            Dim m4h4 = DirectCast(m4Tuple.GetMembers("h4").Single(), FieldSymbol)

            AssertVirtualTupleElementField(m4h4)

            Assert.True(m4h4.IsTupleField)
            Assert.Same(m4h4, m4h4.OriginalDefinition)
            Assert.True(m4h4.Equals(m4h4))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m4h4.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m4h4.AssociatedSymbol)
            Assert.Same(m4Tuple, m4h4.ContainingSymbol)
            Assert.NotEqual(m4Tuple.TupleUnderlyingType, m4h4.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m4h4.CustomModifiers.IsEmpty)
            Assert.True(m4h4.GetAttributes().IsEmpty)
            Assert.Null(m4h4.GetUseSiteErrorInfo())
            Assert.False(m4h4.Locations.IsEmpty)
            Assert.Equal("Item1", m4h4.TupleUnderlyingField.Name)
            Assert.False(m4h4.IsImplicitlyDeclared)
            Assert.Null(m4h4.TypeLayoutOffset)

            Dim m4TupleRestTuple = DirectCast(DirectCast(m4Tuple.GetMembers("Rest").Single(), FieldSymbol).Type, NamedTypeSymbol)
            AssertTestDisplayString(m4TupleRestTuple.GetMembers(),
"(System.Int32, System.Int32).Item1 As System.Int32",
"(System.Int32, System.Int32).Item2 As System.Int32",
"Sub (System.Int32, System.Int32)..ctor()",
"Sub (System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
"Function (System.Int32, System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (System.Int32, System.Int32).Equals(other As (System.Int32, System.Int32)) As System.Boolean",
"Function (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (System.Int32, System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (System.Int32, System.Int32).CompareTo(other As (System.Int32, System.Int32)) As System.Int32",
"Function (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (System.Int32, System.Int32).GetHashCode() As System.Int32",
"Function (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32).ToString() As System.String",
"Function (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (System.Int32, System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (System.Int32, System.Int32).System.ITupleInternal.Size As System.Int32"
 )

            For Each m In m4TupleRestTuple.GetMembers().OfType(Of FieldSymbol)()
                Assert.True(m.Locations.IsEmpty)
            Next

        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_04()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim v4 = M4()
        Console.WriteLine(v4.Rest.a4)
        Console.WriteLine(v4.Rest.b4)
        Console.WriteLine(v4.Rest.c4)
        Console.WriteLine(v4.Rest.d4)
        Console.WriteLine(v4.Rest.e4)
        Console.WriteLine(v4.Rest.f4)
        Console.WriteLine(v4.Rest.g4)
        Console.WriteLine(v4.Rest.h4)
        Console.WriteLine(v4.Rest.i4)

        Console.WriteLine(v4.ToString())
    End Sub

    Shared Function M4() As (a4 As Integer, b4 As Integer, c4 As Integer, d4 As Integer, e4 As Integer, f4 As Integer, g4 As Integer, h4 As Integer, i4 As Integer)
        Return (41, 42, 43, 44, 45, 46, 47, 48, 49)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30456: 'a4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.a4)
                          ~~~~~~~~~~
BC30456: 'b4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.b4)
                          ~~~~~~~~~~
BC30456: 'c4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.c4)
                          ~~~~~~~~~~
BC30456: 'd4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.d4)
                          ~~~~~~~~~~
BC30456: 'e4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.e4)
                          ~~~~~~~~~~
BC30456: 'f4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.f4)
                          ~~~~~~~~~~
BC30456: 'g4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.g4)
                          ~~~~~~~~~~
BC30456: 'h4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.h4)
                          ~~~~~~~~~~
BC30456: 'i4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v4.Rest.i4)
                          ~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_05()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim v5 = M5()
        Console.WriteLine(v5.Item1)
        Console.WriteLine(v5.Item2)
        Console.WriteLine(v5.Item3)
        Console.WriteLine(v5.Item4)
        Console.WriteLine(v5.Item5)
        Console.WriteLine(v5.Item6)
        Console.WriteLine(v5.Item7)
        Console.WriteLine(v5.Item8)
        Console.WriteLine(v5.Item9)
        Console.WriteLine(v5.Item10)
        Console.WriteLine(v5.Item11)
        Console.WriteLine(v5.Item12)
        Console.WriteLine(v5.Item13)
        Console.WriteLine(v5.Item14)
        Console.WriteLine(v5.Item15)
        Console.WriteLine(v5.Item16)
        Console.WriteLine(v5.Rest.Item1)
        Console.WriteLine(v5.Rest.Item2)
        Console.WriteLine(v5.Rest.Item3)
        Console.WriteLine(v5.Rest.Item4)
        Console.WriteLine(v5.Rest.Item5)
        Console.WriteLine(v5.Rest.Item6)
        Console.WriteLine(v5.Rest.Item7)
        Console.WriteLine(v5.Rest.Item8)
        Console.WriteLine(v5.Rest.Item9)
        Console.WriteLine(v5.Rest.Rest.Item1)
        Console.WriteLine(v5.Rest.Rest.Item2)

        Console.WriteLine(v5.ToString())
    End Sub

    Shared Function M5() As (Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer,
        Item9 As Integer, Item10 As Integer, Item11 As Integer, Item12 As Integer, Item13 As Integer, Item14 As Integer, Item15 As Integer, Item16 As Integer)
        Return (501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
501
502
503
504
505
506
507
508
509
510
511
512
513
514
515
516
508
509
510
511
512
513
514
515
516
515
516
(501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516)
")

            Dim c = comp.GetTypeByMetadataName("C")

            Dim m5Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M5").ReturnType, NamedTypeSymbol)
            AssertTupleTypeEquality(m5Tuple)

            AssertTestDisplayString(m5Tuple.GetMembers(),
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item1 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item2 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item3 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item4 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item5 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item6 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item7 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Rest As (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
"Sub (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32)..ctor()",
"Sub (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32))",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Equals(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32))) As System.Boolean",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).CompareTo(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32))) As System.Int32",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).GetHashCode() As System.Int32",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).ToString() As System.String",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).System.ITupleInternal.Size As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item8 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item9 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item10 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item11 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item12 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item13 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item14 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item15 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32, Item9 As System.Int32, Item10 As System.Int32, Item11 As System.Int32, Item12 As System.Int32, Item13 As System.Int32, Item14 As System.Int32, Item15 As System.Int32, Item16 As System.Int32).Item16 As System.Int32"
)

            Dim m5Item8 = DirectCast(m5Tuple.GetMembers("Item8").Single(), FieldSymbol)

            AssertVirtualTupleElementField(m5Item8)

            Assert.True(m5Item8.IsTupleField)
            Assert.Same(m5Item8, m5Item8.OriginalDefinition)
            Assert.True(m5Item8.Equals(m5Item8))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32)).Item1 As System.Int32", m5Item8.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m5Item8.AssociatedSymbol)
            Assert.Same(m5Tuple, m5Item8.ContainingSymbol)
            Assert.NotEqual(m5Tuple.TupleUnderlyingType, m5Item8.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m5Item8.CustomModifiers.IsEmpty)
            Assert.True(m5Item8.GetAttributes().IsEmpty)
            Assert.Null(m5Item8.GetUseSiteErrorInfo())
            Assert.False(m5Item8.Locations.IsEmpty)
            Assert.Equal("Item8 As Integer", m5Item8.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.Equal("Item1", m5Item8.TupleUnderlyingField.Name)
            Assert.False(m5Item8.IsImplicitlyDeclared)
            Assert.Null(m5Item8.TypeLayoutOffset)

            Dim m5TupleRestTuple = DirectCast(DirectCast(m5Tuple.GetMembers("Rest").Single(), FieldSymbol).Type, NamedTypeSymbol)
            AssertVirtualTupleElementField(m5Item8)

            AssertTestDisplayString(m5TupleRestTuple.GetMembers(),
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item1 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item2 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item3 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item4 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item5 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item6 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item7 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Rest As (System.Int32, System.Int32)",
"Sub (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor()",
"Sub (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As (System.Int32, System.Int32))",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).CompareTo(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).GetHashCode() As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).ToString() As System.String",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item8 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item9 As System.Int32"
)

            For Each m In m5TupleRestTuple.GetMembers().OfType(Of FieldSymbol)()
                If m.Name <> "Rest" Then
                    Assert.True(m.Locations.IsEmpty)
                Else
                    Assert.Equal("Rest", m.Name)
                End If
            Next

            Dim m5TupleRestTupleRestTuple = DirectCast(DirectCast(m5TupleRestTuple.GetMembers("Rest").Single(), FieldSymbol).Type, NamedTypeSymbol)
            AssertTupleTypeEquality(m5TupleRestTupleRestTuple)

            AssertTestDisplayString(m5TupleRestTuple.GetMembers(),
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item1 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item2 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item3 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item4 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item5 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item6 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item7 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Rest As (System.Int32, System.Int32)",
"Sub (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor()",
"Sub (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As (System.Int32, System.Int32))",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).CompareTo(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32))) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).GetHashCode() As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).ToString() As System.String",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item8 As System.Int32",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item9 As System.Int32"
)

            For Each m In m5TupleRestTupleRestTuple.GetMembers().OfType(Of FieldSymbol)()
                Assert.True(m.Locations.IsEmpty)
            Next

        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_06()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
        Dim v5 = M5()
        Console.WriteLine(v5.Rest.Item10)
        Console.WriteLine(v5.Rest.Item11)
        Console.WriteLine(v5.Rest.Item12)
        Console.WriteLine(v5.Rest.Item13)
        Console.WriteLine(v5.Rest.Item14)
        Console.WriteLine(v5.Rest.Item15)
        Console.WriteLine(v5.Rest.Item16)

        Console.WriteLine(v5.Rest.Rest.Item3)
        Console.WriteLine(v5.Rest.Rest.Item4)
        Console.WriteLine(v5.Rest.Rest.Item5)
        Console.WriteLine(v5.Rest.Rest.Item6)
        Console.WriteLine(v5.Rest.Rest.Item7)
        Console.WriteLine(v5.Rest.Rest.Item8)
        Console.WriteLine(v5.Rest.Rest.Item9)
        Console.WriteLine(v5.Rest.Rest.Item10)
        Console.WriteLine(v5.Rest.Rest.Item11)
        Console.WriteLine(v5.Rest.Rest.Item12)
        Console.WriteLine(v5.Rest.Rest.Item13)
        Console.WriteLine(v5.Rest.Rest.Item14)
        Console.WriteLine(v5.Rest.Rest.Item15)
        Console.WriteLine(v5.Rest.Rest.Item16)
    End Sub

    Shared Function M5() As (Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer,
        Item9 As Integer, Item10 As Integer, Item11 As Integer, Item12 As Integer, Item13 As Integer, Item14 As Integer, Item15 As Integer, Item16 As Integer)
        Return (501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30456: 'Item10' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item10)
                          ~~~~~~~~~~~~~~
BC30456: 'Item11' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item11)
                          ~~~~~~~~~~~~~~
BC30456: 'Item12' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item12)
                          ~~~~~~~~~~~~~~
BC30456: 'Item13' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item13)
                          ~~~~~~~~~~~~~~
BC30456: 'Item14' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item14)
                          ~~~~~~~~~~~~~~
BC30456: 'Item15' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item15)
                          ~~~~~~~~~~~~~~
BC30456: 'Item16' is not a member of '(Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer)'.
        Console.WriteLine(v5.Rest.Item16)
                          ~~~~~~~~~~~~~~
BC30456: 'Item3' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item3)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item4' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item4)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item5' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item5)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item6' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item6)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item7' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item7)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item8' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item8)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item9' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item9)
                          ~~~~~~~~~~~~~~~~~~
BC30456: 'Item10' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item10)
                          ~~~~~~~~~~~~~~~~~~~
BC30456: 'Item11' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item11)
                          ~~~~~~~~~~~~~~~~~~~
BC30456: 'Item12' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item12)
                          ~~~~~~~~~~~~~~~~~~~
BC30456: 'Item13' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item13)
                          ~~~~~~~~~~~~~~~~~~~
BC30456: 'Item14' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item14)
                          ~~~~~~~~~~~~~~~~~~~
BC30456: 'Item15' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item15)
                          ~~~~~~~~~~~~~~~~~~~
BC30456: 'Item16' is not a member of '(Integer, Integer)'.
        Console.WriteLine(v5.Rest.Rest.Item16)
                          ~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_07()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Class C
    Shared Sub Main()
    End Sub

    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
        Return (701, 702, 703, 704, 705, 706, 707, 708, 709)
    End Function
End Class
<%= s_trivial2uple %><%= s_trivial3uple %><%= s_trivialRemainingTuples %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics(
<errors>
BC37261: Tuple element name 'Item9' is only allowed at position 9.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                             ~~~~~
BC37261: Tuple element name 'Item1' is only allowed at position 1.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                               ~~~~~
BC37261: Tuple element name 'Item2' is only allowed at position 2.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                 ~~~~~
BC37261: Tuple element name 'Item3' is only allowed at position 3.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                                   ~~~~~
BC37261: Tuple element name 'Item4' is only allowed at position 4.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                                                     ~~~~~
BC37261: Tuple element name 'Item5' is only allowed at position 5.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                                                                       ~~~~~
BC37261: Tuple element name 'Item6' is only allowed at position 6.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                                                                                         ~~~~~
BC37261: Tuple element name 'Item7' is only allowed at position 7.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                                                                                                           ~~~~~
BC37261: Tuple element name 'Item8' is only allowed at position 8.
    Shared Function M7() As (Item9 As Integer, Item1 As Integer, Item2 As Integer, Item3 As Integer, Item4 As Integer, Item5 As Integer, Item6 As Integer, Item7 As Integer, Item8 As Integer)
                                                                                                                                                                             ~~~~~
</errors>)

            Dim c = comp.GetTypeByMetadataName("C")

            Dim m7Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M7").ReturnType, NamedTypeSymbol)
            AssertTupleTypeEquality(m7Tuple)

            AssertTestDisplayString(m7Tuple.GetMembers(),
"Sub (Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32)..ctor()",
"Sub (Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As (System.Int32, System.Int32))",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item8 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item7 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item9 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item8 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item1 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item9 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item2 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item1 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item3 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item2 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item4 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item3 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item5 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item4 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item6 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item5 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item7 As System.Int32",
"(Item9 As System.Int32, Item1 As System.Int32, Item2 As System.Int32, Item3 As System.Int32, Item4 As System.Int32, Item5 As System.Int32, Item6 As System.Int32, Item7 As System.Int32, Item8 As System.Int32).Item6 As System.Int32"
                )

        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_08()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
    End Sub

    Shared Function M8() As (a1 As Integer, a2 As Integer, a3 As Integer, a4 As Integer, a5 As Integer, a6 As Integer, a7 As Integer, Item1 As Integer)
        Return (801, 802, 803, 804, 805, 806, 807, 808)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.DebugExe, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37261: Tuple element name 'Item1' is only allowed at position 1.
    Shared Function M8() As (a1 As Integer, a2 As Integer, a3 As Integer, a4 As Integer, a5 As Integer, a6 As Integer, a7 As Integer, Item1 As Integer)
                                                                                                                                      ~~~~~
</errors>)

            Dim c = comp.GetTypeByMetadataName("C")

            Dim m8Tuple = DirectCast(c.GetMember(Of MethodSymbol)("M8").ReturnType, NamedTypeSymbol)
            AssertTupleTypeEquality(m8Tuple)

            AssertTestDisplayString(m8Tuple.GetMembers(),
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item1 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a1 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item2 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a2 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item3 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a3 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item4 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a4 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item5 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a5 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item6 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a6 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item7 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).a7 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Rest As ValueTuple(Of System.Int32)",
"Sub (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32)..ctor()",
"Sub (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32, item3 As System.Int32, item4 As System.Int32, item5 As System.Int32, item6 As System.Int32, item7 As System.Int32, rest As ValueTuple(Of System.Int32))",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Equals(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, ValueTuple(Of System.Int32))) As System.Boolean",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).CompareTo(other As System.ValueTuple(Of System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, ValueTuple(Of System.Int32))) As System.Int32",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).GetHashCode() As System.Int32",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).ToString() As System.String",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property (a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).System.ITupleInternal.Size As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item8 As System.Int32",
"(a1 As System.Int32, a2 As System.Int32, a3 As System.Int32, a4 As System.Int32, a5 As System.Int32, a6 As System.Int32, a7 As System.Int32, Item1 As System.Int32).Item1 As System.Int32"
)

            Dim m8Item8 = DirectCast(m8Tuple.GetMembers("Item8").Single(), FieldSymbol)

            AssertVirtualTupleElementField(m8Item8)

            Assert.True(m8Item8.IsTupleField)
            Assert.Same(m8Item8, m8Item8.OriginalDefinition)
            Assert.True(m8Item8.Equals(m8Item8))
            Assert.Equal("System.ValueTuple(Of System.Int32).Item1 As System.Int32", m8Item8.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m8Item8.AssociatedSymbol)
            Assert.Same(m8Tuple, m8Item8.ContainingSymbol)
            Assert.NotEqual(m8Tuple.TupleUnderlyingType, m8Item8.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m8Item8.CustomModifiers.IsEmpty)
            Assert.True(m8Item8.GetAttributes().IsEmpty)
            Assert.Null(m8Item8.GetUseSiteErrorInfo())
            Assert.False(m8Item8.Locations.IsEmpty)
            Assert.Equal("Item1", m8Item8.TupleUnderlyingField.Name)
            Assert.True(m8Item8.IsImplicitlyDeclared)
            Assert.Null(m8Item8.TypeLayoutOffset)

            Dim m8Item1 = DirectCast(m8Tuple.GetMembers("Item1").Last(), FieldSymbol)

            AssertVirtualTupleElementField(m8Item1)

            Assert.True(m8Item1.IsTupleField)
            Assert.Same(m8Item1, m8Item1.OriginalDefinition)
            Assert.True(m8Item1.Equals(m8Item1))
            Assert.Equal("System.ValueTuple(Of System.Int32).Item1 As System.Int32", m8Item1.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m8Item1.AssociatedSymbol)
            Assert.Same(m8Tuple, m8Item1.ContainingSymbol)
            Assert.NotEqual(m8Tuple.TupleUnderlyingType, m8Item1.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m8Item1.CustomModifiers.IsEmpty)
            Assert.True(m8Item1.GetAttributes().IsEmpty)
            Assert.Null(m8Item1.GetUseSiteErrorInfo())
            Assert.False(m8Item1.Locations.IsEmpty)
            Assert.Equal("Item1", m8Item1.TupleUnderlyingField.Name)
            Assert.False(m8Item1.IsImplicitlyDeclared)
            Assert.Null(m8Item1.TypeLayoutOffset)

            Dim m8TupleRestTuple = DirectCast(DirectCast(m8Tuple.GetMembers("Rest").Single(), FieldSymbol).Type, NamedTypeSymbol)
            AssertTupleTypeEquality(m8TupleRestTuple)

            AssertTestDisplayString(m8TupleRestTuple.GetMembers(),
"ValueTuple(Of System.Int32).Item1 As System.Int32",
"Sub ValueTuple(Of System.Int32)..ctor()",
"Sub ValueTuple(Of System.Int32)..ctor(item1 As System.Int32)",
"Function ValueTuple(Of System.Int32).Equals(obj As System.Object) As System.Boolean",
"Function ValueTuple(Of System.Int32).Equals(other As ValueTuple(Of System.Int32)) As System.Boolean",
"Function ValueTuple(Of System.Int32).System.Collections.IStructuralEquatable.Equals(other As System.Object, comparer As System.Collections.IEqualityComparer) As System.Boolean",
"Function ValueTuple(Of System.Int32).System.IComparable.CompareTo(other As System.Object) As System.Int32",
"Function ValueTuple(Of System.Int32).CompareTo(other As ValueTuple(Of System.Int32)) As System.Int32",
"Function ValueTuple(Of System.Int32).System.Collections.IStructuralComparable.CompareTo(other As System.Object, comparer As System.Collections.IComparer) As System.Int32",
"Function ValueTuple(Of System.Int32).GetHashCode() As System.Int32",
"Function ValueTuple(Of System.Int32).System.Collections.IStructuralEquatable.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function ValueTuple(Of System.Int32).System.ITupleInternal.GetHashCode(comparer As System.Collections.IEqualityComparer) As System.Int32",
"Function ValueTuple(Of System.Int32).ToString() As System.String",
"Function ValueTuple(Of System.Int32).System.ITupleInternal.ToStringEnd() As System.String",
"Function ValueTuple(Of System.Int32).System.ITupleInternal.get_Size() As System.Int32",
"ReadOnly Property ValueTuple(Of System.Int32).System.ITupleInternal.Size As System.Int32"
)

        End Sub

        <Fact>
        Public Sub DefaultAndFriendlyElementNames_09()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Shared Sub Main()
        Dim v1 = (1, 11)
        Console.WriteLine(v1.Item1)
        Console.WriteLine(v1.Item2)

        Dim v2 = (a2:=2, b2:=22)
        Console.WriteLine(v2.Item1)
        Console.WriteLine(v2.Item2)
        Console.WriteLine(v2.a2)
        Console.WriteLine(v2.b2)

        Dim v6 = (item1:=6, item2:=66)
        Console.WriteLine(v6.Item1)
        Console.WriteLine(v6.Item2)
        Console.WriteLine(v6.item1)
        Console.WriteLine(v6.item2)

        Console.WriteLine(v1.ToString())
        Console.WriteLine(v2.ToString())
        Console.WriteLine(v6.ToString())
    End Sub

End Class
<%= s_trivial2uple %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:="
1
11
2
22
2
22
6
66
6
66
{1, 11}
{2, 22}
{6, 66}
")

            Dim c = comp.GetTypeByMetadataName("C")
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim node = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().First()

            Dim m1Tuple = DirectCast(model.LookupSymbols(node.SpanStart, name:="v1").OfType(Of LocalSymbol)().Single().Type, NamedTypeSymbol)
            Dim m2Tuple = DirectCast(model.LookupSymbols(node.SpanStart, name:="v2").OfType(Of LocalSymbol)().Single().Type, NamedTypeSymbol)
            Dim m6Tuple = DirectCast(model.LookupSymbols(node.SpanStart, name:="v6").OfType(Of LocalSymbol)().Single().Type, NamedTypeSymbol)

            AssertTestDisplayString(m1Tuple.GetMembers(),
"Sub (System.Int32, System.Int32)..ctor()",
"(System.Int32, System.Int32).Item1 As System.Int32",
"(System.Int32, System.Int32).Item2 As System.Int32",
"Sub (System.Int32, System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
"Function (System.Int32, System.Int32).ToString() As System.String"
)

            AssertTestDisplayString(m2Tuple.GetMembers(),
"Sub (a2 As System.Int32, b2 As System.Int32)..ctor()",
"(a2 As System.Int32, b2 As System.Int32).Item1 As System.Int32",
"(a2 As System.Int32, b2 As System.Int32).a2 As System.Int32",
"(a2 As System.Int32, b2 As System.Int32).Item2 As System.Int32",
"(a2 As System.Int32, b2 As System.Int32).b2 As System.Int32",
"Sub (a2 As System.Int32, b2 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
"Function (a2 As System.Int32, b2 As System.Int32).ToString() As System.String"
)

            AssertTestDisplayString(m6Tuple.GetMembers(),
"Sub (Item1 As System.Int32, Item2 As System.Int32)..ctor()",
"(Item1 As System.Int32, Item2 As System.Int32).Item1 As System.Int32",
"(Item1 As System.Int32, Item2 As System.Int32).Item2 As System.Int32",
"Sub (Item1 As System.Int32, Item2 As System.Int32)..ctor(item1 As System.Int32, item2 As System.Int32)",
"Function (Item1 As System.Int32, Item2 As System.Int32).ToString() As System.String"
)

            Assert.Equal("", m1Tuple.Name)
            Assert.Equal(SymbolKind.NamedType, m1Tuple.Kind)
            Assert.Equal(TypeKind.Struct, m1Tuple.TypeKind)
            Assert.False(m1Tuple.IsImplicitlyDeclared)
            Assert.True(m1Tuple.IsTupleType)
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32)", m1Tuple.TupleUnderlyingType.ToTestDisplayString())
            Assert.Same(m1Tuple, m1Tuple.ConstructedFrom)
            Assert.Same(m1Tuple, m1Tuple.OriginalDefinition)
            AssertTupleTypeEquality(m1Tuple)
            Assert.Same(m1Tuple.TupleUnderlyingType.ContainingSymbol, m1Tuple.ContainingSymbol)
            Assert.Null(m1Tuple.GetUseSiteErrorInfo())
            Assert.Null(m1Tuple.EnumUnderlyingType)
            Assert.Equal({".ctor", "Item1", "Item2", "ToString"},
                         m1Tuple.MemberNames.ToArray())
            Assert.Equal({".ctor", "Item1", "a2", "Item2", "b2", "ToString"},
                         m2Tuple.MemberNames.ToArray())
            Assert.Equal(0, m1Tuple.Arity)
            Assert.True(m1Tuple.TypeParameters.IsEmpty)
            Assert.Equal("System.ValueType", m1Tuple.BaseType.ToTestDisplayString())
            Assert.False(m1Tuple.HasTypeArgumentsCustomModifiers)
            Assert.False(m1Tuple.IsComImport)
            Assert.True(m1Tuple.TypeArgumentsNoUseSiteDiagnostics.IsEmpty)
            Assert.True(m1Tuple.GetAttributes().IsEmpty)
            Assert.Equal("(a2 As System.Int32, b2 As System.Int32).Item1 As System.Int32", m2Tuple.GetMembers("Item1").Single().ToTestDisplayString())
            Assert.Equal("(a2 As System.Int32, b2 As System.Int32).a2 As System.Int32", m2Tuple.GetMembers("a2").Single().ToTestDisplayString())
            Assert.True(m1Tuple.GetTypeMembers().IsEmpty)
            Assert.True(m1Tuple.GetTypeMembers("C9").IsEmpty)
            Assert.True(m1Tuple.GetTypeMembers("C9", 0).IsEmpty)
            Assert.True(m1Tuple.Interfaces.IsEmpty)
            Assert.True(m1Tuple.GetTypeMembersUnordered().IsEmpty)
            Assert.Equal(1, m1Tuple.Locations.Length)
            Assert.Equal("(1, 11)", m1Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.Equal("(a2:=2, b2:=22)", m2Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.Equal("Public Structure ValueTuple(Of T1, T2)", m1Tuple.TupleUnderlyingType.DeclaringSyntaxReferences.Single().GetSyntax().ToString().Substring(0, 38))

            AssertTupleTypeEquality(m2Tuple)
            AssertTupleTypeEquality(m6Tuple)

            Assert.False(m1Tuple.Equals(m2Tuple))
            Assert.False(m1Tuple.Equals(m6Tuple))
            Assert.False(m6Tuple.Equals(m2Tuple))
            AssertTupleTypeMembersEquality(m1Tuple, m2Tuple)
            AssertTupleTypeMembersEquality(m1Tuple, m6Tuple)
            AssertTupleTypeMembersEquality(m2Tuple, m6Tuple)

            Dim m1Item1 = DirectCast(m1Tuple.GetMembers()(1), FieldSymbol)
            AssertNonvirtualTupleElementField(m1Item1)

            Assert.True(m1Item1.IsTupleField)
            Assert.Same(m1Item1, m1Item1.OriginalDefinition)
            Assert.True(m1Item1.Equals(m1Item1))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m1Item1.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m1Item1.AssociatedSymbol)
            Assert.Same(m1Tuple, m1Item1.ContainingSymbol)
            Assert.Same(m1Tuple.TupleUnderlyingType, m1Item1.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m1Item1.CustomModifiers.IsEmpty)
            Assert.True(m1Item1.GetAttributes().IsEmpty)
            Assert.Null(m1Item1.GetUseSiteErrorInfo())
            Assert.False(m1Item1.Locations.IsEmpty)
            Assert.True(m1Item1.DeclaringSyntaxReferences.IsEmpty)
            Assert.Equal("Item1", m1Item1.TupleUnderlyingField.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.True(m1Item1.IsImplicitlyDeclared)
            Assert.Null(m1Item1.TypeLayoutOffset)

            Dim m2Item1 = DirectCast(m2Tuple.GetMembers()(1), FieldSymbol)
            AssertNonvirtualTupleElementField(m2Item1)

            Assert.True(m2Item1.IsTupleField)
            Assert.Same(m2Item1, m2Item1.OriginalDefinition)
            Assert.True(m2Item1.Equals(m2Item1))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m2Item1.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m2Item1.AssociatedSymbol)
            Assert.Same(m2Tuple, m2Item1.ContainingSymbol)
            Assert.Same(m2Tuple.TupleUnderlyingType, m2Item1.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m2Item1.CustomModifiers.IsEmpty)
            Assert.True(m2Item1.GetAttributes().IsEmpty)
            Assert.Null(m2Item1.GetUseSiteErrorInfo())
            Assert.False(m2Item1.Locations.IsEmpty)
            Assert.True(m2Item1.DeclaringSyntaxReferences.IsEmpty)
            Assert.Equal("Item1", m2Item1.TupleUnderlyingField.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.NotEqual(m2Item1.Locations.Single(), m2Item1.TupleUnderlyingField.Locations.Single())
            Assert.Equal("SourceFile(a.vb[760..765))", m2Item1.TupleUnderlyingField.Locations.Single().ToString())
            Assert.Equal("SourceFile(a.vb[175..177))", m2Item1.Locations.Single().ToString())
            Assert.True(m2Item1.IsImplicitlyDeclared)
            Assert.Null(m2Item1.TypeLayoutOffset)

            Dim m2a2 = DirectCast(m2Tuple.GetMembers()(2), FieldSymbol)
            AssertVirtualTupleElementField(m2a2)

            Assert.True(m2a2.IsTupleField)
            Assert.Same(m2a2, m2a2.OriginalDefinition)
            Assert.True(m2a2.Equals(m2a2))
            Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32).Item1 As System.Int32", m2a2.TupleUnderlyingField.ToTestDisplayString())
            Assert.Null(m2a2.AssociatedSymbol)
            Assert.Same(m2Tuple, m2a2.ContainingSymbol)
            Assert.Same(m2Tuple.TupleUnderlyingType, m2a2.TupleUnderlyingField.ContainingSymbol)
            Assert.True(m2a2.CustomModifiers.IsEmpty)
            Assert.True(m2a2.GetAttributes().IsEmpty)
            Assert.Null(m2a2.GetUseSiteErrorInfo())
            Assert.False(m2a2.Locations.IsEmpty)
            Assert.Equal("a2", m2a2.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.Equal("Item1", m2a2.TupleUnderlyingField.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.False(m2a2.IsImplicitlyDeclared)
            Assert.Null(m2a2.TypeLayoutOffset)

            Dim m1ToString = m1Tuple.GetMember(Of MethodSymbol)("ToString")

            Assert.True(m1ToString.IsTupleMethod)
            Assert.Same(m1ToString, m1ToString.OriginalDefinition)
            Assert.Same(m1ToString, m1ToString.ConstructedFrom)
            Assert.Equal("Function System.ValueTuple(Of System.Int32, System.Int32).ToString() As System.String",
                         m1ToString.TupleUnderlyingMethod.ToTestDisplayString())
            Assert.Same(m1ToString.TupleUnderlyingMethod, m1ToString.TupleUnderlyingMethod.ConstructedFrom)
            Assert.Same(m1Tuple, m1ToString.ContainingSymbol)
            Assert.Same(m1Tuple.TupleUnderlyingType, m1ToString.TupleUnderlyingMethod.ContainingType)
            Assert.Null(m1ToString.AssociatedSymbol)
            Assert.True(m1ToString.ExplicitInterfaceImplementations.IsEmpty)
            Assert.False(m1ToString.ReturnType.SpecialType = SpecialType.System_Void)
            Assert.True(m1ToString.TypeArguments.IsEmpty)
            Assert.True(m1ToString.TypeParameters.IsEmpty)
            Assert.True(m1ToString.GetAttributes().IsEmpty)
            Assert.Null(m1ToString.GetUseSiteErrorInfo())
            Assert.Equal("Function System.ValueType.ToString() As System.String",
                         m1ToString.OverriddenMethod.ToTestDisplayString())
            Assert.False(m1ToString.Locations.IsEmpty)
            Assert.Equal("Public Overrides Function ToString()", m1ToString.DeclaringSyntaxReferences.Single().GetSyntax().ToString().Substring(0, 36))
            Assert.Equal(m1ToString.Locations.Single(), m1ToString.TupleUnderlyingMethod.Locations.Single())

        End Sub

        <Fact>
        Public Sub OverriddenMethodWithDifferentTupleNamesInReturn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Function M1() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
    Public Overridable Function M2() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
    Public Overridable Function M3() As (a As Integer, b As Integer)()
        Return {(1, 2)}
    End Function
    Public Overridable Function M4() As (a As Integer, b As Integer)?
        Return (1, 2)
    End Function
    Public Overridable Function M5() As (c As (a As Integer, b As Integer), d As Integer)
        Return ((1, 2), 3)
    End Function
End Class

Public Class Derived
    Inherits Base

    Public Overrides Function M1() As (A As Integer, B As Integer)
        Return (1, 2)
    End Function
    Public Overrides Function M2() As (notA As Integer, notB As Integer)
        Return (1, 2)
    End Function
    Public Overrides Function M3() As (notA As Integer, notB As Integer)()
        Return {(1, 2)}
    End Function
    Public Overrides Function M4() As (notA As Integer, notB As Integer)?
        Return (1, 2)
    End Function
    Public Overrides Function M5() As (c As (notA As Integer, notB As Integer), d As Integer)
        Return ((1, 2), 3)
    End Function
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Function M2() As (notA As Integer, notB As Integer)' cannot override 'Public Overridable Function M2() As (a As Integer, b As Integer)' because they differ by their tuple element names.
    Public Overrides Function M2() As (notA As Integer, notB As Integer)
                              ~~
BC40001: 'Public Overrides Function M3() As (notA As Integer, notB As Integer)()' cannot override 'Public Overridable Function M3() As (a As Integer, b As Integer)()' because they differ by their tuple element names.
    Public Overrides Function M3() As (notA As Integer, notB As Integer)()
                              ~~
BC40001: 'Public Overrides Function M4() As (notA As Integer, notB As Integer)?' cannot override 'Public Overridable Function M4() As (a As Integer, b As Integer)?' because they differ by their tuple element names.
    Public Overrides Function M4() As (notA As Integer, notB As Integer)?
                              ~~
BC40001: 'Public Overrides Function M5() As (c As (notA As Integer, notB As Integer), d As Integer)' cannot override 'Public Overridable Function M5() As (c As (a As Integer, b As Integer), d As Integer)' because they differ by their tuple element names.
    Public Overrides Function M5() As (c As (notA As Integer, notB As Integer), d As Integer)
                              ~~
</errors>)

            Dim m3 = comp.GetMember(Of MethodSymbol)("Derived.M3").ReturnType
            Assert.Equal("(notA As System.Int32, notB As System.Int32)()", m3.ToTestDisplayString())
            Assert.Equal({"System.Collections.Generic.IList(Of (notA As System.Int32, notB As System.Int32))"},
                         m3.Interfaces.SelectAsArray(Function(t) t.ToTestDisplayString()))

        End Sub

        <Fact>
        Public Sub OverriddenMethodWithNoTupleNamesInReturn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Function M6() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
End Class

Public Class Derived
    Inherits Base

    Public Overrides Function M6() As (Integer, Integer)
        Return (1, 2)
    End Function
    Sub M()
        Dim result = Me.M6()
        Dim result2 = MyBase.M6()
        System.Console.Write(result.a)
        System.Console.Write(result2.a)
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30456: 'a' is not a member of '(Integer, Integer)'.
        System.Console.Write(result.a)
                             ~~~~~~~~
</errors>)

            Dim m6 = comp.GetMember(Of MethodSymbol)("Derived.M6").ReturnType
            Assert.Equal("(System.Int32, System.Int32)", m6.ToTestDisplayString())

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(0)
            Assert.Equal("Me.M6()", invocation.ToString())
            Assert.Equal("Function Derived.M6() As (System.Int32, System.Int32)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString())

            Dim invocation2 = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("MyBase.M6()", invocation2.ToString())
            Assert.Equal("Function Base.M6() As (a As System.Int32, b As System.Int32)", model.GetSymbolInfo(invocation2).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub OverriddenMethodWithDifferentTupleNamesInReturnUsingTypeArg()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Function M1(Of T)() As (a As T, b As T)
        Return (Nothing, Nothing)
    End Function
    Public Overridable Function M2(Of T)() As (a As T, b As T)
        Return (Nothing, Nothing)
    End Function
    Public Overridable Function M3(Of T)() As (a As T, b As T)()
        Return {(Nothing, Nothing)}
    End Function
    Public Overridable Function M4(Of T)() As (a As T, b As T)?
        Return (Nothing, Nothing)
    End Function
    Public Overridable Function M5(Of T)() As (c As (a As T, b As T), d As T)
        Return ((Nothing, Nothing), Nothing)
    End Function
End Class

Public Class Derived
    Inherits Base

    Public Overrides Function M1(Of T)() As (A As T, B As T)
        Return (Nothing, Nothing)
    End Function
    Public Overrides Function M2(Of T)() As (notA As T, notB As T)
        Return (Nothing, Nothing)
    End Function
    Public Overrides Function M3(Of T)() As (notA As T, notB As T)()
        Return {(Nothing, Nothing)}
    End Function
    Public Overrides Function M4(Of T)() As (notA As T, notB As T)?
        Return (Nothing, Nothing)
    End Function
    Public Overrides Function M5(Of T)() As (c As (notA As T, notB As T), d As T)
        Return ((Nothing, Nothing), Nothing)
    End Function
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Function M2(Of T)() As (notA As T, notB As T)' cannot override 'Public Overridable Function M2(Of T)() As (a As T, b As T)' because they differ by their tuple element names.
    Public Overrides Function M2(Of T)() As (notA As T, notB As T)
                              ~~
BC40001: 'Public Overrides Function M3(Of T)() As (notA As T, notB As T)()' cannot override 'Public Overridable Function M3(Of T)() As (a As T, b As T)()' because they differ by their tuple element names.
    Public Overrides Function M3(Of T)() As (notA As T, notB As T)()
                              ~~
BC40001: 'Public Overrides Function M4(Of T)() As (notA As T, notB As T)?' cannot override 'Public Overridable Function M4(Of T)() As (a As T, b As T)?' because they differ by their tuple element names.
    Public Overrides Function M4(Of T)() As (notA As T, notB As T)?
                              ~~
BC40001: 'Public Overrides Function M5(Of T)() As (c As (notA As T, notB As T), d As T)' cannot override 'Public Overridable Function M5(Of T)() As (c As (a As T, b As T), d As T)' because they differ by their tuple element names.
    Public Overrides Function M5(Of T)() As (c As (notA As T, notB As T), d As T)
                              ~~
</errors>)

        End Sub

        <Fact>
        Public Sub OverridenMethodWithDifferentTupleNamesInParameters()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Sub M1(x As (a As Integer, b As Integer))
    End Sub
    Public Overridable Sub M2(x As (a As Integer, b As Integer))
    End Sub
    Public Overridable Sub M3(x As (a As Integer, b As Integer)())
    End Sub
    Public Overridable Sub M4(x As (a As Integer, b As Integer)?)
    End Sub
    Public Overridable Sub M5(x As (c As (a As Integer, b As Integer), d As Integer))
    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Overrides Sub M1(x As (A As Integer, B As Integer))
    End Sub
    Public Overrides Sub M2(x As (notA As Integer, notB As Integer))
    End Sub
    Public Overrides Sub M3(x As (notA As Integer, notB As Integer)())
    End Sub
    Public Overrides Sub M4(x As (notA As Integer, notB As Integer)?)
    End Sub
    Public Overrides Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Sub M2(x As (notA As Integer, notB As Integer))' cannot override 'Public Overridable Sub M2(x As (a As Integer, b As Integer))' because they differ by their tuple element names.
    Public Overrides Sub M2(x As (notA As Integer, notB As Integer))
                         ~~
BC40001: 'Public Overrides Sub M3(x As (notA As Integer, notB As Integer)())' cannot override 'Public Overridable Sub M3(x As (a As Integer, b As Integer)())' because they differ by their tuple element names.
    Public Overrides Sub M3(x As (notA As Integer, notB As Integer)())
                         ~~
BC40001: 'Public Overrides Sub M4(x As (notA As Integer, notB As Integer)?)' cannot override 'Public Overridable Sub M4(x As (a As Integer, b As Integer)?)' because they differ by their tuple element names.
    Public Overrides Sub M4(x As (notA As Integer, notB As Integer)?)
                         ~~
BC40001: 'Public Overrides Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))' cannot override 'Public Overridable Sub M5(x As (c As (a As Integer, b As Integer), d As Integer))' because they differ by their tuple element names.
    Public Overrides Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))
                         ~~
</errors>)

        End Sub

        <Fact>
        Public Sub OverriddenMethodWithDifferentTupleNamesInParametersUsingTypeArg()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Sub M1(Of T)(x As (a As T, b As T))
    End Sub
    Public Overridable Sub M2(Of T)(x As (a As T, b As T))
    End Sub
    Public Overridable Sub M3(Of T)(x As (a As T, b As T)())
    End Sub
    Public Overridable Sub M4(Of T)(x As (a As T, b As T)?)
    End Sub
    Public Overridable Sub M5(Of T)(x As (c As (a As T, b As T), d As T))
    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Overrides Sub M1(Of T)(x As (A As T, B As T))
    End Sub
    Public Overrides Sub M2(Of T)(x As (notA As T, notB As T))
    End Sub
    Public Overrides Sub M3(Of T)(x As (notA As T, notB As T)())
    End Sub
    Public Overrides Sub M4(Of T)(x As (notA As T, notB As T)?)
    End Sub
    Public Overrides Sub M5(Of T)(x As (c As (notA As T, notB As T), d As T))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Sub M2(Of T)(x As (notA As T, notB As T))' cannot override 'Public Overridable Sub M2(Of T)(x As (a As T, b As T))' because they differ by their tuple element names.
    Public Overrides Sub M2(Of T)(x As (notA As T, notB As T))
                         ~~
BC40001: 'Public Overrides Sub M3(Of T)(x As (notA As T, notB As T)())' cannot override 'Public Overridable Sub M3(Of T)(x As (a As T, b As T)())' because they differ by their tuple element names.
    Public Overrides Sub M3(Of T)(x As (notA As T, notB As T)())
                         ~~
BC40001: 'Public Overrides Sub M4(Of T)(x As (notA As T, notB As T)?)' cannot override 'Public Overridable Sub M4(Of T)(x As (a As T, b As T)?)' because they differ by their tuple element names.
    Public Overrides Sub M4(Of T)(x As (notA As T, notB As T)?)
                         ~~
BC40001: 'Public Overrides Sub M5(Of T)(x As (c As (notA As T, notB As T), d As T))' cannot override 'Public Overridable Sub M5(Of T)(x As (c As (a As T, b As T), d As T))' because they differ by their tuple element names.
    Public Overrides Sub M5(Of T)(x As (c As (notA As T, notB As T), d As T))
                         ~~
</errors>)

        End Sub

        <Fact>
        Public Sub HiddenMethodsWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Function M1() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
    Public Overridable Function M2() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
    Public Overridable Function M3() As (a As Integer, b As Integer)()
        Return {(1, 2)}
    End Function
    Public Overridable Function M4() As (a As Integer, b As Integer)?
        Return (1, 2)
    End Function
    Public Overridable Function M5() As (c As (a As Integer, b As Integer), d As Integer)
        Return ((1, 2), 3)
    End Function
End Class

Public Class Derived
    Inherits Base

    Public Function M1() As (A As Integer, B As Integer)
        Return (1, 2)
    End Function
    Public Function M2() As (notA As Integer, notB As Integer)
        Return (1, 2)
    End Function
    Public Function M3() As (notA As Integer, notB As Integer)()
        Return {(1, 2)}
    End Function
    Public Function M4() As (notA As Integer, notB As Integer)?
        Return (1, 2)
    End Function
    Public Function M5() As (c As (notA As Integer, notB As Integer), d As Integer)
        Return ((1, 2), 3)
    End Function
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40005: function 'M1' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Function M1() As (A As Integer, B As Integer)
                    ~~
BC40005: function 'M2' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Function M2() As (notA As Integer, notB As Integer)
                    ~~
BC40005: function 'M3' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Function M3() As (notA As Integer, notB As Integer)()
                    ~~
BC40005: function 'M4' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Function M4() As (notA As Integer, notB As Integer)?
                    ~~
BC40005: function 'M5' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Function M5() As (c As (notA As Integer, notB As Integer), d As Integer)
                    ~~
</errors>)

        End Sub

        <Fact>
        Public Sub DuplicateMethodDetectionWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Public Sub M1(x As (A As Integer, B As Integer))
    End Sub
    Public Sub M1(x As (a As Integer, b As Integer))
    End Sub

    Public Sub M2(x As (noIntegerA As Integer, noIntegerB As Integer))
    End Sub
    Public Sub M2(x As (a As Integer, b As Integer))
    End Sub

    Public Sub M3(x As (notA As Integer, notB As Integer)())
    End Sub
    Public Sub M3(x As (a As Integer, b As Integer)())
    End Sub

    Public Sub M4(x As (notA As Integer, notB As Integer)?)
    End Sub
    Public Sub M4(x As (a As Integer, b As Integer)?)
    End Sub

    Public Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))
    End Sub
    Public Sub M5(x As (c As (a As Integer, b As Integer), d As Integer))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30269: 'Public Sub M1(x As (A As Integer, B As Integer))' has multiple definitions with identical signatures.
    Public Sub M1(x As (A As Integer, B As Integer))
               ~~
BC37271: 'Public Sub M2(x As (noIntegerA As Integer, noIntegerB As Integer))' has multiple definitions with identical signatures with different tuple element names, including 'Public Sub M2(x As (a As Integer, b As Integer))'.
    Public Sub M2(x As (noIntegerA As Integer, noIntegerB As Integer))
               ~~
BC37271: 'Public Sub M3(x As (notA As Integer, notB As Integer)())' has multiple definitions with identical signatures with different tuple element names, including 'Public Sub M3(x As (a As Integer, b As Integer)())'.
    Public Sub M3(x As (notA As Integer, notB As Integer)())
               ~~
BC37271: 'Public Sub M4(x As (notA As Integer, notB As Integer)?)' has multiple definitions with identical signatures with different tuple element names, including 'Public Sub M4(x As (a As Integer, b As Integer)?)'.
    Public Sub M4(x As (notA As Integer, notB As Integer)?)
               ~~
BC37271: 'Public Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))' has multiple definitions with identical signatures with different tuple element names, including 'Public Sub M5(x As (c As (a As Integer, b As Integer), d As Integer))'.
    Public Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))
               ~~
</errors>)

        End Sub

        <Fact>
        Public Sub HiddenMethodParametersWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Sub M1(x As (a As Integer, b As Integer))
    End Sub
    Public Overridable Sub M2(x As (a As Integer, b As Integer))
    End Sub
    Public Overridable Sub M3(x As (a As Integer, b As Integer)())
    End Sub
    Public Overridable Sub M4(x As (a As Integer, b As Integer)?)
    End Sub
    Public Overridable Sub M5(x As (c As (a As Integer, b As Integer), d As Integer))
    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Sub M1(x As (A As Integer, B As Integer))
    End Sub
    Public Sub M2(x As (notA As Integer, notB As Integer))
    End Sub
    Public Sub M3(x As (notA As Integer, notB As Integer)())
    End Sub
    Public Sub M4(x As (notA As Integer, notB As Integer)?)
    End Sub
    Public Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40005: sub 'M1' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Sub M1(x As (A As Integer, B As Integer))
               ~~
BC40005: sub 'M2' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Sub M2(x As (notA As Integer, notB As Integer))
               ~~
BC40005: sub 'M3' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Sub M3(x As (notA As Integer, notB As Integer)())
               ~~
BC40005: sub 'M4' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Sub M4(x As (notA As Integer, notB As Integer)?)
               ~~
BC40005: sub 'M5' shadows an overridable method in the base class 'Base'. To override the base method, this method must be declared 'Overrides'.
    Public Sub M5(x As (c As (notA As Integer, notB As Integer), d As Integer))
               ~~
</errors>)

        End Sub

        <Fact>
        Public Sub ExplicitInterfaceImplementationWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0
    Sub M1(x As (Integer, (Integer, c As Integer)))
    Sub M2(x As (a As Integer, (b As Integer, c As Integer)))
    Function MR1() As (Integer, (Integer, c As Integer))
    Function MR2() As (a As Integer, (b As Integer, c As Integer))
End Interface

Public Class Derived
    Implements I0

    Public Sub M1(x As (notMissing As Integer, (notMissing As Integer, c As Integer))) Implements I0.M1
    End Sub
    Public Sub M2(x As (notA As Integer, (notB As Integer, c As Integer))) Implements I0.M2
    End Sub
    Public Function MR1() As (notMissing As Integer, (notMissing As Integer, c As Integer)) Implements I0.MR1
        Return (1, (2, 3))
    End Function
    Public Function MR2() As (notA As Integer, (notB As Integer, c As Integer)) Implements I0.MR2
        Return (1, (2, 3))
    End Function
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30402: 'M1' cannot implement sub 'M1' on interface 'I0' because the tuple element names in 'Public Sub M1(x As (notMissing As Integer, (notMissing As Integer, c As Integer)))' do not match those in 'Sub M1(x As (Integer, (Integer, c As Integer)))'.
    Public Sub M1(x As (notMissing As Integer, (notMissing As Integer, c As Integer))) Implements I0.M1
                                                                                                  ~~~~~
BC30402: 'M2' cannot implement sub 'M2' on interface 'I0' because the tuple element names in 'Public Sub M2(x As (notA As Integer, (notB As Integer, c As Integer)))' do not match those in 'Sub M2(x As (a As Integer, (b As Integer, c As Integer)))'.
    Public Sub M2(x As (notA As Integer, (notB As Integer, c As Integer))) Implements I0.M2
                                                                                      ~~~~~
BC30402: 'MR1' cannot implement function 'MR1' on interface 'I0' because the tuple element names in 'Public Function MR1() As (notMissing As Integer, (notMissing As Integer, c As Integer))' do not match those in 'Function MR1() As (Integer, (Integer, c As Integer))'.
    Public Function MR1() As (notMissing As Integer, (notMissing As Integer, c As Integer)) Implements I0.MR1
                                                                                                       ~~~~~~
BC30402: 'MR2' cannot implement function 'MR2' on interface 'I0' because the tuple element names in 'Public Function MR2() As (notA As Integer, (notB As Integer, c As Integer))' do not match those in 'Function MR2() As (a As Integer, (b As Integer, c As Integer))'.
    Public Function MR2() As (notA As Integer, (notB As Integer, c As Integer)) Implements I0.MR2
                                                                                           ~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub InterfaceImplementationOfPropertyWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0
    Property P1 As (a As Integer, b As Integer)
    Property P2 As (Integer, b As Integer)
End Interface

Public Class Derived
    Implements I0

    Public Property P1 As (notA As Integer, notB As Integer) Implements I0.P1
    Public Property P2 As (notMissing As Integer, b As Integer) Implements I0.P2
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30402: 'P1' cannot implement property 'P1' on interface 'I0' because the tuple element names in 'Public Property P1 As (notA As Integer, notB As Integer)' do not match those in 'Property P1 As (a As Integer, b As Integer)'.
    Public Property P1 As (notA As Integer, notB As Integer) Implements I0.P1
                                                                        ~~~~~
BC30402: 'P2' cannot implement property 'P2' on interface 'I0' because the tuple element names in 'Public Property P2 As (notMissing As Integer, b As Integer)' do not match those in 'Property P2 As (Integer, b As Integer)'.
    Public Property P2 As (notMissing As Integer, b As Integer) Implements I0.P2
                                                                           ~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub InterfaceImplementationOfEventWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Interface I0
    Event E1 As Action(Of (a As Integer, b As Integer))
    Event E2 As Action(Of (Integer, b As Integer))
End Interface

Public Class Derived
    Implements I0

    Public Event E1 As Action(Of (notA As Integer, notB As Integer)) Implements I0.E1
    Public Event E2 As Action(Of (notMissing As Integer, notB As Integer)) Implements I0.E2
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30402: 'E1' cannot implement event 'E1' on interface 'I0' because the tuple element names in 'Public Event E1 As Action(Of (notA As Integer, notB As Integer))' do not match those in 'Event E1 As Action(Of (a As Integer, b As Integer))'.
    Public Event E1 As Action(Of (notA As Integer, notB As Integer)) Implements I0.E1
                                                                                ~~~~~
BC30402: 'E2' cannot implement event 'E2' on interface 'I0' because the tuple element names in 'Public Event E2 As Action(Of (notMissing As Integer, notB As Integer))' do not match those in 'Event E2 As Action(Of (Integer, b As Integer))'.
    Public Event E2 As Action(Of (notMissing As Integer, notB As Integer)) Implements I0.E2
                                                                                      ~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub InterfaceHidingAnotherInterfaceWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0
    Sub M1(x As (a As Integer, b As Integer))
    Sub M2(x As (a As Integer, b As Integer))

    Function MR1() As (a As Integer, b As Integer)
    Function MR2() As (a As Integer, b As Integer)
End Interface

Public Interface I1
    Inherits I0

    Sub M1(x As (notA As Integer, b As Integer))
    Shadows Sub M2(x As (notA As Integer, b As Integer))

    Function MR1() As (notA As Integer, b As Integer)
    Shadows Function MR2() As (notA As Integer, b As Integer)
End Interface
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40003: sub 'M1' shadows an overloadable member declared in the base interface 'I0'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Sub M1(x As (notA As Integer, b As Integer))
        ~~
BC40003: function 'MR1' shadows an overloadable member declared in the base interface 'I0'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Function MR1() As (notA As Integer, b As Integer)
             ~~~
</errors>)

        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0(Of T)
End Interface

Public Class C1
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (notA As Integer, notB As Integer))
End Class
Public Class C2
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (a As Integer, b As Integer))
End Class
Public Class C3
    Implements I0(Of Integer), I0(Of Integer)
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37272: Interface 'I0(Of (notA As Integer, notB As Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (notA As Integer, notB As Integer))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31033: Interface 'I0(Of (a As Integer, b As Integer))' can be implemented only once by this type.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (a As Integer, b As Integer))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31033: Interface 'I0(Of Integer)' can be implemented only once by this type.
    Implements I0(Of Integer), I0(Of Integer)
                               ~~~~~~~~~~~~~~
</errors>)
            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim c1 = model.GetDeclaredSymbol(nodes.OfType(Of TypeBlockSyntax)().ElementAt(1))
            Assert.Equal("C1", c1.Name)
            Assert.Equal(2, c1.AllInterfaces.Count)
            Assert.Equal("I0(Of (a As System.Int32, b As System.Int32))", c1.AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I0(Of (notA As System.Int32, notB As System.Int32))", c1.AllInterfaces(1).ToTestDisplayString())

            Dim c2 = model.GetDeclaredSymbol(nodes.OfType(Of TypeBlockSyntax)().ElementAt(2))
            Assert.Equal("C2", c2.Name)
            Assert.Equal(1, c2.AllInterfaces.Count)
            Assert.Equal("I0(Of (a As System.Int32, b As System.Int32))", c2.AllInterfaces(0).ToTestDisplayString())

            Dim c3 = model.GetDeclaredSymbol(nodes.OfType(Of TypeBlockSyntax)().ElementAt(3))
            Assert.Equal("C3", c3.Name)
            Assert.Equal(1, c3.AllInterfaces.Count)
            Assert.Equal("I0(Of System.Int32)", c3.AllInterfaces(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_02()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb">
public interface I1(Of T) 
    Sub M()
end interface

public interface I2
    Inherits I1(Of (a As Integer, b As Integer))
end interface

public interface I3
    Inherits I1(Of (c As Integer, d As Integer))
end interface

public class C1 
    Implements I2, I1(Of (c As Integer, d As Integer)) 

    Sub M_1() Implements I1(Of (a As Integer, b As Integer)).M
    End Sub
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C1
    End Sub
End class

public class C2
    Implements I1(Of (c As Integer, d As Integer)), I2 

    Sub M_1() Implements I1(Of (a As Integer, b As Integer)).M
    End Sub
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C2
    End Sub
End class

public class C3
    Implements I1(Of (a As Integer, b As Integer)), I1(Of (c As Integer, d As Integer))

    Sub M_1() Implements I1(Of (a As Integer, b As Integer)).M
    End Sub
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C3
    End Sub
End class

public class C4
    Implements I2, I3 

    Sub M_1() Implements I1(Of (a As Integer, b As Integer)).M
    End Sub
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C4
    End Sub
End class
    </file>
</compilation>
            )

            comp.AssertTheseDiagnostics(
<errors>
BC37273: Interface 'I1(Of (c As Integer, d As Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (a As Integer, b As Integer))' (via 'I2').
    Implements I2, I1(Of (c As Integer, d As Integer)) 
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30583: 'I1(Of (c As Integer, d As Integer)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C1
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37274: Interface 'I1(Of (a As Integer, b As Integer))' (via 'I2') can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (c As Integer, d As Integer))'.
    Implements I1(Of (c As Integer, d As Integer)), I2 
                                                    ~~
BC30583: 'I1(Of (c As Integer, d As Integer)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C2
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37272: Interface 'I1(Of (c As Integer, d As Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (a As Integer, b As Integer))'.
    Implements I1(Of (a As Integer, b As Integer)), I1(Of (c As Integer, d As Integer))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30583: 'I1(Of (c As Integer, d As Integer)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C3
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37275: Interface 'I1(Of (c As Integer, d As Integer))' (via 'I3') can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (a As Integer, b As Integer))' (via 'I2').
    Implements I2, I3 
                   ~~
BC30583: 'I1(Of (c As Integer, d As Integer)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As Integer, d As Integer)).M ' C4
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

            Dim c1 As INamedTypeSymbol = comp.GetTypeByMetadataName("C1")
            Dim c1Interfaces = c1.Interfaces
            Dim c1AllInterfaces = c1.AllInterfaces
            Assert.Equal(2, c1Interfaces.Length)
            Assert.Equal(3, c1AllInterfaces.Length)
            Assert.Equal("I2", c1Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c1Interfaces(1).ToTestDisplayString())
            Assert.Equal("I2", c1AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c1AllInterfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c1AllInterfaces(2).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_02_AssertExplicitInterfaceImplementations(c1)

            Dim c2 As INamedTypeSymbol = comp.GetTypeByMetadataName("C2")
            Dim c2Interfaces = c2.Interfaces
            Dim c2AllInterfaces = c2.AllInterfaces
            Assert.Equal(2, c2Interfaces.Length)
            Assert.Equal(3, c2AllInterfaces.Length)
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c2Interfaces(0).ToTestDisplayString())
            Assert.Equal("I2", c2Interfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c2AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I2", c2AllInterfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c2AllInterfaces(2).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_02_AssertExplicitInterfaceImplementations(c2)

            Dim c3 As INamedTypeSymbol = comp.GetTypeByMetadataName("C3")
            Dim c3Interfaces = c3.Interfaces
            Dim c3AllInterfaces = c3.AllInterfaces
            Assert.Equal(2, c3Interfaces.Length)
            Assert.Equal(2, c3AllInterfaces.Length)
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c3Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c3Interfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c3AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c3AllInterfaces(1).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_02_AssertExplicitInterfaceImplementations(c3)

            Dim c4 As INamedTypeSymbol = comp.GetTypeByMetadataName("C4")
            Dim c4Interfaces = c4.Interfaces
            Dim c4AllInterfaces = c4.AllInterfaces
            Assert.Equal(2, c4Interfaces.Length)
            Assert.Equal(4, c4AllInterfaces.Length)
            Assert.Equal("I2", c4Interfaces(0).ToTestDisplayString())
            Assert.Equal("I3", c4Interfaces(1).ToTestDisplayString())
            Assert.Equal("I2", c4AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c4AllInterfaces(1).ToTestDisplayString())
            Assert.Equal("I3", c4AllInterfaces(2).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c4AllInterfaces(3).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_02_AssertExplicitInterfaceImplementations(c4)
        End Sub

        Private Shared Sub DuplicateInterfaceDetectionWithDifferentTupleNames_02_AssertExplicitInterfaceImplementations(c As INamedTypeSymbol)
            Dim cMabImplementations = DirectCast(DirectCast(c, TypeSymbol).GetMember("M_1"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, cMabImplementations.Length)
            Assert.Equal("Sub I1(Of (a As System.Int32, b As System.Int32)).M()", cMabImplementations(0).ToTestDisplayString())
            Dim cMcdImplementations = DirectCast(DirectCast(c, TypeSymbol).GetMember("M_2"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, cMcdImplementations.Length)
            Assert.Equal("Sub I1(Of (c As System.Int32, d As System.Int32)).M()", cMcdImplementations(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_03()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb">
public interface I1(Of T) 
    Sub M()
end interface

public class C1 
    Implements I1(Of (a As Integer, b As Integer)) 

    Sub M() Implements I1(Of (a As Integer, b As Integer)).M
        System.Console.WriteLine("C1.M")
    End Sub
End class

public class C2
    Inherits C1
    Implements I1(Of (c As Integer, d As Integer)) 

    Overloads Sub M() Implements I1(Of (c As Integer, d As Integer)).M
        System.Console.WriteLine("C2.M")
    End Sub

    Shared Sub Main()
        Dim x As C1 = new C2()
        Dim y As I1(Of (a As Integer, b As Integer)) = x
        y.M()
    End Sub
End class
    </file>
</compilation>,
            options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics()

            Dim validate As Action(Of ModuleSymbol) =
            Sub(m)
                Dim isMetadata As Boolean = TypeOf m Is PEModuleSymbol

                Dim c1 As INamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C1")
                Dim c1Interfaces = c1.Interfaces
                Dim c1AllInterfaces = c1.AllInterfaces
                Assert.Equal(1, c1Interfaces.Length)
                Assert.Equal(1, c1AllInterfaces.Length)
                Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c1Interfaces(0).ToTestDisplayString())
                Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c1AllInterfaces(0).ToTestDisplayString())

                Dim c2 As INamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C2")
                Dim c2Interfaces = c2.Interfaces
                Dim c2AllInterfaces = c2.AllInterfaces
                Assert.Equal(1, c2Interfaces.Length)
                Assert.Equal(2, c2AllInterfaces.Length)
                Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c2Interfaces(0).ToTestDisplayString())
                Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c2AllInterfaces(0).ToTestDisplayString())
                Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c2AllInterfaces(1).ToTestDisplayString())

                Dim m2 = DirectCast(DirectCast(c2, TypeSymbol).GetMember("M"), IMethodSymbol)
                Dim m2Implementations = m2.ExplicitInterfaceImplementations
                Assert.Equal(1, m2Implementations.Length)
                Assert.Equal(If(isMetadata,
                                 "Sub I1(Of (System.Int32, System.Int32)).M()",
                                 "Sub I1(Of (c As System.Int32, d As System.Int32)).M()"),
                             m2Implementations(0).ToTestDisplayString())

                Assert.Same(m2, c2.FindImplementationForInterfaceMember(DirectCast(c2Interfaces(0), TypeSymbol).GetMember("M")))
                Assert.Same(m2, c2.FindImplementationForInterfaceMember(DirectCast(c1Interfaces(0), TypeSymbol).GetMember("M")))
            End Sub

            CompileAndVerify(comp, sourceSymbolValidator:=validate, symbolValidator:=validate, expectedOutput:="C2.M")
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_04()

            Dim csSource = "
public interface I1<T> 
{
    void M();
}
public class C1 : I1<(int a, int b)> 
{ 
    public void M() => System.Console.WriteLine(""C1.M"");
}

public class C2 : C1, I1<(int c, int d)> 
{ 
    new public void M() => System.Console.WriteLine(""C2.M""); 
}
"
            Dim csComp = CreateCSharpCompilation(csSource,
                                                 compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                 referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.StandardAndVBRuntime))
            csComp.VerifyDiagnostics()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb">
public class C3
    Shared Sub Main()
        Dim x As C1 = new C2()
        Dim y As I1(Of (a As Integer, b As Integer)) = x
        y.M()
    End Sub
End class
    </file>
</compilation>,
            options:=TestOptions.DebugExe, references:={csComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics()

            CompileAndVerify(comp, expectedOutput:="C2.M")

            Dim c1 As INamedTypeSymbol = comp.GlobalNamespace.GetTypeMember("C1")
            Dim c1Interfaces = c1.Interfaces
            Dim c1AllInterfaces = c1.AllInterfaces
            Assert.Equal(1, c1Interfaces.Length)
            Assert.Equal(1, c1AllInterfaces.Length)
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c1Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c1AllInterfaces(0).ToTestDisplayString())

            Dim c2 As INamedTypeSymbol = comp.GlobalNamespace.GetTypeMember("C2")
            Dim c2Interfaces = c2.Interfaces
            Dim c2AllInterfaces = c2.AllInterfaces
            Assert.Equal(1, c2Interfaces.Length)
            Assert.Equal(2, c2AllInterfaces.Length)
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c2Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c2AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As System.Int32, d As System.Int32))", c2AllInterfaces(1).ToTestDisplayString())

            Dim m2 = DirectCast(DirectCast(c2, TypeSymbol).GetMember("M"), IMethodSymbol)
            Dim m2Implementations = m2.ExplicitInterfaceImplementations
            Assert.Equal(0, m2Implementations.Length)

            Assert.Same(m2, c2.FindImplementationForInterfaceMember(DirectCast(c2Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m2, c2.FindImplementationForInterfaceMember(DirectCast(c1Interfaces(0), TypeSymbol).GetMember("M")))
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_05()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb">
public interface I1(Of T) 
    Sub M()
end interface

public interface I2(Of I2T)
    Inherits I1(Of (a As I2T, b As I2T))
end interface

public interface I3(Of I3T)
    Inherits I1(Of (c As I3T, d As I3T))
end interface

public class C1(Of T) 
    Implements I2(Of T), I1(Of (c As T, d As T)) 

    Sub M_1() Implements I1(Of (a As T, b As T)).M
    End Sub
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C1
    End Sub
End class

public class C2(Of T)
    Implements I1(Of (c As T, d As T)), I2(Of T) 

    Sub M_1() Implements I1(Of (a As T, b As T)).M
    End Sub
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C2
    End Sub
End class

public class C3(Of T)
    Implements I1(Of (a As T, b As T)), I1(Of (c As T, d As T))

    Sub M_1() Implements I1(Of (a As T, b As T)).M
    End Sub
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C3
    End Sub
End class

public class C4(Of T)
    Implements I2(Of T), I3(Of T) 

    Sub M_1() Implements I1(Of (a As T, b As T)).M
    End Sub
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C4
    End Sub
End class
    </file>
</compilation>
            )

            comp.AssertTheseDiagnostics(
<errors>
BC37273: Interface 'I1(Of (c As T, d As T))' can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (a As T, b As T))' (via 'I2(Of T)').
    Implements I2(Of T), I1(Of (c As T, d As T)) 
                         ~~~~~~~~~~~~~~~~~~~~~~~
BC30583: 'I1(Of (c As T, d As T)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C1
                         ~~~~~~~~~~~~~~~~~~~~~~~~~
BC37274: Interface 'I1(Of (a As T, b As T))' (via 'I2(Of T)') can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (c As T, d As T))'.
    Implements I1(Of (c As T, d As T)), I2(Of T) 
                                        ~~~~~~~~
BC30583: 'I1(Of (c As T, d As T)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C2
                         ~~~~~~~~~~~~~~~~~~~~~~~~~
BC37272: Interface 'I1(Of (c As T, d As T))' can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (a As T, b As T))'.
    Implements I1(Of (a As T, b As T)), I1(Of (c As T, d As T))
                                        ~~~~~~~~~~~~~~~~~~~~~~~
BC30583: 'I1(Of (c As T, d As T)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C3
                         ~~~~~~~~~~~~~~~~~~~~~~~~~
BC37275: Interface 'I1(Of (c As T, d As T))' (via 'I3(Of T)') can be implemented only once by this type, but already appears with different tuple element names, as 'I1(Of (a As T, b As T))' (via 'I2(Of T)').
    Implements I2(Of T), I3(Of T) 
                         ~~~~~~~~
BC30583: 'I1(Of (c As T, d As T)).M' cannot be implemented more than once.
    Sub M_2() Implements I1(Of (c As T, d As T)).M ' C4
                         ~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

            Dim c1 As INamedTypeSymbol = comp.GetTypeByMetadataName("C1`1")
            Dim c1Interfaces = c1.Interfaces
            Dim c1AllInterfaces = c1.AllInterfaces
            Assert.Equal(2, c1Interfaces.Length)
            Assert.Equal(3, c1AllInterfaces.Length)
            Assert.Equal("I2(Of T)", c1Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As T, d As T))", c1Interfaces(1).ToTestDisplayString())
            Assert.Equal("I2(Of T)", c1AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As T, b As T))", c1AllInterfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (c As T, d As T))", c1AllInterfaces(2).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_05_AssertExplicitInterfaceImplementations(c1)

            Dim c2 As INamedTypeSymbol = comp.GetTypeByMetadataName("C2`1")
            Dim c2Interfaces = c2.Interfaces
            Dim c2AllInterfaces = c2.AllInterfaces
            Assert.Equal(2, c2Interfaces.Length)
            Assert.Equal(3, c2AllInterfaces.Length)
            Assert.Equal("I1(Of (c As T, d As T))", c2Interfaces(0).ToTestDisplayString())
            Assert.Equal("I2(Of T)", c2Interfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (c As T, d As T))", c2AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I2(Of T)", c2AllInterfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (a As T, b As T))", c2AllInterfaces(2).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_05_AssertExplicitInterfaceImplementations(c2)

            Dim c3 As INamedTypeSymbol = comp.GetTypeByMetadataName("C3`1")
            Dim c3Interfaces = c3.Interfaces
            Dim c3AllInterfaces = c3.AllInterfaces
            Assert.Equal(2, c3Interfaces.Length)
            Assert.Equal(2, c3AllInterfaces.Length)
            Assert.Equal("I1(Of (a As T, b As T))", c3Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As T, d As T))", c3Interfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (a As T, b As T))", c3AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As T, d As T))", c3AllInterfaces(1).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_05_AssertExplicitInterfaceImplementations(c3)

            Dim c4 As INamedTypeSymbol = comp.GetTypeByMetadataName("C4`1")
            Dim c4Interfaces = c4.Interfaces
            Dim c4AllInterfaces = c4.AllInterfaces
            Assert.Equal(2, c4Interfaces.Length)
            Assert.Equal(4, c4AllInterfaces.Length)
            Assert.Equal("I2(Of T)", c4Interfaces(0).ToTestDisplayString())
            Assert.Equal("I3(Of T)", c4Interfaces(1).ToTestDisplayString())
            Assert.Equal("I2(Of T)", c4AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As T, b As T))", c4AllInterfaces(1).ToTestDisplayString())
            Assert.Equal("I3(Of T)", c4AllInterfaces(2).ToTestDisplayString())
            Assert.Equal("I1(Of (c As T, d As T))", c4AllInterfaces(3).ToTestDisplayString())
            DuplicateInterfaceDetectionWithDifferentTupleNames_05_AssertExplicitInterfaceImplementations(c4)
        End Sub

        Private Shared Sub DuplicateInterfaceDetectionWithDifferentTupleNames_05_AssertExplicitInterfaceImplementations(c As INamedTypeSymbol)
            Dim cMabImplementations = DirectCast(DirectCast(c, TypeSymbol).GetMember("M_1"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, cMabImplementations.Length)
            Assert.Equal("Sub I1(Of (a As T, b As T)).M()", cMabImplementations(0).ToTestDisplayString())
            Dim cMcdImplementations = DirectCast(DirectCast(c, TypeSymbol).GetMember("M_2"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, cMcdImplementations.Length)
            Assert.Equal("Sub I1(Of (c As T, d As T)).M()", cMcdImplementations(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_06()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb">
public interface I1(Of T) 
    Sub M()
end interface

public class C3(Of T, U)
    Implements I1(Of (a As T, b As T)), I1(Of (c As U, d As U))

    Sub M_1() Implements I1(Of (a As T, b As T)).M
    End Sub
    Sub M_2() Implements I1(Of (c As U, d As U)).M
    End Sub
End class
    </file>
</compilation>
            )

            comp.AssertTheseDiagnostics(
<errors>
BC32072: Cannot implement interface 'I1(Of (c As U, d As U))' because its implementation could conflict with the implementation of another implemented interface 'I1(Of (a As T, b As T))' for some type arguments.
    Implements I1(Of (a As T, b As T)), I1(Of (c As U, d As U))
                                        ~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

            Dim c3 As INamedTypeSymbol = comp.GetTypeByMetadataName("C3`2")
            Dim c3Interfaces = c3.Interfaces
            Dim c3AllInterfaces = c3.AllInterfaces
            Assert.Equal(2, c3Interfaces.Length)
            Assert.Equal(2, c3AllInterfaces.Length)
            Assert.Equal("I1(Of (a As T, b As T))", c3Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As U, d As U))", c3Interfaces(1).ToTestDisplayString())
            Assert.Equal("I1(Of (a As T, b As T))", c3AllInterfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (c As U, d As U))", c3AllInterfaces(1).ToTestDisplayString())

            Dim cMabImplementations = DirectCast(DirectCast(c3, TypeSymbol).GetMember("M_1"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, cMabImplementations.Length)
            Assert.Equal("Sub I1(Of (a As T, b As T)).M()", cMabImplementations(0).ToTestDisplayString())
            Dim cMcdImplementations = DirectCast(DirectCast(c3, TypeSymbol).GetMember("M_2"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, cMcdImplementations.Length)
            Assert.Equal("Sub I1(Of (c As U, d As U)).M()", cMcdImplementations(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames_07()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb">
public interface I1(Of T) 
    Sub M()
end interface

public class C3
    Implements I1(Of (a As Integer, b As Integer))

    Sub M() Implements I1(Of (c As Integer, d As Integer)).M
    End Sub
End class

public class C4
    Implements I1(Of (c As Integer, d As Integer))

    Sub M() Implements I1(Of (c As Integer, d As Integer)).M
    End Sub
End class
    </file>
</compilation>
            )

            comp.AssertTheseDiagnostics(
<errors>
BC31035: Interface 'I1(Of (c As Integer, d As Integer))' is not implemented by this class.
    Sub M() Implements I1(Of (c As Integer, d As Integer)).M
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

            Dim c3 As INamedTypeSymbol = comp.GetTypeByMetadataName("C3")
            Dim c3Interfaces = c3.Interfaces
            Dim c3AllInterfaces = c3.AllInterfaces
            Assert.Equal(1, c3Interfaces.Length)
            Assert.Equal(1, c3AllInterfaces.Length)
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c3Interfaces(0).ToTestDisplayString())
            Assert.Equal("I1(Of (a As System.Int32, b As System.Int32))", c3AllInterfaces(0).ToTestDisplayString())

            Dim mImplementations = DirectCast(DirectCast(c3, TypeSymbol).GetMember("M"), IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, mImplementations.Length)
            Assert.Equal("Sub I1(Of (c As System.Int32, d As System.Int32)).M()", mImplementations(0).ToTestDisplayString())

            Assert.Equal("Sub C3.M()",
                         c3.FindImplementationForInterfaceMember(DirectCast(c3Interfaces(0), TypeSymbol).GetMember("M")).ToTestDisplayString())
            Assert.Equal("Sub C3.M()",
                         c3.FindImplementationForInterfaceMember(comp.GetTypeByMetadataName("C4").InterfacesNoUseSiteDiagnostics()(0).GetMember("M")).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub AccessCheckLooksInsideTuples()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Public Function M() As (C2.C3, Integer)
        Throw New System.Exception()
    End Function
End Class
Public Class C2
    Private Class C3
    End Class
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30389: 'C2.C3' is not accessible in this context because it is 'Private'.
    Public Function M() As (C2.C3, Integer)
                            ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AccessCheckLooksInsideTuples2()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Public Function M() As (C2, Integer)
        Throw New System.Exception()
    End Function
    Private Class C2
    End Class
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            Dim expectedErrors = <errors><![CDATA[
BC30508: 'M' cannot expose type 'C.C2' in namespace '<Default>' through class 'C'.
    Public Function M() As (C2, Integer)
                           ~~~~~~~~~~~~~
                 ]]></errors>
            comp.AssertTheseDiagnostics(expectedErrors)
        End Sub

        <Fact>
        Public Sub DuplicateInterfaceDetectionWithDifferentTupleNames2()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0(Of T)
End Interface

Public Class C2
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (a As Integer, b As Integer))
End Class
Public Class C3
    Implements I0(Of Integer), I0(Of Integer)
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC31033: Interface 'I0(Of (a As Integer, b As Integer))' can be implemented only once by this type.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (a As Integer, b As Integer))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31033: Interface 'I0(Of Integer)' can be implemented only once by this type.
    Implements I0(Of Integer), I0(Of Integer)
                               ~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub ImplicitAndExplicitInterfaceImplementationWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Interface I0(Of T)
    Function Pop() As T
    Sub Push(x As T)
End Interface

Public Class C1
    Implements I0(Of (a As Integer, b As Integer))

    Public Function Pop() As (a As Integer, b As Integer) Implements I0(Of (a As Integer, b As Integer)).Pop
        Throw New Exception()
    End Function
    Public Sub Push(x As (a As Integer, b As Integer)) Implements I0(Of (a As Integer, b As Integer)).Push
    End Sub
End Class


Public Class C2
    Inherits C1
    Implements I0(Of (a As Integer, b As Integer))

    Public Overloads Function Pop() As (notA As Integer, notB As Integer) Implements I0(Of (a As Integer, b As Integer)).Pop
        Throw New Exception()
    End Function
    Public Overloads Sub Push(x As (notA As Integer, notB As Integer)) Implements I0(Of (a As Integer, b As Integer)).Push
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30402: 'Pop' cannot implement function 'Pop' on interface 'I0(Of (a As Integer, b As Integer))' because the tuple element names in 'Public Overloads Function Pop() As (notA As Integer, notB As Integer)' do not match those in 'Function Pop() As (a As Integer, b As Integer)'.
    Public Overloads Function Pop() As (notA As Integer, notB As Integer) Implements I0(Of (a As Integer, b As Integer)).Pop
                                                                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30402: 'Push' cannot implement sub 'Push' on interface 'I0(Of (a As Integer, b As Integer))' because the tuple element names in 'Public Overloads Sub Push(x As (notA As Integer, notB As Integer))' do not match those in 'Sub Push(x As (a As Integer, b As Integer))'.
    Public Overloads Sub Push(x As (notA As Integer, notB As Integer)) Implements I0(Of (a As Integer, b As Integer)).Push
                                                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub PartialMethodsWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Partial Class C1
    Private Partial Sub M1(x As (a As Integer, b As Integer))
    End Sub
    Private Partial Sub M2(x As (a As Integer, b As Integer))
    End Sub
    Private Partial Sub M3(x As (a As Integer, b As Integer))
    End Sub
End Class

Public Partial Class C1
    Private Sub M1(x As (notA As Integer, notB As Integer))
    End Sub
    Private Sub M2(x As (Integer, Integer))
    End Sub
    Private Sub M3(x As (a As Integer, b As Integer))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37271: 'Private Sub M1(x As (a As Integer, b As Integer))' has multiple definitions with identical signatures with different tuple element names, including 'Private Sub M1(x As (notA As Integer, notB As Integer))'.
    Private Partial Sub M1(x As (a As Integer, b As Integer))
                        ~~
BC37271: 'Private Sub M2(x As (a As Integer, b As Integer))' has multiple definitions with identical signatures with different tuple element names, including 'Private Sub M2(x As (Integer, Integer))'.
    Private Partial Sub M2(x As (a As Integer, b As Integer))
                        ~~
</errors>)

        End Sub

        <Fact>
        Public Sub PartialClassWithDifferentTupleNamesInBaseInterfaces()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0(Of T)
End Interface

Public Partial Class C
    Implements I0(Of (a As Integer, b As Integer))
End Class
Public Partial Class C
    Implements I0(Of (notA As Integer, notB As Integer))
End Class
Public Partial Class C
    Implements I0(Of (Integer, Integer))
End Class

Public Partial Class D
    Implements I0(Of (a As Integer, b As Integer))
End Class
Public Partial Class D
    Implements I0(Of (a As Integer, b As Integer))
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37272: Interface 'I0(Of (notA As Integer, notB As Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Implements I0(Of (notA As Integer, notB As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37272: Interface 'I0(Of (Integer, Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Implements I0(Of (Integer, Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub PartialClassWithDifferentTupleNamesInBaseTypes()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base(Of T)
End Class
Public Partial Class C1
    Inherits Base(Of (a As Integer, b As Integer))
End Class
Public Partial Class C1
    Inherits Base(Of (notA As Integer, notB As Integer))
End Class

Public Partial Class C2
    Inherits Base(Of (a As Integer, b As Integer))
End Class
Public Partial Class C2
    Inherits Base(Of (Integer, Integer))
End Class

Public Partial Class C3
    Inherits Base(Of (a As Integer, b As Integer))
End Class
Public Partial Class C3
    Inherits Base(Of (a As Integer, b As Integer))
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30928: Base class 'Base(Of (notA As Integer, notB As Integer))' specified for class 'C1' cannot be different from the base class 'Base(Of (a As Integer, b As Integer))' of one of its other partial types.
    Inherits Base(Of (notA As Integer, notB As Integer))
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30928: Base class 'Base(Of (Integer, Integer))' specified for class 'C2' cannot be different from the base class 'Base(Of (a As Integer, b As Integer))' of one of its other partial types.
    Inherits Base(Of (Integer, Integer))
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub IndirectInterfaceBasesWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0(Of T)
End Interface
Public Interface I1
    Inherits I0(Of (a As Integer, b As Integer))
End Interface
Public Interface I2
    Inherits I0(Of (notA As Integer, notB As Integer))
End Interface
Public Interface I3
    Inherits I0(Of (a As Integer, b As Integer))
End Interface

Public Class C1
    Implements I1, I3
End Class
Public Class C2
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (notA As Integer, notB As Integer))
End Class
Public Class C3
    Implements I2, I0(Of (a As Integer, b As Integer))
End Class
Public Class C4
    Implements I0(Of (a As Integer, b As Integer)), I2
End Class
Public Class C5
    Implements I1, I2
End Class

Public Interface I11
    Inherits I0(Of (a As Integer, b As Integer)), I0(Of (notA As Integer, notB As Integer))
End Interface
Public Interface I12
    Inherits I2, I0(Of (a As Integer, b As Integer))
End Interface
Public Interface I13
    Inherits I0(Of (a As Integer, b As Integer)), I2
End Interface
Public Interface I14
    Inherits I1, I2
End Interface
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC37272: Interface 'I0(Of (notA As Integer, notB As Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (notA As Integer, notB As Integer))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37273: Interface 'I0(Of (a As Integer, b As Integer))' can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (notA As Integer, notB As Integer))' (via 'I2').
    Implements I2, I0(Of (a As Integer, b As Integer))
                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37274: Interface 'I0(Of (notA As Integer, notB As Integer))' (via 'I2') can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Implements I0(Of (a As Integer, b As Integer)), I2
                                                    ~~
BC37275: Interface 'I0(Of (notA As Integer, notB As Integer))' (via 'I2') can be implemented only once by this type, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))' (via 'I1').
    Implements I1, I2
                   ~~
BC37276: Interface 'I0(Of (notA As Integer, notB As Integer))' can be inherited only once by this interface, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Inherits I0(Of (a As Integer, b As Integer)), I0(Of (notA As Integer, notB As Integer))
                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37277: Interface 'I0(Of (a As Integer, b As Integer))' can be inherited only once by this interface, but already appears with different tuple element names, as 'I0(Of (notA As Integer, notB As Integer))' (via 'I2').
    Inherits I2, I0(Of (a As Integer, b As Integer))
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37278: Interface 'I0(Of (notA As Integer, notB As Integer))' (via 'I2') can be inherited only once by this interface, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))'.
    Inherits I0(Of (a As Integer, b As Integer)), I2
                                                  ~~
BC37279: Interface 'I0(Of (notA As Integer, notB As Integer))' (via 'I2') can be inherited only once by this interface, but already appears with different tuple element names, as 'I0(Of (a As Integer, b As Integer))' (via 'I1').
    Inherits I1, I2
                 ~~
</errors>)

        End Sub

        <Fact>
        Public Sub InterfaceUnification()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0(Of T1)
End Interface

Public Class C1(Of T2)
    Implements I0(Of Integer), I0(Of T2)
End Class
Public Class C2(Of T2)
    Implements I0(Of (Integer, Integer)), I0(Of System.ValueTuple(Of T2, T2))
End Class
Public Class C3(Of T2)
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (T2, T2))
End Class
Public Class C4(Of T2)
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (a As T2, b As T2))
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC32072: Cannot implement interface 'I0(Of T2)' because its implementation could conflict with the implementation of another implemented interface 'I0(Of Integer)' for some type arguments.
    Implements I0(Of Integer), I0(Of T2)
                               ~~~~~~~~~
BC32072: Cannot implement interface 'I0(Of (T2, T2))' because its implementation could conflict with the implementation of another implemented interface 'I0(Of (Integer, Integer))' for some type arguments.
    Implements I0(Of (Integer, Integer)), I0(Of System.ValueTuple(Of T2, T2))
                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32072: Cannot implement interface 'I0(Of (T2, T2))' because its implementation could conflict with the implementation of another implemented interface 'I0(Of (a As Integer, b As Integer))' for some type arguments.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (T2, T2))
                                                    ~~~~~~~~~~~~~~~
BC32072: Cannot implement interface 'I0(Of (a As T2, b As T2))' because its implementation could conflict with the implementation of another implemented interface 'I0(Of (a As Integer, b As Integer))' for some type arguments.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (a As T2, b As T2))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub InterfaceUnification2()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Public Interface I0(Of T1)
End Interface
Public Class Derived(Of T)
    Implements I0(Of Derived(Of (T, T))), I0(Of T)
End Class
     ]]></file>
</compilation>,
references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
</errors>)
            ' Didn't run out of memory in trying to substitute T with Derived(Of (T, T)) in a loop
        End Sub

        <Fact>
        Public Sub AmbiguousExtensionMethodWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Module M1
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(self As String, x As (Integer, Integer))
        Throw New Exception()
    End Sub
End Module
Public Module M2
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(self As String, x As (a As Integer, b As Integer))
        Throw New Exception()
    End Sub
End Module
Public Module M3
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(self As String, x As (c As Integer, d As Integer))
        Throw New Exception()
    End Sub
End Module
Public Class C
    Public Sub M(s As String)
        s.M((1, 1))
        s.M((a:=1, b:=1))
        s.M((c:=1, d:=1))
    End Sub
End Class
     ]]></file>
</compilation>,
references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    Extension method 'Public Sub M(x As (Integer, Integer))' defined in 'M1': Not most specific.
    Extension method 'Public Sub M(x As (a As Integer, b As Integer))' defined in 'M2': Not most specific.
    Extension method 'Public Sub M(x As (c As Integer, d As Integer))' defined in 'M3': Not most specific.
        s.M((1, 1))
          ~
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    Extension method 'Public Sub M(x As (Integer, Integer))' defined in 'M1': Not most specific.
    Extension method 'Public Sub M(x As (a As Integer, b As Integer))' defined in 'M2': Not most specific.
    Extension method 'Public Sub M(x As (c As Integer, d As Integer))' defined in 'M3': Not most specific.
        s.M((a:=1, b:=1))
          ~
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    Extension method 'Public Sub M(x As (Integer, Integer))' defined in 'M1': Not most specific.
    Extension method 'Public Sub M(x As (a As Integer, b As Integer))' defined in 'M2': Not most specific.
    Extension method 'Public Sub M(x As (c As Integer, d As Integer))' defined in 'M3': Not most specific.
        s.M((c:=1, d:=1))
          ~
</errors>)

        End Sub

        <Fact>
        Public Sub InheritFromMetadataWithDifferentNames()

            Dim il =
"
.assembly extern mscorlib { }
.assembly extern System.ValueTuple
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )
  .ver 4:0:1:0
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual
          instance class [System.ValueTuple]System.ValueTuple`2<int32,int32>
          M() cil managed
  {
    .param [0]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        = {string[2]('a' 'b')}
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (class [System.ValueTuple]System.ValueTuple`2<int32,int32> V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.2
    IL_0003:  newobj     instance void class [System.ValueTuple]System.ValueTuple`2<int32,int32>::.ctor(!0,
                                                                                                        !1)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Base::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base::.ctor

} // end of class Base

.class public auto ansi beforefieldinit Base2
       extends Base
{
  .method public hidebysig virtual instance class [System.ValueTuple]System.ValueTuple`2<int32,int32>
          M() cil managed
  {
    .param [0]
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        = {string[2]('notA' 'notB')}
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (class [System.ValueTuple]System.ValueTuple`2<int32,int32> V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.2
    IL_0003:  newobj     instance void class [System.ValueTuple]System.ValueTuple`2<int32,int32>::.ctor(!0,
                                                                                                        !1)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Base2::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base2::.ctor

} // end of class Base2
"

            Dim compMatching = CreateCompilationWithCustomILSource(
<compilation>
    <file name="a.vb">
Public Class C
    Inherits Base2

    Public Overrides Function M() As (notA As Integer, notB As Integer)
        Return (1, 2)
    End Function
End Class
    </file>
</compilation>,
il,
additionalReferences:=s_valueTupleRefs)

            compMatching.AssertTheseDiagnostics()

            Dim compDifferent1 = CreateCompilationWithCustomILSource(
<compilation>
    <file name="a.vb">
Public Class C
    Inherits Base2

    Public Overrides Function M() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
End Class
    </file>
</compilation>,
il,
additionalReferences:=s_valueTupleRefs)

            compDifferent1.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Function M() As (a As Integer, b As Integer)' cannot override 'Public Overrides Function M() As (notA As Integer, notB As Integer)' because they differ by their tuple element names.
    Public Overrides Function M() As (a As Integer, b As Integer)
                              ~
</errors>)

            Dim compDifferent2 = CreateCompilationWithCustomILSource(
<compilation>
    <file name="a.vb">
Public Class C
    Inherits Base2

    Public Overrides Function M() As (Integer, Integer)
        Return (1, 2)
    End Function
End Class
    </file>
</compilation>,
il,
additionalReferences:=s_valueTupleRefs)

            compDifferent2.AssertTheseDiagnostics(
<errors>
</errors>)

        End Sub

        <Fact>
        Public Sub TupleNamesInAnonymousTypes()

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Public Shared Sub Main()
        Dim x1 = New With {.Tuple = (a:=1, b:=2) }
        Dim x2 = New With {.Tuple = (c:=1, 2) }
        x2 = x1
        Console.Write(x1.Tuple.a)
    End Sub
End Class
    </file>
</compilation>,
references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim model = comp.GetSemanticModel(comp.SyntaxTrees(0))
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim node = nodes.OfType(Of TupleExpressionSyntax)().First()

            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Assert.Equal("x1 As <anonymous type: Tuple As (a As System.Int32, b As System.Int32)>", model.GetDeclaredSymbol(x1).ToTestDisplayString())

            Dim x2 = nodes.OfType(Of VariableDeclaratorSyntax)().Skip(1).First().Names(0)
            Assert.Equal("x2 As <anonymous type: Tuple As (c As System.Int32, System.Int32)>", model.GetDeclaredSymbol(x2).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub OverriddenPropertyWithDifferentTupleNamesInReturn()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Property P1 As (a As Integer, b As Integer)
    Public Overridable Property P2 As (a As Integer, b As Integer)
    Public Overridable Property P3 As (a As Integer, b As Integer)()
    Public Overridable Property P4 As (a As Integer, b As Integer)?
    Public Overridable Property P5 As (c As (a As Integer, b As Integer), d As Integer)
End Class

Public Class Derived
    Inherits Base

    Public Overrides Property P1 As (a As Integer, b As Integer)
    Public Overrides Property P2 As (notA As Integer, notB As Integer)
    Public Overrides Property P3 As (notA As Integer, notB As Integer)()
    Public Overrides Property P4 As (notA As Integer, notB As Integer)?
    Public Overrides Property P5 As (c As (notA As Integer, notB As Integer), d As Integer)
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40001: 'Public Overrides Property P2 As (notA As Integer, notB As Integer)' cannot override 'Public Overridable Property P2 As (a As Integer, b As Integer)' because they differ by their tuple element names.
    Public Overrides Property P2 As (notA As Integer, notB As Integer)
                              ~~
BC40001: 'Public Overrides Property P3 As (notA As Integer, notB As Integer)()' cannot override 'Public Overridable Property P3 As (a As Integer, b As Integer)()' because they differ by their tuple element names.
    Public Overrides Property P3 As (notA As Integer, notB As Integer)()
                              ~~
BC40001: 'Public Overrides Property P4 As (notA As Integer, notB As Integer)?' cannot override 'Public Overridable Property P4 As (a As Integer, b As Integer)?' because they differ by their tuple element names.
    Public Overrides Property P4 As (notA As Integer, notB As Integer)?
                              ~~
BC40001: 'Public Overrides Property P5 As (c As (notA As Integer, notB As Integer), d As Integer)' cannot override 'Public Overridable Property P5 As (c As (a As Integer, b As Integer), d As Integer)' because they differ by their tuple element names.
    Public Overrides Property P5 As (c As (notA As Integer, notB As Integer), d As Integer)
                              ~~
</errors>)

        End Sub

        <Fact>
        Public Sub OverriddenPropertyWithNoTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Property P6 As (a As Integer, b As Integer)
End Class

Public Class Derived
    Inherits Base

    Public Overrides Property P6 As (Integer, Integer)
    Sub M()
        Dim result = Me.P6
        Dim result2 = MyBase.P6
        System.Console.Write(result.a)
        System.Console.Write(result2.a)
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
</errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim propertyAccess = nodes.OfType(Of MemberAccessExpressionSyntax)().ElementAt(0)
            Assert.Equal("Me.P6", propertyAccess.ToString())
            Assert.Equal("Property Derived.P6 As (System.Int32, System.Int32)", model.GetSymbolInfo(propertyAccess).Symbol.ToTestDisplayString())

            Dim propertyAccess2 = nodes.OfType(Of MemberAccessExpressionSyntax)().ElementAt(1)
            Assert.Equal("MyBase.P6", propertyAccess2.ToString())
            Assert.Equal("Property Base.P6 As (a As System.Int32, b As System.Int32)", model.GetSymbolInfo(propertyAccess2).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub OverriddenPropertyWithNoTupleNamesWithValueTuple()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Base
    Public Overridable Property P6 As (a As Integer, b As Integer)
End Class

Public Class Derived
    Inherits Base

    Public Overrides Property P6 As System.ValueTuple(Of Integer, Integer)
    Sub M()
        Dim result = Me.P6
        Dim result2 = MyBase.P6
        System.Console.Write(result.a)
        System.Console.Write(result2.a)
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
</errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()

            Dim propertyAccess = nodes.OfType(Of MemberAccessExpressionSyntax)().ElementAt(0)
            Assert.Equal("Me.P6", propertyAccess.ToString())
            Assert.Equal("Property Derived.P6 As (System.Int32, System.Int32)", model.GetSymbolInfo(propertyAccess).Symbol.ToTestDisplayString())

            Dim propertyAccess2 = nodes.OfType(Of MemberAccessExpressionSyntax)().ElementAt(1)
            Assert.Equal("MyBase.P6", propertyAccess2.ToString())
            Assert.Equal("Property Base.P6 As (a As System.Int32, b As System.Int32)", model.GetSymbolInfo(propertyAccess2).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub OverriddenEventWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class Base
    Public Overridable Event E1 As Action(Of (a As Integer, b As Integer))
End Class

Public Class Derived
    Inherits Base

    Public Overrides Event E1 As Action(Of (Integer, Integer))
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30243: 'Overridable' is not valid on an event declaration.
    Public Overridable Event E1 As Action(Of (a As Integer, b As Integer))
           ~~~~~~~~~~~
BC30243: 'Overrides' is not valid on an event declaration.
    Public Overrides Event E1 As Action(Of (Integer, Integer))
           ~~~~~~~~~
BC40004: event 'E1' conflicts with event 'E1' in the base class 'Base' and should be declared 'Shadows'.
    Public Overrides Event E1 As Action(Of (Integer, Integer))
                           ~~
</errors>)

        End Sub

        <Fact>
        Public Sub StructInStruct()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Public Structure S
    Public Field As (S, S)
End Structure
     ]]></file>
</compilation>,
references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30294: Structure 'S' cannot contain an instance of itself: 
    'S' contains '(S, S)' (variable 'Field').
    '(S, S)' contains 'S' (variable 'Item1').
    Public Field As (S, S)
           ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AssignNullWithMissingValueTuple()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class S
    Dim t As (Integer, Integer) = Nothing
End Class
    </file>
</compilation>)

            comp.AssertTheseDiagnostics(
<errors>
BC37267: Predefined type 'ValueTuple(Of ,)' is not defined or imported.
    Dim t As (Integer, Integer) = Nothing
             ~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub MultipleImplementsWithDifferentTupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Interface I0
    Sub M(x As (a0 As Integer, b0 As Integer))
    Function MR() As (a0 As Integer, b0 As Integer)
End Interface

Public Interface I1
    Sub M(x As (a1 As Integer, b1 As Integer))
    Function MR() As (a1 As Integer, b1 As Integer)
End Interface

Public Class C1
    Implements I0, I1

    Public Sub M(x As (a2 As Integer, b2 As Integer)) Implements I0.M, I1.M
    End Sub

    Public Function MR() As (a2 As Integer, b2 As Integer) Implements I0.MR, I1.MR
        Return (1, 2)
    End Function
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC30402: 'M' cannot implement sub 'M' on interface 'I0' because the tuple element names in 'Public Sub M(x As (a2 As Integer, b2 As Integer))' do not match those in 'Sub M(x As (a0 As Integer, b0 As Integer))'.
    Public Sub M(x As (a2 As Integer, b2 As Integer)) Implements I0.M, I1.M
                                                                 ~~~~
BC30402: 'M' cannot implement sub 'M' on interface 'I1' because the tuple element names in 'Public Sub M(x As (a2 As Integer, b2 As Integer))' do not match those in 'Sub M(x As (a1 As Integer, b1 As Integer))'.
    Public Sub M(x As (a2 As Integer, b2 As Integer)) Implements I0.M, I1.M
                                                                       ~~~~
BC30402: 'MR' cannot implement function 'MR' on interface 'I0' because the tuple element names in 'Public Function MR() As (a2 As Integer, b2 As Integer)' do not match those in 'Function MR() As (a0 As Integer, b0 As Integer)'.
    Public Function MR() As (a2 As Integer, b2 As Integer) Implements I0.MR, I1.MR
                                                                      ~~~~~
BC30402: 'MR' cannot implement function 'MR' on interface 'I1' because the tuple element names in 'Public Function MR() As (a2 As Integer, b2 As Integer)' do not match those in 'Function MR() As (a1 As Integer, b1 As Integer)'.
    Public Function MR() As (a2 As Integer, b2 As Integer) Implements I0.MR, I1.MR
                                                                             ~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub MethodSignatureComparerTest()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Public Sub M1(x As (a As Integer, b As Integer))
    End Sub

    Public Sub M2(x As (a As Integer, b As Integer))
    End Sub

    Public Sub M3(x As (notA As Integer, notB As Integer))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim m1 = comp.GetMember(Of MethodSymbol)("C.M1")
            Dim m2 = comp.GetMember(Of MethodSymbol)("C.M2")
            Dim m3 = comp.GetMember(Of MethodSymbol)("C.M3")

            Dim comparison12 = MethodSignatureComparer.DetailedCompare(m1, m2, SymbolComparisonResults.TupleNamesMismatch)
            Assert.Equal(0, comparison12)

            Dim comparison13 = MethodSignatureComparer.DetailedCompare(m1, m3, SymbolComparisonResults.TupleNamesMismatch)
            Assert.Equal(SymbolComparisonResults.TupleNamesMismatch, comparison13)

        End Sub

        <Fact>
        Public Sub IsSameTypeTest()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Public Sub M1(x As (Integer, Integer))
    End Sub

    Public Sub M2(x As (a As Integer, b As Integer))
    End Sub
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim m1 = comp.GetMember(Of MethodSymbol)("C.M1")

            Dim tuple1 As TypeSymbol = m1.Parameters(0).Type
            Dim underlying1 As NamedTypeSymbol = tuple1.TupleUnderlyingType
            Assert.True(tuple1.IsSameType(tuple1, TypeCompareKind.ConsiderEverything))
            Assert.False(tuple1.IsSameType(underlying1, TypeCompareKind.ConsiderEverything))
            Assert.False(underlying1.IsSameType(tuple1, TypeCompareKind.ConsiderEverything))
            Assert.True(underlying1.IsSameType(underlying1, TypeCompareKind.ConsiderEverything))

            Assert.True(tuple1.IsSameType(tuple1, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.False(tuple1.IsSameType(underlying1, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.False(underlying1.IsSameType(tuple1, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.True(underlying1.IsSameType(underlying1, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))

            Assert.True(tuple1.IsSameType(tuple1, TypeCompareKind.IgnoreTupleNames))
            Assert.True(tuple1.IsSameType(underlying1, TypeCompareKind.IgnoreTupleNames))
            Assert.True(underlying1.IsSameType(tuple1, TypeCompareKind.IgnoreTupleNames))
            Assert.True(underlying1.IsSameType(underlying1, TypeCompareKind.IgnoreTupleNames))

            Assert.False(tuple1.IsSameType(Nothing, TypeCompareKind.ConsiderEverything))
            Assert.False(tuple1.IsSameType(Nothing, TypeCompareKind.IgnoreTupleNames))

            Dim m2 = comp.GetMember(Of MethodSymbol)("C.M2")
            Dim tuple2 As TypeSymbol = m2.Parameters(0).Type
            Dim underlying2 As NamedTypeSymbol = tuple2.TupleUnderlyingType
            Assert.True(tuple2.IsSameType(tuple2, TypeCompareKind.ConsiderEverything))
            Assert.False(tuple2.IsSameType(underlying2, TypeCompareKind.ConsiderEverything))
            Assert.False(underlying2.IsSameType(tuple2, TypeCompareKind.ConsiderEverything))
            Assert.True(underlying2.IsSameType(underlying2, TypeCompareKind.ConsiderEverything))

            Assert.True(tuple2.IsSameType(tuple2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.False(tuple2.IsSameType(underlying2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.False(underlying2.IsSameType(tuple2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.True(underlying2.IsSameType(underlying2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))

            Assert.True(tuple2.IsSameType(tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.True(tuple2.IsSameType(underlying2, TypeCompareKind.IgnoreTupleNames))
            Assert.True(underlying2.IsSameType(tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.True(underlying2.IsSameType(underlying2, TypeCompareKind.IgnoreTupleNames))

            Assert.False(tuple1.IsSameType(tuple2, TypeCompareKind.ConsiderEverything))
            Assert.False(tuple2.IsSameType(tuple1, TypeCompareKind.ConsiderEverything))

            Assert.False(tuple1.IsSameType(tuple2, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))
            Assert.False(tuple2.IsSameType(tuple1, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds))

            Assert.True(tuple1.IsSameType(tuple2, TypeCompareKind.IgnoreTupleNames))
            Assert.True(tuple2.IsSameType(tuple1, TypeCompareKind.IgnoreTupleNames))

        End Sub

        <Fact>
        Public Sub PropertySignatureComparer_TupleNames()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Public Property P1 As (a As Integer, b As Integer)

    Public Property P2 As (a As Integer, b As Integer)

    Public Property P3 As (notA As Integer, notB As Integer)
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim p1 = comp.GetMember(Of PropertySymbol)("C.P1")
            Dim p2 = comp.GetMember(Of PropertySymbol)("C.P2")
            Dim p3 = comp.GetMember(Of PropertySymbol)("C.P3")

            Dim comparison12 = PropertySignatureComparer.DetailedCompare(p1, p2, SymbolComparisonResults.TupleNamesMismatch)
            Assert.Equal(0, comparison12)

            Dim comparison13 = PropertySignatureComparer.DetailedCompare(p1, p3, SymbolComparisonResults.TupleNamesMismatch)
            Assert.Equal(SymbolComparisonResults.TupleNamesMismatch, comparison13)

        End Sub

        <Fact>
        Public Sub PropertySignatureComparer_TypeCustomModifiers()

            Dim il = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly extern System.ValueTuple { .publickeytoken = (CC 7B 13 FF CD 2D DD 51 ) .ver 4:0:1:0 }
.assembly '<<GeneratedFileName>>' { }

.class public auto ansi beforefieldinit CL1`1<T1>
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CL1`1::.ctor

    .property instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
            Test()
    {
      .get instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) CL1`1::get_Test()
      .set instance void CL1`1::set_Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst))
    } // end of property CL1`1::Test

    .method public hidebysig newslot specialname virtual
            instance !T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)
            get_Test() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  throw
    } // end of method CL1`1::get_Test

    .method public hidebysig newslot specialname virtual
            instance void  set_Test(!T1 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
    {
      // Code size       3 (0x3)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  throw
      IL_0002:  ret
    } // end of method CL1`1::set_Test
} // end of class CL1`1
]]>.Value

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class CL2(Of T1)
    Public Property Test As T1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithCustomILSource(source1, il, appendDefaultHeader:=False, additionalReferences:={ValueTupleRef, SystemRuntimeFacadeRef})
            comp1.AssertTheseDiagnostics()

            Dim property1 = comp1.GlobalNamespace.GetMember(Of PropertySymbol)("CL1.Test")
            Assert.Equal("Property CL1(Of T1).Test As T1 modopt(System.Runtime.CompilerServices.IsConst)", property1.ToTestDisplayString())

            Dim property2 = comp1.GlobalNamespace.GetMember(Of PropertySymbol)("CL2.Test")
            Assert.Equal("Property CL2(Of T1).Test As T1", property2.ToTestDisplayString())

            Assert.False(PropertySignatureComparer.RuntimePropertySignatureComparer.Equals(property1, property2))

        End Sub

        <Fact>
        Public Sub EventSignatureComparerTest()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Public Class C
    Public Event E1 As Action(Of (a As Integer, b As Integer))
    Public Event E2 As Action(Of (a As Integer, b As Integer))
    Public Event E3 As Action(Of (notA As Integer, notB As Integer))
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim e1 = comp.GetMember(Of EventSymbol)("C.E1")
            Dim e2 = comp.GetMember(Of EventSymbol)("C.E2")
            Dim e3 = comp.GetMember(Of EventSymbol)("C.E3")

            Assert.True(EventSignatureComparer.ExplicitEventImplementationWithTupleNamesComparer.Equals(e1, e2))
            Assert.False(EventSignatureComparer.ExplicitEventImplementationWithTupleNamesComparer.Equals(e1, e3))

        End Sub

        <Fact>
        Public Sub OperatorOverloadingWithDifferentTupleNames()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Class B1
    Shared Operator >=(x1 As (a As B1, b As B1), x2 As B1) As Boolean
        Return Nothing
    End Operator

    Shared Operator <=(x1 As (notA As B1, notB As B1), x2 As B1) As Boolean
        Return Nothing
    End Operator
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, additionalRefs:=s_valueTupleRefs)
            compilation.AssertTheseDiagnostics()

        End Sub

        <Fact>
        Public Sub Shadowing()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C0
    Public Function M(x As (a As Integer, (b As Integer, c As Integer))) As (a As Integer, (b As Integer, c As Integer))
        Return (1, (2, 3))
    End Function
End Class
Public Class C1
    Inherits C0

    Public Function M(x As (a As Integer, (notB As Integer, c As Integer))) As (a As Integer, (notB As Integer, c As Integer))
        Return (1, (2, 3))
    End Function
End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC40003: function 'M' shadows an overloadable member declared in the base class 'C0'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Public Function M(x As (a As Integer, (notB As Integer, c As Integer))) As (a As Integer, (notB As Integer, c As Integer))
                    ~
</errors>)

        End Sub

        <Fact>
        Public Sub UnifyDifferentTupleName()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Interface I0(Of T1)
End Interface

Class C(Of T2)
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (notA As T2, notB As T2))

End Class
    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC32072: Cannot implement interface 'I0(Of (notA As T2, notB As T2))' because its implementation could conflict with the implementation of another implemented interface 'I0(Of (a As Integer, b As Integer))' for some type arguments.
    Implements I0(Of (a As Integer, b As Integer)), I0(Of (notA As T2, notB As T2))
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub BC31407ERR_MultipleEventImplMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Interface I1
    Event evtTest1(x As (A As Integer, B As Integer))
    Event evtTest2(x As (A As Integer, notB As Integer))
End Interface
Class C1
    Implements I1
    Event evtTest3(x As (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)
            Dim expectedErrors1 = <errors><![CDATA[
BC30402: 'evtTest3' cannot implement event 'evtTest1' on interface 'I1' because the tuple element names in 'Public Event evtTest3 As I1.evtTest1EventHandler' do not match those in 'Event evtTest1(x As (A As Integer, B As Integer))'.
    Event evtTest3(x As (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                        ~~~~~~~~~~~
BC30402: 'evtTest3' cannot implement event 'evtTest2' on interface 'I1' because the tuple element names in 'Public Event evtTest3 As I1.evtTest1EventHandler' do not match those in 'Event evtTest2(x As (A As Integer, notB As Integer))'.
    Event evtTest3(x As (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                                     ~~~~~~~~~~~
BC31407: Event 'Public Event evtTest3 As I1.evtTest1EventHandler' cannot implement event 'I1.Event evtTest2(x As (A As Integer, notB As Integer))' because its delegate type does not match the delegate type of another event implemented by 'Public Event evtTest3 As I1.evtTest1EventHandler'.
    Event evtTest3(x As (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                                     ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub ImplementingEventWithDifferentTupleNames()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Event evtTest1 As Action(Of (A As Integer, B As Integer))
    Event evtTest2 As Action(Of (A As Integer, notB As Integer))
End Interface
Class C1
    Implements I1
    Event evtTest3 As Action(Of (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)
            Dim expectedErrors1 = <errors><![CDATA[
BC30402: 'evtTest3' cannot implement event 'evtTest1' on interface 'I1' because the tuple element names in 'Public Event evtTest3 As Action(Of (A As Integer, stilNotB As Integer))' do not match those in 'Event evtTest1 As Action(Of (A As Integer, B As Integer))'.
    Event evtTest3 As Action(Of (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                                ~~~~~~~~~~~
BC30402: 'evtTest3' cannot implement event 'evtTest2' on interface 'I1' because the tuple element names in 'Public Event evtTest3 As Action(Of (A As Integer, stilNotB As Integer))' do not match those in 'Event evtTest2 As Action(Of (A As Integer, notB As Integer))'.
    Event evtTest3 As Action(Of (A As Integer, stilNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                                             ~~~~~~~~~~~
                 ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub ImplementingEventWithNoTupleNames()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Event evtTest1 As Action(Of (A As Integer, B As Integer))
    Event evtTest2 As Action(Of (A As Integer, notB As Integer))
End Interface
Class C1
    Implements I1
    Event evtTest3 As Action(Of (Integer, Integer)) Implements I1.evtTest1, I1.evtTest2
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            CompilationUtils.AssertNoDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub ImplementingPropertyWithDifferentTupleNames()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Property P(x As (a As Integer, b As Integer)) As Boolean
End Interface
Class C1
    Implements I1
    Property P(x As (notA As Integer, notB As Integer)) As Boolean Implements I1.P
        Get
            Return True
        End Get
        Set
        End Set
    End Property
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics(<errors>
BC30402: 'P' cannot implement property 'P' on interface 'I1' because the tuple element names in 'Public Property P(x As (notA As Integer, notB As Integer)) As Boolean' do not match those in 'Property P(x As (a As Integer, b As Integer)) As Boolean'.
    Property P(x As (notA As Integer, notB As Integer)) As Boolean Implements I1.P
                                                                              ~~~~
                                               </errors>)
        End Sub

        <Fact()>
        Public Sub ImplementingPropertyWithNoTupleNames()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Property P(x As (a As Integer, b As Integer)) As Boolean
End Interface
Class C1
    Implements I1
    Property P(x As (Integer, Integer)) As Boolean Implements I1.P
        Get
            Return True
        End Get
        Set
        End Set
    End Property
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()
        End Sub

        <Fact()>
        Public Sub ImplementingPropertyWithNoTupleNames2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Property P As (a As Integer, b As Integer)
End Interface
Class C1
    Implements I1
    Property P As (Integer, Integer) Implements I1.P
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()
        End Sub

        <Fact()>
        Public Sub ImplementingMethodWithNoTupleNames()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Sub M(x As (a As Integer, b As Integer))
    Function M2 As (a As Integer, b As Integer)
End Interface
Class C1
    Implements I1

    Sub M(x As (Integer, Integer)) Implements I1.M
    End Sub

    Function M2 As (Integer, Integer) Implements I1.M2
        Return (1, 2)
    End Function
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()
        End Sub

        <Fact>
        Public Sub BC31407ERR_MultipleEventImplMismatch3_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Interface I1
    Event evtTest1 As Action(Of (A As Integer, B As Integer))
    Event evtTest2 As Action(Of (A As Integer, notB As Integer))
End Interface
Class C1
    Implements I1
    Event evtTest3(x As (A As Integer, stillNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)
            compilation1.AssertTheseDiagnostics(
<errors>
BC30402: 'evtTest3' cannot implement event 'evtTest1' on interface 'I1' because the tuple element names in 'Public Event evtTest3 As Action(Of (A As Integer, B As Integer))' do not match those in 'Event evtTest1 As Action(Of (A As Integer, B As Integer))'.
    Event evtTest3(x As (A As Integer, stillNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                         ~~~~~~~~~~~
BC30402: 'evtTest3' cannot implement event 'evtTest2' on interface 'I1' because the tuple element names in 'Public Event evtTest3 As Action(Of (A As Integer, B As Integer))' do not match those in 'Event evtTest2 As Action(Of (A As Integer, notB As Integer))'.
    Event evtTest3(x As (A As Integer, stillNotB As Integer)) Implements I1.evtTest1, I1.evtTest2
                                                                                      ~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(11689, "https://github.com/dotnet/roslyn/issues/11689")>
        <Fact>
        Public Sub ValueTupleNotStruct0()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim x as (a As Integer, b As String)
        x.Item1 = 1
        x.b = "2"
 
        ' by the language rules tuple x is definitely assigned
        ' since all its elements are definitely assigned
        System.Console.WriteLine(x)
    end sub
end class

namespace System
    public class ValueTuple(Of T1, T2)
        public Item1 as T1
        public Item2 as T2

        public Sub New(item1 as T1 , item2 as T2 )
            Me.Item1 = item1
            Me.Item2 = item2
        end sub
    End class
end Namespace

    <%= s_tupleattributes %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
        Dim x as (a As Integer, b As String)
            ~
</errors>)

        End Sub

        <WorkItem(11689, "https://github.com/dotnet/roslyn/issues/11689")>
        <Fact>
        Public Sub ValueTupleNotStruct1()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation name="Tuples">
                <file name="a.vb">
class C
    Shared Sub Main()
        Dim x = (1,2,3,4,5,6,7,8,9)
 
        System.Console.WriteLine(x)
    end sub
end class

namespace System
    public class ValueTuple(Of T1, T2)
        public Item1 as T1
        public Item2 as T2

        public Sub New(item1 as T1 , item2 as T2 )
            Me.Item1 = item1
            Me.Item2 = item2
        end sub
    End class

    public class ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)
        public Item1 As T1
        public Item2 As T2
        public Item3 As T3
        public Item4 As T4
        public Item5 As T5
        public Item6 As T6
        public Item7 As T7
        public Rest As TRest

        public Sub New(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7, rest As TRest)
            Item1 = item1
            Item2 = item2
            Item3 = item3
            Item4 = item4
            Item5 = item5
            Item6 = item6
            Item7 = item7
            Rest = rest
        end Sub

    End Class
end Namespace

    <%= s_tupleattributes %>
                </file>
            </compilation>,
            options:=TestOptions.DebugExe)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
        Dim x = (1,2,3,4,5,6,7,8,9)
            ~
BC37281: Predefined type 'ValueTuple`8' must be a structure.
        Dim x = (1,2,3,4,5,6,7,8,9)
            ~
</errors>)
        End Sub

        <Fact>
        Public Sub ConversionToBase()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class Base(Of T)
End Class

Public Class Derived
    Inherits Base(Of (a As Integer, b As Integer))

    Public Shared Narrowing Operator CType(ByVal arg As Derived) As Base(Of (Integer, Integer))
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(ByVal arg As Base(Of (Integer, Integer))) As Derived
        Return Nothing
    End Operator
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            compilation1.AssertTheseDiagnostics(
<errors>
BC33026: Conversion operators cannot convert from a type to its base type.
    Public Shared Narrowing Operator CType(ByVal arg As Derived) As Base(Of (Integer, Integer))
                                     ~~~~~
BC33030: Conversion operators cannot convert from a base type.
    Public Shared Narrowing Operator CType(ByVal arg As Base(Of (Integer, Integer))) As Derived
                                     ~~~~~
</errors>)
        End Sub

        <WorkItem(11689, "https://github.com/dotnet/roslyn/issues/11689")>
        <Fact>
        Public Sub ValueTupleNotStruct2()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
class C
    Shared Sub Main()
    end sub

    Shared Sub Test2(arg as (a As Integer, b As Integer))
    End Sub
end class

namespace System
    public class ValueTuple(Of T1, T2)
        public Item1 as T1
        public Item2 as T2

        public Sub New(item1 as T1 , item2 as T2 )
            Me.Item1 = item1
            Me.Item2 = item2
        end sub
    End class
end Namespace

    <%= s_tupleattributes %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
</errors>)

        End Sub

        <WorkItem(11689, "https://github.com/dotnet/roslyn/issues/11689")>
        <Fact>
        Public Sub ValueTupleNotStruct2i()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
class C
    Shared Sub Main()
    end sub

    Shared Sub Test2(arg as (a As Integer, b As Integer))
    End Sub
end class

namespace System
    public interface ValueTuple(Of T1, T2)
    End Interface
end Namespace

    <%= s_tupleattributes %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
</errors>)

        End Sub


        <WorkItem(11689, "https://github.com/dotnet/roslyn/issues/11689")>
        <Fact>
        Public Sub ValueTupleNotStruct3()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim x as (a As Integer, b As String)() = Nothing
 
        ' by the language rules tuple x is definitely assigned
        ' since all its elements are definitely assigned
        System.Console.WriteLine(x)
    end sub
end class

namespace System
    public class ValueTuple(Of T1, T2)
        public Item1 as T1
        public Item2 as T2

        public Sub New(item1 as T1 , item2 as T2 )
            Me.Item1 = item1
            Me.Item2 = item2
        end sub
    End class
end Namespace

    <%= s_tupleattributes %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
        Dim x as (a As Integer, b As String)() = Nothing
            ~
</errors>)

        End Sub

        <WorkItem(11689, "https://github.com/dotnet/roslyn/issues/11689")>
        <Fact>
        Public Sub ValueTupleNotStruct4()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Tuples">
    <file name="a.vb">
class C
    Shared Sub Main()

    end sub
    
    Shared Function Test2()as (a As Integer, b As Integer)
    End Function
end class

namespace System
    public class ValueTuple(Of T1, T2)
        public Item1 as T1
        public Item2 as T2

        public Sub New(item1 as T1 , item2 as T2 )
            Me.Item1 = item1
            Me.Item2 = item2
        end sub
    End class
end Namespace

    <%= s_tupleattributes %>
    </file>
</compilation>,
options:=TestOptions.DebugExe)

            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
    Shared Function Test2()as (a As Integer, b As Integer)
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub ValueTupleBaseError_NoSystemRuntime()
            Dim comp = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
    Function F() As ((Integer, Integer), (Integer, Integer))
End Interface
    </file>
</compilation>,
                references:={ValueTupleRef})
            comp.AssertTheseEmitDiagnostics(
<errors>
BC30652: Reference required to assembly 'System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the type 'ValueType'. Add one to your project.
    Function F() As ((Integer, Integer), (Integer, Integer))
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the type 'ValueType'. Add one to your project.
    Function F() As ((Integer, Integer), (Integer, Integer))
                     ~~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the type 'ValueType'. Add one to your project.
    Function F() As ((Integer, Integer), (Integer, Integer))
                                         ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(16879, "https://github.com/dotnet/roslyn/issues/16879")>
        <Fact>
        Public Sub ValueTupleBaseError_MissingReference()
            Dim comp0 = CreateCompilationWithMscorlib40(
<compilation name="5a03232e-1a0f-4d1b-99ba-5d7b40ea931e">
    <file name="a.vb">
Public Class A
End Class
Public Class B
End Class
    </file>
</compilation>)
            comp0.AssertNoDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()
            Dim comp1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Public Class C(Of T)
End Class
Namespace System
    Public Class ValueTuple(Of T1, T2)
        Inherits A
        Public Sub New(_1 As T1, _2 As T2)
        End Sub
    End Class
    Public Class ValueTuple(Of T1, T2, T3)
        Inherits C(Of B)
        Public Sub New(_1 As T1, _2 As T2, _3 As T3)
        End Sub
    End Class
End Namespace
    </file>
</compilation>,
                references:={ref0})
            Dim ref1 = comp1.EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
    Function F() As (Integer, (Integer, Integer), (Integer, Integer))
End Interface
    </file>
</compilation>,
                references:={ref1})
            comp.AssertTheseEmitDiagnostics(
<errors>
BC30652: Reference required to assembly '5a03232e-1a0f-4d1b-99ba-5d7b40ea931e, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
BC30652: Reference required to assembly '5a03232e-1a0f-4d1b-99ba-5d7b40ea931e, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B'. Add one to your project.
</errors>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub ValueTupleBase_AssemblyUnification()
            Dim signedDllOptions = TestOptions.SigningReleaseDll.
                WithCryptoKeyFile(SigningTestHelpers.KeyPairFile)
            Dim comp0v1 = CreateCompilationWithMscorlib40(
<compilation name="A">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.0.0.0")>
Public Class A
End Class
    ]]></file>
</compilation>,
                options:=signedDllOptions)
            comp0v1.AssertNoDiagnostics()
            Dim ref0v1 = comp0v1.EmitToImageReference()
            Dim comp0v2 = CreateCompilationWithMscorlib40(
<compilation name="A">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("2.0.0.0")>
Public Class A
End Class
    ]]></file>
</compilation>,
                options:=signedDllOptions)
            comp0v2.AssertNoDiagnostics()
            Dim ref0v2 = comp0v2.EmitToImageReference()
            Dim comp1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Public Class B
    Inherits A
End Class
    </file>
</compilation>,
                references:={ref0v1})
            comp1.AssertNoDiagnostics()
            Dim ref1 = comp1.EmitToImageReference()
            Dim comp2 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Namespace System
    Public Class ValueTuple(Of T1, T2)
        Inherits B
        Public Sub New(_1 As T1, _2 As T2)
        End Sub
    End Class
End Namespace
    </file>
</compilation>,
                references:={ref0v1, ref1})
            Dim ref2 = comp2.EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
    Function F() As (Integer, Integer)
End Interface
    </file>
</compilation>,
                references:={ref0v2, ref1, ref2})
            comp.AssertTheseEmitDiagnostics(
<errors>
BC37281: Predefined type 'ValueTuple`2' must be a structure.
</errors>)
        End Sub

        <Fact>
        Public Sub TernaryTypeInferenceWithDynamicAndTupleNames()
            ' No dynamic in VB

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim flag As Boolean = True
        Dim x1 = If(flag, (a:=1, b:=2), (a:=1, c:=3))
        System.Console.Write(x1.a)
    End Sub
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
        Dim x1 = If(flag, (a:=1, b:=2), (a:=1, c:=3))
                                 ~~~~
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
        Dim x1 = If(flag, (a:=1, b:=2), (a:=1, c:=3))
                                               ~~~~
</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().Skip(1).First().Names(0)
            Dim x1Symbol = model.GetDeclaredSymbol(x1)
            Assert.Equal("x1 As (a As System.Int32, System.Int32)", x1Symbol.ToTestDisplayString())

        End Sub


        <Fact>
        <WorkItem(16825, "https://github.com/dotnet/roslyn/issues/16825")>
        Public Sub NullCoalescingOperatorWithTupleNames()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim nab As (a As Integer, b As Integer)? = (1, 2)
        Dim nac As (a As Integer, c As Integer)? = (1, 3)

        Dim x1 = If(nab, nac) ' (a, )?
        Dim x2 = If(nab, nac.Value) ' (a, )
        Dim x3 = If(new C(), nac) ' C
        Dim x4 = If(new D(), nac) ' (a, c)?

        Dim x5 = If(nab IsNot Nothing, nab, nac) ' (a, )?

        Dim x6 = If(nab, (a:= 1, c:= 3)) ' (a, )
        Dim x7 = If(nab, (a:= 1, 3)) ' (a, )
        Dim x8 = If(new C(), (a:= 1, c:= 3)) ' C
        Dim x9 = If(new D(), (a:= 1, c:= 3)) ' (a, c)
        Dim x6double = If(nab, (d:= 1.1, c:= 3)) ' (d, c)

    End Sub
    Public Shared Narrowing Operator CType(ByVal x As (Integer, Integer)) As C
        Throw New System.Exception()
    End Operator
End Class
Class D
    Public Shared Narrowing Operator CType(ByVal x As D) As (d1 As Integer, d2 As Integer)
        Throw New System.Exception()
    End Operator
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
        Dim x6 = If(nab, (a:= 1, c:= 3)) ' (a, )
                                 ~~~~~
</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(2).Names(0)
            Assert.Equal("x1 As System.Nullable(Of (a As System.Int32, System.Int32))", model.GetDeclaredSymbol(x1).ToTestDisplayString())

            Dim x2 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(3).Names(0)
            Assert.Equal("x2 As (a As System.Int32, System.Int32)", model.GetDeclaredSymbol(x2).ToTestDisplayString())

            Dim x3 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(4).Names(0)
            Assert.Equal("x3 As C", model.GetDeclaredSymbol(x3).ToTestDisplayString())

            Dim x4 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(5).Names(0)
            Assert.Equal("x4 As System.Nullable(Of (a As System.Int32, c As System.Int32))", model.GetDeclaredSymbol(x4).ToTestDisplayString())

            Dim x5 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(6).Names(0)
            Assert.Equal("x5 As System.Nullable(Of (a As System.Int32, System.Int32))", model.GetDeclaredSymbol(x5).ToTestDisplayString())

            Dim x6 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(7).Names(0)
            Assert.Equal("x6 As (a As System.Int32, System.Int32)", model.GetDeclaredSymbol(x6).ToTestDisplayString())

            Dim x7 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(8).Names(0)
            Assert.Equal("x7 As (a As System.Int32, System.Int32)", model.GetDeclaredSymbol(x7).ToTestDisplayString())

            Dim x8 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(9).Names(0)
            Assert.Equal("x8 As C", model.GetDeclaredSymbol(x8).ToTestDisplayString())

            Dim x9 = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(10).Names(0)
            Assert.Equal("x9 As (a As System.Int32, c As System.Int32)", model.GetDeclaredSymbol(x9).ToTestDisplayString())

            Dim x6double = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(11).Names(0)
            Assert.Equal("x6double As (d As System.Double, c As System.Int32)", model.GetDeclaredSymbol(x6double).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TernaryTypeInferenceWithNoNames()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim flag As Boolean = True
        Dim x1 = If(flag, (a:=1, b:=2), (1, 3))
        Dim x2 = If(flag, (1, 2), (a:=1, b:=3))
    End Sub
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'a' is ignored because a different name or no name is specified by the target type '(Integer, Integer)'.
        Dim x1 = If(flag, (a:=1, b:=2), (1, 3))
                           ~~~~
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(Integer, Integer)'.
        Dim x1 = If(flag, (a:=1, b:=2), (1, 3))
                                 ~~~~
BC41009: The tuple element name 'a' is ignored because a different name or no name is specified by the target type '(Integer, Integer)'.
        Dim x2 = If(flag, (1, 2), (a:=1, b:=3))
                                   ~~~~
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(Integer, Integer)'.
        Dim x2 = If(flag, (1, 2), (a:=1, b:=3))
                                         ~~~~
</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().Skip(1).First().Names(0)
            Dim x1Symbol = model.GetDeclaredSymbol(x1)
            Assert.Equal("x1 As (System.Int32, System.Int32)", x1Symbol.ToTestDisplayString())

            Dim x2 = nodes.OfType(Of VariableDeclaratorSyntax)().Skip(2).First().Names(0)
            Dim x2Symbol = model.GetDeclaredSymbol(x2)
            Assert.Equal("x2 As (System.Int32, System.Int32)", x2Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TernaryTypeInferenceDropsCandidates()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim flag As Boolean = True
        Dim x1 = If(flag, (a:=1, b:=CType(2, Long)), (a:=CType(1, Byte), c:=3))
    End Sub
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(a As Integer, b As Long)'.
        Dim x1 = If(flag, (a:=1, b:=CType(2, Long)), (a:=CType(1, Byte), c:=3))
                                                                         ~~~~
</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().Skip(1).First().Names(0)
            Dim x1Symbol = model.GetDeclaredSymbol(x1)
            Assert.Equal("x1 As (a As System.Int32, b As System.Int64)", x1Symbol.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub LambdaTypeInferenceWithTupleNames()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim x1 = M2(Function()
            Dim flag = True
            If flag Then
                Return (a:=1, b:=2)
            Else
                If flag Then
                    Return (a:=1, c:=3)
                Else
                    Return (a:=1, d:=4)
                End If
            End If
        End Function)
    End Sub
    Function M2(Of T)(f As System.Func(Of T)) As T
        Return f()
    End Function
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
                Return (a:=1, b:=2)
                              ~~~~
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
                    Return (a:=1, c:=3)
                                  ~~~~
BC41009: The tuple element name 'd' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
                    Return (a:=1, d:=4)
                                  ~~~~

</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Dim x1Symbol = model.GetDeclaredSymbol(x1)
            Assert.Equal("x1 As (a As System.Int32, System.Int32)", x1Symbol.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub LambdaTypeInferenceFallsBackToObject()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim x1 = M2(Function()
            Dim flag = True
            Dim l1 = CType(2, Long)
            If flag Then
                Return (a:=1, b:=l1)
            Else
                If flag Then
                    Return (a:=l1, c:=3)
                Else
                    Return (a:=1, d:=l1)
                End If
            End If
        End Function)
    End Sub
    Function M2(Of T)(f As System.Func(Of T)) As T
        Return f()
    End Function
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics()

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim x1 = nodes.OfType(Of VariableDeclaratorSyntax)().First().Names(0)
            Dim x1Symbol = model.GetDeclaredSymbol(x1)
            Assert.Equal("x1 As System.Object", x1Symbol.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub IsBaseOf_WithoutCustomModifiers()
            ' The IL is from this code, but with modifiers
            ' public class Base<T> { }
            ' public class Derived<T> : Base<T> { }
            ' public class Test
            ' {
            '    public Base<Object> GetBaseWithModifiers() { return null; }
            '    public Derived<Object> GetDerivedWithoutModifiers() { return null; }
            ' }

            Dim il = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly extern System.ValueTuple { .publickeytoken = (CC 7B 13 FF CD 2D DD 51 ) .ver 4:0:1:0 }
.assembly '<<GeneratedFileName>>' { }

.class public auto ansi beforefieldinit Base`1<T>
	extends [mscorlib]System.Object
{
	// Methods
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	} // end of method Base`1::.ctor

} // end of class Base`1

.class public auto ansi beforefieldinit Derived`1<T>
	extends class Base`1<!T>
{
	// Methods
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		// Method begins at RVA 0x2059
		// Code size 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void class Base`1<!T>::.ctor()
		IL_0006: nop
		IL_0007: ret
	} // end of method Derived`1::.ctor

} // end of class Derived`1

.class public auto ansi beforefieldinit Test
	extends [mscorlib]System.Object
{
	// Methods
	.method public hidebysig
		instance class Base`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)> GetBaseWithModifiers () cil managed
	{
		// Method begins at RVA 0x2064
		// Code size 7 (0x7)
		.maxstack 1
		.locals init (
			[0] class Base`1<object modopt([mscorlib]System.Runtime.CompilerServices.IsLong)>
		)

		IL_0000: nop
		IL_0001: ldnull
		IL_0002: stloc.0
		IL_0003: br.s IL_0005

		IL_0005: ldloc.0
		IL_0006: ret
	} // end of method Test::GetBaseWithModifiers

	.method public hidebysig
		instance class Derived`1<object> GetDerivedWithoutModifiers () cil managed
	{
		// Method begins at RVA 0x2078
		// Code size 7 (0x7)
		.maxstack 1
		.locals init (
			[0] class Derived`1<object>
		)

		IL_0000: nop
		IL_0001: ldnull
		IL_0002: stloc.0
		IL_0003: br.s IL_0005

		IL_0005: ldloc.0
		IL_0006: ret
	} // end of method Test::GetDerivedWithoutModifiers

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		// Method begins at RVA 0x2050
		// Code size 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	} // end of method Test::.ctor

} // end of class Test
]]>.Value

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithCustomILSource(source1, il, appendDefaultHeader:=False)
            comp1.AssertTheseDiagnostics()

            Dim baseWithModifiers = comp1.GlobalNamespace.GetMember(Of MethodSymbol)("Test.GetBaseWithModifiers").ReturnType
            Assert.Equal("Base(Of System.Object modopt(System.Runtime.CompilerServices.IsLong))", baseWithModifiers.ToTestDisplayString())

            Dim derivedWithoutModifiers = comp1.GlobalNamespace.GetMember(Of MethodSymbol)("Test.GetDerivedWithoutModifiers").ReturnType
            Assert.Equal("Derived(Of System.Object)", derivedWithoutModifiers.ToTestDisplayString())

            Dim diagnostics = New HashSet(Of DiagnosticInfo)()
            Assert.True(baseWithModifiers.IsBaseTypeOf(derivedWithoutModifiers, diagnostics))
            Assert.True(derivedWithoutModifiers.IsOrDerivedFrom(derivedWithoutModifiers, diagnostics))

        End Sub

        <Fact>
        Public Sub WarnForDroppingNamesInConversion()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim x1 As (a As Integer, Integer) = (1, b:=2)
        Dim x2 As (a As Integer, String) = (1, b:=Nothing)
    End Sub
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
        Dim x1 As (a As Integer, Integer) = (1, b:=2)
                                                ~~~~
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(a As Integer, String)'.
        Dim x2 As (a As Integer, String) = (1, b:=Nothing)
                                               ~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub MethodTypeInferenceMergesTupleNames()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim t = M2((a:=1, b:=2), (a:=1, c:=3))
        System.Console.Write(t.a)
        System.Console.Write(t.b)
        System.Console.Write(t.c)
        M2((1, 2), (c:=1, d:=3))
        M2({(a:=1, b:=2)}, {(1, 3)})
    End Sub
    Function M2(Of T)(x1 As T, x2 As T) As T
        Return x1
    End Function
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
        Dim t = M2((a:=1, b:=2), (a:=1, c:=3))
                          ~~~~
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(a As Integer, Integer)'.
        Dim t = M2((a:=1, b:=2), (a:=1, c:=3))
                                        ~~~~
BC30456: 'b' is not a member of '(a As Integer, Integer)'.
        System.Console.Write(t.b)
                             ~~~
BC30456: 'c' is not a member of '(a As Integer, Integer)'.
        System.Console.Write(t.c)
                             ~~~
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(Integer, Integer)'.
        M2((1, 2), (c:=1, d:=3))
                    ~~~~
BC41009: The tuple element name 'd' is ignored because a different name or no name is specified by the target type '(Integer, Integer)'.
        M2((1, 2), (c:=1, d:=3))
                          ~~~~
</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation1 = model.GetSymbolInfo(nodes.OfType(Of InvocationExpressionSyntax)().First())
            Assert.Equal("(a As System.Int32, System.Int32)",
                         DirectCast(invocation1.Symbol, MethodSymbol).ReturnType.ToTestDisplayString())

            Dim invocation2 = model.GetSymbolInfo(nodes.OfType(Of InvocationExpressionSyntax)().Skip(4).First())
            Assert.Equal("(System.Int32, System.Int32)",
                         DirectCast(invocation2.Symbol, MethodSymbol).ReturnType.ToTestDisplayString())

            Dim invocation3 = model.GetSymbolInfo(nodes.OfType(Of InvocationExpressionSyntax)().Skip(5).First())
            Assert.Equal("(a As System.Int32, b As System.Int32)()",
                         DirectCast(invocation3.Symbol, MethodSymbol).ReturnType.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub MethodTypeInferenceDropsCandidates()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="Tuples">
        <file name="a.vb"><![CDATA[
Class C
    Sub M()
        M2((a:=1, b:=2), (a:=CType(1, Byte), c:=CType(3, Byte)))
        M2((CType(1, Long), b:=2), (c:=1, d:=CType(3, Byte)))
        M2((a:=CType(1, Long), b:=2), (CType(1, Byte), 3))
    End Sub
    Function M2(Of T)(x1 As T, x2 As T) As T
        Return x1
    End Function
End Class
        ]]></file>
    </compilation>, references:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(a As Integer, b As Integer)'.
        M2((a:=1, b:=2), (a:=CType(1, Byte), c:=CType(3, Byte)))
                                             ~~~~~~~~~~~~~~~~~
BC41009: The tuple element name 'c' is ignored because a different name or no name is specified by the target type '(Long, b As Integer)'.
        M2((CType(1, Long), b:=2), (c:=1, d:=CType(3, Byte)))
                                    ~~~~
BC41009: The tuple element name 'd' is ignored because a different name or no name is specified by the target type '(Long, b As Integer)'.
        M2((CType(1, Long), b:=2), (c:=1, d:=CType(3, Byte)))
                                          ~~~~~~~~~~~~~~~~~
</errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation1 = model.GetSymbolInfo(nodes.OfType(Of InvocationExpressionSyntax)().First())
            Assert.Equal("(a As System.Int32, b As System.Int32)",
                         DirectCast(invocation1.Symbol, MethodSymbol).ReturnType.ToTestDisplayString())

            Dim invocation2 = model.GetSymbolInfo(nodes.OfType(Of InvocationExpressionSyntax)().Skip(1).First())
            Assert.Equal("(System.Int64, b As System.Int32)",
                         DirectCast(invocation2.Symbol, MethodSymbol).ReturnType.ToTestDisplayString())

            Dim invocation3 = model.GetSymbolInfo(nodes.OfType(Of InvocationExpressionSyntax)().Skip(2).First())
            Assert.Equal("(a As System.Int64, b As System.Int32)",
                         DirectCast(invocation3.Symbol, MethodSymbol).ReturnType.ToTestDisplayString())

        End Sub

        <Fact()>
        <WorkItem(14267, "https://github.com/dotnet/roslyn/issues/14267")>
        Public Sub NoSystemRuntimeFacade()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module C

    Sub Main()
        Dim o = (1, 2)
    End Sub
End Module

]]></file>
</compilation>, additionalRefs:={ValueTupleRef})

            Assert.Equal(TypeKind.Class, comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).TypeKind)

            comp.AssertTheseDiagnostics(
<errors>
BC30652: Reference required to assembly 'System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the type 'ValueType'. Add one to your project.
        Dim o = (1, 2)
                ~~~~~~
</errors>)
        End Sub

        <Fact>
        <WorkItem(14888, "https://github.com/dotnet/roslyn/issues/14888")>
        Public Sub Iterator_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C
    Shared Sub Main()
        For Each x in Test()
            Console.WriteLine(x)
        Next
    End Sub

    Shared Iterator Function Test() As IEnumerable(Of (integer, integer))
        yield (1, 2)
    End Function
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(1, 2)")
        End Sub

        <Fact()>
        <WorkItem(14888, "https://github.com/dotnet/roslyn/issues/14888")>
        Public Sub Iterator_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C
    Iterator Function Test() As IEnumerable(Of (integer, integer))
        yield (1, 2)
    End Function
End Class
    </file>
</compilation>, additionalRefs:={ValueTupleRef})

            comp.AssertTheseEmitDiagnostics(
<errors>
BC30652: Reference required to assembly 'System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the type 'ValueType'. Add one to your project.
    Iterator Function Test() As IEnumerable(Of (integer, integer))
                                               ~~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the type 'ValueType'. Add one to your project.
        yield (1, 2)
              ~~~~~~
</errors>)
        End Sub

        <Fact>
        <WorkItem(269808, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=269808")>
        Public Sub UserDefinedConversionsAndNameMismatch_01()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test((X1:=1, Y1:=2))
        Dim t1 = (X1:=3, Y1:=4)
        Test(t1)
        Test((5, 6))
        Dim t2 = (7, 8)
        Test(t2)
    End Sub

    Shared Sub Test(val As AA)
    End Sub
End Class

Public Class AA
    Public Shared Widening Operator CType(x As (X1 As Integer, Y1 As Integer)) As AA
        System.Console.WriteLine(x)	
        return new AA()
    End Operator
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
(1, 2)
(3, 4)
(5, 6)
(7, 8)
")
        End Sub

        <Fact>
        <WorkItem(269808, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=269808")>
        Public Sub UserDefinedConversionsAndNameMismatch_02()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test((X1:=1, Y1:=2))
        Dim t1 = (X1:=3, Y1:=4)
        Test(t1)
        Test((5, 6))
        Dim t2 = (7, 8)
        Test(t2)
    End Sub

    Shared Sub Test(val As AA?)
    End Sub
End Class

Public Structure AA
    Public Shared Widening Operator CType(x As (X1 As Integer, Y1 As Integer)) As AA
        System.Console.WriteLine(x)	
        return new AA()
    End Operator
End Structure
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
(1, 2)
(3, 4)
(5, 6)
(7, 8)
")
        End Sub

        <Fact>
        <WorkItem(269808, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=269808")>
        Public Sub UserDefinedConversionsAndNameMismatch_03()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim t1 As (X1 as Integer, Y1 as Integer)?  = (X1:=3, Y1:=4)
        Test(t1)
        Dim t2 As (Integer, Integer)? = (7, 8)
        Test(t2)
        System.Console.WriteLine("--")	
        t1 = Nothing
        Test(t1)
        t2 = Nothing
        Test(t2)
        System.Console.WriteLine("--")	
    End Sub

    Shared Sub Test(val As AA?)
    End Sub
End Class

Public Structure AA
    Public Shared Widening Operator CType(x As (X1 As Integer, Y1 As Integer)) As AA
        System.Console.WriteLine(x)	
        return new AA()
    End Operator
End Structure
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
(3, 4)
(7, 8)
--
--
")
        End Sub

        <Fact>
        <WorkItem(269808, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=269808")>
        Public Sub UserDefinedConversionsAndNameMismatch_04()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test(new AA())
    End Sub

    Shared Sub Test(val As (X1 As Integer, Y1 As Integer))
        System.Console.WriteLine(val)
    End Sub
End Class

Public Class AA
    Public Shared Widening Operator CType(x As AA) As (Integer, Integer)
        return (1, 2)
    End Operator
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(1, 2)")
        End Sub

        <Fact>
        <WorkItem(269808, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=269808")>
        Public Sub UserDefinedConversionsAndNameMismatch_05()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Test(new AA())
    End Sub

    Shared Sub Test(val As (X1 As Integer, Y1 As Integer))
        System.Console.WriteLine(val)
    End Sub
End Class

Public Class AA
    Public Shared Widening Operator CType(x As AA) As (X1 As Integer, Y1 As Integer)
        return (1, 2)
    End Operator
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(1, 2)")
        End Sub

        <Fact>
        <WorkItem(269808, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=269808")>
        Public Sub UserDefinedConversionsAndNameMismatch_06()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C
    Shared Sub Main()
        Dim t1 As BB(Of (X1 as Integer, Y1 as Integer))? = New BB(Of (X1 as Integer, Y1 as Integer))()
        Test(t1)
        Dim t2 As BB(Of (Integer, Integer))? = New BB(Of (Integer, Integer))()
        Test(t2)
        System.Console.WriteLine("--")	
        t1 = Nothing
        Test(t1)
        t2 = Nothing
        Test(t2)
        System.Console.WriteLine("--")	
    End Sub

    Shared Sub Test(val As AA?)
    End Sub
End Class

Public Structure AA
    Public Shared Widening Operator CType(x As BB(Of (X1 As Integer, Y1 As Integer))) As AA
        System.Console.WriteLine("implicit operator AA")	
        return new AA()
    End Operator
End Structure

Public Structure BB(Of T)
End Structure
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="
implicit operator AA
implicit operator AA
--
--
")
        End Sub


        <Fact>
        Public Sub GenericConstraintAttributes()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports ClassLibrary4

Public Interface ITest(Of T)
    ReadOnly Property [Get] As T
End Interface

Public Class Test
    Implements ITest(Of (key As Integer, val As Integer))

    Public ReadOnly Property [Get] As (key As Integer, val As Integer) Implements ITest(Of (key As Integer, val As Integer)).Get
        Get
            Return (0, 0)
        End Get
    End Property
End Class

Public Class Base(Of T) : Implements ITest(Of T)
    Public ReadOnly Property [Get] As T Implements ITest(Of T).Get

    Protected Sub New(t As T)
        [Get] = t
    End Sub
End Class

Public Class C(Of T As ITest(Of (key As Integer, val As Integer)))
    Public ReadOnly Property [Get] As T

    Public Sub New(t As T)
        [Get] = t
    End Sub
End Class

Public Class C2(Of T As Base(Of (key As Integer, val As Integer)))
    Public ReadOnly Property [Get] As T

    Public Sub New(t As T)
        [Get] = t
    End Sub
End Class

Public NotInheritable Class Test2
    Inherits Base(Of (key As Integer, val As Integer))

    Sub New()
        MyBase.New((-1, -2))
    End Sub
End Class

Public Class C3(Of T As IEnumerable(Of (key As Integer, val As Integer)))
    Public ReadOnly Property [Get] As T
    Public Sub New(t As T)
        [Get] = t
    End Sub
End Class

Public Structure TestEnumerable
    Implements IEnumerable(Of (key As Integer, val As Integer))
    Private ReadOnly _backing As (Integer, Integer)()

    Public Sub New(backing As (Integer, Integer)())
        _backing = backing
    End Sub

    Private Class Inner
        Implements IEnumerator(Of (key As Integer, val As Integer)), IEnumerator

        Private index As Integer = -1
        Private ReadOnly _backing As (Integer, Integer)()

        Public Sub New(backing As (Integer, Integer)())
            _backing = backing
        End Sub

        Public ReadOnly Property Current As (key As Integer, val As Integer) Implements IEnumerator(Of (key As Integer, val As Integer)).Current
            Get
                Return _backing(index)
            End Get
        End Property

        Public ReadOnly Property Current1 As Object Implements IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public Function MoveNext() As Boolean
            index += 1
            Return index &lt; _backing.Length
        End Function

        Public Sub Reset()
            Throw New NotSupportedException()
        End Sub

        Private Function IEnumerator_MoveNext() As Boolean Implements IEnumerator.MoveNext
            Return MoveNext()
        End Function

        Private Sub IEnumerator_Reset() Implements IEnumerator.Reset
            Throw New NotImplementedException()
        End Sub
    End Class

    Public Function GetEnumerator() As IEnumerator(Of (key As Integer, val As Integer)) Implements IEnumerable(Of (key As Integer, val As Integer)).GetEnumerator
        Return New Inner(_backing)
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New Inner(_backing)
    End Function
    End Structure


    </file>
</compilation>,
additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, symbolValidator:=Sub(m)
                                                        Dim c = m.GlobalNamespace.GetTypeMember("C")
                                                        Assert.Equal(1, c.TypeParameters.Length)
                                                        Dim param = c.TypeParameters(0)
                                                        Assert.Equal(1, param.ConstraintTypes.Length)
                                                        Dim constraint = Assert.IsAssignableFrom(Of NamedTypeSymbol)(param.ConstraintTypes(0))
                                                        Assert.True(constraint.IsGenericType)
                                                        Assert.Equal(1, constraint.TypeArguments.Length)
                                                        Dim typeArg As TypeSymbol = constraint.TypeArguments(0)
                                                        Assert.True(typeArg.IsTupleType)
                                                        Assert.Equal(2, typeArg.TupleElementTypes.Length)
                                                        Assert.All(typeArg.TupleElementTypes,
                                                                        Sub(t) Assert.Equal(SpecialType.System_Int32, t.SpecialType))
                                                        Assert.False(typeArg.TupleElementNames.IsDefault)
                                                        Assert.Equal(2, typeArg.TupleElementNames.Length)
                                                        Assert.Equal({"key", "val"}, typeArg.TupleElementNames)
                                                        Dim c2 = m.GlobalNamespace.GetTypeMember("C2")
                                                        Assert.Equal(1, c2.TypeParameters.Length)
                                                        param = c2.TypeParameters(0)
                                                        Assert.Equal(1, param.ConstraintTypes.Length)
                                                        constraint = Assert.IsAssignableFrom(Of NamedTypeSymbol)(param.ConstraintTypes(0))
                                                        Assert.True(constraint.IsGenericType)
                                                        Assert.Equal(1, constraint.TypeArguments.Length)
                                                        typeArg = constraint.TypeArguments(0)
                                                        Assert.True(typeArg.IsTupleType)
                                                        Assert.Equal(2, typeArg.TupleElementTypes.Length)
                                                        Assert.All(typeArg.TupleElementTypes,
                                                                         Sub(t) Assert.Equal(SpecialType.System_Int32, t.SpecialType))
                                                        Assert.False(typeArg.TupleElementNames.IsDefault)
                                                        Assert.Equal(2, typeArg.TupleElementNames.Length)
                                                        Assert.Equal({"key", "val"}, typeArg.TupleElementNames)
                                                    End Sub
            )

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        Dim c = New C(Of Test)(New Test())
        Dim temp = c.Get.Get
        Console.WriteLine(temp)
        Console.WriteLine("key:  " &amp; temp.key)
        Console.WriteLine("val:  " &amp; temp.val)

        Dim c2 = New C2(Of Test2)(New Test2())
        Dim temp2 = c2.Get.Get
        Console.WriteLine(temp2)
        Console.WriteLine("key:  " &amp; temp2.key)
        Console.WriteLine("val:  " &amp; temp2.val)

        Dim backing = {(1, 2), (3, 4), (5, 6)}
        Dim c3 = New C3(Of TestEnumerable)(New TestEnumerable(backing))
        For Each kvp In c3.Get
            Console.WriteLine($"key:    {kvp.key}, val: {kvp.val}")
        Next

        Dim c4 = New C(Of Test2)(New Test2())
        Dim temp4 = c4.Get.Get
        Console.WriteLine(temp4)
        Console.WriteLine("key:  " &amp; temp4.key)
        Console.WriteLine("val:   " &amp; temp4.val)
    End Sub
End Module


    </file>
</compilation>, references:=s_valueTupleRefs.Concat({comp.EmitToImageReference()}).ToArray(), expectedOutput:=<![CDATA[
(0, 0)
key:  0
val:  0
(-1, -2)
key:  -1
val:  -2
key:    1, val: 2
key:    3, val: 4
key:    5, val: 6
(-1, -2)
key:  -1
val:   -2
]]>)

        End Sub

        <Fact>
        Public Sub UnusedTuple()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Sub M()
        ' Warnings
        Dim x2 As Integer
        Const x3 As Integer = 1
        Const x4 As String = "hello"
        Dim x5 As (Integer, Integer)
        Dim x6 As (String, String)

        ' No warnings
        Dim y10 As Integer = 1
        Dim y11 As String = "hello"
        Dim y12 As (Integer, Integer) = (1, 2)
        Dim y13 As (String, String) = ("hello", "world")
        Dim tuple As (String, String) = ("hello", "world")
        Dim y14 As (String, String) = tuple
        Dim y15 = (2, 3)
    End Sub
End Class

]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
BC42024: Unused local variable: 'x2'.
        Dim x2 As Integer
            ~~
BC42099: Unused local constant: 'x3'.
        Const x3 As Integer = 1
              ~~
BC42099: Unused local constant: 'x4'.
        Const x4 As String = "hello"
              ~~
BC42024: Unused local variable: 'x5'.
        Dim x5 As (Integer, Integer)
            ~~
BC42024: Unused local variable: 'x6'.
        Dim x6 As (String, String)
            ~~
</errors>)

        End Sub

        <Fact>
        <WorkItem(15198, "https://github.com/dotnet/roslyn/issues/15198")>
        Public Sub TuplePropertyArgs001()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C
    Shared Sub Main()
        dim inst = new C
        dim f As (Integer, Integer) = (inst.P1, inst.P1)
        System.Console.WriteLine(f)
    End Sub

    public readonly Property P1 as integer
        Get 
            return 42
        End Get
    end Property
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(42, 42)")
        End Sub

        <Fact>
        <WorkItem(15198, "https://github.com/dotnet/roslyn/issues/15198")>
        Public Sub TuplePropertyArgs002()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C
    Shared Sub Main()
        dim inst = new C
        dim f As IComparable(of (Integer, Integer)) = (inst.P1, inst.P1)
        System.Console.WriteLine(f)
    End Sub

    public readonly Property P1 as integer
        Get 
            return 42
        End Get
    end Property
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(42, 42)")
        End Sub

        <Fact>
        <WorkItem(15198, "https://github.com/dotnet/roslyn/issues/15198")>
        Public Sub TuplePropertyArgs003()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class C
    Shared Sub Main()
        dim inst as Object = new C
        dim f As (Integer, Integer) = (inst.P1, inst.P1)
        System.Console.WriteLine(f)
    End Sub

    public readonly Property P1 as integer
        Get 
            return 42
        End Get
    end Property
End Class
    </file>
</compilation>,
options:=TestOptions.ReleaseExe, additionalRefs:=s_valueTupleRefs)

            CompileAndVerify(comp, expectedOutput:="(42, 42)")
        End Sub

        <Fact>
        <WorkItem(14844, "https://github.com/dotnet/roslyn/issues/14844")>
        Public Sub InterfaceImplAttributesAreNotSharedAcrossTypeRefs()
            Dim src1 = <compilation>
                           <file name="a.vb">
                               <![CDATA[
Public Interface I1(Of T)
End Interface

Public Interface I2 
    Inherits I1(Of (a As Integer, b As Integer))
End Interface
Public Interface I3 
    Inherits I1(Of (c As Integer, d As Integer))
End Interface
]]>
                           </file>
                       </compilation>

            Dim src2 = <compilation>
                           <file name="a.vb">
                               <![CDATA[
Class C1 
    Implements I2
    Implements I1(Of (a As Integer, b As Integer))
End Class
Class C2
    Implements I3
    Implements I1(Of (c As Integer, d As Integer))
End Class
]]>
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1, references:=s_valueTupleRefs)
            AssertTheseDiagnostics(comp1)

            Dim comp2 = CreateCompilationWithMscorlib40(src2,
                references:={SystemRuntimeFacadeRef, ValueTupleRef, comp1.ToMetadataReference()})
            AssertTheseDiagnostics(comp2)

            Dim comp3 = CreateCompilationWithMscorlib40(src2,
                references:={SystemRuntimeFacadeRef, ValueTupleRef, comp1.EmitToImageReference()})
            AssertTheseDiagnostics(comp3)
        End Sub

        <Fact()>
        <WorkItem(14881, "https://github.com/dotnet/roslyn/issues/14881")>
        <WorkItem(15476, "https://github.com/dotnet/roslyn/issues/15476")>
        Public Sub TupleElementVsLocal()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module C

    Sub Main()
        Dim tuple As (Integer, elem2 As Integer)
        Dim elem2 As Integer

        tuple = (5, 6)
        tuple.elem2 = 23
        elem2 = 10

        Console.WriteLine(tuple.elem2)
        Console.WriteLine(elem2)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "elem2").ToArray()

            Assert.Equal(4, nodes.Length)

            Assert.Equal("tuple.elem2 = 23", nodes(0).Parent.Parent.ToString())
            Assert.Equal("(System.Int32, elem2 As System.Int32).elem2 As System.Int32", model.GetSymbolInfo(nodes(0)).Symbol.ToTestDisplayString())

            Assert.Equal("elem2 = 10", nodes(1).Parent.ToString())
            Assert.Equal("elem2 As System.Int32", model.GetSymbolInfo(nodes(1)).Symbol.ToTestDisplayString())

            Assert.Equal("(tuple.elem2)", nodes(2).Parent.Parent.Parent.ToString())
            Assert.Equal("(System.Int32, elem2 As System.Int32).elem2 As System.Int32", model.GetSymbolInfo(nodes(2)).Symbol.ToTestDisplayString())

            Assert.Equal("(elem2)", nodes(3).Parent.Parent.ToString())
            Assert.Equal("elem2 As System.Int32", model.GetSymbolInfo(nodes(3)).Symbol.ToTestDisplayString())

            Dim type = tree.GetRoot().DescendantNodes().OfType(Of TupleTypeSyntax)().Single()

            Dim symbolInfo = model.GetSymbolInfo(type)
            Assert.Equal("(System.Int32, elem2 As System.Int32)", symbolInfo.Symbol.ToTestDisplayString())

            Dim typeInfo = model.GetTypeInfo(type)
            Assert.Equal("(System.Int32, elem2 As System.Int32)", typeInfo.Type.ToTestDisplayString())

            Assert.Same(symbolInfo.Symbol, typeInfo.Type)

            Assert.Equal(SyntaxKind.TypedTupleElement, type.Elements.First().Kind())
            Assert.Equal("(System.Int32, elem2 As System.Int32).Item1 As System.Int32", model.GetDeclaredSymbol(type.Elements.First()).ToTestDisplayString())
            Assert.Equal(SyntaxKind.NamedTupleElement, type.Elements.Last().Kind())
            Assert.Equal("(System.Int32, elem2 As System.Int32).elem2 As System.Int32", model.GetDeclaredSymbol(type.Elements.Last()).ToTestDisplayString())
            Assert.Equal("(System.Int32, elem2 As System.Int32).Item1 As System.Int32", model.GetDeclaredSymbol(DirectCast(type.Elements.First(), SyntaxNode)).ToTestDisplayString())
            Assert.Equal("(System.Int32, elem2 As System.Int32).elem2 As System.Int32", model.GetDeclaredSymbol(DirectCast(type.Elements.Last(), SyntaxNode)).ToTestDisplayString())
        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ImplementSameInterfaceViaBaseWithDifferentTupleNames()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Interface ITest(Of T)
End Interface

Class Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class

Class Derived
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim derived = tree.GetRoot().DescendantNodes().OfType(Of ClassStatementSyntax)().ElementAt(1)
            Dim derivedSymbol = model.GetDeclaredSymbol(derived)
            Assert.Equal("Derived", derivedSymbol.ToTestDisplayString())

            Assert.Equal(New String() {
                         "ITest(Of (a As System.Int32, b As System.Int32))",
                         "ITest(Of (notA As System.Int32, notB As System.Int32))"},
                derivedSymbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ImplementSameInterfaceViaBase()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Interface ITest(Of T)
End Interface

Class Base
    Implements ITest(Of Integer)
End Class

Class Derived
    Inherits Base
    Implements ITest(Of Integer)
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim derived = tree.GetRoot().DescendantNodes().OfType(Of ClassStatementSyntax)().ElementAt(1)
            Dim derivedSymbol = model.GetDeclaredSymbol(derived)
            Assert.Equal("Derived", derivedSymbol.ToTestDisplayString())

            Assert.Equal(New String() {"ITest(Of System.Int32)"},
                derivedSymbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub GenericImplementSameInterfaceViaBaseWithoutTuples()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Interface ITest(Of T)
End Interface

Class Base
    Implements ITest(Of Integer)
End Class

Class Derived(Of T)
    Inherits Base
    Implements ITest(Of T)
End Class

Module M
    Sub Main()
        Dim instance1 = New Derived(Of Integer)
        Dim instance2 = New Derived(Of String)
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim derived = tree.GetRoot().DescendantNodes().OfType(Of ClassStatementSyntax)().ElementAt(1)
            Dim derivedSymbol = DirectCast(model.GetDeclaredSymbol(derived), NamedTypeSymbol)
            Assert.Equal("Derived(Of T)", derivedSymbol.ToTestDisplayString())

            Assert.Equal(New String() {"ITest(Of System.Int32)", "ITest(Of T)"},
                derivedSymbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

            Dim instance1 = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().ElementAt(0).Names(0)
            Dim instance1Symbol = DirectCast(model.GetDeclaredSymbol(instance1), LocalSymbol).Type
            Assert.Equal("Derived(Of Integer)", instance1Symbol.ToString())

            Assert.Equal(New String() {"ITest(Of System.Int32)"},
                instance1Symbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

            Dim instance2 = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().ElementAt(1).Names(0)
            Dim instance2Symbol = DirectCast(model.GetDeclaredSymbol(instance2), LocalSymbol).Type
            Assert.Equal("Derived(Of String)", instance2Symbol.ToString())

            Assert.Equal(New String() {"ITest(Of System.Int32)", "ITest(Of System.String)"},
                instance2Symbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

            Assert.Empty(derivedSymbol.AsUnboundGenericType().AllInterfaces)

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub GenericImplementSameInterfaceViaBase()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Interface ITest(Of T)
End Interface

Class Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class

Class Derived(Of T)
    Inherits Base
    Implements ITest(Of T)
End Class

Module M
    Sub Main()
        Dim instance1 = New Derived(Of (notA As Integer, notB As Integer))
        Dim instance2 = New Derived(Of (notA As String, notB As String))
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.First()
            Dim model = compilation.GetSemanticModel(tree)
            Dim derived = tree.GetRoot().DescendantNodes().OfType(Of ClassStatementSyntax)().ElementAt(1)
            Dim derivedSymbol = model.GetDeclaredSymbol(derived)
            Assert.Equal("Derived(Of T)", derivedSymbol.ToTestDisplayString())

            Assert.Equal(New String() {"ITest(Of (a As System.Int32, b As System.Int32))", "ITest(Of T)"},
                derivedSymbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

            Dim instance1 = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().ElementAt(0).Names(0)
            Dim instance1Symbol = DirectCast(model.GetDeclaredSymbol(instance1), LocalSymbol).Type
            Assert.Equal("Derived(Of (notA As Integer, notB As Integer))", instance1Symbol.ToString())

            Assert.Equal(New String() {
                         "ITest(Of (a As System.Int32, b As System.Int32))",
                         "ITest(Of (notA As System.Int32, notB As System.Int32))"},
                instance1Symbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

            Dim instance2 = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().ElementAt(1).Names(0)
            Dim instance2Symbol = DirectCast(model.GetDeclaredSymbol(instance2), LocalSymbol).Type
            Assert.Equal("Derived(Of (notA As String, notB As String))", instance2Symbol.ToString())

            Assert.Equal(New String() {
                         "ITest(Of (a As System.Int32, b As System.Int32))",
                         "ITest(Of (notA As System.String, notB As System.String))"},
                instance2Symbol.AllInterfaces.Select(Function(i) i.ToTestDisplayString()))

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub GenericExplicitIEnumerableImplementationUsedWithDifferentTypesAndTupleNames()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Class Base
    Implements IEnumerable(Of (a As Integer, b As Integer))

    Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New Exception()
    End Function
    Function GetEnumerator2() As IEnumerator(Of (a As Integer, b As Integer)) Implements IEnumerable(Of (a As Integer, b As Integer)).GetEnumerator
        Throw New Exception()
    End Function
End Class

Class Derived(Of T)
    Inherits Base
    Implements IEnumerable(Of T)

    Public Dim state As T

    Function GetEnumerator3() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New Exception()
    End Function
    Function GetEnumerator4() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return New DerivedEnumerator With {.state = state}
    End Function

    Public Class DerivedEnumerator
        Implements IEnumerator(Of T)

        Public Dim state As T
        Dim done As Boolean = False

        Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If done Then
                Return False
            Else
                done = True
                Return True
            End If
        End Function

        ReadOnly Property Current As T Implements IEnumerator(Of T).Current
            Get
                Return state
            End Get
        End Property

        ReadOnly Property Current2 As Object Implements IEnumerator.Current
            Get
                Throw New Exception()
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public Sub Reset() Implements IEnumerator.Reset
        End Sub
    End Class
End Class

Module M
    Sub Main()
        Dim collection = New Derived(Of (notA As String, notB As String)) With {.state = (42, 43)}
        For Each x In collection
            Console.Write(x.notA)
        Next
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            compilation.AssertTheseDiagnostics(<errors>
BC32096: 'For Each' on type 'Derived(Of (notA As String, notB As String))' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each x In collection
                      ~~~~~~~~~~
</errors>)

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub GenericExplicitIEnumerableImplementationUsedWithDifferentTupleNames()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Class Base
    Implements IEnumerable(Of (a As Integer, b As Integer))

    Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New Exception()
    End Function
    Function GetEnumerator2() As IEnumerator(Of (a As Integer, b As Integer)) Implements IEnumerable(Of (a As Integer, b As Integer)).GetEnumerator
        Throw New Exception()
    End Function
End Class

Class Derived(Of T)
    Inherits Base
    Implements IEnumerable(Of T)

    Public Dim state As T

    Function GetEnumerator3() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New Exception()
    End Function
    Function GetEnumerator4() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return New DerivedEnumerator With {.state = state}
    End Function

    Public Class DerivedEnumerator
        Implements IEnumerator(Of T)

        Public Dim state As T
        Dim done As Boolean = False

        Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If done Then
                Return False
            Else
                done = True
                Return True
            End If
        End Function

        ReadOnly Property Current As T Implements IEnumerator(Of T).Current
            Get
                Return state
            End Get
        End Property

        ReadOnly Property Current2 As Object Implements IEnumerator.Current
            Get
                Throw New Exception()
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public Sub Reset() Implements IEnumerator.Reset
        End Sub
    End Class
End Class

Module M
    Sub Main()
        Dim collection = New Derived(Of (notA As Integer, notB As Integer)) With {.state = (42, 43)}
        For Each x In collection
            Console.Write(x.notA)
        Next
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            compilation.AssertTheseDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="42")

        End Sub

        <Fact()>
        <WorkItem(14843, "https://github.com/dotnet/roslyn/issues/14843")>
        Public Sub TupleNameDifferencesIgnoredInConstraintWhenNotIdentityConversion()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I1(Of T)
End Interface

Class Base(Of U As I1(Of (a As Integer, b As Integer)))
End Class

Class Derived
    Inherits Base(Of I1(Of (notA As Integer, notB As Integer)))
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

        End Sub

        <Fact()>
        <WorkItem(14843, "https://github.com/dotnet/roslyn/issues/14843")>
        Public Sub TupleNameDifferencesIgnoredInConstraintWhenNotIdentityConversion2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I1(Of T)
End Interface

Interface I2(Of T)
    Inherits I1(Of T)
End Interface

Class Base(Of U As I1(Of (a As Integer, b As Integer)))
End Class

Class Derived
    Inherits Base(Of I2(Of (notA As Integer, notB As Integer)))
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub CanReImplementInterfaceWithDifferentTupleNames()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface ITest(Of T)
    Function M() As T
End Interface

Class Base
    Implements ITest(Of (a As Integer, b As Integer))

    Function M() As (a As Integer, b As Integer) Implements ITest(Of (a As Integer, b As Integer)).M
        Return (1, 2)
    End Function
End Class

Class Derived
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))

    Overloads Function M() As (notA As Integer, notB As Integer) Implements ITest(Of (notA As Integer, notB As Integer)).M
        Return (3, 4)
    End Function
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics()

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ExplicitBaseImplementationNotConsideredImplementationForInterfaceWithDifferentTupleNames_01()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface ITest(Of T)
    Function M() As T
End Interface

Class Base
    Implements ITest(Of (a As Integer, b As Integer))

    Function M() As (a As Integer, b As Integer) Implements ITest(Of (a As Integer, b As Integer)).M
        Return (1, 2)
    End Function
End Class

Class Derived1
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))
End Class
Class Derived2
    Inherits Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics(<errors>
BC30149: Class 'Derived1' must implement 'Function M() As (notA As Integer, notB As Integer)' for interface 'ITest(Of (notA As Integer, notB As Integer))'.
    Implements ITest(Of (notA As Integer, notB As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ExplicitBaseImplementationNotConsideredImplementationForInterfaceWithDifferentTupleNames_02()

            Dim csSource = "
public interface ITest<T>
{
    T M();
}
public class Base : ITest<(int a, int b)>
{
    (int a, int b) ITest<(int a, int b)>.M() { return (1, 2); } // explicit implementation
    public virtual (int notA, int notB) M() { return (1, 2); }
}
"
            Dim csComp = CreateCSharpCompilation(csSource,
                                                 compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                 referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.StandardAndVBRuntime))
            csComp.VerifyDiagnostics()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Class Derived1
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))
End Class
Class Derived2
    Inherits Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class
]]></file>
</compilation>, references:={csComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<errors>
BC30149: Class 'Derived1' must implement 'Function M() As (notA As Integer, notB As Integer)' for interface 'ITest(Of (notA As Integer, notB As Integer))'.
    Implements ITest(Of (notA As Integer, notB As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

            Dim derived1 As INamedTypeSymbol = comp.GetTypeByMetadataName("Derived1")
            Assert.Equal("ITest(Of (notA As System.Int32, notB As System.Int32))", derived1.Interfaces(0).ToTestDisplayString())

            Dim derived2 As INamedTypeSymbol = comp.GetTypeByMetadataName("Derived2")
            Assert.Equal("ITest(Of (a As System.Int32, b As System.Int32))", derived2.Interfaces(0).ToTestDisplayString())

            Dim m = comp.GetTypeByMetadataName("Base").GetMembers("ITest<(System.Int32a,System.Int32b)>.M").Single()
            Dim mImplementations = DirectCast(m, IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(1, mImplementations.Length)
            Assert.Equal("Function ITest(Of (System.Int32, System.Int32)).M() As (System.Int32, System.Int32)", mImplementations(0).ToTestDisplayString())

            Assert.Same(m, derived1.FindImplementationForInterfaceMember(DirectCast(derived1.Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m, derived1.FindImplementationForInterfaceMember(DirectCast(derived2.Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m, derived2.FindImplementationForInterfaceMember(DirectCast(derived1.Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m, derived2.FindImplementationForInterfaceMember(DirectCast(derived2.Interfaces(0), TypeSymbol).GetMember("M")))
        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ExplicitBaseImplementationNotConsideredImplementationForInterfaceWithDifferentTupleNames_03()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface ITest(Of T)
    Function M() As T
End Interface

Class Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class

Class Derived1
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))
End Class
Class Derived2
    Inherits Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            compilation.AssertTheseDiagnostics(<errors>
BC30149: Class 'Base' must implement 'Function M() As (a As Integer, b As Integer)' for interface 'ITest(Of (a As Integer, b As Integer))'.
    Implements ITest(Of (a As Integer, b As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30149: Class 'Derived1' must implement 'Function M() As (notA As Integer, notB As Integer)' for interface 'ITest(Of (notA As Integer, notB As Integer))'.
    Implements ITest(Of (notA As Integer, notB As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               </errors>)

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ExplicitBaseImplementationNotConsideredImplementationForInterfaceWithDifferentTupleNames_04()
            Dim compilation1 = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Public Interface ITest(Of T)
    Function M() As T
End Interface

Public Class Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class
]]></file>
</compilation>)

            compilation1.AssertTheseDiagnostics(<errors>
BC30149: Class 'Base' must implement 'Function M() As (a As Integer, b As Integer)' for interface 'ITest(Of (a As Integer, b As Integer))'.
    Implements ITest(Of (a As Integer, b As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                </errors>)

            Dim compilation2 = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Class Derived1
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))
End Class
Class Derived2
    Inherits Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class
]]></file>
</compilation>, references:={compilation1.ToMetadataReference()})

            compilation2.AssertTheseDiagnostics(<errors>
BC30149: Class 'Derived1' must implement 'Function M() As (notA As Integer, notB As Integer)' for interface 'ITest(Of (notA As Integer, notB As Integer))'.
    Implements ITest(Of (notA As Integer, notB As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                </errors>)

        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ExplicitBaseImplementationNotConsideredImplementationForInterfaceWithDifferentTupleNames_05()

            Dim csSource = "
public interface ITest<T>
{
    T M();
}
public class Base : ITest<(int a, int b)>
{
    public virtual (int a, int b) M() { return (1, 2); }
}
"
            Dim csComp = CreateCSharpCompilation(csSource,
                                                 compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                                                 referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.StandardAndVBRuntime))
            csComp.VerifyDiagnostics()

            Dim comp = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Class Derived1
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))
End Class
Class Derived2
    Inherits Base
    Implements ITest(Of (a As Integer, b As Integer))
End Class
]]></file>
</compilation>, references:={csComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<errors>
BC30149: Class 'Derived1' must implement 'Function M() As (notA As Integer, notB As Integer)' for interface 'ITest(Of (notA As Integer, notB As Integer))'.
    Implements ITest(Of (notA As Integer, notB As Integer))
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

            Dim derived1 As INamedTypeSymbol = comp.GetTypeByMetadataName("Derived1")
            Assert.Equal("ITest(Of (notA As System.Int32, notB As System.Int32))", derived1.Interfaces(0).ToTestDisplayString())

            Dim derived2 As INamedTypeSymbol = comp.GetTypeByMetadataName("Derived2")
            Assert.Equal("ITest(Of (a As System.Int32, b As System.Int32))", derived2.Interfaces(0).ToTestDisplayString())

            Dim m = comp.GetTypeByMetadataName("Base").GetMember("M")
            Dim mImplementations = DirectCast(m, IMethodSymbol).ExplicitInterfaceImplementations
            Assert.Equal(0, mImplementations.Length)

            Assert.Same(m, derived1.FindImplementationForInterfaceMember(DirectCast(derived1.Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m, derived1.FindImplementationForInterfaceMember(DirectCast(derived2.Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m, derived2.FindImplementationForInterfaceMember(DirectCast(derived1.Interfaces(0), TypeSymbol).GetMember("M")))
            Assert.Same(m, derived2.FindImplementationForInterfaceMember(DirectCast(derived2.Interfaces(0), TypeSymbol).GetMember("M")))
        End Sub

        <Fact()>
        <WorkItem(14841, "https://github.com/dotnet/roslyn/issues/14841")>
        Public Sub ReImplementationAndInference()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface ITest(Of T)
    Function M() As T
End Interface

Class Base
    Implements ITest(Of (a As Integer, b As Integer))

    Function M() As (a As Integer, b As Integer) Implements ITest(Of (a As Integer, b As Integer)).M
        Return (1, 2)
    End Function
End Class

Class Derived
    Inherits Base
    Implements ITest(Of (notA As Integer, notB As Integer))

    Overloads Function M() As (notA As Integer, notB As Integer) Implements ITest(Of (notA As Integer, notB As Integer)).M
        Return (3, 4)
    End Function
End Class

Class C
    Shared Sub Main()
        Dim b As Base = New Derived()
        Dim x = Test(b) ' tuple names from Base, implementation from Derived
        System.Console.WriteLine(x.a)
    End Sub

    Shared Function Test(Of T)(t1 As ITest(Of T)) As T
        Return t1.M()
    End Function
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)

            comp.AssertTheseDiagnostics()
            CompileAndVerify(comp, expectedOutput:="3")

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()
            Dim x = nodes.OfType(Of VariableDeclaratorSyntax)().ElementAt(1).Names(0)
            Assert.Equal("x", x.Identifier.ToString())
            Dim xSymbol = DirectCast(model.GetDeclaredSymbol(x), LocalSymbol).Type
            Assert.Equal("(a As System.Int32, b As System.Int32)", xSymbol.ToTestDisplayString())
        End Sub

        <Fact()>
        <WorkItem(14091, "https://github.com/dotnet/roslyn/issues/14091")>
        Public Sub TupleTypeWithTooFewElements()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Shared Sub M(x As Integer, y As (), z As (a As Integer))
    End Sub
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(<errors>
BC30182: Type expected.
    Shared Sub M(x As Integer, y As (), z As (a As Integer))
                                     ~
BC37259: Tuple must contain at least two elements.
    Shared Sub M(x As Integer, y As (), z As (a As Integer))
                                     ~
BC37259: Tuple must contain at least two elements.
    Shared Sub M(x As Integer, y As (), z As (a As Integer))
                                                          ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()

            Dim y = nodes.OfType(Of TupleTypeSyntax)().ElementAt(0)
            Assert.Equal("()", y.ToString())
            Dim yType = model.GetTypeInfo(y)
            Assert.Equal("(?, ?)", yType.Type.ToTestDisplayString())

            Dim z = nodes.OfType(Of TupleTypeSyntax)().ElementAt(1)
            Assert.Equal("(a As Integer)", z.ToString())
            Dim zType = model.GetTypeInfo(z)
            Assert.Equal("(a As System.Int32, ?)", zType.Type.ToTestDisplayString())
        End Sub

        <Fact()>
        <WorkItem(14091, "https://github.com/dotnet/roslyn/issues/14091")>
        Public Sub TupleExpressionWithTooFewElements()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Dim x = (Alice:=1)
End Class
]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(<errors>
BC37259: Tuple must contain at least two elements.
    Dim x = (Alice:=1)
                     ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = comp.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes()
            Dim tuple = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(0)
            Assert.Equal("(Alice:=1)", tuple.ToString())
            Dim tupleType = model.GetTypeInfo(tuple)
            Assert.Equal("(Alice As System.Int32, ?)", tupleType.Type.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub GetWellKnownTypeWithAmbiguities()
            Const versionTemplate = "<Assembly: System.Reflection.AssemblyVersion(""{0}.0.0.0"")>"

            Const corlib_vb = "
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
    Public Class [String]
    End Class
    Public Class Attribute
    End Class
End Namespace

Namespace System.Reflection
    Public Class AssemblyVersionAttribute
        Inherits Attribute

        Public Sub New(version As String)
        End Sub
    End Class
End Namespace
"

            Const valuetuple_vb As String = "
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
        End Sub
    End Structure
End Namespace
"

            Dim corlibWithoutVT = CreateEmptyCompilation({String.Format(versionTemplate, "1") + corlib_vb}, options:=TestOptions.DebugDll, assemblyName:="corlib")
            corlibWithoutVT.AssertTheseDiagnostics()
            Dim corlibWithoutVTRef = corlibWithoutVT.EmitToImageReference()

            Dim corlibWithVT = CreateEmptyCompilation({String.Format(versionTemplate, "2") + corlib_vb + valuetuple_vb}, options:=TestOptions.DebugDll, assemblyName:="corlib")
            corlibWithVT.AssertTheseDiagnostics()
            Dim corlibWithVTRef = corlibWithVT.EmitToImageReference()

            Dim libWithVT = CreateEmptyCompilation(valuetuple_vb, references:={corlibWithoutVTRef}, options:=TestOptions.DebugDll)
            libWithVT.VerifyDiagnostics()
            Dim libWithVTRef = libWithVT.EmitToImageReference()

            Dim comp = VisualBasicCompilation.Create("test", references:={libWithVTRef, corlibWithVTRef})
            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).IsErrorType())

            Dim comp2 = comp.WithOptions(comp.Options.WithIgnoreCorLibraryDuplicatedTypes(True))
            Dim tuple2 = comp2.GetWellKnownType(WellKnownType.System_ValueTuple_T2)
            Assert.False(tuple2.IsErrorType())
            Assert.Equal(libWithVTRef.Display, tuple2.ContainingAssembly.MetadataName.ToString())

            Dim comp3 = VisualBasicCompilation.Create("test", references:={corlibWithVTRef, libWithVTRef}). ' order reversed
                WithOptions(comp.Options.WithIgnoreCorLibraryDuplicatedTypes(True))
            Dim tuple3 = comp3.GetWellKnownType(WellKnownType.System_ValueTuple_T2)
            Assert.False(tuple3.IsErrorType())
            Assert.Equal(libWithVTRef.Display, tuple3.ContainingAssembly.MetadataName.ToString())

        End Sub

        <Fact>
        Public Sub CheckedConversions()
            Dim source =
<compilation>
    <file>
Imports System
Class C
    Shared Function F(t As (Integer, Integer)) As (Long, Byte)
        Return CType(t, (Long, Byte))
    End Function
    Shared Sub Main()
        Try
            Dim t = F((-1, -1))
            Console.WriteLine(t)
        Catch e As OverflowException
            Console.WriteLine("overflow")
        End Try
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(
                source,
                options:=TestOptions.ReleaseExe.WithOverflowChecks(False),
                references:=s_valueTupleRefs, expectedOutput:=<![CDATA[(-1, 255)]]>)
            verifier.VerifyIL("C.F", <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0008:  conv.i8
  IL_0009:  ldloc.0
  IL_000a:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_000f:  conv.u1
  IL_0010:  newobj     "Sub System.ValueTuple(Of Long, Byte)..ctor(Long, Byte)"
  IL_0015:  ret
}
]]>)
            verifier = CompileAndVerify(
                source,
                options:=TestOptions.ReleaseExe.WithOverflowChecks(True),
                references:=s_valueTupleRefs, expectedOutput:=<![CDATA[overflow]]>)
            verifier.VerifyIL("C.F", <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldfld      "System.ValueTuple(Of Integer, Integer).Item1 As Integer"
  IL_0008:  conv.i8
  IL_0009:  ldloc.0
  IL_000a:  ldfld      "System.ValueTuple(Of Integer, Integer).Item2 As Integer"
  IL_000f:  conv.ovf.u1
  IL_0010:  newobj     "Sub System.ValueTuple(Of Long, Byte)..ctor(Long, Byte)"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(18738, "https://github.com/dotnet/roslyn/issues/18738")>
        Public Sub TypelessTupleWithNoImplicitConversion()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
option strict on
Imports System
Module C
    Sub M()
        Dim e As Integer? = 5
        Dim x as (Integer, String) = (e, Nothing) ' No implicit conversion
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)
            comp.AssertTheseDiagnostics(<errors>
BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Integer'.
        Dim x as (Integer, String) = (e, Nothing) ' No implicit conversion
                                      ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.NarrowingTuple, model.GetConversion(node).Kind)
        End Sub

        <Fact>
        <WorkItem(18738, "https://github.com/dotnet/roslyn/issues/18738")>
        Public Sub TypelessTupleWithNoImplicitConversion2()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
option strict on
Imports System
Module C
    Sub M()
        Dim e As Integer? = 5
        Dim x as (Integer, String, Integer) = (e, Nothing) ' No conversion
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)
            comp.AssertTheseDiagnostics(<errors>
BC30311: Value of type '(e As Integer?, Object)' cannot be converted to '(Integer, String, Integer)'.
        Dim x as (Integer, String, Integer) = (e, Nothing) ' No conversion
                                              ~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e, Nothing)", node.ToString())
            Assert.Null(model.GetTypeInfo(node).Type)
            Assert.Equal("(System.Int32, System.String, System.Int32)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, model.GetConversion(node).Kind)
        End Sub

        <Fact>
        <WorkItem(18738, "https://github.com/dotnet/roslyn/issues/18738")>
        Public Sub TypedTupleWithNoImplicitConversion()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
option strict on
Imports System
Module C
    Sub M()
        Dim e As Integer? = 5
        Dim x as (Integer, String, Integer) = (e, "") ' No conversion
        System.Console.WriteLine(x.ToString())
    End Sub
End Module

    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)
            comp.AssertTheseDiagnostics(<errors>
BC30311: Value of type '(e As Integer?, String)' cannot be converted to '(Integer, String, Integer)'.
        Dim x as (Integer, String, Integer) = (e, "") ' No conversion
                                              ~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=False)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().Single()

            Assert.Equal("(e, """")", node.ToString())
            Assert.Equal("(e As System.Nullable(Of System.Int32), System.String)", model.GetTypeInfo(node).Type.ToTestDisplayString())
            Assert.Equal("(System.Int32, System.String, System.Int32)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, model.GetConversion(node).Kind)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        Public Sub MoreGenericTieBreaker_01()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Module Module1
    Public Sub Main()
        Dim a As A(Of A(Of Integer)) = Nothing
        M1(a) ' ok, selects M1(Of T)(A(Of A(Of T)) a)

        Dim b = New ValueTuple(Of ValueTuple(Of Integer, Integer), Integer)()
        M2(b) ' ok, should select M2(Of T)(ValueTuple(Of ValueTuple(Of T, Integer), Integer) a)
    End Sub

    Public Sub M1(Of T)(a As A(Of T))
        Console.Write(1)
    End Sub
    Public Sub M1(Of T)(a As A(Of A(Of T)))
        Console.Write(2)
    End Sub

    Public Sub M2(Of T)(a As ValueTuple(Of T, Integer))
        Console.Write(3)
    End Sub
    Public Sub M2(Of T)(a As ValueTuple(Of ValueTuple(Of T, Integer), Integer))
        Console.Write(4)
    End Sub
End Module

Public Class A(Of T)
End Class
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
24
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        Public Sub MoreGenericTieBreaker_01b()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Module Module1
    Public Sub Main()
        Dim b = ((0, 0), 0)
        M2(b) ' ok, should select M2(Of T)(ValueTuple(Of ValueTuple(Of T, Integer), Integer) a)
    End Sub

    Public Sub M2(Of T)(a As (T, Integer))
        Console.Write(3)
    End Sub
    Public Sub M2(Of T)(a As ((T, Integer), Integer))
        Console.Write(4)
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
4
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a1()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        ' Dim b = (1, 2, 3, 4, 5, 6, 7, 8)
        Dim b = new ValueTuple(Of int, int, int, int, int, int, int, ValueTuple(Of int))(1, 2, 3, 4, 5, 6, 7, new ValueTuple(Of int)(8))
        M1(b)
        M2(b) 
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(1) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, ValueTuple(Of T8)))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(3) 
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
12
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a2()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        ' Dim b = (1, 2, 3, 4, 5, 6, 7, 8)
        Dim b = new ValueTuple(Of int, int, int, int, int, int, int, ValueTuple(Of int))(1, 2, 3, 4, 5, 6, 7, new ValueTuple(Of int)(8))
        M1(b)
        M2(b) 
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(ByRef a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(1) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(ByRef a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, ValueTuple(Of T8)))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(ByRef a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(3) 
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
12
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a3()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        Dim b As I(Of ValueTuple(Of int, int, int, int, int, int, int, ValueTuple(Of int))) = Nothing
        M1(b)
        M2(b) 
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(a As I(Of ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)))
        Console.Write(1) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(a As I(Of ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, ValueTuple(Of T8))))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As I(Of ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)))
        Console.Write(3) 
    End Sub
End Module

Interface I(Of in T)
End Interface
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
12
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a4()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        Dim b As I(Of ValueTuple(Of int, int, int, int, int, int, int, ValueTuple(Of int))) = Nothing
        M1(b)
        M2(b) 
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(a As I(Of ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)))
        Console.Write(1) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(a As I(Of ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, ValueTuple(Of T8))))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As I(Of ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest)))
        Console.Write(3) 
    End Sub
End Module

Interface I(Of out T)
End Interface
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
12
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a5()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        M1((1, 2, 3, 4, 5, 6, 7, 8))
        M2((1, 2, 3, 4, 5, 6, 7, 8)) 
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(1) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, ValueTuple(Of T8)))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(3) 
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
12
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a6()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        M2((Function() 1, Function() 2, Function() 3, Function() 4, Function() 5, Function() 6, Function() 7, Function() 8))
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(a As ValueTuple(Of Func(Of T1), Func(Of T2), Func(Of T3), Func(Of T4), Func(Of T5), Func(Of T6), Func(Of T7), ValueTuple(Of Func(Of T8))))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As ValueTuple(Of Func(Of T1), Func(Of T2), Func(Of T3), Func(Of T4), Func(Of T5), Func(Of T6), Func(Of T7), TRest))
        Console.Write(3) 
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
2
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02a7()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        M1((Function() 1, Function() 2, Function() 3, Function() 4, Function() 5, Function() 6, Function() 7, Function() 8))
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(a As ValueTuple(Of Func(Of T1), Func(Of T2), Func(Of T3), Func(Of T4), Func(Of T5), Func(Of T6), Func(Of T7), TRest))
        Console.Write(1) 
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:=s_valueTupleRefs)
            comp.AssertTheseDiagnostics(
<expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As ValueTuple(Of Func(Of T1), Func(Of T2), Func(Of T3), Func(Of T4), Func(Of T5), Func(Of T6), Func(Of T7), TRest))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        M1((Function() 1, Function() 2, Function() 3, Function() 4, Function() 5, Function() 6, Function() 7, Function() 8))
        ~~
</expected>
            )
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        <WorkItem(20583, "https://github.com/dotnet/roslyn/issues/20583")>
        Public Sub MoreGenericTieBreaker_02b()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports int = System.Int32

Module Module1
    Public Sub Main()
        Dim b = (1, 2, 3, 4, 5, 6, 7, 8)
        M1(b)
        M2(b) 
    End Sub

    Public Sub M1(Of T1, T2, T3, T4, T5, T6, T7, TRest as Structure)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(1) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, T8)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, ValueTuple(Of T8)))
        Console.Write(2) 
    End Sub

    Public Sub M2(Of T1, T2, T3, T4, T5, T6, T7, TRest As Structure)(a As ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, TRest))
        Console.Write(3) 
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
12
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        Public Sub MoreGenericTieBreaker_03()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Module Module1
    Public Sub Main()
        Dim b = ((1, 1), 2, 3, 4, 5, 6, 7, 8, 9, (10, 10), (11, 11))
        M1(b) ' ok, should select M1(Of T, U, V)(a As ((T, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, (U, Integer), V))
    End Sub

    Sub M1(Of T, U, V)(a As ((T, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, (U, Integer), V))
        Console.Write(3)
    End Sub
    Sub M1(Of T, U, V)(a As (T, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, U, (V, Integer)))
        Console.Write(4)
    End Sub
End Module
    </file>
</compilation>, references:=s_valueTupleRefs, expectedOutput:=<![CDATA[
3
]]>)
        End Sub

        <Fact>
        <WorkItem(20494, "https://github.com/dotnet/roslyn/issues/20494")>
        Public Sub MoreGenericTieBreaker_04()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Module Module1
    Public Sub Main()
        Dim b = ((1, 1), 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, (20, 20))
        M1(b) ' error: ambiguous
    End Sub

    Sub M1(Of T, U)(a As (T, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, (U, Integer)))
        Console.Write(3)
    End Sub
    Sub M1(Of T, U)(a As ((T, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, U))
        Console.Write(4)
    End Sub
End Module
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:=s_valueTupleRefs)
            comp.VerifyDiagnostics(
    Diagnostic(ERRID.ERR_NoMostSpecificOverload2, "M1").WithArguments("M1", "
    'Public Sub M1(Of (Integer, Integer), Integer)(a As ((Integer, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer)))': Not most specific.
    'Public Sub M1(Of Integer, (Integer, Integer))(a As ((Integer, Integer), Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, (Integer, Integer)))': Not most specific.").WithLocation(7, 9)
                )
        End Sub

        <Fact>
        <WorkItem(21785, "https://github.com/dotnet/roslyn/issues/21785")>
        Public Sub TypelessTupleInArrayInitializer()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Module1
    Private mTupleArray As (X As Integer, P As System.Func(Of Byte(), Integer))() = {
            (X:=0, P:=Nothing), (X:=0, P:=AddressOf MyFunction)
        }

    Sub Main()
        System.Console.Write(mTupleArray(1).P(Nothing))
    End Sub

    Public Function MyFunction(ArgBytes As Byte()) As Integer
        Return 1
    End Function
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)
            comp.AssertNoDiagnostics()
            CompileAndVerify(comp, expectedOutput:="1")

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(1)

            Assert.Equal("(X:=0, P:=AddressOf MyFunction)", node.ToString())
            Dim tupleSymbol = model.GetTypeInfo(node)
            Assert.Null(tupleSymbol.Type)

            Assert.Equal("(X As System.Int32, P As System.Func(Of System.Byte(), System.Int32))",
                         tupleSymbol.ConvertedType.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(21785, "https://github.com/dotnet/roslyn/issues/21785")>
        Public Sub TypelessTupleInArrayInitializerWithInferenceFailure()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Module1
    Private mTupleArray = { (X:=0, P:=AddressOf MyFunction) }

    Sub Main()
        System.Console.Write(mTupleArray(1).P(Nothing))
    End Sub

    Public Function MyFunction(ArgBytes As Byte()) As Integer
        Return 1
    End Function
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics(<errors>
BC30491: Expression does not produce a value.
    Private mTupleArray = { (X:=0, P:=AddressOf MyFunction) }
                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(0)

            Assert.Equal("(X:=0, P:=AddressOf MyFunction)", node.ToString())
            Dim tupleSymbol = model.GetTypeInfo(node)
            Assert.Null(tupleSymbol.Type)

            Assert.Equal("System.Object", tupleSymbol.ConvertedType.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(21785, "https://github.com/dotnet/roslyn/issues/21785")>
        Public Sub TypelessTupleInArrayInitializerWithInferenceSuccess()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim y As MyDelegate = AddressOf MyFunction
        Dim mTupleArray = { (X:=0, P:=y), (X:=0, P:=AddressOf MyFunction) }
        System.Console.Write(mTupleArray(1).P(Nothing))
    End Sub

    Delegate Function MyDelegate(ArgBytes As Byte()) As Integer

    Public Function MyFunction(ArgBytes As Byte()) As Integer
        Return 1
    End Function
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs, options:=TestOptions.DebugExe)
            comp.AssertNoDiagnostics()
            CompileAndVerify(comp, expectedOutput:="1")

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim node = nodes.OfType(Of TupleExpressionSyntax)().ElementAt(1)

            Assert.Equal("(X:=0, P:=AddressOf MyFunction)", node.ToString())
            Dim tupleSymbol = model.GetTypeInfo(node)
            Assert.Null(tupleSymbol.Type)

            Assert.Equal("(X As System.Int32, P As Module1.MyDelegate)", tupleSymbol.ConvertedType.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(24781, "https://github.com/dotnet/roslyn/issues/24781")>
        Public Sub InferenceWithTuple()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Public Interface IAct(Of T)
    Function Act(Of TReturn)(fn As Func(Of T, TReturn)) As IResult(Of TReturn)
End Interface

Public Interface IResult(Of TReturn)
End Interface

Module Module1
    Sub M(impl As IAct(Of (Integer, Integer)))
        Dim case3 = impl.Act(Function(a As (x As Integer, y As Integer)) a.x * a.y)
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=s_valueTupleRefs)
            comp.AssertTheseDiagnostics()

            Dim tree = comp.SyntaxTrees(0)

            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim actSyntax = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("impl.Act(Function(a As (x As Integer, y As Integer)) a.x * a.y)", actSyntax.ToString())

            Dim actSymbol = DirectCast(model.GetSymbolInfo(actSyntax).Symbol, IMethodSymbol)
            Assert.Equal("IResult(Of System.Int32)", actSymbol.ReturnType.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(21727, "https://github.com/dotnet/roslyn/issues/21727")>
        Public Sub FailedDecodingOfTupleNamesWhenMissingValueTupleType()
            Dim vtLib = CreateEmptyCompilation(s_trivial2uple, references:={MscorlibRef}, assemblyName:="vt")

            Dim libComp = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Imports System.Collections.Generic
Imports System.Collections

Public Class ClassA
    Implements IEnumerable(Of (alice As Integer, bob As Integer))

    Function GetGenericEnumerator() As IEnumerator(Of (alice As Integer, bob As Integer)) Implements IEnumerable(Of (alice As Integer, bob As Integer)).GetEnumerator
        Return Nothing
    End Function

    Function GetEnumerator() As IEnumerator Implements IEnumerable(Of (alice As Integer, bob As Integer)).GetEnumerator
        Return Nothing
    End Function
End Class
                    </file>
                </compilation>, additionalRefs:={vtLib.EmitToImageReference()})
            libComp.VerifyDiagnostics()

            Dim source As Xml.Linq.XElement =
                 <compilation>
                     <file name="a.vb">
Class ClassB
    Sub M()
        Dim x = New ClassA()
        x.ToString()
    End Sub
End Class
                    </file>
                 </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={libComp.EmitToImageReference()}) ' missing reference to vt
            comp.AssertNoDiagnostics()
            FailedDecodingOfTupleNamesWhenMissingValueTupleType_Verify(comp, successfulDecoding:=False)

            Dim compWithMetadataReference = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={libComp.ToMetadataReference()}) ' missing reference to vt

            compWithMetadataReference.AssertNoDiagnostics()
            FailedDecodingOfTupleNamesWhenMissingValueTupleType_Verify(compWithMetadataReference, successfulDecoding:=True)

            Dim fakeVtLib = CreateEmptyCompilation("", references:={MscorlibRef}, assemblyName:="vt")
            Dim compWithFakeVt = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={libComp.EmitToImageReference(), fakeVtLib.EmitToImageReference()}) ' reference to fake vt
            compWithFakeVt.AssertNoDiagnostics()
            FailedDecodingOfTupleNamesWhenMissingValueTupleType_Verify(compWithFakeVt, successfulDecoding:=False)

            Dim source2 As Xml.Linq.XElement =
                 <compilation>
                     <file name="a.vb">
Class ClassB
    Sub M()
        Dim x = New ClassA().GetGenericEnumerator()
        For Each i In New ClassA()
             System.Console.Write(i.alice)
        Next
    End Sub
End Class
                    </file>
                 </compilation>

            Dim comp2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, additionalRefs:={libComp.EmitToImageReference()}) ' missing reference to vt
            comp2.AssertTheseDiagnostics(<errors>
BC30652: Reference required to assembly 'vt, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'ValueTuple(Of ,)'. Add one to your project.
        Dim x = New ClassA().GetGenericEnumerator()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                         </errors>)
            FailedDecodingOfTupleNamesWhenMissingValueTupleType_Verify(comp2, successfulDecoding:=False)

            Dim comp2WithFakeVt = CreateCompilationWithMscorlib40AndVBRuntime(source2, additionalRefs:={libComp.EmitToImageReference(), fakeVtLib.EmitToImageReference()}) ' reference to fake vt
            comp2WithFakeVt.AssertTheseDiagnostics(<errors>
BC31091: Import of type 'ValueTuple(Of ,)' from assembly or module 'vt.dll' failed.
        Dim x = New ClassA().GetGenericEnumerator()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                   </errors>)
            FailedDecodingOfTupleNamesWhenMissingValueTupleType_Verify(comp2WithFakeVt, successfulDecoding:=False)
        End Sub

        Private Sub FailedDecodingOfTupleNamesWhenMissingValueTupleType_Verify(compilation As Compilation, successfulDecoding As Boolean)
            Dim classA = DirectCast(compilation.GetMember("ClassA"), NamedTypeSymbol)
            Dim iEnumerable = classA.Interfaces()(0)

            If successfulDecoding Then
                Assert.Equal("System.Collections.Generic.IEnumerable(Of (alice As System.Int32, bob As System.Int32))",
                    iEnumerable.ToTestDisplayString())

                Dim tuple = iEnumerable.TypeArguments()(0)
                Assert.Equal("(alice As System.Int32, bob As System.Int32)", tuple.ToTestDisplayString())
                Assert.True(tuple.IsTupleType)
                Assert.True(tuple.TupleUnderlyingType.IsErrorType())
            Else
                Assert.Equal("System.Collections.Generic.IEnumerable(Of System.ValueTuple(Of System.Int32, System.Int32)[missing])",
                    iEnumerable.ToTestDisplayString())
                Dim tuple = iEnumerable.TypeArguments()(0)
                Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32)[missing]", tuple.ToTestDisplayString())
                Assert.False(tuple.IsTupleType)
            End If
        End Sub

        <Fact>
        <WorkItem(21727, "https://github.com/dotnet/roslyn/issues/21727")>
        Public Sub FailedDecodingOfTupleNamesWhenMissingContainerType()
            Dim containerLib = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Public Class Container(Of T)
    Public Class Contained(Of U)
    End Class
End Class
                    </file>
                </compilation>)
            containerLib.VerifyDiagnostics()

            Dim libComp = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Imports System.Collections.Generic
Imports System.Collections

Public Class ClassA
    Implements IEnumerable(Of Container(Of (alice As Integer, bob As Integer)).Contained(Of (charlie As Integer, dylan as Integer)))
    
    Function GetGenericEnumerator() As IEnumerator(Of Container(Of (alice As Integer, bob As Integer)).Contained(Of (charlie As Integer, dylan as Integer))) _
        Implements IEnumerable(Of Container(Of (alice As Integer, bob As Integer)).Contained(Of (charlie As Integer, dylan as Integer))).GetEnumerator

        Return Nothing
    End Function

    Function GetEnumerator() As IEnumerator Implements IEnumerable(Of Container(Of (alice As Integer, bob As Integer)).Contained(Of (charlie As Integer, dylan as Integer))).GetEnumerator
        Return Nothing
    End Function
End Class
                    </file>
                </compilation>, additionalRefs:={containerLib.EmitToImageReference(), ValueTupleRef, SystemRuntimeFacadeRef})
            libComp.VerifyDiagnostics()

            Dim source As Xml.Linq.XElement =
                 <compilation>
                     <file name="a.vb">
Class ClassB
    Sub M()
        Dim x = New ClassA()
        x.ToString()
    End Sub
End Class
                    </file>
                 </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source,
                additionalRefs:={libComp.EmitToImageReference(), ValueTupleRef, SystemRuntimeFacadeRef}) ' missing reference to container

            comp.AssertNoDiagnostics()
            FailedDecodingOfTupleNamesWhenMissingContainerType_Verify(comp, decodingSuccessful:=False)

            Dim compWithMetadataReference = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={libComp.ToMetadataReference(), ValueTupleRef, SystemRuntimeFacadeRef}) ' missing reference to container

            compWithMetadataReference.AssertNoDiagnostics()
            FailedDecodingOfTupleNamesWhenMissingContainerType_Verify(compWithMetadataReference, decodingSuccessful:=True)

            Dim fakeContainerLib = CreateEmptyCompilation("", references:={MscorlibRef}, assemblyName:="vt")
            Dim compWithFakeVt = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={libComp.EmitToImageReference(), fakeContainerLib.EmitToImageReference(), ValueTupleRef, SystemRuntimeFacadeRef}) ' reference to fake container
            compWithFakeVt.AssertNoDiagnostics()
            FailedDecodingOfTupleNamesWhenMissingContainerType_Verify(compWithFakeVt, decodingSuccessful:=False)

        End Sub

        Private Sub FailedDecodingOfTupleNamesWhenMissingContainerType_Verify(compilation As Compilation, decodingSuccessful As Boolean)
            Dim classA = DirectCast(compilation.GetMember("ClassA"), NamedTypeSymbol)
            Dim iEnumerable = classA.Interfaces()(0)
            Dim tuple = DirectCast(iEnumerable.TypeArguments()(0), NamedTypeSymbol).TypeArguments()(0)

            If decodingSuccessful Then
                Assert.Equal("System.Collections.Generic.IEnumerable(Of Container(Of (alice As System.Int32, bob As System.Int32))[missing].Contained(Of (charlie As System.Int32, dylan As System.Int32))[missing])", iEnumerable.ToTestDisplayString())
                Assert.Equal("(charlie As System.Int32, dylan As System.Int32)", tuple.ToTestDisplayString())
                Assert.True(tuple.IsTupleType)
                Assert.False(tuple.TupleUnderlyingType.IsErrorType())
            Else
                Assert.Equal("System.Collections.Generic.IEnumerable(Of Container(Of System.ValueTuple(Of System.Int32, System.Int32))[missing].Contained(Of System.ValueTuple(Of System.Int32, System.Int32))[missing])", IEnumerable.ToTestDisplayString())
                Assert.Equal("System.ValueTuple(Of System.Int32, System.Int32)", tuple.ToTestDisplayString())
                Assert.False(tuple.IsTupleType)
            End If
        End Sub
    End Class

End Namespace


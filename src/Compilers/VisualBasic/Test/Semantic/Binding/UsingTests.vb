' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class UsingTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub UsingSimpleWithSingleAsNewDeclarations()
            Dim source =
<compilation name="UsingSimpleWithSingleAsNewDeclarations">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo As New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (MyDisposable V_0) //foo
  IL_0000:  newobj     "Sub MyDisposable..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldstr      "Inside Using."
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  leave.s    IL_001c
}
  finally
{
  IL_0012:  ldloc.0
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_001b:  endfinally
}
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithAsNewDeclarations()
            Dim source =
<compilation name="UsingSimpleWithAsNewDeclarations">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo As New MyDisposable(), foo2 as New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (MyDisposable V_0, //foo
  MyDisposable V_1) //foo2
  IL_0000:  newobj     "Sub MyDisposable..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  newobj     "Sub MyDisposable..ctor()"
  IL_000b:  stloc.1
  .try
{
  IL_000c:  ldstr      "Inside Using."
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  leave.s    IL_002c
}
  finally
{
  IL_0018:  ldloc.1
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.1
  IL_001c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0021:  endfinally
}
}
  finally
{
  IL_0022:  ldloc.0
  IL_0023:  brfalse.s  IL_002b
  IL_0025:  ldloc.0
  IL_0026:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_002b:  endfinally
}
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithMultipleVariablesInAsNewDeclarations()
            Dim source =
<compilation name="UsingSimpleWithMultipleVariablesInAsNewDeclarations">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo, foo2 As New MyDisposable(), foo3, foo4 As New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  1
  .locals init (MyDisposable V_0, //foo
  MyDisposable V_1, //foo2
  MyDisposable V_2, //foo3
  MyDisposable V_3) //foo4
  IL_0000:  newobj     "Sub MyDisposable..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  newobj     "Sub MyDisposable..ctor()"
  IL_000b:  stloc.1
  .try
{
  IL_000c:  newobj     "Sub MyDisposable..ctor()"
  IL_0011:  stloc.2
  .try
{
  IL_0012:  newobj     "Sub MyDisposable..ctor()"
  IL_0017:  stloc.3
  .try
{
  IL_0018:  ldstr      "Inside Using."
  IL_001d:  call       "Sub System.Console.WriteLine(String)"
  IL_0022:  leave.s    IL_004c
}
  finally
{
  IL_0024:  ldloc.3
  IL_0025:  brfalse.s  IL_002d
  IL_0027:  ldloc.3
  IL_0028:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_002d:  endfinally
}
}
  finally
{
  IL_002e:  ldloc.2
  IL_002f:  brfalse.s  IL_0037
  IL_0031:  ldloc.2
  IL_0032:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0037:  endfinally
}
}
  finally
{
  IL_0038:  ldloc.1
  IL_0039:  brfalse.s  IL_0041
  IL_003b:  ldloc.1
  IL_003c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0041:  endfinally
}
}
  finally
{
  IL_0042:  ldloc.0
  IL_0043:  brfalse.s  IL_004b
  IL_0045:  ldloc.0
  IL_0046:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_004b:  endfinally
}
  IL_004c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithAsDeclarations()
            Dim source =
<compilation name="UsingSimpleWithAsDeclarations">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo As MyDisposable = new MyDisposable(), foo2 as MyDisposable = New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (MyDisposable V_0, //foo
  MyDisposable V_1) //foo2
  IL_0000:  newobj     "Sub MyDisposable..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  newobj     "Sub MyDisposable..ctor()"
  IL_000b:  stloc.1
  .try
{
  IL_000c:  ldstr      "Inside Using."
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  leave.s    IL_002c
}
  finally
{
  IL_0018:  ldloc.1
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.1
  IL_001c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0021:  endfinally
}
}
  finally
{
  IL_0022:  ldloc.0
  IL_0023:  brfalse.s  IL_002b
  IL_0025:  ldloc.0
  IL_0026:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_002b:  endfinally
}
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithExpression()
            Dim source =
<compilation name="UsingSimpleWithExpression">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using new MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (MyDisposable V_0)
  IL_0000:  newobj     "Sub MyDisposable..ctor()"
  IL_0005:  stloc.0
  .try
  {
    IL_0006:  ldstr      "Inside Using."
    IL_000b:  call       "Sub System.Console.WriteLine(String)"
    IL_0010:  leave.s    IL_001c
  }
  finally
  {
    IL_0012:  ldloc.0
    IL_0013:  brfalse.s  IL_001b
    IL_0015:  ldloc.0
    IL_0016:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_001b:  endfinally
  }
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithValueTypeExpression()
            Dim source =
<compilation name="UsingSimpleWithValueTypeExpression">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Structure MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class C1
    Public Shared Sub Main()
        Using new MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (MyDisposable V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "MyDisposable"
  .try
  {
    IL_0008:  ldstr      "Inside Using."
    IL_000d:  call       "Sub System.Console.WriteLine(String)"
    IL_0012:  leave.s    IL_0022
  }
  finally
  {
    IL_0014:  ldloca.s   V_0
    IL_0016:  constrained. "MyDisposable"
    IL_001c:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0021:  endfinally
  }
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithPropertyAccessExpression()
            Dim source =
<compilation name="UsingSimpleWithPropertyAccessExpression">
    <file name="a.vb">
Option Strict On
Option Infer On
Option Explicit On

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Readonly Property ADisposable as IDisposable
        Get
            return new MyDisposable()
        End Get
    End Property

    Public Sub DoStuff()
        Using ADisposable
            Console.WriteLine("Inside Using.")
        End Using
    End Sub

    Public Shared Sub Main()
        dim x = new C1()
        x.DoStuff
    End Sub

End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                           expectedOutput:=<![CDATA[
Inside Using.
]]>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithPropertyAccessInitialization()
            Dim source =
<compilation name="UsingSimpleWithPropertyAccessInitialization">
    <file name="a.vb">
Option Strict On
Option Infer On
Option Explicit On

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Readonly Property ADisposable as IDisposable
        Get
            return new MyDisposable()
        End Get
    End Property

    Public Sub DoStuff()
        Using foo As IDisposable = ADisposable
            Console.WriteLine("Inside Using.")
        End Using
    End Sub

    Public Shared Sub Main()
        dim x = new C1()
        x.DoStuff
    End Sub

End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                           expectedOutput:=<![CDATA[
Inside Using.
]]>)
        End Sub

        <Fact()>
        Public Sub UsingHideFieldsAndProperties()
            Dim source =
<compilation name="UsingHideFieldsAndProperties">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Field as MyDisposable

    Public Property MyProperty as MyDisposable

    Public Shared Sub Main()
        Using field as New MyDisposable(), MyProperty as New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                           expectedOutput:=<![CDATA[
Inside Using.
]]>)
        End Sub

        <Fact()>
        Public Sub UsingCannotHideLocalsParametersAndTypeParameters()
            Dim source =
<compilation name="UsingCannotHideLocalsParametersAndTypeParameters">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Field as MyDisposable

    Public Property MyProperty as MyDisposable

    Public Shared Sub Main()
        Dim local as Object = nothing

        Using local as New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using

        Using usingLocal as New MyDisposable()
            Dim usingLocal as New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using

    End Sub

    Public Sub DoStuff(Of P)(param as P)
        Using p as New MyDisposable(), param as new MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub

End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30616: Variable 'local' hides a variable in an enclosing block.
        Using local as New MyDisposable()
              ~~~~~
BC30616: Variable 'usingLocal' hides a variable in an enclosing block.
            Dim usingLocal as New MyDisposable()
                ~~~~~~~~~~
BC32089: 'p' is already declared as a type parameter of this method.
        Using p as New MyDisposable(), param as new MyDisposable()
              ~
BC30734: 'param' is already declared as a parameter of this method.
        Using p as New MyDisposable(), param as new MyDisposable()
                                       ~~~~~                                               
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithInferredType()
            Dim source =
<compilation name="UsingSimpleWithInferredType">
    <file name="a.vb">
Option Strict On
Option Infer On
Option Explicit On

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo = New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using

        Using foo2 = New Integer()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC36010: 'Using' operand of type 'Integer' must implement 'System.IDisposable'.
        Using foo2 = New Integer()
              ~~~~~~~~~~~~~~~~~~~~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub UsingSimpleWithImplicitType()
            Dim source =
<compilation name="UsingSimpleWithImplicitType">
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        foo = New MyDisposable() ' not type inference means this is an object
        Using foo 
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC36010: 'Using' operand of type 'Object' must implement 'System.IDisposable'.
        Using foo 
              ~~~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub UsingMultipleNullableVariablesInAsNew()
            Dim source =
<compilation name="UsingMultipleNullableVariablesInAsNew">
    <file name="a.vb">
Option Strict Off
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using a?, b? as new MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC33101: Type 'MyDisposable' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Using a?, b? as new MyDisposable()
              ~~
BC33101: Type 'MyDisposable' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
        Using a?, b? as new MyDisposable()
                  ~~
BC33109: Nullable modifier cannot be specified in variable declarations with 'As New'.
        Using a?, b? as new MyDisposable()
                     ~~~~~~~~~~~~~~~~~~~~~
BC33109: Nullable modifier cannot be specified in variable declarations with 'As New'.
        Using a?, b? as new MyDisposable()
                     ~~~~~~~~~~~~~~~~~~~~~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub UsingMultipleVariablesInAsNewWithMutableStructType()
            Dim source =
<compilation name="UsingMultipleVariablesInAsNewWithMutableStructType">
    <file name="a.vb">
Option Strict Off
Option Infer Off
Option Explicit Off

Imports System

Structure MyDisposable
    Implements IDisposable

    Public Foo As Integer

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class C1
    Public Shared Sub Main()
        Using a, b as new MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42351: Local variable 'a' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
        Using a, b as new MyDisposable()
              ~
BC42351: Local variable 'b' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
        Using a, b as new MyDisposable()
                 ~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub EmptyUsing()
            Dim source =
<compilation name="EmptyUsing">
    <file name="a.vb">
Imports System

Class C1
    Public Shared Sub Main()
        Using 
        End Using
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30201: Expression expected.
        Using 
              ~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub UsingWithLocalInferenceAndCrossReference()
            Dim source =
<compilation name="UsingWithLocalInferenceAndCrossReference">
    <file name="a.vb">
Option Strict On
Option Infer On
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Identity as String 

    Public Readonly Other as MyDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Disposing " + Identity)
    End Sub

    Public Sub New(id as String)
        Identity = id

        Other = new MyDisposable()
        Other.Identity = "Other"
    End Sub

    Public Sub New()
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo = New MyDisposable("foo"), foo2 = foo.Other
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
Disposing Other
Disposing foo
]]>)
        End Sub

        <Fact()>
        Public Sub UsingWithLocalInferenceAndCrossReferenceInvalid()
            Dim source =
<compilation name="UsingWithLocalInferenceAndCrossReferenceInvalid">
    <file name="a.vb">
Option Strict On
Option Infer On
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Identity as String 

    Public Readonly Other as MyDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Disposing " + Identity)
    End Sub

    Public Sub New(id as String)
        Identity = id

        Other = new MyDisposable()
        Other.Identity = "Other"
    End Sub

    Public Sub New()
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using beforefoo = foo, foo = New MyDisposable("foo"), foo2 = foo.Other
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC32000: Local variable 'foo' cannot be referred to before it is declared.
        Using beforefoo = foo, foo = New MyDisposable("foo"), foo2 = foo.Other
                          ~~~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub UsingResourceIsNothingLiteralNotOptimizedInVB()
            Dim source =
<compilation name="UsingResourceIsNothingLiteralNotOptimizedInVB">
    <file name="a.vb">
Option Strict On
Option Infer On
Option Explicit Off

Imports System

Class C1
    Public Shared Sub Main()
        Using foo As IDisposable = nothing
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Inside Using.
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (System.IDisposable V_0) //foo
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  ldstr      "Inside Using."
  IL_0007:  call       "Sub System.Console.WriteLine(String)"
  IL_000c:  leave.s    IL_0018
}
  finally
{
  IL_000e:  ldloc.0
  IL_000f:  brfalse.s  IL_0017
  IL_0011:  ldloc.0
  IL_0012:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0017:  endfinally
}
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub BC30610ERR_BaseOnlyClassesMustBeExplicit2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BaseOnlyClassesMustBeExplicit2">
        <file name="a.vb">
           Imports System
Class M1
    Implements IDisposable
    Sub DISPOSE() Implements IDisposable.Dispose
    End Sub
    Event MyEvent()
    Sub Fun()
        Using MyClass ' using Myclass
        End Using

        Using MyBase ' using MyBase
        End Using

        Using Me
        End Using

        Using System.Exception ' using Type
        End Using

        Using M1 ' using class
        End Using

        Using E1
        End Using

        Using Main() ' using result of sub
        End Using

        Using AddressOf main ' using result of AddressOf
        End Using

        Using C1 ' using constant
        End Using
    End Sub
    Shared Sub Main()
    End Sub
End Class
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
                        Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                        Diagnostic(ERRID.ERR_ClassNotExpression1, "System.Exception").WithArguments("System.Exception"),
                        Diagnostic(ERRID.ERR_ClassNotExpression1, "M1").WithArguments("M1"),
                        Diagnostic(ERRID.ERR_NameNotDeclared1, "E1").WithArguments("E1"),
                        Diagnostic(ERRID.ERR_VoidValue, "Main()"),
                        Diagnostic(ERRID.ERR_VoidValue, "AddressOf main"),
                        Diagnostic(ERRID.ERR_NameNotDeclared1, "C1").WithArguments("C1"))
        End Sub

        ' Take the parameter as object
        <Fact()>
        Public Sub BC30610ERR_BaseOnlyClassesMustBeExplicit2_TypeParameter()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BaseOnlyClassesMustBeExplicit2">
        <file name="a.vb">
            Class Gen(Of T)
                Public Shared Sub TestUsing(obj As T)
                    Using obj
                    End Using
                End Sub
            End Class
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "obj").WithArguments("T"))
        End Sub

        ' Using block must implement the IDisposable interface
        <Fact()>
        Public Sub BC30610ERR_BaseOnlyClassesMustBeExplicit2_Invalid()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BaseOnlyClassesMustBeExplicit2">
        <file name="a.vb">
            Option Infer On
Option Strict Off

Class MyManagedClass1
    Sub Dispose()
    End Sub
End Class
Class C1
    Shared Sub main()
        Using mnObj = New MyManagedClass1() ' Invalid
        End Using
        Using mnObj1 As String = "123" ' Invalid
        End Using
    End Sub
End Class
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "mnObj = New MyManagedClass1()").WithArguments("MyManagedClass1"),
                Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "mnObj1 As String = ""123""").WithArguments("String"))
        End Sub

        ' Nullable type as resource
        <Fact()>
        Public Sub BC30610ERR_BaseOnlyClassesMustBeExplicit2_Invalid_Nullable()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BaseOnlyClassesMustBeExplicit2">
        <file name="a.vb">
Option Infer On
Option Strict Off
Imports System
Class C1
    Shared mnObj As Nullable(Of MyManagedClass)
    Shared Sub main()
        Using mnObj ' Invalid
        End Using
        Using mnObj1 As Nullable(Of MyManagedClass) = Nothing ' Invalid
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements IDisposable
    Sub dispose() Implements IDisposable.Dispose
    End Sub
End Structure
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1,
                              Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "mnObj").WithArguments("MyManagedClass?"),
                              Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "mnObj1 As Nullable(Of MyManagedClass) = Nothing").WithArguments("MyManagedClass?"),
                              Diagnostic(ERRID.WRN_MutableStructureInUsing, "mnObj1 As Nullable(Of MyManagedClass) = Nothing").WithArguments("mnObj1"))
        End Sub

        ' Using same named variables in different blocks
        <Fact()>
        Public Sub UsableScope()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="UsableScope">
        <file name="a.vb">
Imports System            
Structure cls1
    Implements IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
    Sub New(ByRef x As cls1)
        x = Me
    End Sub
    Sub foo()
        Do
            Dim x As New cls1
            Using x
            End Using
            Exit Do
        Loop
        For i As Integer = 1 To 1
            Dim x As New cls1
            Using x
            End Using
        Next
        While True
            Dim x As New cls1
            Using x
            End Using
            Exit While
        End While
    End Sub
    Shared Sub Main()
        Dim x = New cls1()
        x.foo()
    End Sub
End Structure
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub LateBind()
            CompileAndVerify(
    <compilation name="LateBind">
        <file name="a.vb">
Option Infer On
Option Strict Off
Imports System
Class cls1
    Implements IDisposable
    Public disposed As Boolean = False
    Public Sub Dispose() Implements System.IDisposable.Dispose
        disposed = True
    End Sub
    Public Function GetBoxedInstance() As Object
        Return Me
    End Function
End Class
Class C1
    Sub Main()
        Dim o1 As New cls1
        Using o1.GetBoxedInstance
        End Using
        Dim o2 As Object = New cls1
        foo(o2)
    End Sub
    Sub foo(ByVal o As Object)
        Using o.GetBoxedInstance
        End Using
    End Sub
End Class
        </file>
    </compilation>)
        End Sub

        ' The incorrect control flow (jumps outter to inner)
        <Fact()>
        Public Sub IncorrectJump()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="IncorrectJump">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim obj = New s1()
        Using obj
            Using obj
                GoTo label4
label5:
            End Using
label4:
            GoTo label5 ' Invalid jumps from inner block into outer one (ok) and back(err)
        End Using
    End Sub
End Module
Structure s1
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
        </file>
    </compilation>)

            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_GotoIntoUsing, "label5").WithArguments("label5"))
        End Sub

        ' Assigning stuff to the Using variable
        <Fact()>
        Public Sub Assignment()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Assignment">
        <file name="a.vb">
Imports System
Module Program
    Dim mnObj As MyManagedClass
    Sub FOO()
        Dim obj1 As New MyManagedClass
        Dim obj1a As New MyManagedClass
        Dim obj1b As MyManagedClass = obj1
        Using obj1b
            obj1b = obj1a
        End Using
        Dim obj2 As New MyManagedClass
        Using obj2
            obj2 = obj2
        End Using
    End Sub

    Sub Main(args As String())
    End Sub
End Module
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
        </file>
    </compilation>)

            VerifyDiagnostics(compilation1)
        End Sub

        ' Assigning stuff to the Using variable
        <Fact()>
        Public Sub Assignment_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Assignment">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim obj1 As New MyManagedClass
        Dim obj1a As New MyManagedClass
        Dim obj1b As MyManagedClass = obj1
        Using obj1b
            obj1a = obj1b
        End Using
    End Sub
End Module
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
        </file>
    </compilation>)

            VerifyDiagnostics(compilation1)
        End Sub

        <WorkItem(543059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543059")>
        <Fact()>
        Public Sub MultipleResource()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Using x, = New MyManagedClass, y = New MyManagedClass1

        End Using
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
Class MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose1")
    End Sub
End Class
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC36011: 'Using' resource variable must have an explicit initialization.
        Using x, = New MyManagedClass, y = New MyManagedClass1
              ~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
        Using x, = New MyManagedClass, y = New MyManagedClass1
              ~~~~~~~~~~~~~~~~~~~~~~~
BC30203: Identifier expected.
        Using x, = New MyManagedClass, y = New MyManagedClass1
                 ~
</expected>)
        End Sub

        <WorkItem(543059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543059")>
        <Fact()>
        Public Sub MultipleResource_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Imports System.Collections.Generic

Class Program
    Shared Sub Main()
        Dim objs = GetList()
        Using x As MyManagedClass = (From y In objs Select y).First, foo3, foo4 = x
        End Using
    End Sub
    Shared Function GetList() As List(Of MyManagedClass)
        Return Nothing
    End Function
End Class

Public Class MyManagedClass
    Implements System.IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Class
        </file>
    </compilation>)

            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_ExpectedQueryableSource, "objs").WithArguments("System.Collections.Generic.List(Of MyManagedClass)"),
            Diagnostic(ERRID.ERR_InitWithMultipleDeclarators, "foo3, foo4 = x"),
            Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "foo3"),
            Diagnostic(ERRID.ERR_UsingResourceVarNeedsInitializer, "foo3"),
            Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "foo3").WithArguments("Object"))
        End Sub

        <WorkItem(528963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528963")>
        <Fact()>
        Public Sub InitWithMultipleDeclarators()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="InitWithMultipleDeclarators">
        <file name="a.vb">
            Option Infer On
Option Strict On
Imports System
Imports System.Collections.Generic

Class Program
    Shared Sub Main()
        Dim objs = GetList()
        Using foo As MyManagedClass = New MyManagedClass(), foo3, foo4 As New MyManagedClass()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
    Shared Function GetList() As List(Of MyManagedClass)
        Return Nothing
    End Function
End Class

Public Class MyManagedClass
    Implements System.IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Class
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1)
        End Sub

        ' Query expression in using statement
        <Fact()>
        Public Sub QueryInUsing()

            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
    <compilation name="QueryInUsing">
        <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
            Shared Sub Main()
                Dim objs = GetList()
                Using x As MyManagedClass = (From y In objs Select y).First
                End Using
            End Sub
            Shared Function GetList() As List(Of MyManagedClass)
                Return Nothing
            End Function
        End Class

        Public Class MyManagedClass
            Implements System.IDisposable
            Public Sub Dispose() Implements System.IDisposable.Dispose
                Console.Write("Dispose")
            End Sub
        End Class
        </file>
    </compilation>, {SystemCoreRef})
            VerifyDiagnostics(compilation1)
        End Sub

        ' Error when using a lambda in a using()
        <Fact()>
        Public Sub LambdaInUsing()

            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
    <compilation name="LambdaInUsing">
        <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Using Function(x) x
            ' err
        End Using
        Using Function():End Function
            ' err
        End Using

        Using Function(x As MyManagedClass) x
            ' err
        End Using
    End Sub
End Class

Public Class MyManagedClass
    Implements System.IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Class
        </file>
    </compilation>, {SystemCoreRef})
            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_ExpectedExpression, ""),
    Diagnostic(ERRID.ERR_InvalidEndFunction, "End Function"),
    Diagnostic(ERRID.ERR_StrictDisallowImplicitObjectLambda, "x"),
    Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "Function(x) x").WithArguments("Function <generated method>(x As Object) As Object"),
    Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "Function()").WithArguments("Function <generated method>() As ?"),
    Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "Function(x As MyManagedClass) x").WithArguments("Function <generated method>(x As MyManagedClass) As MyManagedClass"))
        End Sub

        ' Anonymous types cannot appear in using
        <Fact()>
        Public Sub AnonymousInUsing()
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
    <compilation name="AnonymousInUsing">
        <file name="a.vb">
Option Infer On
Option Strict On
Class Program
    Shared Sub Main()
        Using c = New With {Key.p1 = 10.0, Key.p2 = "a"c}
        End Using
    End Sub
End Class
        </file>
    </compilation>)
            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "c = New With {Key.p1 = 10.0, Key.p2 = ""a""c}").WithArguments("<anonymous type: Key p1 As Double, Key p2 As Char>"))
        End Sub

        'Anonymous Delegate in using block
        <WorkItem(528974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528974")>
        <Fact()>
        Public Sub AnonymousDelegateInUsing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
            <compilation name="AnonymousDelegateInUsing">
                <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Delegate Function D1(Of T)(t As T) As T
Class A1
    Private Shared Sub Foo(Of T As IDisposable)(x As T)
        Dim local As T = x
        Using t1 As T = DirectCast(Function(tt As T) x, D1(Of T))(x) ' warning
        End Using
    End Sub
    Shared Sub Main()
    End Sub
End Class
                </file>
            </compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.WRN_MutableGenericStructureInUsing, "t1 As T = DirectCast(Function(tt As T) x, D1(Of T))(x)").WithArguments("t1"))
        End Sub

        ' Using used before calling Mybase.New
        <WorkItem(10570, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub UsingBeforeConstructCall()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="UsingBeforeConstructCall">
    <file name="a.vb">
Imports System        
Class cls1
    Implements IDisposable
    Sub New()
        Using x as IDisposable = nothing
            MyBase.New()
        End Using
    End Sub
    Public Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Class
Class cls2
    Implements IDisposable
    Sub New()
        Using Me
            MyBase.New()
        End Using
    End Sub
    Public Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Class
Class cls3
    Implements IDisposable
    Sub New()
        Using MyBase.New()
        End Using
    End Sub
    Public Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(compilation,
                Diagnostic(ERRID.ERR_InvalidConstructorCall, "MyBase.New"),
                Diagnostic(ERRID.ERR_InvalidConstructorCall, "MyBase.New"),
                Diagnostic(ERRID.ERR_InvalidConstructorCall, "MyBase.New"))

        End Sub

        <WorkItem(528975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528975")>
        <Fact>
        Public Sub InitMultipleResourceWithUsingDecl()
            CompileAndVerify(
<compilation name="InitResourceWithUsingDecl">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim x = 1
        Using foo, foo2 As New MyManagedClass(x), foo3, foo4 As New MyManagedClass(x)
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub New(x As Integer)
    End Sub
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>)
        End Sub

        <WorkItem(543059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543059")>
        <Fact()>
        Public Sub MultipleResource_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Infer On
Class Program
    Shared Sub Main()
        Using x, = New MyManagedClass, y = New MyManagedClass1

        End Using
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
Class MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose1")
    End Sub
End Class
        </file>
    </compilation>)

            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                                            Diagnostic(ERRID.ERR_InitWithMultipleDeclarators, "x, = New MyManagedClass"),
                                            Diagnostic(ERRID.ERR_UsingResourceVarNeedsInitializer, "x"))
        End Sub

        <WorkItem(543059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543059")>
        <Fact()>
        Public Sub MultipleResource_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Imports System.Collections.Generic

Class Program
    Shared Sub Main()
        Dim objs = GetList()
        Using x As MyManagedClass = (From y In objs Select y).First, foo3, foo4 = x
        End Using
    End Sub
    Shared Function GetList() As List(Of MyManagedClass)
        Return Nothing
    End Function
End Class

Public Class MyManagedClass
    Implements System.IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
End Class
        </file>
    </compilation>)

            VerifyDiagnostics(compilation1, Diagnostic(ERRID.ERR_ExpectedQueryableSource, "objs").WithArguments("System.Collections.Generic.List(Of MyManagedClass)"),
                                            Diagnostic(ERRID.ERR_InitWithMultipleDeclarators, "foo3, foo4 = x"),
                                            Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "foo3"),
                                            Diagnostic(ERRID.ERR_UsingResourceVarNeedsInitializer, "foo3"),
                                            Diagnostic(ERRID.ERR_UsingRequiresDisposePattern, "foo3").WithArguments("Object"))
        End Sub

        <WorkItem(529046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529046")>
        <Fact>
        Public Sub UsingOutOfMethod()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockOutOfMethod">
    <file name="a.vb">
Class m1
    Using Nothing
    End Using
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Using Nothing"),
                                Diagnostic(ERRID.ERR_EndUsingWithoutUsing, "End Using"))
        End Sub

        <WorkItem(529046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529046")>
        <Fact>
        Public Sub UsingOutOfMethod_1()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockOutOfMethod">
    <file name="a.vb">
    Using Nothing
    End Using
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Using Nothing"),
                                Diagnostic(ERRID.ERR_EndUsingWithoutUsing, "End Using"))
        End Sub
    End Class
End Namespace

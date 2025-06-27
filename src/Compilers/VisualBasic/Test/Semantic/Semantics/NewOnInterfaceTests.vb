' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class NewOnInterfaceTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub NewOnRegularInterface_SimpleError()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System

Public Interface IInterface
End Interface

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30375: 'New' cannot be used on an interface.
        Dim i = New IInterface()
                ~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_NewOperator()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_AsNewOnLocal()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
End Class

Module M
    Sub Main(args() As String)
        Dim i As New IInterface()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_AsNewOnPropertyAndField()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
End Class

Class Clazz
    Sub Main(args() As String)
    End Sub

    Public instanceField As New IInterface()
    Public Property instanceProperty As New IInterface()

    Public staticField As New IInterface()
    Public Property staticProperty As New IInterface()

End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_NoDefaultConstructorError()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
    Public Sub New(i As Integer)
    End Sub
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30455: Argument not specified for parameter 'i' of 'Public Sub New(i As Integer)'.
        Dim i = New IInterface()
                    ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_NonDefaultConstructor()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
    Public Sub New(i As Integer)
    End Sub
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface(1)
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_ErrorForArrayCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation()(,,)))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
    Public Sub New(i As Integer)
    End Sub
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface(1)
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31450: Type 'CoClassImplementation()(*,*,*)' cannot be used as an implementing class.
        Dim i = New IInterface(1)
                ~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_ModuleCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Module CoClassImplementation
End Module 

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30517: Overload resolution failed because no 'New' is accessible.
        Dim i = New IInterface()
                    ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_EnumCoClass()
            ' WARNING: Roslyn detects the default parameterless 
            ' WARNING: constructor, while Dev11 does not
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Enum CoClassImplementation
    Dummy
End Enum

Module M
    Sub Main(args() As String)
        Dim i1 = New IInterface()
        Dim i2 = New IInterface(1)
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30311: Value of type 'CoClassImplementation' cannot be converted to 'IInterface'.
        Dim i1 = New IInterface()
                 ~~~~~~~~~~~~~~~~
BC30057: Too many arguments to 'Public Sub New()'.
        Dim i2 = New IInterface(1)
                                ~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_StrictCoClass()
            ' WARNING: Roslyn detects the default parameterless 
            ' WARNING: constructor, while Dev11 does not
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Structure CoClassImplementation
    Public Sub New(i As Integer)
    End Sub
End Structure

Module M
    Sub Main(args() As String)
        Dim i1 = New IInterface()
        Dim i2 = New IInterface(1)
        Dim i3 = New IInterface(1, 2)
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30311: Value of type 'CoClassImplementation' cannot be converted to 'IInterface'.
        Dim i1 = New IInterface()
                 ~~~~~~~~~~~~~~~~
BC30311: Value of type 'CoClassImplementation' cannot be converted to 'IInterface'.
        Dim i2 = New IInterface(1)
                 ~~~~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
        Dim i3 = New IInterface(1, 2)
                     ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_InterfaceCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Interface CoClassImplementation
End Interface

Module M
    Sub Main(args() As String)
        Dim i1 = New IInterface()
        Dim i2 = New IInterface(1)
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31094: Implementing class 'CoClassImplementation' for interface 'IInterface' cannot be found.
        Dim i1 = New IInterface()
                 ~~~~~~~~~~~~~~~~
BC31094: Implementing class 'CoClassImplementation' for interface 'IInterface' cannot be found.
        Dim i2 = New IInterface(1)
                 ~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_AbstractClassCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public MustInherit Class CoClassImplementation
End Class 

Module M
    Sub Main(args() As String)
        Dim i1 = New IInterface()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31450: Type 'CoClassImplementation' cannot be used as an implementing class.
        Dim i1 = New IInterface()
                 ~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_GenericInterfaces()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(Complicated))>
Public Interface IComplicated(Of T)
End Interface

Public Class Complicated
    Public Sub New(i As Integer)
    End Sub
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IComplicated(Of Integer)(1)
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_InterfacesInGenericType()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New B(Of Integer).IComplicated()
        Dim j = New B(Of Integer).IComplicated2(1)
    End Sub
End Module


Public Class B(Of T)

    <CoClass(GetType(D))>
    Public Interface IComplicated
    End Interface

    <CoClass(GetType(Date))>
    Public Interface IComplicated2
    End Interface

    Public Class C(Of X)
    End Class

    Public Class D
        Inherits C(Of Integer)
    End Class
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30375: 'New' cannot be used on an interface.
        Dim i = New B(Of Integer).IComplicated()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type 'Date' cannot be converted to 'B(Of Integer).IComplicated2'.
        Dim j = New B(Of Integer).IComplicated2(1)
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32079: Type parameters or types constructed with type parameters are not allowed in attribute arguments.
    &lt;CoClass(GetType(D))&gt;
                     ~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_DelegateCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New IComplicated(Nothing, Nothing)
    End Sub
End Module

Public Delegate Sub S(i As Integer)

<CoClass(GetType(S))>
Public Interface IComplicated
End Interface
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_WrongConstructor_BreakingChange()
            Dim reference = CreateReferenceFromIlCode(<![CDATA[
.class public sequential ansi sealed beforefieldinit StructWithOptional
       extends [mscorlib]System.ValueType
{
    .pack 0
    .size 1
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor([opt] int32 a) cil managed
    {
      .param [1] = int32(0x00000000)
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "public StructWithOptional(int a = 0)"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method StructWithOptional::.ctor
} // end of class StructWithOptional
]]>.Value)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main(args() As String)
        Dim i = New IComplicated()
    End Sub
End Module

<CoClass(GetType(StructWithOptional))>
Public Interface IComplicated
End Interface
]]>
                        </file>
                    </compilation>, references:={reference}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30311: Value of type 'StructWithOptional' cannot be converted to 'IComplicated'.
        Dim i = New IComplicated()
                ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_WrongConstructor_BreakingChange_2()
            Dim reference = CreateReferenceFromIlCode(<![CDATA[
.class public sequential ansi sealed beforefieldinit StructWithOptional
       extends [mscorlib]System.ValueType
       implements IComplicated
{
    .pack 0
    .size 1
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor([opt] int32 a) cil managed
    {
      .param [1] = int32(0x00000000)
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "public StructWithOptional(int a = 0)"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method StructWithOptional::.ctor
} // end of class StructWithOptional

.class interface public abstract auto ansi IComplicated
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type)
           = {type(StructWithOptional)}
} // end of class IComplicated

]]>.Value)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main(args() As String)
        Dim i = New IComplicated()
    End Sub
End Module
]]>
                        </file>
                    </compilation>, references:={reference}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <WorkItem(546682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546682")>
        <Fact()>
        Public Sub NewOnCoClassInterface_16543_StrictOn()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(C1))>
Interface I1
End Interface

Class C1
End Class

Module EmailHelpers
    Public Sub Main(args() As String)
    End Sub
    Public Sub SaveHtmlAsMht()
l1:
        Dim msg1 As New I1()
l2:
        Dim msg2 = New I1()
l3:
        Dim msg3 As I1 = New I1()
l4:
        Dim o As Object
        o = msg1
        o = msg2
        o = msg3
    End Sub
End Module
]]>
                        </file>
                    </compilation>, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30512: Option Strict On disallows implicit conversions from 'C1' to 'I1'.
        Dim msg1 As New I1()
                    ~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'C1' to 'I1'.
        Dim msg2 = New I1()
                   ~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'C1' to 'I1'.
        Dim msg3 As I1 = New I1()
                         ~~~~~~~~
</errors>)
        End Sub

        <WorkItem(546682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546682")>
        <Fact()>
        Public Sub NewOnCoClassInterface_16543_StrictOff()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(C1))>
Interface I1
End Interface

Class C1
End Class

Module EmailHelpers
    Public Sub Main(args() As String)
    End Sub
    Public Sub SaveHtmlAsMht()
l1:
        Dim msg1 As New I1()
l2:
        Dim msg2 = New I1()
l3:
        Dim msg3 As I1 = New I1()
l4:
        Dim o As Object
        o = msg1
        o = msg2
        o = msg3
    End Sub
End Module
]]>
                        </file>
                    </compilation>, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)

            CompileAndVerify(compilation, expectedOutput:="").VerifyIL("EmailHelpers.SaveHtmlAsMht", <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (I1 V_0, //msg1
  I1 V_1, //msg2
  I1 V_2) //msg3
  IL_0000:  newobj     "Sub C1..ctor()"
  IL_0005:  castclass  "I1"
  IL_000a:  stloc.0
  IL_000b:  newobj     "Sub C1..ctor()"
  IL_0010:  castclass  "I1"
  IL_0015:  stloc.1
  IL_0016:  newobj     "Sub C1..ctor()"
  IL_001b:  castclass  "I1"
  IL_0020:  stloc.2
  IL_0021:  ldloc.0
  IL_0022:  pop
  IL_0023:  ldloc.1
  IL_0024:  pop
  IL_0025:  ldloc.2
  IL_0026:  pop
  IL_0027:  ret
}
]]>)
        End Sub

        <WorkItem(546595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546595")>
        <Fact()>
        Public Sub NewOnCoClassInterface_UnboundGenericFromMetadata()
            Dim reference = CreateReferenceFromIlCode(<![CDATA[
.class interface public abstract import I
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = {type(A)}
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = 
        ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
}
.class public A<T> implements I
{
  .method public hidebysig specialname rtspecialname instance void .ctor() 
  { 
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}
]]>.Value)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Private F As Object = New I()
    Public Shared Sub Main(args() As String)
    End Sub
End Class
]]>
                        </file>
                    </compilation>, references:={reference}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31450: Type 'A(Of )' cannot be used as an implementing class.
    Private F As Object = New I()
                          ~~~~~~~
</errors>)
        End Sub

        <WorkItem(546595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546595")>
        <Fact()>
        Public Sub NewOnCoClassInterface_UnboundGenericFromMetadata2()
            Dim reference = CreateReferenceFromIlCode(<![CDATA[
.class interface public abstract import I
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = 
  {string('A`1+B`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089]]')} 

  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = 
        ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
}
.class public auto ansi beforefieldinit A`1<T>
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit B`1<T,U>
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method B`1::.ctor

  } // end of class B`1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`1::.ctor

} // end of class A`1
]]>.Value)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Private F As Object = New I()
    Public Shared Sub Main(args() As String)
    End Sub
End Class
]]>
                        </file>
                    </compilation>, references:={reference}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <WorkItem(546595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546595")>
        <Fact()>
        Public Sub NewOnCoClassInterface_UnboundGenericFromMetadata3()
            Dim reference = CreateReferenceFromIlCode(<![CDATA[
.class interface public abstract import I
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = {string('A`1+B`1')} 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = 
        ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
}
.class public auto ansi beforefieldinit A`1<T>
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit B`1<T,U>
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method B`1::.ctor

  } // end of class B`1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`1::.ctor

} // end of class A`1
]]>.Value)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Private F As Object = New I()
    Public Shared Sub Main(args() As String)
    End Sub
End Class
]]>
                        </file>
                    </compilation>, references:={reference}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31450: Type 'A(Of ).B(Of )' cannot be used as an implementing class.
    Private F As Object = New I()
                          ~~~~~~~
</errors>)
        End Sub

        <WorkItem(546595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546595")>
        <Fact()>
        Public Sub NewOnCoClassInterface_UnboundGenericFromMetadata4()
            Dim reference = CreateReferenceFromIlCode(<![CDATA[
.class interface public abstract import I
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = {string('A`1+B`1[[System.Int32]]')} 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = 
        ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
}
.class public auto ansi beforefieldinit A`1<T>
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit B`1<T,U>
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method B`1::.ctor

  } // end of class B`1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`1::.ctor

} // end of class A`1
]]>.Value)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Private F As Object = New I()
    Public Shared Sub Main(args() As String)
    End Sub
End Class
]]>
                        </file>
                    </compilation>, references:={reference}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31094: Implementing class '?' for interface 'I' cannot be found.
    Private F As Object = New I()
                          ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_UnboundGenericType()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New IComplicated()
    End Sub
End Module

Public Class GenericType(Of T)
    Public Sub New()
    End Sub 
End Class
 
<CoClass(GetType(GenericType(Of )))>
Public Interface IComplicated
End Interface

]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31450: Type 'GenericType(Of )' cannot be used as an implementing class.
        Dim i = New IComplicated()
                ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_ObsoleteOnCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New IComplicated()
    End Sub
End Module

<Obsolete()>
Public Class CoClassType
End Class
 
<CoClass(GetType(CoClassType))>
Public Interface IComplicated
End Interface
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC40008: 'CoClassType' is obsolete.
&lt;CoClass(GetType(CoClassType))&gt;
                 ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_ObsoleteOnInterface()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New IComplicated()
    End Sub
End Module

Public Class CoClassType
End Class
 
<CoClass(GetType(CoClassType))>
<Obsolete()>
Public Interface IComplicated
End Interface
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC40008: 'IComplicated' is obsolete.
        Dim i = New IComplicated()
                    ~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_Inaccessible()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New B.IComplicated()
    End Sub
End Module

Public Class B
    Protected Class CoClassType
    End Class

    <CoClass(GetType(CoClassType))>
    Public Interface IComplicated
    End Interface
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31109: Implementing class 'B.CoClassType' for interface 'B.IComplicated' is not accessible in this context because it is 'Protected'.
        Dim i = New B.IComplicated()
                ~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_NullCoClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim i = New IComplicated()
    End Sub
End Module

<CoClass(Nothing)>
Public Interface IComplicated
End Interface
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30375: 'New' cannot be used on an interface.
        Dim i = New IComplicated()
                ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub SemanticInfo_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()  'BIND:"IInterface"
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Symbol)
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal("IInterface", semanticInfo.Symbol.ToDisplayString())

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
        End Sub

        <Fact()>
        Public Sub SemanticInfo_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()  'BIND:"New IInterface()"
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Symbol)
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal("Sub CoClassImplementation..ctor()", semanticInfo.Symbol.ToTestDisplayString())

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Type.Kind)
            Assert.Equal("IInterface", semanticInfo.Type.ToDisplayString())

            Assert.NotNull(semanticInfo.ConvertedType)
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Type.Kind)
            Assert.Equal("IInterface", semanticInfo.ConvertedType.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub SemanticInfo_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
End Interface

Public Class CoClassImplementation
End Class

Module M
    Sub Main(args() As String)
        Dim i = New IInterface()  
        Dim x = i 'BIND:"i"
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Symbol)
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal("i", semanticInfo.Symbol.ToDisplayString())

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Type.Kind)
            Assert.Equal("IInterface", semanticInfo.Type.ToDisplayString())

            Assert.NotNull(semanticInfo.ConvertedType)
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Type.Kind)
            Assert.Equal("IInterface", semanticInfo.ConvertedType.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_RuntimeException()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main(args() As String)
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Dim i As IComplicated = New IComplicated()
        Catch e As Exception
            Console.WriteLine(e.Message)
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub
End Module

Public Class CoClassImplementation
End Class

<CoClass(GetType(CoClassImplementation))>
Public Interface IComplicated
End Interface
]]>
                        </file>
                    </compilation>, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
            CompileAndVerify(compilation, expectedOutput:="Unable to cast object of type 'CoClassImplementation' to type 'IComplicated'.")
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_Lookup_Implements1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Interface Goo1
    Sub Quit()
End Interface

Interface Goo2
    Event Quit()
End Interface

<CoClass(GetType(GooClass))>
Interface Goo
    Inherits Goo1, Goo2
End Interface

Class GooClass
    Implements Goo
    Public Sub Quit() Implements Goo.Quit
    End Sub
    Public Event Quit1() Implements Goo.Quit
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_Lookup_Implements2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Interface Goo1
    Sub Quit()
End Interface

Interface Goo2
    Event Quit()
End Interface

Interface Goo
    Inherits Goo1, Goo2
End Interface

Class GooClass
    Implements Goo
    Public Sub Quit() Implements Goo.Quit
    End Sub
    Public Event Quit1() Implements Goo.Quit
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30149: Class 'GooClass' must implement 'Event Quit()' for interface 'Goo2'.
    Implements Goo
               ~~~
BC30149: Class 'GooClass' must implement 'Sub Quit()' for interface 'Goo1'.
    Implements Goo
               ~~~
BC31040: 'Quit' exists in multiple base interfaces. Use the name of the interface that declares 'Quit' in the 'Implements' clause instead of the name of the derived interface.
    Public Sub Quit() Implements Goo.Quit
                                 ~~~~~~~~
BC31040: 'Quit' exists in multiple base interfaces. Use the name of the interface that declares 'Quit' in the 'Implements' clause instead of the name of the derived interface.
    Public Event Quit1() Implements Goo.Quit
                                    ~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_Lookup_Implements3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Interface Goo1
    Sub Quit()
End Interface

Interface Goo2
    Event Quit()
End Interface

<CoClass(GetType(GooClass))>
Interface Goo
    Inherits Goo1, Goo2
End Interface

Interface Bar
    Inherits BarInner
End Interface

Interface BarInner
    Inherits Goo1, Goo2
End Interface

Interface AbcGoo
    Inherits Bar, Goo
End Interface

Interface abcBar
    Inherits Goo, Bar
End Interface

Class GooClass
End Class

Class AbcGooClass
    Implements AbcGoo
    Public Sub Quit() Implements AbcGoo.Quit
    End Sub
    Public Event Quit1() Implements AbcGoo.Quit
End Class

Class abcBarClass
    Implements abcBar
    Public Sub Quit() Implements abcBar.Quit
    End Sub
    Public Event Quit1() Implements abcBar.Quit
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30149: Class 'AbcGooClass' must implement 'Event Quit()' for interface 'Goo2'.
    Implements AbcGoo
               ~~~~~~
BC30149: Class 'AbcGooClass' must implement 'Sub Quit()' for interface 'Goo1'.
    Implements AbcGoo
               ~~~~~~
BC31040: 'Quit' exists in multiple base interfaces. Use the name of the interface that declares 'Quit' in the 'Implements' clause instead of the name of the derived interface.
    Public Sub Quit() Implements AbcGoo.Quit
                                 ~~~~~~~~~~~
BC31040: 'Quit' exists in multiple base interfaces. Use the name of the interface that declares 'Quit' in the 'Implements' clause instead of the name of the derived interface.
    Public Event Quit1() Implements AbcGoo.Quit
                                    ~~~~~~~~~~~
BC30149: Class 'abcBarClass' must implement 'Event Quit()' for interface 'Goo2'.
    Implements abcBar
               ~~~~~~
BC30149: Class 'abcBarClass' must implement 'Sub Quit()' for interface 'Goo1'.
    Implements abcBar
               ~~~~~~
BC31040: 'Quit' exists in multiple base interfaces. Use the name of the interface that declares 'Quit' in the 'Implements' clause instead of the name of the derived interface.
    Public Sub Quit() Implements abcBar.Quit
                                 ~~~~~~~~~~~
BC31040: 'Quit' exists in multiple base interfaces. Use the name of the interface that declares 'Quit' in the 'Implements' clause instead of the name of the derived interface.
    Public Event Quit1() Implements abcBar.Quit
                                    ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_Lookup_Handles()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
    End Sub
End Module

Interface Goo1
    Sub Quit()
End Interface

Interface Goo2
    Event Quit()
End Interface

<CoClass(GetType(GooClass))>
Interface Goo
    Inherits Goo1, Goo2
End Interface

Interface GooGoo
    Inherits Goo
End Interface

Class GooClass
    Implements Goo
    Public Sub Quit() Implements Goo1.Quit
    End Sub
    Public Event Quit1() Implements Goo2.Quit
End Class

Class GooGooClass
    Implements GooGoo

    WithEvents Instance1 As GooGoo = New GooGooClass
    WithEvents Instance2 As New GooGooClass
    WithEvents Instance3 As Goo = New GooClass
    WithEvents Instance4 As New GooClass

    WithEvents GooInstance As New GooClass

    Public Sub XYZ1() Handles Instance1.Quit
    End Sub

    Public Sub XYZ2() Handles Instance2.Quit
    End Sub

    Public Sub XYZ3() Handles Instance3.Quit
    End Sub

    Public Sub XYZ4() Handles Instance4.Quit
    End Sub

    Public Sub Quit() Implements Goo1.Quit
    End Sub
    Public Event Quit1() Implements Goo2.Quit
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30590: Event 'Quit' cannot be found.
    Public Sub XYZ2() Handles Instance2.Quit
                                        ~~~~
BC30590: Event 'Quit' cannot be found.
    Public Sub XYZ4() Handles Instance4.Quit
                                        ~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NewOnCoClassInterface_Lookup_AddRemoveHandler()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
    End Sub
End Module

Interface Goo1
    Sub Quit()
End Interface

Interface Goo2
    Event Quit()
End Interface

<CoClass(GetType(GooClass))>
Interface Goo
    Inherits Goo1, Goo2
End Interface

Interface GooGoo
    Inherits Goo
End Interface

Class GooClass
    Implements Goo
    Public Sub Quit2() Implements Goo1.Quit
    End Sub
    Public Event Quit1() Implements Goo2.Quit
End Class

Class GooGooClass
    Implements GooGoo

    Dim Instance1 As GooGoo = New GooGooClass
    Dim Instance2 As New GooGooClass
    Dim Instance3 As Goo = New GooClass
    Dim Instance4 As New GooClass

    WithEvents GooInstance As New GooClass

    Public Sub Quit2() Implements Goo1.Quit
        AddHandler Instance1.Quit, AddressOf Quit2
        AddHandler Instance2.Quit, AddressOf Quit2
        AddHandler Instance3.Quit, AddressOf Quit2
        AddHandler Instance4.Quit, AddressOf Quit2
        RemoveHandler ((Instance1).Quit), AddressOf Quit2
        RemoveHandler Instance2.Quit, AddressOf Quit2
        RemoveHandler Instance3.Quit, AddressOf Quit2
        RemoveHandler Instance4.Quit, AddressOf Quit2
    End Sub
    Public Event Quit1() Implements Goo2.Quit
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30456: 'Quit' is not a member of 'GooGooClass'.
        AddHandler Instance2.Quit, AddressOf Quit2
                   ~~~~~~~~~~~~~~
BC30456: 'Quit' is not a member of 'GooClass'.
        AddHandler Instance4.Quit, AddressOf Quit2
                   ~~~~~~~~~~~~~~
BC30456: 'Quit' is not a member of 'GooGooClass'.
        RemoveHandler Instance2.Quit, AddressOf Quit2
                      ~~~~~~~~~~~~~~
BC30456: 'Quit' is not a member of 'GooClass'.
        RemoveHandler Instance4.Quit, AddressOf Quit2
                      ~~~~~~~~~~~~~~
</errors>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23810, "https://github.com/dotnet/roslyn/issues/23810")>
        <Fact()>
        Public Sub NewOnCoClassInterface_Lookup_AddRemoveHandler2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
    End Sub
End Module

Interface Goo1
    Function Quit() As Goo
End Interface

Interface Goo2
    Event Quit()
End Interface

<CoClass(GetType(GooClass))>
Interface Goo
    Inherits Goo1, Goo2
End Interface

Interface GooGoo
    Inherits Goo
End Interface

Class GooClass
    Implements Goo
    Public Function Quit2() As Goo Implements Goo1.Quit
        Return Nothing
    End Function
    Public Event Quit1() Implements Goo2.Quit
End Class

Class GooGooClass
    Implements GooGoo

    Dim Instance1 As GooGoo = New GooGooClass
    Dim Instance3 As Goo = New GooClass

    WithEvents GooInstance As New GooClass

    Public Function Quit3() As Goo Implements Goo1.Quit
        Return Nothing
    End Function
    Public Sub Quit2()
        AddHandler (((Instance1).Quit.Quit).Quit), AddressOf Quit2
        RemoveHandler Instance3.Quit.Quit.Quit.Quit.Quit.Quit, AddressOf Quit2
    End Sub
    Public Event Quit1() Implements Goo2.Quit
End Class
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().ToArray()

            Assert.Equal("AddHandler (((Instance1).Quit.Quit).Quit), AddressOf Quit2", nodes(0).ToString())

            compilation.VerifyOperationTree(nodes(0), expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... essOf Quit2')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... essOf Quit2')
      Event Reference: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: Goo2.QuitEventHandler) (Syntax: '(((Instance ... Quit).Quit)')
          Operand: 
            IEventReferenceOperation: Event Goo2.Quit() (OperationKind.EventReference, Type: Goo2.QuitEventHandler) (Syntax: '((Instance1 ... .Quit).Quit')
              Instance Receiver: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: Goo) (Syntax: '((Instance1).Quit.Quit)')
                  Operand: 
                    IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: '(Instance1).Quit.Quit')
                      Instance Receiver: 
                        IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: '(Instance1).Quit')
                          Instance Receiver: 
                            IParenthesizedOperation (OperationKind.Parenthesized, Type: GooGoo) (Syntax: '(Instance1)')
                              Operand: 
                                IFieldReferenceOperation: GooGooClass.Instance1 As GooGoo (OperationKind.FieldReference, Type: GooGoo) (Syntax: 'Instance1')
                                  Instance Receiver: 
                                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: GooGooClass, IsImplicit) (Syntax: 'Instance1')
                          Arguments(0)
                      Arguments(0)
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Goo2.QuitEventHandler, IsImplicit) (Syntax: 'AddressOf Quit2')
          Target: 
            IMethodReferenceOperation: Sub GooGooClass.Quit2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Quit2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: GooGooClass, IsImplicit) (Syntax: 'Quit2')
]]>.Value)

            Assert.Equal("RemoveHandler Instance3.Quit.Quit.Quit.Quit.Quit.Quit, AddressOf Quit2", nodes(1).ToString())

            compilation.VerifyOperationTree(nodes(1), expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... essOf Quit2')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... essOf Quit2')
      Event Reference: 
        IEventReferenceOperation: Event Goo2.Quit() (OperationKind.EventReference, Type: Goo2.QuitEventHandler) (Syntax: 'Instance3.Q ... t.Quit.Quit')
          Instance Receiver: 
            IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: 'Instance3.Q ... t.Quit.Quit')
              Instance Receiver: 
                IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: 'Instance3.Q ... t.Quit.Quit')
                  Instance Receiver: 
                    IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: 'Instance3.Quit.Quit.Quit')
                      Instance Receiver: 
                        IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: 'Instance3.Quit.Quit')
                          Instance Receiver: 
                            IInvocationOperation (virtual Function Goo1.Quit() As Goo) (OperationKind.Invocation, Type: Goo) (Syntax: 'Instance3.Quit')
                              Instance Receiver: 
                                IFieldReferenceOperation: GooGooClass.Instance3 As Goo (OperationKind.FieldReference, Type: Goo) (Syntax: 'Instance3')
                                  Instance Receiver: 
                                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: GooGooClass, IsImplicit) (Syntax: 'Instance3')
                              Arguments(0)
                          Arguments(0)
                      Arguments(0)
                  Arguments(0)
              Arguments(0)
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Goo2.QuitEventHandler, IsImplicit) (Syntax: 'AddressOf Quit2')
          Target: 
            IMethodReferenceOperation: Sub GooGooClass.Quit2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Quit2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: GooGooClass, IsImplicit) (Syntax: 'Quit2')
]]>.Value)
        End Sub

        <WorkItem(546560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546560")>
        <Fact()>
        Public Sub RetargetingUseSiteErrorMissingConstraintTypeAndCircularConstraint()
            Dim vbSource1 =
                <compilation name="abc">
                    <file name="c.vb">
Imports System
Public Interface Goo
    Sub Bar()
End Interface
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(vbSource1)
            compilation1.AssertTheseDiagnostics(<errors></errors>)

            Dim vbSource2 =
                <compilation>
                    <file name="c.vb">
Imports System
Public Interface Goo2
    Inherits Goo
End Interface
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(vbSource2, {New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(<expected></expected>)

            Dim vbSource3 =
                <compilation>
                    <file name="c.vb">
Imports System
Class Clazz
    Implements Goo2
    Public Sub Bar() Implements Goo2.Bar
    End Sub
End Class
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(vbSource3, {New VisualBasicCompilationReference(compilation2)})
            compilation3.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'abc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Goo'. Add one to your project.
    Implements Goo2
               ~~~~
BC30652: Reference required to assembly 'abc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Goo'. Add one to your project.
    Public Sub Bar() Implements Goo2.Bar
                                ~~~~
BC30401: 'Bar' cannot implement 'Bar' because there is no matching sub on interface 'Goo2'.
    Public Sub Bar() Implements Goo2.Bar
                                ~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(657731, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657731")>
        Public Sub Bug657731()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System.Runtime.InteropServices
<CoClass(GetType(C))>
Public Interface I
    Property P As Integer
End Interface
Public Class C
    Dim F = New I() With {.P = 2}
End Class
]]>
                        </file>
                    </compilation>, TestOptions.ReleaseDll)

            Dim validator = Sub(m As ModuleSymbol)
                                Assert.Null(m.GlobalNamespace.GetTypeMember("C").CoClassType)
                            End Sub

            CompileAndVerify(compilation, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact()>
        Public Sub Bug873059()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
 
Module Program
    Sub Main(args As String())
        Dim goo = New NS.MyD(Sub()
                             End Sub)
    End Sub
End Module
 
Namespace NS
    Interface MyD
        Private p As Action
 
        Public Sub New(p As Action)
        Me.p = p
        End Sub
    End Interface
End Namespace
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30375: 'New' cannot be used on an interface.
        Dim goo = New NS.MyD(Sub()
                  ~~~~~~~~~~~~~~~~~
BC30602: Interface members must be methods, properties, events, or type definitions.
        Private p As Action
        ~~~~~~~~~~~~~~~~~~~
BC30270: 'Public' is not valid on an interface method declaration.
        Public Sub New(p As Action)
        ~~~~~~
BC30363: 'Sub New' cannot be declared in an interface.
        Public Sub New(p As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
        Me.p = p
        ~~~~~~~~
BC30603: Statement cannot appear within an interface body.
        End Sub
        ~~~~~~~
</errors>)
        End Sub
    End Class

End Namespace

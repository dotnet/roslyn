' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class OptionalArgumentTests
        Inherits BasicTestBase

        <WorkItem(543066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543066")>
        <Fact()>
        Public Sub TestOptionalOnGenericMethod()
            Dim source =
<compilation name="TestOptionalOnGenericMethod">
    <file name="a.vb">
        <![CDATA[
Imports System

Module Program
    Sub Foo(Of T)(x As Integer, Optional y As Integer = 10)
        Console.WriteLine(y) 
    End Sub

    Sub Main(args As String())
        Foo(Of Integer)(1)
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.AssertNoDiagnostics()
            CompileAndVerify(source,
     expectedOutput:=<![CDATA[
10
]]>)
        End Sub

        ' Verify that there is no copy back conversion when optional parameters are byref.
        <WorkItem(543099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543099")>
        <Fact()>
        Public Sub TestIntegerOptionalDoubleWithConversionError()
            Dim source =
<compilation name="TestIntegerOptionalDoubleWithConversionError">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Foo(x As Integer, Optional y As Double = #1/1/2001#)
     End Sub

    Sub Main(args As String())
        Foo(1)
    End Sub
End Module

]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_DateToDoubleConversion, "#1/1/2001#"))
        End Sub

        <WorkItem(543093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543093")>
        <Fact()>
        Public Sub TestOptionalByRef()
            Dim source =
<compilation name="TestOptionalByRef">
    <file name="a.vb">
        <![CDATA[
Option Strict Off
Imports System
Module Program
    Sub Foo(Optional byref y As DateTime = #1/1/2012#)
        Console.WriteLine(y.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
    End Sub

    Sub Bar(Optional byref y As integer = 2D)
    Console.WriteLine(y)
    End Sub

    Sub Main(args As String())
        Foo()
        Bar()
    End Sub
End Module

]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.AssertNoDiagnostics()
            CompileAndVerify(source,
expectedOutput:=<![CDATA[
 1/1/2012 12:00:00 AM
2

]]>)
        End Sub

        ' Report error if the default value of overridden method is different 
        <Fact()>
        Public Sub TestOverridingOptionalWithDifferentDefaultValue()
            Dim source =
<compilation name="TestOverridingOptionalWithDifferentDefaultValue">
    <file name="a.vb">
        <![CDATA[
MustInherit Class c1
    Public MustOverride Sub s1(Optional i As Integer = 0)
    Public MustOverride Sub s1(s As String)
End Class

Class c2
    Inherits c1

    Overrides Sub s1(Optional i As Integer = 2)
    End Sub

    Overrides Sub s1(s As String)
    End Sub
End Class

Module Program
    Sub Main(args As String())
    End Sub
End Module

]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_OverrideWithDefault2, "s1").WithArguments("Public Overrides Sub s1([i As Integer = 2])", "Public MustOverride Sub s1([i As Integer = 0])"))
        End Sub

        ' Should only report an error for guid parameter.
        <WorkItem(543202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543202")>
        <Fact()>
        Public Sub TestOptionalAfterParameterWithConversionError()
            Dim source =
<compilation name="TestOptionalAfterParameterWithConversionError">
    <file name="a.vb">
        <![CDATA[
Imports System

Module Program
    Sub s1(g As Guid, Optional i As Integer = 2)
    End Sub

    Sub Main(args As String())
        s1(1)
    End Sub
End Module

]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeMismatch2, "1").WithArguments("Integer", "System.Guid"))
        End Sub

        <WorkItem(543227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543227")>
        <Fact()>
        Public Sub TestMultipleEnumDefaultValues()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Strict On
Imports System

Module Program

    Enum Alphabet As Byte
        A
        B
    End Enum

    Sub s1(i As Integer, Optional l1 As Alphabet = Alphabet.A, Optional l2 As Alphabet = Alphabet.B)
    End Sub

    Sub Main(args As String())
        s1(0)
    End Sub
End Module

]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.AssertNoDiagnostics()
        End Sub


        <WorkItem(543179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543179")>
        <Fact()>
        Public Sub TestOptionalObject()
            Dim source =
<compilation name="TestOptionalObject">
    <file name="a.vb">
        <![CDATA[
Module Program
    Public Const myvar As Object = " -5 "

    Public Sub foo(Optional o As Object = myvar)
    End Sub

    Sub Main(args As String())
        foo()
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.AssertNoDiagnostics()
        End Sub


        <WorkItem(543230, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543230")>
        <Fact()>
        Public Sub TestOptionalIntegerWithStringValue()
            Dim source =
<compilation name="TestOptionalIntegerWithStringValue">
    <file name="a.vb">
        <![CDATA[
Option Strict Off
Module Program
    Sub foo(Optional arg1 As Integer = "12")
    End Sub
    Sub Main(args As String())
        foo()
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredConstConversion2, """12""").WithArguments("String", "Integer"))
        End Sub


        <WorkItem(543395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543395")>
        <Fact()>
        Public Sub TestEventWithOptionalInteger()
            Dim source =
<compilation name="TestEventWithOptionalInteger">
    <file name="a.vb">
        <![CDATA[
Class A
    Event E(Optional I As Integer = 0)

    Public Sub Do_E()
        RaiseEvent E()
    End Sub
End Class

Module Program
    Sub Main(args As String())
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_OptionalIllegal1, "Optional").WithArguments("Event"),
                                   Diagnostic(ERRID.ERR_OmittedArgument2, "RaiseEvent E()").WithArguments("I", "Public Event E(I As Integer)"))
        End Sub

        <Fact(), WorkItem(543526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543526")>
        Public Sub MidParameterMissOptional()
            Dim source =
<compilation name="MidParameterMissOptional">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main(args As String())
    End Sub
    Sub TEST(ByRef Optional X As Integer = 1, Z As Integer, ByRef Optional Y As String = "STRING")
    End Sub
End Module
]]>
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedOptional, "Z"))
        End Sub

        <Fact()>
        Public Sub ParamArrayAfterOptional()
            Dim source =
<compilation name="MidParameterMissOptional">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main(args As String())
    End Sub
    Sub TEST(ByRef Optional X As Integer = 1, paramarray Y() As object)
    End Sub
End Module
]]>
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ParamArrayWithOptArgs, "Y"))
        End Sub

        <Fact()>
        Public Sub ParamArrayNotLast()
            Dim source =
<compilation name="MidParameterMissOptional">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main(args As String())
    End Sub
    Sub TEST(ByRef X As Integer, paramarray Y() As object, Optional z As Integer = 1)
    End Sub
End Module
]]>
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ParamArrayMustBeLast, "Optional z As Integer = 1"))
        End Sub

        <Fact(), WorkItem(543527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543527")>
        Public Sub OptionalParameterValueAssignedToRuntime()
            Dim source =
<compilation name="OptionalParameterValueAssignedToRuntime">
    <file name="a.vb">
        <![CDATA[
Class Program
    Sub Main(args As String())
    End Sub
    Sub test(Optional x As String = String.Empty)
    End Sub
End Class
]]>
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_RequiredConstExpr, "String.Empty"))
        End Sub

        <Fact(), WorkItem(543531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543531")>
        Public Sub OptionalParameterForConstructorofStructure()
            Dim source =
<compilation name="OptionalParameterForConstructorofStructure">
    <file name="a.vb">
        <![CDATA[
Structure S1
    Public Sub New(Optional ByVal y As Integer = 1)
    End Sub
    Shared Sub main()
    End Sub
End Structure
]]>
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source).AssertNoDiagnostics()
        End Sub

        <Fact(), WorkItem(544515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544515")>
        Public Sub OptionalNullableInteger()
            Dim source =
<compilation name="OptionalNullableInteger">
    <file name="a.vb">
        <![CDATA[
    Imports System

    Module m
      Sub X(Optional i As Integer? = 10)
        Console.WriteLine("{0}", i)
      End Sub
  
    Sub main()
        X()
    End Sub
  End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            CompileAndVerify(source,
     expectedOutput:=<![CDATA[
10
]]>).VerifyIL("m.main", <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.s   10
  IL_0002:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0007:  call       "Sub m.X(Integer?)"
  IL_000c:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(544515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544515")>
        Public Sub OptionalNullableIntegerWithNothingValue()
            Dim source =
<compilation name="OptionalNullableInteger">
    <file name="a.vb">
        <![CDATA[
    Imports System

    Module m
      Sub X(Optional i As Integer? = nothing)
        Console.WriteLine("{0}", i.hasValue)
      End Sub
  
    Sub main()
        X()
    End Sub
  End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            CompileAndVerify(source,
     expectedOutput:=<![CDATA[
False
]]>).VerifyIL("m.main", <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Integer?"
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub m.X(Integer?)"
  IL_000e:  ret
}
]]>)
        End Sub

        <WorkItem(544603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544603")>
        <Fact()>
        Public Sub OptionalParameterValueRefersToContainingFunction1()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Module Module1
  Sub Main()
  End Sub

  Class C
    Public Shared Function Foo(Optional a As C = Foo()) As C
       Return Nothing
    End Function

  End Class

End Module
    ]]></file>
</compilation>)
            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_CircularEvaluation1, "Foo()").WithArguments("[a As Module1.C]")
                    )
        End Sub


        <WorkItem(544603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544603")>
        <Fact()>
        Public Sub OptionalParameterValueRefersToContainingFunction2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Module Module1
  Sub Main()
  End Sub

  Class C
    Public Shared Function Foo(Optional f As C = Bar()) As C
       Return Nothing
    End Function

    Public Shared Function Bar(Optional b As C = Foo()) As C
       Return Nothing
    End Function
  End Class

End Module
    ]]></file>
</compilation>)
            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_CircularEvaluation1, "Bar()").WithArguments("[f As Module1.C]"),
                    Diagnostic(ERRID.ERR_RequiredConstExpr, "Foo()")
                  )
        End Sub


        <WorkItem(544603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544603")>
        <Fact()>
        Public Sub OptionalParameterValueRefersToMe()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Module Module1
  Sub Main()
  End Sub

  Class C
    Public Shared Function Foo(Optional f As C = Me) As C
       Return Nothing
    End Function

    Public Shared Function Bar(Optional f As C = foo(Me)) As C
       Return Nothing
    End Function

  End Class

End Module
    ]]></file>
</compilation>)
            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_UseOfKeywordNotInInstanceMethod1, "Me").WithArguments("Me"),
                    Diagnostic(ERRID.ERR_UseOfKeywordNotInInstanceMethod1, "Me").WithArguments("Me")
                  )
        End Sub


        <WorkItem(545416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545416")>
        <Fact()>
        Public Sub OptionalParameterValueWithEnumValue()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
    Public Enum PropertyPagesDialogAction
        None    'Do nothing (don't close the dialog)
        Apply   'Click the 'Apply' button (don't close the dialog)
        OK      'Click the 'OK' button (and close the dialog)
        Cancel  'Click the 'Cancel' button (and close the dialog)
    End Enum

    Public MustInherit Class Element

        Public MustOverride Overloads Function F1( _
                          Optional ByVal dialogCloseMethod As PropertyPagesDialogAction = PropertyPagesDialogAction.OK) _
                          As Boolean

        Public MustOverride Overloads Property P1( _
                          Optional ByVal dialogCloseMethod As PropertyPagesDialogAction = PropertyPagesDialogAction.OK) _
                          As Boolean
    End Class

    ]]></file>
</compilation>)
            compilation.AssertNoDiagnostics()
        End Sub

        <Fact()>
        Public Sub NullableEnumAsOptional()
            Dim compilation =
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On

Module Module1
    Sub Main(args As String())
        Test()
    End Sub

    Sub Test(Optional x As TestEnum? = TestEnum.B)
        System.Console.WriteLine(x)
    End Sub
End Module

Enum TestEnum
    A
    B
End Enum
    ]]></file>
</compilation>

            CompileAndVerify(compilation, expectedOutput:="B").VerifyDiagnostics()
        End Sub

        <WorkItem(536772, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536772")>
        <Fact()>
        Public Sub NullableConversionsInOptional()
            Dim compilation =
<compilation>
    <file name="c.vb"><![CDATA[
Imports System

Module Program
    Sub foo1(Of T)(Optional x As T = CType(Nothing, T))
        Console.WriteLine("x = {0}", x)
    End Sub
    Sub foo2(Of T)(Optional x As T = DirectCast(Nothing, T))
        Console.WriteLine("x = {0}", x)
    End Sub
    Sub foo3(Of T As Class)(Optional x As T = TryCast(Nothing, T))
        Console.WriteLine("x = {0}", If(x Is Nothing, "nothing", x))
    End Sub
    Sub foo4(Of T As Class)(Optional x As T = CType(CType(Nothing, T), T))
        Console.WriteLine("x = {0}", If(x Is Nothing, "nothing", x))
    End Sub
    Sub Main(args As String())
        foo1(Of Integer)()
        foo2(Of Integer)()
        foo3(Of String)()
        foo4(of string)()
    End Sub
End Module
    ]]></file>
</compilation>

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
x = 0
x = 0
x = nothing
x = nothing
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(543187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543187")>
        <Fact()>
        Public Sub OptionalWithIUnknownConstantAndIDispatchConstant()

            Dim libSource =
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Public Class C

    Public Shared Sub M1(Optional x As Object = Nothing)
        Console.WriteLine(If(x, 1))
    End Sub

    Public Shared Sub M2(<[Optional]> x As Object)
        Console.WriteLine(If(x, 2))
    End Sub

    Public Shared Sub M3(<IDispatchConstant> Optional x As Object = Nothing)
        Console.WriteLine(If(x, 3))
    End Sub

    Public Shared Sub M4(<IDispatchConstant> <[Optional]> x As Object)
        Console.WriteLine(If(x, 4))
    End Sub

    Public Shared Sub M5(<IUnknownConstant> Optional x As Object = Nothing)
        Console.WriteLine(If(x, 5))
    End Sub

    Public Shared Sub M6(<IUnknownConstant> <[Optional]> x As Object) 
        Console.WriteLine(If(x, 6))
    End Sub

    Public Shared Sub M7(<IUnknownConstant> <IDispatchConstant> Optional x As Object = Nothing)
        Console.WriteLine(If(x, 7))
    End Sub

    Public Shared Sub M8(<IUnknownConstant> <IDispatchConstant> <[Optional]> x As Object)
        Console.WriteLine(If(x, 8))
    End Sub

End Class
    ]]></file>
</compilation>

            Dim libComp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(libSource)

            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
Module Module1

    Sub Main()
        C.M1()
        C.M2()
        C.M3()
        C.M4()
        C.M5()
        C.M6()
        C.M7()
        C.M8()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilationRef As MetadataReference = libComp.ToMetadataReference()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:={compilationRef})

            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_OmittedArgument2, "M2").WithArguments("x", "Public Shared Sub M2(x As Object)"),
                                   Diagnostic(ERRID.ERR_OmittedArgument2, "M4").WithArguments("x", "Public Shared Sub M4(x As Object)"),
                                   Diagnostic(ERRID.ERR_OmittedArgument2, "M6").WithArguments("x", "Public Shared Sub M6(x As Object)"),
                                   Diagnostic(ERRID.ERR_OmittedArgument2, "M8").WithArguments("x", "Public Shared Sub M8(x As Object)"))

            Dim metadataRef = MetadataReference.CreateFromImage(libComp.EmitToArray())

            CompileAndVerify(source, additionalRefs:={metadataRef}, expectedOutput:=<![CDATA[
1
System.Reflection.Missing
3
System.Runtime.InteropServices.DispatchWrapper
5
System.Runtime.InteropServices.UnknownWrapper
7
System.Runtime.InteropServices.DispatchWrapper
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(543187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543187")>
        <Fact()>
        Public Sub OptionalWithIUnknownConstantAndIDispatchConstantWithString()

            Dim libSource =
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Namespace SpecialOptionalLib

Public Class C

    Public Shared Sub M1(<IDispatchConstant> Optional x As string = Nothing)
        Console.WriteLine(If(x, 1))
    End Sub

    Public Shared Sub M2(<IUnknownConstant> Optional x As string = Nothing)
        Console.WriteLine(If(x, 2))
    End Sub

End Class

End Namespace
    ]]></file>
</compilation>

            Dim libComp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(libSource)

            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
Imports SpecialOptionalLib.C

Module Module1

    Sub Main()
        M1()
        M2()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim libRef = MetadataReference.CreateFromImage(libComp.EmitToArray())

            CompileAndVerify(source, additionalRefs:=New MetadataReference() {libRef}, expectedOutput:=<![CDATA[
1
2
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(543187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543187")>
        <Fact>
        Public Sub OptionalWithMarshallAs()
            Dim libSource =
            <compilation>
                <file name="c.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Public Class C

    Public Shared Sub M1(<MarshalAs(UnmanagedType.Interface)> <[Optional]> x As Object)
        Console.WriteLine(If(x, 1))
    End Sub

    Public Shared Sub M2(<IDispatchConstant> <MarshalAs(UnmanagedType.Interface)> <[Optional]> x As Object)
        Console.WriteLine(If(x, 2))
    End Sub

    Public Shared Sub M3(<IUnknownConstant> <MarshalAs(UnmanagedType.Interface)> <[Optional]> x As Object)
        Console.WriteLine(If(x, 3))
    End Sub

    Public Shared Sub M4(<IUnknownConstant> <IDispatchConstant> <MarshalAs(UnmanagedType.Interface)> <[Optional]> x As Object)
        Console.WriteLine(If(x, 4))
    End Sub

    Public Shared Sub M5(<IUnknownConstant> <MarshalAs(UnmanagedType.Interface)> <IDispatchConstant> <[Optional]> x As Object)
        Console.WriteLine(If(x, 5))
    End Sub

    Public Shared Sub M6(<MarshalAs(UnmanagedType.Interface)> <IDispatchConstant> <IUnknownConstant> <[Optional]> x As Object)
        Console.WriteLine(If(x, 6))
    End Sub

    Public Shared Sub M7(<IUnknownConstant> <IDispatchConstant> <[Optional]> x As Object)
        Console.WriteLine(If(x, 7))
    End Sub

    Public Shared Sub M8(<IDispatchConstant> <IUnknownConstant> <[Optional]> x As Object)
        Console.WriteLine(If(x, 8))
    End Sub

End Class
    ]]></file>
            </compilation>

            Dim libComp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(libSource)

            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
Class D

    Shared Sub Main()
        C.M1()
        C.M2()
        C.M3()
        C.M4()
        C.M5()
        C.M6()
        C.M7()
        C.M8()
    End Sub

End Class
    ]]></file>
</compilation>

            Dim expected = <![CDATA[
1
2
3
4
5
6
System.Runtime.InteropServices.DispatchWrapper
System.Runtime.InteropServices.DispatchWrapper
]]>

            Dim metadataRef = MetadataReference.CreateFromImage(libComp.EmitToArray())
            CompileAndVerify(source, additionalRefs:={metadataRef}, expectedOutput:=expected).VerifyDiagnostics()
        End Sub

        <WorkItem(545405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545405")>
        <Fact()>
        Public Sub OptionalWithNoDefaultValue()

            Dim libSource =
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Namespace SpecialOptionalLib

Public Class C

    Public Shared Sub Foo1(<[Optional]> x As Object)
        Console.WriteLine(If(x, "nothing"))
    End Sub

    Public Shared Sub Foo2(<[Optional]> x As String)
        Console.WriteLine(If(x, "nothing"))
    End Sub

    Public Shared Sub Foo3(<[Optional]> x As Integer)
        Console.WriteLine(x)
    End Sub

End Class

End Namespace
    ]]></file>
</compilation>

            Dim libComp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(libSource)

            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
Imports SpecialOptionalLib.C

Module Module1

    Sub Main()
        Foo1()
        Foo2()
        Foo3()
    End Sub

End Module
    ]]></file>
</compilation>
            Dim libRef As MetadataReference = libComp.ToMetadataReference()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:=New MetadataReference() {libRef})

            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_OmittedArgument2, "Foo1").WithArguments("x", "Public Shared Sub Foo1(x As Object)"),
                                   Diagnostic(ERRID.ERR_OmittedArgument2, "Foo2").WithArguments("x", "Public Shared Sub Foo2(x As String)"),
                                   Diagnostic(ERRID.ERR_OmittedArgument2, "Foo3").WithArguments("x", "Public Shared Sub Foo3(x As Integer)"))

            libRef = MetadataReference.CreateFromImage(libComp.EmitToArray())

            CompileAndVerify(source, additionalRefs:=New MetadataReference() {libRef}, expectedOutput:=<![CDATA[
System.Reflection.Missing
nothing
0
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(545405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545405")>
        <Fact>
        Public Sub OptionalWithOptionCompare()

            Dim libSource =
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic.CompilerServices

Namespace SpecialOptionalLib

Public Class C

    Public Shared Sub foo1(<OptionCompare()> Optional x As Boolean = True)
        Console.WriteLine(x)
    End Sub

    Public Shared Sub foo2(<OptionCompare()> Optional x As Integer = 5)
        Console.WriteLine(x)
    End Sub

    Public Shared Sub foo3(<OptionCompare()> Optional x As String = "a")
        Console.WriteLine(x)
    End Sub

    Public Shared Sub foo4(<OptionCompare()> Optional x As Decimal = 10.0)
        Console.WriteLine(x)
    End Sub

End Class

End Namespace
    ]]></file>
</compilation>

            Dim libComp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(libSource)

            Dim source =
<compilation>
    <file name="c.vb"><![CDATA[
Imports SpecialOptionalLib.C

Module Module1

    Sub Main()
        Foo1()
        Foo2()
        Foo3()
        foo4()
    End Sub

End Module
    ]]></file>
</compilation>
            Dim libRef As MetadataReference = libComp.ToMetadataReference()

            CompileAndVerify(source, additionalRefs:=New MetadataReference() {libRef}, expectedOutput:=<![CDATA[
True
5
a
10
]]>).VerifyDiagnostics()

            libRef = MetadataReference.CreateFromImage(libComp.EmitToArray())

            CompileAndVerify(source, additionalRefs:=New MetadataReference() {libRef}, expectedOutput:=<![CDATA[
False
0
0
0
]]>).VerifyDiagnostics()

            CompileAndVerify(source,
                             additionalRefs:=New MetadataReference() {libRef},
                             options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optionCompareText:=True),
                             expectedOutput:=<![CDATA[
True
1
1
1
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(545686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545686")>
        <Fact()>
        Public Sub ParameterValueWithGenericSemanticInfo()
            Dim compilation = CreateCompilationWithMscorlib(
          <compilation>
              <file name="a.vb"><![CDATA[
Option Strict On

Class Generic(Of T)
    Public Const X As Integer = 0
End Class

Interface I
    Sub Foo(Of T)(Optional x As Integer = Generic(Of T).X) 'BIND:"Generic(Of T)"'BIND:"Generic(Of T)"
End Interface
    ]]></file>
          </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of GenericNameSyntax)(compilation, "a.vb")

            Assert.Equal("Generic(Of T)", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Generic(Of T)", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Generic(Of T)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(578129, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578129")>
        <Fact()>
        Public Sub Bug578129()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Partial Class PC2 ' attributes on implementation
    Partial Private Sub VerifyCallerInfo(
            expectedPath As String,
            expectedLine As String,
            expectedMember As String,
            Optional f A String = "",
            Optional l As Integer = -1,
            Optional m As String = Nothing)
    End Sub
End Class
Partial Class PC2
    Private Sub VerifyCallerInfo(
            expectedPath As String,
            expectedLine As String,
            expectedMember As String,
            <CallerFilePath> Optional f As String = "",
            <CallerLineNumber> Optional l As Integer = -1,
            <CallerMemberName> Optional m As String = Nothing)
        Console.WriteLine("callerinfo: ({0}, {1}, {2})", "[...]", l, m)
    End Sub
 
End Class
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=TestOptions.ReleaseDll)

            AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30529: All parameters must be explicitly typed if any of them are explicitly typed.
            Optional f A String = "",
                     ~
BC30812: Optional parameters must specify a default value.
            Optional f A String = "",
                       ~
BC30451: 'A' is not declared. It may be inaccessible due to its protection level.
            Optional f A String = "",
                       ~
BC30002: Type 'CallerFilePath' is not defined.
            <CallerFilePath> Optional f As String = "",
             ~~~~~~~~~~~~~~
BC30002: Type 'CallerLineNumber' is not defined.
            <CallerLineNumber> Optional l As Integer = -1,
             ~~~~~~~~~~~~~~~~
BC30002: Type 'CallerMemberName' is not defined.
            <CallerMemberName> Optional m As String = Nothing)
             ~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub CallerInfo1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Class TestBase
    Sub New(
        <CallerLineNumber> Optional number As Integer = -1,
        <CallerMemberName> Optional member As String = "UnknownMember",
        <CallerFilePath> Optional file As String = "UnknownFile")

        System.Console.WriteLine("TestBase.New")
        System.Console.WriteLine("    {0}", file)
        System.Console.WriteLine("    {0}", number)
        System.Console.WriteLine("    {0}", member)
    End Sub


    Property P0 As Integer
End Class

Class TestDerived
    Inherits TestBase
    Implements IEnumerable(Of String)

    Sub New(x As Integer)
        MyBase.New()
        System.Console.WriteLine("+++ TestDerived.New(Integer)")
        System.Console.WriteLine("--- TestDerived.New(Integer)")
    End Sub

    Sub New()
        System.Console.WriteLine("+++ TestDerived.New()")
        CallerInfo("TestDerived.New()")
        System.Console.WriteLine("--- TestDerived.New()")
    End Sub

    Public F1 As Integer = CallerInfo("F1")

    Public P1 As Integer = CallerInfo("P1")

    Public Sub M1()
        CallerInfo("M1")
    End Sub

    Public Property P2 As Integer
        Get
            Return CallerInfo("get_P2")
        End Get
        Set(value As Integer)
            CallerInfo("set_P2")
        End Set
    End Property

    Custom Event E1 As Action
        AddHandler(value As Action)
            CallerInfo("AddHandler")
        End AddHandler
        RemoveHandler(value As Action)
            CallerInfo("RemoveHandler")
        End RemoveHandler
        RaiseEvent()
            CallerInfo("RaiseEvent")
        End RaiseEvent
    End Event

    Sub Raise()
        RaiseEvent E1()
    End Sub

    Function Add(
        context As String,
        <CallerLineNumber> Optional number As Integer = -1,
        <CallerMemberName> Optional member As String = "UnknownMember",
        <CallerFilePath> Optional file As String = "UnknownFile"
    ) As Integer

        System.Console.WriteLine("Add <{0}>", context)
        System.Console.WriteLine("    {0}", file)
        System.Console.WriteLine("    {0}", number)
        System.Console.WriteLine("    {0}", member)

        Return number
    End Function


    'Sub Overloaded(<CallerLineNumber> Optional number As Integer = -1)
    '    System.Console.WriteLine("Overloaded(Integer)")
    'End Sub

    'Sub Overloaded(<CallerLineNumber> Optional number As String = "0")
    '    System.Console.WriteLine("Overloaded(Byte)")
    'End Sub

    Function CallerInfo(
        context As String,
        <CallerLineNumber> Optional number As Integer = -1,
        <CallerMemberName> Optional member As String = "UnknownMember",
        <CallerFilePath> Optional file As String = "UnknownFile"
    ) As Integer

        System.Console.WriteLine("CallerInfo <{0}>", context)
        System.Console.WriteLine("    {0}", file)
        System.Console.WriteLine("    {0}", number)
        System.Console.WriteLine("    {0}", member)

        Return number
    End Function

    Function CallerInfo(
        <CallerLineNumber> Optional number As Integer = -1,
        <CallerMemberName> Optional member As String = "UnknownMember",
        <CallerFilePath> Optional file As String = "UnknownFile"
    ) As Integer

        System.Console.WriteLine("CallerInfo")
        System.Console.WriteLine("    {0}", file)
        System.Console.WriteLine("    {0}", number)
        System.Console.WriteLine("    {0}", member)

        Return number
    End Function

    Public Function GetEnumerator() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class


Class Enumerable
    Function GetEnumerator(
        <CallerLineNumber> Optional number As Integer = -1,
        <CallerMemberName> Optional member As String = "UnknownMember",
        <CallerFilePath> Optional file As String = "UnknownFile"
    ) As Enumerator

        System.Console.WriteLine("GetEnumerator")
        System.Console.WriteLine("    {0}", file)
        System.Console.WriteLine("    {0}", number)
        System.Console.WriteLine("    {0}", member)

        Return New Enumerator()
    End Function

End Class

Class Enumerator
    Private eof As Boolean

    Function MoveNext(
        <CallerLineNumber> Optional number As Integer = -1,
        <CallerMemberName> Optional member As String = "UnknownMember",
        <CallerFilePath> Optional file As String = "UnknownFile"
    ) As Boolean

        System.Console.WriteLine("MoveNext")
        System.Console.WriteLine("    {0}", file)
        System.Console.WriteLine("    {0}", number)
        System.Console.WriteLine("    {0}", member)

        Dim result = Not eof
        eof = True
        Return result
    End Function

    ReadOnly Property Current As String
        Get
            Return "A"
        End Get
    End Property

End Class


'<MyAttribute>
Module Module1

    '<MyAttribute>
    Sub Main()
        System.Console.WriteLine("- 01 -")
        Dim t01 As New TestBase()
        System.Console.WriteLine("- 02 -")
        Dim t02 As New TestBase() With {.P0 = 1}
        System.Console.WriteLine("- 03 -")
        Dim t03 = New TestBase()
        System.Console.WriteLine("- 04 -")
        Dim t1 As New TestDerived() From
                                        {"Val1"}
        System.Console.WriteLine("- 05 -")
        System.Console.WriteLine("F1 = {0}", t1.F1)
        System.Console.WriteLine("- 06 -")
        Dim t2 As New TestDerived(2)
        System.Console.WriteLine("- 07 -")
        System.Console.WriteLine("F1 = {0}", t2.F1)
        System.Console.WriteLine("- 08 -")
        t2.M1()
        System.Console.WriteLine("- 09 -")
        t2.P2 = 1
        System.Console.WriteLine("- 10 -")
        Dim x = t2.P2
        System.Console.WriteLine("- 11 -")
        AddHandler t2.E1, Nothing
        System.Console.WriteLine("- 12 -")
        RemoveHandler t2.E1, Nothing
        System.Console.WriteLine("- 13 -")
        t2.Raise()
        System.Console.WriteLine("- 14 -")

        For Each value In New Enumerable()
            System.Console.WriteLine(value)
        Next

        System.Console.WriteLine("- 15 -")
        x = t2.CallerInfo
        't2.Overloaded()

    End Sub
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
     expectedOutput:=
            <![CDATA[
- 01 -
TestBase.New
    a.vb
    185
    Main
- 02 -
TestBase.New
    a.vb
    187
    Main
- 03 -
TestBase.New
    a.vb
    189
    Main
- 04 -
TestBase.New
    UnknownFile
    -1
    UnknownMember
CallerInfo <F1>
    a.vb
    38
    F1
CallerInfo <P1>
    a.vb
    40
    P1
+++ TestDerived.New()
CallerInfo <TestDerived.New()>
    a.vb
    34
    .ctor
--- TestDerived.New()
Add <Val1>
    a.vb
    192
    Main
- 05 -
F1 = 38
- 06 -
TestBase.New
    a.vb
    27
    .ctor
CallerInfo <F1>
    a.vb
    38
    F1
CallerInfo <P1>
    a.vb
    40
    P1
+++ TestDerived.New(Integer)
--- TestDerived.New(Integer)
- 07 -
F1 = 38
- 08 -
CallerInfo <M1>
    a.vb
    43
    M1
- 09 -
CallerInfo <set_P2>
    a.vb
    51
    P2
- 10 -
CallerInfo <get_P2>
    a.vb
    48
    P2
- 11 -
CallerInfo <AddHandler>
    a.vb
    57
    E1
- 12 -
CallerInfo <RemoveHandler>
    a.vb
    60
    E1
- 13 -
CallerInfo <RaiseEvent>
    a.vb
    63
    E1
- 14 -
GetEnumerator
    a.vb
    213
    Main
MoveNext
    a.vb
    213
    Main
A
MoveNext
    a.vb
    213
    Main
- 15 -
CallerInfo
    a.vb
    218
    Main
]]>)
        End Sub

        <Fact()>
        Public Sub CallerInfo2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Runtime.CompilerServices

Class TestDerived

    Sub Overloaded(<CallerLineNumber> Optional number As Integer = -1)
        System.Console.WriteLine("Overloaded(Integer)")
    End Sub

    Sub Overloaded(<CallerLineNumber> Optional number As String = "0")
        System.Console.WriteLine("Overloaded(Byte)")
    End Sub

End Class

'<MyAttribute>
Module Module1
    '<MyAttribute>
    Sub Main()
        Dim t1 As New TestDerived()
        t1.Overloaded()
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Overloaded' is most specific for these arguments:
    'Public Sub Overloaded([number As Integer = -1])': Not most specific.
    'Public Sub Overloaded([number As String = "0"])': Not most specific.
        t1.Overloaded()
           ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CallerInfo3()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices


<MyAttribute>
Module Module1

    <MyAttribute>
    Sub Main()
        Dim attribute = GetType(MyAttribute)
        Dim output = New List(Of String)

        For Each t In GetType(Module1).Assembly.GetTypes()
            For Each a As MyAttribute In t.GetCustomAttributes(attribute, False)
                output.Add(String.Format("{0} - {1}, {2}, {3}", t, a.X, a.Y, a.Z))
            Next

            For Each m In t.GetMembers(Reflection.BindingFlags.DeclaredOnly Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.Instance)
                For Each a As MyAttribute In m.GetCustomAttributes(attribute, False)
                    output.Add(String.Format("{0} - {1}, {2}, {3}", m, a.X, a.Y, a.Z))
                Next

                If m.MemberType = Reflection.MemberTypes.Method Then
                    Dim method = DirectCast(m, System.Reflection.MethodInfo)

                    For Each p In method.GetParameters()
                        For Each a As MyAttribute In p.GetCustomAttributes(attribute, False)
                            output.Add(String.Format("{4} {0} - {1}, {2}, {3}", p, a.X, a.Y, a.Z, method))
                        Next
                    Next

                    If method.ReturnParameter IsNot Nothing Then
                        Dim p = method.ReturnParameter
                        For Each a As MyAttribute In p.GetCustomAttributes(attribute, False)
                            output.Add(String.Format("{4} {0} - {1}, {2}, {3}", p, a.X, a.Y, a.Z, method))
                        Next
                    End If
                End If
            Next
        Next

        output.Sort()

        For Each s In output
            System.Console.WriteLine(s)
        Next

    End Sub

    Function Test1(<MyAttribute> x As Integer, <MyAttribute> y As Integer) As <MyAttribute> Integer
        Return 0
    End Function

    <MyAttribute>
    Property P1 As Integer

    Property P2 As <MyAttribute> Integer
        <MyAttribute>
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property

    <MyAttribute>
    Event E1 As Action

    <MyAttribute>
    Custom Event E2 As Action
        AddHandler(value As Action)

        End AddHandler

        RemoveHandler(value As Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    Custom Event E3 As Action
        <MyAttribute>
        AddHandler(value As Action)

        End AddHandler

        RemoveHandler(value As Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    <MyAttribute>
    Public F1 As Integer
End Module
]]>
    </file>
</compilation>

            Dim attributeSource =
<compilation>
    <file name="b.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class MyAttribute
    Inherits System.Attribute

    Public ReadOnly X As Integer
    Public ReadOnly Y As String
    Public ReadOnly Z As String

    Sub New(
        <CallerLineNumber> Optional x As Integer = -1,
        <CallerMemberName> Optional y As String = "UnknownMember",
        <CallerFilePath> Optional z As String = "UnknownPath"
    )
        Me.X = x
        Me.Y = y
        Me.Z = z
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim expectedOutput =
            <![CDATA[
Int32 F1 - 102, F1, a.vb
Int32 get_P2() - 60, P2, a.vb
Int32 get_P2() Int32  - 59, P2, a.vb
Int32 P1 - 56, P1, a.vb
Int32 Test1(Int32, Int32) Int32  - 52, Test1, a.vb
Int32 Test1(Int32, Int32) Int32 x - 52, Test1, a.vb
Int32 Test1(Int32, Int32) Int32 y - 52, Test1, a.vb
Module1 - 7, UnknownMember, a.vb
System.Action E1 - 69, E1, a.vb
System.Action E2 - 72, E2, a.vb
Void add_E3(System.Action) - 88, E3, a.vb
Void Main() - 10, Main, a.vb
]]>

            Dim attributeCompilation = CreateCompilationWithReferences(attributeSource, {MscorlibRef_v4_0_30316_17626}, TestOptions.ReleaseDll)
            CompileAndVerify(attributeCompilation)

            Dim compilation = CreateCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef, New VisualBasicCompilationReference(attributeCompilation)}, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput)

            compilation = CreateCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef, MetadataReference.CreateFromImage(attributeCompilation.EmitToArray())}, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput)
        End Sub

        <Fact()>
        Public Sub CallerInfo4()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices


<MyAttribute>
Module Module1

    <MyAttribute>
    Sub Main()
        Dim attribute = GetType(MyAttribute)
        Dim output = New List(Of String)

        For Each t In attribute.Assembly.GetTypes()
            For Each a As MyAttribute In t.GetCustomAttributes(attribute, False)
                output.Add(String.Format("{0} - {1}, {2}, {3}", t, a.X, a.Y, a.Z))
            Next

            For Each m In t.GetMembers(Reflection.BindingFlags.DeclaredOnly Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.Instance)
                For Each a As MyAttribute In m.GetCustomAttributes(attribute, False)
                    output.Add(String.Format("{0} - {1}, {2}, {3}", m, a.X, a.Y, a.Z))
                Next

                If m.MemberType = Reflection.MemberTypes.Method Then
                    Dim method = DirectCast(m, System.Reflection.MethodInfo)

                    For Each p In method.GetParameters()
                        For Each a As MyAttribute In p.GetCustomAttributes(attribute, False)
                            output.Add(String.Format("{4} {0} - {1}, {2}, {3}", p, a.X, a.Y, a.Z, method))
                        Next
                    Next

                    If method.ReturnParameter IsNot Nothing Then
                        Dim p = method.ReturnParameter
                        For Each a As MyAttribute In p.GetCustomAttributes(attribute, False)
                            output.Add(String.Format("{4} {0} - {1}, {2}, {3}", p, a.X, a.Y, a.Z, method))
                        Next
                    End If
                End If
            Next
        Next

        output.Sort()

        For Each s In output
            System.Console.WriteLine(s)
        Next

    End Sub

    Function Test1(<MyAttribute> x As Integer, <MyAttribute> y As Integer) As <MyAttribute> Integer
        Return 0
    End Function

    <MyAttribute>
    Property P1 As Integer

    Property P2 As <MyAttribute> Integer
        <MyAttribute>
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property

    <MyAttribute>
    Event E1 As Action

    <MyAttribute>
    Custom Event E2 As Action
        AddHandler(value As Action)

        End AddHandler

        RemoveHandler(value As Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    Custom Event E3 As Action
        <MyAttribute>
        AddHandler(value As Action)

        End AddHandler

        RemoveHandler(value As Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    <MyAttribute>
    Public F1 As Integer
End Module

Public Class MyAttribute
    Inherits System.Attribute

    Public ReadOnly X As Integer
    Public ReadOnly Y As String
    Public ReadOnly Z As String

    Sub New(
        <CallerLineNumber> Optional x As Integer = -1,
        <CallerMemberName> Optional y As String = "UnknownMember",
        <CallerFilePath> Optional z As String = "UnknownPath"
    )
        Me.X = x
        Me.Y = y
        Me.Z = z
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim expectedOutput =
            <![CDATA[
Int32 F1 - 102, F1, a.vb
Int32 get_P2() - 60, P2, a.vb
Int32 get_P2() Int32  - 59, P2, a.vb
Int32 P1 - 56, P1, a.vb
Int32 Test1(Int32, Int32) Int32  - 52, Test1, a.vb
Int32 Test1(Int32, Int32) Int32 x - 52, Test1, a.vb
Int32 Test1(Int32, Int32) Int32 y - 52, Test1, a.vb
Module1 - 7, UnknownMember, a.vb
System.Action E1 - 69, E1, a.vb
System.Action E2 - 72, E2, a.vb
Void add_E3(System.Action) - 88, E3, a.vb
Void Main() - 10, Main, a.vb
]]>

            Dim compilation = CreateCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef}, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput)
        End Sub

        <WorkItem(1040287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040287")>
        <Fact()>
        Public Sub CallerInfo5()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Class C
    ReadOnly Property P As C
        Get
            Return Me
        End Get
    End Property
    Default ReadOnly Property Q(index As Integer, <CallerLineNumber> Optional line As Integer = 0) As C
        Get
            Console.WriteLine("{0}: {1}", index, line)
            Return Me
        End Get
    End Property
    Function F(Optional id As Integer = 0, <CallerLineNumber> Optional line As Integer = 0) As C
        Console.WriteLine("{0}: {1}", id, line)
        Return Me
    End Function
    Shared Sub Main()
        Dim c = New C()
        c.F(1).
          F
        c = c(
           2
          )(3)
        c = c.
          F(
           4
          ).
          P(5)
        Dim o As Object = c
        o =
          DirectCast(o, C)(
           6
          )
    End Sub
End Class
	]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation,
            <![CDATA[
1: 21
0: 22
2: 23
3: 23
4: 27
5: 30
6: 33
]]>)
        End Sub

        <WorkItem(1040287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040287")>
        <Fact()>
        Public Sub CallerInfo6()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Class C
    Function F() As C
        Return Me
    End Function
    Default ReadOnly Property P(s As String, <CallerLineNumber> Optional line As Integer = 0) As C
        Get
            Console.WriteLine("{0}: {1}", s, line)
            Return Me
        End Get
    End Property
    Shared Sub Main()
        Dim c = (New C())!x.
            F()!y
    End Sub
End Class
	]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation,
            <![CDATA[
x: 14
y: 15
]]>)
        End Sub

        <Fact()>
        Public Sub CallerInfo7()
            Dim compilation1 = CreateCSharpCompilation(<![CDATA[
using System.Runtime.CompilerServices;
public delegate void D(object o = null, [CallerLineNumber]int line = 0);
]]>.Value,
                assemblyName:="1",
                referencedAssemblies:=New MetadataReference() {MscorlibRef_v4_0_30316_17626})
            compilation1.VerifyDiagnostics()
            Dim reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim compilation2 = CreateCompilationWithMscorlib45AndVBRuntime(
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Class C
    Shared Sub M(Optional o As Object = Nothing, <CallerLineNumber> Optional line As Integer = 0)
        Console.WriteLine(line)
    End Sub
    Shared Sub Main()
        Dim d As New D(AddressOf M)
        d(
            1
          )
        d
    End Sub
End Class
	]]>
                    </file>
                </compilation>,
                options:=TestOptions.ReleaseExe,
                additionalRefs:={reference1})
            CompileAndVerify(compilation2,
            <![CDATA[
9
12
]]>)
        End Sub

        <Fact>
        Public Sub TestCallerFilePath1()
            Dim source1 = "
Imports System.Runtime.CompilerServices
Imports System

Partial Module A
    Dim i As Integer

    Sub Log(<CallerFilePath> Optional filePath As String = Nothing)
        i = i + 1
        Console.WriteLine(""{0}: '{1}'"", i, filePath)
    End Sub

    Sub Main()
        Log()
        Main2()
        Main3()
        Main4()
    End Sub
End Module"

            Dim source2 = "
Partial Module A 
    Sub Main2() 
        Log()
    End Sub
End Module
"
            Dim source3 = "
Partial Module A 
    Sub Main3() 
        Log()
    End Sub
End Module
"
            Dim source4 = "
Partial Module A 
    Sub Main4() 
        Log()
    End Sub
End Module
"
            Dim compilation = CreateCompilationWithReferences(
                {
                    SyntaxFactory.ParseSyntaxTree(source1, path:="C:\filename", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source2, path:="a\b\..\c\d", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source3, path:="*", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source4, path:="       ", encoding:=Encoding.UTF8)
                },
                {MscorlibRef_v4_0_30316_17626, MsvbRef},
                TestOptions.ReleaseExe.WithSourceReferenceResolver(SourceFileResolver.Default))

            CompileAndVerify(compilation, expectedOutput:="
1: 'C:\filename'
2: 'a\b\..\c\d'
3: '*'
4: '       '
")
        End Sub

        <Fact>
        Public Sub TestCallerFilePath2()
            Dim source1 = "
Imports System.Runtime.CompilerServices
Imports System

Partial Module A
    Dim i As Integer

    Sub Log(<CallerFilePath> Optional filePath As String = Nothing)
        i = i + 1
        Console.WriteLine(""{0}: '{1}'"", i, filePath)
    End Sub

    Sub Main()
        Log()
        Main2()
        Main3()
        Main4()
        Main5()
    End Sub
End Module"
            Dim source2 = "
Partial Module A 
    Sub Main2() 
        Log()
    End Sub
End Module
"
            Dim source3 = "
#ExternalSource(""make_hidden"", 30)
#End ExternalSource

Partial Module A 
    Sub Main3() 
        Log()
    End Sub
End Module
"
            Dim source4 = "
#ExternalSource(""abc"", 30)

Partial Module A 
    Sub Main4() 
        Log()
    End Sub
End Module

#End ExternalSource
"
            Dim source5 = "
#ExternalSource(""     "", 30)

Partial Module A 
    Sub Main5() 
        Log()
    End Sub
End Module

#End ExternalSource
"

            Dim compilation = CreateCompilationWithReferences(
                {
                    SyntaxFactory.ParseSyntaxTree(source1, path:="C:\filename", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source2, path:="a\b\..\c\d.vb", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source3, path:="*", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source4, path:="C:\x.vb", encoding:=Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source5, path:="C:\x.vb", encoding:=Encoding.UTF8)
                },
                {MscorlibRef_v4_0_30316_17626, MsvbRef},
                TestOptions.ReleaseExe.WithSourceReferenceResolver(New SourceFileResolver(
                    searchPaths:=ImmutableArray(Of String).Empty,
                    baseDirectory:="C:\A\B",
                    pathMap:=ImmutableArray.Create(New KeyValuePair(Of String, String)("C:", "/X")))))

            CompileAndVerify(compilation, expectedOutput:="
1: '/X/filename'
2: '/X/A/B/a/c/d.vb'
3: '*'
4: '/X/abc'
5: '     '
")
        End Sub

        <WorkItem(623122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623122"), WorkItem(619347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619347")>
        <Fact()>
        Public Sub Bug_619347_623122()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Program
    Public Sub Bug623122(Optional ByVal p3 As Long? = 2)
        System.Console.WriteLine(p3)
    End Sub

    Public Sub Bug619347(Optional x As Char? = "abc")
        System.Console.WriteLine(x)
    End Sub

    Sub Main(args As String())
        Bug623122()
	Bug619347()
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source)
            comp.AssertNoDiagnostics()
            CompileAndVerify(source,
     expectedOutput:=<![CDATA[
2
a
]]>)
            Dim prog As NamedTypeSymbol = comp.GetTypeByMetadataName("Program")
            Dim Bug623122 = DirectCast(prog.GetMembers("Bug623122").Single, MethodSymbol)
            Dim Bug619347 = DirectCast(prog.GetMembers("Bug619347").Single, MethodSymbol)

            Assert.Equal("a"c, Bug619347.Parameters(0).ExplicitDefaultValue)
            Assert.IsType(Of Char)(Bug619347.Parameters(0).ExplicitDefaultValue)
            Assert.Equal(2L, Bug623122.Parameters(0).ExplicitDefaultValue)
            Assert.IsType(Of Long)(Bug623122.Parameters(0).ExplicitDefaultValue)
        End Sub



        <Fact>
        <WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")>
        Public Sub IsOptionalVsHasDefaultValue_PrimitiveStruct()
            Dim source =
<compilation name="TestOptionalOnGenericMethod">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class C
    Public Sub M0(p As Integer)
    End Sub
    Public Sub M1(Optional p As Integer = 0) ' default of type
    End Sub
    Public Sub M2(Optional p As Integer = 1) ' not default of type
    End Sub
    Public Sub M3(<[Optional]> p As Integer) ' no default specified (would be illegal)
    End Sub
    Public Sub M4(<DefaultParameterValue(0)> p As Integer) ' default of type, not optional
    End Sub
    Public Sub M5(<DefaultParameterValue(1)> p As Integer) ' not default of type, not optional
    End Sub
    Public Sub M6(<[Optional]> <DefaultParameterValue(0)> p As Integer) ' default of type, optional
    End Sub
    Public Sub M7(<[Optional]> <DefaultParameterValue(1)> p As Integer) ' not default of type, optional
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim validator As Func(Of Boolean, Action(Of ModuleSymbol)) = Function(isFromSource) _
                Sub([module])
                    Dim methods = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().OfType(Of MethodSymbol)().Where(Function(m) m.MethodKind = MethodKind.Ordinary).ToArray()
                    Assert.Equal(8, methods.Length)

                    Dim parameters = methods.Select(Function(m) m.Parameters.Single()).ToArray()

                    Assert.False(parameters(0).IsOptional)
                    Assert.False(parameters(0).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(0).ExplicitDefaultValue)
                    Assert.Null(parameters(0).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(0).GetAttributes().Length)

                    Assert.True(parameters(1).IsOptional)
                    Assert.True(parameters(1).HasExplicitDefaultValue)
                    Assert.Equal(0, parameters(1).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create(0), parameters(1).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(1).GetAttributes().Length)

                    Assert.True(parameters(2).IsOptional)
                    Assert.True(parameters(2).HasExplicitDefaultValue)
                    Assert.Equal(1, parameters(2).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create(1), parameters(2).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(2).GetAttributes().Length)

                    ' 3 - see below

                    Assert.False(parameters(4).IsOptional)
                    Assert.False(parameters(4).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(4).ExplicitDefaultValue)
                    Assert.Null(parameters(4).ExplicitDefaultConstantValue)
                    Assert.Equal(1, parameters(4).GetAttributes().Length)

                    Assert.False(parameters(5).IsOptional)
                    Assert.False(parameters(5).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(5).ExplicitDefaultValue)
                    Assert.Null(parameters(5).ExplicitDefaultConstantValue)
                    Assert.Equal(1, parameters(5).GetAttributes().Length)

                    If isFromSource Then
                        Assert.False(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(3).GetAttributes().Length)

                        Assert.False(parameters(6).IsOptional)
                        Assert.False(parameters(6).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                        Assert.Null(parameters(6).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(6).GetAttributes().Length)

                        Assert.False(parameters(7).IsOptional)
                        Assert.False(parameters(7).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                        Assert.Null(parameters(7).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(7).GetAttributes().Length)
                    Else
                        Assert.True(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(3).GetAttributes().Length)

                        Assert.True(parameters(6).IsOptional)
                        Assert.False(parameters(6).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                        Assert.Null(parameters(6).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(6).GetAttributes().Length) ' DefaultParameterValue

                        Assert.True(parameters(7).IsOptional)
                        Assert.False(parameters(7).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                        Assert.Null(parameters(7).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(7).GetAttributes().Length) ' DefaultParameterValue
                    End If
                End Sub

            CompileAndVerify(source, {MscorlibRef, SystemRef}, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact>
        <WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")>
        Public Sub IsOptionalVsHasDefaultValue_UserDefinedStruct()
            Dim source =
<compilation name="TestOptionalOnGenericMethod">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class C
    Public Sub M0(p As S)
    End Sub
    Public Sub M1(Optional p As S = Nothing)
    End Sub
    Public Sub M2(<[Optional]> p As S) ' no default specified (would be illegal)
    End Sub
    Public Sub M3(<DefaultParameterValue(Nothing)> p As S) ' default of type, not optional
    End Sub
    Public Sub M4(<[Optional]> <DefaultParameterValue(Nothing)> p As S) ' default of type, optional
    End Sub
End Class

Public Structure S
    Public x As Integer
End Structure
]]>
    </file>
</compilation>

            Dim validator As Func(Of Boolean, Action(Of ModuleSymbol)) = Function(isFromSource) _
                Sub([module])
                    Dim methods = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().OfType(Of MethodSymbol)().Where(Function(m) m.MethodKind = MethodKind.Ordinary).ToArray()
                    Assert.Equal(5, methods.Length)

                    Dim parameters = methods.Select(Function(m) m.Parameters.Single()).ToArray()

                    Assert.False(parameters(0).IsOptional)
                    Assert.False(parameters(0).HasExplicitDefaultValue)
                    Assert.Null(parameters(0).ExplicitDefaultConstantValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(0).ExplicitDefaultValue)
                    Assert.Equal(0, parameters(0).GetAttributes().Length)

                    Assert.True(parameters(1).IsOptional)
                    Assert.True(parameters(1).HasExplicitDefaultValue)
                    Assert.Null(parameters(1).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Null, parameters(1).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(1).GetAttributes().Length)

                    ' 2 - see below

                    Assert.False(parameters(3).IsOptional)
                    Assert.False(parameters(3).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                    Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                    Assert.Equal(1, parameters(3).GetAttributes().Length)

                    If isFromSource Then
                        Assert.False(parameters(2).IsOptional)
                        Assert.False(parameters(2).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(2).ExplicitDefaultValue)
                        Assert.Null(parameters(2).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(2).GetAttributes().Length)

                        Assert.False(parameters(4).IsOptional)
                        Assert.False(parameters(4).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(4).ExplicitDefaultValue)
                        Assert.Null(parameters(4).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(4).GetAttributes().Length)
                    Else
                        Assert.True(parameters(2).IsOptional)
                        Assert.False(parameters(2).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(2).ExplicitDefaultValue)
                        Assert.Null(parameters(2).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(2).GetAttributes().Length)

                        Assert.True(parameters(4).IsOptional)
                        Assert.False(parameters(4).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(4).ExplicitDefaultValue)
                        Assert.Null(parameters(4).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(4).GetAttributes().Length) ' DefaultParameterValue
                    End If
                End Sub

            CompileAndVerify(source, {MscorlibRef, SystemRef}, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact>
        <WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")>
        Public Sub IsOptionalVsHasDefaultValue_String()
            Dim source =
<compilation name="TestOptionalOnGenericMethod">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class C
    Public Sub M0(p As String)
    End Sub
    Public Sub M1(Optional p As String = Nothing) ' default of type
    End Sub
    Public Sub M2(Optional p As String = "A") ' not default of type
    End Sub
    Public Sub M3(<[Optional]> p As String) ' no default specified (would be illegal)
    End Sub
    Public Sub M4(<DefaultParameterValue(Nothing)> p As String) ' default of type, not optional
    End Sub
    Public Sub M5(<DefaultParameterValue("A")> p As String) ' not default of type, not optional
    End Sub
    Public Sub M6(<[Optional]> <DefaultParameterValue(Nothing)> p As String) ' default of type, optional
    End Sub
    Public Sub M7(<[Optional]> <DefaultParameterValue("A")> p As String) ' not default of type, optional
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim validator As Func(Of Boolean, Action(Of ModuleSymbol)) = Function(isFromSource) _
                Sub([module])
                    Dim methods = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().OfType(Of MethodSymbol)().Where(Function(m) m.MethodKind = MethodKind.Ordinary).ToArray()
                    Assert.Equal(8, methods.Length)

                    Dim parameters = methods.Select(Function(m) m.Parameters.Single()).ToArray()

                    Assert.False(parameters(0).IsOptional)
                    Assert.False(parameters(0).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(0).ExplicitDefaultValue)
                    Assert.Null(parameters(0).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(0).GetAttributes().Length)

                    Assert.True(parameters(1).IsOptional)
                    Assert.True(parameters(1).HasExplicitDefaultValue)
                    Assert.Null(parameters(1).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Null, parameters(1).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(1).GetAttributes().Length)

                    Assert.True(parameters(2).IsOptional)
                    Assert.True(parameters(2).HasExplicitDefaultValue)
                    Assert.Equal("A", parameters(2).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create("A"), parameters(2).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(2).GetAttributes().Length)

                    ' 3 - see below

                    Assert.False(parameters(4).IsOptional)
                    Assert.False(parameters(4).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(4).ExplicitDefaultValue)
                    Assert.Null(parameters(4).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(4).GetAttributes().Length)

                    Assert.False(parameters(5).IsOptional)
                    Assert.False(parameters(5).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(5).ExplicitDefaultValue)
                    Assert.Null(parameters(5).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(5).GetAttributes().Length)

                    If isFromSource Then
                        Assert.False(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(3).GetAttributes().Length)

                        Assert.False(parameters(6).IsOptional)
                        Assert.False(parameters(6).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                        Assert.Null(parameters(6).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(6).GetAttributes().Length)

                        Assert.False(parameters(7).IsOptional)
                        Assert.False(parameters(7).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                        Assert.Null(parameters(7).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(7).GetAttributes().Length)
                    Else
                        Assert.True(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(3).GetAttributes().Length)

                        Assert.True(parameters(6).IsOptional)
                        Assert.False(parameters(6).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                        Assert.Null(parameters(6).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(6).GetAttributes().Length)

                        Assert.True(parameters(7).IsOptional)
                        Assert.False(parameters(7).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                        Assert.Null(parameters(7).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(7).GetAttributes().Length)
                    End If
                End Sub

            CompileAndVerify(source, {MscorlibRef, SystemRef}, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact>
        <WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")>
        Public Sub IsOptionalVsHasDefaultValue_Decimal()
            Dim source =
<compilation name="TestOptionalOnGenericMethod">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    Public Sub M0(p As Decimal)
    End Sub
    Public Sub M1(Optional p As Decimal = 0) ' default of type
    End Sub
    Public Sub M2(Optional p As Decimal = 1) ' not default of type
    End Sub
    Public Sub M3(<[Optional]> p As Decimal) ' no default specified (would be illegal)
    End Sub
    Public Sub M4(<DefaultParameterValue(0)> p As Decimal) ' default of type, not optional
    End Sub
    Public Sub M5(<DefaultParameterValue(1)> p As Decimal) ' not default of type, not optional
    End Sub
    Public Sub M6(<[Optional]> <DefaultParameterValue(0)> p As Decimal) ' default of type, optional
    End Sub
    Public Sub M7(<[Optional]> <DefaultParameterValue(1)> p As Decimal) ' not default of type, optional
    End Sub
    Public Sub M8(<DecimalConstant(0, 0, 0, 0, 0)> p As Decimal) ' default of type, not optional
    End Sub
    Public Sub M9(<DecimalConstant(0, 0, 0, 0, 1)> p As Decimal) ' not default of type, not optional
    End Sub
    Public Sub M10(<[Optional]> <DecimalConstant(0, 0, 0, 0, 0)> p As Decimal) ' default of type, optional
    End Sub
    Public Sub M11(<[Optional]> <DecimalConstant(0, 0, 0, 0, 1)> p As Decimal) ' not default of type, optional
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim validator As Func(Of Boolean, Action(Of ModuleSymbol)) = Function(isFromSource) _
                Sub([module])
                    Dim methods = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().OfType(Of MethodSymbol)().Where(Function(m) m.MethodKind = MethodKind.Ordinary).ToArray()
                    Assert.Equal(12, methods.Length)

                    Dim parameters = methods.Select(Function(m) m.Parameters.Single()).ToArray()

                    Dim decimalZero = CType(0, Decimal)
                    Dim decimalOne = CType(1, Decimal)

                    Assert.False(parameters(0).IsOptional)
                    Assert.False(parameters(0).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(0).ExplicitDefaultValue)
                    Assert.Null(parameters(0).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(0).GetAttributes().Length)

                    Assert.True(parameters(1).IsOptional)
                    Assert.True(parameters(1).HasExplicitDefaultValue)
                    Assert.Equal(decimalZero, parameters(1).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create(decimalZero), parameters(1).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(1).GetAttributes().Length)

                    Assert.True(parameters(2).IsOptional)
                    Assert.True(parameters(2).HasExplicitDefaultValue)
                    Assert.Equal(decimalOne, parameters(2).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create(decimalOne), parameters(2).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(2).GetAttributes().Length)

                    ' 3 - see below

                    Assert.False(parameters(4).IsOptional)
                    Assert.False(parameters(4).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(4).ExplicitDefaultValue)
                    Assert.Null(parameters(4).ExplicitDefaultConstantValue)
                    Assert.Equal(1, parameters(4).GetAttributes().Length) ' DefaultParameterValue

                    Assert.False(parameters(5).IsOptional)
                    Assert.False(parameters(5).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(5).ExplicitDefaultValue)
                    Assert.Null(parameters(5).ExplicitDefaultConstantValue)
                    Assert.Equal(1, parameters(5).GetAttributes().Length) ' DefaultParameterValue

                    ' 6 - see below

                    ' 7 - see below

                    Assert.False(parameters(8).IsOptional)
                    Assert.False(parameters(8).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(8).ExplicitDefaultValue)
                    Assert.Null(parameters(8).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(8).GetAttributes().Length) ' DecimalConstantAttribute

                    Assert.False(parameters(9).IsOptional)
                    Assert.False(parameters(9).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(9).ExplicitDefaultValue)
                    Assert.Null(parameters(9).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(9).GetAttributes().Length) ' DecimalConstantAttribute

                    If isFromSource Then
                        Assert.False(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(3).GetAttributes().Length)

                        Assert.False(parameters(6).IsOptional)
                        Assert.False(parameters(6).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                        Assert.Null(parameters(6).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(6).GetAttributes().Length)

                        Assert.False(parameters(7).IsOptional)
                        Assert.False(parameters(7).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                        Assert.Null(parameters(7).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(7).GetAttributes().Length)

                        Assert.False(parameters(10).IsOptional)
                        Assert.False(parameters(10).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(10).ExplicitDefaultValue)
                        Assert.Null(parameters(10).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(10).GetAttributes().Length)

                        Assert.False(parameters(11).IsOptional)
                        Assert.False(parameters(11).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(11).ExplicitDefaultValue)
                        Assert.Null(parameters(11).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(11).GetAttributes().Length)
                    Else
                        Assert.True(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(3).GetAttributes().Length)

                        Assert.True(parameters(6).IsOptional)
                        Assert.False(parameters(6).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                        Assert.Null(parameters(6).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(6).GetAttributes().Length) ' DefaultParameterValue

                        Assert.True(parameters(7).IsOptional)
                        Assert.False(parameters(7).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                        Assert.Null(parameters(7).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(7).GetAttributes().Length) ' DefaultParameterValue

                        Assert.True(parameters(10).IsOptional)
                        Assert.True(parameters(10).HasExplicitDefaultValue)
                        Assert.Equal(decimalZero, parameters(10).ExplicitDefaultValue)
                        Assert.Equal(ConstantValue.Create(decimalZero), parameters(10).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(10).GetAttributes().Length)

                        Assert.True(parameters(11).IsOptional)
                        Assert.True(parameters(11).HasExplicitDefaultValue)
                        Assert.Equal(decimalOne, parameters(11).ExplicitDefaultValue)
                        Assert.Equal(ConstantValue.Create(decimalOne), parameters(11).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(11).GetAttributes().Length)
                    End If
                End Sub

            CompileAndVerify(source, {MscorlibRef, SystemRef}, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact>
        <WorkItem(529775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529775")>
        Public Sub IsOptionalVsHasDefaultValue_DateTime()
            Dim source =
<compilation name="TestOptionalOnGenericMethod">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    Public Sub M0(p As Date)
    End Sub
    Public Sub M1(Optional p As Date = Nothing) ' default of type
    End Sub
    Public Sub M2(Optional p As Date = #12/25/1991#) ' not default of type
    End Sub
    Public Sub M3(<[Optional]> p As Date) ' no default specified (would be illegal)
    End Sub
    Public Sub M4(<DefaultParameterValue(Nothing)> p As Date) ' default of type, not optional (note: can't use a date literal here)
    End Sub
    Public Sub M5(<[Optional]> <DefaultParameterValue(Nothing)> p As Date) ' default of type, optional
    End Sub
    Public Sub M6(<DateTimeConstant(0)> p As Date) ' default of type, not optional
    End Sub
    Public Sub M7(<DateTimeConstant(1)> p As Date) ' not default of type, not optional
    End Sub
    Public Sub M8(<[Optional]> <DateTimeConstant(0)> p As Date) ' default of type, optional
    End Sub
    Public Sub M9(<[Optional]> <DateTimeConstant(1)> p As Date) ' not default of type, optional
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim validator As Func(Of Boolean, Action(Of ModuleSymbol)) = Function(isFromSource) _
                Sub([module])
                    Dim methods = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().OfType(Of MethodSymbol)().Where(Function(m) m.MethodKind = MethodKind.Ordinary).ToArray()
                    Assert.Equal(10, methods.Length)

                    Dim parameters = methods.Select(Function(m) m.Parameters.Single()).ToArray()

                    Dim dateTimeZero = New DateTime(0)
                    Dim dateTimeOne = New DateTime(1)
                    Dim dateTimeOther = #12/25/1991#

                    Assert.False(parameters(0).IsOptional)
                    Assert.False(parameters(0).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(0).ExplicitDefaultValue)
                    Assert.Null(parameters(0).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(0).GetAttributes().Length)

                    Assert.True(parameters(1).IsOptional)
                    Assert.True(parameters(1).HasExplicitDefaultValue)
                    Assert.Equal(dateTimeZero, parameters(1).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create(dateTimeZero), parameters(1).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(1).GetAttributes().Length)

                    Assert.True(parameters(2).IsOptional)
                    Assert.True(parameters(2).HasExplicitDefaultValue)
                    Assert.Equal(dateTimeOther, parameters(2).ExplicitDefaultValue)
                    Assert.Equal(ConstantValue.Create(dateTimeOther), parameters(2).ExplicitDefaultConstantValue)
                    Assert.Equal(0, parameters(2).GetAttributes().Length)

                    ' 3 - see below

                    Assert.False(parameters(4).IsOptional)
                    Assert.False(parameters(4).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(4).ExplicitDefaultValue)
                    Assert.Null(parameters(4).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(4).GetAttributes().Length) ' DefaultParameterValue

                    ' 5 - see below

                    Assert.False(parameters(6).IsOptional)
                    Assert.False(parameters(6).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(6).ExplicitDefaultValue)
                    Assert.Null(parameters(6).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(6).GetAttributes().Length) ' DateTimeConstant

                    Assert.False(parameters(7).IsOptional)
                    Assert.False(parameters(7).HasExplicitDefaultValue)
                    Assert.Throws(Of InvalidOperationException)(Function() parameters(7).ExplicitDefaultValue)
                    Assert.Null(parameters(7).ExplicitDefaultConstantValue) ' not imported for non-optional parameter
                    Assert.Equal(1, parameters(7).GetAttributes().Length) ' DateTimeConstant

                    If isFromSource Then
                        Assert.False(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(3).GetAttributes().Length)

                        Assert.False(parameters(5).IsOptional)
                        Assert.False(parameters(5).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(5).ExplicitDefaultValue)
                        Assert.Null(parameters(5).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(5).GetAttributes().Length)

                        Assert.False(parameters(8).IsOptional)
                        Assert.False(parameters(8).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(8).ExplicitDefaultValue)
                        Assert.Null(parameters(8).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(8).GetAttributes().Length)

                        Assert.False(parameters(9).IsOptional)
                        Assert.False(parameters(9).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(9).ExplicitDefaultValue)
                        Assert.Null(parameters(9).ExplicitDefaultConstantValue)
                        Assert.Equal(2, parameters(9).GetAttributes().Length)
                    Else
                        Assert.True(parameters(3).IsOptional)
                        Assert.False(parameters(3).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(3).ExplicitDefaultValue)
                        Assert.Null(parameters(3).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(3).GetAttributes().Length)

                        Assert.True(parameters(5).IsOptional)
                        Assert.False(parameters(5).HasExplicitDefaultValue)
                        Assert.Throws(Of InvalidOperationException)(Function() parameters(5).ExplicitDefaultValue)
                        Assert.Null(parameters(5).ExplicitDefaultConstantValue)
                        Assert.Equal(1, parameters(5).GetAttributes().Length) ' DefaultParameterValue

                        Assert.True(parameters(8).IsOptional)
                        Assert.True(parameters(8).HasExplicitDefaultValue)
                        Assert.Equal(dateTimeZero, parameters(8).ExplicitDefaultValue)
                        Assert.Equal(ConstantValue.Create(dateTimeZero), parameters(8).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(8).GetAttributes().Length)

                        Assert.True(parameters(9).IsOptional)
                        Assert.True(parameters(9).HasExplicitDefaultValue)
                        Assert.Equal(dateTimeOne, parameters(9).ExplicitDefaultValue)
                        Assert.Equal(ConstantValue.Create(dateTimeOne), parameters(9).ExplicitDefaultConstantValue)
                        Assert.Equal(0, parameters(9).GetAttributes().Length)
                    End If
                End Sub

            CompileAndVerify(source, {MscorlibRef, SystemRef}, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

    End Class
End Namespace

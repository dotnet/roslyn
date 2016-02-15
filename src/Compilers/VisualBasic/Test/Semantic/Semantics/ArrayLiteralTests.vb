' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
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

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ArrayLiteralTests
        Inherits BasicTestBase

        Private ReadOnly _strictOff As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off)
        Private ReadOnly _strictOn As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On)
        Private ReadOnly _strictCustom As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom)

        <Fact()>
        Public Sub TestArrayLiteralInferredType()
            Dim source =
<compilation name="TestArrayLiteralInferredType">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program

    Sub Main(args As String())
        dim i1 = {1,2}                          '
        Console.WriteLine(i1.GetType)

        dim i2 = {{1},{2}}
        Console.WriteLine(i2.GetType)

        dim i3 = {1D}
        Console.WriteLine(i3.GetType)

        dim i4 ={({1,2}), ({3, 4})}
        Console.WriteLine(i4.GetType)
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
System.Int32[]
System.Int32[,]
System.Decimal[]
System.Int32[][]
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
System.Int32[]
System.Int32[,]
System.Decimal[]
System.Int32[][]
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestEmptyArrayLiteral()
            Dim source =
<compilation name="TestEmptyArrayLiteral">
    <file name="a.vb">
        <![CDATA[
Module Program

    Sub Main(args As String())
        dim i1 = {}         ' error if option strict is on
        dim i2 = {{}}       ' error if option strict is on
        dim i3(,) = {}      ' error if option strict is on
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
                Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{{}}"),
                Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}")
                )

        End Sub

        <Fact()>
        Public Sub TestNestedEmptyArrayLiteralWithObject()
            Dim source =
<compilation name="TestNestedEmptyArrayLiteral">
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic
Module Program
    Class c
    End Class

    Sub S(o()() As Object)
    End Sub

    Sub Main(args As String())

        Dim i0 = {({})}                 ' should report an error with option strict
        Dim i1 As Object = {({})}       ' should report an error with option strict
        Dim i2 As Object() = {({})}     ' should report an error with option strict
        Dim i3 As Object()() = {({})}

        Dim j0 = {}                     ' should report an error with option strict
        Dim j1 As Object = {}           ' should report an error with option strict
        Dim j11() As Object = {}
        Dim j3(,) As Object = {}
        Dim j31(,) = {}                 ' should report an error with option strict
        Dim j4()() As Object = {}
        Dim j5() = {"a", New c}         ' should report an error with option strict

        S({({})})                       ' should report and error with option strict

        Dim ie1 As IEnumerable(Of Object) = {}
        Dim ie2 As IEnumerable(Of Object) = {({})} ' should report an error with option strict
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
                               ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{""a"", New c}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}"),
    Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{}")
                )

        End Sub

        <Fact()>
        Public Sub TestEmptyArrayLiteralWithTargetType()
            Dim source =
<compilation name="TestEmptyArrayLiteralWithTargetType">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program

    Sub Main(args As String())
        dim i1 as Integer() = {} 
        dim i2 as Integer(,) = {}  
        dim i3 as Integer(,) = {{}}
        dim i4 as IEnumerable(of Decimal) = {}
        dim i5 as IList(of Decimal) = {}
        dim i6 as ICollection(of Decimal) = {}
    End Sub

End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWithTargetType()
            Dim source =
<compilation name="TestArrayLiteralWithTargetType">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Program

    Sub Main(args As String())
        dim i1 as IEnumerable = {1}
        dim i2 as IEnumerable(of Double) = {1}
        dim i3 as IList(of Decimal) = {1}
        dim i4 as ICollection(of short) = {1}
        dim i5() as double = {1}
        dim i6(,) as double = {{1}}
        dim i7 as string = {"h"c, "e"c, "l"c, "l"c, "o"c} 'should convert char() to string
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralDimensionMismatch()
            Dim source =
<compilation name="TestArrayLiteralWithTargetType">
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic

Module Program

    Sub Main(args As String())
        dim a1() = {{1}}
        dim a2 = {1, {2}}
        dim a3(,) = {{{1}}, 2}
        dim i2 as IEnumerable(of Double) = {{1}}
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            comp.AssertTheseDiagnostics(
<expected>
BC36909: Cannot infer a data type for 'a1' because the array dimensions do not match.
        dim a1() = {{1}}
            ~~~~
BC30414: Value of type 'Integer(*,*)' cannot be converted to 'Object()' because the array types have different numbers of dimensions.
        dim a1() = {{1}}
                   ~~~~~
BC30566: Array initializer has too many dimensions.
        dim a2 = {1, {2}}
                     ~~~
BC30565: Array initializer has too few dimensions.
        dim a3(,) = {{{1}}, 2}
                            ~
BC30311: Value of type 'Integer(*,*)' cannot be converted to 'IEnumerable(Of Double)'.
        dim i2 as IEnumerable(of Double) = {{1}}
                                           ~~~~~
</expected>
            )
            comp = comp.WithOptions(_strictOn)
            comp.AssertTheseDiagnostics(
<expected>
BC36909: Cannot infer a data type for 'a1' because the array dimensions do not match.
        dim a1() = {{1}}
            ~~~~
BC30414: Value of type 'Integer(*,*)' cannot be converted to 'Object()' because the array types have different numbers of dimensions.
        dim a1() = {{1}}
                   ~~~~~
BC30566: Array initializer has too many dimensions.
        dim a2 = {1, {2}}
                     ~~~
BC30565: Array initializer has too few dimensions.
        dim a3(,) = {{{1}}, 2}
                            ~
BC30311: Value of type 'Integer(*,*)' cannot be converted to 'IEnumerable(Of Double)'.
        dim i2 as IEnumerable(of Double) = {{1}}
                                           ~~~~~
</expected>
            )

        End Sub

        <Fact()>
        Public Sub TestArrayLiteralImplicitWidening()
            Dim source =
<compilation name="TestArrayLiteralImplicitWidening">
    <file name="a.vb">
        <![CDATA[
Module Module1

    Class C
        Public Sub New(i() As Integer)
        End Sub

        Public Shared Widening Operator CType(i() As Integer) As C
            Return nothing
        End Operator
    End Class

    Sub Main()
        Dim c1 as C = {1}
        Dim c2 = DirectCast({1}, C) 'Should fail, user defined conversion should not be applied.
        Dim c3 = TryCast({1}, C)    'Should fail, user defined conversion should not be applied.
    End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            comp.VerifyDiagnostics(
                   Diagnostic(ERRID.ERR_TypeMismatch2, "{1}").WithArguments("Integer()", "Module1.C"),
                   Diagnostic(ERRID.ERR_TypeMismatch2, "{1}").WithArguments("Integer()", "Module1.C"))

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                   Diagnostic(ERRID.ERR_TypeMismatch2, "{1}").WithArguments("Integer()", "Module1.C"),
                   Diagnostic(ERRID.ERR_TypeMismatch2, "{1}").WithArguments("Integer()", "Module1.C"))
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralUserDefinedWideningFromShort()
            Dim source =
<compilation name="TestArrayLiteralUserDefinedWideningFromShort">
    <file name="a.vb">
        <![CDATA[
Module Module1

    Class C
        Public Sub New(i() As Integer)
        End Sub

        Public Shared Widening Operator CType(i() As Integer) As C
            Return nothing
        End Operator
    End Class

    Sub Main()
        Dim c1 as C = {1S}
       End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralUserDefinedWideningFromIntegerInvolvesNarrowingLiteralLong()
            Dim source =
<compilation name="TestArrayLiteralUserDefinedWideningFromIntegerInvolvesNarrowingLiteralLong">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Module1

    Class C
        Public Sub New(i() As Short)
        End Sub

        Public Shared Widening Operator CType(i() As Integer) As C
            Console.WriteLine("using widening operator CType(i() as integer) as C")
            Return nothing
        End Operator
    End Class

    Sub Main()
        Dim c1 as C = {1L} 'Dev10 fails with Option Strict On but Roslyn succeeds. However, Dev10 behavior is wrong.
       End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
using widening operator CType(i() as integer) as C
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
using widening operator CType(i() as integer) as C
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestUserDefinedWideningFromIntegerNarrowingFromString()
            Dim source =
<compilation name="TestUserDefinedWideningFromIntegerNarrowingFromString">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Module1

    Class C
        Public Sub New(i() As Integer)
        End Sub

        Public Shared Widening Operator CType(i() As Integer) As C
            Console.WriteLine("using widening operator CType(i() as integer) as C")
            Return nothing
        End Operator
    End Class

    Sub Main()
        Dim c1 as C = {"1"} 'Narrowing not allowed with option strict on
    End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
            using widening operator CType(i() as integer) as C
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, """1""").WithArguments("String", "Integer"))

        End Sub


        <Fact()>
        Public Sub TestUserDefinedNarrowFromInteger()
            Dim source =
<compilation name="TestUserDefinedNarrowFromInteger">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Module1

    Class C
        Public Sub New(i() As Integer)
        End Sub

        Public Shared Narrowing Operator CType(i() As Integer) As C
            Console.WriteLine("using narrowing operator CType(i() as integer) as C")
            Return nothing
        End Operator
    End Class

    Sub Main()
        Dim c1 as C = {1S} 'Should fail when option strict is on
        Dim c2 as C = {1} 'Should fail when option strict is on
        Dim c3 as C = {1L} 'Should fail when option strict is on
        Dim c4 as C = {1D} 'Should fail when option strict is on
        Dim c5 as C = {1.0R} 'Should fail when option strict is on
    End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
using narrowing operator CType(i() as integer) as C
using narrowing operator CType(i() as integer) as C
using narrowing operator CType(i() as integer) as C
using narrowing operator CType(i() as integer) as C
using narrowing operator CType(i() as integer) as C
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "{1S}").WithArguments("Integer()", "Module1.C"),
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "{1}").WithArguments("Integer()", "Module1.C"),
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "{1L}").WithArguments("Integer()", "Module1.C"),
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "1D").WithArguments("Decimal", "Integer"),
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "1.0R").WithArguments("Double", "Integer"))

        End Sub

        <Fact>
        Public Sub TestArrayLiteralInferredElementTypeDiagnostics()
            Dim source =
<compilation name="TestArrayLiteralInferredElementTypeDiagnostics">
    <file name="a.vb">
        <![CDATA[
        Imports System

Module Program
    Class c
    End Class

    Sub Main(args As String())
        Dim a As ArgIterator = Nothing
        Dim x = {a}
        Dim y = {1, 1D, "a"}
        Dim z = {"a", New c}
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_RestrictedType1, "= {a}").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedType1, "{a}").WithArguments("System.ArgIterator"))

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_RestrictedType1, "= {a}").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedType1, "{a}").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_ArrayInitTooManyTypesObjectDisallowed, "{1, 1D, ""a""}"),
                Diagnostic(ERRID.ERR_ArrayInitNoTypeObjectDisallowed, "{""a"", New c}"))

            comp = comp.WithOptions(_strictCustom)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_RestrictedType1, "= {a}").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedType1, "{a}").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.WRN_ObjectAssumed1, "{1, 1D, ""a""}").WithArguments("Cannot infer an element type because more than one type is possible; 'Object' assumed."),
                Diagnostic(ERRID.WRN_ObjectAssumed1, "{""a"", New c}").WithArguments("Cannot infer an element type; 'Object' assumed."))
        End Sub

        <Fact(Skip:="529377")>
        Public Sub TestArrayLiteralInferredElementArgIterator()
            Dim source =
<compilation name="TestArrayLiteralInferredElementTypeDiagnostics">
    <file name="a.vb">
        <![CDATA[
        Imports System
Imports System.Collections.Generic

Module Program
    Class c
    End Class

    Sub Main(args As String())
        Dim a As ArgIterator = Nothing
        Dim x as ArgIterator= {a} ' Error should be reported on ArgIterator not the array literal
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics() ' Error should be reported on ArgIterator not the array literal

            comp = comp.WithOptions(_strictCustom)
            comp.VerifyDiagnostics() ' Error should be reported on ArgIterator not the array literal

        End Sub

        <Fact()>
        Public Sub TestArrayLiteralInDirectCast()
            Dim source =
<compilation name="TestArrayLiteralWithCast">
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        Dim x1 = DirectCast({1S}, Integer())
        Dim x2 = Directcast({1}, Integer())
        Dim x3 = Directcast({1D}, Integer())
        Dim x4 = DirectCast({1}, Decimal())
        Dim x5 = Directcast({"a"c, "b"c}, string)
     End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            CompileAndVerify(comp,
     expectedOutput:=<![CDATA[
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "1D").WithArguments("Decimal", "Integer")
                       )
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWithAddressOfActionUndefined()
            Dim source =
<compilation name="TestArrayLiteralWithAddressOfActionUndefined">
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        Dim b = {Addressof Main}
        Dim a As Action() = {AddressOf Main} 'Action is undefined
    End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ArrayInitNoType, "{Addressof Main}"),
                Diagnostic(ERRID.ERR_AddressOfNotDelegate1, "Addressof Main").WithArguments("Object"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Action").WithArguments("Action")
                )

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ArrayInitNoType, "{Addressof Main}"),
                Diagnostic(ERRID.ERR_AddressOfNotDelegate1, "Addressof Main").WithArguments("Object"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Action").WithArguments("Action")
                )
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWithAddressOf()
            Dim source =
<compilation name="TestArrayLiteralWithAddressOf">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Module1

    Sub Main()
        Dim b = {Addressof Main}
        Dim a As Action() = {AddressOf Main} 'Action is undefined
    End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ArrayInitNoType, "{Addressof Main}"),
                Diagnostic(ERRID.ERR_AddressOfNotDelegate1, "Addressof Main").WithArguments("Object")
                )

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_ArrayInitNoType, "{Addressof Main}"),
                    Diagnostic(ERRID.ERR_AddressOfNotDelegate1, "Addressof Main").WithArguments("Object")
                       )
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralNarrowingConversion()
            Dim source =
<compilation name="TestArrayLiteralNarrowingConversion">
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Class C
        Public Shared Narrowing Operator CType(ByVal x As Integer()) As C
            Console.WriteLine("Public Shared Narrowing Operator CType(ByVal x As Integer()) As C")
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(ByVal x As C) As Integer()
            Console.WriteLine("Public Shared Narrowing Operator CType(ByVal x As C) As Integer()")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        'Both Narrowing
        Dim c1 As New C
        c1 = {1, 2, 3, 4, 5} '//UD Conversion
    End Sub

End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)

            CompileAndVerify(comp,
          expectedOutput:=<![CDATA[
Public Shared Narrowing Operator CType(ByVal x As Integer()) As C
                             ]]>)


            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "{1, 2, 3, 4, 5}").WithArguments("Integer()", "Module1.C")
                )
        End Sub


        <Fact()>
        Public Sub TestArrayLiteralInvolvesNarrowingFromNumericConstantAllWidening()
            'All conversions in the array literal are narrowing with InvolvesNarrowingFromNumericConstant
            'Because the second argument is identity (byte) test2 should resolve to Sub Test2(x As Short(), y As Byte)
            Dim source =
<compilation name="TestArrayLiteralInvolvesNarrowingFromNumericConstantAllWidening">
    <file name="a.vb">
        <![CDATA[
Module m
    Sub Test2(x As Integer(), y As Integer)
        System.Console.WriteLine("Test2(x As Integer(), y As Integer)")
    End Sub

    Sub Test2(x As Byte(), y As Byte)
        System.Console.WriteLine("Test2(x As Byte(), y As Byte)")
    End Sub

    Sub Main()
        Dim b As Byte = 1
        Test2({&H7FFFFFFFL, &H7FFFFFFFL, &H7FFFFFFFL}, b) 
    End Sub

End Module
]]></file>
</compilation>

            Dim strictOffOverflowChecksOff = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=strictOffOverflowChecksOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Test2(x As Byte(), y As Byte)
            ]]>)

            Dim strictOnOverflowChecksOff = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On).WithOverflowChecks(False)

            comp = comp.WithOptions(strictOnOverflowChecksOff)
            CompileAndVerify(comp,
                            expectedOutput:=<![CDATA[
Test2(x As Byte(), y As Byte)
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralExactlyOneInvolvesNarrowingFromNumericConstantAndWidening()
            'The conversion in the array literal is narrowing with InvolvesNarrowingFromNumericConstant. The others are widening.
            'Because the second argument is identity (byte) test2 should resolve to Sub Test2(x As Short(), y As Byte)
            Dim source =
<compilation name="TestArrayLiteralExactlyOneInvolvesNarrowingFromNumericConstantAndWidening">
    <file name="a.vb">
        <![CDATA[
Module m
    Sub Test2(x As Integer(), y As Integer)
        System.Console.WriteLine("Test2(x As Integer(), y As Integer)")
    End Sub

    Sub Test2(x As Short(), y As Byte)
        System.Console.WriteLine("Test2(x As Short(), y As Byte)")
    End Sub

    Sub Main()
        Dim b As Byte = 1
        Test2({1S, 2S, &H7FFFFFFFL, 3S}, b) 
    End Sub

End Module
]]></file>
</compilation>

            Dim strictOffOverflowChecksOff = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=strictOffOverflowChecksOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Test2(x As Short(), y As Byte)
            ]]>)

            Dim strictOnOverflowChecksOff = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On).WithOverflowChecks(False)

            comp = comp.WithOptions(strictOnOverflowChecksOff)
            CompileAndVerify(comp,
                            expectedOutput:=<![CDATA[
Test2(x As Short(), y As Byte)
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralExactlyInvolvesNarrowingFromNumericConstantAndNarrowing()
            'All conversions in the array literal are widening, one is narrowing with InvolvesNarrowingFromNumericConstant and one is narrowing
            'Because one narrowing conversion lacks InvolvesNarrowingFromNumericConstant, this fails overload resolution.
            Dim source =
<compilation name="TestArrayLiteralExactlyInvolvesNarrowingFromNumericConstantAndNarrowing">
    <file name="a.vb">
        <![CDATA[
Module m
    Sub Test2(x As Integer(), y As Integer)
        System.Console.WriteLine("Test2(x As Integer(), y As Integer)")
    End Sub

    Sub Test2(x As Short(), y As Byte)
        System.Console.WriteLine("Test2(x As Short(), y As Byte)")
    End Sub

    Sub Main()
        Dim b As Byte = 1
        Test2({1S, 2S, &H7FFFFFFFL, 1.0}, b) 
    End Sub

End Module
]]></file>
</compilation>

            Dim strictOffOverflowChecksOff = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=strictOffOverflowChecksOff)
            comp.VerifyDiagnostics(
                   Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Test2").WithArguments("Test2", vbCrLf &
            "    'Public Sub Test2(x As Integer(), y As Integer)': Argument matching parameter 'x' narrows to 'Integer()'." & vbCrLf &
            "    'Public Sub Test2(x As Short(), y As Byte)': Argument matching parameter 'x' narrows to 'Short()'."))

            Dim strictOnOverflowChecksOff = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On).WithOverflowChecks(False)

            comp = comp.WithOptions(strictOnOverflowChecksOff)
            comp.VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_NoCallableOverloadCandidates2, "Test2").WithArguments("Test2", vbCrLf &
          "    'Public Sub Test2(x As Integer(), y As Integer)': Option Strict On disallows implicit conversions from 'Double' to 'Integer'." & vbCrLf &
          "    'Public Sub Test2(x As Short(), y As Byte)': Option Strict On disallows implicit conversions from 'Double' to 'Short'."))
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWithNarrowing1()
            Dim source =
<compilation name="TestArrayLiteralWithNarrowing1">
    <file name="a.vb">
        <![CDATA[
Module Program

    Sub Main(args As String())
        dim s0() as short = {1000, 1000L} 'Pass Both
        dim s1() as short = {1000, 1000L, 1000.0} 'Pass Option Strict Off/Fail Option Strict On
        dim s2() as short = {1000, 1000L, "a"c} 'Fail Both
        dim s3() as short = {1000, 1000L, "a"}  'Pass Option Strict Off/Fail Option Strict On
    End Sub
End Module
]]></file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, """a""c").WithArguments("Short"))

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "1000.0").WithArguments("Double", "Short"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, """a""c").WithArguments("Short"),
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, """a""").WithArguments("String", "Short"))
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWithNarrowing2()
            Dim source =
<compilation name="TestArrayLiteralWithNarrowing2">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Module1

    Class C
        Public Sub New(i() As Short)
        End Sub

        Public Shared Narrowing Operator CType(i() As Short) As C
            Console.WriteLine("Public Shared Narrowing Operator CType(i() As Short) As C")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim c1 As C = {10S, 10, 10L} 
    End Sub

End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Public Shared Narrowing Operator CType(i() As Short) As C
            ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                     Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "{10S, 10, 10L}").WithArguments("Short()", "Module1.C")
               )
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWideningUserDefinedWithNarrowingLiteral1()
            Dim source =
<compilation name="TestArrayLiteralWithNarrowing2">
    <file name="a.vb">
        <![CDATA[
Module m
    Class B1
        Shared Widening Operator CType(x As Short()) As B1
            Return Nothing
        End Operator
    End Class

    Class B2
        Shared Widening Operator CType(x As Byte()) As B2
            Return Nothing
        End Operator
    End Class

    Sub Test2(x As B1, y As Integer)
        System.Console.WriteLine("Test2(x As B1)")
    End Sub

    Sub Test2(x As B2, y As Byte)
        System.Console.WriteLine("Test2(x As B2)")
    End Sub

    Sub Main()
        Dim i As Integer = 1
        Dim b As Byte = 0
        Test2({1}, b)         ' differs from dev10 - dev10 reports error because it fails to apply user defined conversion for array literal.  
        Test2({1S}, b)        ' differs from dev10 - binds to B2, Dev10 binds to B1.
        Test2({CShort(i)}, b) 
    End Sub
End Module
]]>
    </file>
</compilation>
            ' See comments above.  Two of the calls differ from Dev10.  Roslyn considers user defined conversions for array literal arguments
            ' when doing overload resolution.  Dev10 does not.

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Test2(x As B2)
Test2(x As B2)
Test2(x As B1)
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Test2(x As B2)
Test2(x As B2)
Test2(x As B1)
            ]]>)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralWideningUserDefinedWithNarrowingLiteral2()
            Dim source =
<compilation name="TestArrayLiteralWithNarrowing2">
    <file name="a.vb">
        <![CDATA[
Module m
    Class B1
        Shared Widening Operator CType(x As Short()) As B1
            Return Nothing
        End Operator
    End Class

    Class B2
        Shared Widening Operator CType(x As Byte()) As B2
            Return Nothing
        End Operator
    End Class

    Sub Test2(x As B1, y As Integer)
        System.Console.WriteLine("Test2(x As B1)")
    End Sub

    Sub Test2(x As B2, y As Byte)
        System.Console.WriteLine("Test2(x As B2)")
    End Sub

    Sub Main()
        Dim i As Integer = 1
        Dim b As Byte = 0
        Test2({i}, b)
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Test2").WithArguments("Test2", vbCrLf &
     "    'Public Sub Test2(x As m.B1, y As Integer)': Argument matching parameter 'x' narrows to 'm.B1'." & vbCrLf &
     "    'Public Sub Test2(x As m.B2, y As Byte)': Argument matching parameter 'x' narrows to 'm.B2'."))

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_NoCallableOverloadCandidates2, "Test2").WithArguments("Test2", vbCrLf &
     "    'Public Sub Test2(x As m.B1, y As Integer)': Option Strict On disallows implicit conversions from 'Integer' to 'Short'." & vbCrLf &
     "    'Public Sub Test2(x As m.B2, y As Byte)': Option Strict On disallows implicit conversions from 'Integer' to 'Byte'."))
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralLegacy1()
            Dim source =
<compilation name="TestArrayLiteralLegacy1">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Module1

    Public Class c1
        Private Shared Sub StrReturnMethod(msg As String)
            Console.WriteLine(msg)
        End Sub

        Public Shared Sub foo(ByVal ParamArray z As String())
            StrReturnMethod("string()")
        End Sub

        Public Shared Sub foo(ByVal z As Char())
            StrReturnMethod("char()")
        End Sub

        Public Shared Sub foo(ByVal z As String)
            StrReturnMethod("string")
        End Sub
    End Class

    Sub Main()
        c1.foo({"a"c}) '//Char()
    End Sub

End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
char()
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
char()
            ]]>)

        End Sub

        <Fact()>
        Public Sub TestArrayLiteralLegacy2()
            Dim source =
<compilation name="TestArrayLiteralLegacy2">
    <file name="a.vb">
        <![CDATA[
Imports System
Module Program
    Dim strMethod As String = ""

    Class S5_C1a
        Public Shared Widening Operator CType(ByVal x As Integer()) As S5_C1a
            Console.WriteLine("Widening Operator CType(ByVal x As Integer())")
            Return Nothing
        End Operator

        Public Shared Widening Operator CType(ByVal x As Short()) As S5_C1a
            Console.WriteLine("Widening Operator CType(ByVal x As Short())")
            Return Nothing
        End Operator

    End Class

    Sub Main(args As String())
        'All Widening
        Dim Obj_S5_C1a As New S5_C1a

        Obj_S5_C1a = {1S, 2S, 3S, 4S, 5S}

    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Widening Operator CType(ByVal x As Short())
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Widening Operator CType(ByVal x As Short())
            ]]>)


        End Sub

        <Fact()>
        Public Sub TestArrayLiteralLegacy3()
            Dim source =
<compilation name="TestArrayLiteralLegacy3">
    <file name="a.vb">
        <![CDATA[
Imports System

Module Program
    Sub SubExpectingFuncOfT(Of t)(ByVal x As Func(Of t)())
        Console.WriteLine("SubExpectingFuncOfT - Array")
    End Sub

    Sub Main(args As String())

                'Specified
        Dim b2 As Func(Of Integer) = Function() 1 'Lambda Single

        SubExpectingFuncOfT({b2})
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
SubExpectingFuncOfT - Array
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
SubExpectingFuncOfT - Array
            ]]>)

        End Sub

        <Fact()>
        Public Sub TestArrayLiteralLegacy4()
            Dim source =
<compilation name="TestArrayLiteralLegacy4">
    <file name="a.vb">
        <![CDATA[
Imports System
Module m

    Sub Main()
        Foo2Params({1, 2, 3}, {1.1, 2.2, 3.3})
        Foo2Params({1, 2, 3}, {1L, 2L, 3L})
    End Sub

    Function Foo2Params(Of t)(x As t, y As t) As String
        Console.WriteLine("Function Foo2Param(of {0})(x as {0}, y as {0})", x.getType())
        Return Nothing
    End Function
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Function Foo2Param(of System.Double[])(x as System.Double[], y as System.Double[])
Function Foo2Param(of System.Int64[])(x as System.Int64[], y as System.Int64[])
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Function Foo2Param(of System.Double[])(x as System.Double[], y as System.Double[])
Function Foo2Param(of System.Int64[])(x as System.Int64[], y as System.Int64[])
            ]]>)

        End Sub

        <Fact()>
        Public Sub TestArrayLiteralLegacy5()
            Dim source =
<compilation name="TestArrayLiteralLegacy5">
    <file name="a.vb">
        <![CDATA[
Imports System
Module m

    Sub Main()
       Foo2ParamsArray({1, 2, 3}, 3L)
    End Sub

   Function Foo2ParamsArray(Of t)(x() As t, y As t) As String
     Console.WriteLine("Function Foo2Param(of {0})(x as {0}, y as {0})", x.getType())
     return nothing
    End Function

End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Function Foo2Param(of System.Int64[])(x as System.Int64[], y as System.Int64[])
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Function Foo2Param(of System.Int64[])(x as System.Int64[], y as System.Int64[])
            ]]>)

        End Sub


        ' Tests inferring generic parameter type via user defined conversion in C.
        <Fact()>
        Public Sub TestArrayLiteralGenericParameterAndUserDefinedConversion()
            Dim source =
<compilation name="TestArrayLiteralGenericParameterAndUserDefinedConversion">
    <file name="a.vb">
        <![CDATA[
Imports System
Module m
    Class C
        Public Shared Widening Operator CType(ByVal x As Integer()) As C
            Console.WriteLine("Widening Operator CType(ByVal x As Integer())")
            Return new C
        End Operator
    End Class

    Sub Main()
        Foo({1, 2})
        Foo2Params({1, 2, 3}, New C())
    End Sub

    Function Foo(Of t)(ByVal x As t) As String
        Console.WriteLine("Function Foo(of {0})(x as {0})", x.getType())
        Return nothing
    End Function

   Function Foo2Params(Of t)(x As t, y As t) As String
     Console.WriteLine("Function Foo2Params(of {0})(x as {0}, y as {1})", x.getType(), y.GetType())
     return nothing
    End Function

End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Function Foo(of System.Int32[])(x as System.Int32[])
Widening Operator CType(ByVal x As Integer())
Function Foo2Params(of m+C)(x as m+C, y as m+C)
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Function Foo(of System.Int32[])(x as System.Int32[])
Widening Operator CType(ByVal x As Integer())
Function Foo2Params(of m+C)(x as m+C, y as m+C)
            ]]>)

        End Sub

        ' Tests dominant type when array is converted to c via user defined function
        <Fact()>
        Public Sub TestArrayLiteralDominantTypeAndUserDefinedConversion()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Module m
    Class C
        Public Shared Widening Operator CType(ByVal x As Integer()) As C
            Console.WriteLine("Widening Operator CType(ByVal x As Integer())")
            Return new C
        End Operator
    End Class

    Sub Main()
         Dim x = {New C(), ({1, 2, 3})}
        Console.WriteLine("{0}", x.GetType())
    End Sub

End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Widening Operator CType(ByVal x As Integer())
m+C[]
            ]]>)


            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Widening Operator CType(ByVal x As Integer())
m+C[]
            ]]>)

        End Sub

        <WorkItem(544203, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544203")>
        <Fact()>
        Public Sub ArrayLiteralInferenceBug12426()
            Dim source =
<compilation name="ArrayLiteralInferenceBug12426">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic
Module Program
    Sub Main()
        Dim a =
            ImmutableArray(Of BoundStatement).CreateFrom(
            {
                New BoundExpressionStatement(),
                New BoundReturnStatement()
            })

        Console.WriteLine("{0}", a.GetType())
    End Sub
End Module
Class ImmutableArray(Of T)
    Public Shared Function CreateFrom(ParamArray items() As T) As ImmutableArray(Of T)
        Console.WriteLine("Public Shared Function CreateFrom(ParamArray items() As {0}) As ImmutableArray(Of {0})",
                          items.GetType().GetElementType())
        Return New ImmutableArray(Of T)()
    End Function
    Public Shared Function CreateFrom(Of U As T)(items As ICollection(Of U)) As ImmutableArray(Of T)
        Console.WriteLine("Public Shared Function CreateFrom(Of U As T)(items As ICollection(Of {0})) As ImmutableArray(Of T)",
                          items.GetType().GetElementType())
        Return New ImmutableArray(Of T)()
    End Function
End Class
Class BoundStatement
End Class
Class BoundReturnStatement
    Inherits BoundStatement
End Class
Class BoundExpressionStatement
    Inherits BoundStatement
End Class
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Public Shared Function CreateFrom(ParamArray items() As BoundStatement) As ImmutableArray(Of BoundStatement)
ImmutableArray`1[BoundStatement]
            ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Public Shared Function CreateFrom(ParamArray items() As BoundStatement) As ImmutableArray(Of BoundStatement)
ImmutableArray`1[BoundStatement]
            ]]>)

        End Sub

        <WorkItem(544381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544381")>
        <Fact()>
        Public Sub ArrayLiteralInferenceBug12679()
            Dim source =
<compilation name="ArrayLiteralInferenceBug12679">
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1

    Enum TypedConstantKind
        Array
    End Enum

    Friend Sub VerifyValue(Of T)(i As Integer, kind As TypedConstantKind, v As T)
        Console.WriteLine("Friend Sub VerifyValue(Of T)(i As Integer, kind As TypedConstantKind, v As {0})", v.GetType())
    End Sub

    Sub Main(args As String())
        VerifyValue(0, TypedConstantKind.Array, {1, "two", GetType(String), 3.1415926})
        VerifyValue(0, TypedConstantKind.Array, {})
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Friend Sub VerifyValue(Of T)(i As Integer, kind As TypedConstantKind, v As System.Object[])
Friend Sub VerifyValue(Of T)(i As Integer, kind As TypedConstantKind, v As System.Object[])
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_TypeInferenceAssumed3, "{1, ""two"", GetType(String), 3.1415926}").WithArguments("T", "Friend Sub VerifyValue(Of T)(i As Integer, kind As Module1.TypedConstantKind, v As T)", "Object()"),
                Diagnostic(ERRID.WRN_TypeInferenceAssumed3, "{}").WithArguments("T", "Friend Sub VerifyValue(Of T)(i As Integer, kind As Module1.TypedConstantKind, v As T)", "Object()"))
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
Friend Sub VerifyValue(Of T)(i As Integer, kind As TypedConstantKind, v As System.Object[])
Friend Sub VerifyValue(Of T)(i As Integer, kind As TypedConstantKind, v As System.Object[])
                         ]]>)

        End Sub

        <Fact>
        Public Sub TestSemanticInfoTooDeeplyNestedArrayLiteral()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x = {1, {2}}'BIND:"{2}"
    End Sub
End Module
    ]]></file>
</compilation>)
            ' No semantic information is reported. It should be Int32.
            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Equal("?", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticSummary.Type.TypeKind)
            Assert.Equal("?", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(544363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544363")>
        <Fact()>
        Public Sub TestArrayLiteralWithAmbiguousOverloadWithParamArray()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(ByVal args As String())
        fooModules({"1"}, {1})
    End Sub
    Public Sub fooModules(Of T)(ByVal ParamArray z As T())
    End Sub
    Public Sub fooModules(ByVal ParamArray z As String())
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeInferenceFailureAmbiguous2, "fooModules").WithArguments("Public Sub fooModules(Of T)(ParamArray z As T())"))

            compilation = compilation.WithOptions(_strictOn)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeInferenceFailureAmbiguous2, "fooModules").WithArguments("Public Sub fooModules(Of T)(ParamArray z As T())"))
        End Sub

        <WorkItem(544352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544352")>
        <Fact()>
        Public Sub TestArrayLiteralErrorMsgArrayOfDelegate()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module Program
    Sub Main(ByVal args As String())
        Dim x1 As Func(Of Integer) = {AddressOf Foo}
    End Sub
 
    Sub Foo()
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ArrayInitNoType, "{AddressOf Foo}"))

            compilation = compilation.WithOptions(_strictOn)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ArrayInitNoType, "{AddressOf Foo}"))
        End Sub


        <WorkItem(544566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544566")>
        <Fact()>
        Public Sub TestArrayLiteralInTernaryIf()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Infer On
Imports System
Module M
Sub Main()
        Dim o1 = If(True, {1, 2S, 3S}, Nothing)
        Dim o2 = If(True, {1, 2S, 3S}, CType(Nothing, Long()))
        Dim o3 As Integer() = If(True, {1, 2S, 3S}, Nothing)
        Dim o4 As Long() = If(True, {1, 2S, 3S}, CType(Nothing, Long()))
        Dim x As Long() = New Long() {}
        Dim o5 = If(True, {1, 2S, 3S}, x)

        Console.WriteLine(o1.GetType())
        Console.WriteLine(o2.GetType())
        Console.WriteLine(o5.GetType())
End Sub
End Module

    ]]></file>
</compilation>, options:=_strictOff)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
System.Int32[]
System.Int64[]
System.Int64[]
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp, expectedOutput:=<![CDATA[
System.Int32[]
System.Int64[]
System.Int64[]
                        ]]>)
        End Sub

        <WorkItem(529543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529543")>
        <Fact()>
        Public Sub TestArrayLiteralSemanticModelCTypeLongConversion()
            Dim compilation = CreateCompilationWithMscorlib(
 <compilation>
     <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Object = CType({1}, Long())'BIND:"{1}"
        Console.WriteLine(x.GetType)
    End Sub
End Module
    ]]></file>
 </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.Int64()", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Widening, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim conversion = model.ClassifyConversion(node, semanticSummary.ConvertedType)
            Assert.False(conversion.IsArray)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralSemanticModelUserDefinedConversion()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module m
    Class C
        Public Shared Widening Operator CType(ByVal x As Integer()) As C
            Console.WriteLine("Widening Operator CType(ByVal x As Integer())")
            Return New C
        End Operator
    End Class

    Sub Main()
        Foo({1, 2})
        Foo2Params({1, 2, 3}, New C())'BIND:"{1, 2, 3}"
    End Sub

    Function Foo(Of t)(ByVal x As t) As String
        Console.WriteLine("Function Foo(of {0})(x as {0})", x.GetType())
        Return Nothing
    End Function

    Function Foo2Params(Of t)(x As t, y As t) As String
        Console.WriteLine("Function Foo2Params(of {0})(x as {0}, y as {1})", x.GetType(), y.GetType())
        Return Nothing
    End Function

End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("m.C", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Widening Or ConversionKind.UserDefined, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestArrayLiteralSemanticModelCharArrayConversion()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim s As String = {"a"c}'BIND:"{"a"c}"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.String", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningString, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(545375, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545375")>
        <Fact()>
        Public Sub TestArrayLiteralInferTypeInIf()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim i1 = If(True, {2, "hello"}, New Object() {})  ' Object()                        '
        Console.WriteLine(i1.GetType)
    End Sub

End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
            System.Object[]
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
            System.Object[]
                        ]]>)
        End Sub

        <WorkItem(545517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545517")>
        <Fact()>
        Public Sub TestArrayLiteralInferTypeInWithinParens1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic
Module M
    Sub Main()
       Foo({({1})})
    End Sub
    Sub Foo(Of T As Structure)(x As T?()())
        Console.WriteLine("Sub Foo(Of T As Structure)(x As T?()())")
    End Sub
    Sub Foo(Of T)(x As IList(Of T)())
        Console.WriteLine("Sub Foo(Of T)(x As IList(Of T)())")
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
Sub Foo(Of T)(x As IList(Of T)())
                        ]]>)

            comp = comp.WithOptions(_strictOn)
            CompileAndVerify(comp,
                             expectedOutput:=<![CDATA[
Sub Foo(Of T)(x As IList(Of T)())
                        ]]>)
        End Sub

        <WorkItem(545517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545517")>
        <Fact()>
        Public Sub TestArrayLiteralInferTypeInWithinParens2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module M
    Sub Main()
       dim x as integer?() = {1}    ' should succeed
       dim y as integer?() = ({1})  ' should fail
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, options:=_strictOff)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "({1})").WithArguments("Integer()", "Integer?()", "Integer", "Integer?"))

            comp = comp.WithOptions(_strictOn)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "({1})").WithArguments("Integer()", "Integer?()", "Integer", "Integer?"))

        End Sub

        <WorkItem(530876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530876")>
        <Fact()>
        Public Sub Bug17124()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main()
    	Dim x = New Long()()() {({({1L,2L}), ({3L, 4L})})}
    End Sub
End Module
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(796610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/796610")>
        <Fact()>
        Public Sub Bug796610_1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Explicit On
Imports System
Imports Microsoft.VisualBasic

Module M
    Sub Main()
        Dim obj1 = If(True, {Nothing, New With {.a = 1}}, {Nothing})
        Validate(obj1)
        Validate({Nothing, New With {.a = 1}}, {Nothing})
        Console.WriteLine("--------------")

        Dim a1 = If(True, {<basic xmlns="">true</basic>}, {Nothing})
        Validate(a1)
        Validate({<basic xmlns="">true</basic>}, {Nothing})
        Dim a3 = If({"foo"}, {Nothing})
        Validate(a3)
        Validate({"foo"}, {Nothing})
        System.Console.WriteLine("--------------")

        Dim b1 = If(True, {New Animal, New Mammal}, {New Fish, New Mammal})
        Dim b2 = If(False, {New Animal, New Mammal}, {New Fish, New Mammal})
        Validate(b1)
        Validate(b2)
        Validate({New Animal, New Mammal}, {New Fish, New Mammal})
        System.Console.WriteLine("--------------")

        Dim x5 = If(True, {CType(New Bar(1), Foo)}, {Nothing})
        Validate(x5)
        Validate({CType(New Bar(1), Foo)}, {Nothing})

        System.Console.WriteLine("======================" & vbCrLf)
        Dim obj2 = If(True, {Nothing}, {Nothing, New With {.a = 1}})
        Validate(obj2)
        Validate({Nothing}, {Nothing, New With {.a = 1}})
        Console.WriteLine("--------------")
        Dim obj3 = If(True, {Nothing, New With {.a = 1}}, {})
        Validate(obj3)
        Validate({Nothing, New With {.a = 1}}, {})
        Console.WriteLine("--------------")
        Dim obj4 = If(True, {}, {Nothing, New With {.a = 1}})
        Validate(obj4)
        Validate({}, {Nothing, New With {.a = 1}})
        Console.WriteLine("--------------")

        Dim obj5 = If(True, {}, {{New Object()}})
        Validate(obj5)
        Validate({}, {{New Object()}})
        Console.WriteLine("--------------")
        Dim obj6 = If(True, {{New Object()}}, {})
        Validate(obj6)
        Validate({{New Object()}}, {})
        Console.WriteLine("--------------")

        Dim obj7 = If(True, Nothing, {{New Object()}})
        Validate(obj7)
        Validate(Nothing, {{New Object()}})
        Console.WriteLine("--------------")
        Dim obj8 = If(True, {{New Object()}}, Nothing)
        Validate(obj8)
        Validate({{New Object()}}, Nothing)
        Console.WriteLine("--------------")

        Dim obj9 = If(True, (Nothing), {{New Object()}})
        Validate(obj9)
        Validate((Nothing), {{New Object()}})
        Console.WriteLine("--------------")
        Dim obj10 = If(True, {{New Object()}}, (Nothing))
        Validate(obj10)
        Validate({{New Object()}}, (Nothing))
        Console.WriteLine("--------------")
    End Sub

    Sub Validate(Of T)(ByVal x As T)
        Dim ActualType As Type = GetType(T)
        Console.WriteLine("Static={0}, Runtime={1}", ActualType.ToString, If(x Is Nothing, "Nothing", x.GetType().ToString()))
    End Sub

    Sub Validate(Of T)(ByVal x As T, ByVal y As T)
        Dim ActualType As Type = GetType(T)
        Console.WriteLine("Static={0}, Runtime x={1}, Runtime y={2}", ActualType.ToString, If(x Is Nothing, "Nothing", x.GetType().ToString()), If(y Is Nothing, "Nothing", y.GetType().ToString()))
    End Sub
End Module

Class Animal : End Class
Class Mammal : Inherits Animal : End Class
Class Fish : Inherits Animal : End Class

Class Foo
    Sub New(ByVal x As Integer)
        Field = x
    End Sub
    Public Field As Integer
    Public Overloads Shared Widening Operator CType(ByVal x As Bar) As Foo
        Return New Foo(x.Field)
    End Operator
End Class
Class Bar
    Public Field As Integer
    Sub New(ByVal x As Integer)
        Field = x
    End Sub
    Public Overloads Shared Widening Operator CType(ByVal x As Foo) As Bar
        Return New Bar(x.Field)
    End Operator
End Class
]]></file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, XmlReferences, TestOptions.ReleaseExe)
            CompileAndVerify(comp,
            <![CDATA[
Static=VB$AnonymousType_0`1[System.Int32][], Runtime=VB$AnonymousType_0`1[System.Int32][]
Static=VB$AnonymousType_0`1[System.Int32][], Runtime x=VB$AnonymousType_0`1[System.Int32][], Runtime y=VB$AnonymousType_0`1[System.Int32][]
--------------
Static=System.Xml.Linq.XElement[], Runtime=System.Xml.Linq.XElement[]
Static=System.Xml.Linq.XElement[], Runtime x=System.Xml.Linq.XElement[], Runtime y=System.Xml.Linq.XElement[]
Static=System.String[], Runtime=System.String[]
Static=System.String[], Runtime x=System.String[], Runtime y=System.String[]
--------------
Static=Animal[], Runtime=Animal[]
Static=Animal[], Runtime=Animal[]
Static=Animal[], Runtime x=Animal[], Runtime y=Animal[]
--------------
Static=Foo[], Runtime=Foo[]
Static=Foo[], Runtime x=Foo[], Runtime y=Foo[]
======================

Static=VB$AnonymousType_0`1[System.Int32][], Runtime=VB$AnonymousType_0`1[System.Int32][]
Static=VB$AnonymousType_0`1[System.Int32][], Runtime x=VB$AnonymousType_0`1[System.Int32][], Runtime y=VB$AnonymousType_0`1[System.Int32][]
--------------
Static=VB$AnonymousType_0`1[System.Int32][], Runtime=VB$AnonymousType_0`1[System.Int32][]
Static=VB$AnonymousType_0`1[System.Int32][], Runtime x=VB$AnonymousType_0`1[System.Int32][], Runtime y=VB$AnonymousType_0`1[System.Int32][]
--------------
Static=VB$AnonymousType_0`1[System.Int32][], Runtime=VB$AnonymousType_0`1[System.Int32][]
Static=VB$AnonymousType_0`1[System.Int32][], Runtime x=VB$AnonymousType_0`1[System.Int32][], Runtime y=VB$AnonymousType_0`1[System.Int32][]
--------------
Static=System.Object[,], Runtime=System.Object[,]
Static=System.Object[,], Runtime x=System.Object[,], Runtime y=System.Object[,]
--------------
Static=System.Object[,], Runtime=System.Object[,]
Static=System.Object[,], Runtime x=System.Object[,], Runtime y=System.Object[,]
--------------
Static=System.Object[,], Runtime=Nothing
Static=System.Object[,], Runtime x=Nothing, Runtime y=System.Object[,]
--------------
Static=System.Object[,], Runtime=System.Object[,]
Static=System.Object[,], Runtime x=System.Object[,], Runtime y=Nothing
--------------
Static=System.Object[,], Runtime=Nothing
Static=System.Object[,], Runtime x=Nothing, Runtime y=System.Object[,]
--------------
Static=System.Object[,], Runtime=System.Object[,]
Static=System.Object[,], Runtime x=System.Object[,], Runtime y=Nothing
--------------
]]>)
        End Sub

        <WorkItem(796610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/796610")>
        <Fact()>
        Public Sub Bug796610_2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(ByVal args As String())
        fooModules({"1"}, {})
    End Sub
    Public Sub fooModules(Of T)(ByVal ParamArray z As T())
        System.Console.WriteLine(GetType(T))
    End Sub
    Public Sub fooModules(ByVal ParamArray z As String())
    End Sub
End Module
    ]]></file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[System.String[]]]>)

            compilation = compilation.WithOptions(_strictOn)
            CompileAndVerify(compilation, <![CDATA[System.String[]]]>)
        End Sub

        <WorkItem(116, "https://github.com/dotnet/roslyn/issues/116")>
        <Fact()>
        Public Sub TestArrayLiteralSemanticModelImplicitConversion()
            Dim compilation = CreateCompilationWithMscorlib(
 <compilation>
     <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As Integer() = {1, 2, 3} 'BIND:"{1, 2, 3}"
    End Sub
End Module
    ]]></file>
 </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.Int32()", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Widening, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(799045, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/799045")>
        <Fact()>
        Public Sub TestArrayLiteralSemanticModelImplicitConversionParenthesized()
            Dim compilation = CreateCompilationWithMscorlib(
 <compilation>
     <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As Integer() = ({1, 2, 3}) 'BIND:"{1, 2, 3}"
    End Sub
End Module
    ]]></file>
 </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.Int32()", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Widening, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

    End Class
End Namespace



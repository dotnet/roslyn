' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CheckedUserDefinedOperatorsTests
        Inherits BasicTestBase

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeUnaryOperator_01(overflowChecksEnabled As Boolean)

            '// The IL Is equivalent to
            '//
            '// class C0 
            '// {
            '//     public static C0 operator checked -(C0 x)
            '//     {
            '//         System.Console.WriteLine(""-"");
            '//         return x;
            '//     }
            '//     public static C0 operator checked --(C0 x)
            '//     {
            '//         System.Console.WriteLine(""--"");
            '//         return x;
            '//     }
            '//     public static C0 operator checked ++(C0 x)
            '//     {
            '//         System.Console.WriteLine(""++"");
            '//         return x;
            '//     }
            '// }

            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 op_CheckedUnaryNegation (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr "-"
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname static 
        class C0 op_CheckedDecrement (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr "--"
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname static 
        class C0 op_CheckedIncrement (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr "++"
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
        ]]>

            Dim ilReference = CompileIL(ilSource.Value)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        x = -x
        x += 1
        x -= 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, references:={ilReference}, options:=TestOptions.DebugDll.WithOverflowChecks(enabled:=overflowChecksEnabled))
            comp1.AssertTheseDiagnostics(
<errors>
BC30487: Operator '-' is not defined for type 'C0'.
        x = -x
            ~~
BC30452: Operator '+' is not defined for types 'C0' and 'Integer'.
        x += 1
        ~~~~~~
BC30452: Operator '-' is not defined for types 'C0' and 'Integer'.
        x -= 1
        ~~~~~~
</errors>
            )

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        C0.op_CheckedUnaryNegation(x)
        C0.op_CheckedDecrement(x)
        C0.op_CheckedIncrement(x)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, references:={ilReference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))

            ' VB operators can be accessed by name as well. See ConsumeUnaryOperator_02 below.
            CompileAndVerify(comp2, expectedOutput:="
-
--
++
").VerifyDiagnostics()

            Dim c0_2 = comp2.GetTypeByMetadataName("C0")

            For Each m In c0_2.GetMembers().OfType(Of MethodSymbol)()
                If m.MethodKind <> MethodKind.Constructor Then
                    Assert.Equal(MethodKind.Ordinary, m.MethodKind)
                End If
            Next

            Dim comp3 = CreateCSharpCompilation("", referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard, {ilReference}))

            Dim c0_3 = comp3.GetTypeByMetadataName("C0")

            Dim operators = c0_3.GetMembers().OfType(Of IMethodSymbol)().Where(Function(m) m.MethodKind <> MethodKind.Constructor).ToArray()

            Assert.Equal(3, operators.Length)

            For Each m In operators
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind)
            Next

            Assert.Equal("Function C0.op_CheckedUnaryNegation(x As C0) As C0", SymbolDisplay.ToDisplayString(operators(0), SymbolDisplayFormat.TestFormat))
            Assert.Equal("Function C0.op_CheckedDecrement(x As C0) As C0", SymbolDisplay.ToDisplayString(operators(1), SymbolDisplayFormat.TestFormat))
            Assert.Equal("Function C0.op_CheckedIncrement(x As C0) As C0", SymbolDisplay.ToDisplayString(operators(2), SymbolDisplayFormat.TestFormat))

            Assert.Equal("Public Shared Function op_CheckedUnaryNegation(x As C0) As C0", SymbolDisplay.ToDisplayString(operators(0)))
            Assert.Equal("Public Shared Function op_CheckedDecrement(x As C0) As C0", SymbolDisplay.ToDisplayString(operators(1)))
            Assert.Equal("Public Shared Function op_CheckedIncrement(x As C0) As C0", SymbolDisplay.ToDisplayString(operators(2)))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeUnaryOperator_02(overflowChecksEnabled As Boolean)

            Dim source0 =
"
Public Class C0
    Public Shared Operator -(x as C0) As C0
        System.Console.WriteLine(""regular C0"")
        Return x
    End Operator
End Class
"
            Dim comp0 = CreateCompilation(source0, options:=TestOptions.DebugDll)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        x = -x
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        C0.op_UnaryNegation(x)
    End Sub
End Class
]]></file>
</compilation>

            For Each comp0Reference In {comp0.ToMetadataReference(), comp0.EmitToImageReference()}

                Dim comp1 = CreateCompilation(source1, references:={comp0Reference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))
                CompileAndVerify(comp1, expectedOutput:="regular C0").VerifyDiagnostics()

                Dim comp2 = CreateCompilation(source2, references:={comp0Reference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))
                CompileAndVerify(comp2, expectedOutput:="regular C0").VerifyDiagnostics()
                Dim c0_2 = comp2.GetTypeByMetadataName("C0")

                For Each m In c0_2.GetMembers().OfType(Of MethodSymbol)()
                    If m.MethodKind <> MethodKind.Constructor Then
                        Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind)
                    End If
                Next
            Next

        End Sub

        <Theory>
        <InlineData("+", "op_CheckedAddition", True)>
        <InlineData("-", "op_CheckedSubtraction", True)>
        <InlineData("*", "op_CheckedMultiply", True)>
        <InlineData("/", "op_CheckedDivision", True)>
        <InlineData("+", "op_CheckedAddition", False)>
        <InlineData("-", "op_CheckedSubtraction", False)>
        <InlineData("*", "op_CheckedMultiply", False)>
        <InlineData("/", "op_CheckedDivision", False)>
        <InlineData("\", "op_CheckedDivision", True)>
        <InlineData("\", "op_CheckedDivision", False)>
        Public Sub ConsumeBinaryOperator_01(op As String, metadataName As String, overflowChecksEnabled As Boolean)

            '// The IL is equivalent to
            '//
            '// class C0 
            '// {
            '//     public static C0 operator checked -(C0 x, C0 y)
            '//     {
            '//         System.Console.WriteLine(""checked C0"");
            '//         return x;
            '//     }
            '// }

            Dim ilSource = "
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 " + metadataName + " (
            class C0 x,
            class C0 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
"

            Dim ilReference = CompileIL(ilSource)

            Dim source1 =
"
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        x = x " + op + " x
    End Sub
End Class
"

            Dim comp1 = CreateCompilation(source1, references:={ilReference}, options:=TestOptions.DebugDll.WithOverflowChecks(enabled:=overflowChecksEnabled))
            comp1.AssertTheseDiagnostics(
(
"
BC30452: Operator '" + op + "' is not defined for types 'C0' and 'C0'.
        x = x " + op + " x
            ~~~~~
"
).Trim()
            )

            Dim source2 =
"
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        C0." + metadataName + "(x, x)
    End Sub
End Class
"
            Dim comp2 = CreateCompilation(source2, references:={ilReference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))

            ' VB operators can be accessed by name as well. See ConsumeBinaryOperator_02 below.
            CompileAndVerify(comp2, expectedOutput:="checked C0").VerifyDiagnostics()

            Dim c0_2 = comp2.GetTypeByMetadataName("C0")

            For Each m In c0_2.GetMembers().OfType(Of MethodSymbol)()
                If m.MethodKind <> MethodKind.Constructor Then
                    Assert.Equal(MethodKind.Ordinary, m.MethodKind)
                End If
            Next

            Dim comp3 = CreateCSharpCompilation("", referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard, {ilReference}))

            Dim c0_3 = comp3.GetTypeByMetadataName("C0")

            Dim operators = c0_3.GetMembers().OfType(Of IMethodSymbol)().Where(Function(m) m.MethodKind <> MethodKind.Constructor).ToArray()

            Assert.Equal(1, operators.Length)

            Assert.Equal(MethodKind.UserDefinedOperator, operators(0).MethodKind)
            Assert.Equal("Function C0." + metadataName + "(x As C0, y As C0) As C0", SymbolDisplay.ToDisplayString(operators(0), SymbolDisplayFormat.TestFormat))
            Assert.Equal("Public Shared Function " + metadataName + "(x As C0, y As C0) As C0", SymbolDisplay.ToDisplayString(operators(0)))
        End Sub

        <Theory>
        <InlineData("+", "op_Addition", True)>
        <InlineData("-", "op_Subtraction", True)>
        <InlineData("*", "op_Multiply", True)>
        <InlineData("/", "op_Division", True)>
        <InlineData("+", "op_Addition", False)>
        <InlineData("-", "op_Subtraction", False)>
        <InlineData("*", "op_Multiply", False)>
        <InlineData("/", "op_Division", False)>
        <InlineData("\", "op_IntegerDivision", True)>
        <InlineData("\", "op_IntegerDivision", False)>
        Public Sub ConsumeBinaryOperator_02(op As String, metadataName As String, overflowChecksEnabled As Boolean)

            Dim source0 =
"
Public Class C0
    Public Shared Operator " + op + "(x as C0, y as C0) As C0
        System.Console.WriteLine(""regular C0"")
        Return x
    End Operator
End Class
"
            Dim comp0 = CreateCompilation(source0, options:=TestOptions.DebugDll)

            Dim source1 =
"
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        x = x " + op + " x
    End Sub
End Class
"
            Dim source2 =
"
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        C0." + metadataName + "(x, x)
    End Sub
End Class
"

            For Each comp0Reference In {comp0.ToMetadataReference(), comp0.EmitToImageReference()}

                Dim comp1 = CreateCompilation(source1, references:={comp0Reference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))
                CompileAndVerify(comp1, expectedOutput:="regular C0").VerifyDiagnostics()

                Dim comp2 = CreateCompilation(source2, references:={comp0Reference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))
                CompileAndVerify(comp2, expectedOutput:="regular C0").VerifyDiagnostics()
                Dim c0_2 = comp2.GetTypeByMetadataName("C0")

                For Each m In c0_2.GetMembers().OfType(Of MethodSymbol)()
                    If m.MethodKind <> MethodKind.Constructor Then
                        Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind)
                    End If
                Next
            Next

        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeConversion_01(overflowChecksEnabled As Boolean)

            '// The IL is equivalent to
            '//
            '// public class C0 
            '// {
            '//     public static explicit operator checked long(C0 x)
            '//     {
            '//         System.Console.WriteLine(""checked C0"");
            '//         return 0;
            '//     }
            '// }

            Dim ilSource = "
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        int64 op_CheckedExplicit (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] int64
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: conv.i8
        IL_000e: stloc.0
        IL_000f: br.s IL_0011

        IL_0011: ldloc.0
        IL_0012: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
"
            Dim ilReference = CompileIL(ilSource)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        Dim y = CLng(x)
        y = CType(x, Long)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, references:={ilReference}, options:=TestOptions.DebugDll.WithOverflowChecks(enabled:=overflowChecksEnabled))
            comp1.AssertTheseDiagnostics(
<errors>
BC30311: Value of type 'C0' cannot be converted to 'Long'.
        Dim y = CLng(x)
                     ~
BC30311: Value of type 'C0' cannot be converted to 'Long'.
        y = CType(x, Long)
                  ~
</errors>
            )

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        C0.op_CheckedExplicit(x)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, references:={ilReference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))

            ' VB operators can be accessed by name as well. See ConsumeConversion_02 below.
            CompileAndVerify(comp2, expectedOutput:="checked C0").VerifyDiagnostics()
            Dim c0_2 = comp2.GetTypeByMetadataName("C0")

            For Each m In c0_2.GetMembers().OfType(Of MethodSymbol)()
                If m.MethodKind <> MethodKind.Constructor Then
                    Assert.Equal(MethodKind.Ordinary, m.MethodKind)
                End If
            Next

            Dim comp3 = CreateCSharpCompilation("", referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard, {ilReference}))

            Dim c0_3 = comp3.GetTypeByMetadataName("C0")

            Dim operators = c0_3.GetMembers().OfType(Of IMethodSymbol)().Where(Function(m) m.MethodKind <> MethodKind.Constructor).ToArray()

            Assert.Equal(1, operators.Length)

            Assert.Equal(MethodKind.Conversion, operators(0).MethodKind)
            Assert.Equal("Function C0.op_CheckedExplicit(x As C0) As System.Int64", SymbolDisplay.ToDisplayString(operators(0), SymbolDisplayFormat.TestFormat))
            Assert.Equal("Public Shared Function op_CheckedExplicit(x As C0) As Long", SymbolDisplay.ToDisplayString(operators(0)))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeConversion_02(overflowChecksEnabled As Boolean)

            Dim source0 =
"
Public Class C0
    Public shared Narrowing Operator CType(x as C0) As Long
        System.Console.WriteLine(""regular C0"")
        Return 0
    End Operator
End Class
"
            Dim comp0 = CreateCompilation(source0, options:=TestOptions.DebugDll)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        Dim y = CType(x, Long)
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Program
    Shared Sub Main()
        Dim x as New C0()
        C0.op_Explicit(x)
    End Sub
End Class
]]></file>
</compilation>

            For Each comp0Reference In {comp0.ToMetadataReference(), comp0.EmitToImageReference()}

                Dim comp1 = CreateCompilation(source1, references:={comp0Reference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))
                CompileAndVerify(comp1, expectedOutput:="regular C0").VerifyDiagnostics()

                Dim comp2 = CreateCompilation(source2, references:={comp0Reference}, options:=TestOptions.DebugExe.WithOverflowChecks(enabled:=overflowChecksEnabled))
                CompileAndVerify(comp2, expectedOutput:="regular C0").VerifyDiagnostics()
                Dim c0_2 = comp2.GetTypeByMetadataName("C0")

                For Each m In c0_2.GetMembers().OfType(Of MethodSymbol)()
                    If m.MethodKind <> MethodKind.Constructor Then
                        Assert.Equal(MethodKind.Conversion, m.MethodKind)
                    End If
                Next
            Next

        End Sub

    End Class
End Namespace

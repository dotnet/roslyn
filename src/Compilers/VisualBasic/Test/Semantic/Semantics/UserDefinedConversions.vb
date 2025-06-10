' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class UserDefinedConversions
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleTest1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Widening Operator CType(x As Integer) As B2
            Return New B2(x)
        End Operator
    End Class

    Sub Main()
        Dim x as Integer = 11
        Dim b2 As B2 = x 'BIND1:"x"
        System.Console.WriteLine(b2.f)
        System.Console.WriteLine(CType(x,B2).f) 'BIND2:"CType(x,B2)"
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
11
11
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("Module1.B2", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsWidening)
            Assert.Equal("Function Module1.B2.op_Implicit(x As System.Int32) As Module1.B2", conv.Method.ToTestDisplayString())

            Dim ctype_node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 2)
            Dim symbolInfo = model.GetSymbolInfo(ctype_node)
            Assert.Equal("Function Module1.B2.op_Implicit(x As System.Int32) As Module1.B2", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub SimpleTest2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Widening Operator CType(x As Integer) As B2
            Return New B2(x)
        End Operator
    End Class

    Sub Main()
        Dim x as Byte = 11
        Dim b2 As B2 = x 'BIND1:"x"
        System.Console.WriteLine(b2.f)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
11
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("Module1.B2", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsWidening)
            Assert.Equal("Function Module1.B2.op_Implicit(x As System.Int32) As Module1.B2", conv.Method.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub SimpleTest3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Shared Widening Operator CType(x As Integer) As B2
            Return New B3(x)
        End Operator
    End Class

    Class B3
        Inherits B2

        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub
    End Class

    Sub Main()
        Dim x as Integer = 11
        Dim b2 As B3 = x 'BIND1:"x"
        System.Console.WriteLine(b2.f)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
11
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("Module1.B3", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsNarrowing)
            Assert.Equal("Function Module1.B2.op_Implicit(x As System.Int32) As Module1.B2", conv.Method.ToTestDisplayString())

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Integer' to 'Module1.B3'.
        Dim b2 As B3 = x 'BIND1:"x"
                       ~
</expected>)

        End Sub

        <Fact>
        Public Sub SimpleTest4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class B2
        Shared Widening Operator CType(x As Integer) As B2
            Return New B3(x)
        End Operator
    End Class

    Class B3
        Inherits B2

        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub
    End Class

    Sub Main()
        Dim x as Byte = 11
        Dim b2 As B3 = x 'BIND1:"x"
        System.Console.WriteLine(b2.f)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
11
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("Module1.B3", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsNarrowing)
            Assert.Equal("Function Module1.B2.op_Implicit(x As System.Int32) As Module1.B2", conv.Method.ToTestDisplayString())

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Byte' to 'Module1.B3'.
        Dim b2 As B3 = x 'BIND1:"x"
                       ~
</expected>)

        End Sub

        <Fact>
        Public Sub Genericity1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)
        Inherits C1(Of S)

        Shared Shadows Widening Operator CType(x As C1(Of T)) As C2(Of T, S)
            System.Console.WriteLine("{0}.CType(x As {1}) As {2}", GetType(C2(Of T, S)), GetType(C1(Of T)), GetType(C2(Of T, S)))
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As New C1(Of Byte)
        Dim y As C2(Of Byte, Integer) = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Int32].CType(x As Module1+C1`1[System.Byte]) As Module1+C2`2[System.Byte,System.Int32]
]]>)
        End Sub

        <Fact>
        Public Sub Genericity2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
        Overloads Shared Widening Operator CType(x As C1(Of T)) As C2(Of Byte, Integer)
            System.Console.WriteLine("{0}.CType(x As {1}) As {2}", GetType(C1(Of T)), GetType(C1(Of T)), GetType(C2(Of Byte, Integer)))
            Return Nothing
        End Operator
    End Class

    Class C2(Of T, S)
        Inherits C1(Of S)

        Shared Shadows Widening Operator CType(x As C1(Of T)) As C2(Of T, S)
            System.Console.WriteLine("{0}.CType(x As {1}) As {2}", GetType(C2(Of T, S)), GetType(C1(Of T)), GetType(C2(Of T, S)))
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As New C1(Of Byte)
        Dim y As C2(Of Byte, Integer) = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C1`1[System.Byte].CType(x As Module1+C1`1[System.Byte]) As Module1+C2`2[System.Byte,System.Int32]
]]>)
        End Sub

        <Fact>
        Public Sub Genericity3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)
        Inherits C1(Of S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As {2}) As {1}", GetType(C2(Of T, S)), GetType(C1(Of T)), GetType(C2(Of T, S)))
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As New C2(Of Byte, Integer)
        Dim y As C1(Of Byte) = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Int32].CType(x As Module1+C2`2[System.Byte,System.Int32]) As Module1+C1`1[System.Byte]
]]>)
        End Sub

        <Fact>
        Public Sub Genericity4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
        Overloads Shared Widening Operator CType(x As C2(Of Byte, Integer)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As {2}) As {1}", GetType(C1(Of T)), GetType(C1(Of T)), GetType(C2(Of Byte, Integer)))
            Return Nothing
        End Operator
    End Class

    Class C2(Of T, S)
        Inherits C1(Of S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As {2}) As {1}", GetType(C2(Of T, S)), GetType(C1(Of T)), GetType(C2(Of T, S)))
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As New C2(Of Byte, Integer)
        Dim y As C1(Of Byte) = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C1`1[System.Byte].CType(x As Module1+C2`2[System.Byte,System.Int32]) As Module1+C1`1[System.Byte]
]]>)
        End Sub

        <Fact>
        Public Sub Genericity5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of T)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of S)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C1(Of Byte) = New C2(Of Byte, Byte)
        Dim y As C1(Of Byte) = New C2(Of Byte, Integer)
        Dim z As C1(Of Byte) = New C2(Of Integer, Byte)
        Dim u As C1(Of Integer) = New C2(Of Byte, Integer)
        Dim v As C1(Of Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C2(Of Byte, Byte)' cannot be converted to 'Module1.C1(Of Byte)'.
        Dim x As C1(Of Byte) = New C2(Of Byte, Byte)
                               ~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Genericity5_1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of T)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of S)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim y As C1(Of Byte) = New C2(Of Byte, Integer)
        Dim z As C1(Of Byte) = New C2(Of Integer, Byte)
        Dim u As C1(Of Integer) = New C2(Of Byte, Integer)
        Dim v As C1(Of Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of T)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of S)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of S)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of T)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of Byte)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of Byte)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of T)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of S)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C1(Of Byte) = New C2(Of Byte, Byte)
        Dim y As C1(Of Byte) = New C2(Of Byte, Integer)
        Dim z As C1(Of Byte) = New C2(Of Integer, Byte)
        Dim u As C1(Of Integer) = New C2(Of Byte, Integer)
        Dim v As C1(Of Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Byte].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of S)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of T)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of T)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of Byte)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of Byte)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of S)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C1(Of Byte) = New C2(Of Byte, Byte)
        Dim y As C1(Of Byte) = New C2(Of Byte, Integer)
        Dim z As C1(Of Byte) = New C2(Of Integer, Byte)
        Dim u As C1(Of Integer) = New C2(Of Byte, Integer)
        Dim v As C1(Of Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Byte].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of S)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of T)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity8()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of T)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of T)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of S)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1(Of Byte)
            System.Console.WriteLine("{0}.CType(x As C2(Of T, S)) As C1(Of Byte)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C1(Of Byte) = New C2(Of Byte, Byte)
        Dim y As C1(Of Byte) = New C2(Of Byte, Integer)
        Dim z As C1(Of Byte) = New C2(Of Integer, Byte)
        Dim u As C1(Of Integer) = New C2(Of Byte, Integer)
        Dim v As C1(Of Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Byte].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of Byte)
Module1+C2`2[System.Byte,System.Int32].CType(x As C2(Of T, S)) As C1(Of S)
Module1+C2`2[System.Int32,System.Byte].CType(x As C2(Of T, S)) As C1(Of T)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity9()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1(Of T)
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C1(Of Byte)) As C2(Of T, S)
            System.Console.WriteLine("{0}.CType(x As C1(Of Byte)) As C2(Of T, S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C1(Of T)) As C2(Of T, S)
            System.Console.WriteLine("{0}.CType(x As C1(Of T)) As C2(Of T, S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C1(Of S)) As C2(Of T, S)
            System.Console.WriteLine("{0}.CType(x As C1(Of S)) As C2(Of T, S)", GetType(C2(Of T, S)))
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C2(Of Byte, Byte) = New C1(Of Byte)
        Dim y As C2(Of Byte, Integer) = New C1(Of Byte)
        Dim z As C2(Of Integer, Byte) = New C1(Of Byte)
        Dim u As C2(Of Byte, Integer) = New C1(Of Integer)
        Dim v As C2(Of Integer, Byte) = New C1(Of Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Module1+C2`2[System.Byte,System.Byte].CType(x As C1(Of Byte)) As C2(Of T, S)
Module1+C2`2[System.Byte,System.Int32].CType(x As C1(Of Byte)) As C2(Of T, S)
Module1+C2`2[System.Int32,System.Byte].CType(x As C1(Of Byte)) As C2(Of T, S)
Module1+C2`2[System.Byte,System.Int32].CType(x As C1(Of S)) As C2(Of T, S)
Module1+C2`2[System.Int32,System.Byte].CType(x As C1(Of T)) As C2(Of T, S)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity10()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of S, T)) As C2(Of T, S)
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of Integer, Byte)) As C2(Of T, S)
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C2(Of S, T)
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C2(Of Byte, Integer)
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C2(Of Byte, Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C2(Of Integer, Byte)' cannot be converted to 'Module1.C2(Of Byte, Integer)'.
        Dim x As C2(Of Byte, Integer) = New C2(Of Integer, Byte)
                                        ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Genericity11()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1
            System.Console.WriteLine("CType(x As C2(Of T, S)) As C1")
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C1) As C2(Of T, S)
            System.Console.WriteLine("CType(x As C1) As C2(Of T, S)")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As C1 = New C2(Of Integer, Integer)
        Dim y As C2(Of Integer, Integer) = New C1
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C2(Of T, S)) As C1
CType(x As C1) As C2(Of T, S)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity12()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Shared Shadows Widening Operator CType(x As C2(Of Integer, Integer)) As C1
            System.Console.WriteLine("CType(x As C2(Of Integer, Integer)) As C1")
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C1) As C2(Of Integer, Integer)
            System.Console.WriteLine("CType(x As C1) As C2(Of Integer, Integer)")
            Return Nothing
        End Operator
    End Class

    Class C2(Of T, S)

        Shared Shadows Widening Operator CType(x As C2(Of T, S)) As C1
            System.Console.WriteLine("CType(x As C2(Of T, S)) As C1")
            Return Nothing
        End Operator

        Shared Shadows Widening Operator CType(x As C1) As C2(Of T, S)
            System.Console.WriteLine("CType(x As C1) As C2(Of T, S)")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As C1 = New C2(Of Integer, Integer)
        Dim y As C2(Of Integer, Integer) = New C1
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C2(Of Integer, Integer)) As C1
CType(x As C1) As C2(Of Integer, Integer)
]]>)
        End Sub

        <Fact>
        Public Sub Genericity13()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Shared Shadows Widening Operator CType(x As C2) As C1
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Shadows Widening Operator CType(x As C2) As C1
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As C1 = New C2
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C2' cannot be converted to 'Module1.C1'.
        Dim x As C1 = New C2
                      ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Genericity14()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Shared Shadows Narrowing Operator CType(x As C2) As C1
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Shadows Narrowing Operator CType(x As C2) As C1
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As C1 = New C2
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C2' cannot be converted to 'Module1.C1'.
        Dim x As C1 = New C2
                      ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Genericity15()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C2(Of T, S)

        Shared Shadows Narrowing Operator CType(x As C2(Of S, T)) As C2(Of T, S)
            Return Nothing
        End Operator

        Shared Shadows Narrowing Operator CType(x As C2(Of Integer, Byte)) As C2(Of T, S)
            Return Nothing
        End Operator

        Shared Shadows Narrowing Operator CType(x As C2(Of T, S)) As C2(Of S, T)
            Return Nothing
        End Operator

        Shared Shadows Narrowing Operator CType(x As C2(Of T, S)) As C2(Of Byte, Integer)
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As C2(Of Byte, Integer) = New C2(Of Integer, Byte)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C2(Of Integer, Byte)' cannot be converted to 'Module1.C2(Of Byte, Integer)'.
        Dim x As C2(Of Byte, Integer) = New C2(Of Integer, Byte)
                                        ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Shadowing1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Shared Shadows Widening Operator CType(x As C1) As C3
            System.Console.WriteLine("CType(x As C1) As C3")
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1

        Shared Shadows Narrowing Operator CType(x As C2) As C3
            System.Console.WriteLine("CType(x As C2) As C3")
            Return Nothing
        End Operator
    End Class

    Class C3
    End Class

    Sub Main()
        Dim x As C3 = New C2
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C2) As C3
]]>)
        End Sub

        <Fact>
        Public Sub Shadowing2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Shared Shadows Widening Operator CType(x As C1) As C3
            System.Console.WriteLine("CType(x As C1) As C3")
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1

        Overloads Shared Narrowing Operator CType(x As C2) As C3
            System.Console.WriteLine("CType(x As C2) As C3")
            Return Nothing
        End Operator
    End Class

    Class C3
    End Class

    Sub Main()
        Dim x As C3 = New C2
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C1) As C3
]]>)
        End Sub

        <Fact>
        Public Sub Shadowing3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Overloads Shared Widening Operator CType(x As C4) As C1
            System.Console.WriteLine("CType(x As C4) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1

        Shared Shadows Narrowing Operator CType(x As C3) As C2
            System.Console.WriteLine("CType(x As C3) As C2")
            Return Nothing
        End Operator
    End Class

    Class C3
    End Class

    Class C4
        Inherits C3
    End Class

    Class C5
        Inherits C4
    End Class

    Sub Main()
        Dim c4 As New C5()
        Dim x As C2 = c4
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C3) As C2
]]>)
        End Sub

        <Fact>
        Public Sub Shadowing4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Overloads Shared Widening Operator CType(x As C4) As C1
            System.Console.WriteLine("CType(x As C4) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1

        Overloads Shared Narrowing Operator CType(x As C3) As C2
            System.Console.WriteLine("CType(x As C3) As C2")
            Return Nothing
        End Operator
    End Class

    Class C3
    End Class

    Class C4
        Inherits C3
    End Class

    Class C5
        Inherits C4
    End Class

    Sub Main()
        Dim c4 As New C5()
        Dim x As C2 = c4
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C5' cannot be converted to 'Module1.C2'.
        Dim x As C2 = c4
                      ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Shadowing5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Widening Operator CType(x As C4) As C0
            System.Console.WriteLine("CType(x As C4) As C1")
            Return Nothing
        End Operator
    End Class


    Class C1
        Inherits C0

        Public op_Implicit As Integer
        Public op_Explicit As Integer
    End Class

    Class C2
        Inherits C1

        Overloads Shared Narrowing Operator CType(x As C3) As C2
            System.Console.WriteLine("CType(x As C3) As C2")
            Return Nothing
        End Operator
    End Class

    Class C3
    End Class

    Class C4
        Inherits C3
    End Class

    Class C5
        Inherits C4
    End Class

    Sub Main()
        Dim c4 As New C5()
        Dim x As C2 = c4
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC40014: variable 'op_Implicit' conflicts with a member implicitly declared for operator 'CType' in the base class 'C0' and should be declared 'Shadows'.
        Public op_Implicit As Integer
               ~~~~~~~~~~~
BC40012: operator 'CType' implicitly declares 'op_Explicit', which conflicts with a member in the base class 'C1', and so the operator should be declared 'Shadows'.
        Overloads Shared Narrowing Operator CType(x As C3) As C2
                                            ~~~~~
BC30311: Value of type 'Module1.C5' cannot be converted to 'Module1.C2'.
        Dim x As C2 = c4
                      ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Shadowing6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Widening Operator CType(x As C4) As C0
            System.Console.WriteLine("CType(x As C4) As C1")
            Return Nothing
        End Operator
    End Class


    Class C1
        Inherits C0

        Public Sub op_Implicit()
        End Sub

        Public Sub op_Explicit()
        End Sub
    End Class

    Class C2
        Inherits C1

        Overloads Shared Narrowing Operator CType(x As C3) As C2
            System.Console.WriteLine("CType(x As C3) As C2")
            Return Nothing
        End Operator
    End Class

    Class C3
    End Class

    Class C4
        Inherits C3
    End Class

    Class C5
        Inherits C4
    End Class

    Sub Main()
        Dim c4 As New C5()
        Dim x As C2 = c4
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC40014: sub 'op_Implicit' conflicts with a member implicitly declared for operator 'CType' in the base class 'C0' and should be declared 'Shadows'.
        Public Sub op_Implicit()
                   ~~~~~~~~~~~
BC40012: operator 'CType' implicitly declares 'op_Explicit', which conflicts with a member in the base class 'C1', and so the operator should be declared 'Shadows'.
        Overloads Shared Narrowing Operator CType(x As C3) As C2
                                            ~~~~~
BC30311: Value of type 'Module1.C5' cannot be converted to 'Module1.C2'.
        Dim x As C2 = c4
                      ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1

        Overloads Shared Widening Operator CType(x As C3) As C1
            Return Nothing
        End Operator

    End Class

    Class C2
        Inherits C1

    End Class

    Class C3
    End Class

    Class C4
        Inherits C3

        Overloads Shared Widening Operator CType(x As C4) As C2
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As C1 = New C4()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C4' cannot be converted to 'Module1.C1'.
        Dim x As C1 = New C4()
                      ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1

        Overloads Shared Narrowing Operator CType(x As C3) As C1
            Return Nothing
        End Operator

    End Class

    Class C2
        Inherits C1

    End Class

    Class C3
    End Class

    Class C4
        Inherits C3

        Overloads Shared Narrowing Operator CType(x As C4) As C2
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As C1 = New C4()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C4' cannot be converted to 'Module1.C1'.
        Dim x As C1 = New C4()
                      ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
    End Class

    Class C2
        Overloads Shared Widening Operator CType(x As C2) As C1
            Return Nothing
        End Operator
    End Class

    Class C3
        Inherits C2
        Overloads Shared Widening Operator CType(x As C3) As C1
            Return Nothing
        End Operator
    End Class

    Class C4
        Inherits C3

        Overloads Shared Widening Operator CType(x As C4) As C1
            System.Console.WriteLine("CType(x As C4) As C1")
            Return Nothing
        End Operator
    End Class

    Class C5
        Inherits C4
    End Class

    Sub Main()
        Dim x As C1 = New C5()
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C4) As C1
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
    End Class

    Class C2
        Overloads Shared Narrowing Operator CType(x As C2) As C1
            Return Nothing
        End Operator
    End Class

    Class C3
        Inherits C2
        Overloads Shared Narrowing Operator CType(x As C3) As C1
            Return Nothing
        End Operator
    End Class

    Class C4
        Inherits C3

        Overloads Shared Narrowing Operator CType(x As C4) As C1
            System.Console.WriteLine("CType(x As C4) As C1")
            Return Nothing
        End Operator
    End Class

    Class C5
        Inherits C4
    End Class

    Sub Main()
        Dim x As C1 = New C5()
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C4) As C1
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Module1.C5' to 'Module1.C1'.
        Dim x As C1 = New C5()
                      ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Widening Operator CType(x As C0) As C2
            System.Console.WriteLine("CType(x As C0) As C2")
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0) As C3
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0) As C4
            Return Nothing
        End Operator
    End Class

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class

    Class C3
        Inherits C2
    End Class

    Class C4
        Inherits C3
    End Class


    Sub Main()
        Dim x As C1 = New C0()
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C0) As C2
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Narrowing Operator CType(x As C0) As C2
            System.Console.WriteLine("CType(x As C0) As C2")
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0) As C3
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0) As C4
            Return Nothing
        End Operator
    End Class

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class

    Class C3
        Inherits C2
    End Class

    Class C4
        Inherits C3
    End Class


    Sub Main()
        Dim x As C1 = New C0()
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C0) As C2
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Module1.C0' to 'Module1.C1'.
        Dim x As C1 = New C0()
                      ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Widening Operator CType(x As Long) As C0
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As Integer) As C0
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As UInteger) As C0
            Return Nothing
        End Operator
    End Class


    Sub Main()
        Dim y As Byte = 1
        Dim x As C0 = y
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Byte' cannot be converted to 'Module1.C0'.
        Dim x As C0 = y
                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Narrowing Operator CType(x As Long) As C0
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As Integer) As C0
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As UInteger) As C0
            Return Nothing
        End Operator
    End Class


    Sub Main()
        Dim y As Byte = 1
        Dim x As C0 = y
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Byte' cannot be converted to 'Module1.C0'.
        Dim x As C0 = y
                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Widening Operator CType(x As C0) As Short
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0) As Integer
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0) As UInteger
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim y As Long = New C0()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C0' cannot be converted to 'Long'.
        Dim y As Long = New C0()
                        ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Narrowing Operator CType(x As C0) As Short
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0) As Integer
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0) As UInteger
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim y As Long = New C0()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C0' cannot be converted to 'Long'.
        Dim y As Long = New C0()
                        ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0(Of T, S)
        Overloads Shared Widening Operator CType(x As C0(Of T, S)) As T
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0(Of T, S)) As S
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As Long = New C0(Of Integer, Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C0(Of Integer, Integer)' cannot be converted to 'Long'.
        Dim x As Long = New C0(Of Integer, Integer)
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Widening6_2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0(Of T, S)
        Overloads Shared Widening Operator CType(x As C0(Of T, S)) As T
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0(Of T, S)) As S
            Return Nothing
        End Operator
        Overloads Shared Widening Operator CType(x As C0(Of T, S)) As Integer
            System.Console.WriteLine("CType(x As C0(Of T, S)) As Integer")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As Long = New C0(Of Integer, Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C0(Of T, S)) As Integer
]]>)
        End Sub

        <Fact>
        Public Sub Narrowing6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0(Of T, S)
        Overloads Shared Narrowing Operator CType(x As C0(Of T, S)) As T
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0(Of T, S)) As S
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As Long = New C0(Of Integer, Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C0(Of Integer, Integer)' cannot be converted to 'Long'.
        Dim x As Long = New C0(Of Integer, Integer)
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing6_2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0(Of T, S)
        Overloads Shared Narrowing Operator CType(x As C0(Of T, S)) As T
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0(Of T, S)) As S
            Return Nothing
        End Operator
        Overloads Shared Narrowing Operator CType(x As C0(Of T, S)) As Integer
            System.Console.WriteLine("CType(x As C0(Of T, S)) As Integer")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As Byte = New C0(Of Integer, Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C0(Of T, S)) As Integer
]]>)
        End Sub

        <Fact>
        Public Sub Widening7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Overloads Shared Narrowing Operator CType(x As C1) As Long
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1
        Overloads Shared Widening Operator CType(x As C2) As UInteger
            Return Nothing
        End Operator
    End Class

    Class C3
        Inherits C2
        Overloads Shared Widening Operator CType(x As C3) As Integer
            Return Nothing
        End Operator
    End Class

    Class C4
        Inherits C3
    End Class

    Sub Main()
        Dim x As Long = New C4()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C4' cannot be converted to 'Long'.
        Dim x As Long = New C4()
                        ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Overloads Shared Narrowing Operator CType(x As C1) As Long
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1
        Overloads Shared Narrowing Operator CType(x As C2) As UInteger
            Return Nothing
        End Operator
    End Class

    Class C3
        Inherits C2
        Overloads Shared Narrowing Operator CType(x As C3) As Integer
            Return Nothing
        End Operator
    End Class

    Class C4
        Inherits C3
    End Class

    Sub Main()
        Dim x As Long = New C4()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.C4' cannot be converted to 'Long'.
        Dim x As Long = New C4()
                        ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub Widening8()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A3
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          uint8  op_Implicit(class A3 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A3.op_Implicit"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A3::op_Implicit

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A3::.ctor

} // end of class A3

.class public auto ansi beforefieldinit A4
       extends A3
{
  .method public hidebysig specialname static 
          uint8  op_Implicit(class A4 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A4.op_Implicit "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A4::op_Implicit

  .method public hidebysig specialname static 
          uint8  op_Explicit(class A4 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A4.op_Implicit "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A4::op_Explicit

  .method public hidebysig specialname static 
          int32  OP_IMPLICIT(class A4 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "int A4.op_Implicit"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A4::OP_IMPLICIT

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A3::.ctor()
    IL_0006:  ret
  } // end of method A4::.ctor

} // end of class A4
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim x12 As Object

        x12 = CInt(New A4())
        x12 = CByte(New A4())
        x12 = CShort(New A4())
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
int A4.op_Implicit
byte A3.op_Implicit
byte A3.op_Implicit
]]>)
        End Sub

        <Fact()>
        Public Sub Narrowing8_1()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A7
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          uint8  op_Explicit(class A7 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A7.op_Explicit"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A7::op_Explicit

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A7::.ctor

} // end of class A7

.class public auto ansi beforefieldinit A8
       extends A7
{
  .method public hidebysig specialname static 
          uint8  op_Explicit(class A8 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A8.op_Explicit "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A8::op_Explicit

  .method public hidebysig specialname static 
          uint8  OP_EXPLICIT(class A8 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A8.OP_EXPLICIT "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A8::OP_EXPLICIT

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A7::.ctor()
    IL_0006:  ret
  } // end of method A8::.ctor

} // end of class A8

.class public auto ansi beforefieldinit A9
       extends A7
{
  .method public hidebysig specialname static 
          uint8  op_Explicit(class A9 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A9.op_Explicit "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A9::op_Explicit

  .method public hidebysig specialname static 
          uint8  OP_EXPLICIT(class A9 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A9.OP_EXPLICIT "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A9::OP_EXPLICIT

  .method public hidebysig specialname static 
          int32  op_ExpliciT(class A9 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "int A9.op_ExpliciT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A9::op_ExpliciT

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A7::.ctor()
    IL_0006:  ret
  } // end of method A9::.ctor

} // end of class A9
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim x12 As Object

        x12 = CInt(New A8())
        x12 = CByte(New A8())
        x12 = CShort(New A8())

        x12 = CInt(New A9())
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
byte A7.op_Explicit
byte A7.op_Explicit
byte A7.op_Explicit
int A9.op_ExpliciT
]]>)
        End Sub

        <Fact()>
        Public Sub Narrowing8_2()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A7
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          uint8  op_Explicit(class A7 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A7.op_Explicit"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A7::op_Explicit

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A7::.ctor

} // end of class A7

.class public auto ansi beforefieldinit A9
       extends A7
{
  .method public hidebysig specialname static 
          uint8  op_Explicit(class A9 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A9.op_Explicit "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A9::op_Explicit

  .method public hidebysig specialname static 
          uint8  OP_EXPLICIT(class A9 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "byte A9.OP_EXPLICIT "
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A9::OP_EXPLICIT

  .method public hidebysig specialname static 
          int32  op_ExpliciT(class A9 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "int A9.op_ExpliciT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A9::op_ExpliciT

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A7::.ctor()
    IL_0006:  ret
  } // end of method A9::.ctor

} // end of class A9
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim x12 As Object

        x12 = CByte(New A9())
        x12 = CShort(New A9())
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'A9' cannot be converted to 'Byte'.
        x12 = CByte(New A9())
                    ~~~~~~~~
BC30311: Value of type 'A9' cannot be converted to 'Short'.
        x12 = CShort(New A9())
                     ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub Widening9()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A6
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          uint8  op_Implicit(class A6 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A6::op_Implicit

  .method public hidebysig specialname static 
          uint8  op_Explicit(class A6 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A6::op_Explicit

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A6::.ctor

} // end of class A6
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim x As Byte = New A6()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'A6' cannot be converted to 'Byte'.
        Dim x As Byte = New A6()
                        ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub Narrowing9()
            Dim customIL =
            <![CDATA[
.class public auto ansi beforefieldinit A10
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          uint8  OP_EXPLICIT(class A10 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A10::OP_EXPLICIT

  .method public hidebysig specialname static 
          uint8  op_Explicit(class A10 x) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] uint8 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A10::op_Explicit

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A10::.ctor

} // end of class A10
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim x As Byte = New A10()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'A10' cannot be converted to 'Byte'.
        Dim x As Byte = New A10()
                        ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing10()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C2

        Shared Shadows Narrowing Operator CType(x As Short) As C2
            System.Console.WriteLine("CType(x As Short) As C2")
            Return Nothing
        End Operator

        Shared Shadows Narrowing Operator CType(x As Byte) As C2
            System.Console.WriteLine("CType(x As Byte) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As Integer = 11
        Dim y As C2 = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As Short) As C2
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Integer' to 'Module1.C2'.
        Dim y As C2 = x
                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing11()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C2

        Shared Shadows Narrowing Operator CType(x As C2) As UInteger
            System.Console.WriteLine("CType(x As C2) As UInteger")
            Return Nothing
        End Operator

        Shared Shadows Narrowing Operator CType(x As C2) As Long
            System.Console.WriteLine("CType(x As C2) As Long")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim y As Short = New C2()
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C2) As UInteger
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Module1.C2' to 'Short'.
        Dim y As Short = New C2()
                         ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing12()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Overloads Shared Narrowing Operator CType(x As Integer) As C1
            System.Console.WriteLine("CType(x As Integer) As C1")
            Return Nothing
        End Operator

        Overloads Shared Narrowing Operator CType(x As C1) As Short
            Return Nothing
        End Operator
    End Class

    Class C2
        Inherits C1
        Overloads Shared Narrowing Operator CType(x As Short) As C2
            System.Console.WriteLine("CType(x As Short) As C2")
            Return Nothing
        End Operator

        Overloads Shared Narrowing Operator CType(x As C2) As Byte
            Return Nothing
        End Operator
    End Class

    Class C3
        Inherits C2
    End Class

    Sub Main()
        Dim x As Byte = 11
        Dim y As C3 = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As Short) As C2
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Byte' to 'Module1.C3'.
        Dim y As C3 = x
                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub Narrowing13()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C1
        Overloads Shared Narrowing Operator CType(x As Short) As C1
            System.Console.WriteLine("CType(x As Short) As C1")
            Return Nothing
        End Operator

        Overloads Shared Narrowing Operator CType(x As Long) As C1
            System.Console.WriteLine("CType(x As Long) As C1")
            Return Nothing
        End Operator
    End Class


    Sub Main()
        Dim x As Integer = 11
        Dim y As C1 = x
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As Long) As C1
]]>)
        End Sub

        <Fact>
        Public Sub Lifting1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Structure D1
        Shared Widening Operator CType(x As D1) As Integer
            System.Console.WriteLine("CType(x As D1) As Integer")
            Return Nothing
        End Operator
    End Structure


    Sub Main()
        Dim y As D1? = New D1()
        Dim x As Integer? = y 'BIND1:"y"
        System.Console.WriteLine("-----")
        y = Nothing
        x = y
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Nullable(Of Module1.D1)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsWidening)
            Assert.True(conv.IsNullableValueType)
            Assert.Equal("Function Module1.D1.op_Implicit(x As Module1.D1) As System.Int32", conv.Method.ToTestDisplayString())

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
CType(x As D1) As Integer
-----
]]>)
        End Sub

        <Fact>
        Public Sub Lifting2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Structure D1
        Shared Widening Operator CType(x As D1) As Integer
            Return Nothing
        End Operator
        Shared Widening Operator CType(x As D1) As UInteger
            Return Nothing
        End Operator
        Shared Narrowing Operator CType(x As D1) As Long
            Return Nothing
        End Operator
    End Structure


    Sub Main()
        Dim y As D1? = New D1()
        Dim x As Long? = y
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.D1?' cannot be converted to 'Long?'.
        Dim x As Long? = y
                         ~
</expected>)
        End Sub

        <Fact>
        Public Sub Lifting3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Structure D1
        Shared Narrowing Operator CType(x As D1) As Integer
            System.Console.WriteLine("CType(x As D1) As Integer")
            Return Nothing
        End Operator
    End Structure


    Sub Main()
        Dim y As D1? = New D1()
        Dim x As Integer? = y 'BIND1:"y"
        System.Console.WriteLine("-----")
        y = Nothing
        x = y
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Nullable(Of Module1.D1)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNullableValueType)
            Assert.Equal("Function Module1.D1.op_Explicit(x As Module1.D1) As System.Int32", conv.Method.ToTestDisplayString())

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
CType(x As D1) As Integer
-----
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.D1?' to 'Integer?'.
        Dim x As Integer? = y 'BIND1:"y"
                            ~
BC42016: Implicit conversion from 'Module1.D1?' to 'Integer?'.
        x = y
            ~
</expected>)
        End Sub

        <Fact>
        Public Sub Lifting4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Structure D1
        Shared Widening Operator CType(x As D1?) As Integer
            Return Nothing
        End Operator
        Shared Widening Operator CType(x As D1?) As UInteger
            Return Nothing
        End Operator

        Shared Widening Operator CType(x As D1) As Byte?
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim y As D1? = New D1()
        Dim x As Byte? = y 'BIND1:"y"
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.D1?' cannot be converted to 'Byte?'.
        Dim x As Byte? = y 'BIND1:"y"
                         ~
</expected>)
        End Sub

        <Fact>
        Public Sub Lifting5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Structure D1
        Shared Widening Operator CType(x As D1) As Byte?
            System.Console.WriteLine("CType(x As D1) As Byte?")
            Return Nothing
        End Operator
    End Structure


    Sub Main()
        Dim y As D1? = New D1()
        Dim x As Byte? = y 'BIND1:"y"
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Nullable(Of Module1.D1)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Byte)", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsNarrowing)
            Assert.False(conv.IsNullableValueType)
            Assert.Equal("Function Module1.D1.op_Implicit(x As Module1.D1) As System.Nullable(Of System.Byte)", conv.Method.ToTestDisplayString())

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
CType(x As D1) As Byte?
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.D1?' to 'Byte?'.
        Dim x As Byte? = y 'BIND1:"y"
                         ~
</expected>)
        End Sub

        <Fact>
        Public Sub GenericParam()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class C0
        Overloads Shared Narrowing Operator CType(x As C0) As Integer
            System.Console.WriteLine("CType(x As C0) As Integer")
            Return Nothing
        End Operator
    End Class

    Class C1(Of T As C0)
        Public Shared Sub Test(x As T)
            Dim y As Integer = x
        End Sub
    End Class

    Sub Main()
        C1(Of C0).Test(New C0())
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As C0) As Integer
]]>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Widening Operator CType(x As Integer) As C2
            System.Console.WriteLine("CType(x As Integer) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Test1(x As C1, y As Short)
        System.Console.WriteLine("Test1(x As C1, y As Short)")
    End Sub

    Sub Test1(x As C2, y As Byte)
        System.Console.WriteLine("Test1(x As C2, y As Byte)")
    End Sub

    Sub Main()

        Dim x As Long = &H7FFFFFFFL
        Dim y As Integer = 1
        Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        Test1(x, y)
        Test1(&H7FFFFFFFL, y)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[ 
BC30439: Constant expression not representable in type 'Byte'.
        Dim z1 As C1 = &H7FFFFFFFL
                       ~~~~~~~~~~~
BC30519: Overload resolution failed because no accessible 'Test1' can be called without a narrowing conversion:
    'Public Sub Test1(x As Module1.C1, y As Short)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C1'.
    'Public Sub Test1(x As Module1.C1, y As Short)': Argument matching parameter 'y' narrows from 'Integer' to 'Short'.
    'Public Sub Test1(x As Module1.C2, y As Byte)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C2'.
    'Public Sub Test1(x As Module1.C2, y As Byte)': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
        Test1(x, y)
        ~~~~~
BC42016: Implicit conversion from 'Integer' to 'Byte'.
        Test1(&H7FFFFFFFL, y)
                           ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Widening Operator CType(x As Integer) As C2
            System.Console.WriteLine("CType(x As Integer) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Test1(x As C1, y As Short)
        System.Console.WriteLine("Test1(x As C1, y As Short)")
    End Sub

    Sub Test1(x As C2, y As Byte)
        System.Console.WriteLine("Test1(x As C2, y As Byte)")
    End Sub

    Sub Main()
        Dim x As Long = &H7FFFFFFFL
        Dim y As Integer = 1
        'Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        'Test1(x, y)
        Test1(&H7FFFFFFFL, y)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True),
                             expectedOutput:=
            <![CDATA[
CType(x As Integer) As C2
CType(x As Integer) As C2
Test1(x As C2, y As Byte)
]]>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Widening Operator CType(x As Integer) As C2
            System.Console.WriteLine("CType(x As Integer) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Test1(x As C1, y As Short)
        System.Console.WriteLine("Test1(x As C1, y As Short)")
    End Sub

    Sub Test1(x As C2, y As Byte)
        System.Console.WriteLine("Test1(x As C2, y As Byte)")
    End Sub

    Sub Main()

        Dim x As Long = &H7FFFFFFFL
        Dim y As Integer = 1
        Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        Test1(x, y)
        Test1(&H7FFFFFFFL, y)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[ 
BC30519: Overload resolution failed because no accessible 'Test1' can be called without a narrowing conversion:
    'Public Sub Test1(x As Module1.C1, y As Short)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C1'.
    'Public Sub Test1(x As Module1.C1, y As Short)': Argument matching parameter 'y' narrows from 'Integer' to 'Short'.
    'Public Sub Test1(x As Module1.C2, y As Byte)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C2'.
    'Public Sub Test1(x As Module1.C2, y As Byte)': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
        Test1(x, y)
        ~~~~~
BC30519: Overload resolution failed because no accessible 'Test1' can be called without a narrowing conversion:
    'Public Sub Test1(x As Module1.C1, y As Short)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C1'.
    'Public Sub Test1(x As Module1.C1, y As Short)': Argument matching parameter 'y' narrows from 'Integer' to 'Short'.
    'Public Sub Test1(x As Module1.C2, y As Byte)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C2'.
    'Public Sub Test1(x As Module1.C2, y As Byte)': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
        Test1(&H7FFFFFFFL, y)
        ~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Widening Operator CType(x As Integer) As C2
            System.Console.WriteLine("CType(x As Integer) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Test1(x As C1)
        System.Console.WriteLine("Test1(x As C1)")
    End Sub

    Sub Test1(x As C2)
        System.Console.WriteLine("Test1(x As C2)")
    End Sub

    Sub Main()

        Dim x As Long = &H7FFFFFFFL
        Dim y As Integer = 1
        Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        Test1(x)
        Test1(&H7FFFFFFFL)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[ 
BC30519: Overload resolution failed because no accessible 'Test1' can be called without a narrowing conversion:
    'Public Sub Test1(x As Module1.C1)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C1'.
    'Public Sub Test1(x As Module1.C2)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C2'.
        Test1(x)
        ~~~~~
BC30519: Overload resolution failed because no accessible 'Test1' can be called without a narrowing conversion:
    'Public Sub Test1(x As Module1.C1)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C1'.
    'Public Sub Test1(x As Module1.C2)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C2'.
        Test1(&H7FFFFFFFL)
        ~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator

        Shared Widening Operator CType(x As C1) As C2
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Widening Operator CType(x As Integer) As C2
            System.Console.WriteLine("CType(x As Integer) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Test1(x As C1)
        System.Console.WriteLine("Test1(x As C1)")
    End Sub

    Sub Test1(x As C2)
        System.Console.WriteLine("Test1(x As C2)")
    End Sub

    Sub Main()

        Dim x As Long = &H7FFFFFFFL
        Dim y As Integer = 1
        Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        Test1(x)
        Test1(&H7FFFFFFFL)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[ 
BC30519: Overload resolution failed because no accessible 'Test1' can be called without a narrowing conversion:
    'Public Sub Test1(x As Module1.C1)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C1'.
    'Public Sub Test1(x As Module1.C2)': Argument matching parameter 'x' narrows from 'Long' to 'Module1.C2'.
        Test1(x)
        ~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator

        Shared Widening Operator CType(x As C1) As C2
            System.Console.WriteLine("CType(x As Byte) As C1")
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Widening Operator CType(x As Integer) As C2
            System.Console.WriteLine("CType(x As Integer) As C2")
            Return Nothing
        End Operator
    End Class

    Sub Test1(x As C1)
        System.Console.WriteLine("Test1(x As C1)")
    End Sub

    Sub Test1(x As C2)
        System.Console.WriteLine("Test1(x As C2)")
    End Sub

    Sub Main()

        Dim x As Long = &H7FFFFFFFL
        Dim y As Integer = 1
        Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        Test1(&H7FFFFFFFL)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False),
                             expectedOutput:=
            <![CDATA[
CType(x As Byte) As C1
CType(x As Integer) As C2
CType(x As Byte) As C1
Test1(x As C1)
]]>)
        End Sub

        <Fact>
        Public Sub IntegerOverflow7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            Return Nothing
        End Operator
    End Class

    Class C2
        Shared Narrowing Operator CType(x As Byte) As C2
            Return Nothing
        End Operator
    End Class

    Class C3
        Inherits C1
    End Class


    Sub Main()
        Dim z1 As C1 = &H7FFFFFFFL
        Dim z2 As C2 = &H7FFFFFFFL
        Dim z3 As C3 = &H7FFFFFFFL
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[ 
BC42016: Implicit conversion from 'Long' to 'Module1.C2'.
        Dim z2 As C2 = &H7FFFFFFFL
                       ~~~~~~~~~~~
BC42016: Implicit conversion from 'Long' to 'Module1.C3'.
        Dim z3 As C3 = &H7FFFFFFFL
                       ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions0()
            Dim compilationDef =
<compilation name="BooleanExpressions0">
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System

Class MyBool
    Public Shared Widening Operator CType(x As boolean) As MyBool
        Console.WriteLine("Widening")
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(x As MyBool) As Boolean
        Console.WriteLine("Narrowing")
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As MyBool) As Boolean
        Console.WriteLine("IsTrue")
        Return False
    End Operator

    Public Shared Operator IsFalse(x As MyBool) As Boolean
        Console.WriteLine("IsFalse")
        Return true
    End Operator
End Class

Module Module1
    Sub Main()
        Dim x As New MyBool

        If x Then 'BIND1:"x"
            Console.WriteLine("If")
        Else
            Console.WriteLine("Else")
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe,
                             expectedOutput:=
            <![CDATA[
IsTrue
Else
]]>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Module Module1
    Sub Main()
        Dim x As Boolean?

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New Boolean?(False)

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New Boolean?(True)

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe,
                             expectedOutput:=
            <![CDATA[
Else
Else
If
]]>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Widening Operator CType(x As S8) As Boolean?
            System.Console.WriteLine("Widening Operator CType(x As S8) As Boolean?")
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As New S8

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub

    Sub Test()
        Dim x As S8

        If x Then
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Widening Operator CType(x As S8) As Boolean?
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(node)

            Assert.Equal("Module1.S8", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Boolean)", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(node)
            Assert.True(conv.IsUserDefined)
            Assert.Equal("Function Module1.S8.op_Implicit(x As Module1.S8) As System.Nullable(Of System.Boolean)", conv.Method.ToTestDisplayString())

            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.Equal("x As Module1.S8", symbolInfo.Symbol.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub BooleanExpressions3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As New S8

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
IsTrue(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(node)

            Assert.Equal("Module1.S8", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("Module1.S8", typeInfo.ConvertedType.ToTestDisplayString())
            Dim conv = model.GetConversion(node)
            Assert.True(conv.IsIdentity)
            Assert.Null(conv.Method)

            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.Equal("x As Module1.S8", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub BooleanExpressions4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x As New S8

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Narrowing Operator CType(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S8' to 'Boolean'.
        If x Then
           ~
</expected>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8) As Boolean?
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean?")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As New S8

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
IsTrue(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8) As Boolean?
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean?")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x As New S8

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Narrowing Operator CType(x As S8) As Boolean?
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S8' to 'Boolean?'.
        If x Then
           ~
</expected>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Widening Operator CType(x As S8) As Boolean?
            System.Console.WriteLine("Widening Operator CType(x As S8) As Boolean?")
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New S8?(New S8())

        If x Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Else
IsTrue(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

        End Sub

        <Fact>
        Public Sub BooleanExpressions8()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Widening Operator CType(x As S8) As Boolean?
            System.Console.WriteLine("Widening Operator CType(x As S8) As Boolean?")
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        x = New S8?(New S8())

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Nullable(Of Module1.S8)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Boolean)", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsNarrowing)
            Assert.False(conv.IsNullableValueType)
            Assert.Equal("Function Module1.S8.op_Implicit(x As Module1.S8) As System.Nullable(Of System.Boolean)", conv.Method.ToTestDisplayString())

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Widening Operator CType(x As S8) As Boolean?
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S8?' to 'Boolean?'.
        If x Then 'BIND1:"x"
           ~
</expected>)

        End Sub

        <Fact>
        Public Sub BooleanExpressions9()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New S8?(New S8())

        If x Then 
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim x_node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(x_node)

            Assert.Equal("System.Nullable(Of Module1.S8)", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Boolean)", typeInfo.ConvertedType.ToTestDisplayString())

            Dim conv = model.GetConversion(x_node)
            Assert.True(conv.IsUserDefined)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNullableValueType)
            Assert.Equal("Function Module1.S8.op_Explicit(x As Module1.S8) As System.Boolean", conv.Method.ToTestDisplayString())

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Else
Narrowing Operator CType(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S8?' to 'Boolean?'.
        If x Then 'BIND1:"x"
           ~
BC42016: Implicit conversion from 'Module1.S8?' to 'Boolean?'.
        If x Then 
           ~
</expected>)

        End Sub

        <Fact>
        Public Sub BooleanExpressions10()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Widening Operator CType(x As S8) As Boolean
            System.Console.WriteLine("Widening Operator CType(x As S8) As Boolean")
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(x As S8) As Boolean?
            System.Console.WriteLine("Narrowing Operator CType(x As S8) As Boolean?")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New S8?(New S8())

        If x Then 
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Else
IsTrue(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

        End Sub

        <Fact>
        Public Sub BooleanExpressions11()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Widening Operator CType(x As S8?) As Boolean?
            System.Console.WriteLine("Widening Operator CType(x As S8?) As Boolean?")
            Return Nothing
        End Operator

        Public Shared Narrowing Operator CType(x As S8?) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8?) As Boolean")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New S8?(New S8())

        If x Then 
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Widening Operator CType(x As S8?) As Boolean?
Else
Widening Operator CType(x As S8?) As Boolean?
Else
]]>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions12()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8?) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8?) As Boolean")
            Return Nothing
        End Operator

        Public Shared Operator IsTrue(x As S8) As Boolean
            System.Console.WriteLine("IsTrue(x As S8) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsFalse(x As S8) As Boolean
            System.Console.WriteLine("IsFalse(x As S8) As Boolean")
            Return False
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New S8?(New S8())

        If x Then 
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Else
IsTrue(x As S8) As Boolean
Else
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

        End Sub

        <Fact>
        Public Sub BooleanExpressions13()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
        Public Shared Narrowing Operator CType(x As S8?) As Boolean
            System.Console.WriteLine("Narrowing Operator CType(x As S8?) As Boolean")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        x = New S8?(New S8())

        If x Then 
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Narrowing Operator CType(x As S8?) As Boolean
Else
Narrowing Operator CType(x As S8?) As Boolean
Else
]]>)
        End Sub

        <Fact>
        Public Sub BooleanExpressions14()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Structure S8
    End Structure

    Sub Main()
        Dim x As S8? = Nothing

        If x Then 'BIND1:"x"
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.S8?' cannot be converted to 'Boolean'.
        If x Then 'BIND1:"x"
           ~
</expected>)
        End Sub

        <Fact>
        Public Sub ParamArrayConversion()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Class B1
        Shared Widening Operator CType(x As B1) As B2()
            System.Console.WriteLine("CType(x As B1) As B2()")
            Return New B2() {}
        End Operator
    End Class

    Class B2
    End Class

    Sub Test(ParamArray x As B2())
        System.Console.WriteLine("Test: {0}", x.GetType())
    End Sub

    Sub Main()
        Test(New B1())
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
CType(x As B1) As B2()
Test: Module1+B2[]
]]>)

            verifier.VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  newobj     "Sub Module1.B1..ctor()"
  IL_0005:  call       "Function Module1.B1.op_Implicit(Module1.B1) As Module1.B2()"
  IL_000a:  call       "Sub Module1.Test(ParamArray Module1.B2())"
  IL_000f:  ret
}
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub Bug13172()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Module Module1
 
    Structure S1
        Shared Widening Operator CType(x As Integer) As S1
            System.Console.WriteLine("CType(x As Integer) As S1")
            Return Nothing
        End Operator
 
        Shared Widening Operator CType(x As S1) As Integer
            System.Console.WriteLine("CType(x As S1) As Integer")
            Return Nothing
        End Operator
    End Structure
 
    Sub Main()
        Dim s1 As S1? = Nothing
        Dim l As Long? = Nothing
        Dim s As Short? = Nothing
 
        s1 = l ' produces Nothing
        System.Console.WriteLine(s1.HasValue)
        s1 = s ' produces Nothing
        System.Console.WriteLine(s1.HasValue)
        l = s1 ' produces Nothing
        System.Console.WriteLine(l.HasValue)
        s = s1 ' InvalidOperationException - Nullable object must have a value.
        System.Console.WriteLine(s.HasValue)
     End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
False
False
False
False
]]>)

        End Sub

#Region "Regressions"

        <Fact(), WorkItem(544073, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544073")>
        Public Sub NoReturnInOperatorBody()
            Dim compilationDef =
<compilation name="NoReturnInOperatorBody">
    <file name="a.vb">
Imports System

Friend Module GenOLConv04mod
    Class A
        Public Shared result As String
        Public Shared Narrowing Operator CType(ByVal x As A) As Integer
            result = x.ToString.Length
        End Operator

        Public Shared Operator -(x As A) As Integer
        End Operator ' A2
    End Class

    Class B
        Public Shared Narrowing Operator CType(ByVal x As A) As B
        End Operator ' B

        Public Shared Operator -(x As B) As B
        End Operator ' B2
    End Class

    Class C(Of T)
        Public Shared Narrowing Operator CType(ByVal x As C(Of T)) As T
        End Operator ' C
        Public Shared Operator -(x As C(Of T)) As T
        End Operator ' C2
    End Class

    Class D
        Public Shared Narrowing Operator CType(ByVal x As D) As System.Guid
        End Operator ' D
        Public Shared Operator -(x As D) As System.Guid
        End Operator ' D2
    End Class

    Sub Main()
        Dim x1 As New A()
        Dim str = CType(x1, Integer)
        Console.WriteLine(str)
    End Sub
End Module

Namespace Program
    Class C6
        Public Shared Narrowing Operator CType(ByVal arg As C6) As Exception
        End Operator ' C6
    End Class
End Namespace

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="0")

            AssertTheseDiagnostics(compilation,
<expected>
BC42354: Operator 'CType' doesn't return a value on all code paths. Are you missing a 'Return' statement?
        End Operator
        ~~~~~~~~~~~~
BC42354: Operator '-' doesn't return a value on all code paths. Are you missing a 'Return' statement?
        End Operator ' A2
        ~~~~~~~~~~~~
BC42106: Operator 'CType' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Operator ' B
        ~~~~~~~~~~~~
BC42106: Operator '-' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Operator ' B2
        ~~~~~~~~~~~~
BC42106: Operator 'CType' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Operator ' C6
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(56376, "https://github.com/dotnet/roslyn/issues/56376")>
        <Fact>
        Public Sub UserDefinedConversionOperatorInGenericExpressionTree_01()
            Dim compilationDef =
<compilation name="OperatorsWithDefaultValuesAreNotBound">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Public Class Program
    Public Shared Sub Main()
        GenericMethod(Of String)()
    End Sub
    
    Private Shared Sub GenericMethod(Of T)()
        Dim func As Func(Of Expression(Of Func(Of T, C(Of T)))) =
            Function ()
                Return Function(x) x 
            End Function
            
        func().Compile()(Nothing)
    End Sub
End Class

Class C(Of T)
    Public Shared Widening Operator CType(t1 As T) As C(Of T)
        Console.Write("Run")
        Return Nothing 
    End Operator
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilation(compilationDef, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics()

            CompileAndVerify(compilation, expectedOutput:="Run")
        End Sub

        <WorkItem(56376, "https://github.com/dotnet/roslyn/issues/56376")>
        <Fact>
        Public Sub UserDefinedConversionOperatorInGenericExpressionTree_02()
            Dim compilationDef =
<compilation name="OperatorsWithDefaultValuesAreNotBound">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Public Class Program
    Public Shared Sub Main()
        GenericMethod(Of String)()
    End Sub
    
    Private Shared Sub GenericMethod(Of T)()
        Dim func As Func(Of Expression(Of Func(Of T, C(Of T)))) =
            Function ()
                Return Function(x) CType(x, C(Of T))
            End Function
            
        func().Compile()(Nothing)
    End Sub
End Class

Class C(Of T)
    Public Shared Narrowing Operator CType(t1 As T) As C(Of T)
        Console.Write("Run")
        Return Nothing 
    End Operator
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilation(compilationDef, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics()

            CompileAndVerify(compilation, expectedOutput:="Run")
        End Sub

#End Region
    End Class

End Namespace

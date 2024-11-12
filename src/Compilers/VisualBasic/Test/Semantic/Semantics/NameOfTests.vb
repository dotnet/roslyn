' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class NameOfTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestParsing_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Integer.MaxValue)
        Dim y = NameOf(Integer)
        Dim z = NameOf(Variant)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37244: This expression does not have a name.
        Dim y = NameOf(Integer)
                       ~~~~~~~
BC30804: 'Variant' is no longer a supported type; use the 'Object' type instead.
        Dim z = NameOf(Variant)
                       ~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().ToArray()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            If True Then
                Dim node1 = nodes(0)
                Assert.Equal("NameOf(Integer.MaxValue)", node1.ToString())
                Assert.Equal("MaxValue", model.GetConstantValue(node1).Value)

                typeInfo = model.GetTypeInfo(node1)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

                symbolInfo = model.GetSymbolInfo(node1)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                group = model.GetMemberGroup(node1)
                Assert.True(group.IsEmpty)

                Dim argument = node1.Argument

                typeInfo = model.GetTypeInfo(argument)
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

                symbolInfo = model.GetSymbolInfo(argument)
                Assert.Equal("System.Int32.MaxValue As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                group = model.GetMemberGroup(argument)
                Assert.True(group.IsEmpty)

                Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

                typeInfo = model.GetTypeInfo(receiver)
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

                symbolInfo = model.GetSymbolInfo(receiver)
                Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            End If

            If True Then
                Dim node2 = nodes(1)
                Assert.Equal("NameOf(Integer)", node2.ToString())
                Assert.Null(model.GetConstantValue(node2).Value)

                typeInfo = model.GetTypeInfo(node2)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

                symbolInfo = model.GetSymbolInfo(node2)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                group = model.GetMemberGroup(node2)
                Assert.True(group.IsEmpty)

                Dim argument = node2.Argument

                typeInfo = model.GetTypeInfo(argument)
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

                symbolInfo = model.GetSymbolInfo(argument)
                Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                group = model.GetMemberGroup(argument)
                Assert.True(group.IsEmpty)
            End If

            If True Then
                Dim node3 = nodes(2)
                Assert.Equal("NameOf(Variant)", node3.ToString())
                Assert.Equal("Variant", model.GetConstantValue(node3).Value)

                typeInfo = model.GetTypeInfo(node3)
                Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

                symbolInfo = model.GetSymbolInfo(node3)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                group = model.GetMemberGroup(node3)
                Assert.True(group.IsEmpty)

                Dim argument = node3.Argument

                typeInfo = model.GetTypeInfo(argument)
                Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind)

                symbolInfo = model.GetSymbolInfo(argument)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                group = model.GetMemberGroup(argument)
                Assert.True(group.IsEmpty)
            End If
        End Sub

        <Fact>
        Public Sub TestParsing_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of ))
        Dim y = NameOf(C2(Of ).C3(Of Integer))
        Dim z = NameOf(C2(Of Integer).C3(Of Integer))
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)

    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30182: Type expected.
        Dim x = NameOf(C2(Of Integer).C3(Of ))
                                            ~
BC30182: Type expected.
        Dim y = NameOf(C2(Of ).C3(Of Integer))
                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of ).M1)
        Dim y = NameOf(C2(Of ).C3(Of Integer).M1)
        Dim z = NameOf(C2(Of Integer).C3(Of Integer).M1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Sub M1()
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30182: Type expected.
        Dim x = NameOf(C2(Of Integer).C3(Of ).M1)
                                            ~
BC30182: Type expected.
        Dim y = NameOf(C2(Of ).C3(Of Integer).M1)
                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Global)
        Dim y = NameOf(Global.System)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC36000: 'Global' must be followed by '.' and an identifier.
        Dim x = NameOf(Global)
                       ~~~~~~
BC37244: This expression does not have a name.
        Dim x = NameOf(Global)
                       ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module

Class CTest
    Sub Test1()
        Dim x = NameOf(MyClass)
        Dim y = NameOf(MyClass.Test1)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC32028: 'MyClass' must be followed by '.' and an identifier.
        Dim x = NameOf(MyClass)
                       ~~~~~~~
BC37244: This expression does not have a name.
        Dim x = NameOf(MyClass)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module

Class CTest
    Sub Test1()
        Dim x = NameOf(MyBase)
        Dim y = NameOf(MyBase.GetHashCode)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC32027: 'MyBase' must be followed by '.' and an identifier.
        Dim x = NameOf(MyBase)
                       ~~~~~~
BC37244: This expression does not have a name.
        Dim x = NameOf(MyBase)
                       ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_07()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module

Class CTest
    Sub Test1()
        Dim x = NameOf(Me)
        Dim y = NameOf(Me.GetHashCode)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37244: This expression does not have a name.
        Dim x = NameOf(Me)
                       ~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_08()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Integer?)
        Dim y = NameOf(Integer?.GetValueOrDefault)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37244: This expression does not have a name.
        Dim x = NameOf(Integer?)
                       ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_09()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As Integer? = Nothing
        Dim y = NameOf(x.GetValueOrDefault)
        Dim z = NameOf((x).GetValueOrDefault)
        Dim u = NameOf(New Integer?().GetValueOrDefault)
        Dim v = NameOf(GetVal().GetValueOrDefault)
        Dim w = NameOf(GetVal.GetValueOrDefault)
    End Sub

    Function GetVal() As Integer?
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37245: This sub-expression cannot be used inside NameOf argument.
        Dim z = NameOf((x).GetValueOrDefault)
                       ~~~
BC37245: This sub-expression cannot be used inside NameOf argument.
        Dim u = NameOf(New Integer?().GetValueOrDefault)
                       ~~~~~~~~~~~~~~
BC37245: This sub-expression cannot be used inside NameOf argument.
        Dim v = NameOf(GetVal().GetValueOrDefault)
                       ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_10()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As Integer? = Nothing
        NameOf(x.GetValueOrDefault)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30035: Syntax error.
        NameOf(x.GetValueOrDefault)
        ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Namespace_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(Global.System))
        System.Console.WriteLine(NameOf(Global.system))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
System
system
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(Global.System)", node1.ToString())
            Assert.Equal("System", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("System", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("Global", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub Method_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of Integer).C3(Of Short).M1))
        System.Console.WriteLine(NameOf(C2(Of Integer).C3(Of Short).m1))
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Sub M1()
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
m1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short).M1)", node1.ToString())
            Assert.Equal("M1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Sub C2(Of System.Int32).C3(Of System.Int16).M1()", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(1, group.Length)
            Assert.Equal("Sub C2(Of System.Int32).C3(Of System.Int16).M1()", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            receiver = DirectCast(receiver, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub Method_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1))
        System.Console.WriteLine(NameOf(C1.m1))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub

    Sub M1(x as Integer)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
m1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C1.M1)", node1.ToString())
            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub C1.M1(Of T)()", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub C1.M1(x As System.Int32)", symbolInfo.CandidateSymbols(1).ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(2, group.Length)
            Assert.Equal("Sub C1.M1(Of T)()", group(0).ToTestDisplayString())
            Assert.Equal("Sub C1.M1(x As System.Int32)", group(1).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Method_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C1.M1)", node1.ToString())
            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Sub C1.M1(Of T)()", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Sub C1.M1(Of T)()", group.Single.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Method_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC37246: Method type arguments unexpected.
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
                                             ~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C1.M1(Of Integer))", node1.ToString())
            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Sub C1.M1(Of System.Int32)()", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Sub C1.M1(Of System.Int32)()", group.Single.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Method_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub

    Sub M1(x as Integer)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC37246: Method type arguments unexpected.
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
                                             ~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C1.M1(Of Integer))", node1.ToString())
            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Sub C1.M1(Of System.Int32)()", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Sub C1.M1(Of System.Int32)()", group.Single.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Method_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
    End Sub
End Module

Class C1
    Sub M1(x as Integer)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC37246: Method type arguments unexpected.
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
                                             ~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C1.M1(Of Integer))", node1.ToString())
            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub GenericType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of Integer).C3(Of Short)))
        System.Console.WriteLine(NameOf(C2(Of Integer).c3(Of Short)))
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
C3
c3
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short))", node1.ToString())
            Assert.Equal("C3", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub AmbiguousType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of Integer).CC3))
        System.Console.WriteLine(NameOf(C2(Of Integer).cc3))
    End Sub
End Module

Class C2(Of T)
    Class Cc3(Of S)
    End Class

    Class cC3(Of U, V)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC32042: Too few type arguments to 'C2(Of Integer).Cc3(Of S)'.
        System.Console.WriteLine(NameOf(C2(Of Integer).CC3))
                                        ~~~~~~~~~~~~~~~~~~
BC32042: Too few type arguments to 'C2(Of Integer).Cc3(Of S)'.
        System.Console.WriteLine(NameOf(C2(Of Integer).cc3))
                                        ~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).CC3)", node1.ToString())
            Assert.Equal("CC3", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("C2(Of System.Int32).Cc3(Of S)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.WrongArity, symbolInfo.CandidateReason)
            Assert.Equal("C2(Of System.Int32).Cc3(Of S)", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub AmbiguousType_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.CC3))
        System.Console.WriteLine(NameOf(C2.cc3))
    End Sub
End Module

Class C1
    Class Cc3(Of S)
    End Class
End Class

Class C2
    Inherits C1

    Class cC3(Of U, V)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC32042: Too few type arguments to 'C2.cC3(Of U, V)'.
        System.Console.WriteLine(NameOf(C2.CC3))
                                        ~~~~~~
BC32042: Too few type arguments to 'C2.cC3(Of U, V)'.
        System.Console.WriteLine(NameOf(C2.cc3))
                                        ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InaccessibleNonGenericType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.CC3))
    End Sub
End Module

Class C2
    protected Class Cc3
    End Class

    Class cC3(Of U, V)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2.Cc3' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(NameOf(C2.CC3))
                                        ~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.CC3)", node1.ToString())
            Assert.Equal("CC3", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("C2.Cc3", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
            Assert.Equal("C2.Cc3", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub Alias_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports [alias] = System

Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf([alias]))
        System.Console.WriteLine(NameOf([ALIAS]))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
alias
ALIAS
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf([alias])", node1.ToString())
            Assert.Equal("alias", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("System", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Assert.Equal("[alias]=System", model.GetAliasInfo(argument).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub InaccessibleMethod_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).M1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected Sub M1()
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30390: 'C3.Protected Sub M1()' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).M1)
                                                   ~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short).M1)", node1.ToString())
            Assert.Equal("M1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
            Assert.Equal("Sub C2(Of System.Int32).C3(Of System.Int16).M1()", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(1, group.Length)
            Assert.Equal("Sub C2(Of System.Int32).C3(Of System.Int16).M1()", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            receiver = DirectCast(receiver, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InaccessibleProperty_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).P1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected Property P1 As Integer
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2(Of Integer).C3(Of Short).P1' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).P1)
                                                   ~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short).P1)", node1.ToString())
            Assert.Equal("P1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
            Assert.Equal("Property C2(Of System.Int32).C3(Of System.Int16).P1 As System.Int32", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(1, group.Length)
            Assert.Equal("Property C2(Of System.Int32).C3(Of System.Int16).P1 As System.Int32", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            receiver = DirectCast(receiver, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InaccessibleField_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).F1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected F1 As Integer
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2(Of Integer).C3(Of Short).F1' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).F1)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short).F1)", node1.ToString())
            Assert.Equal("F1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16).F1 As System.Int32", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            receiver = DirectCast(receiver, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InaccessibleEvent_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).E1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected Event E1 As System.Action
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2(Of Integer).C3(Of Short).E1' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).E1)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short).E1)", node1.ToString())
            Assert.Equal("E1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
            Assert.Equal("Event C2(Of System.Int32).C3(Of System.Int16).E1 As System.Action", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            receiver = DirectCast(receiver, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub Missing_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).Missing)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30456: 'Missing' is not a member of 'C2(Of Integer).C3(Of Short)'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).Missing)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2(Of Integer).C3(Of Short).Missing)", node1.ToString())
            Assert.Equal("Missing", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32).C3(Of System.Int16)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            receiver = DirectCast(receiver, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub Missing_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Missing.M1)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30451: 'Missing' is not declared. It may be inaccessible due to its protection level.
        Dim x = NameOf(Missing.M1)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Missing)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30451: 'Missing' is not declared. It may be inaccessible due to its protection level.
        Dim x = NameOf(Missing)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousMethod_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(Ambiguous))
    End Sub
End Module

Module Module2
    Sub Ambiguous()
    End Sub
End Module

Module Module3
    Sub Ambiguous()
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30562: 'Ambiguous' is ambiguous between declarations in Modules 'Module2, Module3'.
        System.Console.WriteLine(NameOf(Ambiguous))
                                        ~~~~~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(Ambiguous)", node1.ToString())
            Assert.Equal("Ambiguous", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Void", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason)
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub Module2.Ambiguous()", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub Module3.Ambiguous()", symbolInfo.CandidateSymbols(1).ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub AmbiguousMethod_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(I3.Ambiguous))
    End Sub
End Module

Interface I1
    Sub Ambiguous()
End Interface

Interface I2
    Sub Ambiguous(x as Integer)
End Interface

Interface I3
    Inherits I1, I2
End Interface
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
Ambiguous
]]>)
        End Sub

        <Fact>
        Public Sub Local_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim local As Integer = 0
        System.Console.WriteLine(NameOf(LOCAL))
        System.Console.WriteLine(NameOf(loCal))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
loCal
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(LOCAL)", node1.ToString())
            Assert.Equal("LOCAL", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("local As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Local_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(LOCAL))
        Dim local As Integer = 0
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC32000: Local variable 'local' cannot be referred to before it is declared.
        System.Console.WriteLine(NameOf(LOCAL))
                                        ~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(LOCAL)", node1.ToString())
            Assert.Equal("LOCAL", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("local As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Local_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim local = NameOf(LOCAL)
        System.Console.WriteLine(local)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30980: Type of 'local' cannot be inferred from an expression containing 'local'.
        Dim local = NameOf(LOCAL)
                           ~~~~~
</expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(LOCAL)", node1.ToString())
            Assert.Equal("LOCAL", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("local As System.String", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Local_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(LOCAL))
        local = 0
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(LOCAL)", node1.ToString())
            Assert.Equal("LOCAL", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Object", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("LOCAL As System.Object", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Local_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        local = 3
        System.Console.WriteLine(NameOf(LOCAL))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)
        End Sub

        <Fact>
        Public Sub Local_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        local = NameOf(LOCAL)
        System.Console.WriteLine(local)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)
        End Sub

        <Fact>
        Public Sub TypeParameterAsQualifier_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        C3(Of C2).Test()
    End Sub
End Module

Class C2
    Sub M1()
    End Sub
End Class

Class C3(Of T As C2)
    Shared Sub Test()
        System.Console.WriteLine(NameOf(T.M1))
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC32098: Type parameters cannot be used as qualifiers.
        System.Console.WriteLine(NameOf(T.M1))
                                        ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.F1))
    End Sub
End Module

Class C2
    Public F1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.F1)", node1.ToString())
            Assert.Equal("F1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("C2.F1 As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InstanceOfType_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.F1.F2))
    End Sub
End Module

Class C2
    Public F1 As C3
End Class

Class C3
    Public F2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.F1.F2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.P1))
    End Sub
End Module

Class C2
    Public Property P1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
P1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.P1)", node1.ToString())
            Assert.Equal("P1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Property C2.P1 As System.Int32", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Property C2.P1 As System.Int32", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InstanceOfType_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.P1.P2))
    End Sub
End Module

Class C2
    Public Property P1 As C3
End Class

Class C3
    Public Property P2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.P1.P2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1))
    End Sub
End Module

Class C2
    Public Function M1() As Integer
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.M1)", node1.ToString())
            Assert.Equal("M1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Function C2.M1() As System.Int32", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Function C2.M1() As System.Int32", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InstanceOfType_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1.M2))
    End Sub
End Module

Class C2
    Public Function M1() As C3
        Return Nothing
    End Function
End Class

Class C3
    Public Sub M2()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.M1.M2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_07()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1))
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Function M1(this As C2) As Integer
        Return Nothing
    End Function
End Module

Class C2
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {Net40.References.SystemCore}, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.M1)", node1.ToString())
            Assert.Equal("M1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Function C2.M1() As System.Int32", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Function C2.M1() As System.Int32", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InstanceOfType_08()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1.M2))
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Function M1(this As C2) As C3
        Return Nothing
    End Function
End Module

Class C2
End Class

Class C3
    Public Sub M2()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {Net40.References.SystemCore}, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.M1.M2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_09()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.E1))
    End Sub
End Module

Class C2
    Public Event E1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
E1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.E1)", node1.ToString())
            Assert.Equal("E1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Action", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("Event C2.E1 As System.Action", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("C2", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("C2", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub InstanceOfType_10()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.E1.Invoke))
    End Sub
End Module

Class C2
    Public Event E1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.E1.Invoke))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.F1))
        System.Console.WriteLine(NameOf(x.F1.F2))
    End Sub
End Module

Class C2
    Shared Public F1 As C3
End Class

Class C3
    Shared Public F2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.F1))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.F1.F2))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.F1.F2))
                                        ~~~~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
F2
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.P1))
        System.Console.WriteLine(NameOf(x.P1.P2))
    End Sub
End Module

Class C2
    Shared Public Property P1 As C3
End Class

Class C3
    Shared Public Property P2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.P1.P2))
                                        ~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
P1
P2
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.M1))
        System.Console.WriteLine(NameOf(x.M1.M2))
    End Sub
End Module

Class C2
    Shared Public Function M1() As C3
        Return Nothing
    End Function
End Class

Class C3
    Shared Public Sub M2()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.M1.M2))
                                        ~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
M2
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.E1))
    End Sub
End Module

Class C2
    Shared Public Event E1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertNoDiagnostics(comp)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
E1
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.T1))
        System.Console.WriteLine(NameOf(x.P1.T2))
        System.Console.WriteLine(NameOf(x.P1))
    End Sub
End Module

Class C2
    Shared Public Property P1 As C3

    Public Class T1
    End Class
End Class

Class C3
    Public Class T2
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.T1))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.P1.T2))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.P1.T2))
                                        ~~~~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
T1
T2
P1
]]>)
        End Sub

        <Fact>
        Public Sub DataFlow_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As C2
        System.Console.WriteLine(NameOf(x.F1))

        Dim y As C2

        Return 
        System.Console.WriteLine(y.F1)
    End Sub
End Module

Class C2
    Public F1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
]]>).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Attribute_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
<System.Diagnostics.DebuggerDisplay("={" + NameOf(Test.MTest) + "()}")>
Class Test

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    Function MTest() As String
        Return ""
    End Function
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
={MTest()}
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(Test.MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Function Test.MTest() As System.String", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Function Test.MTest() As System.String", group.Single.ToTestDisplayString())

            Dim receiver = DirectCast(argument, MemberAccessExpressionSyntax).Expression

            typeInfo = model.GetTypeInfo(receiver)
            Assert.Equal("Test", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(receiver)
            Assert.Equal("Test", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub Attribute_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
<System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
Class Test

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    Function MTest() As String
        Return ""
    End Function
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30059: Constant expression is required.
<System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30451: 'MTest' is not declared. It may be inaccessible due to its protection level.
<System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
                                                  ~~~~~
]]></expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Attribute_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
<System.Diagnostics.DebuggerDisplay("={" + NameOf(.MTest) + "()}")>
Class Test

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    Function MTest() As String
        Return ""
    End Function
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30059: Constant expression is required.
<System.Diagnostics.DebuggerDisplay("={" + NameOf(.MTest) + "()}")>
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
<System.Diagnostics.DebuggerDisplay("={" + NameOf(.MTest) + "()}")>
                                                  ~~~~~~
]]></expected>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(.MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Attribute_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Class Module1

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    <System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
    Class Test
    End Class

    Function MTest() As String
        Return ""
    End Function
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
={MTest()}
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)
            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Function Module1.MTest() As System.String", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Function Module1.MTest() As System.String", group.Single.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Attribute_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Class Module1

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    <System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
    Class Test
    End Class

    Property MTest As String
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
={MTest()}
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)
            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Property Module1.MTest As System.String", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Property Module1.MTest As System.String", group.Single.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Attribute_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Class Module1

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    <System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
    Class Test
    End Class

    Dim MTest As String
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
={MTest()}
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)
            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("Module1.MTest As System.String", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub Attribute_07()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Class Module1

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    <System.Diagnostics.DebuggerDisplay("={" + NameOf(MTest) + "()}")>
    Class Test
    End Class

    Event MTest As System.Action
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
={MTest()}
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)
            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Action", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("Event Module1.MTest As System.Action", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub InstanceAndExtension()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1))
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Function M1(this As C2) As Integer
        Return Nothing
    End Function
End Module

Class C2
    Sub M1(x as Integer)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {Net40.References.SystemCore}, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(C2.M1)", node1.ToString())
            Assert.Equal("M1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub C2.M1(x As System.Int32)", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal("Function C2.M1() As System.Int32", symbolInfo.CandidateSymbols(1).ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal(2, group.Length)
            Assert.Equal("Sub C2.M1(x As System.Int32)", group(0).ToTestDisplayString())
            Assert.Equal("Function C2.M1() As System.Int32", group(1).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub InstanceInShared_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class Module1
    Shared Sub Main()
        System.Console.WriteLine(NameOf(F1))
    End Sub

    Public F1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(F1)", node1.ToString())
            Assert.Equal("F1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("Module1.F1 As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub InstanceInShared_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class Module1
    Shared Sub Main()
        System.Console.WriteLine(NameOf(F1))
    End Sub

    Event F1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
]]>)

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(F1)", node1.ToString())
            Assert.Equal("F1", model.GetConstantValue(node1).Value)

            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Equal("System.Action", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Equal("Event Module1.F1 As System.Action", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            group = model.GetMemberGroup(argument)
            Assert.Equal(0, group.Length)
        End Sub

        <Fact>
        Public Sub InstanceInShared_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class Module1
    Shared Sub Main()
        System.Console.WriteLine(NameOf(MTest))
    End Sub

    Function MTest() As String
        Return ""
    End Function
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
MTest
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)
            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Function Module1.MTest() As System.String", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Function Module1.MTest() As System.String", group.Single.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub InstanceInShared_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class Module1
    Shared Sub Main()
        System.Console.WriteLine(NameOf(MTest))
    End Sub

    Property MTest As String
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
MTest
]]>).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.NameOfExpression).Cast(Of NameOfExpressionSyntax)().First()

            Dim typeInfo As TypeInfo
            Dim symbolInfo As SymbolInfo
            Dim group As ImmutableArray(Of ISymbol)

            Assert.Equal("NameOf(MTest)", node1.ToString())
            Assert.Equal("MTest", model.GetConstantValue(node1).Value)
            typeInfo = model.GetTypeInfo(node1)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(node1)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

            group = model.GetMemberGroup(node1)
            Assert.True(group.IsEmpty)

            Dim argument = node1.Argument

            typeInfo = model.GetTypeInfo(argument)
            Assert.Null(typeInfo.Type)

            symbolInfo = model.GetSymbolInfo(argument)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason)
            Assert.Equal("Property Module1.MTest As System.String", symbolInfo.CandidateSymbols.Single.ToTestDisplayString())

            group = model.GetMemberGroup(argument)
            Assert.Equal("Property Module1.MTest As System.String", group.Single.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(543, "https://github.com/dotnet/roslyn")>
        Public Sub NameOfConstantInInitializer()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class Module1
    Const N1 As String = NameOf(N1)
    Shared Sub Main()
        Const N2 As String = NameOf(N2)
        System.Console.WriteLine(N1 &amp; N2)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
N1N2
]]>).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(564, "https://github.com/dotnet/roslyn/issues/564")>
        Public Sub NameOfTypeParameterInDefaultValue()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Program
    Sub M(Of TP)(Optional name As String = NameOf(TP))
        System.Console.WriteLine(name)
    End Sub
    Sub Main()
        M(Of String)()
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
TP
]]>).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(10839, "https://github.com/dotnet/roslyn/issues/10839")>
        Public Sub NameOfByRefInLambda()
            Dim compilationDef =
                <compilation>
                    <file name="a.vb">
Module Program
    Sub DoSomething(ByRef x As Integer)
        Dim f = Function()
                    Return NameOf(x)
                End Function
        System.Console.WriteLine(f())
    End Sub
    Sub Main()
        Dim x =  5
        DoSomething(x)
    End Sub
End Module
                </file>
                </compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:="x").VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(10839, "https://github.com/dotnet/roslyn/issues/10839")>
        Public Sub NameOfByRefInQuery()
            Dim compilationDef =
                <compilation>
                    <file name="a.vb">
Imports System.Linq

Module Program
    Sub DoSomething(ByRef x As Integer)
        Dim f = from y in {1, 2, 3}
                select nameof(x)
        System.Console.WriteLine(f.Aggregate("", Function(a, b) a + b))
    End Sub
    Sub Main()
        Dim x =  5
        DoSomething(x)
    End Sub
End Module
                </file>
                </compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.DebugExe, additionalRefs:={LinqAssemblyRef})
            CompileAndVerify(comp, expectedOutput:="xxx").VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(10839, "https://github.com/dotnet/roslyn/issues/10839")>
        Public Sub ForbidInstanceQualifiedFromTypeInNestedExpression()
            Dim compilationDef =
                <compilation>
                    <file name="a.vb">
Class C
    Public MyInstance As String
    Public Shared MyStatic As String

    Sub M()
        Dim X As String
        X = NameOf(C.MyInstance)
        X = NameOf(C.MyInstance.Length)
        X = NameOf(C.MyStatic)
        X = NameOf(C.MyStatic.Length)
    End Sub
End Class
Class C(Of T)
    Public MyInstance As String
    Public Shared MyStatic As String

    Sub M()
        Dim X As String
        X = NameOf(C(Of Integer).MyInstance)
        X = NameOf(C(Of Integer).MyInstance.Length)
        X = NameOf(C(Of Integer).MyStatic)
        X = NameOf(C(Of Integer).MyStatic.Length)
    End Sub
End Class
                </file>
                </compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.DebugDll)

            AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
        X = NameOf(C.MyInstance.Length)
                   ~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        X = NameOf(C(Of Integer).MyInstance.Length)
                   ~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(23019, "https://github.com/dotnet/roslyn/issues/23019")>
        Public Sub NameOfInAsync()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
    Sub Main()
        M().GetAwaiter().GetResult()
    End Sub
    Async Function M() As Task
        Console.WriteLine(NameOf(M))
        Await Task.CompletedTask
    End Function
End Module
    </file>
</compilation>

            Dim comp = CreateCompilation(compilationDef, options:=TestOptions.DebugExe)
            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M
]]>).VerifyDiagnostics()
        End Sub
    End Class
End Namespace

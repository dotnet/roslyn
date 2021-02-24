' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class GotoTests
        Inherits FlowTestBase

#Region "ControlFlowPass and DataflowAnalysis"

        <Fact()>
        Public Sub GotoInLambda()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
    <compilation name="GotoInLambda">
        <file name="a.vb">
Imports System
Imports System.Linq
Class C1
    Sub MAIN()
        Dim lists = goo()
        lists.Where(Function(ByVal item)
                        [|GoTo lab1|]
                        For Each item In lists
                            item = New myattribute1()
lab1:
                        Next
                        Return item.ToString() = ""
                    End Function).ToList()
    End Sub
    Shared Function goo() As List(Of myattribute1)
        Return Nothing
    End Function
End Class
Class myattribute1
    Inherits Attribute
    Implements IDisposable
    Sub dispose() Implements IDisposable.Dispose
    End Sub
End Class
</file>
    </compilation>)
            Dim controlFlowResults = analysis.Item1
            Dim dataFlowResults = analysis.Item2

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("lists, item", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, lists, item", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))

            Assert.Equal(1, controlFlowResults.ExitPoints.Count)
            Assert.Equal(0, controlFlowResults.EntryPoints.Count)
            Assert.False(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)

        End Sub

        <Fact()>
        Public Sub GotoInNestedSyncLock()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
    <compilation name="GotoInNestedSyncLock">
        <file name="a.vb">
Imports System
Imports System.Linq
Delegate Sub MyDelegate(ByRef x As Integer)
Class C1
    Shared Sub Main()
        SyncLock "a"

            [|SyncLock "B"
                GoTo lab1
            End SyncLock
lab1:|]
            Dim x As MyDelegate = Sub(ByRef y As Integer)
lab1:
            End Sub
        End SyncLock
    End Sub
End Class

</file>
    </compilation>)
            Dim controlFlowResults = analysis.Item1
            Dim dataFlowResults = analysis.Item2

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))

            Assert.Equal(0, controlFlowResults.ExitPoints.Count)
            Assert.Equal(0, controlFlowResults.EntryPoints.Count)
            Assert.True(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)

        End Sub

        <Fact()>
        Public Sub GotoCaseElse()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
    <compilation name="GotoCaseElse">
        <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim str As String
        Dim flag1 = 1
        [|GoTo 2
        Select flag1
            Case 1 :
1:              str = "abc"
            Case 2 :
                str = "abc"
2:          Case Else :
                str = "abc"
        End Select|]
        Console.Write(str)
    End Sub
End Module
</file>
    </compilation>)
            Dim controlFlowResults = analysis.Item1
            Dim dataFlowResults = analysis.Item2

            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("flag1", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("str", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("str", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("flag1", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
            Assert.Equal(0, controlFlowResults.ExitPoints.Count)
            Assert.Equal(0, controlFlowResults.EntryPoints.Count)
            Assert.True(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)

        End Sub

        <Fact()>
        Public Sub InfiniteLoop()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
    <compilation name="GotoCaseElse">
        <file name="a.vb">
Imports System
Module M
    Sub Main()
        [|A:
        GoTo B
        B:
        GoTo A|]
    End Sub
End Module
</file>
    </compilation>)
            Dim controlFlowResults = analysis.Item1

            Assert.Equal(0, controlFlowResults.ExitPoints.Count)
            Assert.Equal(0, controlFlowResults.EntryPoints.Count)
            Assert.False(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)

        End Sub

#End Region

#Region "Semantic API test"

        <Fact()>
        Public Sub SimpleLabel()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SimpleLabel">
    <file name="a.vb">
Module M
    Sub Main()
lab1:      GoTo lab1
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().First
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim declaredSymbol = model.GetDeclaredSymbol(labelStatementSyntax)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("lab1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol, semanticSummary.Symbol)

        End Sub

        <WorkItem(543378, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543378")>
        <Fact()>
        Public Sub DuplicatedLabel()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="DuplicatedLabel">
    <file name="a.vb">
Module M
    Sub Main()
lab1:      GoTo lab1
lab1:      GoTo lab1
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntaxArray = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToArray()
            Dim gotoSyntaxArray = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToArray()

            Dim declaredSymbol0 = model.GetDeclaredSymbol(labelStatementSyntaxArray(0))
            Dim semanticSummary0 = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntaxArray(0).Label)
            Assert.Null(semanticSummary0.Type)
            Assert.Null(semanticSummary0.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary0.ImplicitConversion.Kind)
            Assert.Equal("lab1", semanticSummary0.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary0.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol0, semanticSummary0.Symbol)

            Dim declaredSymbol1 = model.GetDeclaredSymbol(labelStatementSyntaxArray(1))
            Dim semanticSummary1 = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntaxArray(1).Label)
            Assert.Null(semanticSummary1.Type)
            Assert.Null(semanticSummary1.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary1.ImplicitConversion.Kind)
            Assert.Equal("lab1", semanticSummary1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary1.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol0, semanticSummary1.Symbol)

            Assert.NotEqual(declaredSymbol0, declaredSymbol1)
            Assert.Equal(semanticSummary0.Symbol, semanticSummary1.Symbol)
            Assert.Equal(semanticSummary0.Symbol.Name, semanticSummary1.Symbol.Name)
        End Sub

        <Fact()>
        Public Sub NumericLabel()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NumericLabel">
    <file name="a.vb">
Module M
    Sub Main()
0:      GoTo 0
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().First
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim declaredSymbol = model.GetDeclaredSymbol(labelStatementSyntax)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("0", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol, semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub SameLabelNameInDifferentScope()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SameLabelNameInDifferentScope">
    <file name="a.vb">
Module M
    Sub Main()
0:      Dim s = Sub()
0:                  GoTo 0
                End Sub
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntaxOuter = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().First
            Dim labelStatementSyntaxInner = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().Last
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim declaredSymbolOuter = model.GetDeclaredSymbol(labelStatementSyntaxOuter)
            Dim declaredSymbolInner = model.GetDeclaredSymbol(labelStatementSyntaxInner)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("0", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.NotEqual(Of ISymbol)(declaredSymbolOuter, semanticSummary.Symbol)
            Assert.Equal(Of ISymbol)(declaredSymbolInner, semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub LabelOnCaseElse()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelOnCaseElse">
    <file name="a.vb">
Module M
    Sub Main()
        GoTo 1
        Dim x As Integer = 1
        Select Case x
            Case 1 :
                Return
1:          Case Else:
                x = 2
        End Select
        Console.Write(x)
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().First
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim declaredSymbol = model.GetDeclaredSymbol(labelStatementSyntax)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol, semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub LabelOnIfElse()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelOnIfElse">
    <file name="a.vb">
Module M
    Sub Main()
        GoTo 1
        Dim str As String = "Init"
        If str = "a"
            Return
1:      Else If str = "b"
        End If
        Console.Write(str)
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().First
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim declaredSymbol = model.GetDeclaredSymbol(labelStatementSyntax)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol, semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub GotoLabelDefinedInTryFromCatch()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoLabelDefinedInTryFromCatch">
    <file name="a.vb">
Module M
    Sub Main()
        Try
lab1:
        Catch ex As Exception
            GoTo lab1
        End Try
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim labelStatementSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LabelStatementSyntax)().ToList().First
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim declaredSymbol = model.GetDeclaredSymbol(labelStatementSyntax)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("lab1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.Equal(Of ISymbol)(declaredSymbol, semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub GoToInNestedLambda()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GoToInNestedLambda">
    <file name="a.vb">
Module M
    Sub Main()
        Dim x = Sub()
lab1:
                    Dim y = Sub()
                                GoTo lab1
                            End Sub
                End Sub
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim gotoSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().ToList().First
            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Compilation, gotoSyntax.Label)

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
        End Sub
#End Region

    End Class

End Namespace

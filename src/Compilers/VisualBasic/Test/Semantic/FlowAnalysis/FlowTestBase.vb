' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public MustInherit Class FlowTestBase
        Inherits BasicTestBase

        Friend Function FlowDiagnostics(compilation As VisualBasicCompilation) As ImmutableArray(Of Diagnostic)
            Dim diagnostics = DiagnosticBag.GetInstance()
            For Each method In AllMethods(compilation.SourceModule.GlobalNamespace)
                Dim sourceSymbol = TryCast(method, SourceMethodSymbol)
                If sourceSymbol Is Nothing OrElse method.IsPartialWithoutImplementation Then
                    Continue For
                End If
                Dim compilationState As New TypeCompilationState(compilation, Nothing, initializeComponentOpt:=Nothing)
                Dim boundBody = sourceSymbol.GetBoundMethodBody(compilationState, New DiagnosticBag())
                FlowAnalysisPass.Analyze(sourceSymbol, boundBody, diagnostics)

                Debug.Assert(Not compilationState.HasSynthesizedMethods)
            Next
            Return diagnostics.ToReadOnlyAndFree()
        End Function

        Private Function AllMethods(symbol As Symbol) As IList(Of MethodSymbol)
            Dim symbols As New List(Of MethodSymbol)

            Select Case symbol.Kind
                Case SymbolKind.Method
                    symbols.Add(TryCast(symbol, MethodSymbol))

                Case SymbolKind.NamedType
                    For Each m In (TryCast(symbol, NamedTypeSymbol)).GetMembers()
                        symbols.AddRange(AllMethods(m))
                    Next
                Case SymbolKind.[Namespace]
                    For Each m In (TryCast(symbol, NamespaceSymbol)).GetMembers()
                        symbols.AddRange(AllMethods(m))
                    Next
            End Select

            Return symbols
        End Function

#Region "Utilities"

        Protected Function CompileAndAnalyzeControlFlow(program As XElement, Optional ilSource As XCData = Nothing, Optional errors As XElement = Nothing) As ControlFlowAnalysis
            Return CompileAndGetModelAndSpan(program, Function(binding, startNodes, endNodes) AnalyzeControlFlow(binding, startNodes, endNodes), ilSource, errors)
        End Function

        Protected Function CompileAndAnalyzeDataFlow(program As XElement, Optional ilSource As XCData = Nothing, Optional errors As XElement = Nothing) As DataFlowAnalysis
            Return CompileAndGetModelAndSpan(program, Function(binding, startNodes, endNodes) AnalyzeDataFlow(binding, startNodes, endNodes), ilSource, errors)
        End Function

        Protected Function CompileAndAnalyzeControlAndDataFlow(program As XElement, Optional ilSource As XCData = Nothing, Optional errors As XElement = Nothing) As Tuple(Of ControlFlowAnalysis, DataFlowAnalysis)
            Return CompileAndGetModelAndSpan(program, Function(binding, startNodes, endNodes) Tuple.Create(AnalyzeControlFlow(binding, startNodes, endNodes), AnalyzeDataFlow(binding, startNodes, endNodes)), ilSource, errors)
        End Function

        Private Function CompileAndGetModelAndSpan(Of T)(program As XElement, analysisDelegate As Func(Of SemanticModel, List(Of VisualBasicSyntaxNode), List(Of VisualBasicSyntaxNode), T), ilSource As XCData, errors As XElement) As T
            Dim startNodes As New List(Of VisualBasicSyntaxNode)
            Dim endNodes As New List(Of VisualBasicSyntaxNode)
            Dim comp = CompileAndGetModelAndSpan(program, startNodes, endNodes, ilSource, errors)
            Return analysisDelegate(comp.GetSemanticModel(comp.SyntaxTrees(0)), startNodes, endNodes)
        End Function

        Protected Function CompileAndGetModelAndSpan(program As XElement, startNodes As List(Of VisualBasicSyntaxNode), endNodes As List(Of VisualBasicSyntaxNode), ilSource As XCData, errors As XElement, Optional parseOptions As VisualBasicParseOptions = Nothing) As VisualBasicCompilation
            Debug.Assert(program.<file>.Count = 1, "Only one file can be in the compilation.")

            Dim references = {MscorlibRef, MsvbRef, SystemCoreRef}
            If ilSource IsNot Nothing Then
                Dim ilImage As ImmutableArray(Of Byte) = Nothing
                references = references.Concat(CreateReferenceFromIlCode(ilSource?.Value, appendDefaultHeader:=True, ilImage:=ilImage)).ToArray()
            End If

            Dim assemblyName As String = Nothing
            Dim spans As IEnumerable(Of IEnumerable(Of TextSpan)) = Nothing
            Dim trees = ParseSourceXml(program, parseOptions, assemblyName, spans).ToArray()

            Dim comp = CreateEmptyCompilation(trees, references, assemblyName:=assemblyName)

            If errors IsNot Nothing Then
                AssertTheseDiagnostics(comp, errors)
            End If

            Debug.Assert(spans.Count = 1 AndAlso spans(0).Count = 1, "Exactly one region must be selected")
            Dim span = spans.Single.Single
            FindRegionNodes(comp.SyntaxTrees(0), span, startNodes, endNodes)
            Return comp
        End Function

        Protected Shared Function GetSymbolNamesJoined(Of T As ISymbol)(symbols As IEnumerable(Of T)) As String
            Return If(Not symbols.IsEmpty(), String.Join(", ", symbols.Select(Function(symbol) symbol.Name)), Nothing)
        End Function

        Protected Function AnalyzeControlFlow(model As SemanticModel,
                                              startNodes As List(Of VisualBasicSyntaxNode), endNodes As List(Of VisualBasicSyntaxNode)) As ControlFlowAnalysis

            Dim pair = (From s In startNodes From e In endNodes
                        Where s.Parent Is e.Parent AndAlso TypeOf s Is ExecutableStatementSyntax AndAlso TypeOf e Is ExecutableStatementSyntax
                        Select New With {.first = DirectCast(s, ExecutableStatementSyntax), .last = DirectCast(e, ExecutableStatementSyntax)}).LastOrDefault()

            If pair IsNot Nothing Then
                Return model.AnalyzeControlFlow(pair.first, pair.last)
            End If

            Throw New ArgumentException("Failed to identify statement sequence, maybe the region is invalid")
        End Function

        Protected Function AnalyzeDataFlow(model As SemanticModel,
                                           startNodes As List(Of VisualBasicSyntaxNode), endNodes As List(Of VisualBasicSyntaxNode)) As DataFlowAnalysis

            Dim pair = (From s In startNodes From e In endNodes
                        Where s.Parent Is e.Parent AndAlso TypeOf s Is ExecutableStatementSyntax AndAlso TypeOf e Is ExecutableStatementSyntax
                        Select New With {.first = DirectCast(s, ExecutableStatementSyntax), .last = DirectCast(e, ExecutableStatementSyntax)}).LastOrDefault()

            If pair IsNot Nothing Then
                Return model.AnalyzeDataFlow(pair.first, pair.last)
            End If

            Dim expr = (From s In startNodes From e In endNodes
                        Where s Is e AndAlso TypeOf s Is ExpressionSyntax
                        Select DirectCast(s, ExpressionSyntax)).LastOrDefault()

            If expr IsNot Nothing Then
                Return model.AnalyzeDataFlow(expr)
            End If

            Throw New ArgumentException("Failed to identify expression or statement sequence, maybe the region is invalid")
        End Function

#Region "Mapping text region into syntax node(s)"

        Private Function GetNextToken(token As SyntaxToken) As SyntaxToken
            Dim nextToken = token.GetNextToken()
            AdjustToken(nextToken)
            Return nextToken
        End Function

        Private Sub AdjustToken(ByRef token As SyntaxToken)
tryAgain:
            Select Case token.Kind
                Case SyntaxKind.StatementTerminatorToken
                    token = token.GetNextToken()
                    GoTo tryAgain

                Case SyntaxKind.ColonToken
                    Dim parent = token.Parent
                    If TypeOf parent Is StatementSyntax AndAlso parent.Kind <> SyntaxKind.LabelStatement Then
                        ' let's assume this is a statement block, what else can it be?
                        token = token.GetNextToken()
                        GoTo tryAgain
                    End If
            End Select
        End Sub

        Private Sub FindRegionNodes(tree As SyntaxTree, region As TextSpan,
                                   startNodes As List(Of VisualBasicSyntaxNode), endNodes As List(Of VisualBasicSyntaxNode))

            Dim startToken As SyntaxToken = tree.GetCompilationUnitRoot().FindToken(region.Start, True)
            AdjustToken(startToken)
            While startToken.Span.End <= region.Start
                startToken = GetNextToken(startToken)
            End While

            Dim startPosition = startToken.SpanStart
            Dim startNode = startToken.Parent
            If startPosition <= startNode.SpanStart Then startPosition = startNode.SpanStart
            While startNode IsNot Nothing AndAlso startNode.SpanStart = startPosition
                startNodes.Add(DirectCast(startNode, VisualBasicSyntaxNode))
                startNode = startNode.Parent
            End While

            Dim endToken As SyntaxToken = Nothing
            Do
                Dim nextToken = GetNextToken(startToken)
                If nextToken.SpanStart >= region.End Then
                    endToken = startToken
                    Exit Do
                End If
                startToken = nextToken
            Loop

            Dim endPosition = endToken.Span.End
            Dim endNode = endToken.Parent
            If endPosition >= endNode.Span.End Then endPosition = endNode.Span.End
            While endNode IsNot Nothing AndAlso endNode.Span.End = endPosition
                endNodes.Add(DirectCast(endNode, VisualBasicSyntaxNode))
                endNode = endNode.Parent
            End While
        End Sub

#End Region

#End Region

        Protected Sub VerifyDataFlowAnalysis(
                code As XElement,
                Optional alwaysAssigned() As String = Nothing,
                Optional captured() As String = Nothing,
                Optional dataFlowsIn() As String = Nothing,
                Optional dataFlowsOut() As String = Nothing,
                Optional definitelyAssignedOnEntry() As String = Nothing,
                Optional definitelyAssignedOnExit() As String = Nothing,
                Optional readInside() As String = Nothing,
                Optional readOutside() As String = Nothing,
                Optional variablesDeclared() As String = Nothing,
                Optional writtenInside() As String = Nothing,
                Optional writtenOutside() As String = Nothing,
                Optional capturedInside() As String = Nothing,
                Optional capturedOutside() As String = Nothing)
            Dim analysis = CompileAndAnalyzeDataFlow(code)

            Assert.True(analysis.Succeeded)
            Assert.Equal(If(alwaysAssigned, {}), analysis.AlwaysAssigned.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(captured, {}), analysis.Captured.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(dataFlowsIn, {}), analysis.DataFlowsIn.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(dataFlowsOut, {}), analysis.DataFlowsOut.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(definitelyAssignedOnEntry, {}), analysis.DefinitelyAssignedOnEntry.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(definitelyAssignedOnExit, {}), analysis.DefinitelyAssignedOnExit.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(readInside, {}), analysis.ReadInside.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(readOutside, {}), analysis.ReadOutside.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(variablesDeclared, {}), analysis.VariablesDeclared.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(writtenInside, {}), analysis.WrittenInside.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(writtenOutside, {}), analysis.WrittenOutside.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(capturedInside, {}), analysis.CapturedInside.Select(Function(s) s.Name).ToArray())
            Assert.Equal(If(capturedOutside, {}), analysis.CapturedOutside.Select(Function(s) s.Name).ToArray())
        End Sub

    End Class

End Namespace

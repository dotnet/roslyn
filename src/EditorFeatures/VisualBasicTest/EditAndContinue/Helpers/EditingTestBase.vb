' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Public MustInherit Class EditingTestBase
        Inherits BasicTestBase

        Friend Shared Function CreateAnalyzer() As VisualBasicEditAndContinueAnalyzer
            Return New VisualBasicEditAndContinueAnalyzer()
        End Function

        Public Enum MethodKind
            Regular
            Async
            Iterator
        End Enum

        Friend Shared NoSemanticEdits As SemanticEditDescription() = Array.Empty(Of SemanticEditDescription)

        Friend Overloads Shared Function Diagnostic(rudeEditKind As RudeEditKind, squiggle As String, ParamArray arguments As String()) As RudeEditDiagnosticDescription
            Return New RudeEditDiagnosticDescription(rudeEditKind, squiggle, arguments, firstLine:=Nothing)
        End Function

        Friend Shared Function SemanticEdit(kind As SemanticEditKind,
                                            symbolProvider As Func(Of Compilation, ISymbol),
                                            syntaxMap As IEnumerable(Of KeyValuePair(Of TextSpan, TextSpan)),
                                            Optional partialType As String = Nothing) As SemanticEditDescription
            Return New SemanticEditDescription(
                kind,
                symbolProvider,
                If(partialType Is Nothing, Nothing, Function(c As Compilation) CType(c.GetMember(partialType), ITypeSymbol)),
                syntaxMap,
                hasSyntaxMap:=syntaxMap IsNot Nothing)
        End Function

        Friend Shared Function SemanticEdit(kind As SemanticEditKind,
                                            symbolProvider As Func(Of Compilation, ISymbol),
                                            Optional partialType As String = Nothing,
                                            Optional preserveLocalVariables As Boolean = False) As SemanticEditDescription
            Return New SemanticEditDescription(
                kind,
                symbolProvider,
                If(partialType Is Nothing, Nothing, Function(c As Compilation) CType(c.GetMember(partialType), ITypeSymbol)),
                syntaxMap:=Nothing,
                hasSyntaxMap:=preserveLocalVariables)
        End Function

        Friend Shared Function DeletedSymbolDisplay(kind As String, displayName As String) As String
            Return String.Format(FeaturesResources.member_kind_and_name, kind, displayName)
        End Function

        Friend Shared Function DocumentResults(
            Optional activeStatements As ActiveStatementsDescription = Nothing,
            Optional semanticEdits As SemanticEditDescription() = Nothing,
            Optional diagnostics As RudeEditDiagnosticDescription() = Nothing) As DocumentAnalysisResultsDescription
            Return New DocumentAnalysisResultsDescription(activeStatements, semanticEdits, diagnostics)
        End Function

        Private Shared Function ParseSource(markedSource As String) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(
                ActiveStatementsDescription.ClearTags(markedSource),
                VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                path:="test.vb")
        End Function

        Friend Shared Function GetTopEdits(src1 As String, src2 As String) As EditScript(Of SyntaxNode)
            Dim tree1 = ParseSource(src1)
            Dim tree2 = ParseSource(src2)

            tree1.GetDiagnostics().Verify()
            tree2.GetDiagnostics().Verify()

            Dim match = SyntaxComparer.TopLevel.ComputeMatch(tree1.GetRoot(), tree2.GetRoot())
            Return match.GetTreeEdits()
        End Function

        Public Shared Function GetTopEdits(methodEdits As EditScript(Of SyntaxNode)) As EditScript(Of SyntaxNode)
            Dim oldMethodSource = methodEdits.Match.OldRoot.ToFullString()
            Dim newMethodSource = methodEdits.Match.NewRoot.ToFullString()

            Return GetTopEdits(WrapMethodBodyWithClass(oldMethodSource), WrapMethodBodyWithClass(newMethodSource))
        End Function

        Friend Shared Function GetMethodEdits(src1 As String, src2 As String, Optional methodKind As MethodKind = MethodKind.Regular) As EditScript(Of SyntaxNode)
            Dim match = GetMethodMatch(src1, src2, methodKind)
            Return match.GetTreeEdits()
        End Function

        Friend Shared Function GetMethodMatch(src1 As String, src2 As String, Optional methodKind As MethodKind = MethodKind.Regular) As Match(Of SyntaxNode)
            Dim m1 = MakeMethodBody(src1, methodKind)
            Dim m2 = MakeMethodBody(src2, methodKind)

            Dim diagnostics = New ArrayBuilder(Of RudeEditDiagnostic)()

            Dim oldHasStateMachineSuspensionPoint = False, newHasStateMachineSuspensionPoint = False
            Dim match = CreateAnalyzer().GetTestAccessor().ComputeBodyMatch(m1, m2, Array.Empty(Of AbstractEditAndContinueAnalyzer.ActiveNode)(), diagnostics, oldHasStateMachineSuspensionPoint, newHasStateMachineSuspensionPoint)
            Dim needsSyntaxMap = oldHasStateMachineSuspensionPoint AndAlso newHasStateMachineSuspensionPoint

            Assert.Equal(methodKind <> MethodKind.Regular, needsSyntaxMap)

            If methodKind = MethodKind.Regular Then
                Assert.Empty(diagnostics)
            End If

            Return match
        End Function

        Public Shared Function GetMethodMatches(src1 As String,
                                                src2 As String,
                                                Optional stateMachine As MethodKind = MethodKind.Regular) As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))
            Dim methodMatch = GetMethodMatch(src1, src2, stateMachine)
            Return EditAndContinueTestHelpers.GetMethodMatches(CreateAnalyzer(), methodMatch)
        End Function

        Public Shared Function ToMatchingPairs(match As Match(Of SyntaxNode)) As MatchingPairs
            Return EditAndContinueTestHelpers.ToMatchingPairs(match)
        End Function

        Public Shared Function ToMatchingPairs(matches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As MatchingPairs
            Return EditAndContinueTestHelpers.ToMatchingPairs(matches)
        End Function

        Friend Shared Function MakeMethodBody(bodySource As String, Optional stateMachine As MethodKind = MethodKind.Regular) As SyntaxNode
            Dim source = WrapMethodBodyWithClass(bodySource, stateMachine)

            Dim tree = ParseSource(source)
            Dim root = tree.GetRoot()
            tree.GetDiagnostics().Verify()

            Dim declaration = DirectCast(DirectCast(root, CompilationUnitSyntax).Members(0), ClassBlockSyntax).Members(0)
            Return SyntaxFactory.SyntaxTree(declaration).GetRoot()
        End Function

        Private Shared Function WrapMethodBodyWithClass(bodySource As String, Optional kind As MethodKind = MethodKind.Regular) As String
            Select Case kind
                Case MethodKind.Iterator
                    Return "Class C" & vbLf & "Iterator Function F() As IEnumerable(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case MethodKind.Async
                    Return "Class C" & vbLf & "Async Function F() As Task(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case Else
                    Return "Class C" & vbLf & "Sub F()" & vbLf & bodySource & " : End Sub : End Class"
            End Select
        End Function

        Friend Shared Function GetActiveStatements(oldSource As String, newSource As String, Optional flags As ActiveStatementFlags() = Nothing, Optional path As String = "0") As ActiveStatementsDescription
            Return New ActiveStatementsDescription(oldSource, newSource, Function(source) SyntaxFactory.ParseSyntaxTree(source, path:=path), flags)
        End Function

        Friend Shared Function GetSyntaxMap(oldSource As String, newSource As String) As SyntaxMapDescription
            Return New SyntaxMapDescription(oldSource, newSource)
        End Function

        Friend Shared Function GetActiveStatementDebugInfos(
            markedSources As String(),
            Optional filePaths As String() = Nothing,
            Optional methodRowIds As Integer() = Nothing,
            Optional modules As Guid() = Nothing,
            Optional methodVersions As Integer() = Nothing,
            Optional ilOffsets As Integer() = Nothing,
            Optional flags As ActiveStatementFlags() = Nothing) As ImmutableArray(Of ManagedActiveStatementDebugInfo)

            Return ActiveStatementsDescription.GetActiveStatementDebugInfos(
                Function(source, path) SyntaxFactory.ParseSyntaxTree(source, path:=path),
                markedSources,
                filePaths,
                extension:=".vb",
                methodRowIds,
                modules,
                methodVersions,
                ilOffsets,
                flags)
        End Function
    End Class
End Namespace

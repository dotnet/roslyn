' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Public MustInherit Class RudeEditTestBase
        Inherits BasicTestBase

        Friend Shared ReadOnly Analyzer As VisualBasicEditAndContinueAnalyzer = New VisualBasicEditAndContinueAnalyzer()

        Public Enum StateMachineKind
            None
            Async
            Iterator
        End Enum

        Friend Overloads Shared Function Diagnostic(rudeEditKind As RudeEditKind, squiggle As String, ParamArray arguments As String()) As RudeEditDiagnosticDescription
            Return New RudeEditDiagnosticDescription(rudeEditKind, squiggle, arguments, firstLine:=Nothing)
        End Function

        Friend Shared Function SemanticEdit(kind As SemanticEditKind, symbolProvider As Func(Of Compilation, ISymbol), syntaxMap As IEnumerable(Of KeyValuePair(Of TextSpan, TextSpan))) As SemanticEditDescription
            Assert.NotNull(syntaxMap)
            Return New SemanticEditDescription(kind, symbolProvider, syntaxMap, preserveLocalVariables:=True)
        End Function

        Friend Shared Function SemanticEdit(kind As SemanticEditKind, symbolProvider As Func(Of Compilation, ISymbol), Optional preserveLocalVariables As Boolean = False) As SemanticEditDescription
            Return New SemanticEditDescription(kind, symbolProvider, Nothing, preserveLocalVariables)
        End Function

        Private Shared Function ParseSource(source As String, Optional options As ParseOptions = Nothing) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(ActiveStatementsDescription.ClearTags(source), options)
        End Function

        Friend Shared Function GetTopEdits(src1 As String, src2 As String) As EditScript(Of SyntaxNode)
            Dim tree1 = ParseSource(src1)
            Dim tree2 = ParseSource(src2)

            tree1.GetDiagnostics().Verify()
            tree2.GetDiagnostics().Verify()

            Dim match = TopSyntaxComparer.Instance.ComputeMatch(tree1.GetRoot(), tree2.GetRoot())
            Return match.GetTreeEdits()
        End Function

        Friend Shared Function GetMethodEdits(src1 As String, src2 As String, Optional options As ParseOptions = Nothing, Optional stateMachine As StateMachineKind = StateMachineKind.None) As EditScript(Of SyntaxNode)
            Dim match = GetMethodMatch(src1, src2, options, stateMachine)
            Return match.GetTreeEdits()
        End Function

        Friend Shared Function GetMethodMatch(src1 As String, src2 As String, Optional options As ParseOptions = Nothing, Optional stateMachine As StateMachineKind = StateMachineKind.None) As Match(Of SyntaxNode)
            Dim m1 = MakeMethodBody(src1, options, stateMachine)
            Dim m2 = MakeMethodBody(src2, options, stateMachine)

            Dim diagnostics = New List(Of RudeEditDiagnostic)()

            Dim oldHasStateMachineSuspensionPoint = False, newHasStateMachineSuspensionPoint = False
            Dim match = Analyzer.ComputeBodyMatch(m1, m2, Array.Empty(Of AbstractEditAndContinueAnalyzer.ActiveNode)(), diagnostics, oldHasStateMachineSuspensionPoint, newHasStateMachineSuspensionPoint)
            Dim needsSyntaxMap = oldHasStateMachineSuspensionPoint AndAlso newHasStateMachineSuspensionPoint

            Assert.Equal(stateMachine <> StateMachineKind.None, needsSyntaxMap)

            If stateMachine = StateMachineKind.None Then
                Assert.Empty(diagnostics)
            End If

            Return match
        End Function

        Public Shared Function GetMethodMatches(src1 As String,
                                                src2 As String,
                                                Optional options As ParseOptions = Nothing,
                                                Optional stateMachine As StateMachineKind = StateMachineKind.None) As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))
            Dim methodMatch = GetMethodMatch(src1, src2, options, stateMachine)
            Return EditAndContinueTestHelpers.GetMethodMatches(Analyzer, methodMatch)
        End Function

        Public Shared Function ToMatchingPairs(match As Match(Of SyntaxNode)) As MatchingPairs
            Return EditAndContinueTestHelpers.ToMatchingPairs(match)
        End Function

        Public Shared Function ToMatchingPairs(matches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As MatchingPairs
            Return EditAndContinueTestHelpers.ToMatchingPairs(matches)
        End Function

        Friend Shared Function MakeMethodBody(bodySource As String, Optional options As ParseOptions = Nothing, Optional stateMachine As StateMachineKind = StateMachineKind.None) As SyntaxNode
            Dim source As String
            Select Case stateMachine
                Case StateMachineKind.Iterator
                    source = "Class C" & vbLf & "Iterator Function F() As IEnumerable(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case StateMachineKind.Async
                    source = "Class C" & vbLf & "Async Function F() As Task(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case Else
                    source = "Class C" & vbLf & "Sub F()" & vbLf & bodySource & " : End Sub : End Class"
            End Select

            Dim tree = ParseSource(source, options)
            Dim root = tree.GetRoot()
            tree.GetDiagnostics().Verify()

            Dim declaration = DirectCast(DirectCast(root, CompilationUnitSyntax).Members(0), ClassBlockSyntax).Members(0)
            Return SyntaxFactory.SyntaxTree(declaration).GetRoot()

        End Function

        Friend Shared Function GetActiveStatements(oldSource As String, newSource As String) As ActiveStatementsDescription
            Return New ActiveStatementsDescription(oldSource, newSource)
        End Function

        Friend Shared Function GetSyntaxMap(oldSource As String, newSource As String) As SyntaxMapDescription
            Return New SyntaxMapDescription(oldSource, newSource)
        End Function
    End Class
End Namespace

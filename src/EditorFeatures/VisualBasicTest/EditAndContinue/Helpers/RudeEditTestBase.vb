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

        Friend Enum StateMachineKind
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

        Private Shared Function ParseSource(source As String) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(ActiveStatementsDescription.ClearTags(source))
        End Function

        Friend Shared Function GetTopEdits(src1 As String, src2 As String) As EditScript(Of SyntaxNode)
            Dim tree1 = ParseSource(src1)
            Dim tree2 = ParseSource(src2)

            tree1.GetDiagnostics().Verify()
            tree2.GetDiagnostics().Verify()

            Dim match = TopSyntaxComparer.Instance.ComputeMatch(tree1.GetRoot(), tree2.GetRoot())
            Return match.GetTreeEdits()
        End Function

        Friend Shared Function GetMethodEdits(src1 As String, src2 As String, Optional stateMachine As StateMachineKind = StateMachineKind.None) As EditScript(Of SyntaxNode)
            Dim match = GetMethodMatch(src1, src2, stateMachine)
            Return match.GetTreeEdits()
        End Function

        Friend Shared Function GetMethodMatch(src1 As String, src2 As String, Optional stateMachine As StateMachineKind = StateMachineKind.None) As Match(Of SyntaxNode)
            Dim m1 = MakeMethodBody(src1, stateMachine)
            Dim m2 = MakeMethodBody(src2, stateMachine)

            Dim diagnostics = New List(Of RudeEditDiagnostic)()
            Dim needsSyntaxMap As Boolean
            Dim match = Analyzer.ComputeBodyMatch(m1, m2, {New AbstractEditAndContinueAnalyzer.ActiveNode}, diagnostics, needsSyntaxMap)

            Assert.Equal(stateMachine <> StateMachineKind.None, needsSyntaxMap)

            If stateMachine = StateMachineKind.None Then
                Assert.Empty(diagnostics)
            End If

            Return match

        End Function

        Friend Shared Iterator Function GetMethodMatches(src1 As String, src2 As String, Optional stateMachine As StateMachineKind = StateMachineKind.None) As IEnumerable(Of Match(Of SyntaxNode))
            Dim methodMatch = GetMethodMatch(src1, src2, stateMachine)

            Dim queue = New Queue(Of Match(Of SyntaxNode))()
            queue.Enqueue(methodMatch)

            While queue.Count > 0
                Dim match = queue.Dequeue()
                Yield match

                For Each m In match.Matches
                    If m.Key Is match.OldRoot Then
                        Assert.Equal(match.NewRoot, m.Value)
                        Continue For
                    End If

                    For Each comparer In GetLambdaBodyComparers(m.Key, m.Value)
                        Dim lambdaMatch = comparer.ComputeMatch(m.Key, m.Value)
                        queue.Enqueue(lambdaMatch)
                    Next
                Next
            End While
        End Function

        Public Shared Function ToMatchingPairs(match As Match(Of SyntaxNode)) As MatchingPairs
            Return New MatchingPairs(ToMatchingPairs(match.Matches.Where(Function(partners) partners.Key IsNot match.OldRoot)))
        End Function

        Public Shared Function ToMatchingPairs(match As IEnumerable(Of Match(Of SyntaxNode))) As MatchingPairs
            Return New MatchingPairs(ToMatchingPairs(match.SelectMany(Function(m) m.Matches.Where(Function(partners) partners.Key IsNot m.OldRoot))))
        End Function

        Private Shared Function ToMatchingPairs(matches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As IEnumerable(Of MatchingPair)
            Return matches.
                OrderBy(Function(partners) partners.Key.GetLocation().SourceSpan.Start).
                ThenByDescending(Function(partners) partners.Key.Span.Length).
                Select(Function(partners) New MatchingPair With {.Old = partners.Key.ToString().Replace(vbCrLf, " ").Replace(vbLf, " "),
                                                                 .[New] = partners.Value.ToString().Replace(vbCrLf, " ").Replace(vbLf, " ")})
        End Function

        Friend Shared Function MakeMethodBody(bodySource As String, Optional stateMachine As StateMachineKind = StateMachineKind.None) As SyntaxNode
            Dim source As String
            Select Case stateMachine
                Case StateMachineKind.Iterator
                    source = "Class C" & vbLf & "Iterator Function F() As IEnumerable(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case StateMachineKind.Async
                    source = "Class C" & vbLf & "Async Function F() As Task(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case Else
                    source = "Class C" & vbLf & "Sub F()" & vbLf & bodySource & " : End Sub : End Class"
            End Select

            Dim tree = ParseSource(source)
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

        Private Shared Function GetLambdaBodyComparers(oldNode As SyntaxNode, newNode As SyntaxNode) As IEnumerable(Of StatementSyntaxComparer)
            Select Case oldNode.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return {New StatementSyntaxComparer.SingleBody(oldNode, newNode)}

                Case SyntaxKind.WhereClause
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, WhereClauseSyntax).Condition, DirectCast(newNode, WhereClauseSyntax).Condition)}

                Case SyntaxKind.CollectionRangeVariable
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, CollectionRangeVariableSyntax).Expression, DirectCast(newNode, CollectionRangeVariableSyntax).Expression)}

                Case SyntaxKind.FunctionAggregation
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, FunctionAggregationSyntax).Argument, DirectCast(newNode, FunctionAggregationSyntax).Argument)}

                Case SyntaxKind.ExpressionRangeVariable
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, ExpressionRangeVariableSyntax).Expression, DirectCast(newNode, ExpressionRangeVariableSyntax).Expression)}

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, PartitionWhileClauseSyntax).Condition, DirectCast(newNode, PartitionWhileClauseSyntax).Condition)}

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, OrderingSyntax).Expression, DirectCast(newNode, OrderingSyntax).Expression)}

                Case SyntaxKind.JoinCondition
                    Return {New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, JoinConditionSyntax).Left, DirectCast(newNode, JoinConditionSyntax).Left),
                            New StatementSyntaxComparer.MultiBody(DirectCast(oldNode, JoinConditionSyntax).Right, DirectCast(newNode, JoinConditionSyntax).Right)}

                Case Else
                    Return SpecializedCollections.EmptyEnumerable(Of StatementSyntaxComparer)()
            End Select
        End Function
    End Class
End Namespace

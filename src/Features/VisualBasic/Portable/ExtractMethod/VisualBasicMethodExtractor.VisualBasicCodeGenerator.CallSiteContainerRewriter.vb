' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private MustInherit Class VisualBasicCodeGenerator
            Private Class CallSiteContainerRewriter
                Inherits VisualBasicSyntaxRewriter
                Private ReadOnly _outmostCallSiteContainer As SyntaxNode
                Private ReadOnly _statementsOrFieldToInsert As IEnumerable(Of StatementSyntax)
                Private ReadOnly _variableToRemoveMap As HashSet(Of SyntaxAnnotation)
                Private ReadOnly _firstStatementOrFieldToReplace As StatementSyntax
                Private ReadOnly _lastStatementOrFieldToReplace As StatementSyntax

                Private Shared ReadOnly s_removeAnnotation As SyntaxAnnotation = New SyntaxAnnotation()

                Public Sub New(outmostCallSiteContainer As SyntaxNode,
                               variableToRemoveMap As HashSet(Of SyntaxAnnotation),
                               firstStatementOrFieldToReplace As StatementSyntax,
                               lastStatementOrFieldToReplace As StatementSyntax,
                               statementsOrFieldToInsert As IEnumerable(Of StatementSyntax))
                    Contract.ThrowIfNull(outmostCallSiteContainer)
                    Contract.ThrowIfNull(variableToRemoveMap)
                    Contract.ThrowIfNull(firstStatementOrFieldToReplace)
                    Contract.ThrowIfNull(lastStatementOrFieldToReplace)
                    Contract.ThrowIfTrue(statementsOrFieldToInsert.IsEmpty())

                    Me._outmostCallSiteContainer = outmostCallSiteContainer

                    Me._variableToRemoveMap = variableToRemoveMap
                    Me._statementsOrFieldToInsert = statementsOrFieldToInsert

                    Me._firstStatementOrFieldToReplace = firstStatementOrFieldToReplace
                    Me._lastStatementOrFieldToReplace = lastStatementOrFieldToReplace

                    Contract.ThrowIfFalse(Me._firstStatementOrFieldToReplace.Parent Is Me._lastStatementOrFieldToReplace.Parent)
                End Sub

                Public Function Generate() As SyntaxNode
                    Dim result = Visit(Me._outmostCallSiteContainer)

                    ' remove any nodes annotated for removal
                    If result.ContainsAnnotations Then
                        Dim nodesToRemove = result.DescendantNodes(Function(n) n.ContainsAnnotations).Where(Function(n) n.HasAnnotation(s_removeAnnotation))
                        result = result.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)
                    End If

                    Return result
                End Function

                Private ReadOnly Property ContainerOfStatementsOrFieldToReplace() As SyntaxNode
                    Get
                        Return Me._firstStatementOrFieldToReplace.Parent
                    End Get
                End Property

                Public Overrides Function VisitLocalDeclarationStatement(node As LocalDeclarationStatementSyntax) As SyntaxNode
                    node = CType(MyBase.VisitLocalDeclarationStatement(node), LocalDeclarationStatementSyntax)

                    Dim expressionStatements = New List(Of StatementSyntax)()
                    Dim variableDeclarators = New List(Of VariableDeclaratorSyntax)()
                    Dim triviaList = New List(Of SyntaxTrivia)()

                    If Not Me._variableToRemoveMap.ProcessLocalDeclarationStatement(node, expressionStatements, variableDeclarators, triviaList) Then
                        Contract.ThrowIfFalse(expressionStatements.Count = 0)
                        Return node
                    End If

                    Contract.ThrowIfFalse(expressionStatements.Count = 0)

                    If variableDeclarators.Count = 0 AndAlso
                       triviaList.Any(Function(t) t.Kind <> SyntaxKind.WhitespaceTrivia AndAlso t.Kind <> SyntaxKind.EndOfLineTrivia) Then
                        ' well, there are trivia associated with the node.
                        ' we can't just delete the node since then, we will lose
                        ' the trivia. unfortunately, it is not easy to attach the trivia
                        ' to next token. for now, create an empty statement and associate the
                        ' trivia to the statement

                        ' TODO : think about a way to trivia attached to next token
                        Return SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxKind.EmptyToken).WithLeadingTrivia(SyntaxFactory.TriviaList(triviaList)))
                    End If

                    ' return survived var decls
                    If variableDeclarators.Count > 0 Then
                        Return SyntaxFactory.LocalDeclarationStatement(
                                    node.Modifiers,
                                    SyntaxFactory.SeparatedList(variableDeclarators)).WithPrependedLeadingTrivia(triviaList)
                    End If

                    Return node.WithAdditionalAnnotations(s_removeAnnotation)
                End Function

                Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitMethodBlock(node)
                    End If

                    Return node.WithSubOrFunctionStatement(ReplaceStatementIfNeeded(node.SubOrFunctionStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitConstructorBlock(node)
                    End If

                    Return node.WithSubNewStatement(ReplaceStatementIfNeeded(node.SubNewStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitOperatorBlock(node)
                    End If

                    Return node.WithOperatorStatement(ReplaceStatementIfNeeded(node.OperatorStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitAccessorBlock(node)
                    End If

                    Return node.WithAccessorStatement(ReplaceStatementIfNeeded(node.AccessorStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitWhileBlock(node As WhileBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the switch section
                        Return MyBase.VisitWhileBlock(node)
                    End If

                    Return node.WithWhileStatement(ReplaceStatementIfNeeded(node.WhileStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitUsingBlock(node As UsingBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitUsingBlock(node)
                    End If

                    Return node.WithUsingStatement(ReplaceStatementIfNeeded(node.UsingStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitSyncLockBlock(node As SyncLockBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitSyncLockBlock(node)
                    End If

                    Return node.WithSyncLockStatement(ReplaceStatementIfNeeded(node.SyncLockStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitWithBlock(node As WithBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitWithBlock(node)
                    End If

                    Return node.WithWithStatement(ReplaceStatementIfNeeded(node.WithStatement)).
                                WithStatements(ReplaceStatementsIfNeeded(node.Statements))
                End Function

                Public Overrides Function VisitSingleLineIfStatement(node As SingleLineIfStatementSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitSingleLineIfStatement(node)
                    End If

                    Return SyntaxFactory.SingleLineIfStatement(node.IfKeyword,
                                                               node.Condition,
                                                               node.ThenKeyword,
                                                               VisitList(ReplaceStatementsIfNeeded(node.Statements, colon:=True)),
                                                               node.ElseClause)

                End Function

                Public Overrides Function VisitSingleLineElseClause(node As SingleLineElseClauseSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitSingleLineElseClause(node)
                    End If

                    Return SyntaxFactory.SingleLineElseClause(node.ElseKeyword, VisitList(ReplaceStatementsIfNeeded(node.Statements, colon:=True)))
                End Function

                Public Overrides Function VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitMultiLineIfBlock(node)
                    End If

                    Return node.WithIfStatement(ReplaceStatementIfNeeded(node.IfStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitElseBlock(node As ElseBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitElseBlock(node)
                    End If

                    Return node.WithElseStatement(ReplaceStatementIfNeeded(node.ElseStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitTryBlock(node As TryBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitTryBlock(node)
                    End If

                    Return node.WithTryStatement(ReplaceStatementIfNeeded(node.TryStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitCatchBlock(node As CatchBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitCatchBlock(node)
                    End If

                    Return node.WithCatchStatement(ReplaceStatementIfNeeded(node.CatchStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitFinallyBlock(node As FinallyBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitFinallyBlock(node)
                    End If

                    Return node.WithFinallyStatement(ReplaceStatementIfNeeded(node.FinallyStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitSelectBlock(node As SelectBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitSelectBlock(node)
                    End If

                    Return node.WithSelectStatement(ReplaceStatementIfNeeded(node.SelectStatement)).
                                WithCaseBlocks(VisitList(node.CaseBlocks)).
                                WithEndSelectStatement(ReplaceStatementIfNeeded(node.EndSelectStatement))
                End Function

                Public Overrides Function VisitCaseBlock(node As CaseBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitCaseBlock(node)
                    End If

                    Return node.WithCaseStatement(ReplaceStatementIfNeeded(node.CaseStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements)))
                End Function

                Public Overrides Function VisitDoLoopBlock(node As DoLoopBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitDoLoopBlock(node)
                    End If

                    Return node.WithDoStatement(ReplaceStatementIfNeeded(node.DoStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements))).
                                WithLoopStatement(ReplaceStatementIfNeeded(node.LoopStatement))
                End Function

                Public Overrides Function VisitForBlock(node As ForBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitForBlock(node)
                    End If

                    Return node.WithForStatement(ReplaceStatementIfNeeded(node.ForStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements))).
                                WithNextStatement(ReplaceStatementIfNeeded(node.NextStatement))
                End Function

                Public Overrides Function VisitForEachBlock(node As ForEachBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitForEachBlock(node)
                    End If

                    Return node.WithForEachStatement(ReplaceStatementIfNeeded(node.ForEachStatement)).
                                WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements))).
                                WithNextStatement(ReplaceStatementIfNeeded(node.NextStatement))
                End Function

                Public Overrides Function VisitSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitSingleLineLambdaExpression(node)
                    End If

                    Dim body = SyntaxFactory.SingletonList(DirectCast(node.Body, StatementSyntax))
                    Return node.WithBody(VisitList(ReplaceStatementsIfNeeded(body, colon:=True)).First()).
                                WithSubOrFunctionHeader(ReplaceStatementIfNeeded(node.SubOrFunctionHeader))
                End Function

                Public Overrides Function VisitMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        Return MyBase.VisitMultiLineLambdaExpression(node)
                    End If

                    Return node.WithStatements(VisitList(ReplaceStatementsIfNeeded(node.Statements))).
                                WithSubOrFunctionHeader(ReplaceStatementIfNeeded(node.SubOrFunctionHeader))
                End Function

                Private Function ReplaceStatementIfNeeded(Of T As StatementSyntax)(statement As T) As T
                    Contract.ThrowIfNull(statement)

                    ' if all three same
                    If (statement IsNot _firstStatementOrFieldToReplace) OrElse (Me._firstStatementOrFieldToReplace IsNot Me._lastStatementOrFieldToReplace) Then
                        Return statement
                    End If

                    Contract.ThrowIfFalse(Me._statementsOrFieldToInsert.Count() = 1)
                    Return CType(Me._statementsOrFieldToInsert.Single(), T)
                End Function

                Private Function ReplaceStatementsIfNeeded(statements As SyntaxList(Of StatementSyntax), Optional colon As Boolean = False) As SyntaxList(Of StatementSyntax)
                    Dim newStatements = New List(Of StatementSyntax)(statements)
                    Dim firstStatementIndex = newStatements.FindIndex(Function(s) s Is Me._firstStatementOrFieldToReplace)

                    ' looks like statements belong to parent's Begin statement. there is nothing we need to do here.
                    If firstStatementIndex < 0 Then
                        Contract.ThrowIfFalse(Me._firstStatementOrFieldToReplace Is Me._lastStatementOrFieldToReplace)
                        Return statements
                    End If

                    Dim lastStatementIndex = newStatements.FindIndex(Function(s) s Is Me._lastStatementOrFieldToReplace)
                    Contract.ThrowIfFalse(lastStatementIndex >= 0)

                    Contract.ThrowIfFalse(firstStatementIndex <= lastStatementIndex)

                    ' okay, this visit contains the statement

                    ' remove statement that must be removed
                    statements = statements.RemoveRange(firstStatementIndex, lastStatementIndex - firstStatementIndex + 1)

                    ' insert new statements
                    Return statements.InsertRange(firstStatementIndex, Join(Me._statementsOrFieldToInsert, colon).ToArray())
                End Function

                Private Function Join(statements As IEnumerable(Of StatementSyntax), colon As Boolean) As IEnumerable(Of StatementSyntax)
                    If Not colon Then
                        Return statements
                    End If

                    Dim removeEndOfLine = Function(t As SyntaxTrivia) Not t.IsElastic() AndAlso t.Kind <> SyntaxKind.EndOfLineTrivia

                    Dim i = 0
                    Dim count = statements.Count()
                    Dim trivia = SyntaxFactory.ColonTrivia(SyntaxFacts.GetText(SyntaxKind.ColonTrivia))

                    Dim newStatements = New List(Of StatementSyntax)
                    For Each statement In statements
                        statement = statement.WithLeadingTrivia(statement.GetLeadingTrivia().Where(removeEndOfLine))

                        If i < count - 1 Then
                            statement = statement.WithTrailingTrivia(statement.GetTrailingTrivia().Where(removeEndOfLine).Concat(trivia))
                        End If

                        newStatements.Add(statement)
                        i += 1
                    Next

                    Return newStatements
                End Function

                Public Overrides Function VisitModuleBlock(ByVal node As ModuleBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitModuleBlock(node)
                    End If

                    Return node.WithMembers(VisitList(ReplaceStatementsIfNeeded(node.Members)))
                End Function

                Public Overrides Function VisitClassBlock(ByVal node As ClassBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitClassBlock(node)
                    End If

                    Return node.WithMembers(VisitList(ReplaceStatementsIfNeeded(node.Members)))
                End Function

                Public Overrides Function VisitStructureBlock(ByVal node As StructureBlockSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitStructureBlock(node)
                    End If

                    Return node.WithMembers(VisitList(ReplaceStatementsIfNeeded(node.Members)))
                End Function

                Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SyntaxNode
                    If node IsNot Me.ContainerOfStatementsOrFieldToReplace Then
                        ' make sure we visit nodes under the block
                        Return MyBase.VisitCompilationUnit(node)
                    End If

                    Return node.WithMembers(VisitList(ReplaceStatementsIfNeeded(node.Members)))
                End Function
            End Class
        End Class
    End Class
End Namespace

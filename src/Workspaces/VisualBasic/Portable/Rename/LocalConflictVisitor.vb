' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
    Friend Class LocalConflictVisitor
        Inherits VisualBasicSyntaxVisitor

        Private ReadOnly _tracker As ConflictingIdentifierTracker
        Private ReadOnly _newSolution As Solution
        Private ReadOnly _cancellationToken As CancellationToken

        Public Sub New(tokenBeingRenamed As SyntaxToken, newSolution As Solution, cancellationToken As CancellationToken)
            _tracker = New ConflictingIdentifierTracker(tokenBeingRenamed, CaseInsensitiveComparison.Comparer)
            _newSolution = newSolution
            _cancellationToken = cancellationToken
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            For Each child In node.ChildNodes()
                Visit(child)
            Next
        End Sub

        Private Sub VisitMethodBlockBase(node As MethodBlockBaseSyntax)
            Dim tokens As New List(Of SyntaxToken)

            If node.BlockStatement.ParameterList IsNot Nothing Then
                tokens.AddRange(From parameter In node.BlockStatement.ParameterList.Parameters
                                Select parameter.Identifier.Identifier)
            End If

            _tracker.AddIdentifiers(tokens)
            VisitBlock(node.Statements)
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public Overrides Sub VisitMethodBlock(node As MethodBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitConstructorBlock(node As ConstructorBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitOperatorBlock(node As OperatorBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitAccessorBlock(node As AccessorBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitQueryExpression(node As QueryExpressionSyntax)
            Dim tokens As New List(Of SyntaxToken)

            ' TODO: fully implement handling of all range vars incl. hiding rules.

            For Each clause In node.Clauses

                Select Case clause.Kind
                    Case SyntaxKind.FromClause
                        tokens.AddRange(From variable In DirectCast(clause, FromClauseSyntax).Variables
                                        Select variable.Identifier.Identifier)
                End Select
            Next

            _tracker.AddIdentifiers(tokens)
            For Each clause In node.Clauses
                Visit(clause)
            Next
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Private Sub VisitBlock(block As SyntaxList(Of StatementSyntax))
            Dim tokens As New List(Of SyntaxToken)

            For Each statement In block
                If statement.Kind = SyntaxKind.LocalDeclarationStatement Then
                    Dim declarationStatement = DirectCast(statement, LocalDeclarationStatementSyntax)

                    For Each declarator In declarationStatement.Declarators
                        tokens.AddRange(From i In declarator.Names
                                        Select i.Identifier)
                    Next
                End If
            Next

            _tracker.AddIdentifiers(tokens)
            For Each statement In block
                Visit(statement)
            Next
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public Overrides Sub VisitSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax)
            Dim tokens As New List(Of SyntaxToken)

            If node.SubOrFunctionHeader.ParameterList IsNot Nothing Then
                tokens.AddRange(From parameter In node.SubOrFunctionHeader.ParameterList.Parameters
                                Select parameter.Identifier.Identifier)
            End If

            _tracker.AddIdentifiers(tokens)
            Visit(node.Body)
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public Overrides Sub VisitMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax)
            Dim tokens As New List(Of SyntaxToken)

            If node.SubOrFunctionHeader.ParameterList IsNot Nothing Then
                tokens.AddRange(From parameter In node.SubOrFunctionHeader.ParameterList.Parameters
                                Select parameter.Identifier.Identifier)
            End If

            _tracker.AddIdentifiers(tokens)
            VisitBlock(node.Statements)
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public Overrides Sub VisitForBlock(node As ForBlockSyntax)
            VisitForOrForEachBlock(node)
        End Sub

        Public Overrides Sub VisitForEachBlock(node As ForEachBlockSyntax)
            VisitForOrForEachBlock(node)
        End Sub

        Private Sub VisitForOrForEachBlock(node As ForOrForEachBlockSyntax)
            Dim tokens As New List(Of SyntaxToken)

            Dim controlVariable As SyntaxNode
            If node.ForOrForEachStatement.Kind = SyntaxKind.ForEachStatement Then
                controlVariable = DirectCast(node.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable
            Else
                controlVariable = DirectCast(node.ForOrForEachStatement, ForStatementSyntax).ControlVariable
            End If

            If controlVariable.Kind = SyntaxKind.VariableDeclarator Then
                ' it's only legal to have one name in the variable declarator for for and foreach loops.
                tokens.Add(DirectCast(controlVariable, VariableDeclaratorSyntax).Names.First().Identifier)
            Else
                Dim semanticModel = _newSolution.GetDocument(controlVariable.SyntaxTree).GetSemanticModelAsync(_cancellationToken).WaitAndGetResult_CanCallOnBackground(_cancellationToken)
                Dim symbol = semanticModel.GetSymbolInfo(controlVariable).Symbol

                ' if it is a field we don't care
                If symbol IsNot Nothing AndAlso symbol.IsKind(SymbolKind.Local) Then
                    Dim local = DirectCast(symbol, ILocalSymbol)

                    ' is this local declared in the for or for each loop?
                    ' if not it was already added to the tracker before.
                    If local.IsFor OrElse local.IsForEach Then
                        If controlVariable.Kind = SyntaxKind.IdentifierName Then
                            tokens.Add(DirectCast(controlVariable, IdentifierNameSyntax).Identifier)
                        Else
                            Debug.Fail($"Unexpected control variable kind '{controlVariable.Kind}'")
                        End If
                    End If
                End If
            End If

            _tracker.AddIdentifiers(tokens)
            VisitBlock(node.Statements)
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public Overrides Sub VisitUsingBlock(node As UsingBlockSyntax)
            Dim tokens As New List(Of SyntaxToken)

            ' add all declared variable names to token list
            For Each usingVariableDeclarator In node.UsingStatement.Variables
                For Each name In usingVariableDeclarator.Names
                    tokens.Add(name.Identifier)
                Next
            Next

            _tracker.AddIdentifiers(tokens)
            VisitBlock(node.Statements)
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public Overrides Sub VisitCatchBlock(node As CatchBlockSyntax)
            Dim tokens As New List(Of SyntaxToken)

            Dim semanticModel = _newSolution.GetDocument(node.SyntaxTree).GetSemanticModelAsync(_cancellationToken).WaitAndGetResult_CanCallOnBackground(_cancellationToken)
            Dim identifierToken = node.CatchStatement.IdentifierName?.Identifier

            If identifierToken.HasValue Then
                Dim symbol = semanticModel.GetSymbolInfo(identifierToken.Value).Symbol

                ' if it is a field we don't care
                If symbol IsNot Nothing AndAlso symbol.IsKind(SymbolKind.Local) Then
                    Dim local = DirectCast(symbol, ILocalSymbol)

                    ' is this local declared in the for or for each loop?
                    ' if not it was already added to the tracker before.
                    If local.IsCatch Then
                        tokens.Add(identifierToken.Value)
                    End If
                End If
            End If

            _tracker.AddIdentifiers(tokens)
            VisitBlock(node.Statements)
            _tracker.RemoveIdentifiers(tokens)
        End Sub

        Public ReadOnly Property ConflictingTokens As IEnumerable(Of SyntaxToken)
            Get
                Return _tracker.ConflictingTokens
            End Get
        End Property
    End Class
End Namespace

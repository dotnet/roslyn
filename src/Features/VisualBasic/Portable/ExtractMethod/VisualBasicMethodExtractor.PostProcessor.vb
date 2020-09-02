' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Private Class PostProcessor
            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _contextPosition As Integer

            Public Sub New(semanticModel As SemanticModel, contextPosition As Integer)
                Contract.ThrowIfNull(semanticModel)

                Me._semanticModel = semanticModel
                Me._contextPosition = contextPosition
            End Sub

            Public Function MergeDeclarationStatements(statements As ImmutableArray(Of StatementSyntax)) As ImmutableArray(Of StatementSyntax)
                If statements.FirstOrDefault() Is Nothing Then
                    Return statements
                End If

                Return MergeDeclarationStatementsWorker(statements)
            End Function

            Private Function MergeDeclarationStatementsWorker(statements As ImmutableArray(Of StatementSyntax)) As ImmutableArray(Of StatementSyntax)
                Dim declarationStatements = New List(Of StatementSyntax)()

                Dim map = New Dictionary(Of ITypeSymbol, List(Of LocalDeclarationStatementSyntax))()
                For Each statement In statements
                    If Not IsDeclarationMergable(statement) Then
                        For Each declStatement In GetMergedDeclarationStatements(map)
                            declarationStatements.Add(declStatement)
                        Next declStatement

                        declarationStatements.Add(statement)
                        Continue For
                    End If

                    AppendDeclarationStatementToMap(TryCast(statement, LocalDeclarationStatementSyntax), map)
                Next statement

                ' merge leftover
                If map.Count > 0 Then
                    For Each declStatement In GetMergedDeclarationStatements(map)
                        declarationStatements.Add(declStatement)
                    Next declStatement
                End If

                Return declarationStatements.ToImmutableArray()
            End Function

            Private Sub AppendDeclarationStatementToMap(statement As LocalDeclarationStatementSyntax, map As Dictionary(Of ITypeSymbol, List(Of LocalDeclarationStatementSyntax)))
                Contract.ThrowIfNull(statement)
                Contract.ThrowIfFalse(statement.Declarators.Count = 1)

                Dim declarator = statement.Declarators(0)
                Dim symbolInfo = Me._semanticModel.GetSpeculativeSymbolInfo(Me._contextPosition, declarator.AsClause.Type, SpeculativeBindingOption.BindAsTypeOrNamespace)
                Dim type = TryCast(symbolInfo.Symbol, ITypeSymbol)
                Contract.ThrowIfNull(type)

                map.GetOrAdd(type, Function() New List(Of LocalDeclarationStatementSyntax)()).Add(statement)
            End Sub

            Private Shared Function GetMergedDeclarationStatements(map As Dictionary(Of ITypeSymbol, List(Of LocalDeclarationStatementSyntax))) As IEnumerable(Of LocalDeclarationStatementSyntax)
                Dim declarationStatements = New List(Of LocalDeclarationStatementSyntax)()

                For Each keyValuePair In map
                    Contract.ThrowIfFalse(keyValuePair.Value.Count > 0)

                    ' merge all variable decl for current type
                    Dim variables = New List(Of ModifiedIdentifierSyntax)()
                    For Each statement In keyValuePair.Value
                        For Each variable In statement.Declarators(0).Names
                            variables.Add(variable)
                        Next variable
                    Next statement

                    ' and create one decl statement
                    ' use type name from the first decl statement
                    Dim firstDeclaration = keyValuePair.Value.First()
                    declarationStatements.Add(
                        SyntaxFactory.LocalDeclarationStatement(firstDeclaration.Modifiers,
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.VariableDeclarator(SyntaxFactory.SeparatedList(variables)).WithAsClause(firstDeclaration.Declarators(0).AsClause))
                                                    ))
                Next keyValuePair

                map.Clear()

                Return declarationStatements
            End Function

            Private Function IsDeclarationMergable(statement As StatementSyntax) As Boolean
                Contract.ThrowIfNull(statement)

                ' to be mergable, statement must be
                ' 1. decl statement without any extra info
                ' 2. no initialization on any of its decls
                ' 3. no trivia except whitespace
                ' 4. type must be known

                Dim declarationStatement = TryCast(statement, LocalDeclarationStatementSyntax)
                If declarationStatement Is Nothing Then
                    Return False
                End If

                If declarationStatement.Modifiers.Any(SyntaxKind.ConstKeyword) OrElse
                   declarationStatement.IsMissing Then
                    Return False
                End If

                If ContainsAnyInitialization(declarationStatement) Then
                    Return False
                End If

                If Not ContainsOnlyWhitespaceTrivia(declarationStatement) Then
                    Return False
                End If

                If declarationStatement.Declarators.Count <> 1 Then
                    Return False
                End If

                If declarationStatement.Declarators(0).AsClause Is Nothing Then
                    Return False
                End If

                Dim symbolInfo = Me._semanticModel.GetSpeculativeSymbolInfo(Me._contextPosition, declarationStatement.Declarators(0).AsClause.Type, SpeculativeBindingOption.BindAsTypeOrNamespace)
                Dim type = TryCast(symbolInfo.Symbol, ITypeSymbol)
                If type Is Nothing OrElse
                   type.TypeKind = TypeKind.Error OrElse
                   type.TypeKind = TypeKind.Unknown Then
                    Return False
                End If

                Return True
            End Function

            Private Shared Function ContainsAnyInitialization(statement As LocalDeclarationStatementSyntax) As Boolean
                For Each variable In statement.Declarators
                    If variable.Initializer IsNot Nothing Then
                        Return True
                    End If
                Next variable

                Return False
            End Function

            Private Shared Function ContainsOnlyWhitespaceTrivia(statement As StatementSyntax) As Boolean
                For Each token In statement.DescendantTokens()
                    If Not ContainsOnlyWhitespaceTrivia(token) Then
                        Return False
                    End If
                Next token

                Return True
            End Function

            Private Shared Function ContainsOnlyWhitespaceTrivia(token As SyntaxToken) As Boolean
                For Each trivia In token.LeadingTrivia.Concat(token.TrailingTrivia)
                    If trivia.Kind <> SyntaxKind.WhitespaceTrivia AndAlso trivia.Kind <> SyntaxKind.EndOfLineTrivia Then
                        Return False
                    End If
                Next trivia

                Return True
            End Function

            Public Shared Function RemoveDeclarationAssignmentPattern(statements As ImmutableArray(Of StatementSyntax)) As ImmutableArray(Of StatementSyntax)
                If statements.Count() < 2 Then
                    Return statements
                End If

                ' if we have inline temp variable as service, we could just use that service here.
                ' since it is not a service right now, do very simple clean up
                Dim declaration = TryCast(statements(0), LocalDeclarationStatementSyntax)
                Dim assignment = TryCast(statements(1), AssignmentStatementSyntax)
                If declaration Is Nothing OrElse assignment Is Nothing Then
                    Return statements
                End If

                If ContainsAnyInitialization(declaration) OrElse
                   declaration.Modifiers.Any(Function(m) m.Kind <> SyntaxKind.DimKeyword) OrElse
                   declaration.Declarators.Count <> 1 OrElse
                   declaration.Declarators(0).Names.Count <> 1 OrElse
                   assignment.Left Is Nothing OrElse
                   assignment.Right Is Nothing Then
                    Return statements
                End If

                If Not ContainsOnlyWhitespaceTrivia(declaration) OrElse
                   Not ContainsOnlyWhitespaceTrivia(assignment) Then
                    Return statements
                End If

                Dim variableName = declaration.Declarators(0).Names(0).ToString()

                If assignment.Left.ToString() <> variableName Then
                    Return statements
                End If

                Dim variable = declaration.Declarators(0).WithoutTrailingTrivia().WithInitializer(SyntaxFactory.EqualsValue(assignment.Right))
                Dim newDeclaration = declaration.WithDeclarators(SyntaxFactory.SingletonSeparatedList(variable))

                Return SpecializedCollections.SingletonEnumerable(Of StatementSyntax)(newDeclaration).Concat(statements.Skip(2)).ToImmutableArray()
            End Function

            Public Shared Function RemoveInitializedDeclarationAndReturnPattern(statements As ImmutableArray(Of StatementSyntax)) As ImmutableArray(Of StatementSyntax)
                ' if we have inline temp variable as service, we could just use that service here.
                ' since it is not a service right now, do very simple clean up
                If statements.Count() <> 2 Then
                    Return statements
                End If

                Dim declaration = TryCast(statements.ElementAtOrDefault(0), LocalDeclarationStatementSyntax)
                Dim returnStatement = TryCast(statements.ElementAtOrDefault(1), ReturnStatementSyntax)
                If declaration Is Nothing OrElse returnStatement Is Nothing Then
                    Return statements
                End If

                If declaration.Declarators.Count <> 1 OrElse
                    declaration.Declarators(0).Names.Count <> 1 OrElse
                    declaration.Declarators(0).Initializer Is Nothing OrElse
                    returnStatement.Expression Is Nothing Then
                    Return statements
                End If

                If Not ContainsOnlyWhitespaceTrivia(declaration) OrElse
                   Not ContainsOnlyWhitespaceTrivia(returnStatement) Then
                    Return statements
                End If

                Dim variableName = declaration.Declarators(0).Names(0).ToString()
                If returnStatement.Expression.ToString() <> variableName Then
                    Return statements
                End If

                Return SpecializedCollections.SingletonEnumerable(Of StatementSyntax)(
                    SyntaxFactory.ReturnStatement(declaration.Declarators(0).Initializer.Value)).Concat(statements.Skip(2)).ToImmutableArray()
            End Function
        End Class
    End Class
End Namespace

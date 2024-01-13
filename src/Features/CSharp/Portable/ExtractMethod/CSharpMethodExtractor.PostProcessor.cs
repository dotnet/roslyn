// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private class PostProcessor
        {
            private readonly SemanticModel _semanticModel;
            private readonly int _contextPosition;

            public PostProcessor(SemanticModel semanticModel, int contextPosition)
            {
                Contract.ThrowIfNull(semanticModel);

                _semanticModel = semanticModel;
                _contextPosition = contextPosition;
            }

            public static ImmutableArray<StatementSyntax> RemoveRedundantBlock(ImmutableArray<StatementSyntax> statements)
            {
                // it must have only one statement
                if (statements.Count() != 1)
                {
                    return statements;
                }

                // that statement must be a block
                if (statements.Single() is not BlockSyntax block)
                {
                    return statements;
                }

                // we have a block, remove them
                return RemoveRedundantBlock(block);
            }

            private static ImmutableArray<StatementSyntax> RemoveRedundantBlock(BlockSyntax block)
            {
                // if block doesn't have any statement
                if (block.Statements.Count == 0)
                {
                    // either remove the block if it doesn't have any trivia, or return as it is if
                    // there are trivia attached to block
                    return (block.OpenBraceToken.GetAllTrivia().IsEmpty() && block.CloseBraceToken.GetAllTrivia().IsEmpty())
                        ? ImmutableArray<StatementSyntax>.Empty
                        : ImmutableArray.Create<StatementSyntax>(block);
                }

                // okay transfer asset attached to block to statements
                var firstStatement = block.Statements.First();
                var firstToken = firstStatement.GetFirstToken(includeZeroWidth: true);
                var firstTokenWithAsset = block.OpenBraceToken.CopyAnnotationsTo(firstToken).WithPrependedLeadingTrivia(block.OpenBraceToken.GetAllTrivia());

                var lastStatement = block.Statements.Last();
                var lastToken = lastStatement.GetLastToken(includeZeroWidth: true);
                var lastTokenWithAsset = block.CloseBraceToken.CopyAnnotationsTo(lastToken).WithAppendedTrailingTrivia(block.CloseBraceToken.GetAllTrivia());

                // create new block with new tokens
                block = block.ReplaceTokens(new[] { firstToken, lastToken }, (o, c) => (o == firstToken) ? firstTokenWithAsset : lastTokenWithAsset);

                // return only statements without the wrapping block
                return ImmutableArray.CreateRange(block.Statements);
            }

            public ImmutableArray<StatementSyntax> MergeDeclarationStatements(ImmutableArray<StatementSyntax> statements)
            {
                if (statements.FirstOrDefault() == null)
                {
                    return statements;
                }

                return MergeDeclarationStatementsWorker(statements);
            }

            private ImmutableArray<StatementSyntax> MergeDeclarationStatementsWorker(ImmutableArray<StatementSyntax> statements)
            {
                using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

                var map = new Dictionary<ITypeSymbol, List<LocalDeclarationStatementSyntax>>();
                foreach (var statement in statements)
                {
                    if (!IsDeclarationMergable(statement))
                    {
                        result.AddRange(GetMergedDeclarationStatements(map));
                        result.Add(statement);
                        continue;
                    }

                    AppendDeclarationStatementToMap(statement as LocalDeclarationStatementSyntax, map);
                }

                // merge leftover
                if (map.Count > 0)
                    result.AddRange(GetMergedDeclarationStatements(map));

                return result.ToImmutable();
            }

            private void AppendDeclarationStatementToMap(
                LocalDeclarationStatementSyntax statement,
                Dictionary<ITypeSymbol, List<LocalDeclarationStatementSyntax>> map)
            {
                Contract.ThrowIfNull(statement);

                var type = _semanticModel.GetSpeculativeTypeInfo(_contextPosition, statement.Declaration.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                Contract.ThrowIfNull(type);

                map.GetOrAdd(type, _ => new List<LocalDeclarationStatementSyntax>()).Add(statement);
            }

            private static IEnumerable<LocalDeclarationStatementSyntax> GetMergedDeclarationStatements(
                Dictionary<ITypeSymbol, List<LocalDeclarationStatementSyntax>> map)
            {
                foreach (var keyValuePair in map)
                {
                    Contract.ThrowIfFalse(keyValuePair.Value.Count > 0);

                    // merge all variable decl for current type
                    var variables = new List<VariableDeclaratorSyntax>();
                    foreach (var statement in keyValuePair.Value)
                    {
                        foreach (var variable in statement.Declaration.Variables)
                        {
                            variables.Add(variable);
                        }
                    }

                    // and create one decl statement
                    // use type name from the first decl statement
                    yield return
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(keyValuePair.Value.First().Declaration.Type, SyntaxFactory.SeparatedList(variables)));
                }

                map.Clear();
            }

            private bool IsDeclarationMergable(StatementSyntax statement)
            {
                Contract.ThrowIfNull(statement);

                // to be mergable, statement must be
                // 1. decl statement without any extra info
                // 2. no initialization on any of its decls
                // 3. no trivia except whitespace
                // 4. type must be known

                if (statement is not LocalDeclarationStatementSyntax declarationStatement)
                {
                    return false;
                }

                if (declarationStatement.Modifiers.Count > 0 ||
                    declarationStatement.IsConst ||
                    declarationStatement.IsMissing)
                {
                    return false;
                }

                if (ContainsAnyInitialization(declarationStatement))
                {
                    return false;
                }

                if (!ContainsOnlyWhitespaceTrivia(declarationStatement))
                {
                    return false;
                }

                var semanticInfo = _semanticModel.GetSpeculativeTypeInfo(_contextPosition, declarationStatement.Declaration.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                if (semanticInfo == null ||
                    semanticInfo.TypeKind is TypeKind.Error or TypeKind.Unknown)
                {
                    return false;
                }

                return true;
            }

            private static bool ContainsAnyInitialization(LocalDeclarationStatementSyntax statement)
            {
                foreach (var variable in statement.Declaration.Variables)
                {
                    if (variable.Initializer != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool ContainsOnlyWhitespaceTrivia(StatementSyntax statement)
            {
                foreach (var token in statement.DescendantTokens())
                {
                    foreach (var trivia in token.LeadingTrivia.Concat(token.TrailingTrivia))
                    {
                        if (trivia.Kind() is not SyntaxKind.WhitespaceTrivia and
                            not SyntaxKind.EndOfLineTrivia)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public static ImmutableArray<StatementSyntax> RemoveInitializedDeclarationAndReturnPattern(ImmutableArray<StatementSyntax> statements)
            {
                // if we have inline temp variable as service, we could just use that service here.
                // since it is not a service right now, do very simple clean up
                if (statements.ElementAtOrDefault(2) != null)
                {
                    return statements;
                }

                if (statements.ElementAtOrDefault(0) is not LocalDeclarationStatementSyntax declaration || statements.ElementAtOrDefault(1) is not ReturnStatementSyntax returnStatement)
                {
                    return statements;
                }

                if (declaration.Declaration == null ||
                    declaration.Declaration.Variables.Count != 1 ||
                    declaration.Declaration.Variables[0].Initializer == null ||
                    declaration.Declaration.Variables[0].Initializer.Value == null ||
                    declaration.Declaration.Variables[0].Initializer.Value is StackAllocArrayCreationExpressionSyntax ||
                    returnStatement.Expression == null)
                {
                    return statements;
                }

                if (!ContainsOnlyWhitespaceTrivia(declaration) ||
                    !ContainsOnlyWhitespaceTrivia(returnStatement))
                {
                    return statements;
                }

                var variableName = declaration.Declaration.Variables[0].Identifier.ToString();
                if (returnStatement.Expression.ToString() != variableName)
                {
                    return statements;
                }

                return ImmutableArray.Create<StatementSyntax>(SyntaxFactory.ReturnStatement(declaration.Declaration.Variables[0].Initializer.Value));
            }

            public static ImmutableArray<StatementSyntax> RemoveDeclarationAssignmentPattern(ImmutableArray<StatementSyntax> statements)
            {
                if (statements.ElementAtOrDefault(0) is not LocalDeclarationStatementSyntax declaration || statements.ElementAtOrDefault(1) is not ExpressionStatementSyntax assignment)
                {
                    return statements;
                }

                if (ContainsAnyInitialization(declaration) ||
                    declaration.Declaration == null ||
                    declaration.Declaration.Variables.Count != 1 ||
                    assignment.Expression == null ||
                    assignment.Expression.Kind() != SyntaxKind.SimpleAssignmentExpression)
                {
                    return statements;
                }

                if (!ContainsOnlyWhitespaceTrivia(declaration) ||
                    !ContainsOnlyWhitespaceTrivia(assignment))
                {
                    return statements;
                }

                var variableName = declaration.Declaration.Variables[0].Identifier.ToString();

                var assignmentExpression = assignment.Expression as AssignmentExpressionSyntax;
                if (assignmentExpression.Left == null ||
                    assignmentExpression.Right == null ||
                    assignmentExpression.Left.ToString() != variableName)
                {
                    return statements;
                }

                var variable = declaration.Declaration.Variables[0].WithInitializer(SyntaxFactory.EqualsValueClause(assignmentExpression.Right));
                using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

                result.Add(declaration.WithDeclaration(
                    declaration.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(variable))));
                result.AddRange(statements.Skip(2));

                return result.ToImmutable();
            }
        }
    }
}

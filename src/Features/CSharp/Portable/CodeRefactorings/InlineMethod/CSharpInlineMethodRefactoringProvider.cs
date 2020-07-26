// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider : AbstractInlineMethodRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider()
        {
        }

        protected override async Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context)
        {
            var syntaxNode = await context.TryGetRelevantNodeAsync<InvocationExpressionSyntax>().ConfigureAwait(false);
            return syntaxNode;
        }

        private static bool ShouldStatementBeInlined(StatementSyntax statementSyntax)
            => statementSyntax is ReturnStatementSyntax || statementSyntax is ExpressionStatementSyntax || statementSyntax is ThrowStatementSyntax;

        protected override bool IsMethodContainsOneStatement(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            if (calleeMethodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
            {
                var blockSyntaxNode = declarationSyntax.Body;
                // 1. If it is an ordinary method with block
                if (blockSyntaxNode != null)
                {
                    var blockStatements = blockSyntaxNode.Statements;
                    return blockStatements.Count == 1 && ShouldStatementBeInlined(blockStatements[0]);
                }
                else
                {
                    // 2. If it is an Arrow Expression
                    var arrowExpressionNodes = declarationSyntax
                        .DescendantNodes().Where(node => node.IsKind(SyntaxKind.ArrowExpressionClause)).ToImmutableArray();
                    return arrowExpressionNodes.Length == 1;
                }
            }

            return false;
        }

        protected override SyntaxNode GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            SyntaxNode? inlineSyntaxNode = null;
            if (calleeMethodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
            {
                var blockSyntaxNode = declarationSyntax.Body;
                // 1. If it is a ordinary method with block
                if (blockSyntaxNode != null)
                {
                    var blockStatements = blockSyntaxNode.Statements;
                    if (blockStatements.Count == 1)
                    {
                        inlineSyntaxNode = GetExpressionFromStatementSyntaxNode(blockStatements[0]);
                    }
                }
                else
                {
                    // 2. If it is using Arrow Expression
                    var arrowExpressionNodes = declarationSyntax
                        .DescendantNodes().Where(node => node.IsKind(SyntaxKind.ArrowExpressionClause)).ToImmutableArray();
                    if (arrowExpressionNodes.Length == 1)
                    {
                        inlineSyntaxNode = ((ArrowExpressionClauseSyntax)arrowExpressionNodes[0]).Expression;
                    }
                }
            }

            return inlineSyntaxNode ??= SyntaxFactory.EmptyStatement();
        }

        private static SyntaxNode? GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression == null ? null : SyntaxFactory.ExpressionStatement(returnStatementSyntax.Expression),
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax,
                ThrowStatementSyntax throwExpressionSyntax => throwExpressionSyntax,
                _ => null
            };

        protected override ImmutableArray<IInlineChange> ComputeInlineChanges(
            SyntaxNode calleeInvocationExpressionSyntaxNode,
            SemanticModel semanticModel,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            CancellationToken cancellationToken)
        {
            var changeBuilder = ArrayBuilder<IInlineChange>.GetInstance();
            if (calleeInvocationExpressionSyntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                var argumentExpressionSyntaxNodes = invocationExpressionSyntax.ArgumentList.Arguments;
                var allParameters = calleeMethodSymbol.Parameters;

                // Track the parameter needs renaming according to the input argument from caller
                var renameParametersBuilder =
                    ArrayBuilder<(IParameterSymbol parameterSymbol, string argumentName)>.GetInstance();
                // Track the 'out' variable declaration expression. A declaration needs to be put to caller's body after inlining
                var declarationExpressionParametersBuilder =
                    ArrayBuilder<(IParameterSymbol parameterSymbol, DeclarationExpressionSyntax expressionSyntax)>.GetInstance();
                // Track the expression argument. It needs to be put to caller's body after inlining.
                var expressionArgumentsBuilder =
                    ArrayBuilder<(IParameterSymbol parameterSymbol, ExpressionSyntax argumentExpression)>.GetInstance();
                // Track the possible param array arguments in the parameter.
                var paramArrayArgumentsBuilder = ArrayBuilder<ExpressionSyntax>.GetInstance();
                // Used to track if a parameter has been mapped to an arguments.
                var unprocessedParameters = allParameters.ToSet();
                var paramArrayParameter = allParameters.FirstOrDefault(p => p.IsParams);

                foreach (var argumentSyntaxNode in argumentExpressionSyntaxNodes)
                {
                    var argumentExpression = argumentSyntaxNode.Expression;
                    var argumentSymbol = TryGetBestMatchSymbol(argumentExpression, semanticModel, cancellationToken);
                    var parameterSymbol =
                        argumentSyntaxNode.DetermineParameter(semanticModel, allowParams: true, cancellationToken);
                    // In case there is syntax error the parameter symbol might be null.
                    if (parameterSymbol != null && !parameterSymbol.IsDiscard)
                    {
                        unprocessedParameters.Remove(parameterSymbol);
                        if (!parameterSymbol.IsParams)
                        {
                            if (argumentExpression.IsAnyLiteralExpression())
                            {
                                changeBuilder.Add(new ReplaceVariableChange(argumentExpression, parameterSymbol));
                            }
                            else if (argumentSymbol == null)
                            {
                                // Argument could be some special expressions, like Conditional Expression.
                                expressionArgumentsBuilder.Add((parameterSymbol, argumentExpression));
                            }
                            else if (argumentSymbol.IsKind(SymbolKind.Field)
                                     || argumentSymbol.IsKind(SymbolKind.Parameter)
                                     || argumentSymbol.IsKind(SymbolKind.Property))
                            {
                                renameParametersBuilder.Add((parameterSymbol, argumentSymbol.Name));
                            }
                            else if (argumentSymbol.IsKind(SymbolKind.Local))
                            {
                                // If the argument is "int out x", then two operations need to be done.
                                // 1. 'int x;' declaration should be generated in the caller.
                                // 2. 'x' should replace all the mapping parameter in callee
                                if (argumentExpression is DeclarationExpressionSyntax declarationExpressionSyntax)
                                {
                                    declarationExpressionParametersBuilder.Add((parameterSymbol, declarationExpressionSyntax));
                                }

                                renameParametersBuilder.Add((parameterSymbol, argumentSymbol.Name));
                            }
                            else if (argumentSymbol.IsKind(SymbolKind.Method))
                            {
                                expressionArgumentsBuilder.Add((parameterSymbol, argumentExpression));
                            }
                        }
                        else
                        {
                            paramArrayArgumentsBuilder.Add(argumentSyntaxNode.Expression);
                        }
                    }
                }

                // Replace the unprocessed parameter by using its default value if it is an optional parameter
                foreach (var unprocessedParameter in unprocessedParameters
                    .Where(unprocessedParameter =>
                        !unprocessedParameter.IsDiscard
                        && unprocessedParameter.IsOptional
                        && !unprocessedParameter.IsParams
                        && unprocessedParameter.HasExplicitDefaultValue))
                {
                    changeBuilder.Add(
                        new ReplaceVariableChange(
                            ExpressionGenerator.GenerateExpression(
                                unprocessedParameter.Type,
                                unprocessedParameter.ExplicitDefaultValue,
                                canUseFieldReference: false),
                            unprocessedParameter));
                }

                var renameParameters = renameParametersBuilder.ToImmutableAndFree();
                var expressionArguments = expressionArgumentsBuilder.ToImmutableAndFree();
                var declarationParameters = declarationExpressionParametersBuilder.ToImmutableAndFree();
                var renameTable = ComputeRenameTable(
                    calleeInvocationExpressionSyntaxNode,
                    semanticModel,
                    calleeMethodDeclarationSyntaxNode,
                    paramArrayParameter,
                    renameParameters,
                    expressionArguments.Select(parameterAndArgument => parameterAndArgument.parameterSymbol).ToImmutableArray(),
                    cancellationToken);

                foreach (var (symbol, newName) in renameTable)
                {
                    if (!newName.Equals(symbol.Name))
                    {
                        changeBuilder.Add(new IdentifierRenameVariableChange(symbol, SyntaxFactory.IdentifierName(newName)));
                    }
                }

                foreach (var (parameterSymbol, argumentExpression) in expressionArguments)
                {
                    var equalsValueCause = SyntaxFactory.EqualsValueClause(argumentExpression);
                    changeBuilder.Add(new ExtractDeclarationChange(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                parameterSymbol.Type.GenerateTypeSyntax(),
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(renameTable[parameterSymbol]),
                                        argumentList: null, initializer: equalsValueCause))))));
                }

                foreach (var (parameterSymbol, argument) in declarationParameters)
                {
                    if (argument.Designation is SingleVariableDesignationSyntax singleVariableDesignationSyntax)
                    {
                        var identifier = singleVariableDesignationSyntax.Identifier;
                        changeBuilder.Add(new ExtractDeclarationChange(
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(
                                    parameterSymbol.Type.GenerateTypeSyntax(),
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(identifier))))));
                    }
                }

                if (paramArrayParameter != null && renameTable.TryGetValue(paramArrayParameter, out var paramArrayNewName))
                {
                    var arguments = paramArrayArgumentsBuilder.ToImmutableAndFree();
                    var listOfArguments = SyntaxFactory.SeparatedList(
                        arguments,
                        arguments.Length <= 1
                            ? Enumerable.Empty<SyntaxToken>()
                            : Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), arguments.Length - 1));
                    var initializerExpression =
                        SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, listOfArguments);
                    var equalValueClause = SyntaxFactory.EqualsValueClause(initializerExpression);

                    changeBuilder.Add(new ExtractDeclarationChange(SyntaxFactory
                        .LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                paramArrayParameter.Type.GenerateTypeSyntax(),
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                            SyntaxFactory.Identifier(paramArrayNewName),
                                            argumentList: null, initializer: equalValueClause))))));
                }
            }

            return changeBuilder.ToImmutableAndFree();
        }
    }
}

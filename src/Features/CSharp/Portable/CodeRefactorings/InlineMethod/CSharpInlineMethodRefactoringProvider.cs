// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;
using Microsoft.CodeAnalysis.PooledObjects;
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

        protected override SyntaxNode ExtractExpressionFromMethodDeclaration(SyntaxNode calleeMethodDeclarationSyntaxNode)
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
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                ThrowStatementSyntax throwExpressionSyntax => throwExpressionSyntax.Expression,
                _ => null
            };

        protected override ImmutableArray<IInlineChange> ComputeInlineChanges(
            SyntaxNode calleeInvocationExpressionSyntaxNode,
            SemanticModel semanticModel,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            CancellationToken cancellation)
        {
            var changeBuilder = ArrayBuilder<IInlineChange>.GetInstance();
            if (calleeInvocationExpressionSyntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                var inputArguments = invocationExpressionSyntax.ArgumentList.Arguments;
                var mappingParameters = inputArguments
                    .Select(arg => arg.DetermineParameter(semanticModel, allowParams: true, cancellation));

                var argumentsAndMappingParameters = inputArguments
                    .Zip(mappingParameters, (argument, parameter) => (argument, parameter))
                    .Where(argumentAndParameter => argumentAndParameter.parameter != null)
                    .ToImmutableArray();

                var declarationParametersBuilder = ArrayBuilder<IParameterSymbol>.GetInstance();
                var parametersNeedRenameBuilder = ArrayBuilder<(IParameterSymbol parameterSymbol, string argumentName)>.GetInstance();

                var paramSymbols = argumentsAndMappingParameters
                    .FirstOrDefault(argumentAndParameter => argumentAndParameter.parameter.IsParams);

                if (paramSymbols != default)
                {
                    declarationParametersBuilder.Add(paramSymbols.parameter);
                }

                var unprocessedParameters = calleeMethodSymbol.Parameters.ToSet();

                foreach (var (argument, parameterSymbol) in argumentsAndMappingParameters)
                {
                    if (!parameterSymbol.IsDiscard && !parameterSymbol.IsParams)
                    {
                        var inputArgumentExpression = argument.Expression;

                        if (inputArgumentExpression.IsAnyLiteralExpression())
                        {
                            changeBuilder.Add(
                                new ReplaceVariableChange(inputArgumentExpression, parameterSymbol));
                        }
                        else if (inputArgumentExpression.IsKind(SyntaxKind.DeclarationExpression)
                            || inputArgumentExpression.IsAnyLambdaOrAnonymousMethod()
                            || inputArgumentExpression.IsKind(SyntaxKind.InvocationExpression)
                            || inputArgumentExpression.IsKind(SyntaxKind.ConditionalExpression))
                        {
                            declarationParametersBuilder.Add(parameterSymbol);
                        }
                        else if (inputArgumentExpression is IdentifierNameSyntax identifierNameSyntax
                            && !parameterSymbol.Name.Equals(identifierNameSyntax.Identifier.ValueText))
                        {
                            parametersNeedRenameBuilder.Add((parameterSymbol, identifierNameSyntax.Identifier.ValueText));
                        }

                        unprocessedParameters.Remove(parameterSymbol);
                    }
                }

                var renameTable = ComputeRenameTable(
                    calleeInvocationExpressionSyntaxNode,
                    semanticModel,
                    calleeMethodDeclarationSyntaxNode,
                    parametersNeedRenameBuilder.ToImmutableArray(),
                    declarationParametersBuilder.ToImmutableArray(),
                    cancellation);

                foreach (var (symbol, newName) in renameTable)
                {
                    if (!newName.Equals(symbol.Name))
                    {
                        changeBuilder.Add(new IdentifierRenameVariableChange(symbol, SyntaxFactory.IdentifierName(newName)));
                    }
                }

                foreach (var symbol in declarationParametersBuilder)
                {
                    changeBuilder.Add(new ExtractDeclarationChange(
                        SyntaxFactory));
                }

                if (paramSymbols != null && renameTable.TryGetValue(paramSymbols, out var paramArrayNewName))
                {
                    var arguments = argumentsAndMappingParameters
                        .Where(arguAndParamSymbol => arguAndParamSymbol.parameter.IsParams)
                        .SelectAsArray(arguAndParamSymbol => arguAndParamSymbol.argument.Expression);

                    var listOfArguments = SyntaxFactory.SeparatedList(
                        arguments,
                        arguments.Length <= 1
                        ? Enumerable.Empty<SyntaxToken>()
                        : Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), arguments.Length - 1));
                    var initializerExpression =
                        SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, listOfArguments);

                    changeBuilder.Add(new ExtractDeclarationChange(
                        SyntaxFactory.ArrayCreationExpression((ArrayTypeSyntax)paramSymbols.Type.GenerateTypeSyntax(),
                            initializerExpression)));
                }

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
            }

            return changeBuilder.ToImmutableArray();
        }
    }
}

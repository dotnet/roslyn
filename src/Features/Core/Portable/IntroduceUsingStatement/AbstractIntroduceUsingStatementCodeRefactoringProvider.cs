// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.IntroduceUsingStatement
{
    internal abstract class AbstractIntroduceUsingStatementCodeRefactoringProvider<TStatementSyntax, TLocalDeclarationSyntax> : CodeRefactoringProvider
        where TStatementSyntax : SyntaxNode
        where TLocalDeclarationSyntax : TStatementSyntax
    {
        protected abstract string CodeActionTitle { get; }

        protected abstract bool CanRefactorToContainBlockStatements(SyntaxNode parent);
        protected abstract SyntaxList<TStatementSyntax> GetStatements(SyntaxNode parentOfStatementsToSurround);
        protected abstract SyntaxNode WithStatements(SyntaxNode parentOfStatementsToSurround, SyntaxList<TStatementSyntax> statements);

        protected abstract TStatementSyntax CreateUsingStatement(TLocalDeclarationSyntax declarationStatement, SyntaxList<TStatementSyntax> statementsToSurround);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var declarationSyntax = await FindDisposableLocalDeclarationAsync(document, span, cancellationToken).ConfigureAwait(false);

            if (declarationSyntax != null)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        CodeActionTitle,
                        cancellationToken => IntroduceUsingStatementAsync(document, declarationSyntax, cancellationToken),
                        CodeActionTitle),
                    declarationSyntax.Span);
            }
        }

        private async Task<TLocalDeclarationSyntax?> FindDisposableLocalDeclarationAsync(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var declarationSyntax = await document.TryGetRelevantNodeAsync<TLocalDeclarationSyntax>(selection, cancellationToken).ConfigureAwait(false);
            if (declarationSyntax is null || !CanRefactorToContainBlockStatements(declarationSyntax.GetRequiredParent()))
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var disposableType = semanticModel.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            if (disposableType is null)
            {
                return null;
            }

            var operation = semanticModel.GetOperation(declarationSyntax, cancellationToken) as IVariableDeclarationGroupOperation;
            if (operation?.Declarations.Length != 1)
            {
                return null;
            }

            var localDeclaration = operation.Declarations[0];
            if (localDeclaration.Declarators.Length != 1)
            {
                return null;
            }

            var declarator = localDeclaration.Declarators[0];

            var localType = declarator.Symbol?.Type;
            if (localType is null)
            {
                return null;
            }

            var initializer = (localDeclaration.Initializer ?? declarator.Initializer)?.Value;

            // Initializer kind is invalid when incomplete declaration syntax ends in an equals token.
            if (initializer is null || initializer.Kind == OperationKind.Invalid)
            {
                return null;
            }

            if (!IsLegalUsingStatementType(semanticModel.Compilation, disposableType, localType))
            {
                return null;
            }

            return declarationSyntax;
        }

        /// <summary>
        /// Up to date with C# 7.3. Pattern-based disposal is likely to be added to C# 8.0,
        /// in which case accessible instance and extension methods will need to be detected.
        /// </summary>
        private static bool IsLegalUsingStatementType(Compilation compilation, ITypeSymbol disposableType, ITypeSymbol type)
        {
            if (disposableType == null)
            {
                return false;
            }

            // CS1674: type used in a using statement must be implicitly convertible to 'System.IDisposable'
            return compilation.ClassifyCommonConversion(type, disposableType).IsImplicit;
        }

        private async Task<Document> IntroduceUsingStatementAsync(
            Document document,
            TLocalDeclarationSyntax declarationStatement,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var statementsToSurround = GetStatementsToSurround(declarationStatement, semanticModel, syntaxFacts, cancellationToken);

            var usingStatement = CreateUsingStatement(declarationStatement, statementsToSurround);

            if (statementsToSurround.Any())
            {
                var parentStatements = GetStatements(declarationStatement.GetRequiredParent());
                var declarationStatementIndex = parentStatements.IndexOf(declarationStatement);

                var newParent = WithStatements(
                    declarationStatement.GetRequiredParent(),
                    new SyntaxList<TStatementSyntax>(parentStatements
                        .Take(declarationStatementIndex)
                        .Concat(usingStatement)
                        .Concat(parentStatements.Skip(declarationStatementIndex + 1 + statementsToSurround.Count))));

                return document.WithSyntaxRoot(root.ReplaceNode(
                    declarationStatement.GetRequiredParent(),
                    newParent.WithAdditionalAnnotations(Formatter.Annotation)));
            }
            else
            {
                // Either the parent is not blocklike, meaning WithStatements can’t be used as in the other branch,
                // or there’s just no need to replace more than the statement itself because no following statements
                // will be surrounded.
                return document.WithSyntaxRoot(root.ReplaceNode(
                    declarationStatement,
                    usingStatement.WithAdditionalAnnotations(Formatter.Annotation)));
            }
        }

        private SyntaxList<TStatementSyntax> GetStatementsToSurround(
            TLocalDeclarationSyntax declarationStatement,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            // Find the minimal number of statements to move into the using block
            // in order to not break existing references to the local.
            var lastUsageStatement = FindSiblingStatementContainingLastUsage(
                declarationStatement,
                semanticModel,
                syntaxFactsService,
                cancellationToken);

            if (lastUsageStatement == declarationStatement)
            {
                return default;
            }

            var parentStatements = GetStatements(declarationStatement.GetRequiredParent());
            var declarationStatementIndex = parentStatements.IndexOf(declarationStatement);
            var lastUsageStatementIndex = parentStatements.IndexOf(lastUsageStatement, declarationStatementIndex + 1);

            return new SyntaxList<TStatementSyntax>(parentStatements
                .Take(lastUsageStatementIndex + 1)
                .Skip(declarationStatementIndex + 1));
        }

        private static TStatementSyntax FindSiblingStatementContainingLastUsage(
            TStatementSyntax declarationSyntax,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            // We are going to step through the statements starting with the trigger variable's declaration.
            // We will track when new locals are declared and when they are used. To determine the last
            // statement that we should surround, we will walk through the locals in the order they are declared.
            // If the local's declaration index falls within the last variable usage index, we will extend
            // the last variable usage index to include the local's last usage.

            // Take all the statements starting with the trigger variable's declaration.
            var statementsFromDeclarationToEnd = declarationSyntax.GetRequiredParent().ChildNodesAndTokens()
                .Select(nodeOrToken => nodeOrToken.AsNode())
                .OfType<TStatementSyntax>()
                .SkipWhile(node => node != declarationSyntax)
                .ToImmutableArray();

            // List of local variables that will be in the order they are declared.
            using var _0 = ArrayBuilder<ISymbol>.GetInstance(out var localVariables);

            // Map a symbol to an index into the statementsFromDeclarationToEnd array.
            using var _1 = PooledDictionary<ISymbol, int>.GetInstance(out var variableDeclarationIndex);
            using var _2 = PooledDictionary<ISymbol, int>.GetInstance(out var lastVariableUsageIndex);

            // Loop through the statements from the trigger declaration to the end of the containing body.
            // By starting with the trigger declaration it will add the trigger variable to the list of
            // local variables.
            for (var statementIndex = 0; statementIndex < statementsFromDeclarationToEnd.Length; statementIndex++)
            {
                var currentStatement = statementsFromDeclarationToEnd[statementIndex];

                // Determine which local variables were referenced in this statement.
                using var _ = PooledHashSet<ISymbol>.GetInstance(out var referencedVariables);
                AddReferencedLocalVariables(referencedVariables, currentStatement, localVariables, semanticModel, syntaxFactsService, cancellationToken);

                // Update the last usage index for each of the referenced variables.
                foreach (var referencedVariable in referencedVariables)
                {
                    lastVariableUsageIndex[referencedVariable] = statementIndex;
                }

                // Determine if new variables were declared in this statement.
                var declaredVariables = semanticModel.GetAllDeclaredSymbols(currentStatement, cancellationToken);
                foreach (var declaredVariable in declaredVariables)
                {
                    // Initialize the declaration and usage index for the new variable and add it
                    // to the list of local variables.
                    variableDeclarationIndex[declaredVariable] = statementIndex;
                    lastVariableUsageIndex[declaredVariable] = statementIndex;
                    localVariables.Add(declaredVariable);
                }
            }

            // Initially we will consider the trigger declaration statement the end of the using 
            // statement. This index will grow as we examine the last usage index of the local
            // variables declared within the using statements scope.
            var endOfUsingStatementIndex = 0;

            // Walk through the local variables in the order that they were declared, starting
            // with the trigger variable.
            foreach (var localSymbol in localVariables)
            {
                var declarationIndex = variableDeclarationIndex[localSymbol];
                if (declarationIndex > endOfUsingStatementIndex)
                {
                    // If the variable was declared after the last statement to include in
                    // the using statement, we have gone far enough and other variables will
                    // also be declared outside the using statement.
                    break;
                }

                // If this variable was used later in the method than what we were considering
                // the scope of the using statement, then increase the scope to include its last
                // usage.
                endOfUsingStatementIndex = Math.Max(endOfUsingStatementIndex, lastVariableUsageIndex[localSymbol]);
            }

            return statementsFromDeclarationToEnd[endOfUsingStatementIndex];
        }

        /// <summary>
        /// Adds local variables that are being referenced within a statement to a set of symbols.
        /// </summary>
        private static void AddReferencedLocalVariables(
            HashSet<ISymbol> referencedVariables,
            SyntaxNode node,
            IReadOnlyList<ISymbol> localVariables,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            // If this node matches one of our local variables, then we can say it has been referenced.
            if (syntaxFactsService.IsIdentifierName(node))
            {
                var identifierName = syntaxFactsService.GetIdentifierOfSimpleName(node).ValueText;

                var variable = localVariables.FirstOrDefault(localVariable
                    => syntaxFactsService.StringComparer.Equals(localVariable.Name, identifierName) &&
                        localVariable.Equals(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol));

                if (variable is object)
                {
                    referencedVariables.Add(variable);
                }
            }

            // Walk through child nodes looking for references
            foreach (var nodeOrToken in node.ChildNodesAndTokens())
            {
                // If we have already referenced all the local variables we are
                // concerned with, then we can return early.
                if (referencedVariables.Count == localVariables.Count)
                {
                    return;
                }

                var childNode = nodeOrToken.AsNode();
                if (childNode is null)
                {
                    continue;
                }

                AddReferencedLocalVariables(referencedVariables, childNode, localVariables, semanticModel, syntaxFactsService, cancellationToken);
            }
        }
    }
}

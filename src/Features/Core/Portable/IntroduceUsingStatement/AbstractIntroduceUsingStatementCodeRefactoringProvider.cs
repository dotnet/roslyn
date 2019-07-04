// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        protected abstract TStatementSyntax CreateUsingStatement(TLocalDeclarationSyntax declarationStatement, SyntaxTriviaList sameLineTrivia, SyntaxList<TStatementSyntax> statementsToSurround);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var declarationSyntax = await FindDisposableLocalDeclaration(document, span, context.CancellationToken).ConfigureAwait(false);

            if (declarationSyntax != null)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    CodeActionTitle,
                    cancellationToken => IntroduceUsingStatementAsync(document, span, cancellationToken)));
            }
        }

        private async Task<TLocalDeclarationSyntax> FindDisposableLocalDeclaration(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var declarationSyntax =
                root.FindNode(selection)?.GetAncestorOrThis<TLocalDeclarationSyntax>()
                ?? root.FindTokenOnLeftOfPosition(selection.End).GetAncestor<TLocalDeclarationSyntax>();

            if (declarationSyntax is null || !CanRefactorToContainBlockStatements(declarationSyntax.Parent))
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var disposableType = semanticModel.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            if (disposableType is null)
            {
                return default;
            }

            var operation = semanticModel.GetOperation(declarationSyntax, cancellationToken) as IVariableDeclarationGroupOperation;
            if (operation?.Declarations.Length != 1)
            {
                return default;
            }

            var localDeclaration = operation.Declarations[0];
            if (localDeclaration.Declarators.Length != 1)
            {
                return default;
            }

            var declarator = localDeclaration.Declarators[0];

            var localType = declarator.Symbol?.Type;
            if (localType is null)
            {
                return default;
            }

            var initializer = (localDeclaration.Initializer ?? declarator.Initializer)?.Value;

            // Initializer kind is invalid when incomplete declaration syntax ends in an equals token.
            if (initializer is null || initializer.Kind == OperationKind.Invalid)
            {
                return default;
            }

            // Infer the intent of the selection. Offer the refactoring only if the selection
            // appears to be aimed at the declaration statement but not at its initializer expression.
            var isValidSelection = await CodeRefactoringHelpers.RefactoringSelectionIsValidAsync(
                document,
                selection,
                node: declarationSyntax,
                holes: ImmutableArray.Create(initializer.Syntax),
                cancellationToken).ConfigureAwait(false);

            if (!isValidSelection)
            {
                return default;
            }

            if (!IsLegalUsingStatementType(semanticModel.Compilation, disposableType, localType))
            {
                return default;
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
            TextSpan span,
            CancellationToken cancellationToken)
        {
            var declarationStatement = await FindDisposableLocalDeclaration(document, span, cancellationToken).ConfigureAwait(false);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            var statementsToSurround = GetStatementsToSurround(declarationStatement, semanticModel, syntaxFactsService, cancellationToken);

            // Separate the newline from the trivia that is going on the using declaration line.
            var (sameLine, endOfLine) = SplitTrailingTrivia(declarationStatement, syntaxFactsService);

            var usingStatement =
                CreateUsingStatement(
                    declarationStatement,
                    sameLine,
                    statementsToSurround)
                    .WithLeadingTrivia(declarationStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(endOfLine);

            if (statementsToSurround.Any())
            {
                var parentStatements = GetStatements(declarationStatement.Parent);
                var declarationStatementIndex = parentStatements.IndexOf(declarationStatement);

                var newParent = WithStatements(
                    declarationStatement.Parent,
                    new SyntaxList<TStatementSyntax>(parentStatements
                        .Take(declarationStatementIndex)
                        .Concat(usingStatement)
                        .Concat(parentStatements.Skip(declarationStatementIndex + 1 + statementsToSurround.Count))));

                return document.WithSyntaxRoot(root.ReplaceNode(
                    declarationStatement.Parent,
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

            var parentStatements = GetStatements(declarationStatement.Parent);
            var declarationStatementIndex = parentStatements.IndexOf(declarationStatement);
            var lastUsageStatementIndex = parentStatements.IndexOf(lastUsageStatement, declarationStatementIndex + 1);

            return new SyntaxList<TStatementSyntax>(parentStatements
                .Take(lastUsageStatementIndex + 1)
                .Skip(declarationStatementIndex + 1));
        }

        private static (SyntaxTriviaList sameLine, SyntaxTriviaList endOfLine) SplitTrailingTrivia(SyntaxNode node, ISyntaxFactsService syntaxFactsService)
        {
            var trailingTrivia = node.GetTrailingTrivia();
            var lastIndex = trailingTrivia.Count - 1;

            return lastIndex != -1 && syntaxFactsService.IsEndOfLineTrivia(trailingTrivia[lastIndex])
                ? (sameLine: trailingTrivia.RemoveAt(lastIndex), endOfLine: new SyntaxTriviaList(trailingTrivia[lastIndex]))
                : (sameLine: trailingTrivia, endOfLine: SyntaxTriviaList.Empty);
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
            var statementsAfterDeclaration = declarationSyntax.Parent.ChildNodesAndTokens()
                .Select(nodeOrToken => nodeOrToken.AsNode())
                .OfType<TStatementSyntax>()
                .SkipWhile(node => node != declarationSyntax)
                .ToArray();

            // List of local variables that will be in the order they are declared.
            var localVariables = new List<ISymbol>();

            // Map a symbol to an index into the statementsAfterDeclaration 
            var variableDeclarationIndex = PooledDictionary<ISymbol, int>.GetInstance();
            var lastVariableUsageIndex = PooledDictionary<ISymbol, int>.GetInstance();

            for (var statementIndex = 0; statementIndex < statementsAfterDeclaration.Length; statementIndex++)
            {
                var node = statementsAfterDeclaration[statementIndex];

                // Update the last usage index for any variable referenced in the statement.
                var referencedVariables = GetReferencedLocalVariables(node, localVariables, semanticModel, syntaxFactsService, cancellationToken);
                foreach (var referencedVariable in referencedVariables)
                {
                    lastVariableUsageIndex[referencedVariable] = statementIndex;
                }

                // Add new variables that were declared in the statement
                var declaredVariables = semanticModel.GetAllDeclaredSymbols(node, cancellationToken);
                foreach (var declaredVariable in declaredVariables)
                {
                    variableDeclarationIndex[declaredVariable] = statementIndex;
                    lastVariableUsageIndex[declaredVariable] = statementIndex;
                    localVariables.Add(declaredVariable);
                }
            }

            var lastUsageIndex = 0;

            // Walk through the local variables in the order that they were declared.
            foreach (var localSymbol in localVariables)
            {
                var declarationIndex = variableDeclarationIndex[localSymbol];
                if (declarationIndex > lastUsageIndex)
                {
                    // If the variable was declared after the last statement to include in
                    // the using statement, we have gone far enough and other variables will
                    // also be declared outside the using statement.
                    break;
                }

                lastUsageIndex = Math.Max(lastUsageIndex, lastVariableUsageIndex[localSymbol]);
            }

            variableDeclarationIndex.Free();
            lastVariableUsageIndex.Free();

            return statementsAfterDeclaration[lastUsageIndex];
        }

        private static ImmutableArray<ISymbol> GetReferencedLocalVariables(
            SyntaxNode node,
            List<ISymbol> localVariables,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            var referencedVariables = ArrayBuilder<ISymbol>.GetInstance();
            if (syntaxFactsService.IsIdentifierName(node))
            {
                var identifierName = syntaxFactsService.GetIdentifierOfSimpleName(node).ValueText;

                var variable = localVariables.FirstOrDefault(localVariable
                    => syntaxFactsService.StringComparer.Equals(localVariable.Name, identifierName) &&
                        localVariable.Equals(semanticModel.GetSymbolInfo(node).Symbol));

                referencedVariables.AddIfNotNull(variable);
            }

            foreach (var nodeOrToken in node.ChildNodesAndTokens())
            {
                var childNode = nodeOrToken.AsNode();
                if (childNode is null)
                {
                    continue;
                }

                var variables = GetReferencedLocalVariables(childNode, localVariables, semanticModel, syntaxFactsService, cancellationToken);
                referencedVariables.AddRange(variables);
            }

            return referencedVariables.ToImmutableAndFree();
        }

        private sealed class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}

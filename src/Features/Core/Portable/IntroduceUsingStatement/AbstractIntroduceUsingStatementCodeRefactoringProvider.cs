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
            var (document, span, cancellationToken) = context;
            var declarationSyntax = await FindDisposableLocalDeclaration(document, span, cancellationToken).ConfigureAwait(false);

            if (declarationSyntax != null)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    CodeActionTitle,
                    cancellationToken => IntroduceUsingStatementAsync(document, span, cancellationToken)));
            }
        }

        private async Task<TLocalDeclarationSyntax> FindDisposableLocalDeclaration(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var refactoringHelperService = document.GetLanguageService<IRefactoringHelpersService>();
            var declarationSyntax = await refactoringHelperService.TryGetSelectedNodeAsync<TLocalDeclarationSyntax>(document, selection, cancellationToken).ConfigureAwait(false);

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
            var statementsFromDeclarationToEnd = declarationSyntax.Parent.ChildNodesAndTokens()
                .Select(nodeOrToken => nodeOrToken.AsNode())
                .OfType<TStatementSyntax>()
                .SkipWhile(node => node != declarationSyntax)
                .ToImmutableArray();

            // List of local variables that will be in the order they are declared.
            var localVariables = ArrayBuilder<ISymbol>.GetInstance();

            // Map a symbol to an index into the statementsFromDeclarationToEnd array.
            var variableDeclarationIndex = PooledDictionary<ISymbol, int>.GetInstance();
            var lastVariableUsageIndex = PooledDictionary<ISymbol, int>.GetInstance();

            // Loop through the statements from the trigger declaration to the end of the containing body.
            // By starting with the trigger declaration it will add the trigger variable to the list of
            // local variables.
            for (var statementIndex = 0; statementIndex < statementsFromDeclarationToEnd.Length; statementIndex++)
            {
                var currentStatement = statementsFromDeclarationToEnd[statementIndex];

                // Determine which local variables were referenced in this statement.
                var referencedVariables = PooledHashSet<ISymbol>.GetInstance();
                AddReferencedLocalVariables(referencedVariables, currentStatement, localVariables, semanticModel, syntaxFactsService, cancellationToken);

                // Update the last usage index for each of the referenced variables.
                foreach (var referencedVariable in referencedVariables)
                {
                    lastVariableUsageIndex[referencedVariable] = statementIndex;
                }

                referencedVariables.Free();

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

            localVariables.Free();
            variableDeclarationIndex.Free();
            lastVariableUsageIndex.Free();

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
                        localVariable.Equals(semanticModel.GetSymbolInfo(node).Symbol));

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

        private sealed class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}

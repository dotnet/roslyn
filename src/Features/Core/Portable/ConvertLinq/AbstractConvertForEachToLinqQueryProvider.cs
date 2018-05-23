// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertForEachToLinqQueryProvider<TForEachStatement, TStatement> : CodeRefactoringProvider
        where TForEachStatement : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var forEachStatement = FindNodeToRefactor(root, context.Span);
            if (forEachStatement == null)
            {
                return;
            }

            if (!Validate(forEachStatement))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (TryBuildConverter(forEachStatement, semanticModel, cancellationToken, out IConverter converter) &&
                !semanticModel.GetDiagnostics(forEachStatement.Span, cancellationToken).Any(diagnostic => diagnostic.DefaultSeverity == DiagnosticSeverity.Error))
            {
                context.RegisterRefactoring(new ForEachToLinqQueryCodeAction(Title, c =>
                {
                    var editor = new SyntaxEditor(semanticModel.SyntaxTree.GetRoot(c), document.Project.Solution.Workspace);
                    converter.Convert(editor, semanticModel, c);
                    return Task.FromResult(document.WithSyntaxRoot(editor.GetChangedRoot()));
                }));
            }
        }

        protected abstract string Title { get; }

        protected abstract TForEachStatement FindNodeToRefactor(SyntaxNode root, TextSpan span);

        // Performs a validation of the foreach statement.
        protected abstract bool Validate(TForEachStatement forEachStatement);

        /// <summary>
        /// Parses the forEachStatement until a child node cannot be converted into a query clause.
        /// </summary>
        /// <param name="forEachStatement"></param>
        /// <returns>
        /// 1) nodes that can be converted into clauses
        /// 2) identifiers introduced in nodes that can be converted
        /// 3) statements that cannot be converted into clauses
        /// </returns>
        protected abstract (ImmutableArray<SyntaxNode> ConvertingNodes, ImmutableArray<SyntaxToken> Identifiers, ImmutableArray<TStatement> Statements) CreateForEachInfo(TForEachStatement forEachStatement);

        /// <summary>
        /// Tries to build a specific converter that covers e.g. Count, ToList, yield return or other similar cases.
        /// </summary>
        protected abstract bool TryBuildSpecificConverter(
            TForEachStatement forEachStatement,
            SemanticModel semanticModel,
            ImmutableArray<SyntaxNode> convertingNodes,
            TStatement statementCannotBeConverted,
            CancellationToken cancellationToken,
            out IConverter converter);

        /// <summary>
        /// Creates a default converter where foreach is joined with some children statements but other children statements are kept unmodified.
        /// Example:
        /// foreach(...)
        /// {
        ///     if (condition)
        ///     {
        ///        doSomething();
        ///     }
        /// }
        /// is converted to
        /// foreach(... where condition)
        /// {
        ///        doSomething(); 
        /// }
        /// </summary>
        protected abstract IConverter CreateDefaultConverter(
            TForEachStatement forEachStatement,
            ImmutableArray<SyntaxNode> convertingNodes,
            ImmutableArray<SyntaxToken> identifiers,
            ImmutableArray<TStatement> statements);

        /// <summary>
        /// Tries to build either a specific or a default converter.
        /// </summary>
        private bool TryBuildConverter(
            TForEachStatement forEachStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IConverter converter)
        {
            var (convertingNodes, identifiers, statementsCannotBeConverted) = CreateForEachInfo(forEachStatement);

            if (statementsCannotBeConverted.Length == 1 &&
                TryBuildSpecificConverter(forEachStatement, semanticModel, convertingNodes, statementsCannotBeConverted.Single(), cancellationToken, out converter))
            {
                return true;
            }

            // No sense to convert a single foreach to foreach over the same collection.
            if (convertingNodes.Length >= 1)
            {
                converter = CreateDefaultConverter(forEachStatement, convertingNodes, identifiers, statementsCannotBeConverted);
                return true;
            }

            converter = default;
            return false;
        }

        protected interface IConverter
        {
            void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken);
        }

        private class ForEachToLinqQueryCodeAction : CodeAction.DocumentChangeAction
        {
            public ForEachToLinqQueryCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument) { }
        }
    }
}

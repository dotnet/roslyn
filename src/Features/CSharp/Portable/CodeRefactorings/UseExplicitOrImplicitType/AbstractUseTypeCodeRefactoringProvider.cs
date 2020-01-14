// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseType
{
    internal abstract class AbstractUseTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract string Title { get; }
        protected abstract Task HandleDeclarationAsync(Document document, SyntaxEditor editor, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract TypeStyleResult AnalyzeTypeName(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var declaration = await GetDeclarationAsync(context).ConfigureAwait(false);
            if (declaration == null)
            {
                return;
            }

            Debug.Assert(declaration.IsKind(SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression));

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declaredType = FindAnalyzableType(declaration, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var typeStyle = AnalyzeTypeName(declaredType, semanticModel, optionSet, cancellationToken);
            if (typeStyle.IsStylePreferred && typeStyle.Severity != ReportDiagnostic.Suppress)
            {
                // the analyzer would handle this.  So we do not.
                return;
            }

            if (!typeStyle.CanConvert())
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    Title,
                    c => UpdateDocumentAsync(document, declaredType, c)),
                declaredType.Span);
        }

        private static async Task<SyntaxNode> GetDeclarationAsync(CodeRefactoringContext context)
        {
            // We want to provide refactoring for changing the Type of newly introduced variables in following cases:
            // - DeclarationExpressionSyntax: `"42".TryParseInt32(out var number)`
            // - VariableDeclarationSyntax: General field / variable declaration statement `var number = 42`
            // - ForEachStatementSyntax: The variable that gets introduced by foreach `foreach(var number in numbers)`
            //
            // In addition to providing the refactoring when the whole node (i.e. the node that introduces the new variable) in question is selected 
            // we also want to enable it when only the type node is selected because this refactoring changes the type. We still have to make sure 
            // we're only working on TypeNodes for in above-mentioned situations.
            //
            // For foreach we need to guard against selecting just the expression because it is also of type `TypeSyntax`.

            var declNode = await context.TryGetRelevantNodeAsync<DeclarationExpressionSyntax>().ConfigureAwait(false);
            if (declNode != null)
            {
                return declNode;
            }

            var variableNode = await context.TryGetRelevantNodeAsync<VariableDeclarationSyntax>().ConfigureAwait(false);
            if (variableNode != null)
            {
                return variableNode;
            }

            var foreachStatement = await context.TryGetRelevantNodeAsync<ForEachStatementSyntax>().ConfigureAwait(false);
            if (foreachStatement != null)
            {
                return foreachStatement;
            }

            var syntaxFacts = context.Document.GetLanguageService<ISyntaxFactsService>();

            var typeNode = await context.TryGetRelevantNodeAsync<TypeSyntax>().ConfigureAwait(false);
            var typeNodeParent = typeNode?.Parent;
            if (typeNodeParent != null &&
                (typeNodeParent.IsKind(SyntaxKind.DeclarationExpression, SyntaxKind.VariableDeclaration) ||
                (typeNodeParent.IsKind(SyntaxKind.ForEachStatement) && !syntaxFacts.IsExpressionOfForeach(typeNode))))
            {
                return typeNodeParent;
            }

            return null;
        }

        private async Task<Document> UpdateDocumentAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            await HandleDeclarationAsync(document, editor, node, cancellationToken).ConfigureAwait(false);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}

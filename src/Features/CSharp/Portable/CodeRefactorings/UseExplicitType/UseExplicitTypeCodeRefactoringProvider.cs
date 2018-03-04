// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using System.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle.CSharpTypeStyleDiagnosticAnalyzerBase;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseExplicitType), Shared]
    internal partial class UseExplicitTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly CSharpUseExplicitTypeHelper s_useExplicitTypeHelper = new CSharpUseExplicitTypeHelper();

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var declaration = GetDeclaration(root, textSpan);
            Debug.Assert(declaration.IsKind(SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression));

            TypeSyntax declaredType = s_useExplicitTypeHelper.CanOfferFix(declaration, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            State state = State.Generate(declaration, semanticModel, optionSet,
                isVariableDeclarationContext: declaration.IsKind(SyntaxKind.VariableDeclaration), cancellationToken: cancellationToken);

            // UseExplicitType analyzer/fixer already gives an action in this case
            if (s_useExplicitTypeHelper.IsStylePreferred(semanticModel, optionSet, state, cancellationToken))
            {
                return;
            }

            Debug.Assert(state != null, "analyzing a declaration and state is null.");
            if (!s_useExplicitTypeHelper.TryAnalyzeVariableDeclaration(declaredType, semanticModel, optionSet, cancellationToken))
            {
                return;
            }

            MakeRefactoring(context, document, declaredType, textSpan, cancellationToken);
        }

        private static SyntaxNode GetDeclaration(SyntaxNode root, TextSpan textSpan)
        {
            // Ex:
            // var (x, y) = ...
            // (var x, ...) = ...
            // foreach (var (x, y) in ...) ...
            var declarationExpression = root.FindToken(textSpan.Start).GetAncestor<DeclarationExpressionSyntax>();
            if (declarationExpression != null)
            {
                return declarationExpression;
            }

            // Ex:
            // var x = ...;
            // var x;
            // for (var x ... ) ...
            // using (var x = ...) ...
            var localDeclaration = root.FindToken(textSpan.Start).GetAncestor<VariableDeclarationSyntax>();
            if (localDeclaration != null)
            {
                return localDeclaration;
            }

            // Ex:
            // foreach (var x in ...) ...
            var foreachStatement = root.FindToken(textSpan.Start).GetAncestor<ForEachStatementSyntax>();
            if (foreachStatement != null)
            {
                return foreachStatement;
            }

            return null;
        }

        private static void MakeRefactoring(CodeRefactoringContext context, Document document, TypeSyntax typeName, TextSpan textSpan, CancellationToken cancellationToken)
        {
            if (!typeName.IsParentKind(SyntaxKind.DeclarationExpression))
            {
            }

            if (typeName.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            if (!typeName.Span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Use_explicit_type,
                    c => UpdateDocumentAsync(document, typeName, c)));
        }

        private static async Task<Document> UpdateDocumentAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            await UseExplicitTypeCodeFixProvider.HandleDeclarationAsync(document, editor, node, cancellationToken).ConfigureAwait(false);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}

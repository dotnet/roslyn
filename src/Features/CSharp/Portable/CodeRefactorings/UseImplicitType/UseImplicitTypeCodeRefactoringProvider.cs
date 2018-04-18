// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using System.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle.CSharpTypeStyleDiagnosticAnalyzerBase;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseImplicitType), Shared]
    internal partial class UseImplicitTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly CSharpUseImplicitTypeHelper s_useImplicitTypeHelper = new CSharpUseImplicitTypeHelper();

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
            if (declaration == null)
            {
                return;
            }

            Debug.Assert(declaration.IsKind(SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression));

            var declaredType = s_useImplicitTypeHelper.FindAnalyzableType(declaration, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            if (declaredType.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            if (!declaredType.Span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            var state = State.Generate(declaration, semanticModel, optionSet,
                isVariableDeclarationContext: declaration.IsKind(SyntaxKind.VariableDeclaration), cancellationToken: cancellationToken);

            // UseImplicitType analyzer/fixer already gives an action in this case
            if (s_useImplicitTypeHelper.IsStylePreferred(semanticModel, optionSet, state, cancellationToken))
            {
                return;
            }

            Debug.Assert(state != null, "analyzing a declaration and state is null.");
            if (!s_useImplicitTypeHelper.TryAnalyzeVariableDeclaration(declaredType, semanticModel, optionSet, cancellationToken))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Use_implicit_type,
                    c => UpdateDocumentAsync(document, declaredType, c)));
        }

        private static SyntaxNode GetDeclaration(SyntaxNode root, TextSpan textSpan)
        {
            var token = root.FindToken(textSpan.Start);
            return token.Parent?.FirstAncestorOrSelf<SyntaxNode>(
                a => a.IsKind(SyntaxKind.DeclarationExpression, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement));
        }

        private static async Task<Document> UpdateDocumentAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            UseImplicitTypeCodeFixProvider.HandleDeclaration(editor, node);

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

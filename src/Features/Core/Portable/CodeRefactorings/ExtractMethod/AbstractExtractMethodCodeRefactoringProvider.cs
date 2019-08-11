// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.ExtractMethod), Shared]
    internal class ExtractMethodCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        public ExtractMethodCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Don't bother if there isn't a selection
            var (document, textSpan, cancellationToken) = context;
            if (textSpan.IsEmpty)
            {
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var activeInlineRenameSession = workspace.Services.GetService<ICodeRefactoringHelpersService>().ActiveInlineRenameSession;
            if (activeInlineRenameSession)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var action = await GetCodeActionAsync(document, textSpan, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (action == null)
            {
                return;
            }

            context.RegisterRefactoring(action.Item1);
        }

        private async Task<Tuple<CodeAction, string>> GetCodeActionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var description = documentOptions.GetOption(ExtractMethodOptions.AllowMovingDeclaration) ?
                                      FeaturesResources.Extract_Method_plus_Local : FeaturesResources.Extract_Method;

                var codeAction = new MyCodeAction(description, c => AddRenameAnnotationAsync(result.Document, result.InvocationNameToken, c));
                var methodBlock = result.MethodDeclarationNode;

                return Tuple.Create<CodeAction, string>(codeAction, methodBlock.ToString());
            }

            return null;
        }

        private async Task<Document> AddRenameAnnotationAsync(Document document, SyntaxToken invocationNameToken, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var finalRoot = root.ReplaceToken(
                invocationNameToken,
                invocationNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()));

            return document.WithSyntaxRoot(finalRoot);
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

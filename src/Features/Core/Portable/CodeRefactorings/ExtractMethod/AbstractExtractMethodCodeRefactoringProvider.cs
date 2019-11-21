// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

            var actions = await GetCodeActionAsync(document, textSpan, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (actions.IsEmpty)
            {
                return;
            }

            foreach (var action in actions)
            {
                context.RegisterRefactoring(action, textSpan);
            }
        }

        protected virtual async Task<ImmutableArray<CodeAction>> GetCodeActionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var actions = ImmutableArray.CreateBuilder<CodeAction>();
            var methodAction = await ExtractMethod(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (methodAction != default)
            {
                actions.Add(methodAction);
            }

            var localFunctionAction = await ExtractLocalFunction(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (localFunctionAction != default)
            {
                actions.Add(localFunctionAction);
            }

            return actions.ToImmutable();
        }

        private async Task<CodeAction> ExtractMethod(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                localFunction: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var description = documentOptions.GetOption(ExtractMethodOptions.AllowMovingDeclaration) ?
                                      FeaturesResources.Extract_method_plus_local : FeaturesResources.Extract_method;

                var codeAction = new MyCodeAction(description, c => AddRenameAnnotationAsync(result.Document, result.InvocationNameToken, c));
                var methodBlock = result.MethodDeclarationNode;

                return codeAction;
            }

            return default;
        }

        private async Task<CodeAction> ExtractLocalFunction(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!syntaxFacts.SupportsLocalFunctionDeclaration())
            {
                return default;
            }

            var localFunctionResult = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                localFunction: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(localFunctionResult);

            if (localFunctionResult.Succeeded || localFunctionResult.SucceededWithSuggestion)
            {
                var codeAction = new MyCodeAction(FeaturesResources.Extract_local_function, c => AddRenameAnnotationAsync(localFunctionResult.Document, localFunctionResult.InvocationNameToken, c));
                return codeAction;
            }

            return default;
        }

        protected async Task<Document> AddRenameAnnotationAsync(Document document, SyntaxToken invocationNameToken, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var finalRoot = root.ReplaceToken(
                invocationNameToken,
                invocationNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()));

            return document.WithSyntaxRoot(finalRoot);
        }

        protected class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}

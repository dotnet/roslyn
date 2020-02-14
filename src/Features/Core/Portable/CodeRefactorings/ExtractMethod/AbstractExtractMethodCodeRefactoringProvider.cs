﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
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

            var actions = await GetCodeActionsAsync(document, textSpan, cancellationToken: cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        private async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var actions = ArrayBuilder<CodeAction>.GetInstance();
            var methodAction = await ExtractMethod(document, textSpan, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(methodAction);

            var localFunctionAction = await ExtractLocalFunction(document, textSpan, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(localFunctionAction);

            return actions.ToImmutableAndFree();
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

            return null;
        }

        private async Task<CodeAction> ExtractLocalFunction(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!syntaxFacts.SupportsLocalFunctionDeclaration(syntaxTree.Options))
            {
                return null;
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

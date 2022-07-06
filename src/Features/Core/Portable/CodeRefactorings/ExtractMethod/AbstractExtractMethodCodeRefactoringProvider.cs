// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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

            var options = context.Options.ExtractMethodOptions;
            var actions = await GetCodeActionsAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        private static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            Document document,
            TextSpan textSpan,
            ExtractMethodOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
            var methodAction = await ExtractMethodAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(methodAction);

            var localFunctionAction = await ExtractLocalFunctionAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(localFunctionAction);

            return actions.ToImmutable();
        }

        private static async Task<CodeAction> ExtractMethodAsync(Document document, TextSpan textSpan, ExtractMethodOptions options, CancellationToken cancellationToken)
        {
            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                localFunction: false,
                options,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(result);

            if (!result.Succeeded && !result.SucceededWithSuggestion)
                return null;

            return new MyCodeAction(
                FeaturesResources.Extract_method,
                async c =>
                {
                    var (document, invocationNameToken) = await result.GetFormattedDocumentAsync(c).ConfigureAwait(false);
                    return await AddRenameAnnotationAsync(document, invocationNameToken, c).ConfigureAwait(false);
                },
                nameof(FeaturesResources.Extract_method));
        }

        private static async Task<CodeAction> ExtractLocalFunctionAsync(Document document, TextSpan textSpan, ExtractMethodOptions options, CancellationToken cancellationToken)
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
                options,
                cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(localFunctionResult);

            if (localFunctionResult.Succeeded || localFunctionResult.SucceededWithSuggestion)
            {
                var codeAction = new MyCodeAction(
                    FeaturesResources.Extract_local_function,
                    async c =>
                    {
                        var (document, invocationNameToken) = await localFunctionResult.GetFormattedDocumentAsync(c).ConfigureAwait(false);
                        return await AddRenameAnnotationAsync(document, invocationNameToken, c).ConfigureAwait(false);
                    },
                    nameof(FeaturesResources.Extract_local_function));
                return codeAction;
            }

            return null;
        }

        private static async Task<Document> AddRenameAnnotationAsync(Document document, SyntaxToken invocationNameToken, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var finalRoot = root.ReplaceToken(
                invocationNameToken,
                invocationNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()));

            return document.WithSyntaxRoot(finalRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}

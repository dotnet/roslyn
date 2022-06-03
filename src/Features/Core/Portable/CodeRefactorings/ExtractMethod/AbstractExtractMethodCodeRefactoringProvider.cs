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
using Microsoft.CodeAnalysis.CodeCleanup;
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

            var extractOptions = await document.GetExtractMethodGenerationOptionsAsync(context.Options, cancellationToken).ConfigureAwait(false);
            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(context.Options, cancellationToken).ConfigureAwait(false);

            var actions = await GetCodeActionsAsync(document, textSpan, extractOptions, cleanupOptions, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        private static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            Document document,
            TextSpan textSpan,
            ExtractMethodGenerationOptions extractOptions,
            CodeCleanupOptions cleanupOptions,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
            var methodAction = await ExtractMethodAsync(document, textSpan, extractOptions, cleanupOptions, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(methodAction);

            var localFunctionAction = await ExtractLocalFunctionAsync(document, textSpan, extractOptions, cleanupOptions, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(localFunctionAction);

            return actions.ToImmutable();
        }

        private static async Task<CodeAction> ExtractMethodAsync(Document document, TextSpan textSpan, ExtractMethodGenerationOptions extractOptions, CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
        {
            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                localFunction: false,
                extractOptions,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(result);

            if (!result.Succeeded && !result.SucceededWithSuggestion)
                return null;

            return CodeAction.Create(
                FeaturesResources.Extract_method,
                async c =>
                {
                    var (document, invocationNameToken) = await result.GetFormattedDocumentAsync(cleanupOptions, c).ConfigureAwait(false);
                    return await AddRenameAnnotationAsync(document, invocationNameToken, c).ConfigureAwait(false);
                },
                nameof(FeaturesResources.Extract_method));
        }

        private static async Task<CodeAction> ExtractLocalFunctionAsync(Document document, TextSpan textSpan, ExtractMethodGenerationOptions extractOptions, CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
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
                extractOptions,
                cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(localFunctionResult);

            if (localFunctionResult.Succeeded || localFunctionResult.SucceededWithSuggestion)
            {
                var codeAction = CodeAction.Create(
                    FeaturesResources.Extract_local_function,
                    async c =>
                    {
                        var (document, invocationNameToken) = await localFunctionResult.GetFormattedDocumentAsync(cleanupOptions, c).ConfigureAwait(false);
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
    }
}

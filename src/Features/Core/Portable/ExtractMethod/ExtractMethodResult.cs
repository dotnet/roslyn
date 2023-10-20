// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal sealed class ExtractMethodResult
    {
        /// <summary>
        /// True if the extract method operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The reasons why the extract method operation did not succeed.
        /// </summary>
        public ImmutableArray<string> Reasons { get; }

        private readonly AsyncLazy<(Document documentWithoutFinalFormatting, ImmutableArray<AbstractFormattingRule> FormattingRules, SyntaxToken invocationNameToken)>? _lazyData;

        internal ExtractMethodResult(
            OperationStatusFlag status,
            ImmutableArray<string> reasons,
            Func<CancellationToken, Task<(Document documentWithoutFinalFormatting, ImmutableArray<AbstractFormattingRule> FormattingRules, SyntaxToken invocationNameToken)>>? computeDataAsync)
        {
            Succeeded = status.Succeeded();

            Reasons = reasons.NullToEmpty();

            if (computeDataAsync != null)
                _lazyData = AsyncLazy.Create(computeDataAsync);
        }

        public static ExtractMethodResult Fail(OperationStatus status)
            => new(status.Flag, status.Reasons, computeDataAsync: null);

        public static ExtractMethodResult Success(
            OperationStatus status,
            Func<CancellationToken, Task<(Document documentWithoutFinalFormatting, ImmutableArray<AbstractFormattingRule> FormattingRules, SyntaxToken invocationNameToken)>> computeDataAsync)
        {
            return new(status.Flag, status.Reasons, computeDataAsync);
        }

        /// <summary>
        /// The transformed document that was produced as a result of the extract method operation.
        /// </summary>
        public async Task<Document?> GetDocumentWithoutFinalFormattingAsync(CancellationToken cancellationToken)
            => _lazyData is null ? null : (await _lazyData.GetValueAsync(cancellationToken).ConfigureAwait(false)).documentWithoutFinalFormatting;

        /// <summary>
        /// Formatting rules to apply to <see cref="GetDocumentWithoutFinalFormattingAsync"/> to obtain the final
        /// formatted document.
        /// </summary>
        public async Task<ImmutableArray<AbstractFormattingRule>> GetFormattingRulesAsync(CancellationToken cancellationToken)
            => _lazyData is null ? ImmutableArray<AbstractFormattingRule>.Empty : (await _lazyData.GetValueAsync(cancellationToken).ConfigureAwait(false)).FormattingRules;

        /// <summary>
        /// The name token for the invocation node that replaces the extracted code.
        /// </summary>
        public async Task<SyntaxToken?> GetInvocationNameTokenAsync(CancellationToken cancellationToken)
            => _lazyData is null ? null : (await _lazyData.GetValueAsync(cancellationToken).ConfigureAwait(false)).invocationNameToken;

        public async Task<(Document document, SyntaxToken? invocationNameToken)> GetFormattedDocumentAsync(CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
        {
            var documentWithoutFinalFormatting = await this.GetDocumentWithoutFinalFormattingAsync(cancellationToken).ConfigureAwait(false);
            if (documentWithoutFinalFormatting is null)
                throw new InvalidOperationException();

            var annotation = new SyntaxAnnotation();

            var root = await documentWithoutFinalFormatting.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var invocationNameToken = await this.GetInvocationNameTokenAsync(cancellationToken).ConfigureAwait(false);
            if (invocationNameToken != null)
                root = root.ReplaceToken(invocationNameToken.Value, invocationNameToken.Value.WithAdditionalAnnotations(annotation));

            var annotatedDocument = documentWithoutFinalFormatting.WithSyntaxRoot(root);
            var simplifiedDocument = await Simplifier.ReduceAsync(annotatedDocument, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
            var simplifiedRoot = await simplifiedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var services = documentWithoutFinalFormatting.Project.Solution.Services;

            var formattingRules = await this.GetFormattingRulesAsync(cancellationToken).ConfigureAwait(false);
            var formattedDocument = simplifiedDocument.WithSyntaxRoot(
                Formatter.Format(simplifiedRoot, Formatter.Annotation, services, cleanupOptions.FormattingOptions, formattingRules, cancellationToken));

            var formattedRoot = await formattedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var finalInvocationNameToken = formattedRoot.GetAnnotatedTokens(annotation).SingleOrDefault();
            return (formattedDocument, finalInvocationNameToken == default ? null : finalInvocationNameToken);
        }
    }
}

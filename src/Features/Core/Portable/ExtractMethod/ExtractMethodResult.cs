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

        /// <summary>
        /// The transformed document that was produced as a result of the extract method operation.
        /// </summary>
        public Document? DocumentWithoutFinalFormatting { get; }

        /// <summary>
        /// Formatting rules to apply to <see cref="DocumentWithoutFinalFormatting"/> to obtain the final formatted
        /// document.
        /// </summary>
        public ImmutableArray<AbstractFormattingRule> FormattingRules { get; }

        /// <summary>
        /// The name token for the invocation node that replaces the extracted code.
        /// </summary>
        public SyntaxToken? InvocationNameToken { get; }

        internal ExtractMethodResult(
            OperationStatusFlag status,
            ImmutableArray<string> reasons,
            Document? documentWithoutFinalFormatting,
            ImmutableArray<AbstractFormattingRule> formattingRules,
            SyntaxToken? invocationNameToken)
        {
            Succeeded = status.Succeeded();

            Reasons = reasons.NullToEmpty();

            DocumentWithoutFinalFormatting = documentWithoutFinalFormatting;
            FormattingRules = formattingRules;
            InvocationNameToken = invocationNameToken;
        }

        public static ExtractMethodResult Fail(OperationStatus status)
            => new(status.Flag, status.Reasons, null, ImmutableArray<AbstractFormattingRule>.Empty, invocationNameToken: null);

        public static ExtractMethodResult Success(
            OperationStatus status,
            Document documentWithoutFinalFormatting,
            ImmutableArray<AbstractFormattingRule> formattingRules,
            SyntaxToken invocationNameToken)
        {
            return new(status.Flag, status.Reasons, documentWithoutFinalFormatting, formattingRules, invocationNameToken);
        }

        public async Task<(Document document, SyntaxToken? invocationNameToken)> GetFormattedDocumentAsync(CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
        {
            if (DocumentWithoutFinalFormatting is null)
                throw new InvalidOperationException();

            var annotation = new SyntaxAnnotation();

            var root = await DocumentWithoutFinalFormatting.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (InvocationNameToken != null)
                root = root.ReplaceToken(InvocationNameToken.Value, InvocationNameToken.Value.WithAdditionalAnnotations(annotation));

            var annotatedDocument = DocumentWithoutFinalFormatting.WithSyntaxRoot(root);
            var simplifiedDocument = await Simplifier.ReduceAsync(annotatedDocument, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
            var simplifiedRoot = await simplifiedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var services = DocumentWithoutFinalFormatting.Project.Solution.Services;

            var formattedDocument = simplifiedDocument.WithSyntaxRoot(
                Formatter.Format(simplifiedRoot, Formatter.Annotation, services, cleanupOptions.FormattingOptions, FormattingRules, cancellationToken));

            var formattedRoot = await formattedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var finalInvocationNameToken = formattedRoot.GetAnnotatedTokens(annotation).SingleOrDefault();
            return (formattedDocument, finalInvocationNameToken == default ? null : finalInvocationNameToken);
        }
    }
}

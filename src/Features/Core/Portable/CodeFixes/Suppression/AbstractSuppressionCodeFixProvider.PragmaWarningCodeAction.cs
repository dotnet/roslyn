// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
{
    internal sealed class PragmaWarningCodeAction : AbstractSuppressionCodeAction, IPragmaBasedCodeAction
    {
        private readonly SuppressionTargetInfo _suppressionTargetInfo;
        private readonly Document _document;
        private readonly SyntaxFormattingOptions _options;
        private readonly Diagnostic _diagnostic;
        private readonly bool _forFixMultipleContext;

        public static PragmaWarningCodeAction Create(
            SuppressionTargetInfo suppressionTargetInfo,
            Document document,
            SyntaxFormattingOptions options,
            Diagnostic diagnostic,
            AbstractSuppressionCodeFixProvider fixer)
        {
            // We need to normalize the leading trivia on start token to account for
            // the trailing trivia on its previous token (and similarly normalize trailing trivia for end token).
            PragmaHelpers.NormalizeTriviaOnTokens(fixer, ref document, ref suppressionTargetInfo);

            return new PragmaWarningCodeAction(suppressionTargetInfo, document, options, diagnostic, fixer);
        }

        private PragmaWarningCodeAction(
            SuppressionTargetInfo suppressionTargetInfo,
            Document document,
            SyntaxFormattingOptions options,
            Diagnostic diagnostic,
            AbstractSuppressionCodeFixProvider fixer,
            bool forFixMultipleContext = false)
            : base(fixer, title: FeaturesResources.in_Source)
        {
            _suppressionTargetInfo = suppressionTargetInfo;
            _document = document;
            _options = options;
            _diagnostic = diagnostic;
            _forFixMultipleContext = forFixMultipleContext;
        }

        public PragmaWarningCodeAction CloneForFixMultipleContext()
            => new(_suppressionTargetInfo, _document, _options, _diagnostic, Fixer, forFixMultipleContext: true);

        protected override string DiagnosticIdForEquivalenceKey
            => _forFixMultipleContext ? string.Empty : _diagnostic.Id;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            => await GetChangedDocumentAsync(includeStartTokenChange: true, includeEndTokenChange: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        public async Task<Document> GetChangedDocumentAsync(bool includeStartTokenChange, bool includeEndTokenChange, CancellationToken cancellationToken)
        {
            return await PragmaHelpers.GetChangeDocumentWithPragmaAdjustedAsync(
                _document,
                _diagnostic.Location.SourceSpan,
                _suppressionTargetInfo,
                (startToken, currentDiagnosticSpan) =>
                {
                    return includeStartTokenChange
                        ? PragmaHelpers.GetNewStartTokenWithAddedPragma(startToken, currentDiagnosticSpan, _diagnostic, Fixer, FormatNode, isRemoveSuppression: false, cancellationToken)
                        : startToken;
                },
                (endToken, currentDiagnosticSpan) =>
                {
                    return includeEndTokenChange
                        ? PragmaHelpers.GetNewEndTokenWithAddedPragma(endToken, currentDiagnosticSpan, _diagnostic, Fixer, FormatNode, isRemoveSuppression: false, cancellationToken)
                        : endToken;
                },
                cancellationToken).ConfigureAwait(false);
        }

        public SyntaxToken StartToken_TestOnly => _suppressionTargetInfo.StartToken;
        public SyntaxToken EndToken_TestOnly => _suppressionTargetInfo.EndToken;

        private SyntaxNode FormatNode(SyntaxNode node, CancellationToken cancellationToken)
            => Formatter.Format(node, _document.Project.Solution.Services, _options, cancellationToken);
    }
}

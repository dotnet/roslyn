// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class PragmaWarningCodeAction : AbstractSuppressionCodeAction, IPragmaBasedCodeAction
        {
            private readonly SuppressionTargetInfo _suppressionTargetInfo;
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;
            private readonly bool _forFixMultipleContext;

            public static PragmaWarningCodeAction Create(
                    SuppressionTargetInfo suppressionTargetInfo,
                    Document document,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer)
            {
                // We need to normalize the leading trivia on start token to account for
                // the trailing trivia on its previous token (and similarly normalize trailing trivia for end token).
                PragmaHelpers.NormalizeTriviaOnTokens(fixer, ref document, ref suppressionTargetInfo);

                return new PragmaWarningCodeAction(suppressionTargetInfo, document, diagnostic, fixer);
            }

            private PragmaWarningCodeAction(
                SuppressionTargetInfo suppressionTargetInfo,
                Document document,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                bool forFixMultipleContext = false)
                : base(fixer, title: FeaturesResources.SuppressWithPragma)
            {
                _suppressionTargetInfo = suppressionTargetInfo;
                _document = document;
                _diagnostic = diagnostic;
                _forFixMultipleContext = forFixMultipleContext;
            }

            public PragmaWarningCodeAction CloneForFixMultipleContext()
            {
                return new PragmaWarningCodeAction(_suppressionTargetInfo, _document, _diagnostic, Fixer, forFixMultipleContext: true);

            }
            protected override string DiagnosticIdForEquivalenceKey =>
                _forFixMultipleContext ? string.Empty : _diagnostic.Id;

            protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return await GetChangedDocumentAsync(includeStartTokenChange: true, includeEndTokenChange: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            public async Task<Document> GetChangedDocumentAsync(bool includeStartTokenChange, bool includeEndTokenChange, CancellationToken cancellationToken)
            {
                return await PragmaHelpers.GetChangeDocumentWithPragmaAdjustedAsync(
                    _document,
                    _diagnostic.Location.SourceSpan,
                    _suppressionTargetInfo,
                    async (startToken, currentDiagnosticSpan) => 
                    {
                        return includeStartTokenChange
                            ? await PragmaHelpers.GetNewStartTokenWithAddedPragmaAsync(startToken, currentDiagnosticSpan, _diagnostic, Fixer, FormatNodeAsync).ConfigureAwait(false)
                            : startToken;
                    },
                    async (endToken, currentDiagnosticSpan) =>
                    {
                        return includeEndTokenChange
                            ? await PragmaHelpers.GetNewEndTokenWithAddedPragmaAsync(endToken, currentDiagnosticSpan, _diagnostic, Fixer, FormatNodeAsync).ConfigureAwait(false)
                            : endToken;
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            public SyntaxToken StartToken_TestOnly => _suppressionTargetInfo.StartToken;
            public SyntaxToken EndToken_TestOnly => _suppressionTargetInfo.EndToken;

            private Task<SyntaxNode> FormatNodeAsync(SyntaxNode node)
            {
                return Formatter.FormatAsync(node, _document.Project.Solution.Workspace);
            }
        }
    }
}

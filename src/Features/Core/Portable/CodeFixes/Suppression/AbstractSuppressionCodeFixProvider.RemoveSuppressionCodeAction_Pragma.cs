// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal abstract partial class RemoveSuppressionCodeAction
        {
            /// <summary>
            /// Code action to edit/remove/add the pragma directives for removing diagnostic suppression.
            /// </summary>
            private class PragmaRemoveAction : RemoveSuppressionCodeAction, IPragmaBasedCodeAction
            {
                private readonly Document _document;
                private readonly SuppressionTargetInfo _suppressionTargetInfo;

                public static PragmaRemoveAction Create(
                    SuppressionTargetInfo suppressionTargetInfo,
                    Document document,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer)
                {
                    // We need to normalize the leading trivia on start token to account for
                    // the trailing trivia on its previous token (and similarly normalize trailing trivia for end token).
                    PragmaHelpers.NormalizeTriviaOnTokens(fixer, ref document, ref suppressionTargetInfo);

                    return new PragmaRemoveAction(suppressionTargetInfo, document, diagnostic, fixer);
                }

                private PragmaRemoveAction(
                    SuppressionTargetInfo suppressionTargetInfo,
                    Document document,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer,
                    bool forFixMultipleContext = false)
                    : base(diagnostic, fixer, forFixMultipleContext)
                {
                    _document = document;
                    _suppressionTargetInfo = suppressionTargetInfo;
                }

                public override RemoveSuppressionCodeAction CloneForFixMultipleContext()
                {
                    return new PragmaRemoveAction(_suppressionTargetInfo, _document, _diagnostic, Fixer, forFixMultipleContext: true);
                }

                public override SyntaxTree SyntaxTreeToModify => _suppressionTargetInfo.StartToken.SyntaxTree;

                protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                {
                    return await GetChangedDocumentAsync(includeStartTokenChange: true, includeEndTokenChange: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                public async Task<Document> GetChangedDocumentAsync(bool includeStartTokenChange, bool includeEndTokenChange, CancellationToken cancellationToken)
                {
                    bool add = false;
                    bool toggle = false;

                    int indexOfLeadingPragmaDisableToRemove = -1, indexOfTrailingPragmaEnableToRemove = -1;
                    if (CanRemovePragmaTrivia(_suppressionTargetInfo.StartToken, _diagnostic, Fixer, isStartToken: true, indexOfTriviaToRemove: out indexOfLeadingPragmaDisableToRemove) &&
                        CanRemovePragmaTrivia(_suppressionTargetInfo.EndToken, _diagnostic, Fixer, isStartToken: false, indexOfTriviaToRemove: out indexOfTrailingPragmaEnableToRemove))
                    {
                        // Verify if there is no other trivia before the start token would again cause this diagnostic to be suppressed.
                        // If invalidated, then we just toggle existing pragma enable and disable directives before and start of the line.
                        // If not, then we just remove the existing pragma trivia surrounding the line.
                        toggle = await IsDiagnosticSuppressedBeforeLeadingPragmaAsync(indexOfLeadingPragmaDisableToRemove, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Otherwise, just add a pragma enable before the start token and a pragma restore after it.
                        add = true;
                    }

                    Func<SyntaxToken, TextSpan, SyntaxToken> getNewStartToken = (startToken, currentDiagnosticSpan) => includeStartTokenChange ?
                        GetNewTokenWithModifiedPragma(startToken, currentDiagnosticSpan, add, toggle, indexOfLeadingPragmaDisableToRemove, isStartToken: true) :
                        startToken;

                    Func<SyntaxToken, TextSpan, SyntaxToken> getNewEndToken = (endToken, currentDiagnosticSpan) => includeEndTokenChange ?
                        GetNewTokenWithModifiedPragma(endToken, currentDiagnosticSpan, add, toggle, indexOfTrailingPragmaEnableToRemove, isStartToken: false) :
                        endToken;

                    return await PragmaHelpers.GetChangeDocumentWithPragmaAdjustedAsync(
                        _document,
                        _diagnostic.Location.SourceSpan,
                        _suppressionTargetInfo,
                        getNewStartToken,
                        getNewEndToken,
                        cancellationToken).ConfigureAwait(false);
                }

                private static SyntaxTriviaList GetTriviaListForSuppression(SyntaxToken token, bool isStartToken, AbstractSuppressionCodeFixProvider fixer)
                {
                    return isStartToken || fixer.IsEndOfFileToken(token) ?
                        token.LeadingTrivia :
                        token.TrailingTrivia;
                }

                private static SyntaxToken UpdateTriviaList(SyntaxToken token, bool isStartToken, SyntaxTriviaList triviaList, AbstractSuppressionCodeFixProvider fixer)
                {
                    return isStartToken || fixer.IsEndOfFileToken(token) ?
                        token.WithLeadingTrivia(triviaList) :
                        token.WithTrailingTrivia(triviaList);
                }

                private static bool CanRemovePragmaTrivia(SyntaxToken token, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer, bool isStartToken, out int indexOfTriviaToRemove)
                {
                    indexOfTriviaToRemove = -1;

                    var triviaList = GetTriviaListForSuppression(token, isStartToken, fixer);

                    var diagnosticSpan = diagnostic.Location.SourceSpan;
                    Func<SyntaxTrivia, bool> shouldIncludeTrivia = t => isStartToken ? t.FullSpan.End <= diagnosticSpan.Start : t.FullSpan.Start >= diagnosticSpan.End;
                    var filteredTriviaList = triviaList.Where(shouldIncludeTrivia);
                    if (isStartToken)
                    {
                        // Walk bottom up for leading trivia.
                        filteredTriviaList = filteredTriviaList.Reverse();
                    }

                    foreach (var trivia in filteredTriviaList)
                    {
                        bool isEnableDirective, hasMultipleIds;
                        if (fixer.IsAnyPragmaDirectiveForId(trivia, diagnostic.Id, out isEnableDirective, out hasMultipleIds))
                        {
                            if (hasMultipleIds)
                            {
                                // Handle only simple cases where we have a single pragma directive with single ID matching ours in the trivia.
                                return false;
                            }

                            // We want to look for leading disable directive and trailing enable directive.
                            if ((isStartToken && !isEnableDirective) ||
                                (!isStartToken && isEnableDirective))
                            {
                                indexOfTriviaToRemove = triviaList.IndexOf(trivia);
                                return true;
                            }

                            return false;
                        }
                    }

                    return false;
                }

                private SyntaxToken GetNewTokenWithModifiedPragma(SyntaxToken token, TextSpan currentDiagnosticSpan, bool add, bool toggle, int indexOfTriviaToRemoveOrToggle, bool isStartToken)
                {
                    return add ?
                        GetNewTokenWithAddedPragma(token, currentDiagnosticSpan, isStartToken) :
                        GetNewTokenWithRemovedOrToggledPragma(token, indexOfTriviaToRemoveOrToggle, isStartToken, toggle);
                }

                private SyntaxToken GetNewTokenWithAddedPragma(SyntaxToken token, TextSpan currentDiagnosticSpan, bool isStartToken)
                {
                    if (isStartToken)
                    {
                        return PragmaHelpers.GetNewStartTokenWithAddedPragma(token, currentDiagnosticSpan, _diagnostic, Fixer, FormatNode, isRemoveSuppression: true);
                    }
                    else
                    {
                        return PragmaHelpers.GetNewEndTokenWithAddedPragma(token, currentDiagnosticSpan, _diagnostic, Fixer, FormatNode, isRemoveSuppression: true);
                    }
                }

                private SyntaxToken GetNewTokenWithRemovedOrToggledPragma(SyntaxToken token, int indexOfTriviaToRemoveOrToggle, bool isStartToken, bool toggle)
                {
                    if (isStartToken)
                    {
                        return GetNewTokenWithPragmaUnsuppress(token, indexOfTriviaToRemoveOrToggle, _diagnostic, Fixer, isStartToken, toggle);
                    }
                    else
                    {
                        return GetNewTokenWithPragmaUnsuppress(token, indexOfTriviaToRemoveOrToggle, _diagnostic, Fixer, isStartToken, toggle);
                    }
                }

                private static SyntaxToken GetNewTokenWithPragmaUnsuppress(SyntaxToken token, int indexOfTriviaToRemoveOrToggle, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer, bool isStartToken, bool toggle)
                {
                    Contract.ThrowIfFalse(indexOfTriviaToRemoveOrToggle >= 0);

                    var triviaList = GetTriviaListForSuppression(token, isStartToken, fixer);

                    if (toggle)
                    {
                        var triviaToToggle = triviaList.ElementAt(indexOfTriviaToRemoveOrToggle);
                        Contract.ThrowIfFalse(triviaToToggle != default(SyntaxTrivia));
                        var toggledTrivia = fixer.TogglePragmaDirective(triviaToToggle);
                        triviaList = triviaList.Replace(triviaToToggle, toggledTrivia);
                    }
                    else
                    {
                        triviaList = triviaList.RemoveAt(indexOfTriviaToRemoveOrToggle);
                    }

                    return UpdateTriviaList(token, isStartToken, triviaList, fixer);
                }

                private async Task<bool> IsDiagnosticSuppressedBeforeLeadingPragmaAsync(int indexOfPragma, CancellationToken cancellationToken)
                {
                    var model = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var tree = model.SyntaxTree;

                    // get the warning state of this diagnostic ID at the start of the pragma
                    var trivia = _suppressionTargetInfo.StartToken.LeadingTrivia.ElementAt(indexOfPragma);
                    var spanToCheck = new TextSpan(
                        start: Math.Max(0, trivia.Span.Start - 1),
                        length: 1);
                    var locationToCheck = Location.Create(tree, spanToCheck);
                    var dummyDiagnosticWithLocationToCheck = Diagnostic.Create(_diagnostic.Descriptor, locationToCheck);
                    var effectiveDiagnostic = CompilationWithAnalyzers.GetEffectiveDiagnostics(new[] { dummyDiagnosticWithLocationToCheck }, model.Compilation).FirstOrDefault();
                    return effectiveDiagnostic == null || effectiveDiagnostic.IsSuppressed;
                }

                public SyntaxToken StartToken_TestOnly => _suppressionTargetInfo.StartToken;
                public SyntaxToken EndToken_TestOnly => _suppressionTargetInfo.EndToken;

                private SyntaxNode FormatNode(SyntaxNode node)
                {
                    return Formatter.Format(node, _document.Project.Solution.Workspace);
                }
            }
        }
    }
}
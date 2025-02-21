// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal class CopilotGenerateDocumentationCommentProvider : SuggestionProviderBase
    {
        private SuggestionManagerBase? _suggestionManager;
        private readonly ICopilotCodeAnalysisService _copilotService;

        public readonly IThreadingContext ThreadingContext;

        public bool Enabled => _enabled && (_suggestionManager != null);
        private bool _enabled = true;

        public CopilotGenerateDocumentationCommentProvider(IThreadingContext threadingContext, ICopilotCodeAnalysisService copilotService)
        {
            _copilotService = copilotService;
            ThreadingContext = threadingContext;
        }

        public async Task InitializeAsync(ITextView textView, SuggestionServiceBase suggestionServiceBase, CancellationToken cancellationToken)
        {
            _suggestionManager ??= await suggestionServiceBase.TryRegisterProviderAsync(this, textView, "AmbientAIDocumentationComments", cancellationToken).ConfigureAwait(false);
        }

        public async Task GenerateDocumentationProposalAsync(DocumentationCommentSnippet snippet,
        ITextSnapshot oldSnapshot, VirtualSnapshotPoint oldCaret, CancellationToken cancellationToken)
        {
            await Task.Yield().ConfigureAwait(false);

            if (!Enabled)
            {
                return;
            }

            var snippetProposal = GetSnippetProposal(snippet.SnippetText, snippet.MemberNode, snippet.Position, snippet.CaretOffset);

            if (snippetProposal is null)
            {
                return;
            }

            // Do not do IntelliCode line completions if we're about to generate a documentation comment
            // so that won't have interfering grey text.
            var intellicodeLineCompletionsDisposable = await _suggestionManager!.DisableProviderAsync(SuggestionServiceNames.IntelliCodeLineCompletions, cancellationToken).ConfigureAwait(false);

            var proposalEdits = await GetProposedEditsAsync(snippetProposal, _copilotService, oldSnapshot, snippet.IndentText, cancellationToken).ConfigureAwait(false);

            var proposal = Proposal.TryCreateProposal(null, proposalEdits, oldCaret, flags: ProposalFlags.SingleTabToAccept);

            if (proposal is null)
            {
                await intellicodeLineCompletionsDisposable.DisposeAsync().ConfigureAwait(false);
                return;
            }

            var suggestion = new DocumentationCommentSuggestion(this, proposal, _suggestionManager, intellicodeLineCompletionsDisposable);
            await suggestion.TryDisplaySuggestionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Traverses the documentation comment shell and retrieves the pieces that are needed to generate the documentation comment.
        /// </summary>
        private static DocumentationCommentProposal? GetSnippetProposal(string comments, SyntaxNode? memberNode, int? position, int caret)
        {
            if (memberNode is null)
            {
                return null;
            }

            if (position is null)
            {
                return null;
            }

            var startIndex = position.Value;
            var proposedEdits = ArrayBuilder<DocumentationCommentProposedEdit>.GetInstance();
            var index = 0;

            var summaryStartTag = comments.IndexOf("<summary>", index, StringComparison.Ordinal);
            var summaryEndTag = comments.IndexOf("</summary>", index, StringComparison.Ordinal);
            if (summaryEndTag != -1 && summaryStartTag != -1)
            {
                proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(caret + startIndex, 0), null, DocumentationCommentTagType.Summary));
            }

            while (true)
            {
                var typeParamEndTag = comments.IndexOf("</typeparam>", index, StringComparison.Ordinal);
                var typeParamStartTag = comments.IndexOf("<typeparam name=\"", index, StringComparison.Ordinal);

                if (typeParamStartTag == -1 || typeParamEndTag == -1)
                {
                    break;
                }

                var typeParamNameStart = typeParamStartTag + "<typeparam name=\"".Length;
                var typeParamNameEnd = comments.IndexOf("\">", typeParamNameStart, StringComparison.Ordinal);
                if (typeParamNameEnd != -1)
                {
                    var parameterName = comments.Substring(typeParamNameStart, typeParamNameEnd - typeParamNameStart);
                    proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(typeParamEndTag + startIndex, 0), parameterName, DocumentationCommentTagType.TypeParam));
                }

                index = typeParamEndTag + "</typeparam>".Length;
            }

            while (true)
            {
                var paramEndTag = comments.IndexOf("</param>", index, StringComparison.Ordinal);
                var paramStartTag = comments.IndexOf("<param name=\"", index, StringComparison.Ordinal);

                if (paramStartTag == -1 || paramEndTag == -1)
                {
                    break;
                }

                var paramNameStart = paramStartTag + "<param name=\"".Length;
                var paramNameEnd = comments.IndexOf("\">", paramNameStart, StringComparison.Ordinal);
                if (paramNameEnd != -1)
                {
                    var parameterName = comments.Substring(paramNameStart, paramNameEnd - paramNameStart);
                    proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(paramEndTag + startIndex, 0), parameterName, DocumentationCommentTagType.Param));
                }

                index = paramEndTag + "</param>".Length;
            }

            var returnsEndTag = comments.IndexOf("</returns>", index, StringComparison.Ordinal);
            if (returnsEndTag != -1)
            {
                proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(returnsEndTag + startIndex, 0), null, DocumentationCommentTagType.Returns));
            }

            while (true)
            {
                var exceptionEndTag = comments.IndexOf("</exception>", index, StringComparison.Ordinal);
                var exceptionStartTag = comments.IndexOf("<exception cref=\"", index, StringComparison.Ordinal);

                if (exceptionEndTag == -1 || exceptionStartTag == -1)
                {
                    break;
                }

                var exceptionNameStart = exceptionStartTag + "<exception cref=\"".Length;
                var exceptionNameEnd = comments.IndexOf("\">", exceptionNameStart, StringComparison.Ordinal);
                if (exceptionNameEnd != -1)
                {
                    var exceptionName = comments.Substring(exceptionNameStart, exceptionNameEnd - exceptionNameStart);
                    proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(exceptionEndTag + startIndex, 0), exceptionName, DocumentationCommentTagType.Exception));
                }

                index = exceptionEndTag + "</exception>".Length;
            }

            return new DocumentationCommentProposal(memberNode.ToFullString(), proposedEdits.ToImmutableArray());
        }

        /// <summary>
        /// Calls into the copilot service to get the pieces for the documentation comment.
        /// </summary>
        private static async Task<IReadOnlyList<ProposedEdit>> GetProposedEditsAsync(
            DocumentationCommentProposal proposal, ICopilotCodeAnalysisService copilotService,
            ITextSnapshot oldSnapshot, string? indentText, CancellationToken cancellationToken)
        {
            var list = new List<ProposedEdit>();
            var (documentationCommentDictionary, isQuotaExceeded) = await copilotService.GetDocumentationCommentAsync(proposal, cancellationToken).ConfigureAwait(false);

            // Quietly fail if the quota has been exceeded.
            if (isQuotaExceeded)
            {
                return list;
            }

            if (documentationCommentDictionary is null)
            {
                return list;
            }

            if (documentationCommentDictionary.Count == 0)
            {
                return list;
            }

            foreach (var edit in proposal.ProposedEdits)
            {
                string? copilotStatement = null;
                var textSpan = edit.SpanToReplace;

                string? symbolKey = null;

                if (edit.SymbolName is not null)
                {
                    symbolKey = edit.TagType.ToString() + "- " + edit.SymbolName;
                }

                if (edit.TagType == DocumentationCommentTagType.Summary && documentationCommentDictionary.TryGetValue(DocumentationCommentTagType.Summary.ToString(), out var summary) && !string.IsNullOrEmpty(summary))
                {
                    copilotStatement = summary;
                }
                if (edit.TagType == DocumentationCommentTagType.TypeParam && documentationCommentDictionary.TryGetValue(symbolKey!, out var typeParam) && !string.IsNullOrEmpty(typeParam))
                {
                    copilotStatement = typeParam;
                }
                else if (edit.TagType == DocumentationCommentTagType.Param && documentationCommentDictionary.TryGetValue(symbolKey!, out var param) && !string.IsNullOrEmpty(param))
                {
                    copilotStatement = param;
                }
                else if (edit.TagType == DocumentationCommentTagType.Returns && documentationCommentDictionary.TryGetValue(DocumentationCommentTagType.Returns.ToString(), out var returns) && !string.IsNullOrEmpty(returns))
                {
                    copilotStatement = returns;
                }
                else if (edit.TagType == DocumentationCommentTagType.Exception && documentationCommentDictionary.TryGetValue(symbolKey!, out var exception) && !string.IsNullOrEmpty(exception))
                {
                    copilotStatement = exception;
                }

                var proposedEdit = new ProposedEdit(new SnapshotSpan(oldSnapshot, textSpan.Start, textSpan.Length),
                    AddNewLinesToCopilotText(copilotStatement!, indentText, characterLimit: 120));
                list.Add(proposedEdit);
            }

            return list;

            static string AddNewLinesToCopilotText(string copilotText, string? indentText, int characterLimit)
            {
                var builder = new StringBuilder();
                var words = copilotText.Split(' ');
                var currentLineLength = 0;
                characterLimit -= (indentText!.Length + "/// ".Length);
                foreach (var word in words)
                {
                    if (currentLineLength + word.Length >= characterLimit)
                    {
                        builder.AppendLine();
                        builder.Append(indentText);
                        builder.Append("/// ");
                        currentLineLength = 0;
                    }

                    if (currentLineLength > 0)
                    {
                        builder.Append(' ');
                        currentLineLength++;
                    }

                    builder.Append(word);
                    currentLineLength += word.Length;
                }

                return builder.ToString();
            }
        }

        public override Task EnabledAsync(CancellationToken cancel)
        {
            _enabled = true;
            return Task.CompletedTask;
        }

        public override Task DisabledAsync(CancellationToken cancel)
        {
            _enabled = false;
            return Task.CompletedTask;
        }
    }
}

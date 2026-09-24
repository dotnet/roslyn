// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

/// <summary>
/// Editor-agnostic core for Copilot-generated documentation comments: builds the proposal from the
/// documentation comment shell, calls the Copilot service, and produces the resulting edits. It carries no
/// dependency on the editor's suggestion/proposal types so it can be reused by both the grey-text suggestion
/// path and the InlinePrompt chip-accept path.
/// </summary>
internal static class CopilotDocumentationCommentGenerator
{
    /// <summary>
    /// Traverses the documentation comment shell and retrieves the pieces that are needed to generate the
    /// documentation comment.
    /// </summary>
    public static DocumentationCommentProposal? GetSnippetProposal(string comments, SyntaxNode memberNode, int? position, int caret)
    {
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
            proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(caret + startIndex, 0), symbolName: null, DocumentationCommentTagType.Summary));
        }

        // We may receive remarks from the model. In that case, we want to insert the remark tags and remark directly after the summary.
        proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(summaryEndTag + "</summary>".Length + startIndex, 0), symbolName: null, DocumentationCommentTagType.Remarks));

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
            proposedEdits.Add(new DocumentationCommentProposedEdit(new TextSpan(returnsEndTag + startIndex, 0), symbolName: null, DocumentationCommentTagType.Returns));
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

        return new DocumentationCommentProposal(memberNode.ToFullString(), proposedEdits.ToImmutableAndFree());
    }

    /// <summary>
    /// Calls into the Copilot service to get the pieces for the documentation comment and maps them onto the
    /// spans described by <paramref name="proposal"/>. Returns an empty array when the quota has been exceeded
    /// or the service returns no content.
    /// </summary>
    public static async Task<ImmutableArray<DocumentationCommentEdit>> GenerateEditsAsync(
        DocumentationCommentProposal proposal, ICopilotCodeAnalysisService copilotService,
        string? indentText, CancellationToken cancellationToken)
    {
        var (documentationCommentDictionary, isQuotaExceeded) = await copilotService.GetDocumentationCommentAsync(proposal, cancellationToken).ConfigureAwait(false);

        // Quietly fail if the quota has been exceeded.
        if (isQuotaExceeded)
        {
            return [];
        }

        if (documentationCommentDictionary is null || documentationCommentDictionary.Count == 0)
        {
            return [];
        }

        var edits = ArrayBuilder<DocumentationCommentEdit>.GetInstance();

        foreach (var edit in proposal.ProposedEdits)
        {
            string? symbolKey = null;

            if (edit.SymbolName is not null)
            {
                symbolKey = edit.TagType.ToString() + "-" + edit.SymbolName;
            }

            var copilotStatement = GetCopilotStatement(documentationCommentDictionary, edit, symbolKey);

            // Just skip this piece of the documentation comment if, for some reason, it is not found.
            if (copilotStatement is null)
            {
                continue;
            }

            var replacementText = AddNewLinesToCopilotText(copilotStatement, indentText, edit.TagType, characterLimit: 120);
            edits.Add(new DocumentationCommentEdit(edit.SpanToReplace, replacementText));
        }

        return edits.ToImmutableAndFree();

        static string? GetCopilotStatement(Dictionary<string, string> documentationCommentDictionary, DocumentationCommentProposedEdit edit, string? symbolKey)
        {
            if (edit.TagType == DocumentationCommentTagType.Summary && documentationCommentDictionary.TryGetValue(DocumentationCommentTagType.Summary.ToString(), out var summary) && !string.IsNullOrEmpty(summary))
            {
                return summary;
            }
            else if (edit.TagType == DocumentationCommentTagType.Remarks && documentationCommentDictionary.TryGetValue(DocumentationCommentTagType.Remarks.ToString(), out var remarks) && !string.IsNullOrEmpty(remarks))
            {
                return remarks;
            }
            else if (edit.TagType == DocumentationCommentTagType.TypeParam && documentationCommentDictionary.TryGetValue(symbolKey!, out var typeParam) && !string.IsNullOrEmpty(typeParam))
            {
                return typeParam;
            }
            else if (edit.TagType == DocumentationCommentTagType.Param && documentationCommentDictionary.TryGetValue(symbolKey!, out var param) && !string.IsNullOrEmpty(param))
            {
                return param;
            }
            else if (edit.TagType == DocumentationCommentTagType.Returns && documentationCommentDictionary.TryGetValue(DocumentationCommentTagType.Returns.ToString(), out var returns) && !string.IsNullOrEmpty(returns))
            {
                return returns;
            }
            else if (edit.TagType == DocumentationCommentTagType.Exception && documentationCommentDictionary.TryGetValue(symbolKey!, out var exception) && !string.IsNullOrEmpty(exception))
            {
                return exception;
            }

            return null;
        }

        static string AddNewLinesToCopilotText(string copilotText, string? indentText, DocumentationCommentTagType tagType, int characterLimit)
        {
            // Double check that the resultant from Copilot does not produce any strings containing new line characters.
            copilotText = Regex.Replace(copilotText, @"\r?\n", " ");
            var builder = new StringBuilder();
            copilotText = BuildCopilotTextForRemarks(copilotText, indentText, tagType, builder);

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

            static string BuildCopilotTextForRemarks(string copilotText, string? indentText, DocumentationCommentTagType tagType, StringBuilder builder)
            {
                if (tagType is DocumentationCommentTagType.Remarks)
                {
                    builder.AppendLine();
                    builder.Append(indentText);
                    builder.Append("/// <remarks>");
                    builder.Append(copilotText);
                    builder.Append("</remarks>");
                    copilotText = builder.ToString();
                    builder.Clear();
                }

                return copilotText;
            }
        }
    }
}

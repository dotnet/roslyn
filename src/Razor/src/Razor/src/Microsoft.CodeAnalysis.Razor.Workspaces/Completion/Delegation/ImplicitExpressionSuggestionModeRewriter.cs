// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Completion.Delegation;

/// <summary>
/// Manages completion behavior for C# completions at the top level of implicit Razor expressions.
/// </summary>
/// <remarks>
/// This rewriter handles two cases:
///
/// 1. **At the @ transition** (cursor immediately after <c>@</c>): clears all commit characters
///    and sets <c>IsIncomplete = true</c>. This prevents characters like <c>{</c> from committing a
///    completion item when the user intends <c>@{</c> (a code block). The <c>IsIncomplete</c> flag
///    ensures the client re-queries on the next keystroke, restoring normal commit behavior.
///
/// 2. **At the top level of the implicit expression** (e.g., <c>@h|</c>): clears false-positive
///    <see cref="Roslyn.LanguageServer.Protocol.VSInternalCompletionList.SuggestionMode"/>. In implicit
///    expressions, the Razor compiler generates C# like <c>__builder.AddContent(seq, h)</c>. Because
///    <c>RenderTreeBuilder.AddContent</c> has a <c>RenderFragment</c> (delegate) overload, Roslyn's
///    suggestion-mode completion provider detects a potential lambda context and enables suggestion mode.
///    This is a false positive—users almost always intend to reference an identifier, not start a lambda.
///
/// However, implicit expressions can contain nested method calls with balanced parentheses
/// (e.g., <c>@items.Where(x =&gt; x.Name)</c>), where suggestion mode from the inner method's
/// delegate parameter is legitimate. Case 2 only clears suggestion mode when the cursor is
/// at the top level of the implicit expression (not inside parentheses).
/// </remarks>
internal class ImplicitExpressionSuggestionModeRewriter : IDelegatedCSharpCompletionResponseRewriter
{
    public RazorVSInternalCompletionList Rewrite(
        RazorVSInternalCompletionList completionList,
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        Position projectedPosition,
        RazorCompletionOptions completionOptions)
    {
        // The cursor is positioned right after the last typed character.  For example, in @h|
        // the host document index points at the character after 'h', which is typically the
        // start of the next token (e.g., '<' in '</div>').  To find the token the user is
        // actually typing, we look one position back.
        if (hostDocumentIndex == 0)
        {
            return completionList;
        }

        var owner = codeDocument
            .GetRequiredSyntaxRoot()
            .FindInnermostNode(hostDocumentIndex - 1);

        // Check if cursor is immediately after the @ transition in an implicit expression.
        // FindInnermostNode at the @ position returns the CSharpTransitionSyntax whose parent is
        // the implicit expression. We clear commit characters to prevent characters like '{' from
        // committing (the user may intend @{ for a code block). We also mark the list incomplete
        // so the client re-queries on the next keystroke, restoring normal commit behavior.
        if (owner is CSharpTransitionSyntax { Parent: CSharpImplicitExpressionSyntax })
        {
            completionList.IsIncomplete = true;
            ClearCommitCharacters(completionList);
            return completionList;
        }

        if (!completionList.SuggestionMode)
        {
            return completionList;
        }

        var implicitExpression = owner is { Parent: CSharpCodeBlockSyntax { Parent: CSharpImplicitExpressionBodySyntax { Parent: CSharpImplicitExpressionSyntax expr } } }
            ? expr
            : null;

        if (implicitExpression is not null
            && !IsInsideParentheses(codeDocument.Source.Text, implicitExpression.SpanStart, hostDocumentIndex))
        {
            completionList.SuggestionMode = false;
        }

        return completionList;
    }

    /// <summary>
    /// Checks whether the position at <paramref name="end"/> is inside unbalanced parentheses
    /// relative to <paramref name="start"/>.
    /// </summary>
    /// <remarks>
    /// We use character scanning rather than Razor syntax tree inspection because the Razor parser
    /// treats all C# content in an implicit expression as a single flat <c>CSharpExpressionLiteralSyntax</c>
    /// token. Both <c>@h</c> and <c>@items.Where(x)</c> produce identical ancestor chains, so tree
    /// depth cannot distinguish top-level positions from those nested inside parentheses.
    /// </remarks>
    private static bool IsInsideParentheses(SourceText sourceText, int start, int end)
    {
        var depth = 0;

        for (var i = start; i < end; i++)
        {
            var ch = sourceText[i];
            if (ch is '(')
            {
                depth++;
            }
            else if (ch is ')' && depth > 0)
            {
                depth--;
            }
        }

        return depth > 0;
    }

    /// <summary>
    /// Clears all commit characters from the completion list to prevent unwanted commits
    /// at the @ transition position. This is necessary for VS Code (which ignores SuggestionMode)
    /// and also prevents commit characters from firing in VS IDE.
    /// </summary>
    private static void ClearCommitCharacters(RazorVSInternalCompletionList completionList)
    {
        completionList.CommitCharacters = null;

        if (completionList.ItemDefaults is { } itemDefaults)
        {
            itemDefaults.CommitCharacters = null;
        }

        foreach (var item in completionList.Items)
        {
            item.CommitCharacters = null;
            item.VsCommitCharacters = null;
        }
    }
}

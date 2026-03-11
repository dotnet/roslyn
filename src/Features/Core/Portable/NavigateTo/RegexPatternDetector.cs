// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Detects whether a NavigateTo search pattern contains regex syntax, and performs
/// regex-aware splitting of the pattern into container and name portions.
/// </summary>
internal static class RegexPatternDetector
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="pattern"/> contains any regex
    /// metacharacter that distinguishes it from a plain text search. The set includes:
    /// <c>| ( ) [ ] { } + ? * \ ^ $</c>.
    /// <para/>
    /// A bare <c>.</c> alone does NOT trigger regex mode — it is treated as a container/name
    /// separator by the existing NavigateTo logic. However, <c>.*</c>, <c>.+</c>, <c>\.}</c>,
    /// etc. do trigger it because <c>*</c>, <c>+</c>, <c>\</c> are in the metacharacter set.
    /// </summary>
    public static bool IsRegexPattern(string pattern)
    {
        foreach (var ch in pattern)
        {
            switch (ch)
            {
                case '|':
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '+':
                case '?':
                case '*':
                case '\\':
                case '^':
                case '$':
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Splits a regex pattern into container and name portions by finding the last unquantified
    /// <see cref="RegexWildcardNode"/> (bare <c>.</c>) in the top-level sequence of the parsed
    /// regex AST.
    /// <para/>
    /// A bare dot is structurally distinct from <c>\.</c> (an escape node) and <c>.*</c> / <c>.+</c>
    /// / <c>.?</c> (a wildcard wrapped in a quantifier). Using the parsed AST avoids ad-hoc
    /// lexical scanning and handles all edge cases (escapes, character classes, nested groups)
    /// correctly.
    /// <para/>
    /// We split on the <b>last</b> bare wildcard, consistent with how the existing
    /// <c>PatternMatcher.GetNameAndContainer</c> uses <c>LastIndexOf('.')</c>. This keeps
    /// the name portion minimal (matching <c>DeclaredSymbolInfo.Name</c>) and the container
    /// portion maximal (matching the fully-qualified container).
    /// </summary>
    /// <returns>
    /// A tuple of (container substring, name substring). If no split point is found, container
    /// is <see langword="null"/> and name is the full pattern.
    /// </returns>
    public static (string? container, string name) SplitOnContainerDot(string pattern)
    {
        var sequence = VirtualCharSequence.Create(0, pattern);
        var tree = RegexParser.TryParse(sequence, RegexOptions.None);
        if (tree is null || tree.Diagnostics.Length > 0)
            return (null, pattern);

        // The root expression is always an alternation node. If there's no top-level alternation
        // (single branch), drill into the sole sequence. Otherwise, no split — the dot would be
        // inside an alternation branch, not at the top level.
        var rootExpr = tree.Root.Expression;
        if (rootExpr is not RegexAlternationNode alternation || alternation.SequenceList.Length != 1)
            return (null, pattern);

        var topSequence = alternation.SequenceList[0];

        // Walk children right-to-left looking for an unquantified RegexWildcardNode.
        for (var i = topSequence.Children.Length - 1; i >= 0; i--)
        {
            if (topSequence.Children[i] is RegexWildcardNode wildcard)
            {
                // Found a bare dot. Split the original string at this position.
                var dotSpan = wildcard.DotToken.VirtualChars[0].Span;
                var containerEnd = dotSpan.Start;
                var nameStart = dotSpan.End;

                if (containerEnd == 0 || nameStart >= pattern.Length)
                    continue;

                return (pattern[..containerEnd], pattern[nameStart..]);
            }
        }

        return (null, pattern);
    }
}

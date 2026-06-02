// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;

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
    public static (string? container, string name) SplitOnContainerDot(string pattern, RegexTree tree)
    {
        // The Roslyn regex parser wraps the root in an alternation node even when there's no `|`.
        // We only split at the top-level sequence — a dot inside an alternation branch (e.g.
        // `Goo.Bar|Baz.Quux`) is ambiguous and doesn't make sense as a single container/name split.
        var rootExpr = tree.Root.Expression;
        if (rootExpr is not RegexAlternationNode { SequenceList: [var topSequence] })
            return (null, pattern);

        // Walk right-to-left to find the last bare dot. The direction is mostly arbitrary, but it
        // mirrors how qualified names work: for `A.B.C`, the last dot separates the container `A.B`
        // from the name `C`, which is consistent with how `PatternMatcher.GetNameAndContainer` uses
        // `LastIndexOf('.')`.
        //
        // A RegexWildcardNode that appears directly as a child of the top-level sequence (not
        // wrapped in a quantifier) represents a bare `.`. If it were quantified (e.g. `.*`), the
        // parser would wrap it in a quantifier node and it wouldn't appear directly as a
        // RegexWildcardNode child.
        for (var i = topSequence.Children.Length - 1; i >= 0; i--)
        {
            if (topSequence.Children[i] is RegexWildcardNode wildcard)
            {
                var dotSpan = wildcard.DotToken.VirtualChars[0].Span;
                var containerEnd = dotSpan.Start;
                var nameStart = dotSpan.End;

                // Skip dots at the very start or end — they can't form a valid container/name pair.
                if (containerEnd == 0 || nameStart >= pattern.Length)
                    continue;

                return (pattern[..containerEnd], pattern[nameStart..]);
            }
        }

        return (null, pattern);
    }
}

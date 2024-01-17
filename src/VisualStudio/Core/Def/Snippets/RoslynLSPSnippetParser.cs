// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

[Export]
[Shared]
internal class RoslynLSPSnippetParser
{
    /*
     * LSP's full snippet syntax defined in the following EBNF grammar.
     * -------------------------------------------------------------------------------------------------------------------------
     * any         ::= tabstop | placeholder | choice | variable | text
     * tabstop     ::= '$' int | '${' int '}'
     * placeholder ::= '${' int ':' any '}'
     * choice      ::= '${' int '|' text (',' text)* '|}'
     * variable    ::= '$' var | '${' var }'
     *                 | '${' var ':' any '}'
     *                 | '${' var '/' regex '/' (format | text)+ '/' options '}'
     * format      ::= '$' int | '${' int '}'
     *                 | '${' int ':' '/upcase' | '/downcase' | '/capitalize' '}'
     *                 | '${' int ':+' if '}'
     *                 | '${' int ':?' if ':' else '}'
     *                 | '${' int ':-' else '}' | '${' int ':' else '}'
     * regex       ::= Regular Expression value (ctor-string)
     * options     ::= Regular Expression option (ctor-options)
     * var         ::= [_a-zA-Z] [_a-zA-Z0-9]*
     * int         ::= [0-9]+
     * text        ::= .*
     * -------------------------------------------------------------------------------------------------------------------------
     * We don't currently support all of the above, the current parsers only supports text, tabstop and non-nested placeholders:
     * -------------------------------------------------------------------------------------------------------------------------
     * any         ::= tabstop | placeholder | text
     * tabstop     ::= '$' int | '${' int '}'
     * placeholder ::= '${' int ':' text '}'
     * int         ::= [0-9]+
     * text        ::= .*
     */

    private const string TabStopIndexGroupName = "tabStopIndex";
    private const string PlaceholderNameGroupName = "placeholderName";
    private const string TextGroupName = "text";
    private const string TabStopGroupName = "tabstop";
    private const string PlaceholderGroupName = "placeholder";
    private const string TextRegexPattern = "[^$]+";

    // tabstop     ::= '$' int | '${' int '}'
    private const string TabStopRegexPattern = $@"\$(?<{TabStopIndexGroupName}>[0-9]+)|\$\{{(?<{TabStopIndexGroupName}>[0-9]+)\}}";

    // placeholder ::= '${' int ':' text '}'
    private const string PlaceholderRegexPattern = $@"\$\{{(?<{TabStopIndexGroupName}>[0-9]+):(?<{PlaceholderNameGroupName}>.+?)\}}";
    private const string SnippetRegexPattern = $@"(?<{TabStopGroupName}>{TabStopRegexPattern})|(?<{PlaceholderGroupName}>{PlaceholderRegexPattern}|(?<{TextGroupName}>{TextRegexPattern}))";
    private static readonly Regex s_snippetRegex = new(SnippetRegexPattern, RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynLSPSnippetParser()
    {
    }

    internal bool TryParse(string input, [NotNullWhen(true)] out RoslynLSPSnippetList? snippet)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var matches = s_snippetRegex.Matches(input);
        var pieces = new List<RoslynLSPSnippetSyntax>();

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!match.Success)
            {
                continue;
            }

            if (TryConvertToText(match, out var textSyntax) && textSyntax is not null)
            {
                pieces.Add(textSyntax);
            }
            else if (TryConvertToTabStop(match, out var tabStopSyntax) && tabStopSyntax is not null)
            {
                pieces.Add(tabStopSyntax);
            }
            else if (TryConvertToPlaceholder(match, out var placeholderSyntax) && placeholderSyntax is not null)
            {
                pieces.Add(placeholderSyntax);
            }
            else
            {
                // Unknown match type, this is completely unexpected.
                Debug.Fail("Unexpected match type, this should never happen");
                snippet = null;
                return false;
            }
        }

        if (pieces.Count == 0)
        {
            snippet = null;
            return false;
        }

        if (IsInvalidSnippet(input, pieces))
        {
            // We failed to fully match the original input string meaning the provided input
            // is not a valid LSP snippet;
            snippet = null;
            return false;
        }

        snippet = new RoslynLSPSnippetList(input, pieces);
        return true;
    }

    // This method is coupled to the SnippetRegexPattern pattern above. Meaning, if the regex doesn't support a feature then we will deem
    // a snippet invalid. For instance, in its v1 form if you have a snippet: "Hello $*"
    // Normally $* would be treated as text because * does not associate with a variable; however, because our regex doesn't understand
    // variables yet a $* will not be capatured and will get interpreted as "invalid".
    private static bool IsInvalidSnippet(string input, IReadOnlyList<RoslynLSPSnippetSyntax> pieces)
    {
        var totalSyntaxLength = pieces.Sum(piece => piece.Length);
        if (input.Length != totalSyntaxLength)
        {
            return true;
        }

        var endLocations = pieces.OfType<RoslynLSPSnippetEndLocationSyntax>();
        if (endLocations.Count() > 1)
        {
            // Multiple end-locations specified. Invalid.
            return true;
        }

        // Valid!
        return false;
    }

    private static bool TryConvertToText(Match match, [NotNullWhen(true)] out RoslynLSPSnippetTextSyntax? textSyntax)
    {
        if (!match.Groups.TryGetValue(TextGroupName, out var textGroup) || textGroup == null || !textGroup.Success)
        {
            textSyntax = null;
            return false;
        }

        textSyntax = new RoslynLSPSnippetTextSyntax(textGroup.Value, match.Index, match.Value.Length);
        return true;
    }

    private static bool TryConvertToTabStop(Match match, [NotNullWhen(true)] out RoslynLSPSnippetSyntax? tabStopSyntax)
    {
        if (!match.Groups.TryGetValue(TabStopGroupName, out var tabStopGroup) || tabStopGroup == null || !tabStopGroup.Success)
        {
            tabStopSyntax = null;
            return false;
        }

        if (!TryExtractTabStopIndex(match, out var tabStopIndex))
        {
            tabStopSyntax = null;
            return false;
        }

        if (tabStopIndex == 0)
        {
            // $0
            tabStopSyntax = new RoslynLSPSnippetEndLocationSyntax(match.Index, match.Value.Length);
        }
        else
        {
            tabStopSyntax = new RoslynLSPSnippetTabStopSyntax(tabStopIndex, match.Index, match.Value.Length);
        }

        return true;
    }

    private static bool TryConvertToPlaceholder(Match match, [NotNullWhen(true)] out RoslynLSPSnippetSyntax? placeholderSyntax)
    {
        if (!match.Groups.TryGetValue(PlaceholderGroupName, out var placeholderGroup) || placeholderGroup == null || !placeholderGroup.Success)
        {
            placeholderSyntax = null;
            return false;
        }

        if (!TryExtractTabStopIndex(match, out var tabStopIndex))
        {
            placeholderSyntax = null;
            return false;
        }

        if (tabStopIndex == 0)
        {
            // Tab stop 0 is not valid in placeholders
            placeholderSyntax = null;
            return false;
        }

        if (!TryExtractPlaceholder(match, out var placeholder) || placeholder == null)
        {
            placeholderSyntax = null;
            return false;
        }

        placeholderSyntax = new RoslynLSPSnippetPlaceholderSyntax(tabStopIndex, placeholder ?? string.Empty, match.Index, match.Value.Length);
        return true;
    }

    private static bool TryExtractTabStopIndex(Match match, [NotNullWhen(true)] out int tabStopIndex)
    {
        if (!match.Groups.TryGetValue(TabStopIndexGroupName, out var tabStopIndexGroup) || tabStopIndexGroup == null || !tabStopIndexGroup.Success)
        {
            tabStopIndex = -1;
            return false;
        }

        if (!int.TryParse(match.Groups[TabStopIndexGroupName].Value, out tabStopIndex))
        {
            tabStopIndex = -1;
            return false;
        }

        return true;
    }

    private static bool TryExtractPlaceholder(Match match, [NotNullWhen(true)] out string? placeholder)
    {
        if (!match.Groups.TryGetValue(PlaceholderNameGroupName, out var placeholderGroup) || placeholderGroup == null || !placeholderGroup.Success)
        {
            placeholder = null;
            return false;
        }

        placeholder = placeholderGroup.Value;
        return true;
    }
}

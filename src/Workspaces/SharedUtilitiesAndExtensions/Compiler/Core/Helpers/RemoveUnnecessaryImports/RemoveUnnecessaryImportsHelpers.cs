// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Shared.Helpers.RemoveUnnecessaryImports;

internal static class RemoveUnnecessaryImportsHelpers
{
    public static SyntaxToken StripNewLines(ISyntaxFacts syntaxFacts, SyntaxToken token)
    {
        var trimmedLeadingTrivia = token.LeadingTrivia.SkipWhile(syntaxFacts.IsEndOfLineTrivia).ToList();

        // If the list ends with 3 newlines remove the last one until there's only 2 newlines to end the leading trivia.
        while (trimmedLeadingTrivia.Count >= 3 &&
               syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[^3]) &&
               syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[^2]) &&
               syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[^1]))
        {
            trimmedLeadingTrivia.RemoveAt(trimmedLeadingTrivia.Count - 1);
        }

        return token.WithLeadingTrivia(trimmedLeadingTrivia);
    }
}

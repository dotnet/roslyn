// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    internal static class EmbeddedLanguageUtilities
    {
        internal static void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents)
        {
            var triviaList = SyntaxFactory.TriviaList(
                SyntaxFactory.Comment($"/*{commentContents}*/"),
                SyntaxFactory.ElasticSpace);
            var newStringLiteral = stringLiteral.WithLeadingTrivia(
                stringLiteral.LeadingTrivia.AddRange(triviaList));
            var parent = stringLiteral.GetRequiredParent();
            editor.ReplaceNode(
               parent,
               parent.ReplaceToken(stringLiteral, newStringLiteral));
        }

        public static string EscapeText(string text, SyntaxToken token)
        {
            // This function is called when Completion needs to escape something its going to insert into the user's
            // string token.  This means that we only have to escape things that completion could insert.  In this case,
            // the only regex character that is relevant is the \ character, and it's only relevant if we insert into a
            // normal string and not a verbatim string.  There are no other regex characters that completion will
            // produce that need any escaping.
            return token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken && !token.IsVerbatimStringLiteral()
                ? text.Replace(@"\", @"\\")
                : text;
        }
    }
}

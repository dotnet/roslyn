// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editing;

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
            editor.ReplaceNode(stringLiteral.Parent, stringLiteral.Parent.ReplaceToken(stringLiteral, newStringLiteral));
        }
    }
}

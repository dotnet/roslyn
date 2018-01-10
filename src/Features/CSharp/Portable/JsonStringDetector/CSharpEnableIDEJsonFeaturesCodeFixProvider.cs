// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.JsonStringDetector;

namespace Microsoft.CodeAnalysis.CSharp.JsonStringDetector
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpEnableIDEJsonFeaturesCodeFixProvider)), Shared]
    internal class CSharpEnableIDEJsonFeaturesCodeFixProvider : AbstractEnableIDEJsonFeaturesCodeFixProvider
    {
        private static readonly List<SyntaxTrivia> s_commentTrivia = new List<SyntaxTrivia>
        {
            SyntaxFactory.Comment("/*lang=json*/"),
            SyntaxFactory.ElasticSpace
        };
        private static readonly List<SyntaxTrivia> s_strictCommentTrivia = new List<SyntaxTrivia>
        {
            SyntaxFactory.Comment("/*lang=json,strict*/"),
            SyntaxFactory.ElasticSpace
        };

        protected override void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, bool strict)
        {
            var newStringLiteral = stringLiteral.WithLeadingTrivia(
                stringLiteral.LeadingTrivia.AddRange(
                    strict ? s_strictCommentTrivia : s_commentTrivia));

            editor.ReplaceNode(stringLiteral.Parent, stringLiteral.Parent.ReplaceToken(stringLiteral, newStringLiteral));
        }
    }
}

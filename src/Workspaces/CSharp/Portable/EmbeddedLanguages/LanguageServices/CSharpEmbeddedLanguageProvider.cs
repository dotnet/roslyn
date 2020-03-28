// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices
{
    [ExportLanguageService(typeof(IEmbeddedLanguageProvider), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguageProvider : AbstractEmbeddedLanguageProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEmbeddedLanguageProvider()
            : base((int)SyntaxKind.StringLiteralToken,
                   CSharpSyntaxFacts.Instance,
                   CSharpSemanticFactsService.Instance,
                   CSharpVirtualCharService.Instance)
        {
        }

        internal override void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents)
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

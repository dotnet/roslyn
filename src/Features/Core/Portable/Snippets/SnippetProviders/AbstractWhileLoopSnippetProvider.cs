// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractWhileLoopSnippetProvider : AbstractConditionalBlockSnippetProvider
    {
        public override string Identifier => "while";

        public override string Description => FeaturesResources.while_loop;

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts) => syntaxFacts.IsWhileStatement;

        protected override TextChange GenerateSnippetTextChange(Document document, int position)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var whileStatement = generator.WhileStatement(generator.TrueLiteralExpression(), Array.Empty<SyntaxNode>());

            return new TextChange(TextSpan.FromBounds(position, position), whileStatement.ToFullString());
        }
    }
}

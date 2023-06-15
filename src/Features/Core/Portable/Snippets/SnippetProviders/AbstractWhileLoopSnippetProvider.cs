// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractWhileLoopSnippetProvider : AbstractConditionalBlockSnippetProvider
    {
        public override string Identifier => "while";

        public override string Description => FeaturesResources.while_loop;

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts) => syntaxFacts.IsWhileStatement;

        protected override SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, SyntaxNode? inlineExpression)
            => generator.WhileStatement(inlineExpression?.WithoutLeadingTrivia() ?? generator.TrueLiteralExpression(), Array.Empty<SyntaxNode>());
    }
}

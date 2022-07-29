// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpForEachLoopSnippetProvider : AbstractForEachLoopSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpForEachLoopSnippetProvider()
        {
        }

        /// <summary>
        /// Creates the foreach statement syntax.
        /// Must be done in language specific file since there is no generic way to generate the syntax.
        /// </summary>
        protected override SyntaxNode CreateForEachLoopStatementSyntax()
        {
            var varIdentifier = SyntaxFactory.IdentifierName("var");
            var collectionIdentifier = SyntaxFactory.IdentifierName("collection");
            var foreachLoopSyntax = SyntaxFactory.ForEachStatement(varIdentifier, "item", collectionIdentifier, SyntaxFactory.Block());

            return foreachLoopSyntax;
        }

        /// <summary>
        /// Goes through each piece of the foreach statement and extracts the identifiers
        /// as well as their locations to create SnippetPlaceholder's of each.
        /// </summary>
        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            GetPartsOfForEachStatement(node, out var identifier, out var expression, out var _1);
            arrayBuilder.Add(new SnippetPlaceholder(identifier.ToString(), ImmutableArray.Create(identifier.SpanStart)));
            arrayBuilder.Add(new SnippetPlaceholder(expression.ToString(), ImmutableArray.Create(expression.SpanStart)));

            return arrayBuilder.ToImmutableArray();

        }

        /// <summary>
        /// Gets the start of the BlockSyntax of the for statement
        /// to be able to insert the caret position at that location.
        /// </summary>
        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget)
        {
            GetPartsOfForEachStatement(caretTarget, out _, out _, out var statement);
            return statement.SpanStart + 1;
        }

        private static void GetPartsOfForEachStatement(SyntaxNode node, out SyntaxToken identifier, out SyntaxNode expression, out SyntaxNode statement)
        {
            var forEachStatement = (ForEachStatementSyntax)node;
            identifier = forEachStatement.Identifier;
            expression = forEachStatement.Expression;
            statement = forEachStatement.Statement;
        }
    }
}

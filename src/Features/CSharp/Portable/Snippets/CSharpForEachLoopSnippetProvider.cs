// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpForEachLoopSnippetProvider : AbstractForEachLoopSnippetProvider
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
        protected override async Task<SyntaxNode> CreateForEachLoopStatementSyntaxAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var varIdentifier = SyntaxFactory.IdentifierName("var");
            var enumerationSymbol = semanticModel.LookupSymbols(position).First(symbol => symbol.GetSymbolType() != null &&
                symbol.GetSymbolType()!.AllInterfaces.Any(namedSymbol => namedSymbol.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T or SpecialType.System_Collections_IEnumerable)); 
            var collectionIdentifier = enumeration is null
                ? SyntaxFactory.IdentifierName("collection")
                : SyntaxFactory.IdentifierName(enumeration.Name);
            var itemString = NameGenerator.GenerateUniqueName(
                "item", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);
            var foreachLoopSyntax = SyntaxFactory.ForEachStatement(varIdentifier, itemString, collectionIdentifier, SyntaxFactory.Block());

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

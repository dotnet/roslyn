// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal abstract class OperatorIndexerCompletionProviderBase : LSPCompletionProvider
    {

        // CompletionItems for indexers/operators should be sorted below other suggestions like methods or properties of the type.
        // Identifier (of methods or properties) can start with "A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl". https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers
        // Sorting is done via StringComparer.OrdinalIgnoreCase which compares the utf-16 bytes after converting to uppercase.
        // \ufffd http://www.fileformat.info/info/unicode/char/fffd/index.htm is the largest possible value for utf-16
        // and is also greater than surrogate pairs, if byte comparison is used. The "biggest" possible characters are 
        // \u3134a http://www.fileformat.info/info/unicode/char/3134a/index.htm surrogate pair "\ud884\udf4a" and
        // \uffdc http://www.fileformat.info/info/unicode/char/ffdc/index.htm (non-surrogate)
        protected const string SortingPrefix = "\uFFFD";

        internal override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet.Create('.');

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => text[insertedCharacterPosition] == '.';

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        protected static ImmutableDictionary<string, string> CreatePropertiesBag(params (string key, string value)[] properties)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            foreach (var (key, value) in properties)
            {
                builder.Add(key, value);
            }

            return builder.ToImmutable();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var position = context.Position;
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);
            if (!token.IsKind(SyntaxKind.DotToken))
            {
                return;
            }

            var expression = GetParentExpressionOfToken(token);
            if (expression is null)
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(expression.SpanStart, cancellationToken).ConfigureAwait(false);
            var container = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (container is null)
            {
                return;
            }

            var completionItems = GetCompletionItemsForTypeSymbol(container, semanticModel, position);
            context.AddItems(completionItems);
        }

        /// <summary>
        /// Returns the expression left to the passed dot <paramref name="token"/>.
        /// </summary>
        /// <example>
        /// Expression: a.b?.ccc.
        /// Token     :  ↑  ↑   ↑
        /// Returns   :  a  a.b .ccc
        /// </example>
        /// <param name="token">A dot token.</param>
        /// <returns>The expression left to the dot token or null.</returns>
        protected static ExpressionSyntax? GetParentExpressionOfToken(SyntaxToken token)
        {
            var syntaxNode = token.Parent;
            return syntaxNode switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                MemberBindingExpressionSyntax memberBinding => memberBinding.GetParentConditionalAccessExpression()?.Expression,
                _ => null,
            };
        }

        protected abstract IEnumerable<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, SemanticModel semanticModel, int position);

        protected static async Task<CompletionChange?> ReplaceDotAndTokenAfterWithTextAsync(Document document, CompletionItem item, string text, int positionOffset, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = SymbolCompletionItem.GetContextPosition(item);
            var tokenAtPosition = root.FindTokenOnLeftOfPosition(position);
            var token = tokenAtPosition.GetPreviousTokenIfTouchingWord(position);
            if (token.IsKind(SyntaxKind.DotToken))
            {
                var newPosition = token.Span.End + (text.Length - 1) + positionOffset;
                var replaceSpan = TextSpan.FromBounds(token.SpanStart, tokenAtPosition.Span.End);
                return CompletionChange.Create(new TextChange(replaceSpan, text), newPosition);
            }

            return null;
        }
    }
}

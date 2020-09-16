// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(OperatorIndexerCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    internal class OperatorIndexerCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OperatorIndexerCompletionProvider()
        {
        }

        private const string CompletionHandlerPropertyName = "CompletionHandler";
        private const string CompletionHandlerConversion = "Conversion";
        private const string CompletionHandlerIndexer = "Indexer";

        internal override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet.Create('.');

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            var ch = text[insertedCharacterPosition];
            return ch == '.';
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        private static ImmutableDictionary<string, string> CreateCompletionHandlerProperty(string operation)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder.Add(CompletionHandlerPropertyName, operation);
            return builder.ToImmutable();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var position = context.Position;
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            if (!(token.IsKind(SyntaxKind.DotToken) | token.IsKind(SyntaxKind.IdentifierToken)))
            {
                return;
            }

            var expression = GetExpressionOfInvocation(token);
            if (expression is null)
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var container = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (container is null)
            {
                return;
            }

            var allMembers = container.GetMembers();
            var allExplicitConversions = from m in allMembers.OfType<IMethodSymbol>()
                                         where
                                             m.IsConversion() && // MethodKind.Conversion
                                             m.Name == WellKnownMemberNames.ExplicitConversionName && // op_Explicit
                                             container.Equals(m.Parameters[0].Type) // Convert from container type to other type
                                         select SymbolCompletionItem.CreateWithSymbolId(
                                             displayText: $"({m.ReturnType.ToMinimalDisplayString(semanticModel, position)})", // The type to convert to
                                             symbols: ImmutableList.Create(m),
                                             rules: CompletionItemRules.Default,
                                             contextPosition: position,
                                             properties: CreateCompletionHandlerProperty(CompletionHandlerConversion));
            var indexers = from p in allMembers.OfType<IPropertySymbol>()
                           where p.IsIndexer
                           select SymbolCompletionItem.CreateWithSymbolId(
                               displayText: $"[{string.Join(", ", p.Parameters.Select(p => p.Type.ToMinimalDisplayString(semanticModel, position)))}]", // The type to convert to
                               symbols: ImmutableList.Create(p),
                               rules: CompletionItemRules.Default,
                               contextPosition: position,
                               properties: CreateCompletionHandlerProperty(CompletionHandlerIndexer));
            context.AddItems(allExplicitConversions.Union(indexers));
        }

        private static ExpressionSyntax? GetExpressionOfInvocation(SyntaxToken token)
        {
            var syntaxNode = token.IsKind(SyntaxKind.IdentifierToken)
                ? token.Parent?.Parent
                : token.Parent;
            return syntaxNode switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                MemberBindingExpressionSyntax { Parent: ConditionalAccessExpressionSyntax conditionalAccess } _ => conditionalAccess.Expression,
                _ => null,
            };
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            if (item.Properties.TryGetValue(CompletionHandlerPropertyName, out var value))
            {
                var completionChange = value switch
                {
                    CompletionHandlerConversion => await HandleConversionChangeAsync(document, item, cancellationToken).ConfigureAwait(false),
                    CompletionHandlerIndexer => await HandleIndexerChangeAsync(document, item, cancellationToken).ConfigureAwait(false),
                    _ => null,
                };
                if (completionChange is { })
                {
                    return completionChange;
                }
            }

            return await base.GetChangeAsync(document, item, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<CompletionChange?> HandleConversionChangeAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            var position = SymbolCompletionItem.GetContextPosition(item);
            var symbol = (IMethodSymbol)symbols.Single();
            var convertToType = symbol.ReturnType;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindTokenOnLeftOfPosition(position);
            var expression = GetExpressionOfInvocation(token);
            if (expression is { })
            {
                var typeName = convertToType.ToMinimalDisplayString(await document.ReuseExistingSpeculativeModelAsync(expression.SpanStart, cancellationToken).ConfigureAwait(false), expression.SpanStart);

                // expr. -> ((type)expr).
                var newNode =
                    (SyntaxNode)ParenthesizedExpression(
                        CastExpression(IdentifierName(typeName), expression.WithoutTrivia()));
                var syntaxToReplace = (SyntaxNode)expression;
                var identifier = token.Parent as IdentifierNameSyntax;
                if (identifier is { })
                {
                    // The user typed parts of the type name. This needs to be removed. We need to replace the expression and the identifier.
                    // expr.ty$$ -> ((type)expr).$$
                    syntaxToReplace = expression.GetCommonRoot(identifier);
                    newNode = syntaxToReplace
                        .ReplaceNodes(new[] { expression, identifier }, (n1, n2) => n1 switch
                        {
                            var n when ReferenceEquals(n, expression) => newNode,
                            var n when ReferenceEquals(n, identifier) => IdentifierName(""),
                            var n => n,
                        });
                }
                var newNodeText = newNode.ToFullString();
                var replaceSpan = syntaxToReplace.Span;
                var textChange = new TextChange(replaceSpan, newNodeText);
                var newPosition = position + (newNodeText.Length - replaceSpan.Length);
                return CompletionChange.Create(textChange, newPosition);
            }

            return null;
        }

        private static async Task<CompletionChange?> HandleIndexerChangeAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = SymbolCompletionItem.GetContextPosition(item);
            var token = root.FindTokenOnLeftOfPosition(position);
            if (token.IsKind(SyntaxKind.DotToken))
            {
                var newPosition = token.Span.End;
                return CompletionChange.Create(new TextChange(token.Span, "[]"), newPosition);
            }

            return null;
        }
    }
}

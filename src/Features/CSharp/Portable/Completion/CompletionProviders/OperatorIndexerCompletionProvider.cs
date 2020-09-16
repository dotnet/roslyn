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
    [ExportCompletionProvider(nameof(OperatorIndexerCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    [Shared]
    internal class OperatorIndexerCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OperatorIndexerCompletionProvider()
        {
        }

        private const string CompletionHandlerPropertyname = "CompletionHandler";
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
            builder.Add(CompletionHandlerPropertyname, operation);
            return builder.ToImmutable();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var position = context.Position;
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree is { })
            {
                var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
                if (token.IsKind(SyntaxKind.DotToken))
                {
                    var expression = GetExpressionOfInvocation(token);
                    if (expression is { })
                    {
                        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
                        var container = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                        if (container is { })
                        {
                            var allMembers = container.GetMembers();
                            var allExplicitConversions = from m in allMembers.OfType<IMethodSymbol>()
                                                         where
                                                             m.IsConversion() && // MethodKind.Conversion
                                                             m.Name == WellKnownMemberNames.ExplicitConversionName && // op_Explicit
                                                             m.Parameters[0].Type == container // Convert from container type to other type
                                                         select SymbolCompletionItem.CreateWithSymbolId(
                                                             displayText: $"({m.ReturnType.ToMinimalDisplayString(semanticModel, position)})", // The type to convert to
                                                             symbols: ImmutableList.Create(m),
                                                             rules: CompletionItemRules.Default,
                                                             contextPosition: position,
                                                             properties: CreateCompletionHandlerProperty(CompletionHandlerConversion));
                            var indexer = from p in allMembers.OfType<IPropertySymbol>()
                                          where p.IsIndexer
                                          select SymbolCompletionItem.CreateWithSymbolId(
                                              displayText: $"[{string.Join(", ", p.Parameters.Select(p => p.Type.ToMinimalDisplayString(semanticModel, position)))}]", // The type to convert to
                                              symbols: ImmutableList.Create(p),
                                              rules: CompletionItemRules.Default,
                                              contextPosition: position,
                                              properties: CreateCompletionHandlerProperty(CompletionHandlerIndexer));
                            context.AddItems(allExplicitConversions.Union(indexer));
                        }
                    }
                }
            }
        }

        private static ExpressionSyntax? GetExpressionOfInvocation(SyntaxToken dotToken)
        {
            Debug.Assert(dotToken.IsKind(SyntaxKind.DotToken));
            return dotToken.Parent switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                MemberBindingExpressionSyntax { Parent: ConditionalAccessExpressionSyntax conditionalAccess } _ => conditionalAccess.Expression,
                _ => null,
            };
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            if (item.Properties.TryGetValue(CompletionHandlerPropertyname, out var value))
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
            var symbol = symbols.Single() as IMethodSymbol;
            var convertToType = symbol!.ReturnType;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindTokenOnLeftOfPosition(position);
            var expression = GetExpressionOfInvocation(token);
            if (expression is { })
            {
                var typeName = convertToType.ToMinimalDisplayString(await document.ReuseExistingSpeculativeModelAsync(expression.SpanStart, cancellationToken).ConfigureAwait(false), expression.SpanStart);

                // expr. -> ((type)expr).
                var newNode =
                    ParenthesizedExpression(
                        CastExpression(IdentifierName(typeName), expression.WithoutTrivia()));
                var newNodeText = newNode.ToFullString();
                var expressionSpan = expression.Span;
                var textChange = new TextChange(expressionSpan, newNodeText);
                var newPosition = position + (newNodeText.Length - expressionSpan.Length);
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

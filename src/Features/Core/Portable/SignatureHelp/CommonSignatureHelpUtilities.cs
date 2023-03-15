// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal static class CommonSignatureHelpUtilities
    {
        internal static SignatureHelpState? GetSignatureHelpState<TArgumentList>(
            TArgumentList argumentList,
            int position,
            Func<TArgumentList, SyntaxToken> getOpenToken,
            Func<TArgumentList, SyntaxToken> getCloseToken,
            Func<TArgumentList, SyntaxNodeOrTokenList> getArgumentsWithSeparators,
            Func<TArgumentList, IEnumerable<string?>> getArgumentNames)
            where TArgumentList : SyntaxNode
        {
            if (TryGetCurrentArgumentIndex(argumentList, position, getOpenToken, getCloseToken, getArgumentsWithSeparators, out var argumentIndex))
            {
                var argumentNames = getArgumentNames(argumentList).ToImmutableArray();
                var argumentCount = argumentNames.Length;

                return new SignatureHelpState(
                    argumentIndex,
                    argumentCount,
                    argumentIndex < argumentCount ? argumentNames[argumentIndex] : null,
                    argumentNames.WhereNotNull().ToImmutableArray());
            }

            return null;
        }

        private static bool TryGetCurrentArgumentIndex<TArgumentList>(
            TArgumentList argumentList,
            int position,
            Func<TArgumentList, SyntaxToken> getOpenToken,
            Func<TArgumentList, SyntaxToken> getCloseToken,
            Func<TArgumentList, SyntaxNodeOrTokenList> getArgumentsWithSeparators,
            out int index) where TArgumentList : SyntaxNode
        {
            index = 0;
            if (position < getOpenToken(argumentList).Span.End)
                return false;

            var closeToken = getCloseToken(argumentList);
            if (!closeToken.IsMissing && position > closeToken.SpanStart)
                return false;

            foreach (var element in getArgumentsWithSeparators(argumentList))
            {
                if (element.IsToken && position >= element.Span.End)
                    index++;
            }

            return true;
        }

        internal static TextSpan GetSignatureHelpSpan<TArgumentList>(
            TArgumentList argumentList,
            Func<TArgumentList, SyntaxToken> getCloseToken)
            where TArgumentList : SyntaxNode
        {
            return GetSignatureHelpSpan(argumentList, argumentList.GetRequiredParent().SpanStart, getCloseToken);
        }

        internal static TextSpan GetSignatureHelpSpan<TArgumentList>(
            TArgumentList argumentList,
            int start,
            Func<TArgumentList, SyntaxToken> getCloseToken)
            where TArgumentList : SyntaxNode
        {
            var closeToken = getCloseToken(argumentList);
            if (closeToken.RawKind != 0 && !closeToken.IsMissing)
            {
                return TextSpan.FromBounds(start, closeToken.SpanStart);
            }

            // Missing close paren, the span is up to the start of the next token.
            var lastToken = argumentList.GetLastToken();
            var nextToken = lastToken.GetNextToken();
            if (nextToken.RawKind == 0)
            {
                nextToken = argumentList.AncestorsAndSelf().Last().GetLastToken(includeZeroWidth: true);
            }

            return TextSpan.FromBounds(start, nextToken.SpanStart);
        }

        internal static bool TryGetSyntax<TSyntax>(
            SyntaxNode root,
            int position,
            ISyntaxFactsService syntaxFacts,
            SignatureHelpTriggerReason triggerReason,
            Func<SyntaxToken, bool> isTriggerToken,
            Func<TSyntax, SyntaxToken, bool> isArgumentListToken,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out TSyntax? expression)
            where TSyntax : SyntaxNode
        {
            var token = root.FindTokenOnLeftOfPosition(position);
            if (triggerReason == SignatureHelpTriggerReason.TypeCharCommand)
            {
                if (isTriggerToken(token) &&
                    !syntaxFacts.IsInNonUserCode(root.SyntaxTree, position, cancellationToken))
                {
                    expression = token.GetAncestor<TSyntax>();
                    return expression != null;
                }
            }
            else if (triggerReason == SignatureHelpTriggerReason.InvokeSignatureHelpCommand)
            {
                expression = token.Parent?.GetAncestorsOrThis<TSyntax>().SkipWhile(syntax => !isArgumentListToken(syntax, token)).FirstOrDefault();
                return expression != null;
            }
            else if (triggerReason == SignatureHelpTriggerReason.RetriggerCommand)
            {
                if (!syntaxFacts.IsInNonUserCode(root.SyntaxTree, position, cancellationToken) ||
                    syntaxFacts.IsEntirelyWithinStringOrCharOrNumericLiteral(root.SyntaxTree, position, cancellationToken))
                {
                    expression = token.Parent?.AncestorsAndSelf()
                        .TakeWhile(n => !syntaxFacts.IsAnonymousFunctionExpression(n))
                        .OfType<TSyntax>()
                        .SkipWhile(syntax => !isArgumentListToken(syntax, token))
                        .FirstOrDefault();
                    return expression != null;
                }
            }

            expression = null;
            return false;
        }

        public static async Task<ImmutableArray<IMethodSymbol>> GetCollectionInitializerAddMethodsAsync(
            Document document, SyntaxNode initializer, SignatureHelpOptions options, CancellationToken cancellationToken)
        {
            if (initializer is not { Parent: not null })
                return default;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var ienumerableType = compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName!);
            if (ienumerableType == null)
                return default;

            // get the regular signature help items
            var parentOperation = semanticModel.GetOperation(initializer.Parent, cancellationToken) as IObjectOrCollectionInitializerOperation;
            var parentType = parentOperation?.Type;
            if (parentType == null)
                return default;

            if (!parentType.AllInterfaces.Contains(ienumerableType))
                return default;

            var position = initializer.SpanStart;
            var addSymbols = semanticModel.LookupSymbols(
                position, parentType, WellKnownMemberNames.CollectionInitializerAddMethodName, includeReducedExtensionMethods: true);

            var addMethods = addSymbols.OfType<IMethodSymbol>()
                                       .Where(m => m.Parameters.Length >= 1)
                                       .ToImmutableArray()
                                       .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                       .Sort(semanticModel, position);

            return addMethods;
        }
    }
}

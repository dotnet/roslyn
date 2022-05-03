// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    internal abstract class AbstractLanguageDetector<TOptions>
        where TOptions : struct, Enum
    {
        protected readonly EmbeddedLanguageInfo Info;

        private readonly EmbeddedLanguageDetector _detector;

        protected AbstractLanguageDetector(
            EmbeddedLanguageInfo info,
            ImmutableArray<string> languageIdentifiers)
        {
            Info = info;
            _detector = new EmbeddedLanguageDetector(info, languageIdentifiers);
        }

        /// <summary>
        /// Whether or not this is an argument to a well known api for this language (like Regex.Match or JToken.Parse).
        /// We light up support if we detect these, even if these APIs don't have the StringSyntaxAttribute attribute on
        /// them.  That way users can get a decent experience even on downlevel frameworks.
        /// </summary>
        protected abstract bool IsArgumentToWellKnownAPI(SyntaxToken token, SyntaxNode argumentNode, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options);

        /// <summary>
        /// Giving a sibling argument expression to the string literal, attempts to determine if they correspond to
        /// options for that language.  For example with <c>new Regex("[a-z]", RegexOptions.CaseInsensitive)</c> the 
        /// second argument's expression defines options that control how the literal is parsed.
        /// </summary>
        protected abstract bool TryGetOptions(SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out TOptions options);

        // Most embedded languages don't support being in an interpolated string text token.
        protected virtual bool IsEmbeddedLanguageInterpolatedStringTextToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
            => false;

        /// <summary>
        /// What options we should assume by default if we're matched up against a symbol that has a [StringSyntax]
        /// attribute on it.
        /// </summary>
        protected virtual TOptions GetStringSyntaxDefaultOptions()
            => default;

        public bool IsEmbeddedLanguageToken(
            SyntaxToken token,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out TOptions options)
        {
            options = default;

            // First check for the standard pattern of either a `// lang=...` comment or an API annotated with the
            // [StringSyntax] attribute.
            if (_detector.IsEmbeddedLanguageToken(token, semanticModel, cancellationToken, out _, out var stringOptions))
            {
                // If we got string-options back, then we were on a comment string (e.g. `// lang=regex,option1,option2`).
                // Attempt to convert the string options to actual options requested.
                if (stringOptions != null)
                    return EmbeddedLanguageCommentOptions<TOptions>.TryGetOptions(stringOptions, out options);

                // If we weren't on a comment, then we were on an API with StringSyntaxAttribute on it.  Attempt to grab
                // API specific options for the client to use.
                var syntaxFacts = Info.SyntaxFacts;
                if (syntaxFacts.IsLiteralExpression(token.Parent) &&
                    syntaxFacts.IsArgument(token.Parent.Parent))
                {
                    options = GetOptionsFromSiblingArgument(token.Parent.Parent, semanticModel, cancellationToken) ??
                              GetStringSyntaxDefaultOptions();
                }

                return true;
            }
            else
            {
                // We did not have a comment, and we didn't have a StringSyntax API.  See if this is an unannotated API
                // (e.g. Regex/Json prior to .net core).

                if (Info.IsAnyStringLiteral(token.RawKind))
                    return IsEmbeddedLanguageStringLiteralToken(token, semanticModel, cancellationToken, out options);

                if (token.RawKind == Info.SyntaxKinds.InterpolatedStringTextToken)
                {
                    options = default;
                    return IsEmbeddedLanguageInterpolatedStringTextToken(token, semanticModel, cancellationToken);
                }

                return false;
            }
        }

        private bool IsEmbeddedLanguageStringLiteralToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options)
        {
            options = default;
            var syntaxFacts = Info.SyntaxFacts;

            return syntaxFacts.IsLiteralExpression(token.Parent) &&
                   syntaxFacts.IsArgument(token.Parent.Parent) &&
                   IsArgumentToWellKnownAPI(token, token.Parent.Parent, semanticModel, cancellationToken, out options);
        }

        protected TOptions? GetOptionsFromSiblingArgument(
            SyntaxNode argument,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = Info.SyntaxFacts;
            var argumentList = argument.GetRequiredParent();
            var arguments = syntaxFacts.IsArgument(argument)
                ? syntaxFacts.GetArgumentsOfArgumentList(argumentList)
                : syntaxFacts.GetArgumentsOfAttributeArgumentList(argumentList);
            foreach (var siblingArg in arguments)
            {
                if (siblingArg != argument)
                {
                    var expr = syntaxFacts.IsArgument(argument)
                        ? syntaxFacts.GetExpressionOfArgument(siblingArg)
                        : syntaxFacts.GetExpressionOfAttributeArgument(siblingArg);
                    if (expr != null)
                    {
                        var exprType = semanticModel.GetTypeInfo(expr, cancellationToken);
                        if (exprType.Type != null &&
                            TryGetOptions(semanticModel, exprType.Type, expr, cancellationToken, out var options))
                        {
                            return options;
                        }
                    }
                }
            }

            return null;
        }

        protected static string? GetNameOfType(SyntaxNode? typeNode, ISyntaxFacts syntaxFacts)
        {
            if (syntaxFacts.IsQualifiedName(typeNode))
            {
                return GetNameOfType(syntaxFacts.GetRightSideOfDot(typeNode), syntaxFacts);
            }
            else if (syntaxFacts.IsIdentifierName(typeNode))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(typeNode).ValueText;
            }

            return null;
        }

        protected string? GetNameOfInvokedExpression(SyntaxNode invokedExpression)
        {
            var syntaxFacts = Info.SyntaxFacts;
            if (syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;
            }
            else if (syntaxFacts.IsIdentifierName(invokedExpression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;
            }

            return null;
        }
    }

    internal abstract class AbstractLanguageDetector<TOptions, TTree> :
        AbstractLanguageDetector<TOptions>
        where TOptions : struct, Enum
        where TTree : class
    {
        protected AbstractLanguageDetector(
            EmbeddedLanguageInfo info,
            ImmutableArray<string> languageIdentifiers)
            : base(info, languageIdentifiers)
        {
        }

        /// <summary>
        /// Tries to parse out an appropriate language tree given the characters in this string literal.
        /// </summary>
        protected abstract TTree? TryParse(VirtualCharSequence chars, TOptions options);

        /// <summary>
        /// Attempts to parse the string-literal-like <paramref name="token"/> into an embedded language tree.  The
        /// token must either be in a location semantically known to accept this language, or it must have an
        /// appropriate comment on it stating that it should be interpreted as this language.
        /// </summary>
        public TTree? TryParseString(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!this.IsEmbeddedLanguageToken(token, semanticModel, cancellationToken, out var options))
                return null;

            var chars = Info.VirtualCharService.TryConvertToVirtualChars(token);
            return TryParse(chars, options);
        }
    }
}

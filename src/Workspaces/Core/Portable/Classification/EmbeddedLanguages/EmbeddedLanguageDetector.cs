// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Classification
{

    /// <summary>
    /// Helps match patterns of the form: language=name,option1,option2,option3
    /// 
    /// All matching is case insensitive, with spaces allowed between the punctuation. Option values will be or'ed
    /// together to produce final options value.  If an unknown option is encountered, processing will stop with
    /// whatever value has accumulated so far.
    /// 
    /// Option names are the values from the TOptions enum.
    /// </summary>
    internal struct LanguageCommentDetector<TOptions> where TOptions : struct, Enum
    {
        private static readonly Dictionary<string, TOptions> s_nameToOption =
            typeof(TOptions).GetTypeInfo().DeclaredFields
                .Where(f => f.FieldType == typeof(TOptions))
                .ToDictionary(f => f.Name, f => (TOptions)f.GetValue(null)!, StringComparer.OrdinalIgnoreCase);

        private readonly Regex _regex;

        public LanguageCommentDetector(params string[] languageNames)
        {
            var namePortion = string.Join("|", languageNames.Select(n => $"({Regex.Escape(n)})"));
            _regex = new Regex($@"^((//)|(')|(/\*))\s*lang(uage)?\s*=\s*({namePortion})\b((\s*,\s*)(?<option>[a-zA-Z]+))*",
                RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public bool TryMatch(string text, out TOptions options)
        {
            var match = _regex.Match(text);
            options = default;
            if (!match.Success)
                return false;

            var optionGroup = match.Groups["option"];
            foreach (Capture? capture in optionGroup.Captures)
            {
                if (!s_nameToOption.TryGetValue(capture!.Value, out var specificOption))
                {
                    // hit something we don't understand.  bail out.  that will help ensure
                    // users don't have weird behavior just because they misspelled something.
                    // instead, they will know they need to fix it up.
                    return false;
                }

                options = CombineOptions(options, specificOption);
            }

            return true;
        }

        private static TOptions CombineOptions(TOptions options, TOptions specificOption)
        {
            var int1 = (int)(object)options;
            var int2 = (int)(object)specificOption;
            return (TOptions)(object)(int1 | int2);
        }
    }

    internal sealed class EmbeddedLanguageDetector
    {
        protected readonly EmbeddedLanguageInfo Info;

        private readonly string _stringSyntaxAttributeName;
        private readonly LanguageCommentDetector<TOptions> _commentDetector;

        protected AbstractLanguageDetector(
            string stringSyntaxAttributeName,
            EmbeddedLanguageInfo info,
            LanguageCommentDetector<TOptions> commentDetector)
        {
            _stringSyntaxAttributeName = stringSyntaxAttributeName;
            Info = info;
            _commentDetector = commentDetector;
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

        private bool HasLanguageComment(
            SyntaxToken token, ISyntaxFacts syntaxFacts, out TOptions options)
        {
            if (HasLanguageComment(token.GetPreviousToken().TrailingTrivia, syntaxFacts, out options))
                return true;

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (HasLanguageComment(node.GetLeadingTrivia(), syntaxFacts, out options))
                    return true;

                // Stop walking up once we hit a statement.  We don't need/want statements higher up the parent chain to
                // have any impact on this token.
                if (syntaxFacts.IsStatement(node))
                    break;
            }

            options = default;
            return false;
        }

        private bool HasLanguageComment(
            SyntaxTriviaList list, ISyntaxFacts syntaxFacts, out TOptions options)
        {
            foreach (var trivia in list)
            {
                if (HasLanguageComment(trivia, syntaxFacts, out options))
                    return true;
            }

            options = default;
            return false;
        }

        private bool HasLanguageComment(
            SyntaxTrivia trivia, ISyntaxFacts syntaxFacts, out TOptions options)
        {
            if (syntaxFacts.IsRegularComment(trivia))
            {
                // Note: ToString on SyntaxTrivia is non-allocating.  It will just return the
                // underlying text that the trivia is already pointing to.
                var text = trivia.ToString();
                if (_commentDetector.TryMatch(text, out options))
                    return true;
            }

            options = default;
            return false;
        }

        public bool IsEmbeddedLanguageToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options)
        {
            options = default;

            if (Info.IsAnyStringLiteral(token.RawKind))
                return IsEmbeddedLanguageStringLiteralToken(token, semanticModel, cancellationToken, out options);

            if (token.RawKind == Info.SyntaxKinds.InterpolatedStringTextToken)
            {
                options = default;
                return IsEmbeddedLanguageInterpolatedStringTextToken(token, semanticModel, cancellationToken);
            }

            return false;
        }

        private bool IsEmbeddedLanguageStringLiteralToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options)
        {
            options = default;
            var syntaxFacts = Info.SyntaxFacts;
            if (!syntaxFacts.IsLiteralExpression(token.Parent))
                return false;

            if (HasLanguageComment(token, syntaxFacts, out options))
                return true;
            // If we're a string used in a collection initializer, treat this as a lang string if the collection itself
            // is properly annotated.  This is for APIs that do things like DateTime.ParseExact(..., string[] formats, ...);
            var container = TryFindContainer(token);
            if (container is null)
                return false;

            if (syntaxFacts.IsArgument(container.Parent))
            {
                var argument = container.Parent;
                if (IsArgumentToWellKnownAPI(token, argument, semanticModel, cancellationToken, out options))
                    return true;

                if (IsArgumentToParameterWithMatchingStringSyntaxAttribute(semanticModel, argument, cancellationToken, out options))
                    return true;
            }
            else if (syntaxFacts.IsAttributeArgument(container.Parent))
            {
                if (IsArgumentToAttributeParameterWithMatchingStringSyntaxAttribute(semanticModel, container.Parent, cancellationToken, out options))
                    return true;
            }
            else
            {
                var statement = container.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsStatement);
                if (syntaxFacts.IsSimpleAssignmentStatement(statement))
                {
                    syntaxFacts.GetPartsOfAssignmentStatement(statement, out var left, out var right);
                    if (container == right &&
                        IsFieldOrPropertyWithMatchingStringSyntaxAttribute(semanticModel, left, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private SyntaxNode? TryFindContainer(SyntaxToken token)
        {
            var syntaxFacts = Info.SyntaxFacts;
            var node = syntaxFacts.WalkUpParentheses(token.GetRequiredParent());

            // if we're inside some collection-like initializer, find the instance actually being created. 
            if (syntaxFacts.IsAnyInitializerExpression(node.Parent, out var instance))
                node = syntaxFacts.WalkUpParentheses(instance);

            return node;
        }

        private bool IsArgumentToAttributeParameterWithMatchingStringSyntaxAttribute(
            SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken, out TOptions options)
        {
            var parameter = Info.SemanticFacts.FindParameterForAttributeArgument(semanticModel, argument, cancellationToken);
            return IsParameterWithMatchingStringSyntaxAttribute(semanticModel, argument, parameter, cancellationToken, out options);
        }

        private bool IsArgumentToParameterWithMatchingStringSyntaxAttribute(SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken, out TOptions options)
        {
            var parameter = Info.SemanticFacts.FindParameterForArgument(semanticModel, argument, cancellationToken);
            return IsParameterWithMatchingStringSyntaxAttribute(semanticModel, argument, parameter, cancellationToken, out options);
        }

        private bool IsParameterWithMatchingStringSyntaxAttribute(SemanticModel semanticModel, SyntaxNode argument, IParameterSymbol parameter, CancellationToken cancellationToken, out TOptions options)
        {
            if (HasMatchingStringSyntaxAttribute(parameter))
            {
                options = GetOptionsFromSiblingArgument(argument, semanticModel, cancellationToken) ??
                          GetStringSyntaxDefaultOptions();
                return true;
            }

            options = default;
            return false;
        }

        private bool IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
            SemanticModel semanticModel, SyntaxNode left, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolInfo(left, cancellationToken).Symbol;
            return symbol is IFieldSymbol or IPropertySymbol &&
                HasMatchingStringSyntaxAttribute(symbol);
        }

        private bool HasMatchingStringSyntaxAttribute([NotNullWhen(true)] ISymbol? symbol)
        {
            if (symbol != null)
            {
                foreach (var attribute in symbol.GetAttributes())
                {
                    if (IsMatchingStringSyntaxAttribute(attribute))
                        return true;
                }
            }

            return false;
        }

        private bool IsMatchingStringSyntaxAttribute(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 0)
                return false;

            if (attribute.AttributeClass is not
                {
                    Name: "StringSyntaxAttribute",
                    ContainingNamespace:
                    {
                        Name: nameof(CodeAnalysis),
                        ContainingNamespace:
                        {
                            Name: nameof(Diagnostics),
                            ContainingNamespace:
                            {
                                Name: nameof(System),
                                ContainingNamespace.IsGlobalNamespace: true,
                            }
                        }
                    }
                })
            {
                return false;
            }

            var argument = attribute.ConstructorArguments[0];
            return argument.Kind == TypedConstantKind.Primitive && argument.Value is string argString && argString == _stringSyntaxAttributeName;
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

        protected string? GetNameOfType(SyntaxNode? typeNode, ISyntaxFacts syntaxFacts)
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

}

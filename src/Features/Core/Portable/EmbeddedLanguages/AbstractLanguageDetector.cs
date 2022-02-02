// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    internal abstract class AbstractLanguageDetector<TOptions, TTree>
        where TOptions : struct, Enum
        where TTree : class
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

        protected abstract bool IsArgumentToWellKnownAPI(SyntaxToken token, SyntaxNode argumentNode, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options);
        protected abstract TTree? TryParse(VirtualCharSequence chars, TOptions options);
        protected abstract bool TryGetOptions(SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out TOptions options);

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

        public bool IsEmbeddedLanguageString(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options)
        {
            options = default;

            var syntaxFacts = Info.SyntaxFacts;
            if (!syntaxFacts.IsStringLiteral(token))
                return false;

            if (!syntaxFacts.IsLiteralExpression(token.Parent))
                return false;

            if (HasLanguageComment(token, syntaxFacts, out options))
                return true;

            if (syntaxFacts.IsArgument(token.Parent.Parent))
            {
                var argument = token.Parent.Parent;
                if (IsArgumentToWellKnownAPI(token, argument, semanticModel, cancellationToken, out options))
                    return true;

                if (IsArgumentToParameterWithMatchingStringSyntaxAttribute(semanticModel, argument, cancellationToken, out options))
                    return true;
            }
            else
            {
                var parent = syntaxFacts.WalkUpParentheses(token.Parent);
                if (syntaxFacts.IsSimpleAssignmentStatement(parent.Parent))
                {
                    syntaxFacts.GetPartsOfAssignmentStatement(parent.Parent, out var left, out var right);
                    if (parent == right &&
                        IsFieldOrPropertyWithMatchingStringSyntaxAttribute(semanticModel, left, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsArgumentToParameterWithMatchingStringSyntaxAttribute(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken, out TOptions options)
        {
            var operation = semanticModel.GetOperation(argumentNode, cancellationToken);
            if (operation is IArgumentOperation { Parameter: { } parameter } &&
                HasMatchingStringSyntaxAttribute(parameter))
            {
                options = GetOptionsFromSiblingArgument(argumentNode, semanticModel, cancellationToken);
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

        private bool HasMatchingStringSyntaxAttribute(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (IsMatchingStringSyntaxAttribute(attribute))
                    return true;
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

        public TTree? TryParseString(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!this.IsEmbeddedLanguageString(token, semanticModel, cancellationToken, out var options))
                return null;

            var chars = Info.VirtualCharService.TryConvertToVirtualChars(token);
            return TryParse(chars, options);
        }

        protected TOptions GetOptionsFromSiblingArgument(SyntaxNode argumentNode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var syntaxFacts = Info.SyntaxFacts;
            var argumentList = argumentNode.GetRequiredParent();
            var arguments = syntaxFacts.GetArgumentsOfArgumentList(argumentList);
            foreach (var siblingArg in arguments)
            {
                if (siblingArg != argumentNode)
                {
                    var expr = syntaxFacts.GetExpressionOfArgument(siblingArg);
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

            return default;
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

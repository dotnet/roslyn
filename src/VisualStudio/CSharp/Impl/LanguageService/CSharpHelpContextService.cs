// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExportLanguageService(typeof(IHelpContextService), LanguageNames.CSharp), Shared]
    internal class CSharpHelpContextService : AbstractHelpContextService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpHelpContextService()
        {
        }

        public override string Language
        {
            get
            {
                return "csharp";
            }
        }

        public override string Product
        {
            get
            {
                return "csharp";
            }
        }

        private static string Keyword(string text)
            => text + "_CSharpKeyword";

        public override async Task<string> GetHelpTermAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // For now, find the token under the start of the selection.
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await syntaxTree.GetTouchingTokenAsync(span.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            if (IsValid(token, span))
            {
                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(span, cancellationToken).ConfigureAwait(false);

                var result = TryGetText(token, semanticModel, document, syntaxFacts, cancellationToken);
                if (string.IsNullOrEmpty(result))
                {
                    var previousToken = token.GetPreviousToken();
                    if (IsValid(previousToken, span))
                    {
                        result = TryGetText(previousToken, semanticModel, document, syntaxFacts, cancellationToken);
                    }
                }

                return result;
            }

            var trivia = root.FindTrivia(span.Start, findInsideTrivia: true);
            if (trivia.Span.IntersectsWith(span) && trivia.Kind() == SyntaxKind.PreprocessingMessageTrivia &&
                trivia.Token.GetAncestor<RegionDirectiveTriviaSyntax>() != null)
            {
                return "#region";
            }

            if (trivia.IsRegularOrDocComment())
            {
                // just find the first "word" that intersects with our position
                var text = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var start = span.Start;
                var end = span.Start;

                while (start > 0 && syntaxFacts.IsIdentifierPartCharacter(text[start - 1]))
                {
                    start--;
                }

                while (end < text.Length - 1 && syntaxFacts.IsIdentifierPartCharacter(text[end]))
                {
                    end++;
                }

                return text.GetSubText(TextSpan.FromBounds(start, end)).ToString();
            }

            return string.Empty;
        }

        private static bool IsValid(SyntaxToken token, TextSpan span)
        {
            // If the token doesn't actually intersect with our position, give up
            return token.Kind() == SyntaxKind.EndIfDirectiveTrivia || token.Span.IntersectsWith(span);
        }

        private string TryGetText(SyntaxToken token, SemanticModel semanticModel, Document document, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            if (TryGetTextForSpecialCharacters(token, out var text) ||
                TryGetTextForContextualKeyword(token, out text) ||
                TryGetTextForCombinationKeyword(token, syntaxFacts, out text) ||
                TryGetTextForKeyword(token, syntaxFacts, out text) ||
                TryGetTextForPreProcessor(token, syntaxFacts, out text) ||
                TryGetTextForOperator(token, document, out text) ||
                TryGetTextForSymbol(token, semanticModel, document, cancellationToken, out text))
            {
                return text;
            }

            return string.Empty;
        }

        private bool TryGetTextForSpecialCharacters(SyntaxToken token, out string text)
        {
            if (token.IsKind(SyntaxKind.InterpolatedStringStartToken) ||
                token.IsKind(SyntaxKind.InterpolatedStringEndToken) ||
                token.IsKind(SyntaxKind.InterpolatedStringTextToken))
            {
                text = "$_CSharpKeyword";
                return true;
            }

            if (token.IsVerbatimStringLiteral())
            {
                text = "@_CSharpKeyword";
                return true;
            }

            if (token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
            {
                text = "@$_CSharpKeyword";
                return true;
            }

            text = null;
            return false;
        }

        private bool TryGetTextForSymbol(SyntaxToken token, SemanticModel semanticModel, Document document, CancellationToken cancellationToken, out string text)
        {
            ISymbol symbol;
            if (token.Parent is TypeArgumentListSyntax)
            {
                var genericName = token.GetAncestor<GenericNameSyntax>();
                symbol = semanticModel.GetSymbolInfo(genericName, cancellationToken).Symbol ?? semanticModel.GetTypeInfo(genericName, cancellationToken).Type;
            }
            else if (token.Parent is NullableTypeSyntax && token.IsKind(SyntaxKind.QuestionToken))
            {
                text = "System.Nullable`1";
                return true;
            }
            else
            {
                symbol = semanticModel.GetSemanticInfo(token, document.Project.Solution.Workspace, cancellationToken)
                                      .GetAnySymbol(includeType: true);

                if (symbol == null)
                {
                    var bindableParent = document.GetLanguageService<ISyntaxFactsService>().TryGetBindableParent(token);
                    var overloads = bindableParent != null ? semanticModel.GetMemberGroup(bindableParent) : ImmutableArray<ISymbol>.Empty;
                    symbol = overloads.FirstOrDefault();
                }
            }

            // Local: return the name if it's the declaration, otherwise the type
            if (symbol is ILocalSymbol localSymbol && !symbol.DeclaringSyntaxReferences.Any(d => d.GetSyntax().DescendantTokens().Contains(token)))
            {
                symbol = localSymbol.Type;
            }

            // Range variable: use the type
            if (symbol is IRangeVariableSymbol)
            {
                var info = semanticModel.GetTypeInfo(token.Parent, cancellationToken);
                symbol = info.Type;
            }

            // Just use syntaxfacts for operators
            if (symbol is IMethodSymbol method && method.MethodKind == MethodKind.BuiltinOperator)
            {
                text = null;
                return false;
            }

            text = symbol != null ? FormatSymbol(symbol) : null;
            return symbol != null;
        }

        private static bool TryGetTextForOperator(SyntaxToken token, Document document, out string text)
        {
            if (token.IsKind(SyntaxKind.ExclamationToken) &&
                token.Parent.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                text = Keyword("nullForgiving");
                return true;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsOperator(token) || syntaxFacts.IsPredefinedOperator(token) || SyntaxFacts.IsAssignmentExpressionOperatorToken(token.Kind()))
            {
                text = Keyword(syntaxFacts.GetText(token.RawKind));
                return true;
            }

            if (token.IsKind(SyntaxKind.ColonColonToken))
            {
                text = "::_CSharpKeyword";
                return true;
            }

            if (token.IsKind(SyntaxKind.ColonToken) && token.Parent is NameColonSyntax)
            {
                text = "namedParameter_CSharpKeyword";
                return true;
            }

            if (token.IsKind(SyntaxKind.QuestionToken) && token.Parent is ConditionalExpressionSyntax)
            {
                text = "?_CSharpKeyword";
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsGreaterThanToken))
            {
                text = "=>_CSharpKeyword";
                return true;
            }

            text = null;
            return false;
        }

        private static bool TryGetTextForPreProcessor(SyntaxToken token, ISyntaxFactsService syntaxFacts, out string text)
        {
            if (syntaxFacts.IsPreprocessorKeyword(token))
            {
                text = "#" + token.Text;
                return true;
            }

            if (token.IsKind(SyntaxKind.EndOfDirectiveToken) && token.GetAncestor<RegionDirectiveTriviaSyntax>() != null)
            {
                text = "#region";
                return true;
            }

            text = null;
            return false;
        }

        private static bool TryGetTextForContextualKeyword(SyntaxToken token, out string text)
        {
            if (token.Text == "nameof")
            {
                text = Keyword("nameof");
                return true;
            }

            if (token.IsContextualKeyword())
            {
                switch (token.Kind())
                {
                    case SyntaxKind.PartialKeyword:
                        if (token.Parent.GetAncestorOrThis<MethodDeclarationSyntax>() != null)
                        {
                            text = "partialmethod_CSharpKeyword";
                            return true;
                        }
                        else if (token.Parent.GetAncestorOrThis<ClassDeclarationSyntax>() != null)
                        {
                            text = "partialtype_CSharpKeyword";
                            return true;
                        }

                        break;

                    case SyntaxKind.WhereKeyword:
                        if (token.Parent.GetAncestorOrThis<TypeParameterConstraintClauseSyntax>() != null)
                        {
                            text = "whereconstraint_CSharpKeyword";
                        }
                        else
                        {
                            text = "whereclause_CSharpKeyword";
                        }

                        return true;
                }
            }

            text = null;
            return false;
        }
        private static bool TryGetTextForCombinationKeyword(SyntaxToken token, ISyntaxFactsService syntaxFacts, out string text)
        {
            switch (token.Kind())
            {
                case SyntaxKind.PrivateKeyword when ModifiersContains(token, syntaxFacts, SyntaxKind.ProtectedKeyword):
                case SyntaxKind.ProtectedKeyword when ModifiersContains(token, syntaxFacts, SyntaxKind.PrivateKeyword):
                    text = "privateprotected_CSharpKeyword";
                    return true;

                case SyntaxKind.ProtectedKeyword when ModifiersContains(token, syntaxFacts, SyntaxKind.InternalKeyword):
                case SyntaxKind.InternalKeyword when ModifiersContains(token, syntaxFacts, SyntaxKind.ProtectedKeyword):
                    text = "protectedinternal_CSharpKeyword";
                    return true;
            }

            text = null;
            return false;

            static bool ModifiersContains(SyntaxToken token, ISyntaxFactsService syntaxFacts, SyntaxKind kind)
            {
                return syntaxFacts.GetModifiers(token.Parent).Any(t => t.IsKind(kind));
            }
        }

        private static bool TryGetTextForKeyword(SyntaxToken token, ISyntaxFactsService syntaxFacts, out string text)
        {
            if (token.IsKind(SyntaxKind.InKeyword))
            {
                if (token.GetAncestor<FromClauseSyntax>() != null)
                {
                    text = "from_CSharpKeyword";
                    return true;
                }

                if (token.GetAncestor<JoinClauseSyntax>() != null)
                {
                    text = "join_CSharpKeyword";
                    return true;
                }
            }

            if (token.IsKeyword())
            {
                text = Keyword(token.Text);
                return true;
            }

            if (token.ValueText == "var" && token.IsKind(SyntaxKind.IdentifierToken) &&
                token.Parent.Parent is VariableDeclarationSyntax declaration && token.Parent == declaration.Type)
            {
                text = "var_CSharpKeyword";
                return true;
            }

            if (syntaxFacts.IsTypeNamedDynamic(token, token.Parent))
            {
                text = "dynamic_CSharpKeyword";
                return true;
            }

            text = null;
            return false;
        }

        private static string FormatNamespaceOrTypeSymbol(INamespaceOrTypeSymbol symbol)
        {
            var displayString = symbol.ToDisplayString(TypeFormat);

            if (symbol is ITypeSymbol type && type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return "System.Nullable`1";
            }

            if (symbol.GetTypeArguments().Any())
            {
                return $"{displayString}`{symbol.GetTypeArguments().Length}";
            }

            return displayString;
        }

        public override string FormatSymbol(ISymbol symbol)
        {
            if (symbol is ITypeSymbol || symbol is INamespaceSymbol)
            {
                return FormatNamespaceOrTypeSymbol((INamespaceOrTypeSymbol)symbol);
            }

            if (symbol.MatchesKind(SymbolKind.Alias, SymbolKind.Local, SymbolKind.Parameter))
            {
                return FormatSymbol(symbol.GetSymbolType());
            }

            var containingType = FormatNamespaceOrTypeSymbol(symbol.ContainingType);
            var name = symbol.ToDisplayString(NameFormat);

            if (symbol.IsConstructor())
            {
                return $"{containingType}.#ctor";
            }

            if (symbol.GetTypeArguments().Any())
            {
                return $"{containingType}.{name}``{symbol.GetTypeArguments().Length}";
            }

            return $"{containingType}.{name}";
        }
    }
}

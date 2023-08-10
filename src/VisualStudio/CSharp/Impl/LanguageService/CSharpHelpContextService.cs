// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExportLanguageService(typeof(IHelpContextService), LanguageNames.CSharp), Shared]
    internal class CSharpHelpContextService : AbstractHelpContextService
    {
        // This redirects to https://docs.microsoft.com/visualstudio/ide/not-in-toc/default, indicating nothing is found.
        private const string NotFoundHelpTerm = "vs.texteditor";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpHelpContextService()
        {
        }

        public override string Language => "csharp";
        public override string Product => "csharp";

        private static string Keyword(string text)
            => text + "_CSharpKeyword";

        public override async Task<string> GetHelpTermAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            // For now, find the token under the start of the selection.
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await syntaxTree.GetTouchingTokenAsync(span.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            if (token.Span.IntersectsWith(span))
            {
                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(span, cancellationToken).ConfigureAwait(false);

                var result = TryGetText(token, semanticModel, document, cancellationToken);
                if (result is null)
                {
                    var previousToken = token.GetPreviousToken();
                    if (previousToken.Span.IntersectsWith(span))
                        result = TryGetText(previousToken, semanticModel, document, cancellationToken);
                }

                return result ?? NotFoundHelpTerm;
            }

            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
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

                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                while (start > 0 && syntaxFacts.IsIdentifierPartCharacter(text[start - 1]))
                    start--;

                while (end < text.Length - 1 && syntaxFacts.IsIdentifierPartCharacter(text[end]))
                    end++;

                return text.GetSubText(TextSpan.FromBounds(start, end)).ToString();
            }

            return NotFoundHelpTerm;
        }

        private string? TryGetText(SyntaxToken token, SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
        {
            if (TryGetTextForSpecialCharacters(token, out var text) ||
                TryGetTextForContextualKeyword(token, out text) ||
                TryGetTextForCombinationKeyword(token, out text) ||
                TryGetTextForPreProcessor(token, out text) ||
                TryGetTextForKeyword(token, out text) ||
                TryGetTextForOperator(token, document, out text) ||
                TryGetTextForSymbol(token, semanticModel, document, cancellationToken, out text))
            {
                return text;
            }

            return null;
        }

        private static bool TryGetTextForSpecialCharacters(SyntaxToken token, [NotNullWhen(true)] out string? text)
        {
            if (token.Kind()
                    is SyntaxKind.InterpolatedStringStartToken
                    or SyntaxKind.InterpolatedStringEndToken
                    or SyntaxKind.InterpolatedRawStringEndToken
                    or SyntaxKind.InterpolatedStringTextToken
                    or SyntaxKind.InterpolatedSingleLineRawStringStartToken
                    or SyntaxKind.InterpolatedMultiLineRawStringStartToken)
            {
                text = Keyword("$");
                return true;
            }

            if (token.IsVerbatimStringLiteral())
            {
                text = Keyword("@");
                return true;
            }

            if (token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
            {
                text = Keyword("@$");
                return true;
            }

            if (token.Kind()
                    is SyntaxKind.Utf8StringLiteralToken
                    or SyntaxKind.Utf8SingleLineRawStringLiteralToken
                    or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
            {
                text = Keyword("Utf8StringLiteral");
                return true;
            }

            if (token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken)
            {
                text = Keyword("RawStringLiteral");
                return true;
            }

            text = null;
            return false;
        }

        private bool TryGetTextForSymbol(
            SyntaxToken token, SemanticModel semanticModel, Document document, CancellationToken cancellationToken,
            [NotNullWhen(true)] out string? text)
        {
            ISymbol? symbol = null;
            if (token.Parent is TypeArgumentListSyntax)
            {
                var genericName = token.GetAncestor<GenericNameSyntax>();
                if (genericName != null)
                    symbol = semanticModel.GetSymbolInfo(genericName, cancellationToken).Symbol ?? semanticModel.GetTypeInfo(genericName, cancellationToken).Type;
            }
            else if (token.Parent is NullableTypeSyntax && token.IsKind(SyntaxKind.QuestionToken))
            {
                text = "System.Nullable`1";
                return true;
            }
            else
            {
                symbol = semanticModel.GetSemanticInfo(token, document.Project.Solution.Services, cancellationToken)
                                      .GetAnySymbol(includeType: true);

                if (symbol == null)
                {
                    var bindableParent = document.GetRequiredLanguageService<ISyntaxFactsService>().TryGetBindableParent(token);
                    var overloads = bindableParent != null ? semanticModel.GetMemberGroup(bindableParent) : ImmutableArray<ISymbol>.Empty;
                    symbol = overloads.FirstOrDefault();
                }
            }

            // Local: return the name if it's the declaration, otherwise the type
            if (symbol is ILocalSymbol localSymbol && !symbol.DeclaringSyntaxReferences.Any(static (d, token) => d.GetSyntax().DescendantTokens().Contains(token), token))
            {
                symbol = localSymbol.Type;
            }

            // Range variable: use the type
            if (symbol is IRangeVariableSymbol)
            {
                var info = semanticModel.GetTypeInfo(token.GetRequiredParent(), cancellationToken);
                symbol = info.Type;
            }

            // Just use syntaxfacts for operators
            if (symbol is IMethodSymbol method && method.MethodKind == MethodKind.BuiltinOperator)
            {
                text = null;
                return false;
            }

            if (symbol is IDiscardSymbol)
            {
                text = Keyword("discard");
                return true;
            }

            text = FormatSymbol(symbol);
            return text != null;
        }

        private static bool TryGetTextForOperator(SyntaxToken token, Document document, [NotNullWhen(true)] out string? text)
        {
            if (token.IsKind(SyntaxKind.ExclamationToken) &&
                token.Parent.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                text = Keyword("nullForgiving");
                return true;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsOperator(token))
            {
                text = Keyword(syntaxFacts.GetText(token.RawKind));
                return true;
            }

            if (token.IsKind(SyntaxKind.ColonColonToken))
            {
                text = Keyword("::");
                return true;
            }

            if (token.IsKind(SyntaxKind.ColonToken) && token.Parent is NameColonSyntax)
            {
                text = Keyword("namedParameter");
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsToken))
            {
                if (token.Parent.IsKind(SyntaxKind.EqualsValueClause))
                {
                    if (token.Parent.Parent.IsKind(SyntaxKind.Parameter))
                    {
                        text = Keyword("optionalParameter");
                        return true;
                    }
                    else if (token.Parent.Parent.IsKind(SyntaxKind.PropertyDeclaration))
                    {
                        text = Keyword("propertyInitializer");
                        return true;
                    }
                    else if (token.Parent.Parent.IsKind(SyntaxKind.EnumMemberDeclaration))
                    {
                        text = Keyword("enum");
                        return true;
                    }
                    else if (token.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    {
                        text = Keyword("=");
                        return true;
                    }
                }
                else if (token.Parent.IsKind(SyntaxKind.NameEquals))
                {
                    if (token.Parent.Parent.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator))
                    {
                        text = Keyword("anonymousObject");
                        return true;
                    }
                    else if (token.Parent.Parent.IsKind(SyntaxKind.UsingDirective))
                    {
                        text = Keyword("using");
                        return true;
                    }
                    else if (token.Parent.Parent.IsKind(SyntaxKind.AttributeArgument))
                    {
                        text = Keyword("attributeNamedArgument");
                        return true;
                    }
                }
                else if (token.Parent.IsKind(SyntaxKind.LetClause))
                {
                    text = Keyword("let");
                    return true;
                }
                else if (token.Parent is XmlAttributeSyntax)
                {
                    // redirects to https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags
                    text = "see";
                    return true;
                }

                // EqualsToken in assignment expression is handled by syntaxFacts.IsOperator call above.
                // Here we try to handle other contexts of EqualsToken.
                // If we hit this assert, there is a context of the EqualsToken that's not handled.
                // In this case, we currently fallback to https://docs.microsoft.com/dotnet/csharp/language-reference/operators/assignment-operator
                Debug.Fail("Falling back to F1 keyword for assignment token.");
                text = Keyword("=");
                return true;
            }

            if (token.Kind() is SyntaxKind.LessThanToken or SyntaxKind.GreaterThanToken)
            {
                if (token.Parent.IsKind(SyntaxKind.FunctionPointerParameterList))
                {
                    text = Keyword("functionPointer");
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.QuestionToken) && token.Parent is ConditionalExpressionSyntax)
            {
                text = Keyword("?");
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsGreaterThanToken))
            {
                text = Keyword("=>");
                return true;
            }

            if (token.Kind() is SyntaxKind.LessThanToken or SyntaxKind.GreaterThanToken &&
                token.Parent is (kind: SyntaxKind.TypeParameterList or SyntaxKind.TypeArgumentList))
            {
                text = Keyword("generics");
                return true;
            }

            text = null;
            return false;
        }

        private static bool TryGetTextForPreProcessor(SyntaxToken token, [NotNullWhen(true)] out string? text)
        {
            var syntaxFacts = CSharpSyntaxFacts.Instance;

            // Several keywords are both normal keywords and preprocessor keywords.  So only consider this token a
            // pp-keyword if we're actually in a directive.
            var directive = token.GetAncestor<DirectiveTriviaSyntax>();
            if (directive != null)
            {
                if (token.IsKind(SyntaxKind.DefaultKeyword) && token.Parent is LineDirectiveTriviaSyntax)
                {
                    text = Keyword("defaultline");
                    return true;
                }

                if (syntaxFacts.IsPreprocessorKeyword(token))
                {
                    text = $"#{token.Text}";
                    return true;
                }

                if (token.IsKind(SyntaxKind.EndOfDirectiveToken))
                {
                    text = $"#{directive.HashToken.GetNextToken(includeDirectives: true).Text}";
                    return true;
                }
            }

            text = null;
            return false;
        }

        private static bool TryGetTextForContextualKeyword(SyntaxToken token, [NotNullWhen(true)] out string? text)
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
                            text = Keyword("partialmethod");
                            return true;
                        }
                        else if (token.Parent.GetAncestorOrThis<TypeDeclarationSyntax>() != null)
                        {
                            text = Keyword("partialtype");
                            return true;
                        }

                        break;

                    case SyntaxKind.WhereKeyword:
                        text = token.Parent.GetAncestorOrThis<TypeParameterConstraintClauseSyntax>() != null
                            ? Keyword("whereconstraint")
                            : Keyword("whereclause");

                        return true;

                    case SyntaxKind.RequiredKeyword:
                        text = Keyword("required");
                        return true;
                }
            }
            else if (token.ValueText is "notnull" or "unmanaged")
            {
                if (token.Parent is IdentifierNameSyntax { Parent: TypeConstraintSyntax { Parent: TypeParameterConstraintClauseSyntax } })
                {
                    text = Keyword(token.ValueText);
                    return true;
                }
            }

            text = null;
            return false;
        }
        private static bool TryGetTextForCombinationKeyword(SyntaxToken token, [NotNullWhen(true)] out string? text)
        {
            switch (token.Kind())
            {
                case SyntaxKind.PrivateKeyword when ModifiersContains(token, SyntaxKind.ProtectedKeyword):
                case SyntaxKind.ProtectedKeyword when ModifiersContains(token, SyntaxKind.PrivateKeyword):
                    text = Keyword("privateprotected");
                    return true;

                case SyntaxKind.ProtectedKeyword when ModifiersContains(token, SyntaxKind.InternalKeyword):
                case SyntaxKind.InternalKeyword when ModifiersContains(token, SyntaxKind.ProtectedKeyword):
                    text = Keyword("protectedinternal");
                    return true;

                case SyntaxKind.UsingKeyword when token.Parent is UsingDirectiveSyntax:
                    text = token.GetNextToken().IsKind(SyntaxKind.StaticKeyword)
                        ? Keyword("using-static")
                        : Keyword("using");
                    return true;
                case SyntaxKind.StaticKeyword when token.Parent is UsingDirectiveSyntax:
                    text = Keyword("using-static");
                    return true;
                case SyntaxKind.ReturnKeyword when token.Parent.IsKind(SyntaxKind.YieldReturnStatement):
                case SyntaxKind.BreakKeyword when token.Parent.IsKind(SyntaxKind.YieldBreakStatement):
                    text = Keyword("yield");
                    return true;
            }

            text = null;
            return false;

            static bool ModifiersContains(SyntaxToken token, SyntaxKind kind)
            {
                return CSharpSyntaxFacts.Instance.GetModifiers(token.Parent).Any(t => t.IsKind(kind));
            }
        }

        private static bool TryGetTextForKeyword(SyntaxToken token, [NotNullWhen(true)] out string? text)
        {
            if (token.IsKind(SyntaxKind.InKeyword))
            {
                if (token.GetAncestor<FromClauseSyntax>() != null)
                {
                    text = Keyword("from");
                    return true;
                }

                if (token.GetAncestor<JoinClauseSyntax>() != null)
                {
                    text = Keyword("join");
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.DefaultKeyword))
            {
                if (token.Parent is DefaultConstraintSyntax)
                {
                    text = Keyword("defaultconstraint");
                    return true;
                }

                if (token.Parent is DefaultSwitchLabelSyntax or GotoStatementSyntax)
                {
                    text = Keyword("defaultcase");
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.ClassKeyword) && token.Parent is ClassOrStructConstraintSyntax)
            {
                text = Keyword("classconstraint");
                return true;
            }

            if (token.IsKind(SyntaxKind.StructKeyword) && token.Parent is ClassOrStructConstraintSyntax)
            {
                text = Keyword("structconstraint");
                return true;
            }

            if (token.IsKind(SyntaxKind.UsingKeyword) && token.Parent is UsingStatementSyntax or LocalDeclarationStatementSyntax)
            {
                text = Keyword("using-statement");
                return true;
            }

            if (token.IsKind(SyntaxKind.SwitchKeyword) && token.Parent is SwitchExpressionSyntax)
            {
                text = Keyword("switch-expression");
                return true;
            }

            if (token.IsKeyword())
            {
                text = Keyword(token.Text);
                return true;
            }

            if (token.ValueText == "var" && token.IsKind(SyntaxKind.IdentifierToken) &&
                token.Parent?.Parent is VariableDeclarationSyntax declaration && token.Parent == declaration.Type)
            {
                text = Keyword("var");
                return true;
            }

            if (token.IsTypeNamedDynamic())
            {
                text = Keyword("dynamic");
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

        public override string? FormatSymbol(ISymbol? symbol)
        {
            if (symbol == null)
                return null;

            if (symbol is ITypeSymbol or INamespaceSymbol)
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices;

internal sealed partial class CSharpSymbolDisplayService
{
    private sealed class SymbolDescriptionBuilder(
        SemanticModel semanticModel,
        int position,
        Host.LanguageServices languageServices,
        SymbolDescriptionOptions options,
        CancellationToken cancellationToken) : AbstractSymbolDescriptionBuilder(semanticModel, position, languageServices, options, cancellationToken)
    {
        private static readonly SymbolDisplayFormat s_minimallyQualifiedFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
            .AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef)
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName)
            .RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue)
            .WithKindOptions(SymbolDisplayKindOptions.None);

        private static readonly SymbolDisplayFormat s_minimallyQualifiedFormatWithConstants = s_minimallyQualifiedFormat
            .AddLocalOptions(SymbolDisplayLocalOptions.IncludeConstantValue)
            .AddMemberOptions(SymbolDisplayMemberOptions.IncludeConstantValue)
            .AddParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue);

        private static readonly SymbolDisplayFormat s_minimallyQualifiedFormatWithConstantsAndModifiers = s_minimallyQualifiedFormatWithConstants
            .AddMemberOptions(SymbolDisplayMemberOptions.IncludeModifiers);

        protected override SymbolDisplayFormat MinimallyQualifiedFormat
            => s_minimallyQualifiedFormat;

        protected override SymbolDisplayFormat MinimallyQualifiedFormatWithConstants
            => s_minimallyQualifiedFormatWithConstants;

        protected override SymbolDisplayFormat MinimallyQualifiedFormatWithConstantsAndModifiers
            => s_minimallyQualifiedFormatWithConstantsAndModifiers;

        protected override void AddDeprecatedPrefix()
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Punctuation("["),
                PlainText(CSharpFeaturesResources.deprecated),
                Punctuation("]"),
                Space());
        }

        protected override void AddExtensionPrefix()
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Punctuation("("),
                PlainText(CSharpFeaturesResources.extension),
                Punctuation(")"),
                Space());
        }

        protected override void AddAwaitablePrefix()
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Punctuation("("),
                PlainText(CSharpFeaturesResources.awaitable),
                Punctuation(")"),
                Space());
        }

        protected override void AddAwaitableExtensionPrefix()
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Punctuation("("),
                PlainText(CSharpFeaturesResources.awaitable_extension),
                Punctuation(")"),
                Space());
        }

        protected override void AddEnumUnderlyingTypeSeparator()
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Space(),
                Punctuation(":"),
                Space());
        }

        protected override Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
            ISymbol symbol)
        {
            // Actually check for C# symbol types here.  
            if (symbol is IParameterSymbol parameter)
            {
                return GetInitializerSourcePartsAsync(parameter);
            }
            else if (symbol is ILocalSymbol local)
            {
                return GetInitializerSourcePartsAsync(local);
            }
            else if (symbol is IFieldSymbol field)
            {
                return GetInitializerSourcePartsAsync(field);
            }

            return SpecializedTasks.EmptyImmutableArray<SymbolDisplayPart>();
        }

        protected override ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(ISymbol symbol, SemanticModel semanticModel, int position, SymbolDisplayFormat format)
        {
            var displayParts = CodeAnalysis.CSharp.SymbolDisplay.ToMinimalDisplayParts(symbol, semanticModel, position, format);
            return WrapConstraints(symbol, displayParts);
        }

        protected override ImmutableArray<SymbolDisplayPart> WrapConstraints(ISymbol symbol, ImmutableArray<SymbolDisplayPart> displayParts)
        {
            var typeParameter = symbol.GetTypeParameters();
            if (typeParameter.Length == 0)
                return displayParts;

            // For readability, we add every 'where' on its own line if we have two or more constraints to wrap.
            var wrappedConstraints = 0;
            using var _ = ArrayBuilder<SymbolDisplayPart>.GetInstance(displayParts.Length, out var builder);

            var displayPartsSpans = displayParts.AsSpan();
            while (displayPartsSpans is [var firstSpan, ..])
            {
                // Look for ` where T :` and add a line break before it.
                if (displayPartsSpans is [
                    { Kind: SymbolDisplayPartKind.Space },
                    { Kind: SymbolDisplayPartKind.Keyword } keyword,
                    { Kind: SymbolDisplayPartKind.Space },
                    { Kind: SymbolDisplayPartKind.TypeParameterName },
                    { Kind: SymbolDisplayPartKind.Space },
                    { Kind: SymbolDisplayPartKind.Punctuation } punctuation,
                    ..] &&
                    keyword.ToString() == "where" &&
                    punctuation.ToString() == ":")
                {
                    // Intentionally do not this initial space.  We want to replace it with a newline and 4 spaces instead.

                    builder.AddRange(LineBreak());
                    builder.AddRange(Space(4));
                    wrappedConstraints++;
                }
                else
                {
                    builder.Add(firstSpan);
                }

                displayPartsSpans = displayPartsSpans[1..];
            }

            if (wrappedConstraints < 2)
                return displayParts;

            return builder.ToImmutableAndClear();
        }

        protected override string? GetNavigationHint(ISymbol? symbol)
            => symbol == null ? null : CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(symbol, SymbolDisplayFormat.MinimallyQualifiedFormat);

        protected override void AddDocumentationContent(
            ISymbol symbol,
            DocumentationComment documentationComment,
            StructuralTypeDisplayInfo typeDisplayInfo)
        {
            var useReplacement = TryGetReplacementDocumentationComment(symbol, out var replacementDocumentationComment);
            if (useReplacement)
            {
                AddToGroup(SymbolDescriptionGroups.Documentation, replacementDocumentationComment);
                return;
            }

            base.AddDocumentationContent(symbol, documentationComment, typeDisplayInfo);
        }

        /// <summary>
        /// Returns whether documentation content displaying the glyph of 
        /// Unicode-escaped <see cref="System.Char"/> symbol should be added.
        /// If symbol is not a char, returns false.
        /// If char is not displayable (surrogate, control char, etc.),
        /// returns false.
        /// </summary>
        private bool TryGetReplacementDocumentationComment(ISymbol symbol, out IEnumerable<SymbolDisplayPart> additionalParts)
        {
            additionalParts = [];
            if (symbol is not INamedTypeSymbol { SpecialType: SpecialType.System_Char })
                return false;

            var root = SemanticModel.SyntaxTree.GetRoot(CancellationToken);
            var token = root.FindToken(Position);
            if (token.Value is not char)
                token = root.FindTokenOnLeftOfPosition(Position);

            if (token.ContainsDiagnostics)
                return false;

            if (token.Value is not char character)
                return false;

            if (!IsDisplayableInQuotes(character))
                return false;

            if (!token.Text.StartsWith("'\\u", System.StringComparison.Ordinal))
                return false;

            additionalParts = PlainText(string.Format(FeaturesResources.Represents_the_character_0_as_a_UTF_16_code_unit, character));
            return true;
        }

        /// <summary>
        /// Determines whether the specified character can be displayed in quotes,
        /// i.e., whether it has a glyph that is legible when surrounded by quotes.
        /// </summary>
        private static bool IsDisplayableInQuotes(char c)
        {
            var category = char.GetUnicodeCategory(c);
            return category switch
            {
                UnicodeCategory.UppercaseLetter => true,
                UnicodeCategory.LowercaseLetter => true,
                UnicodeCategory.TitlecaseLetter => true,
                UnicodeCategory.ModifierLetter => true,
                UnicodeCategory.OtherLetter => true,
                UnicodeCategory.NonSpacingMark => false, // does not render well when surrounded by single quotes
                UnicodeCategory.SpacingCombiningMark => true, // adds space, so still legible
                UnicodeCategory.EnclosingMark => false, // not generally displayable
                UnicodeCategory.DecimalDigitNumber => true,
                UnicodeCategory.LetterNumber => true,
                UnicodeCategory.OtherNumber => true,
                UnicodeCategory.SpaceSeparator => true,
                UnicodeCategory.LineSeparator => false, // renders as newline, looks awkward
                UnicodeCategory.ParagraphSeparator => false, // renders as newline, looks awkward
                UnicodeCategory.Control => false, // no glyph
                UnicodeCategory.Format => false, // no glyph
                UnicodeCategory.Surrogate => false, // no glyph
                UnicodeCategory.PrivateUse => false, // no glyph
                UnicodeCategory.ConnectorPunctuation => true,
                UnicodeCategory.DashPunctuation => true,
                UnicodeCategory.OpenPunctuation => true,
                UnicodeCategory.ClosePunctuation => true,
                UnicodeCategory.InitialQuotePunctuation => true,
                UnicodeCategory.FinalQuotePunctuation => true,
                UnicodeCategory.OtherPunctuation => true,
                UnicodeCategory.MathSymbol => true,
                UnicodeCategory.CurrencySymbol => true,
                UnicodeCategory.ModifierSymbol => true,
                UnicodeCategory.OtherSymbol => true,
                UnicodeCategory.OtherNotAssigned => false,
                _ => false // should never get here
            };
        }

        private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
            IFieldSymbol symbol)
        {
            EqualsValueClauseSyntax? initializer = null;

            var variableDeclarator = await GetFirstDeclarationAsync<VariableDeclaratorSyntax>(symbol).ConfigureAwait(false);
            if (variableDeclarator != null)
            {
                initializer = variableDeclarator.Initializer;
            }

            if (initializer == null)
            {
                var enumMemberDeclaration = await GetFirstDeclarationAsync<EnumMemberDeclarationSyntax>(symbol).ConfigureAwait(false);
                if (enumMemberDeclaration != null)
                {
                    initializer = enumMemberDeclaration.EqualsValue;
                }
            }

            if (initializer != null)
            {
                return await GetInitializerSourcePartsAsync(initializer).ConfigureAwait(false);
            }

            return [];
        }

        private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
            ILocalSymbol symbol)
        {
            var syntax = await GetFirstDeclarationAsync<VariableDeclaratorSyntax>(symbol).ConfigureAwait(false);
            if (syntax != null)
            {
                return await GetInitializerSourcePartsAsync(syntax.Initializer).ConfigureAwait(false);
            }

            return [];
        }

        private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
            IParameterSymbol symbol)
        {
            var syntax = await GetFirstDeclarationAsync<ParameterSyntax>(symbol).ConfigureAwait(false);
            if (syntax != null)
            {
                return await GetInitializerSourcePartsAsync(syntax.Default).ConfigureAwait(false);
            }

            return [];
        }

        private async Task<T?> GetFirstDeclarationAsync<T>(ISymbol symbol) where T : SyntaxNode
        {
            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                var syntax = await syntaxRef.GetSyntaxAsync(CancellationToken).ConfigureAwait(false);
                if (syntax is T tSyntax)
                {
                    return tSyntax;
                }
            }

            return null;
        }

        private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
            EqualsValueClauseSyntax? equalsValue)
        {
            if (equalsValue?.Value != null)
            {
                var semanticModel = GetSemanticModel(equalsValue.SyntaxTree);
                if (semanticModel != null)
                {
                    return await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                        LanguageServices, semanticModel, equalsValue.Value.Span,
                        Options.ClassificationOptions, cancellationToken: CancellationToken).ConfigureAwait(false);
                }
            }

            return [];
        }

        protected override void AddCaptures(SemanticModel semanticModel, ISymbol symbol, StructuralTypeDisplayInfo typeDisplayInfo)
        {
            if (symbol is IMethodSymbol { ContainingSymbol.Kind: SymbolKind.Method } method)
            {
                var syntax = method.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree == semanticModel.SyntaxTree)?.GetSyntax();
                if (syntax is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
                    AddCaptures(semanticModel, syntax, typeDisplayInfo);
            }
        }

        protected override string GetCommentText(SyntaxTrivia trivia)
            => trivia.ToFullString()["//".Length..];
    }
}

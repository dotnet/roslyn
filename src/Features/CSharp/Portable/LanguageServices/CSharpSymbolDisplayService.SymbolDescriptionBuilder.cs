// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        protected override void AddCaptures(ISymbol symbol)
        {
            if (symbol is IMethodSymbol { ContainingSymbol.Kind: SymbolKind.Method } method)
            {
                var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (syntax is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
                    AddCaptures(syntax);
            }
        }

        protected override string GetCommentText(SyntaxTrivia trivia)
            => trivia.ToFullString()["//".Length..];
    }
}

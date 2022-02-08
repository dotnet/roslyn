// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    internal partial class CSharpSymbolDisplayService
    {
        protected class SymbolDescriptionBuilder : AbstractSymbolDescriptionBuilder
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

            public SymbolDescriptionBuilder(
                SemanticModel semanticModel,
                int position,
                HostWorkspaceServices workspaceServices,
                IStructuralTypeDisplayService structuralTypeDisplayService,
                SymbolDescriptionOptions options,
                CancellationToken cancellationToken)
                : base(semanticModel, position, workspaceServices, structuralTypeDisplayService, options, cancellationToken)
            {
            }

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
                => CodeAnalysis.CSharp.SymbolDisplay.ToMinimalDisplayParts(symbol, semanticModel, position, format);

            protected override string GetNavigationHint(ISymbol symbol)
                => symbol == null ? null : CodeAnalysis.CSharp.SymbolDisplay.ToDisplayString(symbol, SymbolDisplayFormat.MinimallyQualifiedFormat);

            private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
                IFieldSymbol symbol)
            {
                EqualsValueClauseSyntax initializer = null;

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

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
                ILocalSymbol symbol)
            {
                var syntax = await GetFirstDeclarationAsync<VariableDeclaratorSyntax>(symbol).ConfigureAwait(false);
                if (syntax != null)
                {
                    return await GetInitializerSourcePartsAsync(syntax.Initializer).ConfigureAwait(false);
                }

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
                IParameterSymbol symbol)
            {
                var syntax = await GetFirstDeclarationAsync<ParameterSyntax>(symbol).ConfigureAwait(false);
                if (syntax != null)
                {
                    return await GetInitializerSourcePartsAsync(syntax.Default).ConfigureAwait(false);
                }

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            private async Task<T> GetFirstDeclarationAsync<T>(ISymbol symbol) where T : SyntaxNode
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
                EqualsValueClauseSyntax equalsValue)
            {
                if (equalsValue != null && equalsValue.Value != null)
                {
                    var semanticModel = GetSemanticModel(equalsValue.SyntaxTree);
                    if (semanticModel != null)
                    {
                        return await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                            Services, semanticModel, equalsValue.Value.Span,
                            Options.ClassificationOptions, cancellationToken: CancellationToken).ConfigureAwait(false);
                    }
                }

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            protected override void AddCaptures(ISymbol symbol)
            {
                if (symbol is IMethodSymbol method && method.ContainingSymbol.IsKind(SymbolKind.Method))
                {
                    var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                    if (syntax.IsKind(SyntaxKind.LocalFunctionStatement) || syntax is AnonymousFunctionExpressionSyntax)
                    {
                        AddCaptures(syntax);
                    }
                }
            }

            protected override SymbolDisplayFormat MinimallyQualifiedFormat => s_minimallyQualifiedFormat;

            protected override SymbolDisplayFormat MinimallyQualifiedFormatWithConstants => s_minimallyQualifiedFormatWithConstants;

            protected override SymbolDisplayFormat MinimallyQualifiedFormatWithConstantsAndModifiers => s_minimallyQualifiedFormatWithConstantsAndModifiers;
        }
    }
}

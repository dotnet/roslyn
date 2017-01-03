// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName)
                .RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue)
                .WithKindOptions(SymbolDisplayKindOptions.None);

            private static readonly SymbolDisplayFormat s_minimallyQualifiedFormatWithConstants = s_minimallyQualifiedFormat
                .AddLocalOptions(SymbolDisplayLocalOptions.IncludeConstantValue)
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeConstantValue)
                .AddParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue);

            public SymbolDescriptionBuilder(
                ISymbolDisplayService displayService,
                SemanticModel semanticModel,
                int position,
                Workspace workspace,
                IAnonymousTypeDisplayService anonymousTypeDisplayService,
                CancellationToken cancellationToken)
                : base(displayService, semanticModel, position, workspace, anonymousTypeDisplayService, cancellationToken)
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

            protected override Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
                ISymbol symbol)
            {
                // Actually check for C# symbol types here.  
                if (symbol is IParameterSymbol)
                {
                    return GetInitializerSourcePartsAsync((IParameterSymbol)symbol);
                }
                else if (symbol is ILocalSymbol)
                {
                    return GetInitializerSourcePartsAsync((ILocalSymbol)symbol);
                }
                else if (symbol is IFieldSymbol)
                {
                    return GetInitializerSourcePartsAsync((IFieldSymbol)symbol);
                }

                return SpecializedTasks.EmptyImmutableArray<SymbolDisplayPart>();
            }

            private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
                IFieldSymbol symbol)
            {
                EqualsValueClauseSyntax initializer = null;

                var variableDeclarator = await this.GetFirstDeclaration<VariableDeclaratorSyntax>(symbol).ConfigureAwait(false);
                if (variableDeclarator != null)
                {
                    initializer = variableDeclarator.Initializer;
                }

                if (initializer == null)
                {
                    var enumMemberDeclaration = await this.GetFirstDeclaration<EnumMemberDeclarationSyntax>(symbol).ConfigureAwait(false);
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
                var syntax = await this.GetFirstDeclaration<VariableDeclaratorSyntax>(symbol).ConfigureAwait(false);
                if (syntax != null)
                {
                    return await GetInitializerSourcePartsAsync(syntax.Initializer).ConfigureAwait(false);
                }

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            private async Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(
                IParameterSymbol symbol)
            {
                var syntax = await this.GetFirstDeclaration<ParameterSyntax>(symbol).ConfigureAwait(false);
                if (syntax != null)
                {
                    return await GetInitializerSourcePartsAsync(syntax.Default).ConfigureAwait(false);
                }

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            private async Task<T> GetFirstDeclaration<T>(ISymbol symbol) where T : SyntaxNode
            {
                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                {
                    var syntax = await syntaxRef.GetSyntaxAsync(this.CancellationToken).ConfigureAwait(false);
                    if (syntax is T)
                    {
                        return (T)syntax;
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
                            semanticModel, equalsValue.Value.Span,
                            this.Workspace, cancellationToken: this.CancellationToken).ConfigureAwait(false);
                    }
                }

                return ImmutableArray<SymbolDisplayPart>.Empty;
            }

            protected override void AddAwaitableUsageText(IMethodSymbol method, SemanticModel semanticModel, int position)
            {
                AddToGroup(SymbolDescriptionGroups.AwaitableUsageText,
                    method.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "x", semanticModel, position));
            }

            protected override SymbolDisplayFormat MinimallyQualifiedFormat
            {
                get { return s_minimallyQualifiedFormat; }
            }

            protected override SymbolDisplayFormat MinimallyQualifiedFormatWithConstants
            {
                get { return s_minimallyQualifiedFormatWithConstants; }
            }
        }
    }
}

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal partial class AbstractSymbolDisplayService
    {
        protected abstract partial class AbstractSymbolDescriptionBuilder
        {
            private static readonly SymbolDisplayFormat s_typeParameterOwnerFormat =
                new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions:
                        SymbolDisplayGenericsOptions.IncludeTypeParameters |
                        SymbolDisplayGenericsOptions.IncludeVariance |
                        SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                    parameterOptions: SymbolDisplayParameterOptions.None,
                    miscellaneousOptions:
                        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                        SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

            private static readonly SymbolDisplayFormat s_memberSignatureDisplayFormat =
                new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                    memberOptions:
                        SymbolDisplayMemberOptions.IncludeRef |
                        SymbolDisplayMemberOptions.IncludeType |
                        SymbolDisplayMemberOptions.IncludeParameters |
                        SymbolDisplayMemberOptions.IncludeContainingType,
                    kindOptions:
                        SymbolDisplayKindOptions.IncludeMemberKeyword,
                    propertyStyle:
                        SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                    parameterOptions:
                        SymbolDisplayParameterOptions.IncludeName |
                        SymbolDisplayParameterOptions.IncludeType |
                        SymbolDisplayParameterOptions.IncludeParamsRefOut |
                        SymbolDisplayParameterOptions.IncludeExtensionThis |
                        SymbolDisplayParameterOptions.IncludeDefaultValue |
                        SymbolDisplayParameterOptions.IncludeOptionalBrackets,
                    localOptions:
                        SymbolDisplayLocalOptions.IncludeRef |
                        SymbolDisplayLocalOptions.IncludeType,
                    miscellaneousOptions:
                        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                        SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

            private static readonly SymbolDisplayFormat s_descriptionStyle =
                new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                    parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                    kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword);

            private static readonly SymbolDisplayFormat s_globalNamespaceStyle =
                new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);

            private readonly ISymbolDisplayService _displayService;
            private readonly SemanticModel _semanticModel;
            private readonly int _position;
            private readonly IAnonymousTypeDisplayService _anonymousTypeDisplayService;
            private readonly Dictionary<SymbolDescriptionGroups, IList<SymbolDisplayPart>> _groupMap =
                new Dictionary<SymbolDescriptionGroups, IList<SymbolDisplayPart>>();
            protected readonly Workspace Workspace;
            protected readonly CancellationToken CancellationToken;

            protected AbstractSymbolDescriptionBuilder(
                ISymbolDisplayService displayService,
                SemanticModel semanticModel,
                int position,
                Workspace workspace,
                IAnonymousTypeDisplayService anonymousTypeDisplayService,
                CancellationToken cancellationToken)
            {
                _displayService = displayService;
                _anonymousTypeDisplayService = anonymousTypeDisplayService;
                this.Workspace = workspace;
                this.CancellationToken = cancellationToken;
                _semanticModel = semanticModel;
                _position = position;
            }

            protected abstract void AddExtensionPrefix();
            protected abstract void AddAwaitablePrefix();
            protected abstract void AddAwaitableExtensionPrefix();
            protected abstract void AddDeprecatedPrefix();
            protected abstract Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(ISymbol symbol);

            protected abstract SymbolDisplayFormat MinimallyQualifiedFormat { get; }
            protected abstract SymbolDisplayFormat MinimallyQualifiedFormatWithConstants { get; }

            protected void AddPrefixTextForAwaitKeyword()
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    PlainText(FeaturesResources.Awaited_task_returns),
                    Space());
            }

            protected void AddTextForSystemVoid()
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    PlainText(FeaturesResources.no_value));
            }

            protected SemanticModel GetSemanticModel(SyntaxTree tree)
            {
                if (_semanticModel.SyntaxTree == tree)
                {
                    return _semanticModel;
                }

                var model = _semanticModel.GetOriginalSemanticModel();
                if (model.Compilation.ContainsSyntaxTree(tree))
                {
                    return model.Compilation.GetSemanticModel(tree);
                }

                // it is from one of its p2p references
                foreach (var referencedCompilation in model.Compilation.GetReferencedCompilations())
                {
                    // find the reference that contains the given tree
                    if (referencedCompilation.ContainsSyntaxTree(tree))
                    {
                        return referencedCompilation.GetSemanticModel(tree);
                    }
                }

                // the tree, a source symbol is defined in, doesn't exist in universe
                // how this can happen?
                Contract.Requires(false, "How?");
                return null;
            }

            private async Task AddPartsAsync(ImmutableArray<ISymbol> symbols)
            {
                await AddDescriptionPartAsync(symbols[0]).ConfigureAwait(false);

                AddOverloadCountPart(symbols);
                FixAllAnonymousTypes(symbols[0]);
                AddExceptions(symbols[0]);
                AddCaptures(symbols[0]);
            }

            private void AddExceptions(ISymbol symbol)
            {
                var exceptionTypes = symbol.GetDocumentationComment().ExceptionTypes;
                if (exceptionTypes.Any())
                {
                    var parts = new List<SymbolDisplayPart>();
                    parts.Add(new SymbolDisplayPart(kind: SymbolDisplayPartKind.Text, symbol: null, text: $"\r\n{WorkspacesResources.Exceptions_colon}"));
                    foreach (var exceptionString in exceptionTypes)
                    {
                        parts.AddRange(LineBreak());
                        parts.AddRange(Space(count: 2));
                        parts.AddRange(AbstractDocumentationCommentFormattingService.CrefToSymbolDisplayParts(exceptionString, _position, _semanticModel));
                    }

                    AddToGroup(SymbolDescriptionGroups.Exceptions, parts);
                }
            }

            /// <summary>
            /// If the symbol is a local or anonymous function (lambda or delegate), adds the variables captured
            /// by that local or anonymous function to the "Captures" group.
            /// </summary>
            /// <param name="symbol"></param>
            protected abstract void AddCaptures(ISymbol symbol);

            /// <summary>
            /// Given the body of a local or an anonymous function (lambda or delegate), add the variables captured
            /// by that local or anonymous function to the "Captures" group.
            /// </summary>
            protected void AddCaptures(SyntaxNode syntax)
            {
                var semanticModel = GetSemanticModel(syntax.SyntaxTree);
                if(semanticModel.IsSpeculativeSemanticModel)
                {
                    // The region analysis APIs used below are not meaningful/applicable in the context of speculation (because they are designed
                    // to ask questions about an expression if it were in a certain *scope* of code, not if it were inserted at a certain *position*).
                    //
                    // But in the context of symbol completion, we do prepare a description for the symbol while speculating. Only the "main description"
                    // section of that description will be displayed. We still add a "captures" section, just in case.
                    AddToGroup(SymbolDescriptionGroups.Captures, new SymbolDisplayPart(kind: SymbolDisplayPartKind.Text, symbol: null, text: $"\r\n{WorkspacesResources.Variables_captured_colon} ?"));
                    return;
                }

                var analysis = semanticModel.AnalyzeDataFlow(syntax);
                var captures = analysis.CapturedInside;
                if (!captures.IsEmpty)
                {
                    var parts = new List<SymbolDisplayPart>();
                    parts.Add(new SymbolDisplayPart(kind: SymbolDisplayPartKind.Text, symbol: null, text: $"\r\n{WorkspacesResources.Variables_captured_colon}"));
                    bool first = true;
                    foreach (var captured in captures)
                    {
                        if (!first)
                        {
                            parts.AddRange(Punctuation(","));
                        }

                        parts.AddRange(Space(count: 1));
                        parts.AddRange(ToMinimalDisplayParts(captured, s_formatForCaptures));
                        first = false;
                    }

                    AddToGroup(SymbolDescriptionGroups.Captures, parts);
                }
            }

            private static readonly SymbolDisplayFormat s_formatForCaptures = SymbolDisplayFormat.MinimallyQualifiedFormat
                .RemoveLocalOptions(SymbolDisplayLocalOptions.IncludeType)
                .RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeType);

            public async Task<ImmutableArray<SymbolDisplayPart>> BuildDescriptionAsync(
                ImmutableArray<ISymbol> symbolGroup, SymbolDescriptionGroups groups)
            {
                Contract.ThrowIfFalse(symbolGroup.Length > 0);

                await AddPartsAsync(symbolGroup).ConfigureAwait(false);

                return this.BuildDescription(groups);
            }

            public async Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>> BuildDescriptionSectionsAsync(ImmutableArray<ISymbol> symbolGroup)
            {
                Contract.ThrowIfFalse(symbolGroup.Length > 0);

                await AddPartsAsync(symbolGroup).ConfigureAwait(false);

                return this.BuildDescriptionSections();
            }

            private async Task AddDescriptionPartAsync(ISymbol symbol)
            {
                if (symbol.GetAttributes().Any(x => x.AttributeClass.MetadataName == "ObsoleteAttribute"))
                {
                    AddDeprecatedPrefix();
                }

                if (symbol is IDynamicTypeSymbol)
                {
                    AddDescriptionForDynamicType();
                }
                else if (symbol is IFieldSymbol field)
                {
                    await AddDescriptionForFieldAsync(field).ConfigureAwait(false);
                }
                else if (symbol is ILocalSymbol local)
                {
                    await AddDescriptionForLocalAsync(local).ConfigureAwait(false);
                }
                else if (symbol is IMethodSymbol method)
                {
                    AddDescriptionForMethod(method);
                }
                else if (symbol is ILabelSymbol label)
                {
                    AddDescriptionForLabel(label);
                }
                else if (symbol is INamedTypeSymbol namedType)
                {
                    await AddDescriptionForNamedTypeAsync(namedType).ConfigureAwait(false);
                }
                else if (symbol is INamespaceSymbol namespaceSymbol)
                {
                    AddDescriptionForNamespace(namespaceSymbol);
                }
                else if (symbol is IParameterSymbol parameter)
                {
                    await AddDescriptionForParameterAsync(parameter).ConfigureAwait(false);
                }
                else if (symbol is IPropertySymbol property)
                {
                    AddDescriptionForProperty(property);
                }
                else if (symbol is IRangeVariableSymbol rangeVariable)
                {
                    AddDescriptionForRangeVariable(rangeVariable);
                }
                else if (symbol is ITypeParameterSymbol typeParameter)
                {
                    AddDescriptionForTypeParameter(typeParameter);
                }
                else if (symbol is IAliasSymbol alias)
                {
                    await AddDescriptionPartAsync(alias.Target).ConfigureAwait(false);
                }
                else
                {
                    AddDescriptionForArbitrarySymbol(symbol);
                }
            }

            private ImmutableArray<SymbolDisplayPart> BuildDescription(SymbolDescriptionGroups groups)
            {
                var finalParts = new List<SymbolDisplayPart>();
                var orderedGroups = _groupMap.Keys.OrderBy((g1, g2) => g1 - g2);

                foreach (var group in orderedGroups)
                {
                    if ((groups & group) == 0)
                    {
                        continue;
                    }

                    if (!finalParts.IsEmpty())
                    {
                        var newLines = GetPrecedingNewLineCount(group);
                        finalParts.AddRange(LineBreak(newLines));
                    }

                    var parts = _groupMap[group];
                    finalParts.AddRange(parts);
                }

                return finalParts.AsImmutable();
            }

            private static int GetPrecedingNewLineCount(SymbolDescriptionGroups group)
            {
                switch (group)
                {
                    case SymbolDescriptionGroups.MainDescription:
                        // these parts are continuations of whatever text came before them
                        return 0;

                    case SymbolDescriptionGroups.Documentation:
                        return 1;

                    case SymbolDescriptionGroups.AnonymousTypes:
                        return 0;

                    case SymbolDescriptionGroups.Exceptions:
                    case SymbolDescriptionGroups.TypeParameterMap:
                    case SymbolDescriptionGroups.Captures:
                        // Everything else is in a group on its own
                        return 2;

                    default:
                        return Contract.FailWithReturn<int>("unknown part kind");
                }
            }

            private IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>> BuildDescriptionSections()
            {
                return _groupMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToTaggedText());
            }

            private void AddDescriptionForDynamicType()
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    Keyword("dynamic"));
                AddToGroup(SymbolDescriptionGroups.Documentation,
                    PlainText(FeaturesResources.Represents_an_object_whose_operations_will_be_resolved_at_runtime));
            }

            private async Task AddDescriptionForNamedTypeAsync(INamedTypeSymbol symbol)
            {
                if (symbol.IsAwaitableNonDynamic(_semanticModel, _position))
                {
                    AddAwaitablePrefix();
                }

                var token = await _semanticModel.SyntaxTree.GetTouchingTokenAsync(_position, this.CancellationToken).ConfigureAwait(false);
                if (token != default)
                {
                    var syntaxFactsService = this.Workspace.Services.GetLanguageServices(token.Language).GetService<ISyntaxFactsService>();
                    if (syntaxFactsService.IsAwaitKeyword(token))
                    {
                        AddPrefixTextForAwaitKeyword();
                        if (symbol.SpecialType == SpecialType.System_Void)
                        {
                            AddTextForSystemVoid();
                            return;
                        }
                    }
                }

                if (symbol.TypeKind == TypeKind.Delegate)
                {
                    var style = s_descriptionStyle.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        ToDisplayParts(symbol.OriginalDefinition, style));
                }
                else
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        ToDisplayParts(symbol.OriginalDefinition, s_descriptionStyle));
                }

                if (!symbol.IsUnboundGenericType && !TypeArgumentsAndParametersAreSame(symbol))
                {
                    var allTypeParameters = symbol.GetAllTypeParameters().ToList();
                    var allTypeArguments = symbol.GetAllTypeArguments().ToList();

                    AddTypeParameterMapPart(allTypeParameters, allTypeArguments);
                }
            }

            private bool TypeArgumentsAndParametersAreSame(INamedTypeSymbol symbol)
            {
                var typeArguments = symbol.GetAllTypeArguments().ToList();
                var typeParameters = symbol.GetAllTypeParameters().ToList();

                for (int i = 0; i < typeArguments.Count; i++)
                {
                    var typeArgument = typeArguments[i];
                    var typeParameter = typeParameters[i];
                    if (typeArgument is ITypeParameterSymbol && typeArgument.Name == typeParameter.Name)
                    {
                        continue;
                    }

                    return false;
                }

                return true;
            }

            private void AddDescriptionForNamespace(INamespaceSymbol symbol)
            {
                if (symbol.IsGlobalNamespace)
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        ToDisplayParts(symbol, s_globalNamespaceStyle));
                }
                else
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        ToDisplayParts(symbol, s_descriptionStyle));
                }
            }

            private async Task AddDescriptionForFieldAsync(IFieldSymbol symbol)
            {
                var parts = await GetFieldPartsAsync(symbol).ConfigureAwait(false);

                // Don't bother showing disambiguating text for enum members. The icon displayed
                // on Quick Info should be enough.
                if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription, parts);
                }
                else
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        symbol.IsConst
                            ? Description(FeaturesResources.constant)
                            : Description(FeaturesResources.field),
                        parts);
                }
            }

            private async Task<ImmutableArray<SymbolDisplayPart>> GetFieldPartsAsync(IFieldSymbol symbol)
            {
                if (symbol.IsConst)
                {
                    var initializerParts = await GetInitializerSourcePartsAsync(symbol).ConfigureAwait(false);
                    if (!initializerParts.IsDefaultOrEmpty)
                    {
                        var parts = ArrayBuilder<SymbolDisplayPart>.GetInstance();
                        parts.AddRange(ToMinimalDisplayParts(symbol, MinimallyQualifiedFormat));
                        parts.AddRange(Space());
                        parts.AddRange(Punctuation("="));
                        parts.AddRange(Space());
                        parts.AddRange(initializerParts);

                        return parts.ToImmutableAndFree();
                    }
                }

                return ToMinimalDisplayParts(symbol, MinimallyQualifiedFormatWithConstants);
            }

            private async Task AddDescriptionForLocalAsync(ILocalSymbol symbol)
            {
                var parts = await GetLocalPartsAsync(symbol).ConfigureAwait(false);

                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    symbol.IsConst
                        ? Description(FeaturesResources.local_constant)
                        : Description(FeaturesResources.local_variable),
                    parts);
            }

            private async Task<ImmutableArray<SymbolDisplayPart>> GetLocalPartsAsync(ILocalSymbol symbol)
            {
                if (symbol.IsConst)
                {
                    var initializerParts = await GetInitializerSourcePartsAsync(symbol).ConfigureAwait(false);
                    if (initializerParts != null)
                    {
                        var parts = ArrayBuilder<SymbolDisplayPart>.GetInstance();
                        parts.AddRange(ToMinimalDisplayParts(symbol, MinimallyQualifiedFormat));
                        parts.AddRange(Space());
                        parts.AddRange(Punctuation("="));
                        parts.AddRange(Space());
                        parts.AddRange(initializerParts);

                        return parts.ToImmutableAndFree();
                    }
                }

                return ToMinimalDisplayParts(symbol, MinimallyQualifiedFormatWithConstants);
            }

            private void AddDescriptionForLabel(ILabelSymbol symbol)
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    Description(FeaturesResources.label),
                    ToMinimalDisplayParts(symbol));
            }

            private void AddDescriptionForRangeVariable(IRangeVariableSymbol symbol)
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                   Description(FeaturesResources.range_variable),
                   ToMinimalDisplayParts(symbol));
            }

            private void AddDescriptionForMethod(IMethodSymbol method)
            {
                // TODO : show duplicated member case
                var awaitable = method.IsAwaitableNonDynamic(_semanticModel, _position);
                var extension = method.IsExtensionMethod || method.MethodKind == MethodKind.ReducedExtension;
                if (awaitable && extension)
                {
                    AddAwaitableExtensionPrefix();
                }
                else if (awaitable)
                {
                    AddAwaitablePrefix();
                }
                else if (extension)
                {
                    AddExtensionPrefix();
                }

                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    ToMinimalDisplayParts(method, s_memberSignatureDisplayFormat));

                if (awaitable)
                {
                    AddAwaitableUsageText(method, _semanticModel, _position);
                }
            }

            protected abstract void AddAwaitableUsageText(IMethodSymbol method, SemanticModel semanticModel, int position);

            private async Task AddDescriptionForParameterAsync(IParameterSymbol symbol)
            {
                if (symbol.IsOptional)
                {
                    var initializerParts = await GetInitializerSourcePartsAsync(symbol).ConfigureAwait(false);
                    if (!initializerParts.IsDefaultOrEmpty)
                    {
                        var parts = ToMinimalDisplayParts(symbol, MinimallyQualifiedFormat).ToList();
                        parts.AddRange(Space());
                        parts.AddRange(Punctuation("="));
                        parts.AddRange(Space());
                        parts.AddRange(initializerParts);

                        AddToGroup(SymbolDescriptionGroups.MainDescription,
                            Description(FeaturesResources.parameter), parts);

                        return;
                    }
                }

                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    Description(FeaturesResources.parameter),
                    ToMinimalDisplayParts(symbol, MinimallyQualifiedFormatWithConstants));
            }

            protected void AddDescriptionForProperty(IPropertySymbol symbol)
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    ToMinimalDisplayParts(symbol, s_memberSignatureDisplayFormat));
            }

            private void AddDescriptionForArbitrarySymbol(ISymbol symbol)
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    ToMinimalDisplayParts(symbol));
            }

            private void AddDescriptionForTypeParameter(ITypeParameterSymbol symbol)
            {
                Contract.ThrowIfTrue(symbol.TypeParameterKind == TypeParameterKind.Cref);
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    ToMinimalDisplayParts(symbol),
                    Space(),
                    PlainText(FeaturesResources.in_),
                    Space(),
                    ToMinimalDisplayParts(symbol.ContainingSymbol, s_typeParameterOwnerFormat));
            }

            private void AddOverloadCountPart(
                ImmutableArray<ISymbol> symbolGroup)
            {
                var count = GetOverloadCount(symbolGroup);
                if (count >= 1)
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        Space(),
                        Punctuation("("),
                        Punctuation("+"),
                        Space(),
                        PlainText(count.ToString()),
                        Space(),
                        count == 1 ? PlainText(FeaturesResources.overload) : PlainText(FeaturesResources.overloads_),
                        Punctuation(")"));
                }
            }

            private static int GetOverloadCount(ImmutableArray<ISymbol> symbolGroup)
            {
                return symbolGroup.Select(s => s.OriginalDefinition)
                                  .Where(s => !s.Equals(symbolGroup.First().OriginalDefinition))
                                  .Where(s => s is IMethodSymbol || s.IsIndexer())
                                  .Count();
            }

            protected void AddTypeParameterMapPart(
                List<ITypeParameterSymbol> typeParameters,
                List<ITypeSymbol> typeArguments)
            {
                var parts = new List<SymbolDisplayPart>();

                var count = typeParameters.Count;
                for (int i = 0; i < count; i++)
                {
                    parts.AddRange(TypeParameterName(typeParameters[i].Name));
                    parts.AddRange(Space());

                    parts.AddRange(PlainText(FeaturesResources.is_));
                    parts.AddRange(Space());
                    parts.AddRange(ToMinimalDisplayParts(typeArguments[i]));

                    if (i < count - 1)
                    {
                        parts.AddRange(LineBreak());
                    }
                }

                AddToGroup(SymbolDescriptionGroups.TypeParameterMap,
                    parts);
            }

            protected void AddToGroup(SymbolDescriptionGroups group, params SymbolDisplayPart[] partsArray)
            {
                AddToGroup(group, (IEnumerable<SymbolDisplayPart>)partsArray);
            }

            protected void AddToGroup(SymbolDescriptionGroups group, params IEnumerable<SymbolDisplayPart>[] partsArray)
            {
                var partsList = partsArray.Flatten().ToList();
                if (partsList.Count > 0)
                {
                    if (!_groupMap.TryGetValue(group, out var existingParts))
                    {
                        existingParts = new List<SymbolDisplayPart>();
                        _groupMap.Add(group, existingParts);
                    }

                    existingParts.AddRange(partsList);
                }
            }

            private IEnumerable<SymbolDisplayPart> Description(string description)
            {
                return Punctuation("(")
                    .Concat(PlainText(description))
                    .Concat(Punctuation(")"))
                    .Concat(Space());
            }

            protected IEnumerable<SymbolDisplayPart> Keyword(string text)
            {
                return Part(SymbolDisplayPartKind.Keyword, text);
            }

            protected IEnumerable<SymbolDisplayPart> LineBreak(int count = 1)
            {
                for (int i = 0; i < count; i++)
                {
                    yield return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
                }
            }

            protected IEnumerable<SymbolDisplayPart> PlainText(string text)
            {
                return Part(SymbolDisplayPartKind.Text, text);
            }

            protected IEnumerable<SymbolDisplayPart> Punctuation(string text)
            {
                return Part(SymbolDisplayPartKind.Punctuation, text);
            }

            protected IEnumerable<SymbolDisplayPart> Space(int count = 1)
            {
                yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, new string(' ', count));
            }

            protected ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(ISymbol symbol, SymbolDisplayFormat format = null)
            {
                format = format ?? MinimallyQualifiedFormat;
                return _displayService.ToMinimalDisplayParts(_semanticModel, _position, symbol, format);
            }

            protected IEnumerable<SymbolDisplayPart> ToDisplayParts(ISymbol symbol, SymbolDisplayFormat format = null)
            {
                return _displayService.ToDisplayParts(symbol, format);
            }

            private IEnumerable<SymbolDisplayPart> Part(SymbolDisplayPartKind kind, ISymbol symbol, string text)
            {
                yield return new SymbolDisplayPart(kind, symbol, text);
            }

            private IEnumerable<SymbolDisplayPart> Part(SymbolDisplayPartKind kind, string text)
            {
                return Part(kind, null, text);
            }

            private IEnumerable<SymbolDisplayPart> TypeParameterName(string text)
            {
                return Part(SymbolDisplayPartKind.TypeParameterName, text);
            }
        }
    }
}

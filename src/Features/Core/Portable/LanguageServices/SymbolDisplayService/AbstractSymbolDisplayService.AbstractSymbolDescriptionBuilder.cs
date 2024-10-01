// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract partial class AbstractSymbolDisplayService
{
    protected abstract partial class AbstractSymbolDescriptionBuilder
    {
        private static readonly SymbolDisplayFormat s_typeParameterOwnerFormat =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
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
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
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
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                    SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
                    SymbolDisplayMiscellaneousOptions.CollapseTupleTypes);

        private static readonly SymbolDisplayFormat s_descriptionStyle =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.CollapseTupleTypes,
                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword);

        private static readonly SymbolDisplayFormat s_globalNamespaceStyle =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);

        private readonly SemanticModel _semanticModel;
        private readonly int _position;
        private readonly Dictionary<SymbolDescriptionGroups, IList<SymbolDisplayPart>> _groupMap = [];
        private readonly Dictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>> _documentationMap = [];
        private readonly Func<ISymbol?, string?> _getNavigationHint;

        protected readonly LanguageServices LanguageServices;
        protected readonly SymbolDescriptionOptions Options;
        protected readonly CancellationToken CancellationToken;

        protected AbstractSymbolDescriptionBuilder(
            SemanticModel semanticModel,
            int position,
            LanguageServices languageServices,
            SymbolDescriptionOptions options,
            CancellationToken cancellationToken)
        {
            LanguageServices = languageServices;
            Options = options;
            CancellationToken = cancellationToken;
            _semanticModel = semanticModel;
            _position = position;
            _getNavigationHint = GetNavigationHint;
        }

        protected abstract void AddExtensionPrefix();
        protected abstract void AddAwaitablePrefix();
        protected abstract void AddAwaitableExtensionPrefix();
        protected abstract void AddDeprecatedPrefix();
        protected abstract void AddEnumUnderlyingTypeSeparator();
        protected abstract Task<ImmutableArray<SymbolDisplayPart>> GetInitializerSourcePartsAsync(ISymbol symbol);
        protected abstract ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(ISymbol symbol, SemanticModel semanticModel, int position, SymbolDisplayFormat format);
        protected abstract string? GetNavigationHint(ISymbol? symbol);

        protected abstract SymbolDisplayFormat MinimallyQualifiedFormat { get; }
        protected abstract SymbolDisplayFormat MinimallyQualifiedFormatWithConstants { get; }
        protected abstract SymbolDisplayFormat MinimallyQualifiedFormatWithConstantsAndModifiers { get; }

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
            Debug.Assert(false, "How?");
            return null;
        }

        protected Compilation Compilation
            => _semanticModel.Compilation;

        private async Task AddPartsAsync(ImmutableArray<ISymbol> symbols)
        {
            var firstSymbol = symbols[0];

            // Grab the doc comment once as computing it for each portion we're concatenating can be expensive for
            // LSIF (which does this for every symbol in an entire solution).
            var firstSymbolDocumentationComment = firstSymbol.GetAppropriateDocumentationComment(Compilation, CancellationToken);

            await AddDescriptionPartAsync(firstSymbol).ConfigureAwait(false);

            AddOverloadCountPart(symbols);
            FixAllStructuralTypes(firstSymbol);
            AddExceptions(firstSymbolDocumentationComment);
            AddCaptures(firstSymbol);

            AddDocumentationContent(firstSymbol, firstSymbolDocumentationComment);
        }

        private void AddDocumentationContent(ISymbol symbol, DocumentationComment documentationComment)
        {
            var formatter = LanguageServices.GetRequiredService<IDocumentationCommentFormattingService>();
            var format = ISymbolExtensions2.CrefFormat;

            _documentationMap.Add(
                SymbolDescriptionGroups.Documentation,
                formatter.Format(documentationComment.SummaryText, symbol, _semanticModel, _position, format, CancellationToken));

            _documentationMap.Add(
                SymbolDescriptionGroups.RemarksDocumentation,
                formatter.Format(documentationComment.RemarksText, symbol, _semanticModel, _position, format, CancellationToken));

            AddDocumentationPartsWithPrefix(documentationComment.ReturnsText, SymbolDescriptionGroups.ReturnsDocumentation, FeaturesResources.Returns_colon);
            AddDocumentationPartsWithPrefix(documentationComment.ValueText, SymbolDescriptionGroups.ValueDocumentation, FeaturesResources.Value_colon);

            return;

            void AddDocumentationPartsWithPrefix(string? rawXmlText, SymbolDescriptionGroups group, string prefix)
            {
                if (string.IsNullOrEmpty(rawXmlText))
                    return;

                var parts = formatter.Format(rawXmlText, symbol, _semanticModel, _position, format, CancellationToken);
                if (!parts.IsDefaultOrEmpty)
                {
                    _documentationMap.Add(group,
                    [
                        new TaggedText(TextTags.Text, prefix),
                        .. LineBreak().ToTaggedText(),
                        new TaggedText(TextTags.ContainerStart, "  "),
                        .. parts,
                        new TaggedText(TextTags.ContainerEnd, string.Empty),
                    ]);
                }
            }
        }

        private void AddExceptions(DocumentationComment documentationComment)
        {
            if (documentationComment.ExceptionTypes.Any())
            {
                var parts = new List<SymbolDisplayPart>();
                parts.AddLineBreak();
                parts.AddText(WorkspacesResources.Exceptions_colon);
                foreach (var exceptionString in documentationComment.ExceptionTypes)
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
            if (semanticModel.IsSpeculativeSemanticModel)
            {
                // The region analysis APIs used below are not meaningful/applicable in the context of speculation (because they are designed
                // to ask questions about an expression if it were in a certain *scope* of code, not if it were inserted at a certain *position*).
                //
                // But in the context of symbol completion, we do prepare a description for the symbol while speculating. Only the "main description"
                // section of that description will be displayed. We still add a "captures" section, just in case.
                AddToGroup(SymbolDescriptionGroups.Captures, LineBreak());
                AddToGroup(SymbolDescriptionGroups.Captures, PlainText($"{WorkspacesResources.Variables_captured_colon} ?"));
                return;
            }

            var analysis = semanticModel.AnalyzeDataFlow(syntax);
            var captures = analysis.CapturedInside.Except(analysis.VariablesDeclared).ToImmutableArray();
            if (!captures.IsEmpty)
            {
                var parts = new List<SymbolDisplayPart>();
                parts.AddLineBreak();
                parts.AddText(WorkspacesResources.Variables_captured_colon);
                var first = true;
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

            return BuildDescription(groups);
        }

        public async Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>> BuildDescriptionSectionsAsync(ImmutableArray<ISymbol> symbolGroup)
        {
            Contract.ThrowIfFalse(symbolGroup.Length > 0);

            await AddPartsAsync(symbolGroup).ConfigureAwait(false);

            return BuildDescriptionSections();
        }

        private async Task AddDescriptionPartAsync(ISymbol symbol)
        {
            if (symbol.IsObsolete())
            {
                AddDeprecatedPrefix();
            }

            if (symbol is IDiscardSymbol discard)
            {
                AddDescriptionForDiscard(discard);
            }
            else if (symbol is IDynamicTypeSymbol)
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
                if (namedType.IsTupleType)
                {
                    AddToGroup(SymbolDescriptionGroups.MainDescription,
                        symbol.ToDisplayParts(s_descriptionStyle));
                }
                else
                {
                    AddDescriptionForNamedType(namedType);
                }
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
                case SymbolDescriptionGroups.RemarksDocumentation:
                case SymbolDescriptionGroups.ReturnsDocumentation:
                case SymbolDescriptionGroups.ValueDocumentation:
                    return 1;

                case SymbolDescriptionGroups.StructuralTypes:
                    return 0;

                case SymbolDescriptionGroups.Exceptions:
                case SymbolDescriptionGroups.TypeParameterMap:
                case SymbolDescriptionGroups.Captures:
                    // Everything else is in a group on its own
                    return 2;

                default:
                    throw ExceptionUtilities.UnexpectedValue(group);
            }
        }

        private IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>> BuildDescriptionSections()
        {
            var includeNavigationHints = Options.QuickInfoOptions.IncludeNavigationHintsInQuickInfo;

            // Merge the two maps into one final result.
            var result = new Dictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>(_documentationMap);
            foreach (var (group, parts) in _groupMap)
            {
                // To support CodeGeneration symbols, which do not support ToDisplayString we need to pass custom implementation:
                var taggedText = parts.ToTaggedText(TaggedTextStyle.None, _getNavigationHint, includeNavigationHints);
                if (group == SymbolDescriptionGroups.MainDescription)
                {
                    // Mark the main description as a code block.
                    taggedText = taggedText
                        .Insert(0, new TaggedText(TextTags.CodeBlockStart, string.Empty))
                        .Add(new TaggedText(TextTags.CodeBlockEnd, string.Empty));
                }

                result[group] = taggedText;
            }

            return result;
        }

        private void AddDescriptionForDynamicType()
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Keyword("dynamic"));
            AddToGroup(SymbolDescriptionGroups.Documentation,
                PlainText(FeaturesResources.Represents_an_object_whose_operations_will_be_resolved_at_runtime));
        }

        private void AddDescriptionForNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.IsAwaitableNonDynamic(_semanticModel, _position))
            {
                AddAwaitablePrefix();
            }

            AddSymbolDescription(symbol);

            if (!symbol.IsUnboundGenericType &&
                !TypeArgumentsAndParametersAreSame(symbol) &&
                !symbol.IsAnonymousDelegateType())
            {
                var allTypeParameters = symbol.GetAllTypeParameters().ToList();
                var allTypeArguments = symbol.GetAllTypeArguments().ToList();

                AddTypeParameterMapPart(allTypeParameters, allTypeArguments);
            }

            if (symbol.IsEnumType() && symbol.EnumUnderlyingType!.SpecialType != SpecialType.System_Int32)
            {
                AddEnumUnderlyingTypeSeparator();
                var underlyingTypeDisplayParts = symbol.EnumUnderlyingType.ToDisplayParts(s_descriptionStyle.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
                AddToGroup(SymbolDescriptionGroups.MainDescription, underlyingTypeDisplayParts);
            }
        }

        private void AddSymbolDescription(INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Delegate)
            {
                var style = s_descriptionStyle.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

                // Under the covers anonymous delegates are represented with generic types.  However, we don't want
                // to see the unbound form of that generic.  We want to see the fully instantiated signature.
                AddToGroup(SymbolDescriptionGroups.MainDescription, symbol.IsAnonymousDelegateType()
                    ? symbol.ToDisplayParts(style)
                    : symbol.OriginalDefinition.ToDisplayParts(style));
            }
            else
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    symbol.OriginalDefinition.ToDisplayParts(s_descriptionStyle));
            }

            if (symbol.NullableAnnotation == NullableAnnotation.Annotated)
                AddToGroup(SymbolDescriptionGroups.MainDescription, new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "?"));
        }

        private static bool TypeArgumentsAndParametersAreSame(INamedTypeSymbol symbol)
        {
            var typeArguments = symbol.GetAllTypeArguments().ToList();
            var typeParameters = symbol.GetAllTypeParameters().ToList();

            for (var i = 0; i < typeArguments.Count; i++)
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
                    symbol.ToDisplayParts(s_globalNamespaceStyle));
            }
            else
            {
                AddToGroup(SymbolDescriptionGroups.MainDescription,
                    symbol.ToDisplayParts(s_descriptionStyle));
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
                    return
                    [
                        .. ToMinimalDisplayParts(symbol, MinimallyQualifiedFormat),
                        .. Space(),
                        .. Punctuation("="),
                        .. Space(),
                        .. initializerParts,
                    ];
                }
            }

            return ToMinimalDisplayParts(symbol, MinimallyQualifiedFormatWithConstantsAndModifiers);
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
                    return
                    [
                        .. ToMinimalDisplayParts(symbol, MinimallyQualifiedFormat),
                        .. Space(),
                        .. Punctuation("="),
                        .. Space(),
                        .. initializerParts,
                    ];
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
        }

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
                Description(symbol.IsDiscard ? FeaturesResources.discard : FeaturesResources.parameter),
                ToMinimalDisplayParts(symbol, MinimallyQualifiedFormatWithConstants));
        }

        private void AddDescriptionForDiscard(IDiscardSymbol symbol)
        {
            AddToGroup(SymbolDescriptionGroups.MainDescription,
                Description(FeaturesResources.discard),
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
            for (var i = 0; i < count; i++)
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
            => AddToGroup(group, (IEnumerable<SymbolDisplayPart>)partsArray);

        protected void AddToGroup(SymbolDescriptionGroups group, params IEnumerable<SymbolDisplayPart>[] partsArray)
        {
            var partsList = partsArray.Flatten().ToList();
            if (partsList.Count > 0)
            {
                if (!_groupMap.TryGetValue(group, out var existingParts))
                {
                    existingParts = [];
                    _groupMap.Add(group, existingParts);
                }

                existingParts.AddRange(partsList);
            }
        }

        private static IEnumerable<SymbolDisplayPart> Description(string description)
        {
            return
            [
                .. Punctuation("("),
                .. PlainText(description),
                .. Punctuation(")"),
                .. Space(),
            ];
        }

        protected static IEnumerable<SymbolDisplayPart> Keyword(string text)
            => Part(SymbolDisplayPartKind.Keyword, text);

        protected static IEnumerable<SymbolDisplayPart> LineBreak(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
            }
        }

        protected static IEnumerable<SymbolDisplayPart> PlainText(string text)
            => Part(SymbolDisplayPartKind.Text, text);

        protected static IEnumerable<SymbolDisplayPart> Punctuation(string text)
            => Part(SymbolDisplayPartKind.Punctuation, text);

        protected static IEnumerable<SymbolDisplayPart> Space(int count = 1)
        {
            yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, new string(' ', count));
        }

        protected ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(ISymbol symbol, SymbolDisplayFormat? format = null)
        {
            format ??= MinimallyQualifiedFormat;
            return ToMinimalDisplayParts(symbol, _semanticModel, _position, format);
        }

        private static IEnumerable<SymbolDisplayPart> Part(SymbolDisplayPartKind kind, ISymbol? symbol, string text)
        {
            yield return new SymbolDisplayPart(kind, symbol, text);
        }

        private static IEnumerable<SymbolDisplayPart> Part(SymbolDisplayPartKind kind, string text)
            => Part(kind, null, text);

        private static IEnumerable<SymbolDisplayPart> TypeParameterName(string text)
            => Part(SymbolDisplayPartKind.TypeParameterName, text);
    }
}

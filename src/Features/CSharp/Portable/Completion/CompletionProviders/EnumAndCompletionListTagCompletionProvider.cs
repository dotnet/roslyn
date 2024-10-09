// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(EnumAndCompletionListTagCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(CSharpSuggestionModeCompletionProvider))]
[Shared]
internal partial class EnumAndCompletionListTagCompletionProvider : LSPCompletionProvider
{
    private static readonly CompletionItemRules s_enumTypeRules =
        CompletionItemRules.Default.WithCommitCharacterRules([CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '.')])
                                   .WithMatchPriority(MatchPriority.Preselect)
                                   .WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection);

    private static readonly ImmutableHashSet<char> s_triggerCharacters = [' ', '[', '(', '~'];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EnumAndCompletionListTagCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        // Bring up on space or at the start of a word, or after a ( or [.
        //
        // Note: we don't want to bring this up after traditional enum operators like & or |.
        // That's because we don't like the experience where the enum appears directly after the
        // operator.  Instead, the user normally types <space> and we will bring up the list
        // then.
        return
            text[characterPosition] is ' ' or '[' or '(' or '~' ||
            options.TriggerOnTypingLetters && CompletionUtilities.IsStartingNewWord(text, characterPosition);
    }

    public override ImmutableHashSet<char> TriggerCharacters => s_triggerCharacters;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (tree.IsInNonUserCode(position, cancellationToken))
                return;

            var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = syntaxContext.SemanticModel;

            if (syntaxContext.IsTaskLikeTypeContext)
                return;

            var token = syntaxContext.TargetToken;
            if (token.IsMandatoryNamedParameterPosition())
                return;

            // Don't show up within member access
            // This previously worked because the type inferrer didn't work
            // in member access expressions.
            // The regular SymbolCompletionProvider will handle completion after .
            if (token.IsKind(SyntaxKind.DotToken))
                return;

            var typeInferenceService = document.GetLanguageService<ITypeInferenceService>();
            Contract.ThrowIfNull(typeInferenceService, nameof(typeInferenceService));

            var infos = typeInferenceService.GetTypeInferenceInfo(semanticModel, position, cancellationToken);

            if (infos.Length == 0)
                infos = [new TypeInferenceInfo(semanticModel.Compilation.ObjectType)];

            foreach (var (type, isParams) in infos)
                await HandleSingleTypeAsync(context, semanticModel, token, type, isParams, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.General))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static async Task HandleSingleTypeAsync(
        CompletionContext context, SemanticModel semanticModel, SyntaxToken token, ITypeSymbol type, bool isParams, CancellationToken cancellationToken)
    {
        if (isParams && type is IArrayTypeSymbol arrayType)
            type = arrayType.ElementType;

        // If we have a Nullable<T>, unwrap it.
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var typeArg = type.GetTypeArguments().FirstOrDefault();
            if (typeArg == null)
                return;

            type = typeArg;
        }

        // When true, this completion provider shows both the type (e.g. DayOfWeek) and its qualified members (e.g.
        // DayOfWeek.Friday). We set this to false for enum-like cases (static members of structs and classes) so we
        // only show the qualified members in these cases.
        var isEnumOrCompletionListType = true;
        var position = context.Position;
        var enclosingNamedType = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (type.TypeKind != TypeKind.Enum)
        {
            var enumType = TryGetEnumTypeInEnumInitializer(semanticModel, token, type, cancellationToken) ??
                           TryGetCompletionListType(type, enclosingNamedType, semanticModel.Compilation);

            if (enumType == null)
            {
                if (context.Trigger.Kind == CompletionTriggerKind.Insertion && s_triggerCharacters.Contains(context.Trigger.Character))
                {
                    // This completion provider understands static members of matching types, but doesn't
                    // proactively trigger completion for them to avoid interfering with common typing patterns.
                    return;
                }

                // If this isn't an enum or marked with completionlist, also check if it contains static members of
                // a matching type. These 'enum-like' types have similar characteristics to enum completion, but do
                // not show the containing type as a separate item in completion.
                isEnumOrCompletionListType = false;
                enumType = TryGetTypeWithStaticMembers(type);
                if (enumType == null)
                {
                    return;
                }
            }

            type = enumType;
        }

        var hideAdvancedMembers = context.CompletionOptions.MemberDisplayOptions.HideAdvancedMembers;
        if (!type.IsEditorBrowsable(hideAdvancedMembers, semanticModel.Compilation))
            return;

        // Does type have any aliases?
        var alias = await type.FindApplicableAliasAsync(position, semanticModel, cancellationToken).ConfigureAwait(false);

        var displayText = alias != null
            ? alias.Name
            : type.ToMinimalDisplayString(semanticModel, position);

        // Add the enum itself.
        var symbol = alias ?? type;
        var sortText = symbol.Name;

        if (isEnumOrCompletionListType)
        {
            context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                displayText,
                displayTextSuffix: "",
                symbols: ImmutableArray.Create(symbol),
                rules: s_enumTypeRules,
                contextPosition: position,
                sortText: sortText));
        }

        // And now all the accessible members of the enum.
        if (type.TypeKind == TypeKind.Enum)
        {
            // We'll want to build a list of the actual enum members and all accessible instances of that enum, too
            var index = 0;

            var fields = type.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).Where(f => f.HasConstantValue);
            foreach (var field in fields.OrderBy(f => IntegerUtilities.ToInt64(f.ConstantValue)))
            {
                index++;
                if (!field.IsEditorBrowsable(hideAdvancedMembers, semanticModel.Compilation))
                    continue;

                // Use enum member name as an additional filter text, which would promote this item
                // during matching when user types member name only, like "Red" instead of 
                // "Colors.Red"
                var memberDisplayName = $"{displayText}.{field.Name}";
                var additionalFilterTexts = ImmutableArray.Create(field.Name);

                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: memberDisplayName,
                    displayTextSuffix: "",
                    symbols: ImmutableArray.Create<ISymbol>(field),
                    rules: CompletionItemRules.Default,
                    contextPosition: position,
                    sortText: $"{sortText}_{index:0000}",
                    filterText: memberDisplayName,
                    tags: WellKnownTagArrays.TargetTypeMatch)
                    .WithAdditionalFilterTexts(additionalFilterTexts));
            }
        }
        else if (enclosingNamedType is not null)
        {
            // Build a list of the members with the same type as the target
            foreach (var member in type.GetMembers())
            {
                if (!member.CanBeReferencedByName)
                    continue;

                ISymbol staticSymbol;
                ITypeSymbol symbolType;
                if (member is IFieldSymbol { IsStatic: true } field)
                {
                    staticSymbol = field;
                    symbolType = field.Type;
                }
                else if (member is IPropertySymbol { IsStatic: true, IsIndexer: false } property)
                {
                    staticSymbol = property;
                    symbolType = property.Type;
                }
                else
                {
                    // Only fields and properties are supported for static member matching
                    continue;
                }

                // We only show static properties/fields of compatible type if containing type is NOT marked with completionlist tag.
                if (!isEnumOrCompletionListType && !SymbolEqualityComparer.Default.Equals(type, symbolType))
                {
                    continue;
                }

                if (!staticSymbol.IsAccessibleWithin(enclosingNamedType) ||
                    !staticSymbol.IsEditorBrowsable(hideAdvancedMembers, semanticModel.Compilation))
                {
                    continue;
                }

                // Use member name as an additional filter text, which would promote this item
                // during matching when user types member name only, like "Empty" instead of 
                // "ImmutableArray.Empty"
                var memberDisplayName = $"{displayText}.{staticSymbol.Name}";
                var additionalFilterTexts = ImmutableArray.Create(staticSymbol.Name);

                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: memberDisplayName,
                    displayTextSuffix: "",
                    symbols: ImmutableArray.Create(staticSymbol),
                    rules: CompletionItemRules.Default,
                    contextPosition: position,
                    sortText: memberDisplayName,
                    filterText: memberDisplayName,
                    tags: WellKnownTagArrays.TargetTypeMatch)
                    .WithAdditionalFilterTexts(additionalFilterTexts));
            }
        }
    }

    private static ITypeSymbol? TryGetEnumTypeInEnumInitializer(
        SemanticModel semanticModel, SyntaxToken token,
        ITypeSymbol type, CancellationToken cancellationToken)
    {
        // https://github.com/dotnet/roslyn/issues/5419
        //
        // 14.3: "Within an enum member initializer, values of other enum members are always 
        // treated as having the type of their underlying type"

        // i.e. if we have "enum E { X, Y, Z = X | 
        // then we want to offer the enum after the |.  However, the compiler will report this
        // as an 'int' type, not the enum type.

        // See if we're after a common enum-combining operator.
        if (token.Kind() is SyntaxKind.BarToken or
            SyntaxKind.AmpersandToken or
            SyntaxKind.CaretToken)
        {
            // See if the type we're looking at is the underlying type for the enum we're contained in.
            var containingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);
            if (containingType?.TypeKind == TypeKind.Enum &&
                type.Equals(containingType.EnumUnderlyingType))
            {
                // If so, walk back to the token before the operator token and see if it binds to a member
                // of this enum.
                var previousToken = token.GetPreviousToken();
                if (previousToken.Parent != null)
                {
                    var symbol = semanticModel.GetSymbolInfo(previousToken.Parent, cancellationToken).Symbol;

                    if (symbol?.Kind == SymbolKind.Field &&
                        containingType.Equals(symbol.ContainingType))
                    {
                        // If so, then offer this as a place for enum completion for the enum we're currently 
                        // inside of.
                        return containingType;
                    }
                }
            }
        }

        return null;
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    private static INamedTypeSymbol? TryGetCompletionListType(ITypeSymbol type, INamedTypeSymbol? within, Compilation compilation)
    {
        if (within == null)
            return null;

        // PERF: None of the SpecialTypes include <completionlist> tags,
        // so we don't even need to load the documentation.
        if (type.IsSpecialType())
            return null;

        // PERF: Avoid parsing XML unless the text contains the word "completionlist".
        var xmlText = type.GetDocumentationCommentXml();
        if (xmlText == null || !xmlText.Contains(DocumentationCommentXmlNames.CompletionListElementName))
            return null;

        var documentation = CodeAnalysis.Shared.Utilities.DocumentationComment.FromXmlFragment(xmlText);

        var completionListType = documentation.CompletionListCref != null
            ? DocumentationCommentId.GetSymbolsForDeclarationId(documentation.CompletionListCref, compilation).OfType<INamedTypeSymbol>().FirstOrDefault()
            : null;

        return completionListType != null && completionListType.IsAccessibleWithin(within)
            ? completionListType
            : null;
    }

    private static INamedTypeSymbol? TryGetTypeWithStaticMembers(ITypeSymbol type)
    {
        // The reference type might be nullable, so we need to remove the annotation.
        // Otherwise, we will end up with items like "string?.Empty".
        if (type.TypeKind is TypeKind.Struct or TypeKind.Class)
            return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) as INamedTypeSymbol;

        return null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractObjectInitializerCompletionProvider : LSPCompletionProvider
{
    protected abstract (ITypeSymbol type, Location location, bool isObjectInitializer)? GetInitializedType(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    protected abstract HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken);
    protected abstract string EscapeIdentifier(ISymbol symbol);

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var document = context.Document;
        var position = context.Position;
        var cancellationToken = context.CancellationToken;

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        if (GetInitializedType(document, semanticModel, position, cancellationToken) is not var (type, initializerLocation, isObjectInitializer))
            return;

        if (type is ITypeParameterSymbol typeParameterSymbol)
            type = typeParameterSymbol.GetNamedTypeSymbolConstraint();

        if (type is not INamedTypeSymbol initializedType)
            return;

        if (await IsExclusiveAsync(document, position, cancellationToken).ConfigureAwait(false))
            context.IsExclusive = true;

        var enclosing = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        Contract.ThrowIfNull(enclosing);

        // Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
        var members = semanticModel
            .LookupSymbols(position, initializedType)
            .Where(m => IsInitializableFieldOrProperty(m, enclosing));

        // Filter out those members that have already been typed
        var alreadyTypedMembers = GetInitializedMembers(semanticModel.SyntaxTree, position, cancellationToken);
        var uninitializedMembers = members.Where(m => !alreadyTypedMembers.Contains(m.Name));

        // Sort the members by name so if we preselect one, it'll be stable
        uninitializedMembers = uninitializedMembers
            .Where(m => m.IsEditorBrowsable(context.CompletionOptions.MemberDisplayOptions.HideAdvancedMembers, semanticModel.Compilation))
            .OrderBy(m => m.Name);

        var firstUninitializedRequiredMember = true;

        foreach (var uninitializedMember in uninitializedMembers)
        {
            var rules = s_rules;

            // We'll hard select the first required member to make it a bit easier to type out an object initializer
            // with a bunch of members.
            if (firstUninitializedRequiredMember &&
                isObjectInitializer &&
                uninitializedMember.IsRequired())
            {
                rules = rules.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection).WithMatchPriority(MatchPriority.Preselect);
                firstUninitializedRequiredMember = false;
            }

            context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                displayText: EscapeIdentifier(uninitializedMember),
                displayTextSuffix: "",
                insertionText: null,
                symbols: [uninitializedMember],
                contextPosition: initializerLocation.SourceSpan.Start,
                inlineDescription: isObjectInitializer && uninitializedMember.IsRequired() ? FeaturesResources.Required : null,
                rules: rules));
        }
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    protected abstract Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken);

    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

    protected virtual bool IsInitializableFieldOrProperty(ISymbol fieldOrProperty, INamedTypeSymbol containingType)
    {
        if (!fieldOrProperty.IsStatic &&
            !fieldOrProperty.IsImplicitlyDeclared &&
            fieldOrProperty.CanBeReferencedByName &&
            fieldOrProperty is IFieldSymbol or IPropertySymbol &&
            fieldOrProperty.IsAccessibleWithin(containingType))
        {
            if (fieldOrProperty.IsWriteableFieldOrProperty() ||
                fieldOrProperty.ContainingType.IsAnonymousType ||
                CanSupportObjectInitializer(fieldOrProperty))
            {
                return true;
            }
        }

        return false;

        static bool CanSupportObjectInitializer(ISymbol fieldOrProperty)
        {
            Debug.Assert(!fieldOrProperty.IsWriteableFieldOrProperty(), "Assertion failed - expected writable field/property check before calling this method.");

            return MemberTypeCanSupportObjectInitializer(fieldOrProperty switch
            {
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                _ => throw ExceptionUtilities.Unreachable()
            });
        }

        static bool MemberTypeCanSupportObjectInitializer(ITypeSymbol type)
        {
            // NOTE: While in C# it is legal to write 'Member = {}' on a member of any of
            // the ruled out types below, it has no effects and is thus a needless recommendation
            // Example of the above case:
            /*
                class C
                {
                    string S { get; }
                }

                new C()
                {
                    S = {},
                };
            */

            // We avoid some types that are common and easy to rule out
            var definition = type.OriginalDefinition;
            switch (definition.SpecialType)
            {
                case SpecialType.System_Enum:
                case SpecialType.System_String:
                case SpecialType.System_Object:
                case SpecialType.System_Delegate:
                case SpecialType.System_MulticastDelegate:

                // We cannot use collection initializers in symbols of type `System.Array` (as opposed to actual
                // instantiations like `int[]`).
                case SpecialType.System_Array:

                // We cannot add to an enumerable or enumerator
                // so we cannot use a collection initializer
                case SpecialType.System_Collections_IEnumerable:
                case SpecialType.System_Collections_IEnumerator:
                case SpecialType.System_Collections_Generic_IEnumerable_T:
                case SpecialType.System_Collections_Generic_IEnumerator_T:
                    return false;
            }

            // - Delegate types have no settable members, which is the case for Delegate and MulticastDelegate too
            // - Non-settable struct members cannot be used in object initializers
            // - Pointers and function pointers do not have accessible members
            return type.TypeKind is not (TypeKind.Delegate or TypeKind.Struct or TypeKind.FunctionPointer or TypeKind.Pointer);
        }
    }
}

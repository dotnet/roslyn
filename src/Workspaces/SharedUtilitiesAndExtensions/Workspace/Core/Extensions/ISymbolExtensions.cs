// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    public static bool IsPartial(this ISymbol symbol)
    {
        var isPartial = symbol switch
        {
            IMethodSymbol method => method.PartialDefinitionPart != null || method.PartialImplementationPart != null,
            IPropertySymbol property => property.PartialDefinitionPart != null || property.PartialImplementationPart != null,
            _ => false
        };

        if (isPartial)
            return true;

#if !OLDER_ROSLYN
        return symbol is IEventSymbol @event &&
            (@event.PartialDefinitionPart != null || @event.PartialImplementationPart != null);
#else
        return false;
#endif
    }

    public static DeclarationModifiers GetSymbolModifiers(this ISymbol symbol)
    {
        return symbol.InferSafetyModifiersFromSymbol()
            .WithIsStatic(symbol.IsStatic)
            .WithIsAbstract(symbol.IsAbstract)
            .WithIsVirtual(symbol.IsVirtual)
            .WithIsOverride(symbol.IsOverride)
            .WithIsSealed(symbol.IsSealed)
            .WithIsRequired(symbol.IsRequired())
            .WithPartial(symbol.IsPartial());
    }

    /// <summary>
    /// Infers safety modifiers from a symbol using the compiler module's rules or stored generation rules.
    /// Legacy rules use pointer types in the signature; updated rules use the unsafe requirement
    /// and add safe only when an explicit safety annotation is required.
    /// <paramref name="hasBackingField"/> describes actual generated property/event storage;
    /// null uses available symbol information.
    /// </summary>
    public static DeclarationModifiers InferSafetyModifiersFromSymbol(this ISymbol symbol, bool? hasBackingField = null)
    {
        // Check unsafe v1.
        if (!symbol.UsesUpdatedMemorySafetyRules())
            return symbol.RequiresUnsafeModifier() ? DeclarationModifiers.Unsafe : DeclarationModifiers.None;

        // Check unsafe v2.
#if !OLDER_ROSLYN
        if (symbol.RequiresUnsafeContext)
            return DeclarationModifiers.Unsafe;

        if (RequiresExplicitSafetyModifier(symbol, hasBackingField))
            return DeclarationModifiers.Safe;
#endif

        return DeclarationModifiers.None;
    }

    public static bool UsesUpdatedMemorySafetyRules(this ISymbol symbol)
    {
#if !OLDER_ROSLYN
        if (symbol is CodeGenerationSymbol generated)
            return generated.UsesUpdatedMemorySafetyRules;

        var module = symbol as IModuleSymbol ?? symbol.ContainingModule;
        return module?.MemorySafetyRulesVersion is MemorySafetyRulesVersion.Version2;
#else
        return false;
#endif
    }

#if !OLDER_ROSLYN
    private static bool RequiresExplicitSafetyModifier(ISymbol symbol, bool? hasBackingField)
    {
        if (symbol is CodeGenerationSymbol generated)
        {
            // Copied properties/events can gain or lose backing storage when generated. Copies record whether
            // 'safe' is required only because of that storage, so we can adjust it to match the generated form.
            // Fresh symbols and copies with explicit replacement modifiers keep the caller's choice instead.
            var requiresSafeForBackingField = symbol switch
            {
                IPropertySymbol property => CodeGenerationPropertyInfo.GetRequiresSafeForBackingField(property),
                IEventSymbol @event => CodeGenerationEventInfo.GetRequiresSafeForBackingField(@event),
                _ => false,
            };
            if (requiresSafeForBackingField && hasBackingField is { } generatesStorage)
                return generatesStorage;

            return generated.Modifiers.IsSafe;
        }

        // Extern members require an explicit safety choice.
        if (symbol.IsExtern)
            return true;

        if (symbol.IsStatic)
            return false;

        // Auto-properties and field-like events carry the requirement for their hidden fields.
        var generatesField = symbol switch
        {
            IFieldSymbol field => !field.IsConst,
            IPropertySymbol { IsAbstract: false, IsIndexer: false } property
                => hasBackingField ?? property.GetBackingFieldIfAny() is not null,
            IEventSymbol { IsAbstract: false } @event
                => hasBackingField ?? @event.AddMethod?.IsImplicitlyDeclared == true,
            _ => false,
        };

        if (!generatesField || symbol.ContainingType is not { } containingType)
            return false;

        var layout = containingType.TypeLayout;
        // Empty source structs get an implicit size of 1, which disappears when storage is added.
        // Only an explicit source layout size (or a size read from metadata) requires an annotation.
        // Match System.Runtime.InteropServices.StructLayoutAttribute, not a class with the same name
        // nested inside another class. ContainingType refers to the attribute class here.
        var hasExplicitSize = layout.Size != 0 &&
            (containingType.DeclaringSyntaxReferences.IsEmpty ||
             containingType.GetAttributes().Any(attribute =>
                 attribute.AttributeClass is { MetadataName: nameof(StructLayoutAttribute), ContainingType: null } attributeType &&
                 attributeType.ContainingNamespace.ToDisplayString() == "System.Runtime.InteropServices"));
        // Extended layout is not named in all of the framework reference assemblies we target.
        const LayoutKind extendedLayout = (LayoutKind)1;
        return layout.Kind == LayoutKind.Explicit || layout.Kind == extendedLayout ||
            layout.PackingSize != 0 || hasExplicitSize;
    }

    // An override can declare just one accessor and inherit the other, so do not require both to be present.
    public static bool RequiresAccessorSafetyModifiers(this IPropertySymbol property)
        => property.UsesUpdatedMemorySafetyRules() && !property.RequiresUnsafeContext &&
           (property.GetMethod?.RequiresUnsafeContext == true || property.SetMethod?.RequiresUnsafeContext == true);
#endif

#if !OLDER_ROSLYN
    public static ISymbol? ReduceExtensionMember(this ISymbol? member, ITypeSymbol? receiverType)
    {
        if (member is null || receiverType is null)
            return null;

        return member switch
        {
            IPropertySymbol propertySymbol => propertySymbol.ReduceExtensionMember(receiverType),
            IMethodSymbol method => method.ReduceExtensionMember(receiverType),
            _ => null,
        };
    }
#endif
}

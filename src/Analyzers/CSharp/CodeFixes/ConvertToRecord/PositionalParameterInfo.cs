// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRecord;

/// <summary>
/// Represents a property that should be added as a positional parameter
/// </summary>
/// <param name="Declaration">Original declaration, null iff IsInherited is true
/// Null iff <see cref="IsInherited"/> is true</param>
/// <param name="Symbol">Symbol of the property</param>
/// <param name="KeepAsOverride">Whether we should keep the original declaration present</param>
internal record PositionalParameterInfo(
    PropertyDeclarationSyntax? Declaration,
    IPropertySymbol Symbol,
    bool KeepAsOverride)
{
    /// <summary>
    /// Whether this property is inherited from another base record
    /// </summary>
    [MemberNotNullWhen(false, nameof(Declaration))]
    public bool IsInherited => Declaration == null;

    public static ImmutableArray<PositionalParameterInfo> GetPropertiesForPositionalParameters(
        ImmutableArray<PropertyDeclarationSyntax> properties,
        INamedTypeSymbol type,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<PositionalParameterInfo>.GetInstance(out var resultBuilder);

        // get all declared property symbols, put inherited property symbols first
        var symbols = properties.SelectAsArray(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken));

        // add inherited properties from a potential base record first
        var inheritedProperties = GetInheritedPositionalParams(type, cancellationToken);
        resultBuilder.AddRange(inheritedProperties.Select(property => new PositionalParameterInfo(
            Declaration: null,
            property,
            KeepAsOverride: false)));

        // The user may not know about init or be converting code from before init was introduced.
        // In this case we can convert set properties to init ones
        var allowSetToInitConversion = !symbols
            .Any(symbol => symbol.SetMethod is IMethodSymbol { IsInitOnly: true });

        resultBuilder.AddRange(properties.Zip(symbols, (syntax, symbol)
            => ShouldConvertProperty(syntax, symbol, type) switch
            {
                ConvertStatus.DoNotConvert => null,
                ConvertStatus.Override
                    => new PositionalParameterInfo(syntax, symbol, KeepAsOverride: true),
                ConvertStatus.OverrideIfConvertingSetToInit
                    => new PositionalParameterInfo(syntax, symbol, !allowSetToInitConversion),
                ConvertStatus.AlwaysConvert
                    => new PositionalParameterInfo(syntax, symbol, KeepAsOverride: false),
                _ => throw ExceptionUtilities.Unreachable(),
            }).WhereNotNull());

        return resultBuilder.ToImmutable();
    }

    public static ImmutableArray<IPropertySymbol> GetInheritedPositionalParams(
        INamedTypeSymbol currentType,
        CancellationToken cancellationToken)
    {
        var baseType = currentType.BaseType;
        if (baseType != null && baseType.TryGetPrimaryConstructor(out var basePrimary))
        {
            return basePrimary.Parameters
                .Select(param => param.GetAssociatedSynthesizedRecordProperty(cancellationToken))
                .WhereNotNull()
                .AsImmutable();
        }

        return [];
    }

    /// <summary>
    /// for each property, say whether we can convert
    /// to primary constructor parameter or not (and whether it would imply changes)
    /// </summary>
    private enum ConvertStatus
    {
        /// <summary>
        /// no way we can convert this
        /// </summary>
        DoNotConvert,
        /// <summary>
        /// we can convert this because we feel it would be used in a primary constructor,
        /// but some accessibility is non-default and we want to override
        /// </summary>
        Override,
        /// <summary>
        /// we can convert this if we see that the user only ever uses set (not init)
        /// otherwise we should give an override
        /// </summary>
        OverrideIfConvertingSetToInit,
        /// <summary>
        /// we can convert this without changing the meaning 
        /// </summary>
        AlwaysConvert
    }

    private static ConvertStatus ShouldConvertProperty(
        PropertyDeclarationSyntax property,
        IPropertySymbol propertySymbol,
        INamedTypeSymbol containingType)
    {
        // properties with identifiers or expression bodies are too complex to move.
        // unimplemented or static properties shouldn't be in a constructor.
        if (property.Initializer != null ||
            property.ExpressionBody != null ||
            propertySymbol.IsAbstract ||
            propertySymbol.IsStatic)
        {
            return ConvertStatus.DoNotConvert;
        }

        // more restrictive than internal (protected, private, private protected, or unspecified (private by default)).
        // We allow internal props to be converted to public auto-generated ones
        // because it's still as accessible as a constructor would be from outside the class.
        if (propertySymbol.DeclaredAccessibility < Accessibility.Internal)
        {
            return ConvertStatus.DoNotConvert;
        }

        // no accessors declared
        if (property.AccessorList == null || property.AccessorList.Accessors.IsEmpty())
        {
            return ConvertStatus.DoNotConvert;
        }

        // When we convert to primary constructor parameters, the auto-generated properties have default behavior
        // Here are the cases where we wouldn't substantially change default behavior
        // - No accessors can have any explicit implementation or modifiers
        //   - This is because it would indicate complex functionality or explicit hiding which is not default
        // - class records and readonly struct records must have:
        //   - public get accessor
        //   - optionally a public init accessor
        //     - note: if this is not provided the user can still initialize the property in the constructor,
        //             so it's like init but without the user ability to initialize outside the constructor
        // - for non-readonly structs, we must have:
        //   - public get accessor
        //   - public set accessor
        // If the user has a private/protected set method, it could still make sense to be a primary constructor
        // but we should provide the override in case the user sets the property from within the class
        var getAccessor = propertySymbol.GetMethod;
        var setAccessor = propertySymbol.SetMethod;
        var accessors = property.AccessorList.Accessors;
        if (accessors.Any(a => a.Body != null || a.ExpressionBody != null) ||
            getAccessor == null ||
            // private get means they probably don't want it seen from the constructor
            getAccessor.DeclaredAccessibility < Accessibility.Internal)
        {
            return ConvertStatus.DoNotConvert;
        }

        // we consider a internal (by default) get on an internal property as public
        // but if the user specifically declares a more restrictive accessibility
        // it would indicate they want to keep it safer than the rest of the property
        // and we should respect that
        if (getAccessor.DeclaredAccessibility < propertySymbol.DeclaredAccessibility)
        {
            return ConvertStatus.Override;
        }

        if (containingType.TypeKind == TypeKind.Struct && !containingType.IsReadOnly)
        {
            // in a struct, our default is to have a public set
            // but anything else we can still convert and override
            if (setAccessor == null)
            {
                return ConvertStatus.Override;
            }

            // if the user had their property as internal then we are fine with completely moving
            // an internal (by default) set method, but if they explicitly mark the set as internal
            // while the property is public we want to keep that behavior
            if (setAccessor.DeclaredAccessibility != Accessibility.Public &&
                    setAccessor.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
            {
                return ConvertStatus.Override;
            }

            if (setAccessor.IsInitOnly)
            {
                return ConvertStatus.Override;
            }
        }
        else
        {
            // either we are a class or readonly struct, the default is no set or init only set
            if (setAccessor != null)
            {
                if (setAccessor.DeclaredAccessibility != Accessibility.Public &&
                    setAccessor.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                {
                    return ConvertStatus.Override;
                }

                if (!setAccessor.IsInitOnly)
                {
                    return ConvertStatus.OverrideIfConvertingSetToInit;
                }
            }
        }

        return ConvertStatus.AlwaysConvert;
    }
}

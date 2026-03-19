// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Analyzer.Utilities.Extensions
{
    internal static class ITypeSymbolExtensions
    {
        public static bool IsPrimitiveType(this ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Boolean
                or SpecialType.System_Byte
                or SpecialType.System_Char
                or SpecialType.System_Double
                or SpecialType.System_Int16
                or SpecialType.System_Int32
                or SpecialType.System_Int64
                or SpecialType.System_UInt16
                or SpecialType.System_UInt32
                or SpecialType.System_UInt64
                or SpecialType.System_SByte
                or SpecialType.System_Single
                or SpecialType.System_String => true,
                _ => false,
            };
        }

        public static bool Inherits([NotNullWhen(returnValue: true)] this ITypeSymbol? type, [NotNullWhen(returnValue: true)] ITypeSymbol? possibleBase)
        {
            if (type == null || possibleBase == null)
            {
                return false;
            }

            switch (possibleBase.TypeKind)
            {
                case TypeKind.Class:
                    if (type.TypeKind == TypeKind.Interface)
                    {
                        return false;
                    }

                    return DerivesFrom(type, possibleBase, baseTypesOnly: true);

                case TypeKind.Interface:
                    return DerivesFrom(type, possibleBase);

                default:
                    return false;
            }
        }

        public static bool DerivesFrom([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol, [NotNullWhen(returnValue: true)] ITypeSymbol? candidateBaseType, bool baseTypesOnly = false, bool checkTypeParameterConstraints = true)
        {
            if (candidateBaseType == null || symbol == null)
            {
                return false;
            }

            if (!baseTypesOnly && candidateBaseType.TypeKind == TypeKind.Interface)
            {
                var allInterfaces = symbol.AllInterfaces.OfType<ITypeSymbol>();
                if (SymbolEqualityComparer.Default.Equals(candidateBaseType.OriginalDefinition, candidateBaseType))
                {
                    // Candidate base type is not a constructed generic type, so use original definition for interfaces.
                    allInterfaces = allInterfaces.Select(i => i.OriginalDefinition);
                }

                if (allInterfaces.Contains(candidateBaseType))
                {
                    return true;
                }
            }

            if (checkTypeParameterConstraints && symbol.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameterSymbol = (ITypeParameterSymbol)symbol;
                foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
                {
                    if (constraintType.DerivesFrom(candidateBaseType, baseTypesOnly, checkTypeParameterConstraints))
                    {
                        return true;
                    }
                }
            }

            while (symbol != null)
            {
                if (SymbolEqualityComparer.Default.Equals(symbol, candidateBaseType))
                {
                    return true;
                }

                symbol = symbol.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Indicates if the given <paramref name="type"/> is disposable,
        /// and thus can be used in a <code>using</code> or <code>await using</code> statement.
        /// </summary>
        public static bool IsDisposable(this ITypeSymbol type,
            INamedTypeSymbol? iDisposable,
            INamedTypeSymbol? iAsyncDisposable,
            INamedTypeSymbol? configuredAsyncDisposable)
        {
            if (type.IsReferenceType)
            {
                return IsInterfaceOrImplementsInterface(type, iDisposable)
                    || IsInterfaceOrImplementsInterface(type, iAsyncDisposable);
            }
            else if (SymbolEqualityComparer.Default.Equals(type, configuredAsyncDisposable))
            {
                return true;
            }

            if (type.IsRefLikeType)
            {
                return type.GetMembers("Dispose").OfType<IMethodSymbol>()
                    .Any(method => method.HasDisposeSignatureByConvention());
            }

            return false;

            static bool IsInterfaceOrImplementsInterface(ITypeSymbol type, INamedTypeSymbol? interfaceType)
                => interfaceType != null &&
                   (SymbolEqualityComparer.Default.Equals(type, interfaceType) || type.AllInterfaces.Contains(interfaceType));
        }

        /// <summary>
        /// Gets all attributes directly applied to the type or inherited from a base type.
        /// </summary>
        /// <param name="type">The type symbol.</param>
        /// <param name="attributeUsageAttribute">The compilation symbol for <see cref="AttributeUsageAttribute"/>.</param>
        public static IEnumerable<AttributeData> GetApplicableAttributes(this INamedTypeSymbol type, INamedTypeSymbol? attributeUsageAttribute)
        {
            var attributes = new List<AttributeData>();
            var onlyIncludeInherited = false;

            while (type != null)
            {
                var current = type.GetAttributes();
                if (!onlyIncludeInherited || attributeUsageAttribute is null)
                {
                    attributes.AddRange(current);
                }
                else
                {
                    foreach (var attribute in current)
                    {
                        if (!IsInheritedAttribute(attribute, attributeUsageAttribute))
                        {
                            continue;
                        }

                        attributes.Add(attribute);
                    }
                }

                type = type.BaseType;
                onlyIncludeInherited = true;
            }

            return attributes;

            // Local functions
            static bool IsInheritedAttribute(AttributeData attributeData, INamedTypeSymbol attributeUsageAttribute)
            {
                for (var currentAttributeClass = attributeData.AttributeClass;
                    currentAttributeClass is object;
                    currentAttributeClass = currentAttributeClass.BaseType)
                {
                    foreach (var attributeClassData in currentAttributeClass.GetAttributes())
                    {
                        if (!SymbolEqualityComparer.Default.Equals(attributeClassData.AttributeClass, attributeUsageAttribute))
                        {
                            continue;
                        }

                        foreach (var (name, typedConstant) in attributeClassData.NamedArguments)
                        {
                            if (name != nameof(AttributeUsageAttribute.Inherited))
                            {
                                continue;
                            }

                            // The default is true, so use that when explicitly specified and for cases where the value
                            // is not a boolean (i.e. compilation error scenarios).
                            return !Equals(false, typedConstant.Value);
                        }

                        // [AttributeUsage] was found, but did not specify Inherited explicitly. The default is true.
                        return true;
                    }
                }

                // [AttributeUsage] was not found. The default is true.
                return true;
            }
        }

        public static IEnumerable<AttributeData> GetApplicableExportAttributes(this INamedTypeSymbol? type, INamedTypeSymbol? exportAttributeV1, INamedTypeSymbol? exportAttributeV2, INamedTypeSymbol? inheritedExportAttribute)
        {
            var attributes = new List<AttributeData>();
            var onlyIncludeInherited = false;

            while (type != null)
            {
                var current = type.GetAttributes();
                foreach (var attribute in current)
                {
                    if (attribute.AttributeClass.Inherits(inheritedExportAttribute))
                    {
                        attributes.Add(attribute);
                    }
                    else if (!onlyIncludeInherited &&
                        (attribute.AttributeClass.Inherits(exportAttributeV1) || attribute.AttributeClass.Inherits(exportAttributeV2)))
                    {
                        attributes.Add(attribute);
                    }
                }

                if (inheritedExportAttribute is null)
                {
                    break;
                }

                type = type.BaseType;
                onlyIncludeInherited = true;
            }

            return attributes;
        }

        public static bool HasValueCopySemantics(this ITypeSymbol typeSymbol)
            => typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_String;

        public static bool CanHoldNullValue([NotNullWhen(returnValue: true)] this ITypeSymbol? typeSymbol)
            => typeSymbol.IsReferenceTypeOrNullableValueType() ||
               typeSymbol?.IsRefLikeType == true ||
               typeSymbol is ITypeParameterSymbol typeParameter && !typeParameter.IsValueType;

        public static bool IsNullableValueType([NotNullWhen(returnValue: true)] this ITypeSymbol? typeSymbol)
            => typeSymbol != null && typeSymbol.IsValueType && typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        public static bool IsReferenceTypeOrNullableValueType([NotNullWhen(returnValue: true)] this ITypeSymbol? typeSymbol)
            => typeSymbol != null && (typeSymbol.IsReferenceType || typeSymbol.IsNullableValueType());

        public static bool IsNullableOfBoolean([NotNullWhen(returnValue: true)] this ITypeSymbol? typeSymbol)
            => typeSymbol.IsNullableValueType() && ((INamedTypeSymbol)typeSymbol).TypeArguments[0].SpecialType == SpecialType.System_Boolean;

        public static ITypeSymbol? GetUnderlyingValueTupleTypeOrThis(this ITypeSymbol? typeSymbol)
            => (typeSymbol as INamedTypeSymbol)?.TupleUnderlyingType ?? typeSymbol;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable RS1024 // Use 'SymbolEqualityComparer' when comparing symbols (https://github.com/dotnet/roslyn/issues/78583)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    public const string DefaultParameterName = "value";

    extension([NotNullWhen(true)] ITypeSymbol? type)
    {
        public bool IsIntegralType()
        => type?.SpecialType.IsIntegralType() == true;

        public bool IsSignedIntegralType()
            => type?.SpecialType.IsSignedIntegralType() == true;

        public bool CanAddNullCheck()
        {
            if (type == null)
                return false;

            var isNullableValueType = type.IsNullable();
            var isNonNullableReferenceType = type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated;

            return isNullableValueType || isNonNullableReferenceType;
        }

        public bool IsNumericType()
        {
            if (type != null)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_IntPtr when type.IsNativeIntegerType:
                    case SpecialType.System_UIntPtr when type.IsNativeIntegerType:
                        return true;
                }
            }

            return false;
        }

        public bool IsOrDerivesFromExceptionType(Compilation compilation)
        {
            if (type != null)
            {
                switch (type.Kind)
                {
                    case SymbolKind.NamedType:
                        foreach (var baseType in type.GetBaseTypesAndThis())
                        {
                            if (baseType.Equals(compilation.ExceptionType()))
                            {
                                return true;
                            }
                        }

                        break;

                    case SymbolKind.TypeParameter:
                        foreach (var constraint in ((ITypeParameterSymbol)type).ConstraintTypes)
                        {
                            if (constraint.IsOrDerivesFromExceptionType(compilation))
                            {
                                return true;
                            }
                        }

                        break;
                }
            }

            return false;
        }

        public bool IsEnumType()
            => IsEnumType(type, out _);

        public bool IsEnumType([NotNullWhen(true)] out INamedTypeSymbol? enumType)
        {
            if (type != null && type.IsValueType && type.TypeKind == TypeKind.Enum)
            {
                enumType = (INamedTypeSymbol)type;
                return true;
            }

            enumType = null;
            return false;
        }

        public bool IsDisposable([NotNullWhen(returnValue: true)] ITypeSymbol? iDisposableType)
            => iDisposableType != null &&
               (Equals(iDisposableType, type) ||
                type?.AllInterfaces.Contains(iDisposableType) == true);

        public bool IsSpanOrReadOnlySpan()
            => type.IsSpan() || type.IsReadOnlySpan();

        public bool IsSpan()
            => type is INamedTypeSymbol
            {
                Name: nameof(Span<>),
                TypeArguments.Length: 1,
                ContainingNamespace: { Name: nameof(System), ContainingNamespace.IsGlobalNamespace: true }
            };

        public bool IsInlineArray()
            => type is INamedTypeSymbol namedType &&
               namedType.OriginalDefinition.GetAttributes().Any(static a => a.AttributeClass?.SpecialType == SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute);
    }

    extension(ITypeSymbol type)
    {
        public ImmutableArray<INamedTypeSymbol> GetAllInterfacesIncludingThis()
        {
            var allInterfaces = type.AllInterfaces;
            return type is INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType && !allInterfaces.Contains(namedType)
                ? [namedType, .. allInterfaces]
                : allInterfaces;
        }

        private HashSet<INamedTypeSymbol> GetOriginalInterfacesAndTheirBaseInterfaces(
            HashSet<INamedTypeSymbol>? symbols = null)
        {
            symbols ??= new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            foreach (var interfaceType in type.Interfaces)
            {
                symbols.Add(interfaceType.OriginalDefinition);
                symbols.AddRange(interfaceType.AllInterfaces.Select(i => i.OriginalDefinition));
            }

            return symbols;
        }

        public IEnumerable<INamedTypeSymbol> GetContainingTypes()
        {
            var current = type.ContainingType;
            while (current != null)
            {
                yield return current;
                current = current.ContainingType;
            }
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types, optionally including interfaces,
        // dealing only with original types.
        public bool InheritsFromOrEquals(
    ITypeSymbol baseType, bool includeInterfaces)
        {
            if (!includeInterfaces)
            {
                return InheritsFromOrEquals(type, baseType);
            }

            return type.GetBaseTypesAndThis().Concat(type.AllInterfaces).Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, baseType));
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types and interfaces, dealing
        // only with original types.
        public bool InheritsFromOrEquals(
    ITypeSymbol baseType)
        {
            return type.GetBaseTypesAndThis().Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, baseType));
        }

        // Determine if "type" inherits from or implements "baseType", ignoring constructed types, and dealing
        // only with original types.
        public bool InheritsFromOrImplementsOrEqualsIgnoringConstruction(
    ITypeSymbol baseType)
        {
            var originalBaseType = baseType.OriginalDefinition;
            type = type.OriginalDefinition;

            if (SymbolEquivalenceComparer.Instance.Equals(type, originalBaseType))
            {
                return true;
            }

            IEnumerable<ITypeSymbol> baseTypes = (baseType.TypeKind == TypeKind.Interface) ? type.AllInterfaces : type.GetBaseTypes();
            return baseTypes.Contains(t => SymbolEquivalenceComparer.Instance.Equals(t.OriginalDefinition, originalBaseType));
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types, and dealing
        // only with original types.
        public bool InheritsFromIgnoringConstruction(
    ITypeSymbol baseType)
        {
            var originalBaseType = baseType.OriginalDefinition;

            // We could just call GetBaseTypes and foreach over it, but this
            // is a hot path in Find All References. This avoid the allocation
            // of the enumerator type.
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                if (SymbolEquivalenceComparer.Instance.Equals(currentBaseType.OriginalDefinition, originalBaseType))
                {
                    return true;
                }

                currentBaseType = currentBaseType.BaseType;
            }

            return false;
        }

        public bool ImplementsIgnoringConstruction(
    ITypeSymbol interfaceType)
        {
            var originalInterfaceType = interfaceType.OriginalDefinition;
            return type.AllInterfaces.Any(static (t, originalInterfaceType) => SymbolEquivalenceComparer.Instance.Equals(t.OriginalDefinition, originalInterfaceType), originalInterfaceType);
        }

        public bool Implements(
    ITypeSymbol interfaceType)
        {
            return type.AllInterfaces.Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, interfaceType));
        }

        public string CreateParameterName(bool capitalize = false)
        {
            while (true)
            {
                switch (type)
                {
                    case IArrayTypeSymbol arrayType:
                        type = arrayType.ElementType;
                        continue;
                    case IPointerTypeSymbol pointerType:
                        type = pointerType.PointedAtType;
                        continue;
                }

                break;
            }

            var shortName = GetParameterName(type);
            return capitalize ? shortName.ToPascalCase() : shortName.ToCamelCase();
        }

        public bool? IsMutableValueType()
        {
            if (type.IsNullable(out var underlyingType))
            {
                // Nullable<T> can only be mutable if T is mutable. This case ensures types like 'int?' are treated as
                // immutable.
                type = underlyingType;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return false;

                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return false;

                case SpecialType.System_DateTime:
                    return false;

                default:
                    break;
            }

            // Special case certain structs that we know for certain are immutable, but which may have not been marked that
            // way (especially in older frameworks before we had the `readonly struct` feature).
            if (IsWellKnownImmutableValueType(type))
                return false;

            // An error type may or may not be a struct, and it may or may not be mutable.  As we cannot make a determination,
            // we return null to allow the caller to decide what to do.
            if (type.IsErrorType())
                return null;

            if (type.TypeKind != TypeKind.Struct)
                return false;

            if (type.IsReadOnly)
                return false;

            var hasPrivateField = false;
            foreach (var member in type.GetMembers())
            {
                if (member is not IFieldSymbol fieldSymbol)
                    continue;

                if (!fieldSymbol.IsConst && !fieldSymbol.IsReadOnly && !fieldSymbol.IsStatic)
                    return true;

                hasPrivateField |= fieldSymbol.DeclaredAccessibility == Accessibility.Private;
            }

            if (!hasPrivateField)
            {
                // Some reference assemblies omit information about private fields. If we can't be sure the field is
                // immutable, treat it as potentially mutable.
                foreach (var attributeData in type.ContainingAssembly.GetAttributes())
                {
                    if (attributeData.AttributeClass?.Name == nameof(ReferenceAssemblyAttribute)
                        && attributeData.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName)
                    {
                        return null;
                    }
                }
            }

            return false;

            static bool IsWellKnownImmutableValueType(ITypeSymbol type)
            {
                // We know that these types are immutable, even if they don't have the IsReadOnly attribute.
                if (type is not INamedTypeSymbol
                    {
                        ContainingNamespace:
                        {
                            Name: nameof(System),
                            ContainingNamespace.IsGlobalNamespace: true
                        }
                    })
                {
                    return false;
                }

                if (type.Name
                        is nameof(DateTime)
                        or nameof(ArraySegment<>)
                        or nameof(DateTimeOffset)
                        or nameof(Guid)
                        or nameof(Index)
                        or nameof(Range)
                        or nameof(ReadOnlyMemory<>)
                        or nameof(ReadOnlySpan<>)
                        or nameof(TimeSpan))
                {
                    return true;
                }

                return false;
            }
        }

        public ITypeSymbol WithNullableAnnotationFrom(ITypeSymbol symbolForNullableAnnotation)
            => type.WithNullableAnnotation(symbolForNullableAnnotation.NullableAnnotation);
    }

    extension([NotNullWhen(true)] ITypeSymbol? symbol)
    {
        public bool IsAbstractClass()
        => symbol?.TypeKind == TypeKind.Class && symbol.IsAbstract;

        public bool IsSystemVoid()
            => symbol?.SpecialType == SpecialType.System_Void;

        public bool IsNullable()
            => symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        public bool IsNonNullableValueType()
            => symbol is { IsValueType: true } && !symbol.IsNullable();

        public bool IsNullable(
            [NotNullWhen(true)] out ITypeSymbol? underlyingType)
        {
            if (IsNullable(symbol))
            {
                underlyingType = ((INamedTypeSymbol)symbol).TypeArguments[0];
                return true;
            }

            underlyingType = null;
            return false;
        }

        public bool IsModuleType()
            => symbol?.TypeKind == TypeKind.Module;

        public bool IsInterfaceType()
            => symbol?.TypeKind == TypeKind.Interface;

        public bool IsDelegateType()
            => symbol?.TypeKind == TypeKind.Delegate;

        public bool IsFunctionPointerType()
            => symbol?.TypeKind == TypeKind.FunctionPointer;

        public bool IsStructType()
            => symbol?.TypeKind == TypeKind.Struct;

        public bool IsFormattableStringOrIFormattable()
        {
            return symbol?.MetadataName is nameof(FormattableString) or nameof(IFormattable)
                && symbol.ContainingType == null
                && symbol.ContainingNamespace?.Name == "System"
                && symbol.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
        }

        public bool ContainsAnonymousType()
        {
            return symbol switch
            {
                IArrayTypeSymbol a => ContainsAnonymousType(a.ElementType),
                IPointerTypeSymbol p => ContainsAnonymousType(p.PointedAtType),
                INamedTypeSymbol n => ContainsAnonymousType(n),
                _ => false,
            };
        }

        public bool IsSpecialType()
        {
            if (symbol != null)
            {
                switch (symbol.SpecialType)
                {
                    case SpecialType.System_Object:
                    case SpecialType.System_Void:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Char:
                    case SpecialType.System_String:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_IntPtr when symbol.IsNativeIntegerType:
                    case SpecialType.System_UIntPtr when symbol.IsNativeIntegerType:
                        return true;
                }
            }

            return false;
        }
    }

    extension([NotNullWhen(true)] INamedTypeSymbol? symbol)
    {
        public bool IsAnonymousType()
        => symbol?.IsAnonymousType == true;
    }

    extension(ITypeSymbol? type)
    {
        public IEnumerable<ITypeSymbol> GetBaseTypesAndThis()
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public IEnumerable<INamedTypeSymbol> GetBaseTypes()
        {
            var current = type?.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public IEnumerable<ITypeSymbol> GetContainingTypesAndThis()
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.ContainingType;
            }
        }

        [return: NotNullIfNotNull(parameterName: nameof(type))]
        public ITypeSymbol? RemoveUnavailableTypeParameters(
            Compilation compilation,
            IEnumerable<ITypeParameterSymbol> availableTypeParameters)
        {
            return type?.RemoveUnavailableTypeParameters(compilation, availableTypeParameters.Select(t => t.Name).ToSet());
        }

        [return: NotNullIfNotNull(parameterName: nameof(type))]
        private ITypeSymbol? RemoveUnavailableTypeParameters(
            Compilation compilation,
            ISet<string> availableTypeParameterNames)
        {
            return type?.Accept(new UnavailableTypeParameterRemover(compilation, availableTypeParameterNames));
        }

        [return: NotNullIfNotNull(parameterName: nameof(type))]
        public ITypeSymbol? RemoveAnonymousTypes(
            Compilation compilation)
        {
            return type?.Accept(new AnonymousTypeRemover(compilation));
        }

        [return: NotNullIfNotNull(parameterName: nameof(type))]
        public ITypeSymbol? RemoveUnnamedErrorTypes(
            Compilation compilation)
        {
            return type?.Accept(new UnnamedErrorTypeRemover(compilation));
        }

        public void AddReferencedMethodTypeParameters(
    ArrayBuilder<ITypeParameterSymbol> result)
        {
            AddReferencedTypeParameters(type, result, onlyMethodTypeParameters: true);
        }

        public void AddReferencedTypeParameters(
    ArrayBuilder<ITypeParameterSymbol> result)
        {
            AddReferencedTypeParameters(type, result, onlyMethodTypeParameters: false);
        }

        private void AddReferencedTypeParameters(
    ArrayBuilder<ITypeParameterSymbol> result, bool onlyMethodTypeParameters)
        {
            if (type != null)
            {
                using var collector = new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters);
                type.Accept(collector);
            }
        }

        public IList<ITypeParameterSymbol> GetReferencedTypeParameters()
        {
            using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var result);
            AddReferencedTypeParameters(type, result);
            return result.ToList();
        }

        [return: NotNullIfNotNull(parameterName: nameof(type))]
        public ITypeSymbol? SubstituteTypes<TType1, TType2>(
            IDictionary<TType1, TType2> mapping,
            Compilation compilation)
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            return type.SubstituteTypes(mapping, new CompilationTypeGenerator(compilation));
        }

        [return: NotNullIfNotNull(parameterName: nameof(type))]
        public ITypeSymbol? SubstituteTypes<TType1, TType2>(
            IDictionary<TType1, TType2> mapping,
            ITypeGenerator typeGenerator)
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            return type?.Accept(new SubstituteTypesVisitor<TType1, TType2>(mapping, typeGenerator));
        }
    }

    extension(ITypeSymbol symbol)
    {
        public bool IsAttribute()
        {
            for (var b = symbol.BaseType; b != null; b = b.BaseType)
            {
                if (b.MetadataName == "Attribute" &&
                    b.ContainingType == null &&
                    b.ContainingNamespace != null &&
                    b.ContainingNamespace.Name == "System" &&
                    b.ContainingNamespace.ContainingNamespace != null &&
                    b.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
                {
                    return true;
                }
            }

            return false;
        }
    }

    extension(ITypeSymbol typeSymbol)
    {
        public bool IsUnexpressibleTypeParameterConstraint(
bool allowDelegateAndEnumConstraints = false)
        {
            if (typeSymbol.IsSealed || typeSymbol.IsValueType)
            {
                return true;
            }

            switch (typeSymbol.TypeKind)
            {
                case TypeKind.Array:
                case TypeKind.Delegate:
                    return true;
            }

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Array or SpecialType.System_ValueType:
                    return true;

                case SpecialType.System_Delegate:
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_Enum:
                    return !allowDelegateAndEnumConstraints;
            }

            return false;
        }

        public Accessibility DetermineMinimalAccessibility()
            => typeSymbol.Accept(MinimalAccessibilityVisitor.Instance);

        public bool CanSupportCollectionInitializer(ISymbol within)
        {
            return
                typeSymbol.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_Collections_IEnumerable) &&
                typeSymbol.GetBaseTypesAndThis()
                    .Union(typeSymbol.GetOriginalInterfacesAndTheirBaseInterfaces())
                    .SelectAccessibleMembers<IMethodSymbol>(WellKnownMemberNames.CollectionInitializerAddMethodName, within ?? typeSymbol)
                    .OfType<IMethodSymbol>()
                    .Any(m => m.Parameters.Any());
        }
    }

    private static bool ContainsAnonymousType(INamedTypeSymbol type)
    {
        if (type.IsAnonymousType)
            return true;

        foreach (var typeArg in type.GetAllTypeArguments())
        {
            if (ContainsAnonymousType(typeArg))
                return true;
        }

        return false;
    }

    private static string GetParameterName(ITypeSymbol? type)
    {
        if (type == null || type.IsAnonymousType() || type.IsTupleType)
        {
            return DefaultParameterName;
        }

        var shortName = type.GetShortName();
        return shortName.Length == 0
            ? DefaultParameterName
            : shortName;
    }

    extension(ITypeSymbol? typeSymbol)
    {
        public INamedTypeSymbol? GetDelegateType(Compilation compilation)
        {
            if (typeSymbol != null)
            {
                var expressionOfT = compilation.ExpressionOfTType();
                if (typeSymbol.OriginalDefinition.Equals(expressionOfT))
                {
                    var typeArgument = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
                    return typeArgument as INamedTypeSymbol;
                }

                if (typeSymbol.IsDelegateType())
                {
                    return typeSymbol as INamedTypeSymbol;
                }
            }

            return null;
        }
    }

    extension(ITypeSymbol containingType)
    {
        public IEnumerable<T> GetAccessibleMembersInBaseTypes<T>(ISymbol within) where T : class, ISymbol
        {
            if (containingType == null)
                return [];

            var types = containingType.GetBaseTypes();
            return types.SelectMany(x => x.GetMembers().OfType<T>().Where(m => m.IsAccessibleWithin(within)));
        }
    }

    extension(ITypeSymbol? containingType)
    {
        public ImmutableArray<T> GetAccessibleMembersInThisAndBaseTypes<T>(ISymbol within) where T : class, ISymbol
        {
            if (containingType == null)
            {
                return [];
            }

            return [.. containingType.GetBaseTypesAndThis().SelectAccessibleMembers<T>(within)];
        }

        public ImmutableArray<T> GetAccessibleMembersInThisAndBaseTypes<T>(string memberName, ISymbol within) where T : class, ISymbol
        {
            if (containingType == null)
            {
                return [];
            }

            return [.. containingType.GetBaseTypesAndThis().SelectAccessibleMembers<T>(memberName, within)];
        }
    }

    extension(IList<ITypeSymbol> t1)
    {
        public bool? AreMoreSpecificThan(IList<ITypeSymbol> t2)
        {
            if (t1.Count != t2.Count)
            {
                return null;
            }

            // For t1 to be more specific than t2, it has to be not less specific in every member,
            // and more specific in at least one.

            bool? result = null;
            for (var i = 0; i < t1.Count; ++i)
            {
                var r = t1[i].IsMoreSpecificThan(t2[i]);
                if (r == null)
                {
                    // We learned nothing. Do nothing.
                }
                else if (result == null)
                {
                    // We have found the first more specific type. See if
                    // all the rest on this side are not less specific.
                    result = r;
                }
                else if (result != r)
                {
                    // We have more specific types on both left and right, so we
                    // cannot succeed in picking a better type list. Bail out now.
                    return null;
                }
            }

            return result;
        }
    }

    extension(IEnumerable<ITypeSymbol>? types)
    {
        public IEnumerable<T> SelectAccessibleMembers<T>(ISymbol within) where T : class, ISymbol
        {
            if (types == null)
            {
                return [];
            }

            return types.SelectMany(x => x.GetMembers().OfType<T>().Where(m => m.IsAccessibleWithin(within)));
        }

        private IEnumerable<T> SelectAccessibleMembers<T>(string memberName, ISymbol within) where T : class, ISymbol
        {
            if (types == null)
            {
                return [];
            }

            return types.SelectMany(x => x.GetMembers(memberName).OfType<T>().Where(m => m.IsAccessibleWithin(within)));
        }
    }

    extension(ITypeSymbol t1)
    {
        private bool? IsMoreSpecificThan(ITypeSymbol t2)
        {
            // SPEC: A type parameter is less specific than a non-type parameter.

            var isTypeParameter1 = t1 is ITypeParameterSymbol;
            var isTypeParameter2 = t2 is ITypeParameterSymbol;

            if (isTypeParameter1 && !isTypeParameter2)
            {
                return false;
            }

            if (!isTypeParameter1 && isTypeParameter2)
            {
                return true;
            }

            if (isTypeParameter1)
            {
                Debug.Assert(isTypeParameter2);
                return null;
            }

            if (t1.TypeKind != t2.TypeKind)
            {
                return null;
            }

            // There is an identity conversion between the types and they are both substitutions on type parameters.
            // They had better be the same kind.

            // UNDONE: Strip off the dynamics.

            // SPEC: An array type is more specific than another
            // SPEC: array type (with the same number of dimensions)
            // SPEC: if the element type of the first is
            // SPEC: more specific than the element type of the second.

            if (t1 is IArrayTypeSymbol arr1)
            {
                var arr2 = (IArrayTypeSymbol)t2;

                // We should not have gotten here unless there were identity conversions
                // between the two types.

                return arr1.ElementType.IsMoreSpecificThan(arr2.ElementType);
            }

            // SPEC EXTENSION: We apply the same rule to pointer types.

            if (t1 is IPointerTypeSymbol)
            {
                var p1 = (IPointerTypeSymbol)t1;
                var p2 = (IPointerTypeSymbol)t2;
                return p1.PointedAtType.IsMoreSpecificThan(p2.PointedAtType);
            }

            // SPEC: A constructed type is more specific than another
            // SPEC: constructed type (with the same number of type arguments) if at least one type
            // SPEC: argument is more specific and no type argument is less specific than the
            // SPEC: corresponding type argument in the other.

            var n2 = t2 as INamedTypeSymbol;

            if (t1 is not INamedTypeSymbol n1)
            {
                return null;
            }

            // We should not have gotten here unless there were identity conversions between the
            // two types.

            var allTypeArgs1 = n1.GetAllTypeArguments();
            var allTypeArgs2 = n2.GetAllTypeArguments();

            return allTypeArgs1.AreMoreSpecificThan(allTypeArgs2);
        }
    }

    extension(ITypeSymbol? symbol)
    {
        [return: NotNullIfNotNull(parameterName: nameof(symbol))]
        public ITypeSymbol? RemoveNullableIfPresent()
        {
            if (symbol.IsNullable(out var underlyingType))
            {
                return underlyingType;
            }

            return symbol;
        }
    }

    extension([NotNullWhen(true)] ISymbol? symbol)
    {
        public bool IsReadOnlySpan()
        => symbol is INamedTypeSymbol
        {
            Name: nameof(ReadOnlySpan<>),
            TypeArguments.Length: 1,
            ContainingNamespace: { Name: nameof(System), ContainingNamespace.IsGlobalNamespace: true }
        };
    }
}

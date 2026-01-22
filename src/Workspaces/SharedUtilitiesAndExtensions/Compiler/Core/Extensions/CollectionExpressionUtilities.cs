// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class CollectionExpressionUtilities
{
    public static bool IsWellKnownCollectionInterface(ITypeSymbol type)
        => IsWellKnownCollectionReadOnlyInterface(type) || IsWellKnownCollectionReadWriteInterface(type);

    public static bool IsWellKnownCollectionReadOnlyInterface(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType
            is SpecialType.System_Collections_Generic_IEnumerable_T
            or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
            or SpecialType.System_Collections_Generic_IReadOnlyList_T;
    }

    public static bool IsWellKnownCollectionReadWriteInterface(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType
            is SpecialType.System_Collections_Generic_ICollection_T
            or SpecialType.System_Collections_Generic_IList_T;
    }

    public static bool IsConstructibleCollectionType(
        Compilation compilation,
        [NotNullWhen(true)] ITypeSymbol? type)
    {
        return IsConstructibleCollectionType(compilation, type, out _);
    }

    public static bool IsConstructibleCollectionType(
        Compilation compilation,
        [NotNullWhen(true)] ITypeSymbol? type,
        [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (type is null)
        {
            elementType = null;
            return false;
        }

        // Arrays are always a valid collection expression type.
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        // Has to be a real named type at this point.
        if (type is INamedTypeSymbol namedType)
        {
            // Span<T> and ReadOnlySpan<T> are always valid collection expression types.
            if (namedType.OriginalDefinition.Equals(compilation.SpanOfTType()) ||
                namedType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType()))
            {
                elementType = namedType.TypeArguments.Single();
                return true;
            }

            var ienumerableOfTType = compilation.IEnumerableOfTType();
            var ienumerableType = compilation.IEnumerableType();
            var foundType =
                namedType.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.Equals(ienumerableOfTType)) ??
                namedType.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.Equals(ienumerableType));
            elementType = foundType?.TypeArguments.FirstOrDefault() ?? compilation.ObjectType;

            // If it has a [CollectionBuilder] attribute on it, it is a valid collection expression type.
            var collectionBuilderMethods = TryGetCollectionBuilderFactoryMethods(
                compilation, namedType);
            if (collectionBuilderMethods is [var builderMethod, ..])
                return true;

            if (IsWellKnownCollectionInterface(namedType))
                return true;

            // At this point, all that is left are collection-initializer types.  These need to derive from
            // System.Collections.IEnumerable, and have an invokable no-arg constructor.

            // Abstract type don't have invokable constructors at all.
            if (namedType.IsAbstract)
                return false;

            if (foundType != null)
            {
                // If they have an accessible `public C(int capacity)` constructor, the lang prefers calling that.
                var constructors = namedType.Constructors;
                var capacityConstructor = GetAccessibleInstanceConstructor(constructors, c => c.Parameters is [{ Name: "capacity", Type.SpecialType: SpecialType.System_Int32 }]);
                if (capacityConstructor != null)
                    return true;

                var noArgConstructor =
                    GetAccessibleInstanceConstructor(constructors, c => c.Parameters.IsEmpty) ??
                    GetAccessibleInstanceConstructor(constructors, c => c.Parameters.All(p => p.IsOptional || p.IsParams));
                if (noArgConstructor != null)
                {
                    // If we have a struct, and the constructor we find is implicitly declared, don't consider this
                    // a constructible type.  It's likely the user would just get the `default` instance of the
                    // collection (like with ImmutableArray<T>) which would then not actually work.  If the struct
                    // does have an explicit constructor though, that's a good sign it can actually be constructed
                    // safely with the no-arg `new S()` call.
                    if (!(namedType.TypeKind == TypeKind.Struct && noArgConstructor.IsImplicitlyDeclared))
                        return true;
                }
            }
        }

        // Anything else is not constructible.
        elementType = null;
        return false;

        IMethodSymbol? GetAccessibleInstanceConstructor(ImmutableArray<IMethodSymbol> constructors, Func<IMethodSymbol, bool> predicate)
        {
            var constructor = constructors.FirstOrDefault(c => !c.IsStatic && predicate(c));
            return constructor is not null && constructor.IsAccessibleWithin(compilation.Assembly) ? constructor : null;
        }
    }

    public static ImmutableArray<IMethodSymbol>? TryGetCollectionBuilderFactoryMethods(
        Compilation compilation, INamedTypeSymbol collectionExpressionType)
    {
        var readonlySpanOfTType = compilation.ReadOnlySpanOfTType();
        var attribute = collectionExpressionType.GetAttributes().FirstOrDefault(
            static a => a.AttributeClass.IsCollectionBuilderAttribute());

        // https://github.com/dotnet/csharplang/blob/main/proposals/collection-expression-arguments.md#create-method-candidates
        // A [CollectionBuilder(...)] attribute specifies the builder type and method name of a method to be invoked to
        // construct an instance of the collection type.
        if (attribute is not { ConstructorArguments: [{ Value: INamedTypeSymbol builderType }, { Value: string builderMethodName }] })
            return null;

        // Find all the static methods in the builder type with the given name that have a ReadOnlySpan<T> as their last
        // parameter, matching the arity of the returned collection type.  Then construct the construction method if
        // generic. And filter to only those that return the collection type being created.

        var builderMethods = builderType
            .GetMembers(builderMethodName)
            .OfType<IMethodSymbol>()
            .Where(m =>
                m.IsStatic &&
                m.Arity == collectionExpressionType.Arity &&
                m.Parameters is [.., var lastParameter] &&
                Equals(lastParameter.Type.OriginalDefinition, readonlySpanOfTType))
            .Select(m => m.Arity == 0 ? m : m.Construct(ImmutableCollectionsMarshal.AsArray(collectionExpressionType.TypeArguments)!))
            .Where(m => compilation.ClassifyCommonConversion(m.ReturnType, collectionExpressionType).IsIdentityOrImplicitReference());

        return [.. builderMethods];
    }
}

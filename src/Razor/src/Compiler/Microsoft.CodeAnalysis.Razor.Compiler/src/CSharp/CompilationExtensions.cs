// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

internal static class CompilationExtensions
{
    public static bool HasAddComponentParameter(this Compilation compilation)
    {
        return compilation.GetTypesByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder")
            .Any(static t =>
                t.DeclaredAccessibility == Accessibility.Public &&
                t.GetMembers("AddComponentParameter")
                    .Any(static m => m.DeclaredAccessibility == Accessibility.Public));
    }

    /// <summary>
    /// Determines whether the type identified by <paramref name="typeMetadataName"/> has a callable
    /// instance <c>WriteLiteral(ReadOnlySpan&lt;byte&gt;)</c> overload accessible from that type.
    /// </summary>
    public static bool HasCallableUtf8WriteLiteralOverload(this Compilation compilation, string typeMetadataName)
    {
        var type = compilation.GetTypeByMetadataName(typeMetadataName);
        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return false;
        }

        return compilation.HasCallableUtf8WriteLiteralOverload(type);
    }

    /// <summary>
    /// Determines whether the given <paramref name="type"/> has a callable
    /// instance <c>WriteLiteral(ReadOnlySpan&lt;byte&gt;)</c> overload that is accessible
    /// to a generated subclass living in <paramref name="compilation"/>'s assembly.
    /// </summary>
    public static bool HasCallableUtf8WriteLiteralOverload(this Compilation compilation, INamedTypeSymbol type)
    {
        var readOnlySpanType = compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");
        var byteType = compilation.GetSpecialType(SpecialType.System_Byte);
        if (readOnlySpanType is not INamedTypeSymbol readOnlySpanNamedType ||
            byteType.TypeKind == TypeKind.Error)
        {
            return false;
        }

        var readOnlySpanOfByte = readOnlySpanNamedType.Construct(byteType);

        for (var currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var member in currentType.GetMembers("WriteLiteral"))
            {
                if (member is IMethodSymbol
                    {
                        IsStatic: false,
                        ReturnsVoid: true,
                        Parameters: [{ Type: var paramType }]
                    } method &&
                    SymbolEqualityComparer.Default.Equals(paramType, readOnlySpanOfByte) &&
                    IsAccessibleFromGeneratedSubclass(method, compilation))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanConvertStringLiteral(this Compilation compilation, ITypeSymbol type)
    {
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        if (stringType.TypeKind == TypeKind.Error ||
            SymbolEqualityComparer.Default.Equals(type, stringType))
        {
            return false;
        }

        var conversion = compilation.ClassifyConversion(source: stringType, destination: type);
#pragma warning disable RSEXPERIMENTAL006 // Conversion.IsUnion is a preview language feature API.
        return conversion.IsImplicit && conversion.IsUnion;
#pragma warning restore RSEXPERIMENTAL006
    }

    /// <summary>
    /// Determines whether <paramref name="method"/> would be callable from a class derived
    /// from its containing type, where the derived class is declared in
    /// <paramref name="compilation"/>'s assembly. This models the generated Razor page subclass:
    /// using the base type itself as the lookup context (as <c>IsSymbolAccessibleWithin</c>
    /// would) is wrong because that would make <see langword="private"/> overloads -- and
    /// <see langword="internal"/> overloads from a referenced assembly -- look callable.
    /// </summary>
    private static bool IsAccessibleFromGeneratedSubclass(IMethodSymbol method, Compilation compilation)
    {
        return method.DeclaredAccessibility switch
        {
            Accessibility.Public => true,

            // Always accessible from any derived class.
            Accessibility.Protected or Accessibility.ProtectedOrInternal => true,

            // `internal` and `private protected` need the derived class to live in the
            // declaring assembly (or one with InternalsVisibleTo to it).
            Accessibility.Internal or Accessibility.ProtectedAndInternal =>
                method.ContainingAssembly is { } declaringAssembly &&
                (SymbolEqualityComparer.Default.Equals(declaringAssembly, compilation.Assembly) ||
                 declaringAssembly.GivesAccessTo(compilation.Assembly)),

            // `private` (and anything unrecognized) is never accessible from a separate type.
            _ => false,
        };
    }
}

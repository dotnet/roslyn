// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

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
    /// instance <c>WriteLiteral(ReadOnlySpan&lt;byte&gt;)</c> overload accessible from that type.
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
                    compilation.IsSymbolAccessibleWithin(method, type))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

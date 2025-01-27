// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class TypeSyntaxExtensions
{
    public static bool IsVoid(this TypeSyntax typeSyntax)
        => typeSyntax is PredefinedTypeSyntax predefinedType &&
           predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);

    public static bool IsPartial(this TypeSyntax typeSyntax)
    {
        return typeSyntax is IdentifierNameSyntax &&
            ((IdentifierNameSyntax)typeSyntax).Identifier.IsKind(SyntaxKind.PartialKeyword);
    }

    /// <summary>
    /// Determines whether the specified TypeSyntax is actually 'var'.
    /// </summary>
    public static bool IsTypeInferred(this TypeSyntax typeSyntax, SemanticModel semanticModel)
    {
        if (!typeSyntax.IsVar)
        {
            return false;
        }

        if (semanticModel.GetAliasInfo(typeSyntax) != null)
        {
            return false;
        }

        var type = semanticModel.GetTypeInfo(typeSyntax).Type;
        if (type == null)
        {
            return false;
        }

        if (type.Name == "var")
        {
            return false;
        }

        return true;
    }

    public static TypeSyntax StripRefIfNeeded(this TypeSyntax type)
        => type is RefTypeSyntax refType ? refType.Type : type;
}

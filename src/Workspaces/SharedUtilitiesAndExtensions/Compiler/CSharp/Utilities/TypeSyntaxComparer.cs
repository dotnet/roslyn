// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Utilities;

internal sealed class TypeSyntaxComparer(IComparer<SyntaxToken> tokenComparer, IComparer<NameSyntax?> nameComparer)
    : IComparer<TypeSyntax?>
{
    private readonly IComparer<SyntaxToken> _tokenComparer = tokenComparer;
    internal readonly IComparer<NameSyntax?> NameComparer = nameComparer;

    public int Compare(TypeSyntax? x, TypeSyntax? y)
    {
        if (x is null)
            return y is null ? 0 : -1;
        else if (y is null)
            return 1;

        if (x == y)
        {
            return 0;
        }

        x = UnwrapType(x);
        y = UnwrapType(y);

        if (x is NameSyntax xName && y is NameSyntax yName)
        {
            return NameComparer.Compare(xName, yName);
        }

        // we have two predefined types, or a predefined type and a normal C# name.  We only need
        // to compare the first tokens here.
        return _tokenComparer.Compare(x.GetFirstToken(includeSkipped: true), y.GetFirstToken());
    }

    private static TypeSyntax UnwrapType(TypeSyntax type)
    {
        while (true)
        {
            switch (type.Kind())
            {
                case SyntaxKind.ArrayType:
                    type = ((ArrayTypeSyntax)type).ElementType;
                    break;
                case SyntaxKind.PointerType:
                    type = ((PointerTypeSyntax)type).ElementType;
                    break;
                case SyntaxKind.NullableType:
                    type = ((NullableTypeSyntax)type).ElementType;
                    break;
                default:
                    return type;
            }
        }
    }
}

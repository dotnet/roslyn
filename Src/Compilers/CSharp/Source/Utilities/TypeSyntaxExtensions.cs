// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal static class TypeSyntaxExtensions
    {
        internal static string SimpleName(this TypeSyntax syntax)
        {
            if (syntax is NameSyntax)
            {
                return (syntax as NameSyntax).SimpleName();
            }

            if (syntax is PredefinedTypeSyntax)
            {
                return (syntax as PredefinedTypeSyntax).Keyword.ValueText;
            }

            if (syntax is PointerTypeSyntax)
            {
                return ((PointerTypeSyntax)syntax).ElementType.SimpleName() + ((PointerTypeSyntax)syntax).AsteriskToken.ValueText;
            }

            throw new NotImplementedException();
        }
    }
}

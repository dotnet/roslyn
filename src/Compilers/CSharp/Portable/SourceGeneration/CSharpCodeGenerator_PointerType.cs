// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static PointerTypeSyntax GeneratePointerTypeSyntaxWithoutNullable(
            IPointerTypeSymbol symbol, bool onlyNames)
        {
            if (onlyNames)
                throw new ArgumentException("Pointer cannot be generated in a name-only location.");

            return PointerType(symbol.PointedAtType.GenerateTypeSyntax());
        }
    }
}

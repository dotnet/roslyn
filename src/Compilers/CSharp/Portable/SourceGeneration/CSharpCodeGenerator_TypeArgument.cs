// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static TypeArgumentListSyntax GenerateTypeArgumentList(ImmutableArray<ITypeSymbol> types)
        {
            using var _ = GetArrayBuilder<TypeSyntax>(out var builder);

            foreach (var type in types)
                builder.Add(type.GenerateTypeSyntax());

            return TypeArgumentList(SeparatedList(builder));
        }
    }
}

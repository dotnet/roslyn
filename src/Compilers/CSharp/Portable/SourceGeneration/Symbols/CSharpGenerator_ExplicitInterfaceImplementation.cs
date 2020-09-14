// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static ExplicitInterfaceSpecifierSyntax? GenerateExplicitInterfaceSpecification<TSymbol>(
            ImmutableArray<TSymbol> explicitInterfaceImplementations) where TSymbol : ISymbol
        {
            if (explicitInterfaceImplementations.Length == 0)
                return null;

            if (explicitInterfaceImplementations.Length >= 2)
                throw new ArgumentException("C# symbols cannot have multiple explicit interface specifications");

            var containingType = explicitInterfaceImplementations[0].ContainingType;
            if (containingType == null)
                return null;

            return ExplicitInterfaceSpecifier(containingType.GenerateNameSyntax());
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static MemberDeclarationSyntax GenerateEventDeclaration(IEventSymbol symbol)
        {
            if (GenerateEventField(symbol))
                return GenerateEventFieldDeclaration(symbol);

            return EventDeclaration(
                GenerateAttributeLists(symbol.GetAttributes()),
                GenerateModifiers(symbol.DeclaredAccessibility, symbol.GetModifiers()),
                symbol.Type.GenerateTypeSyntax(),
                GenerateExplicitInterfaceSpecification(symbol.ExplicitInterfaceImplementations),
                Identifier(symbol.Name),
                GenerateEventAccessorList(symbol));
        }

        private static AccessorListSyntax GenerateEventAccessorList(IEventSymbol symbol)
        {
            using var _ = GetArrayBuilder<AccessorDeclarationSyntax>(out var accessors);

            accessors.AddIfNotNull(GenerateAccessorDeclaration(SyntaxKind.AddAccessorDeclaration, symbol.AddMethod));
            accessors.AddIfNotNull(GenerateAccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration, symbol.RemoveMethod));

            return AccessorList(List(accessors));
        }

        private static bool GenerateEventField(IEventSymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
                return false;

            if (symbol.AddMethod == null && symbol.RemoveMethod == null && symbol.RaiseMethod == null)
                return true;

            return symbol.ContainingType != null &&
                   symbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(
                       f => Equals(f.AssociatedSymbol, symbol));
        }

        private static EventFieldDeclarationSyntax GenerateEventFieldDeclaration(IEventSymbol symbol)
        {
            return EventFieldDeclaration(
                GenerateAttributeLists(symbol.GetAttributes()),
                GenerateModifiers(symbol.DeclaredAccessibility, symbol.GetModifiers()),
                VariableDeclaration(symbol.Type.GenerateTypeSyntax(), SingletonSeparatedList(
                    VariableDeclarator(Identifier(symbol.Name)))));
        }
    }
}

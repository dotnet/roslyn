// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers;

internal static partial class AddOrRemoveAccessibilityModifiersHelpers
{
    public static void UpdateDeclaration(
        SyntaxEditor editor, ISymbol symbol, SyntaxNode declaration)
    {
        Contract.ThrowIfNull(symbol);

        var preferredAccessibility = GetPreferredAccessibility(symbol);

        // Check to see if we need to add or remove
        // If there's a modifier, then we need to remove it, otherwise no modifier, add it.
        editor.ReplaceNode(
            declaration,
            (currentDeclaration, _) => UpdateAccessibility(currentDeclaration, preferredAccessibility));

        return;

        SyntaxNode UpdateAccessibility(SyntaxNode declaration, Accessibility preferredAccessibility)
        {
            var generator = editor.Generator;

            // If there was accessibility on the member, then remove it.  If there was no accessibility, then add
            // the preferred accessibility for this member.
            return generator.GetAccessibility(declaration) == Accessibility.NotApplicable
                ? generator.WithAccessibility(declaration, preferredAccessibility)
                : generator.WithAccessibility(declaration, Accessibility.NotApplicable);
        }
    }

    private static Accessibility GetPreferredAccessibility(ISymbol symbol)
    {
        // If we have an overridden member, then if we're adding an accessibility modifier, use the
        // accessibility of the member we're overriding as both should be consistent here.
        if (symbol.GetOverriddenMember() is { DeclaredAccessibility: var accessibility })
            return accessibility;

        // Default abstract members to be protected, and virtual members to be public.  They can't be private as
        // that's not legal.  And these are reasonable default values for them.
        if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol)
        {
            if (symbol.ContainingType?.TypeKind == TypeKind.Interface)
                return Accessibility.Public;

            if (symbol.IsAbstract)
                return Accessibility.Protected;

            if (symbol.IsVirtual)
                return Accessibility.Public;
        }

        // Otherwise, default to whatever accessibility no-accessibility means for this member;
        return symbol.DeclaredAccessibility;
    }
}

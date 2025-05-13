// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddOrRemoveAccessibilityModifiers;

internal class CSharpAddOrRemoveAccessibilityModifiers : AbstractAddOrRemoveAccessibilityModifiers<MemberDeclarationSyntax>
{
    public static readonly CSharpAddOrRemoveAccessibilityModifiers Instance = new();

    protected CSharpAddOrRemoveAccessibilityModifiers()
    {
    }

    public override bool ShouldUpdateAccessibilityModifier(
        IAccessibilityFacts accessibilityFacts,
        MemberDeclarationSyntax member,
        AccessibilityModifiersRequired option,
        out SyntaxToken name,
        out bool modifierAdded)
    {
        modifierAdded = false;

        // Have to have a name to report the issue on.
        name = member.GetNameToken();
        if (name.Kind() == SyntaxKind.None)
            return false;

        // User has no preference set.  Do not add or remove any accessibility modifiers.
        if (option == AccessibilityModifiersRequired.Never)
            return false;

        // Certain members never have accessibility. Don't bother reporting on them.
        if (!accessibilityFacts.CanHaveAccessibility(member))
            return false;

        // Find what accessibility the member was *directly* declared with.  This only considers the modifiers on the
        // member itself.  Not any sort of computed accessibility based on the containing type.
        var accessibility = accessibilityFacts.GetAccessibility(member);

        if (option == AccessibilityModifiersRequired.Always)
        {
            // User *always* wants to have explicit accessibility modifiers.  So add if the member doesn't have any
            // explicit modifiers currently.
            modifierAdded = true;
            return accessibility == Accessibility.NotApplicable;
        }

        if (option == AccessibilityModifiersRequired.ForNonInterfaceMembers)
        {
            if (member.Parent is InterfaceDeclarationSyntax)
            {
                // We want to have require accessibility modifiers on non-interface-members, *excluding* only public
                // interface members due to the long history of this being the only way to declare interface members.
                // So remove an explicit `public` from an interface member if present.
                modifierAdded = false;
                return accessibility == Accessibility.Public;
            }
            else
            {
                // We want to have require accessibility modifiers on non-interface-members, and this is a non-interface
                // member. So add if the member doesn't have any accessibility modifiers currently.
                modifierAdded = true;
                return accessibility == Accessibility.NotApplicable;
            }
        }

        // Only option left is to remove the accessibility modifier if it matches the default for the containing symbol
        Contract.ThrowIfFalse(option == AccessibilityModifiersRequired.OmitIfDefault);

        // We want to omit redundant accessibility modifiers. If the member already doesn't have any accessibility
        // modifiers, then there's nothing we could even remove.
        if (accessibility == Accessibility.NotApplicable)
            return false;

        modifierAdded = false;
        switch (member.GetRequiredParent().Kind())
        {
            case SyntaxKind.CompilationUnit:
            case SyntaxKind.FileScopedNamespaceDeclaration:
            case SyntaxKind.NamespaceDeclaration:
                // Inside a namespace, default is internal, and can be removed if explicitly stated.
                return accessibility == Accessibility.Internal;

            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
                // Inside a type, default is private, and can be removed if explicitly stated.
                return accessibility == Accessibility.Private;

            case SyntaxKind.InterfaceDeclaration:
                // Inside an interface, default is public, and can be removed if explicitly stated.
                return accessibility == Accessibility.Public;
        }

        return false;
    }
}

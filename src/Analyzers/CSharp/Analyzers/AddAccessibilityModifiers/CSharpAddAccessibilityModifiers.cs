// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;

internal class CSharpAddAccessibilityModifiers : AbstractAddAccessibilityModifiers<MemberDeclarationSyntax>
{
    public static readonly CSharpAddAccessibilityModifiers Instance = new();

    protected CSharpAddAccessibilityModifiers()
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

        // Certain members never have accessibility. Don't bother reporting on them.
        if (!accessibilityFacts.CanHaveAccessibility(member))
            return false;

        // Find what accessibility the member was *directly* declared with.  This only considers the modifiers on the
        // member itself.  Not any sort of computed accessibility based on the containing type.
        var accessibility = accessibilityFacts.GetAccessibility(member);

        if (option == AccessibilityModifiersRequired.ForNonInterfaceMembers &&
            member.Parent is InterfaceDeclarationSyntax)
        {
            // A member in an interface explicitly declared as 'public'.  Remove this modifier as it's the default for
            // interfaces, and the user only wants explicit default accessibility modifiers for things *outside* of interfaces.
            if (accessibility == Accessibility.Public)
            {
                modifierAdded = false;
                return true;
            }

            return false;
        }

        if (option != AccessibilityModifiersRequired.OmitIfDefault)
        {
            // We want to have explicit accessibility modifiers.  So add if the member doesn't have any accessibility
            // modifiers currently.
            modifierAdded = true;
            return accessibility == Accessibility.NotApplicable;
        }

        // We want to omit redundant accessibility modifiers. If the member already doesn't have any accessibility
        // modifiers, then there's nothing to remove.
        if (accessibility == Accessibility.NotApplicable)
            return false;

        var parentKind = member.GetRequiredParent().Kind();
        switch (parentKind)
        {
            // Check for default modifiers in namespace and outside of namespace
            case SyntaxKind.CompilationUnit:
            case SyntaxKind.FileScopedNamespaceDeclaration:
            case SyntaxKind.NamespaceDeclaration:
                {
                    // Default is internal
                    if (accessibility != Accessibility.Internal)
                        return false;
                }

                break;

            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
                {
                    // Inside a type, default is private
                    if (accessibility != Accessibility.Private)
                        return false;
                }

                break;

            case SyntaxKind.InterfaceDeclaration:
                {
                    // Inside an interface, default is public
                    if (accessibility != Accessibility.Public)
                        return false;
                }

                break;

            default:
                return false; // Unknown parent kind, don't do anything
        }

        // Looks like a member whose declared accessibility matches the default accessibility for its parent.
        // We can remove this modifier.
        modifierAdded = false;
        return true;
    }
}

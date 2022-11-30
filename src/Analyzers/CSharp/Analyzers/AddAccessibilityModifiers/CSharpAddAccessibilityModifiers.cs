// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
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

            // This analyzer bases all of its decisions on the accessibility
            var accessibility = accessibilityFacts.GetAccessibility(member);

            // Omit will flag any accessibility values that exist and are default
            // The other options will remove or ignore accessibility
            var isOmit = option == AccessibilityModifiersRequired.OmitIfDefault;
            modifierAdded = !isOmit;

            if (isOmit)
            {
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

                    default:
                        return false; // Unknown parent kind, don't do anything
                }
            }
            else
            {
                // Mode is always, so we have to flag missing modifiers
                if (accessibility != Accessibility.NotApplicable)
                    return false;
            }

            return true;
        }
    }
}

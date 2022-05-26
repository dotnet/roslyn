﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.AddAccessibilityModifiers
{
    internal abstract class AbstractAddAccessibilityModifiers<TMemberDeclarationSyntax> : IAddAccessibilityModifiers
        where TMemberDeclarationSyntax : SyntaxNode
    {
        public bool ShouldUpdateAccessibilityModifier(
            IAccessibilityFacts accessibilityFacts,
            SyntaxNode member,
            AccessibilityModifiersRequired option,
            out SyntaxToken name)
        {
            name = default;
            return member is TMemberDeclarationSyntax memberDecl &&
                ShouldUpdateAccessibilityModifier(accessibilityFacts, memberDecl, option, out name);
        }

        public abstract bool ShouldUpdateAccessibilityModifier(
            IAccessibilityFacts accessibilityFacts,
            TMemberDeclarationSyntax member,
            AccessibilityModifiersRequired option,
            out SyntaxToken name);
    }
}

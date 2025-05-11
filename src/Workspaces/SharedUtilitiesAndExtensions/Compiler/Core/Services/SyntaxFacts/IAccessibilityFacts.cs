// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageService;

internal interface IAccessibilityFacts
{
    /// <summary>
    /// Returns whether a given declaration can have accessibility or not.
    /// </summary>
    /// <param name="declaration">The declaration node to check</param>
    /// <param name="ignoreDeclarationModifiers">A flag that indicates whether to consider modifiers on the given declaration that blocks adding accessibility.</param>
    bool CanHaveAccessibility(SyntaxNode declaration, bool ignoreDeclarationModifiers = false);
    Accessibility GetAccessibility(SyntaxNode declaration);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Editing;

internal static class ModifierExtensions
{
    public static DeclarationModifiers ToDeclarationModifiers(this Modifiers modifiers)
    {
#if WORKSPACE
        return new DeclarationModifiers(modifiers);
#else
        return Unsafe.As<Modifiers, DeclarationModifiers>(ref modifiers);
#endif
    }
}

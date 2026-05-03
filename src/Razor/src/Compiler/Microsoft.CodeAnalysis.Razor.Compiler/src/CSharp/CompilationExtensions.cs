// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

internal static class CompilationExtensions
{
    public static bool HasAddComponentParameter(this Compilation compilation)
    {
        return compilation.GetTypesByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder")
            .Any(static t =>
                t.DeclaredAccessibility == Accessibility.Public &&
                t.GetMembers("AddComponentParameter")
                    .Any(static m => m.DeclaredAccessibility == Accessibility.Public));
    }
}

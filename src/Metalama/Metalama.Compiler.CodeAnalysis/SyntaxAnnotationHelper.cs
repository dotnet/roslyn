// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler;

/// <summary>
/// Implements fast access to annotations without allocating memory.
/// </summary>
internal static class SyntaxAnnotationHelper
{
    public static bool TryGetAnnotationFast(this SyntaxNode node, string kind, out SyntaxAnnotation? annotation)
    {
        var array = node.GetAnnotations();
        foreach (var item in array)
        {
            if (string.Equals(item.Kind, kind, StringComparison.Ordinal))
            {
                annotation = item;
                return true;
            }
        }

        annotation = null;
        return false;
    }

    public static bool TryGetAnnotationFast(this SyntaxToken token, string kind, out SyntaxAnnotation? annotation)
    {
        var array = token.Node?.GetAnnotations();

        if (array == null)
        {
            annotation = null;
            return false;
        }

        foreach (var item in array)
        {
            if (string.Equals(item.Kind, kind, StringComparison.Ordinal))
            {
                annotation = item;
                return true;
            }
        }

        annotation = null;
        return false;
    }
}

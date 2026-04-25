// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Razor.Diagnostics.Analyzers;

internal static class Extensions
{
    public static Diagnostic CreateDiagnostic(this IOperation operation, DiagnosticDescriptor rule, params object?[]? messageArgs)
    {
        var location = operation.Syntax.GetLocation();

        if (!location.IsInSource)
        {
            location = Location.None;
        }

        return Diagnostic.Create(rule, location, messageArgs);
    }
}

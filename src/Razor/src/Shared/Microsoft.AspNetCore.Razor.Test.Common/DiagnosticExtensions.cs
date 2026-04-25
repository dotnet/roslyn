// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis;

public static class RazorDiagnosticExtensions
{
    public static void Verify(
        this IEnumerable<RazorDiagnostic> diagnostics,
        params DiagnosticDescription[] expected)
    {
        diagnostics.Select(d => d.AsDiagnostic()).Verify(expected);
    }
}

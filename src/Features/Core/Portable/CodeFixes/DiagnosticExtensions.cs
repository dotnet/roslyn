// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeFixes;

internal static class DiagnosticExtensions
{
    public static bool IsMoreSevereThanOrEqualTo(this DiagnosticSeverity left, DiagnosticSeverity right)
    {
        var leftInt = (int)left;
        var rightInt = (int)right;
        return leftInt >= rightInt;
    }
}

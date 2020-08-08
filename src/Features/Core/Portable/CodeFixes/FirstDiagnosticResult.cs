// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal readonly struct FirstDiagnosticResult
    {
        public readonly bool PartialResult;
        public readonly bool HasFix;
        public readonly DiagnosticData Diagnostic;

        public FirstDiagnosticResult(bool partialResult, bool hasFix, DiagnosticData diagnostic)
        {
            PartialResult = partialResult;
            HasFix = hasFix;
            Diagnostic = diagnostic;
        }
    }
}

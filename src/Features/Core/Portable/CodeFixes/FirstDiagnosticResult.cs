// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal readonly struct FirstDiagnosticResult
    {
        public readonly bool PartialResult;
        public readonly DiagnosticData? Diagnostic;

        [MemberNotNullWhen(true, nameof(Diagnostic))]
        public bool HasFix => Diagnostic != null;

        public FirstDiagnosticResult(bool partialResult, DiagnosticData? diagnostic)
        {
            PartialResult = partialResult;
            Diagnostic = diagnostic;
        }
    }
}

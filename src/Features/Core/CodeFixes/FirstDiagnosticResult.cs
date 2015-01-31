// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal struct FirstDiagnosticResult
    {
        public readonly bool PartialResult;
        public readonly bool HasFix;
        public readonly DiagnosticData Diagnostic;

        public FirstDiagnosticResult(bool partialResult, bool hasFix, DiagnosticData diagnostic)
        {
            this.PartialResult = partialResult;
            this.HasFix = hasFix;
            this.Diagnostic = diagnostic;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class DiagnosticBagErrorLogger : ErrorLogger
    {
        internal readonly DiagnosticBag Diagnostics;

        internal DiagnosticBagErrorLogger(DiagnosticBag diagnostics)
        {
            Diagnostics = diagnostics;
        }

        public override void LogDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class AnalyzerExceptionDiagnosticArgs : EventArgs
    {
        public readonly Diagnostic Diagnostic;
        public readonly DiagnosticAnalyzer FaultedAnalyzer;

        public AnalyzerExceptionDiagnosticArgs(DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            this.FaultedAnalyzer = analyzer;
            this.Diagnostic = diagnostic;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class WorkspaceAnalyzerExceptionDiagnosticArgs : EventArgs
    {
        public readonly Diagnostic Diagnostic;
        public readonly DiagnosticAnalyzer FaultedAnalyzer;
        public readonly Workspace Workspace;

        public WorkspaceAnalyzerExceptionDiagnosticArgs(AnalyzerExceptionDiagnosticArgs args, Workspace workspace)
            : this(args.FaultedAnalyzer, args.Diagnostic, workspace)
        {            
        }

        public WorkspaceAnalyzerExceptionDiagnosticArgs(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Workspace workspace)
        {
            this.FaultedAnalyzer = analyzer;
            this.Diagnostic = diagnostic;
            this.Workspace = workspace;
        }
    }
}

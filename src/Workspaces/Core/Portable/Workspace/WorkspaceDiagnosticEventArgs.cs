// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public class WorkspaceDiagnosticEventArgs : EventArgs
    {
        public WorkspaceDiagnostic Diagnostic { get; }

        public WorkspaceDiagnosticEventArgs(WorkspaceDiagnostic diagnostic)
        {
            this.Diagnostic = diagnostic;
        }
    }
}

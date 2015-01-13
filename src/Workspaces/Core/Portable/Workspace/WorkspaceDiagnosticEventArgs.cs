// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public class WorkspaceDiagnosticEventArgs : EventArgs
    {
        public WorkspaceDiagnostic Diagnostic { get; private set; }

        public WorkspaceDiagnosticEventArgs(WorkspaceDiagnostic diagnostic)
        {
            this.Diagnostic = diagnostic;
        }
    }
}
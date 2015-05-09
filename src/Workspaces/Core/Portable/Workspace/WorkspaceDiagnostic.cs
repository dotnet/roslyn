// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public class WorkspaceDiagnostic
    {
        public WorkspaceDiagnosticKind Kind { get; }
        public string Message { get; }

        public WorkspaceDiagnostic(WorkspaceDiagnosticKind kind, string message)
        {
            this.Kind = kind;
            this.Message = message;
        }

        public override string ToString()
        {
            return GetDebuggerDisplay();
        }

        /// <remarks>Internal for testing purposes</remarks>
        internal string GetDebuggerDisplay()
        {
            return string.Format("[{0}] {1}", this.Kind.ToString(), this.Message);
        }
    }
}

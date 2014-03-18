// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{DebuggerText,nq}")]
    public abstract class WorkspaceDiagnostic
    {
        public WorkspaceDiagnosticKind Kind { get; private set; }
        public string Message { get; private set; }

        public WorkspaceDiagnostic(WorkspaceDiagnosticKind kind, string message)
        {
            this.Kind = kind;
            this.Message = message;
        }

        public override string ToString()
        {
            return DebuggerText;
        }

        internal string DebuggerText
        {
            get
            {
                return string.Format("[{0}] {1}", this.Kind.ToString(), this.Message);
            }
        }
    }
}
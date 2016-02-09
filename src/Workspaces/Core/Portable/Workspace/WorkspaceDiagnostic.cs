// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
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
            string kindText;

            switch (Kind)
            {
                case WorkspaceDiagnosticKind.Failure: kindText = WorkspacesResources.Failure; break;
                case WorkspaceDiagnosticKind.Warning: kindText = WorkspacesResources.Warning; break;
                default: throw ExceptionUtilities.UnexpectedValue(Kind);
            }

            return $"[{kindText}] {Message}";
        }
    }
}

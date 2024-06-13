// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class WorkspaceDiagnostic(WorkspaceDiagnosticKind kind, string message)
    {
        public WorkspaceDiagnosticKind Kind { get; } = kind;
        public string Message { get; } = message;

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

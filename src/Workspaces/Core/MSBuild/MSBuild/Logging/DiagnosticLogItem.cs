// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.MSBuild.Logging
{
    internal class DiagnosticLogItem
    {
        public WorkspaceDiagnosticKind Kind { get; }
        public string Message { get; }
        public string ProjectFilePath { get; }

        public DiagnosticLogItem(WorkspaceDiagnosticKind kind, string message, string projectFilePath)
        {
            Kind = kind;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ProjectFilePath = projectFilePath ?? throw new ArgumentNullException(nameof(message));
        }

        public DiagnosticLogItem(WorkspaceDiagnosticKind kind, Exception exception, string projectFilePath)
            : this(kind, exception.Message, projectFilePath)
        {
        }

        public override string ToString() => Message;
    }
}

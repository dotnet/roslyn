// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

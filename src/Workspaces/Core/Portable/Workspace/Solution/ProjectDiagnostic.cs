// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    public class ProjectDiagnostic : WorkspaceDiagnostic
    {
        public ProjectId ProjectId { get; }

        public ProjectDiagnostic(WorkspaceDiagnosticKind kind, string message, ProjectId projectId)
            : base(kind, message)
        {
            this.ProjectId = projectId;
        }
    }
}

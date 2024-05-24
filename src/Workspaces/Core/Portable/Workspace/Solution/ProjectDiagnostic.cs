// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis;

public class ProjectDiagnostic(WorkspaceDiagnosticKind kind, string message, ProjectId projectId) : WorkspaceDiagnostic(kind, message)
{
    public ProjectId ProjectId { get; } = projectId;
}

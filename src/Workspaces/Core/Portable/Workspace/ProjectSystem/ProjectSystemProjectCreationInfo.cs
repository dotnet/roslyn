// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal class ProjectSystemProjectCreationInfo
{
    public string? AssemblyName { get; set; }
    public CompilationOptions? CompilationOptions { get; set; }
    public string? FilePath { get; set; }
    public ParseOptions? ParseOptions { get; set; }
    public string? CompilationOutputAssemblyFilePath { get; set; }

    public Guid TelemetryId { get; set; }
}
